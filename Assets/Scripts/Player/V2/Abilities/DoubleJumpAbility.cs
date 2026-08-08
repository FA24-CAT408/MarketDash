using UnityEngine;

namespace CrazyMarket.Player.V2
{
    public sealed class DoubleJumpAbility : PlayerAbilityComponent
    {
        [SerializeField] private int extraJumps = 1;
        [SerializeField] private int remainingJumps;

        public int ExtraJumps => Mathf.Max(0, extraJumps);
        public int RemainingJumps => Mathf.Max(0, remainingJumps);

        private void Awake() => Reset();

        private void OnValidate()
        {
            extraJumps = Mathf.Max(0, extraJumps);
            if (!Application.isPlaying) remainingJumps = extraJumps;
        }

        public override void Reset() => remainingJumps = ExtraJumps;

        public override void Cancel(AbilityCancellationReason reason)
        {
            if (reason == AbilityCancellationReason.ControlBlocked ||
                reason == AbilityCancellationReason.ProfileChanged ||
                reason == AbilityCancellationReason.Replaced)
            {
                remainingJumps = 0;
            }
        }

        public override PlayerAbilityResult Evaluate(PlayerAbilityContext context)
        {
            if (context.Mode != LocomotionMode.Airborne || !context.Intent.JumpPressed ||
                RemainingJumps <= 0)
            {
                return PlayerAbilityResult.Rejected;
            }

            remainingJumps--;
            return PlayerAbilityResult.Jump(context.Tuning.JumpSpeed);
        }
    }
}
