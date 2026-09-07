#!/usr/bin/env python3
"""Look through a captured shot before cutting it.

    python3 review.py <shot_dir>                  # what happened, and when
    python3 review.py <shot_dir> <a> <b> <n> out.png   # a strip of n frames

The event summary is read from the capture's own events.csv, so picking an in
point is a matter of finding the frame where the game actually landed the
parry, not scrubbing a thousand JPEGs by eye.
"""
import os
import sys

from PIL import Image, ImageDraw

FPS = 60


def events(shot_dir):
    rows = []
    with open(os.path.join(shot_dir, "events.csv")) as fh:
        head = next(fh).strip().split(",")
        for line in fh:
            rows.append(dict(zip(head, line.rstrip("\n").split(","))))
    return rows


def summarise(shot_dir):
    rows = events(shot_dir)
    print(f"{shot_dir}: {len(rows)} frames ({len(rows) / FPS:.1f}s)")
    counts = {}
    for r in rows:
        for e in filter(None, r["events"].split(";")):
            counts[e] = counts.get(e, 0) + 1
    print("  events:", ", ".join(f"{k}={v}" for k, v in sorted(counts.items())))
    # Every frame worth cutting on, with the combo state around it.
    for r in rows:
        names = [e for e in r["events"].split(";")
                 if e in ("execute", "kill", "bossdown", "parry", "dodge")]
        if names:
            print(f"  f{r['frame']:>5} {float(r['t']):6.2f}s combo={r['combo']:>2} "
                  f"alive={r['alive']} bossHp={float(r['bossHp01']):.2f} {','.join(names)}")


def strip(shot_dir, a, b, n, out):
    rows = events(shot_dir)
    w, h = 300, 533
    sheet = Image.new("RGB", (w * n, h + 20), (10, 10, 12))
    d = ImageDraw.Draw(sheet)
    for i in range(n):
        f = a + round((b - a) * i / max(n - 1, 1))
        path = os.path.join(shot_dir, f"f{f:04d}.jpg")
        sheet.paste(Image.open(path).resize((w, h)), (i * w, 20))
        ev = rows[f]["events"] if f < len(rows) else ""
        d.text((i * w + 5, 4), f"f{f}  {f / FPS:.2f}s  {ev}", fill=(235, 235, 235))
    sheet.save(out)
    print(out)


if __name__ == "__main__":
    if len(sys.argv) == 2:
        summarise(sys.argv[1])
    else:
        strip(sys.argv[1], int(sys.argv[2]), int(sys.argv[3]),
              int(sys.argv[4]), sys.argv[5])
