using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CrazyMarket.TestCampus
{
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class TestCampusCameraPrototypeController : MonoBehaviour, ITestResettable
    {
        [Header("Assisted orbit")]
        [SerializeField] private float mouseSensitivity = 0.08f;
        [SerializeField] private float legacyMouseSensitivity = 0.8f;
        [SerializeField] private float controllerYawSpeed = 140f;
        [SerializeField] private float controllerPitchSpeed = 95f;
        [SerializeField] private float recenterDelay = 2.5f;
        [SerializeField] private float recenterSmoothTime = 1.2f;
        [SerializeField] private float maximumRecenterSpeed = 65f;
        [SerializeField] private float minimumMovementSpeed = 0.6f;

        [Header("Selective obstruction")]
        [SerializeField] private float occlusionProbeRadius = 0.22f;

        private readonly HashSet<TestCampusSelectiveOccluder> _hidden = new();
        private readonly HashSet<TestCampusSelectiveOccluder> _nextHidden = new();
        private CinemachineCamera _assistedCamera;
        private CinemachineCamera _railCamera;
        private CinemachineOrbitalFollow _orbit;
        private TestCampusCameraSurfaceConstraint _surfaceConstraint;
        private Transform _player;
        private TestCampusPlayerAdapter _playerAdapter;
        private Vector3 _previousPlayerPosition;
        private Vector3 _lastGroundedMoveDirection = Vector3.forward;
        private InputAction _mouseLookAction;
        private Vector2 _pendingMouseLookDelta;
        private float _lastManualInputTime = float.NegativeInfinity;
        private float _recenterVelocity;
        private float _latchedRecenterHeading;
        private Vector2 _lastLookDelta;
        private string _lookInputSource = "Waiting for look input";
        private bool _uiHasFocus;
        private bool _guidedZoneActive;
        private bool _autoRecenterActive;
        private bool _recenterConsumedForMovement;
        private TestCampusCameraMode _mode = TestCampusCameraMode.AssistedOrbit;
        private float _initialYaw;
        private float _initialPitch;
        private float _initialRadiusScale = 1f;

        public static TestCampusCameraPrototypeController Instance { get; private set; }
        public TestCampusCameraMode Mode => _mode;
        public bool UiHasFocus => _uiHasFocus;
        public bool IsRecentering => _autoRecenterActive;
        public Vector2 LastLookDelta => _lastLookDelta;
        public string LookInputSource => _lookInputSource;
        public float Yaw => _orbit != null ? _orbit.HorizontalAxis.Value : 0f;
        public float Pitch => _orbit != null ? _orbit.VerticalAxis.Value : 0f;
        public bool PointerLookActive =>
            Cursor.lockState == CursorLockMode.Confined && !Cursor.visible;
        public bool OrbitLive => IsOrbitLive();
        public float OrbitRadius => _surfaceConstraint != null ? _surfaceConstraint.OrbitRadius : 0f;
        public bool FloorLimitActive => _surfaceConstraint != null && _surfaceConstraint.FloorLimitActive;
        public float FloorPitchLimit => _surfaceConstraint != null ? _surfaceConstraint.FloorPitchLimit : 0f;
        public bool CeilingLimitActive => _surfaceConstraint != null && _surfaceConstraint.CeilingLimitActive;
        public float CeilingPitchLimit => _surfaceConstraint != null ? _surfaceConstraint.CeilingPitchLimit : 0f;
        public float CameraY => Camera.main != null ? Camera.main.transform.position.y : 0f;
        public string Status =>
            $"{_mode} | {FocusStatus} | "
            + $"Yaw {Yaw:0}° Pitch {Pitch:0}°"
            + $" | R {OrbitRadius:0.00}m CamY {CameraY:0.00}"
            + (FloorLimitActive ? $" | FLOOR {FloorPitchLimit:0.0}°" : "")
            + (CeilingLimitActive ? $" | CEILING {CeilingPitchLimit:0.0}°" : "")
            + $" | {_lookInputSource}"
            + (_autoRecenterActive ? " | RECENTERING" : "");

        private string FocusStatus => _uiHasFocus
            ? "UI INPUT"
            : PointerLookActive ? "CONFINED MOUSE LOOK" : "POINTER VISIBLE";

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            TestCampusPlayerAdapter.PlayerWarped += OnPlayerWarped;
            _mouseLookAction = new InputAction(
                "Test Campus Mouse Look",
                InputActionType.PassThrough,
                "<Mouse>/delta",
                expectedControlType: "Vector2");
            _mouseLookAction.performed += AccumulateMouseLook;
            _mouseLookAction.Enable();
        }

        private void OnDisable()
        {
            TestCampusPlayerAdapter.PlayerWarped -= OnPlayerWarped;
            if (_mouseLookAction == null)
                return;

            _mouseLookAction.performed -= AccumulateMouseLook;
            _mouseLookAction.Disable();
            _mouseLookAction.Dispose();
            _mouseLookAction = null;
            _pendingMouseLookDelta = Vector2.zero;
        }

        private void Start()
        {
            RefreshRigs();
            if (_player != null)
                _previousPlayerPosition = _player.position;
            ApplyMode();
            SetUiFocus(false);
            CaptureInitialState();
        }

        private void OnDestroy()
        {
            RestoreOccluders();
            if (Instance == this)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Instance = null;
            }
        }

        private void Update()
        {
            if (_player == null || _assistedCamera == null || _orbit == null || _railCamera == null)
                RefreshRigs();

            bool wantsPointerLook = !_uiHasFocus && HasGameplayPointerFocus();
            if (wantsPointerLook
                && (Cursor.lockState != CursorLockMode.Confined || Cursor.visible))
                ApplyCursorState();
            else if (!wantsPointerLook
                     && (Cursor.lockState != CursorLockMode.None || !Cursor.visible))
                ApplyCursorState();

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f6Key.wasPressedThisFrame) SetMode(TestCampusCameraMode.AssistedOrbit);
                if (keyboard.f7Key.wasPressedThisFrame) SetMode(TestCampusCameraMode.GuidedRail);
                if (keyboard.f8Key.wasPressedThisFrame) SetMode(TestCampusCameraMode.HybridZones);
                if (keyboard.rKey.wasPressedThisFrame) RecenterNow();
            }

            if (_player == null || _orbit == null)
                return;

            Vector3 displacement = _player.position - _previousPlayerPosition;
            _previousPlayerPosition = _player.position;
            Vector3 planarVelocity = new(displacement.x, 0f, displacement.z);
            if (Time.unscaledDeltaTime > 0f)
                planarVelocity /= Time.unscaledDeltaTime;

            bool grounded = Physics.Raycast(
                _player.position + Vector3.up * 0.25f, Vector3.down, 0.65f, ~0, QueryTriggerInteraction.Ignore);
            if (grounded && planarVelocity.magnitude >= minimumMovementSpeed)
                _lastGroundedMoveDirection = planarVelocity.normalized;

            if (IsOrbitLive())
                UpdateAssistedOrbit(grounded, planarVelocity.magnitude);
            else
                _pendingMouseLookDelta = Vector2.zero;

        }

        private void LateUpdate() => UpdateSelectiveOcclusion();

        public void SetMode(TestCampusCameraMode mode)
        {
            _mode = mode;
            _pendingMouseLookDelta = Vector2.zero;
            ApplyMode();
        }

        public void SetGuidedZoneActive(bool active)
        {
            _guidedZoneActive = active;
            if (_mode == TestCampusCameraMode.HybridZones)
                ApplyMode();
        }

        public void SetUiFocus(bool hasFocus)
        {
            _uiHasFocus = hasFocus;
            _lookInputSource = hasFocus ? "UI focus" : "Waiting for look input";
            _pendingMouseLookDelta = Vector2.zero;
            ApplyCursorState();
            _playerAdapter?.SetMovementEnabled(!hasFocus);
        }

        public void CaptureInitialState()
        {
            _initialYaw = _orbit != null ? _orbit.HorizontalAxis.Value : 0f;
            _initialPitch = _orbit != null ? _orbit.VerticalAxis.Value : 0f;
            _initialRadiusScale = _orbit != null ? _orbit.RadialAxis.Value : 1f;
        }

        public void ResetToInitialState()
        {
            Time.timeScale = 1f;
            _guidedZoneActive = false;
            _pendingMouseLookDelta = Vector2.zero;
            _lastLookDelta = Vector2.zero;
            _lastManualInputTime = float.NegativeInfinity;
            _recenterVelocity = 0f;
            _autoRecenterActive = false;
            _recenterConsumedForMovement = false;
            RestoreOccluders();
            if (_orbit != null)
            {
                _orbit.HorizontalAxis.Value = _orbit.HorizontalAxis.ClampValue(_initialYaw);
                _orbit.VerticalAxis.Value = _orbit.VerticalAxis.ClampValue(_initialPitch);
                _orbit.RadialAxis.Value = _orbit.RadialAxis.ClampValue(_initialRadiusScale);
            }
            _surfaceConstraint?.ResetConstraint(_initialRadiusScale);
            SetMode(TestCampusCameraMode.AssistedOrbit);
            SetUiFocus(false);
        }

        private void OnPlayerWarped(Transform player, Vector3 positionDelta)
        {
            if (_player == null || player != _player)
                return;
            _assistedCamera?.OnTargetObjectWarped(_player, positionDelta);
            _railCamera?.OnTargetObjectWarped(_player, positionDelta);
            _previousPlayerPosition = _player.position;
        }

        private void OnApplicationFocus(bool focused)
        {
            ApplyCursorState();
        }

        private void ApplyCursorState()
        {
            // Confined keeps the pointer inside the Game view without warping it
            // back to the center like CursorLockMode.Locked.
            bool wantsPointerLook = !_uiHasFocus && HasGameplayPointerFocus();
            Cursor.lockState = wantsPointerLook ? CursorLockMode.Confined : CursorLockMode.None;
            Cursor.visible = !wantsPointerLook;
        }

        private void AccumulateMouseLook(InputAction.CallbackContext context)
        {
            if (!PointerLookActive)
                return;

            _pendingMouseLookDelta += context.ReadValue<Vector2>();
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

        public void RecenterNow()
        {
            if (_orbit == null)
                return;
            _orbit.HorizontalAxis.Value = HeadingFor(_lastGroundedMoveDirection);
            _recenterVelocity = 0f;
            _autoRecenterActive = false;
            _recenterConsumedForMovement = true;
            _lastManualInputTime = Time.unscaledTime;
        }

        private void RefreshRigs()
        {
            _player = TestCampusController.Instance != null
                ? TestCampusController.Instance.PlayerRoot
                : GameObject.FindGameObjectWithTag("Player")?.transform;
            _playerAdapter = _player != null ? _player.GetComponent<TestCampusPlayerAdapter>() : null;

            foreach (TestCampusCameraRigTag tag in FindObjectsByType<TestCampusCameraRigTag>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                CinemachineCamera camera = tag.GetComponent<CinemachineCamera>();
                if (tag.Mode == TestCampusCameraMode.AssistedOrbit)
                {
                    _assistedCamera = camera;
                    _orbit = camera != null ? camera.GetComponent<CinemachineOrbitalFollow>() : null;
                    _surfaceConstraint = camera != null
                        ? camera.GetComponent<TestCampusCameraSurfaceConstraint>()
                        : null;
                }
                else if (tag.Mode == TestCampusCameraMode.GuidedRail)
                    _railCamera = camera;
            }

            if (_player != null)
            {
                if (_assistedCamera != null)
                {
                    _assistedCamera.Follow = _player;
                    _assistedCamera.LookAt = _player;
                }
                if (_railCamera != null)
                {
                    _railCamera.Follow = _player;
                    _railCamera.LookAt = _player;
                }
            }
            ApplyMode();
        }

        private void ApplyMode()
        {
            bool railLive = _mode == TestCampusCameraMode.GuidedRail
                || (_mode == TestCampusCameraMode.HybridZones && _guidedZoneActive);
            if (_assistedCamera != null)
                _assistedCamera.Priority.Value = railLive ? 10 : 30;
            if (_railCamera != null)
                _railCamera.Priority.Value = railLive ? 30 : 10;
            _surfaceConstraint?.SetOrbitLive(!railLive);
        }

        private bool IsOrbitLive() =>
            _mode == TestCampusCameraMode.AssistedOrbit
            || (_mode == TestCampusCameraMode.HybridZones && !_guidedZoneActive);

        private void UpdateAssistedOrbit(bool grounded, float speed)
        {
            Vector2 input = Vector2.zero;

            if (!_uiHasFocus)
            {
                Vector2 actionMouseDelta = _pendingMouseLookDelta;
                _pendingMouseLookDelta = Vector2.zero;
                if (actionMouseDelta.sqrMagnitude > 0.0001f)
                {
                    input += actionMouseDelta * mouseSensitivity;
                    _lookInputSource = "Input Action mouse delta";
                }

                if (input.sqrMagnitude <= 0.0001f && Mouse.current != null)
                {
                    Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                    if (mouseDelta.sqrMagnitude > 0.0001f)
                    {
                        input += mouseDelta * mouseSensitivity;
                        _lookInputSource = "Input System mouse";
                    }
                }

                if (input.sqrMagnitude <= 0.0001f)
                {
                    Vector2 legacyDelta = new(
                        Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
                    if (legacyDelta.sqrMagnitude > 0.0001f)
                    {
                        input += legacyDelta * legacyMouseSensitivity;
                        _lookInputSource = "Legacy mouse fallback";
                    }
                }

                if (Gamepad.current != null)
                {
                    Vector2 stick = Gamepad.current.rightStick.ReadValue();
                    input.x += stick.x * controllerYawSpeed * Time.unscaledDeltaTime;
                    input.y += stick.y * controllerPitchSpeed * Time.unscaledDeltaTime;
                    if (stick.sqrMagnitude > 0.0001f)
                        _lookInputSource = "Gamepad right stick";
                }
            }
            else if (_uiHasFocus)
                _lookInputSource = "UI focus";

            if (input.sqrMagnitude > 0.0001f)
            {
                _lastLookDelta = input;
                _orbit.HorizontalAxis.Value = _orbit.HorizontalAxis.ClampValue(
                    _orbit.HorizontalAxis.Value + input.x);
                _orbit.VerticalAxis.Value = _orbit.VerticalAxis.ClampValue(
                    _orbit.VerticalAxis.Value - input.y);
                _lastManualInputTime = Time.unscaledTime;
                _recenterVelocity = 0f;
                _autoRecenterActive = false;
                _recenterConsumedForMovement = false;
                return;
            }

            if (!grounded || speed < minimumMovementSpeed)
            {
                _autoRecenterActive = false;
                _recenterConsumedForMovement = false;
                _recenterVelocity = 0f;
                return;
            }

            if (Time.unscaledTime - _lastManualInputTime < recenterDelay)
                return;

            if (!_autoRecenterActive)
            {
                if (_recenterConsumedForMovement)
                    return;

                Vector3 currentCameraForward =
                    Quaternion.Euler(0f, _orbit.HorizontalAxis.Value, 0f) * Vector3.forward;
                float directionDifference = Mathf.Abs(Vector3.SignedAngle(
                    currentCameraForward, _lastGroundedMoveDirection, Vector3.up));
                if (directionDifference > 65f)
                    return;

                _latchedRecenterHeading = HeadingFor(_lastGroundedMoveDirection);
                _autoRecenterActive = true;
                _recenterConsumedForMovement = true;
                _recenterVelocity = 0f;
            }

            _orbit.HorizontalAxis.Value = Mathf.SmoothDampAngle(
                _orbit.HorizontalAxis.Value, _latchedRecenterHeading, ref _recenterVelocity,
                recenterSmoothTime, maximumRecenterSpeed, Time.unscaledDeltaTime);
            if (Mathf.Abs(Mathf.DeltaAngle(
                    _orbit.HorizontalAxis.Value, _latchedRecenterHeading)) < 0.25f)
            {
                _orbit.HorizontalAxis.Value = _latchedRecenterHeading;
                _autoRecenterActive = false;
                _recenterVelocity = 0f;
            }
        }

        private void UpdateSelectiveOcclusion()
        {
            if (_player == null || Camera.main == null || !IsOrbitLive())
            {
                RestoreOccluders();
                return;
            }

            Vector3 target = _player.position
                + (_orbit != null
                    ? _orbit.TargetOffset
                    : Vector3.up * TestCampusCameraGroundGuard.DefaultTargetYOffset);
            Vector3 direction = Camera.main.transform.position - target;
            float distance = direction.magnitude;
            _nextHidden.Clear();
            if (distance > 0.01f)
            {
                RaycastHit[] hits = Physics.SphereCastAll(
                    target, occlusionProbeRadius, direction / distance, distance,
                    ~0, QueryTriggerInteraction.Ignore);
                foreach (RaycastHit hit in hits)
                {
                    TestCampusSelectiveOccluder occluder =
                        hit.collider.GetComponentInParent<TestCampusSelectiveOccluder>();
                    if (occluder != null)
                        _nextHidden.Add(occluder);
                }
            }

            foreach (TestCampusSelectiveOccluder old in _hidden)
                if (old != null && !_nextHidden.Contains(old))
                    old.SetOccluded(false);
            foreach (TestCampusSelectiveOccluder current in _nextHidden)
                if (current != null)
                    current.SetOccluded(true);

            _hidden.Clear();
            foreach (TestCampusSelectiveOccluder item in _nextHidden)
                _hidden.Add(item);
        }

        private void RestoreOccluders()
        {
            foreach (TestCampusSelectiveOccluder item in _hidden)
                if (item != null)
                    item.SetOccluded(false);
            _hidden.Clear();
            _nextHidden.Clear();
        }

        private static float HeadingFor(Vector3 direction) =>
            Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
    }
}
