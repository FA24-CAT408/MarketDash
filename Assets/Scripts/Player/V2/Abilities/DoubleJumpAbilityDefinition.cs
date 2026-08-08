using UnityEngine;

namespace CrazyMarket.Player.V2
{
    [CreateAssetMenu(menuName = "CrazyMarket/Player/V2/Double Jump Ability",
        fileName = "DoubleJumpAbilityV2")]
    public sealed class DoubleJumpAbilityDefinition : PlayerAbilityDefinition
    {
        public override PlayerAbilityId Id => PlayerAbilityId.DoubleJump;
        public override PlayerAbilityData CreateRuntimeData() =>
            new PlayerAbilityData(PlayerAbilityId.DoubleJump);
    }
}
