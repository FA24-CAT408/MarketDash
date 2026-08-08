using System.Collections.Generic;
using CrazyMarket.Player.V2;
using KinematicCharacterController;
using UnityEngine;

namespace CrazyMarket.Player.V2.Unity
{
    [DisallowMultipleComponent]
    public sealed class PlayerControllerV2 : MonoBehaviour, ICharacterController, IPlayerController
    {
        [Header("V2 composition")]
        [SerializeField] private PlayerProfile profile;
        [SerializeField] private InputReader input;
        [SerializeField] private KinematicCharacterMotor motor;
        [SerializeField] private Transform movementReference;

        [Header("Collision filtering")]
        [SerializeField] private List<Collider> ignoredColliders = new List<Collider>();

        private PlayerLocomotionMachine locomotion;
        private Vector2 rawMove;
        private bool jumpHeld;
        private bool jumpQueued;
        private bool movementEnabled = true;
        private bool inputSubscribed;
        private bool stepProducedOutput;
        private LocomotionOutput output;
        private Vector3 movementDirection;
        private float orientationSharpness;

        private const float MotorSafetyMagnitude = 1000000f;

        public PlayerSnapshot Snapshot => locomotion == null ? default : locomotion.Snapshot;
        public PlayerPresentationState Presentation => locomotion == null ? default : locomotion.Presentation;
        public Vector3 MovementDirection => movementDirection;

        public bool TryGetMovementIntent(out Vector3 direction)
        {
            direction = movementDirection;
            direction.y = 0f;
            return locomotion != null;
        }

        private void Awake()
        {
            if (motor == null)
                motor = GetComponent<KinematicCharacterMotor>();

            if (motor == null)
            {
                Debug.LogError("PlayerControllerV2 requires a KinematicCharacterMotor.", this);
                enabled = false;
                return;
            }

            PlayerBodyObservation initial = ObserveBody();
            locomotion = new PlayerLocomotionMachine(profile, initial);
            orientationSharpness = locomotion.CaptureRuntimeProfile().Locomotion.OrientationSharpness;
            motor.CharacterController = this;

            if (profile == null)
                Debug.LogWarning("PlayerControllerV2 is using the production fallback profile.", this);
            if (input == null)
                Debug.LogWarning("PlayerControllerV2 has no InputReader; movement will remain idle.", this);
        }

        private void OnEnable() => SubscribeToInput();

        private void OnDisable()
        {
            UnsubscribeFromInput();
            rawMove = Vector2.zero;
            movementDirection = Vector3.zero;
            jumpHeld = false;
            jumpQueued = false;
        }

        public PlayerOperationResult SetControlBlocked(string source, bool blocked) =>
            locomotion == null
                ? PlayerOperationResult.RejectedInvalidProfile
                : locomotion.SetControlBlocked(source, blocked);

        public PlayerOperationResult Teleport(Vector3 position, Quaternion rotation, bool resetVelocity = true) =>
            locomotion == null
                ? PlayerOperationResult.RejectedInvalidProfile
                : locomotion.Teleport(position, rotation, resetVelocity);

        public void TeleportTo(Vector3 position, Quaternion rotation) => Teleport(position, rotation, true);

        public PlayerOperationResult SelectProfile(PlayerProfile selected) =>
            locomotion == null
                ? PlayerOperationResult.RejectedInvalidProfile
                : locomotion.SelectProfile(selected);

        public PlayerRuntimeProfile CaptureRuntimeProfile() =>
            locomotion == null ? null : locomotion.CaptureRuntimeProfile();

        public PlayerOperationResult ReplaceRuntimeProfile(PlayerRuntimeProfile selected) =>
            locomotion == null
                ? PlayerOperationResult.RejectedInvalidProfile
                : locomotion.ReplaceRuntimeProfile(selected);

        public PlayerOperationResult SetModifier(PlayerModifierId id, PlayerStat stat,
            PlayerModifierOperation operation, float value) =>
            locomotion == null
                ? PlayerOperationResult.RejectedInvalidProfile
                : locomotion.SetModifier(id, stat, operation, value);

        public PlayerOperationResult RemoveModifier(PlayerModifierId id, PlayerStat stat) =>
            locomotion == null
                ? PlayerOperationResult.RejectedInvalidProfile
                : locomotion.RemoveModifier(id, stat);

        public void SetMovementEnabled(bool enabled)
        {
            movementEnabled = enabled;
            if (!enabled)
            {
                rawMove = Vector2.zero;
                jumpHeld = false;
                jumpQueued = false;
            }
            SetControlBlocked("MovementEnabled", !enabled);
        }

        public void SetMovementReference(Transform reference) => movementReference = reference;

        // Existing Test Campus camera releases this name while newer callers use SetMovementReference.
        public void SetCameraMovementReference(Transform reference) => SetMovementReference(reference);

        private void SubscribeToInput()
        {
            if (inputSubscribed || input == null) return;
            input.MoveEvent += OnMove;
            input.JumpEvent += OnJump;
            input.JumpCancelledEvent += OnJumpCancelled;
            inputSubscribed = true;
        }

