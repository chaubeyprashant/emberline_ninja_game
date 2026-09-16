"""Voice every cinematic line with Gemini text-to-speech into Assets/Resources/Voices/<md5>.wav.

Same contract as generate_voices.py: run Emberline/Voices/Export Voice Lines first so
Logs/voice_lines.tsv (hash, speaker, text, source) is current. The md5 is of the exact
string the runtime looks up (VoiceLines.Key); the file name is the whole contract.
A regenerated line replaces the old macOS `say` .aiff (and its .meta) so Resources.Load
never sees two clips with one name.

The API key is read from $GEMINI_API_KEY or ~/.config/emberline/gemini_api_key and is
never written anywhere.

    python3 Assets/Editor/Tools/generate_voices_gemini.py --sample     # 6 audition lines -> Logs/voice_samples/
    python3 Assets/Editor/Tools/generate_voices_gemini.py              # lines with no .wav yet
    python3 Assets/Editor/Tools/generate_voices_gemini.py --all        # re-speak everything
    python3 Assets/Editor/Tools/generate_voices_gemini.py --speakers RENZO,AIKO
"""
import argparse
import base64
import json
import os
import random
import re
import sys
import time
import urllib.error
import urllib.request
import wave
from concurrent.futures import ThreadPoolExecutor

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
MANIFEST = os.path.join(ROOT, "Logs", "voice_lines.tsv")
OUT_DIR = os.path.join(ROOT, "Assets", "Resources", "Voices")
SAMPLE_DIR = os.path.join(ROOT, "Logs", "voice_samples")
KEY_FILE = os.path.expanduser("~/.config/emberline/gemini_api_key")
DEFAULT_MODEL = os.environ.get("GEMINI_TTS_MODEL", "gemini-2.5-flash-preview-tts")
ENDPOINT = "https://generativelanguage.googleapis.com/v1beta"
SAMPLE_RATE = 24000  # Gemini returns 16-bit mono PCM at 24 kHz

# One prebuilt voice per character and a standing direction for how they speak.
# Never the same voice for two characters who share a scene. Directions are the
# only "acting" in the game, so they carry the canon: Renzo is quiet and damaged,
# not a hero; Kagachi is unhurried; Aiko is a child in the memories and a young
# woman at the end.
CAST = {
    "RENZO":            ("Charon",       "a man in his late twenties, quiet, controlled and worn down; low voice, never raised, grief held under the words"),
    "REN":              ("Leda",         "a boy of about ten, earnest and a little frightened"),
    "FATHER":           ("Schedar",      "a village swordmaster in his fifties, steady, unhurried, warm but firm"),
    "AIKO":             ("Achernar",     "a girl, soft and clear, brave in a small voice"),
    "MOTHER":           ("Vindemiatrix", "a mother, gentle and tired, speaking low so the children do not hear"),
    "SUZU":             ("Zephyr",       "a young scout, quick, bright, always slightly out of breath"),
    "FUMI":             ("Despina",      "an informant, smooth, careful, every sentence weighed before it is said"),
    "TSURU":            ("Iapetus",      "an archer, clear, dry, economical with words"),
    "DAIGO":            ("Fenrir",       "a big warrior, loud, warm, cannot whisper"),
    "TOKU":             ("Sadaltager",   "an old blacksmith, gravel in the voice, patient"),
    "NIRE":             ("Sulafat",      "a healer, warm, calm, speaks to the wounded"),
    "GORO":             ("Algenib",      "a huge toll-captain, gravelly, cruel and amused, enjoys himself"),
    "JIN":              ("Orus",         "a duelist, precise, cold, faintly mocking, never hurried"),
    "KAGACHI":          ("Enceladus",    "a warlord, breathy, soft, unhurried, dangerous because he is calm"),
    "KAGEHIRA":         ("Enceladus",    "a warlord, breathy, soft, unhurried, dangerous because he is calm"),
    "WHISPER":          ("Aoede",        "a whisper from the dark, barely voiced, close to the ear"),
    "PALE SHADE":       ("Aoede",        "something drowned, whispering, wet and slow"),
    "SOLDIER":          ("Zubenelgenubi","a rank-and-file soldier, rough, bored, then alarmed"),
    "GUARD":            ("Alnilam",      "a gate guard, firm, suspicious"),
    "PATROL":           ("Achird",       "a patrolman, calling out to the others"),
    "OFFICER":          ("Rasalgethi",   "an officer, clipped, giving orders"),
    "SEARCHER":         ("Umbriel",      "a searcher, impatient, shouting across a room"),
    "RUNNER":           ("Puck",         "a runner, out of breath, urgent"),
    "VISITOR":          ("Callirrhoe",   "a traveller, easy-going, curious"),
    "SCAVENGER KING":   ("Algieba",      "a scavenger lord, oily, smooth, pleased with himself"),
    "CONVOY CAPTAIN":   ("Orus",         "a convoy captain, precise, contemptuous"),
    "DROWNED GUARDIAN": ("Algenib",      "a huge drowned warden, slow, hollow, gravelly"),
    "IRON GUARD":       ("Alnilam",      "an armoured bodyguard, flat, immovable"),
    "COMMANDER HOSHU":  ("Rasalgethi",   "a commander, formal, clipped, certain"),
    "BLADE":            ("Kore",         "one of three sister assassins, firm, quiet, amused"),
    "RYO":              ("Puck",         "a young villager, eager"),
    "KANTA":            ("Leda",         "a village child"),
    "OBA":              ("Gacrux",       "an old woman of the village, mature, unafraid"),
    "EXECUTIONER":      ("Algenib",      "an executioner, gravelly, bored by death"),
}
DEFAULT_CAST = ("Charon", "a man, quiet and controlled")


