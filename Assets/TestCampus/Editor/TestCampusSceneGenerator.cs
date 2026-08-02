using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CrazyMarket.TestCampus.Editor
{
    public static class TestCampusSceneGenerator
    {
        public const string SceneFolder = "Assets/TestCampus/Scenes";
        public const string MaterialFolder = "Assets/TestCampus/Materials";
        private static readonly Dictionary<string, Material> Materials = new();
        private static Transform _activeZoneRoot;

        [MenuItem("CrazyMarket/Test Campus/Build All Scenes")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(SceneFolder);
            Directory.CreateDirectory(MaterialFolder);
            CreateMaterials();
            CreateCore();
            CreateMovement();
            CreateCamera();
            CreateLighting();
            CreateNpcInteraction();
            CreateUi();
            CreateIntegration();
            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Test Campus: seven scenes generated and added to development build settings.");
        }

        [MenuItem("CrazyMarket/Test Campus/Open Core")]
        public static void OpenCore() => EditorSceneManager.OpenScene($"{SceneFolder}/TestCampus_Core.unity");

        public static void CaptureOverview()
        {
            EditorSceneManager.OpenScene($"{SceneFolder}/TestCampus_Core.unity", OpenSceneMode.Single);
            foreach (string suffix in new[] { "Movement", "Camera", "Lighting", "NPCInteraction", "UI", "Integration" })
                EditorSceneManager.OpenScene($"{SceneFolder}/TestCampus_{suffix}.unity", OpenSceneMode.Additive);
            Camera camera = Object.FindAnyObjectByType<Camera>();
            camera.transform.position = new Vector3(0, 165, -145);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, -25) - camera.transform.position, Vector3.up);
            camera.fieldOfView = 58f;
            const int width = 1600;
            const int height = 1000;
            RenderTexture target = new(width, height, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            Texture2D image = new(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Temp/TestCampus");
            File.WriteAllBytes("Temp/TestCampus/CampusOverview.png", image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(image);
            Debug.Log("Test Campus overview captured at Temp/TestCampus/CampusOverview.png");
        }

        private static void CreateCore()
        {
            Scene scene = NewScene("TestCampus_Core");
            GameObject root = new("=== TEST CAMPUS CORE ===");
            TestCampusController controller = root.AddComponent<TestCampusController>();
            controller.AutoLoadDefaultZones = true;
            TestZoneRoot hub = root.AddComponent<TestZoneRoot>();
            hub.Configure(TestZoneId.Hub, "Core Control Hub", new Color(0.2f, 0.8f, 1f),
                "Navigate physically or use F1. F2 resets the current zone. F3 returns to hub.");
            _activeZoneRoot = hub.transform;
            CreateSpawn(hub, Vector3.zero + Vector3.up, "Default");
            Cube("Hub Floor", new Vector3(0, -0.5f, 0), new Vector3(40, 1, 40), "Neutral");
            for (int i = -20; i <= 20; i += 5)
            {
                Cube($"Grid X {i}", new Vector3(i, 0.01f, 0), new Vector3(0.08f, 0.02f, 40), "Grid");
                Cube($"Grid Z {i}", new Vector3(0, 0.01f, i), new Vector3(40, 0.02f, 0.08f), "Grid");
            }
            CreateWalkway(new Vector3(-45, 0, 15), new Vector3(50, 0.3f, 6));
            CreateWalkway(new Vector3(45, 0, 15), new Vector3(50, 0.3f, 6));
            CreateWalkway(new Vector3(0, 0, 45), new Vector3(6, 0.3f, 50));
            CreateWalkway(new Vector3(-40, 0, -35), new Vector3(6, 0.3f, 60), 45);
            CreateWalkway(new Vector3(40, 0, -35), new Vector3(6, 0.3f, 60), -45);
            CreateWalkway(new Vector3(0, 0, -55), new Vector3(6, 0.3f, 75));
            Label("CRAZYMARKET SYSTEMS TEST CAMPUS", new Vector3(0, 4, 12), Color.cyan, 0.7f);
            foreach (TestZoneId id in System.Enum.GetValues(typeof(TestZoneId)))
                if (id != TestZoneId.Hub)
                    controller.ZoneScenes.Add(new TestZoneScene { Zone = id, SceneName = $"TestCampus_{SceneSuffix(id)}", LoadByDefault = true });
            _activeZoneRoot = null;
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/KCC Player Controller.prefab");
            GameObject player = playerPrefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene) : Capsule("Test Player", Vector3.up);
            player.transform.position = Vector3.up;
            controller.PlayerRoot = player.transform;
            InstantiatePrefab("Assets/Prefabs/Level Components/Managers/Game Manager.prefab", scene);
            InstantiatePrefab("Assets/Prefabs/Level Components/Managers/Camera Manager.prefab", scene);
            InstantiatePrefab("Assets/Prefabs/Level Components/Managers/Timer Manager.prefab", scene);
            InstantiatePrefab("Assets/Prefabs/Level Components/Managers/Debug Controller.prefab", scene);
            GameObject eventPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Level Components/UI/EventSystem.prefab");
            if (eventPrefab != null) PrefabUtility.InstantiatePrefab(eventPrefab, scene);
            else new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            new GameObject("Test Campus Control Panel").AddComponent<TestCampusControlPanel>();
            GameObject camera = new("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.SetPositionAndRotation(new Vector3(0, 18, -22), Quaternion.Euler(30, 0, 0));
            CreateDirectionalLight();
            Save(scene);
        }

        private static void CreateMovement()
        {
            Scene scene = NewZone(TestZoneId.Movement, "Movement Gym", new Vector3(-75, 0, 20), new Vector3(45, 1, 70), "Movement",
                "Measure acceleration, stopping, slopes, steps, jumps, coyote time, moving platforms, beams, falls, and respawn.");
            for (int z = -10; z <= 45; z += 5) Cube($"Distance {z + 10}m", new Vector3(-82, 0.05f, z), new Vector3(8, 0.1f, 0.15f), "Grid");
            for (int i = 0; i < 5; i++) Cube($"Step {i + 1}", new Vector3(-68 + i * 2, i * 0.25f, 4), new Vector3(2, 0.5f + i * 0.5f, 5), "Movement");
            Cube("Slope 15 degrees", new Vector3(-80, 1.5f, 25), new Vector3(10, 0.5f, 12), "Movement", Quaternion.Euler(15, 0, 0));
            Cube("Slope 30 degrees", new Vector3(-67, 3, 25), new Vector3(10, 0.5f, 12), "Movement", Quaternion.Euler(30, 0, 0));
            for (int i = 0; i < 6; i++) Cube($"Jump Target {i + 1}", new Vector3(-84 + i * 4, 1 + i * 0.4f, 43), new Vector3(2.5f, 0.4f, 2.5f), "Movement");
            Cube("Narrow Beam", new Vector3(-72, 2, -2), new Vector3(1, 0.4f, 16), "Movement");
            Cube("Low Ceiling", new Vector3(-62, 2.5f, -5), new Vector3(8, 0.4f, 12), "Movement");
            Save(scene);
        }

        private static void CreateCamera()
        {
            Scene scene = NewZone(TestZoneId.Camera, "Camera Course", new Vector3(75, 0, 20), new Vector3(40, 1, 65), "Camera",
                "Walk the marked route to inspect follow composition, obstructions, corridors, height changes, framing, and trigger transitions.");
            for (int i = 0; i < 7; i++)
            {
                Cube($"Corridor Left {i}", new Vector3(68, 2, -5 + i * 7), new Vector3(1, 4, 6), "Camera");
                Cube($"Corridor Right {i}", new Vector3(82, 2, -5 + i * 7), new Vector3(1, 4, 6), "Camera");
            }
            for (int i = 0; i < 5; i++) Cube($"Height Target {i}", new Vector3(60 + i * 7, 1 + i, 42), new Vector3(4, 2 + i * 2, 4), "Camera");
            Sphere("Look At Target", new Vector3(75, 6, 48), new Vector3(3, 3, 3), "Accent");
            Save(scene);
        }

        private static void CreateLighting()
        {
            Scene scene = NewZone(TestZoneId.Lighting, "Lighting and Shading Gallery", new Vector3(0, 0, 70), new Vector3(50, 1, 60), "Lighting",
                "Compare identical forms under directional, point, spot, warm, cool, bright, dark, emissive, and high-contrast conditions.");
            for (int bay = 0; bay < 5; bay++)
            {
                float x = -20 + bay * 10;
                Cube($"Bay {bay} Wall", new Vector3(x, 3, 78), new Vector3(8, 6, 0.5f), "Neutral");
                Sphere($"Reference Sphere {bay}", new Vector3(x, 1.5f, 70), Vector3.one * 3, bay % 2 == 0 ? "Glossy" : "Neutral");
                Cube($"Reference Cube {bay}", new Vector3(x, 1.5f, 65), Vector3.one * 3, "Neutral");
                GameObject lightObject = new($"Bay Light {bay}", typeof(Light));
                Light light = lightObject.GetComponent<Light>();
                light.type = bay % 2 == 0 ? LightType.Point : LightType.Spot;
                light.color = Color.Lerp(new Color(0.4f, 0.65f, 1f), new Color(1f, 0.55f, 0.25f), bay / 4f);
                light.intensity = 4f; light.range = 15f;
                lightObject.transform.position = new Vector3(x, 6, 65);
                lightObject.transform.rotation = Quaternion.Euler(90, 0, 0);
                ParentToActiveZone(lightObject);
            }
            Save(scene);
        }

        private static void CreateNpcInteraction()
        {
            Scene scene = NewZone(TestZoneId.NPCInteraction, "NPC and Interaction Sandbox", new Vector3(-70, 0, -55), new Vector3(45, 1, 60), "NPC",
                "Exercise production NPC and interaction prefabs across open lanes, obstructions, range tests, and count presets.");
            for (int i = 0; i < 3; i++)
            {
                GameObject npc = InstantiatePrefab("Assets/Prefabs/NPC.prefab", scene);
                if (npc != null) npc.transform.position = new Vector3(-80 + i * 10, 1, -55);
            }
            for (int i = 0; i < 5; i++)
            {
                GameObject item = InstantiatePrefab("Assets/Prefabs/Items/Apple.prefab", scene);
                if (item != null) item.transform.position = new Vector3(-82 + i * 6, 1, -40);
            }
            Cube("Line of Sight Wall", new Vector3(-70, 2, -48), new Vector3(12, 4, 0.5f), "NPC");
            Save(scene);
        }

        private static void CreateUi()
        {
            Scene scene = NewZone(TestZoneId.UI, "UI Systems Lab", new Vector3(70, 0, -55), new Vector3(40, 1, 45), "UI",
                "Use the persistent panel to inspect HUD, pause, settings, focus, long text, empty data, aspect ratios, and contrast backdrops.");
            Cube("Bright Backdrop", new Vector3(62, 5, -55), new Vector3(10, 10, 1), "Bright");
            Cube("Dark Backdrop", new Vector3(78, 5, -55), new Vector3(10, 10, 1), "Dark");
            InstantiatePrefab("Assets/Prefabs/Level Components/UI/In - Game Canvas.prefab", scene);
            InstantiatePrefab("Assets/Prefabs/UI/Pause Canvas.prefab", scene);
            Label("UI STATE GALLERY", new Vector3(70, 7, -43), Color.magenta, 0.5f);
            Save(scene);
        }

        private static void CreateIntegration()
        {
            Scene scene = NewZone(TestZoneId.Integration, "Integration and Performance Arena", new Vector3(0, 0, -85), new Vector3(60, 1, 70), "Integration",
                "Follow checkpoints through movement, camera framing, NPC crossings, interactions, lights, moving platforms, and UI updates.");
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = new(-24 + i * 7, 1 + (i % 3), -105 + i * 6);
                Cube($"Checkpoint {i + 1}", p, new Vector3(4, 0.5f, 4), "Integration");
                Label($"{i + 1}", p + Vector3.up * 2, Color.yellow, 0.35f);
            }
            for (int i = 0; i < 4; i++)
            {
                GameObject npc = InstantiatePrefab("Assets/Prefabs/NPC.prefab", scene);
                if (npc != null) npc.transform.position = new Vector3(-18 + i * 12, 1, -75);
            }
            InstantiatePrefab("Assets/Prefabs/Environment/Moving Platform.prefab", scene);
            Save(scene);
        }

        private static Scene NewZone(TestZoneId id, string displayName, Vector3 center, Vector3 size, string material, string instructions)
        {
            Scene scene = NewScene($"TestCampus_{SceneSuffix(id)}");
            GameObject rootObject = new($"=== {displayName.ToUpperInvariant()} ===");
            TestZoneRoot root = rootObject.AddComponent<TestZoneRoot>();
            root.Configure(id, displayName, Materials[material].color, instructions);
            _activeZoneRoot = root.transform;
            rootObject.AddComponent<TestZonePresetProvider>();
            CreateSpawn(root, center + Vector3.up, "Default");
            Cube($"{displayName} Floor", center - Vector3.up * 0.5f, size, material);
            Label(displayName.ToUpperInvariant(), center + new Vector3(0, 5, size.z * 0.42f), Materials[material].color, 0.65f);
            return scene;
        }

        private static Scene NewScene(string name)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = name;
            return scene;
        }

        private static void Save(Scene scene)
        {
            EditorSceneManager.SaveScene(scene, $"{SceneFolder}/{scene.name}.unity");
            _activeZoneRoot = null;
        }
        private static void CreateSpawn(TestZoneRoot root, Vector3 position, string id)
        {
            GameObject spawn = new($"Spawn - {id}");
            spawn.transform.SetParent(root.transform);
            spawn.transform.position = position;
            root.ConfigureSpawn(id, spawn.transform);
        }
        private static void CreateWalkway(Vector3 position, Vector3 scale, float yRotation = 0) =>
            Cube("Campus Walkway", position, scale, "Grid", Quaternion.Euler(0, yRotation, 0));

        private static GameObject Cube(string name, Vector3 position, Vector3 scale, string material, Quaternion rotation = default)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.position = position; go.transform.localScale = scale;
            go.transform.rotation = rotation == default ? Quaternion.identity : rotation;
            go.GetComponent<Renderer>().sharedMaterial = Materials[material];
            go.AddComponent<TestResettableTransform>();
            ParentToActiveZone(go);
            return go;
        }
        private static GameObject Sphere(string name, Vector3 position, Vector3 scale, string material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name; go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Materials[material];
            go.AddComponent<TestResettableTransform>();
            ParentToActiveZone(go);
            return go;
        }
        private static GameObject Capsule(string name, Vector3 position)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name; go.transform.position = position;
            ParentToActiveZone(go);
            return go;
        }
        private static void Label(string text, Vector3 position, Color color, float size)
        {
            GameObject go = new($"SIGN - {text}", typeof(TextMesh));
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0, 180, 0);
            ParentToActiveZone(go);
            TextMesh mesh = go.GetComponent<TextMesh>();
            mesh.text = text; mesh.color = color; mesh.characterSize = size; mesh.fontSize = 64;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center;
        }
        private static GameObject InstantiatePrefab(string path, Scene scene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return null;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            ParentToActiveZone(instance);
            return instance;
        }

        private static void ParentToActiveZone(GameObject gameObject)
        {
            if (_activeZoneRoot != null)
                gameObject.transform.SetParent(_activeZoneRoot, true);
        }
        private static void CreateDirectionalLight()
        {
            GameObject go = new("Campus Directional Light", typeof(Light));
            Light light = go.GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f;
            go.transform.rotation = Quaternion.Euler(45, -35, 0);
        }
        private static string SceneSuffix(TestZoneId id) => id == TestZoneId.NPCInteraction ? "NPCInteraction" : id.ToString();

        private static void CreateMaterials()
        {
            Materials.Clear();
            CreateMaterial("Neutral", new Color(0.42f, 0.45f, 0.5f), 0.15f);
            CreateMaterial("Grid", new Color(0.12f, 0.14f, 0.18f), 0.05f);
            CreateMaterial("Movement", new Color(0.15f, 0.65f, 1f), 0.1f);
            CreateMaterial("Camera", new Color(1f, 0.55f, 0.12f), 0.1f);
            CreateMaterial("Lighting", new Color(1f, 0.85f, 0.15f), 0.1f);
            CreateMaterial("NPC", new Color(0.25f, 0.9f, 0.45f), 0.1f);
            CreateMaterial("UI", new Color(0.95f, 0.25f, 0.8f), 0.1f);
            CreateMaterial("Integration", new Color(0.75f, 0.35f, 1f), 0.1f);
            CreateMaterial("Accent", Color.cyan, 0.5f);
            CreateMaterial("Glossy", new Color(0.7f, 0.75f, 0.8f), 0.9f);
            CreateMaterial("Bright", Color.white, 0.1f);
            CreateMaterial("Dark", new Color(0.015f, 0.02f, 0.03f), 0.1f);
        }

        private static void AddScenesToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new(EditorBuildSettings.scenes);
            foreach (TestZoneId id in System.Enum.GetValues(typeof(TestZoneId)))
            {
                string suffix = id switch
                {
                    TestZoneId.Hub => "Core",
                    TestZoneId.NPCInteraction => "NPCInteraction",
                    _ => id.ToString()
                };
                string path = $"{SceneFolder}/TestCampus_{suffix}.unity";
                if (!scenes.Exists(scene => scene.path == path))
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        private static void CreateMaterial(string name, Color color, float smoothness)
        {
            string path = $"{MaterialFolder}/TC_{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            Materials[name] = material;
        }
    }
}
