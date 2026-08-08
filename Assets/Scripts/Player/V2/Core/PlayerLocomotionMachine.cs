using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrazyMarket.Player.V2
{
    public sealed class PlayerLocomotionMachine : IPlayerController
    {
        private sealed class TeleportRequest
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public bool ResetVelocity;
        }

        private readonly HashSet<string> blocks = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<PlayerModifierId, Dictionary<PlayerStat, Modifier>> modifiers =
            new Dictionary<PlayerModifierId, Dictionary<PlayerStat, Modifier>>();
        private readonly List<IPlayerAbilityRuntime> abilities = new List<IPlayerAbilityRuntime>();
        private readonly LocomotionRoot root = new LocomotionRoot();
        private PlayerRuntimeProfile profile;
        private PlayerRuntimeProfile pendingProfile;
        private TeleportRequest pendingTeleport;
        private LocomotionNode state;
        private LocomotionMode mode;
        private bool wasStable;
        private float coyote;
        private float jumpBuffer;
        private long revision;
        private PlayerSnapshot snapshot;
        private PlayerPresentationState presentation;

        public PlayerSnapshot Snapshot => snapshot;
        public PlayerPresentationState Presentation => presentation;

        public PlayerLocomotionMachine(PlayerProfile selected, PlayerBodyObservation initial)
        {
            profile = selected == null ? PlayerProfile.CreateProductionRuntimeProfile() : selected.CreateRuntimeProfile();
            if (!CanBuild(profile)) profile = PlayerProfile.CreateProductionRuntimeProfile();
            Build(profile);
            wasStable = Stable(initial);
            state = root.Resolve(false, wasStable);
            mode = state.Mode;
            coyote = wasStable ? profile.Locomotion.CoyoteTime : 0f;
            snapshot = new PlayerSnapshot(0, initial, initial.Velocity, mode, 0, Remaining(), coyote, 0f,
                profile.ProfileId, profile.Id, wasStable ? PlayerActionFlags.Grounded : PlayerActionFlags.None);
            presentation = Present(initial.Velocity, Vector2.zero, snapshot.ActionFlags);
        }

        public LocomotionOutput Step(PlayerIntent intent, PlayerBodyObservation observation, float deltaTime)
        {
            float dt = Mathf.Max(0f, Finite(deltaTime) ? deltaTime : 0f);
            TeleportRequest teleport = pendingTeleport;
            PlayerActionFlags requestFlags = ApplyRequests(ref observation);
            if (teleport != null && teleport.ResetVelocity)
            {
                wasStable = false;
                coyote = jumpBuffer = 0f;
                CancelAbilities(AbilityCancellationReason.Replaced);
                intent = PlayerIntent.Empty;
            }
            LocomotionTuning baseTuning = profile.Locomotion;
            LocomotionTuning tuning = baseTuning;
            tuning.CoyoteTime = Duration(Resolve(PlayerStat.CoyoteTime, baseTuning.CoyoteTime));
            tuning.JumpBufferTime = Duration(Resolve(PlayerStat.JumpBufferTime, baseTuning.JumpBufferTime));
            tuning.JumpSpeed = Safe(Resolve(PlayerStat.JumpSpeed, baseTuning.JumpSpeed), baseTuning.JumpSpeed);
            bool stable = Stable(observation);
            bool blocked = blocks.Count != 0;

            if (stable)
            {
                coyote = tuning.CoyoteTime;
                ResetAbilities();
            }
            else
            {
                if (wasStable) coyote = tuning.CoyoteTime;
                coyote = Mathf.Max(0f, coyote - dt);
            }
            state = root.Resolve(blocked, stable);
            mode = state.Mode;

            if (blocked)
            {
                jumpBuffer = 0f;
                if (!stable) CancelAbilities(AbilityCancellationReason.ControlBlocked);
            }
            else if (intent.JumpPressed) jumpBuffer = Mathf.Max(jumpBuffer, tuning.JumpBufferTime);
            else jumpBuffer = Mathf.Max(0f, jumpBuffer - dt);

            PlayerActionFlags flags = blocked ? PlayerActionFlags.ControlBlocked : PlayerActionFlags.None;
            bool jumped = false;
            float jumpVelocity = 0f;
            if (!blocked && (intent.JumpPressed || jumpBuffer > 0f))
            {
                bool baseJump = mode == LocomotionMode.Grounded ||
                    mode == LocomotionMode.Airborne && coyote > 0f;
                if (baseJump)
                {
                    state = root.Controllable.Airborne;
                    mode = state.Mode;
                    jumped = true;
                    jumpVelocity = tuning.JumpSpeed;
                    coyote = jumpBuffer = 0f;
                    flags |= PlayerActionFlags.Jumped;
                }
                else if (mode == LocomotionMode.Airborne)
                {
                    PlayerAbilityResult result = Evaluate(new PlayerAbilityContext(mode, intent, observation, tuning));
                    if (result.Accepted)
                    {
                        jumped = true;
                        jumpVelocity = Safe(result.VerticalInfluence, 0f);
                        jumpBuffer = 0f;
                        flags |= PlayerActionFlags.Jumped | PlayerActionFlags.DoubleJumped;
                    }
                }
            }

            float stableSpeed = Resolve(PlayerStat.StableMoveSpeed, baseTuning.StableMoveSpeed);
            float airSpeed = Resolve(PlayerStat.AirMoveSpeed, baseTuning.AirMoveSpeed);
            float airAcceleration = Resolve(PlayerStat.AirAcceleration, baseTuning.AirAcceleration);
            float gravity = Resolve(PlayerStat.Gravity, baseTuning.Gravity);
            float fall = Resolve(PlayerStat.FallGravityMultiplier, baseTuning.FallGravityMultiplier);
            Vector2 move = Vector2.ClampMagnitude(intent.Move, 1f);
            float speed = mode == LocomotionMode.Grounded ? stableSpeed : airSpeed;
            Vector3 target = blocked ? Vector3.zero : new Vector3(move.x, 0f, move.y) * speed;
            bool falling = !stable && observation.Velocity.y < 0f;
            Vector3 gravityVector = new Vector3(0f, gravity * (falling ? fall : 1f), 0f);
            if (mode == LocomotionMode.Grounded) flags |= PlayerActionFlags.Grounded;
            LocomotionOutput output = new LocomotionOutput(mode, target,
                mode == LocomotionMode.Airborne && !blocked ? airAcceleration : 0f, gravityVector, stable,
                jumped, jumpVelocity, flags, teleport != null,
                teleport == null ? Vector3.zero : teleport.Position,
                teleport == null ? Quaternion.identity : teleport.Rotation,
                teleport != null && teleport.ResetVelocity);

            Vector3 velocity = Sanitize(observation.Velocity);
            if (jumped) velocity.y = jumpVelocity;
            if (output.HasTeleport && output.ResetVelocity) velocity = Vector3.zero;
            revision++;
            snapshot = new PlayerSnapshot(revision, observation, velocity, mode, blocks.Count, Remaining(), coyote,
                jumpBuffer, profile.ProfileId, profile.Id, flags | requestFlags);
            presentation = Present(velocity, move, snapshot.ActionFlags);
            wasStable = stable;
            if (output.HasTeleport) pendingTeleport = null;
            return new LocomotionOutput(output.Mode, output.TargetPlanarVelocity, output.AirAcceleration,
                output.Gravity, output.ApplyGrounding, output.HasJumpInfluence, output.JumpVerticalVelocity,
                output.ActionFlags | requestFlags, output.HasTeleport, output.TeleportPosition,
                output.TeleportRotation, output.ResetVelocity);
        }

        public PlayerOperationResult SetControlBlocked(string source, bool blocked)
        {
            if (string.IsNullOrWhiteSpace(source)) return PlayerOperationResult.RejectedInvalidArgument;
            if (blocked)
            {
                if (blocks.Add(source))
                {
                    jumpBuffer = 0f;
                    CancelAbilities(AbilityCancellationReason.ControlBlocked);
                }
            }
            else blocks.Remove(source);
            return PlayerOperationResult.Accepted;
        }

        public PlayerOperationResult Teleport(Vector3 position, Quaternion rotation, bool resetVelocity = true)
        {
            float quaternionMagnitude = rotation.x * rotation.x + rotation.y * rotation.y +
                rotation.z * rotation.z + rotation.w * rotation.w;
            if (!Finite(position) || !Finite(rotation) || quaternionMagnitude <= 0.00000001f)
                return PlayerOperationResult.RejectedInvalidArgument;
            pendingTeleport = new TeleportRequest { Position = position, Rotation = rotation, ResetVelocity = resetVelocity };
            return PlayerOperationResult.Accepted;
        }

        public PlayerOperationResult SelectProfile(PlayerProfile selected)
        {
            PlayerRuntimeProfile candidate = selected == null ? null : selected.CreateRuntimeProfile();
            if (!CanBuild(candidate)) return PlayerOperationResult.RejectedInvalidProfile;
            pendingProfile = candidate;
            return PlayerOperationResult.Accepted;
        }

        public PlayerRuntimeProfile CaptureRuntimeProfile() => profile.Clone();

        public PlayerOperationResult ReplaceRuntimeProfile(PlayerRuntimeProfile replacement)
        {
            if (!CanBuild(replacement)) return PlayerOperationResult.RejectedInvalidProfile;
            pendingProfile = replacement.Clone();
            return PlayerOperationResult.Accepted;
        }

        public PlayerOperationResult SetModifier(PlayerModifierId id, PlayerStat stat,
            PlayerModifierOperation operation, float value)
        {
            if (!id.IsValid || !Finite(value)) return PlayerOperationResult.RejectedInvalidArgument;
            if (!Enum.IsDefined(typeof(PlayerStat), stat)) return PlayerOperationResult.RejectedUnknownModifier;
            if (!Enum.IsDefined(typeof(PlayerModifierOperation), operation))
                return PlayerOperationResult.RejectedInvalidArgument;
            Dictionary<PlayerStat, Modifier> values;
            if (!modifiers.TryGetValue(id, out values))
            {
                values = new Dictionary<PlayerStat, Modifier>();
                modifiers.Add(id, values);
            }
            values[stat] = new Modifier(operation, value);
            return PlayerOperationResult.Accepted;
        }

        public PlayerOperationResult RemoveModifier(PlayerModifierId id, PlayerStat stat)
        {
            if (!id.IsValid || !Enum.IsDefined(typeof(PlayerStat), stat))
                return PlayerOperationResult.RejectedInvalidArgument;
            Dictionary<PlayerStat, Modifier> values;
            if (!modifiers.TryGetValue(id, out values)) return PlayerOperationResult.Accepted;
            if (!values.Remove(stat)) return PlayerOperationResult.Accepted;
            if (values.Count == 0) modifiers.Remove(id);
            return PlayerOperationResult.Accepted;
        }

        private PlayerActionFlags ApplyRequests(ref PlayerBodyObservation observation)
        {
            PlayerActionFlags flags = PlayerActionFlags.None;
            if (pendingProfile != null)
            {
                PlayerRuntimeProfile replacement = pendingProfile;
                pendingProfile = null;
                CancelAbilities(AbilityCancellationReason.ProfileChanged);
                Build(replacement);
                profile = replacement;
                if (!Stable(observation)) CancelAbilities(AbilityCancellationReason.ProfileChanged);
                coyote = jumpBuffer = 0f;
                flags |= PlayerActionFlags.ProfileChanged;
            }
            if (pendingTeleport != null)
            {
                TeleportRequest request = pendingTeleport;
                observation = new PlayerBodyObservation(request.Position, request.Rotation,
                    request.ResetVelocity ? Vector3.zero : observation.Velocity,
                    request.ResetVelocity ? false : observation.StableGrounded,
                    request.ResetVelocity ? false : observation.WalkableGround,
                    request.ResetVelocity ? Vector3.up : observation.GroundNormal);
                flags |= PlayerActionFlags.Teleported;
            }
            return flags;
        }

        private PlayerAbilityResult Evaluate(PlayerAbilityContext context)
        {
            for (int i = 0; i < abilities.Count; i++)
            {
                PlayerAbilityResult result = abilities[i].Evaluate(context);
                if (result.Accepted) return result;
            }
            return PlayerAbilityResult.Rejected;
        }

        private void Build(PlayerRuntimeProfile source)
        {
            abilities.Clear();
            IReadOnlyList<PlayerAbilityId> ids = source.AbilityLoadout.AbilityIds;
            for (int i = 0; i < ids.Count; i++)
            {
                PlayerAbilityDefinition definition = source.AbilityLoadout.FindDefinition(ids[i]);
                abilities.Add(definition == null ? DoubleJumpAbilityDefinition.CreateDefaultRuntime() : definition.CreateRuntime());
            }
        }

        private static bool CanBuild(PlayerRuntimeProfile source)
        {
            if (source == null || !source.IsValid || source.AbilityLoadout == null || source.AbilityLoadout.AbilityIds == null) return false;
            IReadOnlyList<PlayerAbilityId> ids = source.AbilityLoadout.AbilityIds;
            bool doubleJumpSeen = false;
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] != PlayerAbilityId.DoubleJump || doubleJumpSeen) return false;
                doubleJumpSeen = true;
            }
            return true;
        }

        private void ResetAbilities() { for (int i = 0; i < abilities.Count; i++) abilities[i].Reset(); }
        private void CancelAbilities(AbilityCancellationReason reason) { for (int i = 0; i < abilities.Count; i++) abilities[i].Cancel(reason); }
        private int Remaining() { int total = 0; for (int i = 0; i < abilities.Count; i++) total += abilities[i].RemainingCharges; return total; }

        private float Resolve(PlayerStat stat, float baseValue)
        {
            float add = 0f, multiply = 1f;
            foreach (Dictionary<PlayerStat, Modifier> values in modifiers.Values)
            {
                Modifier modifier;
                if (!values.TryGetValue(stat, out modifier)) continue;
                if (modifier.Operation == PlayerModifierOperation.Additive) add += modifier.Value;
                else multiply *= modifier.Value;
            }
            return Safe((baseValue + add) * multiply, baseValue);
        }

        private static bool Stable(PlayerBodyObservation observation) => observation.StableGrounded && observation.WalkableGround;
        private PlayerPresentationState Present(Vector3 velocity, Vector2 move, PlayerActionFlags flags) =>
            new PlayerPresentationState(mode, mode == LocomotionMode.Grounded,
                new Vector3(velocity.x, 0f, velocity.z).magnitude, velocity.y, move, flags);
        private static Vector3 Sanitize(Vector3 value) => new Vector3(Safe(value.x, 0f), Safe(value.y, 0f), Safe(value.z, 0f));
        private static float Duration(float value) => Mathf.Max(0f, Safe(value, 0f));
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        private static bool Finite(Quaternion value) => Finite(value.x) && Finite(value.y) && Finite(value.z) && Finite(value.w);
        private static float Safe(float value, float fallback) => Finite(value) ? value : fallback;

        private abstract class LocomotionNode
        {
            public abstract LocomotionMode Mode { get; }
        }

        private sealed class DisabledNode : LocomotionNode
        {
            public override LocomotionMode Mode => LocomotionMode.Disabled;
        }

        private sealed class GroundedNode : LocomotionNode
        {
            public override LocomotionMode Mode => LocomotionMode.Grounded;
        }

        private sealed class AirborneNode : LocomotionNode
        {
            public override LocomotionMode Mode => LocomotionMode.Airborne;
        }

        private sealed class ControllableNode : LocomotionNode
        {
            public readonly GroundedNode Grounded = new GroundedNode();
            public readonly AirborneNode Airborne = new AirborneNode();
            public LocomotionNode Child { get; private set; }
            public override LocomotionMode Mode => Child.Mode;
            public void Reconcile(bool stable) { Child = stable ? Grounded : Airborne; }
        }

        private sealed class LocomotionRoot
        {
            public readonly DisabledNode Disabled = new DisabledNode();
            public readonly ControllableNode Controllable = new ControllableNode();
            public LocomotionNode Resolve(bool blocked, bool stable)
            {
                Controllable.Reconcile(stable);
                return blocked ? Disabled : Controllable.Child;
            }
        }

        private readonly struct Modifier
        {
            public readonly PlayerModifierOperation Operation;
            public readonly float Value;
            public Modifier(PlayerModifierOperation operation, float value) { Operation = operation; Value = value; }
        }
    }
}
