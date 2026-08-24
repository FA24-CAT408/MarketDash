# Handoff: Implement the approved sticky-note Main Menu

**Status:** design approved 2026-08-23. This document is self-contained — no prior chat context needed.

## Goal

Replace the current Main Menu UI with the approved "sticky note" design. The interactive HTML
prototype is the source of truth for look, layout, and motion:

- `Prototypes/MainMenu/final-sticky.html` — frozen approved version (serve with
  `python -m http.server` from that folder and open in a browser; the sticky variant loads by
  default; press `H` to hide the dev toolbar). `bg.png` in the same folder is a real gameplay
  capture used as the backdrop.
- All px values below are in a **1280×800 design frame**; the whole UI scales uniformly.

## What exists in the project today (integration points)

- Scene: `Assets/Scenes/Levels/Main Menu.unity`, driven by
  `Assets/Scripts/Managers/MainMenuController.cs` (uGUI, DOTween). It already has
  `mainMenuCanvas` / `settingsCanvas`, a `CanvasGroup` fade transition, and public handlers:
  `PlayGame()`, `OpenSettings()`, `CloseSettings()`, `QuitGame()`, `ResetGame()`.
  **Keep this controller and its handlers; rebuild the visual hierarchy under `mainMenuCanvas`.**
  The settings canvas is out of scope — leave it as is.
- Font: `Assets/Fonts/Atma/Atma-Bold SDF.asset` (TMP) — the game's UI font, used everywhere.
- Timer/mono font: none under `Assets/Fonts/` yet. Add a mono TTF + TMP SDF (IBM Plex Mono to
  match the prototype, or promote RobotoMono from `Assets/Feel/NiceVibrations/Demo/`) into
  `Assets/Fonts/` for all time displays. Enable tabular figures (or rely on mono spacing).
- Save data: `GameSaveManager : ScriptableSave<GameSaveData>` exposes `TotalTime` (float,
  seconds). `GameSaveData.LevelEntries` has per-level `BestCompletionTime` / `IsCompleted`.
- Tweening: DOTween is in use; Feel (MMFeedbacks) is also available. Either is fine — prefer
  whichever keeps the scene simplest; the motion specs below are duration/ease pairs.

## Design spec

### Canvas

