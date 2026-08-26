# CrazyMarket Improvement Board

## Active

<!-- No active improvement. -->

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

### 2026-08-25 — Receipt-printer detail and Settings receipt

- **Outcome:** Made the production UI Toolkit register feel like a physical receipt printer, gave the unavailable Leaderboard row a naturally rubbed-off treatment, and made Options tear off the main receipt before printing a fully interactive Settings receipt.
- **Validated scope:** Added a recessed slot, serrated tear comb, rollers, vents, shell seam, fasteners, model plate, contact shadow, feed-state light, paper edge definition, a licensed CC0 rubbed-print overlay, a shorter Settings slip, stepped sensitivity/volume scales, an animated centered invert check, Back/Escape/gamepad-B behavior, and overlapping tear/feed choreography. Removed the redundant receipt-side Best Run while preserving the detached Best Run · All Levels ticket. Kept the Post-It variant archived and left the 3D environment untouched.
- **Behavior evidence:** Unity 6000.4.1f1 compiled with zero errors. Both receipt states were visually inspected at 1024×768, 1280×720, 1280×800, 1920×800, and 1920×1080. Real Options submission kept the main canvas active and legacy Settings canvas inactive. Settings navigation wrapped `0→3→0`; main navigation returned to Options and wrapped `3→4→0`; Leaderboard stayed disabled and unfocusable. Sensitivity, volume, and invert updated through the existing settings owner and were restored after testing. Back reprinted Main with focus on Options. Both NPC walkers moved and the active KCC animator advanced. The isolated Play Mode Console had zero errors; one pre-existing convex-mesh warning was unrelated.
- **Design review:** A dedicated design pass refined the printer hardware, overlapping paper motion, shortened Settings slip, header collision fix, contact shadow, typography floor, control alignment, and stronger printed-paper treatment.
- **Branch/commits:** `main`; atomic UI Toolkit receipt commit based on `a81b571`.
- **Deferred work:** Leaderboard still has no backend by design. A dedicated user-facing reduced-motion preference can later drive the already-supported reduced-motion transition path.

### 2026-08-24 — Main Menu UI Toolkit migration

- **Outcome:** Migrated the approved sticky-note Main Menu from generated uGUI to one runtime `UIDocument`, then corrected the Toolkit authoring defaults and composition to match the approved web and GameObject versions without changing the live market backdrop or menu action ownership.
- **Validated scope:** Removed `Sticky Menu Visuals`, `StickyMainMenuView`, and `StickyMenuRow`; made the complete UXML composition visible by default in UI Builder; restored the approved 1280×800 design frame, 470×400 note, 47px row rhythm, Atma typography, copy, logo colors, and tinted interaction art; removed the UI-owned vignette so post-processing owns screen-edge treatment; grouped the logo into independently movable `market-word` and `dash-word` authoring units with an explicit gap; joined the marker arrow shaft and right-facing head into one slim continuous visual; preserved entrance staging, hover/selection styling, submit feedback, keyboard/gamepad wrap navigation, save-driven best-run state, idle timer, leaderboard unavailable feedback, and the unchanged legacy Settings canvas.
- **Behavior evidence:** The production scene compiled and ran in Unity 6000.4.1f1. The corrected Play Mode view was visually inspected against `Prototypes/MainMenu/final-sticky.html`; Blobby's active animator advanced and both NPC spline walkers changed world position. The selection arrow resolved to a 180-degree right-facing head, a roughly 4.3px shaft, and approximately 5.2px of shaft/head overlap, then followed the selected row through `Quit -> Continue` wrap navigation. Keyboard and gamepad navigation wrapped selection; Options opened Settings and returned to a rebuilt UI Toolkit tree with one callback bound. The isolated final Play Mode Console contained zero errors.
- **Review note:** The generic `unity-smoke` baseline expected the seven-scene Test Campus topology and therefore rejected the production menu's single loaded scene. Focused production Play Mode, interaction, lifecycle, motion, visual, and Console checks supplied the applicable acceptance evidence.
- **Branch/commits:** `main`; local uncommitted migration based on `a81b571`.
- **Deferred work:** Settings remains on its existing uGUI canvas by request. Leaderboards remains a serialized placeholder/no-op until a backend exists.

### 2026-08-23 — Approved sticky-note Main Menu

- **Outcome:** Replaced the production Main Menu visuals with the approved sticky-note composition while preserving the existing settings canvas, fade transition, controller API, and save ownership.
- **Validated scope:** Live gameplay backdrop with Blobby's idle/breathing animation and both NPC paths preserved; vignette; stacked Market Dash logo; outlined/curling sticky note; five menu rows; shared hover/selection arrow and background dim; green submit check and squash; idle timer reset; save-driven best-run card and empty state; leaderboard no-op shake; Options open/close reset; keyboard and gamepad wrap navigation; entrance staging; 1280x800, 1920x1080, and 1920x800 visual layouts.
- **Behavior evidence:** The production scene compiled and ran in Unity 6000.4.1f1. Mouse hover/submit and Settings return were exercised in the Game view. Connected Input System events moved `Continue -> New Game`, wrapped `Continue -> Quit` by keyboard, and wrapped `Quit -> Continue` by gamepad. A 12-second production capture confirmed the live Blobby animation and both NPC paths continued behind the UI. The isolated final Play Mode log contained zero relevant errors, exceptions, or assertions.
- **Review note:** The generic `unity-smoke` baseline was run but expected the seven-scene Test Campus topology and found the one loaded production menu scene. Focused production Play Mode and visual evidence supplied the applicable acceptance signal.
- **Branch/commits:** `main`; atomic feature commit prepared for publication.
- **Deferred work:** New Game still intentionally has no confirmation dialog, and Leaderboards remains a serialized placeholder/no-op until a backend exists.
