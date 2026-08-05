using UnityEngine;

namespace CrazyMarket.TestCampus
{
    /// <summary>
    /// Marks a collider as camera-only ground. Trigger colliders carrying this marker are treated
    /// as walkable surfaces by the camera ground probe while remaining invisible to the player and
    /// to the KCC motor. Used by the hub camera aprons, which read as floor but deliberately do not
    /// collide, so the camera used to sink below the level when it swung outside the room.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TestCampusCameraGround : MonoBehaviour
    {
        private const int HitBufferSize = 24;

        // Matches the KCC motor's MaxStableSlopeAngle of 60 degrees, so the probe accepts every
        // surface the player can actually stand on and rejects vertical faces such as walls and
        // columns. Without this a downward sweep that grazes a column's side face would report the
        // grazing point as ground and launch the camera up the column.
        private const float MinimumGroundNormalY = 0.5f;

        private static readonly RaycastHit[] Hits = new RaycastHit[HitBufferSize];

        /// <summary>
        /// Strips the layers the camera must never rest on. The player's own capsule would
        /// otherwise be a perfectly valid "ground" hit directly beneath the orbit target.
        /// </summary>
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

        /// <summary>
        /// Finds the highest walkable surface directly beneath <paramref name="origin"/>.
        /// </summary>
        /// <remarks>
        /// Callers must pass an origin at or below the orbit target's height and sweep downward
        /// only. That is what makes a permissive layer mask safe here: the sweep can never reach
        /// the hub ceiling or the Movement gym's low ceiling, which is exactly the failure mode
        /// that makes Cinemachine's own TerrainResolution unusable in this scene.
        /// </remarks>
        public static bool ProbeGroundY(
            Vector3 origin, float searchDistance, float sphereRadius, int mask, out float groundY)
            => Probe(origin, Vector3.down, searchDistance, sphereRadius, mask, out groundY);

        /// <summary>
        /// Finds the lowest ceiling underside directly above <paramref name="origin"/>.
        /// </summary>
        /// <remarks>
        /// The mirror of <see cref="ProbeGroundY"/>: callers must pass an origin at or above the
        /// orbit target's height and sweep upward only, so a floor can never be mistaken for a
        /// ceiling.
        /// </remarks>
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

                // A zero distance means the sweep started already overlapping this collider, in
                // which case Unity leaves point and normal undefined.
                if (hit.distance <= 0f)
                    continue;

                // Accept only surfaces facing back along the sweep: floors face up, ceilings face
                // down. Without this a sweep that grazes a wall's vertical face would report the
                // grazing point as a surface.
                if (searchingDown ? hit.normal.y < MinimumGroundNormalY
                                  : hit.normal.y > -MinimumGroundNormalY)
                    continue;
                if (hit.collider.isTrigger
                    && hit.collider.GetComponentInParent<TestCampusCameraGround>() == null)
                    continue;

                // Nearest surface wins: the highest floor below, the lowest ceiling above.
                if (!found || (searchingDown ? hit.point.y > surfaceY : hit.point.y < surfaceY))
                {
                    surfaceY = hit.point.y;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// Computes the lowest height the camera may occupy at <paramref name="desiredCameraPosition"/>
        /// without dropping below the walkable surface under it. Returns false when the camera is
        /// at or above the target, or when there is genuinely no ground beneath it — over a real
        /// drop the camera is left free to dip.
        /// </summary>
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

        /// <summary>
        /// Computes the highest height the camera may occupy at
        /// <paramref name="desiredCameraPosition"/> without rising through the ceiling above it.
        /// Returns false when the camera is at or below the target, or when there is genuinely no
        /// ceiling above it — outdoors the camera is left free to rise.
        /// </summary>
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
