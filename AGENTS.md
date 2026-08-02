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
