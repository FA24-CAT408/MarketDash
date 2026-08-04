using System.Collections.Generic;
using UnityEngine;

namespace CrazyMarket.TestCampus
{
    /// <summary>Hides explicitly marked geometry between the orbit target and live camera.</summary>
    [DisallowMultipleComponent]
    public sealed class TestCampusCameraOcclusionController : MonoBehaviour
    {
        [SerializeField] private float probeRadius = 0.22f;

        private readonly HashSet<TestCampusSelectiveOccluder> _hidden = new();
        private readonly HashSet<TestCampusSelectiveOccluder> _nextHidden = new();

        private void OnDisable() => RestoreOccluders();

        public void UpdateOcclusion(Transform player, Vector3 targetOffset, bool orbitLive)
        {
            if (player == null || Camera.main == null || !orbitLive)
            {
                RestoreOccluders();
                return;
            }

            Vector3 target = player.position + targetOffset;
            Vector3 direction = Camera.main.transform.position - target;
            float distance = direction.magnitude;
            _nextHidden.Clear();
            if (distance > 0.01f)
            {
                RaycastHit[] hits = Physics.SphereCastAll(
                    target, probeRadius, direction / distance, distance,
                    ~0, QueryTriggerInteraction.Ignore);
                foreach (RaycastHit hit in hits)
                {
                    TestCampusSelectiveOccluder occluder =
                        hit.collider.GetComponentInParent<TestCampusSelectiveOccluder>();
                    if (occluder != null)
                        _nextHidden.Add(occluder);
                }
            }

            foreach (TestCampusSelectiveOccluder old in _hidden)
                if (old != null && !_nextHidden.Contains(old))
                    old.SetOccluded(false);
            foreach (TestCampusSelectiveOccluder current in _nextHidden)
                if (current != null)
                    current.SetOccluded(true);

            _hidden.Clear();
            foreach (TestCampusSelectiveOccluder item in _nextHidden)
                _hidden.Add(item);
        }

        public void RestoreOccluders()
        {
            foreach (TestCampusSelectiveOccluder item in _hidden)
                if (item != null)
                    item.SetOccluded(false);
            _hidden.Clear();
            _nextHidden.Clear();
        }
    }
}
