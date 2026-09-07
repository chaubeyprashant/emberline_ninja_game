# Release notes — 1.2.1 (version code 7)

First release since **1.0.1 (code 6)**. Everything authored as 1.1.0, 1.2.0 and
1.3.0 ships together here, plus the combat, cast and camera work that landed
after them.

---

## Play Console "What's new"

Paste this into the release's release-notes field. Play's limit is 500
characters per language.

```
The biggest update to Emberline yet.

• A 100-mission campaign across 10 chapters — every mission has its own purpose, and one leads into the next
• Enemies that fight with intent: feints, spacing, and a personality per type
• Difficulty now changes how enemies think, not just their health
• Nine duel opponents, each its own model and its own fight
• A new close over-the-shoulder camera, and a whole new cast
• The Road North — an endless procedural run
```

---

## Full notes

**The campaign.** Ten story levels became 100 missions across 10 chapters and
3 acts. Every mission carries its own purpose, discovery and climax, and the
results screen names *why* the next one follows. Finishing it unlocks New
Game+, the nine-opponent duel roster and the Infinite March.

**Combat.** Enemies choose attacks by situation rather than at random —
distance, your state, their morale and what you have been doing to them all
score into the decision. Feints, delayed strikes, guard breaks and team
tactics, with a personality per archetype and phase changes on bosses.

**Difficulty.** Easy through Lethal change *behaviour*, not numbers: how well
enemies defend, how reliably they punish an opening, how tightly they hold
spacing, how fast they react, how often they err. Medium is unchanged.

**Duels.** Nine opponents, each a phased confrontation with its own posture
pool, arena, weather and dialogue — and, as of this release, its own model.
No two duel opponents share a body.

**The cast.** Player and all thirteen enemy kinds rebuilt on thirteen realistic
bodies sharing one skeleton, replacing the placeholder models.

**Camera.** A close over-the-shoulder camera roughly 4.2 m behind Renzo at eye
level, down from 10.7 m looking 50° down. Proper collision, and a gyro fix:
the sensor reports a rotation rate that was integrated into a permanent tilt,
so the framing drifted from 12° to 4° and stayed there.

**Presentation.** Rebuilt dark cinematic UI, home screen and duel grid. New
app icon.

**Fixed.** A frame-time debug overlay could switch itself on in a release
build. Weapon trails only ever drove the first weapon. The hit flash was
invisible on an opaque character at its default tint.

**Performance.** Thermal-aware frame capping and a tiered graphics path. Holds
60 fps on a Galaxy A33 through boss duels at ~228k triangles.

---

## Known gaps, stated plainly

- Forest, mountain, snow, temple and fortress geometry is carried by lighting,
  weather and dressing on two arenas rather than built.
- The five story-cast characters (Father, Mother, Aiko as a child, the village
  child, young Ren) are still placeholder models.
- No backend, so daily challenges are device-local.
