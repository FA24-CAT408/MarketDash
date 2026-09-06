# Butter VFX

The Market Player V2 scene uses `Butter VFX.prefab` for presentation and live surface footprints. Its optional `ButterSurfaceMovement` component lets Blobby build bonus speed on existing butter; clean floor smoothly returns him to normal running speed with more grip.

- **ButterSurfaceMovement (Market player):** normal speed 10, normal acceleration 20 m/s² on either surface, clean-floor deceleration sharpness 3, turn sharpness 10. Fresh patches activate after 0.45 seconds; doubling back over a trail builds bonus speed. Contact matches the visible organic shape and its supporting collider; nearly faded patches stop contributing. Air input preserves carried momentum without building bonus speed.
- **Butter player profile:** Stable Move Speed remains the butter cap; Ground Acceleration controls buildup above normal speed independently of that cap. Coast Deceleration Sharpness and Ground Turn Sharpness apply on butter. Releasing movement always coasts.

- **ButterVfx component:** trail spacing/lifetime, pooled-puddle limit, slide spray, and drip attachment sizes/offsets. Each body drip stays on the same mesh through growth, release, falling, and a short squash into a wet patch, without a permanent blob at its source. The pool holds 256 trail patches sized to leave a ribbon roughly as wide as Blobby.
- **Butter Player controller:** scene-only animation copy with slower, speed-driven strides. The V2 presenter's inertial locomotion option blends to idle while coasting without input; the shared production animation controller is unchanged.
- **Butter Puddle material:** liquid colors and reflected cream. The custom URP shader makes irregular silhouettes and continuous world-space gloss; depth clipping keeps patches attached to ramps and ledges.
- **Butter Liquid material:** hanging/falling teardrops.
- **Butter Character Glaze material:** a scene-specific copy of the character's toon material with wet highlights.
- **Reset:** F2 or controller Select clears the trail and airborne droplets. Pause freezes growth, drips, and fading.

`CrazyMarket > Test Campus > Rebuild Butter VFX` regenerates the drop/puddle meshes and prefab defaults, and attaches the rig to the open, stopped Market Player V2 scene. It preserves material edits and saves that scene. Tune prefab defaults in the authoring script if they must survive a rebuild.
