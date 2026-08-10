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
        public PlayerAbilityAction Action { get; }
        public float VerticalInfluence { get; }
        public bool Accepted => Action != PlayerAbilityAction.None;

        private PlayerAbilityResult(PlayerAbilityAction action, float verticalInfluence)
        {
            Action = action;
            VerticalInfluence = verticalInfluence;
        }

        public static PlayerAbilityResult Rejected =>
            new PlayerAbilityResult(PlayerAbilityAction.None, 0f);
        public static PlayerAbilityResult AirJump(float verticalInfluence) =>
            new PlayerAbilityResult(PlayerAbilityAction.AirJump, verticalInfluence);
    }

    public enum PlayerAbilityAction
    {
        None,
        AirJump
    }

    public interface IPlayerAbility
    {
        bool IsParticipating { get; }
        void Reset();
        void Cancel(AbilityCancellationReason reason);
        PlayerAbilityResult Evaluate(PlayerAbilityContext context);
    }

    // Components are the composition seam: add, remove, or disable one on the player
    // GameObject to change the ability set without changing the locomotion machine.
    public abstract class PlayerAbilityComponent : MonoBehaviour, IPlayerAbility
    {
        public bool IsParticipating => isActiveAndEnabled;
        public abstract void Reset();
        public abstract void Cancel(AbilityCancellationReason reason);
        public abstract PlayerAbilityResult Evaluate(PlayerAbilityContext context);
    }
}
