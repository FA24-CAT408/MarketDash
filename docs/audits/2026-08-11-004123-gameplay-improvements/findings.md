# Prioritized improvements

## Now — make the current loop trustworthy

### F-001 Preserve shopping progress across pause

**High, confirmed.** Resuming `InProgress` re-runs order initialization, clearing collected flags while collected objects remain disabled. Consequence: a paused run can become impossible. Evidence: `Assets/Scripts/Managers/GameManager.cs:254`, `:267`; `Assets/Scripts/Gameplay/GroceryListManager.cs:56`; `Assets/Scripts/Items/Item.cs:85`.

### F-002 Replace the mutable level counter with a completed-run result

**High, confirmed.** Save, UI, restart, and next-level behavior share a mutable `currentLevel`. It increments before results UI reads it; campaign Try Again does not reset it; serialized storage begins at zero. Consequences include Level 0 labels, wrong best-time comparisons, and replaying Level 1 under Level 4. Evidence: `GameManager.cs:213-215`; `UIManager.cs:267-280`; `EndScreenManager.cs:307-310`; Game Manager prefab line 47.

Create an immutable result containing scene/level ID, run time, previous best, new best, and next destination. Pass it to persistence and UI before advancing.

### F-003 Read the real best time

**Medium, confirmed.** `GetLevelTime` returns the latest completion although `BestCompletionTime` is stored separately. Slower retries can replace the displayed “best.” Evidence: `Assets/Scripts/SOAP/GameData/GameSaveManager.cs:42-46`, `:61-70`; `TimerManager.cs:78-107`.

### F-004 Repair settings and camera serialization

**High, strongly supported.** Manager prefabs/scenes still serialize retired camera fields; `_gameSettingsData` is absent while startup dereferences it. Live sensitivity/invert can fail and menu-to-Level-1 startup may throw. Evidence: `GameManager.cs:22-25`, `:350-379`; `UIManager.cs:41-45`, `:69-73`; manager prefabs and Levels 1-4 overrides.

### F-005 Make lifecycle entry/resume explicit

**Medium, strongly supported.** Initial `LoadingIn` effects can be skipped because the serialized state already matches, and unpause replays full state entry. Separate `Enter`, `Resume`, and forced initialization semantics. Evidence: `GameManager.cs:74-105`, `:137-142`, `:254-268`.

### F-006 Validate production hazards and respawn

**High, strongly supported.** The legacy player can own `PlayerCollisionManager` without the `RespawnComponent` it dereferences; V2 has no production death/checkpoint adapter. Evidence: `PlayerCollisionManager.cs:13-16`, `:27-32`; KCC and V2 player prefabs; Levels 3-4 overrides.

## Next — make the grocery run readable and satisfying

### F-007 Ship one production Player V2 vertical slice

**High opportunity, confirmed boundary.** V2 supplies profiles, modifiers, control blocks, snapshots, motor-safe teleport, and component abilities, but production levels still instantiate the older KCC controller and lifecycle code names it directly. Migrate Level 1 end-to-end before expanding abilities.

### F-008 Turn the list into a route-planning tool

The current list is text that changes color. Add item silhouettes/category color, remaining count, nearest/selected target, subtle world beacons, and a clear “return to checkout” transition. Keep guidance optional so speedrunners can route freely.

### F-009 Add collection juice and audio identity

Item pickup audio is commented out; SFX support is dormant; animation only distinguishes running versus airborne. Add a pickup pop, list-entry strike/check animation, controller rumble, distinct double-jump/fall feedback, checkout fanfare, and a new-best sting. Evidence: `Item.cs:77-88`; `AudioManager.cs`; `PlayerAnimationPresenter.cs:27-43`.

### F-010 Fix music-state semantics

**Medium, confirmed.** InProgress selects end-game music, GameOver returns to gameplay music, and menu/gameplay use the same clip. Map tracks to semantic phases and give the timer run its own escalating identity. Evidence: `AudioManager.cs:91-107`; AudioManager prefab lines 48-53.

### F-011 Make jumps player-controlled and legible

V2 already receives held/cancelled jump state but does not use it. Add jump-cut/variable height, a readable apex/fall state, and distinct second-jump feedback. Evidence: `InputReader.cs:72-82`; V2 controller intent; locomotion machine jump trail.

### F-012 Add first-run onboarding and safe settings UX

The world arrow logic is disabled and reset-times is destructive without confirmation. Teach start → collect → return in play, then fade prompts permanently. Add reset confirmation/feedback and verify pause fully freezes NPC/camera/gameplay. Evidence: `ArrowController.cs:37-44`; `MainMenuController.cs:140-143`.

## Later — deepen mastery and replayability

### F-013 Seeded orders and shelf layouts

The current four orders are tiny and fixed; runtime shelf randomization is commented out. Generate deterministic seeds that guarantee requested items, publish the seed on results, and preserve fair comparisons. Evidence: level grocery-list serialization; `ShelfItemSpawner.cs:15-18`, `:29-50`.

### F-014 Multiple viable routes and movement-specific shortcuts

Design each market around safe, direct routes plus risky traversal shortcuts that reward double jump or future abilities. Avoid a single dominant path; measure split times per department.

### F-015 Medals, split times, ghosts, and instant retry

The timer/save foundation already exists. Add bronze/silver/gold targets, department splits, personal-best delta, a lightweight ghost, and a one-button retry that resets the same seed without a long scene transition.

### F-016 Make NPC shoppers systemic obstacles

Level 3’s current NPC can snap to spline start and ignores pause. Build predictable shopper archetypes—slow carts, aisle blockers, stockers—whose movement is readable and seed-stable. Evidence: `NPCController.cs:52-70`; Level 3 NPC configuration.

### F-017 Let abilities create route choices

Use V2 profiles/modifiers for temporary supermarket-themed effects: sugar rush speed, sticky spill drag, heavy bulk-item jump penalty, cart momentum, or short-lived aisle shortcuts. Prefer trade-offs and route decisions over permanent stat inflation.

## Audit dimension disposition

- Architecture/boundaries: assessed.
- Coupling/orphans/duplication: assessed; legacy interaction, signposting, and old controller seams are material.
- Correctness/failure propagation: assessed statically.
- Security/trust: local save/settings only; no network surface found.
- Performance/resource lifetime: assessed at high level; no profiler run.
- Tests: production campaign connections lack focused coverage; V2 core lacks direct tests.
- Operability/configuration/recovery: assessed; serialized drift and save/reset behavior noted.
- Documentation drift: assessed; production scenes predate the V2 work and several serialized event/field names are stale.
