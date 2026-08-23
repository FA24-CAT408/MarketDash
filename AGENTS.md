# CrazyMarket Agent Instructions

## Working style

- Keep work tight and playable: define the requested outcome and touch only files needed for it. Prefer the smallest reversible solution; defer refactors, abstractions, assets, docs, tests, and tooling unless required.
- Check `git status` first. Preserve unrelated dirty work; never reset, discard, or overwrite it.
- Treat alignment, explanation, and read-only review requests as read-only. Do not start Unity, dashboards, audits, or subagents unless the request requires them.

## Game development

- Iterate in narrow playable slices: change one behavior, playtest it, inspect the result, then expand.
- Test Campus is a prototype and diagnostic harness. It can validate reusable seams, but it is not proof that production scenes work.
- Keep production gameplay rules in production owners. Test adapters and fixtures compose them without duplicating them.
- Decide ownership before changing serialization or generated content.
- Regenerate scenes and prefabs only when their inputs or generator logic changed. Check repeatability and inspect the resulting diff before accepting it.

## Verification

- For gameplay, camera, UI, scene, prefab, or generator changes: compile; enter the actual affected scene in Play Mode; exercise the changed happy path and a reset or boundary path; inspect the Console and Game view.
- Visual changes require visual inspection at a relevant Game-view resolution or aspect ratio.
- Report validation as `verified`, `partially verified`, or `unverified`. Static inspection, stale evidence, and Test Campus results must not be presented as production verification.
- If Unity is unavailable, say so clearly. Do not infer runtime success.
- Automated tests are optional unless requested; interactive Play Mode testing is required for Unity-facing changes.

## Project workflow

- GitHub owns scope, status, dependencies, and acceptance criteria. Notion is supporting design reference. For GitHub work, read `docs/agents/issue-tracker.md` and use the `Abe-54` account.
- Use `crazy-market-review-loop` only when completing or explicitly reviewing a feature or stack branch. Use architecture-audit skills only when explicitly requested.
- Keep work local. Do not push, create PRs, or make unrelated project-management changes unless explicitly requested.
