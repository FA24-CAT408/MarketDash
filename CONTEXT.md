# CrazyMarket

CrazyMarket is a player-driven market game whose movement and abilities can be
tuned into distinct playable styles.

## Language

**Player Profile**:
A named definition of a player's movement and world-interaction tuning. It
excludes ability composition, visual choices, and presentation choices.
_Avoid_: Theme, preset

**Ability Composition**:
The collection of enabled Player Ability components on a player prefab. The
prefab owns this composition independently of its Player Profile.
_Avoid_: Profile loadout, skill set

**Ability**:
A capability granted by an enabled Player Ability component. It returns a
typed result to locomotion policy but never moves the player body directly.
_Avoid_: Locomotion state, skill

**Locomotion Mode**:
The single active movement policy that has final authority over how the player
body moves.
_Avoid_: Locomotive mode, ability

**Player Intent**:
The player's current movement direction and action requests, expressed without
directly controlling the player body.
_Avoid_: Raw input, motor command

**Locomotion Output**:
The authoritative movement result produced by the active Locomotion Mode for
the player body to apply.
_Avoid_: Player Intent, raw motor mutation

**Player Snapshot**:
A read-only description of the player after a completed movement step,
including its movement, Locomotion Mode, resolved tuning, and presentation.
_Avoid_: Mutable player state, controller reference

**Control Block**:
A source-owned reason that prevents the player from acting. The player can act
only when no Control Blocks remain.
_Avoid_: Shared enabled flag, toggle

**Player Modifier**:
A named runtime adjustment applied to Player Profile values without changing
the profile itself.
_Avoid_: Direct stat mutation, profile edit

**Player Presentation State**:
A semantic, read-only description of player gameplay that presentation uses to
choose visuals and animation. It contains no Animator parameter names.
_Avoid_: Animator state, animation booleans

**Main Menu Market Vignette**:
The production 3D supermarket environment visible behind the Main Menu UI,
including its composition, lighting, post-processing, props, and ambient activity.
It excludes UI controls, menu layout, and general-purpose NPC behavior.
_Avoid_: Background, setting behind the UI, menu scene
