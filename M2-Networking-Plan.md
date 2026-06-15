# Naija Empires — M2 Multiplayer Plan (Photon Fusion 2, **Host-authoritative**)

> Detailed engineering companion to `unity-m0/M2-MULTIPLAYER.md` (the product/GDD-level plan).
> Architecture decision: **host-authoritative**, per GDD §11/§13 and the repo's existing M2 doc.

**App ID:** `d34fcd29-31d5-44fd-a25c-8e471d1bdfe3` (claim 100 free CCU on this app).
**Mode:** Fusion 2 **Host** (`GameMode.Host` / `AutoHostOrClient`). One peer = host = single source of truth.

---

## The model in one paragraph

The **host runs essentially the M1 simulation** — economy, movement, combat, spawning, win/lose —
on host-owned `NetworkObject`s, and the resulting state replicates to the client via `[Networked]`
properties + `NetworkTransform`. The **client renders and sends commands**: it never mutates game
state locally. A client command (move/gather/attack/build/train) is an **RPC to the host**; the host
**validates** (does this player own that unit? can they afford it?) and applies it. Selection, camera,
ghost preview, and juice (bob/lunge/health bars) stay **client-local and are never networked**.

This maps cleanly onto M1: the central `Match`/`Bootstrap`/`Faction.All` simulation stays centralized
— it just runs *on the host under StateAuthority* instead of locally.

---

## Authority model

| Concern | Authority | Mechanism |
|---|---|---|
| All units/buildings (spawn, move, combat, death) | **Host** (StateAuthority) | host `Runner.Spawn`; logic runs only `if (Object.HasStateAuthority)` in `FixedUpdateNetwork` |
| Per-player input over *their* units | that player (InputAuthority) | host spawns a player's units with `inputAuthority: playerRef` |
| Economy / pop / age | **Host** holds it; client reads | `[Networked]` on `PlayerState` (one per player, host-owned) |
| Commands (move/gather/attack/build/train) | client requests → host decides | `[Rpc(InputAuthority → StateAuthority)]` on a per-player command channel |
| Win/lose, town-centre tally | **Host** | `[Networked]` on `MatchState` (single, host-owned) |
| Selection, camera, ghost, juice | client-local | not networked at all |

**Why RPCs, not Fusion's input struct:** RTS commands are discrete events (issue once), not per-tick
continuous input. RPCs fit; `OnInput`/`GetInput<T>()` would be awkward. (Camera/selection need no
network input.)

---

## Script-by-script refactor map

| M1 script | M2 change (host-authoritative) |
|---|---|
| `Core/Match.cs` (static) | Becomes `MatchState : NetworkBehaviour`, **host-owned singleton**. Town-centre tally + `[Networked] Over/WinnerId`. Per-faction economy moves to `PlayerState`. Logic runs on host only. |
| `Economy/Economy.cs` (plain C#) | Fields → `[Networked]` on `PlayerState : NetworkBehaviour` (host-owned, one per player/faction). `Spend/Add/AddPop` run **on host**; client HUD reads `[Networked]`. |
| `Factory/Factories.cs` | Root → registered **NetworkObject prefab shell** (collider + `Faction` + `Health` + `Unit`/`Building` + `NetworkTransform`). Procedural model/primitives move into `Spawned()` as **local cosmetic** children (each client builds its own visuals). Factory methods callable **only on host**, do `Runner.Spawn(prefab, pos, rot, inputAuthority: ownerRef)`. |
| `Units/Unit.cs` | `MonoBehaviour` → `NetworkBehaviour`. Movement in `FixedUpdateNetwork`, gated `if (HasStateAuthority)`, using `Runner.DeltaTime`. `moveTarget` → `[Networked]`. Add `NetworkTransform`. Client just sees replicated transform. |
| `Combat/Combat.cs` (`CombatUnit`,`Tower`) | `NetworkBehaviour`; acquisition + attack run **host-only** in `FixedUpdateNetwork`. No cross-authority problem — host owns attacker *and* victim, so `target.TakeDamage()` is a direct host call. Cooldown via `TickTimer`. |
| `Combat/Health.cs` | `Current` → `[Networked]`. `TakeDamage` runs host-only; death → `Runner.Despawn`. Health-bar `OnGUI` stays (reads `[Networked] Current`). No damage RPC needed (host owns everything). |
| `Buildings/*` | `NetworkBehaviour`, host-only logic. `ProductionBuilding.Train()` runs on host → `Runner.Spawn` new unit with the owner's InputAuthority. `TownCentre` spawn/death → host updates `MatchState`. |
| `Input/SelectionManager.cs` | Stays **client-local** for selection. Commands no longer call unit methods directly — they package `{targetNetId, point, kind}` and fire a **command RPC to the host** via the local `PlayerState`. Still filters to owned units. |
| `Build/BuildPlacer.cs` | Ghost stays local. On place → **RPC host** `RequestBuild(kind, pos)`; host validates age+cost on that `PlayerState`, then `Runner.Spawn`. |
| `Core/Faction.cs` | `All` registry stays (host uses it for target acquisition; populated in `Spawned`). `FactionId` stays `{Player, Enemy}` for 1v1; host maps each `PlayerRef` → a `FactionId`. |
| `Core/Bootstrap.cs` | **Split:** (a) `LocalScene` — ground/water/light/camera/decor/nodes, built deterministically on **every** client (cosmetic). (b) host-only `MatchBootstrap` — on game start the **host** spawns both bases, both sides' starting villagers, `PlayerState`×2, `MatchState`, assigning InputAuthority per seat. Nearly the M1 `Bootstrap` body, but via `Runner.Spawn` and host-gated. |
| `AI/EnemyAI.cs` | Runs **on host only**. Used to fill seat 2 until a human joins (bot-fill); disabled once a human takes the seat. |
| `World/ResourceNode.cs` | **M2 first cut:** local + deterministic visuals; gathering credited on **host** to the owning `PlayerState`. Networked depletion is a later refinement. |

---

## Lobby / connection (room code)

- Room code = Fusion **Session Name**. Host: `StartGame(GameMode.Host, SessionName=code)`. Client:
  `GameMode.Client, SessionName=code`. `AutoHostOrClient` for quick-play.
- Lobby UI reuses `Theme`/`UIKit`: create-room (shows code) / join-by-code / ready-up. Game starts
  when both seats ready; host runs `MatchBootstrap`.

---

## Open tradeoffs (flagged, not silently decided)

1. **Resource nodes** local-cosmetic vs networked depletion → local for M2.
2. **`FactionId {Player,Enemy}`** hard-codes 2 seats → fine for 1v1; revisit for >2 players.
3. **Host presence** — host leaving ends/needs-migrating the match. Host migration is out of scope
   for M2 (acceptable for "friends 1v1 by room code").
4. **Commands as RPC vs input struct** → RPC (discrete commands). Movement smoothing handled by
   `NetworkTransform` interpolation on the client.

---

## Build order (tasks #2–#8)

1. Connection + lobby (room code) — two players into one session, ready-up.
2. `PlayerState` + `MatchState` (host-owned `[Networked]`).
3. Prefab shells + host spawning (factories → `Runner.Spawn`).
4. Movement host-side + `NetworkTransform`; client command RPCs.
5. Combat + health host-side (direct calls; no cross-authority RPC).
6. Split Bootstrap (local cosmetic vs host `MatchBootstrap`) + host-only AI bot-fill.
7. Two-client end-to-end verification.

Each stage compiles in Unity before the next begins.
