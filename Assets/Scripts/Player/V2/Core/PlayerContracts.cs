using System;
using UnityEngine;

namespace CrazyMarket.Player.V2
{
    public enum LocomotionMode
    {
        Disabled,
        Grounded,
        Airborne
    }

    public enum PlayerModifierOperation
    {
        Additive,
        Multiplicative
    }

    public enum PlayerStat
    {
        StableMoveSpeed,
        AirMoveSpeed,
        AirAcceleration,
        JumpSpeed,
        JumpBufferTime,
        CoyoteTime,
        Gravity,
        FallGravityMultiplier,
        Drag,
        StableMovementSharpness,
        OrientationSharpness
    }

    public enum PlayerOperationResult
    {
        Accepted,
        RejectedInvalidArgument,
        RejectedInvalidProfile,
        RejectedMissingAbility,
        RejectedUnknownModifier
    }

    public readonly struct PlayerProfileId
    {
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public PlayerProfileId(string value) { Value = value ?? string.Empty; }
        public override bool Equals(object obj) => obj is PlayerProfileId && Value == ((PlayerProfileId)obj).Value;
        public override int GetHashCode() => Value == null ? 0 : Value.GetHashCode();
        public override string ToString() => Value;
    }

    public readonly struct RuntimeProfileId
    {
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public RuntimeProfileId(string value) { Value = value ?? string.Empty; }
        public override bool Equals(object obj) => obj is RuntimeProfileId && Value == ((RuntimeProfileId)obj).Value;
        public override int GetHashCode() => Value == null ? 0 : Value.GetHashCode();
        public override string ToString() => Value;
    }

    public readonly struct PlayerModifierId
    {
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public PlayerModifierId(string value) { Value = value ?? string.Empty; }
        public override bool Equals(object obj) => obj is PlayerModifierId && Value == ((PlayerModifierId)obj).Value;
        public override int GetHashCode() => Value == null ? 0 : Value.GetHashCode();
        public override string ToString() => Value;
    }

    public readonly struct PlayerIntent
    {
        public Vector2 Move { get; }
        public bool JumpPressed { get; }
        public bool JumpHeld { get; }

        public PlayerIntent(Vector2 move, bool jumpPressed, bool jumpHeld)
        {
            Move = move;
            JumpPressed = jumpPressed;
            JumpHeld = jumpHeld;
        }

        public PlayerIntent(Vector2 move, bool jumpPressed) : this(move, jumpPressed, jumpPressed) { }
        public static PlayerIntent Empty => new PlayerIntent(Vector2.zero, false, false);
    }

    public readonly struct PlayerBodyObservation
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Velocity { get; }
        public bool StableGrounded { get; }
        public bool WalkableGround { get; }
        public Vector3 GroundNormal { get; }

        public PlayerBodyObservation(Vector3 position, Quaternion rotation, Vector3 velocity,
            bool stableGrounded, bool walkableGround, Vector3 groundNormal)
        {
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            StableGrounded = stableGrounded;
            WalkableGround = walkableGround;
            GroundNormal = groundNormal.sqrMagnitude > 0f ? groundNormal.normalized : Vector3.up;
        }

