# Mission: Master Player Controller V2

## Why
Build enough architectural and runtime understanding to confidently maintain CrazyMarket's Player Controller V2: modify or extend behavior when needed, simplify it without breaking important seams, and diagnose movement defects systematically.

## Success looks like
- Trace a player action from input through locomotion policy to KCC motor output and presentation.
- Identify the correct layer for a requested change before editing code.
- Add, remove, or simplify locomotion behavior while preserving the controller's contracts.
- Debug movement problems by locating whether the fault is in intent, observation, policy, motor application, tuning, or presentation.

## Constraints
- Learn against the controller and prefab that actually ship in this repository.
- Prefer short, hands-on lessons with immediate feedback and durable reference sheets.

## Out of scope
- Rebuilding the third-party Kinematic Character Controller package itself.
- Studying legacy player-controller implementations unless a comparison helps explain V2.
