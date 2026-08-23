# Glossary

- **Shopping run:** the timed `InProgress` phase from entrance trigger to checkout completion.
- **Order:** the exact serialized `Item` instances owned by `GroceryListManager`.
- **Staging area / checkout:** the EndGame destination that reveals collected item prefabs and results UI.
- **Completed-run result:** proposed immutable value carrying the level identity, final time, best-time outcome, and next destination.
- **Player V2:** profile-driven KCC controller with abilities, modifiers, control blocks, snapshots, and motor-safe teleport.
- **Player profile:** named locomotion tuning copied into a runtime profile.
- **Ability composition:** enabled `PlayerAbilityComponent` instances on the player prefab.
- **Seeded run:** proposed deterministic order/layout/NPC configuration suitable for fair retries and ghosts.
