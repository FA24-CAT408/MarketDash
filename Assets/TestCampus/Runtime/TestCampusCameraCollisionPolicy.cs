using UnityEngine;

namespace CrazyMarket.TestCampus
{
    /// <summary>Defines which authored geometry may reposition the assisted camera.</summary>
    [DisallowMultipleComponent]
    public sealed class TestCampusCameraCollisionPolicy : MonoBehaviour
    {
        [SerializeField] private LayerMask obstacleLayers;
        [SerializeField] private LayerMask surfaceLayers;

        public LayerMask ObstacleLayers => obstacleLayers;
        public LayerMask SurfaceLayers => surfaceLayers;

        public void Configure(LayerMask obstacles, LayerMask surfaces)
        {
            obstacleLayers = obstacles;
            surfaceLayers = surfaces;
        }
    }
}
