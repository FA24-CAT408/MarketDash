using System;
using UnityEngine;
namespace CrazyMarket.Player.V2
{
    public enum AbilityCancellationReason
    {
        Landed,
        ControlBlocked,
        ProfileChanged,
        Replaced
    }

    public readonly struct PlayerAbilityContext
    {
        public LocomotionMode Mode { get; }
        public PlayerIntent Intent { get; }
        public PlayerBodyObservation Body { get; }
        public LocomotionTuning Tuning { get; }

        internal PlayerAbilityContext(LocomotionMode mode, PlayerIntent intent,
            PlayerBodyObservation body, LocomotionTuning tuning)
        {
            Mode = mode;
            Intent = intent;
            Body = body;
            Tuning = tuning;
        }
    }

    public readonly struct PlayerAbilityResult
    {
        public bool Accepted { get; }
        public float VerticalInfluence { get; }

        internal PlayerAbilityResult(bool accepted, float verticalInfluence)
        {
            Accepted = accepted;
            VerticalInfluence = verticalInfluence;
        }

        public static PlayerAbilityResult Rejected => new PlayerAbilityResult(false, 0f);
        public static PlayerAbilityResult Jump(float verticalInfluence) =>
            new PlayerAbilityResult(true, verticalInfluence);
    }

    public interface IPlayerAbilityRuntime
    {
        PlayerAbilityId Id { get; }
        int RemainingCharges { get; }
        void Reset();
        void Cancel(AbilityCancellationReason reason);
        PlayerAbilityResult Evaluate(PlayerAbilityContext context);
    }

    public readonly struct PlayerAbilityData
    {
        public PlayerAbilityId Id { get; }
        public bool IsValid => Id == PlayerAbilityId.DoubleJump;

        public PlayerAbilityData(PlayerAbilityId id)
        {
            Id = id;
        }
    }

    public abstract class PlayerAbilityDefinition : ScriptableObject
    {
        public abstract PlayerAbilityId Id { get; }
        public abstract PlayerAbilityData CreateRuntimeData();
    }

    [CreateAssetMenu(menuName = "CrazyMarket/Player/V2/Double Jump Ability",
        fileName = "DoubleJumpAbilityV2")]
    public sealed class DoubleJumpAbilityDefinition : PlayerAbilityDefinition
    {
        public override PlayerAbilityId Id => PlayerAbilityId.DoubleJump;
        public override PlayerAbilityData CreateRuntimeData() =>
            new PlayerAbilityData(PlayerAbilityId.DoubleJump);
    }

    internal sealed class DoubleJumpAbilityRuntime : IPlayerAbilityRuntime
    {
        private bool available = true;

        public PlayerAbilityId Id => PlayerAbilityId.DoubleJump;
        public int RemainingCharges => available ? 1 : 0;

        public void Reset() { available = true; }

        public void Cancel(AbilityCancellationReason reason)
        {
            if (reason == AbilityCancellationReason.ControlBlocked ||
                reason == AbilityCancellationReason.ProfileChanged ||
                reason == AbilityCancellationReason.Replaced)
            {
                available = false;
            }
        }

        public PlayerAbilityResult Evaluate(PlayerAbilityContext context)
        {
            if (context.Mode != LocomotionMode.Airborne || !context.Intent.JumpPressed || !available)
            {
                return PlayerAbilityResult.Rejected;
            }

            available = false;
            return PlayerAbilityResult.Jump(context.Tuning.JumpSpeed);
        }
    }
}
