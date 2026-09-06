using System;
using UnityEngine;

namespace CrazyMarket.Player.V2
{
    [Serializable]
    public struct LocomotionTuning
    {
        public float StableMoveSpeed;
        public float AirMoveSpeed;
        public float AirAcceleration;
        public float StableMovementSharpness;
        [Tooltip("Tune speed gain, coasting, and steering independently. Disabled profiles keep their existing movement response.")]
        public bool SeparateGroundResponse;
        [Min(0f), Tooltip("Speed gained per second, independent of Stable Move Speed (m/s²).")]
        public float GroundAcceleration;
        [Min(0f), Tooltip("How quickly excess speed fades when releasing or reducing movement input. Lower values give a longer coast (1/s).")]
        public float GroundDecelerationSharpness;
        [Min(0f), Tooltip("How quickly travel direction follows movement input. Does not change speed or character-facing response (1/s).")]
        public float GroundTurnSharpness;
        public float OrientationSharpness;
        public float JumpSpeed;
        public float JumpBufferTime;
        public float CoyoteTime;
        public float Gravity;
        public float FallGravityMultiplier;
        public float Drag;

        public static LocomotionTuning ProductionDefaults => new LocomotionTuning
        {
            StableMoveSpeed = 10f,
            AirMoveSpeed = 8f,
            AirAcceleration = 60f,
            StableMovementSharpness = 15f,
            GroundAcceleration = 12f,
            GroundDecelerationSharpness = 1f,
            GroundTurnSharpness = 6f,
            OrientationSharpness = 10f,
            JumpSpeed = 12f,
            JumpBufferTime = 0.15f,
            CoyoteTime = 0.2f,
            Gravity = -30f,
            FallGravityMultiplier = 2f,
            Drag = 0.1f
        };

        internal bool IsFinite()
        {
            return Finite(StableMoveSpeed) && Finite(AirMoveSpeed) && Finite(AirAcceleration) &&
                   Finite(StableMovementSharpness) && Finite(OrientationSharpness) && Finite(JumpSpeed) &&
                   Finite(GroundAcceleration) && Finite(GroundDecelerationSharpness) && Finite(GroundTurnSharpness) &&
                   Finite(JumpBufferTime) && Finite(CoyoteTime) && Finite(Gravity) &&
                   Finite(FallGravityMultiplier) && Finite(Drag);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    [Serializable]
    public sealed class PlayerProfileData
    {
        [SerializeField] private LocomotionTuning locomotion = LocomotionTuning.ProductionDefaults;

        public LocomotionTuning Locomotion => locomotion;

        public PlayerProfileData() { }

        internal PlayerProfileData(LocomotionTuning locomotion)
        {
            this.locomotion = locomotion;
        }

        internal PlayerProfileData Clone() => new PlayerProfileData(locomotion);
        internal bool IsValid => locomotion.IsFinite();
    }

    [CreateAssetMenu(menuName = "CrazyMarket/Player/V2 Player Profile", fileName = "PlayerProfileV2")]
    public sealed class PlayerProfile : ScriptableObject
    {
        [SerializeField] private string profileId = "Production";
        [SerializeField] private PlayerProfileData data = new PlayerProfileData();

        public PlayerProfileId Id => new PlayerProfileId(profileId);
        public LocomotionTuning Locomotion => data == null ? LocomotionTuning.ProductionDefaults : data.Locomotion;

        private void OnEnable()
        {
            if (data == null) data = new PlayerProfileData();
        }

        private void OnValidate()
        {
            if (data == null || !data.IsValid)
            {
                Debug.LogError("PlayerProfile V2 requires finite locomotion data.", this);
            }
        }

        public bool TryCreateRuntime(out PlayerRuntimeProfile runtime)
        {
            runtime = null;
            if (data == null || !data.IsValid || !Id.IsValid) return false;
            runtime = new PlayerRuntimeProfile(Id, data.Locomotion, name);
            return runtime.IsValid;
        }

        public PlayerRuntimeProfile CreateRuntimeProfile()
        {
            PlayerRuntimeProfile runtime;
            return TryCreateRuntime(out runtime) ? runtime : null;
        }

        public static PlayerRuntimeProfile CreateProductionRuntimeProfile()
        {
            return new PlayerRuntimeProfile(new PlayerProfileId("Production"),
                LocomotionTuning.ProductionDefaults, "Production");
        }
    }

    public sealed class PlayerRuntimeProfile
    {
        private readonly PlayerProfileId profileId;
        private readonly RuntimeProfileId runtimeId;
        private readonly LocomotionTuning locomotion;
        private readonly string name;

        public PlayerProfileId ProfileId => profileId;
        public RuntimeProfileId Id => runtimeId;
        public LocomotionTuning Locomotion => locomotion;
        public string Name => name;

        internal PlayerRuntimeProfile(PlayerProfileId profileId, LocomotionTuning locomotion, string name)
        {
            this.profileId = profileId;
            this.locomotion = locomotion;
            this.name = string.IsNullOrEmpty(name) ? profileId.Value : name;
            runtimeId = new RuntimeProfileId(this.name + "#" + Guid.NewGuid().ToString("N"));
        }

        public PlayerRuntimeProfile Clone() => new PlayerRuntimeProfile(profileId, locomotion, name);

        public PlayerRuntimeProfile WithLocomotion(LocomotionTuning tuning)
        {
            return !tuning.IsFinite() ? null : new PlayerRuntimeProfile(profileId, tuning, name);
        }

        internal bool IsValid => profileId.IsValid && locomotion.IsFinite();
    }
}
