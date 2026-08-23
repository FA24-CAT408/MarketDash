using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CrazyMarket.TestCampus;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
using CrazyMarket.TestCampus.Editor;

namespace CrazyMarket.TestCampus.Tests
{
    public sealed class TestCampusControllerTests
    {
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

        private GameObject _controllerObject;
        private TestCampusController _controller;

        [SetUp]
        public void SetUp()
        {
            _controllerObject = new GameObject("Test Campus Controller");
            _controller = _controllerObject.AddComponent<TestCampusController>();
            _controller.AutoLoadDefaultZones = false;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_controllerObject);

            foreach (TestZoneRoot zone in Object.FindObjectsByType<TestZoneRoot>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(zone.gameObject);
            }
        }

        [Test]
        public void RegisterZone_MakesZoneAvailableThroughPublicSeam()
        {
            TestZoneRoot movement = CreateZone(TestZoneId.Movement, "Movement");

            bool registered = _controller.RegisterZone(movement);

            Assert.That(registered, Is.True);
            Assert.That(_controller.IsZoneRegistered(TestZoneId.Movement), Is.True);
            Assert.That(_controller.RegisteredZoneCount, Is.EqualTo(1));
        }

        [Test]
        public void RegisterZone_RejectsDuplicateZoneIdentifier()
        {
            TestZoneRoot first = CreateZone(TestZoneId.Camera, "Camera A");
            TestZoneRoot duplicate = CreateZone(TestZoneId.Camera, "Camera B");

            Assert.That(_controller.RegisterZone(first), Is.True);
            LogAssert.Expect(LogType.Error, new Regex("Duplicate test zone identifier"));
            Assert.That(_controller.RegisterZone(duplicate), Is.False);
            Assert.That(_controller.RegisteredZoneCount, Is.EqualTo(1));
        }

        [Test]
        public void ResetZone_OnlyResetsRequestedZone()
        {
            ResetProbe movementProbe = CreateZoneWithProbe(TestZoneId.Movement);
            ResetProbe cameraProbe = CreateZoneWithProbe(TestZoneId.Camera);
            movementProbe.Value = 9;
            cameraProbe.Value = 7;

            bool reset = _controller.ResetZone(TestZoneId.Movement);

            Assert.That(reset, Is.True);
            Assert.That(movementProbe.Value, Is.EqualTo(1));
            Assert.That(cameraProbe.Value, Is.EqualTo(7));
        }

        [Test]
        public void ResolveSpawn_UnknownNameFallsBackToDefault()
        {
            TestZoneRoot zone = CreateZone(TestZoneId.UI, "UI");
            Transform defaultSpawn = new GameObject("Default").transform;
            defaultSpawn.SetParent(zone.transform);
            defaultSpawn.position = new Vector3(2f, 3f, 4f);
            zone.ConfigureSpawn("Default", defaultSpawn);
            _controller.RegisterZone(zone);

            Transform result = _controller.ResolveSpawn(TestZoneId.UI, "Missing");

            Assert.That(result, Is.SameAs(defaultSpawn));
        }

        [Test]
        public void GeneratedCampus_SatisfiesSceneOwnershipContract()
        {
            Assert.That(TestCampusValidator.Validate(), Is.Empty);
        }

        [Test]
        public void BuildExisting_WhenGenerationIsCurrent_DoesNotRewriteScenes()
        {
            Assert.That(TestCampusSceneGenerator.AreExistingScenesCurrent(), Is.True,
                "Regenerate Test Campus scenes and record their generation state before running this test.");
            Dictionary<string, string> before = HashGeneratedScenes();

            TestCampusSceneGenerator.BuildExisting();

            Dictionary<string, string> after = HashGeneratedScenes();
            foreach (string sceneName in GeneratedSceneNames)
                Assert.That(after[sceneName], Is.EqualTo(before[sceneName]), sceneName);
        }

