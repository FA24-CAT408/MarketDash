using UnityEngine;

namespace CrazyMarket.TestCampus
{
    [DisallowMultipleComponent]
    public sealed class TestCampusSelectiveOccluder : MonoBehaviour
    {
        private Renderer[] _renderers;

        private void Awake() => _renderers = GetComponentsInChildren<Renderer>(true);

        public void SetOccluded(bool occluded)
        {
            _renderers ??= GetComponentsInChildren<Renderer>(true);
            foreach (Renderer item in _renderers)
                if (item != null)
                    item.forceRenderingOff = occluded;
        }

        private void OnDisable() => SetOccluded(false);
    }
}
