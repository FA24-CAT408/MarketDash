using CrazyMarket.Player;
using UnityEngine;

namespace CrazyMarket.Player.V2.Unity
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerControllerV2))]
    public sealed class TestCampusPlayerV2Bridge : MonoBehaviour, IPlayerSceneControl
    {
        [SerializeField] private PlayerControllerV2 controller;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerControllerV2>();
        }

        public void SetMovementEnabled(bool enabled) => controller?.SetMovementEnabled(enabled);

        public void SetMovementReference(Transform reference) => controller?.SetMovementReference(reference);

        public bool TryGetMovementIntent(out Vector3 direction)
        {
            if (controller != null) return controller.TryGetMovementIntent(out direction);
            direction = Vector3.zero;
            return false;
        }

        public void TeleportTo(Vector3 position, Quaternion rotation) =>
            controller?.TeleportTo(position, rotation);
    }
}
