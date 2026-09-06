using UnityEngine;

namespace CrazyMarket.Player.V2.Unity
{
    [DisallowMultipleComponent]
    public sealed class PlayerMovementParticles : MonoBehaviour
    {
        [SerializeField] private PlayerControllerV2 player;
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private float minimumSpeed = .6f;
        [SerializeField] private float particlesPerMetre = 3f;
        private Vector3 previousPosition;

        private void OnEnable() => previousPosition = transform.position;

        private void LateUpdate()
        {
            if (player == null || particles == null) return;
            var snapshot = player.Snapshot;
            bool teleported = (snapshot.ActionFlags & CrazyMarket.Player.V2.PlayerActionFlags.Teleported) != 0 ||
                Vector3.Distance(previousPosition, transform.position) > 3f;
            previousPosition = transform.position;
            if (teleported) particles.Clear();
            float speed = Vector3.ProjectOnPlane(snapshot.Velocity, Vector3.up).magnitude;
            var emission = particles.emission;
            emission.rateOverTime = !teleported && snapshot.StableGrounded &&
                !snapshot.ControlBlocked && speed >= minimumSpeed ? speed * particlesPerMetre : 0f;
        }

        private void OnDisable()
        {
            if (particles == null) return;
            var emission = particles.emission;
            emission.rateOverTime = 0;
            particles.Clear();
        }
    }
}