def api_key():
    key = os.environ.get("GEMINI_API_KEY", "").strip()
    if not key and os.path.exists(KEY_FILE):
        with open(KEY_FILE, encoding="utf-8") as f:
            key = f.read().strip()
    if not key:
        sys.exit("no API key: set GEMINI_API_KEY or write it to " + KEY_FILE)
    return key


def speakable(text):
    t = text.replace("…", "...").replace("—", ", ").replace("–", ", ")
    t = t.replace('"', "").replace("“", "").replace("”", "").strip()
    return t if re.search(r"[A-Za-z0-9]", t) else ""


def request(key, model, voice, direction, line):
    prompt = f"You are voicing a line of dialogue for a serious, restrained ninja drama. Speak as {direction}. Say only the line, nothing else:\n\n{line}"
    body = {
        "contents": [{"parts": [{"text": prompt}]}],
        "generationConfig": {
            "responseModalities": ["AUDIO"],
            "speechConfig": {"voiceConfig": {"prebuiltVoiceConfig": {"voiceName": voice}}},
        },
    }
    req = urllib.request.Request(
        f"{ENDPOINT}/models/{model}:generateContent",
        data=json.dumps(body).encode("utf-8"),
        headers={"Content-Type": "application/json", "x-goog-api-key": key},
        method="POST")
    with urllib.request.urlopen(req, timeout=120) as r:
        payload = json.loads(r.read().decode("utf-8"))
    for cand in payload.get("candidates", []):
        for part in cand.get("content", {}).get("parts", []):
            inline = part.get("inlineData")
            if inline and inline.get("data"):
                return base64.b64decode(inline["data"]), inline.get("mimeType", "")
    raise RuntimeError("no audio in response: " + json.dumps(payload)[:300])


def write_wav(path, pcm, mime):
    rate = SAMPLE_RATE
    m = re.search(r"rate=(\d+)", mime or "")
    if m:
        rate = int(m.group(1))
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(pcm)


def speak(key, model, row, out_path, force):
    digest, speaker, text = row
    if os.path.exists(out_path) and not force:
        return "skip", speaker, None
    line = speakable(text)
    if not line:
        return "silent", speaker, text
    voice, direction = CAST.get(speaker, DEFAULT_CAST)
    delay = 2.0
    for attempt in range(8):
        try:
            pcm, mime = request(key, model, voice, direction, line)
            tmp = out_path + ".part"
            write_wav(tmp, pcm, mime)
            os.replace(tmp, out_path)
            # Retire the macOS `say` clip so Resources.Load has one clip per name.
            for old in (os.path.join(OUT_DIR, digest + ".aiff"), os.path.join(OUT_DIR, digest + ".aiff.meta")):
                if os.path.dirname(out_path) == OUT_DIR and os.path.exists(old):
                    os.remove(old)
            return "made", speaker, voice
        except urllib.error.HTTPError as e:
            detail = e.read().decode("utf-8", "replace")[:200]
            if e.code in (429, 500, 502, 503, 504):
                retry = e.headers.get("Retry-After")
                wait = float(retry) if retry and retry.isdigit() else delay + random.uniform(0, 1)
                time.sleep(wait)
                delay = min(delay * 2, 60)
                continue
            return "failed", speaker, f"HTTP {e.code}: {detail}"
        except Exception as e:  # network blips: retry, then give up on this line
            time.sleep(delay)
            delay = min(delay * 2, 60)
            last = str(e)
    return "failed", speaker, "gave up: " + last


