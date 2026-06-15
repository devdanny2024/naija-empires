# Naija Empires — Brand Identity v1

**Theme: "Bronze & Indigo."** Drawn from **Benin bronze plaques** (regal cast metal) on **Yoruba adire indigo** (deep dyed cloth). Premium, earthy, unmistakably West-African — not generic-fantasy. Codified in `Assets/Scripts/UI/Theme.cs` (single source of truth).

## Palette
| Token | Hex | Use |
|---|---|---|
| Bronze | `#C8901E` | Primary accent, headings, primary buttons, rules |
| Bronze Light | `#E6BC63` | Hover / highlight |
| Bronze Deep | `#8A5E16` | Pressed / engraved lines |
| Night | `#121829` | Background tone, banner |
| Panel | `#1B2238` | Panel fill (≈96% opacity over the map) |
| Panel Hi | `#27314F` | Raised / selected / button fill |
| Ivory | `#F3E7CC` | Primary text |
| Muted | `#B9A98A` | Secondary text / captions |
| Confirm | `#5BA84F` | Affordable / Victory |
| Danger | `#C0492F` | Can't-afford / Defeat / Oyo |
| Yam / Timber / Iron | `#E9C24A` / `#5E9B47` / `#9AA3AE` | Resource swatches |
| Benin / Oyo team | `#3389F2` / `#E65242` | Player / enemy (color-blind-safe) |

## Type
Heavy caps for the wordmark and section headers; sentence case for body. Currently the engine built-in face (zero-dependency); swap to a display serif/slab for the wordmark when we commission art (a Benin-bronze-flavored display face).

## Motifs
- **Bronze lozenge** (plaque rivet): a small 45°-rotated bronze diamond as divider/bullet.
- **Bronze rule**: a hairline bronze accent along the top inner edge of every panel.
- **Soft-cornered indigo panels** floating over the bright map — regal, readable.

## Voice
Confident, proverb-flavored. "Build your dynasty." "An empire is not built in a day." Section labels are single strong words: BUILD, TRAIN, AGE.

## UI principles
- Landscape-first; resolution-independent (uGUI CanvasScaler @1920×1080, match-height).
- Generous touch targets (≥48px) per the mobile GDD.
- Affordability is always color-coded: green = can afford, red = can't, grey = locked by Age.

*This is v1 — a starting identity. Replace freely as the art direction firms up; keep `Theme.cs` as the one place colors/type live.*
