# Test Campus Playtest Bug Log

Playtest date: 2026-07-25

## Fixed

### Zone buttons changed status but did not move the player
- Severity: High
- Reproduction: Enter Play Mode, click any specialist-zone button.
- Expected: Player appears at that zone's default spawn.
- Actual: `CurrentZone` changed, but the KCC motor restored the old position.
- Likely cause: The controller wrote only to `Transform`; KCC owns the authoritative position.
- Fix: Route campus teleports through `KinematicCharacterMotor.SetPositionAndRotation` and clear velocity.
- Verification: Clicked all seven zone buttons in Play Mode and observed distinct scene spawn coordinates.

### Camera collision changed the movement frame and caused 180-degree oscillation
- Severity: High
- Reproduction: Face the hub's south wall and hold `S` while the camera is behind the player.
- Expected: The player backs into the wall and remains there with stable screen-relative controls.
- Actual: Camera collision changed `Camera.main` heading; KCC then recalculated movement from the corrected camera transform, producing repeated 180-degree camera/player reversals.
- Likely cause: Movement and rendering shared the same physically corrected camera transform.
- Fix: Added a stable camera-heading proxy for KCC movement, driven by the Cinemachine orbit axis rather than the collision-corrected Main Camera transform.
- Verification: Held the real `S` key for four seconds. The player stopped at `(10.00, 0.03, -19.22)`, camera yaw remained `0°`, and orbit radius remained `9.5 m`.

### Obstruction handling zoomed too close and hid the route
- Severity: High
- Reproduction: Move the player against a foreground wall or rotate the camera behind one.
- Expected: Preserve enough distance to read the player, route, and nearby obstacles without clipping.
- Actual: A collision-only camera response pulled the camera into a close-up.
- Likely cause: Obstruction was resolved only by shortening the camera-to-player distance.
- Fix: Replaced the test-campus camera with a Cinemachine Orbital Follow rig at a fixed `9.5 m` radius, retained a Decollider only for camera-body intersections, and added selectively tagged foreground-wall hiding plus non-colliding interior aprons/backdrops.
- Verification: The exact south-wall test hid one tagged wall section while the orbit radius stayed `9.5 m`; the direct Camera render retained the player and room floor.

### Automatic recenter chased camera-relative diagonal input
- Severity: High
- Reproduction: Rotate the camera to `52°`, hold `W+A`, and continue moving after the recenter delay.
- Expected: One gentle correction, then a stable camera heading.
- Actual: The camera changed the movement basis while chasing the resulting movement heading, creating a self-reinforcing turn.
- Likely cause: The desired recenter heading was recalculated every frame from camera-relative motion.
- Fix: Latch one heading per continuous grounded movement bout and consume the correction instead of continuously chasing the transformed input.
- Verification: At one second the delayed camera remained at `52°`; the correction settled at `7.14°` and remained `7.14°` after input stopped.

### Guided rail loses composition when the player leaves its authored corridor
- Severity: Medium
- Reproduction: In Guided Rail mode, move from `(75, 0, 20)` to `(84, 0, 20)`.
- Expected: A freely explorable camera keeps the player and route readable.
- Actual: The rail camera remained at X=`75` and the player became occluded behind a corridor wall.
- Likely cause: Nearest-point spline movement constrains camera position to the authored centerline.
- Fix: Kept the rail as a comparison prototype and made Assisted Orbit the default; the Hybrid mode uses rail cameras only inside explicit trigger zones.
- Verification: At the identical off-path position, Assisted Orbit moved to `(75.19, 4.77, 20.00)` and hid one tagged foreground wall, keeping the player visible.

### Lighting Gallery spawn intersected a reference sphere
- Severity: High
- Reproduction: Click `Lighting`.
- Expected: Spawn on clear floor with the comparison bays visible.
- Actual: KCC resolved the player onto the top of the center reference sphere.
- Likely cause: Generic zone-center spawn overlapped a fixture.
- Fix: Added deliberate clear spawn positions for Movement, Lighting, NPC Interaction, and UI.
- Verification: Lighting now spawns at `(0, 0, 55)` on clear floor.

### Reset Current ignored most fixtures
- Severity: High
- Reproduction: Move a resettable scene-root fixture, then click `RESET CURRENT`.
- Expected: Fixture returns to its captured pose.
- Actual: Only providers parented under `TestZoneRoot` were registered.
- Likely cause: Generated fixtures are scene roots, while registration used `GetComponentsInChildren`.
- Fix: Discover providers and resettables across the owning additive scene only.
- Verification: Moved an Integration fixture upward by five meters, clicked reset, and observed it return from Y=7 to Y=2.

