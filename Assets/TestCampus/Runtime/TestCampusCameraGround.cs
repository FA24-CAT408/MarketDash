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
        private const int HitBufferSize = 24;
        private const float MinimumGroundNormalY = 0.5f;

        private static readonly RaycastHit[] Hits = new RaycastHit[HitBufferSize];

        public static int ResolveMask(int authoredMask)
        {
            int excluded = 0;
            excluded |= LayerBit("Ignore Raycast");
            excluded |= LayerBit("Player");
            excluded |= LayerBit("Player (Not See Through)");
            return authoredMask & ~excluded;

            static int LayerBit(string name)
            {
                int layer = LayerMask.NameToLayer(name);
                return layer < 0 ? 0 : 1 << layer;
            }
        }

        public static bool ProbeGroundY(
            Vector3 origin, float searchDistance, float sphereRadius, int mask, out float groundY)
            => Probe(origin, Vector3.down, searchDistance, sphereRadius, mask, out groundY);

        public static bool ProbeCeilingY(
            Vector3 origin, float searchDistance, float sphereRadius, int mask, out float ceilingY)
            => Probe(origin, Vector3.up, searchDistance, sphereRadius, mask, out ceilingY);

        private static bool Probe(
            Vector3 origin, Vector3 direction, float searchDistance, float sphereRadius,
            int mask, out float surfaceY)
        {
            surfaceY = 0f;
            if (searchDistance <= 0f || sphereRadius <= 0f)
                return false;

            bool searchingDown = direction.y < 0f;
            int count = Physics.SphereCastNonAlloc(
                origin, sphereRadius, direction, Hits, searchDistance,
                ResolveMask(mask), QueryTriggerInteraction.Collide);

            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = Hits[i];
                if (hit.distance <= 0f)
                    continue;
                if (searchingDown ? hit.normal.y < MinimumGroundNormalY
                                  : hit.normal.y > -MinimumGroundNormalY)
                    continue;
                if (hit.collider.isTrigger
                    && hit.collider.GetComponentInParent<TestCampusCameraGround>() == null)
                    continue;

                if (!found || (searchingDown ? hit.point.y > surfaceY : hit.point.y < surfaceY))
                {
                    surfaceY = hit.point.y;
                    found = true;
                }
            }

            return found;
        }

        public static bool TryGetMinimumCameraY(
            Vector3 targetPoint, Vector3 desiredCameraPosition, float cameraRadius,
            float clearance, float slack, int mask, out float minimumY)
        {
            minimumY = 0f;
            if (desiredCameraPosition.y >= targetPoint.y)
                return false;

            Vector3 origin = new(desiredCameraPosition.x, targetPoint.y, desiredCameraPosition.z);
            float distance = targetPoint.y - desiredCameraPosition.y + slack;
            if (!ProbeGroundY(origin, distance, cameraRadius, mask, out float groundY))
                return false;

            minimumY = groundY + cameraRadius + clearance;
            return true;
        }

        public static bool TryGetMaximumCameraY(
            Vector3 targetPoint, Vector3 desiredCameraPosition, float cameraRadius,
            float clearance, float slack, int mask, out float maximumY)
        {
            maximumY = 0f;
            if (desiredCameraPosition.y <= targetPoint.y)
                return false;

            Vector3 origin = new(desiredCameraPosition.x, targetPoint.y, desiredCameraPosition.z);
            float distance = desiredCameraPosition.y - targetPoint.y + slack;
            if (!ProbeCeilingY(origin, distance, cameraRadius, mask, out float ceilingY))
                return false;

            maximumY = ceilingY - cameraRadius - clearance;
            return true;
        }
    }
}
