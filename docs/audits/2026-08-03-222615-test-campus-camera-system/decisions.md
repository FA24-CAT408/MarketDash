# Architectural decisions

- Observed, confirmed: use Cinemachine priority switching rather than enabling/disabling cameras.
- Observed, confirmed: keep broad horizontal obstacle decollision separate from vertical
  floor/ceiling guarantees.
- Observed, confirmed: disable Decollider terrain resolution because the generated campus shares
  layer 0 and its terrain probe can mistake ceilings for ground.
- Inferred, strongly supported: Ground Guard is added after Decollider so its Body-stage callback
  is the last positional guarantee before Aim recomposes the shot.
- Unresolved design choice: whether the rail camera should ignore collision by design or receive a
  separate rail-specific policy.
