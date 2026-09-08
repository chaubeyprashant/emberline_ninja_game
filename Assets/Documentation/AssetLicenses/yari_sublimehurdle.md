# Spear — SublimeHurdle yari

```
Asset:                 Yari
Source:                Sketchfab
URL:                   https://sketchfab.com/3d-models/yari-b5a4864a97fa40799314a45bb54d1ece
Creator:               SublimeHurdle_1542 (https://sketchfab.com/sublimehurdle_1542)
License:               Creative Commons Attribution 4.0 (CC BY 4.0)
Commercial Use:        Yes (Sketchfab download dialog: "Commercial use is allowed")
Attribution Required:  Yes — "Yari" by SublimeHurdle_1542, CC BY 4.0
Downloaded Date:       2026-09-08
Unity Version:         6000.5.9f1
Render Pipeline:       Built-in. Textures not used; shared Mat_Prop steel on attach.
File Format:           Original is .blend (no Blender on this machine). Sketchfab's glTF conversion
                       was downloaded and converted to OBJ by scratchpad/gltf2obj.py — a
                       dependency-free glTF 2.0 reader written for this — giving 1,754 verts,
                       1,874 triangles, matching Sketchfab's stated 1.9k.
```

**In project:** `Assets/Art/Weapons/Yari_SublimeHurdle/yari_sublimehurdle.obj` and
`Assets/Art/Weapons/Prefabs/yari.prefab`.

**Correction (`EmberWeaponImport.Wraps`):** authored along X with the origin mid-shaft; tip at
x=−7.83, butt at +8.80, head collar near −3.5. Rotated −90° about Z, held a third of the way from
the butt, sized 3.43/16.63 so it spans 2.4 m on the Pike Guard (prop scale 0.70) and 2.1 m on Renzo.
Guard at prop y=1.34, tip at 2.23.

**Referenced by:**
- `AshigaruYari.asset` — a new WeaponDef (`id = "yari"`, unlocks at mission 10): 3.8 m reach,
  narrow 70° thrust arc, slow chain, 150° sweeping cleave.
- The Pike Guard boss spec in `EmberCharacterFactory`, which previously faked a spear by
  stretching `sword_2handed` to (0.36, 1.35, 0.36). That hack is gone; his prop scale is uniform.
