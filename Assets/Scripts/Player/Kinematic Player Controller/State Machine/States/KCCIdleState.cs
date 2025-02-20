using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;

public class KCCIdleState : KCCBaseState
{
    public KCCIdleState(PlayerBrain currentContext, KCCStateFactory kccStateFactory) : base(currentContext, kccStateFactory)
    {
    }

    public override void EnterState()
    {
        Debug.Log("Entering Idle State");
    }

    public override void UpdateState(float deltaTime)
    {
        CheckSwitchStates();
    }

    public override void ExitState()
    {
    }

    public override void CheckSwitchStates()
    {
        if (Ctx.IsJumpPressed)
        {
            SwitchState(Factory.Jump());
        } else if (Ctx.MovementDirection.magnitude > 0f)
        {
            SwitchState(Factory.Run());
        }
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (Ctx.MovementDirection.magnitude > 0)
        {
            Vector3 targetDirection = new Vector3(Ctx.MovementDirection.x, 0, Ctx.MovementDirection.y);
            if (targetDirection != Vector3.zero)
            {
                currentRotation = Quaternion.Slerp(currentRotation, Quaternion.LookRotation(targetDirection), deltaTime * 10f);
            }
        }
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (!Ctx.Motor.GroundingStatus.IsStableOnGround)
        {
            currentVelocity.y += Ctx.Gravity.y * deltaTime; // Apply gravity
        }
        else
        {
            currentVelocity.y = 0; // Reset vertical velocity if grounded
        }
    }

    public override void BeforeCharacterUpdate(float deltaTime)
    {
    }

    public override void PostGroundingUpdate(float deltaTime)
    {
    }

    public override void AfterCharacterUpdate(float deltaTime)
    {
    }

    public override void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public override void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition,
        Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    public override void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }
}
