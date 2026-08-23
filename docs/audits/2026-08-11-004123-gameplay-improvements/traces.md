# Runtime traces

## Start a shopping run

`MainMenuController.PlayGame` loads the next build scene. `GameManager` discovers scene services and requests `LoadingIn`; `UIManager` fades to `PreGame`; crossing the entrance changes to `InProgress`, creates the grocery list, and starts the timer.

Gap: the serialized initial state is already `LoadingIn`, so its entry effect can be skipped.

## Collect and check out

Player collision calls `Item.Interact`, which emits the static collection event and disables the object. `GroceryListManager` marks the exact configured instance, updates its text, and raises `OrderCompleted`. `GameManager` enters `EndGame`, doubles legacy movement speed, and waits for the exit trigger. `GameOver` stops the timer, saves, increments the level counter, animates the order onto staging, and shows results.

## Pause and resume

Pause input opens UI and changes the game to `Pause`. Resume re-enters the previous state. If that state is `InProgress`, `CreateAndShowList` clears progress while already collected world objects remain disabled.

## Campaign progression

The Next Level button loads the next build-index scene. After Level 4, the `GameOver` scene reads all saved entries. Its Try Again button loads Level 1 but does not reset the persistent level counter.

## Player V2 vertical slice

`InputReader` feeds `PlayerControllerV2`, which translates camera-relative intent and body observations into `PlayerLocomotionMachine.Step`; output drives KCC velocity, rotation, jumps, control blocks, modifiers, abilities, snapshots, and teleports. This trail is not yet connected to production game-state, dolly, hazard, or respawn clients.
