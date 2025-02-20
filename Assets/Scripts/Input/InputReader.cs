using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "InputReader")]
public class InputReader : ScriptableObject, PlayerControls.IPlayerActions
{
    private PlayerControls _playerControls;

    private void OnEnable()
    {
        if (_playerControls == null)
        {
            _playerControls = new PlayerControls();
            
            _playerControls.Player.SetCallbacks(this);
            
            SetPlayerControls();
        }
    }

    public void SetPlayerControls()
    {
        _playerControls.Player.Enable();
    }
    
    public event Action<Vector2> MoveEvent;
    
    public event Action JumpEvent;
    public event Action JumpCancelledEvent;

    public event Action CrouchEvent;
    
    public event Action InteractEvent;
    
    public event Action ToggleDebugModeEvent;
    

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext context)
    {
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            JumpEvent?.Invoke();
        }

        if (context.phase == InputActionPhase.Canceled)
        {
            JumpCancelledEvent?.Invoke();
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        InteractEvent?.Invoke();
    }

    public void OnRestart(InputAction.CallbackContext context)
    {
    }

    public void OnToggleDebug(InputAction.CallbackContext context)
    {
        ToggleDebugModeEvent?.Invoke();
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        CrouchEvent?.Invoke();
    }

    public void OnSwapTargets(InputAction.CallbackContext context)
    {
    }
}
