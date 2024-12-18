using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    //Singleton
    public static PlayerStateMachine Instance { get; private set; }

    [SerializeField] string _currentStateName;

    //reference variables
    PlayerControls _playerInput;
    CharacterController _characterController;
    Animator _animator;

    int _isRunningHash;
    int _isJumpingHash;

    //Variables to store player input
    Vector2 _currentMovementInput;
    Vector3 _currentMovement;
    // Vector3 _currentRunMovement;
    Vector3 _appliedMovement;
    Vector3 _cameraRelativeMovement;
    bool _isMovementPresed;
    bool _isRunPressed;

    //Constants
    float _rotationFactorPerFrame = 15f;
    bool _hasStoppedMoving;
    [SerializeField] float _baseSpeed = 10.0f;
    [SerializeField] float _runMultiplier = 3.0f;

    //gravity variables
    [SerializeField] float _gravity = -9.8f;

    //jumping variables
    [SerializeField] bool _godMode = false;
    bool _isJumpPressed = false;
    bool _isCrouchPressed = false;
    float _initialJumpVelocity;
    [SerializeField] float _maxJumpHeight = 3.0f;
    [SerializeField] float _maxJumpTime = 0.75f;
    bool _isJumping;
    bool _isFlying;
    bool _requireNewJumpPress;

    // state variables
    PlayerBaseState _currentState;
    PlayerStateFactory _states;

    // getters and setters
    public string CurrentStateName { get { return _currentStateName; } }

    public PlayerBaseState CurrentState
    {
        get { return _currentState; }
        set
        {
            _currentState = value;
            _currentStateName = _currentState.GetStateHierarchy();
        }
    }

    public CharacterController CharacterController { get { return _characterController; } }
    public Animator Animator { get { return _animator; } }
    public int IsJumpingHash { get { return _isJumpingHash; } }
    public int IsRunningHash { get { return _isRunningHash; } }
    public bool GodMode { get { return _godMode; } set { _godMode = value; } }
    public float InitialJumpVelocity { get { return _initialJumpVelocity; } }
    public float Gravity { get { return _gravity; } }
    public bool IsJumpPressed { get { return _isJumpPressed; } }
    public bool IsMovementPressed { get { return _isMovementPresed; } }
    // public bool IsRunPressed { get { return _isRunPressed; } }
    public bool IsCrouchPressed { get { return _isCrouchPressed; } }
    public bool IsJumping { set { _isJumping = value; } }
    public bool IsFlying { set { _isFlying = value; } }
    public bool RequireNewJumpPress { get { return _requireNewJumpPress; } set { _requireNewJumpPress = value; } }
    public float CurrentMovementY { get { return _currentMovement.y; } set { _currentMovement.y = value; } }
    public float AppliedMovementY { get { return _appliedMovement.y; } set { _appliedMovement.y = value; } }
    public float AppliedMovementX { get { return _appliedMovement.x; } set { _appliedMovement.x = value; } }
    public float AppliedMovementZ { get { return _appliedMovement.z; } set { _appliedMovement.z = value; } }
    public bool HasStoppedMoving { get { return _hasStoppedMoving; } }
    // public float RunMultiplier { get { return _runMultiplier; } }
    public float BaseSpeed { get { return _baseSpeed; } set { _baseSpeed = value; } }
    public Vector2 CurrentMovementInput { get { return _currentMovementInput; } }

    private Transform _currentPlatform;
    private Vector3 _platformPreviousPosition;
    private Vector3 _platformVelocity;

    void Awake()
    {
        //Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        _playerInput = new PlayerControls();
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();   
        
        _isRunningHash = Animator.StringToHash("isRunning");
        _isJumpingHash = Animator.StringToHash("isJumping");

        // setup state
        _states = new PlayerStateFactory(this);
        _currentState = _states.Grounded();
        _currentState.EnterState();

        _playerInput.Player.Move.started += OnMovementInput;
        _playerInput.Player.Move.canceled += OnMovementInput;
        _playerInput.Player.Move.performed += OnMovementInput;

        _playerInput.Player.Jump.started += OnJump;
        _playerInput.Player.Jump.canceled += OnJump;

        _playerInput.Player.Crouch.started += OnCrouch;
        _playerInput.Player.Crouch.canceled += OnCrouch;

        SetupJumpVariables();
    }

    void SetupJumpVariables()
    {
        float timeToApex = _maxJumpTime / 2;
        _gravity = -2 * _maxJumpHeight / Mathf.Pow(timeToApex, 2);
        _initialJumpVelocity = 2 * _maxJumpHeight / timeToApex;
    }

    // Start is called before the first frame update
    void Start()
    {
        _characterController.Move(_appliedMovement * Time.deltaTime);
    }

    // Update is called once per frame
    void Update()
    {
        if (DebugController.Instance.ShowConsole) return;

        HandleRotation();
        // HandleAnimation();
        HasPlayerStoppedMoving();
        _currentState.UpdateStates();
        _cameraRelativeMovement = ConvertToCameraSpace(_appliedMovement);

        ApplyPlatformMovement();

        _characterController.Move(_cameraRelativeMovement * Time.deltaTime);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("MovingPlatform"))
        {
            // Attach to the platform
            _currentPlatform = hit.collider.transform;
            _platformPreviousPosition = _currentPlatform.position;
        }
        else
        {
            // Detach from the platform if no longer colliding
            _currentPlatform = null;
        }
    }

    void ApplyPlatformMovement()
    {
        if (_currentPlatform != null)
        {
            // Calculate platform delta movement
            Vector3 platformDelta = _currentPlatform.position - _platformPreviousPosition;

            // Apply platform movement to the player
            _cameraRelativeMovement += platformDelta / Time.deltaTime;

            // Update platform's previous position
            _platformPreviousPosition = _currentPlatform.position;
        }
    }

    void HasPlayerStoppedMoving()
    {
        float threshold = 0.05f;
        _hasStoppedMoving = Mathf.Abs(_appliedMovement.x) < threshold && Mathf.Abs(_appliedMovement.z) < threshold;
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

    void HandleRotation()
    {
        Vector3 positionToLookAt;
        positionToLookAt.x = _cameraRelativeMovement.x;
        positionToLookAt.y = 0.0f;
        positionToLookAt.z = _cameraRelativeMovement.z;
        Quaternion currentRotation = transform.rotation;


        if (_isMovementPresed && positionToLookAt != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, _rotationFactorPerFrame * Time.deltaTime);
        }
    }

    // void HandleAnimation()
    // {
    //     bool isRunning = _animator.GetBool(_isRunningHash);
    //
    //     if (_isMovementPresed)
    //     {
    //         _animator.SetBool(_isRunningHash, true);
    //     } else if (!_isMovementPresed)
    //     {
    //         _animator.SetBool(_isRunningHash, false);
    //     }
    // }

    void OnMovementInput(InputAction.CallbackContext ctx)
    {
        _currentMovementInput = ctx.ReadValue<Vector2>();
        _currentMovement.x = _currentMovementInput.x * _baseSpeed;
        _currentMovement.z = _currentMovementInput.y * _baseSpeed;
        // _currentRunMovement.x = _currentMovementInput.x * _baseSpeed * _runMultiplier;
        // _currentRunMovement.z = _currentMovementInput.y * _baseSpeed * _runMultiplier;
        _isMovementPresed = _currentMovementInput.x != 0 || _currentMovementInput.y != 0;
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        _isJumpPressed = ctx.ReadValueAsButton();
        _requireNewJumpPress = false;
    }

    void OnCrouch(InputAction.CallbackContext ctx)
    {
        _isCrouchPressed = ctx.ReadValueAsButton();
    }

    void OnEnable()
    {
        _playerInput.Player.Enable();
    }

    void OnDisable()
    {
        _currentPlatform = null;
        _playerInput.Player.Disable();
    }
}
