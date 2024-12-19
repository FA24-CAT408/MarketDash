using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFallState : PlayerBaseState, IRootState
{
    private float _coyoteTimeDuration = 0.15f; // How long after falling can the player still jump
    private float _coyoteTimeCounter = 0f;    // Counter to track coyote time
    private float _fallMultiplier = 1.75f; // Apply a multiplier to make falling faster
    public PlayerFallState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }

    public override void EnterState()
    {
        InitializeSubStates();
        Ctx.Animator.SetBool(Ctx.IsJumpingHash, true);
        _coyoteTimeCounter = 0f; // Reset coyote time on enter state
    }
    public override void UpdateState()
    {
        HandleGravity();
        HandleCoyoteTime();
        CheckSwitchStates();
    }

    private void HandleCoyoteTime()
    {
        // Update coyote time if player has left the ground
        if (!Ctx.CharacterController.isGrounded)
        {
            _coyoteTimeCounter += Time.deltaTime;
        }
        else
        {
            _coyoteTimeCounter = 0f; // Reset coyote time when grounded
        }

        // Debug.Log("Coyote Time: " + _coyoteTimeCounter);
    }

    public override void ExitState()
    {
        _coyoteTimeCounter = _coyoteTimeDuration + 1f; // Set coyote time to be greater than duration on exit
        
        Ctx.Animator.SetBool(Ctx.IsJumpingHash, false);
    }

    public void HandleGravity()
    {
        bool applyFullGravity = _coyoteTimeCounter <= _coyoteTimeDuration;

        _fallMultiplier = applyFullGravity ? 2f : 1.75f; // Apply a multiplier to make falling faster

        float previousYVelocity = Ctx.CurrentMovementY;

        Ctx.CurrentMovementY += Ctx.Gravity * _fallMultiplier * Time.deltaTime;
        Ctx.AppliedMovementY = Mathf.Max((previousYVelocity + Ctx.CurrentMovementY) * .5f, -Ctx.InitialJumpVelocity * 2f);

        Debug.Log("Applying Gravity: " + applyFullGravity);

    }

    public override void CheckSwitchStates()
    {
        if ((Ctx.CharacterController.isGrounded || _coyoteTimeCounter <= _coyoteTimeDuration) && Ctx.IsJumpPressed && !Ctx.RequireNewJumpPress)
        {
            Debug.Log("Coyote Time Jump Triggered");
            SwitchState(Factory.Jump());
        }
        else if (Ctx.IsJumpPressed && Ctx.GodMode)
        {
            SwitchState(Factory.Fly());
        }
        else if (Ctx.CharacterController.isGrounded)
        {
            SwitchState(Factory.Grounded());
        }
    }

    public override void InitializeSubStates()
    {
        if (!Ctx.IsMovementPressed && Ctx.HasStoppedMoving)
        {
            SetSubState(Factory.Idle());
        }
        else if (Ctx.IsMovementPressed)
        {
            SetSubState(Factory.Walk());
        }
        // else
        // {
        //     SetSubState(Factory.Run());
        // }
    }
}