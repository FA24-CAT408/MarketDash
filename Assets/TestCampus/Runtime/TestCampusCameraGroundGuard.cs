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
    /// the corrected position automatically and composition is preserved without any manual
    /// restore. Place it after the CinemachineDecollider on the rig: extension callbacks fire in
    /// component order. Ordering is not load bearing because this only ever raises the camera and
    /// the clearance keeps the camera sphere clear of penetration, but keep it deterministic.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TestCampusCameraGroundGuard : CinemachineExtension
    {
        [Tooltip("Should match the CinemachineDecollider's Camera Radius on this rig.")]
        [SerializeField] private float cameraRadius = 0.35f;

        [Tooltip("Gap held between the camera sphere and the floor below or ceiling above it.")]
        [SerializeField] private float groundClearance = 0.1f;

        [Tooltip("Extra probe distance past the camera, so the surface is still found on the "
            + "frame the camera first breaches it.")]
        [SerializeField] private float groundProbeSlack = 0.5f;

        [Tooltip("Should match the CinemachineOrbitalFollow's Target Offset Y on this rig.")]
        [SerializeField] private float targetYOffset = 1.2f;

        [SerializeField] private LayerMask groundLayers = ~0;

        // Deliberately undamped: this is the guarantee layer, so it must apply in full on the
        // frame the breach happens. Easing it lets a stale correction bleed into the next
        // situation and push the camera the wrong way. Edge-crossing smoothness is the
        // controller's job, via its pitch-limit ramps.
        public bool IsLifting { get; private set; }
        public float LastLift { get; private set; }

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

            Vector3 target = follow.position + Vector3.up * targetYOffset;
            Vector3 camera = state.GetCorrectedPosition();

            float correction = 0f;
            if (TestCampusCameraGround.TryGetMinimumCameraY(
                    target, camera, cameraRadius, groundClearance, groundProbeSlack,
                    groundLayers, out float minimumY)
                && camera.y < minimumY)
            {
                correction = minimumY - camera.y;
            }
            else if (TestCampusCameraGround.TryGetMaximumCameraY(
                         target, camera, cameraRadius, groundClearance, groundProbeSlack,
                         groundLayers, out float maximumY)
                     && camera.y > maximumY)
            {
                // Never push down past the surface the player is standing on: in a space too tight
                // to satisfy both, clipping a ceiling reads far better than falling out of the level.
                float floorY = TestCampusCameraGround.TryGetMinimumCameraY(
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
