---
name: crazy-market-review-loop
description: Run a bounded local review, fix, and Unity-validation loop for CrazyMarket stack branches. Use when reviewing or finishing a feature, splitting a large change, handing a diff to reviewer agents, validating component architecture and readability, exercising Test Campus behavior in Unity, or showing live review progress in the local dashboard. Keep all branches, findings, and validation local; do not push or create PRs.
---

# CrazyMarket Review Loop

Use the project tool at `Tools/LocalReview/review_loop.py`. Read
`references/acceptance-gates.md` before deciding that a branch is complete.

## Start

1. Identify the immediate parent branch in `gh stack view`.
2. Initialize state and run preflight:

   ```bash
   python3 Tools/LocalReview/review_loop.py init --base <parent-branch>
   python3 Tools/LocalReview/review_loop.py preflight
   ```

3. Start the dashboard if it is not already running:

   ```bash
   python3 Tools/LocalReview/review_loop.py dashboard
   ```

   This is a long-running local server. Start it in a persistent terminal or
   background tool session so the review can continue alongside it. Always
   open the graph visibly: use the T3 shared preview first; if it is unavailable,
   let the command open the machine's default browser. Do not continue with a
   hidden dashboard. Verify that the page shows the current branch and phase.

Keep the dashboard state current with the `phase`, `agent`, `finding`, and
`check` commands. The dashboard is evidence of work, not a substitute for it.

## Review round

Run at most three rounds. For each round:

1. Mark `review` running.
2. Spawn two fresh, read-only reviewers in parallel when agent tools are
   available. Pass only the base/head range, raw diff, requirements, and
   project instructions. Do not leak earlier conclusions.
3. Use these roles:
   - `architecture`: component responsibilities, ownership, dependency
     direction, public contracts, duplication, and understandability.
   - `correctness`: Unity lifecycle, serialization/GUID safety, scene/build
     safety, regressions, unnecessary scope, and reproducibility.
4. Register each reviewer before spawning it, then mark it passed, failed, or
   blocked when it returns:

   ```bash
   python3 Tools/LocalReview/review_loop.py agent architecture-r1 running --role architecture --task "Review component boundaries and understandability"
   python3 Tools/LocalReview/review_loop.py agent correctness-r1 running --role correctness --task "Review Unity correctness, safety, and scope"
   ```

   Spawn both only after registration. Give each the `<base>...HEAD` range,
   raw diff, requirements, and `AGENTS.md`; forbid edits and Unity execution.
   When each returns, rerun its `agent` command with `passed`, `failed`, or
   `blocked` and a short `--note`.

5. Normalize findings to `blocker`, `should-fix`, or `suggestion`. Record them
   in the dashboard. Missing new tests is not a finding unless the user opts in
   to tests.

   ```bash
   python3 Tools/LocalReview/review_loop.py finding ARCH-001 open --severity should-fix --title "Short actionable finding" --owner architecture-r1
   python3 Tools/LocalReview/review_loop.py finding ARCH-001 resolved --owner main
   ```
6. Deduplicate the reviews. Main agent fixes only blockers and should-fix
   items. Reviewers never edit files.

Prefer the installed Open Code Review artifacts as additional evidence, not as
a replacement for fresh reviewer agents. Never post OCR output to GitHub.

## Fix and validate

Keep fixes small and on the branch that owns the component. After committing
there, run `gh stack rebase --no-trunk --upstack` to rebase its descendants
locally. Never push.

After changes, run Unity validation:

```bash
python3 Tools/LocalReview/review_loop.py unity-smoke
```

Add `--regenerate` when the generator, scenes, prefabs, or their inputs changed.
Add `--build-guard` when build behavior changed. Adjust `--expected-scenes` and
`--zone` when validating a lower stack branch that intentionally contains only
part of the campus.

Inspect all files Unity changed. Preserve intended regenerated assets and remove
only known validation side effects. Do not automatically discard dirty user
work.

## Iterate and stop

Start another round only when a blocker or should-fix item required code
changes:

```bash
python3 Tools/LocalReview/review_loop.py next-round
```

Stop successfully only when every acceptance gate passes. Stop after three
rounds and ask the user for direction rather than chasing perfection. Leave
suggestions documented unless the user chooses them.

Give the user a short branch summary: purpose, components, data flow, fixes,
Unity behavior exercised, and any deferred suggestions.
