using System.Collections.Generic;
using UnityEngine;

namespace CrazyMarket.TestCampus
{
    /// <summary>Owns the scalable production fixtures used by the Integration room.</summary>
    [DisallowMultipleComponent]
    public sealed class TestCampusIntegrationScenario : MonoBehaviour, ITestPresetProvider, ITestResettable, ITestDiagnosticsProvider
    {
        private static readonly string[] Presets = { "Low", "Normal", "Stress" };

        [SerializeField] private GameObject[] npcFixtures;
        [SerializeField] private GameObject movingPlatform;
        [SerializeField] private GameObject[] lightFixtures;
        [SerializeField] private GameObject collectible;
        [SerializeField] private string activePreset = "Normal";

        private bool[] _initialNpcStates;
        private bool[] _initialLightStates;
        private bool _initialPlatformState;
        private string _initialPreset;

        public IReadOnlyList<string> PresetIds => Presets;

        public void Configure(GameObject[] npcs, GameObject platform, GameObject[] roomLights, GameObject routeCollectible)
        {
            npcFixtures = npcs;
            movingPlatform = platform;
            lightFixtures = roomLights;
            collectible = routeCollectible;
            ApplyPreset("Normal");
        }

        public bool ApplyPreset(string presetId)
        {
            switch (presetId)
            {
                case "Low":
                    SetLoad(npcCount: 1, lightCount: 1, platformActive: false);
                    break;
                case "Normal":
                    SetLoad(npcCount: 2, lightCount: 2, platformActive: true);
                    break;
                case "Stress":
                    SetLoad(npcCount: npcFixtures?.Length ?? 0, lightCount: lightFixtures?.Length ?? 0, platformActive: true);
                    break;
                default:
                    return false;
            }

            activePreset = presetId;
            return true;
        }

        public void CaptureInitialState()
        {
            _initialNpcStates = CaptureStates(npcFixtures);
            _initialLightStates = CaptureStates(lightFixtures);
            _initialPlatformState = movingPlatform != null && movingPlatform.activeSelf;
            _initialPreset = activePreset;
        }

        public void ResetToInitialState()
        {
            RestoreStates(npcFixtures, _initialNpcStates);
            RestoreStates(lightFixtures, _initialLightStates);
            if (movingPlatform != null) movingPlatform.SetActive(_initialPlatformState);
            activePreset = string.IsNullOrEmpty(_initialPreset) ? "Normal" : _initialPreset;
        }

        public IEnumerable<TestDiagnostic> GetDiagnostics()
        {
            yield return new TestDiagnostic("Integration load", activePreset, TestDiagnosticStatus.Pass);
            yield return new TestDiagnostic("Active NPCs", CountActive(npcFixtures).ToString());
            yield return new TestDiagnostic("Active lights", CountActive(lightFixtures).ToString());
            yield return new TestDiagnostic("Moving platform", IsActive(movingPlatform) ? "Active" : "Disabled");
            yield return new TestDiagnostic("Route collectible", IsActive(collectible) ? "Available" : "Collected");
        }

        private void SetLoad(int npcCount, int lightCount, bool platformActive)
        {
            SetActiveCount(npcFixtures, npcCount);
            SetActiveCount(lightFixtures, lightCount);
            if (movingPlatform != null) movingPlatform.SetActive(platformActive);
        }

        private static bool[] CaptureStates(GameObject[] fixtures)
        {
            if (fixtures == null) return System.Array.Empty<bool>();
            bool[] states = new bool[fixtures.Length];
            for (int i = 0; i < fixtures.Length; i++) states[i] = IsActive(fixtures[i]);
            return states;
        }

        private static void RestoreStates(GameObject[] fixtures, bool[] states)
        {
            if (fixtures == null || states == null) return;
            for (int i = 0; i < fixtures.Length && i < states.Length; i++)
                if (fixtures[i] != null) fixtures[i].SetActive(states[i]);
        }

        private static void SetActiveCount(GameObject[] fixtures, int activeCount)
        {
            if (fixtures == null) return;
            for (int i = 0; i < fixtures.Length; i++)
                if (fixtures[i] != null) fixtures[i].SetActive(i < activeCount);
        }

        private static int CountActive(GameObject[] fixtures)
        {
            if (fixtures == null) return 0;
            int count = 0;
            foreach (GameObject fixture in fixtures)
                if (IsActive(fixture)) count++;
            return count;
        }

        private static bool IsActive(GameObject fixture) => fixture != null && fixture.activeSelf;
    }
}
