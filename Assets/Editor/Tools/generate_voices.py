"""Speak every dialogue line with macOS `say` into Assets/Resources/Voices/<md5>.aiff.

Run Emberline/Voices/Export Voice Lines first: it writes Logs/voice_lines.tsv
(hash, speaker, text, source) from the game's loaded data. The md5 is of the exact
string the runtime looks up (VoiceLines.Key), so the file name is the whole
contract; nothing here needs to know where a line came from.

    python3 Assets/Editor/Tools/generate_voices.py          # only lines with no voice yet
    python3 Assets/Editor/Tools/generate_voices.py --all    # re-speak everything
"""
import argparse
import os
import re
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
MANIFEST = os.path.join(ROOT, "Logs", "voice_lines.tsv")
OUT_DIR = os.path.join(ROOT, "Assets", "Resources", "Voices")

# One voice per character, and never the same voice for two characters who share
# a scene. RENZO/GORO/JIN/AIKO/KAGACHI keep the voices of the first voice pass.
VOICES = {
    "RENZO": "Alex",
    "REN": "Junior",                        # Renzo as a boy, in the memories
    "FATHER": "Reed (English (US))",
    "AIKO": "Samantha",
    "SUZU": "Kathy",
    "YOTSU": "Grandpa (English (US))",
    "GORO": "Fred",
    "JIN": "Daniel",
    "KAGACHI": "Ralph",
    "KAGEHIRA": "Grandpa (English (UK))",
    "WHISPER": "Whisper",
    "PALE SHADE": "Whisper",
    "SOLDIER": "Eddy (English (US))",
    "GUARD": "Rocko (English (UK))",
    "PATROL": "Eddy (English (UK))",
    "OFFICER": "Albert",
    "SEARCHER": "Reed (English (UK))",
    "RUNNER": "Rocko (English (US))",
    "VISITOR": "Moira",
}
DEFAULT_VOICE = "Alex"

# 16 kHz mono is plenty for speech and keeps the repo small; Unity re-encodes to
# Vorbis on import anyway (EmberVoiceLines.ConfigureImports).
DATA_FORMAT = "BEI16@16000"


def speakable(text):
    t = text.replace("…", "...").replace("—", ", ").replace("–", ", ")
    t = t.replace('"', "").replace("“", "").replace("”", "").strip()
    return t if re.search(r"[A-Za-z0-9]", t) else ""


def speak(row, force):
    digest, speaker, text = row
    path = os.path.join(OUT_DIR, digest + ".aiff")
    if os.path.exists(path) and not force:
        return "skip", speaker, None
    line = speakable(text)
    if not line:
        return "silent", speaker, text
    voice = VOICES.get(speaker, DEFAULT_VOICE)
    for v in (voice, DEFAULT_VOICE):
        r = subprocess.run(["say", "-v", v, "--data-format=" + DATA_FORMAT, "-o", path, line],
                           capture_output=True, text=True)
        if r.returncode == 0 and os.path.exists(path) and os.path.getsize(path) > 0:
            return ("made" if v == voice else "fallback"), speaker, v
    return "failed", speaker, r.stderr.strip()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--all", action="store_true", help="re-speak lines that already have a voice")
    ap.add_argument("--jobs", type=int, default=6)
    args = ap.parse_args()

    if not os.path.exists(MANIFEST):
        sys.exit(f"{MANIFEST} not found - run Emberline/Voices/Export Voice Lines first")
    rows = []
    with open(MANIFEST, encoding="utf-8") as f:
        for raw in f:
            cols = raw.rstrip("\n").split("\t")
            if len(cols) >= 3:
                rows.append((cols[0], cols[1], cols[2]))
    os.makedirs(OUT_DIR, exist_ok=True)

    counts = {}
    with ThreadPoolExecutor(max_workers=args.jobs) as pool:
        for status, speaker, info in pool.map(lambda r: speak(r, args.all), rows):
            counts[status] = counts.get(status, 0) + 1
            if status in ("fallback", "failed", "silent"):
                print(f"{status}: {speaker}: {info}")
    unmapped = sorted({r[1] for r in rows if r[1] not in VOICES})
    print(f"{len(rows)} lines: " + ", ".join(f"{k} {v}" for k, v in sorted(counts.items())))
    if unmapped:
        print("speakers using the default voice:", ", ".join(s or "(none)" for s in unmapped))


if __name__ == "__main__":
    main()
