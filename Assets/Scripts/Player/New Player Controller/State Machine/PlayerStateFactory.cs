using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum PlayerStates
{
    idle,
    walk,
    grounded,
    jump,
    fall,
    fly,
}

public class PlayerStateFactory
{
    PlayerStateMachine _context;
    Dictionary<PlayerStates, PlayerBaseState> _states = new Dictionary<PlayerStates, PlayerBaseState>();

    public PlayerStateFactory(PlayerStateMachine currentContext)
    {
        _context = currentContext;
        _states[PlayerStates.idle] = new PlayerIdleState(_context, this);
        _states[PlayerStates.walk] = new PlayerWalkState(_context, this);
        _states[PlayerStates.jump] = new PlayerJumpState(_context, this);
        _states[PlayerStates.grounded] = new PlayerGroundedState(_context, this);
        _states[PlayerStates.fall] = new PlayerFallState(_context, this);
        _states[PlayerStates.fly] = new PlayerFlyState(_context, this);
    }

    public PlayerBaseState Idle()
    {
        return _states[PlayerStates.idle];
    }
    public PlayerBaseState Walk()
    {
        return _states[PlayerStates.walk];
    }
    
    public PlayerBaseState Jump()
    {
        return _states[PlayerStates.jump];
    }
    public PlayerBaseState Grounded()
    {
        return _states[PlayerStates.grounded];
    }
    public PlayerBaseState Fall()
    {
        return _states[PlayerStates.fall];
    }

    public PlayerBaseState Fly()
    {
        return _states[PlayerStates.fly];
    }
}
