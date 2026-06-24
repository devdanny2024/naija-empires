# Naija Empires — Project Continuation (read this first)

This file lets any Claude session continue the art + gameplay work without re-deriving context.
Pair it with `BLENDER_ASSETS.md` (the model checklist).

---

## What this is
West-African empires RTS (Unity, iOS/TestFlight). Repo: `D:\Work\naija-empires` (remote
`github.com:devdanny2024/naija-empires`, branch `master`). Unity project under `unity-m0/`.
Source art `.blend`: `blender/NaijaEmpires_Assets.blend` (built via the **Blender MCP** — Blender must be
open + the MCP add-on running).

Build/deploy: **Unity Build Automation (UBA)** in the cloud → download `.ipa` → `gh release create
ios-build-N "<ipa>#NaijaEmpires-build-N.ipa"` → `gh workflow run upload-testflight.yml --field
tag=ios-build-N`. **Build number lives in `unity-m0/ProjectSettings/ProjectSettings.asset`
(`buildNumber: iPhone:`) and MUST increment every TestFlight upload.** Currently 16; verify a fresh
IPA's CFBundleVersion before uploading (extract Info.plist).

---

## ⚠️ ASSET PIPELINE RULES (hard-won — do not relearn these the hard way)

Models live in `unity-m0/Assets/Resources/NE/Models/`. Loaded by key = enum name via
`ModelLibrary.CreateModel(type.ToString())`. Registered in `Core/ModelLibrary.cs` `Map`.

1. **Build at the ORIGIN in Blender, not a staging offset.** Building at an offset and moving objects
   later separated a skinned mesh from its armature (mesh is a *child* of the armature, so moving both
   double-offsets it) → exported villager rendered invisible/off-screen. If you must stage, move only
   the armature; keep mesh local position (0,0,0).
2. **Static buildings need `RotX = -90` in their Def.** `CreateModel` *overrides* the model's rotation
   with `Euler(RotX, RotY, 0)`, so a Blender Z-up static mesh ends up sideways unless `RotX=-90`.
   Skinned characters (villager) do NOT need it (bones drive them).
3. **Scale:** `Def.Fit` auto-scales a model so its *height* ≈ Fit, then grounds it. Use Fit for tall
   things. **Flat things (Farm) use `Scale`, not Fit** (height-fit blows up a flat mesh).
   ⚠️ Auto-fit on SKINNED meshes can misbehave (bounds) — the villager uses `Fit = 1.7` and works, but
   if a rigged unit goes invisible, suspect bounds/scale and consider a fixed `Scale`.
4. **`Raw = true`** keeps the model's own imported materials (skip the Kenney colormap auto-bind).
5. **Export settings (static):** `export_scene.fbx(use_selection=True, object_types={'MESH'},
   bake_anim=False, mesh_smooth_type='FACE', apply_unit_scale=True)`. Set object origin to base-centre
   (`cursor at base, origin_set ORIGIN_CURSOR`) so it grounds right.
6. **Export settings (animated):** `object_types={'MESH','ARMATURE'}, bake_anim=True,
   bake_anim_use_all_actions=True, add_leaf_bones=False, apply_unit_scale=True`.
7. **Animation clip names matter.** `ModelAnimator` splits the FBX clip name on the last `|` and needs
   exactly: **`Idle`**, **`Walk`** (or `Run`), and for gathering **`PickUp`**; combat uses
   **`SwordSlash`** (melee) / **`Shoot_OneHanded`** (ranged); optional `Walk_Carry`, `Death`. Name the
   Blender *actions* exactly these (give them `use_fake_user=True`). If clips don't bind, the game logs
   `[NaijaEmpires] <name>: rig present but expected clips missing — imported clips = [...]` — read the
   Console to see the real names. If no warning but no animation, the FBX imported with no Animator →
   set the FBX **Rig → Animation Type = Generic** in Unity.
8. **Team colour:** give one material slot the name **`NE_Team`**. `UnitFactory` and `BuildingFactory`
   call `MaterialUtil.TintSlot(model, "NE_Team", BodyColor(faction))` to recolour it per empire.
