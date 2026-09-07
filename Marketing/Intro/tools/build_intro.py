#!/usr/bin/env python3
"""Cuts the captured gameplay into the 16:9 first-launch intro.

Same idea as the reel's build.py, turned on its side: the source is the portrait
frame sequences under Logs/reel/<shot>/ plus the events.csv the capture wrote
beside each one. Each output frame is a 16:9 band cropped out of one captured
frame — the fighters sit in the middle of the portrait picture, so a band
centred on them is the shot a landscape camera would have made — and then:

  1. output frames map to source frames through the clip's speed ramps, so a
     clip can run at speed or slow into a kill without inventing a frame;
  2. the gameplay events under each clip are lifted into the intro's timeline,
     so the sound pass lands a blade hit on the frame the blade actually landed;
  3. the title cards are burned in, with a soft vignette and the fades;
  4. ffmpeg encodes the 1080p master and the 720p copy that ships in the APK.

    python3 build_intro.py edl.json master_1080p.mp4 [app_720p.mp4]

Needs numpy, pillow and ffmpeg. The score and the effects layer come from the
reel's audio.py: the same synthesised taiko/drone voices (no licensed music) and
the game's own shipping .ogg effects, placed on the cut's 120 BPM grid.
"""
import json
import os
import shutil
import subprocess
import sys
import tempfile

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageEnhance, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "Marketing", "Reel", "tools"))
import audio as A  # noqa: E402  (the reel's voices, SFX layer and mastering)

W, H, FPS = 1920, 1080, 30
SRC_W, SRC_H, SRC_FPS = 1080, 1920, 60
STEP = SRC_FPS / FPS                  # source frames per output frame at 1x
BAND = SRC_W * H / W                  # source rows that make one 16:9 frame

FONT_DIR = os.path.join(ROOT, "Assets", "Resources", "Art", "Fonts")
FONTS = {"HEADING": os.path.join(FONT_DIR, "Rajdhani-Bold.ttf"),
         "DISPLAY": os.path.join(FONT_DIR, "Shojumaru-Regular.ttf")}


# ------------------------------------------------------------------- titles

def tracked_text(draw, xy, text, font, fill, tracking):
    widths = [draw.textlength(c, font=font) for c in text]
    total = sum(widths) + tracking * (len(text) - 1)
    x, y = xy[0] - total / 2, xy[1]
    for c, w in zip(text, widths):
        draw.text((x, y), c, font=font, fill=fill)
        x += w + tracking


def card(lines):
    """One title card as an RGBA layer: white type, soft shadow, tight edge."""
    def pass_(dy, fill, blur):
        layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        d = ImageDraw.Draw(layer)
        for line in lines:
            font = ImageFont.truetype(FONTS[line.get("font", "HEADING")], line["size"])
            y = line["y"] * H - line["size"] * 0.62 + dy
            tracked_text(d, (W / 2, y), line["text"], font, fill,
                         line.get("tracking", line["size"] * 0.06))
        return layer.filter(ImageFilter.GaussianBlur(blur)) if blur else layer

    soft = pass_(6, (0, 0, 0, 210), 14)
    tight = pass_(2, (0, 0, 0, 150), 3)
    ink = pass_(0, (255, 255, 255, 255), 0)
    return Image.alpha_composite(Image.alpha_composite(soft, tight), ink)


def title_opacity(t, start, end, rise=0.07, fall=0.12):
    if t < start or t > end:
        return 0.0
    if t < start + rise:
        return (t - start) / rise
    if t > end - fall:
        return max(0.0, (end - t) / fall)
    return 1.0


def vignette():
    """A quiet darkening of the corners. Premium rather than pretty: it pulls
    the eye to the fight and hides that the crop's edges are the least
    interesting part of the portrait plate."""
    yy, xx = np.mgrid[0:H, 0:W]
    nx, ny = (xx - W / 2) / (W / 2), (yy - H / 2) / (H / 2)
    r = np.sqrt(nx * nx + ny * ny) / np.sqrt(2.0)
    dark = np.clip((r - 0.42) / 0.58, 0.0, 1.0) ** 1.7 * 0.46
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 255))
    layer.putalpha(Image.fromarray((dark * 255).astype(np.uint8)))
    return layer


