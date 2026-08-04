using System.Collections.Generic;
using UnityEngine;

namespace CrazyMarket.TestCampus
{
    public sealed class TestZonePresetProvider : MonoBehaviour, ITestPresetProvider, ITestDiagnosticsProvider, ITestResettable
    {
        private static readonly string[] Presets = { "Low", "Normal", "Stress" };
        [SerializeField] private string activePreset = "Normal";
        private string _initialPreset;

        public IReadOnlyList<string> PresetIds => Presets;
        public string ActivePreset => activePreset;

        public bool ApplyPreset(string presetId)
        {
            if (System.Array.IndexOf(Presets, presetId) < 0) return false;
            activePreset = presetId;
            return true;
        }

        public IEnumerable<TestDiagnostic> GetDiagnostics()
        {
            yield return new TestDiagnostic("Preset", activePreset, TestDiagnosticStatus.Pass);
            yield return new TestDiagnostic("Active fixtures", transform.childCount.ToString());
        }

        public void CaptureInitialState() => _initialPreset = activePreset;
        public void ResetToInitialState() => activePreset = string.IsNullOrEmpty(_initialPreset) ? "Normal" : _initialPreset;
    }
}
