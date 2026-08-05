using UnityEngine;

namespace CrazyMarket.TestCampus
{
    /// <summary>
    /// Marks a trigger collider as a camera-only surface. The camera probe can use it while the
    /// player and KCC motor continue to pass through it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TestCampusCameraGround : MonoBehaviour
    {
    }
}
