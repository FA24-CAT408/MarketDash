using System;
using CrazyMarket.Player;
using UnityEngine;
using System.Reflection;

namespace CrazyMarket.TestCampus
{
    [DisallowMultipleComponent]
    public sealed class TestCampusPlayerAdapter : MonoBehaviour, IPlayerSceneControl
    {
        public static event Action<Transform, Vector3> PlayerWarped;

        private Component _controller;
        private FieldInfo _movementIntent;

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
            SetMovementEnabled(true);
        }

        private void EnableMovement() => SetMovementEnabled(true);

        public void SetMovementEnabled(bool enabled)
        {
            _controller ??= GetComponent("KCCPlayerController");
            if (_controller == null) return;

            FieldInfo canMove = _controller.GetType().GetField("canMove", BindingFlags.Instance | BindingFlags.Public);
            if (canMove?.FieldType == typeof(bool))
            {
                canMove.SetValue(_controller, enabled);
                return;
            }

            _controller.GetType()
                .GetMethod("SetMovementEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.Invoke(_controller, new object[] { enabled });
        }

        public bool TryGetMovementIntent(out Vector3 direction)
        {
            direction = Vector3.zero;
            _controller ??= GetComponent("KCCPlayerController");
            if (_controller == null) return false;

            _movementIntent ??= _controller.GetType().GetField(
                "_moveInputVector", BindingFlags.Instance | BindingFlags.NonPublic);
            if (_movementIntent?.FieldType != typeof(Vector3)) return false;

            direction = (Vector3)_movementIntent.GetValue(_controller);
            direction.y = 0f;
            return true;
        }

        public void SetMovementReference(Transform reference)
        {
            _controller ??= GetComponent("KCCPlayerController");
            _controller?.GetType()
                .GetMethod("SetCameraMovementReference",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.Invoke(_controller, new object[] { reference });
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
            Vector3 previousPosition = transform.position;
            _controller ??= GetComponent("KCCPlayerController");
            object motor = _controller?.GetType().GetField("Motor", BindingFlags.Instance | BindingFlags.Public)?.GetValue(_controller);
            if (motor != null)
            {
                MethodInfo teleport = motor.GetType().GetMethod(
                    "SetPositionAndRotation",
                    new[] { typeof(Vector3), typeof(Quaternion), typeof(bool) });
                if (teleport != null)
                {
                    teleport.Invoke(motor, new object[] { position, rotation, true });
                    motor.GetType().GetField("BaseVelocity", BindingFlags.Instance | BindingFlags.Public)
                        ?.SetValue(motor, Vector3.zero);
                }
                else
                {
                    Debug.LogWarning("KCC motor has no compatible teleport method; using Transform fallback.", this);
                    transform.SetPositionAndRotation(position, rotation);
                }
            }
            else
                transform.SetPositionAndRotation(position, rotation);

            PlayerWarped?.Invoke(transform, transform.position - previousPosition);
        }
    }
}
