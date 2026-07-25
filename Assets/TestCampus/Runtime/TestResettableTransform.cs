using UnityEngine;

namespace CrazyMarket.TestCampus
{
    public sealed class TestResettableTransform : MonoBehaviour, ITestResettable
    {
        private Vector3 _position;
        private Quaternion _rotation;
        private Vector3 _scale;
        private bool _active;

        public void CaptureInitialState()
        {
            _position = transform.position;
            _rotation = transform.rotation;
            _scale = transform.localScale;
            _active = gameObject.activeSelf;
        }

        public void ResetToInitialState()
        {
            transform.SetPositionAndRotation(_position, _rotation);
            transform.localScale = _scale;
            gameObject.SetActive(_active);
            if (TryGetComponent<Rigidbody>(out Rigidbody body))
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
    }
}
