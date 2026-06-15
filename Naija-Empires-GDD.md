# Naija Empires — Game Design Document

**Working title:** Naija Empires *(alternatives: "Age of Kingdoms: Naija", "Empires of the Niger")*
**Genre:** Real-Time Strategy (RTS) — base-building & army command
**Platform:** iOS first (iPhone/iPad); Android later
**Core mode:** Online multiplayer with friends
**Version:** GDD v0.1 (MVP blueprint)
**Date:** 2026-06-12
**Author:** Olukayode Soliu

---

## 1. Vision

> **Build your kingdom. Command your empire. Outsmart your friends.**

A mobile real-time strategy game where players grow a Nigerian/West African empire — gathering yam and iron, raising the Walls of Benin, training Oyo cavalry — and go to war **online against their friends**. It is an original homage to the classic *Age of Empires*-style RTS, set in a world that has never been given the genre it deserves: the empires of Nigerian history.

**Why it can win:** no serious RTS is built on Benin, Oyo, Sokoto, Kanem-Bornu, Nri, or the Hausa city-states. The history is rich, visual, and full of real military and architectural marvels. This is a recognisable genre with an unclaimed, culturally powerful setting.

### Design pillars
1. **Easy to touch, hard to master** — controls built for phones first, not ported from PC.
2. **Friends-first** — the fun is beating someone you know. Invite-and-play in under a minute.
3. **Authentically Nigerian** — real empires, architecture, music, and languages, treated with respect.
4. **Fair fights** — competitive multiplayer with no pay-to-win.

---

## 2. Target Platform & Audience

- **Primary:** iOS (iPhone/iPad). **Note:** iOS builds require a **Mac + Xcode** and an **Apple Developer Program** membership ($99/yr).
- **Secondary (later):** Android (Unity makes this largely a re-export).
- **Audience:** mobile strategy players (Clash of Clans, Iron Marines, Rise of Kingdoms), the African diaspora, and players hungry for fresh RTS settings. Ages 14+.

---

## 3. Core Gameplay Loop

The classic, proven RTS loop — tuned for short mobile matches (10–20 min):

```
Gather resources  →  Build & expand base  →  Advance an Age
       ↑                                            ↓
   Defend / scout   ←   Train army   ←   Unlock units & upgrades
                          ↓
                   Attack the enemy  →  Destroy their Town Centre = WIN
```

1. Start with a **Town Centre** and a few **Villagers**.
2. Villagers gather **Yam, Timber, Iron**.
3. Spend resources to build **Houses** (population), a **Barracks** (army), and **defences**.
4. **Advance through Ages** to unlock stronger units, buildings, and a **Wonder**.
5. Build an army, attack, and **destroy the enemy Town Centre** (or last empire standing).

---

## 4. Game Modes

| Mode | MVP? | Description |
|---|---|---|
| **Online vs Friends** | ✅ Core | Create a match, share a **room code**, friends join. 1v1 first; 2v2 / 4-player FFA next. |
| **vs AI (offline)** | ✅ | Practice / tutorial / play without internet. Also the development backbone (see §13). |
| **Ranked matchmaking** | 🚧 Later | Skill-based pairing, ladder, seasons. |
| **Co-op vs AI** | 🚧 Later | Team up with friends against bots. |

---

## 5. Civilizations (Nigerian Empires)

Each civ plays differently via a **unique unit** and a **bonus**. **MVP ships with 2**; the rest are the post-launch roadmap.

