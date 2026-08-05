# Test Campus camera system audit

Status: complete for the scoped Test Campus camera subsystem.

The rendered `Main Camera` is a normal Unity Camera driven by a Cinemachine Brain. Two
Cinemachine cameras feed that Brain: an assisted orbit camera and a guided spline-rail camera.
The assisted camera uses Cinemachine's Decollider, the custom Ground Guard extension, and the
custom Surface Constraint. The rail camera has no collision policy.

The reported unwanted movement is confirmed. The assisted camera's Decollider, Ground Guard,
and Surface Constraint are authored with all-layer masks. Moving platforms, props, and other
ordinary colliders can therefore push, shorten, lift, or pitch-limit the camera even when they
were not intended as camera blockers.

The bounded remediation is to introduce explicit camera-obstacle and camera-surface policies,
exclude dynamic gameplay fixtures, regenerate the two affected scenes, and validate both orbit
and rail modes in Play Mode.
