using System.Collections.Generic;
using CrazyMarket.Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrazyMarket.TestCampus
{
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TestCampusCameraInputFocus))]
    public sealed class TestCampusCameraPrototypeController : MonoBehaviour
    {
        [Header("Assisted orbit")]
        [SerializeField] private float recenterDelay = 2.5f;
        [SerializeField] private float recenterSmoothTime = 1.2f;
        [SerializeField] private float maximumRecenterSpeed = 65f;
        [SerializeField] private float minimumMovementSpeed = 0.6f;

        [Header("Selective obstruction")]
        [SerializeField] private float occlusionProbeRadius = 0.22f;

        [Header("Floor constraint")]
        [Tooltip("Turn off to compare against the unconstrained orbit, which drops the camera "
            + "through the floor at low pitch. Also disable the rig's ground guard for a true "
            + "unconstrained baseline.")]
        [SerializeField] private bool floorConstraintEnabled = true;

        [Tooltip("Smallest fraction of the authored orbit radius the camera may pull in to before "
            + "it starts riding the surface instead. 0.7368 = 7.0 m of the authored 9.5 m.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float minimumOrbitRadiusScale = 0.7368f;

        [Tooltip("Gap held between the camera sphere and the surface below it.")]
        [SerializeField] private float cameraGroundClearance = 0.1f;

        [Tooltip("Extra downward probe distance below the desired camera position.")]
        [SerializeField] private float groundProbeSlack = 0.5f;

        [Tooltip("How far ahead, in seconds of the current orbit rotation, the surface probe looks. "
            + "Ceilings are discrete slabs, so without lookahead the allowed height drops as a step "
            + "function the instant the camera crosses an overhang edge and the camera pops.")]
        [SerializeField] private float surfaceLookaheadSeconds = 0.35f;

        [SerializeField] private float radiusTightenSmoothTime = 0.05f;
        [SerializeField] private float radiusRelaxSmoothTime = 0.25f;
        [SerializeField] private float pitchLimitSmoothTime = 0.12f;
        [SerializeField] private LayerMask cameraGroundMask = ~0;

        private readonly HashSet<TestCampusSelectiveOccluder> _hidden = new();
        private readonly HashSet<TestCampusSelectiveOccluder> _nextHidden = new();
        private CinemachineCamera _assistedCamera;
        private CinemachineCamera _railCamera;
        private CinemachineOrbitalFollow _orbit;
        private CinemachineDecollider _decollider;
        private TestCampusCameraInputFocus _inputFocus;
        private Transform _player;
        private IPlayerSceneControl _playerController;
        private Transform _movementReference;
        private Vector3 _previousPlayerPosition;
        private Vector3 _lastGroundedMoveDirection = Vector3.forward;
        private float _lastManualInputTime = float.NegativeInfinity;
        private float _recenterVelocity;
        private float _latchedRecenterHeading;
        private Vector2 _lastLookDelta;
        private string _lookInputSource = "Waiting for look input";
        private bool _guidedZoneActive;
        private bool _autoRecenterActive;
        private bool _recenterConsumedForMovement;
        private float _groundRadiusScale = 1f;
        private float _groundRadiusVelocity;
        private float _smoothedPitchLimit;
        private float _pitchLimitVelocity;
        private bool _floorLimitActive;
        private float _smoothedCeilingLimit;
        private float _ceilingLimitVelocity;
        private bool _ceilingLimitActive;
        private float _previousYaw;
        private float _yawRate;
        private TestCampusCameraMode _mode = TestCampusCameraMode.AssistedOrbit;

        public static TestCampusCameraPrototypeController Instance { get; private set; }
        public TestCampusCameraMode Mode => _mode;
        public bool UiHasFocus => _inputFocus != null && _inputFocus.UiHasFocus;
        public bool IsRecentering => _autoRecenterActive;
        public Vector2 LastLookDelta => _lastLookDelta;
        public string LookInputSource => _lookInputSource;
        public float Yaw => _orbit != null ? _orbit.HorizontalAxis.Value : 0f;
        public float Pitch => _orbit != null ? _orbit.VerticalAxis.Value : 0f;
        public bool PointerLookActive => _inputFocus != null && _inputFocus.PointerLookActive;
        public float OrbitRadius => _orbit != null ? _orbit.Radius * _orbit.RadialAxis.Value : 0f;
        public bool FloorLimitActive => _floorLimitActive;
        public float FloorPitchLimit => _smoothedPitchLimit;
        public bool CeilingLimitActive => _ceilingLimitActive;
        public float CeilingPitchLimit => _smoothedCeilingLimit;
        public float CameraY => Camera.main != null ? Camera.main.transform.position.y : 0f;
        public string Status =>
            $"{_mode} | {FocusStatus} | "
            + $"Yaw {Yaw:0}° Pitch {Pitch:0}°"
            + $" | R {OrbitRadius:0.00}m CamY {CameraY:0.00}"
            + (_floorLimitActive ? $" | FLOOR {_smoothedPitchLimit:0.0}°" : "")
            + (_ceilingLimitActive ? $" | CEILING {_smoothedCeilingLimit:0.0}°" : "")
            + $" | {_lookInputSource}"
            + (_autoRecenterActive ? " | RECENTERING" : "");

        private string FocusStatus => _inputFocus != null
            ? _inputFocus.FocusStatus
            : "POINTER INPUT UNAVAILABLE";

        private void Awake()
        {
            Instance = this;
            _inputFocus = GetComponent<TestCampusCameraInputFocus>();
            _movementReference = new GameObject("Camera Movement Reference").transform;
            _movementReference.SetParent(transform, false);
        }

        private void Start()
        {
            RefreshRigs();
            if (_player != null)
            {
                _previousPlayerPosition = _player.position;
                _playerController?.SetMovementReference(_movementReference);
            }
            ApplyMode();
            SetUiFocus(false);
        }

        private void OnDestroy()
        {
            RestoreOccluders();
            if (Instance == this)
            {
                Instance = null;
            }
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

            ApplyGroundConstraint();
            UpdateMovementReference();
        }

        private void LateUpdate() => UpdateSelectiveOcclusion();

        public void SetMode(TestCampusCameraMode mode)
        {
            _mode = mode;
            _inputFocus?.ResetInput();
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
            _lookInputSource = hasFocus ? "UI focus" : "Waiting for look input";
            _inputFocus?.SetUiFocus(hasFocus);
            _playerController?.SetMovementEnabled(!hasFocus);
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
            _playerController = ResolvePlayerController(_player);

            foreach (TestCampusCameraRigTag tag in FindObjectsByType<TestCampusCameraRigTag>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                CinemachineCamera camera = tag.GetComponent<CinemachineCamera>();
                if (tag.Mode == TestCampusCameraMode.AssistedOrbit)
                {
                    _assistedCamera = camera;
                    _orbit = camera != null ? camera.GetComponent<CinemachineOrbitalFollow>() : null;
                    _decollider = camera != null ? camera.GetComponent<CinemachineDecollider>() : null;
                    EnsureRadialAxisRange();
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
                _playerController?.SetMovementReference(_movementReference);
            }
            ApplyMode();
        }

        private static IPlayerSceneControl ResolvePlayerController(Transform player)
        {
            if (player == null) return null;
            foreach (MonoBehaviour behaviour in player.GetComponents<MonoBehaviour>())
                if (behaviour is IPlayerSceneControl controller)
                    return controller;
            return null;
        }

        private void ApplyMode()
        {
            bool railLive = _mode == TestCampusCameraMode.GuidedRail
                || (_mode == TestCampusCameraMode.HybridZones && _guidedZoneActive);
            if (_assistedCamera != null)
                _assistedCamera.Priority.Value = railLive ? 10 : 30;
            if (_railCamera != null)
                _railCamera.Priority.Value = railLive ? 30 : 10;
        }

        private bool IsOrbitLive() =>
            _mode == TestCampusCameraMode.AssistedOrbit
            || (_mode == TestCampusCameraMode.HybridZones && !_guidedZoneActive);

        private void UpdateAssistedOrbit(bool grounded, float speed)
        {
            Vector2 input = _inputFocus != null
                ? _inputFocus.ConsumeLookInput()
                : Vector2.zero;
            _lookInputSource = _inputFocus != null
                ? _inputFocus.InputSource
                : "Pointer input unavailable";

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

        /// <summary>
        /// Widens the radial axis so the floor constraint can actually pull the camera in.
        /// The generated rig authors this range, but enforcing it here means the fix survives a
        /// regeneration or a hand-edited scene rather than silently clamping back to a fixed radius.
        /// </summary>
        private void EnsureRadialAxisRange()
        {
            if (_orbit == null)
                return;

            InputAxis radial = _orbit.RadialAxis;
            if (Mathf.Approximately(radial.Range.x, minimumOrbitRadiusScale)
                && Mathf.Approximately(radial.Range.y, 1f))
                return;

            radial.Range = new Vector2(minimumOrbitRadiusScale, 1f);
            radial.Center = 1f;
            radial.Value = Mathf.Clamp(radial.Value, minimumOrbitRadiusScale, 1f);
            _orbit.RadialAxis = radial;
        }

        /// <summary>
        /// Keeps the desired orbit position above the walkable surface beneath it, by pulling the
        /// camera in first and then limiting downward pitch so it rides the surface.
        /// </summary>
        /// <remarks>
        /// This drives Cinemachine's own radial and vertical axes rather than writing positions, so
        /// damping, decollision and composition all continue to work normally. It deliberately
        /// never touches the horizontal axis, which is what <see cref="UpdateMovementReference"/>
        /// derives the movement heading from — the fix therefore cannot re-enter the 180 degree
        /// movement/camera oscillation.
        /// </remarks>
        private void ApplyGroundConstraint()
        {
            if (!floorConstraintEnabled || _orbit == null || _player == null || !IsOrbitLive())
            {
                ReleaseGroundConstraint();
                return;
            }

            float baseRadius = _orbit.Radius;
            float minimumRadius = baseRadius * minimumOrbitRadiusScale;
            float cameraRadius = _decollider != null ? _decollider.CameraRadius : 0.35f;
            Vector3 target = _player.position + _orbit.TargetOffset;
            float yaw = _orbit.HorizontalAxis.Value;

            // Smoothed so a single noisy mouse frame cannot fling the lookahead probe far away.
            if (Time.unscaledDeltaTime > 0f)
            {
                float instantRate = Mathf.DeltaAngle(_previousYaw, yaw) / Time.unscaledDeltaTime;
                _yawRate = Mathf.Lerp(_yawRate, instantRate, 0.25f);
            }
            _previousYaw = yaw;

            float pitch = _orbit.VerticalAxis.Value;
            float radius = baseRadius;
            float pitchFloor = float.NegativeInfinity;
            float pitchCeiling = float.PositiveInfinity;

            // Two passes: the probe location depends on the pitch, and the pitch limit depends on
            // the probe. The coupling is weak because moving the camera barely shifts its
            // footprint, so a single refinement is enough to converge.
            for (int pass = 0; pass < 2; pass++)
            {
                Vector3 direction = Quaternion.Euler(pitch, yaw, 0f) * Vector3.back;
                Vector3 desired = target + direction * radius;

                // Evaluate at both the position the orbit wants and the position the camera is
                // actually at. Rig damping can put metres between the two while rotating, and the
                // clamp has to engage while the camera is physically against a surface, not only
                // when its desired position is. Without this the axis stays unclamped and the
                // guard has to make the whole correction in a single frame, which pops.
                Vector3 actual = Camera.main != null ? Camera.main.transform.position : desired;

                // ...and at where the orbit is heading, so the camera starts ducking before it
                // reaches an overhang edge rather than on the frame it arrives under one.
                Vector3 predicted = target
                    + Quaternion.Euler(pitch, yaw + _yawRate * surfaceLookaheadSeconds, 0f)
                    * Vector3.back * radius;

                // The camera is either above or below the target, never both, so at most one of
                // the two surface constraints can bind on a given pass.
                bool below = direction.y < -0.0001f;
                bool above = direction.y > 0.0001f;

                float limitY = 0f;
                bool constrained = false;
                if (below)
                {
                    limitY = float.NegativeInfinity;
                    constrained = MostRestrictiveFloor(target, desired, cameraRadius, ref limitY)
                        | MostRestrictiveFloor(target, actual, cameraRadius, ref limitY)
                        | MostRestrictiveFloor(target, predicted, cameraRadius, ref limitY);
                    if (constrained && desired.y >= limitY)
                        break;
                }
                else if (above)
                {
                    limitY = float.PositiveInfinity;
                    constrained = MostRestrictiveCeiling(target, desired, cameraRadius, ref limitY)
                        | MostRestrictiveCeiling(target, actual, cameraRadius, ref limitY)
                        | MostRestrictiveCeiling(target, predicted, cameraRadius, ref limitY);
                    if (constrained && desired.y <= limitY)
                        break;
                }

                if (!constrained)
                {
                    // Level with the target, or no surface on that side: over a genuine drop or
                    // out in the open the camera is left free.
                    radius = baseRadius;
                    pitchFloor = float.NegativeInfinity;
                    pitchCeiling = float.PositiveInfinity;
                    break;
                }

                float requiredRadius = (limitY - target.y) / direction.y;
                if (requiredRadius >= minimumRadius)
                {
                    // Pulling in is enough on its own.
                    radius = Mathf.Min(requiredRadius, baseRadius);
                    break;
                }

                // Fully pulled in and still through the surface, so start riding it.
                radius = minimumRadius;
                pitch = Mathf.Asin(
                    Mathf.Clamp((limitY - target.y) / minimumRadius, -1f, 1f)) * Mathf.Rad2Deg;
                if (below)
                    pitchFloor = pitch;
                else
                    pitchCeiling = pitch;
            }

            ApplyRadiusScale(radius / baseRadius);
            ApplyPitchLimits(pitchFloor, pitchCeiling);
        }

        /// <summary>Raises <paramref name="limitY"/> to the floor under <paramref name="point"/>.</summary>
        private bool MostRestrictiveFloor(
            Vector3 target, Vector3 point, float cameraRadius, ref float limitY)
        {
            if (!TestCampusCameraGround.TryGetMinimumCameraY(
                    target, point, cameraRadius, cameraGroundClearance, groundProbeSlack,
                    cameraGroundMask, out float candidate))
                return false;

            limitY = Mathf.Max(limitY, candidate);
            return true;
        }

        /// <summary>Lowers <paramref name="limitY"/> to the ceiling over <paramref name="point"/>.</summary>
        private bool MostRestrictiveCeiling(
            Vector3 target, Vector3 point, float cameraRadius, ref float limitY)
        {
            if (!TestCampusCameraGround.TryGetMaximumCameraY(
                    target, point, cameraRadius, cameraGroundClearance, groundProbeSlack,
                    cameraGroundMask, out float candidate))
                return false;

            limitY = Mathf.Min(limitY, candidate);
            return true;
        }

        private void ApplyRadiusScale(float targetScale)
        {
            // Tighten quickly, relax gently, mirroring the Decollider's own asymmetric damping.
            float smoothTime = targetScale < _groundRadiusScale
                ? radiusTightenSmoothTime
                : radiusRelaxSmoothTime;
            _groundRadiusScale = Mathf.SmoothDamp(
                _groundRadiusScale, targetScale, ref _groundRadiusVelocity, smoothTime,
                Mathf.Infinity, Time.unscaledDeltaTime);
            _orbit.RadialAxis.Value = _orbit.RadialAxis.ClampValue(_groundRadiusScale);
        }

        private void ApplyPitchLimits(float pitchFloor, float pitchCeiling)
        {
            ApplySurfaceLimit(
                pitchFloor, false, ref _smoothedPitchLimit, ref _pitchLimitVelocity,
                ref _floorLimitActive);
            ApplySurfaceLimit(
                pitchCeiling, true, ref _smoothedCeilingLimit, ref _ceilingLimitVelocity,
                ref _ceilingLimitActive);
        }

        /// <summary>
        /// Eases one side of the pitch clamp toward its target and applies it.
        /// </summary>
        /// <remarks>
        /// Both engaging and releasing are ramped. Engaging starts from wherever the axis already
        /// is, so a surface arriving suddenly under a running player eases the camera across.
        /// Releasing ramps the clamp out to the authored bound rather than dropping it, because
        /// ceilings are discrete slabs — rotating out past a slab edge would otherwise remove the
        /// clamp in a single frame and pop the camera several metres.
        /// </remarks>
        private void ApplySurfaceLimit(
            float limit, bool isCeiling, ref float smoothed, ref float velocity, ref bool active)
        {
            bool constrained = isCeiling
                ? !float.IsPositiveInfinity(limit)
                : !float.IsNegativeInfinity(limit);

            if (!constrained)
            {
                if (!active)
                    return;
                limit = isCeiling ? _orbit.VerticalAxis.Range.y : _orbit.VerticalAxis.Range.x;
            }
            else if (!active)
            {
                smoothed = _orbit.VerticalAxis.Value;
                velocity = 0f;
                active = true;
            }

            smoothed = Mathf.SmoothDamp(
                smoothed, limit, ref velocity, pitchLimitSmoothTime,
                Mathf.Infinity, Time.unscaledDeltaTime);

            float axis = _orbit.VerticalAxis.Value;
            if (!constrained && (isCeiling ? smoothed >= axis : smoothed <= axis))
            {
                // The ramp has passed the axis, so the clamp no longer binds.
                active = false;
                velocity = 0f;
                return;
            }

            if (isCeiling ? axis > smoothed : axis < smoothed)
                _orbit.VerticalAxis.Value = _orbit.VerticalAxis.ClampValue(smoothed);
        }

        private void ReleaseGroundConstraint()
        {
            _floorLimitActive = false;
            _pitchLimitVelocity = 0f;
            _ceilingLimitActive = false;
            _ceilingLimitVelocity = 0f;
            _groundRadiusScale = 1f;
            _groundRadiusVelocity = 0f;
            if (_orbit != null)
                _orbit.RadialAxis.Value = _orbit.RadialAxis.ClampValue(1f);
        }

        private void UpdateMovementReference()
        {
            if (_movementReference == null)
                return;
            if (IsOrbitLive() && _orbit != null)
                _movementReference.rotation = Quaternion.Euler(0f, _orbit.HorizontalAxis.Value, 0f);
            else if (Camera.main != null)
            {
                Vector3 forward = Camera.main.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.001f)
                    _movementReference.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }
        }

        private void UpdateSelectiveOcclusion()
        {
            if (_player == null || Camera.main == null || !IsOrbitLive())
            {
                RestoreOccluders();
                return;
            }

            Vector3 target = _player.position + Vector3.up * 1.2f;
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