        [Test]
        public void BuildExisting_WhenSceneIsMissingFromBuildSettings_RestoresItWithoutRewritingScenes()
        {
            EditorBuildSettingsScene[] originalScenes = EditorBuildSettings.scenes;
            string missingPath = Path.Combine(TestCampusSceneGenerator.SceneFolder, GeneratedSceneNames[0]);
            List<EditorBuildSettingsScene> incompleteScenes = new(originalScenes);
            incompleteScenes.RemoveAll(scene => scene.path == missingPath);
            Dictionary<string, string> before = HashGeneratedScenes();

            try
            {
                EditorBuildSettings.scenes = incompleteScenes.ToArray();

                TestCampusSceneGenerator.BuildExisting();

                Assert.That(System.Array.Exists(EditorBuildSettings.scenes,
                    scene => scene.path == missingPath && scene.enabled), Is.True);
                Dictionary<string, string> after = HashGeneratedScenes();
                foreach (string sceneName in GeneratedSceneNames)
                    Assert.That(after[sceneName], Is.EqualTo(before[sceneName]), sceneName);
            }
            finally
            {
                EditorBuildSettings.scenes = originalScenes;
            }
        }

        [Test]
        public void BuildExisting_WhenCameraCollisionMatrixDrifts_RestoresItWithoutRewritingScenes()
        {
            int obstacleLayer = LayerMask.NameToLayer("Camera Obstacle");
            const int defaultLayer = 0;
            Assert.That(obstacleLayer, Is.GreaterThanOrEqualTo(0));
            bool originalValue = Physics.GetIgnoreLayerCollision(obstacleLayer, defaultLayer);
            Dictionary<string, string> before = HashGeneratedScenes();

            try
            {
                Physics.IgnoreLayerCollision(obstacleLayer, defaultLayer, false);

                TestCampusSceneGenerator.BuildExisting();

                Assert.That(Physics.GetIgnoreLayerCollision(obstacleLayer, defaultLayer), Is.True);
                Dictionary<string, string> after = HashGeneratedScenes();
                foreach (string sceneName in GeneratedSceneNames)
                    Assert.That(after[sceneName], Is.EqualTo(before[sceneName]), sceneName);
            }
            finally
            {
                Physics.IgnoreLayerCollision(obstacleLayer, defaultLayer, originalValue);
            }
        }

        [Test]
        public void ApplyPreset_UsesRegisteredZoneProvider()
        {
            TestZoneRoot zone = CreateZone(TestZoneId.Integration, "Integration");
            TestZonePresetProvider provider = zone.gameObject.AddComponent<TestZonePresetProvider>();
            zone.RefreshProviders();
            _controller.RegisterZone(zone);

            Assert.That(_controller.ApplyPreset("Stress"), Is.True);
            Assert.That(provider.ActivePreset, Is.EqualTo("Stress"));
        }

        private TestZoneRoot CreateZone(TestZoneId id, string displayName)
        {
            GameObject zoneObject = new GameObject(displayName);
            TestZoneRoot zone = zoneObject.AddComponent<TestZoneRoot>();
            zone.Configure(id, displayName, Color.white);
            return zone;
        }

        private static Dictionary<string, string> HashGeneratedScenes()
        {
            Dictionary<string, string> hashes = new();
            using SHA256 sha256 = SHA256.Create();
            foreach (string sceneName in GeneratedSceneNames)
            {
                string path = Path.Combine(TestCampusSceneGenerator.SceneFolder, sceneName);
                byte[] hash = sha256.ComputeHash(File.ReadAllBytes(path));
                hashes.Add(sceneName, System.BitConverter.ToString(hash).Replace("-", string.Empty));
            }
            return hashes;
        }

        private ResetProbe CreateZoneWithProbe(TestZoneId id)
        {
            TestZoneRoot zone = CreateZone(id, id.ToString());
            ResetProbe probe = zone.gameObject.AddComponent<ResetProbe>();
            probe.Value = 1;
            zone.RefreshProviders();
            _controller.RegisterZone(zone);
            return probe;
        }

        private sealed class ResetProbe : MonoBehaviour, ITestResettable
        {
            private int _initialValue;

            public int Value { get; set; }

            public void CaptureInitialState()
            {
                _initialValue = Value;
            }

            public void ResetToInitialState()
            {
                Value = _initialValue;
            }
        }
    }
}
