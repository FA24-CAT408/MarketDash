# Traces

## T-PLAY — launch to active grocery run

Main-menu Play loads the next build-index scene. Persistent `GameManager` enters `LoadingIn`, discovers scene managers, and emits state. `UIManager` completes the fade and requests `PreGame`. Player collision with `EntranceController` requests `InProgress`; the manager creates the grocery list, starts the timer, and raises scene events.

Evidence: `ProjectSettings/EditorBuildSettings.asset:7`, `Assets/Scripts/Managers/MainMenuController.cs:119`, `Assets/Scripts/Managers/GameManager.cs:72`, `Assets/Scripts/Managers/UIManager.cs:157`, `Assets/Scripts/Gameplay/EntranceController.cs:6`.

## T-MOVE — input to KCC motor

`PlayerControls.inputactions` drives generated callbacks. `InputReader.OnMove` emits a vector; `KCCPlayerController` converts it through the current main-camera basis, supplies rotation/velocity through `ICharacterController`, and the vendored motor applies the pose.

Evidence: `Assets/Scripts/Input/InputReader.cs:63`, `Assets/Scripts/Player/Kinematic Player Controller/KCCPlayerController.cs:72`, `:145`, `:353`.

Runtime callback ordering was not exercised.

## T-COMPLETE — item collision to persisted completion

Player collision calls `Item.Interact`, which emits `OnItemCollected`. `GroceryListManager` marks object identity and requests `EndGame` after all entries are true. A serialized trigger is expected to request `GameOver`; only then does `GameManager` stop the timer, save level time, increment the level, and start staging. `StagingAreaController` reconstructs item prefabs along a spline, scales them with DOTween, and displays completion UI.

Evidence: `Assets/Scripts/Item Scripts/Item.cs:69`, `Assets/Scripts/Gameplay/GroceryListManager.cs:35`, `Assets/Scripts/Managers/GameManager.cs:190`, `Assets/Scripts/Gameplay/StagingAreaController.cs:30`.

Gap: the exact `EndGame → GameOver` serialized transition remains unresolved.

## T-PAUSE — input to state restoration

Pause input emits through `InputReader`; `UIManager` opens pause UI and calls `GameManager.PauseGame`. The manager stores the prior state and stops the timer. Unpause re-enters the saved state and restores movement/UI behavior.

Evidence: `Assets/Scripts/Input/InputReader.cs:103`, `Assets/Scripts/Managers/UIManager.cs:112`, `Assets/Scripts/Managers/GameManager.cs:224`.

## T-SETTINGS-AUDIO — UI to disk and live systems

UI sliders/toggles mutate SOAP-backed settings. Each property save writes JSON synchronously; sensitivity/inversion mutate Cinemachine axes, while volume changes `AudioManager`. Game-state events start audio crossfade coroutines whose volume steps use DOTween.

Evidence: `Assets/Scripts/SOAP/Game Settings/GameSettingsManager.cs:9`, `Assets/Scripts/Managers/UIManager.cs:355`, `Assets/Scripts/Managers/AudioManager.cs:55`, `:148`.

## T-NPC — tween time to physics motion

`NPCSplineWalker` registers as the KCC `PhysicsMover` controller. DOTween advances normalized spline time; Splines yields position/tangent; the component returns the world-space goal to KCC. Destruction kills the active tween.

Evidence: `Assets/Scripts/NPC/NPCController.cs:23`, `:36`, `:57`, `:96`.

## T-INTERACT — unresolved handoff

Interact input reaches `InputReader.InteractEvent` and stops. No active subscriber reaches `InteractionController.Interact`, the active KCC prefab omits that component, and no active implementation of `IInteractable` was found. Item pickup is a separate automatic collision path.

Evidence: `Assets/Scripts/Input/InputReader.cs:89`, `Assets/Scripts/Interaction/InteractionController.cs:23`, `Assets/Scripts/Interaction/IInteractable.cs:5`, `Assets/Scripts/Item Scripts/Item.cs:69`.
