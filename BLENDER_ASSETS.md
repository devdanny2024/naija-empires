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
| 1 | Villager  | `Villager`  | ~1.7 | idle, walk (✅ rigged + exported) | Stone | ✅ custom sculpted villager (face, team headband+sash) → NE_Villager.fbx, wired in |
| 2 | Spearman  | `Spearman`  | ~1.8 | idle, walk, attack | Iron | ✅ NE_Spearman (rig reused + spear + helmet + thrust) |
| 3 | Archer    | `Archer`    | ~1.7 | idle, walk, shoot  | Iron | ⬜ (downloaded placeholder) |
| 4 | Cavalry   | `Cavalry`   | ~2.0 | walk, attack (mounted) | Bronze | ⬜ (downloaded placeholder) |
| 5 | Caravan   | `Caravan`   | ~1.8 | roll/move (trade cart) | Iron | ⬜ (downloaded placeholder) |
| 6 | Catapult  | `Catapult`  | ~2.2 | move, fire (wheeled siege) | Bronze | ⬜ (downloaded placeholder) |
| 7 | Tank      | `Tank`      | ~1.9 | move, fire | Modern | ⬜ (downloaded; RotX -90 + RotY 90 to orient) |
| 8 | Gunner    | `Gunner`    | ~1.8 | idle, walk, shoot | Modern | ✅ NE_Gunner (rig reused + SMG + tactical helmet, Shoot) |
| 9 | Rifleman  | `Rifleman`  | ~1.8 | idle, walk, shoot | Modern | ✅ NE_Rifleman (rig reused + rifle + olive helmet, Shoot) |

> **Scholar is NOT modelled** — scholars are now a *count* held inside the University, not a spawned unit.

## 2. Buildings — 11 models (static OBJ)

Each needs **2–3 age "tier" looks** (key suffix `_T2`, `_T3`, `_T4`) that grow grander as the empire ages.

Each needs **2–3 age "tier" looks** (key suffix `_T2`, `_T3`, `_T4`). Style arc: T2 = grander
traditional, **T3 = modern**, **T4 = grand modern** (concrete + glass), so buildings visibly modernise.

| # | Building | Key | Fit | Age req | Tier looks | Status |
|---|----------|-----|-----|---------|-----------|--------|
| 1 | Town Centre | `TownCentre` | ~3.2 | 1 (found more from 2) | ✅ T2 grander mud · T3 modern mid-rise · T4 glass high-rise | ✅ base + T2/T3/T4 |
| 2 | House | `House` | ~2.0 | 1 | ✅ T2 hut compound · T3 modern bungalow · T4 modern block | ✅ base + T2/T3/T4 |
| 3 | Barracks | `Barracks` | ~2.5 | 2 | ✅ T2 war-camp · T3 armory · T4 military HQ | ✅ base + T2/T3/T4 |
| 4 | Stable | `Stable` | ~2.4 | 3 | ✅ T2 stable · T3 garage · T4 motor pool | ✅ base + T2/T3/T4 |
| 5 | Tower | `Tower` | ~3.2 | 2 | watchtower → **gun-turret** (downloaded T2/T3/T4) | ⬜ Blender tiers todo |
| 6 | Wall | `Wall` | ~1.6 | 1 | ✅ base NE_Wall (downloaded T2/T3) | ⬜ Blender tiers todo |
| 7 | Farm | `Farm` | ~1.0 | 1 | ✅ yam-mound plot → NE_Farm | ✅ done |
| 8 | University | `University` | ~3.0 | 2 | ✅ T2 Sankore hall · T3 modern college · T4 research tower | ✅ base + T2/T3/T4 |
| 9 | Market | `Market` | ~2.4 | 2 | ✅ T2 pavilion · T3 modern shop · T4 commercial block | ✅ base + T2/T3/T4 |
| 10 | War Factory | `WarFactory` | ~3.0 | 5 (Modern) | ✅ industrial works (smokestack, tank door) → NE_WarFactory | 🔨 base done |
| 11 | Oil Pump | `OilPump` | ~2.2 | 5 (Modern) | ✅ pumpjack on a well → NE_OilPump | 🔨 base done |

## 3. Resource nodes / world items — 5 models (static OBJ)

| # | Item | Key | Fit | Notes | Status |
|---|------|-----|-----|-------|--------|
| 1 | Yam plant (food) | `Yam` | ~0.9 | ✅ Blender → NE_Yam | ✅ |
| 2 | Iron ore rock | `Rocks` / iron node | ~1.4 | ✅ Blender → NE_OreIron (also used as decor Rocks) | ✅ |
| 3 | Iron Mountain | `IronMountain` | ~8 | ✅ Blender → NE_IronMountain (wired into BuildIronMountain) | ✅ |
| 4 | Oil well / patch | `OilWell` | flat | ✅ Blender → NE_OilWell (Scale; build Oil Pump on it) | ✅ |
| 5 | Gold/rich deposit | `OreGold` | ~1.6 | ✅ Blender → NE_OreGold | ✅ |

> Timber comes from **trees** (decor below). Cowries (trade) & Knowledge have **no world node** — HUD icons only.

## 4. Environment / decor — 7 models (static OBJ)

| # | Item | Key | Fit | Status |
|---|------|-----|-----|--------|
| 1 | Tree | `Tree` | ~5 | ✅ Blender → NE_Tree |
| 2 | Tree (short variant) | `TreePalmBend` | ~4.6 | ✅ NE_Tree reused |
| 3 | Forest tree | `ForestTree` | ~5 | ✅ NE_Tree reused |
| 4 | Forest tree (tall) | `ForestTreeB` | ~5.5 | ✅ NE_Tree reused |
| 5 | Bush / shrub | `Bush` | ~1.3 | 🔨 using small NE_Tree (no dedicated bush model yet) |
| 6 | Grass tuft | `Grass` | ~0.6 | ⬜ still Kenney (no Blender grass yet) |
| 7 | Rocks (decor) | `Rocks` | ~1.4 | ✅ Blender → NE_OreIron |
| — | Terrain + Lake water | `Terrain` | — | ✅ Blender → NE_Terrain (terrain + lakes joined) |
| — | **Fish** | — | — | ✅ fish built + jump/splash loop animated (in .blend, lakes) |

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
