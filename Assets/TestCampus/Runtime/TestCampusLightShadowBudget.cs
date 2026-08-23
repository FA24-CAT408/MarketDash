using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrazyMarket.TestCampus
{
    [DisallowMultipleComponent]
    public sealed class TestCampusLightShadowBudget : MonoBehaviour
    {
        [SerializeField] private bool disablePointLightShadows = true;

        private readonly Dictionary<Light, LightShadows> _overrides = new();

        public void Apply()
        {
            if (!disablePointLightShadows) return;

            foreach (Light light in FindObjectsByType<Light>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!IsCampusScene(light.gameObject.scene)
                    || light.type != LightType.Point
                    || light.shadows == LightShadows.None)
                    continue;

                _overrides.TryAdd(light, light.shadows);
                light.shadows = LightShadows.None;
            }
        }

        private bool IsCampusScene(Scene scene)
        {
            if (!scene.IsValid()) return false;
            if (scene == gameObject.scene) return true;
            TestCampusController campus = GetComponent<TestCampusController>();
            return campus != null && campus.ZoneScenes.Exists(
                zone => zone != null && zone.SceneName == scene.name);
        }

        private void OnDestroy()
        {
            foreach ((Light light, LightShadows shadows) in _overrides)
                if (light != null)
                    light.shadows = shadows;
            _overrides.Clear();
        }
    }
}
