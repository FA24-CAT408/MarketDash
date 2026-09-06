using UnityEngine;

namespace CrazyMarket.Player.V2.Unity
{
    /// <summary>Optional surface response for the butter experiment, evaluated by the V2 motor.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerControllerV2))]
    public sealed class ButterSurfaceMovement : MonoBehaviour
    {
        [SerializeField] private ButterVfx trail;
        [Header("Clean floor")]
        [SerializeField, Min(0f), Tooltip("Normal running speed. Existing profile speed is the maximum on butter.")]
        private float cleanMoveSpeed = 10f;
        [SerializeField, Min(.1f), Tooltip("Acceleration up to normal running speed on either surface. The profile controls bonus speed buildup on butter.")]
        private float normalAcceleration = 20f;
        [SerializeField, Min(0f), Tooltip("How quickly bonus speed or coasting fades on clean floor (1/s).")]
        private float cleanDecelerationSharpness = 3f;
        [SerializeField, Min(0f)] private float cleanTurnSharpness = 10f;
        [Header("Butter contact")]
        [SerializeField, Min(.2f), Tooltip("Fresh drops cannot immediately boost the player creating them.")]
        private float activationDelay = .45f;
        [Header("Buttered feet")]
        [SerializeField, Min(0f), Tooltip("Maximum coast braking while fully buttered (1/s). Lower profile values are preserved.")]
        private float butterCoastSharpness = .12f;
        [SerializeField, Min(.05f), Tooltip("Time in a mature pool to fully coat the feet.")]
        private float coatingBuildTime = .65f;
        [SerializeField, Min(.1f), Tooltip("How long a full coating lasts after leaving butter.")]
        private float coatingDuration = 4f;
        [SerializeField, Min(.1f), Tooltip("The final portion of the coating gradually restores dry-floor grip.")]
        private float coatingFadeTime = 2.5f;

        public bool IsOnButter { get; private set; }
        private ButterBody body;
        private float coatingTime;
        private bool canCoat;
        private float Reserve => body != null && body.isActiveAndEnabled ? body.Reserve : 1f;
        private float NormalMoveSpeed => cleanMoveSpeed *
            (body != null && body.isActiveAndEnabled ? body.MoveSpeedMultiplier : 1f);

        private void Awake()
        {
            if (trail == null) trail = GetComponentInChildren<ButterVfx>();
            body = GetComponent<ButterBody>();
        }

        internal void BeginMotorStep(bool teleported, bool blocked, float deltaTime)
        {
            canCoat = isActiveAndEnabled && !teleported && !blocked;
            if (!canCoat) ClearCoating();
            else
            {
                coatingTime = Mathf.Min(coatingTime, coatingDuration * Reserve);
                // The preceding contact step keeps the coating wet. Airborne time also wears it off.
                if (!IsOnButter) coatingTime = Mathf.Max(0f, coatingTime - deltaTime);
            }
            IsOnButter = false;
            // Clear before another physics step can sample the pre-teleport trail.
            if (teleported && trail != null) trail.ClearTransientEffects();
        }

        internal void ResolveGroundResponse(Collider floor, Vector3 contactPoint, float moveAmount, float deltaTime,
            ref Vector3 target, ref float deceleration, ref float turnSharpness)
        {
            IsOnButter = canCoat && trail != null && trail.ContainsButter(floor, contactPoint, activationDelay);
            float reserve = Reserve;
            if (IsOnButter)
            {
                coatingTime = Mathf.MoveTowards(coatingTime, coatingDuration * reserve,
                    deltaTime * coatingDuration / Mathf.Max(.05f, coatingBuildTime));
                float normalTarget = Mathf.Min(target.magnitude, NormalMoveSpeed * Mathf.Clamp01(moveAmount));
                target = target.normalized * Mathf.Lerp(normalTarget, target.magnitude, reserve);
            }
            else
                // Coating carries existing speed; only actual pools provide bonus acceleration.
                target = Vector3.ClampMagnitude(target, Mathf.Max(0f, NormalMoveSpeed) * Mathf.Clamp01(moveAmount));

            float coating = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(coatingTime / Mathf.Max(.1f, coatingFadeTime)));
            float slip = IsOnButter ? reserve : Mathf.Min(reserve, coating);
            deceleration = Mathf.Lerp(cleanDecelerationSharpness, Mathf.Min(deceleration, butterCoastSharpness), slip);
            turnSharpness = Mathf.Lerp(cleanTurnSharpness, turnSharpness, slip);
        }

        internal void LimitAirAcceleration(ref Vector3 velocity, Vector3 up, float previousPlanarSpeed)
        {
            // Steering may redirect carried momentum, but air input cannot create bonus speed.
            Vector3 planar = Vector3.ProjectOnPlane(velocity, up);
            float limit = Mathf.Max(previousPlanarSpeed, Mathf.Max(0f, NormalMoveSpeed));
            velocity += Vector3.ClampMagnitude(planar, limit) - planar;
        }

        internal float Accelerate(float speed, float targetSpeed, float moveAmount, float butterAcceleration, float deltaTime)
        {
            float normalTarget = Mathf.Min(targetSpeed, NormalMoveSpeed * Mathf.Clamp01(moveAmount));
            if (speed < normalTarget)
            {
                float acceleration = Mathf.Max(.1f, normalAcceleration);
                float timeToNormal = (normalTarget - speed) / acceleration;
                if (deltaTime <= timeToNormal) return Mathf.MoveTowards(speed, normalTarget, acceleration * deltaTime);
                speed = normalTarget;
                deltaTime -= timeToNormal;
            }
            // Only the remaining time can build bonus speed after reaching the normal pace.
            return Mathf.MoveTowards(speed, targetSpeed, Mathf.Max(0f, butterAcceleration) * deltaTime);
        }

        internal void ClearCoating()
        {
            coatingTime = 0f;
            IsOnButter = false;
        }

        private void OnDisable() => ClearCoating();
    }
}
