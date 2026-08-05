# CrazyMarket Camera Prototype Study

Date: 2026-07-25

## Decision

Use a hybrid camera architecture:

- Assisted orbit is the default for traversal, exploration, backtracking, secrets, and large rooms.
- Guided Cinemachine cameras are short-lived overrides for authored hallways, precision set pieces, showcases, and brief staged moments.
- Camera zones may constrain yaw, pitch, distance, or switch to a rail, but ordinary traversal must not require constant right-stick or mouse correction.
- The current prototype does not expose player zoom.

This is an implementation-tested direction for the Test Campus, not yet a production-level rollout to every CrazyMarket level.

## Design interview and its effect

The design was narrowed through one-question-at-a-time discussion:

| Decision | Answer | Design consequence |
|---|---|---|
| Level scale | Production rooms are much larger than the Test Campus rooms. | A permanently fixed or room-wide camera cannot be the general solution. |
| Structure | Freely explorable, bounded rooms/districts with backtracking and secrets. | Exploration needs player-adjustable orbit; rails become contextual. |
| Platforming | Mixed precision, with demanding optional challenges. | Landing areas need readable pitch and authored overrides in selected challenges. |
| Primary activity | Traversal, not combat. | No lock-on or combat shoulder camera is part of this prototype. |
| Manual control | Optional assist; hands-off play must remain viable. | Mouse/right stick rotate, while gentle automatic assistance remains available. |
| Recenter | Grounded and moving only, with a gentle delay. | Recenter waits `2.5 s`, does not act while airborne or stationary, and performs one latched correction per movement bout. |
| Occlusion | Selected walls/props may fade or hide. | Tagged foreground occluders are hidden instead of forcing a close-up. |
| Authored framing | Some hubs/showcase rooms may constrain orbit; full lock only briefly. | Priority-based camera zones and smooth Brain blends remain part of the architecture. |
| Multiplayer | Single-player only. | No target-group framing or shared-screen compromise is required. |
| Comfort | Comfort-first automatic movement. | Auto yaw is capped, delayed, optional by architecture, and vertical pitch is constrained. |
| Mouse | Always-on mouse look while gameplay has focus. | Gameplay focus reads `Mouse.current.delta`, with a prototype-local legacy fallback for Editor sessions whose Input System mouse reports zero; the test UI releases the cursor. |
| Vertical look | Meaningful but constrained. | Pitch is clamped to `-20°..55°`. The `-20°` bound is reachable only where there is a genuine drop beneath the camera; standing on flat ground the floor constraint limits it to about `-6.2°`, because a `9.5 m` orbit geometrically cannot go lower without leaving the level. |
| Zoom | Avoid for now. | Radius is `9.5 m` and is only ever shortened by the floor constraint, which may pull in to `7.0 m` before it starts lifting the camera along the surface instead. Settings architecture can add player-facing distance later. |

## Research summary

The detailed primary-source review is in [Camera_Primary_Source_Research.md](Camera_Primary_Source_Research.md).

The most applicable findings were:

- Nintendo described *Super Mario 3D World* as using a stable, largely authored camera that reduces disorientation and does not require advanced camera control to finish its courses.
- Nintendo presents *Bowser's Fury* as free-roaming and explicitly teaches right-stick look-around.
- Cinemachine Orbital Follow directly supports constrained horizontal/vertical orbit and input-driven axes.
- Cinemachine Spline Dolly supports authored position and nearest-target progress, but Unity warns that nearest-point selection can become unstable on ambiguous spline shapes.
- Cinemachine priority and Brain blends are an appropriate basis for zone changes.
- Decollider prevents camera-body intersection but does not solve line-of-sight composition.
- Deoccluder can preserve line of sight, but its pull-forward behavior recreates the exact close-up the project is trying to avoid unless carefully limited.
- Microsoft's camera accessibility guidance supports independent sensitivity/inversion, disabling automatic motion, configurable recenter delay/speed, FOV options, and reduced camera shake.

