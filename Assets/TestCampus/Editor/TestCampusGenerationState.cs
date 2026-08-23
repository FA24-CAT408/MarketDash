using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CrazyMarket.TestCampus.Editor
{
    internal static class TestCampusGenerationState
    {
        private const string StatePath = "ProjectSettings/TestCampusGenerationState.json";
        private const int Version = 1;

        private static readonly string[] GeneratedSceneNames =
        {
            "TestCampus_Core.unity",
            "TestCampus_Movement.unity",
            "TestCampus_Camera.unity",
            "TestCampus_Lighting.unity",
            "TestCampus_NPCInteraction.unity",
            "TestCampus_UI.unity",
            "TestCampus_Integration.unity"
        };

        private static readonly string[] GeneratedMaterialNames =
        {
            "Neutral", "Grid", "Movement", "Camera", "Lighting", "NPC", "UI", "Integration",
            "Accent", "Glossy", "Bright", "Dark", "Wall", "Ceiling", "LightFixture", "Hub"
        };

        private static readonly string[] SourceAssetPaths =
        {
            "Assets/Prefabs/Player/KCC Player Controller.prefab",
            "Assets/Prefabs/Level Components/Managers/Game Manager.prefab",
            "Assets/Prefabs/Level Components/Managers/Timer Manager.prefab",
            "Assets/Prefabs/Level Components/Managers/Debug Controller.prefab",
            "Assets/Prefabs/Level Components/UI/EventSystem.prefab",
            "Assets/Prefabs/Environment/Moving Platform.prefab",
            "Assets/Prefabs/NPC.prefab",
            "Assets/Prefabs/Items/Apple.prefab",
            "Assets/Prefabs/Level Components/UI/In - Game Canvas.prefab",
            "Assets/Prefabs/UI/Pause Canvas.prefab"
        };

        public static bool IsCurrent()
        {
            State state = Read();
            if (state == null || state.Version != Version || state.InputHash != CalculateInputHash())
                return false;
            return state.OutputHash == CalculateOutputHash();
        }

        public static void Record()
        {
            State state = new()
            {
                Version = Version,
                InputHash = CalculateInputHash(),
                OutputHash = CalculateOutputHash()
            };
            string json = JsonUtility.ToJson(state, true) + System.Environment.NewLine;
            if (!File.Exists(StatePath) || File.ReadAllText(StatePath) != json)
                File.WriteAllText(StatePath, json);
        }

        private static State Read()
        {
            if (!File.Exists(StatePath))
                return null;
            try
            {
                return JsonUtility.FromJson<State>(File.ReadAllText(StatePath));
            }
            catch (System.Exception exception) when (
                exception is IOException || exception is System.ArgumentException)
            {
                return null;
            }
        }

        private static string CalculateInputHash()
        {
            HashSet<string> inputs = new(System.StringComparer.Ordinal);
            AddDirectoryFiles(inputs, "Assets/TestCampus/Editor");
            AddDirectoryFiles(inputs, "Assets/TestCampus/Runtime");
            AddDirectoryFiles(inputs, TestCampusSceneGenerator.UiFolder);
            foreach (string sourceAssetPath in SourceAssetPaths)
                AddInputWithMeta(inputs, sourceAssetPath);

            foreach (string scenePath in ExistingGeneratedScenePaths())
            {
                foreach (string dependency in AssetDatabase.GetDependencies(scenePath, true))
                    AddInputWithMeta(inputs, dependency);
            }

            AddInputWithMeta(inputs, "ProjectSettings/ProjectVersion.txt");
            AddInputWithMeta(inputs, "ProjectSettings/TagManager.asset");
            AddInputWithMeta(inputs, "Packages/manifest.json");
            AddInputWithMeta(inputs, "Packages/packages-lock.json");

            List<string> orderedInputs = new(inputs);
            orderedInputs.Sort(System.StringComparer.Ordinal);
            System.Text.StringBuilder manifest = new();
            manifest.Append("version:").Append(Version).Append('\n');
            foreach (string path in orderedInputs)
            {
                manifest.Append(path).Append(':');
                manifest.Append(File.Exists(path) ? HashFile(path) : "missing").Append('\n');
            }
            return HashBytes(System.Text.Encoding.UTF8.GetBytes(manifest.ToString()));
        }

        private static string CalculateOutputHash()
        {
            List<string> paths = new();
            foreach (string scenePath in ExistingGeneratedScenePaths())
                AddExistingFileWithMeta(paths, scenePath);
            foreach (string materialName in GeneratedMaterialNames)
                AddExistingFileWithMeta(paths, $"{TestCampusSceneGenerator.MaterialFolder}/TC_{materialName}.mat");
            AddExistingFileWithMeta(paths, TestCampusSceneGenerator.UiPanelSettingsPath);
            paths.Sort(System.StringComparer.Ordinal);

            System.Text.StringBuilder manifest = new();
            foreach (string path in paths)
                manifest.Append(path).Append(':').Append(HashFile(path)).Append('\n');
            return HashBytes(System.Text.Encoding.UTF8.GetBytes(manifest.ToString()));
        }

        private static List<string> ExistingGeneratedScenePaths()
        {
            List<string> paths = new();
            foreach (string sceneName in GeneratedSceneNames)
            {
                string path = $"{TestCampusSceneGenerator.SceneFolder}/{sceneName}";
                if (File.Exists(path))
                    paths.Add(path);
            }
            return paths;
        }

        private static void AddDirectoryFiles(HashSet<string> paths, string directory)
        {
            if (!Directory.Exists(directory))
                return;
            foreach (string path in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                AddInputWithMeta(paths, path);
        }

        private static void AddInputWithMeta(HashSet<string> paths, string path)
        {
            path = path.Replace('\\', '/');
            if (IsGeneratedOutput(path))
                return;
            paths.Add(path);
            if (!path.EndsWith(".meta", System.StringComparison.Ordinal))
                paths.Add(path + ".meta");
        }

        private static bool IsGeneratedOutput(string path)
        {
            if (path == TestCampusSceneGenerator.UiPanelSettingsPath ||
                path == TestCampusSceneGenerator.UiPanelSettingsPath + ".meta")
            {
                return true;
            }
            if (path.StartsWith(TestCampusSceneGenerator.MaterialFolder + "/", System.StringComparison.Ordinal))
                return true;
            foreach (string sceneName in GeneratedSceneNames)
            {
                string scenePath = $"{TestCampusSceneGenerator.SceneFolder}/{sceneName}";
                if (path == scenePath || path == scenePath + ".meta")
                    return true;
            }
            return false;
        }

        private static void AddExistingFileWithMeta(List<string> paths, string path)
        {
            if (File.Exists(path))
                paths.Add(path);
            if (File.Exists(path + ".meta"))
                paths.Add(path + ".meta");
        }

        private static string HashFile(string path) => HashBytes(File.ReadAllBytes(path));

        private static string HashBytes(byte[] bytes)
        {
            using System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
            return System.BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", string.Empty);
        }

        [System.Serializable]
        private sealed class State
        {
            public int Version;
            public string InputHash;
            public string OutputHash;
        }
    }
}
