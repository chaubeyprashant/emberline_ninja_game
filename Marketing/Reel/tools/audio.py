#!/usr/bin/env python3
"""Builds the reel's soundtrack: an original synthesised score plus the game's
own sound effects, placed on the beats the cut actually lands on.

Nothing here is sampled from a record. The score is generated from scratch —
taiko-style membrane hits, a low drone, a noise riser and a gong — so the reel
carries no third-party music licence. The effects layer is the game's shipping
audio: the same .ogg files Sfx3D plays on a phone, mixed at the frames where
the captured footage actually landed a hit, a parry or a kill.

    python3 audio.py events.json out.wav

events.json: {"duration": 15.0, "cues": [[t, "kind", gain], ...]}
Cue kinds: hit, heavy, parry, dodge, kill, finisher, step.
"""
import json
import os
import subprocess
import sys
import tempfile
import wave

import numpy as np

SR = 48000
SFX_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "..",
                       "Assets", "Resources", "Art", "Audio", "SFX")
BPM = 120.0
BEAT = 60.0 / BPM

rng = np.random.default_rng(7)


# ----------------------------------------------------------------- utilities

def env(n, attack, decay, curve=3.0):
    """Percussive envelope: short linear attack, exponential tail."""
    a = max(1, int(attack * SR))
    e = np.empty(n)
    e[:a] = np.linspace(0.0, 1.0, a)
    tail = np.arange(n - a) / SR
    e[a:] = np.exp(-tail / max(decay, 1e-4) * curve)
    return e


def one_pole(x, cutoff):
    """First-order low-pass. cutoff may be an array for a sweeping filter."""
    a = np.exp(-2.0 * np.pi * np.asarray(cutoff, dtype=float) / SR)
    a = np.broadcast_to(a, x.shape).copy()
    y = np.empty_like(x)
    acc = 0.0
    for i in range(x.shape[0]):
        acc = (1.0 - a[i]) * x[i] + a[i] * acc
        y[i] = acc
    return y


def soft_clip(x, drive=1.6):
    return np.tanh(x * drive) / np.tanh(drive)


def place(buf, sig, t, gain=1.0, pan=0.0):
    """Mix a mono signal into the stereo buffer at time t seconds."""
    i = int(t * SR)
    if i >= buf.shape[0]:
        return
    n = min(sig.shape[0], buf.shape[0] - i)
    l, r = np.sqrt(0.5 * (1.0 - pan)), np.sqrt(0.5 * (1.0 + pan))
    buf[i:i + n, 0] += sig[:n] * gain * l
    buf[i:i + n, 1] += sig[:n] * gain * r


# ------------------------------------------------------------- score voices

def taiko(amp=1.0, low=False):
    """Membrane hit: a fast downward pitch sweep with a noise attack on top."""
    dur = 0.75 if low else 0.45
    n = int(dur * SR)
    t = np.arange(n) / SR
    f0, f1 = (78.0, 41.0) if low else (132.0, 68.0)
    f = f1 + (f0 - f1) * np.exp(-t * (9.0 if low else 15.0))
    body = np.sin(2 * np.pi * np.cumsum(f) / SR)
    skin = one_pole(rng.normal(0, 1, n), 2200.0) * np.exp(-t * 45.0)
    sig = body * env(n, 0.001, dur * 0.30) + skin * 0.5
    return soft_clip(sig * amp, 1.3) * 0.9


def boom(amp=1.0, decay=1.1):
    """Sub impact: the low end under an accent, with a bit of transient click."""
    n = int((decay + 0.4) * SR)
    t = np.arange(n) / SR
    f = 34.0 + 40.0 * np.exp(-t * 14.0)
    sub = np.sin(2 * np.pi * np.cumsum(f) / SR) * env(n, 0.002, decay, 2.2)
    click = one_pole(rng.normal(0, 1, n), 4000.0) * np.exp(-t * 60.0) * 0.35
    return soft_clip((sub + click) * amp, 1.8)


def gong(amp=1.0, root=98.0, decay=3.2):
    """Inharmonic metal tail for the end card."""
    n = int(decay * SR)
    t = np.arange(n) / SR
    sig = np.zeros(n)
    for k, w in ((1.0, 1.0), (2.03, 0.6), (2.71, 0.42), (3.94, 0.3), (5.13, 0.2), (7.31, 0.12)):
        sig += w * np.sin(2 * np.pi * root * k * t + rng.uniform(0, 6.28))
    wash = one_pole(rng.normal(0, 1, n), 5200.0) * np.exp(-t * 9.0) * 0.4
    return (sig / 2.6 + wash) * env(n, 0.004, decay * 0.45, 2.4) * amp


