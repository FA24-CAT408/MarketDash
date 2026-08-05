# Architectural decisions

These are observed or inferred from implementation; they are not claims of original author intent.

## Persistent global state with scene-local projections

Status: observed. Confidence: confirmed.

`GameManager` survives scene loads and owns state, while list, timer, UI, and event managers are scene-local and rebound after load. This centralizes lifecycle control but creates singleton and binding dependence.

Evidence: `Assets/Scripts/Managers/GameManager.cs:51`, `:64`, `:72`.

## Serialized UnityEvents as the scene adaptation layer

Status: inferred. Confidence: strongly-supported.

`SceneEventManager` exposes state-specific UnityEvents, and level scenes override them to call player/UI behaviors. This permits per-level variation without new coordinator code, while moving critical edges outside compiler checks.

Evidence: `Assets/Scripts/Managers/SceneEventManager.cs:7`, `Assets/Scenes/Levels/Level 4.unity:20715`.

## ScriptableObject adapters for cross-scene input and persistence

Status: observed. Confidence: confirmed.

Input events and save/settings state live in ScriptableObjects, decoupling scene objects from generated input and filesystem details. Object lifetime and event unsubscription become explicit concerns.

Evidence: `Assets/Scripts/Input/InputReader.cs:7`, `Assets/Scripts/SOAP/Game Data/GameSaveManager.cs:7`, `Assets/Scripts/SOAP/Game Settings/GameSettingsManager.cs:6`.

## Vendor engines behind narrow first-party adapters

Status: observed. Confidence: confirmed.

First-party components adapt KCC, Cinemachine, Splines, DOTween, and SOAP rather than duplicating their implementations. The audit follows each dependency only far enough to establish its terminal effect or recovery behavior.

Evidence: `Packages/manifest.json`, `Assets/Scripts/Player/Kinematic Player Controller/KCCPlayerController.cs:20`, `Assets/Scripts/NPC/NPCController.cs:7`.
