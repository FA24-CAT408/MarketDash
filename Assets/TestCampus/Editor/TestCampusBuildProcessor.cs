using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace CrazyMarket.TestCampus.Editor
{
    public sealed class TestCampusBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static EditorBuildSettingsScene[] _originalScenes;
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            _originalScenes = EditorBuildSettings.scenes;
            if ((report.summary.options & BuildOptions.Development) != 0) return;
            List<EditorBuildSettingsScene> releaseScenes = new();
            foreach (EditorBuildSettingsScene scene in _originalScenes)
                if (!scene.path.StartsWith(TestCampusSceneGenerator.SceneFolder))
                    releaseScenes.Add(scene);
            EditorBuildSettings.scenes = releaseScenes.ToArray();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (_originalScenes != null) EditorBuildSettings.scenes = _originalScenes;
            _originalScenes = null;
        }
    }
}
