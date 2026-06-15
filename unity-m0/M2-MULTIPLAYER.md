# M2 — Online Multiplayer (Photon Fusion 2)

Goal (GDD §11/§13): friends play 1v1 via a **room code**, **host-authoritative** real-time sync (NOT lockstep). Build networking *around* the working single-player sim, incrementally.

## Product & topology
- **Photon Fusion 2**, **Shared/Host mode** — one peer is authority (host) and owns the simulation; the other is a client.
- Host runs the game logic (economy, AI-or-none, combat, spawning). Clients **send input/commands**; host **validates + applies**; resulting state replicates to clients via `[Networked]` properties + `NetworkBehaviour`.

## What gets networked
| System | Approach |
|---|---|
| Units / buildings | `NetworkObject` spawned by host via `Runner.Spawn`; transform + key state `[Networked]` |
| Commands (select is local; move/gather/attack/build/train) | client → host RPC / input struct; host authorizes against that player's economy/ownership |
| Economy (Yam/Timber/Iron, pop, age) | `[Networked]` per-player on a `PlayerState` NetworkObject |
| Match state (win/lose) | `[Networked]` on a session object |
| Lobby | room code = Fusion **Session Name**; create/join + ready-up |

Selection, camera, minimap, juice (bob/lunge) stay **local/client-side** — never networked.

## Build phases
1. **Connect** — NetworkRunner bootstrap; create/join a room by code; show both players in a lobby. *(branded lobby UI, reuse Theme/UIKit)*
2. **Sync one thing** — host spawns a `NetworkObject` test unit; client sees it move. Proves the pipe.
3. **Network the economy + ownership** — per-player `PlayerState`; resources sync.
4. **Network units/buildings** — convert `UnitFactory`/`BuildingFactory` spawns to `Runner.Spawn`; sync position + health + state.
5. **Command routing** — client commands → host authorization → applied on host → replicated.
6. **Match flow** — both players' Town Centres; win/lose synced; (AI optional in MP).

Single-player Bootstrap stays as an **offline/practice** mode; MP path is a parallel entry that runs the same systems under the host.

## Setup checklist (you, in/around the editor)
- [ ] Create a free Photon account → **dashboard.photonengine.com**.
- [ ] **Create a New App → Photon SDK: Fusion** → name "Naija Empires" → copy the **App ID**.
- [ ] Download the **Fusion 2** SDK (`.unitypackage`) from the Photon site (Fusion → SDKs, Unity).
- [ ] Unity: **Assets → Import Package → Custom Package…** → import all.
- [ ] **Fusion Hub** auto-opens → paste the **App ID** → Save. (Reopen via Tools/Photon → Fusion → Fusion Hub.)
- [ ] Tell me it's in → I start Phase 1.

*Unity 6 (6000.4.11f1) is supported by Fusion 2.*
