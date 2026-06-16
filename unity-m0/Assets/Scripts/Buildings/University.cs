using UnityEngine;

namespace NaijaEmpires
{
    /// Researches permanent troop upgrades for the owning faction. One research runs at a time;
    /// when it finishes the troop type is marked in TechState so all future troops of that type
    /// (already handled by UnitConfig.Hp/Damage faction overloads) spawn stronger.
    ///
    /// Cost / age gate / time live in UnitConfig (ResearchCost / ResearchAgeRequired / ResearchTime).
    /// UI uses: CanResearch, BeginResearch, IsResearching, InProgress, Progress01, IsDone.
    public class University : MonoBehaviour
    {
        FactionId _faction;

        UnitType _inProgress;
        bool _busy;
        float _timer;
        float _duration;

        public bool IsResearching => _busy;
        public UnitType InProgress => _inProgress;
        public float Progress01 => _busy && _duration > 0f ? Mathf.Clamp01(_timer / _duration) : 0f;

        void Start()
        {
            var f = GetComponent<Faction>();
            _faction = f != null ? f.Id : FactionId.Player;
        }

        /// Already researched (done) for this faction.
        public bool IsDone(UnitType t) => TechState.IsResearched(_faction, t);

        /// Can this type be researched right now: researchable, not already done, nothing else in
        /// progress, age requirement met, and affordable.
        public bool CanResearch(UnitType t)
        {
            if (_busy) return false;
            if (!UnitConfig.IsResearchable(t)) return false;
            if (IsDone(t)) return false;
            var e = Match.Econ(_faction);
            if (e == null || e.Age < UnitConfig.ResearchAgeRequired(t)) return false;
            return e.CanAfford(UnitConfig.ResearchCost(t));
        }

        /// Spend the cost and start the research timer. Returns false if it can't be started.
        public bool BeginResearch(UnitType t)
        {
            var e = Match.Econ(_faction);
            if (!CanResearch(t) || e == null) return false;
            if (!e.Spend(UnitConfig.ResearchCost(t))) return false;

            _inProgress = t;
            _duration = UnitConfig.ResearchTime(t);
            _timer = 0f;
            _busy = true;
            return true;
        }

        void Update()
        {
            if (!_busy) return;
            _timer += Time.deltaTime;
            if (_timer >= _duration)
            {
                TechState.MarkResearched(_faction, _inProgress);
                _busy = false;
                _timer = 0f;
            }
        }
    }
}
