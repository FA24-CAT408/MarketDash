using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CrazyMarket.TestCampus
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class TestCampusControlPanel : MonoBehaviour
    {
        [SerializeField] private TestCampusController controller;

        private readonly List<(Button Button, Action Action)> _bindings = new();
        private UIDocument _document;
        private ScrollView _panel;
        private VisualElement _gameplayHud;
        private Label _status;
        private Label _feedback;
        private Label _diagnostics;
        private Label _gameplayHelp;
        private TestCampusCameraPrototypeController _cameraPrototypes;
        private bool _showDiagnostics;
        private bool _built;
        private float _feedbackUntil;

        private void Start()
        {
            controller ??= TestCampusController.Instance;
            _document = GetComponent<UIDocument>();
            Build();
        }

        private void OnEnable()
        {
            if (_built) BindStaticActions();
        }

        private void OnDisable()
        {
            UnbindActions();
            if (_panel != null) _panel.style.display = DisplayStyle.None;
            if (_gameplayHud != null) _gameplayHud.style.display = DisplayStyle.Flex;
            _cameraPrototypes?.SetUiFocus(false);
            if (Time.timeScale == 0f) Time.timeScale = 1f;
        }

        private void Update()
        {
            _cameraPrototypes ??= TestCampusCameraPrototypeController.Instance;
            if (_status != null && controller != null)
            {
                _status.text = $"CURRENT: {controller.CurrentZone}   LOADED: {controller.RegisteredZoneCount}/7\n"
                    + (_cameraPrototypes != null ? _cameraPrototypes.Status : "Camera prototypes loading…");
            }

            if (_gameplayHelp != null)
            {
                string cameraStatus = _cameraPrototypes != null
                    ? _cameraPrototypes.Status
                    : "Camera prototypes loading…";
                _gameplayHelp.text =
                    "WASD MOVE  ·  SPACE JUMP  ·  MOUSE LOOK  ·  F1 CONTROLS\n"
                    + "AUTO RECENTER: 2.5s while grounded + moving  ·  R: immediate recenter\n"
                    + cameraStatus;
            }

            if (_feedback != null && _feedbackUntil > 0f && Time.unscaledTime > _feedbackUntil)
            {
                _feedback.text = "READY — choose a zone or test action";
                _feedback.style.color = new StyleColor(new Color(0.46f, 0.91f, 1f));
                _feedbackUntil = 0f;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f1Key.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
                SetPanelOpen(!PanelOpen);
            if (keyboard.f2Key.wasPressedThisFrame) ResetCurrentZone();
            if (keyboard.f3Key.wasPressedThisFrame) controller?.ReturnToHub();
            if (keyboard.pKey.wasPressedThisFrame) TogglePause();
            if (_showDiagnostics) RefreshDiagnostics();
        }

        private bool PanelOpen => _panel != null && _panel.style.display != DisplayStyle.None;

        private void Build()
        {
            if (_document == null || _document.panelSettings == null || _document.visualTreeAsset == null)
            {
                Debug.LogError("Test Campus UI Toolkit document requires PanelSettings and a VisualTreeAsset.", this);
                enabled = false;
                return;
            }

            VisualElement root = _document.rootVisualElement;
            _panel = Require<ScrollView>(root, "campus-panel");
            _gameplayHud = Require<VisualElement>(root, "gameplay-hud");
            _status = Require<Label>(root, "status-label");
            _feedback = Require<Label>(root, "feedback-label");
            _diagnostics = Require<Label>(root, "diagnostics-label");
            _gameplayHelp = Require<Label>(root, "gameplay-help");
            VisualElement zoneButtons = Require<VisualElement>(root, "zone-buttons");
            if (enabled == false) return;

            root.pickingMode = PickingMode.Ignore;
            _gameplayHud.pickingMode = PickingMode.Ignore;
            _panel.pickingMode = PickingMode.Position;

            foreach (TestZoneId zone in Enum.GetValues(typeof(TestZoneId)))
            {
                TestZoneId capturedZone = zone;
                Button button = new(() => Report(
                    controller != null && controller.TeleportToZone(capturedZone),
                    $"TELEPORTED: {capturedZone}", $"UNAVAILABLE: {capturedZone}"))
                {
                    text = capturedZone.ToString(),
                    name = $"zone-{capturedZone.ToString().ToLowerInvariant()}"
                };
                zoneButtons.Add(button);
            }

            BindStaticActions();
            _cameraPrototypes = TestCampusCameraPrototypeController.Instance;
            _built = true;
            SetPanelOpen(false);
        }

        private T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            T element = root.Q<T>(name);
            if (element != null) return element;
            Debug.LogError($"Test Campus UI Toolkit layout is missing '{name}'.", this);
            enabled = false;
            return null;
        }

        private void BindStaticActions()
        {
            if (_document == null || _bindings.Count != 0) return;
            VisualElement root = _document.rootVisualElement;
            Bind(root, "reset-current", () => Report(
                ResetCurrentZone(), $"RESET: {controller?.CurrentZone}", "RESET FAILED"));
            Bind(root, "reset-campus", () =>
            {
                controller?.ResetCampus();
                SetPanelOpen(false);
                Report(controller != null, "CAMPUS RESET — returned to Hub", "CAMPUS RESET FAILED");
            });
            Bind(root, "return-hub", () => Report(
                controller != null && controller.ReturnToHub(), "RETURNED TO HUB", "HUB UNAVAILABLE"));
            Bind(root, "preset-low", () => ApplyPreset("Low"));
            Bind(root, "preset-normal", () => ApplyPreset("Normal"));
            Bind(root, "preset-stress", () => ApplyPreset("Stress"));
            Bind(root, "camera-assisted", () => SetCameraMode(TestCampusCameraMode.AssistedOrbit));
            Bind(root, "camera-guided", () => SetCameraMode(TestCampusCameraMode.GuidedRail));
            Bind(root, "camera-hybrid", () => SetCameraMode(TestCampusCameraMode.HybridZones));
            Bind(root, "diagnostics-toggle", ToggleDiagnostics);
            Bind(root, "pause-toggle", TogglePause);
        }

        private void Bind(VisualElement root, string name, Action action)
        {
            Button button = Require<Button>(root, name);
            if (button == null) return;
            button.clicked += action;
            _bindings.Add((button, action));
        }

        private void UnbindActions()
        {
            foreach ((Button button, Action action) in _bindings)
                button.clicked -= action;
            _bindings.Clear();
        }

        private void SetPanelOpen(bool open)
        {
            if (_panel == null || _gameplayHud == null) return;
            if (!open && Time.timeScale == 0f) Time.timeScale = 1f;
            _panel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            _gameplayHud.style.display = open ? DisplayStyle.None : DisplayStyle.Flex;
            _cameraPrototypes ??= TestCampusCameraPrototypeController.Instance;
            _cameraPrototypes?.SetUiFocus(open);
            if (open) _panel.Focus();
        }

        private void SetCameraMode(TestCampusCameraMode mode)
        {
            _cameraPrototypes ??= TestCampusCameraPrototypeController.Instance;
            bool available = _cameraPrototypes != null;
            if (available) _cameraPrototypes.SetMode(mode);
            Report(available, $"CAMERA MODE: {mode}", "CAMERA PROTOTYPE UNAVAILABLE");
        }

        private bool ResetCurrentZone() =>
            controller != null && controller.ResetZone(controller.CurrentZone);

        private void TogglePause()
        {
            bool pause = Time.timeScale > 0f;
            Time.timeScale = pause ? 0f : 1f;
            SetPanelOpen(pause);
            Report(true, pause ? "SIMULATION PAUSED — UI remains active" : "SIMULATION RESUMED", "");
        }

        private void ApplyPreset(string preset)
        {
            Report(controller != null && controller.ApplyPreset(controller.CurrentZone, preset),
                $"PRESET APPLIED: {preset}", $"PRESET UNAVAILABLE: {preset}");
        }

        private void ToggleDiagnostics()
        {
            _showDiagnostics = !_showDiagnostics;
            _diagnostics.style.display = _showDiagnostics ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshDiagnostics();
            Report(true, _showDiagnostics ? "DIAGNOSTICS SHOWN" : "DIAGNOSTICS HIDDEN", "");
        }

        private void Report(bool success, string successMessage, string failureMessage)
        {
            if (_feedback == null) return;
            _feedback.text = success ? successMessage : failureMessage;
            _feedback.style.color = new StyleColor(success
                ? new Color(0.45f, 1f, 0.62f)
                : new Color(1f, 0.55f, 0.4f));
            _feedbackUntil = Time.unscaledTime + 3f;
        }

        private void RefreshDiagnostics()
        {
            if (_diagnostics == null || controller == null) return;
            StringBuilder text = new();
            foreach (TestZoneRoot zone in controller.RegisteredZones.Values)
                foreach (TestDiagnostic diagnostic in zone.GetDiagnostics())
                    text.AppendLine($"{zone.ZoneId}: {diagnostic.Label} = {diagnostic.Value}");
            _diagnostics.text = text.Length == 0 ? "No diagnostics reported." : text.ToString();
        }
    }
}
