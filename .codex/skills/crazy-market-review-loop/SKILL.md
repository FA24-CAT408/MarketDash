---
name: crazy-market-review-loop
description: Use for explicit feature or stack-branch completion or review, substantial gameplay or generated-content changes, or requested Test Campus validation. Small fixes, alignment, documentation, skill edits, and read-only questions use targeted checks instead.
---

# CrazyMarket Review Loop

Use this skill for a formal local review, fix, and Unity-validation handoff. Use
`Tools/LocalReview/review_loop.py` and read `references/acceptance-gates.md`
before declaring a formal branch complete.

For explicit architecture review or substantial cross-component changes, also
read and apply `../thermo-nuclear-code-quality-review/SKILL.md`. Routine feature
validation uses the focused Unity checks below.

## Choose the path

- **Targeted change:** inspect the diff, run the narrowest relevant check, and
  report what was verified. Do not initialize review state, start the dashboard,
  or spawn reviewers.
- **Feature change:** validate the affected behavior in Unity and provide
  focused Play Mode and visual evidence.
- **Formal branch review:** run the bounded review loop below.

## Formal branch review

1. Define one feature purpose, the intended file scope, the target scene, the
   changed happy path, the reset or boundary path, and any expected generated
   outputs.
2. Inspect `git status` and keep unrelated dirty work outside the review scope.
   Use `init --base <parent-branch>` and `preflight` only for a formal review.
3. Start the dashboard only for the formal loop or when the user requests it.
   If started, open it visibly and keep its phase, findings, and checks current.
   Dashboard state is evidence of the workflow, not proof of behavior.
4. Run at most three review rounds. Each formal round uses two fresh,
   read-only reviewers: `architecture` and `correctness`. Prefer Fable for
   difficult reasoning and review when it is available. Otherwise use the
   strongest available reviewer and record the actual backend, model, and
   effort. Pause for approval only when the user explicitly requires a
   particular model. Never label a reviewer as Fable unless it performed the
   review.
5. Reviewers receive the declared scope, raw diff, requirements, `AGENTS.md`,
   and their rubric. They do not edit files, run Unity, inspect dashboard
   conclusions, or read another reviewer's report. Keep the reviewed scope
   unchanged while both passes run.
6. Normalize findings as `blocker`, `should-fix`, or `suggestion`. Fix only
   blockers and should-fix items in the formal loop; leave suggestions
   documented unless the user chooses them.

## Unity validation

For production gameplay or presentation changes:

- compile the project;
- enter the actual affected production scene in Play Mode;
- exercise the changed happy path and one reset or boundary path;
- inspect the Console and Game view;
- visually inspect a representative capture for camera, UI, scene, prefab, or
  presentation changes.

Test Campus is supporting evidence only. It does not replace production
validation. If Unity is unavailable or the target scene was not exercised,
report the work as `unverified` rather than complete.

Run `unity-smoke` as a baseline, then exercise the feature-specific path with
the applicable Unity action or live Editor interaction. Record the behavior
evidence separately before marking the formal workflow done.

Regenerate only when the generator or its inputs changed, or when regeneration
is explicitly part of the task. When generated output changes, check
repeatability and inspect diff size and content before accepting it. Use
`--build-guard` for build-behavior changes.

## Handoff and stopping

- Do not commit or rebase automatically. Commit and rebase only for an
  explicitly requested stack handoff.
- Stop successfully only when the formal acceptance gates pass. Stop after
  three rounds and ask the user for direction rather than chasing perfection.
- Summarize the purpose, changed components, behavior exercised, visual
  evidence, unverified areas, fixes, and deferred suggestions.
