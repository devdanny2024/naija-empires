using UnityEngine;

namespace NaijaEmpires
{
    /// Lets a building be upgraded through tiers (Level 1..MaxLevel). Each upgrade spends
    /// resources, raises the building's max HP (and heals it by the gained amount), and swaps
    /// in the next-tier mesh so the structure visibly grows. Balance lives in UpgradeConfig.
    ///
    /// The owning faction is read from the Faction component; the building Kind is set by
    /// BuildingFactory at spawn time (Init). UI uses Level / MaxLevel / CanUpgrade / Upgrade.
    public class Upgradeable : MonoBehaviour
    {
        public BuildingKind Kind { get; private set; }
        public int Level { get; private set; } = 1;
        public int MaxLevel => UpgradeConfig.MaxLevel;

        FactionId _faction;
        Health _health;

        /// Called by BuildingFactory right after AddComponent so Kind is known before use.
        public void Init(BuildingKind kind) => Kind = kind;

        void Start()
        {
            var f = GetComponent<Faction>();
            _faction = f != null ? f.Id : FactionId.Player;
            _health = GetComponent<Health>();

            // Match the building's look to its owner's current age the moment it's finished, so a
            // structure raised in a later age already shows its grander tier.
            var e = Match.Econ(_faction);
            if (e != null) SetAgeTier(e.Age);
        }

        /// Visibly raise this building to the tier matching `age` (bigger model + more HP), for FREE —
        /// the age advancement itself already cost resources. No-op if already at/above that tier, not
        /// upgradeable, or still a construction scaffold. Called when the owner advances an age.
        public void SetAgeTier(int age)
        {
            if (!UpgradeConfig.IsUpgradeable(Kind)) return;
            var con = GetComponent<Construction>();
            if (con != null && !con.Complete) return; // don't reskin a half-built scaffold
            int tier = Mathf.Clamp(age, 1, MaxLevel);
            if (tier <= Level) return;
            Level = tier;
            if (_health != null)
            {
                float baseHp = BuildingConfig.Hp(Kind, Match.Econ(_faction)?.Civ ?? Civ.Benin);
                _health.Init(baseHp * UpgradeConfig.HpMult(tier)); // also repairs to full — fine on age-up
            }
            ModelLibrary.SwapModel(transform, UpgradeConfig.ModelKey(Kind, tier), Color.white);
        }

        /// Cost to reach the next level (CostTo of Level+1). UI can show this on the upgrade button.
        public Cost NextCost => UpgradeConfig.CostTo(Kind, Level + 1);

        public bool AtMaxLevel => Level >= MaxLevel;

        public bool CanUpgrade(Economy e)
        {
            if (e == null || AtMaxLevel || !UpgradeConfig.IsUpgradeable(Kind)) return false;
            return e.CanAfford(NextCost);
        }

        /// Spend, bump the level, raise max HP, and swap to the next-tier model. Returns false if
        /// it can't be afforded / already maxed.
        public bool Upgrade()
        {
            var e = Match.Econ(_faction);
            if (!CanUpgrade(e)) return false;
            if (!e.Spend(NextCost)) return false;

            int newLevel = Level + 1;

            // Raise max HP to the new tier. Init(newMax) also tops the building up to full — an upgrade
            // doubles as a repair, which is fine for a construction sink. Base HP comes from BuildingConfig,
            // scaled by the tier mult.
            if (_health != null)
            {
                float baseHp = BuildingConfig.Hp(Kind, e.Civ);
                _health.Init(baseHp * UpgradeConfig.HpMult(newLevel));
            }

            Level = newLevel;

            // Show the next-tier mesh. white tint -> ModelLibrary binds the shared colormap (like spawn).
            ModelLibrary.SwapModel(transform, UpgradeConfig.ModelKey(Kind, Level), Color.white);
            return true;
        }
    }
}
