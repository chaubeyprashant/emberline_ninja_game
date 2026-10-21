# Feel pass — playtest script

Roadmap step 2 of the AAA benchmark report. Nobody has yet judged on a phone
whether the parry window, dodge timing, hit-stop or camera feel right; the bot
proves flow, not feel. This is the script for one person, two phones, about a
week of short sessions.

## Setup

- **Phones:** the Galaxy A33 (reference, 30 fps on tier 1) and one high-end
  phone at 60 fps (Settings → frame rate). Feel changes with frame rate: a
  0.16 s window is 5 frames at 30 fps and 10 at 60.
- **Build:** `./perf_run.sh --build` makes `Builds/emberline-dev.apk`; install
  it with `adb install -r`. Development builds only show the **FEEL** button
  (bottom centre). Release builds compile the dials to their defaults.
- **The dials** apply on the next tap, mid-fight, and persist across launches.
  A changed dial shows `*`. RESET returns all six to default.

| Dial | Scales | Default today |
| --- | --- | --- |
| Parry window | Perfect-parry opening of a guard (`CombatController.perfectParryWindow`) | 0.16 s |
| Dodge i-frames | Invulnerability of a flicker, not its distance (`PlayerLocomotion.flickerDuration`) | 0.28 s (warp 0.17 s) |
| Hit-stop | Every hit-stop dip (`CombatController.RunHitStop`) | 0.04–0.07 s at 5 % time |
| Camera distance | Distance behind Renzo (`CameraRig.backDistance`) | 3.0–3.8 m by framing preset |
| Camera FOV | Added to the field of view, in degrees | 56° |
| Camera follow | How tightly the camera chases (`CameraRig.followLerp`) | 12 |

## Sessions

Play each at default first, then move one dial at a time. Write down what
changed and why, in a sentence, before touching the next.

1. **Parry** — Duels, first opponent. Hold block into 20 swings. Count perfect
   parries you meant to land against ones you got. Fewer than about 1 in 2
   intended means the window is too tight on that phone; perfects you did
   not try for mean it is too loose.
2. **Dodge** — Story mission 1, then a ranged enemy. Dodge through 20 attacks
   at the last moment. A hit you saw yourself dodge through is the failure to
   count. A dodge that clips should feel like your fault, never the game's.
3. **Hit-stop** — any fight with three or more enemies. Light chains, heavies,
   a crush, an execution. Does each hit land with weight, and does a chain of
   lights stay readable or turn sticky? Try 0.75x and 1.5x.
4. **Camera** — explore mode (THE VALLEY), village to stronghold, then an
   eight-enemy fight. Any moment you lost Renzo, lost a threat off-screen,
   felt sick, or fought the camera is a note. Distance and FOV first, follow
   last.
5. **Both phones** — repeat 1 and 2 on the other phone with the values you
   settled on. If the phones disagree, the windows should be frame-based, not
   second-based; note it rather than averaging.

## Screens to open on hardware

Never seen on a phone before this pass. A text overflow, a button under a
notch or a tap that does nothing is a bug for `QA_CHECKLIST.md`.

- [ ] Loading between menu and a mission (and between missions)
- [ ] Mission complete: every button, rank and stars visible
- [ ] Game over: SECOND WIND (AD), RISE AGAIN, MENU
- [ ] Both on a 20:9 phone and anything with a notch or punch-hole

## Reporting back

Tap **LOG**. The panel shows, and logcat gets, one line:

```
[FEEL] parry=1.2 iframes=1 hitstop=1.25 camdist=0.95 camfov=2 camfollow=1
```

Send that line with your notes. The values are then baked into the authored
fields, the dials go back to 1, and the next build ships the new feel.
