using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Text;

namespace CrazyMarket.TestCampus
{
    public sealed class TestCampusControlPanel : MonoBehaviour
    {
        [SerializeField] private TestCampusController controller;
        private GameObject _panel;
        private Transform _content;
        private TMP_Text _status;
        private TMP_Text _feedback;
        private TMP_Text _diagnostics;
        private TMP_Text _gameplayHud;
        private bool _showDiagnostics;
        private float _feedbackUntil;
        private TestCampusCameraPrototypeController _cameraPrototypes;

        private void Start()
        {
            controller ??= TestCampusController.Instance;
            Build();
        }

        private void Update()
        {
            _cameraPrototypes ??= TestCampusCameraPrototypeController.Instance;
            if (_status != null && controller != null)
            {
                _status.text = $"CURRENT: {controller.CurrentZone}   LOADED: {controller.RegisteredZoneCount}/7\n"
                    + (_cameraPrototypes != null ? _cameraPrototypes.Status : "Camera prototypes loading...");
                FitTextHeight(_status, 42f);
            }
            if (_gameplayHud != null)
            {
                string cameraStatus = _cameraPrototypes != null
                    ? _cameraPrototypes.Status
                    : "Camera prototypes loading...";
                _gameplayHud.text =
                    "WASD MOVE  ·  SPACE JUMP  ·  MOUSE LOOK  ·  F1 CONTROLS\n"
                    + "AUTO RECENTER: 2.5s while grounded + moving  ·  R: immediate recenter\n"
                    + cameraStatus;
            }
            if (_feedback != null && _feedbackUntil > 0f && Time.unscaledTime > _feedbackUntil)
            {
                _feedback.text = "READY — choose a zone or test action";
                _feedbackUntil = 0f;
            }
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if ((keyboard.f1Key.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
                && _panel != null)
                SetPanelOpen(!_panel.activeSelf);
            if (keyboard.f2Key.wasPressedThisFrame) controller?.ResetZone(controller.CurrentZone);
            if (keyboard.f3Key.wasPressedThisFrame) controller?.ReturnToHub();
            if (keyboard.pKey.wasPressedThisFrame) TogglePause();
            if (_showDiagnostics && _diagnostics != null) RefreshDiagnostics();
        }

        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            gameObject.AddComponent<GraphicRaycaster>();

            _gameplayHud = CreateOverlayText();

            _panel = CreateUi("Campus Panel", transform, typeof(Image), typeof(ScrollRect));
            RectTransform rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.015f, 0.04f);
            rect.anchorMax = new Vector2(0.32f, 0.94f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _panel.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.065f, 0.96f);

            GameObject viewport = CreateUi(
                "Viewport", _panel.transform, typeof(Image), typeof(RectMask2D));
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(10f, 10f);
            viewportRect.offsetMax = new Vector2(-10f, -10f);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);

            GameObject content = CreateUi(
                "Content", viewport.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _content = content.transform;
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = _panel.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            CreateText("CRAZYMARKET TEST CAMPUS", 20, FontStyles.Bold);
            _status = CreateText("Initializing...", 14, FontStyles.Normal);
            _feedback = CreateText("READY — choose a zone or test action", 13, FontStyles.Italic);
            _feedback.color = new Color(0.45f, 0.95f, 1f);
            foreach (TestZoneId zone in Enum.GetValues(typeof(TestZoneId)))
                CreateButton(zone.ToString(), () => Report(
                    controller != null && controller.TeleportToZone(zone),
                    $"TELEPORTED: {zone}", $"UNAVAILABLE: {zone}"));
            CreateButton("RESET CURRENT (F2)", () => Report(
                controller != null && controller.ResetZone(controller.CurrentZone),
                $"RESET: {controller?.CurrentZone}", "RESET FAILED"));
            CreateButton("RESET CAMPUS", () =>
            {
                controller?.ResetCampus();
                Report(controller != null, "CAMPUS RESET — returned to Hub", "CAMPUS RESET FAILED");
            });
            CreateButton("RETURN TO HUB (F3)", () => Report(
                controller != null && controller.ReturnToHub(), "RETURNED TO HUB", "HUB UNAVAILABLE"));
            CreateButton("LOW PRESET", () => ApplyPreset("Low"));
            CreateButton("NORMAL PRESET", () => ApplyPreset("Normal"));
            CreateButton("STRESS PRESET", () => ApplyPreset("Stress"));
            CreateButton("CAMERA: ASSISTED ORBIT (F6)", () => SetCameraMode(TestCampusCameraMode.AssistedOrbit));
            CreateButton("CAMERA: GUIDED RAIL (F7)", () => SetCameraMode(TestCampusCameraMode.GuidedRail));
            CreateButton("CAMERA: HYBRID ZONES (F8)", () => SetCameraMode(TestCampusCameraMode.HybridZones));
            CreateButton("TOGGLE DIAGNOSTICS", () =>
            {
                _showDiagnostics = !_showDiagnostics;
                _diagnostics.gameObject.SetActive(_showDiagnostics);
                RefreshDiagnostics();
                Report(true, _showDiagnostics ? "DIAGNOSTICS SHOWN" : "DIAGNOSTICS HIDDEN", "");
            });
            CreateButton("PAUSE / RESUME (P)", TogglePause);
            _diagnostics = CreateText("", 11, FontStyles.Normal);
            _diagnostics.gameObject.SetActive(false);
            _cameraPrototypes = TestCampusCameraPrototypeController.Instance;
            SetPanelOpen(false);
        }

