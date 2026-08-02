using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrazyMarket.TestCampus
{
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TestCampusCameraInputFocus))]
    [RequireComponent(typeof(TestCampusCameraOcclusionController))]
    public sealed class TestCampusCameraPrototypeController : MonoBehaviour, ITestResettable
    {
        [Header("Assisted orbit")]
        [SerializeField] private float recenterDelay = 2.5f;
        [SerializeField] private float recenterSmoothTime = 1.2f;
        [SerializeField] private float maximumRecenterSpeed = 65f;
        [SerializeField] private float minimumMovementSpeed = 0.6f;

        private CinemachineCamera _assistedCamera;
        private readonly HashSet<TestCampusCameraModeZone> _guidedZones = new();
        private CinemachineCamera _railCamera;
        private CinemachineOrbitalFollow _orbit;
        private TestCampusCameraSurfaceConstraint _surfaceConstraint;
        private TestCampusCameraInputFocus _inputFocus;
        private TestCampusCameraOcclusionController _occlusion;
        private Transform _player;
        private TestCampusPlayerAdapter _playerAdapter;
        private Vector3 _previousPlayerPosition;
        private Vector3 _lastGroundedMoveDirection = Vector3.forward;
        private float _lastManualInputTime = float.NegativeInfinity;
        private float _recenterVelocity;
        private float _latchedRecenterHeading;
        private Vector2 _lastLookDelta;
        private bool _guidedZoneActive;
        private bool _autoRecenterActive;
        private bool _recenterConsumedForMovement;
        private TestCampusCameraMode _mode = TestCampusCameraMode.AssistedOrbit;
        private float _initialYaw;
        private float _initialPitch;
        private float _initialRadiusScale = 1f;

        public static TestCampusCameraPrototypeController Instance { get; private set; }
        public TestCampusCameraMode Mode => _mode;
        public bool UiHasFocus => _inputFocus != null && _inputFocus.UiHasFocus;
        public bool IsRecentering => _autoRecenterActive;
        public Vector2 LastLookDelta => _lastLookDelta;
        public string LookInputSource => _inputFocus != null
            ? _inputFocus.InputSource
            : "Waiting for look input";
        public float Yaw => _orbit != null ? _orbit.HorizontalAxis.Value : 0f;
        public float Pitch => _orbit != null ? _orbit.VerticalAxis.Value : 0f;
        public bool PointerLookActive => _inputFocus != null && _inputFocus.PointerLookActive;
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
            + $" | {LookInputSource}"
            + (_autoRecenterActive ? " | RECENTERING" : "");

        private string FocusStatus => _inputFocus != null
            ? _inputFocus.FocusStatus
            : "POINTER VISIBLE";

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            TestCampusPlayerAdapter.PlayerWarped += OnPlayerWarped;
        }

        private void OnDisable()
        {
            TestCampusPlayerAdapter.PlayerWarped -= OnPlayerWarped;
        }

        private void Start()
        {
            RefreshRigs();
            if (_player != null)
                _previousPlayerPosition = _player.position;
            ApplyMode();
            SetUiFocus(false);
            CaptureInitialState();
            TestCampusController.Instance?.RegisterZoneResettable(TestZoneId.Camera, this);
        }

        private void OnDestroy()
        {
            TestCampusController.Instance?.UnregisterZoneResettable(TestZoneId.Camera, this);
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (_player == null || _assistedCamera == null || _orbit == null || _railCamera == null)
                RefreshRigs();

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
                _inputFocus?.ResetInput();

        }

        private void LateUpdate()
        {
            Vector3 targetOffset = _orbit != null
                ? _orbit.TargetOffset
                : Vector3.up * TestCampusCameraGroundGuard.DefaultTargetYOffset;
            _occlusion?.UpdateOcclusion(_player, targetOffset, IsOrbitLive());
        }

        public void SetMode(TestCampusCameraMode mode)
        {
            _mode = mode;
            _inputFocus?.ResetInput();
            ApplyMode();
        }

        public void SetGuidedZoneActive(TestCampusCameraModeZone zone, bool active)
        {
            if (zone == null)
                return;
            if (active)
                _guidedZones.Add(zone);
            else
                _guidedZones.Remove(zone);
            _guidedZoneActive = _guidedZones.Count > 0;
            if (_mode == TestCampusCameraMode.HybridZones)
                ApplyMode();
        }

        public void SetUiFocus(bool hasFocus)
        {
            _inputFocus?.SetUiFocus(hasFocus);
        }

        public void CaptureInitialState()
        {
            _initialYaw = _orbit != null ? _orbit.HorizontalAxis.Value : 0f;
            _initialPitch = _orbit != null ? _orbit.VerticalAxis.Value : 0f;
            _initialRadiusScale = _orbit != null ? _orbit.RadialAxis.Value : 1f;
        }

        public void ResetToInitialState()
        {
            _inputFocus?.ResetInput();
            _lastLookDelta = Vector2.zero;
            _lastManualInputTime = float.NegativeInfinity;
            _recenterVelocity = 0f;
            _autoRecenterActive = false;
            _recenterConsumedForMovement = false;
            _occlusion?.RestoreOccluders();
            if (_orbit != null)
            {
                _orbit.HorizontalAxis.Value = _orbit.HorizontalAxis.ClampValue(_initialYaw);
                _orbit.VerticalAxis.Value = _orbit.VerticalAxis.ClampValue(_initialPitch);
                _orbit.RadialAxis.Value = _orbit.RadialAxis.ClampValue(_initialRadiusScale);
            }
            _surfaceConstraint?.ResetConstraint(_initialRadiusScale);
            SetMode(TestCampusCameraMode.AssistedOrbit);
        }

        private void OnPlayerWarped(Transform player, Vector3 positionDelta)
        {
            if (_player == null || player != _player)
                return;
            _assistedCamera?.OnTargetObjectWarped(_player, positionDelta);
            _railCamera?.OnTargetObjectWarped(_player, positionDelta);
            _previousPlayerPosition = _player.position;
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
            _inputFocus ??= GetComponent<TestCampusCameraInputFocus>();
            _occlusion ??= GetComponent<TestCampusCameraOcclusionController>();
            _inputFocus?.SetPlayerAdapter(_playerAdapter);

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
            Vector2 input = _inputFocus != null
                ? _inputFocus.ConsumeLookInput()
                : Vector2.zero;

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

        private static float HeadingFor(Vector3 direction) =>
            Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
    }
}