### Preset and reset buttons had no visible acknowledgement
- Severity: Medium
- Reproduction: Click Low, Normal, Stress, Reset Current, Reset Campus, or Return to Hub.
- Expected: Immediate confirmation or failure feedback.
- Actual: No visible response.
- Likely cause: Actions returned booleans that the UI ignored.
- Fix: Added a color-coded action message with success/failure text.
- Verification: Clicked each action and observed its specific message.

### Diagnostics text was clipped to one line
- Severity: Medium
- Reproduction: Click `TOGGLE DIAGNOSTICS`.
- Expected: All zone records remain readable.
- Actual: A 151-pixel text block was constrained to 23 pixels.
- Likely cause: Fixed `LayoutElement.preferredHeight`.
- Fix: Recalculate preferred height from TMP's rendered content.
- Verification: All fourteen diagnostic lines were visible in Play Mode.

### Specialist floor accents washed out depth cues
- Severity: Medium
- Reproduction: Teleport to Movement.
- Expected: Neutral gray floor with accent-colored fixtures.
- Actual: The entire floor was saturated cyan.
- Likely cause: Zone accent material was assigned to the floor.
- Fix: Use the neutral material for floors and retain accents on fixtures, columns, and signs.
- Verification: Revisited Movement and compared the rendered floor and obstacle edges.

### Jump input spammed the Console
- Severity: Low
- Reproduction: Hold Space briefly.
- Expected: No per-frame debug output.
- Actual: `JUMP PRESSED` was logged every held frame.
- Likely cause: Debug logging inside the per-frame input path.
- Fix: Removed the debug log.
- Verification: Repeated jump testing and reviewed the Console.

### Campus had no usable pause/resume path
- Severity: Medium
- Reproduction: Press Escape while exploring the campus.
- Expected: Pause test state becomes observable and can be restored.
- Actual: The production Game Manager remained in `LoadingIn`; time scale and UI did not change.
- Likely cause: The campus intentionally bypasses the production level loop but had no test-only pause adapter.
- Fix: Added a visible `PAUSE / RESUME (P)` action that toggles simulation time while keeping the test UI responsive.
- Verification: Tested both the button and P shortcut, then moved immediately after resuming.

### Prototype booted in UI focus and discarded all mouse look
- Severity: Critical
- Reproduction: Enter Play Mode, click the Game view, and move the mouse.
- Expected: Gameplay owns the cursor and Cinemachine orbits immediately; `F1` or Escape explicitly transfers focus to the control panel.
- Actual: The control panel called `SetUiFocus(true)` during startup, so the camera discarded every mouse delta. `R` still worked because its shortcut bypassed the focus gate.
- Likely cause: The initial UI visibility state and the camera input-focus state were treated as the same implicit default.
- Fix: The prototype now starts with its panel collapsed, locks/hides the cursor, enables movement, and continuously owns gameplay focus. `F1`/Escape opens the panel, releases the cursor, and suspends player movement; closing it restores gameplay focus.
- Verification: Real macOS pointer events changed Cinemachine yaw from `0°` to `5.76°` and pitch from `22°` to `20.08°`; an actual held `W` then moved the player from `(0,0,0)` to `(1.20,0.01,11.93)`. Opening the panel produced `Cursor=None`, `visible=true`, and `canMove=false`; closing it produced `Cursor=Locked`, `visible=false`, and `canMove=true`.

### Editor focus gate left the gameplay cursor free
- Severity: Critical
- Reproduction: Enter Play Mode, focus the Game view, leave the control panel closed, and move the physical mouse.
- Expected: Gameplay immediately captures and hides the cursor so mouse delta continuously drives Cinemachine orbit.
- Actual: Cursor capture and look sampling were both gated by `Application.isFocused`; Unity Editor focus reporting could fail that gate even though the Game view was the active gameplay surface.
- Likely cause: Window/application focus was incorrectly used as the authority for gameplay input ownership.
- Fix: Gameplay/UI ownership is now the authority. While the panel is closed the controller restores the intended hidden-pointer state and samples look input without the unreliable application-focus gate. Application focus regain still reapplies the intended cursor state.
- Verification: In Play Mode the runtime reported hidden gameplay mouse look; an actual held `W` plus Space moved the player to `(0.00, 2.09, 7.54)`. `F1` made the pointer visible and entered `UI INPUT`; closing it restored hidden gameplay input.

