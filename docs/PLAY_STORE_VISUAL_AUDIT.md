# Play Store visual audit

Phase 1 of the store-listing overhaul. Audited against the build installed on
the Galaxy A33 on 2026-09-04 (versionCode 7 candidate, close third-person
camera, thirteen-body Mixamo cast, new icon).

## App icon — replaced

The shipped icon was an abstract orange diamond with a crack through it, which
communicated nothing about the game. It is now a cinematic portrait of Renzo:
black and charcoal armour, crimson accents, ember rim light, katana over the
shoulder, no text. Master in `Marketing/PlayStore/AppIcon/`, verified inside the
66% adaptive-icon safe zone and legible at 48 px.

## Capture pipeline — one real bug found and fixed

`PerfOverlay` was reachable in a **release** build: `Enabled` returned true if
either a PlayerPref or a flag file in the app's data folder was present, with no
build-configuration gate. Every capture taken before the fix carried a
frame-time HUD — the kind of thing that ends up in a store listing by accident.
It is now compiled to `false` outside `UNITY_EDITOR || DEVELOPMENT_BUILD`.
`CombatDebugOverlay` was already gated correctly.

## What the build can currently show

| Screenshot | Feasible now? | Notes |
| --- | --- | --- |
| 1 Hero combat | **Yes** | The close camera reads well: Renzo large, over-the-shoulder, sword visible, clean HUD. |
| 2 Boss fight | **Yes** | Jin duel on the rain-lit rooftop with banners is the strongest encounter shot. Goro and Kagachi also work. |
| 3 Multi-enemy | **Partly** | The cast is now visually distinct, but getting three enemies engaged *and* well composed needs a controlled spawn, not blind input. |
| 4 Cinematic story | **No — content does not exist** | The brief asks for snow, village and the red thread bracelet. `docs/CHANGELOG.md` lists forest, mountain, snow, temple and fortress geometry as not built, and adult Aiko and Jin as marked stand-ins. Staging it would be fabricating gameplay. |
| 5 Weapon variety | **Yes** | Weapons swap on the rig and are visible; needs a deliberate loadout change before capture. |
| 6 Duel mode | **Yes** | Nine distinct opponents; the duel grid and the fights both present well. |
| 7 Environment | **No, not at the quality bar** | `docs/ART_DIRECTION.md` §4.3 states the deck is "untextured flat cubes" and names a tiling roof-tile albedo as the single highest-impact missing asset. Captures confirm it: flat-coloured ground, white untextured prop cubes. An environment screenshot would advertise the weakest part of the game. |
| 8 Endless mode | **Below bar** | Same environment limitation, without a boss to carry the frame. Per the brief's own rule, omit rather than pad. |

## The honest conclusion

The **characters and the camera are ready**; the **environments are not.**

The recent work moved the cast to thirteen realistic Mixamo bodies and put the
camera 4.2 m behind Renzo at eye level, so any shot framed tightly on
characters looks like a current-generation mobile action game. Any shot that
gives the environment real estate exposes untextured geometry.

That points at a listing built from **five to six character-led screenshots**
rather than the eight in the brief — combat, boss, duel, multi-enemy and
weapon — with the story and environment slots left out until the art lands.
The brief's own instruction covers this: *quality is more important than
feature count.*

## Recommended sequence

1. Hero combat — close third-person, sword drawn
2. Boss fight — Jin in the rain
3. Duel — a second, visually distinct opponent
4. Multi-enemy — combat variety
5. Weapon — a distinctive blade

## Blocked

- **Story screenshot** — needs snow/village geometry and the adult story cast.
- **Environment screenshot** — needs the roof-tile and stone tiling textures in
  `docs/ART_DIRECTION.md` §4.3.
- **Play Console upload (Phases 9–12)** — not attempted; access unverified.
