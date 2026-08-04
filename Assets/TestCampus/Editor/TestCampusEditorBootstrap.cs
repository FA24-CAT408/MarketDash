using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrazyMarket.TestCampus.Editor
{
    [InitializeOnLoad]
    public static class TestCampusEditorBootstrap
    {
        static TestCampusEditorBootstrap() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            if (Object.FindAnyObjectByType<TestCampusController>() != null) return;
            if (Object.FindAnyObjectByType<TestZoneRoot>() == null) return;
            string corePath = $"{TestCampusSceneGenerator.SceneFolder}/TestCampus_Core.unity";
            Scene core = SceneManager.GetSceneByPath(corePath);
            if (!core.IsValid() || !core.isLoaded)
                EditorSceneManager.OpenScene(corePath, OpenSceneMode.Additive);
        }
    }
}
