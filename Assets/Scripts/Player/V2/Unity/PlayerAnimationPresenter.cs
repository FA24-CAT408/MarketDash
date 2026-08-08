using CrazyMarket.Player.V2;
using UnityEngine;

namespace CrazyMarket.Player.V2.Unity
{
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerControllerV2 controller;
        [SerializeField] private Animator animator;
        [SerializeField] private float runningSpeedThreshold = 0.1f;

        private static readonly int IsRunning = Animator.StringToHash("isRunning");
        private static readonly int IsJumping = Animator.StringToHash("isJumping");

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerControllerV2>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (controller == null || animator == null)
                Debug.LogError("PlayerAnimationPresenter requires PlayerControllerV2 and Animator references.", this);
        }

        private void Update()
        {
            if (controller == null || animator == null) return;
            PlayerPresentationState state = controller.Presentation;
            animator.SetBool(IsRunning, state.Grounded && state.PlanarSpeed >= runningSpeedThreshold);
            animator.SetBool(IsJumping, !state.Grounded);
        }
    }
}
