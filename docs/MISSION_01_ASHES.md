# Mission 1 — ASHES

Production design. Phase 1 of the chapter-one rework. Nothing here is implemented yet.

Grounded in what the engine can already do: every stage goal, camera move, audio bed and
dressing kind named below exists today unless it is marked **NEW**.

---

## 1. Narrative purpose

Introduce Renzo, Yorune, and the emotional reason for the whole game — in that order, with
no mythology. The player leaves this mission believing three things and understanding one:

- believes: *this was his home*, *his sister died here*, *someone is here right now*
- understands: **follow the map**

Aiko is introduced as a loss, not as an objective. That is what makes mission 9 land.

## 2. Player objective (UI text, verbatim)

| Beat | Objective line | Banner |
|---|---|---|
| 1 | `WALK INTO YORUNE` | `TEN YEARS LATER` |
| 2 | `FIND WHAT IS LEFT OF YOUR HOUSE` | `THE KUROGAWA HOUSE` |
| 3 | `SEARCH THE HOUSE` | — |
| 4 | `FOLLOW THE FOOTPRINTS` | `THEY ARE FRESH` |
| 5 | `SURVIVE` | `HE WAS WAITING` |
| 6 | `SEARCH THE BODY` | — |

One objective on screen at a time. Never a sentence — a command.

## 3. Mission flow

```
OPENING BEAT  ashes_return        (~55 s, letterboxed, skippable)
   ↓
STAGE 1  Reach      WALK INTO YORUNE            teach: move, camera        checkpoint
   ↓
STAGE 2  Reach      FIND WHAT IS LEFT…          teach: sprint              checkpoint
   ↓
STAGE 3  Examine    SEARCH THE HOUSE  ×3        NEW goal — authored props
   ↓     └─ prop 3 (the bracelet) fires beat  ashes_bracelet  (~25 s)
   ↓
STAGE 4  Reach      FOLLOW THE FOOTPRINTS       teach: dodge               checkpoint
   ↓     └─ arrival fires StageEvent.Ambush
STAGE 5  Wave       SURVIVE   1 × Assassin      teach: attack, dodge
   ↓
STAGE 6  Examine    SEARCH THE BODY   ×1        the map
   ↓
CLOSING BEAT  ashes_map           (~40 s)
```

Six stages, three checkpoints, one fight, one enemy. Target length **8–11 minutes**.

## 4. Opening cinematic — beat `ashes_return` (NEW)

Runs as `StageGoal.Cinematic` stage 0, so the mission holds until it finishes. Skippable
after 3 s (the existing skip prompt).

| # | Subject | Camera | Dur | Audio | Line |
|---|---|---|---|---|---|
| 1 | — | Hold | 4.0 | Wind | *(black; ash only)* |
| 2 | — | Wide | 5.0 | Wind | *(Yorune, ruined, from above)* |
| 3 | REN | OverShoulder | 4.5 | Silence | — |
| 4 | REN | PushIn | 5.0 | — | **RENZO:** "This was my home." |
| 5 | — | Hold | 1.5 | Sting | *(hard cut to white)* |
| 6 | AIKO | Handheld | 3.0 | Birds · **SetState.Peace** | *(children running — flashback)* |
| 7 | — | Hold | 2.5 | Village | **MOTHER:** "Ren! Aiko! Inside, both of you!" |
| 8 | — | Hold | 1.0 | Silence | *(cut back)* |
| 9 | REN | Hold | 4.5 | Wind · **SetState.Ruin** | — |

`SetState.Peace` / `Ruin` is how the flashback is achieved: the village set already has three
authored states and switching between them costs no load. That is the single most valuable
thing the existing system gives this rework.

**Total ~31 s + black.** Under the 30–90 s rule.

## 5. Camera direction

- The reveal (shot 2) is the only Wide in the mission. Destruction is established once and
  never re-sold; after this the camera stays low and close.
- Shot 4 is a PushIn that stops *before* it reaches him. Do not land the framing — the shot
  should feel like it wanted to get closer and could not.
