using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;

public abstract class KCCBaseState
{
    private PlayerBrain _ctx;
    private KCCStateFactory _factory;
    
    protected PlayerBrain Ctx { get { return _ctx; } }
    protected KCCStateFactory Factory { get { return _factory; } }
    
    public KCCBaseState(PlayerBrain currentContext, KCCStateFactory kccStateFactory)
    {
        _ctx = currentContext;
        _factory = kccStateFactory;
    }
    
    public abstract void EnterState();
    public abstract void UpdateState(float deltaTime);
    public abstract void ExitState();
    public abstract void CheckSwitchStates();

    protected void SwitchState(KCCBaseState newState)
    {
        ExitState();
        EnterState();
        _ctx.CurrentState = newState;
    }

    // Abstract methods for ICharacterController
    public abstract void UpdateRotation(ref Quaternion currentRotation, float deltaTime);
    public abstract void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime);
    public abstract void BeforeCharacterUpdate(float deltaTime);
    public abstract void PostGroundingUpdate(float deltaTime);
    public abstract void AfterCharacterUpdate(float deltaTime);
    public abstract void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport);
    public abstract void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport);
    public abstract void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport);
    public abstract void OnDiscreteCollisionDetected(Collider hitCollider);
}