9. **Rig (humanoids):** skin+subsurf body → bones `root/spine/head/arm_L/arm_R/leg_L/leg_R` →
   `parent_set(type='ARMATURE')` (auto weights). The villager rig is the template — reuse it for
   Archer, Spearman, Cavalry rider, Gunner, Rifleman.
10. After any Blender change, `bpy.ops.wm.save_as_mainfile(...)` the `.blend`.

---

## PROGRESS (✅ done / 🔨 base done, tiers/anim pending / ⬜ todo)

**Buildings (custom Blender, wired, `RotX=-90`, NE_Team):** TownCentre ✅, House ✅, Barracks ✅,
Farm ✅, Stable ✅, Wall ✅, University ✅, Market ✅, WarFactory ✅, OilPump ✅. *Age-tier variants
(grander/modern per age) NOT built yet — see design below.*

**Units:** ALL built + wired in `ModelLibrary` now —
- Rigged humanoids (Walk/Idle + Shoot/Attack, team headband/sash): Villager ✅ (also PickUp),
  Spearman ✅, Archer ✅ (NE_Archer), Cavalry ✅ (NE_Cavalry), Gunner ✅, Rifleman ✅.
- Vehicles (static, `RotX=-90`, built facing -Y so they face travel dir): Catapult ✅ (NE_Catapult),
  Caravan ✅ (NE_Caravan), Tank ✅ (NE_Tank, custom, replaced the reversed downloaded one).
- Scout ✅ (gameplay — queued-waypoint movement `Units/Scout.cs`; reuses villager model for now).
**TODO unit models:** `RocketVehicle` (Catapult's Modern form), optional distinct Scout + Machine-Gunner
(Archer's Age-3 form) models. **All unit FBX are local/uncommitted — user verifying in Unity.**

**Map:** Blender diorama (`Scene`, 240×240, 3 lakes+fish, forest ring, resources, sample base) — this
is a VISUAL reference only; the in-game map is procedural (`Bootstrap`/`TerrainBuilder`). To change the
in-game look, edit the procedural generator, NOT import the diorama mesh.

**Oil economy:** ✅ implemented — `ResourceType.Oil`, `Cost.Oil` (6-arg ctor), `Economy.Oil`
(CanAfford/Spend/Add), 8th HUD badge + code-drawn droplet `Glyph`. Source = **Oil Pump** building
(`BuildingKind.OilPump`, Modern age, `OilProducer` adds ~2 Oil/sec, disabled during construction;
model `NE_OilPump`). Mechanical units now cost Oil (Tank 90, Gunner 15, Rifleman 12). **TODO refinement
(deferred):** hidden oil WELLS you must scout before placing a pump (reuse `World/HiddenResource.cs`);
right now the pump is buildable anywhere at the Modern age.

**UI/HUD polish (user asked, 2026-06-24):** ✅ Health bars upgraded in `Combat/Health.cs` OnGUI —
glossy gradient fill, drop shadow, frame, green→yellow→red by HP. ✅ Resource badges are now
INTERACTIVE — each has a hover highlight + press feedback (`MakeBadgeClickable`) and click expands a
detail popup (`BuildBadgeInfo`/`ToggleBadgeInfo`) showing live amount, +N/sec income, and a tip on how
it's produced. **TODO:** broader "more game-like" pass (animated panel transitions, richer styling). NOTE: the "NAIJA EMPIRES" title is only in the **menu** (`Core/GameFlow.cs`
:161 splash, :232 main menu) — there is NO such title in the in-game `BrandedHud` (it shows the empire
crest e.g. "Kingdom of Benin"). Confirm with the user which title they see in-game before removing.

**Combat effects:** still code-drawn placeholders. Muzzle flash / projectiles / explosions to be added
(see `World/ClickFeedback.cs` `CommandPing` for the code-drawn FX pattern).

**Git state:** lots of work is LOCAL/UNCOMMITTED pending the user's Unity verification. Do NOT push to
UBA until the user confirms. When confirmed: commit + push, bump build number if the prior was uploaded.

---

## DESIGN DRAFT — Unit Age-Modernization (the "no medieval tools in the Oil age" system)

**Principle:** military units modernize as the empire advances ages; by the Modern/Oil age nothing uses
medieval tools. Examples the user gave: **Archer → Machine-Gunner (≈Age 3)**, **Catapult → Rocket
Vehicle**. This is the unit equivalent of the building tier system.

**Proposed architecture (`UnitForm` / age-tier per role):**
- Keep the gameplay `UnitType` (so combat-triangle, training building, AI stay valid) but give each
  combat type an **age-indexed FORM**: `{ modelKey, displayName, hpMult, dmgMult, rangeMult,
  projectile/FX, moveClip/attackClip }`.
- On **age-up**, an empire's existing units of that type swap model + restat to the new form (mirror
  `Buildings/Upgradeable.SetAgeTier` + `AgeProgression`, but for units). Newly trained units spawn in
  the current form. The train tile + selection show the current form's name.
