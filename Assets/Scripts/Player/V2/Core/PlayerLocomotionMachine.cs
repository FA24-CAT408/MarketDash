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
        private readonly List<PlayerAbilityComponent> abilities = new List<PlayerAbilityComponent>();
        private PlayerRuntimeProfile profile;
        private PlayerRuntimeProfile pendingProfile;
        private TeleportRequest pendingTeleport;
        private LocomotionMode mode;
        private bool wasStable;
        private float coyote;
        private float jumpBuffer;
        private long revision;
        private PlayerSnapshot snapshot;
        private PlayerPresentationState presentation;

        public PlayerSnapshot Snapshot => snapshot;
        public PlayerPresentationState Presentation => presentation;

        public PlayerLocomotionMachine(PlayerProfile selected, PlayerBodyObservation initial,
            IEnumerable<PlayerAbilityComponent> composedAbilities)
        {
            profile = selected == null ? PlayerProfile.CreateProductionRuntimeProfile() : selected.CreateRuntimeProfile();
            if (!IsValidProfile(profile))
            {
                profile = PlayerProfile.CreateProductionRuntimeProfile();
            }
            if (composedAbilities != null)
            {
                foreach (PlayerAbilityComponent ability in composedAbilities)
                {
                    if (ability != null && !abilities.Contains(ability)) abilities.Add(ability);
                }
            }
            ResetAbilities();
            wasStable = Stable(initial);
            mode = ResolveMode(false, wasStable);
            coyote = wasStable ? profile.Locomotion.CoyoteTime : 0f;
            snapshot = new PlayerSnapshot(0, initial, initial.Velocity, mode, 0, coyote, 0f,
                profile.ProfileId, profile.Id, wasStable ? PlayerActionFlags.Grounded : PlayerActionFlags.None);
            presentation = Present(initial.Velocity, Vector2.zero, snapshot.ActionFlags, wasStable);
        }
        public LocomotionOutput Step(PlayerIntent intent, PlayerBodyObservation observation, float deltaTime)
        {
            float dt = Mathf.Max(0f, Finite(deltaTime) ? deltaTime : 0f);
            TeleportRequest teleport = pendingTeleport;
            PlayerActionFlags requestFlags = ApplyRequests(ref observation);
            if (teleport != null)
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
            mode = ResolveMode(blocked, stable);

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
                    mode = LocomotionMode.Airborne;
                    jumped = true;
                    jumpVelocity = MotorSafe(tuning.JumpSpeed);
                    coyote = jumpBuffer = 0f;
                    flags |= PlayerActionFlags.Jumped;
                }
                else if (mode == LocomotionMode.Airborne)
                {
                    PlayerAbilityResult result = Evaluate(new PlayerAbilityContext(mode, intent, observation, tuning));
                    if (result.Accepted)
                    {
                        jumped = true;
                        jumpVelocity = MotorSafe(result.VerticalInfluence);
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
            float stableMovementSharpness = Resolve(PlayerStat.StableMovementSharpness,
                baseTuning.StableMovementSharpness);
            float orientationSharpness = Resolve(PlayerStat.OrientationSharpness,
                baseTuning.OrientationSharpness);
            float drag = Resolve(PlayerStat.Drag, baseTuning.Drag);
            Vector2 move = Vector2.ClampMagnitude(Finite(intent.Move) ?
                new Vector2(MotorSafe(intent.Move.x), MotorSafe(intent.Move.y)) : Vector2.zero, 1f);
            float speed = mode == LocomotionMode.Grounded ? stableSpeed : airSpeed;
            Vector3 target = blocked ? Vector3.zero : MotorSafe(new Vector3(move.x, 0f, move.y) * speed);
            bool falling = !stable && observation.Velocity.y < 0f;
            Vector3 gravityVector = MotorSafe(new Vector3(0f, gravity * (falling ? fall : 1f), 0f));
            if (mode == LocomotionMode.Grounded) flags |= PlayerActionFlags.Grounded;
            LocomotionOutput output = new LocomotionOutput(mode, target,
                mode == LocomotionMode.Airborne && !blocked ? MotorSafe(airAcceleration) : 0f,
                MotorSafe(stableMovementSharpness), MotorSafe(orientationSharpness), MotorSafe(drag), gravityVector,
                stable && !jumped && teleport == null, jumped, MotorSafe(jumpVelocity), flags, teleport != null,
                teleport == null ? Vector3.zero : teleport.Position,
                teleport == null ? Quaternion.identity : teleport.Rotation,
                teleport != null && teleport.ResetVelocity);

            Vector3 velocity = Sanitize(observation.Velocity);
            if (jumped) velocity.y = jumpVelocity;
            if (output.HasTeleport && output.ResetVelocity) velocity = Vector3.zero;
            revision++;
            snapshot = new PlayerSnapshot(revision, observation, velocity, mode, blocks.Count, coyote,
                jumpBuffer, profile.ProfileId, profile.Id, flags | requestFlags);
            presentation = Present(velocity, move, snapshot.ActionFlags, stable && !jumped);
            wasStable = stable && !jumped;
            if (output.HasTeleport) pendingTeleport = null;
            return new LocomotionOutput(output.Mode, output.TargetPlanarVelocity, output.AirAcceleration,
                output.StableMovementSharpness, output.OrientationSharpness, output.Drag, output.Gravity,
                output.ApplyGrounding, output.HasJumpInfluence, output.JumpVerticalVelocity,
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
            Quaternion normalized;
            if (!Finite(position) || !TryNormalize(rotation, out normalized))
                return PlayerOperationResult.RejectedInvalidArgument;
            pendingTeleport = new TeleportRequest { Position = position, Rotation = normalized, ResetVelocity = resetVelocity };
            return PlayerOperationResult.Accepted;
        }
        public PlayerOperationResult SelectProfile(PlayerProfile selected)
        {
            PlayerRuntimeProfile candidate = selected == null ? null : selected.CreateRuntimeProfile();
            if (!IsValidProfile(candidate)) return PlayerOperationResult.RejectedInvalidProfile;
            pendingProfile = candidate;
            return PlayerOperationResult.Accepted;
        }
        public PlayerRuntimeProfile CaptureRuntimeProfile() => profile.Clone();

        public PlayerOperationResult ReplaceRuntimeProfile(PlayerRuntimeProfile replacement)
        {
            if (!IsValidProfile(replacement)) return PlayerOperationResult.RejectedInvalidProfile;
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
                    false, false, Vector3.up);
                flags |= PlayerActionFlags.Teleported;
            }
            return flags;
        }

        private PlayerAbilityResult Evaluate(PlayerAbilityContext context)
        {
            for (int i = 0; i < abilities.Count; i++)
            {
                if (!abilities[i].IsParticipating) continue;
                PlayerAbilityResult result = abilities[i].Evaluate(context);
                if (result.Accepted) return result;
            }
            return PlayerAbilityResult.Rejected;
        }

        private static bool IsValidProfile(PlayerRuntimeProfile source) => source != null && source.IsValid;

        private void ResetAbilities()
        {
            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilities[i].IsParticipating) abilities[i].Reset();
            }
        }

        private void CancelAbilities(AbilityCancellationReason reason)
        {
            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilities[i].IsParticipating) abilities[i].Cancel(reason);
            }
        }

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
        private PlayerPresentationState Present(Vector3 velocity, Vector2 move, PlayerActionFlags flags,
            bool grounded) =>
            new PlayerPresentationState(mode, grounded,
                new Vector3(velocity.x, 0f, velocity.z).magnitude, velocity.y, move, flags);
        // Final motor-output safety boundary; ordinary negative and large authoring values remain valid before this cap.
        private const float EngineSafetyMagnitude = 1000000f;
        private static Vector3 Sanitize(Vector3 value) => MotorSafe(value);
        private static Vector3 MotorSafe(Vector3 value) =>
            new Vector3(MotorSafe(value.x), MotorSafe(value.y), MotorSafe(value.z));
        private static float MotorSafe(float value, float fallback = 0f) =>
            Mathf.Clamp(Safe(value, fallback), -EngineSafetyMagnitude, EngineSafetyMagnitude);
        private static float Duration(float value) => Mathf.Max(0f, Safe(value, 0f));
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(Vector2 value) => Finite(value.x) && Finite(value.y);
        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        private static bool Finite(Quaternion value) => Finite(value.x) && Finite(value.y) && Finite(value.z) && Finite(value.w);
        private static float Safe(float value, float fallback) => Finite(value) ? value : fallback;
        private static bool TryNormalize(Quaternion value, out Quaternion normalized)
        {
            normalized = Quaternion.identity;
            if (!Finite(value)) return false;
            float scale = Mathf.Max(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z), Mathf.Abs(value.w));
            if (scale <= 0f) return false;
            float x = value.x / scale, y = value.y / scale, z = value.z / scale, w = value.w / scale;
            float inverse = 1f / Mathf.Sqrt(x * x + y * y + z * z + w * w);
            if (scale <= 0.0001f * inverse) return false;
            normalized = new Quaternion(x * inverse, y * inverse, z * inverse, w * inverse);
            return true;
        }

        // Hierarchy remains explicit in the resolver: Disabled is the root branch;
        // otherwise Controllable selects its Grounded or Airborne child.
        private static LocomotionMode ResolveMode(bool blocked, bool stable)
        {
            if (blocked) return LocomotionMode.Disabled;
            return stable ? LocomotionMode.Grounded : LocomotionMode.Airborne;
        }

        private readonly struct Modifier
        {
            public readonly PlayerModifierOperation Operation;
            public readonly float Value;
            public Modifier(PlayerModifierOperation operation, float value) { Operation = operation; Value = value; }
        }
    }
}
