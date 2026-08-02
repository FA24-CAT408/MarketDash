using System;
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
        private Text _status;
        private Text _diagnostics;
        private bool _showDiagnostics;

        private void Start()
        {
            controller ??= TestCampusController.Instance;
            Build();
        }

        private void Update()
        {
            if (_status != null && controller != null)
                _status.text = $"CURRENT: {controller.CurrentZone}   LOADED: {controller.RegisteredZoneCount}/7   F1: PANEL";
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f1Key.wasPressedThisFrame && _panel != null) _panel.SetActive(!_panel.activeSelf);
            if (keyboard.f2Key.wasPressedThisFrame) controller?.ResetZone(controller.CurrentZone);
            if (keyboard.f3Key.wasPressedThisFrame) controller?.ReturnToHub();
            if (_showDiagnostics && _diagnostics != null) RefreshDiagnostics();
        }

        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
            _panel = CreateUi("Campus Panel", transform, typeof(Image), typeof(VerticalLayoutGroup));
            RectTransform rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.15f);
            rect.anchorMax = new Vector2(0.25f, 0.95f);
            rect.offsetMin = new Vector2(12f, 0f);
            rect.offsetMax = new Vector2(-6f, 0f);
            _panel.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.08f, 0.9f);
            VerticalLayoutGroup layout = _panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 5f;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            CreateText("CRAZYMARKET TEST CAMPUS", 18, FontStyle.Bold);
            _status = CreateText("Initializing...", 12, FontStyle.Normal);
            foreach (TestZoneId zone in Enum.GetValues(typeof(TestZoneId)))
                CreateButton(zone.ToString(), () => controller?.TeleportToZone(zone));
            CreateButton("RESET CURRENT (F2)", () => controller?.ResetZone(controller.CurrentZone));
            CreateButton("RESET CAMPUS", () => controller?.ResetCampus());
            CreateButton("RETURN TO HUB (F3)", () => controller?.ReturnToHub());
            CreateButton("LOW PRESET", () => controller?.ApplyPreset("Low"));
            CreateButton("NORMAL PRESET", () => controller?.ApplyPreset("Normal"));
            CreateButton("STRESS PRESET", () => controller?.ApplyPreset("Stress"));
            CreateButton("TOGGLE DIAGNOSTICS", () =>
            {
                _showDiagnostics = !_showDiagnostics;
                _diagnostics.gameObject.SetActive(_showDiagnostics);
                RefreshDiagnostics();
            });
            _diagnostics = CreateText("", 11, FontStyle.Normal);
            _diagnostics.gameObject.SetActive(false);
        }

        private void RefreshDiagnostics()
        {
            if (_diagnostics == null || controller == null) return;
            StringBuilder text = new();
            foreach (TestZoneRoot zone in controller.RegisteredZones.Values)
                foreach (TestDiagnostic diagnostic in zone.GetDiagnostics())
                    text.AppendLine($"{zone.ZoneId}: {diagnostic.Label} = {diagnostic.Value}");
            _diagnostics.text = text.ToString();
        }

        private Text CreateText(string value, int size, FontStyle style)
        {
            GameObject go = CreateUi(value, _panel.transform, typeof(Text), typeof(LayoutElement));
            Text text = go.GetComponent<Text>();
            text.text = value;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            go.GetComponent<LayoutElement>().preferredHeight = size + 12;
            return text;
        }

        private void CreateButton(string label, UnityEngine.Events.UnityAction action)
        {
            GameObject go = CreateUi(label, _panel.transform, typeof(Image), typeof(Button), typeof(LayoutElement));
            go.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.28f, 1f);
            go.GetComponent<Button>().onClick.AddListener(action);
            go.GetComponent<LayoutElement>().preferredHeight = 28;
            GameObject textObject = CreateUi("Label", go.transform, typeof(Text));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            Text text = textObject.GetComponent<Text>();
            text.text = label; text.color = Color.white; text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 12;
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
