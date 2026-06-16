using UnityEngine;

namespace NaijaEmpires
{
    /// A Farm slowly produces renewable Yam for its owner (no villager required). Yield per second
    /// comes from BuildingConfig.FarmYamPerSec and scales with the Farm's upgrade tier if it has one.
    /// Yam is granted in whole units once a second of accumulation reaches >= 1.
    public class FarmProduction : MonoBehaviour
    {
        FactionId _faction;
        Upgradeable _upgrade;
        float _accum;

        void Start()
        {
            var f = GetComponent<Faction>();
            _faction = f != null ? f.Id : FactionId.Player;
            _upgrade = GetComponent<Upgradeable>();
        }

        void Update()
        {
            var e = Match.Econ(_faction);
            if (e == null) return;

            float perSec = BuildingConfig.FarmYamPerSec(BuildingKind.Farm);
            // Higher-tier farms yield more (mirrors the HP tier multiplier).
            if (_upgrade != null) perSec *= UpgradeConfig.HpMult(_upgrade.Level);

            _accum += perSec * Time.deltaTime;
            if (_accum >= 1f)
            {
                int whole = Mathf.FloorToInt(_accum);
                _accum -= whole;
                e.Add(ResourceType.Yam, whole);
            }
        }
    }
}
