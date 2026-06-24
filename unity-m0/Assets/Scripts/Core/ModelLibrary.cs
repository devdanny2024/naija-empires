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
            // Fit > 0 → ignore Scale and instead AUTO-scale the model so its height ≈ Fit world units,
            // then sit its base on the ground. Makes any downloaded model (authored at any size) come in
            // correct without hand-tuning Scale. Set via object-initializer: new Def("Tower", 0f){Fit=3.2f}.
            public float Fit;
            // Extra X-axis rotation to STAND a model up: some FBX are authored Z-up and import lying on
            // their side, so RotX = -90 tips them upright. Applied before Fit measures/ grounds the model.
            public float RotX;
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
            { "TownCentre", new Def("NE_TownCentre", 0f) { Fit = 3.2f, Raw = true, RotX = -90f } }, // custom Blender Town Centre (team banner = NE_Team slot)
            { "House",      new Def("NE_House", 0f) { Fit = 2.0f, Raw = true, RotX = -90f } }, // custom Blender round mud hut (team painted band = NE_Team)
            { "Barracks",   new Def("NE_Barracks", 0f) { Fit = 2.5f, Raw = true, RotX = -90f } }, // custom Blender war-camp (fence, spears, shields, banner)
            { "BarracksHall",  new Def("tower-square-base", 1.4f) }, // the swappable training-hall body of the war-camp
            { "BarracksFence", new Def("wall-narrow-wood", 1.2f) },  // wood-fence run framing the training yard
            { "BarracksFlag",  new Def("flag", 1.2f) },              // banner so the compound reads as military
            { "Stable",     new Def("NE_Stable", 0f) { Fit = 2.4f, Raw = true, RotX = -90f } }, // custom Blender stable (paddock, hay, banner)
            { "Tower",      new Def("NE_Tower", 0f) { Fit = 3.4f, Raw = true, RotX = -90f } }, // custom Blender stone watchtower (crenellations, lookout roof, NE_Team flag)
            { "Wall",       new Def("NE_Wall", 0f) { Fit = 1.6f, Raw = true, RotX = -90f } }, // custom Blender Sahel rampart segment
            { "Farm",       new Def("NE_Farm", 0f) { Fit = 1.0f, Raw = true, RotX = -90f } }, // custom Blender yam-mound plot
            { "University",  new Def("NE_University", 0f) { Fit = 3.0f, Raw = true, RotX = -90f } }, // custom Blender Sankore-style hall (toron minaret)
            { "Market",     new Def("NE_Market", 0f) { Fit = 2.4f, Raw = true, RotX = -90f } }, // custom Blender trade pavilion (stalls, awning, banner)

            // Hand-crafted map terrain (Terrain + lake water joined into one mesh). Scale 1 (no Fit —
            // it's authored at world size), RotX -90 to lay the Z-up Blender sheet flat like the buildings.
            { "Terrain",    new Def("NE_Terrain", 1f) { Raw = true, RotX = -90f } },

            // --- Upgrade tiers. Custom Blender models that EVOLVE traditional → modern across the ages:
            // T2 = grander traditional, T3 = modern, T4 = grand modern. Each is a single-mesh Z-up FBX
            // (Raw + RotX -90), auto-fit to a growing target height so the structure visibly upgrades.
            { "TownCentre_T2", new Def("NE_TownCentre_T2", 0f) { Fit = 3.6f, Raw = true, RotX = -90f } }, // Iron: grander mud capitol
            { "TownCentre_T3", new Def("NE_TownCentre_T3", 0f) { Fit = 4.6f, Raw = true, RotX = -90f } }, // Bronze/Golden: modern mid-rise
            { "TownCentre_T4", new Def("NE_TownCentre_T4", 0f) { Fit = 6.5f, Raw = true, RotX = -90f } }, // Modern: glass high-rise capitol
            { "House_T2",      new Def("NE_House_T2", 0f) { Fit = 2.4f, Raw = true, RotX = -90f } },       // grander mud hut compound
            { "House_T3",      new Def("NE_House_T3", 0f) { Fit = 2.6f, Raw = true, RotX = -90f } },       // modern bungalow
            { "House_T4",      new Def("NE_House_T4", 0f) { Fit = 3.2f, Raw = true, RotX = -90f } },       // modern 2-storey block
            { "Barracks_T2",   new Def("NE_Barracks_T2", 0f) { Fit = 2.8f, Raw = true, RotX = -90f } },   // grander war-camp
            { "Barracks_T3",   new Def("NE_Barracks_T3", 0f) { Fit = 3.0f, Raw = true, RotX = -90f } },   // modern armory/bunker
            { "Barracks_T4",   new Def("NE_Barracks_T4", 0f) { Fit = 3.4f, Raw = true, RotX = -90f } },   // modern military HQ
            { "University_T2", new Def("NE_University_T2", 0f) { Fit = 3.4f, Raw = true, RotX = -90f } },  // grander Sankore hall
            { "University_T3", new Def("NE_University_T3", 0f) { Fit = 3.2f, Raw = true, RotX = -90f } },  // modern college
            { "University_T4", new Def("NE_University_T4", 0f) { Fit = 5.2f, Raw = true, RotX = -90f } },  // modern research tower
            { "Stable_T2",     new Def("NE_Stable_T2", 0f) { Fit = 2.6f, Raw = true, RotX = -90f } },     // grander stable
            { "Stable_T3",     new Def("NE_Stable_T3", 0f) { Fit = 2.4f, Raw = true, RotX = -90f } },     // modern stable/garage
            { "Stable_T4",     new Def("NE_Stable_T4", 0f) { Fit = 2.8f, Raw = true, RotX = -90f } },     // modern motor pool
            { "Market_T2",     new Def("NE_Market_T2", 0f) { Fit = 2.6f, Raw = true, RotX = -90f } },     // grander market pavilion
            { "Market_T3",     new Def("NE_Market_T3", 0f) { Fit = 2.6f, Raw = true, RotX = -90f } },     // modern shop
            { "Market_T4",     new Def("NE_Market_T4", 0f) { Fit = 3.2f, Raw = true, RotX = -90f } },     // modern commercial block
            // Tower & Wall keep their downloaded tier models for now (no Blender variants built yet).
            { "Tower_T2",      new Def("NE_Tower", 0f) { Fit = 3.8f, Raw = true, RotX = -90f } },                    // taller custom watchtower (was the sideways downloaded mesh)
            { "Tower_T3",      new Def("GatelngGunTurret", 0f) { Fit = 3.0f, Raw = true, RotX = -90f } },            // Age 3: gun-turret
            { "Wall_T2",       new Def("wall", 1.7f) },              // stone wall = sturdier upgrade over wood
            { "Wall_T3",       new Def("wall", 2.0f) },

            // Resources / decor — custom Blender models (ship their own flat materials → Raw; Z-up → RotX -90).
            { "Tree",       new Def("NE_Tree", 0f) { Fit = 5.0f, Raw = true, RotX = -90f } },   // Blender low-poly tree
            { "Rocks",      new Def("NE_OreIron", 0f) { Fit = 1.4f, Raw = true, RotX = -90f } }, // Blender iron-ore rock

            // Extra savanna decor used only for scatter variety in Bootstrap.Decorate.
            { "TreePalmBend", new Def("NE_Tree", 0f) { Fit = 4.6f, Raw = true, RotX = -90f } },  // Blender tree (slightly shorter for variety)
            { "Bush",         new Def("NE_Tree", 0f) { Fit = 1.3f, Raw = true, RotX = -90f } },  // small Blender tree as a shrub (no dedicated bush yet)
            { "Grass",        new Def("grass-large", 1.4f, new Color(0.42f, 0.56f, 0.24f)) },    // Kenney tuft (no Blender grass yet)

            // Leafy forest trees used to ring the whole map with a dense forest border.
            { "ForestTree",   new Def("NE_Tree", 0f) { Fit = 5.0f, Raw = true, RotX = -90f } },  // Blender tree
            { "ForestTreeB",  new Def("NE_Tree", 0f) { Fit = 5.5f, Raw = true, RotX = -90f } },  // Blender tree (taller variant)

            // Resource nodes — custom Blender models.
            { "Yam",          new Def("NE_Yam", 0f) { Fit = 0.9f, Raw = true, RotX = -90f } },         // food yam plant
            { "IronMountain", new Def("NE_IronMountain", 0f) { Fit = 7f, Raw = true, RotX = -90f } }, // iron mountain — big enough to read as a mountain next to the base
            { "OreGold",      new Def("NE_OreGold", 0f) { Fit = 1.6f, Raw = true, RotX = -90f } },      // rich/gold deposit variant
            { "OilWell",      new Def("NE_OilWell", 1.0f) { Raw = true, RotX = -90f } },                // flat oil pool (Scale, not Fit — zero height)

            // Characters (animated Quaternius). Tint=false so their natural per-part colours
            // (skin/shirt/pants) show through — friend/foe is carried by TeamRing now, not by
            // flattening the whole body to one faction colour.
            // Scaled to ~human height relative to the huts (was 1.0 = too big).
            // RotY 0 = the animated FBX already face +Z (travel dir). (The earlier static meshes
            // needed 180; the animated pack exports the opposite way.) If they moonwalk, try 180.
            { "Villager",   new Def("NE_Villager", 0f) { Fit = 1.7f, Raw = true } }, // custom rigged Blender villager (Walk/Idle clips, NE_Team slot)
            { "Scout",      new Def("NE_Villager", 0f) { Fit = 1.55f, Raw = true } }, // reuses villager rig for now (lighter); custom model later
            { "Spearman",   new Def("NE_Spearman", 0f) { Fit = 1.8f, Raw = true } }, // custom rigged Blender spearman (Idle/Walk/Attack, helmet+spear, NE_Team)
            { "Archer",     new Def("NE_Archer", 0f) { Fit = 1.75f, Raw = true } }, // custom rigged Blender archer (Idle/Walk/Shoot, bow+quiver, NE_Team)
            { "Cavalry",    new Def("NE_Cavalry", 0f) { Fit = 2.0f, Raw = true } }, // custom rigged Blender cavalry rider + mount (NE_Team)
            { "Caravan",    new Def("NE_Caravan", 0f) { Fit = 1.7f, Raw = true, RotX = -90f } }, // custom Blender covered trade cart (team canopy)
            { "Scholar",    new Def("Adventurer", 0.6f) { Raw = true } }, // downloaded animated explorer → Scholar (had no model)

            // --- Modern Age content. Models pre-registered here now; the gameplay (new resource Oil,
            //     War Factory, Tank/Gunner/Catapult units, 5th age, Tower→turret upgrade, oil wells) is
            //     wired in a follow-up. All keep their own downloaded materials (Raw); scales are first
            //     guesses to tune in-editor.
            { "OilPump",     new Def("NE_OilPump", 0f) { Fit = 2.2f, Raw = true, RotX = -90f } }, // custom Blender pumpjack (replaced corrupt OBJ)
            { "WarFactory",  new Def("NE_WarFactory", 0f) { Fit = 3.0f, Raw = true, RotX = -90f } },  // custom Blender industrial works (smokestack, tank door)
            { "Tank",        new Def("NE_Tank", 0f) { Fit = 1.9f, Raw = true, RotX = -90f } }, // custom Blender tank (built facing -Y → faces travel dir; turret gun + team panel)
            { "RocketVehicle", new Def("NE_RocketVehicle", 0f) { Fit = 2.0f, Raw = true, RotX = -90f } }, // Catapult's modern form (tracked hull, 4 rocket tubes, NE_Team panel; built facing -Y)
            { "Gunner",      new Def("NE_Gunner", 0f) { Fit = 1.8f, Raw = true } },   // custom rigged Blender gunner (Idle/Walk/Shoot, SMG + tactical helmet)
            { "Rifleman",    new Def("NE_Rifleman", 0f) { Fit = 1.8f, Raw = true } }, // custom rigged Blender rifleman (Idle/Walk/Shoot, rifle + olive helmet)
            { "Catapult",    new Def("NE_Catapult", 0f) { Fit = 2.2f, Raw = true, RotX = -90f } }, // custom Blender catapult (frame, wheels, loaded arm, team pennant)
            { "Tower_T4",    new Def("GatelngGunTurret", 0f) { Fit = 3.2f, Raw = true, RotX = -90f } },  // Modern-age gun-turret
            // (TownCentre_T4 now defined with the other custom Blender tiers above.)
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
            go.transform.localRotation = Quaternion.Euler(d.RotX, d.RotY, 0f); // RotX stands up Z-up models

            // The gameplay collider lives on the root; strip any from the imported mesh.
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);

            if (d.Fit > 0f)
                FitAndGround(go, d.Fit, d.YOffset);          // auto-size any model to the right height
            else
            {
                go.transform.localPosition = new Vector3(0f, d.YOffset, 0f);
                go.transform.localScale = Vector3.one * d.Scale;
            }

            if (d.Raw) { /* downloaded model keeps its own imported materials/textures */ }
            else if (d.Tint) Tint(go, d.HasFixedTint ? d.FixedTint : tintColor);
            else ApplyColormap(go); // Kenney models share colormap.png; ensure it's bound even if import didn't link it.
            return go;
        }

        // Scale a freshly-instantiated model so its rendered height ≈ targetHeight world units, then drop
        // it so its base sits at the parent's ground (y=0), plus an optional extra Y nudge. Works for any
        // model regardless of how big it was authored — no per-model Scale guessing. Falls back to scale 1
        // if the model has no renderers / zero-height bounds (e.g. a corrupt mesh).
        static void FitAndGround(GameObject go, float targetHeight, float extraY)
        {
            go.transform.localScale = Vector3.one;
            go.transform.localPosition = Vector3.zero;
            if (!WorldBounds(go, out var b) || b.size.y < 1e-4f) { go.transform.localScale = Vector3.one; return; }

            float k = targetHeight / b.size.y;
            go.transform.localScale = Vector3.one * k;

            // Re-measure after scaling and lift so the base touches the parent ground plane (y=0 local).
            if (WorldBounds(go, out var b2))
            {
                float parentY = go.transform.parent != null ? go.transform.parent.position.y : 0f;
                go.transform.localPosition = new Vector3(0f, (parentY - b2.min.y) + extraY, 0f);
            }
        }

        // Combined world-space bounds of every renderer under `go` (false if it has none).
        static bool WorldBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return false;
            bounds = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) bounds.Encapsulate(rends[i].bounds);
            return true;
        }

        /// Replace the existing "Model" child of a root with the model for a different key (used by
        /// Upgradeable to swap a building to its next-tier mesh). Destroys the old Model child, builds
        /// the new one, and returns it (or null if the key isn't mapped — caller keeps the old model).
        public static GameObject SwapModel(Transform root, string key, Color tintColor)
        {
            var old = root.Find("Model");
            var replacement = CreateModel(key, root, tintColor);
            if (replacement == null)
            {
                Debug.LogWarning($"[NE] SwapModel('{key}') FAILED — Resources.Load returned null (model not imported?).");
                return null;            // keep existing only because there is no new model at all
            }
            // DIAGNOSTIC: report exactly what the new model looks like so we can see why it may not draw.
            var rends = replacement.GetComponentsInChildren<Renderer>();
            int withMesh = 0;
            Bounds bb = new Bounds(replacement.transform.position, Vector3.zero);
            foreach (var r in rends)
            {
                bb.Encapsulate(r.bounds);
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.vertexCount > 0) withMesh++;
            }
            Debug.Log($"[NE] Swap '{key}': renderers={rends.Length} withMesh={withMesh} " +
                      $"size={bb.size:F2} center={bb.center:F2} scale={replacement.transform.lossyScale:F2}");

            // No fallback — always use the new model (per design: new models only).
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
