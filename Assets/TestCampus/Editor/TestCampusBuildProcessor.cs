using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace CrazyMarket.TestCampus.Editor
{
    public sealed class TestCampusBuildProcessor : IPreprocessBuildWithReport, IProcessSceneWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if ((report.summary.options & BuildOptions.Development) != 0) return;

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && IsTestCampusScene(scene.path))
                {
                    throw new BuildFailedException(
                        "Release build cancelled because an enabled Test Campus scene is in Build Settings. " +
                        "Disable Test Campus scenes or make a Development Build. Build Settings were not changed.");
                }
            }
        }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null || (report.summary.options & BuildOptions.Development) != 0) return;
            if (IsTestCampusScene(scene.path))
                throw new BuildFailedException($"Release build cancelled because it includes Test Campus scene '{scene.path}'.");
        }

        private static bool IsTestCampusScene(string path) =>
            !string.IsNullOrEmpty(path) && path.StartsWith(TestCampusSceneGenerator.SceneFolder + "/");
    }
}
