using UnityEngine;

namespace NaijaEmpires
{
    /// Sets sensible flat colours on an animated character's "Model" child, keyed by material
    /// part-name (Skin/Shirt/Pants/...). The Quaternius FBX import their per-part colours
    /// unreliably (often black, or sampling the wrong palette), so we assign them in code —
    /// earthy West-African tones. Runs in Start (after the model has been instantiated).
    public class CharacterColors : MonoBehaviour
    {
        void Start()
        {
            var model = transform.Find("Model");
            if (model == null) return;

            foreach (var r in model.GetComponentsInChildren<Renderer>())
                foreach (var m in r.materials) // instanced per unit — fine, matches existing Tint()
                    Apply(m, ColorFor(m.name));
        }

        static void Apply(Material m, Color c)
        {
            // Flat colour, no texture — stops it sampling the wrong (Kenney) colormap as black.
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", null);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        static Color ColorFor(string matName)
        {
            string n = matName.ToLowerInvariant();
            if (n.Contains("skin") || n.Contains("face") || n.Contains("head") || n.Contains("hand"))
                return new Color(0.55f, 0.38f, 0.26f);   // brown skin
            if (n.Contains("hair") || n.Contains("beard") || n.Contains("eye"))
                return new Color(0.10f, 0.08f, 0.06f);   // near-black hair/eyes
            if (n.Contains("pant") || n.Contains("short") || n.Contains("trouser") || n.Contains("leg"))
                return new Color(0.32f, 0.24f, 0.17f);   // earth-brown trousers
            if (n.Contains("shoe") || n.Contains("boot") || n.Contains("foot"))
                return new Color(0.20f, 0.14f, 0.10f);   // dark leather
            if (n.Contains("hat") || n.Contains("helmet") || n.Contains("cap"))
                return new Color(0.48f, 0.34f, 0.20f);
            if (n.Contains("metal") || n.Contains("armor") || n.Contains("armour") ||
                n.Contains("sword") || n.Contains("iron") || n.Contains("steel"))
                return new Color(0.58f, 0.60f, 0.66f);   // steel
            if (n.Contains("shirt") || n.Contains("vest") || n.Contains("torso") ||
                n.Contains("body") || n.Contains("cloth") || n.Contains("tunic"))
                return new Color(0.82f, 0.74f, 0.55f);   // sand/cream tunic
            return new Color(0.72f, 0.62f, 0.46f);       // neutral cloth — never black
        }
    }
}
