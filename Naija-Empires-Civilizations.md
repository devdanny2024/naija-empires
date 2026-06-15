# Naija Empires — Civilizations, Perks & Bonuses

**Document:** Civilization design reference
**Date:** 2026-06-12
**For:** Naija Empires (mobile RTS)
**Author:** Olukayode Soliu

---

## How civilizations work

Each civilization (an empire/"state") plays differently through three levers:

1. **Unique unit** — a signature troop only that civ can train.
2. **Passive bonuses** — always-on perks (economy, military, or defence) that shape its playstyle.
3. **Wonder** — a landmark that can be built for prestige and an alternate win path.

Civs are designed to be **distinct but balanced** — every civ has a clear strength and a matching weakness, so the rock-paper-scissors of unit counters (Spear ▶ Cavalry ▶ Archer ▶ Spear) still decides fights.

**MVP status:** **Benin** and **Oyo** are the two playable civs in the current build; the other four are designed here for the post-MVP roadmap. Benin's defensive bonuses are already live in code (`Config/GameConfig.cs`); the rest are the design spec to implement next.

---

## At a glance

| Civilization | Playstyle | Unique Unit | Signature Bonus | Status |
|---|---|---|---|---|
| **Benin Empire** | Defensive fortress | Iyokuo Royal Guard | Walls & Towers −30% cost, +50% HP | ✅ Playable |
| **Oyo Empire** | Cavalry aggression | Eso Cavalry | Cavalry faster, cheaper, quicker | ✅ Playable |
| **Sokoto Caliphate** | Fast-teching economy | Sokoto Lancer | Advance Ages faster & cheaper | 🚧 Designed |
| **Kanem-Bornu** | Trade & ranged | Camel Archer | Markets earn extra Cowries; +range | 🚧 Designed |
| **Hausa City-States** | Walled commerce | Kano Archer | Cheaper economy; strong behind walls | 🚧 Designed |
| **Nri Kingdom** | Support & economy | Nri Priest | Villagers work faster; healing | 🚧 Designed |

---

## 1. Benin Empire — *The Fortress*  ✅ Playable

**Playstyle:** Defensive turtle. Hold strong behind unbreakable walls, out-tech the enemy, and win the late game.

**Unique unit — Iyokuo Royal Guard:** Elite heavy infantry. Tanky, strong against cavalry, the backbone of a Benin hold.

**Bonuses**
- **Walls & Towers cost −30%** *(live)*
- **Towers have +50% HP** *(live)*
- **Town Centre +25% HP** — your heart is harder to crack.
- **Bronze-working:** military upgrades cost −20% (the famed Benin bronze guild).

**Wonder:** *The Walls of Benin* — one of history's largest earthworks.

**Strength / Weakness:** Nearly unbreakable on defence and strong late · slower, weaker early aggression.