# --------------------------------------------------------------- the timeline

def fr(seconds):
    """Seconds -> output frame, rounding half up. Python's round() is half-to-even,
    which put a cut at 7.75 s on frame 232 and its neighbour on 255: a gap."""
    return int(seconds * FPS + 0.5)


def clip_mapping(clip):
    """Output frame -> source frame, honouring the clip's speed ramps."""
    out_frames = fr(clip["out"]) - fr(clip["in"])
    ramps = clip.get("ramps") or [[0.0, 1.0]]
    src = float(clip["src"])
    mapping = []
    for i in range(out_frames):
        t = i / FPS
        speed = ramps[0][1]
        for (t0, s0), (t1, s1) in zip(ramps, ramps[1:]):
            if t >= t1:
                speed = s1
            elif t >= t0:
                speed = s0 + (s1 - s0) * (t - t0) / max(t1 - t0, 1e-6)
                break
        mapping.append(src)
        src += speed * STEP
    return mapping


def read_events(shot_dir):
    out = {}
    with open(os.path.join(shot_dir, "events.csv")) as fh:
        next(fh)
        for row in fh:
            parts = row.rstrip("\n").split(",")
            if len(parts) >= 11 and parts[10]:
                out[int(parts[0])] = parts[10].split(";")
    return out


def last_frame(shot_dir):
    return max(int(p[1:5]) for p in os.listdir(shot_dir) if p.endswith(".jpg"))


def grade(img, spec):
    if not spec:
        return img
    if spec.get("b", 1.0) != 1.0:
        img = ImageEnhance.Brightness(img).enhance(spec["b"])
    if spec.get("c", 1.0) != 1.0:
        img = ImageEnhance.Contrast(img).enhance(spec["c"])
    if spec.get("s", 1.0) != 1.0:
        img = ImageEnhance.Color(img).enhance(spec["s"])
    return img


def widescreen(img, cy, zoom):
    """The 16:9 band of a portrait frame, centred on cy (fraction of height),
    scaled by zoom about that centre, resampled straight to the output size."""
    bw, bh = SRC_W / zoom, BAND / zoom
    cx, cyp = SRC_W / 2, cy * SRC_H
    top = min(max(cyp - bh / 2, 0.0), SRC_H - bh)
    left = min(max(cx - bw / 2, 0.0), SRC_W - bw)
    return img.resize((W, H), Image.LANCZOS, box=(left, top, left + bw, top + bh))


CUE_FOR = {"execute": ("finisher", 1.0), "bossdown": ("finisher", 1.0),
           "kill": ("kill", 0.85), "parry": ("parry", 0.9),
           "hit": ("hit", 0.7), "dodge": ("dodge", 0.5)}