The shipped-game references fall into useful behavior families:

- *Mario Odyssey*, *Bowser's Fury*, *Spyro*, *A Hat in Time*, *Kingdom Hearts*, and modern *Zelda* demonstrate the value of an exploration orbit with contextual automatic help.
- *Super Mario 3D World*, *Kirby and the Forgotten Land*, *Crash 4*, and *Sackboy* demonstrate how authored direction and wider framing can improve linear platforming readability.
- *Mario Galaxy*, *Psychonauts 2*, *Ratchet & Clank*, and *It Takes Two* support a contextual approach: ordinary player agency with authored cameras for particular traversal or set-piece needs.
- *God of War* is a useful contrast: its close shoulder composition supports combat intimacy, but sacrifices the surrounding floor and landing visibility needed here.

Those comparisons describe observable design patterns, not undocumented internal implementations.

Rejected as a universal default:

- A fully fixed theatrical camera conflicts with free exploration and backtracking.
- A rail-only camera requires too much per-level authoring and fails when the player leaves its intended corridor.
- A collision response based only on pushing the camera toward the player preserves geometry separation but destroys spatial awareness.
- Fully unconstrained orbit would increase indoor collision problems and permit unusable pitch angles.

## Current camera diagnosis

### Why the wall case spun

`KCCPlayerController.ConvertToCameraSpace` used the physical `Camera.main` transform as the movement frame. When collision or obstruction moved the Main Camera, its horizontal heading changed. The next movement frame used that changed heading, which redirected the player, which moved the camera again. Holding `S` at a wall could therefore create a repeated 180-degree camera/player feedback loop.

The fix separates responsibilities:

- Cinemachine owns rendered camera position and collision.
- A stable `Camera Movement Reference` transform owns the movement heading.
- Assisted Orbit drives that heading from the uncorrected orbit yaw.
- Guided mode derives it from the active Main Camera's planar forward direction.

### Why the camera zoomed

Pull-forward obstruction strategies solve line of sight by shortening the target-to-camera distance. Near a wall, that makes the player dominate the frame and removes the route from view. The prototype instead:

- keeps an orbit radius of `9.5 m`;
- uses Cinemachine Decollider only to prevent camera-body intersection;
- sphere-casts from the player to the camera;
- hides only explicitly tagged foreground wall/column renderers;
- uses non-colliding floor aprons and backdrops so a cutaway view still reads as an interior.

The aprons now carry trigger-only colliders marked `TestCampusCameraGround`. They remain invisible
to the player and to the KCC motor, so the cutaway still works, but the camera's ground probe can
see them — without that, the camera dropped straight through the level whenever it swung outside
the room.

### Why the floor did not stop the camera

The Decollider only displaces the camera when `Physics.ComputePenetration` reports the camera sphere
is *inside* a collider. Looking up drives the vertical axis toward `-20°`, which places the camera at
`target.y + 9.5·sin(-20°)` ≈ `-2.0` — below the hub floor slab's `[-1, 0]` span entirely, in open
air, so there was no penetration to resolve and the camera rendered the level from underneath.
Measured unconstrained: camera Y `-2.04` at the hub corner at the full `9.5 m` radius.

Cinemachine's own `TerrainResolution` is not the fix. `CinemachineDecollider.DecollideCamera` removes
the terrain layers from its obstacle layers, and every Test Campus object is on layer `0`, so
enabling it would silently disable all decollision; its probe also starts 10 m above the camera and
would mistake the hub ceiling for ground. The floor is handled instead by a ground constraint whose
probe starts at the orbit target's height and only ever sweeps downward.

### Input mismatch found during audit

The project input asset currently binds Look to `<Mouse>/position`, while camera look needs pointer delta. The prototype reads `Mouse.current.delta` directly, falls back to the legacy mouse axes/pointer only inside the Test Campus when the Editor does not update the Input System mouse, and reads the controller right stick directly. Production integration should correct the action binding to delta and route camera input through the project's input/settings layer rather than carrying the fallback into production.

