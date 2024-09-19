using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class PlayerMovementTutorial : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;

    public float groundDrag;
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    public float walkSpeed;
    public float sprintSpeed;

    [Header("Keybinds")]
    public PlayerControls controls;
    private InputAction movement;
    private InputAction jump;
    private InputAction sprint;

    // public KeyCode jumpKey = KeyCode.Space;
    // public KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    public Transform orientation;

    // float horizontalInput;
    // float verticalInput;

    bool isSprinting;
    Vector2 movementInput;
    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState currentMovementState;

    public enum MovementState
    {
        Walking,
        Sprinting,
        Air
    }

    private void StateHandler()
    {
        if (grounded && isSprinting)
        {
            currentMovementState = MovementState.Sprinting;
            moveSpeed = sprintSpeed;
        }
        else if (grounded)
        {
            currentMovementState = MovementState.Walking;
            moveSpeed = walkSpeed;
        }
        else
        {
            currentMovementState = MovementState.Air;
        }
    }

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        movement = controls.Player.Move;
        movement.Enable();
        movement.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
        movement.canceled += ctx => movementInput = Vector2.zero;


        jump = controls.Player.Jump;
        jump.Enable();


        sprint = controls.Player.Sprint;
        sprint.Enable();
        sprint.performed += ctx => isSprinting = true;
        sprint.canceled += ctx => isSprinting = false;


    }

    private void OnDisable()
    {
        movement.Disable();
        jump.Disable();
        sprint.Disable();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;
    }

    private void Update()
    {
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);

        SpeedControl();
        StateHandler();

        // handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && readyToJump && grounded)
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * movementInput.y + orientation.right * movementInput.x;

        // on ground
        if (grounded)
            rb.AddForce(10f * moveSpeed * moveDirection.normalized, ForceMode.Force);

        // in air
        else if (!grounded)
            rb.AddForce(10f * airMultiplier * moveSpeed * moveDirection.normalized, ForceMode.Force);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // limit velocity if needed
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        readyToJump = true;
    }
}