        public static PlayerBodyObservation DefaultGrounded => new PlayerBodyObservation(
            Vector3.zero, Quaternion.identity, Vector3.zero, true, true, Vector3.up);
    }

    [Flags]
    public enum PlayerActionFlags
    {
        None = 0,
        Jumped = 1,
        DoubleJumped = 2,
        Grounded = 4,
        ControlBlocked = 8,
        ProfileChanged = 16,
        Teleported = 32
    }

    public readonly struct LocomotionOutput
    {
        public LocomotionMode Mode { get; }
        public Vector3 TargetPlanarVelocity { get; }
        public float AirAcceleration { get; }
        public float StableMovementSharpness { get; }
        public float OrientationSharpness { get; }
        public float Drag { get; }
        public Vector3 Gravity { get; }
        public bool ApplyGrounding { get; }
        public bool HasJumpInfluence { get; }
        public float JumpVerticalVelocity { get; }
        public PlayerActionFlags ActionFlags { get; }
        public bool HasTeleport { get; }
        public Vector3 TeleportPosition { get; }
        public Quaternion TeleportRotation { get; }
        public bool ResetVelocity { get; }

        internal LocomotionOutput(LocomotionMode mode, Vector3 targetPlanarVelocity, float airAcceleration,
            float stableMovementSharpness, float orientationSharpness, float drag, Vector3 gravity,
            bool applyGrounding, bool hasJumpInfluence, float jumpVerticalVelocity,
            PlayerActionFlags actionFlags, bool hasTeleport, Vector3 teleportPosition,
            Quaternion teleportRotation, bool resetVelocity)
        {
            Mode = mode;
            TargetPlanarVelocity = targetPlanarVelocity;
            AirAcceleration = airAcceleration;
            StableMovementSharpness = stableMovementSharpness;
            OrientationSharpness = orientationSharpness;
            Drag = drag;
            Gravity = gravity;
            ApplyGrounding = applyGrounding;
            HasJumpInfluence = hasJumpInfluence;
            JumpVerticalVelocity = jumpVerticalVelocity;
            ActionFlags = actionFlags;
            HasTeleport = hasTeleport;
            TeleportPosition = teleportPosition;
            TeleportRotation = teleportRotation;
            ResetVelocity = resetVelocity;
        }

        public static LocomotionOutput Empty => new LocomotionOutput(
            LocomotionMode.Disabled, Vector3.zero, 0f, 0f, 0f, 0f, Vector3.zero, false, false, 0f,
            PlayerActionFlags.None, false, Vector3.zero, Quaternion.identity, false);
    }

    public readonly struct PlayerPresentationState
    {
        public LocomotionMode Mode { get; }
        public bool Grounded { get; }
        public float PlanarSpeed { get; }
        public float VerticalSpeed { get; }
        public Vector2 MoveIntent { get; }
        public PlayerActionFlags ActionFlags { get; }

        internal PlayerPresentationState(LocomotionMode mode, bool grounded, float planarSpeed,
            float verticalSpeed, Vector2 moveIntent, PlayerActionFlags actionFlags)
        {
            Mode = mode;
            Grounded = grounded;
            PlanarSpeed = planarSpeed;
            VerticalSpeed = verticalSpeed;
            MoveIntent = moveIntent;
            ActionFlags = actionFlags;
        }
    }

    public readonly struct PlayerSnapshot
    {
        public long Revision { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Velocity { get; }
        public LocomotionMode Mode { get; }
        public bool StableGrounded { get; }
        public bool ControlBlocked { get; }
        public int ControlBlockCount { get; }
        public int AirJumpsRemaining { get; }
        public float CoyoteTimeRemaining { get; }
        public float JumpBufferRemaining { get; }
        public PlayerProfileId ProfileId { get; }
        public RuntimeProfileId RuntimeProfileId { get; }
        public PlayerActionFlags ActionFlags { get; }

        internal PlayerSnapshot(long revision, PlayerBodyObservation observation, Vector3 velocity,
            LocomotionMode mode, int controlBlockCount, int airJumpsRemaining, float coyoteTimeRemaining,
            float jumpBufferRemaining, PlayerProfileId profileId, RuntimeProfileId runtimeProfileId,
            PlayerActionFlags actionFlags)
        {
            Revision = revision;
            Position = observation.Position;
            Rotation = observation.Rotation;
            Velocity = velocity;
            Mode = mode;
            StableGrounded = observation.StableGrounded && observation.WalkableGround;
            ControlBlockCount = controlBlockCount;
            ControlBlocked = controlBlockCount > 0;
            AirJumpsRemaining = airJumpsRemaining;
            CoyoteTimeRemaining = coyoteTimeRemaining;
            JumpBufferRemaining = jumpBufferRemaining;
            ProfileId = profileId;
            RuntimeProfileId = runtimeProfileId;
            ActionFlags = actionFlags;
        }
    }

    public interface IPlayerController
    {
        PlayerSnapshot Snapshot { get; }
        PlayerPresentationState Presentation { get; }
        PlayerOperationResult SetControlBlocked(string source, bool blocked);
        PlayerOperationResult Teleport(Vector3 position, Quaternion rotation, bool resetVelocity = true);
        PlayerOperationResult SelectProfile(PlayerProfile profile);
        PlayerRuntimeProfile CaptureRuntimeProfile();
        PlayerOperationResult ReplaceRuntimeProfile(PlayerRuntimeProfile profile);
        PlayerOperationResult SetModifier(PlayerModifierId id, PlayerStat stat,
            PlayerModifierOperation operation, float value);
        PlayerOperationResult RemoveModifier(PlayerModifierId id, PlayerStat stat);
    }
}