def build(edl, workdir):
    total = int(round(edl["duration"] * FPS))
    cards = [(t, card(t["lines"])) for t in edl["titles"]]
    vig = vignette()
    black = Image.new("RGB", (W, H), (0, 0, 0))
    fade_in, fade_out = edl.get("fade_in", 0.0), edl.get("fade_out", 0.0)

    cues, plan = [], [None] * total
    for clip in edl["clips"]:
        shot_dir = os.path.join(ROOT, edl["source"], clip["shot"])
        mapping = clip_mapping(clip)
        events = read_events(shot_dir)
        base = fr(clip["in"])
        end = last_frame(shot_dir)
        seen = set()
        for i, src in enumerate(mapping):
            f = min(int(round(src)), end)
            idx = base + i
            if idx >= total:
                break
            zoom = clip.get("zoom")
            z = 1.0 if not zoom else zoom[0] + (zoom[1] - zoom[0]) * i / max(len(mapping) - 1, 1)
            plan[idx] = (shot_dir, f, clip.get("grade"), z, clip.get("cy", 0.6))
            # Every source frame the output frame spans, so a hit that resolved
            # on an odd source frame is not lost to the 60 -> 30 step.
            span = range(int(src), int(round(src + STEP)) + 1) if i else [f]
            for sf in span:
                for name in events.get(sf, []):
                    if name in CUE_FOR and (sf, name) not in seen:
                        seen.add((sf, name))
                        kind, gain = CUE_FOR[name]
                        cues.append([round(idx / FPS, 4), kind, gain])
        for t, kind, gain in clip.get("accents", []):
            cues.append([round(clip["in"] + t, 4), kind, gain])

    for idx in range(total):
        entry = plan[idx]
        if entry is None:
            raise SystemExit(f"gap in the edit at output frame {idx}")
        shot_dir, f, g, z, cy = entry
        frame = Image.open(os.path.join(shot_dir, f"f{f:04d}.jpg")).convert("RGB")
        frame = grade(widescreen(frame, cy, z), g).convert("RGBA")
        frame = Image.alpha_composite(frame, vig)

        t = idx / FPS
        for spec, layer in cards:
            a = title_opacity(t, spec["start"], spec["end"],
                              spec.get("rise", 0.07), spec.get("fall", 0.12))
            if a <= 0.001:
                continue
            lay = layer
            if a < 0.999:
                lay = layer.copy()
                lay.putalpha(lay.getchannel("A").point(lambda v, a=a: int(v * a)))
            frame = Image.alpha_composite(frame, lay)

        frame = frame.convert("RGB")
        k = 1.0
        if fade_in and t < fade_in:
            k = t / fade_in
        if fade_out and t > edl["duration"] - fade_out:
            k = min(k, max(0.0, (edl["duration"] - t) / fade_out))
        if k < 0.999:
            frame = Image.blend(black, frame, k)
        frame.save(os.path.join(workdir, f"o{idx:05d}.jpg"), quality=95, subsampling=0)
        if idx % 90 == 0:
            print(f"  frame {idx}/{total}")
    cues.sort()
    return cues


# -------------------------------------------------------------------- score

def build_score(duration):
    """The intro's own music, written against its six beats at 120 BPM.

    0-3   A NEW NINJA RISES      one statement, then space
    3-7   MASTER THE BLADE       driving eighths, sixteenth ticks underneath
    7-12  FIGHT. ADAPT. SURVIVE. every cut carries a drum
    12-17 FACE DEADLY WARRIORS   half time, a fifth stacked on the drone
    17-22 the build              riser and an accelerating roll into 21.0
    22-25 EMBERLINE              gong, lift, fade
    """
    rng = A.rng
    n = int(duration * A.SR)
    out = np.zeros((n, 2))

    A.place(out, A.drone(duration, 73.42, 0.38, 0.35), 0.0, pan=-0.15)
    A.place(out, A.drone(duration, 73.42, 0.33, 0.30), 0.0, pan=0.15)
    A.place(out, A.drone(5.2, 110.0, 0.22, 0.5), 12.0)     # A2 under the warriors
    A.place(out, A.drone(5.2, 87.31, 0.20, 0.6), 17.0)     # F2 under the build
    A.place(out, A.drone(3.2, 146.83, 0.15, 0.7), 22.0)    # D3 over the end card

    hits = [
        (0.00, "boom", 1.00), (0.00, "taiko_low", 0.85),
        (1.00, "taiko", 0.45), (1.50, "boom", 0.60), (1.50, "taiko_low", 0.80),
        (2.00, "taiko", 0.45), (2.50, "taiko", 0.30),
    ]
    # MASTER THE BLADE: eighths, the cuts underlined.
    for t in np.arange(3.0, 7.0, 0.25):
        t = float(t)
        if t % 1.0 == 0:
            hits.append((t, "taiko_low", 0.75))
        elif t % 0.5 == 0:
            hits.append((t, "taiko", 0.50))
        else:
            hits.append((t, "taiko", 0.26))
    for t in (3.0, 4.5, 5.75):
        hits.append((t, "boom", 0.55))
    # FIGHT. ADAPT. SURVIVE.: a drum on every cut.
    cuts = (7.0, 7.75, 8.5, 9.25, 10.0, 10.75, 11.5)
    for t in np.arange(7.0, 12.0, 0.25):
        t = float(t)
        if t in cuts:
            hits += [(t, "taiko_low", 0.85), (t, "boom", 0.62)]
        elif t % 0.5 == 0:
            hits.append((t, "taiko", 0.42))
        else:
            hits.append((t, "taiko", 0.22))
    # FACE DEADLY WARRIORS: half time, heavier.
    for t in (12.0, 13.5, 15.0):
        hits += [(t, "boom", 0.9), (t, "taiko_low", 1.0)]
    for t in (13.0, 14.5, 16.0):
        hits.append((t, "taiko", 0.45))
    hits.append((16.5, "taiko_low", 0.7))
    # THE BUILD.
    hits += [(17.0, "boom", 0.8), (17.0, "taiko_low", 0.9), (17.5, "taiko", 0.5),
             (18.0, "taiko_low", 0.75), (18.75, "boom", 0.7), (18.75, "taiko_low", 0.85)]
    t, step = 19.0, 0.25
    while t < 20.92:
        hits.append((t, "taiko", 0.35 + 0.85 * (t - 19.0) / 1.92))
        step = max(0.075, step * 0.9)
        t += step
    # RELEASE on Jin's defeat, then the gong on the cut to the end card.
    hits += [(21.0, "boom", 1.0), (21.0, "taiko_low", 1.0), (23.5, "taiko", 0.3)]

    for t, kind, amp in hits:
        if kind == "boom":
            A.place(out, A.boom(amp * 0.72), t)
        elif kind == "taiko_low":
            A.place(out, A.taiko(amp, low=True), t, pan=rng.uniform(-0.1, 0.1))
        else:
            A.place(out, A.taiko(amp * 0.8), t, pan=rng.uniform(-0.35, 0.35))

    t = 3.0
    while t < 12.0:
        strong = abs(t % 0.5) < 1e-6
        loud = t >= 7.0
        A.place(out, A.tick((0.24 if loud else 0.20) if strong else (0.12 if loud else 0.10)),
                t, pan=rng.uniform(-0.5, 0.5))
        t += 0.125

    A.place(out, A.riser(4.0, 0.85), 17.0)
    A.place(out, A.gong(0.5, 98.0, 3.0), 22.0, pan=-0.1)
    A.place(out, A.gong(0.28, 146.83, 2.4), 22.05, pan=0.2)

    tail = np.linspace(1.0, 0.0, int(0.8 * A.SR))
    out[-tail.shape[0]:] *= tail[:, None]
    return out


