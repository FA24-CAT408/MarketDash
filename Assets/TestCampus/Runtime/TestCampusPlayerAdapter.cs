using UnityEngine;
using System.Reflection;

namespace CrazyMarket.TestCampus
{
    [DisallowMultipleComponent]
    public sealed class TestCampusPlayerAdapter : MonoBehaviour
    {
        private Component _controller;

        private void Awake()
        {
            Time.timeScale = 1f;
            _controller = GetComponent("KCCPlayerController");
            EnableMovement();
            DisableLegacyShadows();

            if (GetComponent<TestCampusBlobShadow>() == null)
                gameObject.AddComponent<TestCampusBlobShadow>();
        }

        private void OnEnable()
        {
            EnableMovement();
        }

        private void EnableMovement()
        {
            if (_controller != null)
                _controller.SendMessage("SetMovementEnabled", true, SendMessageOptions.RequireReceiver);
        }

        private void DisableLegacyShadows()
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child != transform && child.name.Contains("Shadow Decal", System.StringComparison.OrdinalIgnoreCase))
                    child.gameObject.SetActive(false);
            }
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            _controller ??= GetComponent("KCCPlayerController");
            object motor = _controller?.GetType().GetField("Motor", BindingFlags.Instance | BindingFlags.Public)?.GetValue(_controller);
            if (motor != null)
            {
                motor.GetType().GetMethod("SetPositionAndRotation", new[] { typeof(Vector3), typeof(Quaternion), typeof(bool) })
                    ?.Invoke(motor, new object[] { position, rotation, true });
                motor.GetType().GetField("BaseVelocity", BindingFlags.Instance | BindingFlags.Public)
                    ?.SetValue(motor, Vector3.zero);
                return;
            }

            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
