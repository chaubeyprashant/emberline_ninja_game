#!/usr/bin/env python3
"""Cuts the captured gameplay into the 15-second vertical reel.

Input is the frame sequences under Logs/reel/<shot>/ plus the events.csv the
capture wrote beside each one. This script does four things:

  1. maps output frames to source frames, so a clip can hold, ramp or run in
     slow motion without ever inventing a frame that was not rendered;
  2. lifts the gameplay events under each clip into the reel's own timeline, so
     the sound pass lands a blade hit on the frame the blade actually landed;
  3. burns in the title cards;
  4. hands the frames and the mixed audio to ffmpeg.

    python3 build.py edl.json out.mp4
"""
import json
import os
import shutil
import subprocess
import sys
import tempfile

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

W, H, FPS = 1080, 1920, 60
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
FONT_DIR = os.path.join(ROOT, "Assets", "Resources", "Art", "Fonts")
HEADING = os.path.join(FONT_DIR, "Rajdhani-Bold.ttf")
DISPLAY = os.path.join(FONT_DIR, "Shojumaru-Regular.ttf")
# The game's own two faces: Rajdhani sets its headings, Shojumaru its logo.
FONTS = {"HEADING": HEADING, "DISPLAY": DISPLAY}


# ------------------------------------------------------------------- titles

def tracked_text(draw, xy, text, font, fill, tracking, anchor_center=True):
    """PIL has no letter-spacing, and these cards need it to read as a title."""
    widths = [draw.textlength(c, font=font) for c in text]
    total = sum(widths) + tracking * (len(text) - 1)
    x, y = xy
    if anchor_center:
        x -= total / 2
    for c, w in zip(text, widths):
        draw.text((x, y), c, font=font, fill=fill)
        x += w + tracking
    return total


def card(lines):
    """Render one title card to an RGBA layer.

    lines: [{text, size, y, tracking, font, alpha}] with y as a fraction of the
    frame height. White type with a tight dark outline over a soft drop shadow —
    enough separation to stay legible over snow or over firelight without
    putting a box on the picture.
    """
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    shadow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ld, sd = ImageDraw.Draw(layer), ImageDraw.Draw(shadow)
    for line in lines:
        font = ImageFont.truetype(FONTS.get(line.get("font", "HEADING"), HEADING), line["size"])
        y = line["y"] * H - line["size"] * 0.62
        tr = line.get("tracking", line["size"] * 0.06)
        a = line.get("alpha", 255)
        tracked_text(sd, (W / 2, y + 6), line["text"], font, (0, 0, 0, 210), tr)
        tracked_text(ld, (W / 2, y), line["text"], font, (255, 255, 255, a), tr)
    shadow = shadow.filter(ImageFilter.GaussianBlur(14))
    out = Image.alpha_composite(shadow, layer)
    # A second, tighter shadow pass keeps the edges crisp on bright frames.
    tight = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    td = ImageDraw.Draw(tight)
    for line in lines:
        font = ImageFont.truetype(FONTS.get(line.get("font", "HEADING"), HEADING), line["size"])
        y = line["y"] * H - line["size"] * 0.62
        tr = line.get("tracking", line["size"] * 0.06)
        tracked_text(td, (W / 2, y + 2), line["text"], font, (0, 0, 0, 150), tr)
    tight = tight.filter(ImageFilter.GaussianBlur(3))
    return Image.alpha_composite(Image.alpha_composite(shadow, tight), layer)


def title_opacity(t, start, end, rise=0.07, fall=0.12):
    """Titles snap in and leave quickly: they must never outstay the shot."""
    if t < start or t > end:
        return 0.0
    if t < start + rise:
        return (t - start) / rise
    if t > end - fall:
        return max(0.0, (end - t) / fall)
    return 1.0


# --------------------------------------------------------------- the timeline

def clip_mapping(clip):
    """Output frame -> source frame for one clip, honouring its speed ramps.

    ramps is a list of [t, speed] control points in clip-local seconds; speed is
    linearly interpolated between them and integrated, so a ramp down into a
    kill reads as a real deceleration rather than a step.
    """
    out_frames = int(round((clip["out"] - clip["in"]) * FPS))
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
        src += speed
    return mapping


def read_events(shot_dir):
    """frame -> list of event names, from the capture's own log."""
    path = os.path.join(shot_dir, "events.csv")
    out = {}
    with open(path) as fh:
        next(fh)
        for row in fh:
            parts = row.rstrip("\n").split(",")
            if len(parts) < 11 or not parts[10]:
                continue
            out[int(parts[0])] = parts[10].split(";")
    return out


def grade(img, spec):
    """Per-clip finish.

    The engine already applies its own filmic grade to every frame, so this is
    a trim rather than a look: the rainy missions capture two stops darker than
    the firelit ones, and a reel that swings that far between cuts reads as
    footage from two different games.
    """
    if not spec:
        return img
    if spec.get("b", 1.0) != 1.0:
        img = ImageEnhance.Brightness(img).enhance(spec["b"])
    if spec.get("c", 1.0) != 1.0:
        img = ImageEnhance.Contrast(img).enhance(spec["c"])
    if spec.get("s", 1.0) != 1.0:
        img = ImageEnhance.Color(img).enhance(spec["s"])
    return img


