# Kunai — psicodelik

```
Asset:                 kunai
Source:                Sketchfab
URL:                   https://sketchfab.com/3d-models/kunai-06c7e794025d4f288b6fb278ab4bf393
Creator:               psicodelik (https://sketchfab.com/psicodelik)
License:               Creative Commons Attribution 4.0 (CC BY 4.0)
Commercial Use:        Yes (Sketchfab download dialog: "Commercial use is allowed")
Attribution Required:  Yes — "kunai" by psicodelik, CC BY 4.0
Downloaded Date:       2026-09-08
Unity Version:         6000.5.9f1
Render Pipeline:       Built-in. The pack's diffuse/normal/metallic textures are not used; the
                       thrown prefab gets the project's ThrownKunai steel material.
File Format:           FBX (binary 7400), 2,268 triangles, one mesh
```

**In project:** `Assets/Art/Weapons/Kunai_psicodelik/kunai_psicodelik.fbx` and
`Assets/Art/Weapons/Prefabs/kunai.prefab`.

**Correction (`EmberWeaponImport.Wraps`):** authored along Z with the origin on the handle just
above the ring — ring at z=−0.03, blade base at +0.032, tip at +0.0965, 0.129 long. Rotated −90°
about X so the blade points +Y, sized 0.5/0.129 (0.31 m on Renzo).

**Referenced by:** `EmberlineBootstrap.ThrownPrefab("Kunai", "kunai", …)` — the thrown-kunai
pool's `Resources/Props/Kunai.prefab`, which every kunai throw (`Kunai.Spawn`) instantiates. It
replaces the KayKit dagger that stood in for a kunai. `ThrownPrefab` now prefers a weapon wrapper
over a KayKit FBX, the same rule `AttachProp` uses.
