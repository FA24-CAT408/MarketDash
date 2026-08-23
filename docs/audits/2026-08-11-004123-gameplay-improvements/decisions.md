# Observed and proposed decisions

## Observed

- The campaign is ordered by Unity Build Settings and advances by build index. **Confirmed.**
- Orders reference exact scene item instances, not item-type IDs or quantities. **Confirmed.**
- Collection is automatic on player trigger contact. **Confirmed.**
- The timer continues during `EndGame` until the player reaches checkout. **Confirmed.**
- Production levels use the legacy KCC controller; Player V2 is a separate vertical slice. **Confirmed.**
- Latest and best completion are stored separately, although consumers conflate them. **Confirmed.**

## Proposed

- Keep the core fantasy as an arcade grocery speedrun, not a realistic shopping simulator.
- Make level identity scene/data-driven and results immutable.
- Migrate V2 through one complete production level before adding more abilities.
- Make randomization deterministic and seed-visible so mastery remains fair.
- Treat assistance as optional layers: readable orders and target selection without forcing a single route.
