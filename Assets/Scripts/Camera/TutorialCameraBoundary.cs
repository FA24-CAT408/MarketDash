using Unity.Cinemachine;
using UnityEngine;

/// <summary>Keeps the camera's near plane inside the welcome room while its player is inside.</summary>
[DefaultExecutionOrder(10006)]
public sealed class TutorialCameraBoundary : CinemachineExtension
{
    [SerializeField] private Transform player;
    [SerializeField] private Bounds hubInterior = new Bounds(new Vector3(188.5f,8.9f,-223.4f), new Vector3(16.8f,16.4f,56.6f));
    [SerializeField] private float doorwayX = 179.9f;
    [SerializeField] private float clearance = .45f;
    [SerializeField] private float doorwayCenterZ = -222.08f;
    [SerializeField] private float doorwayHalfWidth = 4.95f;
    [SerializeField] private float doorwayCeiling = 8.89f;
    private CinemachineOrbitalFollow orbit;

    private void Update()
    {
        if (!Application.isPlaying || player == null) return;
        if (orbit == null) orbit = GetComponent<CinemachineOrbitalFollow>();
        if (orbit == null || orbit.VerticalAxis.Value <= 0) return;
        Vector3 target = player.position + orbit.TargetOffset;
        float radius = orbit.Radius * orbit.RadialAxis.Value;
        if (radius <= 0) return;
        Vector3 desired = target + Quaternion.Euler(orbit.VerticalAxis.Value,
            orbit.HorizontalAxis.Value, 0) * Vector3.back * radius;
        if (Mathf.Abs(desired.z - doorwayCenterZ) > doorwayHalfWidth) return;
        // Anticipate the lintel instead of waiting for an overlap to force the camera down.
        float distance = Mathf.Abs(desired.x - doorwayX);
        float ceiling = doorwayCeiling - clearance - .35f + Mathf.Max(0, distance - 1f) * .45f;
        float pitchLimit = Mathf.Asin(Mathf.Clamp((ceiling - target.y) / radius, -1, 1)) * Mathf.Rad2Deg;
        orbit.VerticalAxis.Value = Mathf.Min(orbit.VerticalAxis.Value, Mathf.Max(0, pitchLimit));
    }
    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase camera,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (player == null ||
            (stage != CinemachineCore.Stage.Body && stage != CinemachineCore.Stage.Finalize)) return;
        Vector3 point = state.GetCorrectedPosition();
        if (player.position.x < doorwayX && point.x < doorwayX) return;
        Vector3 minimum = hubInterior.min + Vector3.one * clearance;
        Vector3 maximum = hubInterior.max - Vector3.one * clearance;
        Vector3 confined = new Vector3(Mathf.Clamp(point.x, minimum.x, maximum.x),
            Mathf.Clamp(point.y, minimum.y, maximum.y), Mathf.Clamp(point.z, minimum.z, maximum.z));
        // The camera can lead or trail the player through the real opening.
        // Door-leaf collision still applies; crossing the threshold must not snap the orbit.
        if (Mathf.Abs(point.z - doorwayCenterZ) < doorwayHalfWidth - clearance)
            confined.x = Mathf.Min(point.x, maximum.x);
        state.PositionCorrection += confined - point;
    }
}
