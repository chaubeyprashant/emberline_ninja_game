# Emberline — first-launch intro

`emberline_intro_v1.mp4` — 1920×1080, 30 fps, 25.00 s, H.264 High + AAC 48 kHz,
39 MB, the master. `Assets/StreamingAssets/intro.mp4` is the copy that ships in the
APK: 1280×720, H.264 Main (every Android hardware decoder), AAC 44.1 kHz, 6.0 MB —
under the 8 MB budget the story check enforces. Audio measures −14.8 LUFS integrated /
−1.0 dBTP.

It plays once, the first time the app is opened, before the opening cinematic
(`Assets/Scripts/Story/IntroVideo.cs`, called from `StoryRunner`). A tap after
the first second skips it. Either way it never plays again — its own
PlayerPrefs flag, separate from the opening's, so quitting during the
cinematic replays the cinematic and not the video.

Everything on screen is the current build rendering itself: the same captured
takes under `Logs/reel/` the Instagram reel was cut from, on the current cast.
Nothing is staged, generated or added that the game does not have.

## The cut

Cuts fall on the 120 BPM grid the score is written at; the fast section cuts
on eighth notes.

| Time | Card | What it is |
|---|---|---|
| 0.0–3.0 | `A NEW NINJA RISES` | Mission 53, the burning village. The clash on frame one, sparks on the first beat; then Renzo finishing a swordsman against the barrels. |
| 3.0–7.0 | `MASTER THE BLADE` | Mission 23, the cold mountain deck: two cuts of close blade work ending in executions, then the marsh — a perfect dodge into the game's own slow motion and the kill it buys. |
| 7.0–12.0 | `FIGHT.  ADAPT.  SURVIVE.` | Seven cuts, one every 0.75 s: a dodge, an execution, Goro's slam on the telegraph ring, Jin's after-images, two more kills. Each cut carries a drum. |
| 12.0–17.0 | `FACE DEADLY WARRIORS` | Goro — the heaviest body in the cast — winding up on the red ring, his combo, then Jin Kurogane and his clones in the rain. |
| 17.0–22.0 | — | Goro goes down at full speed; Jin's last exchange ramps to 0.35× — the parry at 20.1 s, his defeat on the release at 21.0 s. The only slow motion in the cut. |
| 22.0–25.0 | `EMBERLINE` / `3D NINJA ACTION` | Mission 53 with the field cleared: Renzo walking out under the banner, slowed and pushed in, fading to black. |

## Framing

The captures are portrait (the reel needed 9:16). A 16:9 frame is the middle
band of each portrait plate, centred on the fighters (`cy` per clip in
`edl.json`) and resampled to 1080p. A quiet vignette darkens the corners. No
letterbox, no anime effects, no stickers; the titles are the game's own faces —
Rajdhani Bold for the cards, Shojumaru for the logo.

## Audio

Same construction as the reel: an original synthesised score (`Reel/tools/audio.py`
voices — taiko, drone, riser, gong; no licensed music, nothing to claim) with a
new 25-second arrangement in `tools/build_intro.py`, plus the game's own shipping
`.ogg` effects placed on the frames where the capture logged each hit, parry,
dodge and kill. The riser runs 17–21 s into the release on Jin's defeat; the gong
lands on the cut to the end card.

## Rebuilding it

```bash
# needs numpy + pillow + ffmpeg; the frames come from the reel capture (see Marketing/Reel/README.md)
python3 Marketing/Intro/tools/build_intro.py Marketing/Intro/edl.json \
  Marketing/Intro/emberline_intro_v1.mp4 Assets/StreamingAssets/intro.mp4
```

`edl.json` is the whole edit: shot, source frame, crop centre, speed ramps,
zoom, grade, titles. Changing a clip is changing one number in it.

To replace the footage with a different source video, cut it to the same
25-second structure and write the 720p copy to `Assets/StreamingAssets/intro.mp4`;
the player reads that path and nothing else. Keep it H.264 Main, `yuv420p`,
`+faststart`, and under 8 MB — `Emberline/Check Story Framework` fails otherwise.
