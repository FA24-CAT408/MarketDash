using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerBaseState
{
    public PlayerIdleState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory) { }
    public override void EnterState()
    {
        Ctx.AppliedMovementX = 0;
        Ctx.AppliedMovementZ = 0;
        
        
    }

    public override void UpdateState()
    {
        CheckSwitchStates();
    }

    public override void ExitState() { }

    public override void InitializeSubStates() { }

    public override void CheckSwitchStates()
    {
        // if (Ctx.IsMovementPressed && Ctx.IsRunPressed && !Ctx.HasStoppedMoving)
        // {
        //     SwitchState(Factory.Run());
        // }
        // else if (Ctx.IsMovementPressed)
        // {
        //     SwitchState(Factory.Walk());
        // }
        
        if (Ctx.IsMovementPressed)
        {
            SwitchState(Factory.Walk());
        }
    }
}