### Locked cursor did not reliably produce Cinemachine orbit input
- Severity: Critical
- Reproduction: Enter Play Mode, click the Game view, and move the physical mouse while the cursor is captured.
- Expected: The invisible, center-locked cursor produces relative look deltas and rotates the Cinemachine Orbital Follow camera.
- Actual: The controller polled mouse state late in `Update` and fell back to absolute pointer-position differences. A locked cursor cannot provide meaningful absolute motion, so capture succeeded while orbit input remained absent or cancelled itself.
- Likely cause: Pointer position was used as a substitute for relative mouse delta.
- Fix: Added a prototype-local Pass Through Input Action bound directly to `<Mouse>/delta`. Its performed callbacks accumulate relative motion before the camera update and drive the Cinemachine horizontal and constrained vertical orbit axes. The absolute-position fallback was removed.
- Verification: A raw mouse delta changed Cinemachine yaw from `0°` to `8°` and pitch from `22°` to `24°`, reporting `Input Action mouse delta`. The same input was ignored while `F1` UI focus was active, then worked again after closing the panel. With the rotated camera, `W` plus Space moved the player from `(0.00, 0.03, 0.00)` to `(1.04, 2.29, 7.42)`.

### Gameplay mouse needed edge confinement without center locking
- Severity: High
- Reproduction: Click the Game view while the control panel is closed.
- Expected: The pointer is hidden during mouse look and remains inside the Game view without being continuously centered.
- Actual: `CursorLockMode.Locked` pinned and warped the pointer to the Game-view center.
- Likely cause: FPS-style cursor locking was assumed to be required for camera-relative mouse delta.
- Final behavior: Gameplay uses hidden `CursorLockMode.Confined` instead of center-locking. The experimental macOS edge clamp was removed because its `Mouse.WarpCursorPosition` corrections felt undesirable. `F1` switches to visible `CursorLockMode.None` for unrestricted UI interaction.
- Verification: The camera controller contains no pointer-warp call, while raw `<Mouse>/delta` continues to drive Cinemachine. Known limitation: Unity does not natively enforce Confined mode on macOS, so the pointer can leave the Game view on that platform.

### Prototype control panel overflowed and obscured its own labels
- Severity: Medium
- Reproduction: Run the campus in a docked 16:9 Game view.
- Expected: The panel remains readable and scrollable without overlapping button labels.
- Actual: A single vertical layout compressed all controls into the available height, causing status and button text to overlap.
- Likely cause: The content had no scroll viewport and several multiline TMP fields had fixed one-line layout heights.
- Fix: Rebuilt the runtime panel around a masked `ScrollRect`, content-sized vertical layout, dynamically fitted TMP heights, and constant-pixel UI sizing. Gameplay now uses a separate non-raycasting camera/input HUD.
- Verification: Inspected the running docked Game view with the panel open; buttons remained separated and the list scrolled instead of compressing.

