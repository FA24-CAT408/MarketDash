using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFlyState : PlayerBaseState, IRootState
{
    public PlayerFlyState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }

    public override void EnterState()
    {
        InitializeSubStates();
        Ctx.IsFlying = true;
    }

    public override void UpdateState()
    {
        HandleVerticalMovement();
        HandleHorizontalMovement();
        CheckSwitchStates();
    }

    public override void ExitState()
    {
        Ctx.IsFlying = false;
    }

    public void HandleGravity()
    {
        // NO GRAVITY
    }

    public override void CheckSwitchStates()
    {
        if (!Ctx.GodMode)
        {
            // If GodMode is disabled, switch back to fall or grounded state
            SwitchState(Factory.Fall());
        }
        else if (Ctx.CharacterController.isGrounded)
        {
            SwitchState(Factory.Grounded());
        }
    }

    public override void InitializeSubStates()
    {
        if (!Ctx.IsMovementPressed && Ctx.IsRunPressed && Ctx.HasStoppedMoving)
        {
            SetSubState(Factory.Idle());
        }
        else if (Ctx.IsMovementPressed && !Ctx.IsRunPressed)
        {
            SetSubState(Factory.Walk());
        }
        else
        {
            SetSubState(Factory.Run());
        }
    }

    public void HandleVerticalMovement()
    {
        if (Ctx.IsJumpPressed)
        {
            // Move up when jump is pressed
            Ctx.CurrentMovementY = Ctx.InitialJumpVelocity;
            Ctx.AppliedMovementY = Ctx.InitialJumpVelocity;
        }
        else if (Ctx.IsCrouchPressed)
        {
            // Move down when crouch is pressed
            Ctx.CurrentMovementY = -Ctx.InitialJumpVelocity;
            Ctx.AppliedMovementY = -Ctx.InitialJumpVelocity;
        }
        else
        {
            // Stop vertical movement when no input is given
            Ctx.CurrentMovementY = 0f;
            Ctx.AppliedMovementY = 0f;
        }
    }

    public void HandleHorizontalMovement()
    {
        if (Ctx.IsMovementPressed)
        {
            // Use normal movement controls for horizontal movement while flying
            Ctx.AppliedMovementX = Mathf.Lerp(Ctx.AppliedMovementX, Ctx.CurrentMovementInput.x * Ctx.BaseSpeed * Ctx.RunMultiplier, 0.1f);
            Ctx.AppliedMovementZ = Mathf.Lerp(Ctx.AppliedMovementZ, Ctx.CurrentMovementInput.y * Ctx.BaseSpeed * Ctx.RunMultiplier, 0.1f);
        }
        else
        {
            // Gradual deceleration when no input is pressed
            float decelerationRate = 0.01f;
            Ctx.AppliedMovementX = Mathf.Lerp(Ctx.AppliedMovementX, 0, decelerationRate * Time.deltaTime);
            Ctx.AppliedMovementZ = Mathf.Lerp(Ctx.AppliedMovementZ, 0, decelerationRate * Time.deltaTime);
        }
    }
}
