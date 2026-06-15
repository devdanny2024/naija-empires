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
    }
}
