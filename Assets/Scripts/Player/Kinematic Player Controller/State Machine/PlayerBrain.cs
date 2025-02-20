using System;
using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;

public class PlayerBrain : MonoBehaviour, ICharacterController
{
    //reference variables
    [SerializeField] 
    private KinematicCharacterMotor _motor;
    [SerializeField]
    private InputReader _input;
    
    //Movement
    [SerializeField] private Vector3 _gravity = new Vector3(0, -30f, 0);
    [SerializeField] private float _maxStableMoveSpeed = 10f;
    [SerializeField] private  float _stableMovementSharpness = 15f;
    private Vector2 _movementDirection;
    private bool _isJumpPressed;
    
    KCCBaseState _currentState;
    KCCStateFactory _states;
    
    public KCCBaseState CurrentState
    {
        get { return _currentState; }
        set
        {
            _currentState = value;
        }
    }
    
    //Getters & Setters
    public KinematicCharacterMotor Motor { get { return _motor; } }
    public Vector2 MovementDirection { get { return _movementDirection; } }
    public Vector3 Gravity { get { return _gravity; } }
    public bool IsJumpPressed { get { return _isJumpPressed; } }
    public float MaxStableMoveSpeed { get { return _maxStableMoveSpeed; } }
    public float StableMovementSharpness { get { return _stableMovementSharpness; } }

    private void Start()
    {
        _motor.CharacterController = this;
        _states = new KCCStateFactory(this);

        CurrentState = _states.Idle();
        CurrentState.EnterState();
        
        _input.MoveEvent += HandleMovementInput;
        
        _input.JumpEvent += HandleJumpInput;
        _input.JumpCancelledEvent += HandleJumpCancelledInput;
    }

    void HandleMovementInput(Vector2 movementInput)
    {
        _movementDirection = movementInput;
    }

    void HandleJumpInput()
    {
        _isJumpPressed = true;
    }

    void HandleJumpCancelledInput()
    {
        _isJumpPressed = false;
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        _currentState.BeforeCharacterUpdate(deltaTime);
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        _currentState.UpdateRotation(ref currentRotation, deltaTime);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _currentState.UpdateVelocity(ref currentVelocity, deltaTime);
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        _currentState.PostGroundingUpdate(deltaTime);
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        _currentState.AfterCharacterUpdate(deltaTime);
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        _currentState.OnGroundHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        _currentState.OnMovementHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
        _currentState.ProcessHitStabilityReport(hitCollider, hitNormal, hitPoint, atCharacterPosition, atCharacterRotation, ref hitStabilityReport);
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
        _currentState.OnDiscreteCollisionDetected(hitCollider);
    }
}