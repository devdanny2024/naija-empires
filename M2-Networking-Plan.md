# Naija Empires — M2 Multiplayer Plan (Photon Fusion 2, Shared mode)

**Topology:** Fusion 2 **Shared mode** — Photon cloud relays, one client is "Master".
**App ID:** `d34fcd29-31d5-44fd-a25c-8e471d1bdfe3` (claim 100 free CCU on this app).
**Scope:** Full networked 1v1 skirmish.

---

## The core problem this refactor solves

The M1 game is built for a single local simulation:

- **Everything is procedural** — `UnitFactory` / `BuildingFactory` / `Bootstrap` create objects with
  `new GameObject()` + `AddComponent`. Fusion can only replicate objects spawned via
  `Runner.Spawn(prefab)` from a `NetworkObject` prefab registered in `NetworkProjectConfig`.
- **`Match` and `Economy` are static / plain C#** — no replication.
- **Movement / combat run on `Time.deltaTime` in `Update`** — Fusion ticks in `FixedUpdateNetwork`.
- **One peer (`Bootstrap`) builds *both* bases + the enemy AI** — in MP each player must spawn
  and own their own side.

So M2 is a real refactor, not an annotation pass. The plan below keeps your visuals and game
feel intact while moving *state and spawning* onto the network.

---

## Authority model (the decisions that drive everything)

| Concern | Owner | Mechanism |
|---|---|---|
| A player's units & buildings | that player (State + Input authority) | `Runner.Spawn` on that client gives it authority in Shared mode |
| A player's economy / pop / age | that player | `PlayerState` NetworkBehaviour, one per player |
| Win condition / town-centre counts | Master client | single shared `MatchState` NetworkBehaviour |
| Damage to an enemy object | the **victim's** authority | attacker sends RPC → victim's StateAuthority applies it |
| Unit position | the unit's owner | owner integrates in `FixedUpdateNetwork`, `NetworkTransform` replicates |

Key Shared-mode fact: the client that calls `Runner.Spawn` owns that object. So each player
spawns their own town centre, villagers, buildings, and trained units — and naturally has the
authority to move/command them. You never command an object you don't own.

---

## Script-by-script refactor map

| M1 script | M2 change |
|---|---|
| `Core/Match.cs` (static) | **Split.** Per-faction economy → `PlayerState : NetworkBehaviour` (per player). Win/lose + town-centre tally → `MatchState : NetworkBehaviour` (one shared). `Match.Econ(id)` callers → look up the relevant `PlayerState`. |
| `Economy/Economy.cs` (plain C#) | Fields become `[Networked]` props on `PlayerState`. `Changed` event → HUD polls `[Networked]` each frame (or `OnChanged`). Same authority as owning player, so `Spend`/`Add`/`AddPop` run locally on the owner. |
| `Factory/Factories.cs` | Root becomes a registered **NetworkObject prefab shell** (collider + `Faction` + `Health` + `Unit`/`Building` + `NetworkTransform`). The procedural model/primitive children move into `Spawned()` as **local cosmetic** children (each client builds its own visuals — not networked). Factory methods → `Runner.Spawn(prefab, pos, …)` then init `[Networked]` fields on the authority. |
| `Units/Unit.cs` | `MonoBehaviour` → `NetworkBehaviour`. Movement to `FixedUpdateNetwork()` using `Runner.DeltaTime`. `moveTarget` → `[Networked] Vector3 MoveTarget` + `[Networked] bool HasTarget`. Add `NetworkTransform`. |
| `Combat/Combat.cs` (`CombatUnit`, `Tower`) | `NetworkBehaviour`, target acquisition in `FixedUpdateNetwork`. `TakeDamage` on an enemy → `Health.RpcApplyDamage` to victim's StateAuthority. Cooldowns use `TickTimer`. |
| `Combat/Health.cs` | `Current` → `[Networked]`. `TakeDamage` only valid on StateAuthority; expose `[Rpc(Sources=All, Targets=StateAuthority)] RpcApplyDamage(float)`. Death → `Runner.Despawn(Object)` (not `Destroy`). Health bar OnGUI stays (reads `[Networked] Current`). |
| `Buildings/*` (`Building`, `ProductionBuilding`, `TownCentre`) | `NetworkBehaviour`. Production `Train()` → owner calls `Runner.Spawn` for the new unit. `TownCentre` spawn/despawn → RPC `MatchState` to inc/dec that faction's count. |
| `Input/SelectionManager.cs` | Mostly unchanged — already filters to `OwnedByPlayer`. Commands call methods on owned units (which now write `[Networked]` targets under input authority). Stays client-local. |
| `Build/BuildPlacer.cs` | Ghost stays local. `TryPlace` → spend on local `PlayerState`, then `Runner.Spawn` the building (owner = local player). |
| `Core/Faction.cs` | Keep the `All` registry (populated in `Spawned`/`OnEnable`) — works fine since networked objects exist on every client. `FactionId` becomes per-player assignment, not just Player/Enemy. |
| `Core/Bootstrap.cs` | **Split into two.** (a) `LocalScene` — ground, water, light, camera, decor, resource nodes: built deterministically on every client (cosmetic, identical seeds). (b) `PlayerSpawn` — on join, each player spawns *their own* town centre + 4 villagers at the base assigned by player index (Master = base A, 2nd = base B). |
| `AI/EnemyAI.cs` | Disabled in PvP (the enemy is a human). Kept for single-player / bot-fill only, gated on whether seat 2 is filled by a human. |
| `World/ResourceNode.cs` | **M2 first cut:** keep local + deterministic (each player gathers into their own economy; node depletion is cosmetic). Networked depletion is a later refinement — noted, not done now. |

---

## Open tradeoffs (flagging, not deciding silently)

1. **Resource nodes local vs networked.** Local is far simpler and correct for "each player mines
   their own pile." Downside: depletion visuals can differ between clients. Recommend local for M2,
   revisit if shared/contested nodes become a design goal.
2. **`FactionId` enum is `{Player, Enemy}`.** Fine for 1v1 but hard-codes two seats. Keeping it for
   M2; if you want >2 players later this becomes a `PlayerRef`-keyed model.
3. **Determinism.** Shared mode is *not* lockstep-deterministic (that's Quantum). State is
   replicated, not recomputed — so this is robust to float drift but not anti-cheat-grade. Matches
   the "ship a 1v1 on free CCU" goal.

---

## Build order (matches the task list)

1. Connection layer — two clients into one session.
2. `PlayerState` + `MatchState`.
3. Prefab shells + networked spawning.
4. Networked movement + commands.
5. Networked combat + health.
6. Split Bootstrap + AI handling.
7. End-to-end two-client verification.

Each stage is compiled in Unity before the next begins.