        private void UnsubscribeFromInput()
        {
            if (!inputSubscribed || input == null) return;
            input.MoveEvent -= OnMove;
            input.JumpEvent -= OnJump;
            input.JumpCancelledEvent -= OnJumpCancelled;
            inputSubscribed = false;
        }

        private void OnMove(Vector2 value) => rawMove = Vector2.ClampMagnitude(value, 1f);
        private void OnJump() { if (movementEnabled) { jumpQueued = true; jumpHeld = true; } }
        private void OnJumpCancelled() => jumpHeld = false;

        public void BeforeCharacterUpdate(float deltaTime)
        {
            stepProducedOutput = false;
            // Cache camera-relative intent once for this motor step. KCC calls this
            // before both rotation and velocity callbacks.
            movementDirection = movementEnabled
                ? ConvertToMovementSpace(new Vector3(rawMove.x, 0f, rawMove.y))
                : Vector3.zero;
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Vector3 direction = movementDirection;
            direction = Vector3.ProjectOnPlane(direction, motor.CharacterUp);
            if (direction.sqrMagnitude <= 0.0001f || Presentation.Mode == LocomotionMode.Disabled)
                return;

            float sharpness = orientationSharpness;
            if (sharpness <= 0f) return;
            float safeDeltaTime = SafeDeltaTime(deltaTime);
            Vector3 smoothed = Vector3.Slerp(motor.CharacterForward, direction.normalized,
                SafeSmoothingFactor(sharpness, safeDeltaTime));
            if (smoothed.sqrMagnitude > 0.0001f)
                currentRotation = Quaternion.LookRotation(smoothed, motor.CharacterUp);
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            float safeDeltaTime = SafeDeltaTime(deltaTime);
            currentVelocity = SanitizeVelocity(currentVelocity);
            PlayerBodyObservation observation = ObserveBody(currentVelocity);
            PlayerIntent intent = BuildIntent();
            output = locomotion.Step(intent, observation, safeDeltaTime);
            orientationSharpness = output.OrientationSharpness;
            stepProducedOutput = true;

            if (output.HasTeleport)
            {
                currentVelocity = output.ResetVelocity ? Vector3.zero : SanitizeVelocity(currentVelocity);
                return;
            }

            bool stable = observation.StableGrounded && observation.WalkableGround;
            if (output.ApplyGrounding && stable)
            {
                float magnitude = currentVelocity.magnitude;
                currentVelocity = motor.GetDirectionTangentToSurface(currentVelocity, observation.GroundNormal) * magnitude;
                Vector3 target = motor.GetDirectionTangentToSurface(output.TargetPlanarVelocity,
                    observation.GroundNormal) * output.TargetPlanarVelocity.magnitude;
                currentVelocity = Vector3.Lerp(currentVelocity, target,
                    SafeSmoothingFactor(output.StableMovementSharpness, safeDeltaTime));
            }
            else if (output.Mode == LocomotionMode.Disabled)
            {
                currentVelocity = Vector3.Project(currentVelocity, motor.CharacterUp);
            }
            else
            {
                ApplyAirAcceleration(ref currentVelocity, safeDeltaTime);
            }

            if (!stable || output.Mode == LocomotionMode.Airborne)
                currentVelocity += output.Gravity * safeDeltaTime;
            // Legacy feel applies drag while airborne or control-blocked, not during
            // stable grounded smoothing where the target velocity is authoritative.
            if (!stable || output.Mode == LocomotionMode.Disabled || output.Mode == LocomotionMode.Airborne)
                currentVelocity = ApplySafeDrag(currentVelocity, output.Drag, safeDeltaTime);

            if (output.HasJumpInfluence)
            {
                Vector3 jumpDirection = motor.CharacterUp;
                if (motor.GroundingStatus.FoundAnyGround && !motor.GroundingStatus.IsStableOnGround)
                    jumpDirection = motor.GroundingStatus.GroundNormal;
                motor.ForceUnground();
                currentVelocity += jumpDirection.normalized * output.JumpVerticalVelocity
                    - Vector3.Project(currentVelocity, motor.CharacterUp);
            }
            currentVelocity = SanitizeVelocity(currentVelocity);
        }

        private void ApplyAirAcceleration(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!IsFinite(output.AirAcceleration) || Mathf.Abs(output.AirAcceleration) <= 0.0001f ||
                output.TargetPlanarVelocity.sqrMagnitude <= 0.0001f)
                return;
            Vector3 up = motor.CharacterUp;
            Vector3 planar = Vector3.ProjectOnPlane(currentVelocity, up);
            Vector3 added = output.TargetPlanarVelocity.normalized * output.AirAcceleration * deltaTime;
            if (planar.magnitude < output.TargetPlanarVelocity.magnitude)
            {
                Vector3 total = Vector3.ClampMagnitude(planar + added, output.TargetPlanarVelocity.magnitude);
                added = total - planar;
            }
            else if (Vector3.Dot(planar, added) > 0f && planar.sqrMagnitude > 0.0001f)
                added = Vector3.ProjectOnPlane(added, planar.normalized);

