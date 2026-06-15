# Naija Empires — Art Integration (Kenney + Quaternius)

Real models are now wired in **with automatic fallback** to the primitive placeholders. If a model
loads, you see it; if anything fails, you still get the cube/capsule — **the game can't break.**

## What was done

- Curated models copied into **`Assets/Resources/NE/Models/`**:
  - **Buildings (Kenney Castle Kit, textured):** `tower-square`, `tower-square-base`, `tower-square-mid`, `gate`, `tower-slant-roof`, `wall` + `colormap.png`
  - **Nature (Kenney):** `tree-large`, `rocks-large`
  - **Characters (Quaternius):** `Worker_Male`, `Soldier_Male`, `BlueSoldier_Male`, `Knight_Male`
- New **`ModelLibrary`** loads them and attaches each as the `Model` child (so the existing
  `ModelAnimator` still does the bob/lunge/hit). Characters are **tinted by faction** (blue = you, red = enemy)
  because Quaternius models are vertex-colored (no texture).

## In-game mapping

| Game object | Model used |
|---|---|
| Town Centre | tower-square |
| House | tower-square-base |
| Barracks | gate |
| Stable | tower-square-mid |
| Tower | tower-slant-roof |
| Timber node / trees | tree-large |
| Iron node / rocks | rocks-large |
| Villager | Worker_Male |
| Spearman | Soldier_Male |
| Archer | BlueSoldier_Male |
| Cavalry | Knight_Male |

## To see it

Just make sure the **`Assets/` folder is copied into your Unity project** (the new `Resources/NE/Models`
comes with it) and press **Play**. Unity auto-imports the FBX and `ModelLibrary` loads them at runtime.

## Expected tuning (this is normal)

Because I can't see the models render, **scale/position will probably need a nudge.** All of it lives in
ONE place — `Assets/Scripts/Core/ModelLibrary.cs`, the `Map` table:

```csharp
{ "TownCentre", new Def("tower-square", 2.0f) },   // 2.0f = scale; bump up/down if too small/large
{ "Villager",   new Def("Worker_Male", 1.0f, 0f, 0f, true) }, // scale, yOffset, rotY, tint
```

- **Too small/large?** change the `scale` number.
- **Floating / sunk into ground?** change the `yOffset`.
- **Facing wrong way?** change `rotY` (degrees).

### If buildings look white/untextured
Kenney models share `colormap.png` (copied in). Usually Unity auto-links it. If not:
1. Select a building FBX in `Resources/NE/Models` → **Inspector → Materials → Extract Materials**.
2. Assign `colormap` as the material's Base Map. Done once, applies to all Kenney models.

## Characters: where they stand

Right now characters show the **real model in bind pose**, moving via the procedural `ModelAnimator`
(bob/lunge). They are **not yet playing real skeletal animations** (walk/attack/gather) — that needs an
**Animator Controller** set up in the Editor, which is the next art step. Tell me when you're ready and
I'll provide the controller + the code hook to drive it (it replaces the procedural motion for characters).

## Adding more models later

1. Drop the `.fbx` into `Assets/Resources/NE/Models/`.
2. Add one line to the `Map` in `ModelLibrary.cs`: `{ "Key", new Def("file-name", scale) }`.
   Keys for buildings/units must match `BuildingKind`/`UnitType` names; or use a custom key you call yourself.

## Note
I couldn't run Unity here, so the loader is defensive (fallback to primitives). If you hit an import or
compile error, paste it and I'll fix it.
