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
        private const string ObsoleteAbilityPath = "Assets/Scripts/Player/V2/Generated/DoubleJumpAbilityV2.asset";
        private const string ProfilePath = "Assets/Scripts/Player/V2/Generated/PlayerProfileV2_Production.asset";
        private const string InputPath = "Assets/Scripts/Input/Input.asset";

        [MenuItem("Tools/CrazyMarket/Player V2/Build Vertical Slice")]
        public static void BuildVerticalSlice()
        {
            EnsureFolder("Assets/Scripts/Player/V2", "Generated");
            EnsureFolder("Assets/Prefabs/Player", "V2");

            // The old ability definition was an unnecessary hidden loadout. The
            // replacement is the DoubleJumpAbility component on the prefab.
            AssetDatabase.DeleteAsset(ObsoleteAbilityPath);

            PlayerProfile profile = BuildProfileFromCurrentLegacyPrefab();
            GameObject prefab = BuildPrefab(profile);
            BuildScene(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = prefab;
            Debug.Log("Built Player Controller V2 vertical slice assets and TestCampus scene copy.");
        }

        private static PlayerProfile BuildProfileFromCurrentLegacyPrefab()
        {
            LocomotionTuning tuning = ReadLegacyLocomotionTuning();
            PlayerProfile profile = AssetDatabase.LoadAssetAtPath<PlayerProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PlayerProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("profileId").stringValue = "Production";
            SerializedProperty locomotion = serialized.FindProperty("data").FindPropertyRelative("locomotion");
            SetFloat(locomotion, "StableMoveSpeed", tuning.StableMoveSpeed);
            SetFloat(locomotion, "AirMoveSpeed", tuning.AirMoveSpeed);
            SetFloat(locomotion, "AirAcceleration", tuning.AirAcceleration);
            SetFloat(locomotion, "StableMovementSharpness", tuning.StableMovementSharpness);
            SetFloat(locomotion, "OrientationSharpness", tuning.OrientationSharpness);
            SetFloat(locomotion, "JumpSpeed", tuning.JumpSpeed);
            SetFloat(locomotion, "JumpBufferTime", tuning.JumpBufferTime);
            SetFloat(locomotion, "CoyoteTime", tuning.CoyoteTime);
            SetFloat(locomotion, "Gravity", tuning.Gravity);
            SetFloat(locomotion, "FallGravityMultiplier", tuning.FallGravityMultiplier);
            SetFloat(locomotion, "Drag", tuning.Drag);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static LocomotionTuning ReadLegacyLocomotionTuning()
        {
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(SourcePrefab);
                KCCPlayerController legacy = root.GetComponent<KCCPlayerController>();
                if (legacy == null)
                    throw new BuildFailedException("Missing legacy KCCPlayerController on " + SourcePrefab + ".");

                return new LocomotionTuning
                {
                    StableMoveSpeed = legacy.MaxStableMoveSpeed,
                    AirMoveSpeed = legacy.MaxAirMoveSpeed,
                    AirAcceleration = legacy.AirAccelerationSpeed,
                    StableMovementSharpness = legacy.StableMovementSharpness,
                    OrientationSharpness = legacy.OrientationSharpness,
                    JumpSpeed = legacy.JumpUpSpeed,
                    JumpBufferTime = legacy.JumpPreGroundingGraceTime,
                    CoyoteTime = legacy.CoyoteTimeDuration,
                    Gravity = legacy.Gravity.y,
                    FallGravityMultiplier = legacy.IncreasedGravityMultiplier,
                    Drag = legacy.Drag
                };
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
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
                // Always load the current legacy prefab. This preserves its current
                // KCC settings and child hierarchy, including intentional shadow edits.
                root = PrefabUtility.LoadPrefabContents(SourcePrefab);
                KCCPlayerController legacy = root.GetComponent<KCCPlayerController>();
                if (legacy == null)
                    throw new BuildFailedException("Missing legacy KCCPlayerController on " + SourcePrefab + ".");

                ParticleSystem legacyJumpParticles = legacy.JumpParticles;
                List<Collider> legacyIgnoredColliders = legacy.IgnoredColliders == null
                    ? new List<Collider>()
                    : new List<Collider>(legacy.IgnoredColliders);

                RemoveComponents<KCCPlayerController>(root);
                RemoveComponents<TestCampusPlayerAdapter>(root);
                RemoveComponents<TestCampusPlayerV2Bridge>(root);
                RemoveComponents<PlayerCollisionManager>(root);
                RemoveComponents<PlayerControllerV2>(root);
                RemoveComponents<DoubleJumpAbility>(root);
                RemoveComponents<PlayerAnimationPresenter>(root);

                KinematicCharacterMotor motor = root.GetComponent<KinematicCharacterMotor>();
                if (motor == null)
                    throw new BuildFailedException("The source player prefab has no KinematicCharacterMotor.");

                DoubleJumpAbility doubleJump = root.AddComponent<DoubleJumpAbility>();
                PlayerControllerV2 controller = root.AddComponent<PlayerControllerV2>();
                PlayerAnimationPresenter presenter = root.AddComponent<PlayerAnimationPresenter>();

                SerializedObject serialized = new SerializedObject(controller);
                serialized.FindProperty("profile").objectReferenceValue = profile;
                serialized.FindProperty("input").objectReferenceValue = input;
                serialized.FindProperty("motor").objectReferenceValue = motor;
                serialized.FindProperty("jumpParticles").objectReferenceValue = legacyJumpParticles;
                SerializedProperty ignored = serialized.FindProperty("ignoredColliders");
                ignored.arraySize = legacyIgnoredColliders.Count;
                for (int i = 0; i < legacyIgnoredColliders.Count; i++)
                    ignored.GetArrayElementAtIndex(i).objectReferenceValue = legacyIgnoredColliders[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject presentation = new SerializedObject(presenter);
                presentation.FindProperty("controller").objectReferenceValue = controller;
                presentation.FindProperty("animator").objectReferenceValue = root.GetComponentInChildren<Animator>();
                presentation.ApplyModifiedPropertiesWithoutUndo();

                // Keep the ability component as a visible, editable prefab component.
                // Its defaults are intentionally serialized by Unity, not hidden here.
                EditorUtility.SetDirty(doubleJump);
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

            // Test Campus behavior belongs to the scene integration, not the
            // reusable player prefab. This also leaves the canonical prefab with
            // exactly its three production behavior components.
            TestCampusPlayerV2Bridge bridge = replacement.GetComponent<TestCampusPlayerV2Bridge>();
            if (bridge == null) bridge = replacement.AddComponent<TestCampusPlayerV2Bridge>();
            PlayerControllerV2 controller = replacement.GetComponent<PlayerControllerV2>();
            SerializedObject integration = new SerializedObject(bridge);
            integration.FindProperty("controller").objectReferenceValue = controller;
            integration.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(replacement, "Create Player Controller V2 instance");
            campus.PlayerRoot = replacement.transform;
            Undo.DestroyObjectImmediate(oldPlayer.gameObject);
            EditorUtility.SetDirty(campus);
            EditorSceneManager.MarkSceneDirty(scene);
            if (scene.path != OutputScene || !EditorSceneManager.SaveScene(scene))
                throw new BuildFailedException("Refused to save generated scene outside " + OutputScene + ".");
        }

        private static void RemoveComponents<T>(GameObject root) where T : Component
        {
            foreach (T component in root.GetComponentsInChildren<T>(true))
                Object.DestroyImmediate(component, true);
        }

        private static void SetFloat(SerializedProperty parent, string name, float value)
        {
            SerializedProperty property = parent.FindPropertyRelative(name);
            if (property == null)
                throw new BuildFailedException("PlayerProfile is missing locomotion field '" + name + "'.");
            property.floatValue = value;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
