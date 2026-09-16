# Traversal — how Renzo moves (Phase 2)

Phase 2 of `docs/OPEN_WORLD_ROADMAP.md`. Everything here lives in
`Assets/Scripts/Player/` and is exercised by `Emberline/Check Traversal`
(`EmberTraversalCheck`, edit mode, no play mode needed).

## The motor

`PlayerLocomotion` smooths horizontal velocity instead of setting it: 40 m/s²
on the ground (0 to a full run in about 0.16 s), 55 m/s² braking, 12 m/s² in
the air. Facing turns at 720°/s as before. Grounded velocity is projected onto
the slope under the feet (one sphere cast a frame), the feet are held down at
6 m/s so a slope is followed instead of hopped off, and anything past the
controller's 50° slope limit slides.

| Gait | Speed | Trigger | Noise radius |
|---|---|---|---|
| Stealth walk | 2.9 m/s | crouch held | 1.6 m |
| Walk | 2.6 m/s, scaled by the stick inside the band | stick under 0.5 | 5 m |
| Run | 6.5 m/s | stick past 0.5 | 9 m |
| Sprint | 8.6 m/s | stick pushed through the rim (≥ 0.95) for 0.25 s, or Left Ctrl in the editor | 12 m |
| Swim | 2.4 m/s | chest-deep in the zone's river | 3.2 m |

Crouch shrinks the controller to 1.25 m and only stands back up with headroom.
A scripted bot never sprints: its input vector is unit length and would trip
the push test on every stage, so sprint needs a real stick or the key.

## Traversal read off colliders

`TraversalProbe.Scan` replaces the marker-circle vault. A capsule sweep in the
move direction finds a face; a ray from above finds its top; a capsule check
proves there is room to stand (crouched if it must). The height of the top above
the feet picks the move:

| Height above feet | Grounded | Airborne |
|---|---|---|
| under 0.45 m | the controller steps it | landing |
| 0.45–1.35 m, thin, ≥ 0.5 m drop beyond | **Vault** over it, 0.5 s | — |
| 0.45–2.45 m | **Mantle** onto it, 0.6 s | — |
| 2.45–3.6 m | **Grab**: a taller jump, then the air grab catches the lip | **Grab** if the lip is 0.5–2.35 m above the feet |
| over 3.6 m | nothing (wall-run only) | nothing |

The probe runs ten times a second and its answer is `PlayerLocomotion.Hint`. The
HUD's JUMP button reads it: the glyph becomes VAULT or CLIMB and the button turns
ember when a move is available, and the same press performs it. Airborne past
the apex, the motor scans on its own toward the stick, the wall it is running
along, or its facing, so a wall-run or a short jump ends on the roof.

**Hang**: hands on the lip, feet 1.55 m below. Push into the wall or press jump
to pull up (the mantle path); crouch or pull away to drop; Flicker lets go into
a dodge. Twelve seconds is the longest hang.

**Slide**: crouch while sprinting. 0.65 s, sprint speed decaying to a walk, low
profile the whole way.

**Landing**: a fall faster than 13 m/s (about 3.4 m) either rolls — moving,
quiet (4 m), keeps the speed — or plants the feet hard for 0.35 s and is heard
at 12 m. Shorter falls land normally.

**Wall-run** is the same one-second run along a vertical face, now on a layer
mask (no water plane, no UI, no ignore-raycast) and eligible for an air grab
at its end.

Every move sets `Traversing`; `CombatController` refuses a strike, cleave or
kunai while it is true, and `TryWarpTo` refuses too.

## Camera

`CameraRig` keeps its occlusion, impact and shake model and adds: a real pitch
band (−22° to +55° around the preset's rest tilt) that stays where the finger
put it (only the gyro trim springs back); yaw drifting back behind the run
direction after 1.6 s without a drag and never under lock; a velocity lead on
the pivot (0.12 s, at most 0.9 m); profiles that lerp from what Renzo is doing —
sprint pulls back 0.5 m and widens 4°, crouch and swim drop 0.55 m and close in
0.45 m, an empty field (no enemy within 14 m, no lock, no boss) gives 0.35 m
more room; a terrain floor 0.45 m above the meadow; and a smoothed aim point so
a step or a shake never snaps the horizon. Minimum distance dropped from 2.6 to
1.4 m so a doorway pulls the camera in rather than through the wall.

## Controls

The control layer sits inside `Screen.safeArea` (`UI/SafeArea`). The stick has
a 0.12 radial dead zone remapped so the rim is still 1.0. Held inputs are reset
on every scene load (`EmberInput.ResetHeld`), which closes the latched-crouch
bug from the audit.

## Animation

`EmberCharacterFactory` builds the player's controller with a four-gait tree on
`Move` (idle 0, walk 0.4, run 0.75, sprint 1), a crouch tree and a swim tree,
and the IK pass on; `SkeletalRig.SetLocoMode` crossfades between them, and
`PlayerFootIk` plants the feet on tier 1 and up. Forced AI poses now crossfade
on a pose change instead of cutting.

The Mixamo pack ships no traversal takes, so every new pose has a fallback
(`PoseFallbacks`): a borrowed run cycle is re-timed for walk and sprint, a
backstep stands in for the roll and slide, the jump for mantle, vault and fall,
the block for the hang. **To replace them, download these from Mixamo on the
Ninja body, name the files as below and drop them into
`Assets/Art/Characters/Mixamo/Anims/`; the next `SetupScenes` picks them up.**

| File | Mixamo take |
|---|---|
| `Walk.fbx` | Walking |
| `Sprint.fbx` | Running (the fast one) |
| `CrouchIdle.fbx` | Crouch Idle |
| `CrouchWalk.fbx` | Crouched Walking |
| `Roll.fbx` | Stand To Roll |
| `Mantle.fbx` | Braced Hang To Crouch |
| `Vault.fbx` | Running Jump (low) |
| `Slide.fbx` | Running Slide |
| `Hang.fbx` | Hanging Idle |
| `Fall.fbx` | Falling Idle |
| `Swim.fbx` | Swimming |
| `SwimIdle.fbx` | Treading Water |
| `Land.fbx` | Hard Landing |

Export with "In Place" ticked where offered (the motor owns displacement) and
"Without Skin".

## What is not in this slice

Rope and zip lines, climbing walls taller than 3.6 m, a control-layout editor,
turn-in-place animations, hand IK on ledges. All are listed in the roadmap.
