# CrazyMarket gameplay-improvement audit

Status: **incomplete but evidence-bearing** at revision `b34383ee55fb75cb7f3db812a5880478a5586a22`.

CrazyMarket currently plays as a four-level, movement-heavy grocery time trial. The player crosses a start boundary, collects a small serialized order by collision, returns to staging, records a time, and advances. The strongest product direction is to deepen route planning and expressive traversal, but the current campaign loop has correctness problems that should be fixed before adding content.

## Highest priorities

1. Preserve order state across pause/resume; the current state re-entry can make a level impossible.
2. Replace the mutable campaign counter with a stable completed-level result; restart, display IDs, and best-time queries currently drift.
3. Adopt Player V2 in one production vertical slice, including lifecycle, camera, hazard, and respawn contracts.
4. Repair stale serialized settings/camera bindings and validate Main Menu to Level 1 startup.
5. Make collection feel excellent: target signposting, pickup SFX/VFX, list motion, route feedback, and distinct checkout payoff.

## Product thesis

Build around **speedrun route planning in a chaotic supermarket**: readable orders, several viable paths, expressive movement abilities, dynamic-but-seeded shelf layouts, shopper/hazard traffic, medal targets, ghosts, and concise replay loops.

See [findings.md](findings.md) for the ranked backlog, [traces.md](traces.md) for current behavior, and [graph.html](graph.html) for the connected map.

## Verification

The connected Unity 6000.4.1f1 Editor was ready, idle, and not compiling. Static source/serialization claims were coordinator-checked. Production Play Mode was not run, so runtime-sensitive findings remain strongly supported rather than confirmed by execution.
