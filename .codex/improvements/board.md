# CrazyMarket Improvement Board

## Active

### Issue #36 Main Menu scene foundation

- **Outcome:** The production Main Menu scene has a readable hierarchy that keeps important objects at the root while consolidating repeated supermarket dressing into clear groups.
- **Why now:** This gives the interactive Main Menu prototypes a clean foundation without prematurely committing to a final vignette architecture.
- **Current behavior:** The scene previously exposed 49 roots, mixing individual NPCs, products, and ceiling lights with its important objects.
- **Minimum:** Group repeated dressing into `NPCs`, `Produce Display`, `Shelf Display`, and `Ceiling Lights` while preserving world transforms and behavior.
- **Target:** Confirm the reorganized production scene opens, compiles, and looks unchanged in Play Mode.
- **Stretch:** Begin the first lightweight interaction concept only after this organization slice is accepted.
- **Acceptance criteria:** Important objects remain at the root; repeated dressing appears under the four agreed groups; object placement and active state remain unchanged; the Main Menu enters Play Mode without new errors.
- **Non-goals:** UI redesign, interaction logic, camera choreography, new art, feedback effects, accessibility behavior, or selection of the final centerpiece.
- **Scene/zone:** `Assets/Scenes/Levels/Main Menu.unity`.
- **Branch/base:** `main` / `origin/main`.
- **Status:** In progress; hierarchy organization applied and smoke-validated.
- **Budget:** Two implementation approaches and one review/fix round.
- **Attempts used:** One hierarchy approach selected from three structural options.
- **Progress:** Added four root groups, moved repeated scene dressing beneath them, and normalized `register` to `Register`. Replaced four invalid negative-scale shelf-prefab colliders with equivalent positive-scale proxies, regenerated the production Sample Level shelf splines, and migrated 148 stale baked Main Menu shelf colliders.
- **Decisions:** Keep major objects at the root and group only repeated dressing. Preserve shelf collision geometry through proxy transforms instead of disabling collision or replacing box colliders with expensive convex mesh colliders.
- **Validation:** Unity recompiled successfully and the production Main Menu entered Play Mode. The exact negative-scale `BoxCollider` warning count remained at 592 before and after the corrected run (`0` new warnings); static inspection found zero remaining direct negative-scale box colliders. Visual comparison and interactive shelf-collision testing remain pending.
- **Checkpoints:** None.

## Paused

### Player Controller V2 vertical slice

- **Outcome:** The player can move, jump, and double-jump responsively in the Player V2 Test Campus scene, with animation matching the action on the frame it begins.
- **Why now:** The current controller work is already integrated and player-visible, but continued framework and timing refinement is delaying a shippable locomotion slice.
- **Current behavior:** A KCC-backed V2 controller, locomotion state machine, double-jump component, runtime profile, animation presenter, prefab, and dedicated Test Campus scene exist. Recent commits repeatedly refined jump/animation timing. A checkpoint smoke on 2026-08-18 confirmed the project compiles, the V2 scene enters Play Mode, and the Console has no errors; input feel and reset/boundary behavior were not re-exercised during this checkpoint.
- **Minimum:** Freeze architecture and ship the integrated walk/run, jump, double-jump, grounded reset, and immediate animation behavior already present. Make only fixes required by review or Unity smoke validation.
- **Target:** Confirm tuning is usable in Play Mode and that Test Campus reset/teleport/control-block paths preserve controller state.
- **Stretch:** None for this improvement.
- **Acceptance criteria:**
  - Movement is camera-relative and responsive in `TestCampus_Core_PlayerV2`.
  - Ground jump and one air jump both fire once per press and show the correct animation immediately.
  - Stable landing restores the air jump; walking off a ledge respects the intended coyote window.
  - Reset/teleport returns the player to a controllable, finite-velocity state.
  - Disabling and restoring movement does not retain queued jump input.
  - The scene compiles, enters Play Mode, and produces no errors or exceptions during the exercised paths.
- **Non-goals:** Additional locomotion modes, wall-kicks or ledge systems, more abilities, a general gameplay-state framework, further profile/modifier API expansion, new editor tooling, or production-scene replacement.
- **Scene/zone:** `Assets/TestCampus/Scenes/TestCampus_Core_PlayerV2.unity`.
- **Branch/base:** `main` / `origin/main`; local branch is 27 commits ahead.
- **Status:** Checkpointed; scope expansion paused pending user direction.
- **Budget:** Two implementation approaches and one review/fix round. Implementation budget is exhausted; retain the single review/fix round for Finish.
- **Attempts used:** Two prototypes (`player locomotion HSM`, `ledge jump recovery`) followed by the integrated V2 approach; approximately 15 controller-specific implementation/fix commits.
- **Progress:** Integrated V2 slice exists across 11 handwritten C# files (about 1,690 lines), a prefab, animator changes, and a dedicated scene. The work exceeds the steady-improvement tripwires for runtime components and handwritten lines.
- **Decisions:** Treat existing generality as sunk implementation for this slice, but add no more abstraction unless a concrete acceptance failure requires it. Do not fold the separate camera/URP working-tree changes into this improvement.
- **Validation:** On 2026-08-18, Unity 6000.4.1f1 reported scripts up to date; `TestCampus_Core_PlayerV2` was the clean active scene; Play Mode entered successfully; the error Console remained empty. No full interactive acceptance pass has been recorded yet.
- **Checkpoints:**
  - **2026-08-18 — depth/scope:** The work is too deep for one bounded improvement: it crossed the >3 runtime-component and ~1,000-line tripwires, consumed the implementation-attempt budget, and includes generalized profiles, modifiers, contracts, editor tooling, and ability composition beyond the player-visible minimum. Smallest shippable option: freeze the design and finish/validate the current movement-jump-double-jump slice. Larger option: continue developing a reusable controller platform, which requires a new improvement contract, explicit production requirements, and a fresh multi-round budget. Recommendation: take the smallest option and defer the platform work.

<!-- When active, keep: Outcome, Why now, Current behavior, Minimum, Target,
Stretch, Acceptance criteria, Non-goals, Scene/zone, Branch/base, Status,
Budget, Attempts used, Progress, Decisions, Validation, and Checkpoints. -->

## Next

<!-- At most three shaped candidates. -->

## Inbox

<!-- Capture adjacent discoveries without expanding Active. -->

- Decide whether runtime profile replacement and stat modifiers have a near-term production consumer; otherwise simplify or defer them after the V2 slice ships.
- Revisit wall/ledge recovery only as a separate player-visible improvement with its own Test Campus scenario.
- Reconcile the unrelated camera, renderer/URP, legacy KCC prefab, and Unity-upgrade working-tree changes separately from Player Controller V2.

## Completed

<!-- Newest first: date, outcome, validated scope, branch/commits, deferred work. -->
