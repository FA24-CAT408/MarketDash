using KinematicCharacterController;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrazyMarket.Player.V2.Unity
{
    /// <summary>Optional butter reserve and air dash, composed on the player that uses butter.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerControllerV2))]
    public sealed class ButterBody : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer bodySkin;
        [SerializeField] private ButterVfx trail;
        [SerializeField, Min(1)] private int dashCapacity = 4;
        [SerializeField, Min(1f)] private float dashSpeed = 24f;
        [SerializeField, Min(.02f)] private float dashDuration = .18f;
        [SerializeField, Min(.02f)] private float dashCooldown = .35f;
        [SerializeField, Range(.1f, 1f)] private float emptySpeedMultiplier = .55f;
        [SerializeField, Range(.1f, 1f)] private float emptyMassMultiplier = .3f;

        private PlayerControllerV2 player;
        private KinematicCharacterMotor motor;
        private ButterSurfaceMovement surfaceMovement;
        private InputAction dashInput, recallInput;
        private Vector3 dashDirection;
        private int reserveShape = -1;
        private float initialShape;
        private float initialMass, dashTime, cooldown;
        private const float RefillDuration = .7f;
        private float refillTime, refillFrom;
        private int spentDashes;
        private bool dashQueued, recallQueued;

        public int RemainingDashes => Mathf.Max(0, Mathf.Max(1, dashCapacity) - spentDashes);
        public float Reserve => RemainingDashes / (float)Mathf.Max(1, dashCapacity);
        public float MoveSpeedMultiplier => Mathf.Lerp(emptySpeedMultiplier, 1f, Reserve);
        public bool IsDashing => dashTime > 0f;

        private void Awake()
        {
            player = GetComponent<PlayerControllerV2>();
            motor = GetComponent<KinematicCharacterMotor>();
            surfaceMovement = GetComponent<ButterSurfaceMovement>();
            if (trail == null) trail = GetComponentInChildren<ButterVfx>();
            initialMass = motor.SimulatedCharacterMass;
            if (bodySkin != null && bodySkin.sharedMesh != null)
            {
                reserveShape = bodySkin.sharedMesh.GetBlendShapeIndex("Butter reserve");
                if (reserveShape >= 0) initialShape = bodySkin.GetBlendShapeWeight(reserveShape);
            }
            dashInput = new InputAction("Butter dash", InputActionType.Button, "<Keyboard>/leftShift");
            dashInput.AddBinding("<Gamepad>/rightTrigger");
            recallInput = new InputAction("Recall butter", InputActionType.Button, "<Keyboard>/r");
            recallInput.AddBinding("<Gamepad>/leftShoulder");
            dashInput.performed += _ => { if (CanReadInput) dashQueued = true; };
            recallInput.performed += _ => { if (CanReadInput) recallQueued = true; };
        }

        private bool CanReadInput => player != null && player.isActiveAndEnabled &&
            !player.Snapshot.ControlBlocked && Time.timeScale > 0f;

        private void OnEnable()
        {
            dashInput.Enable();
            recallInput.Enable();
        }

        internal void BeginMotorStep(bool teleported, bool blocked, float deltaTime)
        {
            if (teleported)
            {
                RestoreButter();
                cooldown = 0f;
            }
            if (blocked)
            {
                dashQueued = recallQueued = false;
                dashTime = 0f;
                return;
            }
            cooldown = Mathf.Max(0f, cooldown - deltaTime);
            if (recallQueued)
            {
                recallQueued = false;
                if (refillTime <= 0f && (trail == null || !trail.GatheringButter)) RestoreButter(true);
            }
        }

        internal void ApplyDash(ref Vector3 velocity, Vector3 intent, Vector3 up, bool grounded,
            bool blocked, bool jumped, float deltaTime)
        {
            bool requested = dashQueued;
            dashQueued = false;
            if (blocked || grounded || jumped)
            {
                dashTime = 0f;
                return;
            }
            if (requested && cooldown <= 0f && RemainingDashes > 0)
            {
                dashDirection = Vector3.ProjectOnPlane(intent, up);
                if (dashDirection.sqrMagnitude < .0025f)
                    dashDirection = Vector3.ProjectOnPlane(transform.forward, up);
                dashDirection.Normalize();
                spentDashes++;
                refillTime = 0f;
                dashTime = dashDuration;
                cooldown = Mathf.Max(dashDuration, dashCooldown);
                ApplyBody();
                if (trail != null) trail.PlayDash(dashDirection);
            }
            if (dashTime <= 0f) return;
            // The motor still owns collision resolution. A short horizontal burst suspends falling.
            velocity = dashDirection * dashSpeed;
            dashTime = Mathf.Max(0f, dashTime - deltaTime);
        }

        internal void OnMovementHit(Vector3 normal)
        {
            if (Vector3.Dot(normal, dashDirection) < -.1f) dashTime = 0f;
        }

        private void RestoreButter(bool animate = false)
        {
            refillFrom = bodySkin != null && reserveShape >= 0 ? bodySkin.GetBlendShapeWeight(reserveShape) : 0f;
            refillTime = animate ? RefillDuration : 0f;
            spentDashes = 0;
            dashQueued = recallQueued = false;
            dashTime = 0f;
            if (trail != null)
            {
                if (animate) trail.PlayRecall(RefillDuration);
                else trail.ClearTransientEffects();
            }
            if (surfaceMovement != null) surfaceMovement.ClearCoating();
            ApplyBody();
            if (animate && bodySkin != null && reserveShape >= 0)
                bodySkin.SetBlendShapeWeight(reserveShape, refillFrom);
        }

        private void ApplyBody()
        {
            float massFraction = Mathf.Lerp(emptyMassMultiplier, 1f, Reserve);
            motor.SimulatedCharacterMass = initialMass * massFraction;
            if (bodySkin != null && reserveShape >= 0)
                bodySkin.SetBlendShapeWeight(reserveShape, Mathf.Lerp(initialShape, 100f, 1f - Reserve));
        }

        private void Update()
        {
            if (refillTime > 0f)
            {
                refillTime = Mathf.Max(0f, refillTime - Time.deltaTime);
                float fill = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.15f, .9f, 1f - refillTime / RefillDuration));
                if (bodySkin != null && reserveShape >= 0)
                    bodySkin.SetBlendShapeWeight(reserveShape, Mathf.Lerp(refillFrom, initialShape, fill));
            }
            if (!CanReadInput) dashQueued = recallQueued = false;
            if (player != null && !player.isActiveAndEnabled && (spentDashes != 0 || IsDashing || refillTime > 0f ||
                (trail != null && trail.HasRecallEffect)))
                RestoreButter();
        }

        private void OnDisable()
        {
            dashInput?.Disable();
            recallInput?.Disable();
            if (motor != null) RestoreButter();
            cooldown = 0f;
        }

        private void OnDestroy()
        {
            dashInput?.Dispose();
            recallInput?.Dispose();
        }
    }
}