- The flashback is the only Handheld in the mission. Memory is the unsteady thing here.
- Gameplay camera is untouched. No cinematic camera during play except the three prop
  discoveries, which use a 1.2 s PushIn and return control immediately.

## 6. Dialogue (complete)

Renzo speaks six times. Nobody else speaks in the present.

| Where | Speaker | Line |
|---|---|---|
| Opening 4 | RENZO | "This was my home." |
| Opening 7 | MOTHER *(memory)* | "Ren! Aiko! Inside, both of you!" |
| Prop 1 | RENZO | "The shrine's still standing. Of course it is." |
| Prop 2 | RENZO | "Father's post. He'd have had me on it at dawn." |
| Bracelet beat | RENZO | "…She never took it off." |
| Footprints | RENZO | "These are days old. Not years." |
| After the fight | RENZO | "Somebody came back." |
| Closing | RENZO | "Then somebody knows why." |

Cut from the current mission: *"Ten years. They kept the fires going ten years."* and
*"Somebody is still here. Somebody is still looking for something."* Both state the
discovery instead of letting the player make it, and the second pre-empts mission 2.

## 7. Gameplay and tutorials

Teaching is by necessity, never by modal. The existing HUD hint line (`EmberHud._hintText`,
already driven by a `_movedOnce` flag) carries all of it.

| Stage | Taught | How |
|---|---|---|
| 1 | move, camera | Hint after 2 s idle: `DRAG TO MOVE`. Clears on first input. |
| 2 | sprint | Long approach; hint at 6 s: `HOLD TO RUN`. |
| 3 | interact | First prop is on the path and cannot be missed. |
| 4 | dodge | Hint on arrival: `SWIPE TO FLICKER`. |
| 5 | attack, dodge | The assassin telegraphs twice before its first real swing. |

No combat tutorial text. The assassin is the tutorial.

## 8. Enemy encounter

**One enemy: `EnemyKind.Assassin`.** Not a wave, not a pack.

- Spawns on `StageEvent.Ambush` when the player reaches the footprint trail's end.
- Scripted opening: two telegraphed attacks it deliberately misses, then it fights properly.
- Difficulty: campaign default. It should be beatable while losing half a health bar.
- It cannot be assassinated — the free silent kill belongs to mission 2, where stealth is
  the mission's identity. **This is a change from the current mission 1**, which opens with
  a free stealth kill and then takes stealth away, teaching a verb the mission does not use.

## 9. Environmental storytelling

Mission dressing, all existing kinds:

`BurnedHome` (the Kurogawa house — placed, not random), `AbandonedWeapons`, `MissingNotice`,
`DestroyedCart`, `BloodTrail` (the footprint trail).

