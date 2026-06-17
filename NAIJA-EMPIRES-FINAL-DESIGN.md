# Naija Empires — Final Design & Version 1 Scope
*West-African real-time strategy, built on the Rise of Nations design language, for the Nigerian niche.*

This document defines two things:
1. **The Full Final Vision** — the complete game we are building toward (our "north star").
2. **Version 1 (V1)** — the strong, shippable first version that establishes the niche.

It maps Rise of Nations (RoN) systems onto an authentic West-African setting and grounds everything in what is **already built** in `unity-m0` today.

---

## 0. Positioning — why this wins the Nigerian niche
- **Authentic, pre-colonial West-African empires** — Benin, Oyo, Sokoto, Kanem-Bornu — with real history, real units, real wonders (the Walls of Benin are literally the largest earthwork on earth). No other RTS owns this.
- **Cultural identity as a feature**: Bronze-&-Indigo art direction, talking-drum & kora audio, Yoruba/Hausa/Edo/Kanuri unit naming and callouts, an educational "history" layer.
- **Mobile-first & offline-friendly**, with **NGN/Paystack** monetization — meets the market where it is.
- **RoN depth, AoE readability**: borders, attrition, territory, research and economy give strategic depth, but the UI keeps it approachable on a phone.

---

## 1. RoN → Naija Empires mechanic map

