# Acceptance Gates

## 1. Scope and manageability

- The branch has one feature purpose explainable in one sentence.
- Generated `.unity`, `.meta`, and material YAML are excluded from handwritten
  size judgment.
- More than 1,000 added handwritten C# lines, more than 12 handwritten source
  files, or three new runtime components requires an explicit reviewer justification or
  another stack split.
- Public components have one clear responsibility and names describe their
  role without requiring implementation archaeology.

## 2. Agent review

- Two fresh read-only reviewers completed the architecture and correctness
  passes.
- No unresolved blocker or should-fix finding remains.
- Suggestions are recorded but do not extend the loop automatically.
- Reviewers did not edit code or see another reviewer's conclusions before
  submitting their own.

## 3. Component architecture

- Ownership and lifecycle boundaries are explicit.
- Dependencies point from adapters/composition toward stable contracts; test
  campus code does not duplicate production gameplay rules.
- Components communicate through small contracts or direct composition rather
  than global searches and hidden side effects where practical.
- Generated scenes reproduce the intended hierarchy and retain valid Unity
  metadata and serialized references.

## 4. Unity behavior

- Scripts compile with zero errors.
- Affected generated content is regenerated when needed and passes
  `CrazyMarket/Test Campus/Validate`.
- The relevant scene enters Play Mode and the changed happy path plus one
  reset/boundary path is exercised.
- Console has zero relevant Error, Exception, or Assert entries.
- Build behavior changes run the release safeguard and leave
  `EditorBuildSettings.asset` byte-for-byte unchanged.
- Unity-created unrelated file changes are identified and cleaned without
  touching pre-existing user work.

## 5. Stack and handoff

- Fixes are committed on the branch that owns the component.
- Descendant stack branches are rebased locally.
- No branches or PRs were pushed or created.
- The final summary explains purpose, components, data flow, validation, and
  deferred suggestions in plain language.
