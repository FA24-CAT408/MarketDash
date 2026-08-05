# Findings

## F-001 — Interaction path is disconnected

Severity: **high**. Confidence: **confirmed**. Dimensions: correctness, architecture.

The Interact action emits from `InputReader`, but no active subscriber reaches `InteractionController`; the active KCC prefab omits it and no active class implements `IInteractable`. Consequence: interact input has no terminal effect. If the old scanner is restored unchanged, `GetComponent<IInteractable>().Interact()` can dereference null.

Evidence: `Assets/Scripts/Input/InputReader.cs:89`, `Assets/Scripts/Interaction/InteractionController.cs:28`, `Assets/Scripts/Interaction/IInteractable.cs:5`.

Direction: explicitly compose the active interaction adapter and make lookup total.

## F-002 — Completion level identity is offset

Severity: **high**. Confidence: **confirmed**. Dimensions: correctness, persistence.

The manager saves using `currentLevel`, increments it, then staging UI queries the incremented value. The prefab begins at zero while Level 1 is the first gameplay scene. Consequence: Level 1 is persisted as level 0 and completion UI can query the next level's best time.

Evidence: `Assets/Prefabs/Level Components/Managers/Game Manager.prefab:47`, `Assets/Scripts/Managers/GameManager.cs:211`, `Assets/Scripts/Managers/UIManager.cs:254`.

Direction: define one level identifier and pass the completed ID through save and presentation before advancing.

## F-003 — Serialized manager schema has drifted from code

Severity: **high**. Confidence: **strongly-supported**. Dimensions: correctness, operability, documentation drift.

Current `GameManager` and `UIManager` fields differ from prefab/scene property names such as `_playerFreeLook`, `playerFreeLook`, and `transitionCanvas`; new settings/camera fields are absent from the prefab defaults. Current code dereferences those replacements. Consequence: plausible startup null exceptions and non-functional live settings.

Evidence: `Assets/Scripts/Managers/GameManager.cs:22`, `Assets/Prefabs/Level Components/Managers/Game Manager.prefab:47`, `Assets/Scripts/Managers/UIManager.cs:41`, `Assets/Prefabs/Level Components/Managers/UI Manager.prefab:58`, `Assets/Scenes/Levels/Level 4.unity:13815`.

Direction: migrate serialized fields deliberately and add binding validation.

## F-004 — Collection can precede list initialization

Severity: **high**. Confidence: **strongly-supported**. Dimensions: correctness, failure propagation.

The static item event is subscribed before `CreateAndShowList` initializes `collectedItems`. The handler verifies list membership and then directly indexes the dictionary. Consequence: early player collision can raise `KeyNotFoundException` and interrupt item deactivation.

Evidence: `Assets/Scripts/Gameplay/GroceryListManager.cs:18`, `:35`, `:46`, `Assets/Scripts/Item Scripts/Item.cs:85`.

Direction: make collection lookup total and gate collectible activity by lifecycle.

## F-005 — Completion depends on a hidden two-phase protocol

Severity: **medium**. Confidence: **strongly-supported**. Dimensions: architecture, correctness.

Final collection requests `EndGame`, but save and staging only occur under `GameOver`. The connection is not visible in source and relies on serialized trigger/event wiring. Consequence: a missing binding leaves a completed order unsaved and unstaged.

Evidence: `Assets/Scripts/Gameplay/GroceryListManager.cs:62`, `Assets/Scripts/Managers/GameManager.cs:155`, `:190`, `Assets/Prefabs/Level Components/Entrance (Spawn).prefab:3218`.

Direction: encode or validate the transition as an explicit lifecycle contract.

## F-006 — Discrete input callbacks can fire on multiple phases

Severity: **medium**. Confidence: **confirmed**. Dimensions: correctness.

Interact, Crouch, and ToggleDebug emit without checking callback phase, unlike Pause and Submit. Consequence: a press/release can produce repeated toggles or interactions.

Evidence: `Assets/Scripts/Input/InputReader.cs:89`, `:94`, `:119`.

Direction: gate discrete actions on `performed`.

## F-007 — Player disable can retain stale input state

Severity: **medium**. Confidence: **strongly-supported**. Dimensions: correctness, lifecycle.

`Update` returns while movement is disabled, bypassing internal input-vector clearing; held jump state is not reset on disable. Consequence: re-enabling after pause/cutscene can resume stale intent.

Evidence: `Assets/Scripts/Player/Kinematic Player Controller/KCCPlayerController.cs:88`, `:145`, `:186`.

Direction: centralize enable-state transitions and clear latched input explicitly.

## F-008 — Audio crossfades can race

Severity: **medium**. Confidence: **strongly-supported**. Dimensions: correctness, resource lifetime.

Every state change starts a new crossfade coroutine without owning/canceling the previous coroutine as a unit. Older routines can resume and stop or replace the shared `AudioSource` after a newer transition.

Evidence: `Assets/Scripts/Managers/AudioManager.cs:91`, `:148`.

Direction: own one authoritative transition sequence and cancel it before replacement.

## F-009 — SOAP load lacks corrupt-file recovery

Severity: **medium**. Confidence: **strongly-supported**. Dimensions: recovery, local trust boundary.

Vendored load code can pass null/deserialization failure into upgrade logic and only logs outer errors. Consequence: a corrupt local save can repeatedly fail without quarantine, backup, or safe-default recovery.

Evidence: `Assets/ThirdParty/Obvious/Soap/Core/Runtime/ScriptableSave/ScriptableSave.cs:75`, `:129`.

Direction: validate loaded data and provide an explicit recovery policy at the first-party boundary.

## F-010 — Audited connections have no focused tests

Severity: **medium**. Confidence: **confirmed**. Dimension: tests.

No first-party EditMode/PlayMode tests or test assemblies were found for interaction wiring, collection lifecycle, scene bindings, persistence recovery, audio transitions, or KCC state changes. The Unity Test Framework package alone is not test evidence.

Direction: add boundary-focused tests, prioritizing F-001 through F-005.

## Dimension disposition

- Architecture/boundaries: assessed.
- Coupling/cycles/orphans/duplication: assessed; interaction and `LevelSaveData` are orphaned, no material cycle proved.
- Correctness/failure propagation: assessed.
- Security/trust: assessed as local-only save/debug boundaries; no network/auth surface found.
- Performance/resource lifetime: assessed.
- Tests: assessed; absent for the traced connections.
- Operability/configuration/recovery: assessed.
- Documentation drift: assessed in code comments, editor tooling, and serialized schema.
