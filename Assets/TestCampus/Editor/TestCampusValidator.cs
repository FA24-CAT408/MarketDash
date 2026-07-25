using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CrazyMarket.TestCampus.Editor
{
    public static class TestCampusValidator
    {
        [MenuItem("CrazyMarket/Test Campus/Validate")]
        public static void ValidateMenu()
        {
            IReadOnlyList<string> errors = Validate();
            if (errors.Count == 0) Debug.Log("Test Campus validation passed: seven scenes and unique zone roots.");
            else foreach (string error in errors) Debug.LogError(error);
        }

        public static IReadOnlyList<string> Validate()
        {
            List<string> errors = new();
            HashSet<TestZoneId> ids = new();
            string original = SceneManager.GetActiveScene().path;
            foreach (TestZoneId id in System.Enum.GetValues(typeof(TestZoneId)))
            {
                string suffix = id switch
                {
                    TestZoneId.Hub => "Core",
                    TestZoneId.NPCInteraction => "NPCInteraction",
                    _ => id.ToString()
                };
                string path = $"{TestCampusSceneGenerator.SceneFolder}/TestCampus_{suffix}.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) { errors.Add($"Missing scene: {path}"); continue; }
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                TestZoneRoot[] roots = Object.FindObjectsByType<TestZoneRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (roots.Length != 1) errors.Add($"{scene.name} must contain exactly one TestZoneRoot; found {roots.Length}.");
                else if (!ids.Add(roots[0].ZoneId)) errors.Add($"Duplicate zone identifier: {roots[0].ZoneId}.");
                if (id != TestZoneId.Hub)
                {
                    if (Object.FindAnyObjectByType<TestCampusController>() != null) errors.Add($"{scene.name} owns a forbidden TestCampusController.");
                    if (Object.FindAnyObjectByType<EventSystem>() != null) errors.Add($"{scene.name} owns a forbidden EventSystem.");
                }
            }
            if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
            return errors;
        }
    }
}