- Add `UnitConfig.Form(UnitType, age)` (a switch table) + a `UnitTier` component on units that listens
  for age changes (or have `AgeProgression` also walk units).

**Draft tier table (tune later):**
| Role (UnitType) | Stone/Iron (1–2) | Bronze/Golden (3–4) | Modern/Oil (5) |
|---|---|---|---|
| Archer (ranged) | Bowman | **Machine-Gunner** (rapid fire FX) | Heavy Gunner |
| Spearman (melee) | Spearman | Musketeer | Soldier (Rifleman model) |
| Cavalry (fast) | Horse rider | Camel/heavy rider | Armoured car |
| Catapult (siege) | Catapult | Cannon | **Rocket Vehicle** (explosive AoE) |

**DECISION (user chose, 2026-06-24): MERGE.** The modern forms ARE the existing heavy units — no
duplicate trains. Advancing age auto-upgrades the existing army to the next form; new trains spawn in
the current form. The **War Factory becomes the modern-tech ENABLER** (a faction's units only reach
their Modern (Age-5) forms once it has built a War Factory) rather than training separate Gunner/
Rifleman/Tank. So `Gunner`, `Rifleman`, `Tank` cease to be separately-trainable — they become FORMS:

| Line (trained as) | Age 1–2 | Age 3–4 | Age 5 (needs War Factory) | Modern model |
|---|---|---|---|---|
| **Archer** (ranged) | Bowman | Machine-Gunner | Heavy Gunner | `Gunner` model (Swat / custom) |
| **Spearman** (melee) | Spearman | Musketeer | Soldier | `Rifleman` model |
| **Cavalry** (fast) | Horse rider | Heavy rider | Armoured car / **Tank** | `Tank` model |
| **Catapult** (siege) | Catapult | Cannon | **Rocket Vehicle** | new `RocketVehicle` model |

**Implementation plan (Merge):**
1. `UnitConfig.Form(UnitType type, int age)` → `{ modelKey, displayName, hpMult, dmgMult, rangeMult,
   moveClip, attackClip, projectileFX }`. Drives model + stats + name from (type, age).
2. Remove `Gunner`/`Rifleman`/`Tank` from any building's `Trainable` (War Factory). Keep the enum values
   (used as modern *forms*/model keys) but they're no longer trained directly. Catapult gains a modern
   form key `RocketVehicle`.
3. Gate Age-5 forms on owning a War Factory: `Form()` returns the Age-3/4 form unless the faction has a
   built War Factory (add `Match`/faction query `HasWarFactory(FactionId)`).
4. Unit form-swap on age-up: mirror `Buildings/AgeProgression` — when an empire's age changes (or it
   builds its first War Factory), walk its units; for each, look up `Form(type, age)`, `SwapModel` the
   "Model" child, restat `Health`/`CombatUnit.damage`/`attackRange`. Newly trained units call `Form()`
   at spawn (in `UnitFactory`).
5. Train tiles + selection labels show `Form().displayName` for the current age.
6. Combat FX per form (bow arrow → bullets/muzzle flash → rockets+explosion).

