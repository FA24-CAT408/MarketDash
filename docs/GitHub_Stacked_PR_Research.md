# GitHub Stacked PR Research

Research date: 2026-08-02

## Conclusion

The feature is GitHub's official **Stacked pull requests** public preview. Its CLI is the first-party GitHub CLI extension [`github/gh-stack`](https://github.com/github/gh-stack), invoked as `gh stack`.

This repository can use the server-side feature. A read-only request to `GET /repos/FA24-CAT408/MarketDash/stacks` returned `HTTP 200` and an empty list (`[]`), meaning the endpoint is available and the repository simply has no stacks yet. GitHub CLI is correctly authenticated as the required personal account `Abe-54`, which has `ADMIN` permission on `FA24-CAT408/MarketDash` and can push to it.

The local prerequisites are almost, but not completely, ready:

- GitHub CLI is installed via Homebrew at `/opt/homebrew/bin/gh`, but its version is `2.89.0`.
- GitHub's current quickstart requires **GitHub CLI 2.90.0 or later**, Git 2.20 or later, authentication via `gh auth login`, and push permission to the repository. See [GitHub's stacked PR quickstart](https://docs.github.com/en/pull-requests/get-started/stacked-prs-quickstart#prerequisites).
- `github/gh-stack` is not currently installed (`gh extension list` returned no extensions).
- The feature is currently a **public preview and subject to change**. [GitHub quickstart](https://docs.github.com/en/pull-requests/get-started/stacked-prs-quickstart).

## Recommended installation

Because this machine's `gh` installation is Homebrew-managed, first update it to meet GitHub's documented minimum, verify the account, and then install the official extension:

```bash
brew update
brew upgrade gh
gh --version                  # must report 2.90.0 or newer
gh auth status               # must show Abe-54 as active
gh extension install github/gh-stack
gh stack --help
```

The extension uses existing GitHub CLI authentication; it does not require a separate StackPR login. GitHub documents `gh auth login` for an unauthenticated installation and requires a repository the user can push to. See the [CLI command reference](https://docs.github.com/en/pull-requests/reference/stacked-prs-cli-commands#installation) and [quickstart prerequisites](https://docs.github.com/en/pull-requests/get-started/stacked-prs-quickstart#prerequisites).

GitHub also publishes an optional agent skill:

```bash
gh skill install github/gh-stack
```

That skill is not required to operate the CLI. It teaches compatible coding agents the workflow. See [GitHub's quickstart](https://docs.github.com/en/pull-requests/get-started/stacked-prs-quickstart#install-the-cli-extension).

## How the stack works

A stack is a linear chain of two or more PRs in one repository. The bottom PR targets the trunk (here, `main`); every subsequent PR targets the branch immediately below it. Consequently, each PR displays only its layer's focused diff. Forks and branching/nonlinear stack shapes are not supported. See [About stacked pull requests](https://docs.github.com/en/pull-requests/get-started/about-stacked-pull-requests) and [GitHub's organization rollout guidance](https://docs.github.com/en/pull-requests/tutorials/roll-out-stacked-prs#stacks-must-be-linear-and-cant-include-forks).

Example:

```text
main
  <- stack/camera-foundation       (PR 1, bottom)
    <- stack/campus-generation     (PR 2)
      <- stack/campus-roams        (PR 3)
        <- stack/testing-philosophy (PR 4, top)
```

GitHub evaluates reviews, required checks, CODEOWNERS, and workflows for every layer against the stack's trunk rules, not merely the intermediate branch that a PR directly targets. Existing `pull_request` workflows targeting `main` therefore run for each stacked PR. See [GitHub's rollout guidance](https://docs.github.com/en/pull-requests/tutorials/roll-out-stacked-prs#2-understand-how-branch-protection-rules-and-ci-work-with-stacks).

## Workflow for an existing large local change

GitHub documents that `gh stack init` can adopt existing branches, while `gh stack modify` can insert, rename, reorder, drop, and fold layers. It requires a clean working tree and linear commit history. See [`gh stack init` and `gh stack modify`](https://docs.github.com/en/pull-requests/reference/stacked-prs-cli-commands#stack-management).

For the current 5,000+ line uncommitted/draft-sized change, the safest workflow is:

1. Inventory the diff and design dependency-ordered, independently reviewable feature layers before committing.
2. Put foundational/shared changes at the bottom. Put features that consume them above. Keep tests with the behavior they validate when practical; use a dedicated testing-philosophy layer only for shared harness/infrastructure changes.
3. Create one branch and one intentional commit set per layer. Avoid blindly staging the entire working tree with `gh stack add -Am`; use explicit path/hunk staging so mixed files land in the correct layer.
4. Initialize/adopt the ordered branches only after their contents and dependencies are verified:

   ```bash
   gh stack init --base main \
     stack/camera-foundation \
     stack/campus-generation \
     stack/campus-roams \
     stack/testing-philosophy
   ```

5. Inspect the local chain with `gh stack view`, and compare every layer against its parent before publishing.
6. Publish as drafts with interactive `gh stack submit`, writing a focused title and explanation for every layer. In non-interactive mode, `gh stack submit --auto` creates drafts by default; `--open` makes them ready for review. See [`gh stack submit`](https://docs.github.com/en/pull-requests/reference/stacked-prs-cli-commands#gh-stack-submit).

This order is illustrative, not a final decomposition. The actual dependency graph must be derived from the current diff; for example, campus roams may need campus-generation assets and APIs, while camera changes may be independent enough to belong in a separate stack rather than a layer of the same stack.

## Day-to-day commands

```bash
gh stack view                 # inspect layers, PRs, and status
gh stack up                   # move one layer away from main
gh stack down                 # move one layer toward main
gh stack rebase --upstack     # propagate a corrected lower layer upward
gh stack push                 # push branches; does not create/update PRs
gh stack submit               # push and create/update PRs + GitHub stack
gh stack sync                 # fetch, cascade rebase, push, and sync state
```

`gh stack` stores local ordering metadata in `.git/gh-stack`, so it is not committed. `gh stack init` automatically enables Git's `rerere` to remember conflict resolutions across cascading rebases. See the [official extension README](https://github.com/github/gh-stack#how-it-works) and [`gh stack init` reference](https://docs.github.com/en/pull-requests/reference/stacked-prs-cli-commands#gh-stack-init).

## Review and merge behavior

- Review from bottom to top when understanding dependencies matters; different specialists can still review separate layers in parallel.
- Correct feedback on the branch where the concern belongs, then cascade that correction through higher layers with `gh stack rebase --upstack`.
- GitHub merges stacks from the bottom up. Selecting a higher PR merges it plus every unmerged PR below it as one contiguous operation; a middle layer cannot merge alone while its prerequisite remains open.
- After a partial merge, GitHub automatically rebases and retargets the next unmerged layer onto the trunk.
- A fully merged stack cannot be extended; further layers start a new stack.

See [GitHub's AI-generated code stacking tutorial](https://docs.github.com/en/copilot/tutorials/stack-ai-generated-code-in-pull-requests) and [Merging stacked pull requests](https://docs.github.com/en/pull-requests/how-tos/merge-and-close-pull-requests/merging-stacked-pull-requests).

## Important operational cautions

- `gh stack push`, `submit`, `sync`, `rebase`, and `modify` can rewrite and force-update stacked branches. The extension uses lease checks, but the planned layer boundaries and branch tips should be verified before any publication.
- A server-side “Rebase stack” produces unsigned commits. If signed commits are required, use the local `gh stack rebase` path so Git honors local signing configuration. See [Managing stacked pull requests](https://docs.github.com/en/pull-requests/how-tos/keeping-your-stacked-pull-requests-in-sync).
- The CLI's local stack metadata does not itself split an undifferentiated working tree. The existing change still needs careful path/hunk-level decomposition and tests on each layer.
- GitHub's docs currently contain a version inconsistency: the detailed CLI reference says the extension needs `gh` 2.0+, while the current quickstart says 2.90.0+. Use the stricter current quickstart requirement, **2.90.0+**.
