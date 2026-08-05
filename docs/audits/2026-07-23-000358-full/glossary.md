# Project glossary

- **LoadingIn:** Game state used while scene-local managers are rebound and UI fades in.
- **PreGame:** Ready state before the player crosses the entrance trigger.
- **InProgress:** Active timed grocery run; list creation and timer start occur on entry.
- **EndGame:** Order-complete return/staging phase; movement speed changes but persistence has not occurred.
- **GameOver:** Persistence and presentation phase; timer stops, time saves, and staging begins.
- **Grocery list:** Object-identity collection of requested `Item` instances plus generated TMP status elements.
- **Staging:** Post-run reconstruction of collected item prefabs along a spline before completion UI.
- **InputReader:** ScriptableObject adapter from generated Input System callbacks to project C# events.
- **SOAP:** Vendored ScriptableObject Architecture Pattern package providing JSON-backed `ScriptableSave<T>`.
- **KCC:** Vendored Kinematic Character Controller motor and mover callbacks used by the player and NPCs.
- **Serialized binding:** A component reference, prefab override, or UnityEvent edge stored in Unity YAML rather than expressed as a C# call.
- **Scene event manager:** Scene-local UnityEvent bridge driven by `GameManager` state entry.
