using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrazyMarket.TestCampus
{
    public enum TestZoneId { Hub, Movement, Camera, Lighting, NPCInteraction, UI, Integration }
    public enum TestDiagnosticStatus { Info, Pass, Warning, Error }

    [Serializable]
    public readonly struct TestDiagnostic
    {
        public TestDiagnostic(string label, string value, TestDiagnosticStatus status = TestDiagnosticStatus.Info)
        {
            Label = label;
            Value = value;
            Status = status;
        }
        public string Label { get; }
        public string Value { get; }
        public TestDiagnosticStatus Status { get; }
    }

    public interface ITestResettable
    {
        void CaptureInitialState();
        void ResetToInitialState();
    }

    public interface ITestPresetProvider
    {
        IReadOnlyList<string> PresetIds { get; }
        bool ApplyPreset(string presetId);
    }

    public interface ITestDiagnosticsProvider
    {
        IEnumerable<TestDiagnostic> GetDiagnostics();
    }

    public interface ITestCampusPlayerController
    {
        void SetMovementEnabled(bool enabled);
        void SetMovementReference(Transform reference);
        bool TryGetMovementIntent(out Vector3 direction);
        void TeleportTo(Vector3 position, Quaternion rotation);
    }

    [Serializable]
    public sealed class TestZoneScene
    {
        public TestZoneId Zone;
        public string SceneName;
        public bool LoadByDefault = true;
    }
}
