# Emberline — Instagram Reel

`emberline_reel_v1.mp4` — 1080×1920, 60 fps, 15.00 s, H.264 High + AAC 48 kHz,
23 MB, faststart. Audio measures −14.7 LUFS integrated / −1.1 dBTP, which is
what Instagram normalises to, so it will not be turned down on upload.

Everything on screen is the current build rendering itself. No footage was
staged in a DCC tool, no effect or UI was added that the game does not have,
and no character, environment or mechanic appears that is not in the game.

## The cut

| Time | Beat | What it is |
|---|---|---|
| 0.0–1.5 | `THIS IS A 3D NINJA GAME.` | Mission 53, the burning village. Renzo trading with a swordsman — sword arc, sparks, close third person. |
| 1.5–4.0 | `MASTER THE BLADE.` | Mission 23, the cold mountain deck. Two cuts of close blade work, the second ending on an execution. |
| 4.0–6.5 | `TIMING MATTERS.` | Mission 44, the marsh. One unbroken take: a perfect dodge, the duel it buys, the execution it ends in. The slow motion is the game's own — `OnPerfectDodge` drops `Time.timeScale`, and the capture records that faithfully. |
| 6.5–9.5 | `EVERY ENEMY FIGHTS DIFFERENT.` | Mission 40, the mountain fortress. Goro — the heaviest body in the cast — winding up, telegraph rings on the deck, Renzo reading them. |
| 9.5–12.5 | — | Mission 61, rain over the village. Jin Kurogane, his after-image clones, and his defeat, ramped to roughly 0.6× so the death burst lands on the cut. |
| 12.5–15.0 | `EMBERLINE` / `3D NINJA ACTION` / `FOLLOW FOR MORE` | Mission 53 again with the field cleared: Renzo walking out under the banner, slowed and pushed in slightly. |

Cuts fall on 1.5, 2.75, 4.0, 6.5, 8.0, 9.5, 11.0 and 12.5 s — every one of them
a beat at 120 BPM, which is the tempo the score is written at.

Titles sit between 18 % and 42 % of frame height: clear of Instagram's top
chrome, well clear of the caption and action rail at the bottom, and above the
fighters in every shot they play over. Type is the game's own — Rajdhani Bold
for the cards, Shojumaru for the logo.

## Audio

- **Music is original and synthesised from scratch** (`tools/audio.py`): taiko-style
  membrane hits, a low drone, a noise riser and a gong, written against the same
  120 BPM grid the cut uses. There is no licensed music in this file, so it
  cannot be muted or claimed.
- **Effects are the game's own shipping `.ogg` files** from `Assets/Resources/Art/Audio/SFX`,
  mixed at the frames where the capture logged the event: every blade hit, parry,
  dodge and kill you hear is on the frame the game actually resolved it. The
  boss's death is the loudest thing in the mix.

## What was left out, and why

Two things the game draws are suppressed during capture:

- **The touch HUD.** It is a screen-space overlay, so a camera render never sees
  it; the footage is clean by construction rather than by removal.
- **The wide floating banners** ("GUARD BROKEN"). They are authored for a
  1600×720 landscape frame and span the whole picture at portrait scale, where
  they read as an overlay bug. Damage numbers stay — they are three characters
  wide, and they are the readout that says this is a game.

Telegraph rings, hit sparks, after-images, the EXECUTE prompt, weather and the
engine's own cinematic grade are all still there.

## Caption

```
3D ninja combat. Every fight is a test of timing.
Master the blade. Read your enemy. Survive the battle.
Welcome to Emberline.
#Emberline #3DNinja #NinjaGame #3DGame #ActionGame #SwordFighting #SamuraiGame #MobileGaming #AndroidGaming #IndieGame #UnityGame #BossFight
```

## Rebuilding it

The game ships landscape — the HUD is authored at 1600×720 — so a 9:16 reel
cannot be a crop of the shipping frame; there is not enough picture above and
below the fight. `ReelDirector` instead has the real game camera drive a second
camera that renders the same scene into a 1080×1920 target from the same eye,
aimed at the duel. Time is stepped by `Time.captureDeltaTime`, so a frame that
takes 200 ms to read back and encode still advances the game by exactly 1/60 s —
and because Unity scales that step by `Time.timeScale`, the game's own hit-stop
and slow motion are recorded as real slow motion.

```bash
# 1. capture ~90 s of play across six missions into Logs/reel/ (about 12 min)
/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$PWD" \
  -executeMethod Emberline.EditorTools.EmberReelCapture.Run \
  -logFile Logs/reel.log
```

```bash
# 2. look through a shot before cutting it
python3 Marketing/Reel/tools/review.py Logs/reel/04_boss_jin
python3 Marketing/Reel/tools/review.py Logs/reel/04_boss_jin 430 525 8 /tmp/strip.png
```

```bash
# 3. cut, score and encode (needs numpy, pillow and ffmpeg)
python3 Marketing/Reel/tools/build.py Marketing/Reel/edl.json Marketing/Reel/emberline_reel_v1.mp4
```

`edl.json` is the whole edit: which shot each cut comes from, its source frame,
its speed ramps, its grade, and every title. Changing a clip is changing one
number in it. `-reelShot N` and `-reelSeconds S` on the capture command re-shoot
a single shot, which is how the framing was dialled in.
