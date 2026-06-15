# Naija Empires — Milestones 0–1 (Playable Skirmish)

A zero-setup RTS sandbox built entirely from code — **no scene file or art assets needed.**
M0 proved the controls; **M1 makes it a winnable 1v1 skirmish vs an AI** (see GDD §6–§9, §14).

## What it does now (M1)
- Angled orthographic ("isometric") RTS camera — pan + zoom (mouse/keyboard or touch)
- Select your units (click / drag-box); right-click to **move, gather, or attack**
- Economy loop: villagers gather Yam / Timber / Iron → carry to your Town Centre
- **Population** (Houses raise the cap) and **Ages I–III** (advance to unlock units/buildings)
- **Build:** House, Barracks, Tower, Stable — each gated by Age + cost
- **Train:** select a Town Centre (villagers) or Barracks/Stable (army) to produce units
- **Combat triangle:** Spear ▶ Cavalry ▶ Archer ▶ Spear (1.6× counter bonus), with HP bars
- **Benin (you) vs Oyo (AI):** Benin's Towers are cheaper and tougher (civ bonus)
- **Enemy AI:** gathers, ages up, builds a barracks, trains an army, and attacks your base
- **Win/Lose:** destroy the enemy Town Centre to win — lose yours and it's over

### Look & feel (procedural — still placeholder, no art assets)
- **Land mass** — a green island surrounded by water, with scattered trees and rocks.
- **Resources read as objects** — Timber = trees, Yam = farm plots, Iron = rock piles.
- **Buildings are huts** — body + a diamond roof; bigger hut = Town Centre.
- **Procedural animation (code, no rigs):** units **bob** while walking, villagers **chop** while
  gathering and show a **carry cube**, soldiers **lunge** when they strike, and everything does a
  **hit punch** when damaged. Unit type is marked by a colored cap (white = spear, green = archer, gold = cavalry).

This is "juiced gray-box": it moves and reads like a game, but it's all primitives. The systems code is
unchanged when we later swap in real 2D-isometric art.

## Setup (5 minutes)

1. Install **Unity 6.3 LTS (6000.3.17f1)** via Unity Hub. **Universal 3D (URP)** or Built-in 3D both work.
2. Create a new project, then copy this repo's **`Assets/`** folder into the project (merge).
3. **Edit → Project Settings → Player → Active Input Handling → "Both"** (uses the legacy Input API).
4. **File → New Scene → Empty**, save as `Assets/Scenes/Skirmish.unity`.
5. **GameObject → Create Empty**, rename `_Bootstrap`, add the **`Bootstrap`** component.
6. Press **Play**.

The ground, both bases (yours bottom-left, enemy top-right), villagers, and resources spawn automatically.

## Controls

| Action | Input (editor) | Touch |
|---|---|---|
| Select | Left-click or drag a box | Tap |
| Move | Right-click ground | — |
| Gather | Right-click a resource pile | — |
| **Attack** | Right-click an enemy unit/building | — |
| Build | Click a Build button, then left-click to place | — |
| Train | Select your Town Centre / Barracks, click a Train button | — |
| Advance Age | Click "Advance to Age N" | — |
| Pan / Zoom | WASD / arrows · scroll wheel | Two-finger drag · pinch |

## Play a match
1. Your 4 villagers auto-gather. Let resources build.
2. **Advance to Age 2**, then **Build a Barracks**.
3. Select the Barracks → **Train Spearmen / Archers** (watch population — build Houses for more).
4. Box-select your army and **right-click the enemy base** to attack.
5. Destroy the enemy **Town Centre** for **VICTORY** (the AI is trying to do the same to you).

## Project layout
```
Assets/Scripts/
  Core/      Bootstrap, Types (enums/Cost), Faction, Match, ResourceType, MaterialUtil
  Config/    GameConfig (UnitConfig, BuildingConfig, Ages)
  Economy/   Economy
  Combat/    Health, Combat (CombatTriangle, CombatUnit, Tower)
  Factory/   Factories (UnitFactory, BuildingFactory)
  Units/     Unit, Villager, Selectable
  World/     ResourceNode
  Buildings/ Building, TownCentre, ProductionBuilding (+ PopCapProvider)
  Build/     BuildPlacer
  Input/     SelectionManager
  AI/        EnemyAI
  Camera/    RTSCameraController
  UI/        HUD (IMGUI)
```

## Balance knobs (all in `Config/GameConfig.cs`)
Unit/building costs, HP, damage, age requirements, the counter bonus (`CombatTriangle.Bonus`),
and the Benin civ discounts live here — tweak and re-play.

## Next milestone (from the GDD)
- **M2:** online multiplayer with friends — Photon room-code invites, server-authoritative state
  (the single-player loop here gets wrapped in networking, per GDD §11/§13).

## Notes
- iOS builds need a **Mac + Xcode + Apple Developer account ($99/yr)** — not needed to test in the Editor.
- M1 is single-player by design; multiplayer is M2.
- I couldn't compile this here (no Unity in my environment). If Unity flags anything on import,
  paste the error and I'll fix it immediately.