# ------------------------------------------------------------------- output

def encode(work, wav, master, app):
    subprocess.run([
        "ffmpeg", "-y", "-loglevel", "error",
        "-framerate", str(FPS), "-i", os.path.join(work, "o%05d.jpg"), "-i", wav,
        "-c:v", "libx264", "-profile:v", "high", "-preset", "slow", "-crf", "18",
        "-pix_fmt", "yuv420p", "-r", str(FPS),
        "-c:a", "aac", "-b:a", "192k", "-ar", "48000",
        "-af", "loudnorm=I=-14:TP=-1.5:LRA=11",
        "-movflags", "+faststart", "-shortest", master], check=True)
    if app:
        # The in-app copy: 720p, Main profile (every Android hardware decoder),
        # capped bitrate so 25 s stays well under the size gate.
        subprocess.run([
            "ffmpeg", "-y", "-loglevel", "error", "-i", master,
            "-vf", "scale=1280:720:flags=lanczos",
            "-c:v", "libx264", "-profile:v", "main", "-level", "4.0", "-preset", "slow",
            "-crf", "24", "-maxrate", "1700k", "-bufsize", "3400k", "-pix_fmt", "yuv420p",
            "-c:a", "aac", "-b:a", "128k", "-ar", "44100",
            "-movflags", "+faststart", app], check=True)


def main():
    edl = json.load(open(sys.argv[1]))
    master = sys.argv[2]
    app = sys.argv[3] if len(sys.argv) > 3 else None
    work = tempfile.mkdtemp(prefix="intro_")
    try:
        cues = build(edl, work)
        score = build_score(edl["duration"])
        sfx = A.build_sfx(edl["duration"], cues)
        wav = os.path.join(work, "audio.wav")
        A.write_wav(wav, score * 0.72 + sfx * 0.85)
        encode(work, wav, master, app)
        print(f"wrote {master}" + (f" and {app}" if app else "") + f"  ({len(cues)} sound cues)")
    finally:
        shutil.rmtree(work, ignore_errors=True)


if __name__ == "__main__":
    main()
