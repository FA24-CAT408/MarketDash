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
    DebugController _debugController;

    //Variables to store player input
    Vector2 _currentMovementInput;
    Vector3 _currentMovement;
    Vector3 _currenRunMovement;
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
    bool _isJumpPressed = false;
    float _initialJumpVelocity;
    [SerializeField] float _maxJumpHeight = 3.0f;
    [SerializeField] float _maxJumpTime = 0.75f;
    bool _isJumping;
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
    public float InitialJumpVelocity { get { return _initialJumpVelocity; } }
    public float Gravity { get { return _gravity; } }
    public bool IsJumpPressed { get { return _isJumpPressed; } }
    public bool IsMovementPressed { get { return _isMovementPresed; } }
    public bool IsRunPressed { get { return _isRunPressed; } }
    public bool IsJumping { set { _isJumping = value; } }
    public bool RequireNewJumpPress { get { return _requireNewJumpPress; } set { _requireNewJumpPress = value; } }
    public float CurrentMovementY { get { return _currentMovement.y; } set { _currentMovement.y = value; } }
    public float AppliedMovementY { get { return _appliedMovement.y; } set { _appliedMovement.y = value; } }
    public float AppliedMovementX { get { return _appliedMovement.x; } set { _appliedMovement.x = value; } }
    public float AppliedMovementZ { get { return _appliedMovement.z; } set { _appliedMovement.z = value; } }
    public bool HasStoppedMoving { get { return _hasStoppedMoving; } }
    public float RunMultiplier { get { return _runMultiplier; } }
    public float BaseSpeed { get { return _baseSpeed; } set { _baseSpeed = value; } }
    public Vector2 CurrentMovementInput { get { return _currentMovementInput; } }

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
        _debugController = GetComponent<DebugController>();

        // setup state
        _states = new PlayerStateFactory(this);
        _currentState = _states.Grounded();
        _currentState.EnterState();

        _playerInput.Player.Move.started += OnMovementInput;
        _playerInput.Player.Move.canceled += OnMovementInput;
        _playerInput.Player.Move.performed += OnMovementInput;

        _playerInput.Player.Sprint.started += OnRun;
        _playerInput.Player.Sprint.canceled += OnRun;

        _playerInput.Player.Jump.started += OnJump;
        _playerInput.Player.Jump.canceled += OnJump;

        SetupJumpVariables();

        //Turn off cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        if (_debugController.ShowConsole) return;

        HandleRotation();
        HasPlayerStoppedMoving();
        _currentState.UpdateStates();
        _cameraRelativeMovement = ConvertToCameraSpace(_appliedMovement);
        _characterController.Move(_cameraRelativeMovement * Time.deltaTime);
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

    void OnMovementInput(InputAction.CallbackContext ctx)
    {
        _currentMovementInput = ctx.ReadValue<Vector2>();
        _currentMovement.x = _currentMovementInput.x * _baseSpeed;
        _currentMovement.z = _currentMovementInput.y * _baseSpeed;
        _currenRunMovement.x = _currentMovementInput.x * _baseSpeed * _runMultiplier;
        _currenRunMovement.z = _currentMovementInput.y * _baseSpeed * _runMultiplier;
        _isMovementPresed = _currentMovementInput.x != 0 || _currentMovementInput.y != 0;
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        _isJumpPressed = ctx.ReadValueAsButton();
        _requireNewJumpPress = false;
    }

    void OnRun(InputAction.CallbackContext ctx)
    {
        _isRunPressed = ctx.ReadValueAsButton();
    }

    void OnEnable()
    {
        _playerInput.Player.Enable();
    }

    void OnDisable()
    {
        _playerInput.Player.Disable();
    }
}