        private TMP_Text CreateOverlayText()
        {
            GameObject background = CreateUi("Gameplay Help", transform, typeof(Image));
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0.015f, 1f);
            backgroundRect.anchorMax = new Vector2(0.72f, 1f);
            backgroundRect.pivot = new Vector2(0f, 1f);
            backgroundRect.offsetMin = new Vector2(0f, -84f);
            backgroundRect.offsetMax = new Vector2(0f, -8f);
            Image image = background.GetComponent<Image>();
            image.color = new Color(0.025f, 0.035f, 0.055f, 0.82f);
            image.raycastTarget = false;

            GameObject textObject = CreateUi("Gameplay Help Text", background.transform, typeof(TextMeshProUGUI));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 5f);
            textRect.offsetMax = new Vector2(-12f, -5f);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.color = Color.white;
            text.fontSize = 16f;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            return text;
        }

        private void SetPanelOpen(bool open)
        {
            if (_panel != null)
                _panel.SetActive(open);
            _cameraPrototypes ??= TestCampusCameraPrototypeController.Instance;
            _cameraPrototypes?.SetUiFocus(open);
            if (_gameplayHud != null)
                _gameplayHud.transform.parent.gameObject.SetActive(!open);
        }

        private void SetCameraMode(TestCampusCameraMode mode)
        {
            _cameraPrototypes ??= TestCampusCameraPrototypeController.Instance;
            bool available = _cameraPrototypes != null;
            if (available)
                _cameraPrototypes.SetMode(mode);
            Report(available, $"CAMERA MODE: {mode}", "CAMERA PROTOTYPE UNAVAILABLE");
        }

        private void TogglePause()
        {
            bool pause = Time.timeScale > 0f;
            Time.timeScale = pause ? 0f : 1f;
            if (pause)
                SetPanelOpen(true);
            Report(true, pause ? "SIMULATION PAUSED — UI remains active" : "SIMULATION RESUMED", "");
        }

        private void ApplyPreset(string preset)
        {
            Report(controller != null && controller.ApplyPreset(preset),
                $"PRESET APPLIED: {preset}", $"PRESET UNAVAILABLE: {preset}");
        }

        private void Report(bool success, string successMessage, string failureMessage)
        {
            if (_feedback == null) return;
            _feedback.text = success ? successMessage : failureMessage;
            _feedback.color = success ? new Color(0.45f, 1f, 0.62f) : new Color(1f, 0.55f, 0.4f);
            _feedbackUntil = Time.unscaledTime + 3f;
            FitTextHeight(_feedback, 28f);
        }

        private void RefreshDiagnostics()
        {
            if (_diagnostics == null || controller == null) return;
            StringBuilder text = new();
            foreach (TestZoneRoot zone in controller.RegisteredZones.Values)
                foreach (TestDiagnostic diagnostic in zone.GetDiagnostics())
                    text.AppendLine($"{zone.ZoneId}: {diagnostic.Label} = {diagnostic.Value}");
            _diagnostics.text = text.ToString();
            FitTextHeight(_diagnostics, 24f);
        }

        private TMP_Text CreateText(string value, int size, FontStyles style)
        {
            GameObject go = CreateUi(value, _content, typeof(TextMeshProUGUI), typeof(LayoutElement));
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.color = Color.white;
            text.fontSize = size;
            text.fontStyle = style;
            text.enableWordWrapping = true;
            go.GetComponent<LayoutElement>().preferredHeight = size + 12;
            return text;
        }

        private void CreateButton(string label, UnityEngine.Events.UnityAction action)
        {
            GameObject go = CreateUi(label, _content, typeof(Image), typeof(Button), typeof(LayoutElement));
            go.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.28f, 1f);
            go.GetComponent<Button>().onClick.AddListener(action);
            go.GetComponent<LayoutElement>().preferredHeight = 34;
            GameObject textObject = CreateUi("Label", go.transform, typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = label; text.color = Color.white; text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 14;
        }

        private static void FitTextHeight(TMP_Text text, float minimum)
        {
            if (text == null)
                return;
            LayoutElement layout = text.GetComponent<LayoutElement>();
            if (layout != null)
                layout.preferredHeight = Mathf.Max(minimum, text.preferredHeight + 8f);
        }

        private static GameObject CreateUi(string name, Transform parent, params Type[] components)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            foreach (Type component in components) go.AddComponent(component);
            return go;
        }
    }
}
