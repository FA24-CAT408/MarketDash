---
name: steady-improvement
description: Keep CrazyMarket moving through one bounded player-visible improvement at a time. Use when choosing the next improvement, scoping a feature or fix, resuming the active improvement, checking scope, recording discoveries, or finishing and handing work into local review.
---

# Steady Improvement

Maintain continuity in `.codex/improvements/board.md`. If it does not exist, create it from `assets/board-template.md`. Treat the board as project state: read it before proposing or changing work, update it after material progress, and preserve unrelated user edits.

## Orient

Read the board, `AGENTS.md`, Git status, the current branch, and recent commits. State the active outcome and its remaining budget. If the working tree contains unexplained changes that overlap the improvement, identify ownership before editing.

Infer the requested mode:

- **Choose:** no active improvement, or the user asks what to do next.
- **Work:** implement or continue the active improvement.
- **Checkpoint:** the user asks for status, or a tripwire fires.
- **Finish:** the target behavior works and needs review, validation, or handoff.

## Choose

Offer at most three candidates grounded in observed gameplay, recorded Inbox items, or the user's idea. Favor a small player-visible outcome that can be validated in one Test Campus scene. The user owns taste and priority; do not begin a materially different candidate without their choice.

Write the selected contract to **Active**:

- one-sentence player-visible outcome;
- why it matters now;
- current behavior or reproduction;
- Minimum, Target, and Stretch scopes;
- concrete acceptance criteria including one reset or boundary path;
- non-goals;
- relevant scene or zone;
- branch/base and status;
- budget, defaulting to two implementation approaches and one review/fix round.

Completion criterion: the Minimum is independently worth shipping and every adjacent idea has an explicit home outside Active.

## Work

Implement Minimum before Target; Stretch is optional and never blocks completion. Keep one implementation owner. Use AI reviewers only during Finish unless the user explicitly asks for parallel implementation and the components are independent.

After each meaningful checkpoint, update progress, decisions, attempts used, validation evidence, and newly discovered work on the board. Put adjacent discoveries in Inbox rather than silently expanding Active.

Trigger a Checkpoint when any of these occurs:

- the change crosses a listed non-goal;
- more than three runtime components, twelve handwritten files, or roughly 1,000 added handwritten C# lines become necessary;
- a scene-wide or architectural rewrite appears necessary;
- a second bug or separate player outcome is discovered;
- two implementation approaches fail;
- the recorded budget is exhausted;
- the behavior cannot be reproduced reliably.

## Checkpoint

Pause scope expansion and report:

1. what works and the evidence;
2. what remains;
3. why scope changed or the attempt failed;
4. the smallest shippable option;
5. the larger option and its cost;
6. a recommendation.

Record the checkpoint on the board. Continue safe investigation while it can distinguish the options, but require the user's direction before materially expanding the contract.

## Finish

Finish the smallest meaningful scope that satisfies the Active contract. Follow the project-required `crazy-market-review-loop` skill for review, Unity smoke validation, dashboard evidence, and its three-round ceiling. Do not duplicate that workflow here.

After the acceptance gates pass:

- mark the Active item completed with the validated scope and commit(s);
- record deferred Target or Stretch work in Inbox or Next;
- move a maximum of three shaped candidates into Next;
- leave Active empty until the user selects another improvement;
- summarize the player-visible result, validation, deferred work, and best next candidate.

Completion means a shippable improvement, not the exhaustion of every related idea.
