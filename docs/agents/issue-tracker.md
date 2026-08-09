# Issue tracker: GitHub

Issues and specs for this repository live in GitHub Issues for
`FA24-CAT408/MarketDash`. Use the `gh` CLI for all operations.

GitHub operations for this project must use the personal account `Abe-54`.
Do not use `AbrahamRubioDCA`.

## Conventions

- Create: `gh issue create --title "..." --body "..."`
- Read: `gh issue view <number> --comments`
- List: `gh issue list --state open --json number,title,body,labels,comments`
- Comment: `gh issue comment <number> --body "..."`
- Add or remove labels: `gh issue edit <number> --add-label "..."` or `--remove-label "..."`
- Close: `gh issue close <number> --comment "..."`

Infer the repository from `git remote -v`; `gh` does this automatically inside
the clone.

## Pull requests as a triage surface

**PRs as a request surface: no.**

## When a skill says "publish to the issue tracker"

Create a GitHub issue.

## When a skill says "fetch the relevant ticket"

Run `gh issue view <number> --comments`.

## Wayfinding operations

The `/wayfinder` map is one issue with child issues as tickets.

- Label the map `wayfinder:map`.
- Label child tickets `wayfinder:<type>`, where the type is `research`,
  `prototype`, `grilling`, or `task`.
- Represent blocking relationships using GitHub native issue dependencies.
- If native dependencies are unavailable, add `Blocked by: #<number>` to the
  child issue.
- Claim a ticket with `gh issue edit <number> --add-assignee @me`.
- Resolve a ticket by recording its answer in a comment and then closing it.
