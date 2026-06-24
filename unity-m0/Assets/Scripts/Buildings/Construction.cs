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
        GameObject _scaffold;   // wooden build-frame shown while under construction

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
            Off<OilProducer>();
            Off<Upgradeable>();
            _disabled = list.ToArray();

            _model = transform.Find("Model");
            if (_model != null)
            {
                BuildScaffold(MeasureFootprint());        // wrap the site in a wooden build-frame
                _modelBase = _model.localScale;
                _model.localScale = _modelBase * StartScale;
            }
        }

        // Combined world-space size of the finished model (so the scaffold wraps it). Falls back to a
        // ~2-unit cube if the model has no renderers.
        Vector3 MeasureFootprint()
        {
            var rends = _model != null ? _model.GetComponentsInChildren<Renderer>() : null;
            if (rends == null || rends.Length == 0) return new Vector3(2f, 2f, 2f);
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b.size;
        }

        // A simple wooden scaffold (4 corner poles + rails + planks + a site flag) framing the rising
        // building. Removed on completion. Pure primitives — no art assets.
        void BuildScaffold(Vector3 size)
        {
            _scaffold = new GameObject("Scaffold");
            _scaffold.transform.SetParent(transform, false);
            float hw = Mathf.Max(0.45f, size.x * 0.55f);
            float hd = Mathf.Max(0.45f, size.z * 0.55f);
            float h  = Mathf.Clamp(size.y * 0.95f, 1.0f, 6f);
            Color wood = new Color(0.52f, 0.36f, 0.18f);
            Color rail = new Color(0.6f, 0.45f, 0.24f);

            (float, float)[] corners = { (-hw, -hd), (hw, -hd), (hw, hd), (-hw, hd) };
            foreach (var (x, z) in corners)
                Bar(new Vector3(x, h * 0.5f, z), new Vector3(0.09f, h, 0.09f), wood);   // corner pole

            for (int lvl = 0; lvl < 2; lvl++)                                            // two rail rings
            {
                float y = lvl == 0 ? h * 0.5f : h - 0.06f;
                Bar(new Vector3(0, y, -hd), new Vector3(hw * 2f, 0.06f, 0.06f), rail);
                Bar(new Vector3(0, y,  hd), new Vector3(hw * 2f, 0.06f, 0.06f), rail);
                Bar(new Vector3(-hw, y, 0), new Vector3(0.06f, 0.06f, hd * 2f), rail);
                Bar(new Vector3( hw, y, 0), new Vector3(0.06f, 0.06f, hd * 2f), rail);
            }
            Bar(new Vector3(-hw * 0.3f, h - 0.02f, 0), new Vector3(0.18f, 0.04f, hd * 2f), rail);  // planks
            Bar(new Vector3( hw * 0.4f, h - 0.02f, 0), new Vector3(0.18f, 0.04f, hd * 2f), rail);
            Bar(new Vector3(hw, h + 0.35f, hd), new Vector3(0.04f, 0.7f, 0.04f), wood);            // flag pole
            Bar(new Vector3(hw + 0.22f, h + 0.55f, hd), new Vector3(0.4f, 0.26f, 0.03f), new Color(0.85f, 0.7f, 0.3f));
        }

        void Bar(Vector3 localPos, Vector3 scale, Color c)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = g.GetComponent<Collider>(); if (col) Destroy(col);
            g.transform.SetParent(_scaffold.transform, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = scale;
            MaterialUtil.SetColor(g.GetComponent<Renderer>(), c);
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
            if (_scaffold != null) Destroy(_scaffold);   // take down the build-frame
            // Re-enable the functional components — now their Start() runs (pop cap added, etc.).
            foreach (var b in _disabled) if (b != null) b.enabled = true;
            enabled = false; // builders see Complete next tick and return to idle
        }
    }
}
