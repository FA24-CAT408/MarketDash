using UnityEngine;

namespace CrazyMarket.TestCampus
{
    /// <summary>Restores a fixture's active state and restarts its normal enable lifecycle.</summary>
    [DisallowMultipleComponent]
    public sealed class TestResettableActivation : MonoBehaviour, ITestResettable
    {
        private bool _initiallyActive;

        public void CaptureInitialState()
        {
            _initiallyActive = gameObject.activeSelf;
        }

        public void ResetToInitialState()
        {
            if (_initiallyActive && gameObject.activeSelf)
                gameObject.SetActive(false);

            gameObject.SetActive(_initiallyActive);
        }
    }
}
