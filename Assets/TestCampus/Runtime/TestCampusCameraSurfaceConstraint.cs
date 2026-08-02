using Unity.Cinemachine;
using UnityEngine;

namespace CrazyMarket.TestCampus
{
    /// <summary>Owns floor/ceiling probing and the assisted rig's radial/pitch correction.</summary>
    [DefaultExecutionOrder(10005)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineCamera), typeof(CinemachineOrbitalFollow))]
    public sealed class TestCampusCameraSurfaceConstraint : MonoBehaviour
    {
        [SerializeField] private bool constraintEnabled = true;
        [SerializeField] private float surfaceLookaheadSeconds = 0.35f;
        [SerializeField] private float radiusTightenSmoothTime = 0.05f;
        [SerializeField] private float radiusRelaxSmoothTime = 0.25f;
        [SerializeField] private float pitchLimitSmoothTime = 0.12f;
        [SerializeField] private LayerMask groundMask = ~0;

        private CinemachineCamera _camera;
        private CinemachineOrbitalFollow _orbit;
        private CinemachineDecollider _decollider;
        private TestCampusCameraGroundGuard _groundGuard;
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
        private bool _orbitLive = true;

        public float OrbitRadius => _orbit != null ? _orbit.Radius * _orbit.RadialAxis.Value : 0f;
        public bool FloorLimitActive => _floorLimitActive;
        public float FloorPitchLimit => _smoothedPitchLimit;
        public bool CeilingLimitActive => _ceilingLimitActive;
        public float CeilingPitchLimit => _smoothedCeilingLimit;

        private void Awake() => CacheRig();

        private void OnEnable()
        {
            CacheRig();
            EnsureRadialAxisRange();
        }

        private void Update() => ApplyConstraint(_orbitLive);

        private void OnDisable() => ReleaseConstraint();

        public void ResetConstraint(float radialScale)
        {
            ReleaseConstraint();
            _groundRadiusScale = radialScale;
            if (_orbit != null)
                _orbit.RadialAxis.Value = _orbit.RadialAxis.ClampValue(radialScale);
        }

        public void SetOrbitLive(bool orbitLive)
        {
            _orbitLive = orbitLive;
            if (!orbitLive)
                ReleaseConstraint();
        }

        private void CacheRig()
        {
            _camera = GetComponent<CinemachineCamera>();
            _orbit = GetComponent<CinemachineOrbitalFollow>();
            _decollider = GetComponent<CinemachineDecollider>();
            _groundGuard = GetComponent<TestCampusCameraGroundGuard>();
        }

        private void EnsureRadialAxisRange()
        {
            if (_orbit == null)
                return;

            InputAxis radial = _orbit.RadialAxis;
            float minimumScale = radial.Range.x;
            if (minimumScale >= 0.1f && minimumScale < 1f
                && Mathf.Approximately(radial.Range.y, 1f))
                return;

            minimumScale = TestCampusCameraGroundGuard.DefaultMinimumOrbitRadiusScale;
            radial.Range = new Vector2(minimumScale, 1f);
            radial.Center = 1f;
            radial.Value = Mathf.Clamp(radial.Value, minimumScale, 1f);
            _orbit.RadialAxis = radial;
        }

        private void ApplyConstraint(bool orbitLive)
        {
            Transform player = _camera != null ? _camera.Follow : null;
            if (!constraintEnabled || _orbit == null || player == null || !orbitLive)
            {
                ReleaseConstraint();
                return;
            }

            float baseRadius = _orbit.Radius;
            float minimumRadius = baseRadius * _orbit.RadialAxis.Range.x;
            float cameraRadius = _decollider != null
                ? _decollider.CameraRadius
                : TestCampusCameraGroundGuard.DefaultCameraRadius;
            Vector3 target = player.position + _orbit.TargetOffset;
            float yaw = _orbit.HorizontalAxis.Value;

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
            for (int pass = 0; pass < 2; pass++)
            {
                Vector3 direction = Quaternion.Euler(pitch, yaw, 0f) * Vector3.back;
                Vector3 desired = target + direction * radius;
                Vector3 actual = Camera.main != null ? Camera.main.transform.position : desired;
                Vector3 predicted = target
                    + Quaternion.Euler(pitch, yaw + _yawRate * surfaceLookaheadSeconds, 0f)
                    * Vector3.back * radius;
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
                    radius = baseRadius;
                    pitchFloor = float.NegativeInfinity;
                    pitchCeiling = float.PositiveInfinity;
                    break;
                }

                float requiredRadius = (limitY - target.y) / direction.y;
                if (requiredRadius >= minimumRadius)
                {
                    radius = Mathf.Min(requiredRadius, baseRadius);
                    break;
                }

                radius = minimumRadius;
                pitch = Mathf.Asin(
                    Mathf.Clamp((limitY - target.y) / minimumRadius, -1f, 1f)) * Mathf.Rad2Deg;
                if (below) pitchFloor = pitch;
                else pitchCeiling = pitch;
            }

            ApplyRadiusScale(radius / baseRadius);
            ApplyPitchLimits(pitchFloor, pitchCeiling);
        }

        private bool MostRestrictiveFloor(
            Vector3 target, Vector3 point, float cameraRadius, ref float limitY)
        {
            if (!TestCampusCameraSurfaceProbe.TryGetMinimumCameraY(
                    target, point, cameraRadius,
                    _groundGuard != null ? _groundGuard.GroundClearance : TestCampusCameraGroundGuard.DefaultClearance,
                    _groundGuard != null ? _groundGuard.GroundProbeSlack : TestCampusCameraGroundGuard.DefaultProbeSlack,
                    groundMask, out float candidate))
                return false;
            limitY = Mathf.Max(limitY, candidate);
            return true;
        }

        private bool MostRestrictiveCeiling(
            Vector3 target, Vector3 point, float cameraRadius, ref float limitY)
        {
            if (!TestCampusCameraSurfaceProbe.TryGetMaximumCameraY(
                    target, point, cameraRadius,
                    _groundGuard != null ? _groundGuard.GroundClearance : TestCampusCameraGroundGuard.DefaultClearance,
                    _groundGuard != null ? _groundGuard.GroundProbeSlack : TestCampusCameraGroundGuard.DefaultProbeSlack,
                    groundMask, out float candidate))
                return false;
            limitY = Mathf.Min(limitY, candidate);
            return true;
        }

        private void ApplyRadiusScale(float targetScale)
        {
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

        private void ApplySurfaceLimit(
            float limit, bool isCeiling, ref float smoothed, ref float velocity, ref bool active)
        {
            bool constrained = isCeiling
                ? !float.IsPositiveInfinity(limit)
                : !float.IsNegativeInfinity(limit);
            if (!constrained)
            {
                if (!active) return;
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
                active = false;
                velocity = 0f;
                return;
            }
            if (isCeiling ? axis > smoothed : axis < smoothed)
                _orbit.VerticalAxis.Value = _orbit.VerticalAxis.ClampValue(smoothed);
        }

        private void ReleaseConstraint()
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
    }
}
