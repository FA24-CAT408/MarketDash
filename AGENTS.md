# CrazyMarket Agent Instructions

## GitHub account

- This project must use the personal GitHub account `Abe-54` for GitHub CLI authentication and pushes. Do not use the work account `AbrahamRubioDCA` for this repository.

## Unity automation

- The official Unity CLI is installed and available through the `unity` command. Prefer it for opening the project, checking Editor status, running tests, creating builds, and other supported Unity workflows.
- Official Unity MCP/Editor automation is available through the `com.unity.pipeline` package. Use `unity status` to discover the connected Editor and `unity command` to list or invoke its live Editor commands.
- The project uses Unity's official Pipeline/MCP integration; do not assume the former third-party CoplayDev Unity MCP package is installed or required.

## Unity validation workflow

- After changing gameplay, scenes, prefabs, editor generation, or build behavior, validate the result in Unity before declaring the work complete.
- At minimum: confirm scripts compile, regenerate and validate affected generated content when applicable, enter Play Mode in the relevant scene, exercise the changed behavior, and inspect the Console for errors or exceptions.
- Automated tests are optional unless the user requests them; interactive Unity validation is still required.

## Local review loop

- Automatically use the `crazy-market-review-loop` skill and `Tools/LocalReview/review_loop.py` whenever finishing or reviewing a feature, splitting a large change, or validating a stack branch. The user does not need to request the skill by name.
- Treat `.codex/skills/crazy-market-review-loop/SKILL.md` and its `references/acceptance-gates.md` as the canonical workflow.
- Always open the live agent graph visibly in the T3 shared preview, falling back to the machine's default browser when needed.
- Keep the live dashboard updated during reviewer handoffs, fixes, and Unity validation.
- Use two fresh read-only reviewers when agent delegation is available: one for architecture and one for Unity correctness. Reviewer agents must not edit files.
- Keep the entire workflow local. Do not push stack branches or create PRs unless the user separately requests publishing.
- Do not add tests unless the user requests them; Unity smoke validation is still required for relevant changes.
- Stop after three review/fix rounds and ask the user rather than looping indefinitely.