- `CanvasScaler`: Scale With Screen Size, reference 1280×800, **Screen Match Mode = Expand**
  (this reproduces the prototype's `min(w/1280, h/800)` uniform scaling).

### Background

- Keep the gameplay store scene behind the menu live. Blobby must continue playing the idle/
  breathing animation, and the NPCs must continue moving along their existing paths.
- Do not cover the camera with a static gameplay capture. A blur post effect is optional only when
  the current rendering pipeline can apply it without replacing or freezing the live view.
- On top of the background: a vignette (radial darkening at edges, subtle).
- **Dim on menu hover:** while the pointer is over the note (or keyboard nav is active), dim the
  background to ~78% brightness, 350 ms ease-out. Implement as a full-screen black Image whose
  alpha tweens 0 → 0.22.

### The sticky note (menu surface) — build in uGUI, no texture needed

470×~400 design px, anchored right-center (right edge ~76 px from frame right), rotated **−1.5°**.
Anatomy (recreate the prototype's CSS with Images):

- Paper: `#F8EDA9`, sharp corners, **3 px near-black outline** (`#1D1D1F`).
- Adhesive strip: top 40 px in `#F8DC80` with a 2 px divider line `#E3C96F` under it.
- Curled bottom-right corner: the corner is visually cut off (54×54 px diagonal) so the
  background shows through, with a folded flap over the cut — dark ink crease line along the
  fold, bright underside `#FDF4BD` fading to `#E9D582`. Easiest in Unity: a small pre-made
  sprite for the corner + a paper sprite with the corner transparent (author one 9-sliceable
  PNG from the prototype rendering if hand-building is fiddly).
- Depth: warm inset shading toward the bottom of the paper, and a drop shadow that follows the
  note (a soft shadow sprite behind it is fine).
- Interior shading/shadow values don't need to be exact — match the prototype by eye.

### Note contents

- Header, on the paper below the strip: **"To do"** — Atma bold, 28 px, `#9C5F12`; right-aligned
  on the same line an **idle timer** `IDLE m:ss.cc` — mono font, 14 px, `#B3541E`, counts up from
  scene load and **resets to zero on any input** (pure flavor). Under both: a full-width marker
  underline, 4 px, rounded ends, rotated −0.6°, same ink color at 80% alpha.
- Menu items (5 rows, 47 px pitch, Atma semi-bold 25 px, ink `#4A3608`), each with a hand-drawn
  style circle checkbox (26 px, 3 px `#4A3608` outline, slightly irregular if you author a
  sprite; a plain circle is acceptable v1):
  `Continue · New Game · Leaderboards · Options · Quit`
- **No sub-text on items, no footer notes, no logo tagline.** (Deliberate — flavor text was cut.)

### Interaction model (exactly two signals — do not add more)

- **Hover/selected:** a red (`#E23C3C`) marker-style arrow (shaft + triangular head, ~30 px long,
  tilted −4°) slides in from the left of the row: 8–10 px slide, 180 ms ease-out, fade in 100 ms.
  The row itself shifts right 10 px, 180 ms ease-out. One arrow instance moved between rows is
  simpler than one per row. Mouse hover and keyboard/gamepad selection drive the same state.
- **Submit:** the checkbox pops in a green (`#1C9E4F`) checkmark — scale 0 → 1, 200 ms ease-out —
  and the row does a tiny squash bump (~scale 0.96 → 1, 500 ms). Then the action fires (existing
  fade transition covers the scene load).
- Keyboard/gamepad navigation must work (Unity UI navigation or the project's `InputReader`).
  Wrap-around up/down. No animation on the initial default selection.

### Logo

Top-left (~56, 44): **MARKET / DASH** stacked, Atma bold ~76/88 px. MARKET white with red
(`#D8452B`-ish) extrusion shadow; DASH yellow (`#FFD23E`) with darker extrusion. Whole logo
rotated −3° with a slight skew. Three small speed-dash marks trail the "DASH" line on its left.
TMP with shadow/outline material presets is fine; a rendered sprite is also fine.

### Best-run card

Bottom-left (~56 from left, ~96 from bottom), rotated −2°: an off-white (`#FFFDF5`) punch card
with notched left/right edges (sprite), containing:

- `BEST RUN · ALL LEVELS` — red (`#E23C3C`), mono, ~10.5 px, letterspaced.
- The time, hero element: mono bold **34 px**, `#123B47` — value = `GameSaveManager.TotalTime`
  formatted `m:ss.cc` (or `h:mm:ss.cc` if ≥ 1 h). If no completed run yet, show `--:--.--`.
- Small line: `RANK #147 · WR 12:34.56 "cartgod"` — **placeholder**; no leaderboard backend
  exists yet. Hardcode behind a serialized string so it's obvious.
- A green circled "PB" stamp on the right side, rotated −14°.

### Wiring

| Item         | Action                                                                 |
| ------------ | ---------------------------------------------------------------------- |
| Continue     | `MainMenuController.PlayGame()` (later: load furthest incomplete level) |
| New Game     | `ResetGame()` then `PlayGame()` — confirm dialog is out of scope, note it |
| Leaderboards | Stub — visually present, either no-op with a small "coming soon" shake or disabled state |
| Options      | `OpenSettings()`                                                        |
| Quit         | `QuitGame()`                                                            |

### Entrance

On scene load (after the existing fade-in): logo, note, and card fade/slide in ~14 px upward,
400–500 ms ease-out, staggered ~60 ms apart. Skip/shorten if a "reduce motion" setting exists.

## Acceptance checklist

- [ ] Matches `final-sticky.html` side-by-side at 1280×800 (composition, colors, type sizes).
- [ ] Scales uniformly at 1920×1080, 2560×1440, ultrawide — no stretching, no anchored drift.
- [ ] Hover arrow + row shift on mouse AND keyboard/gamepad nav; checkmark pop on submit.
- [ ] Idle timer ticks in mono digits without layout jitter; resets on any input.
- [ ] Best-run time reads from `GameSaveManager.TotalTime`; empty-save state handled.
- [ ] All five buttons wired per the table; settings screen still opens/closes.
- [ ] Blobby idles/breathes and both NPCs keep moving on their production paths behind the UI.
- [ ] No console errors; existing scene fade transition still works.

## Process constraints (from AGENTS.md / CLAUDE.md)

- Keep work local — no push/PR unless the user separately asks to publish.
- No new tests unless requested. Run the required Unity smoke validation for the changed scene.
- When finishing, run the local review workflow
  (`.codex/skills/crazy-market-review-loop/SKILL.md`, `Tools/LocalReview/review_loop.py`).
- Do not rename or break `MainMenuController`'s public API — other prefabs/scenes reference it.