def resolve_model(key, model):
    """Confirm the model exists; if not, pick the first TTS-capable Gemini model listed."""
    req = urllib.request.Request(f"{ENDPOINT}/models?pageSize=200", headers={"x-goog-api-key": key})
    with urllib.request.urlopen(req, timeout=60) as r:
        names = [m["name"].split("/", 1)[1] for m in json.loads(r.read())["models"]]
    if model in names:
        return model
    tts = [n for n in names if "tts" in n]
    if not tts:
        sys.exit(f"{model} not available and no TTS model listed for this key")
    pick = next((n for n in tts if "flash" in n), tts[0])
    print(f"{model} not listed; using {pick}")
    return pick


def load_rows():
    if not os.path.exists(MANIFEST):
        sys.exit(f"{MANIFEST} not found - run Emberline/Voices/Export Voice Lines first")
    rows = []
    with open(MANIFEST, encoding="utf-8") as f:
        for raw in f:
            cols = raw.rstrip("\n").split("\t")
            if len(cols) >= 3:
                rows.append((cols[0], cols[1], cols[2]))
    return rows


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--all", action="store_true", help="re-speak lines that already have a .wav")
    ap.add_argument("--sample", action="store_true", help="two lines each for RENZO, AIKO, KAGACHI into Logs/voice_samples")
    ap.add_argument("--speakers", default="", help="comma-separated speaker filter")
    ap.add_argument("--limit", type=int, default=0)
    ap.add_argument("--jobs", type=int, default=3)
    ap.add_argument("--model", default=DEFAULT_MODEL)
    args = ap.parse_args()

    key = api_key()
    model = resolve_model(key, args.model)
    rows = load_rows()
    if args.speakers:
        want = {s.strip().upper() for s in args.speakers.split(",")}
        rows = [r for r in rows if r[1] in want]

    if args.sample:
        os.makedirs(SAMPLE_DIR, exist_ok=True)
        picked = []
        for who in ("RENZO", "AIKO", "KAGACHI"):
            lines = [r for r in rows if r[1] == who and len(speakable(r[2])) > 30][:2]
            picked += lines
        jobs = [(r, os.path.join(SAMPLE_DIR, f"{r[1].lower()}_{i % 2 + 1}.wav")) for i, r in enumerate(picked)]
        force = True
    else:
        os.makedirs(OUT_DIR, exist_ok=True)
        jobs = [(r, os.path.join(OUT_DIR, r[0] + ".wav")) for r in rows]
        force = args.all
    if args.limit:
        jobs = jobs[:args.limit]

    counts, done, t0 = {}, 0, time.time()
    with ThreadPoolExecutor(max_workers=args.jobs) as pool:
        for status, speaker, info in pool.map(lambda j: speak(key, model, j[0], j[1], force), jobs):
            counts[status] = counts.get(status, 0) + 1
            done += 1
            if status in ("failed", "silent"):
                print(f"{status}: {speaker}: {info}", flush=True)
            if done % 25 == 0:
                print(f"{done}/{len(jobs)} ({time.time() - t0:.0f}s) " + ", ".join(f"{k} {v}" for k, v in sorted(counts.items())), flush=True)
    unmapped = sorted({r[1] for r in rows if r[1] not in CAST})
    print(f"{len(jobs)} lines with {model}: " + ", ".join(f"{k} {v}" for k, v in sorted(counts.items())))
    if unmapped:
        print("speakers using the default voice:", ", ".join(s or "(none)" for s in unmapped))
    if args.sample:
        print("samples in", SAMPLE_DIR)


if __name__ == "__main__":
    main()
