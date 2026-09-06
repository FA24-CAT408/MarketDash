using CrazyMarket.Player.V2;
using UnityEngine;

namespace CrazyMarket.Player.V2.Unity
{
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerControllerV2 controller;
        [SerializeField] private Animator animator;
        [SerializeField] private ParticleSystem jumpParticles;
        [SerializeField] private float runningSpeedThreshold = 0.1f;
        [SerializeField] private bool inertialLocomotion;
        [SerializeField, Min(.1f)] private float strideReferenceSpeed = 10f;

        private long lastPresentedRevision = -1;
        private bool hasStrideSpeed;

        private static readonly int IsRunning = Animator.StringToHash("isRunning");
        private static readonly int IsJumping = Animator.StringToHash("isJumping");
        private static readonly int StrideSpeed = Animator.StringToHash("StrideSpeed");

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerControllerV2>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (controller == null || animator == null)
                Debug.LogError("PlayerAnimationPresenter requires PlayerControllerV2 and Animator references.", this);
            if (animator != null && inertialLocomotion)
                foreach (var parameter in animator.parameters)
                    if (parameter.nameHash == StrideSpeed && parameter.type == AnimatorControllerParameterType.Float)
                        hasStrideSpeed = true;
        }

        // Feed parameters before Unity evaluates the Animator for this frame.
        private void Update()
        {
            if (controller == null || animator == null) return;
            PlayerPresentationState state = controller.Presentation;
            bool running = state.Grounded && state.PlanarSpeed >= runningSpeedThreshold;
            if (inertialLocomotion)
            {
                // Residual velocity is a glide, so it must not keep the feet pedaling.
                float threshold = runningSpeedThreshold * (animator.GetBool(IsRunning) ? .65f : 1f);
                running = state.Grounded && state.PlanarSpeed >= threshold &&
                    controller.TryGetMovementIntent(out Vector3 direction) && direction.sqrMagnitude > .0025f;
                if (hasStrideSpeed)
                    animator.SetFloat(StrideSpeed,
                        Mathf.Clamp(state.PlanarSpeed / Mathf.Max(.1f, strideReferenceSpeed), .15f, 3f),
                        .06f, Time.deltaTime);
            }
            animator.SetBool(IsRunning, running);
            animator.SetBool(IsJumping, !state.Grounded);

            PlayerSnapshot snapshot = controller.Snapshot;
            if (snapshot.Revision == lastPresentedRevision) return;
            lastPresentedRevision = snapshot.Revision;
            if (jumpParticles == null || (snapshot.ActionFlags & PlayerActionFlags.Jumped) == 0) return;

            ParticleSystem particles = Instantiate(jumpParticles, transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, 1f);
        }
    }
}
