using System.Collections.Generic;
using UnityEngine;

namespace NaijaEmpires
{
    /// A building the player places starts as a construction site: small and non-functional. It only
    /// makes progress while villagers are building it (more villagers = faster), and finishes when the
    /// accumulated villager-time reaches BuildingConfig.BuildTime. On completion the building's
    /// functional components switch on, it pops to full size, and the builders return to idle.
    ///
    /// Added by BuildPlacer to player-placed buildings only — the AI (and walls) build instantly.
    public class Construction : MonoBehaviour
    {
        const float StartScale = 0.5f;   // the scaffold starts half-size and grows as it's built

        float _buildTime = 8f;
        float _progress;
        readonly HashSet<Villager> _builders = new();
        Behaviour[] _disabled = System.Array.Empty<Behaviour>();

        // We scale only the visual "Model" child as the building rises — the root (and its click
        // collider) stays full size so you can always click the scaffold to (re)assign builders.
        Transform _model;
        Vector3 _modelBase = Vector3.one;

        public bool Complete { get; private set; }

        public void Begin(BuildingKind kind)
        {
            _buildTime = BuildingConfig.BuildTime(kind);

            // Switch off everything that makes the building "work" until it's actually built (no
            // production, no pop cap, no tower fire, no farm, no research, no upgrades).
            var list = new List<Behaviour>();
            void Off<T>() where T : Behaviour { var c = GetComponent<T>(); if (c != null) { c.enabled = false; list.Add(c); } }
            Off<ProductionBuilding>();
            Off<Tower>();
            Off<FarmProduction>();
            Off<University>();
            Off<PopCapProvider>();
            Off<TradeLimitProvider>();
            Off<Upgradeable>();
            _disabled = list.ToArray();

            _model = transform.Find("Model");
            if (_model != null) { _modelBase = _model.localScale; _model.localScale = _modelBase * StartScale; }
        }

        public void AddBuilder(Villager v) { if (!Complete && v != null) _builders.Add(v); }
        public void RemoveBuilder(Villager v) => _builders.Remove(v);

        void Update()
        {
            if (Complete) return;
            _builders.RemoveWhere(b => b == null);
            int n = _builders.Count;
            if (n <= 0) return; // no builders -> no progress (a building needs a villager to rise)

            _progress += n * Time.deltaTime;
            float t = Mathf.Clamp01(_progress / _buildTime);
            if (_model != null) _model.localScale = _modelBase * Mathf.Lerp(StartScale, 1f, t);
            if (_progress >= _buildTime) Finish();
        }

        void Finish()
        {
            Complete = true;
            if (_model != null) _model.localScale = _modelBase;
            // Re-enable the functional components — now their Start() runs (pop cap added, etc.).
            foreach (var b in _disabled) if (b != null) b.enabled = true;
            enabled = false; // builders see Complete next tick and return to idle
        }
    }
}