| Civilization | Identity | Unique unit | Bonus | MVP |
|---|---|---|---|---|
| **Benin Empire** | Defensive fortress | *Iyokuo* royal guard | Walls & towers cheaper and stronger (Walls of Benin / Sungbo's Eredo) | ✅ |
| **Oyo Empire** | Cavalry aggression | *Eso* cavalry | Cavalry trained faster & cheaper | ✅ |
| **Sokoto Caliphate** | Economy & faith | Mounted lancer | Faster Age advance; trade bonus | 🚧 |
| **Kanem-Bornu** | Trade & range | Camel archer | Markets generate extra Cowries | 🚧 |
| **Hausa City-States** | Walls & commerce | Kano archer | Strong economy behind great walls | 🚧 |
| **Nri Kingdom** | Spiritual / support | Priest | Faster villager work; healing | 🚧 |

---

## 6. Resources & Economy

| Resource | Source | Used for |
|---|---|---|
| **Yam** (food) | Farms, hunting | Villagers, infantry, advancing Age |
| **Timber** (wood) | Forests | Buildings, walls, archers |
| **Iron** (Nok) | Mines | Military units, upgrades, siege |
| **Cowries** (trade) 🚧 | Markets, trade routes | Premium units, wonders *(post-MVP)* |

MVP runs on **Yam, Timber, Iron**. Cowries/trade arrives with the Market and the Sokoto/Kanem-Bornu civs.

---

## 7. Ages (Progression)

Advancing an Age costs resources + a required building, and unlocks the next tier:

1. **Age I — Nok Dawn:** founding, gathering, first villagers & defences.
2. **Age II — Rise of Kingdoms:** barracks, core infantry, walls & towers.
3. **Age III — Empire & Trade:** cavalry, archers' range upgrades, Market, **Wonder**.
4. **Age IV — Age of Resistance** 🚧: gunpowder-era units (Dane guns), siege — *post-MVP*.

**MVP ships Ages I–III.**

---

## 8. Buildings (MVP)

| Building | Role |
|---|---|
| **Town Centre** | Trains villagers; drop-off; **its destruction = defeat** |
| **House** | Raises population cap |
| **Barracks** | Trains infantry; gate to military |
| **Stable** | Trains cavalry (Age III) |
| **Wall / Gate / Tower** | Defence (Benin's specialty) |
| **Market** 🚧 | Trade & Cowries (post-MVP) |
| **Wonder** | Win-condition alternative + Age-III prestige (e.g., Walls of Benin, Great Mosque of Kano) |

---

## 9. Units (MVP) & Combat

**Units:** Villager (gather/build/repair), Spearman, Archer, Cavalry (Age III), + each civ's unique unit.

**Combat = a rock-paper-scissors triangle** (readable, balanced, mobile-friendly):

```
Spearman  ▶ beats ▶  Cavalry  ▶ beats ▶  Archer  ▶ beats ▶  Spearman
```

Plus walls/towers for defence, and counters that reward scouting and composition over raw numbers. Hard **unit cap per player** (≈50 in MVP) — essential for performance and netcode (see §11).

**Win conditions:** (1) destroy all enemy Town Centres, or (2) build & defend a **Wonder** for a countdown.

---

## 10. Controls & UX (touch-first — make-or-break)

RTS fails on mobile when PC controls are ported. Reference standard: **Iron Marines** and **Bad North**.

- **Tap** to select a unit; **double-tap** to select all of that type on screen.
- **Drag a box** (two-finger or lasso) for multi-select.
- **Tap-to-move / tap enemy to attack**; long-press for a **command radial** (patrol, hold, gather).
- **Pinch to zoom, two-finger drag to pan**; edge mini-map to jump.
- **Big, thumb-reachable build menu**; production from building cards with clear queue.
- **One-tap "rally" and "select all army"** buttons — reduce micro.
- Generous hit targets, haptic feedback, colour-blind-safe team colours.

A throwaway **controls prototype is Milestone 0** — we prove the feel before building systems on top.

---

## 11. Multiplayer Architecture (the hard core)

Multiplayer RTS is the central technical risk. The plan keeps it shippable for a solo dev.

### Networking model — **server/host-authoritative state synchronisation**
- One authoritative simulation (a **host player via relay**, or a light dedicated server later) owns the truth: resources, combat results, unit positions.
- Clients send **commands** (move, build, train); the authority validates, simulates a fixed **tick (10–15 Hz)**, and **syncs state** back.
- **Why not deterministic lockstep (what PC AoE uses)?** Lockstep sends only inputs and is bandwidth-light but demands a *perfectly deterministic* simulation (fixed-point math, identical across devices) — very hard and slow to debug. We **defer lockstep** until we need huge armies; state-sync with a **low unit cap** is the faster, safer path to "play with friends."

### Stack
- **Engine:** Unity 2022 LTS (C#).
- **Transport/relay:** **Photon Fusion** *(or Unity Netcode for GameObjects + Unity Relay/Lobby)* — both give NAT-punch **relay** so no port-forwarding, and **room-code** join for friends. Free tiers cover friends-scale play.
- **Session flow:** Create match → get **room code** → friends enter code → lobby (pick civ/colour) → start. Anonymous sign-in for MVP; Game Center / accounts later.

### Latency & integrity
- **Client-side prediction** for selection/UI responsiveness; authority reconciles.
- **Command buffering** + a small input delay to smooth jitter.
- **Server-authoritative resources & combat** → no client can fake economy or kills (basic anti-cheat).
- Unit cap + tick budget keep bandwidth sane on Nigerian mobile networks (design for 3G/4G, lossy links).

---

## 12. Art Direction & Audio

- **Visual style:** **2D isometric** sprites on tile maps (like AoE2) — far cheaper and smoother on phones than 3D, and ages beautifully.
- **Aesthetic:** Benin bronze motifs in UI frames, adire/ankara patterns, warm earth tones, lush Niger-delta and savannah maps.
- **Audio:** talking drums (dùndún), shekere, flutes; distinct themes per empire. **Unit voice lines in Yoruba, Hausa, Igbo, and Pidgin.**
- **Pipeline:** start with asset-store isometric kits to prototype fast → commission original Nigerian-themed art for the vertical slice.

---

## 13. Build Strategy & Tech Stack

**Golden rule for a solo dev:** build the game **single-player first**, then network it. The simulation, economy, combat, and AI all work offline against a bot — *then* we wrap it in multiplayer. This de-risks the hardest part.

| Area | Tooling |
|---|---|
| Engine | Unity 2022 LTS (C#) |
| Multiplayer | Photon Fusion / Unity Gaming Services (Relay, Lobby, Auth) |
| Art | Aseprite (sprites), Tiled (maps), asset-store kits to start |
| Audio | Commissioned + Nigerian sample packs |
| Source control | Git + **Git LFS** (binary assets) |
| iOS build | Mac + Xcode + Apple Developer ($99/yr) |
| Backend (later) | Unity Cloud / PlayFab (accounts, leaderboards, seasons) |

---

## 14. Development Roadmap (MVP → playable with friends)

Realistic solo, part-time estimate. Ambitious but achievable if scope holds.

| Milestone | Goal | Est. |
|---|---|---|
| **M0 — Controls prototype** | Isometric map, camera, select a villager, gather, build one building. Prove touch feel. | 2–3 wks |
| **M1 — Core loop (single-player)** | Full economy (3 resources), 4 buildings, 3 units, combat triangle, **AI opponent**, Age I–III, win condition. 1 civ (Benin). | 5–7 wks |
| **M2 — Multiplayer** | Photon, **room-code friend invite**, 1v1 online, authoritative resources/combat, unit cap, reconnect. | 5–7 wks |
| **M3 — Content & polish** | 2nd civ (Oyo), a Wonder, balance pass, UX polish, tutorial, audio. | 4–5 wks |
| **M4 — Ship iOS** | TestFlight beta with friends → App Store. | 3–4 wks |

➡️ **~5–6 months** to a polished, friends-playable MVP. (Faster with help on art/netcode.)

---

## 15. Monetization (post-MVP)

Competitive multiplayer → **never pay-to-win.** Options: **premium one-time purchase**, or free with **cosmetic** civ skins / map themes / a battle pass. Decide after the core is fun. MVP is free for friends/testers.

---

## 16. Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Touch RTS controls feel bad | M0 prototype proves feel before anything else; copy Iron Marines patterns |
| Multiplayer desync / lag | Server-authoritative state-sync, low unit cap, fixed tick, prediction; defer lockstep |
| Scope explosion (solo dev) | Strict MVP: 2 civs, 3 Ages, 1 map, 1v1 first |
| Art cost/time | 2D isometric + asset-store base, commission later |
| iOS friction | Budget Mac + $99 Apple account early |
| Balance | Keep the RPS triangle simple; playtest with friends weekly |

---

## 17. Legal / IP

- **Original IP.** Do **not** use the "Age of Empires" name, art, sound, or assets — Microsoft trademark. Game *mechanics* aren't protected; an original game in the genre is fine.
- Nigerian empires, figures, and architecture are historical/public domain — represent them **accurately and respectfully**; consult sources and, ideally, Nigerian historians/cultural advisors.

---

## 18. Immediate Next Steps

1. **Approve this GDD** (or adjust civs/scope/name).
2. Set up **Unity + Git LFS** project and an iOS signing identity.
3. Build **Milestone 0** — the isometric controls prototype — and test the feel on a real iPhone.
4. From there, march the roadmap: core loop → multiplayer → content → ship.

*Naija Empires — GDD v0.1. All figures, names, and scope are a starting point, open to iteration.*