Explicitly **not** used: `HidingVillagers` (nobody is alive in Yorune — the current mission
places them, which contradicts the premise), `KagehiraBanners` (that is mission 2's reveal).

Three authored props (**NEW** — see §12):

1. **The shrine** — standing, untouched, in a village that burned. Says the fire was aimed.
2. **The training post** — a father's, cut to pieces by ten years of weather, not by fire.
3. **The bracelet** — red thread, half-buried in ash, in the child's room.

## 10. VFX

All from `FxPools`, which already has what is needed:

- Ambient ash drift — a slow `Embers` emitter at low rate over the whole village. **NEW**
  emitter config; the pool exists.
- Prop discovery: a single small `Sparks` burst, cool white, 6 particles. Restrained.
- The bracelet: no VFX at all. It is the one thing in the mission the player finds in silence.
- Combat: unchanged.

## 11. SFX and music

| Moment | Bed |
|---|---|
| Opening 1–3 | `Wind` only. No music. |
| Opening 4 | `Silence` under the line |
| Flashback | `Birds` → `Village`, abruptly cut |
| Return to ruin | `Wind`, and nothing else, for the whole exploration |
| Bracelet | `Silence`, then `MusicSoft` on the hold |
| Ambush | `Sting` → `MusicDark` |
| Closing | `MusicSoft` |

Music enters **twice** in eleven minutes. The silence is the point; a scored exploration
sequence would make the ruin feel like a level instead of a graveyard.

## 12. What must change — files

### New code

| File | Why |
|---|---|
| `Assets/Scripts/Missions/StoryProp.cs` | **NEW.** An authored, named, positioned interactable with a label, an optional line, and an optional beat to fire. The existing `Clue` is a glowing cube spawned at a procedural ring position — it cannot be "the bracelet in Aiko's room". |
| `Assets/Scripts/Missions/MissionStage.cs` | Add `StageGoal.Examine` and a `props` array (id, position, label, line, beatId). |
| `Assets/Scripts/Missions/MissionDirector.cs` | Handle `Examine`: place the authored props, count them, fire per-prop lines and the beat. ~40 lines beside the existing `Investigate` case. |
| `Assets/Scripts/UI/EmberHud.cs` | Extend the existing hint line with the four teach prompts and their trigger conditions. |

### New assets (authored in the editor scripts, as everything here is)

| File | Change |
|---|---|
| `Assets/Editor/EmberStory.cs` | Author beats `ashes_return`, `ashes_bracelet`, `ashes_map`. |
| `Assets/Editor/EmberMissions.cs` | Rewrite the `S01_FirstBlood` plan → rename `S01_Ashes`; six stages as above. |
| `Assets/Scripts/Campaign/CampaignTable.cs` | Mission 1 row: name `ASHES`, new objective/discovery/climax/ending/next-reason, new dialogue, plan `S01_Ashes`, enemies `{ Assassin }`. |

### Existing content preserved

- The whole `opening` prologue beat, with **one line changed** (see the open question).
- `memory_aiko`, `memory_burning`, `memory_lastnight` — these are the flashback fragments
  missions 7 and 9 need. Untouched here.
- Every stage goal, camera, audio and dressing system.
- Checkpoints, challenges, shards, the results screen.

### Not touched

`MissionDirector`'s other 18 goals, all 99 other missions, combat, the campaign validator.

## 13. Checkpoints and failure

- Checkpoints at stages 1, 2 and 4 (existing `MissionStage.checkpoint`).
- Only failure is death, which resumes at the last checkpoint. No stealth-fail, no timer.
- Mission 1 must not be losable in a way that requires replaying the opening cinematic —
  the checkpoint at stage 1 exists for that alone.

## 14. Completion and transition

On the last prop:

> **RENZO:** "Then somebody knows why."

Closing beat `ashes_map`: the map in his hand, one road inked red, a PullOut that keeps
pulling until Yorune is small — and holds two seconds too long on a ridge where a figure is
watching. No line. Cut to black.

Results screen (existing) shows the next-mission reason: *"The map is the only thing in
Yorune that was made recently. Renzo follows the road before whoever drew it comes back."*
Next objective established: **FOLLOW THE RED ROAD.**

---

## Open questions — need your call before implementation

1. **The prologue names the Black Seal.** `opening.asset` shot 11 has Kagehira say *"The
   Black Seal. Say where it is, and this stops."* Your brief withholds that name until
   mission 8–10. Recommend changing the line to **"You know what I came for. Say where it
   is, and this stops."** — the scene keeps its threat, the father's refusal still lands,
   and the name arrives when the letter does in mission 8. One line in `EmberStory.cs`.

2. **Aiko is currently believed alive from mission 2.** Your rework has Renzo believe she
   died until mission 9, which is much stronger. That contradicts missions 2–8 as written,
   and those are Phase 1 too — I need to know whether to carry the "she is dead" belief
   through 2–8 when I design them.

3. **Yorune has no geometry of its own.** Missions play in the generated valley village.
   The Kurogawa house, the shrine and the child's room are dressing props, not interiors —
   the player finds them in the open, not by entering a building. Acceptable for Phase 1,
   or does mission 1 warrant its own small authored set?