**DONE so far (verify in Unity):**
- `WarFactoryTag` marker on War Factory buildings + `WarFactoryTag.Has(FactionId)` query (completed
  factories only). → `Buildings/ProductionBuilding.cs`, added in `BuildingFactory` WarFactory case.
- `UnitConfig.ModernModel(UnitType)` → Archer→`Gunner`, Spearman→`Rifleman`, Cavalry→`Tank`
  (Catapult→RocketVehicle still null: model not built). → `Config/GameConfig.cs`.
- `UnitConfig.ModernMult(UnitType)` → per-form (hp, dmg, range) multipliers. A unit spawned in modern
  form now FIGHTS modern (sturdier, harder-hitting, longer reach), not just looks modern.
- `UnitFactory.Spawn` computes the modern decision ONCE (`modern`/`modelKey`/`mult`) and uses it for the
  model (CreateModel + IsRaw + LoadClips), HP (`Hp × mult.hp`), and combat (`Damage × mult.dmg`,
  `Range × mult.range`). Applies to units *trained in the oil age* with a War Factory.

- `UnitConfig.ModernType(UnitType)` (Archer→Gunner, Spearman→Rifleman, Cavalry→Tank) drives ATTACK FX.
  `CombatUnit.FxType` (set at spawn = modern type when modernised) is what `TryAttack` passes to
  `AttackFX.Fire` — so a modernised archer fires bullets, cavalry→tank fires shells, etc. Gameplay
  identity (counter triangle) stays the base `Type`; only the visuals modernise. `AttackFX` (Combat/
  Effects.cs) already had arrow / muzzle+tracer / shell / explosion — the gap was just passing FxType.

- Oil cost on modern VEHICLE forms (user: "vehicles only"). `UnitConfig.ModernOilCost` (Cavalry→Tank +60,
  Catapult→RocketVehicle +80; infantry forms 0) + `UnitConfig.EffectiveCost(type, faction)` = base cost
  + surcharge when the unit will spawn as a modern vehicle (oil age + War Factory). Wired into
  `ProductionBuilding.Train` (Spend) AND the HUD train tile (RefreshTrain) so shown cost == spent cost.
- Catapult → RocketVehicle DONE: `ModernModel(Catapult)="RocketVehicle"`, `ModernMult` buff, FX via
  `ModernType(Catapult)=Tank` (tank-style shell+boom ≈ rocket). New `NE_RocketVehicle` model built +
  exported + wired in ModelLibrary (Fit 2.0, RotX -90, built facing -Y like the tank).
- `NE_Tower` DONE: the old downloaded watchtower was never swapped and sat sideways — replaced with a
  custom Blender stone watchtower (crenellations, lookout roof, NE_Team flag), Fit 3.4, RotX -90.

**STILL TODO (the riskier / larger parts):**
- Swap EXISTING units' models+stats on age-up / first War Factory (runtime rig swap + re-bind
  ModelAnimator — the risky part; deferred). Currently only newly-trained units modernize.
- Optionally remove `Gunner`/`Rifleman`/`Tank` from War Factory `Trainable` to fully commit the Merge.

## MAP + COMBAT PASS (DONE — verify in Unity)
- **Iron mountain — REAL bug was PLACEMENT, not the FBX.** The per-base iron used a fixed corner-ward
  offset `base+(-24,-22)`, which for edge bases pushed it OFF-SCREEN / into the forest border = "no iron
  mountain". Confirmed via a `[NE] Iron mountain ... spawning at` log + temporarily forcing primitives
  (which then appeared near the base). Fix: `SpawnNodeCluster` now places iron 15u INWARD (toward map
  centre, lake-sidestepped) so it's in the opening view. Restored the Blender `NE_IronMountain` model
  (FBX renders fine — it was never invisible, just off-screen); primitives remain a fallback.
- **Tower / "dark barrel"/"sideways"**: `NE_Tower.fbx` and the upright `NE_TownCentre.fbx` have IDENTICAL
  FBX axis settings, so the Tower was never sideways either — those reports were the off-screen/edge
  artefacts. (Left `Tower_T2`→`NE_Tower` from before.)
