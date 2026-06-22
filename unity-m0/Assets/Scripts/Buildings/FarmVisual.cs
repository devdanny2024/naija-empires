using UnityEngine;

namespace NaijaEmpires
{
    /// The shared crop-plot look for farms: a tilled brown soil bed with rows of green crops.
    /// Used BOTH by the starting farm node (Bootstrap) and the buildable Farm building, so the
    /// build-menu Farm matches the very first farm the villagers work at the start of the game.
    public static class FarmVisual
    {
        static readonly Color Soil = new Color(0.45f, 0.30f, 0.18f);
        static readonly Color Crop = new Color(0.50f, 0.66f, 0.20f);

        /// Build the crop-plot under a child named "Model" (so Construction can scale it, and the
        /// upgrade/animator code can find it). `footprint` is the plot's side length in world units.
        public static GameObject Build(Transform parent, float footprint)
        {
            var model = new GameObject("Model");
            model.transform.SetParent(parent, false);

            float s = footprint / 1.8f; // base art was authored for a 1.8-unit plot

            // tilled soil bed
            Prim(model.transform, new Vector3(0f, 0.1f * s, 0f),
                 new Vector3(1.8f * s, 0.2f * s, 1.8f * s), Soil);

            // three crop rows running across the bed
            for (int i = -1; i <= 1; i++)
                Prim(model.transform, new Vector3(i * 0.5f * s, 0.26f * s, 0f),
                     new Vector3(0.18f * s, 0.18f * s, 1.5f * s), Crop);

            return model;
        }

        static void Prim(Transform parent, Vector3 localPos, Vector3 localScale, Color c)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = g.GetComponent<Collider>(); if (col) Object.Destroy(col);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = localScale;
            MaterialUtil.SetColor(g.GetComponent<Renderer>(), c);
        }
    }
}
