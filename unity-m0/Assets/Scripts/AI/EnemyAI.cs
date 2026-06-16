using System;
using System.Collections.Generic;
using UnityEngine;

namespace NaijaEmpires
{
    /// A simple but functional opponent: keeps villagers gathering, advances ages, builds houses
    /// and a barracks, trains an army, and attacks the player's Town Centre once strong enough.
    public class EnemyAI : MonoBehaviour
    {
        public FactionId Owner = FactionId.Enemy;
        public Vector3 basePos;
        public Transform enemyTarget;       // the player's Town Centre

        public int attackThreshold = 6; // base; randomized per-bot in Start()

        float _think;
        const float Interval = 1.5f;

        // --- per-bot personality, rolled once in Start() so every bot differs ---
        float _matchStart;          // when this bot came online (game time)
        int _armyThreshold;         // army size needed before the first wave
        float _aggression;          // 0..1 personality; higher = bolder/sooner/bigger commits
        float _firstAttackTime;     // minimum game time before the first wave is allowed
        float _commitFraction;      // fraction of the army sent per wave (keeps defenders)
        float _thinkPhase;          // staggers this bot's think tick off the others
        float _nextWaveAt;          // earliest game time the next wave may launch
        bool _firstWaveDone;        // has this bot ever committed a wave?

        void Start()
        {
            _matchStart = Time.time;

            // Personality. Spread bots across passive -> aggressive so a 4-way FFA feels varied.
            _aggression = UnityEngine.Random.Range(0f, 1f);

            // Army size before the first attack: 8 (bold) .. 18 (turtle).
            _armyThreshold = Mathf.RoundToInt(Mathf.Lerp(18f, 8f, _aggression));

            // No rushing: bold bots wait ~75s, passive bots ~165s, plus jitter so they desync.
            _firstAttackTime = Mathf.Lerp(165f, 75f, _aggression) + UnityEngine.Random.Range(0f, 30f);

            // Never commit the whole army; send 45%..75% and keep the rest home as defenders.
            _commitFraction = UnityEngine.Random.Range(0.45f, 0.75f);

            // Desync think ticks so bots don't all act on the same frame.
            _thinkPhase = UnityEngine.Random.Range(0f, Interval);
            _think = _thinkPhase;

            _nextWaveAt = _firstAttackTime;
        }

        void Update()
        {
            if (Match.Over) return;
            _think -= Time.deltaTime;
            if (_think > 0f) return;
            _think = Interval;

            var e = Match.Econ(Owner);
            if (e == null) return;

            AssignIdleVillagers();
            ManageBuild(e);
            TrainArmy();
            MaybeAttack(e);
        }

        void AssignIdleVillagers()
        {
            foreach (var v in FindObjectsByType<Villager>(FindObjectsSortMode.None))
            {
                if (!Mine(v) || !v.Idle) continue;
                var n = NearestNode(v.transform.position);
                if (n != null) v.Gather(n);
            }
        }

        void ManageBuild(Economy e)
        {
            if (e.Age < Ages.Max && e.CanAfford(Ages.CostFor(e.Age + 1)))
                Ages.TryAdvance(Owner);

            if (e.PopUsed >= e.PopCap - 1 && e.CanAfford(BuildingConfig.CostOf(BuildingKind.House, e.Civ)))
                Build(BuildingKind.House, e);

            bool hasBarracks = FindMine<ProductionBuilding>(b => b.Trainable.Contains(UnitType.Spearman)) != null;
            if (e.Age >= 2 && !hasBarracks && e.CanAfford(BuildingConfig.CostOf(BuildingKind.Barracks, e.Civ)))
                Build(BuildingKind.Barracks, e);
        }

        void TrainArmy()
        {
            var barracks = FindMine<ProductionBuilding>(b => b.Trainable.Contains(UnitType.Spearman));
            if (barracks == null) return;
            barracks.Train(UnityEngine.Random.value < 0.5f ? UnitType.Spearman : UnitType.Archer);
        }