def push_in(img, amount):
    """A slow scale into frame — motion under a shot that has none of its own."""
    if amount <= 1.0001:
        return img
    w, h = int(W * amount), int(H * amount)
    big = img.resize((w, h), Image.LANCZOS)
    return big.crop(((w - W) // 2, (h - H) // 2, (w - W) // 2 + W, (h - H) // 2 + H))


# Which captured event becomes which sound. Ordinary trades stay light so the
# finisher has somewhere to go.
CUE_FOR = {"execute": ("finisher", 1.0), "bossdown": ("finisher", 1.0),
           "kill": ("kill", 0.85), "parry": ("parry", 0.9),
           "hit": ("hit", 0.7), "dodge": ("dodge", 0.5)}


def build(edl, workdir):
    total = int(round(edl["duration"] * FPS))
    cards = []
    for t in edl["titles"]:
        cards.append((t, card(t["lines"])))

    cues = []
    plan = [None] * total
    for clip in edl["clips"]:
        shot_dir = os.path.join(ROOT, edl["source"], clip["shot"])
        mapping = clip_mapping(clip)
        events = read_events(shot_dir)
        base = int(round(clip["in"] * FPS))
        seen = set()
        for i, src in enumerate(mapping):
            f = int(round(src))
            idx = base + i
            if idx >= total:
                break
            zoom = clip.get("zoom")
            z = 1.0 if not zoom else zoom[0] + (zoom[1] - zoom[0]) * i / max(len(mapping) - 1, 1)
            plan[idx] = (shot_dir, f, clip.get("grade"), z)
            for name in events.get(f, []):
                if name in CUE_FOR and (f, name) not in seen:
                    seen.add((f, name))
                    kind, gain = CUE_FOR[name]
                    cues.append([round(idx / FPS, 4), kind, gain])
        # A deliberate accent the edit asks for, on top of what play produced.
        for t, kind, gain in clip.get("accents", []):
            cues.append([round(clip["in"] + t, 4), kind, gain])

    # Render.
    for idx in range(total):
        entry = plan[idx]
        if entry is None:
            raise SystemExit(f"gap in the edit at output frame {idx}")
        shot_dir, f, g, z = entry
        path = os.path.join(shot_dir, f"f{f:04d}.jpg")
        if not os.path.exists(path):     # ran past the end of a take
            f = max(int(p.split("f")[-1].split(".")[0])
                    for p in os.listdir(shot_dir) if p.endswith(".jpg"))
            path = os.path.join(shot_dir, f"f{f:04d}.jpg")
        frame = Image.open(path).convert("RGB")
        if frame.size != (W, H):
            frame = frame.resize((W, H))
        frame = grade(push_in(frame, z), g).convert("RGBA")

        t = idx / FPS
        for spec, layer in cards:
            a = title_opacity(t, spec["start"], spec["end"],
                              spec.get("rise", 0.07), spec.get("fall", 0.12))
            if a <= 0.001:
                continue
            lay = layer
            if a < 0.999:
                lay = layer.copy()
                alpha = lay.getchannel("A").point(lambda v, a=a: int(v * a))
                lay.putalpha(alpha)
            frame = Image.alpha_composite(frame, lay)
        frame.convert("RGB").save(os.path.join(workdir, f"o{idx:05d}.jpg"),
                                  quality=95, subsampling=0)
        if idx % 120 == 0:
            print(f"  frame {idx}/{total}")
    cues.sort()
    return cues


def main():
    edl = json.load(open(sys.argv[1]))
    out = sys.argv[2]
    work = tempfile.mkdtemp(prefix="reel_")
    try:
        cues = build(edl, work)
        spec = {"duration": edl["duration"], "cues": cues}
        cue_path = os.path.join(work, "cues.json")
        json.dump(spec, open(cue_path, "w"))
        wav = os.path.join(work, "audio.wav")
        subprocess.run([sys.executable,
                        os.path.join(os.path.dirname(__file__), "audio.py"),
                        cue_path, wav], check=True)
        subprocess.run([
            "ffmpeg", "-y", "-loglevel", "error",
            "-framerate", str(FPS), "-i", os.path.join(work, "o%05d.jpg"),
            "-i", wav,
            "-c:v", "libx264", "-profile:v", "high", "-preset", "slow",
            "-crf", "19", "-pix_fmt", "yuv420p", "-r", str(FPS),
            "-c:a", "aac", "-b:a", "192k", "-ar", "48000",
            "-af", "loudnorm=I=-14:TP=-1.5:LRA=11",
            "-movflags", "+faststart", "-shortest", out], check=True)
        print(f"wrote {out}  ({len(cues)} sound cues)")
    finally:
        shutil.rmtree(work, ignore_errors=True)


if __name__ == "__main__":
    main()
