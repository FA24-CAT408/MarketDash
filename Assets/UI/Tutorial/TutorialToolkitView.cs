using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class TutorialToolkitView : MonoBehaviour
{
    [SerializeField] private TutorialRunFlow flow;
    [SerializeField] private TimerManager timer;
    [SerializeField] private GameSettingsManager settings;
    [SerializeField] private Texture2D itemPreview;
    [SerializeField] private StagingAreaController staging;
    [SerializeField] private GameObject deliveryCamera;
    private VisualElement root, hud, clock, marker, brief, pause, result;
    private Label phase, objective, instruction, time, markerArrow, markerLabel, markerDistance, resultTime;
    private GameManager.GameState lastState = (GameManager.GameState)(-1);
    private bool lastBrief = true;
    private bool ready;
    private bool lastPresentationComplete;

    private void OnEnable() => GameManager.OnStateChanged += StateChanged;
    private void OnDisable() => GameManager.OnStateChanged -= StateChanged;
    private void StateChanged(GameManager.GameState state)
    {
        if (ready) RefreshState();
    }

    private void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        hud = root.Q("hud"); clock = root.Q("clock"); marker = root.Q("marker");
        brief = root.Q("briefShade"); pause = root.Q("pauseShade"); result = root.Q("resultShade");
        phase = root.Q<Label>("phase"); objective = root.Q<Label>("objective"); instruction = root.Q<Label>("instruction");
        time = root.Q<Label>("time"); markerArrow = root.Q<Label>("markerArrow"); markerLabel = root.Q<Label>("markerLabel");
        markerDistance = root.Q<Label>("markerDistance"); resultTime = root.Q<Label>("resultTime");
        root.Q<Image>("itemPreview").image = itemPreview;
        root.Q<Button>("readyButton").clicked += flow.Acknowledge;
        root.Q<Button>("resumeButton").clicked += () => GameManager.Instance.UnpauseGame();
        root.Q<Button>("retryButton").clicked += Restart;
        root.Q<Button>("againButton").clicked += Restart;
        root.Q<Button>("menuButton").clicked += Menu;
        root.Q<Button>("resultMenuButton").clicked += Menu;
        root.Q<Button>("nextButton").clicked += () => GameManager.Instance.LoadNextScene();
        var sensitivity = root.Q<Slider>("sensitivity");
        var volume = root.Q<Slider>("volume");
        var invert = root.Q<Toggle>("invert");
        sensitivity.SetValueWithoutNotify(settings.Sensitivity);
        volume.SetValueWithoutNotify(settings.Volume);
        invert.SetValueWithoutNotify(settings.InvertCamera);
        sensitivity.RegisterValueChangedCallback(e => settings.Sensitivity = e.newValue);
        volume.RegisterValueChangedCallback(e => settings.SetVolume(e.newValue));
        invert.RegisterValueChangedCallback(e => settings.InvertCamera = e.newValue);
        ready = true;
        RefreshState();
    }

    private void Update()
    {
        if (!ready || GameManager.Instance == null) return;
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;
        bool pausePressed = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
            (gamepad != null && gamepad.startButton.wasPressedThisFrame);
        if (pausePressed)
        {
            if (GameManager.Instance.CurrentState == GameManager.GameState.Pause) GameManager.Instance.UnpauseGame();
            else GameManager.Instance.PauseGame();
        }
        if (flow.Briefing && GameManager.Instance.CurrentState == GameManager.GameState.PreGame &&
            ((keyboard != null && keyboard.enterKey.wasPressedThisFrame) ||
             (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame))) flow.Acknowledge();
        if (lastState != GameManager.Instance.CurrentState || lastBrief != flow.Briefing ||
            lastPresentationComplete != (staging == null || staging.PresentationComplete)) RefreshState();
        time.text = timer.GetFormattedTime(timer.Timer);
    }

    private void RefreshState()
    {
        lastState = GameManager.Instance.CurrentState;
        lastBrief = flow.Briefing;
        bool isPause = lastState == GameManager.GameState.Pause;
        bool complete = lastState == GameManager.GameState.GameOver;
        lastPresentationComplete = staging == null || staging.PresentationComplete;
        bool showReceipt = complete && lastPresentationComplete;
        if (deliveryCamera != null) deliveryCamera.SetActive(complete);
        bool isBrief = flow.Briefing && !isPause;
        brief.EnableInClassList("hidden", !isBrief);
        pause.EnableInClassList("hidden", !isPause);
        result.EnableInClassList("hidden", !showReceipt);
        hud.EnableInClassList("hidden", isBrief || isPause || complete);
        root.Q("footer")?.EnableInClassList("hidden", isBrief || isPause || complete);
        clock.EnableInClassList("hidden", lastState != GameManager.GameState.InProgress && lastState != GameManager.GameState.EndGame);
        marker.EnableInClassList("hidden", isBrief || isPause || complete || lastState == GameManager.GameState.LoadingIn);
        phase.text = lastState == GameManager.GameState.PreGame ? "WARM-UP" : flow.Returning ? "DELIVER" : "COLLECT";
        objective.text = lastState == GameManager.GameState.PreGame ? "Try your controls" : flow.Returning ? "Return the eggs" : "Grab the eggs";
        instruction.text = lastState == GameManager.GameState.PreGame
            ? "Enter the market when ready."
            : flow.Returning ? "Follow the delivery marker." : "Jump onto the table.";
        if (complete) resultTime.text = "SHIFT TIME   " + timer.GetFormattedTime(timer.Timer);
        if (isPause) root.Q<Button>("resumeButton").Focus();
        else if (showReceipt) root.Q<Button>("nextButton").Focus();
        else if (isBrief) root.Q<Button>("readyButton").Focus();
        else root.focusController?.focusedElement?.Blur();
    }

    private void LateUpdate()
    {
        if (!ready || marker.ClassListContains("hidden") || Camera.main == null || root.panel == null) return;
        Vector3 target = flow.NavigationTarget + Vector3.up * 2.3f;
        Vector3 screen = Camera.main.WorldToViewportPoint(target);
        float width = root.resolvedStyle.width, height = root.resolvedStyle.height;
        if (width <= 0 || height <= 0) return;
        Vector2 direction = new Vector2(screen.x - .5f, .5f - screen.y);
        if (screen.z < 0) direction = -direction;
        bool offscreen = screen.z < 0 || screen.x < .1f || screen.x > .9f || screen.y < .14f || screen.y > .8f;
        Vector2 position = new Vector2(screen.x * width, (1 - screen.y) * height);
        if (offscreen)
        {
            if (direction.sqrMagnitude < .001f) direction = Vector2.down;
            direction.Normalize();
            float factor = Mathf.Min((width * .5f - 115) / Mathf.Max(Mathf.Abs(direction.x), .001f),
                (height * .5f - 175) / Mathf.Max(Mathf.Abs(direction.y), .001f));
            position = new Vector2(width * .5f, height * .5f) + direction * factor;
        }
        position.x = Mathf.Clamp(position.x, 115, width - 115);
        position.y = Mathf.Clamp(position.y, 140, height - 175);
        // World-projected coordinates must follow the camera; USS owns the marker's appearance.
        marker.style.left = position.x - 100;
        marker.style.top = position.y - 30;
        markerArrow.text = !offscreen ? "\u2193" : Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? (direction.x > 0 ? "\u2192" : "\u2190") : (direction.y > 0 ? "\u2193" : "\u2191");
        markerLabel.text = flow.NavigationLabel;
        markerDistance.text = Mathf.RoundToInt(Vector3.Distance(flow.Player.position, flow.NavigationTarget)) + " m";
    }

    private static void Restart() => GameManager.Instance.LoadScene("Level 1");
    private static void Menu() => GameManager.Instance.LoadScene("Main Menu");
}


