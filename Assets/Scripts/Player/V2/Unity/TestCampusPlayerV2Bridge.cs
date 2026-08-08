using CrazyMarket.TestCampus;
using UnityEngine;

namespace CrazyMarket.Player.V2.Unity
{
    // Keeps Test Campus concerns at the scene-integration boundary. The player
    // controller itself remains usable without a reference to Test Campus.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerControllerV2))]
    public sealed class TestCampusPlayerV2Bridge : MonoBehaviour, ITestCampusPlayerController
    {
        [SerializeField] private PlayerControllerV2 controller;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerControllerV2>();

            Time.timeScale = 1f;
            SetMovementEnabled(true);
            DisableLegacyShadows();

            if (GetComponent<TestCampusBlobShadow>() == null)
                gameObject.AddComponent<TestCampusBlobShadow>();
        }

        private void OnEnable() => SetMovementEnabled(true);

        public void SetMovementEnabled(bool enabled)
        {
            if (controller != null) controller.SetMovementEnabled(enabled);
        }

        public bool TryGetMovementIntent(out Vector3 direction)
        {
            if (controller != null) return controller.TryGetMovementIntent(out direction);
            direction = Vector3.zero;
            return false;
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            if (controller != null) controller.TeleportTo(position, rotation);
        }

        private void DisableLegacyShadows()
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child != transform && child.name.Contains("Shadow Decal", System.StringComparison.OrdinalIgnoreCase))
                    child.gameObject.SetActive(false);
            }
        }
    }
}
