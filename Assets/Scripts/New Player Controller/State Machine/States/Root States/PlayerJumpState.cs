using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerBaseState, IRootState
{
    public PlayerJumpState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }

    public override void EnterState()
    {
        HandleJump();
        InitializeSubStates();
        Ctx.RequireNewJumpPress = true;
        Debug.Log("ENTERED JUMP STATE");
    }

    public override void UpdateState()
    {
        HandleGravity();
        HandleHorizontalMomentum();
        CheckSwitchStates();
    }

    public override void ExitState()
    {
        Ctx.Animator.SetBool(Ctx.IsJumpingHash, false);
        
        if (Ctx.IsJumpPressed)
        {
            Ctx.RequireNewJumpPress = true;
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

    public override void CheckSwitchStates()
    {
        if (Ctx.CharacterController.isGrounded)
        {
            SwitchState(Factory.Grounded());
        }
        else if (Ctx.IsJumpPressed && !Ctx.RequireNewJumpPress)
        {
            if (Ctx.GodMode)
            {
                SwitchState(Factory.Fly()); // Transition to fly state if God Mode is enabled
            }
        }
    }

    void HandleJump()
    {
        Ctx.IsJumping = true;
        Ctx.Animator.SetBool(Ctx.IsJumpingHash, true);
        Ctx.CurrentMovementY = Ctx.InitialJumpVelocity;
        Ctx.AppliedMovementY = Ctx.InitialJumpVelocity;
        Ctx.RequireNewJumpPress = true;
    }

    public void HandleGravity()
    {
        bool isFalling = Ctx.CurrentMovementY <= 0.0f || !Ctx.IsJumpPressed;
        float fallMultiplier = 2.0f;

        if (isFalling)
        {
            float previousYVelocity = Ctx.CurrentMovementY;
            Ctx.CurrentMovementY += Ctx.Gravity * fallMultiplier * Time.deltaTime;
            Ctx.AppliedMovementY = Mathf.Max((previousYVelocity + Ctx.CurrentMovementY) * .5f, -Ctx.InitialJumpVelocity);
        }
        else
        {
            float previousYVelocity = Ctx.CurrentMovementY;
            Ctx.CurrentMovementY += Ctx.Gravity * Time.deltaTime;
            Ctx.AppliedMovementY = (previousYVelocity + Ctx.CurrentMovementY) * .5f;
        }
    }

    private void HandleHorizontalMomentum()
    {
        if (Ctx.IsMovementPressed)
        {
            // Continue using player input for movement if available
            Ctx.AppliedMovementX = Mathf.Lerp(Ctx.AppliedMovementX, Ctx.CurrentMovementInput.x * Ctx.BaseSpeed, 0.1f);
            Ctx.AppliedMovementZ = Mathf.Lerp(Ctx.AppliedMovementZ, Ctx.CurrentMovementInput.y * Ctx.BaseSpeed, 0.1f);
        }
        else
        {
            // Gradual deceleration when input is not pressed
            float decelerationRate = 0.01f;
            Ctx.AppliedMovementX = Mathf.Lerp(Ctx.AppliedMovementX, 0, decelerationRate * Time.deltaTime);
            Ctx.AppliedMovementZ = Mathf.Lerp(Ctx.AppliedMovementZ, 0, decelerationRate * Time.deltaTime);
        }
    }
}
