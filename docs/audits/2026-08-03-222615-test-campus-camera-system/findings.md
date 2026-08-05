# Findings

## CAM-001 — All-layer camera collision policy

- Dimension: correctness and component boundaries
- Severity: medium
- Confidence: confirmed
- Consequence: moving platforms, props, NPC colliders, and arbitrary scene geometry can push,
  shorten, lift, or pitch-limit the assisted camera.
- Evidence: `TestCampusSceneGenerator.cs:250-278`, `TestCampusCameraSurfaceConstraint.cs:12-22`,
  `TestCampusCameraSurfaceProbe.cs:12-18`, and `Moving Platform.prefab:16`.
- Direction: author explicit camera-obstacle and camera-surface masks or marker contracts; exclude
  dynamic fixtures; preserve the camera-only ground apron marker path.

## CAM-002 — Platform-carried displacement steers recentering

- Dimension: correctness
- Severity: medium
- Confidence: strongly supported
- Consequence: a platform transporting a stationary player can be interpreted as intentional
  player movement and rotate the assisted camera during automatic recentering.
- Evidence: `TestCampusCameraPrototypeController.cs:124-136`.
- Direction: derive recenter heading from intentional input, or subtract support-platform motion.

## Dimension dispositions

- Architecture: assessed; collision responsibility is split across three cooperating components.
- Coupling: assessed; shared broad physics masks couple camera behavior to arbitrary world objects.
- Correctness: assessed; CAM-001 confirmed and CAM-002 strongly supported.
- Security: not applicable.
- Performance: assessed; non-alloc surface casts are bounded, while selective occlusion uses
  allocating `SphereCastAll` each live orbit frame.
- Tests: no automated tests requested; prior interactive campus validation exists.
- Operability: assessed; the HUD exposes yaw, pitch, radius, floor/ceiling limits, and camera Y.
- Documentation drift: assessed; comments describe the floor strategy but not the required
  obstacle-layer ownership contract.
