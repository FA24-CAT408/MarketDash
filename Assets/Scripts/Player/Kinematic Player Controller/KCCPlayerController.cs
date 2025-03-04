using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;

public enum CharacterState
{
    Idle,
    Grounded,
    Airborne,
    Jumping,
}

public enum BonusOrientationMethod
{
    None,
    TowardsGravity,
    TowardsGroundSlopeAndGravity,
}

public class KCCPlayerController : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor Motor;
    public Animator _animator;
    public InputReader _input;

    private Vector2 _movementDirection;
    private bool _isJumpPressed;

    [Header("Movement Settings")]
    public bool canMove = true;
    public float MaxStableMoveSpeed = 10f;
    public float MaxAirMoveSpeed = 15f;
    public float AirAccelerationSpeed = 15f;
    public float Drag = 0.1f;
    public float StableMovementSharpness = 15f;
    public float OrientationSharpness = 10f;

    [Header("Jump Settings")]
    public int MaxJumpCount = 2;
    public float JumpUpSpeed = 10f;
    public float JumpPreGroundingGraceTime = 0f;
    public float JumpPostGroundingGraceTime = 0f;
    public float CoyoteTimeDuration = 0.2f;
    public bool _requireNewJumpPress = false;

    [Header("Misc")]
    public List<Collider> IgnoredColliders = new List<Collider>();
    public BonusOrientationMethod BonusOrientationMethod = BonusOrientationMethod.None;
    public float BonusOrientationSharpness = 10f;
    public Vector3 Gravity = new Vector3(0, -30f, 0);
    public float IncreasedGravityMultiplier = 2f;

    public CharacterState CurrentCharacterState { get; private set; }

    // Animation Hashes
    private int _isRunningHash;
    private int _isJumpingHash;

    private int _jumpCount = 0;
    private bool _jumpRequested = false;
    private bool _jumpedThisFrame = false;
    private float _timeSinceJumpRequested = Mathf.Infinity;
    private float _timeSinceLastAbleToJump = 0f;
    private Vector3 _internalVelocityAdd = Vector3.zero;
    
    private float _coyoteTimeCounter = 0f;

    private Vector3 _moveInputVector;
    private Vector3 _lookInputVector;

    private void Awake()
    {
        // Handle initial state
        TransitionToState(CharacterState.Grounded);

        // Assign the characterController to the motor
        Motor.CharacterController = this;
        
        _isRunningHash = Animator.StringToHash("isRunning");
        _isJumpingHash = Animator.StringToHash("isJumping");
        
        _input.MoveEvent += HandleMovementInput;
        _input.JumpEvent += HandleJumpInput;
        _input.JumpCancelledEvent += HandleJumpCancelledInput;
    }

    void Update()
    {
        if(!canMove) return;
        
        SetInputs();
    }

    /// <summary>
    /// Handles movement state transitions and enter/exit callbacks
    /// </summary>
    public void TransitionToState(CharacterState newState)
    {
        CharacterState tmpInitialState = CurrentCharacterState;
        OnStateExit(tmpInitialState, newState);
        CurrentCharacterState = newState;
        OnStateEnter(newState, tmpInitialState);
    }

    /// <summary>
    /// Event when entering a state
    /// </summary>
    public void OnStateEnter(CharacterState state, CharacterState fromState)
    {
        switch (state)
        {
            case CharacterState.Grounded:
                _jumpCount = 0; // Reset jump count when grounded
                break;
            case CharacterState.Airborne:
                _animator.SetBool(_isJumpingHash, true);
                break;
            case CharacterState.Jumping:
                _animator.SetBool(_isJumpingHash, true);
                _jumpCount++;
                break;
        }
    }

    /// <summary>
    /// Event when exiting a state
    /// </summary>
    public void OnStateExit(CharacterState state, CharacterState toState)
    {
        switch (state)
        {
            case CharacterState.Airborne:
                _animator.SetBool(_isJumpingHash, false);
                break;
            case CharacterState.Jumping:
                _animator.SetBool(_isJumpingHash, false);
                break;
        }
    }

    /// <summary>
    /// This is called every frame by ExamplePlayer in order to tell the character what its inputs are
    /// </summary>
    public void SetInputs()
    {
        if (!canMove)
        {
            _moveInputVector = Vector3.zero;
            _lookInputVector = Vector3.zero;
            _animator.SetBool(_isRunningHash, false);
            return;
        }
        
        Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(_movementDirection.x, 0f, _movementDirection.y), 1f);
        _moveInputVector = ConvertToCameraSpace(moveInputVector);
        _lookInputVector = _moveInputVector;
        
        _animator.SetBool(_isRunningHash, _moveInputVector.sqrMagnitude > 0f);

        if (_moveInputVector.sqrMagnitude > 0f)
        {
            HandleRotation();
        }

        // Jumping input
        if (_isJumpPressed && !_requireNewJumpPress)
        {
            Debug.Log("JUMP PRESSED");
            _timeSinceJumpRequested = 0f;
            _jumpRequested = true;
        }
    }
    
    void HandleMovementInput(Vector2 movementInput)
    {
        if (!canMove) 
        {
            _movementDirection = Vector2.zero;
            return;
        }
        
        _movementDirection = movementInput;
    }

    void HandleJumpInput()
    {
        if (!canMove) return;
        _isJumpPressed = true;
        _requireNewJumpPress = false;
    }

    void HandleJumpCancelledInput()
    {
        _isJumpPressed = false;
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is called before the character begins its movement update
    /// </summary>
    public void BeforeCharacterUpdate(float deltaTime)
    {
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is where you tell your character what its rotation should be right now. 
    /// This is the ONLY place where you should set the character's rotation
    /// </summary>
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (_lookInputVector.sqrMagnitude > 0f && OrientationSharpness > 0f)
        {
            // Smoothly interpolate from current to target look direction
            Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector,
                1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

            // Set the current rotation (which will be used by the KinematicCharacterMotor)
            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
        }

        Vector3 currentUp = (currentRotation * Vector3.up);
        if (BonusOrientationMethod == BonusOrientationMethod.TowardsGravity)
        {
            // Rotate from current up to invert gravity
            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized,
                1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
        }
        else if (BonusOrientationMethod == BonusOrientationMethod.TowardsGroundSlopeAndGravity)
        {
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                Vector3 initialCharacterBottomHemiCenter =
                    Motor.TransientPosition + (currentUp * Motor.Capsule.radius);

                Vector3 smoothedGroundNormal = Vector3.Slerp(Motor.CharacterUp,
                    Motor.GroundingStatus.GroundNormal, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGroundNormal) * currentRotation;

                // Move the position to create a rotation around the bottom hemi center instead of around the pivot
                Motor.SetTransientPosition(initialCharacterBottomHemiCenter +
                                           (currentRotation * Vector3.down * Motor.Capsule.radius));
            }
            else
            {
                Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized,
                    1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
            }
        }
        else
        {
            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up,
                1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
        }
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is where you tell your character what its velocity should be right now. 
    /// This is the ONLY place where you can set the character's velocity
    /// </summary>
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        // Reset jump state and handle jump requests
        _jumpedThisFrame = false;
        _timeSinceJumpRequested += deltaTime;

        switch (CurrentCharacterState)
        {
            case CharacterState.Grounded:
                HandleGroundedMovement(ref currentVelocity, deltaTime);
                break;
            case CharacterState.Airborne:
            case CharacterState.Jumping:
                HandleAirMovement(ref currentVelocity, deltaTime);
                break;
        }
        
        if (!canMove)
        {
            // Only zero out horizontal velocity, keep vertical for gravity
            currentVelocity.x = 0;
            currentVelocity.z = 0;
        
            // Apply gravity so player falls to ground
            currentVelocity += Gravity * deltaTime;
        
            // Apply drag
            currentVelocity *= (1f / (1f + (Drag * deltaTime)));
        
            // Check if grounded to transition to Grounded state
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                TransitionToState(CharacterState.Grounded);
                _animator.SetBool(_isRunningHash, false);
            }
        
            return;
        }

        // Handle jumping logic
        if (_jumpRequested)
        {
            bool canJump = !_requireNewJumpPress && 
                           ((Motor.GroundingStatus.IsStableOnGround || _coyoteTimeCounter > 0) || 
                            (_jumpCount < MaxJumpCount));
            
            if (canJump)
            {
                // Calculate jump direction before ungrounding
                Vector3 jumpDirection = Motor.CharacterUp;
                if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                {
                    jumpDirection = Motor.GroundingStatus.GroundNormal;
                }

                // Makes the character skip ground probing/snapping on its next update.
                Motor.ForceUnground();

                // Add to the return velocity and reset jump state
                currentVelocity += (jumpDirection * JumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                
                _jumpRequested = false;
                _jumpedThisFrame = true;
                _requireNewJumpPress = true;
                _coyoteTimeCounter = 0f;

                // Transition to the Jumping state
                TransitionToState(CharacterState.Jumping);
            }
        }

        // Take into account additive velocity
        if (_internalVelocityAdd.sqrMagnitude > 0f)
        {
            currentVelocity += _internalVelocityAdd;
            _internalVelocityAdd = Vector3.zero;
        }
    }
    
     private void HandleGroundedMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        // Ground movement logic
        if (Motor.GroundingStatus.IsStableOnGround)
        {
            _animator.SetBool(_isJumpingHash, false);
            float currentVelocityMagnitude = currentVelocity.magnitude;

            Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;

            // Reorient velocity on slope
            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

            // Calculate target velocity
            Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
            Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
            Vector3 targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;

            // Smooth movement velocity
            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime));
        }
        else
        {
            TransitionToState(CharacterState.Airborne);
        }
    }

    private void HandleAirMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        _coyoteTimeCounter -= deltaTime;
        
        // Air movement logic
        _animator.SetBool(_isJumpingHash, true);

        // Add move input
        if (_moveInputVector.sqrMagnitude > 0f)
        {
            Vector3 addedVelocity = _moveInputVector * AirAccelerationSpeed * deltaTime;

            Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

            // Limit air velocity from inputs
            if (currentVelocityOnInputsPlane.magnitude < MaxAirMoveSpeed)
            {
                // Clamp added velocity to make total velocity not exceed max velocity on inputs plane
                Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, MaxAirMoveSpeed);
                addedVelocity = newTotal - currentVelocityOnInputsPlane;
            }
            else
            {
                // Make sure added velocity doesn't go in the direction of the already-exceeding velocity
                if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                {
                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                }
            }

            // Prevent air-climbing sloped walls
            if (Motor.GroundingStatus.FoundAnyGround)
            {
                if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                {
                    Vector3 perpendicularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpendicularObstructionNormal);
                }
            }

            // Apply added velocity
            currentVelocity += addedVelocity;
        }

        // Gravity
        if (currentVelocity.y < 0) // Only increase gravity when falling
        {
            currentVelocity += Gravity * IncreasedGravityMultiplier * deltaTime;
        }
        else
        {
            currentVelocity += Gravity * deltaTime; // Normal gravity when ascending
        }

        // Drag
        currentVelocity *= (1f / (1f + (Drag * deltaTime)));
        
        // Check if grounded to transition back to Grounded state
        if (Motor.GroundingStatus.IsStableOnGround)
        {
            TransitionToState(CharacterState.Grounded);
        }
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is called after the character has finished its movement update
    /// </summary>
    public void AfterCharacterUpdate(float deltaTime)
    {
        if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
        {
            _jumpRequested = false;
        }

        if (Motor.GroundingStatus.IsStableOnGround)
        {

            _timeSinceLastAbleToJump = 0f;
        }
        else
        {
            // Keep track of time since we were last able to jump (for grace period)
            _timeSinceLastAbleToJump += deltaTime;
        }
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        // Handle landing and leaving ground
        if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
        {
            OnLanded();
        }
        else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
        {
            OnLeaveStableGround();
        }
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        if (IgnoredColliders.Count == 0)
        {
            return true;
        }

        if (IgnoredColliders.Contains(coll))
        {
            return false;
        }

        return true;
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    protected void OnLanded()
    {
    }

    protected void OnLeaveStableGround()
    {
        _coyoteTimeCounter = CoyoteTimeDuration;
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }
    
    private void HandleRotation()
    {
        Vector3 positionToLookAt = new Vector3(_moveInputVector.x, 0.0f, _moveInputVector.z);
        Quaternion currentRotation = transform.rotation;

        if (positionToLookAt != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, OrientationSharpness * Time.deltaTime);
        }
    }
    
    Vector3 ConvertToCameraSpace(Vector3 vectorToRotate)
    {
        float currentYValue = vectorToRotate.y;

        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        cameraForward.y = 0;
        cameraRight.y = 0;

        cameraForward = cameraForward.normalized;
        cameraRight = cameraRight.normalized;

        Vector3 cameraForwardZProduct = vectorToRotate.z * cameraForward;
        Vector3 cameraRightXProduct = vectorToRotate.x * cameraRight;

        Vector3 vectorRotatedToCameraSpace = cameraForwardZProduct + cameraRightXProduct;
        vectorRotatedToCameraSpace.y = currentYValue;
        return vectorRotatedToCameraSpace;
    }
}