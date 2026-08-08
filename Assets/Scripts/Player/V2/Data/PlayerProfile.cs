using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            AirMoveSpeed = 15f,
            AirAcceleration = 60f,
            StableMovementSharpness = 15f,
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
                   Finite(JumpBufferTime) && Finite(CoyoteTime) && Finite(Gravity) &&
                   Finite(FallGravityMultiplier) && Finite(Drag);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    [Serializable]
    public sealed class PlayerAbilityLoadout
    {
        [SerializeField] private List<PlayerAbilityId> abilityIds = new List<PlayerAbilityId>
        {
            PlayerAbilityId.DoubleJump
        };
        [SerializeField] private List<PlayerAbilityDefinition> definitions =
            new List<PlayerAbilityDefinition>();

        public IReadOnlyList<PlayerAbilityId> AbilityIds => abilityIds;

        internal bool IsValid => abilityIds != null && definitions != null;

        internal bool Contains(PlayerAbilityId id)
        {
            return abilityIds != null && abilityIds.Contains(id);
        }

        internal PlayerAbilityDefinition FindDefinition(PlayerAbilityId id)
        {
            if (definitions == null)
            {
                return null;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null && definitions[i].Id == id)
                {
                    return definitions[i];
                }
            }

            return null;
        }

        internal PlayerAbilityLoadout Clone()
        {
            var copy = new PlayerAbilityLoadout();
            copy.abilityIds = abilityIds == null ? null : new List<PlayerAbilityId>(abilityIds);
            copy.definitions = definitions == null ? null : new List<PlayerAbilityDefinition>(definitions);
            return copy;
        }

        internal bool TryCreateRuntime(out PlayerRuntimeAbilityLoadout runtime)
        {
            runtime = null;
            if (!IsValid || abilityIds == null) return false;

            var ids = new List<PlayerAbilityId>(abilityIds.Count);
            for (int i = 0; i < abilityIds.Count; i++)
            {
                PlayerAbilityId id = abilityIds[i];
                if (id != PlayerAbilityId.DoubleJump || ids.Contains(id)) return false;
                ids.Add(id);
            }

            // DoubleJump currently has no asset parameters, so its runtime snapshot is ID-only.
            runtime = new PlayerRuntimeAbilityLoadout(ids);
            return true;
        }
    }

    [Serializable]
    public sealed class PlayerProfileData
    {
        [SerializeField] private LocomotionTuning locomotion = LocomotionTuning.ProductionDefaults;
        [SerializeField] private PlayerAbilityLoadout abilityLoadout = new PlayerAbilityLoadout();

        public LocomotionTuning Locomotion => locomotion;
        public PlayerAbilityLoadout AbilityLoadout => abilityLoadout;

        public PlayerProfileData() { }

        internal PlayerProfileData(LocomotionTuning locomotion, PlayerAbilityLoadout abilityLoadout)
        {
            this.locomotion = locomotion;
            this.abilityLoadout = abilityLoadout;
        }

        internal PlayerProfileData Clone()
        {
            return new PlayerProfileData(locomotion, abilityLoadout == null ? null : abilityLoadout.Clone());
        }

        internal bool IsValid => locomotion.IsFinite() && abilityLoadout != null && abilityLoadout.IsValid;
    }

    [CreateAssetMenu(menuName = "CrazyMarket/Player/V2 Player Profile", fileName = "PlayerProfileV2")]
    public sealed class PlayerProfile : ScriptableObject
    {
        [SerializeField] private string profileId = "Production";
        [SerializeField] private PlayerProfileData data = new PlayerProfileData();

        public PlayerProfileId Id => new PlayerProfileId(profileId);
        public LocomotionTuning Locomotion => data == null ? LocomotionTuning.ProductionDefaults : data.Locomotion;
        public PlayerAbilityLoadout AbilityLoadout => data == null ? null : data.AbilityLoadout;

        private void OnEnable()
        {
            if (data == null)
            {
                data = new PlayerProfileData();
            }
        }

        private void OnValidate()
        {
            if (data == null || !data.IsValid)
            {
                Debug.LogError("PlayerProfile V2 requires finite locomotion data and an Ability Loadout.", this);
            }
        }

        public bool TryCreateRuntime(out PlayerRuntimeProfile runtime)
        {
            runtime = null;
            if (data == null || !data.IsValid || !Id.IsValid)
            {
                return false;
            }

            PlayerRuntimeAbilityLoadout abilities;
            if (!data.AbilityLoadout.TryCreateRuntime(out abilities)) return false;
            runtime = new PlayerRuntimeProfile(Id, data.Locomotion, abilities, name);
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
                LocomotionTuning.ProductionDefaults,
                new PlayerRuntimeAbilityLoadout(new List<PlayerAbilityId> { PlayerAbilityId.DoubleJump }),
                "Production");
        }
    }

    public sealed class PlayerRuntimeAbilityLoadout
    {
        private readonly ReadOnlyCollection<PlayerAbilityId> abilityIds;

        public IReadOnlyList<PlayerAbilityId> AbilityIds => abilityIds;

        internal PlayerRuntimeAbilityLoadout(IList<PlayerAbilityId> ids)
        {
            abilityIds = ids == null ? null : new List<PlayerAbilityId>(ids).AsReadOnly();
        }

        internal PlayerRuntimeAbilityLoadout Clone()
        {
            return new PlayerRuntimeAbilityLoadout(abilityIds);
        }

        internal bool IsValid
        {
            get
            {
                if (abilityIds == null) return false;
                for (int i = 0; i < abilityIds.Count; i++)
                {
                    if (abilityIds[i] != PlayerAbilityId.DoubleJump || abilityIds.IndexOf(abilityIds[i]) != i)
                        return false;
                }
                return true;
            }
        }
    }

    public sealed class PlayerRuntimeProfile
    {
        private readonly PlayerProfileId profileId;
        private readonly RuntimeProfileId runtimeId;
        private readonly LocomotionTuning locomotion;
        private readonly PlayerRuntimeAbilityLoadout abilityLoadout;
        private readonly string name;

        public PlayerProfileId ProfileId => profileId;
        public RuntimeProfileId Id => runtimeId;
        public LocomotionTuning Locomotion => locomotion;
        public PlayerRuntimeAbilityLoadout AbilityLoadout => abilityLoadout;
        public string Name => name;

        internal PlayerRuntimeProfile(PlayerProfileId profileId, LocomotionTuning locomotion,
            PlayerRuntimeAbilityLoadout abilityLoadout, string name)
        {
            this.profileId = profileId;
            this.locomotion = locomotion;
            this.abilityLoadout = abilityLoadout;
            this.name = string.IsNullOrEmpty(name) ? profileId.Value : name;
            runtimeId = new RuntimeProfileId(this.name + "#" + Guid.NewGuid().ToString("N"));
        }

        public PlayerRuntimeProfile Clone()
        {
            return new PlayerRuntimeProfile(profileId, locomotion, abilityLoadout.Clone(), name);
        }

        public PlayerRuntimeProfile WithLocomotion(LocomotionTuning tuning)
        {
            if (!tuning.IsFinite()) return null;
            return new PlayerRuntimeProfile(profileId, tuning, abilityLoadout.Clone(), name);
        }

        internal bool IsValid => profileId.IsValid && locomotion.IsFinite() &&
            abilityLoadout != null && abilityLoadout.IsValid;
    }
}