            // Preserve the legacy wall/slope guard so nearby steep surfaces cannot
            // turn airborne input into upward climbing velocity.
            if (motor.GroundingStatus.FoundAnyGround)
            {
                Vector3 obstructionNormal = Vector3.Cross(
                    Vector3.Cross(motor.CharacterUp, motor.GroundingStatus.GroundNormal),
                    motor.CharacterUp);
                if (obstructionNormal.sqrMagnitude > 0.0001f &&
                    Vector3.Dot(currentVelocity + added, added) > 0f)
                {
                    added = Vector3.ProjectOnPlane(added, obstructionNormal.normalized);
                }
            }
            currentVelocity += added;
        }

        public void PostGroundingUpdate(float deltaTime) { }

        public void AfterCharacterUpdate(float deltaTime)
        {
            if (stepProducedOutput && output.HasTeleport)
            {
                motor.SetPositionAndRotation(output.TeleportPosition, output.TeleportRotation, true);
                if (output.ResetVelocity) motor.BaseVelocity = Vector3.zero;
            }
            // Keep the one-shot press through UpdateVelocity and consume it only
            // after KCC has completed the motor step.
            jumpQueued = false;
        }

        public bool IsColliderValidForCollisions(Collider coll) =>
            ignoredColliders == null || !ignoredColliders.Contains(coll);

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport) { }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport) { }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            Vector3 atCharacterPosition, Quaternion atCharacterRotation,
            ref HitStabilityReport hitStabilityReport) { }

        public void OnDiscreteCollisionDetected(Collider hitCollider) { }

        private PlayerIntent BuildIntent()
        {
            if (!movementEnabled) return PlayerIntent.Empty;
            movementDirection = Vector3.ClampMagnitude(movementDirection, 1f);
            Vector2 coreMove = new Vector2(movementDirection.x, movementDirection.z);
            PlayerIntent intent = new PlayerIntent(coreMove, jumpQueued, jumpHeld);
            return intent;
        }

        private Vector3 ConvertToMovementSpace(Vector3 inputVector)
        {
            Transform reference = movementReference != null
                ? movementReference
                : Camera.main != null ? Camera.main.transform : null;
            if (reference == null) return inputVector;
            Vector3 forward = reference.forward;
            Vector3 right = reference.right;
            forward.y = 0f;
            right.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f) forward = Vector3.forward;
            if (right.sqrMagnitude <= 0.0001f) right = Vector3.right;
            return right.normalized * inputVector.x + forward.normalized * inputVector.z;
        }

        private PlayerBodyObservation ObserveBody()
        {
            return ObserveBody(motor != null ? motor.Velocity : Vector3.zero);
        }

        private PlayerBodyObservation ObserveBody(Vector3 currentVelocity)
        {
            bool stable = motor != null && motor.GroundingStatus.IsStableOnGround;
            Vector3 normal = motor != null ? motor.GroundingStatus.GroundNormal : Vector3.up;
            Quaternion rotation = motor != null ? motor.TransientRotation : transform.rotation;
            Vector3 position = motor != null ? motor.TransientPosition : transform.position;
            // KCC exposes walkable ground through IsStableOnGround; FoundAnyGround
            // also includes steep/non-walkable contacts.
            return new PlayerBodyObservation(position, rotation, currentVelocity, stable, stable, normal);
        }

        private static float SafeDeltaTime(float deltaTime) =>
            IsFinite(deltaTime) ? Mathf.Max(0f, deltaTime) : 0f;

        private static float SafeSmoothingFactor(float sharpness, float deltaTime)
        {
            if (!IsFinite(sharpness) || !IsFinite(deltaTime) || sharpness <= 0f) return 0f;
            float factor = 1f - Mathf.Exp(-sharpness * deltaTime);
            return IsFinite(factor) ? Mathf.Clamp01(factor) : 0f;
        }

        private static Vector3 ApplySafeDrag(Vector3 velocity, float drag, float deltaTime)
        {
            if (!IsFinite(drag) || !IsFinite(deltaTime)) return SanitizeVelocity(velocity);
            float denominator = 1f + (drag * deltaTime);
            if (!IsFinite(denominator) || Mathf.Abs(denominator) <= 0.0001f)
                return SanitizeVelocity(velocity);
            return SanitizeVelocity(velocity / denominator);
        }

        private static Vector3 SanitizeVelocity(Vector3 velocity)
        {
            return new Vector3(SanitizeVelocityComponent(velocity.x),
                SanitizeVelocityComponent(velocity.y), SanitizeVelocityComponent(velocity.z));
        }

        private static float SanitizeVelocityComponent(float value) =>
            IsFinite(value) ? Mathf.Clamp(value, -MotorSafetyMagnitude, MotorSafetyMagnitude) : 0f;

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
