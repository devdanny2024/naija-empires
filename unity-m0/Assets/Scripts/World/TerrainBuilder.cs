using UnityEngine;

namespace NaijaEmpires
{
    /// Procedurally builds a stylized low-poly terrain mesh in code (no asset import):
    /// a grid of quads with gentle Perlin height noise, flat-shaded (each triangle gets its
    /// own vertices so normals are per-face), and biome-coloured via vertex colours — savanna
    /// grass in the centre fading to sandy shore toward the water edge.
    ///
    /// Gameplay stays FLAT: units move kinematically on y=0. The visible mesh height is kept
    /// subtle and dipped slightly below 0, and a separate flat BoxCollider at y=0 is the only
    /// thing clicks/raycasts hit. So the terrain is purely cosmetic — units never leave y~=0.
    public static class TerrainBuilder
    {
        // Mesh resolution: cells per side over the playable square. Higher = smoother silhouette
        // but more triangles. 48 over a 120-unit map = 2.5-unit quads, plenty for a low-poly look.
        const int Cells = 48;

        // Vertical noise amplitude (world units). Kept small so the flat-shaded facets read as
        // gentle rolling savanna, not mountains. The mesh is offset down by SurfaceDrop so its
        // average sits just under the y=0 gameplay plane.
        const float Amplitude = 1.3f;
        const float SurfaceDrop = 0.9f;   // push mesh down so peaks barely reach gameplay plane
        const float NoiseScale = 0.045f;  // lower = broader, gentler hills

        // Biome colours.
        static readonly Color GrassCentre = new Color(0.34f, 0.48f, 0.22f); // savanna grass
        static readonly Color GrassDry    = new Color(0.55f, 0.56f, 0.28f); // drier mid-ground
        static readonly Color Sand        = new Color(0.78f, 0.70f, 0.46f); // shoreline sand

        /// Build the terrain GameObject (named "Ground") plus its flat gameplay collider.
        /// Returns the root so the caller can parent/own it. The collider is a flat BoxCollider
        /// at y=0 so existing raycast/click code (which expects a flat ground) keeps working.
        public static GameObject Build()
        {
            var root = new GameObject("Ground");

            var mesh = BuildMesh();

            var mf = root.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = root.AddComponent<MeshRenderer>();
            mr.sharedMaterial = BuildMaterial(BuildBiomeTexture());
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Flat gameplay collider at y=0 spanning the whole playable square. This — not the
            // bumpy mesh — is what clicks and movement target raycasts hit, so units stay flat.
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.05f, 0f);
            col.size = new Vector3(MapBounds.Size, 0.1f, MapBounds.Size);

            return root;
        }

