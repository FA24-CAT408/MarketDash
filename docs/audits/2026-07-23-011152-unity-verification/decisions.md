# Verification decisions

## Prefer the connected Editor over batch duplication

Status: observed. Confidence: confirmed.

The official CLI discovered the already-running CrazyMarket Editor in a ready state. Verification therefore used its live `recompile`, console, and test commands instead of launching a second Editor or build process.

## Preserve the original audit snapshot

Status: audit-policy decision. Confidence: confirmed.

The original full audit remains immutable. This verification snapshot supersedes its CLI-unavailable limitation without rewriting historical evidence.