        void MaybeAttack(Economy e)
        {
            // Economy first: bots never attack in Age 1. They build up, advance ages, and only
            // start probing in Age 2, getting bolder in Age 3.
            if (e.Age < 2) return;

            float now = Time.time;
            float elapsed = now - _matchStart;
            if (elapsed < _firstAttackTime) return;   // minimum game-time gate
            if (now < _nextWaveAt) return;             // random delay between waves

            var army = new List<CombatUnit>();
            foreach (var c in FindObjectsByType<CombatUnit>(FindObjectsSortMode.None))
                if (Mine(c)) army.Add(c);

            // Effective threshold ramps down with Age and game time: the longer the match runs and
            // the higher the Age, the smaller the force a bot is willing to commit with.
            float ageRamp = (e.Age - 2) * 2f;                       // Age2: 0, Age3: -2
            float timeRamp = Mathf.Min(4f, elapsed / 120f);         // up to -4 over a long match
            int needed = Mathf.Max(4, Mathf.RoundToInt(_armyThreshold - ageRamp - timeRamp));
            if (army.Count < needed) return;

            // FFA: march on the nearest enemy Town Centre (any faction that isn't us).
            Transform target = NearestEnemyTownCentre();
            if (target == null) return;

            // Occasional feint/regroup: now and then a bot pulls back instead of committing, so
            // waves aren't perfectly predictable. Bolder bots feint less often.
            float feintChance = Mathf.Lerp(0.30f, 0.08f, _aggression);
            if (_firstWaveDone && UnityEngine.Random.value < feintChance)
            {
                // Regroup at base and try again shortly.
                foreach (var c in army) c.MoveTo(basePos);
                _nextWaveAt = now + UnityEngine.Random.Range(6f, 14f);
                return;
            }

            // Commit only a fraction; keep the rest home as defenders. Bolder bots send more.
            float commit = Mathf.Clamp01(_commitFraction + _aggression * 0.15f);
            int sendCount = Mathf.Clamp(Mathf.RoundToInt(army.Count * commit), 1, army.Count);
            for (int i = 0; i < sendCount; i++)
                army[i].MoveTo(target.position);

            _firstWaveDone = true;

            // Random delay before the next wave; bolder bots regroup faster.
            float gap = Mathf.Lerp(22f, 9f, _aggression) + UnityEngine.Random.Range(0f, 8f);
            _nextWaveAt = now + gap;
        }

        Transform NearestEnemyTownCentre()
        {
            Transform best = null;
            float bestSqr = float.MaxValue;
            foreach (var tc in FindObjectsByType<TownCentre>(FindObjectsSortMode.None))
            {
                var f = tc.GetComponent<Faction>();
                if (f == null || f.Id == Owner) continue;
                float d = (tc.transform.position - basePos).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = tc.transform; }
            }
            return best;
        }

        // --- helpers ---

        bool Mine(Component c)
        {
            var f = c.GetComponent<Faction>();
            return f != null && f.Id == Owner;
        }

        ResourceNode NearestNode(Vector3 p)
        {
            ResourceNode best = null;
            float bestSqr = float.MaxValue;
            foreach (var n in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                float d = (n.transform.position - p).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = n; }
            }
            return best;
        }

        void Build(BuildingKind k, Economy e)
        {
            if (!e.Spend(BuildingConfig.CostOf(k, e.Civ))) return;
            Vector3 pos = basePos + new Vector3(UnityEngine.Random.Range(-6f, 6f), 0f, UnityEngine.Random.Range(-6f, 6f));
            BuildingFactory.Spawn(k, pos, Owner);
        }

        T FindMine<T>(Func<T, bool> pred) where T : Component
        {
            foreach (var b in FindObjectsByType<T>(FindObjectsSortMode.None))
                if (Mine(b) && pred(b)) return b;
            return null;
        }
    }
}
