using Unity.Cinemachine;
using UnityEngine;

namespace CrazyMarket.TestCampus
{
    /// <summary>
    /// Guarantees the rendered camera stays between the walkable surface beneath it and the
    /// ceiling above it.
    /// </summary>
    /// <remarks>
    /// The prototype controller already constrains the orbit's radial and vertical axes, but the
    /// rig's position damping means the rendered camera lags the desired orbit position and can
    /// transiently break that constraint during fast movement, jumps and step transitions. This
    /// extension enforces the same limit on the final corrected position.
    ///
    /// It runs at the Body stage, so the CinemachineRotationComposer in the Aim stage re-aims from
    /// the corrected position automatically. Place it after the CinemachineDecollider because it
    /// applies the final vertical correction—up from floors or down from ceilings.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TestCampusCameraGroundGuard : CinemachineExtension
    {
        public const float DefaultMinimumOrbitRadiusScale = 0.7368f;
        public const float DefaultCameraRadius = 0.35f;
        public const float DefaultClearance = 0.1f;
        public const float DefaultProbeSlack = 0.5f;
        public const float DefaultTargetYOffset = 1.2f;

        [Tooltip("Gap held between the camera sphere and the floor below or ceiling above it.")]
        [SerializeField] private float groundClearance = DefaultClearance;

        [Tooltip("Extra probe distance past the camera, so the surface is still found on the "
            + "frame the camera first breaches it.")]
        [SerializeField] private float groundProbeSlack = DefaultProbeSlack;

        [SerializeField] private LayerMask groundLayers = ~0;

        // Deliberately undamped: this is the guarantee layer, so it must apply in full on the
        // frame the breach happens. Easing it lets a stale correction bleed into the next
        // situation and push the camera the wrong way. Edge-crossing smoothness is the
        // controller's job, via its pitch-limit ramps.
        public bool IsLifting { get; private set; }
        public float LastLift { get; private set; }
        public float GroundClearance => groundClearance;
        public float GroundProbeSlack => groundProbeSlack;

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Body)
                return;

            Transform follow = vcam != null ? vcam.Follow : null;
            if (follow == null)
            {
                IsLifting = false;
                return;
            }

            CinemachineOrbitalFollow orbit = vcam.GetComponent<CinemachineOrbitalFollow>();
            CinemachineDecollider decollider = vcam.GetComponent<CinemachineDecollider>();
            Vector3 targetOffset = orbit != null
                ? orbit.TargetOffset
                : Vector3.up * DefaultTargetYOffset;
            float cameraRadius = decollider != null
                ? decollider.CameraRadius
                : DefaultCameraRadius;
            Vector3 target = follow.position + targetOffset;
            Vector3 camera = state.GetCorrectedPosition();

            float correction = 0f;
            if (TestCampusCameraSurfaceProbe.TryGetMinimumCameraY(
                    target, camera, cameraRadius, groundClearance, groundProbeSlack,
                    groundLayers, out float minimumY)
                && camera.y < minimumY)
            {
                correction = minimumY - camera.y;
            }
            else if (TestCampusCameraSurfaceProbe.TryGetMaximumCameraY(
                         target, camera, cameraRadius, groundClearance, groundProbeSlack,
                         groundLayers, out float maximumY)
                     && camera.y > maximumY)
            {
                // Never push down past the surface the player is standing on: in a space too tight
                // to satisfy both, clipping a ceiling reads far better than falling out of the level.
                float floorY = TestCampusCameraSurfaceProbe.TryGetMinimumCameraY(
                    target, new Vector3(camera.x, target.y - 0.001f, camera.z), cameraRadius,
                    groundClearance, groundProbeSlack, groundLayers, out float standingY)
                    ? standingY
                    : float.NegativeInfinity;
                correction = Mathf.Max(maximumY, floorY) - camera.y;
            }

            if (Mathf.Abs(correction) < 0.0001f)
            {
                IsLifting = false;
                LastLift = 0f;
                return;
            }

            LastLift = correction;
            IsLifting = true;
            state.PositionCorrection += Vector3.up * correction;
        }
    }
}
