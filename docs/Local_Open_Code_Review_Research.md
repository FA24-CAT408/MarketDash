# Local Open Code Review Research

Research date: 2026-08-02

## Conclusion

The tool that best matches the description is [Spencer Marx's Open Code Review](https://github.com/spencermarx/open-code-review) (`spencermarx/open-code-review`). It is designed to run reviews through an already-installed AI coding assistant, explicitly supports Codex, saves review artifacts as Markdown, and includes a local browser dashboard.

It can review a local stack before anything is pushed to GitHub. The local objects are Git branches and commits, not literal pull requests: a GitHub PR does not exist until a branch is pushed and a PR is created. Each layer can nevertheless be reviewed as the exact diff between its parent branch and its own tip.

This is probably **not** Alibaba's separate, identically named [`alibaba/open-code-review`](https://github.com/alibaba/open-code-review). Alibaba's tool is a standalone review CLI with configurable LLM endpoints; Spencer Marx's project is the one whose Codex integration, Markdown sessions, and local dashboard align with the requested workflow.

## Subscription and billing

Open Code Review does not document a separate model API key requirement. Its prerequisites say to install an AI coding assistant, and `ocr init` configures supported assistants including Codex under `.codex/`. The assistant performs the model work; OCR supplies the review workflow, personas, orchestration, persistence, and viewer. See the project's [supported tools and requirements](https://github.com/spencermarx/open-code-review#supported-ai-tools).

OpenAI officially states that Codex is included with eligible ChatGPT plans and that the Codex CLI supports **Sign in with ChatGPT**. Therefore, when OCR is invoked from an already authenticated Codex session, it should consume that plan's Codex usage rather than requiring OpenAI API billing. It remains subject to the plan's usage limits. See [Using Codex with your ChatGPT plan](https://help.openai.com/en/articles/11369540-using-codex-with-your-chatgpt-plan).

This does not require CI/CD. GitHub CLI is optional in OCR and is needed only to post a completed review to an existing GitHub PR. The review, review history, dashboard, and Markdown artifacts all work without posting. See [GitHub PR Posting](https://github.com/spencermarx/open-code-review#github-pr-posting) and [Requirements](https://github.com/spencermarx/open-code-review#requirements).

## Privacy: what “local” means

- Git inspection, orchestration state, the dashboard, and generated review files live on this machine. The dashboard specification describes it as local-only with no telemetry or cloud sync; review sessions are stored under `.ocr/`. See the project's [dashboard specification](https://github.com/spencermarx/open-code-review/blob/main/spec.md#purpose) and [session storage documentation](https://github.com/spencermarx/open-code-review#session-storage).
- The dashboard is served locally, and there is no need to create or post a GitHub PR.
- Model inference is **not on-device** when Codex or Claude Code is the selected assistant. Relevant code and prompts are sent to that assistant's cloud provider under its account and data controls. OCR's local storage claim should not be interpreted as offline/private-model execution.
- OCR's multi-agent workflow can use substantially more subscription allowance than a single review because several reviewer passes plus synthesis and discourse may run. Start with a small reviewer team for each stack layer.

## Verified installation path

The official quick start requires Node.js 22.5 or later, Git, and a supported AI coding assistant:

```bash
npm install -g @open-code-review/cli
cd "/Users/abrahamrubio/Documents/Unity Projects/CrazyMarket"
ocr init
ocr doctor
ocr dashboard
```

Source: [Open Code Review quick start](https://github.com/spencermarx/open-code-review#quick-start).

Current machine readiness, checked without installing anything:

- Node.js `v25.2.0`: meets the `>=22.5` requirement.
- npm `11.6.2`: available.
- Codex CLI `0.145.0`: installed at `/Users/abrahamrubio/.local/bin/codex`.
- Claude Code is also installed.
- OCR is not installed yet.
- Obsidian is installed at `/Applications/Obsidian.app`.

`ocr init` is a repository mutation, not just a diagnostic. It creates `.ocr/` material, tool-specific command/skill files such as `.codex/`, and may inject an OCR-managed block into `AGENTS.md` or `CLAUDE.md`. The changes should be inspected before committing. The project's update documentation enumerates these managed files: [Updating OCR](https://github.com/spencermarx/open-code-review#updating-ocr).

## Recommended local stacked-review workflow

Suppose a local linear stack is:

```text
main
  <- stack/foundation
    <- stack/camera
      <- stack/campus-generation
        <- stack/campus-roams
          <- stack/testing-philosophy
```

For each checked-out layer, review only that layer's range against its direct parent:

```text
stack/foundation..stack/camera
stack/camera..stack/campus-generation
stack/campus-generation..stack/campus-roams
stack/campus-roams..stack/testing-philosophy
```

OCR accepts staged changes, commits, branches, and commit ranges as review targets. Its map workflow also supports branch-versus-main targets and is intended for large changesets. See [IDE and CLI workflows](https://github.com/spencermarx/open-code-review#ide--cli-workflows), [Code Review Maps](https://github.com/spencermarx/open-code-review#code-review-maps), and the project's [review command specification](https://github.com/spencermarx/open-code-review/blob/main/openspec/specs/slash-commands/spec.md).

Recommended sequence for every layer:

1. Check out the layer and verify its parent and diff with Git.
2. Ask Codex to run the installed OCR review skill against `parent-branch..HEAD`, stating the feature's requirements and intended behavior.
3. Read and triage findings locally; make corrections on that layer.
4. Run another OCR round. Previous rounds are preserved.
5. Run the Unity tests appropriate to that layer.
6. Mark the layer locally approved and proceed upward.
7. Only after every layer is accepted, decide whether to push the branches/create stacked GitHub PRs or integrate them locally into `main`.

For the whole 5,000+ line change, first generate an OCR Code Review Map. For actual approval, use the smaller parent-to-child range for each layer so reviewers do not repeatedly analyze all lower layers.

## Markdown and Obsidian

OCR stores review material under a structure like:

```text
.ocr/sessions/{date}-{branch}/
├── context.md
├── requirements.md
├── rounds/round-1/reviews/*.md
├── rounds/round-1/discourse.md
├── rounds/round-1/final.md
└── maps/run-1/
    ├── map.md
    └── flow-analysis.md
```

These files are a natural fit for Obsidian, including the Mermaid flow analysis. OCR gitignores sessions by default, so the review notes remain local unless that policy is deliberately changed. Source: [Session Storage](https://github.com/spencermarx/open-code-review#session-storage).

The simplest arrangement is to open the CrazyMarket repository as an Obsidian vault and navigate to `.ocr/sessions`. Because `.ocr` is a hidden directory, Obsidian/macOS visibility may need adjustment. OCR's own dashboard is likely better for live progress and finding-status triage; Obsidian is useful for reading, cross-linking, and discussing the durable Markdown output.

## Recommendation

Use the GitHub stack CLI only to maintain local branch ordering initially; do not run its push or submit commands. Use Spencer Marx's OCR from the existing Codex subscription to review `parent..child` locally, and use OCR's dashboard plus Obsidian for the output. Install only after preserving the current dirty worktree and reviewing exactly what `ocr init` adds to the repository.