| RoN system | Naija Empires adaptation | Status today |
|---|---|---|
| 6 resources (Food/Timber/Wealth/Metal/Knowledge/Oil) | **Yam, Timber, Iron, Cowries (wealth/trade), Knowledge (Ifá/scholarship)** — 5, readable on mobile | Have 3 (Yam/Timber/Iron) |
| Commerce Limit (gather cap) | **Trade Limit** — caps gather rate; raised by Markets, research, wonders, rare goods | New |
| 8 historical ages | **4 West-African eras**: Village → Iron (Nok) → Kingdom (Benin/Ife bronze) → Empire (Sokoto/Bornu golden age) | Have 4 ages |
| Library, 4 branches ×7 | **House of Wisdom** research: Military / Economy / Civic / Knowledge branches | Have University (troop research) |
| Territory, borders, attrition | **Influence zones**: build only in your land; enemies take **attrition** inside your borders | Have territory zones |
| Rare resources | **Trade goods**: Kola, Gold, Salt, Ivory, Horses, Palm — income + a passive bonus, claimed by Merchants | Have "hidden resources" stub |
| Cities + radius | **Town Centres / settlements** that expand borders; Civic raises settlement limit | Have Town Centres |
| Market, Caravans, Merchants | **Market + Caravans** (Cowries between towns) + **Merchants** (claim trade goods) | New |
| University + Scholars | **House of Wisdom + Scholars** produce Knowledge | Partial (University) |
| Nations + unique power + unique unit | **4 empires, asymmetric powers + 1 unique unit each** | Have civ perks (color/stat) |
| Wonders | **West-African wonders** (Walls of Benin, Great Mosque, Sungbo's Eredo…) | New |
| Governments + Patriots | **Council/throne path** (Warrior-king / Merchant-council / Scholar-emirate) + a hero (Oba/Alaafin/Sultan/Mai) | New (final vision) |
| Scouts / Generals / Supply | **Scout** (vision), **War Chief** (general), **Bearer** (anti-attrition supply) | New |
| Quick Battle / MP / Conquer the World | **Skirmish / Online FFA / Conquer West Africa campaign** | Have skirmish; MP scaffolded |
| Victory: capital/territory/wonder/score | Same four routes | Have last-standing |

---

## 2. THE FULL FINAL VISION

### 2.1 Economy (5 resources + Trade Limit)
- **Yam** (food): citizens, early units, aging. From farms (villager-worked) + yam nodes.
- **Timber**: buildings, ships, early upgrades. From forests.
- **Iron**: military units/upgrades. Unlocks in the **Iron era**.
- **Cowries** (wealth): trade currency — buy/sell goods at Market, train Merchants/Scholars, elite units. From Caravans, taxation, trade goods.
- **Knowledge**: research + aging. From the **House of Wisdom** (Scholars). Cannot be traded.
- **Trade Limit** caps gather rate per resource; raised by Markets, Commerce research, wonders, and trade goods — the core macro tension.

### 2.2 Ages (4 eras, West-African history)
1. **Village Era** — settlement, farming, basic spear/bow, scouting.
2. **Iron Era (Nok)** — iron-working; metal economy + stronger infantry.
3. **Kingdom Era (Benin/Ife)** — bronze casting, cavalry, walls, wonders, markets.
4. **Empire Era (Sokoto/Bornu Golden Age)** — trans-Saharan trade height, elite cavalry, **early firearms (Bornu muskets)**, grand wonders.
Aging requires resources **and** a minimum number of completed researches (RoN gating).

### 2.3 The 4 Empires (asymmetric)
| Empire | Power | Identity | Unique unit |
|---|---|---|---|
| **Benin Kingdom** | *Power of Bronze* | Defensive juggernaut — cheaper/tougher walls & towers, strong border attrition (the Walls/Moat of Benin) | **Bronze Warrior** (armored heavy infantry) |
| **Oyo Empire** | *Power of Cavalry* | Mobile raiders — faster, cheaper cavalry; raid economy | **Eso Cavalry** (elite light horse) |
| **Sokoto Caliphate** | *Power of Scholarship* | Knowledge & faith — faster research, disciplined armored lancers | **Jihad Lancer** (armored cavalry) |
| **Kanem-Bornu** | *Power of Trade* | Trans-Saharan economy + early gunpowder | **Kanuri Musketeer** (early firearms) |

### 2.4 Research — The House of Wisdom (4 branches)
- **Military** — pop limit, unlock buildings, cheaper units/upgrades.
- **Economy** — Trade Limit, market tools, caravan income.
- **Civic** — settlement limit, **border size**, defensive tech.
- **Knowledge** — research speed, line of sight, production buildings.
Troop upgrades (already built) live under Military; each empire's unique unit has its own upgrade line.

### 2.5 Territory, borders, attrition
- Buildings must sit **inside your borders** (or an ally's). Borders grow from Town Centres, forts/temples, Civic research, and certain wonders/goods.
- **Attrition**: enemy units inside your borders lose HP over time unless protected by a **Bearer** (supply) or specific tech. Makes defense — and Benin — genuinely powerful.
- Capturing a Town Centre shifts borders and can trigger capital/conquest timers.

### 2.6 Trade goods (rare resources, Nigerian)
Map objects giving income + a passive bonus, claimed by a **Merchant** (or worked nearby):
Kola Nut, Gold, Salt, Ivory, Horses (cheaper cavalry), Palm Produce, Leather, Indigo, Pepper. Each tilts composition/economy — claim early.

### 2.7 Buildings
Town Centre, House, Farm, House of Wisdom (Knowledge/research), Market (Caravans/Merchants/buy-sell), Barracks, Stable, Forge, Wall, Tower, Fort (general/garrison/borders), Shrine/Mosque (borders & faith tech), Wonder, War Canoe Dock *(coastal/Niger — final vision)*.

### 2.8 Units (combined arms)
- **Civilian**: Villager (gather+build), Scholar, Merchant, Caravan.
- **Support**: Scout (vision), War Chief (general — auras/abilities), Bearer (supply/anti-attrition), Spy *(final vision)*.
- **Infantry**: Spearman, Archer, Swordsman, Musketeer (late).
- **Cavalry**: Light Horse, Heavy Cavalry, empire uniques.
- **Siege**: Battering Ram, War Drums (morale) *(final vision)*.
- **Naval** *(final vision)*: Niger-Delta **War Canoes**, transport.
Units upgrade across ages; most cost population; same-unit spam ramps in cost.

### 2.9 Wonders (West-African)
Walls of Benin (defense/attrition), Sungbo's Eredo (borders), Great Mosque of Kano (Knowledge/faith), Aafin of Oyo (military/leadership), Nok Terracotta (culture/pop), Trans-Saharan Caravanserai (trade). Empire-wide bonuses + **Wonder points** (Wonder victory).

### 2.10 Leadership paths *(final vision, RoN governments)*
Choose a path at the Council across the eras: **Warrior-King / Merchant-Council / Scholar-Emirate**, each granting permanent bonuses + a **Patriot hero** (Oba, Alaafin, Sultan, Mai).

### 2.11 Modes & victory
- **Skirmish** (vs AI), **Online FFA** (Photon, up to 4), **Conquer West Africa** (Risk-like campaign over a West-African map), **Historical scenarios** (Fall of Oyo, Defense of Benin, Sokoto Jihad).
- **Victory**: Conquest/Capital, Territory control, Wonder points, Score/time.

### 2.12 Identity & live
Talking-drum + kora audio, language callouts, history codex, NGN/Paystack store, cloud saves & leaderboards (post-MP).

---

## 3. VERSION 1 — the strong first release
*Goal: a complete, polished, unmistakably West-African RoN-style skirmish that nails the niche. Everything here builds on current code.*

**V1 ships:**
1. **5-resource economy** — add **Cowries** + **Knowledge** to the existing Yam/Timber/Iron, with a simple **Trade Limit** cap.
2. **4 eras** with West-African theming + research-gated aging *(have ages; reskin + gate)*.
3. **True asymmetric empires** — real powers + **1 unique unit each** (not just color/stat) *(upgrade current civ perks)*.
4. **Economy buildings**: **Market + Caravan** (Cowries), **House of Wisdom + Scholar** (Knowledge) *(extend University)*.
5. **Villager build system** *(done)* + **Scout** unit for exploration.
6. **Territory borders**: build-in-your-land gating + **enemy attrition** in your borders *(extend territory)*.
7. **Trade goods**: 5–6 Nigerian rare goods giving income + a bonus *(extend hidden resources)*.
8. **Research tree** — Military (troop upgrades, done) + a handful of Economy/Civic techs.
9. **Combined-arms roster**: Villager, Scout, Spearman, Archer, Cavalry, + 1 unique/empire, + Wall/Tower/Fort defense, + **War Chief** (basic general).
10. **2 Wonders** (Walls of Benin, Great Mosque) with strong bonuses + optional **Wonder victory**.
11. **Victory routes**: last-standing *(done)* + **capital capture** + wonder points.
12. **AI** with RoN pacing (economy → expand → later attacks) *(done; tune per-empire)*.
13. **Distinct unit models** for villager/spearman/archer/cavalry *(the gap you flagged)*.
14. **Figma-perfect UI** *(in progress)* + first **audio/identity pass** (drum SFX, empire naming).
15. **Mobile build**, offline skirmish, NGN store hook.

**V1 explicitly defers to post-V1 / final vision:** full 8 ages, naval & air, Conquer West Africa campaign, full government/Patriot system, the full 6-wonder set, online ranked MP, spies/supply depth, late-age oil/firearms beyond Bornu's unique.

---

## 4. LOCKED decisions (finalized 2026-06-17)
1. **Resources**: ✅ **5 for V1** — Yam, Timber, Iron, **Cowries**, **Knowledge** (+ Trade Limit).
2. **Empire powers**: ✅ as §2.3 (Benin/Bronze, Oyo/Cavalry, Sokoto/Scholarship, Kanem-Bornu/Trade) + 1 unique unit each.
3. **Borders + attrition**: ✅ **in V1** — build-in-your-territory + enemy attrition (Benin defensive identity).
4. **Wonders in V1**: 2 — Walls of Benin + Great Mosque.

### Locked build order
1. **Finish the UI** (Figma-perfect) — remaining: Match-Setup −/+ stepper + difficulty, Victory/Defeat summary screens. *(in progress)*
2. **Distinct unit models** — villager / spearman / archer / cavalry + empire uniques.
3. **Economy** — Cowries + Knowledge resources, Trade Limit, Market + Caravan, House of Wisdom + Scholar.
4. **Territory borders + attrition** — build-gating + attrition.
5. **Scout + War Chief** units.
6. **Trade goods + 2 Wonders.**
7. **Victory routes** — capital capture + wonder points.
8. **Audio/identity pass** + **mobile hardening**.

We build top-to-bottom from here.
