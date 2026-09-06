# CrazyMarket Systems Test Campus

Open `TestCampus_Core`, enter Play Mode, and allow the six specialist scenes to load additively.

- **F1** toggles the campus panel.
- **F2** resets the current zone.
- **F3** returns the player to the hub.
- The panel teleports to zones, resets the campus, and applies load presets.
- Use **CrazyMarket > Test Campus > Build All Scenes** to regenerate the graybox scenes.
- Use **CrazyMarket > Test Campus > Validate** to check scene ownership and zone identifiers.

The campus is development content. Production prefabs are used where available, and test adapters must not duplicate gameplay rules.

## Market playground

Open `Scenes/TestCampus_Market_PlayerV2.unity` directly for the standalone market lab. It uses the current V2 player, free camera, and player shadow with no shift timer. Keys **1–4** select aisles, movement, physics, and the open ability pad. **F2** or controller **Select** resets the player and crates. This scene is authored separately from Build All Scenes; new controller and ability experiments can start here without changing the tutorial.
