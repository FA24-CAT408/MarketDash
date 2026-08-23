# Player Controller V2 Resources

## Knowledge

- [CrazyMarket: `PlayerControllerV2`](Assets/Scripts/Player/V2/Unity/PlayerControllerV2.cs)
  Primary source for the Unity/KCC adapter: input capture, camera-relative intent, motor callbacks, velocity application, teleports, and effects.
- [CrazyMarket: `PlayerLocomotionMachine`](Assets/Scripts/Player/V2/Core/PlayerLocomotionMachine.cs)
  Primary source for locomotion policy: modes, jump rules, coyote time, jump buffering, abilities, modifiers, queued requests, snapshots, and presentation state.
- [CrazyMarket: player contracts](Assets/Scripts/Player/V2/Core/PlayerContracts.cs)
  Primary source for the controller's vocabulary and boundaries: intent, body observation, locomotion output, snapshot, and public operations.
- [CrazyMarket: player profiles](Assets/Scripts/Player/V2/Data/PlayerProfile.cs)
  Primary source for authored tuning, immutable runtime copies, production defaults, and validation.
- [Bundled KCC: `ICharacterController`](Assets/Other/KinematicCharacterController/Core/ICharacterController.cs)
  Primary package contract for the callbacks KCC invokes on Player Controller V2. Use when diagnosing callback timing or ownership.
- [Bundled KCC: `KinematicCharacterMotor`](Assets/Other/KinematicCharacterController/Core/KinematicCharacterMotor.cs)
  Primary package implementation for grounding, collision resolution, velocity, and motor state. Use only after V2's own adapter/policy boundary has been checked.
- [Unity Input System: `InputAction`](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.4/api/UnityEngine.InputSystem.InputAction.html)
  Official API reference for performed/canceled callbacks and input-update timing. Use when an intent is missing, duplicated, or held incorrectly.

## Wisdom (Communities)

- [Unity Discussions](https://discussions.unity.com/)
  Unity's practitioner community. Use for engine- or KCC-adjacent edge cases after reducing the problem to a minimal reproduction.

## Gaps

- The bundled KCC copy contains API reference pages but no versioned user guide or package manifest, so its exact upstream release is not currently documented.
