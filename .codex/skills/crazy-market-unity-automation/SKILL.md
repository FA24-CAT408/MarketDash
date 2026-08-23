---
name: crazy-market-unity-automation
description: Use for tasks that require running, controlling, or validating the CrazyMarket Unity Editor.
---

# CrazyMarket Unity Automation

Use the narrowest reliable Unity control path and leave the user's editor in a
known state.

## Establish control

1. Read `git status`, identify the affected scene or prefab, and inspect the
   open Unity process and active-scene title before changing Unity state.
2. Prefer product-native Unity MCP or Unity CLI. Inspect the server's advertised
   tools rather than assuming a running relay is usable. An empty tool list is
   unavailable control, even when the MCP process launches successfully.
3. Prefer a scoped Unity Editor API action when MCP/CLI cannot perform the
   operation. Use keyboard input only after the available Unity tooling has
   been checked and shown insufficient.
4. Launch agent-owned unattended editor instances with `-automated`. The flag
   is startup-only; do not restart or close the user's open editor merely to
   add it.

## Protect the open editor

- Treat an asterisk in the Unity title as unsaved active-scene state. Establish
  ownership before saving, reloading, entering Play Mode, or applying a scene
  rewrite.
- Preserve user-authored dirty state. Save automatically only when the changes
  are known to belong to the active task.
- Do not assume a shortcut succeeded across compilation or domain reload.
  Verify the resulting state through Unity tooling, serialized output, or a
  fresh log segment.

### Keyboard fallback

Before every shortcut or key sequence:

1. Resolve the CrazyMarket Unity process by its non-empty window title.
2. activate that process;
3. wait briefly for focus transfer;
4. verify `GetForegroundWindow` belongs to the same Unity process;
5. send no input when verification fails.

Prefer one focused shortcut over coordinate clicks. Wait for compilation or
Play Mode transitions to settle before sending the next shortcut.

## Temporary Editor automation

For a one-time scene, prefab, Console, or regeneration operation:

- place the temporary script in an existing `Editor` folder;
- make it idempotent with an exact asset/scene guard and a `SessionState` key;
- tag diagnostic messages with one searchable prefix;
- preserve world-space transforms and serialized component settings;
- save only task-owned assets;
- remove both the temporary script and its `.meta` after the result is
  serialized;
- refresh/recompile once more and confirm no temporary type or diagnostic tag
  remains in the repository.

Prefer Unity's Console API/tool for clearing test history. Console history and
`Editor.log` are runtime evidence, not source instrumentation; remove tagged
source logs and temporary helpers rather than deleting diagnostic history.

## Generated-content ownership

Find the authoritative input before editing generated children. For nested
generated content, regenerate in ownership order:

1. source model or prefab;
2. prefab that embeds or instantiates it;
3. production prefab that owns the generator;
4. affected production scene and any baked scene cache.

Unity spline `Clear`/`UpdateInstances` may leave baked scene-owned children.
After regeneration, inspect the serialized scene for the original defect and
migrate stale baked children only when the authoritative sources are already
clean. Re-run generation once to check repeatability and inspect the diff size
and content before accepting it.

## Validation

Build a red/green signal for the exact symptom. For Console warnings, capture a
baseline count immediately before entering the affected production scene, let
Play Mode stabilize, and compare the count afterward. A historical warning in
`Editor.log` is not a new failure; a zero delta is meaningful only when the run
was confirmed to occur.

For Unity-facing changes, also follow the project validation requirements:
compile, exercise the changed production path and a boundary/reset path,
inspect the Console and Game view, and visually compare presentation changes.

Completion requires:

- Unity is not unintentionally left in Play Mode;
- the active scene's dirty/saved state is known;
- the exact repro is green;
- generated output is owned and repeatable when applicable;
- temporary scripts, meta files, and diagnostic source logs are gone;
- validation is reported as `verified`, `partially verified`, or `unverified`.
