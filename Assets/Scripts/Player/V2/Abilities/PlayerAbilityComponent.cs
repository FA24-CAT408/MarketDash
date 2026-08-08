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

        private PlayerAbilityResult(bool accepted, float verticalInfluence)
        {
            Accepted = accepted;
            VerticalInfluence = verticalInfluence;
        }

        public static PlayerAbilityResult Rejected => new PlayerAbilityResult(false, 0f);
        public static PlayerAbilityResult Jump(float verticalInfluence) =>
            new PlayerAbilityResult(true, verticalInfluence);
    }

    // Components are the composition seam: add, remove, or disable one on the player
    // GameObject to change the ability set without changing the locomotion machine.
    public abstract class PlayerAbilityComponent : MonoBehaviour
    {
        public bool IsParticipating => isActiveAndEnabled;
        public abstract int RemainingCharges { get; }
        public abstract void Reset();
        public abstract void Cancel(AbilityCancellationReason reason);
        public abstract PlayerAbilityResult Evaluate(PlayerAbilityContext context);
    }
}
