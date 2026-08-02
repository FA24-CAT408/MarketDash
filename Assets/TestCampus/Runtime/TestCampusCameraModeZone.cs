using System.Collections.Generic;
using UnityEngine;

namespace CrazyMarket.TestCampus
{
    public sealed class TestCampusCameraModeZone : MonoBehaviour
    {
        private readonly HashSet<Collider> _playerColliders = new();

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && _playerColliders.Add(other))
                TestCampusCameraPrototypeController.Instance?.SetGuidedZoneActive(this, true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && _playerColliders.Remove(other)
                && _playerColliders.Count == 0)
                TestCampusCameraPrototypeController.Instance?.SetGuidedZoneActive(this, false);
        }

        private void OnDisable()
        {
            _playerColliders.Clear();
            TestCampusCameraPrototypeController.Instance?.SetGuidedZoneActive(this, false);
        }
    }
}