**History:** The Benin Kingdom built vast city walls and moats (and the related Sungbo's Eredo) and was renowned for its bronze-casting guild — hence a defensive, craft-strong identity.

---

## 2. Oyo Empire — *The Cavalry*  ✅ Playable

**Playstyle:** Fast and aggressive. Pressure early with cavalry, raid the enemy economy, never let them settle.

**Unique unit — Eso Cavalry:** Fast, hard-hitting mounted shock troops — Oyo's historic strike force.

**Bonuses**
- **Cavalry train 33% faster.**
- **Cavalry cost −15%.**
- **Cavalry move +10% speed** — better raids and escapes.
- **Stables cost −20%.**

**Wonder:** *The Aafin (Royal Palace of Oyo).*

**Strength / Weakness:** Dominant early/mid aggression and map control · weaker walls and a soft late game vs spearmen-heavy defences.

**History:** The Oyo Empire's power rested on its formidable cavalry (the Eso warriors), dominating the savannah where horses thrived.

---

## 3. Sokoto Caliphate — *The Scholars*  🚧 Designed

**Playstyle:** Boom and tech. Reach higher Ages first and overwhelm with a more advanced army and economy.

**Unique unit — Sokoto Lancer:** Mounted lancer that excels against archers and raiders.

**Bonuses**
- **Advance Ages 25% faster.**
- **Age-up costs −20%.**
- **Trade/Market income +15%.**
- **Villagers gather Yam +10%.**

**Wonder:** *The Great Mosque* — a beacon of scholarship.

**Strength / Weakness:** Fastest to a tech/economy lead · vulnerable to early all-in rushes before the boom pays off.

**History:** The 19th-century Sokoto Caliphate was a vast, organised state famed for scholarship, administration, and trans-regional trade.

---

## 4. Kanem-Bornu — *The Traders*  🚧 Designed

**Playstyle:** Trade-fuelled ranged army. Build wealth through markets and win at range with camels and archers.

**Unique unit — Camel Archer:** Mobile ranged unit that resists enemy cavalry (camels unsettle horses).

**Bonuses**
- **Markets generate passive Cowries** (trade currency).
- **Trade income +20%.**
- **Archers +1 range.**
- **Camel units take −25% bonus damage from cavalry.**

**Wonder:** *The Trans-Saharan Caravanserai.*

**Strength / Weakness:** Rich economy and superior ranged poke · weaker in close melee brawls.

**History:** The Kanem-Bornu Empire around Lake Chad thrived for centuries on trans-Saharan trade and fielded camel cavalry and early firearms.

---

## 5. Hausa City-States — *The Walled Merchants*  🚧 Designed

**Playstyle:** Walled economic powerhouse. Out-produce everyone from behind great city walls.

**Unique unit — Kano Archer:** Defensive archer that gains extra power when fighting near friendly walls/towers.

**Bonuses**
- **Economy buildings cost −15%.**
- **Start with +1 extra Villager.**
- **Walls & gates cost −25%.**
- **Trade income +15%.**

**Wonder:** *The Great Walls of Kano.*

**Strength / Weakness:** Strongest sustained economy behind walls · must defend well; weaker open-field aggression.

**History:** The Hausa city-states (Kano, Katsina, Zaria…) were walled commercial hubs famous for dyeing, crafts, and long-distance trade.

---

## 6. Nri Kingdom — *The Priest-Kings*  🚧 Designed

**Playstyle:** Support and economy. A peaceful, fast-growing economy backed by healers rather than raw aggression.

**Unique unit — Nri Priest:** Heals nearby friendly units and can convert enemies — a force multiplier, not a frontline fighter.

**Bonuses**
- **Villagers work +15% faster.**
- **Houses provide +1 extra population each.**
- **Units slowly regenerate health near a Town Centre.**
- **Priests heal allies in an area.**

**Wonder:** *The Sacred Grove of Nri.*

**Strength / Weakness:** Best economy-per-villager and great army sustain through healing · no innate military bonus — must out-play in fights.

**History:** The Nri Kingdom was an Igbo theocratic state led by priest-kings whose authority was spiritual rather than military — reflected here as a support/economy civ.

---

## Balance philosophy

- **Every civ has a clear weakness.** Bonuses tilt a playstyle; they never make a civ strictly better.
- **Counters still rule fights.** Civ perks change *economy, tempo, and durability* — the Spear/Cavalry/Archer triangle still decides battles, so no civ is unbeatable.
- **Numbers are tunable.** All values live in `Config/GameConfig.cs` and will be adjusted from playtests.

## Implementation roadmap

| Phase | Civs |
|---|---|
| **Now (M1)** | Benin (defensive bonuses live), Oyo |
| **Next** | Sokoto, Kanem-Bornu (economy/tech + trade systems) |
| **Later** | Hausa City-States, Nri (walls + support/healing systems) |

*All civilizations, units, bonuses, and numbers are a design starting point, open to iteration from playtesting.*