## Prototype A: Assisted Orbit

Components and behavior:

- `CinemachineCamera`
- `CinemachineOrbitalFollow`, Sphere style
- `CinemachineRotationComposer`
- `CinemachineDecollider`
- `TestCampusCameraGroundGuard`
- `TestCampusCameraPrototypeController`
- `TestCampusSelectiveOccluder`
- `9.5 m` radius, shortened only by the floor constraint and never below `7.0 m`
- `58°` field of view
- `-20°..55°` pitch, further limited near ground by the floor constraint (about `-6.2°` on flat floor)
- mouse delta and controller right-stick orbit
- `2.5 s` delayed recenter
- one latched, capped recenter correction per continuous grounded movement bout
- no recenter while stationary or airborne
- `R` manual recenter
- stable camera-relative movement proxy

Controls:

- `F1`: hide/show Test Campus UI and transfer cursor focus
- Mouse: always-on orbit while gameplay has focus
- Right stick: orbit
- `R`: recenter
- `F6`: select Assisted Orbit

Known limitations:

- Selective occlusion is binary hide, not an art-ready dither.
- Occluders must be tagged deliberately.
- Sensitivity, inversion, recenter, and reduced-motion settings are not yet exposed.
- The Test Campus controller is a comparison harness, not the final production camera service.

## Prototype B: Guided Rail

Components and behavior:

- `CinemachineCamera`
- `SplineContainer`
- `CinemachineSplineDolly`
- nearest-point automatic dolly
- `CinemachineRotationComposer`
- `55°` field of view
- authored spline points at `(75,8,-18)`, `(75,9,18)`, and `(75,11,55)`
- damped position and angle

Controls:

- `F7`: select Guided Rail
- Player movement remains available; the camera stays on its authored centerline.

Strength:

- Clear, predictable framing while the player follows the intended corridor.

Known limitations:

- Off-axis exploration can put a wall between camera and player.
- Branching, backtracking, and large rooms require more splines, transitions, and authoring rules.
- Nearest-point selection needs special care on curves, loops, and overlapping path segments.

## Prototype C: Hybrid Zone

Components and behavior:

- `TestCampusCameraModeZone` on a trigger volume
- Cinemachine priority changes
- Cinemachine Brain blends
- Assisted Orbit outside the trigger
- Guided Rail inside the authored corridor

Controls:

- `F8`: enable Hybrid Zones

Observed transition:

- Outside trigger at player Z=`5.00`: active camera was `CM Test Campus Player Camera`.
- After holding the real `W` key to Z=`16.96`: active camera changed to `CM Guided Rail Prototype` and `CinemachineBrain.IsBlending` was true.
- After settling: Guided Rail remained active and blending became false.
- Holding the real `S` key back to Z=`5.00` restored Assisted Orbit.

## Prototype comparison

Ratings use 1 (poor) to 5 (strong) for this project.

| Criterion | Assisted Orbit | Guided Rail | Hybrid |
|---|---:|---:|---:|
| Player freedom | 5 | 2 | 5 |
| Spatial awareness | 4 | 4 on-path / 2 off-path | 5 |
| Platforming readability | 4 | 5 on authored route | 5 |
| Tight indoor behavior | 4 | 4 | 5 |
| Large/open-room behavior | 5 | 2 | 5 |
| Backtracking | 5 | 2 | 5 |
| Wall collision/occlusion | 4 | 3 | 4 |
| Comfort | 4 | 4 when well authored | 5 with settings |
| Required camera input | Optional | None on-path | Optional |
| Runtime complexity | 3 | 3 | 4 |
| Per-level authoring cost | 4 | 1 | 3 |
| Long-term maintainability | 4 | 2 as a universal system | 4 with strict zone rules |

The rail gave the cleanest intended-corridor composition. At the identical off-path position, however:

- player: `(84.00, 0.01, 20.00)`
- guided camera: `(75.00, 8.67, 10.38)`
- rail position: `0.5116769`
- result: player occluded behind an orange corridor wall