        static Mesh BuildMesh()
        {
            int verts = Cells + 1;
            float half = MapBounds.Half;
            float size = MapBounds.Size;
            float step = MapBounds.Size / Cells;

            // First pass: a smooth height/colour grid we can sample per corner.
            var gridHeight = new float[verts, verts];
            var gridColor = new Color[verts, verts];
            // Random noise origin so each match's terrain differs slightly.
            float ox = Random.Range(0f, 1000f);
            float oz = Random.Range(0f, 1000f);

            for (int z = 0; z <= Cells; z++)
            {
                for (int x = 0; x <= Cells; x++)
                {
                    float wx = -half + x * step;
                    float wz = -half + z * step;

                    // 0 at centre -> 1 at the square's edge (Chebyshev: square shoreline).
                    float edge = Mathf.Max(Mathf.Abs(wx), Mathf.Abs(wz)) / half;

                    float n = Mathf.PerlinNoise((wx + ox) * NoiseScale, (wz + oz) * NoiseScale);
                    // Flatten the rim down toward the water so the coast is low and beachy.
                    float coast = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.0f, 0.7f, edge));
                    float h = (n - 0.5f) * 2f * Amplitude * coast - SurfaceDrop;
                    // Sink the very edge below the waterline.
                    if (edge > 0.92f) h -= (edge - 0.92f) * 18f;
                    gridHeight[x, z] = h;

                    // Biome: grass centre -> dry mid -> sand at shore.
                    Color c = edge < 0.62f
                        ? Color.Lerp(GrassCentre, GrassDry, edge / 0.62f)
                        : Color.Lerp(GrassDry, Sand, Mathf.InverseLerp(0.62f, 0.95f, edge));
                    gridColor[x, z] = c;
                }
            }

            // Second pass: emit flat-shaded triangles — each quad gets 2 tris with their own
            // 6 vertices (no sharing) so Unity computes one normal per face = faceted low-poly.
            int quads = Cells * Cells;
            var vertices = new Vector3[quads * 6];
            var colors = new Color[quads * 6];
            var uvs = new Vector2[quads * 6];
            var tris = new int[quads * 6];
            int vi = 0;

            for (int z = 0; z < Cells; z++)
            {
                for (int x = 0; x < Cells; x++)
                {
                    float wx0 = -half + x * step;
                    float wz0 = -half + z * step;
                    float wx1 = wx0 + step;
                    float wz1 = wz0 + step;

                    Vector3 p00 = new Vector3(wx0, gridHeight[x, z], wz0);
                    Vector3 p10 = new Vector3(wx1, gridHeight[x + 1, z], wz0);
                    Vector3 p01 = new Vector3(wx0, gridHeight[x, z + 1], wz1);
                    Vector3 p11 = new Vector3(wx1, gridHeight[x + 1, z + 1], wz1);

                    // Average the 4 corner colours so each facet is a single flat tint.
                    Color quadCol = (gridColor[x, z] + gridColor[x + 1, z]
                                     + gridColor[x, z + 1] + gridColor[x + 1, z + 1]) * 0.25f;

                    // Tri A: p00, p01, p11
                    vertices[vi] = p00; vertices[vi + 1] = p01; vertices[vi + 2] = p11;
                    // Tri B: p00, p11, p10
                    vertices[vi + 3] = p00; vertices[vi + 4] = p11; vertices[vi + 5] = p10;
                    for (int k = 0; k < 6; k++) { colors[vi + k] = quadCol; tris[vi + k] = vi + k; }

                    // UVs map world XZ → [0,1] so the biome texture (sampled by URP/Lit's _BaseMap,
                    // which — unlike vertex colours — URP actually reads) paints grass→sand by position.
                    float u0 = (wx0 + half) / size, u1 = (wx1 + half) / size;
                    float v0 = (wz0 + half) / size, v1 = (wz1 + half) / size;
                    uvs[vi] = new Vector2(u0, v0); uvs[vi + 1] = new Vector2(u0, v1); uvs[vi + 2] = new Vector2(u1, v1);
                    uvs[vi + 3] = new Vector2(u0, v0); uvs[vi + 4] = new Vector2(u1, v1); uvs[vi + 5] = new Vector2(u1, v0);
                    vi += 6;
                }
            }

            var mesh = new Mesh { name = "LowPolyTerrain" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // >65k verts
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals(); // per-face normals (verts unshared) => flat shading
            mesh.RecalculateBounds();
            return mesh;
        }

        // A small biome texture: savanna grass in the centre fading to dry mid-ground then sandy shore
        // toward the edge (Chebyshev/square falloff to match the square shoreline). Generated in code,
        // no asset import. URP/Lit reads this via _BaseMap (it does NOT read mesh vertex colours, which
        // is why the earlier vertex-colour approach rendered the ground a flat washed-out tone).
        static Texture2D BuildBiomeTexture()
        {
            const int T = 128;
            var tex = new Texture2D(T, T, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[T * T];
            for (int y = 0; y < T; y++)
            for (int x = 0; x < T; x++)
            {
                float u = (x + 0.5f) / T, v = (y + 0.5f) / T;
                float edge = Mathf.Max(Mathf.Abs(u - 0.5f), Mathf.Abs(v - 0.5f)) * 2f; // 0 centre → 1 edge
                Color c = edge < 0.62f
                    ? Color.Lerp(GrassCentre, GrassDry, edge / 0.62f)
                    : Color.Lerp(GrassDry, Sand, Mathf.InverseLerp(0.62f, 0.95f, edge));
                px[y * T + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // URP-safe matte lit material carrying the biome texture in _BaseMap. Falls back to Standard
        // for the Built-in pipeline.
        static Material BuildMaterial(Texture2D biome)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(sh);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", biome);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", biome);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
            // Dial down smoothness/metallic so the ground reads matte (no plastic sheen / white blowout).
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.05f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            return m;
        }
    }
}
