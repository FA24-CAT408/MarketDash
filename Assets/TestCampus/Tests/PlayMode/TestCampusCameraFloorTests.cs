using System.Collections;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CrazyMarket.TestCampus.Tests
{
    /// <summary>
    /// Play Mode coverage for the camera floor constraint. Each case drives the vertical orbit axis
    /// down to its authored -20 degree bound, exactly as a held mouse-up does, then measures where
    /// the rendered camera actually ends up relative to the surface beneath it.
    /// </summary>
    public sealed class TestCampusCameraFloorTests
    {
        private const string CoreScene = "Assets/TestCampus/Scenes/TestCampus_Core.unity";
        private const float SettleFrames = 45f;

        private TestCampusCameraPrototypeController _controller;
        private CinemachineOrbitalFollow _orbit;
        private CinemachineDecollider _decollider;
        private TestCampusCameraGroundGuard _guard;
        private TestCampusPlayerAdapter _player;
        private PitchDriver _driver;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return LoadSceneAndWait();

            _controller = Object.FindFirstObjectByType<TestCampusCameraPrototypeController>();
            Assert.IsNotNull(_controller, "Prototype camera controller missing from the campus.");

            GameObject rig = GameObject.Find("CM Test Campus Player Camera");
            Assert.IsNotNull(rig, "Assisted orbit rig missing from the campus.");
            _orbit = rig.GetComponent<CinemachineOrbitalFollow>();
            _decollider = rig.GetComponent<CinemachineDecollider>();
            _guard = rig.GetComponent<TestCampusCameraGroundGuard>();
            Assert.IsNotNull(_guard, "Ground guard extension is not on the rig.");

            _player = Object.FindFirstObjectByType<TestCampusPlayerAdapter>();
            Assert.IsNotNull(_player, "Player adapter missing from the campus.");

            _driver = new GameObject("Pitch Driver").AddComponent<PitchDriver>();
            _driver.Orbit = _orbit;
        }

        [TearDown]
        public void TearDown()
        {
            if (_driver != null)
                Object.Destroy(_driver.gameObject);
        }

        private static IEnumerator LoadSceneAndWait()
        {
            if (SceneManager.GetActiveScene().path != CoreScene)
            {
                yield return SceneManager.LoadSceneAsync(CoreScene, LoadSceneMode.Single);
            }

            // The campus loads its specialist zones additively on start.
            for (int i = 0; i < 240; i++)
                yield return null;
        }

        [UnityTest]
        public IEnumerator FlatHubFloor_CameraStaysAboveSurface()
        {
            yield return Measure("Flat hub floor", new Vector3(0f, 1f, 0f), 0f);
        }

        /// <summary>
        /// Reproduces the original defect with both constraint layers switched off, so the passing
        /// cases above are demonstrably measuring the fix rather than restating scene geometry.
        /// </summary>
        [UnityTest]
        public IEnumerator Baseline_WithoutConstraint_CameraDropsThroughFloor()
        {
            (string Label, Vector3 Position, float Yaw)[] cases =
            {
                ("hub centre", new Vector3(0f, 1f, 0f), 0f),
                ("beside south wall", new Vector3(10f, 1f, -18f), 0f),
                ("hub corner", new Vector3(17f, 1f, -17f), 45f),
                ("doorway facing north", new Vector3(0f, 1f, 18f), 180f),
                ("movement gym floor", new Vector3(-75f, 1f, 10f), 0f),
                ("camera course floor", new Vector3(75f, 1f, 20f), 0f),
            };

            SetConstraintEnabled(false);
            _guard.enabled = false;
            float worst = float.MaxValue;
            string worstLabel = "none";
            try
            {
                foreach ((string label, Vector3 position, float yaw) in cases)
                {
                    yield return Teleport(position);
                    _driver.Yaw = yaw;
                    yield return DriveTo(-20f);

                    Vector3 camera = Camera.main.transform.position;
                    Vector3 target = _player.transform.position + _orbit.TargetOffset;
                    float distance = Vector3.Distance(camera, target);
                    Debug.Log($"[FLOOR] BASELINE {label} | pitch {_orbit.VerticalAxis.Value:0.00} deg "
                        + $"| camera Y {camera.y:0.00} | distance to target {distance:0.00} m");

                    if (label == "hub corner")
                        yield return Capture("before-hub-corner");

                    if (camera.y < worst)
                    {
                        worst = camera.y;
                        worstLabel = label;
                    }
                }
            }
            finally
            {
                SetConstraintEnabled(true);
                _guard.enabled = true;
            }

            Debug.Log($"[FLOOR] BASELINE worst camera Y {worst:0.00} at {worstLabel}");
            Assert.Less(worst, 0f,
                "Baseline did not reproduce the defect at any probed position.");
        }

        /// <summary>
        /// Measures how much downward pitch each radius floor actually buys on flat ground, so the
        /// shipped value is chosen from data rather than from arithmetic.
        /// </summary>
        [UnityTest]
        public IEnumerator RadiusFloorTradeoff_IsMeasured()
        {
            float original = GetRadiusScale();
            try
            {
                foreach (float scale in new[] { 1f, 0.7368f, 0.5f, 0.25f })
                {
                    SetRadiusScale(scale);
                    yield return Teleport(new Vector3(0f, 1f, 0f));
                    _driver.Yaw = 0f;
                    yield return DriveTo(-20f);

                    Debug.Log($"[TRADEOFF] radius floor {_orbit.Radius * scale:0.0} m "
                        + $"-> lowest pitch {_orbit.VerticalAxis.Value:0.00} deg "
                        + $"| radius {_controller.OrbitRadius:0.00} m "
                        + $"| camera Y {Camera.main.transform.position.y:0.00}");
                }
            }
            finally
            {
                SetRadiusScale(original);
            }
        }

        private static System.Reflection.FieldInfo RadiusScaleField =>
            typeof(TestCampusCameraPrototypeController).GetField("minimumOrbitRadiusScale",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        private float GetRadiusScale() => (float)RadiusScaleField.GetValue(_controller);

        private void SetRadiusScale(float scale)
        {
            RadiusScaleField.SetValue(_controller, scale);
            InputAxis radial = _orbit.RadialAxis;
            radial.Range = new Vector2(scale, 1f);
            _orbit.RadialAxis = radial;
        }

        [UnityTest]
        public IEnumerator UnderLowCeiling_CameraStaysBelowIt()
        {
            yield return MeasureCeiling("Movement low ceiling", new Vector3(-62f, 1f, -2f), 0f);
        }

        [UnityTest]
        public IEnumerator OnColumnTop_CameraStaysBelowInteriorCeiling()
        {
            yield return MeasureCeiling("Hub column top", new Vector3(-19f, 6.4f, 0f), 0f);
        }

        [UnityTest]
        public IEnumerator OnBayWallTop_CameraStaysBelowInteriorCeiling()
        {
            yield return MeasureCeiling("Lighting bay wall top", new Vector3(-20f, 6.4f, 78f), 0f);
        }

        [UnityTest]
        public IEnumerator OnJumpTarget_CameraStaysBelowInteriorCeiling()
        {
            yield return MeasureCeiling("Movement jump target", new Vector3(-64f, 3.4f, 43f), 0f);
        }

        [UnityTest]
        public IEnumerator RotatingUnderLowCeiling_StaysBelowItAndDoesNotJitter()
        {
            yield return Teleport(new Vector3(-62f, 1f, -2f));
            _driver.Yaw = 0f;
            yield return DriveTo(55f);

            float previousY = Camera.main.transform.position.y;
            float worstBreach = 0f;
            float largestStep = 0f;
            string worstFrame = "none";

            // A fast but plausible mouse flick, in degrees per second rather than per frame.
            const float yawRate = 180f;
            float swept = 0f;
            while (swept < 360f)
            {
                float beforePitch = _orbit.VerticalAxis.Value;
                float beforeRadius = _controller.OrbitRadius;
                bool beforeActive = _controller.CeilingLimitActive;

                float step = yawRate * Time.unscaledDeltaTime;
                _driver.Yaw += step;
                swept += step;
                yield return null;

                float y = Camera.main.transform.position.y;
                float delta = Mathf.Abs(y - previousY);
                if (delta > largestStep)
                {
                    largestStep = delta;
                    worstFrame =
                        $"yaw {_driver.Yaw:0.0} | Y {previousY:0.00}->{y:0.00} "
                        + $"| pitch {beforePitch:0.00}->{_orbit.VerticalAxis.Value:0.00} "
                        + $"| radius {beforeRadius:0.00}->{_controller.OrbitRadius:0.00} "
                        + $"| ceilingActive {beforeActive}->{_controller.CeilingLimitActive} "
                        + $"| limit {_controller.CeilingPitchLimit:0.00} "
                        + $"| guard {_guard.LastLift:0.000}";
                }
                float ceiling = CeilingUndersideAbove(Camera.main.transform.position);
                if (!float.IsPositiveInfinity(ceiling))
                    worstBreach = Mathf.Max(worstBreach, y - ceiling);
                previousY = y;
            }

            Debug.Log($"[CEILING] Rotate under low ceiling | worst breach {worstBreach:0.000} m "
                + $"| largest per-frame Y step {largestStep:0.000} m");
            Debug.Log($"[CEILING] worst step frame: {worstFrame}");
            Assert.Less(worstBreach, 0.05f,
                "Camera rose through the ceiling while rotating beneath it.");
            Assert.Less(largestStep, 0.35f,
                "Camera Y jumped between frames while rotating beneath the ceiling.");
        }

        [UnityTest]
        public IEnumerator LeavingLowCeiling_PitchRecoversSmoothly()
        {
            yield return Teleport(new Vector3(-62f, 1f, -2f));
            _driver.Yaw = 0f;
            yield return DriveTo(55f);

            float pinnedPitch = _orbit.VerticalAxis.Value;

            // Walk out from under the slab; the ceiling limit should release, not snap.
            float previousPitch = pinnedPitch;
            float largestStep = 0f;
            float walked = 0f;
            while (walked < 14f)
            {
                walked += Time.unscaledDeltaTime * 4f;
                _player.TeleportTo(new Vector3(-62f, 1f, -2f + walked), Quaternion.identity);
                yield return null;
                largestStep = Mathf.Max(largestStep,
                    Mathf.Abs(_orbit.VerticalAxis.Value - previousPitch));
                previousPitch = _orbit.VerticalAxis.Value;
            }

            Debug.Log($"[CEILING] Leaving low ceiling | pinned {pinnedPitch:0.00} deg -> "
                + $"{_orbit.VerticalAxis.Value:0.00} deg "
                + $"| largest per-frame pitch step {largestStep:0.000} deg");
            Assert.Greater(_orbit.VerticalAxis.Value, pinnedPitch + 5f,
                "Pitch did not recover after leaving the low ceiling.");
            Assert.Less(largestStep, 3f, "Pitch snapped rather than easing when the ceiling released.");
        }

        private IEnumerator MeasureCeiling(string label, Vector3 playerPosition, float yaw)
        {
            yield return Teleport(playerPosition);
            _driver.Yaw = yaw;
            yield return DriveTo(55f);

            Vector3 camera = Camera.main.transform.position;
            float ceiling = CeilingUndersideAbove(camera);
            float breach = float.IsPositiveInfinity(ceiling) ? float.NegativeInfinity : camera.y - ceiling;

            Debug.Log($"[CEILING] {label} | pitch {_orbit.VerticalAxis.Value:0.00} deg "
                + $"| radius {_controller.OrbitRadius:0.00} m | camera Y {camera.y:0.00} "
                + $"| ceiling underside {(float.IsPositiveInfinity(ceiling) ? "none" : ceiling.ToString("0.00"))} "
                + $"| breach {(float.IsNegativeInfinity(breach) ? "n/a" : breach.ToString("0.000"))} m "
                + $"| limit {(_controller.CeilingLimitActive ? _controller.CeilingPitchLimit.ToString("0.00") + " deg" : "inactive")} "
                + $"| guard correction {_guard.LastLift:0.000} m");

            Assert.IsFalse(float.IsPositiveInfinity(ceiling),
                $"{label}: no ceiling was found above the camera, so this case proves nothing.");
            Assert.Less(breach, 0.05f,
                $"{label}: camera rendered above the ceiling (camera Y {camera.y:0.00}, "
                + $"underside {ceiling:0.00}).");
            yield return null;
        }

        /// <summary>
        /// Probes for a defect symmetric to the floor case: pitching down drives the vertical axis
        /// to +55, which raises the camera and can push it up through a ceiling slab into the open
        /// air above it, where the Decollider again finds no penetration to resolve.
        /// </summary>
        [UnityTest]
        public IEnumerator Baseline_WithoutConstraint_CameraRisesThroughCeiling()
        {
            (string Label, Vector3 Position, float Yaw)[] cases =
            {
                ("hub floor", new Vector3(0f, 1f, 0f), 0f),
                ("hub on column top", new Vector3(-19f, 6.4f, 0f), 0f),
                ("movement under low ceiling", new Vector3(-62f, 1f, -2f), 0f),
                ("movement top step", new Vector3(-60f, 2.6f, 4f), 0f),
                ("movement jump target", new Vector3(-64f, 3.4f, 43f), 0f),
                ("lighting bay wall top", new Vector3(-20f, 6.4f, 78f), 0f),
                ("camera height target top", new Vector3(88f, 10.4f, 42f), 0f),
            };

            SetConstraintEnabled(false);
            _guard.enabled = false;
            float worstBreach = 0f;
            string worstLabel = "none";
            try
            {
                foreach ((string label, Vector3 position, float yaw) in cases)
                {
                    yield return Teleport(position);
                    _driver.Yaw = yaw;
                    yield return DriveTo(55f);

                    Vector3 camera = Camera.main.transform.position;
                    float ceiling = CeilingUndersideAbove(camera);
                    float breach = float.IsPositiveInfinity(ceiling) ? 0f : camera.y - ceiling;

                    Debug.Log($"[CEILING] BASELINE {label} | pitch {_orbit.VerticalAxis.Value:0.00} deg "
                        + $"| camera Y {camera.y:0.00} "
                        + $"| ceiling underside {(float.IsPositiveInfinity(ceiling) ? "none" : ceiling.ToString("0.00"))} "
                        + $"| breach {breach:0.00} m");

                    if (breach > worstBreach)
                    {
                        worstBreach = breach;
                        worstLabel = label;
                    }
                }
            }
            finally
            {
                _driver.Target = -20f;
                SetConstraintEnabled(true);
                _guard.enabled = true;
            }

            Debug.Log($"[CEILING] BASELINE worst breach {worstBreach:0.00} m at {worstLabel}");
            Assert.Greater(worstBreach, 0f,
                "Baseline did not reproduce a ceiling breach at any probed position.");
        }

        /// <summary>
        /// Lowest downward-facing surface above the camera, found by sweeping up from the orbit
        /// target's height so a floor can never be mistaken for a ceiling. Measurement only.
        /// </summary>
        private float CeilingUndersideAbove(Vector3 camera)
        {
            Vector3 target = _player.transform.position + _orbit.TargetOffset;
            if (camera.y <= target.y)
                return float.PositiveInfinity;

            float radius = _decollider != null ? _decollider.CameraRadius : 0.35f;
            Vector3 origin = new(camera.x, target.y, camera.z);
            RaycastHit[] hits = Physics.SphereCastAll(
                origin, radius, Vector3.up, camera.y - target.y + 0.5f, ~0,
                QueryTriggerInteraction.Ignore);

            float best = float.PositiveInfinity;
            foreach (RaycastHit hit in hits)
            {
                if (hit.distance <= 0f || hit.normal.y > -0.5f)
                    continue;
                if (hit.collider.GetComponentInParent<TestCampusPlayerAdapter>() != null)
                    continue;
                best = Mathf.Min(best, hit.point.y);
            }
            return best;
        }

        private void SetConstraintEnabled(bool enabled)
        {
            typeof(TestCampusCameraPrototypeController)
                .GetField("floorConstraintEnabled",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(_controller, enabled);
        }

        [UnityTest]
        public IEnumerator BesideSouthWall_CameraStaysAboveSurface()
        {
            yield return Measure("Beside south wall", new Vector3(10f, 1f, -18f), 0f);
        }

        [UnityTest]
        public IEnumerator InCorner_CameraStaysAboveSurface()
        {
            yield return Measure("Hub corner", new Vector3(17f, 1f, -17f), 45f);
        }

        [UnityTest]
        public IEnumerator OverCameraApron_CameraStaysAboveSurface()
        {
            // Yaw 180 pushes the camera north, out through the doorway and over the apron, which
            // carries no solid collider. This is the case that used to expose the level underside.
            yield return Measure("Doorway over north apron", new Vector3(0f, 1f, 18f), 180f);
        }

        [UnityTest]
        public IEnumerator OnMovementSteps_LimitTracksTheSurface()
        {
            yield return Measure("Movement gym steps", new Vector3(-60f, 2.6f, 4f), 0f);
        }

        [UnityTest]
        public IEnumerator OnSlope_LimitTracksTheSurface()
        {
            yield return Measure("Movement gym 15 degree slope", new Vector3(-80f, 2.4f, 25f), 0f);
        }

        [UnityTest]
        public IEnumerator RotatingWhileCompressed_StaysAboveSurfaceAndDoesNotJitter()
        {
            yield return Teleport(new Vector3(0f, 1f, 0f));
            _driver.Yaw = 0f;
            yield return DriveTo(-20f);

            float previousY = Camera.main.transform.position.y;
            float worstDrop = 0f;
            float largestStep = 0f;
            float swept = 0f;
            while (swept < 360f)
            {
                float yawStep = 180f * Time.unscaledDeltaTime;
                _driver.Yaw += yawStep;
                swept += yawStep;
                yield return null;

                float y = Camera.main.transform.position.y;
                largestStep = Mathf.Max(largestStep, Mathf.Abs(y - previousY));
                worstDrop = Mathf.Min(worstDrop, y - MinimumAllowedY());
                previousY = y;
            }

            Debug.Log($"[FLOOR] Rotate while compressed | worst clearance {worstDrop:0.000} m "
                + $"| largest per-frame Y step {largestStep:0.000} m");
            Assert.GreaterOrEqual(worstDrop, -0.05f,
                "Camera dropped below the floor while rotating under floor compression.");
            Assert.Less(largestStep, 0.35f,
                "Camera Y jumped between frames while rotating under floor compression.");
        }

        /// <summary>
        /// The floor constraint drives only the radial and vertical axes. The horizontal axis is
        /// what the movement reference is derived from, so it must stay untouched while the camera
        /// is compressed — that separation is what keeps the old 180 degree movement/camera
        /// oscillation from returning.
        /// </summary>
        [UnityTest]
        public IEnumerator MovingWhileCompressed_LeavesTheMovementHeadingAlone()
        {
            yield return Teleport(new Vector3(0f, 1f, 0f));
            _driver.Yaw = 37f;
            yield return DriveTo(-20f);

            // Drive the player along a path while the camera is pinned against the floor.
            _driver.HoldYaw = false;
            float startYaw = _orbit.HorizontalAxis.Value;
            float worstDrift = 0f;
            float worstClearance = 0f;
            float travelled = 0f;
            while (travelled < 10f)
            {
                travelled += Time.unscaledDeltaTime * 2f;
                _player.TeleportTo(
                    new Vector3(Mathf.Sin(travelled) * 6f, 1f, Mathf.Cos(travelled) * 6f),
                    Quaternion.identity);
                yield return null;

                worstDrift = Mathf.Max(worstDrift,
                    Mathf.Abs(Mathf.DeltaAngle(_orbit.HorizontalAxis.Value, startYaw)));
                worstClearance = Mathf.Min(worstClearance,
                    Camera.main.transform.position.y - MinimumAllowedY());
            }

            Debug.Log($"[FLOOR] Move while compressed | yaw drift {worstDrift:0.000} deg "
                + $"| worst clearance {worstClearance:0.000} m");
            Assert.Less(worstDrift, 1f,
                "Floor compression moved the camera heading, which would feed back into movement.");
            Assert.GreaterOrEqual(worstClearance, -0.05f,
                "Camera dropped below the surface while the player moved under compression.");
        }

        [UnityTest]
        public IEnumerator ReleasingPitch_RadiusRecoversSmoothly()
        {
            yield return Teleport(new Vector3(0f, 1f, 0f));
            _driver.Yaw = 0f;
            yield return DriveTo(-20f);

            float compressedRadius = _controller.OrbitRadius;

            // Release the input and pitch back up, as the player would.
            _driver.Active = false;
            _driver.Target = 22f;
            _driver.Restore = true;

            float previousRadius = compressedRadius;
            float largestStep = 0f;
            float elapsed = 0f;
            while (elapsed < 3f)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
                largestStep = Mathf.Max(largestStep, Mathf.Abs(_controller.OrbitRadius - previousRadius));
                previousRadius = _controller.OrbitRadius;
            }

            Debug.Log($"[FLOOR] Recovery | compressed {compressedRadius:0.00} m -> "
                + $"{_controller.OrbitRadius:0.00} m | largest per-frame radius step {largestStep:0.000} m");
            Assert.AreEqual(_orbit.Radius, _controller.OrbitRadius, 0.05f,
                "Orbit radius did not return to its authored value after the floor released.");
            Assert.Less(largestStep, 0.3f, "Orbit radius snapped back rather than easing.");
        }

        [UnityTest]
        public IEnumerator OverGenuineDrop_CameraIsLeftFree()
        {
            // Standing on the tallest Camera-zone height target with the camera hanging off the
            // edge. There is genuinely nothing beneath it, so by design it is not lifted.
            yield return Teleport(new Vector3(88f, 10.4f, 42f));
            _driver.Yaw = 0f;
            yield return DriveTo(-20f);

            Vector3 camera = Camera.main.transform.position;
            Debug.Log($"[FLOOR] Genuine drop | pitch {_orbit.VerticalAxis.Value:0.0} deg "
                + $"| radius {_controller.OrbitRadius:0.00} m | camera Y {camera.y:0.00} "
                + $"| floor limit active {_controller.FloorLimitActive}");
            Assert.Pass("Recorded free-dip behaviour over a genuine drop.");
        }

        private IEnumerator Measure(string label, Vector3 playerPosition, float yaw)
        {
            yield return Teleport(playerPosition);

            _driver.Yaw = yaw;
            yield return DriveTo(-20f);

            Vector3 camera = Camera.main.transform.position;
            float minimumY = MinimumAllowedY();
            float clearance = camera.y - minimumY;

            Debug.Log($"[FLOOR] {label} | pitch {_orbit.VerticalAxis.Value:0.00} deg "
                + $"| radius {_controller.OrbitRadius:0.00} m "
                + $"| camera Y {camera.y:0.00} | required Y {minimumY:0.00} "
                + $"| clearance {clearance:0.000} m "
                + $"| limit {(_controller.FloorLimitActive ? _controller.FloorPitchLimit.ToString("0.00") + " deg" : "inactive")} "
                + $"| guard lift {_guard.LastLift:0.000} m");

            Assert.GreaterOrEqual(clearance, -0.05f,
                $"{label}: camera rendered below the walkable surface (camera Y {camera.y:0.00}, "
                + $"required {minimumY:0.00}).");

            if (label == "Hub corner")
                yield return Capture("after-hub-corner");
            yield return null;
        }

        private static IEnumerator Capture(string name)
        {
            string path = $"docs/audits/camera-floor/{name}.png";
            System.IO.Directory.CreateDirectory("docs/audits/camera-floor");
            ScreenCapture.CaptureScreenshot(path);
            for (int i = 0; i < 10; i++)
                yield return null;
            Debug.Log($"[FLOOR] captured {path}");
        }

        /// <summary>
        /// The lowest height the camera is allowed to occupy, measured independently of the
        /// production code path so the assertion is not simply restating the implementation.
        /// </summary>
        private float MinimumAllowedY()
        {
            Vector3 camera = Camera.main.transform.position;
            Vector3 target = _player.transform.position + _orbit.TargetOffset;
            float radius = _decollider != null ? _decollider.CameraRadius : 0.35f;

            if (camera.y >= target.y)
                return float.NegativeInfinity;

            Vector3 origin = new(camera.x, target.y, camera.z);
            float distance = target.y - camera.y + 0.5f;
            return TestCampusCameraGround.ProbeGroundY(origin, distance, radius, ~0, out float groundY)
                ? groundY + radius
                : float.NegativeInfinity;
        }

        /// <summary>
        /// Yields for a wall-clock duration. Test frame rates run far above real time, so a frame
        /// count is not a duration: 240 frames is about half a second here, which is not enough
        /// for a quarter-second smoothing to settle, and makes a per-frame rotation absurdly fast.
        /// </summary>
        private static IEnumerator WaitSeconds(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
        }

        /// <summary>
        /// Drives the pitch axis until it actually settles at the requested bound, rather than for
        /// a fixed frame count — test frame rates vary enough that a frame budget silently stops
        /// short of the bound and hides the very case under test.
        /// </summary>
        private IEnumerator DriveTo(float target)
        {
            _driver.Target = target;
            _driver.Active = true;

            float settled = 0f;
            for (int i = 0; i < 900; i++)
            {
                float before = _orbit.VerticalAxis.Value;
                yield return null;
                settled = Mathf.Abs(_orbit.VerticalAxis.Value - before) < 0.001f ? settled + 1f : 0f;
                if (settled > 60f)
                    break;
            }
        }

        private IEnumerator Teleport(Vector3 position)
        {
            _driver.Active = false;
            _driver.Restore = false;
            _driver.HoldYaw = true;
            _orbit.VerticalAxis.Value = 22f;

            // The scene stays loaded across tests, so the controller's smoothing state and the
            // radial axis carry over. Reset them or an earlier case leaks into this one.
            _orbit.RadialAxis.Value = 1f;
            _player.TeleportTo(position, Quaternion.identity);
            for (int i = 0; i < SettleFrames; i++)
                yield return null;
        }

        /// <summary>
        /// Drives the orbit axes ahead of the prototype controller, standing in for held mouse
        /// input. The negative execution order matters: the controller constrains the axes in its
        /// own Update, so the driver has to write before it, not after.
        /// </summary>
        [DefaultExecutionOrder(-10000)]
        private sealed class PitchDriver : MonoBehaviour
        {
            private const float DegreesPerSecond = 60f;

            public CinemachineOrbitalFollow Orbit;
            public bool Active;
            public bool Restore;
            public bool HoldYaw = true;
            public float Yaw;
            public float Target = -20f;

            private void Update()
            {
                if (Orbit == null)
                    return;

                if (HoldYaw)
                    Orbit.HorizontalAxis.Value = Orbit.HorizontalAxis.ClampValue(Yaw);

                if (!Active && !Restore)
                    return;

                float step = DegreesPerSecond * Time.unscaledDeltaTime;
                float value = Mathf.MoveTowards(Orbit.VerticalAxis.Value, Target, step);
                Orbit.VerticalAxis.Value = Orbit.VerticalAxis.ClampValue(value);
            }
        }
    }
}
