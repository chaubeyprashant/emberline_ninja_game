# Emberline — Unity 3D Scaffold

Starter scripts for the full 3D version of Emberline, porting the systems that were
**validated in the Flutter prototype** (`../emberline`) to Unity. This is not a runnable
Unity project by itself — it's the `Assets/Scripts` layer you drop into a fresh project
so the game logic starts from the proven design instead of a blank file.

## Setup (15 minutes)

1. Install **Unity 6 LTS** (or 2022.3 LTS) via Unity Hub, with Android Build Support.
2. Create a new project using the **URP 3D** template, name it `EmberlineUnity`.
3. Copy this `Assets/Scripts` folder into the project's `Assets/`.
4. Create a capsule + `CharacterController` on an empty scene, add `PlayerLocomotion`
   and `CombatController`, assign a `SenGates` component, and press Play — WASD moves,
   Space dashes with i-frames, mouse buttons attack.
5. For the anime look: URP + a toon shader (e.g. a cel ramp Shader Graph) + outline pass.

## What's here and where it came from

| Script | Ports | Notes |
|---|---|---|
| `Core/SenGates.cs` | Prototype's Sen/Gate/Surge economy | Numbers match the tuned Flutter values |
| `Core/WeaveDefinition.cs` | Design bible's Weave catalog | ScriptableObject: author abilities as assets |
| `Player/PlayerLocomotion.cs` | Movement + Flicker dash | CharacterController-based, camera-relative |
| `Player/CombatController.cs` | Strike/Cleave/Flicker/Surge verbs | Arc hit-checks, combo chain, hit-stop hooks |
| `Enemies/EnemyBrain.cs` | Prototype AI state machine | spawn→chase→windup→strike→recover + stagger |
| `Enemies/AttackTokenPool.cs` | Attack-token coordination | Max 2 simultaneous attackers, bosses exempt |
| `Missions/MissionDef.cs` | Mission/wave data | ScriptableObject: same shape as the Dart MissionDef |
| `GameManager.cs` | Wave flow + D–S ranking | Same scoring formula as the prototype |

## The bigger architecture (from the design bible)

When the project grows past prototype scale, migrate abilities to a data-driven
ability system (the bible specifies UE5+GAS; in Unity the equivalent is a
ScriptableObject ability graph + a cooldown/cost runner, which `WeaveDefinition`
already starts). Keep these invariants from the prototype:

- **Telegraph colors**: white = blockable, red = unblockable. Non-negotiable readability rule.
- **Surge always costs a Gate.** The risk/reward identity lives or dies here.
- **Enemies take turns** via tokens; bosses ignore tokens but armor through light hits mid-windup.
- **Rank scores protection, not kills** (time, damage, combo, gates — never body count alone).
