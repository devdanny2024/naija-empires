using System.Collections.Generic;
using UnityEngine;

namespace NaijaEmpires
{
    /// Loads real art models (from Resources/NE/Models) and attaches them as a child named "Model".
    /// If a model is missing, returns null so the caller falls back to a primitive — the game never breaks.
    ///
    /// TUNING: all per-model scale / vertical offset / rotation lives in the Map below. If a building looks
    /// too small/large or floats, change its numbers here only.
    public static class ModelLibrary
    {
        public class Def
        {
            public string Res; public float Scale; public float YOffset; public float RotY; public bool Tint;
            public Def(string res, float scale, float yOffset = 0f, float rotY = 0f, bool tint = false)
            { Res = res; Scale = scale; YOffset = yOffset; RotY = rotY; Tint = tint; }
        }

        // key (BuildingKind / UnitType / "Tree"/"Rocks") -> model. Scales are first guesses — tune freely.
        static readonly Dictionary<string, Def> Map = new()
        {
            // Buildings (Kenney Castle Kit, textured)
            { "TownCentre", new Def("tower-square", 2.0f) },
            { "House",      new Def("tower-square-base", 1.6f) },
            { "Barracks",   new Def("gate", 1.6f) },
            { "Stable",     new Def("tower-square-mid", 1.6f) },
            { "Tower",      new Def("tower-slant-roof", 1.7f) },
            { "Wall",       new Def("wall", 1.6f) },

            // Resources / decor (Kenney Castle Kit)
            { "Tree",       new Def("tree-large", 1.4f) },
            { "Rocks",      new Def("rocks-large", 1.4f) },

            // Characters (Quaternius, vertex-colored -> we tint by faction).
            // Scaled to ~human height relative to the huts (was 1.0 = too big).
            { "Villager",   new Def("Worker_Male", 0.62f, 0f, 0f, true) },
            { "Spearman",   new Def("Soldier_Male", 0.62f, 0f, 0f, true) },
            { "Archer",     new Def("BlueSoldier_Male", 0.62f, 0f, 0f, true) },
            { "Cavalry",    new Def("Knight_Male", 0.74f, 0f, 0f, true) },
        };

        /// Instantiate the mapped model as a child named "Model". Returns null if not mapped/loadable.
        public static GameObject CreateModel(string key, Transform parent, Color tintColor)
        {
            if (!Map.TryGetValue(key, out var d)) return null;
            var prefab = Resources.Load<GameObject>("NE/Models/" + d.Res);
            if (prefab == null) return null;

            var go = Object.Instantiate(prefab);
            go.name = "Model";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, d.YOffset, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, d.RotY, 0f);
            go.transform.localScale = Vector3.one * d.Scale;

            // The gameplay collider lives on the root; strip any from the imported mesh.
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);

            if (d.Tint) Tint(go, tintColor);
            return go;
        }

        static void Tint(GameObject go, Color c)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i].HasProperty("_BaseColor")) mats[i].SetColor("_BaseColor", c);
                    if (mats[i].HasProperty("_Color")) mats[i].SetColor("_Color", c);
                }
            }
        }
    }
}
