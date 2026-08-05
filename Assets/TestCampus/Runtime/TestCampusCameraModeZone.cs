using UnityEngine;

namespace CrazyMarket.TestCampus
{
    public sealed class TestCampusCameraModeZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                TestCampusCameraPrototypeController.Instance?.SetGuidedZoneActive(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
                TestCampusCameraPrototypeController.Instance?.SetGuidedZoneActive(false);
        }
    }
}