### Looking up drove the camera below the floor
- Severity: Critical
- Reproduction: Stand anywhere on the hub floor and pitch the camera fully upward.
- Expected: Floor geometry blocks the camera; it resolves to the closest valid position without clipping or snapping.
- Actual: The camera passed below the floor and rendered the level from underneath. Measured unconstrained: camera Y `-2.04` at the hub corner and `-2.02` beside the south wall, both at the full `9.5 m` radius. At the hub centre the Decollider instead crushed the distance to `4.15 m` and left the camera inside the floor slab at Y `-0.06`.
- Likely cause: `CinemachineOrbitalFollow` in Sphere style places the camera at `target.y + Radius·sin(VerticalAxis)`, so the `-20°` bound puts it ≈ `2.0 m` below the target — clear of the floor slab's `[-1, 0]` span. `CinemachineDecollider` only displaces the camera when `Physics.ComputePenetration` finds the camera sphere *inside* a collider, so with the camera in open air beneath the slab there was nothing to resolve. It is a de-collider, not a de-occluder. The camera aprons compounded this: they read as floor but their colliders were deliberately destroyed, so outside the room there was no geometry beneath the camera at all.
- Fix: Added a ground constraint in two layers. `TestCampusCameraPrototypeController.ApplyGroundConstraint` probes the walkable surface beneath the desired orbit position and first pulls the camera in via Cinemachine's own `RadialAxis` (never below `7.0 m`), then limits downward pitch so the camera rides the surface. `TestCampusCameraGroundGuard`, a Cinemachine extension running at the Body stage after the Decollider, enforces the same limit on the final corrected position, covering the frames where rig damping lets the rendered camera lag the axis constraint. Both share one probe in `TestCampusCameraGround`, which starts its sweep at the orbit target's height and only searches downward — that is what makes a permissive layer mask safe, since it can never reach the hub ceiling or the Movement gym's low ceiling. The four camera aprons became trigger-only colliders marked `TestCampusCameraGround`, visible to the camera probe but not to the player or the KCC motor. Cinemachine's `TerrainResolution` was deliberately left disabled: `DecollideCamera` strips terrain layers from its obstacle layers, and with every campus object on layer `0` enabling it would silently disable all decollision.
- Verification: Eleven Play Mode tests in `Assets/TestCampus/Tests/PlayMode/TestCampusCameraFloorTests.cs`, all passing with a clean Console. Flat hub floor settles at pitch `-6.25°`, radius `7.02 m`, camera Y `0.47`, clearance `0.100 m`. Beside the south wall and in the corner the camera rides the apron at Y `0.01`. On the Movement steps the limit tracks a surface at Y `2.70`, proving it is surface-relative rather than a fixed height. Rotating under compression: worst clearance `0.000 m`, largest per-frame Y step `0.016 m`. Moving under compression: yaw drift `0.000°`, confirming the movement heading is untouched. Releasing pitch recovers `7.00 m` → `9.49 m` with a largest per-frame step of `0.038 m`, so it eases rather than snaps. A baseline test with both layers disabled reproduces the defect and fails if it cannot. Before/after captures are in `docs/audits/camera-floor/`.
- Trade-off measured on flat ground: radius floor `9.5 m` → `-4.65°`, `7.0 m` → `-6.24°`, `4.8 m` → `-9.22°`, `2.4 m` → `-16.00°`. `7.0 m` was chosen; `minimumOrbitRadiusScale` is serialized for further tuning.

## Remaining limitations

### UI lab production canvases are present but not exposed as state-preview controls
- Severity: Medium
- Status: Deferred
- Detail: The persistent campus panel is fully testable, but the inactive production HUD and Pause Canvas still require a dedicated safe test adapter before their states can be previewed without invoking the production game loop.

### Presets currently change test state vocabulary, not fixture density
- Severity: Medium
- Status: Deferred
- Detail: Low, Normal, and Stress are deterministic and visible in diagnostics, but specialist fixture-count changes are not yet implemented.

### Zone detection follows teleport selection
- Severity: Low
- Status: Deferred
- Detail: Physically walking into another room does not update `CurrentZone`; zone triggers are still needed.

### Selective occlusion is a binary prototype hide
- Severity: Low
- Status: Deferred
- Detail: Tagged foreground walls currently use `Renderer.forceRenderingOff`. Production integration should use an art-approved dither/fade or authored removable wall sections, with per-level occluder tagging.

### Camera settings are not yet exposed in the production settings menu
- Severity: Medium
- Status: Deferred
- Detail: The prototype has fixed sensitivity, `-20°..55°` pitch limits, a `2.5 s` recenter delay, and a `9.5 m` radius. Production work still needs sensitivity, inversion, recenter on/off, recenter delay/strength, and reduced-motion settings.

### Upward look is geometrically limited near ground
- Severity: Medium
- Status: Accepted
- Detail: A `9.5 m` orbit cannot descend far without leaving the level, so on flat floor the floor constraint limits pitch to about `-6.2°` even after pulling in to `7.0 m`. The authored `-20°` bound is only reachable where there is a genuine drop beneath the camera. Reaching `-20°` on flat ground would need the radius floor at roughly `2.4 m`, which is the close-up the study rejects. If more upward look is wanted, the lever is the radius, not the pitch bound.

### Camera is not lifted over genuine drops
- Severity: Low
- Status: Accepted by design
- Detail: The ground probe only constrains the camera where it finds real geometry beneath it, so past a ledge the camera dips below the platform the player is standing on. Measured on the tallest Camera-zone height target: pitch `-14.0°`, camera Y `9.11` against a platform top of `10`. This is the deliberate choice to keep a natural dip over real drops rather than clamp everywhere.

### Decollider collider buffer can truncate
- Severity: Low
- Status: Deferred
- Detail: `CinemachineDecollider` uses a fixed 10-collider `Physics.OverlapCapsuleNonAlloc` buffer. The hub's 18 full-length grid-line colliders make truncation plausible in dense areas. Not observed to cause a failure, and not addressed.