Assisted Orbit at the same player position moved to `(75.19, 4.77, 20.00)`, hid one approved wall section, and kept the player visible.

## Direct Play Mode verification

All inputs below were applied while Unity was in Play Mode. Keyboard tests used real macOS key events delivered to the focused Unity Game view. Mouse-look tests queued a real Input System mouse-delta state into the same runtime path used by the prototype.

| Test | Observed result |
|---|---|
| Exact reported regression: hold `S` at south wall for 4 seconds | Player stopped at `(10.00, 0.03, -19.22)`; yaw stayed `0°`; orbit radius stayed `9.5 m`; one foreground wall hid. No 180-degree loop. |
| Manual mouse look | Delta `(650,-100)` changed yaw from `0°` to `52°` and pitch from `22°` to `30°`. |
| Pitch abuse | Large deltas clamped exactly to `-20°` and `55°`. |
| Camera-relative forward after manual orbit | From `(75,-8)`, real `W` moved the player to `(80.98,-1.70)`; movement remained aligned with the rotated view. |
| Jump near corridor geometry | Real Space input put the player at Y=`2.26`; yaw remained `0°`; the player and blob shadow remained visible. |
| Delayed recenter | One second into `W+A`, yaw remained `52°`; the latched correction later settled at `7.14°` and remained `7.14°` after input stopped. |
| Guided rail on route | At player `(75,0,20)`, camera was `(75,8.67,10.38)` and rail progress was `0.5116779`. |
| Guided rail off route | Moving player to X=`84` left camera X=`75`, exposing the rail's branch/occlusion limitation. |
| Hybrid entry and exit | Real `W` entered the trigger and blended to rail; real `S` exited and restored orbit. |
| Console after final runtime regression | `0` logs, `0` warnings, `0` errors. |

Validation:

- Test Campus scene contract validator: passed all seven scenes.
- EditMode tests: `6/6` passed.
- PlayMode test discovery/run: `0` tests present, `0` failures.

## Evidence

- [Assisted orbit in the open corridor](../Temp/TestCampus/Evidence/final-assisted-open.png)
- [Manual orbit after mouse input](../Temp/TestCampus/Evidence/final-assisted-manual-orbit.png)
- [Fixed-distance wall response](../Temp/TestCampus/Evidence/final-assisted-wall-render.png)
- [Guided rail on its intended route](../Temp/TestCampus/Evidence/final-guided-rail.png)
- [Guided rail off-path occlusion](../Temp/TestCampus/Evidence/final-guided-off-path.png)
- [Assisted orbit at the same off-path position](../Temp/TestCampus/Evidence/final-assisted-off-path.png)
- [Hybrid transition frame](../Temp/TestCampus/Evidence/final-hybrid-transition.png)
- [Jump near corridor geometry](../Temp/TestCampus/Evidence/final-assisted-jump-wall.png)
- [Unity Console with zero runtime entries](../Temp/TestCampus/Evidence/final-console-zero.png)

## Recommendation and next production step

Promote the architecture, not the Test Campus harness:

1. Create a production camera service with Assisted Orbit as the normal gameplay camera.
2. Correct the project's Look input to mouse delta and route both mouse/controller through the existing input/settings layer.
3. Add camera profile assets for open traversal, tight interiors, precision platforming, and authored rail/fixed shots.
4. Add priority-based camera zones with explicit entry/exit direction, interruption, and backtracking tests.
5. Replace binary renderer hiding with an art-approved selective fade or removable foreground-wall system.
6. Tag camera collision and occlusion layers in representative production rooms, then repeat this same route matrix at production scale.
7. Expose sensitivity, horizontal/vertical inversion, recenter off/delay/strength, camera shake, smoothing, FOV, and reduced-motion controls.

No further design answer is required to keep prototyping. The remaining choices are production tuning decisions: the final fade treatment, per-profile camera distance/FOV, and which rooms receive guided zones.
