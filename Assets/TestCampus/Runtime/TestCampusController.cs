using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrazyMarket.TestCampus
{
    [DisallowMultipleComponent]
    public sealed class TestCampusController : MonoBehaviour
    {
        [SerializeField] private bool autoLoadDefaultZones = true;
        [SerializeField] private List<TestZoneScene> zoneScenes = new();
        [SerializeField] private Transform playerRoot;
        [SerializeField] private float killPlaneY = -20f;
        [SerializeField] private bool disablePointLightShadows = true;

        private readonly Dictionary<TestZoneId, TestZoneRoot> _zones = new();
        private readonly Dictionary<TestZoneId, HashSet<ITestResettable>> _externalResettables = new();
        private readonly Dictionary<Light, LightShadows> _overriddenPointLightShadows = new();
        private TestZoneId _currentZone = TestZoneId.Hub;
        private string _lastSpawn = "Default";

        public static TestCampusController Instance { get; private set; }
        public bool AutoLoadDefaultZones { get => autoLoadDefaultZones; set => autoLoadDefaultZones = value; }
        public int RegisteredZoneCount => _zones.Count;
        public TestZoneId CurrentZone => _currentZone;
        public Transform PlayerRoot { get => playerRoot; set => playerRoot = value; }
        public IReadOnlyDictionary<TestZoneId, TestZoneRoot> RegisteredZones => _zones;
        public List<TestZoneScene> ZoneScenes => zoneScenes;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Only one TestCampusController may be active.");
                enabled = false;
                return;
            }
            Instance = this;
        }

        private IEnumerator Start()
        {
            if (playerRoot != null && !playerRoot.gameObject.activeSelf)
                playerRoot.gameObject.SetActive(true);

            yield return null;
            foreach (TestZoneRoot root in FindObjectsByType<TestZoneRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                RegisterZone(root);
            if (autoLoadDefaultZones)
                foreach (TestZoneScene zone in zoneScenes)
                    if (zone.LoadByDefault && !IsSceneLoaded(zone.SceneName))
                        yield return LoadZone(zone.Zone);
            ApplyAdditionalLightShadowBudget();
        }

        private void Update()
        {
            if (playerRoot != null && playerRoot.position.y < killPlaneY) RecoverPlayer();
        }

        private void OnDestroy()
        {
            RestoreAdditionalLightShadows();
            if (Instance == this) Instance = null;
        }

        public bool RegisterZone(TestZoneRoot zone)
        {
            if (zone == null) return false;
            if (_zones.TryGetValue(zone.ZoneId, out TestZoneRoot existing) && existing != null && existing != zone)
            {
                Debug.LogError($"Duplicate test zone identifier: {zone.ZoneId}", zone);
                return false;
            }
            _zones[zone.ZoneId] = zone;
            return true;
        }

        public void UnregisterZone(TestZoneRoot zone)
        {
            if (zone != null && _zones.TryGetValue(zone.ZoneId, out TestZoneRoot existing) && existing == zone)
                _zones.Remove(zone.ZoneId);
        }

        public bool IsZoneRegistered(TestZoneId zone) => _zones.ContainsKey(zone);

        public IEnumerator LoadZone(TestZoneId zone)
        {
            TestZoneScene config = zoneScenes.Find(item => item.Zone == zone);
            if (config == null || string.IsNullOrWhiteSpace(config.SceneName))
            {
                Debug.LogError($"No scene configured for test zone {zone}.");
                yield break;
            }
            if (!IsSceneLoaded(config.SceneName))
                yield return SceneManager.LoadSceneAsync(config.SceneName, LoadSceneMode.Additive);
            ApplyAdditionalLightShadowBudget();
        }

        public IEnumerator UnloadZone(TestZoneId zone)
        {
            TestZoneScene config = zoneScenes.Find(item => item.Zone == zone);
            if (config != null && IsSceneLoaded(config.SceneName))
                yield return SceneManager.UnloadSceneAsync(config.SceneName);
        }

        public IEnumerator ReloadZone(TestZoneId zone)
        {
            yield return UnloadZone(zone);
            yield return LoadZone(zone);
        }

        public bool ResetZone(TestZoneId zone)
        {
            if (!_zones.TryGetValue(zone, out TestZoneRoot root) || root == null) return false;
            root.ResetZone();
            if (_externalResettables.TryGetValue(zone, out HashSet<ITestResettable> resettables))
            {
                foreach (ITestResettable resettable in new List<ITestResettable>(resettables))
                {
                    if (resettable is Object unityObject && unityObject == null)
                    {
                        resettables.Remove(resettable);
                        continue;
                    }
                    resettable.ResetToInitialState();
                }
            }
            return true;
        }

        public void RegisterZoneResettable(TestZoneId zone, ITestResettable resettable)
        {
            if (resettable == null) return;
            if (!_externalResettables.TryGetValue(zone, out HashSet<ITestResettable> resettables))
            {
                resettables = new HashSet<ITestResettable>();
                _externalResettables.Add(zone, resettables);
            }
            resettables.Add(resettable);
        }

        public void UnregisterZoneResettable(TestZoneId zone, ITestResettable resettable)
        {
            if (resettable != null
                && _externalResettables.TryGetValue(zone, out HashSet<ITestResettable> resettables))
                resettables.Remove(resettable);
        }

        public void ResetCampus()
        {
            Time.timeScale = 1f;
            foreach (TestZoneId id in System.Enum.GetValues(typeof(TestZoneId))) ResetZone(id);
            ReturnToHub();
        }

        public bool ApplyPreset(string presetId)
        {
            bool applied = false;
            foreach (TestZoneRoot zone in _zones.Values) applied |= zone.ApplyPreset(presetId);
            if (!applied) Debug.LogWarning($"No test zone accepted preset '{presetId}'.");
            return applied;
        }

        public bool ApplyPreset(TestZoneId zone, string presetId)
        {
            bool applied = _zones.TryGetValue(zone, out TestZoneRoot root) && root.ApplyPreset(presetId);
            if (!applied) Debug.LogWarning($"Test zone {zone} did not accept preset '{presetId}'.");
            return applied;
        }

        public Transform ResolveSpawn(TestZoneId zone, string spawnId = "Default")
        {
            return _zones.TryGetValue(zone, out TestZoneRoot root) ? root.ResolveSpawn(spawnId) : null;
        }

        public bool TeleportToZone(TestZoneId zone, string spawnId = "Default")
        {
            Transform spawn = ResolveSpawn(zone, spawnId);
            if (spawn == null || playerRoot == null) return false;
            TestCampusPlayerAdapter adapter = playerRoot.GetComponent<TestCampusPlayerAdapter>();
            if (adapter != null) adapter.TeleportTo(spawn.position, spawn.rotation);
            else playerRoot.SetPositionAndRotation(spawn.position, spawn.rotation);
            _currentZone = zone;
            _lastSpawn = spawnId;
            return true;
        }

        public bool ReturnToHub() => TeleportToZone(TestZoneId.Hub);

        public bool RecoverPlayer()
        {
            if (!TeleportToZone(_currentZone, _lastSpawn)) return ReturnToHub();
            return true;
        }

        private static bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        private void ApplyAdditionalLightShadowBudget()
        {
            if (!disablePointLightShadows) return;

            // Each point light consumes six atlas tiles. Test Campus keeps spot-light
            // shadows and disables point-light shadows so the 512px URP atlas does
            // not repeatedly resize twenty shadow maps while additive zones load.
            foreach (Light light in FindObjectsByType<Light>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!IsTestCampusScene(light.gameObject.scene)
                    || light.type != LightType.Point
                    || light.shadows == LightShadows.None)
                    continue;

                _overriddenPointLightShadows.TryAdd(light, light.shadows);
                light.shadows = LightShadows.None;
            }
        }

        private bool IsTestCampusScene(Scene scene)
        {
            if (!scene.IsValid()) return false;
            if (scene == gameObject.scene) return true;
            return zoneScenes.Exists(zone => zone != null && zone.SceneName == scene.name);
        }

        private void RestoreAdditionalLightShadows()
        {
            foreach ((Light light, LightShadows shadows) in _overriddenPointLightShadows)
                if (light != null)
                    light.shadows = shadows;
            _overriddenPointLightShadows.Clear();
        }
    }
}
