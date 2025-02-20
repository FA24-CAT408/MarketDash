using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum KCCStates
{
    idle,
    run,
    jump,
    fly
}

public class KCCStateFactory
{
    PlayerBrain _context;
    Dictionary<KCCStates, KCCBaseState> _states = new Dictionary<KCCStates, KCCBaseState>();

    public KCCStateFactory(PlayerBrain currentContext)
    {
        _context = currentContext;
        _states[KCCStates.idle] = new KCCIdleState(_context, this);
        // _states[KCCStates.run] = new KCCWalkState(_context, this);
        // _states[KCCStates.jump] = new KCCJumpState(_context, this);
        // _states[KCCStates.fly] = new KCCFlyState(_context, this);
    }

    public KCCBaseState Idle()
    {
        return _states[KCCStates.idle];
    }
    
    public KCCBaseState Run()
    {
        return _states[KCCStates.run];
    }
    
    public KCCBaseState Jump()
    {
        return _states[KCCStates.jump];
    }

    public KCCBaseState Fly()
    {
        return _states[KCCStates.fly];
    }
}
