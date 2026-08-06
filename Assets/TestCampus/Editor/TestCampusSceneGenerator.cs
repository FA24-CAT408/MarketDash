using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine.Splines;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace CrazyMarket.TestCampus.Editor
{
    public static class TestCampusSceneGenerator
    {
        public const string SceneFolder = "Assets/TestCampus/Scenes";
        public const string MaterialFolder = "Assets/TestCampus/Materials";
        public const string UiFolder = "Assets/TestCampus/UI";
        public const string UiPanelSettingsPath = UiFolder + "/TestCampusPanelSettings.asset";
        public const string UiLayoutPath = UiFolder + "/TestCampusControlPanel.uxml";
        public const string UiThemePath = UiFolder + "/TestCampusRuntimeTheme.tss";
        private const string CameraObstacleLayer = "Camera Obstacle";
        private const string CameraSurfaceLayer = "Camera Surface";

        private static readonly Dictionary<string, Material> Materials = new();
        private static Transform _activeZoneRoot;

        [MenuItem("CrazyMarket/Test Campus/Build All Scenes")]
        public static void BuildAll()
        {
            PrepareBuild();
            CreateCore(true);
            CreateMovement();
            CreateCamera();
            CreateLighting();
            CreateNpcInteraction();
            CreateUi();
            CreateIntegration();
            AddScenesToBuildSettings(true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Test Campus: seven scenes generated and added to development build settings.");
        }

        [MenuItem("CrazyMarket/Test Campus/Build Existing Scenes")]
        public static void BuildExisting()
        {
            bool hasCore = SceneExists(TestZoneId.Hub);
            bool hasMovement = SceneExists(TestZoneId.Movement);
            bool hasCamera = SceneExists(TestZoneId.Camera);
            bool hasLighting = SceneExists(TestZoneId.Lighting);
            bool hasNpcInteraction = SceneExists(TestZoneId.NPCInteraction);
            bool hasUi = SceneExists(TestZoneId.UI);
            bool hasIntegration = SceneExists(TestZoneId.Integration);
            if (!hasCore)
                throw new FileNotFoundException("Build Existing Scenes requires TestCampus_Core.unity.");

            PrepareBuild();
            CreateCore(false);
            if (hasMovement) CreateMovement();
            if (hasCamera) CreateCamera();
            if (hasLighting) CreateLighting();
            if (hasNpcInteraction) CreateNpcInteraction();
            if (hasUi) CreateUi();
            if (hasIntegration) CreateIntegration();
            AddScenesToBuildSettings(false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Test Campus: regenerated the scenes present in this stack layer.");
        }

        private static void PrepareBuild()
        {
            Directory.CreateDirectory(SceneFolder);
            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory(UiFolder);
            ConfigureCameraCollisionLayers();
            CreateMaterials();
            CreateUiPanelSettings();
            RenderSettings.skybox = null;
        }

        private static void CreateUiPanelSettings()
        {
            ThemeStyleSheet theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiThemePath);
            VisualTreeAsset layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiLayoutPath);
            if (theme == null || layout == null)
                throw new FileNotFoundException("Test Campus UI Toolkit theme or UXML layout is missing.");

            PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPanelSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                settings.name = "TestCampusPanelSettings";
                AssetDatabase.CreateAsset(settings, UiPanelSettingsPath);
            }
            settings.themeStyleSheet = theme;
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.sortingOrder = 100;
            EditorUtility.SetDirty(settings);
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

        private static void CreateCore(bool includeAllZones)
        {
            Scene scene = NewScene("TestCampus_Core");
            GameObject root = new("=== TEST CAMPUS CORE ===");
            TestCampusController controller = root.AddComponent<TestCampusController>();
            GameObject cameraSystems = new("Camera Systems");
            cameraSystems.AddComponent<TestCampusCameraInputFocus>();
            cameraSystems.AddComponent<TestCampusCameraOcclusionController>();
            cameraSystems.AddComponent<TestCampusCameraPrototypeController>();
            controller.AutoLoadDefaultZones = true;
            TestZoneRoot hub = root.AddComponent<TestZoneRoot>();
            hub.Configure(TestZoneId.Hub, "Core Control Hub", new Color(0.2f, 0.8f, 1f),
                "Navigate physically or use F1. F2 resets the current zone. F3 returns to hub.");
            _activeZoneRoot = hub.transform;
            CreateSpawn(hub, Vector3.zero + Vector3.up, "Default");
            MarkCameraSurface(Cube("Hub Floor", new Vector3(0, -0.5f, 0), new Vector3(40, 1, 40), "Neutral"));
            CreateInteriorShell(Vector3.zero, new Vector3(40, 12, 40), "Hub");
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
                if (id != TestZoneId.Hub && (includeAllZones || SceneExists(id)))
                    controller.ZoneScenes.Add(new TestZoneScene { Zone = id, SceneName = $"TestCampus_{SceneSuffix(id)}", LoadByDefault = true });
            _activeZoneRoot = null;
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/KCC Player Controller.prefab");
            GameObject player = playerPrefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene) : Capsule("Test Player", Vector3.up);
            player.SetActive(true);
            player.transform.position = Vector3.up;
            player.AddComponent<TestCampusPlayerAdapter>();
            controller.PlayerRoot = player.transform;
            InstantiatePrefab("Assets/Prefabs/Level Components/Managers/Game Manager.prefab", scene);
            InstantiatePrefab("Assets/Prefabs/Level Components/Managers/Timer Manager.prefab", scene);
            InstantiatePrefab("Assets/Prefabs/Level Components/Managers/Debug Controller.prefab", scene);
            GameObject eventPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Level Components/UI/EventSystem.prefab");
            if (eventPrefab != null) PrefabUtility.InstantiatePrefab(eventPrefab, scene);
            else new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            GameObject controlPanelObject = new("Test Campus Control Panel");
            UIDocument uiDocument = controlPanelObject.AddComponent<UIDocument>();
            uiDocument.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPanelSettingsPath);
            uiDocument.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiLayoutPath);
            uiDocument.sortingOrder = 100;
            controlPanelObject.AddComponent<TestCampusControlPanel>();
            GameObject camera = new("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CinemachineBrain));
            camera.tag = "MainCamera";
            camera.transform.SetPositionAndRotation(new Vector3(0, 9, -12), Quaternion.Euler(24, 0, 0));
            _activeZoneRoot = hub.transform;
            CreateAssistedOrbitCamera(player.transform);
            CreateInteriorLight("Hub Key Light", new Vector3(0, 9, 0), 34f, new Color(0.82f, 0.9f, 1f));
            _activeZoneRoot = null;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.18f);
            Save(scene);
        }

        private static void CreateAssistedOrbitCamera(Transform player)
        {
            GameObject rig = new("CM Test Campus Player Camera", typeof(CinemachineCamera));
            ParentToActiveZone(rig);
            rig.transform.SetPositionAndRotation(player.position + new Vector3(0f, 6.5f, -8.5f), Quaternion.Euler(24f, 0f, 0f));
            rig.AddComponent<TestCampusCameraRigTag>().Mode = TestCampusCameraMode.AssistedOrbit;

            CinemachineCamera virtualCamera = rig.GetComponent<CinemachineCamera>();
            virtualCamera.Follow = player;
            virtualCamera.LookAt = player;
            virtualCamera.Priority.Value = 30;
            LensSettings lens = virtualCamera.Lens;
            lens.FieldOfView = 58f;
            virtualCamera.Lens = lens;

            TestCampusCameraCollisionPolicy collisionPolicy =
                rig.AddComponent<TestCampusCameraCollisionPolicy>();
            collisionPolicy.Configure(
                (1 << RequireLayer(CameraObstacleLayer)) | (1 << RequireLayer(CameraSurfaceLayer)),
                1 << RequireLayer(CameraSurfaceLayer));

            CinemachineOrbitalFollow orbit = rig.AddComponent<CinemachineOrbitalFollow>();
            orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbit.Radius = 9.5f;
            orbit.TargetOffset = Vector3.up * TestCampusCameraGroundGuard.DefaultTargetYOffset;
            orbit.RecenteringTarget = CinemachineOrbitalFollow.ReferenceFrames.AxisCenter;
            orbit.HorizontalAxis = new InputAxis
            {
                Value = 0f, Center = 0f, Range = new Vector2(-180f, 180f), Wrap = true
            };
            orbit.VerticalAxis = new InputAxis
            {
                Value = 22f, Center = 22f, Range = new Vector2(-20f, 55f), Wrap = false
            };
            // The radial axis is the floor constraint's pull-in stage: CinemachineOrbitalFollow
            // multiplies Radius by this axis, so widening the range lets the camera shorten to
            // 7.0 m before it starts riding the surface instead. The runtime controller reads this
            // authored range directly, keeping the rig as the single source of truth.
            orbit.RadialAxis = new InputAxis
            {
                Value = 1f, Center = 1f,
                Range = new Vector2(TestCampusCameraGroundGuard.DefaultMinimumOrbitRadiusScale, 1f),
                Wrap = false
            };
            TrackerSettings tracker = orbit.TrackerSettings;
            tracker.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
            tracker.PositionDamping = new Vector3(0.12f, 0.18f, 0.12f);
            tracker.RotationDamping = Vector3.zero;
            orbit.TrackerSettings = tracker;

            CinemachineRotationComposer composer = rig.AddComponent<CinemachineRotationComposer>();
            composer.TargetOffset = Vector3.up * TestCampusCameraGroundGuard.DefaultTargetYOffset;
            composer.Damping = new Vector2(0.08f, 0.08f);
            composer.CenterOnActivate = true;
            ScreenComposerSettings composition = composer.Composition;
            composition.ScreenPosition = new Vector2(0f, -0.08f);
            composer.Composition = composition;

            CinemachineDecollider decollider = rig.AddComponent<CinemachineDecollider>();
            decollider.CameraRadius = TestCampusCameraGroundGuard.DefaultCameraRadius;
            CinemachineDecollider.DecollisionSettings decollision = decollider.Decollision;
            decollision.Enabled = true;
            decollision.ObstacleLayers = collisionPolicy.ObstacleLayers;
            decollision.UseFollowTarget.Enabled = true;
            decollision.UseFollowTarget.YOffset = TestCampusCameraGroundGuard.DefaultTargetYOffset;
            decollision.Damping = 0.35f;
            decollision.SmoothingTime = 0.08f;
            decollider.Decollision = decollision;
            // TerrainResolution must stay disabled, despite looking like the obvious floor fix.
            // CinemachineDecollider.DecollideCamera does `layers &= ~TerrainResolution.TerrainLayers`,
            // and every Test Campus object is on layer 0, so enabling it would silently disable all
            // decollision. Its probe also starts 10 m above the camera, which would mistake the hub
            // ceiling (y 11.5) and the Movement gym's Low Ceiling (y 2.5) for ground. The floor is
            // handled instead by TestCampusCameraGroundGuard, whose probe starts at the orbit
            // target's height and only ever sweeps downward.
            CinemachineDecollider.TerrainSettings terrain = decollider.TerrainResolution;
            terrain.Enabled = false;
            decollider.TerrainResolution = terrain;
            SerializedObject serializedDecollider = new(decollider);
            serializedDecollider.FindProperty("TerrainResolution.Enabled").boolValue = false;
            serializedDecollider.ApplyModifiedPropertiesWithoutUndo();

            // Added after the Decollider so its callback runs last: it enforces the ground floor on
            // the final corrected position, covering the frames where rig damping lets the rendered
            // camera lag behind the axis-level constraint.
            rig.AddComponent<TestCampusCameraGroundGuard>();
            rig.AddComponent<TestCampusCameraSurfaceConstraint>();
        }

        private static void CreateMovement()
        {
            Scene scene = NewZone(TestZoneId.Movement, "Movement Gym", new Vector3(-75, 0, 20), new Vector3(45, 1, 70), "Movement",
                "Measure acceleration, stopping, slopes, steps, jumps, coyote time, moving platforms, beams, falls, and respawn.",
                includePresetProvider: false);
            MoveDefaultSpawn(scene, new Vector3(-75, 1, 10));
            for (int z = -10; z <= 45; z += 5)
            {
                GameObject marker = Cube($"Distance {z + 10}m", new Vector3(-82, 0.05f, z), new Vector3(8, 0.1f, 0.15f), "Grid");
                Object.DestroyImmediate(marker.GetComponent<Collider>());
            }
            for (int i = 0; i < 5; i++) Cube($"Step {i + 1}", new Vector3(-68 + i * 2, i * 0.25f, 4), new Vector3(2, 0.5f + i * 0.5f, 5), "Movement");
            Cube("Slope 15 degrees", new Vector3(-80, 1.5f, 25), new Vector3(10, 0.5f, 12), "Movement", Quaternion.Euler(15, 0, 0));
            Cube("Slope 30 degrees", new Vector3(-67, 3, 25), new Vector3(10, 0.5f, 12), "Movement", Quaternion.Euler(30, 0, 0));
            for (int i = 0; i < 6; i++) Cube($"Jump Target {i + 1}", new Vector3(-84 + i * 4, 1 + i * 0.4f, 43), new Vector3(2.5f, 0.4f, 2.5f), "Movement");
            Cube("Narrow Beam", new Vector3(-72, 2, -2), new Vector3(1, 0.4f, 16), "Movement");
            MarkCameraSurface(Cube(
                "Low Ceiling", new Vector3(-62, 2.5f, -5), new Vector3(8, 0.4f, 12), "Movement"));
            GameObject movingPlatform = InstantiatePrefab("Assets/Prefabs/Environment/Moving Platform.prefab", scene);
            if (movingPlatform != null)
            {
                movingPlatform.name = "Movement Production Moving Platform";
                movingPlatform.transform.position = new Vector3(-58, 2, 25);
            }
            Save(scene);
        }

        private static void CreateCamera()
        {
            Scene scene = NewZone(TestZoneId.Camera, "Camera Course", new Vector3(75, 0, 20), new Vector3(40, 1, 65), "Camera",
                "Walk the marked route to inspect follow composition, obstructions, corridors, height changes, framing, and trigger transitions.");
            for (int i = 0; i < 7; i++)
            {
                MarkCameraObstacle(Cube($"Corridor Left {i}", new Vector3(68, 2, -5 + i * 7), new Vector3(1, 4, 6), "Camera"))
                    .AddComponent<TestCampusSelectiveOccluder>();
                MarkCameraObstacle(Cube($"Corridor Right {i}", new Vector3(82, 2, -5 + i * 7), new Vector3(1, 4, 6), "Camera"))
                    .AddComponent<TestCampusSelectiveOccluder>();
            }
            for (int i = 0; i < 5; i++) Cube($"Height Target {i}", new Vector3(60 + i * 7, 1 + i, 42), new Vector3(4, 2 + i * 2, 4), "Camera");
            Sphere("Look At Target", new Vector3(75, 6, 48), new Vector3(3, 3, 3), "Accent");
            CreateGuidedRailCamera();
            Save(scene);
        }

        private static void CreateGuidedRailCamera()
        {
            GameObject pathObject = new("Camera Prototype Rail", typeof(SplineContainer));
            ParentToActiveZone(pathObject);
            SplineContainer container = pathObject.GetComponent<SplineContainer>();
            Spline spline = new();
            spline.Add(new BezierKnot(new Vector3(75f, 8f, -18f)), TangentMode.AutoSmooth);
            spline.Add(new BezierKnot(new Vector3(75f, 9f, 18f)), TangentMode.AutoSmooth);
            spline.Add(new BezierKnot(new Vector3(75f, 11f, 55f)), TangentMode.AutoSmooth);
            container.Spline = spline;

            GameObject rig = new("CM Guided Rail Prototype", typeof(CinemachineCamera));
            ParentToActiveZone(rig);
            rig.AddComponent<TestCampusCameraRigTag>().Mode = TestCampusCameraMode.GuidedRail;
            CinemachineCamera camera = rig.GetComponent<CinemachineCamera>();
            camera.Priority.Value = 10;
            LensSettings lens = camera.Lens;
            lens.FieldOfView = 55f;
            camera.Lens = lens;

            CinemachineSplineDolly dolly = rig.AddComponent<CinemachineSplineDolly>();
            dolly.Spline = container;
            dolly.PositionUnits = PathIndexUnit.Normalized;
            dolly.SplineOffset = new Vector3(0f, 0f, -9f);
            dolly.CameraRotation = CinemachineSplineDolly.RotationMode.SplineNoRoll;
            dolly.AutomaticDolly = new SplineAutoDolly
            {
                Enabled = true,
                Method = new SplineAutoDolly.NearestPointToTarget()
            };
            dolly.Damping = new CinemachineSplineDolly.DampingSettings
            {
                Enabled = true,
                Position = new Vector3(0.15f, 0.25f, 0.35f),
                Angular = 0.2f
            };

            CinemachineRotationComposer composer = rig.AddComponent<CinemachineRotationComposer>();
            composer.TargetOffset = Vector3.up * TestCampusCameraGroundGuard.DefaultTargetYOffset;
            composer.Damping = new Vector2(0.12f, 0.12f);
            ScreenComposerSettings composition = composer.Composition;
            composition.ScreenPosition = new Vector2(0f, -0.1f);
            composer.Composition = composition;

            GameObject zone = new("Hybrid Rail Camera Zone", typeof(BoxCollider), typeof(TestCampusCameraModeZone));
            ParentToActiveZone(zone);
            BoxCollider trigger = zone.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(75f, 3f, 20f);
            trigger.size = new Vector3(13f, 6f, 18f);
        }

        private static void CreateLighting()
        {
            Scene scene = NewZone(TestZoneId.Lighting, "Lighting and Shading Gallery", new Vector3(0, 0, 70), new Vector3(50, 1, 60), "Lighting",
                "Compare identical forms in five isolated point and spot bays progressing from cool to warm light.",
                includePresetProvider: false, includeInteriorLights: false);
            MoveDefaultSpawn(scene, new Vector3(0, 1, 55));
            string[] bayNames = { "Cool Point", "Cool-Neutral Spot", "Neutral Point", "Warm-Neutral Spot", "Warm Point" };
            for (int bay = 0; bay < 5; bay++)
            {
                float x = -20 + bay * 10;
                string bayName = bayNames[bay];
                GameObject bayRoot = new($"Bay {bay + 1} — {bayName}");
                ParentToActiveZone(bayRoot);

                GameObject wall = Cube($"{bayName} Backdrop", new Vector3(x, 3, 78), new Vector3(8, 6, 0.5f), "Neutral");
                GameObject sphere = Sphere($"{bayName} Reference Sphere", new Vector3(x, 1.5f, 70), Vector3.one * 3, "Glossy");
                GameObject cube = Cube($"{bayName} Reference Cube", new Vector3(x, 1.5f, 65), Vector3.one * 3, "Neutral");
                GameObject label = Label(bayName.ToUpperInvariant(), new Vector3(x, 6.5f, 77.6f),
                    Color.Lerp(new Color(0.4f, 0.65f, 1f), new Color(1f, 0.55f, 0.25f), bay / 4f), 0.3f);
                wall.transform.SetParent(bayRoot.transform, true);
                sphere.transform.SetParent(bayRoot.transform, true);
                cube.transform.SetParent(bayRoot.transform, true);
                label.transform.SetParent(bayRoot.transform, true);

                GameObject lightObject = new($"{bayName} Light", typeof(Light));
                Light light = lightObject.GetComponent<Light>();
                light.type = bay % 2 == 0 ? LightType.Point : LightType.Spot;
                light.color = Color.Lerp(new Color(0.4f, 0.65f, 1f), new Color(1f, 0.55f, 0.25f), bay / 4f);
                light.intensity = 40f;
                light.range = 7.5f;
                light.spotAngle = 65f;
                light.shadows = LightShadows.Soft;
                lightObject.transform.position = new Vector3(x, 6, 67.5f);
                lightObject.transform.rotation = Quaternion.Euler(90, 0, 0);
                lightObject.transform.SetParent(bayRoot.transform, true);
            }
            Save(scene);
        }

        private static void CreateNpcInteraction()
        {
            Scene scene = NewZone(TestZoneId.NPCInteraction, "NPC and Interaction Sandbox", new Vector3(-70, 0, -55), new Vector3(45, 1, 60), "NPC",
                "Watch the three production NPC patrol lanes, then walk through the glowing apple line to collect each item. Reset restores every apple.",
                includePresetProvider: false);
            MoveDefaultSpawn(scene, new Vector3(-88, 1, -78));

            GameObject npcGroup = new("NPC Patrol Lanes");
            ParentToActiveZone(npcGroup);
            string[] npcNames = { "West Patrol NPC", "Obstructed Center Patrol NPC", "East Patrol NPC" };
            for (int i = 0; i < 3; i++)
            {
                GameObject npc = InstantiatePrefab("Assets/Prefabs/NPC.prefab", scene);
                if (npc != null)
                {
                    npc.name = npcNames[i];
                    npc.transform.position = new Vector3(-80 + i * 10, 0, -82);
                    Collider npcCollider = npc.GetComponentInChildren<Collider>();
                    if (npcCollider != null)
                        npc.transform.position += Vector3.up * -npcCollider.bounds.min.y;
                    npc.transform.SetParent(npcGroup.transform, true);
                    npc.AddComponent<TestResettableActivation>();
                }
            }

            GameObject itemGroup = new("Collectible Apple Line");
            ParentToActiveZone(itemGroup);
            for (int i = 0; i < 5; i++)
            {
                GameObject item = InstantiatePrefab("Assets/Prefabs/Items/Apple.prefab", scene);
                if (item == null) continue;

                item.name = $"Collectible Apple {i + 1}";
                item.transform.position = new Vector3(-82 + i * 6, 1, -40);
                item.transform.SetParent(itemGroup.transform, true);
                item.AddComponent<TestResettableActivation>();

                Transform glowObject = null;
                foreach (Transform child in item.GetComponentsInChildren<Transform>(true))
                    if (child.name == "Glow") glowObject = child;
                if (glowObject == null)
                    throw new System.InvalidOperationException("Apple.prefab must retain its Glow child.");
                glowObject.gameObject.SetActive(true);

                Component productionItem = item.GetComponent("Item");
                if (productionItem == null)
                    throw new System.InvalidOperationException("Apple.prefab must retain its production Item component.");

                SerializedObject serializedItem = new(productionItem);
                SerializedProperty collectable = serializedItem.FindProperty("isCollectable");
                SerializedProperty glow = serializedItem.FindProperty("glow");
                if (collectable == null || glow == null)
                    throw new System.InvalidOperationException("Production Item must expose isCollectable and glow fields.");
                collectable.boolValue = true;
                glow.boolValue = true;
                serializedItem.ApplyModifiedPropertiesWithoutUndo();
            }

            Cube("Physical Obstruction Wall", new Vector3(-70, 2, -48), new Vector3(12, 4, 0.5f), "NPC");
            Label("NPC PATROL LANES", new Vector3(-82, 4, -80), Color.white, 0.2f);
            Label("PHYSICAL OBSTRUCTION", new Vector3(-70, 4.8f, -48.4f), Color.yellow, 0.2f);
            Label("COLLECTIBLE APPLES — WALK THROUGH", new Vector3(-70, 3, -38), Color.green, 0.2f);
            Save(scene);
        }

        private static void CreateUi()
        {
            Scene scene = NewZone(TestZoneId.UI, "UI Systems Lab", new Vector3(70, 0, -55), new Vector3(40, 1, 45), "UI",
                "Open F1 after teleporting here. Low hides production fixtures, Normal shows the HUD, Stress adds the pause overlay, and Reset returns to hidden.",
                includePresetProvider: false);
            MoveDefaultSpawn(scene, new Vector3(70, 1, -66));
            Cube("Bright Backdrop", new Vector3(62, 5, -55), new Vector3(10, 10, 1), "Bright");
            Cube("Dark Backdrop", new Vector3(78, 5, -55), new Vector3(10, 10, 1), "Dark");
            GameObject galleryObject = new("Production UI Fixture Gallery");
            ParentToActiveZone(galleryObject);
            GameObject hud = InstantiatePrefab("Assets/Prefabs/Level Components/UI/In - Game Canvas.prefab", scene);
            GameObject pause = InstantiatePrefab("Assets/Prefabs/UI/Pause Canvas.prefab", scene);
            if (hud == null || pause == null)
                throw new FileNotFoundException("UI Systems Lab requires the production HUD and Pause Canvas prefabs.");
            hud.name = "Production HUD Fixture";
            pause.name = "Production Pause Overlay Fixture";
            hud.transform.SetParent(galleryObject.transform, true);
            pause.transform.SetParent(galleryObject.transform, true);
            galleryObject.AddComponent<TestCampusUiFixtureGallery>().Configure(hud, pause);
            Label("UI STATE GALLERY", new Vector3(70, 7, -43), Color.magenta, 0.5f);
            Save(scene);
        }

        private static void CreateIntegration()
        {
            Scene scene = NewZone(TestZoneId.Integration, "Integration and Performance Arena", new Vector3(0, 0, -85), new Vector3(60, 1, 70), "Integration",
                "Follow the numbered route through height changes, production NPC crossings, a collectible, and a moving platform. Low/Normal/Stress scale real NPC, light, and platform load; F1 diagnostics report the active scenario.",
                includePresetProvider: false);
            MoveDefaultSpawn(scene, new Vector3(-24, 1, -109));
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = new(-24 + i * 7, 1 + (i % 3), -105 + i * 6);
                Cube($"Checkpoint {i + 1}", p, new Vector3(4, 0.5f, 4), "Integration");
                Label($"{i + 1}", p + Vector3.up * 2, Color.yellow, 0.35f);
            }

            GameObject npcGroup = new("Production NPC Crossings");
            ParentToActiveZone(npcGroup);
            List<GameObject> npcs = new();
            for (int i = 0; i < 4; i++)
            {
                GameObject npc = InstantiatePrefab("Assets/Prefabs/NPC.prefab", scene);
                if (npc == null)
                    throw new FileNotFoundException("Integration requires the production NPC prefab.");
                npc.name = $"Integration Crossing NPC {i + 1}";
                npc.transform.position = new Vector3(-18 + i * 12, 0, -75);
                Collider npcCollider = npc.GetComponentInChildren<Collider>();
                if (npcCollider != null) npc.transform.position += Vector3.up * -npcCollider.bounds.min.y;
                npc.transform.SetParent(npcGroup.transform, true);
                npc.AddComponent<TestResettableActivation>();
                npcs.Add(npc);
            }

            GameObject collectible = InstantiatePrefab("Assets/Prefabs/Items/Apple.prefab", scene);
            if (collectible == null)
                throw new FileNotFoundException("Integration requires the production Apple prefab.");
            collectible.name = "Integration Route Apple";
            collectible.transform.position = new Vector3(-3, 2, -87);
            collectible.AddComponent<TestResettableActivation>();
            ConfigureProductionCollectible(collectible);

            GameObject movingPlatform = InstantiatePrefab("Assets/Prefabs/Environment/Moving Platform.prefab", scene);
            if (movingPlatform == null)
                throw new FileNotFoundException("Integration requires the production moving-platform prefab.");
            movingPlatform.name = "Integration Production Moving Platform";
            movingPlatform.transform.position = new Vector3(18, 2, -68);
            movingPlatform.AddComponent<TestResettableActivation>();

            GameObject scenarioObject = new("Integration Scenario");
            ParentToActiveZone(scenarioObject);
            List<GameObject> roomLights = new();
            foreach (Light roomLight in _activeZoneRoot.GetComponentsInChildren<Light>(true))
                roomLights.Add(roomLight.gameObject);
            scenarioObject.AddComponent<TestCampusIntegrationScenario>()
                .Configure(npcs.ToArray(), movingPlatform, roomLights.ToArray(), collectible);

            Label("NPC CROSSINGS", new Vector3(0, 5, -76), Color.green, 0.25f);
            Label("COLLECTIBLE", new Vector3(-3, 4, -87), Color.cyan, 0.25f);
            Label("MOVING PLATFORM", new Vector3(18, 5, -68), Color.magenta, 0.25f);
            Save(scene);
        }

        private static void ConfigureProductionCollectible(GameObject item)
        {
            Transform glowObject = null;
            foreach (Transform child in item.GetComponentsInChildren<Transform>(true))
                if (child.name == "Glow") glowObject = child;
            if (glowObject == null)
                throw new System.InvalidOperationException("Apple.prefab must retain its Glow child.");
            glowObject.gameObject.SetActive(true);

            Component productionItem = item.GetComponent("Item");
            if (productionItem == null)
                throw new System.InvalidOperationException("Apple.prefab must retain its production Item component.");
            SerializedObject serializedItem = new(productionItem);
            SerializedProperty collectable = serializedItem.FindProperty("isCollectable");
            SerializedProperty glow = serializedItem.FindProperty("glow");
            if (collectable == null || glow == null)
                throw new System.InvalidOperationException("Production Item must expose isCollectable and glow fields.");
            collectable.boolValue = true;
            glow.boolValue = true;
            serializedItem.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Scene NewZone(TestZoneId id, string displayName, Vector3 center, Vector3 size, string material, string instructions,
            bool includePresetProvider = true, bool includeInteriorLights = true)
        {
            Scene scene = NewScene($"TestCampus_{SceneSuffix(id)}");
            GameObject rootObject = new($"=== {displayName.ToUpperInvariant()} ===");
            TestZoneRoot root = rootObject.AddComponent<TestZoneRoot>();
            root.Configure(id, displayName, Materials[material].color, instructions);
            _activeZoneRoot = root.transform;
            if (includePresetProvider)
                rootObject.AddComponent<TestZonePresetProvider>();
            CreateSpawn(root, center + Vector3.up, "Default");
            MarkCameraSurface(Cube($"{displayName} Floor", center - Vector3.up * 0.5f, size, "Neutral"));
            CreateInteriorShell(center, new Vector3(size.x, 12, size.z), material, includeInteriorLights);
            Label(displayName.ToUpperInvariant(), center + new Vector3(0, 5, size.z * 0.42f), Materials[material].color, 0.65f);
            return scene;
        }

        private static Scene NewScene(string name)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = name;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.18f);
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
            MarkCameraSurface(Cube(
                "Campus Walkway", position, scale, "Grid", Quaternion.Euler(0, yRotation, 0)));

        private static void CreateInteriorShell(Vector3 center, Vector3 size, string accentMaterial, bool includeInteriorLights = true)
        {
            float halfX = size.x * 0.5f;
            float halfZ = size.z * 0.5f;
            float ceilingY = 11.5f;
            const float doorway = 8f;
            CreateCameraApron(center, size);
            MarkCameraSurface(Cube(
                "Interior Ceiling", center + Vector3.up * ceilingY,
                new Vector3(size.x, 0.5f, size.z), "Ceiling"));
            float horizontalWallWidth = (size.x - doorway) * 0.5f;
            float verticalWallWidth = (size.z - doorway) * 0.5f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = center.x + side * (doorway * 0.5f + horizontalWallWidth * 0.5f);
                MarkCameraObstacle(Cube("Interior North Wall", new Vector3(x, center.y + 5.5f, center.z + halfZ), new Vector3(horizontalWallWidth, 12, 0.5f), "Wall"))
                    .AddComponent<TestCampusSelectiveOccluder>();
                MarkCameraObstacle(Cube("Interior South Wall", new Vector3(x, center.y + 5.5f, center.z - halfZ), new Vector3(horizontalWallWidth, 12, 0.5f), "Wall"))
                    .AddComponent<TestCampusSelectiveOccluder>();
                float z = center.z + side * (doorway * 0.5f + verticalWallWidth * 0.5f);
                MarkCameraObstacle(Cube("Interior East Wall", new Vector3(center.x + halfX, center.y + 5.5f, z), new Vector3(0.5f, 12, verticalWallWidth), "Wall"))
                    .AddComponent<TestCampusSelectiveOccluder>();
                MarkCameraObstacle(Cube("Interior West Wall", new Vector3(center.x - halfX, center.y + 5.5f, z), new Vector3(0.5f, 12, verticalWallWidth), "Wall"))
                    .AddComponent<TestCampusSelectiveOccluder>();
            }
            for (int i = -1; i <= 1; i++)
            {
                float z = center.z + i * size.z * 0.28f;
                MarkCameraObstacle(Cube("Structural Column", new Vector3(center.x - halfX + 1f, 3f, z), new Vector3(1.2f, 6f, 1.2f), accentMaterial))
                    .AddComponent<TestCampusSelectiveOccluder>();
                MarkCameraObstacle(Cube("Structural Column", new Vector3(center.x + halfX - 1f, 3f, z), new Vector3(1.2f, 6f, 1.2f), accentMaterial))
                    .AddComponent<TestCampusSelectiveOccluder>();
            }
            if (includeInteriorLights)
            {
                for (int i = -1; i <= 1; i++)
                    CreateInteriorLight($"Ceiling Light {i + 2}", center + new Vector3(i * size.x * 0.27f, 10.7f, 0), 28f, new Color(0.78f, 0.86f, 1f));
            }
        }

        private static void CreateCameraApron(Vector3 center, Vector3 size)
        {
            const float depth = 12f;
            GameObject north = Cube("Camera Apron North",
                center + new Vector3(0f, -0.49f, size.z * 0.5f + depth * 0.5f),
                new Vector3(size.x + depth * 2f, 0.1f, depth), "Neutral");
            GameObject south = Cube("Camera Apron South",
                center + new Vector3(0f, -0.49f, -size.z * 0.5f - depth * 0.5f),
                new Vector3(size.x + depth * 2f, 0.1f, depth), "Neutral");
            GameObject east = Cube("Camera Apron East",
                center + new Vector3(size.x * 0.5f + depth * 0.5f, -0.49f, 0f),
                new Vector3(depth, 0.1f, size.z), "Neutral");
            GameObject west = Cube("Camera Apron West",
                center + new Vector3(-size.x * 0.5f - depth * 0.5f, -0.49f, 0f),
                new Vector3(depth, 0.1f, size.z), "Neutral");
            // The aprons read as floor but used to carry no collider at all, so the camera sank
            // straight through the level whenever it swung outside the room through a doorway.
            // Marked trigger colliders make them ground to the camera probe only: triggers are
            // invisible to the KCC motor, so the deliberate non-colliding cutaway design is intact
            // and the camera still travels outside the walls, it just stops dropping below floor level.
            MakeCameraGround(north);
            MakeCameraGround(south);
            MakeCameraGround(east);
            MakeCameraGround(west);

            GameObject northBackdrop = Cube("Camera Backdrop North",
                center + new Vector3(0f, 5.5f, size.z * 0.5f + depth),
                new Vector3(size.x + depth * 2f, 12f, 0.5f), "Wall");
            GameObject southBackdrop = Cube("Camera Backdrop South",
                center + new Vector3(0f, 5.5f, -size.z * 0.5f - depth),
                new Vector3(size.x + depth * 2f, 12f, 0.5f), "Wall");
            GameObject eastBackdrop = Cube("Camera Backdrop East",
                center + new Vector3(size.x * 0.5f + depth, 5.5f, 0f),
                new Vector3(0.5f, 12f, size.z + depth * 2f), "Wall");
            GameObject westBackdrop = Cube("Camera Backdrop West",
                center + new Vector3(-size.x * 0.5f - depth, 5.5f, 0f),
                new Vector3(0.5f, 12f, size.z + depth * 2f), "Wall");
            Object.DestroyImmediate(northBackdrop.GetComponent<Collider>());
            Object.DestroyImmediate(southBackdrop.GetComponent<Collider>());
            Object.DestroyImmediate(eastBackdrop.GetComponent<Collider>());
            Object.DestroyImmediate(westBackdrop.GetComponent<Collider>());
        }

        private static void MakeCameraGround(GameObject slab)
        {
            Collider collider = slab.GetComponent<Collider>();
            if (collider != null)
                collider.isTrigger = true;
            slab.AddComponent<TestCampusCameraGround>();
            MarkCameraSurface(slab);
        }

        private static GameObject MarkCameraObstacle(GameObject gameObject) =>
            AddCameraCollisionProxy(gameObject, RequireLayer(CameraObstacleLayer));

        private static GameObject MarkCameraSurface(GameObject gameObject) =>
            AddCameraCollisionProxy(gameObject, RequireLayer(CameraSurfaceLayer));

        private static GameObject AddCameraCollisionProxy(GameObject gameObject, int layer)
        {
            BoxCollider source = gameObject.GetComponent<BoxCollider>();
            if (source == null)
                throw new System.InvalidOperationException(
                    $"Camera collision geometry requires a BoxCollider: {gameObject.name}");

            GameObject proxy = new($"{gameObject.name} Camera Collision");
            proxy.layer = layer;
            proxy.transform.SetParent(gameObject.transform, false);
            BoxCollider collision = proxy.AddComponent<BoxCollider>();
            collision.center = source.center;
            collision.size = source.size;
            collision.isTrigger = source.isTrigger;
            return gameObject;
        }

        private static void ConfigureCameraCollisionLayers()
        {
            int obstacleLayer = RequireLayer(CameraObstacleLayer);
            int surfaceLayer = RequireLayer(CameraSurfaceLayer);
            for (int layer = 0; layer < 32; layer++)
            {
                Physics.IgnoreLayerCollision(obstacleLayer, layer, true);
                Physics.IgnoreLayerCollision(surfaceLayer, layer, true);
            }
        }

        private static int RequireLayer(string name)
        {
            int layer = LayerMask.NameToLayer(name);
            if (layer < 0)
                throw new System.InvalidOperationException($"Required Test Campus layer is missing: {name}");
            return layer;
        }

        private static void CreateInteriorLight(string name, Vector3 position, float range, Color color)
        {
            GameObject fixture = Cube(name + " Fixture", position, new Vector3(4f, 0.15f, 1.2f), "LightFixture");
            Object.DestroyImmediate(fixture.GetComponent<Collider>());
            GameObject lightObject = new(name, typeof(Light));
            lightObject.transform.position = position - Vector3.up * 0.2f;
            ParentToActiveZone(lightObject);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = 160f;
            light.color = color;
            light.shadows = LightShadows.None;
        }

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
        private static GameObject Label(string text, Vector3 position, Color color, float size)
        {
            GameObject go = new($"SIGN - {text}", typeof(TextMeshPro));
            go.transform.position = position;
            go.transform.rotation = Quaternion.identity;
            ParentToActiveZone(go);
            TextMeshPro mesh = go.GetComponent<TextMeshPro>();
            mesh.text = text;
            mesh.color = color;
            mesh.fontSize = size * 10f;
            mesh.alignment = TextAlignmentOptions.Center;
            mesh.enableWordWrapping = false;
            return go;
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

        private static void MoveDefaultSpawn(Scene scene, Vector3 position)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                TestZoneRoot root = rootObject.GetComponent<TestZoneRoot>();
                if (root == null) continue;
                Transform spawn = root.ResolveSpawn("Default");
                if (spawn != null) spawn.position = position;
                return;
            }
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
            CreateMaterial("Wall", new Color(0.22f, 0.25f, 0.3f), 0.08f);
            CreateMaterial("Ceiling", new Color(0.09f, 0.11f, 0.14f), 0.05f);
            CreateMaterial("LightFixture", new Color(0.72f, 0.82f, 0.95f), 0.35f);
            CreateMaterial("Hub", new Color(0.18f, 0.62f, 0.78f), 0.1f);
        }

        private static bool SceneExists(TestZoneId id) =>
            AssetDatabase.LoadAssetAtPath<SceneAsset>(
                $"{SceneFolder}/TestCampus_{(id == TestZoneId.Hub ? "Core" : SceneSuffix(id))}.unity") != null;

        private static void AddScenesToBuildSettings(bool includeAllScenes)
        {
            List<EditorBuildSettingsScene> scenes = new(EditorBuildSettings.scenes);
            foreach (TestZoneId id in System.Enum.GetValues(typeof(TestZoneId)))
            {
                if (!includeAllScenes && !SceneExists(id))
                    continue;
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