def drone(dur, root=73.42, amp=1.0, bright=0.5):
    """Sustained low bed: a few detuned harmonic stacks, gently moving."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    sig = np.zeros(n)
    for h, w in ((1, 1.0), (2, 0.5), (3, 0.28), (4, 0.16), (5, 0.09), (6, 0.05)):
        for det in (-0.13, 0.0, 0.15):
            f = root * h + det * h
            sig += w * np.sin(2 * np.pi * f * t + rng.uniform(0, 6.28))
    sig /= 6.0
    # Slow breathing so a fifteen-second hold does not sit dead still.
    sig *= 0.82 + 0.18 * np.sin(2 * np.pi * 0.21 * t)
    return one_pole(sig, 400.0 + 2600.0 * bright) * amp


def riser(dur, amp=1.0):
    """Tension sweep: noise through a rising filter under a rising tone."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    x = t / dur
    noise = one_pole(rng.normal(0, 1, n), 250.0 + 7000.0 * x ** 2.2)
    noise -= one_pole(noise, 120.0)                      # take the mud out
    f = 110.0 * (2.0 ** (2.4 * x))
    tone = np.sin(2 * np.pi * np.cumsum(f) / SR) * 0.35
    shape = x ** 2.0
    return (noise * 0.55 + tone) * shape * amp


def tick(amp=1.0):
    """Dry high transient — the sixteenth-note pulse under the fast section."""
    n = int(0.09 * SR)
    t = np.arange(n) / SR
    x = rng.normal(0, 1, n)
    x = x - one_pole(x, 1800.0)
    return x * np.exp(-t * 90.0) * amp


# ------------------------------------------------------------------- score

def build_score(duration):
    """The cut's own music, written against the six title beats."""
    n = int(duration * SR)
    out = np.zeros((n, 2))

    # --- sustained bed. D1 throughout, a fifth stacked on for the boss, and a
    # slow lift under the finish so the last three seconds feel inevitable.
    place(out, drone(duration, 73.42, 0.38, 0.35), 0.0, pan=-0.15)
    place(out, drone(duration, 73.42, 0.33, 0.30), 0.0, pan=0.15)
    place(out, drone(6.0, 110.0, 0.22, 0.5), 6.5)          # A2 under the boss
    place(out, drone(5.5, 87.31, 0.20, 0.6), 9.5)          # F2 under the finish
    place(out, drone(2.6, 146.83, 0.14, 0.7), 12.5)        # D3 over the end card

    # --- percussion. Times are beats at 120 BPM, so every accent falls on a
    # cut point in the edit.
    hits = [
        # HOOK — one statement, then space.
        (0.00, "boom", 1.00), (0.00, "taiko_low", 0.85),
        (1.00, "taiko", 0.45), (1.25, "taiko", 0.30),
        # MASTER THE BLADE — driving eighths.
        (1.50, "taiko_low", 0.90), (1.50, "boom", 0.55),
        (2.00, "taiko", 0.50), (2.50, "taiko_low", 0.70),
        (3.00, "taiko", 0.50), (3.25, "taiko", 0.28),
        (3.50, "taiko_low", 0.72), (3.75, "taiko", 0.30),
        # TIMING MATTERS — half time, deliberately emptier.
        (4.00, "taiko_low", 0.85), (4.00, "boom", 0.5),
        (5.00, "taiko", 0.42), (6.00, "taiko_low", 0.66),
        # EVERY ENEMY FIGHTS DIFFERENT — weight returns.
        (6.50, "boom", 0.85), (6.50, "taiko_low", 0.95),
        (7.00, "taiko", 0.5), (7.50, "taiko_low", 0.75),
        (8.00, "boom", 0.6), (8.00, "taiko_low", 0.8),
        (8.50, "taiko", 0.5), (9.00, "taiko_low", 0.75), (9.25, "taiko", 0.35),
        # THE FINISH — a roll that closes up into the release.
        (9.50, "boom", 0.8), (9.50, "taiko_low", 0.9),
        (10.00, "taiko", 0.5), (10.50, "taiko_low", 0.7),
    ]
    # Accelerating roll: intervals shrink from a sixteenth to a thirty-second.
    t, step = 11.0, 0.25
    while t < 12.42:
        hits.append((t, "taiko", 0.35 + 0.85 * (t - 11.0) / 1.42))
        step = max(0.075, step * 0.88)
        t += step
    # RELEASE — the loudest moment in the mix, on the cut to the end card.
    hits += [(12.50, "boom", 1.0), (12.50, "taiko_low", 1.0), (13.90, "taiko", 0.3)]

    for t, kind, amp in hits:
        if kind == "boom":
            place(out, boom(amp * 0.72), t)
        elif kind == "taiko_low":
            place(out, taiko(amp * 1.0, low=True), t, pan=rng.uniform(-0.1, 0.1))
        else:
            place(out, taiko(amp * 0.8), t, pan=rng.uniform(-0.35, 0.35))

    # Sixteenth ticks only while the blade section is running.
    t = 1.5
    while t < 4.0:
        place(out, tick(0.20 if abs(t % 0.5) < 1e-6 else 0.10), t,
              pan=rng.uniform(-0.5, 0.5))
        t += 0.125

    place(out, riser(3.0, 0.80), 9.5, pan=0.0)
    place(out, gong(0.5, 98.0, 3.0), 12.5, pan=-0.1)
    place(out, gong(0.28, 146.83, 2.4), 12.55, pan=0.2)

    # A slow duck out of the last half second so the reel loops cleanly.
    tail = np.linspace(1.0, 0.0, int(0.45 * SR))
    out[-tail.shape[0]:] *= tail[:, None]
    return out


