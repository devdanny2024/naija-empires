using System.Collections.Generic;
using UnityEngine;

namespace NaijaEmpires
{
    /// Influence-zone territory control + map coloring. Each building projects a colored control
    /// radius onto the ground; the ground is tinted by the controlling empire and blended where
    /// contested. Compute (per-faction influence over a coarse grid) and render (a transparent
    /// overlay plane carrying a Texture2D written from the grid) are kept separate so a later
    /// networking phase can drive the grid from replicated state instead of local building positions.
    ///
    /// Added to the scene by Bootstrap.BuildManagers.
    public class TerritoryManager : MonoBehaviour
    {
        // --- tuning -----------------------------------------------------------------
        const int Grid = 48;            // cells per side over the ground (48x48 = 2304 cells)
        static readonly float GroundHalf = MapBounds.Half; // half-extent of the playable square (single source)
        const float Recompute = 0.5f;   // seconds between recomputes (NOT every frame)
        const float Falloff = 1.6f;     // influence falloff exponent inside a building's radius
        const float ClaimMin = 0.18f;   // min winning influence for a cell to be claimed (else neutral)
        const float ContestRatio = 0.7f;// runner-up/winner ratio above which a cell reads as contested
        const float TintAlpha = 0.3f;   // overlay opacity for a solidly-owned cell (subtle wash, not a blob)

        // Control radius per building category (world units).
        const float RadiusTownCentre = 14f;
        const float RadiusTower = 9f;
        const float RadiusOther = 6f;

        static readonly int FactionCount = System.Enum.GetValues(typeof(FactionId)).Length;

        // --- compute state (no per-frame allocation) --------------------------------
        readonly FactionId[] _owner = new FactionId[Grid * Grid]; // -1 sentinel via _owned
        readonly bool[] _owned = new bool[Grid * Grid];           // false = neutral/contested
        float[] _influence;                                       // [cell * FactionCount + faction]
        readonly List<Source> _sources = new(64);                // reused scratch list

        struct Source { public Vector3 Pos; public float Radius; public FactionId Id; }

        // --- render state -----------------------------------------------------------
        Texture2D _tex;
        Color32[] _pixels;
        Material _mat;
        Transform _overlay;

        float _timer;

        /// Singleton so FogOfWar (and others) can read territory ownership without holding a reference.
        public static TerritoryManager Instance { get; private set; }

        void Awake() { Instance = this; }

