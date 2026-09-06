using CrazyMarket.Player.V2;
using CrazyMarket.Player.V2.Unity;
using CrazyMarket.TestCampus;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>Production routing for the Test Campus assisted orbit and shared surface protection.</summary>
[DefaultExecutionOrder(10000)]
[RequireComponent(typeof(CinemachineCamera), typeof(CinemachineOrbitalFollow), typeof(TestCampusCameraInputFocus))]
public sealed class PlayerOrbitCamera : MonoBehaviour
{
    [SerializeField] private PlayerControllerV2 player;
    [SerializeField] private GameSettingsManager settings;
    [SerializeField] private float recenterDelay = 2.5f;
    [SerializeField] private float recenterSmoothTime = 1.2f;
    [SerializeField] private float maximumRecenterSpeed = 65f;
    [SerializeField, Min(0f)] private float steadyTravelDelay = .8f;
    [SerializeField, Min(0f)] private float minimumTravelSpeed = 2.5f;
    private CinemachineOrbitalFollow orbit;
    private TestCampusCameraInputFocus input;
    private Transform movementReference;
    private float lastManualInput, recenterVelocity, recenterHeading;
    private bool recenterActive, recenterConsumed, uiFocus;
    private float steadyTravelTime, previousTravelHeading, previousIntentHeading;
    private bool hasTravelHeading;

    private void Start()
    {
        orbit = GetComponent<CinemachineOrbitalFollow>();
        input = GetComponent<TestCampusCameraInputFocus>();
        if (player == null) player = FindAnyObjectByType<PlayerControllerV2>();
        if (player == null) { enabled = false; return; }
        var camera = GetComponent<CinemachineCamera>();
        camera.Follow = camera.LookAt = player.transform;
        movementReference = new GameObject("Orbit Movement Reference").transform;
        movementReference.SetParent(transform, false);
        movementReference.rotation = Quaternion.Euler(0, orbit.HorizontalAxis.Value, 0);
        player.SetMovementReference(movementReference);
        lastManualInput = Time.unscaledTime;
        ApplyFocus();
    }

    private void Update()
    {
        if (player == null || orbit == null) return;
        ApplyFocus();
        if (uiFocus) { ResetRecentering(); return; }
        Vector2 look = input.ConsumeLookInput();
        if (settings != null)
        {
            look *= settings.Sensitivity;
            if (settings.InvertCamera) look.y = -look.y;
        }
        if (look.sqrMagnitude > .0001f)
        {
            orbit.HorizontalAxis.Value = orbit.HorizontalAxis.ClampValue(orbit.HorizontalAxis.Value + look.x);
            orbit.VerticalAxis.Value = orbit.VerticalAxis.ClampValue(orbit.VerticalAxis.Value - look.y);
            lastManualInput = Time.unscaledTime;
            ResetRecentering();
        }
        else
        {
            var snapshot = player.Snapshot;
            Vector3 planar = Vector3.ProjectOnPlane(snapshot.Velocity, Vector3.up);
            float heading = Mathf.Atan2(planar.x, planar.z) * Mathf.Rad2Deg;
            float intentHeading = Mathf.Atan2(player.MovementDirection.x, player.MovementDirection.z) * Mathf.Rad2Deg;
            bool travelling = snapshot.StableGrounded && planar.magnitude >= minimumTravelSpeed
                && player.MovementDirection.sqrMagnitude > .01f
                && (snapshot.ActionFlags & PlayerActionFlags.Teleported) == 0;
            // Input turns cancel immediately, even while momentum still carries the old heading.
            float headingChange = Mathf.Max(Mathf.Abs(Mathf.DeltaAngle(previousTravelHeading, heading)),
                Mathf.Abs(Mathf.DeltaAngle(previousIntentHeading, intentHeading)));
            bool turning = hasTravelHeading && headingChange > 45f * Mathf.Max(Time.deltaTime, .001f);
            if (!travelling || turning)
            {
                ResetRecentering();
            }
            else
            {
                steadyTravelTime += Time.deltaTime;
                if (steadyTravelTime >= steadyTravelDelay && Time.unscaledTime - lastManualInput >= recenterDelay)
                {
                    if (!recenterActive && !recenterConsumed && Mathf.Abs(Mathf.DeltaAngle(orbit.HorizontalAxis.Value, heading)) <= 65)
                    {
                        recenterHeading = heading;
                        recenterActive = recenterConsumed = true;
                    }
                    if (recenterActive)
                    {
                        orbit.HorizontalAxis.Value = Mathf.SmoothDampAngle(orbit.HorizontalAxis.Value, recenterHeading,
                            ref recenterVelocity, recenterSmoothTime, maximumRecenterSpeed, Time.unscaledDeltaTime);
                        if (Mathf.Abs(Mathf.DeltaAngle(orbit.HorizontalAxis.Value, recenterHeading)) < .25f)
                        { orbit.HorizontalAxis.Value = recenterHeading; recenterActive = false; }
                    }
                }
            }
            previousTravelHeading = heading;
            previousIntentHeading = intentHeading;
            hasTravelHeading = travelling;
        }
        movementReference.rotation = Quaternion.Euler(0, orbit.HorizontalAxis.Value, 0);
    }

    private void ResetRecentering()
    {
        recenterActive = recenterConsumed = hasTravelHeading = false;
        recenterVelocity = steadyTravelTime = 0;
    }

    private void OnDisable() => ResetRecentering();

    private void ApplyFocus()
    {
        var gm = GameManager.Instance;
        bool blocked = gm != null && (gm.CurrentState == GameManager.GameState.Pause ||
            gm.CurrentState == GameManager.GameState.LoadingIn || gm.CurrentState == GameManager.GameState.GameOver);
        blocked |= player != null && player.Snapshot.ControlBlocked;
        if (input.UiHasFocus != blocked) input.SetUiFocus(blocked);
        uiFocus = blocked;
    }

    private void OnDestroy()
    {
        if (player != null && Camera.main != null) player.SetMovementReference(Camera.main.transform);
        if (movementReference != null) Destroy(movementReference.gameObject);
    }
}


