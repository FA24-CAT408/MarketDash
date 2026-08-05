using System;
using UnityEngine;

public abstract class PlayerBaseState
{
    private bool _isRootState = false;
    private PlayerStateMachine _ctx;
    private PlayerStateFactory _factory;
    private PlayerBaseState _currentSubState;
    private PlayerBaseState _currentSuperState;

    protected bool IsRootState { set { _isRootState = value; } }
    protected PlayerStateMachine Ctx { get { return _ctx; } }
    protected PlayerStateFactory Factory { get { return _factory; } }

    public PlayerBaseState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
    {
        _ctx = currentContext;
        _factory = playerStateFactory;
    }

    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();
    public abstract void CheckSwitchStates();
    public abstract void InitializeSubStates();

    public void UpdateStates()
    {
        UpdateState();
        if (_currentSubState != null)
        {
            _currentSubState.UpdateStates();
        }
    }

    public string GetStateHierarchy()
    {
        string stateName = GetType().Name;
        if (_currentSubState != null)
        {
            stateName += " -> " + _currentSubState.GetStateHierarchy();
        }
        return stateName;
    }


    protected void SwitchState(PlayerBaseState newState)
    {
        // Guard: If we are a root state, check the state's type against the current state.
        if (_isRootState)
        {
            if (_ctx.CurrentState != null &&
                _ctx.CurrentState.GetType() == newState.GetType())
            {
                Debug.Log("[StateMachine] Already in root state: " +
                          _ctx.CurrentState.GetType().Name);
                return;
            }
        }
        // If we are switching a sub-state and one already exists,
        // check that newState is not the same as the active sub-state.
        else if (_currentSubState != null)
        {
            if (_currentSubState.GetType() == newState.GetType())
            {
                Debug.Log("[StateMachine] Already in sub state: " +
                          _currentSubState.GetType().Name);
                return;
            }
        }

        // // Log the transition using the state hierarchy
        // string currentHierarchy = GetStateHierarchy();
        // string newHierarchy = newState.GetStateHierarchy();
        // Debug.Log("[StateMachine] Switching state from " +
        //           currentHierarchy + " to " + newHierarchy);
        
        Debug.Log($"[StateMachine] Attempting state switch..." +
                  $"\nCurrent State: {GetType().Name}" +
                  $"\nCurrent Hierarchy: {GetStateHierarchy()}" +
                  $"\nIs Root State: {_isRootState}" +
                  $"\nHas Super State: {_currentSuperState != null}" +
                  $"\nTarget State: {newState.GetType().Name}");

        
        ExitState();

        newState.EnterState();

        if (_isRootState)
        {
            _ctx.CurrentState = newState;
        }
        else if (_currentSuperState != null)
        {
            _currentSuperState.SetSubState(newState);
        }
    }
    protected void SetSuperState(PlayerBaseState newSuperState)
    {
        _currentSuperState = newSuperState;
    }

    protected void SetSubState(PlayerBaseState newSubState)
    {
        _currentSubState = newSubState;
        newSubState.SetSuperState(this);
    }
}
