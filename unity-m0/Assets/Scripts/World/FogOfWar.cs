using UnityEngine;

namespace NaijaEmpires
{
    /// Per-faction fog of war over the playable square. Each faction has a coarse vision grid with
    /// three states per cell: unexplored (never seen), explored (seen before, not currently in
    /// vision), and visible (an owned unit/building can currently see it). Units and buildings
    /// (everything in Faction.All) reveal a radius around themselves every recompute tick.
    ///
    /// Only the Player's fog is rendered: a transparent overlay plane above the ground carrying a
    /// Texture2D — unexplored cells are dark, explored-but-not-seen are dimmed, in-vision cells are
    /// clear. Compute and render run a few times a second (NOT every frame). The collider is stripped
    /// so the overlay never blocks clicks, and the material is URP-safe transparent unlit.
    ///
    /// Public read API: IsVisible(world) / IsExplored(world) — used by HiddenResource (and AI/UI) to
    /// query the Player's knowledge of the map. Added to the scene by Bootstrap.BuildManagers.
    public class FogOfWar : MonoBehaviour
    {
        // --- tuning -----------------------------------------------------------------
        const int Grid = 64;            // cells per side over the playable square
        const float Recompute = 0.25f;  // seconds between vision recomputes
        const float UnitVision = 11f;   // reveal radius for a unit (world units)
        const float BuildingVision = 16f; // reveal radius for a building (no Unit component)

        // Overlay darkness (alpha of the black fog texture) per cell state.
        const float UnexploredAlpha = 0.92f; // nearly opaque black
        const float ExploredAlpha = 0.45f;   // dimmed "remembered" terrain
        const float VisibleAlpha = 0f;        // fully clear

        static readonly int FactionCount = System.Enum.GetValues(typeof(FactionId)).Length;
        static readonly float Half = MapBounds.Half;

        // Singleton so HiddenResource (and others) can query without holding a reference.
        public static FogOfWar Instance { get; private set; }

        // --- state ------------------------------------------------------------------
        // Per faction: visible (this tick) and explored (ever). Indexed [faction][cz*Grid+cx].
        bool[][] _visible;
        bool[][] _explored;

        // --- render (Player only) ---------------------------------------------------
        Texture2D _tex;
        Color32[] _pixels;
        Material _mat;
        Transform _overlay;

        float _timer;

        void Awake()
        {
            Instance = this;
            int cells = Grid * Grid;
            _visible = new bool[FactionCount][];
            _explored = new bool[FactionCount][];
            for (int f = 0; f < FactionCount; f++)
            {
                _visible[f] = new bool[cells];
                _explored[f] = new bool[cells];
            }
        }

