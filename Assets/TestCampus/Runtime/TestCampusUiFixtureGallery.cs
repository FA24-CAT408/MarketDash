using System.Collections.Generic;
using UnityEngine;

namespace CrazyMarket.TestCampus
{
    /// <summary>Controls visual-only production UI fixtures without duplicating their gameplay logic.</summary>
    [DisallowMultipleComponent]
    public sealed class TestCampusUiFixtureGallery : MonoBehaviour, ITestPresetProvider, ITestResettable, ITestDiagnosticsProvider
    {
        private static readonly string[] Presets = { "Low", "Normal", "Stress" };

        [SerializeField] private GameObject gameplayHud;
        [SerializeField] private GameObject pauseOverlay;

        private bool _initialHudActive;
        private bool _initialPauseActive;

        public IReadOnlyList<string> PresetIds => Presets;

        public void Configure(GameObject hud, GameObject pause)
        {
            gameplayHud = hud;
            pauseOverlay = pause;
            SetState(false, false);
        }

        public void CaptureInitialState()
        {
            _initialHudActive = gameplayHud != null && gameplayHud.activeSelf;
            _initialPauseActive = pauseOverlay != null && pauseOverlay.activeSelf;
        }

        public void ResetToInitialState() => SetState(_initialHudActive, _initialPauseActive);

        public bool ApplyPreset(string presetId)
        {
            switch (presetId)
            {
                case "Low":
                    SetState(false, false);
                    return true;
                case "Normal":
                    SetState(true, false);
                    return true;
                case "Stress":
                    SetState(true, true);
                    return true;
                default:
                    return false;
            }
        }

        public IEnumerable<TestDiagnostic> GetDiagnostics()
        {
            yield return new TestDiagnostic("Production HUD", IsActive(gameplayHud) ? "Visible" : "Hidden");
            yield return new TestDiagnostic("Production Pause Overlay", IsActive(pauseOverlay) ? "Visible" : "Hidden");
        }

        private void SetState(bool showHud, bool showPause)
        {
            if (gameplayHud != null) gameplayHud.SetActive(showHud);
            if (pauseOverlay != null) pauseOverlay.SetActive(showPause);
        }

        private static bool IsActive(GameObject fixture) => fixture != null && fixture.activeSelf;
    }
}
