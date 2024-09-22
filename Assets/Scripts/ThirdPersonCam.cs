using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCam : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform player;
    public Transform playerObj;
    public Rigidbody rb;

    public float rotationSpeed;

    public GameObject thirdPersonCam;

    [Header("Keybinds")]
    public PlayerControls controls;
    private InputAction moveAction;

    private Vector2 moveInput;
    private Vector3 lastInputDir;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        moveAction = controls.Player.Move;
        moveAction.Enable();
        moveAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        // moveAction.canceled += ctx => moveInput = Vector2.zero;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        lastInputDir = playerObj.forward;
    }

    private void Update()
    {
        // rotate orientation
        Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
        orientation.forward = viewDir.normalized;

        float horizontalInput = moveInput.x;
        float verticalInput = moveInput.y;

        Vector3 inputDir = orientation.forward * verticalInput + orientation.right * horizontalInput;


        if (inputDir != Vector3.zero)
        {
            playerObj.forward = Vector3.Slerp(playerObj.forward, inputDir.normalized, Time.deltaTime * rotationSpeed);

        }
        else
        {
            // Maintain the last rotation when there's no input
            playerObj.forward = Vector3.Slerp(playerObj.forward, lastInputDir, Time.deltaTime * rotationSpeed);
        }
    }
}
