# Release notes — 1.3.1 (version code 30)

Bundle: `Builds/emberline-1.3.1-code30.aab`, signed with the upload key. (Code 29 had only the revive fix.)

## Play Console "What's new"

```
<en-US>
Fixes:
• After Second Wind revived you, Renzo could stay lying on the ground. He now gets straight back up and fights on.
• Signed-in players' star ratings for missions 21-100 are now backed up to the cloud and restored on a new phone.
</en-US>
```

## Detail

**Cloud save.** `CloudSaveManager` only backed up and merged stars for
missions 1-20 (a loop left from the ten-level game, audit item B4), so a
reinstall or new phone lost every rating from mission 21 on. It now covers
every campaign mission (`Campaign.Count`, 100) and duels 1-20.

**Revive.**

Dying plays the death animation, which locks the character rig so nothing
interrupts it. The Second Wind revive restored health, time and the fight but
never released that lock, so the body stayed in its death pose. The revive now
resets the rig first (`GameManager.RevivePlayer` → `ResetVisuals`).

Regression check: `Emberline/Check Revive` (`EmberReviveCheck.Run`) starts the
first duel, dies, revives as the rewarded ad does, and fails if the rig is
still latched or the animator is still in its Dead state. Without the fix it
reports `latched=True animatorInDead=True`; with it, it passes.