# --------------------------------------------------------- game sound effects

_cache = {}


def load_sfx(name):
    """Decode one of the game's shipping .ogg effects into a mono array."""
    if name in _cache:
        return _cache[name]
    src = os.path.abspath(os.path.join(SFX_DIR, name + ".ogg"))
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as tmp:
        path = tmp.name
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", src,
                    "-ac", "1", "-ar", str(SR), path], check=True)
    with wave.open(path) as w:
        raw = w.readframes(w.getnframes())
    os.unlink(path)
    sig = np.frombuffer(raw, dtype="<i2").astype(np.float64) / 32768.0
    _cache[name] = sig
    return sig


def pitched(sig, ratio):
    """Resample by linear interpolation — a cheap pitch/weight shift."""
    if abs(ratio - 1.0) < 1e-3:
        return sig
    n = int(sig.shape[0] / ratio)
    x = np.linspace(0, sig.shape[0] - 1, n)
    return np.interp(x, np.arange(sig.shape[0]), sig)


# What each gameplay event sounds like: the same families Sfx3D reaches for,
# layered so a kill is heavier than a hit and the finisher is heaviest of all.
LAYERS = {
    "hit":      [("knifeSlice", 0.55, 1.0), ("impactMetal_medium_002", 0.5, 1.0)],
    "heavy":    [("knifeSlice2", 0.6, 0.92), ("impactMetal_heavy_001", 0.7, 0.95),
                 ("impactPunch_medium_003", 0.45, 1.0)],
    "parry":    [("impactMetal_heavy_003", 0.85, 1.1), ("bong_001", 0.35, 1.2)],
    "dodge":    [("cloth3", 0.5, 0.9)],
    "kill":     [("knifeSlice", 0.5, 0.85), ("impactSoft_heavy_002", 0.7, 0.95),
                 ("impactPunch_heavy_001", 0.6, 0.9)],
    "finisher": [("knifeSlice2", 0.75, 0.8), ("impactPunch_heavy_004", 0.95, 0.85),
                 ("impactBell_heavy_002", 0.5, 0.9), ("impactMetal_heavy_000", 0.7, 0.85)],
    "step":     [("footstep_wood_002", 0.3, 1.0)],
    "draw":     [("drawKnife2", 0.55, 1.0)],
}


def build_sfx(duration, cues):
    n = int(duration * SR)
    out = np.zeros((n, 2))
    for t, kind, gain in cues:
        for name, amp, ratio in LAYERS.get(kind, []):
            sig = pitched(load_sfx(name), ratio)
            place(out, sig, t, amp * gain, pan=rng.uniform(-0.25, 0.25))
        if kind == "finisher":
            place(out, boom(0.9, 1.4), t)          # the low end the game cannot carry
        elif kind == "kill":
            place(out, boom(0.45, 0.8), t)
    return out


# -------------------------------------------------------------------- output

def compress(buf, thresh=0.22, ratio=3.5, attack=0.004, release=0.16):
    """Feed-forward bus compression.

    Without it the release at 12.5 s is sixteen decibels above the body of the
    track, and everything before it disappears on a phone speaker.
    """
    mono = np.abs(buf).max(axis=1)
    env = one_pole(mono, 1.0 / (2 * np.pi * attack))
    env = np.maximum(env, one_pole(mono, 1.0 / (2 * np.pi * release)))
    over = np.maximum(env, 1e-6) / thresh
    gain = np.where(over > 1.0, over ** (1.0 / ratio - 1.0), 1.0)
    return buf * gain[:, None]


def write_wav(path, buf):
    buf = soft_clip(compress(buf), 1.1)
    peak = np.max(np.abs(buf))
    if peak > 0:
        buf = buf / peak * 0.89
    data = (np.clip(buf, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())


def main():
    spec = json.load(open(sys.argv[1]))
    duration = spec["duration"]
    score = build_score(duration)
    sfx = build_sfx(duration, spec["cues"])
    # The effects sit on top: the score is the bed, the blade is the story.
    write_wav(sys.argv[2], score * 0.72 + sfx * 0.85)
    print(f"wrote {sys.argv[2]}  {duration:.2f}s  {len(spec['cues'])} cues")


if __name__ == "__main__":
    main()
