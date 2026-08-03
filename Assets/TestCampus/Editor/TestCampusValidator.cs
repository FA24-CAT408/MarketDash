using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using UnityEngine.UIElements;

namespace CrazyMarket.TestCampus.Editor
{
    public static class TestCampusValidator
    {
        [MenuItem("CrazyMarket/Test Campus/Validate")]
        public static void ValidateMenu()
        {
            IReadOnlyList<string> errors = Validate();
            if (errors.Count == 0) Debug.Log("Test Campus validation passed: available stack scenes have unique zone roots and matching Core configuration.");
            else foreach (string error in errors) Debug.LogError(error);
        }

        public static IReadOnlyList<string> Validate()
        {
            List<string> errors = new();
            HashSet<TestZoneId> ids = new();
            HashSet<TestZoneId> available = new();
            foreach (TestZoneId id in System.Enum.GetValues(typeof(TestZoneId)))
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath(id)) != null)
                    available.Add(id);
            if (!available.Contains(TestZoneId.Hub))
                errors.Add($"Missing scene: {ScenePath(TestZoneId.Hub)}");

            string original = SceneManager.GetActiveScene().path;
            foreach (TestZoneId id in available)
            {
                string path = ScenePath(id);
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                TestZoneRoot[] roots = Object.FindObjectsByType<TestZoneRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (roots.Length != 1) errors.Add($"{scene.name} must contain exactly one TestZoneRoot; found {roots.Length}.");
                else
                {
                    if (roots[0].ZoneId != id)
                        errors.Add($"{scene.name} has zone identifier {roots[0].ZoneId}; expected {id}.");
                    if (!ids.Add(roots[0].ZoneId))
                        errors.Add($"Duplicate zone identifier: {roots[0].ZoneId}.");
                }
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
                    if (id == TestZoneId.Integration)
                    {
                        if (Object.FindObjectsByType<TestCampusIntegrationScenario>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 1)
                            errors.Add($"{scene.name} must contain exactly one TestCampusIntegrationScenario.");
                        if (GameObject.Find("Integration Production Moving Platform") == null)
                            errors.Add($"{scene.name} must contain its production moving platform inside the route.");
                        if (GameObject.Find("Integration Route Apple") == null)
                            errors.Add($"{scene.name} must contain its production collectible interaction.");
                        if (Object.FindObjectsByType<TestCampusFixtureGuard>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 0)
                            errors.Add($"{scene.name} contains obsolete TestCampusFixtureGuard components; regenerate it.");
                    }
                }
                else
                {
                    TestCampusController controller = Object.FindAnyObjectByType<TestCampusController>();
                    if (controller == null)
                        errors.Add($"{scene.name} must contain a TestCampusController.");
                    else
                    {
                        HashSet<TestZoneId> configured = new();
                        foreach (TestZoneScene configuredScene in controller.ZoneScenes)
                        {
                            configured.Add(configuredScene.Zone);
                            if (!available.Contains(configuredScene.Zone))
                                errors.Add($"{scene.name} references missing scene for {configuredScene.Zone}.");
                        }
                        foreach (TestZoneId availableId in available)
                            if (availableId != TestZoneId.Hub && !configured.Contains(availableId))
                                errors.Add($"{scene.name} does not configure available scene {availableId}.");
                    }
                    if (Object.FindObjectsByType<CinemachineBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 1)
                        errors.Add($"{scene.name} must contain exactly one CinemachineBrain.");
                    if (Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0)
                        errors.Add($"{scene.name} must contain a CinemachineCamera.");
                    UIDocument[] documents = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (documents.Length != 1)
                        errors.Add($"{scene.name} must contain exactly one Test Campus UIDocument; found {documents.Length}.");
                    else
                    {
                        if (documents[0].panelSettings == null)
                            errors.Add($"{scene.name} UIDocument has no PanelSettings.");
                        if (documents[0].visualTreeAsset == null)
                            errors.Add($"{scene.name} UIDocument has no UI Toolkit visual tree.");
                        if (documents[0].GetComponent<TestCampusControlPanel>() == null)
                            errors.Add($"{scene.name} UIDocument must be owned by TestCampusControlPanel.");
                    }
                    if (Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 1)
                        errors.Add($"{scene.name} must retain exactly one shared EventSystem for production UI fixtures.");
                }
            }
            if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
            return errors;
        }

        private static string ScenePath(TestZoneId id)
        {
            string suffix = id switch
            {
                TestZoneId.Hub => "Core",
                TestZoneId.NPCInteraction => "NPCInteraction",
                _ => id.ToString()
            };
            return $"{TestCampusSceneGenerator.SceneFolder}/TestCampus_{suffix}.unity";
        }
    }
}
