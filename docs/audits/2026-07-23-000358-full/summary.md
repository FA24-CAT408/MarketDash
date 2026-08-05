# CrazyMarket codebase trace

Status: **incomplete but evidence-bearing** at revision `4532a1ee8cd8a5a6ed2a320e292bdb847c9848e2`.

CrazyMarket is a Unity 6 game organized around a persistent `GameManager` state machine and scene-local manager prefabs. Input enters through a generated Input System wrapper and a ScriptableObject event adapter. Player locomotion crosses into Kinematic Character Controller; cameras cross into Cinemachine; level completion crosses from item collision through a static event, grocery-list state, serialized state triggers, SOAP persistence, Splines, DOTween, and UI. Scenes and prefabs are executable configuration: several important relationships exist only in serialized YAML.

Three read-only sub-agents traced lifecycle/UI, player/input/camera, and gameplay/persistence/tooling. The coordinator reconciled their cross-boundary handoffs. The strongest risks are:

1. Active interaction input has no active receiver or implementer.
2. Completed-level IDs are incremented before completion UI reads them, producing an offset/wrong-level query.
3. Manager prefabs and scene overrides serialize retired field names while current code dereferences replacement fields.
4. A collectible can reach the grocery-list handler before its dictionary is initialized.
5. Completion uses a hidden `EndGame → GameOver` serialized protocol; the source graph alone cannot guarantee persistence/staging.

The interactive map is [graph.html](graph.html). Durable narratives are in [traces.md](traces.md), and ranked evidence is in [findings.md](findings.md).

## Verification limits

`unity status` returned command-not-found in the available shells despite repository instructions saying the official CLI is installed. No compile, EditMode, PlayMode, or runtime diagnostics were substituted through third-party automation. The report therefore preserves runtime-sensitive conclusions as strongly supported or unresolved rather than confirmed executions.
