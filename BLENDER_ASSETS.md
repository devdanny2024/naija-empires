# Naija Empires — Blender Asset Build Manifest

The complete list of 3D models to rebuild in Blender, replacing the downloaded placeholder assets.
Pulled from the game enums (`Core/Types.cs`, `Core/ResourceType.cs`) and `Core/ModelLibrary.cs`.

---

## Pipeline & style rules (read first)

- **Style:** low-poly, flat-shaded, stylized — phone-friendly. Keep poly counts low.
- **Export:** static props → **OBJ**; animated characters/vehicles → **FBX** (with rig + clips).
  GLB is NOT supported by the game's importer.
- **Drop into:** `unity-m0/Assets/Resources/NE/Models/`.
- **Naming = the enum name** so the factories find it via `type.ToString()` —
  e.g. file/key `Villager`, `TownCentre`, `Barracks`, `Tank`.
- **Orientation:** Blender is Z-up. If a model imports lying down, set `RotX = -90` in `ModelLibrary`.
- **Scale:** don't hand-tune — `ModelLibrary` **auto-fits** each model to a target height (`Fit`).
  Target heights are listed below.
- **Team colour:** ONE model per building/unit, **recoloured per civ** (Benin / Oyo / Sokoto / Kanem-Bornu)
  via a coloured team band/ring. No per-civ duplicates needed.

---

## 1. Units — 9 models (animated FBX)

| # | Unit | Key | Fit (height) | Animations | Age | Status |
|---|------|-----|------|------------|-----|--------|
| 1 | Villager  | `Villager`  | ~1.7 | idle, walk, chop/gather, build, carry | Stone | ⬜ base mesh built |
| 2 | Spearman  | `Spearman`  | ~1.8 | idle, walk, attack | Iron | ⬜ |
| 3 | Archer    | `Archer`    | ~1.7 | idle, walk, shoot  | Iron | ⬜ |
| 4 | Cavalry   | `Cavalry`   | ~2.0 | walk, attack (mounted) | Bronze | ⬜ |
| 5 | Caravan   | `Caravan`   | ~1.8 | roll/move (trade cart) | Iron | ⬜ |
| 6 | Catapult  | `Catapult`  | ~2.2 | move, fire (wheeled siege) | Bronze | ⬜ |
| 7 | Tank      | `Tank`      | ~1.9 | move, fire | Modern | ⬜ |
| 8 | Gunner    | `Gunner`    | ~1.8 | idle, walk, shoot | Modern | ⬜ |
| 9 | Rifleman  | `Rifleman`  | ~1.8 | idle, walk, shoot | Modern | ⬜ |

> **Scholar is NOT modelled** — scholars are now a *count* held inside the University, not a spawned unit.

## 2. Buildings — 11 models (static OBJ)

Each needs **2–3 age "tier" looks** (key suffix `_T2`, `_T3`, `_T4`) that grow grander as the empire ages.

| # | Building | Key | Fit | Age req | Tier looks | Status |
|---|----------|-----|-----|---------|-----------|--------|
| 1 | Town Centre | `TownCentre` | ~3.2 | 1 (found more from 2) | base → grander → **skyscraper** (T4) | ⬜ |
| 2 | House | `House` | ~2.4 | 1 | hut → town house → block | ⬜ |
| 3 | Barracks | `Barracks` | ~2.5 | 2 | war-camp tiers | ⬜ |
| 4 | Stable | `Stable` | ~2.2 | 3 | tiers | ⬜ |
| 5 | Tower | `Tower` | ~3.2 | 2 | watchtower → **gun-turret** (T3/T4) | ⬜ |
| 6 | Wall | `Wall` | ~1.7 | 1 | wood → stone | ⬜ |
| 7 | Farm | `Farm` | ~1.0 | 1 | crop plot | ⬜ |
| 8 | University | `University` | ~2.6 | 2 | scholarly hall | ⬜ |
| 9 | Market | `Market` | ~2.6 | 2 | trade hall | ⬜ |
| 10 | War Factory | `WarFactory` | ~3.0 | 5 (Modern) | industrial works | ⬜ |
| 11 | Oil Pump | `OilPump` | ~2.0 | 5 (Modern, Build 2) | derrick on a well | ⬜ |

## 3. Resource nodes / world items — 5 models (static OBJ)

| # | Item | Key | Fit | Notes | Status |
|---|------|-----|-----|-------|--------|
| 1 | Yam plant (food) | `YamNode` | ~0.8 | gatherable food mound | ⬜ |
| 2 | Iron ore rock | `Rocks` / iron node | ~1.5 | gatherable iron | ⬜ |
| 3 | Iron Mountain | `IronMountain` | ~10 | big central contested deposit (shrinks as mined) | ⬜ |
| 4 | Oil well / patch | `OilWell` | ~0.5 | dark pool; build the Oil Pump on it (Build 2) | ⬜ |
| 5 | Rare/hidden deposit | (reuse ore, tinted) | ~1.5 | Iron Lode / Ironwood Grove / Fertile Plot variants | ⬜ |

> Timber comes from **trees** (decor below). Cowries (trade) & Knowledge have **no world node** — HUD icons only.

## 4. Environment / decor — 7 models (static OBJ)

| # | Item | Key | Fit | Status |
|---|------|-----|-----|--------|
| 1 | Palm tree | `Tree` | ~5 | ⬜ |
| 2 | Leaning palm | `TreePalmBend` | ~5 | ⬜ |
| 3 | Round forest tree | `ForestTree` | ~5 | ⬜ |
| 4 | Dark oak | `ForestTreeB` | ~5 | ⬜ |
| 5 | Bush / shrub | `Bush` | ~1 | ⬜ |
| 6 | Grass tuft | `Grass` | ~0.6 | ⬜ |
| 7 | Rocks (decor) | `Rocks` | ~1.2 | ⬜ |
| — | Lake water + **Fish** | — | — | ✅ fish built + jump/splash loop animated |

## 5. Code-drawn — DO NOT model in Blender

- **All UI icons** — resource / unit / building glyphs are vector-drawn in `UI/Glyph.cs`.
- **Effects** — command ping, resource highlight, muzzle flash, bullets, explosions, fish splash —
  all generated procedurally in code.

---

## Tally

**~32 core models** (9 units + 11 buildings + 5 resources + 7 decor), before age-tier variants.
With 2–3 tier looks on the main buildings, ≈ **45–50 model exports** total.

## Suggested build order

1. **Villager** (base mesh exists) → rig + animate
2. **Core buildings:** Town Centre, House, Barracks, Farm
3. **Resource nodes:** Yam, Iron ore, Iron Mountain
4. **Decor:** palm, forest tree, bush, rocks (forest + map dressing)
5. **Combat units:** Spearman, Archer, Cavalry, Catapult
6. **Economy:** Market, University, Caravan, Wall, Stable
7. **Modern age:** War Factory, Tank, Gunner, Rifleman, Oil Pump + Oil well

---
*Status legend: ⬜ to do · 🔨 in progress · ✅ done & exported into the game*
