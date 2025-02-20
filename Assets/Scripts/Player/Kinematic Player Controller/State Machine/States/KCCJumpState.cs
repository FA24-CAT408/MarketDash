using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;

public class KCCJumpState : KCCBaseState
{
    public KCCJumpState(PlayerBrain currentContext, KCCStateFactory kccStateFactory) : base(currentContext, kccStateFactory)
    {
    }

    public override void EnterState()
    {
        
    }

    public override void UpdateState(float deltaTime)
    {
    }

    public override void ExitState()
    {
    }

    public override void CheckSwitchStates()
    {
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
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