- **Iron mountain "tiny / off-ground / vanishes on gather"**: `ResourceNode.ShrinkToRemaining`/`Deplete`
  set `_model.localScale = Vector3.one * frac`, CLOBBERING the auto-fit scale that `ModelLibrary.FitAndGround`
  applied. Starting villagers auto-gather the nearest node, so the iron collapsed to the FBX's tiny native
  size on the very first mine. Fixed: capture the fitted scale once (`_baseScale`) and shrink RELATIVE to
  it. Also bumped `IronMountain` Fit 4.5→7. (General trap: any code that sets a model's localScale must
  respect the FitAndGround scale, not assume 1.)
- LESSON: when a spawned object "can't be seen", check WHERE it spawns (off-screen/edge/in-fog) and whether
  something RESCALES it, BEFORE assuming the model/FBX is broken. A debug log of the spawn position helps.
- **Settlement-on-lake**: the `Lakes` array in Bootstrap exactly matches the Blender lakes; bumped the
  base spawn lake margin 14→22 so the TC + its platform never touch water.
- **Oil wells** (`World/OilWell.cs`): NEW rare resource. `Bootstrap.SpawnOilWells()` scatters 38 wells
  (avoiding lakes/bases/centre). Plotted on the minimap (fog-gated, violet). Oil Pump can ONLY be built
  on a well — `BuildPlacer.TryPlace` rejects otherwise (toast via `BrandedHud.Notify`) and the ghost
  tints green-on-well / red-off-well while placing.
- **Battle pacing**: `CombatUnit.attackInterval` 1f→1.4f (slower swings ≈ +40% time-to-kill).

## UI RESTYLE — "Midnight & Gold" (DONE — verify in Unity)
Nuked the old bronze-ornament / indigo look. Done CENTRALLY so the whole HUD inherits it:
- `Theme` palette repainted: charcoal-glass surfaces (`Panel 0x151A23`, `Night 0x0B0E13`) + a single warm
  gold accent (`Bronze 0xE8B24A`), off-white text. Names kept stable. Team/faction colours unchanged.
- `Theme` button gradients flattened (near-solid), corner radii softened (Round 22 / RoundSoft 14).
- `UI.Corners` is now a NO-OP — retired the four bronze corner triangles that cluttered every panel.
- `BrandedHud.MakeBadge` simplified: clean dark disc + ONE thin accent ring (was halo+wash+sheen+double
  ring). `AnimateBadges` is a no-op now (badges no longer bob — read as floaty/unstable).
- `ResColor` gained Oil → violet `0x9A6BE0`.

**Models to build in Blender for this (reuse villager rig for humanoids; build AT ORIGIN):**
Archer (bow + Shoot clip), Cavalry rider (+ mount), Caravan (cart, static+wheels), Catapult (siege,
static + throwing arm), Tank (vehicle, custom — user wants custom), Gunner (modern infantry),
Rifleman (modern infantry), RocketVehicle (modern siege). Plus a Machine-Gunner look for Archer's
Age-3/4 form (could be a re-textured gunner).

---

## COMBAT EFFECTS plan (code-drawn, phone-cheap)
- **Muzzle flash:** brief emissive quad/sprite at the weapon tip on attack (reuse `MaterialUtil.SetGlow`).
- **Projectile:** a small fast-moving mesh/line from attacker to target (archers, gunners, tanks).
- **Explosion:** expanding fading sphere/ring for catapult/rocket/tank impacts (like `CommandPing`).
- Hook in `Combat.cs` `CombatUnit.TryAttack` (spawn FX on hit) + `ModelAnimator.Lunge` for the anim.

---

## NEXT STEPS (suggested order)
1. Confirm the modernization decision (A vs B) with the user.
2. Build the unit models in Blender (reuse the villager rig for humanoids; vehicles are static + a few
   moving parts). Clips: Idle/Walk + Shoot_OneHanded (ranged) / SwordSlash (melee). Build AT ORIGIN.
3. Wire each `ModelLibrary` key (units are skinned → no RotX; vehicles static → likely RotX=-90).
4. Implement the `UnitForm` framework + age-up swap.
5. Add combat FX.
6. Hold for the user's Unity check before committing/pushing.
