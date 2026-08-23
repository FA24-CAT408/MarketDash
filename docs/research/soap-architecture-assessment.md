# SOAP architecture assessment

Date: 2026-08-07

## Decision summary

Keep the vendored SOAP package for the two save/settings assets that already depend on `ScriptableSave<T>`, but do not use SOAP in Player Controller V2. Build `PlayerProfile` and ability definitions as project-owned ScriptableObjects, and keep mutable locomotion/HSM state in per-player runtime objects. Reassess SOAP only when the project has a repeated cross-scene data/event problem that SOAP demonstrably simplifies; treat replacement of the current save layer as separate migration work.

## Facts

### What SOAP provides

- SOAP describes itself as a ScriptableObject-based toolkit for modular, reusable, decoupled game systems. Its current documentation covers variables (including runtime-created variables), events, collections/dictionaries, bindings, enums, sub-assets, and JSON-backed `ScriptableSave` assets. The author also cautions that runtime variables are an advanced, fit-dependent pattern and recommends direct calls when components share a local scene/prefab context. [SOAP overview](https://obvious-game.gitbook.io/soap), [runtime variables](https://obvious-game.gitbook.io/soap/soap-core-assets/scriptable-variable/runtime-variables), [bindings](https://obvious-game.gitbook.io/soap/soap-core-assets/bindings), [Scriptable Save](https://obvious-game.gitbook.io/soap/soap-core-assets/scriptable-save)
- The checked-in copy is SOAP `3.5.0`, targets Unity `2019.4`, is stored directly under `Assets`, and is absent from `Packages/manifest.json`; it is therefore vendored project source rather than a Package Manager dependency. The official documentation currently identifies itself as `3.8.0`, so the local copy is behind the documented release line. [local package metadata](../../Assets/Other/Obvious/Soap/package.json), [project package manifest](../../Packages/manifest.json), [official SOAP documentation](https://obvious-game.gitbook.io/soap)
- The vendored package contains 148 C# files: 49 editor files and broad runtime facilities spanning variables, events, lists, dictionaries, bindings, runtime injectors, saves, and supporting types. This count comes from the checked-in source tree. [vendored SOAP source](../../Assets/Other/Obvious/Soap/Core)

### What CrazyMarket uses

- First-party C# has only two substantive SOAP dependencies: `GameSaveManager : ScriptableSave<GameSaveData>` and `GameSettingsManager : ScriptableSave<GameSettingsData>`. `UIManager` imports the namespace but interacts with those project-owned manager types, not SOAP variables/events/collections directly. [GameSaveManager](../../Assets/Scripts/SOAP/GameData/GameSaveManager.cs), [GameSettingsManager](../../Assets/Scripts/SOAP/GameSettings/GameSettingsManager.cs), [UIManager](../../Assets/Scripts/Managers/UIManager.cs)
- Those managers persist level completion/timing and camera/audio settings. Searches of first-party C# found no active use of SOAP's Scriptable Variables, Scriptable Events, Scriptable Lists, Scriptable Dictionaries, bindings, or runtime injection.
- SOAP's local `ScriptableSave<T>` serializes `_saveData` through `JsonUtility` to `<Application.persistentDataPath>/<asset name>.json`; loading is keyed to that same asset name. It exposes manual/interval saving and automatic/manual loading, plus overridable version-upgrade hooks. [local ScriptableSave implementation](../../Assets/Other/Obvious/Soap/Core/Runtime/ScriptableSave/ScriptableSave.cs), [Unity `persistentDataPath`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-persistentDataPath.html)
- Unity's `JsonUtility` follows Unity serialization rules and has format constraints: it serializes fields, supports plain classes/structs for `FromJson`, and does not provide arbitrary JSON/object-graph serialization. Missing fields in current Unity versions retain constructor/initializer values. [Unity `ToJson`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/JsonUtility.ToJson.html), [Unity `FromJson`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/JsonUtility.FromJson.html)
- `ScriptableSave.cs` already has an uncommitted project patch that guards against a destroyed ScriptableObject receiving a queued Editor play-mode callback. This report did not modify it. Updating/reimporting SOAP would have to preserve or supersede that patch. [local ScriptableSave implementation](../../Assets/Other/Obvious/Soap/Core/Runtime/ScriptableSave/ScriptableSave.cs)

### What Player Controller V2 needs

- The agreed domain separates a data-authored `Player Profile` and `Ability Loadout` from per-player mutable state; one Locomotion Mode owns body movement, while abilities submit modifiers, influences, or transition requests. [CrazyMarket domain context](../../CONTEXT.md)
- Unity already supports project-owned ScriptableObject assets as shared, pluggable authoring data. In the Editor, editor tooling can write changes to an asset so they persist between sessions; a deployed build cannot use a ScriptableObject asset as a general save mechanism. [Unity ScriptableObject manual](https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html)

## Inferences

- **SOAP is not needed for V2 profile authoring.** A custom `PlayerProfile : ScriptableObject` supplies the plug-and-play asset workflow without SOAP. An explicit Editor-only **Save to Profile** operation can copy a player's runtime working values into that asset. SOAP's `ScriptableSave` writes a separate JSON save file; it does not inherently implement the desired "commit this tuned runtime copy back into the profile asset" workflow.
- **SOAP's broader variable/event model would duplicate the planned V2 seams.** `Player Intent`, the locomotion HSM, the player interface, and a read-only presentation state already define explicit data flow. Introducing globally referenceable SOAP variables or events for motor speed/state would create additional mutation paths and obscure which player instance owns runtime state.
- **Keeping SOAP now is lower risk than removing it during the controller migration.** The existing saves/settings are small, working consumers, while Player Controller V2 has no dependency on them. Combining both migrations would add save compatibility and regression risk without improving the controller slice.
- **Current SOAP lock-in is narrow but real.** Project code inherits from SOAP base classes, manager assets serialize those script identities, and on-disk filenames depend on asset names. Replacing SOAP must preserve or explicitly migrate the JSON filenames and field schema so existing player data remains readable. Keeping the same bundle identifier also matters for retaining the same persistent data location across builds, per Unity's `persistentDataPath` contract.
- **Maintenance cost is higher than the usage suggests.** CrazyMarket owns a local copy of a commercial framework, is three documented minor versions behind, compiles its auto-referenced runtime assembly, and carries a local vendor edit. This is manageable while the dependency stays stable, but every upgrade must review upstream changes and reconcile the patch.

## Recommendation

| Horizon | Decision | Reason |
|---|---|---|
| Player Controller V2 | Do not reference `Obvious.Soap` | Project-owned profile assets plus per-player runtime state are smaller and keep locomotion authority explicit. |
| Current saves/settings | Keep SOAP `ScriptableSave<T>` for now | Avoid expanding the V2 migration and preserve existing files/assets. Do not rename the manager assets without a save migration. |
| SOAP upgrades | Upgrade only in an isolated branch/change | Compare `3.5.0` to the target release, reapply or retire the local play-mode guard deliberately, and validate save/settings load-save behavior before adoption. |
| Eventual cleanup | Consider replacing only the two persistence consumers, then remove SOAP | This converts a broad 148-file dependency into project-owned persistence while allowing compatibility tests at the seam. Do not delete SOAP first. |

Before removing SOAP, capture representative old JSON files, implement a project-owned persistence interface/adapter, load old files without data loss, round-trip them, validate settings and level times in Editor and a build, then remove the package only after serialized references have migrated.

## Triggers for broader SOAP adoption

Adopt more of SOAP only when at least one of these is observed in production code and a small spike beats a project-owned alternative:

1. Several genuinely independent, cross-scene systems need the same observable value or event and direct references/adapters have become repetitive.
2. Designers repeatedly need Inspector-authored event wiring, variable bindings, or runtime collection inspection across many features, not just the player controller.
3. Multiple runtime instances need dynamically created observable variables, and the team accepts the initialization-order rules identified in SOAP's own runtime-variable documentation.
4. A broader architecture decision standardizes on SOAP, with naming, ownership, reset behavior, debugging, and testing conventions documented for the whole project.

Do not adopt SOAP merely because a value is configurable, because two nearby components communicate, or because an ability changes locomotion. Those cases are already served by `PlayerProfile`, explicit dependencies, and the HSM/player interfaces.
