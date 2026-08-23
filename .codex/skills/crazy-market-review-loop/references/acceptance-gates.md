# Acceptance Gates

These gates apply to formal feature or stack-branch completion. Targeted
changes use the focused validation path in `SKILL.md`.

## 1. Scope and manageability

- The change has one feature purpose explainable in one sentence.
- The reviewed scope excludes unrelated pre-existing dirty work.
- Generated `.unity`, `.meta`, and material YAML are excluded from handwritten
  size judgment, but unexpected generated churn is explained before acceptance.
- More than 1,000 added handwritten C# lines, more than 12 handwritten source
  files, or three new runtime components requires an explicit reviewer
  justification or another stack split.
- Public components have one clear responsibility and names describe their
  role without requiring implementation archaeology.

## 2. Formal review

- Two fresh read-only reviewers completed the architecture and correctness
  passes in isolated contexts.
- Each pass records the actual backend, model, effort, round, and scope.
- Fable is preferred for difficult reasoning and review when available; a
  particular model is not a completion gate unless the user requested it.
- The reviewed scope stayed unchanged during both passes.
- No unresolved blocker or should-fix finding remains.
- A reviewer is `passed` only when it returned suggestions or no findings;
  blocker or should-fix findings make that pass `failed`.
- Suggestions are recorded but do not extend the loop automatically.
- Reviewers did not edit code or see another reviewer's conclusions before
  submitting their own.

## 3. Component architecture

- Ownership and lifecycle boundaries are explicit.
- Dependencies point from adapters and composition toward stable contracts;
  Test Campus does not duplicate production gameplay rules.
- Gameplay state avoids scattered mode booleans and special-case branches when
  a smaller explicit state model or policy would remove complexity.
- Unity-facing adapters own engine lifecycle and serialization concerns where
  that separation materially simplifies the design.
- Generated scenes reproduce the intended hierarchy and retain valid Unity
  metadata and serialized references.

## 4. Unity behavior

- Scripts compile with zero relevant errors.
- Affected generated content is regenerated only when its generator or inputs
  changed, and passes the applicable validator.
- Production changes are exercised in the actual affected production scene.
- The changed happy path plus one reset or boundary path is exercised.
- Feature-specific behavior evidence is recorded separately from generic smoke
  validation.
- Camera, UI, scene, prefab, and presentation changes have inspected Game-view
  evidence at a relevant resolution or aspect ratio.
- The Console has zero relevant Error, Exception, or Assert entries.
- Build behavior changes run the release safeguard and leave
  `EditorBuildSettings.asset` byte-for-byte unchanged.
- Unity-created unrelated file changes are identified without touching
  pre-existing user work.

## 5. Handoff

- The final handoff states whether changes are committed or uncommitted.
- Commit and rebase occur only when an explicit stack handoff requires them.
- No branches or pull requests are pushed or created unless separately
  requested.
- The summary explains purpose, behavior exercised, visual evidence,
  unverified areas, and deferred suggestions in plain language.
