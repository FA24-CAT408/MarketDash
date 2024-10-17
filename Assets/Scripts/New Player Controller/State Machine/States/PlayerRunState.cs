using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunState : PlayerBaseState
{
    public PlayerRunState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory) { }
    public override void EnterState() { }

    public override void UpdateState()
    {
        Debug.Log("RUNNING");

        if (Ctx.IsMovementPressed)
        {
            // Apply movement based on input
            Ctx.AppliedMovementX = Mathf.Lerp(Ctx.AppliedMovementX, Ctx.CurrentMovementInput.x * Ctx.BaseSpeed * Ctx.RunMultiplier, 0.1f);
            Ctx.AppliedMovementZ = Mathf.Lerp(Ctx.AppliedMovementZ, Ctx.CurrentMovementInput.y * Ctx.BaseSpeed * Ctx.RunMultiplier, 0.1f);
        }
        else
        {
            if (Ctx.CharacterController.isGrounded)
            {
                // Gradually decelerate when input is released
                Ctx.AppliedMovementX = Mathf.Lerp(Ctx.AppliedMovementX, 0, 1f);
                Ctx.AppliedMovementZ = Mathf.Lerp(Ctx.AppliedMovementZ, 0, 1f);
            }
            else
            {
                Debug.Log("SLOWING DOWN - RUN STATE");

                Ctx.AppliedMovementX = Mathf.Lerp(Ctx.AppliedMovementX, Ctx.CurrentMovementInput.x * Ctx.BaseSpeed * Ctx.RunMultiplier, 0.01f);
                Ctx.AppliedMovementZ = Mathf.Lerp(Ctx.AppliedMovementZ, Ctx.CurrentMovementInput.y * Ctx.BaseSpeed * Ctx.RunMultiplier, 0.01f);
            }
        }

        CheckSwitchStates();
    }

    public override void ExitState() { }

    public override void InitializeSubStates() { }

    public override void CheckSwitchStates()
    {
        if (!Ctx.IsMovementPressed && Ctx.HasStoppedMoving)
        {
            SwitchState(Factory.Idle());
        }
        else if (Ctx.IsMovementPressed && !Ctx.IsRunPressed)
        {
            SwitchState(Factory.Walk());
        }
    }
}
