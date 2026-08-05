# Runtime traces

## Assisted orbit render

Player input is consumed by `TestCampusCameraInputFocus`. The prototype controller writes the
Orbital Follow yaw and pitch axes. Orbital Follow computes the body position; Decollider applies
Body-stage obstacle correction; Ground Guard applies final vertical Body-stage correction; the
Rotation Composer re-aims during Aim; the Brain sends the selected result to Main Camera.

## Unwanted moving-platform reaction

The moving-platform collider is on Ground layer 3. Decollider's obstacle mask is all layers, so
its target-to-camera overlap capsule includes the platform. Surface Probe also includes Ground
and Default while excluding only Ignore Raycast and the two Player layers. Consequently a moving
platform or ordinary prop beneath/intersecting the camera path can change Decollider displacement,
orbit radius, pitch limits, and final camera Y every frame.

Separately, the controller derives movement direction from raw player transform displacement.
Motion inherited from a support platform can therefore steer automatic recentering even after the
collision masks are corrected.

## Camera mode switch

F6/F7/F8 or the UI changes the controller mode. Assisted orbit receives priority 30 normally;
guided rail receives 30 in Guided Rail mode or inside the Hybrid trigger. Player warps are
forwarded to both Cinemachine cameras so damping history follows zone travel.
