#if UNITY_EDITOR
using System.Collections.Generic;
using CrazyMarket.Player.V2;
using CrazyMarket.Player.V2.Unity;
using CrazyMarket.TestCampus;
using KinematicCharacterController;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrazyMarket.Player.V2.Editor
{
    public static class PlayerV2VerticalSliceBuilder
    {
        private const string SourcePrefab = "Assets/Prefabs/Player/KCC Player Controller.prefab";
        private const string OutputPrefab = "Assets/Prefabs/Player/V2/Player Controller V2.prefab";
        private const string SourceScene = "Assets/TestCampus/Scenes/TestCampus_Core.unity";
        private const string OutputScene = "Assets/TestCampus/Scenes/TestCampus_Core_PlayerV2.unity";
        private const string AbilityPath = "Assets/Scripts/Player/V2/Generated/DoubleJumpAbilityV2.asset";
        private const string ProfilePath = "Assets/Scripts/Player/V2/Generated/PlayerProfileV2_Production.asset";
        private const string InputPath = "Assets/Scripts/Input/Input.asset";

        [MenuItem("Tools/CrazyMarket/Player V2/Build Vertical Slice")]
        public static void BuildVerticalSlice()
        {
            EnsureFolder("Assets/Scripts/Player/V2", "Generated");
            EnsureFolder("Assets/Prefabs/Player", "V2");

            DoubleJumpAbilityDefinition ability = BuildAbility();
            PlayerProfile profile = BuildProfile(ability);
            GameObject prefab = BuildPrefab(profile);
            BuildScene(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = prefab;
            Debug.Log("Built Player Controller V2 vertical slice assets and TestCampus scene copy.");
        }

        private static DoubleJumpAbilityDefinition BuildAbility()
        {
            DoubleJumpAbilityDefinition ability = AssetDatabase.LoadAssetAtPath<DoubleJumpAbilityDefinition>(AbilityPath);
            if (ability == null)
            {
                ability = ScriptableObject.CreateInstance<DoubleJumpAbilityDefinition>();
                AssetDatabase.CreateAsset(ability, AbilityPath);
            }
            return ability;
        }

        private static PlayerProfile BuildProfile(DoubleJumpAbilityDefinition ability)
        {
            PlayerProfile profile = AssetDatabase.LoadAssetAtPath<PlayerProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PlayerProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("profileId").stringValue = "Production";
            SerializedProperty data = serialized.FindProperty("data");
            SerializedProperty loadout = data.FindPropertyRelative("abilityLoadout");
            SerializedProperty ids = loadout.FindPropertyRelative("abilityIds");
            ids.arraySize = 1;
            ids.GetArrayElementAtIndex(0).enumValueIndex = (int)PlayerAbilityId.DoubleJump;
            SerializedProperty definitions = loadout.FindPropertyRelative("definitions");
            definitions.arraySize = 1;
            definitions.GetArrayElementAtIndex(0).objectReferenceValue = ability;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static GameObject BuildPrefab(PlayerProfile profile)
        {
            InputReader input = AssetDatabase.LoadAssetAtPath<InputReader>(InputPath);
            if (input == null)
                throw new BuildFailedException("Missing required InputReader asset at " + InputPath + ".");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab) == null)
                throw new BuildFailedException("Missing source KCC player prefab at " + SourcePrefab + ".");

            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(SourcePrefab);
                KCCPlayerController legacy = root.GetComponent<KCCPlayerController>();
                ParticleSystem legacyJumpParticles = legacy == null ? null : legacy.JumpParticles;
                List<Collider> legacyIgnoredColliders = legacy == null || legacy.IgnoredColliders == null
                    ? new List<Collider>()
                    : new List<Collider>(legacy.IgnoredColliders);

                if (legacy != null) Object.DestroyImmediate(legacy, true);
                TestCampusPlayerAdapter legacyAdapter = root.GetComponent<TestCampusPlayerAdapter>();
                if (legacyAdapter != null) Object.DestroyImmediate(legacyAdapter, true);

                PlayerControllerV2 controller = root.GetComponent<PlayerControllerV2>();
                if (controller == null) controller = root.AddComponent<PlayerControllerV2>();
                TestCampusPlayerV2Bridge bridge = root.GetComponent<TestCampusPlayerV2Bridge>();
                if (bridge == null) bridge = root.AddComponent<TestCampusPlayerV2Bridge>();
                PlayerAnimationPresenter presenter = root.GetComponent<PlayerAnimationPresenter>();
                if (presenter == null) presenter = root.AddComponent<PlayerAnimationPresenter>();
                SerializedObject serialized = new SerializedObject(controller);
                serialized.FindProperty("profile").objectReferenceValue = profile;
                serialized.FindProperty("input").objectReferenceValue = input;
                serialized.FindProperty("motor").objectReferenceValue = root.GetComponent<KinematicCharacterMotor>();
                serialized.FindProperty("jumpParticles").objectReferenceValue = legacyJumpParticles;
                SerializedProperty ignored = serialized.FindProperty("ignoredColliders");
                ignored.arraySize = legacyIgnoredColliders.Count;
                for (int i = 0; i < legacyIgnoredColliders.Count; i++)
                    ignored.GetArrayElementAtIndex(i).objectReferenceValue = legacyIgnoredColliders[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject integration = new SerializedObject(bridge);
                integration.FindProperty("controller").objectReferenceValue = controller;
                integration.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject presentation = new SerializedObject(presenter);
                presentation.FindProperty("controller").objectReferenceValue = controller;
                presentation.FindProperty("animator").objectReferenceValue = root.GetComponentInChildren<Animator>();
                presentation.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, OutputPrefab);
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildScene(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, OutputScene))
                throw new BuildFailedException("Could not create generated scene at " + OutputScene + ".");
            if (scene.path != OutputScene)
                throw new BuildFailedException("Generated scene path mismatch: expected " + OutputScene + ".");

            TestCampusController campus = Object.FindObjectOfType<TestCampusController>();
            if (campus == null || campus.PlayerRoot == null)
                throw new BuildFailedException("Generated TestCampus scene has no player root.");

            Transform oldPlayer = campus.PlayerRoot;
            Vector3 position = oldPlayer.position;
            Quaternion rotation = oldPlayer.rotation;
            string playerName = oldPlayer.name;
            GameObject replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            replacement.name = playerName;
            replacement.transform.SetPositionAndRotation(position, rotation);
            Undo.RegisterCreatedObjectUndo(replacement, "Create Player Controller V2 instance");
            campus.PlayerRoot = replacement.transform;
            Undo.DestroyObjectImmediate(oldPlayer.gameObject);
            EditorUtility.SetDirty(campus);
            EditorSceneManager.MarkSceneDirty(scene);
            if (scene.path != OutputScene || !EditorSceneManager.SaveScene(scene))
                throw new BuildFailedException("Refused to save generated scene outside " + OutputScene + ".");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
