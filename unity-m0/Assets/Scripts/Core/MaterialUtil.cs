using UnityEngine;

namespace NaijaEmpires
{
    /// Tiny helper so the prototype works in BOTH the Built-in and URP render pipelines
    /// without needing any material assets. Sets colour on whichever property exists.
    public static class MaterialUtil
    {
        static Shader _shader;

        static Shader GetShader()
        {
            if (_shader == null)
                _shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return _shader;
        }

        public static void SetColor(Renderer r, Color c)
        {
            var m = new Material(GetShader());
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            r.material = m;
        }

        static Shader _unlit;
        static Shader GetUnlit() =>
            _unlit != null ? _unlit
                           : (_unlit = Shader.Find("Universal Render Pipeline/Unlit")
                                       ?? Shader.Find("Unlit/Color") ?? GetShader());

        /// Always-bright flat colour (ignores scene lighting) — used for selection rings/markers so
        /// they pop the same regardless of time-of-day shading. Supports transparency via the alpha.
        public static void SetGlow(Renderer r, Color c)
        {
            var m = new Material(GetUnlit());
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            // Also push emission on Lit-style shaders so it glows even if the unlit shader was unavailable.
            if (m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c); }
            r.material = m;
        }

        /// Recolour any material slot whose name contains `nameContains` (e.g. "NE_Team") on every
        /// renderer under `root`, instancing the materials so each unit carries its own faction colour.
        /// No-op if no matching slot exists. Used to tint a unit's team band/sash to its empire colour.
        public static void TintSlot(GameObject root, string nameContains, Color c)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                var mats = r.materials; // accessing .materials instances them (per-renderer copies)
                bool hit = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || !mats[i].name.Contains(nameContains)) continue;
                    if (mats[i].HasProperty("_BaseColor")) mats[i].SetColor("_BaseColor", c);
                    if (mats[i].HasProperty("_Color")) mats[i].SetColor("_Color", c);
                    hit = true;
                }
                if (hit) r.materials = mats;
            }
        }
    }
}
