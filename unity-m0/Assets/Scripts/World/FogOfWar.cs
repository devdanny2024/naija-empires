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
        const float UnitVision = 18f;   // reveal radius for a unit (world units) — clear walking reveal
        const float BuildingVision = 22f; // reveal radius for a building (generous home area)

        // Overlay darkness (alpha of the dark fog texture) per cell state. Kept light so unexplored
        // land reads as DIM TERRAIN under haze, never a pitch-black void (the old 0.86 blacked out the map).
        const float UnexploredAlpha = 0.45f; // gentle haze — you can still make out the land
        const float ExploredAlpha = 0.18f;   // barely-dimmed "remembered" terrain
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

        // Fog overlay rendering. The earlier all-black bug was the overlay material rendering OPAQUE;
        // it now uses the same URP transparent-unlit setup as TerritoryManager (which renders correctly),
        // so unexplored cells read dark, explored dim, and cells in a unit/building's vision are clear.
        const bool renderOverlay = true;

        void Start()
        {
            if (renderOverlay) BuildOverlay();
            Recompute_();
            ApplyVisibility();
            if (renderOverlay) Redraw();
        }

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < Recompute) return;
            _timer = 0f;
            Recompute_();
            ApplyVisibility();
            if (renderOverlay) Redraw();
        }

        // The ground overlay only DARKENS terrain — 3D enemy units/buildings standing on it would still
        // show through the fog. So hide them here: an enemy UNIT is shown only while inside the Player's
        // current vision; an enemy BUILDING stays shown once its tile has been explored (you "remember"
        // structures you've scouted, like Rise of Nations). Our own faction is always visible.
        void ApplyVisibility()
        {
            foreach (var f in Faction.All)
            {
                if (f == null) continue;
                if (f.Id == FactionId.Player) continue;
                bool isUnit = f.GetComponent<Unit>() != null;
                bool show = isUnit ? IsVisible(f.transform.position) : IsExplored(f.transform.position);
                var rends = f.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < rends.Length; i++)
                    if (rends[i] != null && rends[i].enabled != show) rends[i].enabled = show;
            }

            // Resource nodes (trees, yam fields, iron mountains, the rivals' starting farms) are 3D
            // objects that also show through the flat fog overlay — so an enemy base's farm would give
            // their position away. Hide any node whose tile the Player hasn't explored; once scouted it
            // stays remembered (IsExplored). Faction-owned farms are skipped (handled by the loop above).
            foreach (var n in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                if (n == null || n.GetComponent<Faction>() != null) continue;
                bool show = IsExplored(n.transform.position);
                var rends = n.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < rends.Length; i++)
                    if (rends[i] != null && rends[i].enabled != show) rends[i].enabled = show;
            }
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

            // "Sun on your empire": every cell inside the Player's territory (up to your borders) is lit.
            // Because buildings — including construction sites — project territory, anything you build
            // also clears the fog around it.
            var terr = TerritoryManager.Instance;
            if (terr != null)
            {
                var visP = _visible[(int)FactionId.Player];
                var expP = _explored[(int)FactionId.Player];
                for (int cz = 0; cz < Grid; cz++)
                {
                    float wz = origin + cz * cell;
                    for (int cx = 0; cx < Grid; cx++)
                    {
                        int idx = cz * Grid + cx;
                        if (visP[idx]) continue;
                        if (terr.OwnerAt(new Vector3(origin + cx * cell, 0f, wz)) == FactionId.Player)
                        { visP[idx] = true; expP[idx] = true; }
                    }
                }
            }

            // "See almost their whole base": when one of YOUR units/buildings reaches a rival's Town
            // Centre, reveal a generous radius around it so most of the enemy base becomes visible.
            const float scoutRange = 30f, baseReveal = 46f;
            var vP = _visible[(int)FactionId.Player];
            var eP = _explored[(int)FactionId.Player];
            foreach (var tc in FindObjectsByType<TownCentre>(FindObjectsSortMode.None))
            {
                var tf = tc.GetComponent<Faction>();
                if (tf == null || tf.Id == FactionId.Player) continue;
                Vector3 tcp = tc.transform.position;
                foreach (var src in Faction.All)
                {
                    if (src == null || src.Id != FactionId.Player) continue;
                    if ((src.transform.position - tcp).sqrMagnitude < scoutRange * scoutRange)
                    { MarkCircle(vP, eP, tcp, baseReveal, cell, origin); break; }
                }
            }
        }

        // Mark every cell within `r` of `pos` as visible + explored for the given grids.
        void MarkCircle(bool[] vis, bool[] exp, Vector3 pos, float r, float cell, float origin)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt((pos.x - r - origin) / cell));
            int maxX = Mathf.Min(Grid - 1, Mathf.CeilToInt((pos.x + r - origin) / cell));
            int minZ = Mathf.Max(0, Mathf.FloorToInt((pos.z - r - origin) / cell));
            int maxZ = Mathf.Min(Grid - 1, Mathf.CeilToInt((pos.z + r - origin) / cell));
            float r2 = r * r;
            for (int cz = minZ; cz <= maxZ; cz++)
            {
                float dz = (origin + cz * cell) - pos.z;
                for (int cx = minX; cx <= maxX; cx++)
                {
                    float dx = (origin + cx * cell) - pos.x;
                    if (dx * dx + dz * dz >= r2) continue;
                    int idx = cz * Grid + cx;
                    vis[idx] = true; exp[idx] = true;
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

            // Flat overlay above the ground. We build an EXPLICIT quad (not Unity's primitive Plane)
            // so the texture UVs are known to map (-Half,-Half)->(0,0) and (+Half,+Half)->(1,1), exactly
            // matching how the grid is written (pixel (cx,cz) = world (origin+cx*cell, origin+cz*cell)).
            // The primitive Plane's UV orientation is ambiguous and was mirroring the reveal so it didn't
            // line up with where units actually were.
            var go = new GameObject("FogOverlay");
            go.transform.position = new Vector3(0f, 0.06f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = OverlayQuad.Mesh();
            var r = go.AddComponent<MeshRenderer>();
            r.material = _mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _overlay = go.transform;
        }

        // Transparent unlit material — the SAME setup TerritoryManager uses (and which renders correctly
        // in this URP project), so the fog blends instead of rendering as an opaque black slab.
        static Material BuildOverlayMaterial(Texture2D tex)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                        ?? Shader.Find("Unlit/Transparent")
                        ?? Shader.Find("Sprites/Default");
            var m = new Material(sh);

            // URP Unlit transparent surface setup.
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);        // 0=opaque,1=transparent
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);            // alpha blend
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 2; // above ground + territory

            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
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
                _pixels[i] = new Color32(14, 20, 26, (byte)(a * 255f)); // soft slate haze, alpha = darkness
            }
            _tex.SetPixels32(_pixels);
            _tex.Apply(false);
        }
    }
}
