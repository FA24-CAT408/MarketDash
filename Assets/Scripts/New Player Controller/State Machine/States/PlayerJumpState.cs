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
    }

    public override void UpdateState()
    {
        HandleGravity();
        HandleHorizontalMomentum();
        CheckSwitchStates();
    }

    public override void ExitState()
    {
        if (Ctx.IsJumpPressed)
        {
            Ctx.RequireNewJumpPress = true;
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

    public override void CheckSwitchStates()
    {
        if (Ctx.CharacterController.isGrounded)
        {
            SwitchState(Factory.Grounded());
        }
    }

    void HandleJump()
    {
        Ctx.IsJumping = true;
        Ctx.CurrentMovementY = Ctx.InitialJumpVelocity;
        Ctx.AppliedMovementY = Ctx.InitialJumpVelocity;
    }

    public void HandleGravity()
    {
        bool isFalling = Ctx.CurrentMovementY <= 0.0f || !Ctx.IsJumpPressed;
        float fallMultiplier = 2.0f;

        if (isFalling)
        {
            float previousYVelocity = Ctx.CurrentMovementY;
            Ctx.CurrentMovementY += Ctx.Gravity * fallMultiplier * Time.deltaTime;
            Ctx.AppliedMovementY = Mathf.Max((previousYVelocity + Ctx.CurrentMovementY) * .5f, -20.0f);
        }
        else
        {
            float previousYVelocity = Ctx.CurrentMovementY;
            Ctx.CurrentMovementY = Ctx.CurrentMovementY + (Ctx.Gravity * Time.deltaTime);
            Ctx.AppliedMovementY = (previousYVelocity + Ctx.CurrentMovementY) * .5f;
        }
    }

    private void HandleHorizontalMomentum()
    {
        if (Ctx.IsMovementPressed)
        {
            // Debug.Log("MOVEMENT IS PRESSED");
            // Debug.Log("Applied Movement X: " + Ctx.AppliedMovementX);
            // Debug.Log("Applied Movement Z: " + Ctx.AppliedMovementZ);
            // Debug.Log("==========");

            // Continue using player input for movement if available
            Ctx.AppliedMovementX = Mathf.Lerp(Ctx.AppliedMovementX, Ctx.CurrentMovementInput.x * Ctx.BaseSpeed * Ctx.RunMultiplier, 0.1f);
            Ctx.AppliedMovementZ = Mathf.Lerp(Ctx.AppliedMovementZ, Ctx.CurrentMovementInput.y * Ctx.BaseSpeed * Ctx.RunMultiplier, 0.1f);
        }
        else
        {
            // Debug.Log("MOVEMENT IS NOT PRESSED");
            // Debug.Log("Applied Movement X: " + Ctx.AppliedMovementX);
            // Debug.Log("Applied Movement Z: " + Ctx.AppliedMovementZ);

            // Gradual deceleration when input is not pressed
            float decelerationRate = 0.01f;
            Debug.Log("Decelerating - JUMP STATE");
            Ctx.AppliedMovementX = Mathf.Lerp(Ctx.AppliedMovementX, 0, decelerationRate * Time.deltaTime);
            Ctx.AppliedMovementZ = Mathf.Lerp(Ctx.AppliedMovementZ, 0, decelerationRate * Time.deltaTime);
        }
    }
}
