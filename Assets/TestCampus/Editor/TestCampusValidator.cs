using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

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
                if (Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0)
                    errors.Add($"{scene.name} must contain at least one TextMesh Pro label.");
                if (Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 0)
                    errors.Add($"{scene.name} contains legacy TextMesh components; use TextMesh Pro.");
                if (Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 0)
                    errors.Add($"{scene.name} contains legacy uGUI Text components; use TextMeshProUGUI.");
                if (id != TestZoneId.Hub)
                {
                    if (Object.FindAnyObjectByType<TestCampusController>() != null) errors.Add($"{scene.name} owns a forbidden TestCampusController.");
                    if (Object.FindAnyObjectByType<EventSystem>() != null) errors.Add($"{scene.name} owns a forbidden EventSystem.");
                    if (Object.FindAnyObjectByType<CinemachineBrain>() != null) errors.Add($"{scene.name} owns a forbidden CinemachineBrain.");
                }
                else
                {
                    if (Object.FindObjectsByType<CinemachineBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 1)
                        errors.Add($"{scene.name} must contain exactly one CinemachineBrain.");
                    if (Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0)
                        errors.Add($"{scene.name} must contain a CinemachineCamera.");
                }
            }
            if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
            return errors;
        }
    }
}
