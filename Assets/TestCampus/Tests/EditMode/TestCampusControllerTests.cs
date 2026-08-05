using NUnit.Framework;
using UnityEngine;
using CrazyMarket.TestCampus;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
using CrazyMarket.TestCampus.Editor;

namespace CrazyMarket.TestCampus.Tests
{
    public sealed class TestCampusControllerTests
    {
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
