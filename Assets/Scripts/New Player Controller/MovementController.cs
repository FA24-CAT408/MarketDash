using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    //reference variables
    PlayerControls _playerInput;
    CharacterController _characterController;

    //Variables to store player input
    Vector2 _currentMovementInput;
    Vector3 _currentMovement;
    Vector3 _currenRunMovement;
    Vector3 _appliedMovement;
    bool _isMovementPresed;
    bool _isRunPressed;

    //Constants
    float _rotationFactorPerFrame = 15f;
    float _runMultiplier = 3.0f;

    //gravity variables
    float _gravity = -9.8f;
    float _groundedGravity = -.05f;

    //jumping variables
    bool _isJumpPressed = false;
    float _initialJumpVelocity;
    float _maxJumpHeight = 3.0f;
    float _maxJumpTime = 0.75f;
    bool _isJumping;

    void Awake()
    {
        _playerInput = new PlayerControls();
        _characterController = GetComponent<CharacterController>();

        _playerInput.Player.Move.started += OnMovementInput;
        _playerInput.Player.Move.canceled += OnMovementInput;
        _playerInput.Player.Move.performed += OnMovementInput;

        _playerInput.Player.Sprint.started += onRun;
        _playerInput.Player.Sprint.canceled += onRun;

        _playerInput.Player.Jump.started += onJump;
        _playerInput.Player.Jump.canceled += onJump;

        setupJumpVariables();
    }

    void handleJump()
    {
        if (!_isJumping && _characterController.isGrounded && _isJumpPressed)
        {
            _isJumping = true;
            _currentMovement.y = _initialJumpVelocity;
            _appliedMovement.y = _initialJumpVelocity;
        }
        else if (!_isJumpPressed && _isJumping && _characterController.isGrounded)
        {
            _isJumping = false;
        }
    }

    void setupJumpVariables()
    {
        float timeToApex = _maxJumpTime / 2;
        _gravity = -2 * _maxJumpHeight / Mathf.Pow(timeToApex, 2);
        _initialJumpVelocity = 2 * _maxJumpHeight / timeToApex;
    }

    void onJump(InputAction.CallbackContext ctx)
    {
        _isJumpPressed = ctx.ReadValueAsButton();
    }

    void onRun(InputAction.CallbackContext ctx)
    {
        _isRunPressed = ctx.ReadValueAsButton();
    }

    void handleRotation()
    {
        Vector3 positionToLookAt;
        positionToLookAt.x = _currentMovement.x;
        positionToLookAt.y = 0.0f;
        positionToLookAt.z = _currentMovement.z;
        Quaternion currentRotation = transform.rotation;


        if (_isMovementPresed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, _rotationFactorPerFrame * Time.deltaTime);
        }
    }

    void OnMovementInput(InputAction.CallbackContext ctx)
    {
        _currentMovementInput = ctx.ReadValue<Vector2>();
        _currentMovement.x = _currentMovementInput.x;
        _currentMovement.z = _currentMovementInput.y;
        _currenRunMovement.x = _currentMovementInput.x * _runMultiplier;
        _currenRunMovement.z = _currentMovementInput.y * _runMultiplier;
        _isMovementPresed = _currentMovementInput.x != 0 || _currentMovementInput.y != 0;
    }

    void handleGravity()
    {
        bool isFalling = _currentMovement.y <= 0.0f || !_isJumpPressed;
        float fallMultiplier = 2.0f;

        if (_characterController.isGrounded)
        {
            _currentMovement.y = _groundedGravity;
            _appliedMovement.y = _groundedGravity;
        }
        else if (isFalling)
        {
            float previousYVelocity = _currentMovement.y;
            _currentMovement.y += _gravity * fallMultiplier * Time.deltaTime;
            _appliedMovement.y = Mathf.Max((previousYVelocity + _currentMovement.y) * .5f, -20.0f);
        }
        else
        {
            float previousYVelocity = _currentMovement.y;
            _currentMovement.y = _currentMovement.y + (_gravity * Time.deltaTime);
            _appliedMovement.y = (previousYVelocity + _currentMovement.y) * .5f;
        }
    }

    // Update is called once per frame
    void Update()
    {

        handleRotation();

        if (_isRunPressed)
        {
            _appliedMovement.x = _currenRunMovement.x;
            _appliedMovement.z = _currenRunMovement.z;
        }
        else
        {
            _appliedMovement.x = _currentMovement.x;
            _appliedMovement.z = _currentMovement.z;
        }

        _characterController.Move(_appliedMovement * Time.deltaTime);

        handleGravity();
        handleJump();
    }

    void OnEnable()
    {
        _playerInput.Enable();
    }

    void OnDisable()
    {
        _playerInput.Disable();
    }
}
