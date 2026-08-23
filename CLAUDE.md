# CrazyMarket Claude Instructions

Read and follow `AGENTS.md` for the project's GitHub, Unity, safety, and review
rules.

## Automatic local review workflow

When finishing or reviewing a feature, splitting a large change, or validating
a stack branch, automatically read and follow:

- `.codex/skills/crazy-market-review-loop/SKILL.md`
- `.codex/skills/crazy-market-review-loop/references/acceptance-gates.md`

Use `Tools/LocalReview/review_loop.py` to run the workflow and keep its live
dashboard current. Open the graph visibly in the machine's browser. If Claude
supports subagents, use two fresh read-only reviewers in parallel: architecture
and Unity correctness. Reviewer agents must not edit files.

Keep review branches, findings, and validation local. Do not push or create PRs
unless the user separately requests publishing. Do not add tests unless the
user requests them. Run the required Unity smoke validation for relevant
changes, and stop after three review/fix rounds.

## Agent skills

### Issue tracker

Issues and specs are tracked in GitHub Issues for `FA24-CAT408/MarketDash`. See `docs/agents/issue-tracker.md`.

### Triage labels

Use the default five-role triage vocabulary. See `docs/agents/triage-labels.md`.

### Domain docs

This repository uses a single-context domain-documentation layout. See `docs/agents/domain.md`.
