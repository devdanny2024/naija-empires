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
            public bool HasFixedTint; public Color FixedTint;
            // Raw = leave the imported materials/textures exactly as they came (downloaded OBJ/FBX that
            // ship their own colours). Skips BOTH the Kenney colormap auto-bind AND any tint. Set via
            // object-initializer: new Def("Tank", 0.8f) { Raw = true }.
            public bool Raw;
            public Def(string res, float scale, float yOffset = 0f, float rotY = 0f, bool tint = false)
            { Res = res; Scale = scale; YOffset = yOffset; RotY = rotY; Tint = tint; }

            /// A Def that always tints its model to a fixed colour (used for nature-kit models, which
            /// ship with vertex colours and NO colormap texture → they import ash-grey; we colour them
            /// in code instead). Whole-model flat tint (low-poly stylised look).
            public Def(string res, float scale, Color fixedTint, float yOffset = 0f, float rotY = 0f)
            { Res = res; Scale = scale; YOffset = yOffset; RotY = rotY; Tint = true; HasFixedTint = true; FixedTint = fixedTint; }
        }

        // key (BuildingKind / UnitType / "Tree"/"Rocks") -> model. Scales are first guesses — tune freely.
        static readonly Dictionary<string, Def> Map = new()
        {
            // Buildings — leaning earthen/village rather than European-square.
            // Kenney pieces share colormap.png (tint=false -> ApplyColormap binds it).
            // Scales/offsets below are eyeball-tunable first guesses; bump scale if a hut reads too small.
            { "TownCentre", new Def("tower-hexagon-base", 2.2f) },   // round chief's-compound footprint (was square tower)
            { "House",      new Def("hut-tent", 1.9f) },             // closed tent = village dwelling (nature kit; colormap-bound)
            { "Barracks",   new Def("tower-square-base", 1.4f) },    // war-camp hall (BarracksVisual adds fence+banner) — was a tombstone-like gate
            { "BarracksHall",  new Def("tower-square-base", 1.4f) }, // the swappable training-hall body of the war-camp
            { "BarracksFence", new Def("wall-narrow-wood", 1.2f) },  // wood-fence run framing the training yard
            { "BarracksFlag",  new Def("flag", 1.2f) },              // banner so the compound reads as military
            { "Stable",     new Def("tower-hexagon-mid", 1.6f) },    // low round structure (was square mid)
            { "Tower",      new Def("tower-slant-roof", 1.7f) },     // reverted: downloaded Tower.fbx rendered wrong (see notes)
            { "Wall",       new Def("wall-narrow-wood", 1.6f) },     // wooden palisade/stockade (was stone castle wall)
            { "Farm",       new Def("grass-large", 2.0f) },          // cultivated yam plot (nature kit; colormap-bound)
            { "University",  new Def("tower-square", 1.7f) },        // scholarly stone hall (square tower reads "institution")
            { "Market",     new Def("Building3_Big", 1.4f) { Raw = true } }, // downloaded big trade hall (was primitive fallback)

            // --- Upgrade tiers (Level 2 / Level 3). Composed from existing Kenney castle/nature pieces,
            // bigger/taller per tier so the structure visibly grows. Level 1 uses the plain key above.
            { "TownCentre_T2", new Def("tower-hexagon-base", 2.6f) }, // reverted: downloaded TC.fbx didn't render on upgrade
            { "TownCentre_T3", new Def("tower-hexagon-mid",  3.0f) },
            { "House_T2",      new Def("hut-tent", 2.3f) },           // reverted to working model (downloaded FBX unverified)
            { "House_T3",      new Def("hut-tent", 2.7f) },
            { "Barracks_T2",   new Def("tower-square-base", 1.7f) }, // bigger war-camp hall (was a gate slab)
            { "Barracks_T3",   new Def("tower-square", 1.9f) },     // tall stone hall at the top tier
            { "Stable_T2",     new Def("tower-hexagon-mid", 1.9f) },
            { "Stable_T3",     new Def("tower-hexagon-base", 2.2f) },
            { "Tower_T2",      new Def("tower-square-mid", 2.0f) },
            { "Tower_T3",      new Def("tower-square", 2.3f) },
            { "Wall_T2",       new Def("wall", 1.7f) },              // stone wall = sturdier upgrade over wood
            { "Wall_T3",       new Def("wall", 2.0f) },
            { "University_T2", new Def("tower-square-mid", 2.0f) },
            { "University_T3", new Def("tower-square-base", 2.3f) },

            // Resources / decor — Kenney Nature Kit. These ship with VERTEX COLOURS and no colormap
            // texture, so they import ash-grey under URP → we give each a fixed flat tint here.
            { "Tree",       new Def("tree-palm-tall", 1.6f, new Color(0.22f, 0.45f, 0.20f)) },  // green palm canopy
            { "Rocks",      new Def("rock-large-a", 1.4f, new Color(0.44f, 0.42f, 0.40f)) },     // warm stone (not ash)

            // Extra savanna decor used only for scatter variety in Bootstrap.Decorate (Kenney Nature Kit).
            { "TreePalmBend", new Def("tree-palm-bend", 1.6f, new Color(0.24f, 0.47f, 0.21f)) }, // leaning green palm
            { "Bush",         new Def("plant-bush", 1.4f, new Color(0.26f, 0.42f, 0.19f)) },     // green shrub
            { "Grass",        new Def("grass-large", 1.4f, new Color(0.42f, 0.56f, 0.24f)) },    // green grass tuft

            // Leafy forest trees used to ring the whole map with a dense forest border (Nature Kit).
            { "ForestTree",   new Def("tree_default_dark", 1.7f, new Color(0.17f, 0.36f, 0.17f)) }, // deep-green round tree
            { "ForestTreeB",  new Def("tree_oak_dark", 1.7f, new Color(0.19f, 0.33f, 0.16f)) },     // deep-green oak

            // Characters (animated Quaternius). Tint=false so their natural per-part colours
            // (skin/shirt/pants) show through — friend/foe is carried by TeamRing now, not by
            // flattening the whole body to one faction colour.
            // Scaled to ~human height relative to the huts (was 1.0 = too big).
            // RotY 0 = the animated FBX already face +Z (travel dir). (The earlier static meshes
            // needed 180; the animated pack exports the opposite way.) If they moonwalk, try 180.
            { "Villager",   new Def("Worker_Male", 0.62f, 0f, 0f, false) },
            { "Spearman",   new Def("Soldier_Male", 0.62f, 0f, 0f, false) },
            { "Archer",     new Def("Archer", 0.15f) { Raw = true } },  // downloaded archer — model is ~11u tall, 0.15 ≈ 1.7u
            { "Cavalry",    new Def("Knight_Male", 0.74f, 0f, 0f, false) },
            { "Scholar",    new Def("Adventurer", 0.6f) { Raw = true } }, // downloaded animated explorer → Scholar (had no model)

            // --- Modern Age content. Models pre-registered here now; the gameplay (new resource Oil,
            //     War Factory, Tank/Gunner/Catapult units, 5th age, Tower→turret upgrade, oil wells) is
            //     wired in a follow-up. All keep their own downloaded materials (Raw); scales are first
            //     guesses to tune in-editor.
            { "OilPump",     new Def("Oil pump", 1.2f) { Raw = true } },          // built on a discovered oil well
            { "WarFactory",  new Def("PUSHILIN_factory", 1.4f) { Raw = true } },  // trains Tanks + Gunners
            { "Tank",        new Def("Tank", 0.8f) { Raw = true } },
            { "Gunner",      new Def("Swat", 0.6f) { Raw = true } },              // animated SWAT trooper
            { "Rifleman",    new Def("soldierbackpack", 0.6f) { Raw = true } },   // NOTE: source texture missing → flat-shaded
            { "Catapult",    new Def("Catapult", 0.2f) { Raw = true } },           // model is ~10u tall, 0.2 ≈ 2.1u
            { "Tower_T4",    new Def("GatelngGunTurret", 1.3f) { Raw = true } },  // Tower's Modern-age gun-turret upgrade
            { "TownCentre_T4", new Def("skyscraperE", 1.8f) { Raw = true } },     // Modern-age capitol = skyscraper
        };

        /// True if the mapped model keeps its own imported materials (a downloaded model). Callers use
        /// this to avoid repainting it (e.g. UnitFactory skips CharacterColors for raw models).
        public static bool IsRaw(string key) => Map.TryGetValue(key, out var d) && d.Raw;

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

            if (d.Raw) { /* downloaded model keeps its own imported materials/textures */ }
            else if (d.Tint) Tint(go, d.HasFixedTint ? d.FixedTint : tintColor);
            else ApplyColormap(go); // Kenney models share colormap.png; ensure it's bound even if import didn't link it.
            return go;
        }

        /// Replace the existing "Model" child of a root with the model for a different key (used by
        /// Upgradeable to swap a building to its next-tier mesh). Destroys the old Model child, builds
        /// the new one, and returns it (or null if the key isn't mapped — caller keeps the old model).
        public static GameObject SwapModel(Transform root, string key, Color tintColor)
        {
            var old = root.Find("Model");
            var replacement = CreateModel(key, root, tintColor);
            if (replacement == null) return null;          // unknown key -> leave existing model intact
            if (old != null) Object.Destroy(old.gameObject);
            return replacement;
        }

        /// Animation clips embedded in the mapped FBX (animated characters). Empty for static models.
        public static AnimationClip[] LoadClips(string key)
        {
            if (!Map.TryGetValue(key, out var d)) return System.Array.Empty<AnimationClip>();
            var all = Resources.LoadAll<AnimationClip>("NE/Models/" + d.Res);
            var list = new List<AnimationClip>();
            foreach (var c in all)
                if (c != null && !c.name.StartsWith("__preview__")) list.Add(c);
            return list.ToArray();
        }

        static Texture _colormap;
        static bool _colormapLoaded;
        static Texture Colormap
        {
            get
            {
                if (!_colormapLoaded) { _colormap = Resources.Load<Texture>("NE/Models/colormap"); _colormapLoaded = true; }
                return _colormap;
            }
        }

        /// Bind the shared Kenney palette texture to any material that imported without one
        /// (the usual cause of white/plastic-looking buildings). No-op if a texture is already set.
        static void ApplyColormap(GameObject go)
        {
            var tex = Colormap;
            if (tex == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.materials;
                foreach (var m in mats)
                {
                    bool hasTex = (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null)
                               || (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null);
                    if (hasTex) continue;
                    if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                    if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
                    if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
                }
            }
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
