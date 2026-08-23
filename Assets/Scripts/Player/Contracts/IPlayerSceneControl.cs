using UnityEngine;

namespace CrazyMarket.Player
{
    public interface IPlayerSceneControl
    {
        void SetMovementEnabled(bool enabled);
        void SetMovementReference(Transform reference);
        bool TryGetMovementIntent(out Vector3 direction);
        void TeleportTo(Vector3 position, Quaternion rotation);
    }
}