        void Start()
        {
            _influence = new float[Grid * Grid * FactionCount];
            BuildOverlay();
            Recalculate();
            Redraw();
        }

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < Recompute) return;
            _timer = 0f;
            Recalculate();
            Redraw();
        }

        // ---------------------------------------------------------------- public read API
        /// Faction controlling the cell under a world position, or null if neutral/contested.
        /// Lets the minimap (or AI) sample territory without knowing the grid internals.
        public FactionId? OwnerAt(Vector3 world)
        {
            if (!TryCell(world, out int cx, out int cz)) return null;
            int i = cz * Grid + cx;
            return _owned[i] ? _owner[i] : (FactionId?)null;
        }

        /// Coarse grid side length (cells per axis), for callers that want to sample directly.
        public int GridSize => Grid;

        /// Owner of grid cell (cx, cz), or null if neutral/contested or out of range.
        public FactionId? OwnerOfCell(int cx, int cz)
        {
            if (cx < 0 || cz < 0 || cx >= Grid || cz >= Grid) return null;
            int i = cz * Grid + cx;
            return _owned[i] ? _owner[i] : (FactionId?)null;
        }

        // ---------------------------------------------------------------- compute
        void Recalculate()
        {
            CollectSources();

            int cells = Grid * Grid;
            System.Array.Clear(_influence, 0, _influence.Length);

            float cell = (GroundHalf * 2f) / Grid;
            float origin = -GroundHalf + cell * 0.5f; // world centre of cell (0,0)

            // Accumulate each source's influence into the cells inside its radius only (cheap).
            foreach (var s in _sources)
            {
                int fOff = (int)s.Id;
                float r = s.Radius;
                float invR = 1f / r;

                // Bounding cell range for this source's radius.
                int minX = Mathf.Max(0, Mathf.FloorToInt((s.Pos.x - r - origin) / cell));
                int maxX = Mathf.Min(Grid - 1, Mathf.CeilToInt((s.Pos.x + r - origin) / cell));
                int minZ = Mathf.Max(0, Mathf.FloorToInt((s.Pos.z - r - origin) / cell));
                int maxZ = Mathf.Min(Grid - 1, Mathf.CeilToInt((s.Pos.z + r - origin) / cell));

                for (int cz = minZ; cz <= maxZ; cz++)
                {
                    float wz = origin + cz * cell;
                    float dz = wz - s.Pos.z;
                    for (int cx = minX; cx <= maxX; cx++)
                    {
                        float wx = origin + cx * cell;
                        float dx = wx - s.Pos.x;
                        float dist = Mathf.Sqrt(dx * dx + dz * dz);
                        if (dist >= r) continue;
                        // Radius falloff: 1 at the building, 0 at the edge.
                        float w = Mathf.Pow(1f - dist * invR, Falloff);
                        _influence[(cz * Grid + cx) * FactionCount + fOff] += w;
                    }
                }
            }

            // Resolve each cell's owner: highest influence wins unless it's too weak or contested.
            for (int i = 0; i < cells; i++)
            {
                int baseIdx = i * FactionCount;
                float best = 0f, second = 0f;
                int bestF = 0;
                for (int f = 0; f < FactionCount; f++)
                {
                    float v = _influence[baseIdx + f];
                    if (v > best) { second = best; best = v; bestF = f; }
                    else if (v > second) { second = v; }
                }

                bool claimed = best >= ClaimMin && (best <= 0f || second / best < ContestRatio);
                _owned[i] = claimed;
                _owner[i] = (FactionId)bestF;
            }
        }

        void CollectSources()
        {
            _sources.Clear();
            // Buildings carry a Faction; units carry a Unit (same convention BrandedHud uses to
            // tell them apart). A building is a Faction whose GameObject has no Unit component.
            foreach (var f in Faction.All)
            {
                if (f == null) continue;
                if (f.GetComponent<Unit>() != null) continue; // skip units
                _sources.Add(new Source
                {
                    Pos = f.transform.position,
                    Radius = RadiusFor(f),
                    Id = f.Id,
                });
            }
        }

        static float RadiusFor(Faction f)
        {
            if (f.GetComponent<TownCentre>() != null) return RadiusTownCentre;
            if (f.GetComponent<Tower>() != null) return RadiusTower;
            return RadiusOther;
        }

        bool TryCell(Vector3 world, out int cx, out int cz)
        {
            float cell = (GroundHalf * 2f) / Grid;
            cx = Mathf.FloorToInt((world.x + GroundHalf) / cell);
            cz = Mathf.FloorToInt((world.z + GroundHalf) / cell);
            return cx >= 0 && cz >= 0 && cx < Grid && cz < Grid;
        }

        // ---------------------------------------------------------------- render
        void BuildOverlay()
        {
            _tex = new Texture2D(Grid, Grid, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear, // soft blend between cells at borders
            };
            _pixels = new Color32[Grid * Grid];

            _mat = BuildOverlayMaterial(_tex);

            // A flat plane just above Ground. Unity's Plane is 10x10 at scale 1, so scale 8 -> 80x80.
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "TerritoryOverlay";
            var col = go.GetComponent<Collider>(); if (col) Destroy(col); // never block clicks
            go.transform.position = new Vector3(0f, 0.03f, 0f);
            go.transform.localScale = new Vector3((GroundHalf * 2f) / 10f, 1f, (GroundHalf * 2f) / 10f);
            go.GetComponent<Renderer>().material = _mat;
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.GetComponent<Renderer>().receiveShadows = false;
            _overlay = go.transform;
        }

        // Transparent unlit URP material (falls back to legacy Transparent if URP unlit is absent),
        // built in code so no asset import is required.
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
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // Bind the territory texture to whichever colour/map slot the shader exposes.
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
            return m;
        }

        void Redraw()
        {
            // Cache each faction's team colour once per redraw (cheap, but avoids per-cell lookups).
            var col = new Color[FactionCount];
            for (int f = 0; f < FactionCount; f++) col[f] = UnitConfig.BodyColor((FactionId)f);

            int cells = Grid * Grid;
            for (int i = 0; i < cells; i++)
            {
                if (_owned[i])
                {
                    Color c = col[(int)_owner[i]];
                    _pixels[i] = new Color32(
                        (byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f),
                        (byte)(TintAlpha * 255f));
                }
                else
                {
                    // Neutral or contested: blend the two strongest factions so borders stripe/mix.
                    BlendContested(i, col, out Color cc, out float a);
                    _pixels[i] = new Color32(
                        (byte)(cc.r * 255f), (byte)(cc.g * 255f), (byte)(cc.b * 255f),
                        (byte)(a * 255f));
                }
            }

            _tex.SetPixels32(_pixels);
            _tex.Apply(false);
        }

        // For an unclaimed cell, mix the colours of the factions contesting it, weighted by influence.
        // Cells with no influence at all stay fully transparent (bare ground shows through).
        void BlendContested(int cellIdx, Color[] col, out Color color, out float alpha)
        {
            int baseIdx = cellIdx * FactionCount;
            float total = 0f;
            Color sum = Color.clear;
            for (int f = 0; f < FactionCount; f++)
            {
                float v = _influence[baseIdx + f];
                if (v <= 0f) continue;
                total += v;
                sum += col[f] * v;
            }
            if (total <= 0f) { color = Color.clear; alpha = 0f; return; }
            color = sum / total;
            // Slightly fainter than owned cells so contested borders read as "in dispute".
            alpha = TintAlpha * 0.6f * Mathf.Clamp01(total);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_overlay != null) Destroy(_overlay.gameObject);
            if (_mat != null) Destroy(_mat);
            if (_tex != null) Destroy(_tex);
        }
    }
}
