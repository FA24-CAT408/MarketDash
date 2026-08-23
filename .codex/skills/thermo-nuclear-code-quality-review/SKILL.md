---
name: thermo-nuclear-code-quality-review
description: Run an extremely strict game-code maintainability review for structural simplification, abstraction quality, giant files, and spaghetti-condition growth. Use for a thermo-nuclear review, deep game-code quality audit, or deliberately harsh maintainability review.
---

# Thermo-Nuclear Game Code Quality Review

Perform a read-only audit unless the user explicitly asks for fixes. Preserve
behavior, serialized data, asset identity, and scene/prefab compatibility while
searching for code-judo moves: restructurings that delete concepts, branches,
layers, or lifecycle hazards instead of merely rearranging them.

## Review procedure

1. Establish the review range, requirements, and affected gameplay paths.
2. Read the raw diff, then inspect enough surrounding code, prefabs/scenes,
   generators, and call sites to understand ownership and runtime behavior.
3. Measure handwritten file growth. Exclude generated scene/prefab/material
   YAML from line-count judgments, but still review generated-asset safety.
4. Apply every standard below to each meaningful change. Trace Unity lifecycle,
   serialization, state transitions, and hot-path behavior where relevant.
5. Report only high-conviction findings, ordered by severity and structural
   leverage. Include the concrete failure or maintenance cost and the smallest
   credible cleaner design.
6. Approve only when no blocker or should-fix remains under the approval bar.
   Return a failed review when either severity is present; suggestions alone do
   not block approval.

Use this baseline:

> Perform a deep code quality audit of the current branch's changes. Rethink
> the structure to improve abstractions, modularity, succinctness, and
> legibility without changing behavior. Be ambitious: pursue clear code-judo
> restructurings that make the implementation dramatically simpler. Measure
> twice, cut once.

## Structural standards

### Delete complexity

- Search first for a reframing that removes modes, flags, branches, helpers, or
  layers entirely.
- Reject refactors that move complexity without reducing the concepts a reader
  must hold in mind.
- Prefer direct, boring code over magic, reflection-heavy indirection, identity
  wrappers, and abstractions that do not simplify ownership or control flow.

### Stop spaghetti growth

- Treat ad-hoc conditionals, scattered feature checks, nullable modes, and
  one-off booleans in unrelated flows as design problems.
- Prefer an explicit state model, policy, dispatcher, data definition, or
  focused component when it makes branches disappear.
- Keep feature logic in the canonical owner. Reuse existing helpers and avoid
  near-duplicate gameplay rules across production, editor, and Test Campus
  code.

### Enforce healthy boundaries

- Keep orchestration separate from gameplay rules when that makes either side
  independently understandable.
- Keep engine-facing concerns—MonoBehaviour lifecycle, serialization, scene
  lookup, input, physics, and asset loading—at clear adapters or composition
  boundaries rather than leaking them throughout domain logic.
- Prefer explicit contracts and invariants over casts, optional parameters,
  silent fallbacks, loosely shaped data, or implicit execution order.
- Avoid both god MonoBehaviours and constellations of thin pass-through
  components. Every component should own a coherent capability or lifecycle.

### Control file and component growth

- Treat a change that pushes a handwritten file from below 1,000 lines to above
  1,000 as a presumptive blocker. Ask for decomposition unless a compelling
  structural reason makes the resulting file unusually cohesive.
- Apply the same scrutiny before the threshold when a MonoBehaviour, editor
  generator, controller, or state machine is already difficult to scan.
- Prefer focused pure C# collaborators, data assets, editor helpers, or
  subcomponents only when the extraction creates a real ownership boundary.

### Keep updates simple and atomic

- Parallelize genuinely independent work when it simplifies orchestration.
- Keep related gameplay state changes atomic enough that observers cannot see
  impossible half-applied state.
- Question event chains, callbacks, coroutines, and async flows whose ordering,
  cancellation, or ownership is implicit.

## Unity and game-development checks

Apply these when relevant to the diff:

- **Lifecycle:** Verify `Awake`, `OnEnable`, `Start`, `Update`, disable/destroy,
  scene unload, domain reload, and object-pooling behavior. Require symmetric
  subscription and cleanup ownership.
- **Serialization:** Protect field continuity, prefab overrides, scene
  references, ScriptableObject data, GUID/meta identity, and migration of
  renamed or reshaped serialized fields.
- **State model:** Look for invalid combinations created by boolean flags,
  duplicated sources of truth, transition logic scattered across callbacks, or
  reset paths that do not restore a valid state.
- **Frame loops:** Flag avoidable per-frame searches, allocations, LINQ,
  reflection, logging, repeated component lookup, or work that belongs on a
  transition/event. Focus on meaningful runtime cost, not speculative micro
  optimization.
- **Time and order:** Check scaled versus unscaled time, physics versus render
  loops, script execution assumptions, input timing, deterministic ordering,
  and pause/reset behavior.
- **Async lifetime:** Ensure coroutines, tasks, tweens, animations, and delayed
  callbacks cannot outlive their owner or mutate stale scene/session state.
- **Editor/runtime separation:** Keep editor-only APIs and generation logic out
  of player code. Generated content must be reproducible and preserve intended
  serialized references.
- **Composition:** Prefer explicit references or composition roots over global
  searches, hidden singletons, and ambient mutable statics. Accept Unity-native
  patterns when they remain local, legible, and lifecycle-safe.
- **Validation:** Identify the Unity smoke path needed to prove the change:
  compile, regenerate when applicable, enter Play Mode, exercise the happy path
  plus one reset/boundary path, and inspect the Console. Do not demand new
  automated tests unless the user requested them.

## Primary questions

For every meaningful change, ask:

- What code-judo move would make this dramatically simpler?
- Can fewer states, components, branches, or helper layers express the same
  behavior?
- Is there one authoritative owner for state, lifecycle, and gameplay rules?
- Does the diff add incidental coupling or execution-order dependence?
- Is this abstraction earning its cost?
- Is a growing file still cohesive, or hiding multiple responsibilities?
- Will scene reload, disable/destroy, pooling, pause, and reset preserve valid
  state?
- Could serialization, prefab overrides, GUIDs, or generated content break?
- Did new hot-path work enter `Update`, physics callbacks, or frequent events?
- Is the proposed cleaner structure realistically verifiable in Unity?

## Findings and approval

Prioritize findings in this order:

1. Structural regressions or ownership/lifecycle hazards
2. Missed code-judo simplifications
3. Spaghetti and state-model growth
4. Serialization, asset, scene, or generated-content risk
5. Boundary, abstraction, and type-contract problems
6. File-size and decomposition concerns
7. Meaningful hot-path, async-lifetime, and legibility problems

For each finding provide:

- severity: `blocker`, `should-fix`, or `suggestion`
- file and line
- the concrete behavior or maintenance cost
- why the current structure causes it
- an actionable remedy that reduces concepts rather than relocating them
- any Unity validation needed after the remedy

Prefer a few high-conviction findings over cosmetic nits. Say clearly when code
works but makes the surrounding design more tangled.

Approval requires all of the following:

- no clear structural regression or lifecycle/ownership hazard
- no plausible high-leverage simplification left unexplored
- no unjustified crossing of the 1,000-line handwritten-file threshold
- no scattered special-case logic or duplicated source of truth
- no needless magic, wrappers, casts, optionality, or boundary leakage
- no credible serialization, prefab/scene, generated-content, or async-lifetime
  hazard
- no avoidable hot-path design that creates meaningful runtime cost

Treat violations as presumptive blockers until the author provides a concrete
justification or a cleaner decomposition.
