using System.Collections.Generic;
using UnityEngine;

namespace CrazyMarket.TestCampus
{
    [DisallowMultipleComponent]
    public sealed class TestZoneRoot : MonoBehaviour
    {
        [System.Serializable]
        private sealed class SpawnEntry
        {
            public string Id;
            public Transform Transform;
        }

        [SerializeField] private TestZoneId zoneId;
        [SerializeField] private string displayName = "Test Zone";
        [SerializeField] private Color accentColor = Color.white;
        [SerializeField, TextArea] private string instructions;
        [SerializeField] private List<SpawnEntry> spawns = new();

        private readonly List<ITestResettable> _resettables = new();
        private readonly List<ITestPresetProvider> _presetProviders = new();
        private readonly List<ITestDiagnosticsProvider> _diagnosticsProviders = new();
        private bool _captured;

        public TestZoneId ZoneId => zoneId;
        public string DisplayName => displayName;
        public Color AccentColor => accentColor;
        public string Instructions => instructions;
        public IReadOnlyList<ITestPresetProvider> PresetProviders => _presetProviders;

        private void OnEnable()
        {
            RefreshProviders();
            TestCampusController.Instance?.RegisterZone(this);
        }

        private void OnDisable()
        {
            TestCampusController.Instance?.UnregisterZone(this);
        }

        public void Configure(TestZoneId id, string zoneDisplayName, Color color, string zoneInstructions = "")
        {
            zoneId = id;
            displayName = zoneDisplayName;
            accentColor = color;
            instructions = zoneInstructions;
        }

        public void ConfigureSpawn(string id, Transform spawn)
        {
            SpawnEntry existing = spawns.Find(entry => entry.Id == id);
            if (existing == null) spawns.Add(new SpawnEntry { Id = id, Transform = spawn });
            else existing.Transform = spawn;
        }

        public Transform ResolveSpawn(string id)
        {
            SpawnEntry match = spawns.Find(entry => entry.Id == id && entry.Transform != null);
            if (match != null) return match.Transform;
            return spawns.Find(entry => entry.Id == "Default" && entry.Transform != null)?.Transform;
        }

        public void RefreshProviders()
        {
            _resettables.Clear();
            _presetProviders.Clear();
            _diagnosticsProviders.Clear();
            foreach (MonoBehaviour behaviour in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is ITestResettable resettable) _resettables.Add(resettable);
                if (behaviour is ITestPresetProvider preset) _presetProviders.Add(preset);
                if (behaviour is ITestDiagnosticsProvider diagnostics) _diagnosticsProviders.Add(diagnostics);
            }
            CaptureInitialState();
        }

        public void CaptureInitialState()
        {
            foreach (ITestResettable resettable in _resettables) resettable.CaptureInitialState();
            _captured = true;
        }

        public void ResetZone()
        {
            if (!_captured) CaptureInitialState();
            foreach (ITestResettable resettable in _resettables) resettable.ResetToInitialState();
        }

        public bool ApplyPreset(string presetId)
        {
            bool applied = false;
            foreach (ITestPresetProvider provider in _presetProviders) applied |= provider.ApplyPreset(presetId);
            return applied;
        }

        public IEnumerable<TestDiagnostic> GetDiagnostics()
        {
            foreach (ITestDiagnosticsProvider provider in _diagnosticsProviders)
                foreach (TestDiagnostic diagnostic in provider.GetDiagnostics())
                    yield return diagnostic;
        }
    }
}
