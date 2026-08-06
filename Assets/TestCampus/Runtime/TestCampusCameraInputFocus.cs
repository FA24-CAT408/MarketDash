using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CrazyMarket.TestCampus
{
    /// <summary>Owns camera look input and the cursor/UI focus hand-off.</summary>
    [DisallowMultipleComponent]
    public sealed class TestCampusCameraInputFocus : MonoBehaviour
    {
        [SerializeField] private float mouseSensitivity = 0.08f;
        [SerializeField] private float legacyMouseSensitivity = 0.8f;
        [SerializeField] private float controllerYawSpeed = 140f;
        [SerializeField] private float controllerPitchSpeed = 95f;

        private InputAction _mouseLookAction;
        private Vector2 _pendingMouseLookDelta;
        private bool _uiHasFocus;

        public bool UiHasFocus => _uiHasFocus;
        public bool PointerLookActive =>
            Cursor.lockState == CursorLockMode.Confined && !Cursor.visible;
        public string InputSource { get; private set; } = "Waiting for look input";
        public string FocusStatus => _uiHasFocus
            ? "UI INPUT"
            : PointerLookActive ? "CONFINED MOUSE LOOK" : "POINTER VISIBLE";

        private void OnEnable()
        {
            _mouseLookAction = new InputAction(
                "Test Campus Mouse Look",
                InputActionType.PassThrough,
                "<Mouse>/delta",
                expectedControlType: "Vector2");
            _mouseLookAction.performed += AccumulateMouseLook;
            _mouseLookAction.Enable();
            ApplyCursorState();
        }

        private void OnDisable()
        {
            if (_mouseLookAction != null)
            {
                _mouseLookAction.performed -= AccumulateMouseLook;
                _mouseLookAction.Disable();
                _mouseLookAction.Dispose();
                _mouseLookAction = null;
            }
            ResetInput();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            bool wantsPointerLook = !_uiHasFocus && HasGameplayPointerFocus();
            if (wantsPointerLook != PointerLookActive)
                ApplyCursorState();
        }

        private void OnApplicationFocus(bool focused) => ApplyCursorState();

        public void SetUiFocus(bool hasFocus)
        {
            _uiHasFocus = hasFocus;
            InputSource = hasFocus ? "UI focus" : "Waiting for look input";
            ResetInput();
            ApplyCursorState();
        }

        public Vector2 ConsumeLookInput()
        {
            Vector2 input = Vector2.zero;
            if (_uiHasFocus || !HasGameplayPointerFocus())
            {
                InputSource = _uiHasFocus ? "UI focus" : "Game view not focused";
                ResetInput();
                return input;
            }

            Vector2 actionMouseDelta = _pendingMouseLookDelta;
            _pendingMouseLookDelta = Vector2.zero;
            if (actionMouseDelta.sqrMagnitude > 0.0001f)
            {
                input += actionMouseDelta * mouseSensitivity;
                InputSource = "Input Action mouse delta";
            }

            if (input.sqrMagnitude <= 0.0001f && Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                if (mouseDelta.sqrMagnitude > 0.0001f)
                {
                    input += mouseDelta * mouseSensitivity;
                    InputSource = "Input System mouse";
                }
            }

            if (input.sqrMagnitude <= 0.0001f)
            {
                Vector2 legacyDelta = new(
                    Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
                if (legacyDelta.sqrMagnitude > 0.0001f)
                {
                    input += legacyDelta * legacyMouseSensitivity;
                    InputSource = "Legacy mouse fallback";
                }
            }

            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.rightStick.ReadValue();
                input.x += stick.x * controllerYawSpeed * Time.unscaledDeltaTime;
                input.y += stick.y * controllerPitchSpeed * Time.unscaledDeltaTime;
                if (stick.sqrMagnitude > 0.0001f)
                    InputSource = "Gamepad right stick";
            }
            return input;
        }

        public void ResetInput()
        {
            _pendingMouseLookDelta = Vector2.zero;
        }

        private void AccumulateMouseLook(InputAction.CallbackContext context)
        {
            if (PointerLookActive)
                _pendingMouseLookDelta += context.ReadValue<Vector2>();
        }

        private void ApplyCursorState()
        {
            bool wantsPointerLook = !_uiHasFocus && HasGameplayPointerFocus();
            Cursor.lockState = wantsPointerLook ? CursorLockMode.Confined : CursorLockMode.None;
            Cursor.visible = !wantsPointerLook;
        }

        private static bool HasGameplayPointerFocus()
        {
#if UNITY_EDITOR
            EditorWindow focusedWindow = EditorWindow.focusedWindow;
            return focusedWindow != null && focusedWindow.GetType().Name == "GameView";
#else
            return Application.isFocused;
#endif
        }
    }
}