        void Start()
        {
            BuildOverlay();
            Recompute_();
            Redraw();
        }

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < Recompute) return;
            _timer = 0f;
            Recompute_();
            Redraw();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_overlay != null) Destroy(_overlay.gameObject);
            if (_mat != null) Destroy(_mat);
            if (_tex != null) Destroy(_tex);
        }

        // ---------------------------------------------------------------- public read API
        /// True if the given world position is currently within the Player's vision.
        public bool IsVisible(Vector3 world) => Sample(_visible, (int)FactionId.Player, world);

        /// True if the Player has ever seen the given world position (visible now or remembered).
        public bool IsExplored(Vector3 world) => Sample(_explored, (int)FactionId.Player, world);

        /// Same queries for an arbitrary faction (handy for AI later).
        public bool IsVisibleFor(FactionId id, Vector3 world) => Sample(_visible, (int)id, world);
        public bool IsExploredFor(FactionId id, Vector3 world) => Sample(_explored, (int)id, world);

        bool Sample(bool[][] grids, int faction, Vector3 world)
        {
            if (grids == null) return false;
            if (!TryCell(world, out int cx, out int cz)) return false;
            return grids[faction][cz * Grid + cx];
        }

        // ---------------------------------------------------------------- compute
        void Recompute_()
        {
            // Clear current visibility; explored is sticky.
            for (int f = 0; f < FactionCount; f++)
                System.Array.Clear(_visible[f], 0, _visible[f].Length);

            float cell = (Half * 2f) / Grid;
            float origin = -Half + cell * 0.5f; // world centre of cell (0,0)

            foreach (var src in Faction.All)
            {
                if (src == null) continue;
                int f = (int)src.Id;
                float r = src.GetComponent<Unit>() != null ? UnitVision : BuildingVision;
                Vector3 pos = src.transform.position;

                int minX = Mathf.Max(0, Mathf.FloorToInt((pos.x - r - origin) / cell));
                int maxX = Mathf.Min(Grid - 1, Mathf.CeilToInt((pos.x + r - origin) / cell));
                int minZ = Mathf.Max(0, Mathf.FloorToInt((pos.z - r - origin) / cell));
                int maxZ = Mathf.Min(Grid - 1, Mathf.CeilToInt((pos.z + r - origin) / cell));
                float r2 = r * r;

                var vis = _visible[f];
                var exp = _explored[f];
                for (int cz = minZ; cz <= maxZ; cz++)
                {
                    float dz = (origin + cz * cell) - pos.z;
                    for (int cx = minX; cx <= maxX; cx++)
                    {
                        float dx = (origin + cx * cell) - pos.x;
                        if (dx * dx + dz * dz >= r2) continue;
                        int idx = cz * Grid + cx;
                        vis[idx] = true;
                        exp[idx] = true;
                    }
                }
            }
        }

        bool TryCell(Vector3 world, out int cx, out int cz)
        {
            float cell = (Half * 2f) / Grid;
            cx = Mathf.FloorToInt((world.x + Half) / cell);
            cz = Mathf.FloorToInt((world.z + Half) / cell);
            return cx >= 0 && cz >= 0 && cx < Grid && cz < Grid;
        }

        // ---------------------------------------------------------------- render (Player fog)
        void BuildOverlay()
        {
            _tex = new Texture2D(Grid, Grid, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear, // soft fog edges
            };
            _pixels = new Color32[Grid * Grid];

            _mat = BuildOverlayMaterial(_tex);

            // Flat plane above the ground (and above the territory overlay at y=0.03). Unity's Plane
            // is 10x10 at scale 1.
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "FogOverlay";
            var col = go.GetComponent<Collider>(); if (col) Destroy(col); // never block clicks
            go.transform.position = new Vector3(0f, 0.06f, 0f);
            float s = (Half * 2f) / 10f;
            go.transform.localScale = new Vector3(s, 1f, s);
            var r = go.GetComponent<Renderer>();
            r.material = _mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _overlay = go.transform;
        }

        // Transparent unlit URP material built in code (no asset import); falls back to legacy
        // transparent shaders for the Built-in pipeline. URP-safe: avoids magenta.
        static Material BuildOverlayMaterial(Texture2D tex)
        {
            // Use a transparent-BY-DEFAULT shader so alpha-0 (visible) cells truly show through. The
            // manual URP-Unlit transparent setup was leaving the overlay opaque -> the whole map read
            // black. Unlit/Transparent + Sprites/Default alpha-blend _MainTex out of the box; Sprites/
            // Default is force-included in GraphicsSettings so it survives shader stripping in builds.
            Shader sh = Shader.Find("Unlit/Transparent")
                        ?? Shader.Find("Sprites/Default")
                        ?? Shader.Find("Universal Render Pipeline/Unlit");
            var m = new Material(sh);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 2; // above ground + territory
            return m;
        }

        void Redraw()
        {
            var vis = _visible[(int)FactionId.Player];
            var exp = _explored[(int)FactionId.Player];
            int cells = Grid * Grid;
            for (int i = 0; i < cells; i++)
            {
                float a = vis[i] ? VisibleAlpha : (exp[i] ? ExploredAlpha : UnexploredAlpha);
                _pixels[i] = new Color32(0, 0, 0, (byte)(a * 255f)); // black fog, alpha = darkness
            }
            _tex.SetPixels32(_pixels);
            _tex.Apply(false);
        }
    }
}
