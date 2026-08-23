# CrazyMarket Improvement Board

## Active

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
