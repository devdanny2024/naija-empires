using UnityEngine;

namespace NaijaEmpires
{
    /// Builds the whole M1 skirmish from code: ground, camera, two faction bases (Player = Benin,
    /// Enemy = Oyo + AI), resources, and managers. Drop on one empty GameObject and press Play.
    public class Bootstrap : MonoBehaviour
    {
        // Base spawn points for this match — randomised across the map each game (no fixed corners),
        // but kept apart so no two empires start too close. Generated in Awake.
        Vector3[] _basePoints;
        int _activeBases;

        void Awake()
        {
            Match.Reset();
            BuildGround();
            BuildLight();

            int n = MatchConfig.Count;
            _activeBases = n;
            _basePoints = GenerateBasePoints(n);

            BuildCamera(_basePoints[0]); // camera starts on the human's base (seat 0)
            BuildManagers();

            // Central contested iron node.
            SpawnNode(ResourceType.Iron, Vector3.zero, Color.gray);

            // One base per empire: economy, resources, Town Centre, starting villagers, and an AI bot
            // for every non-human seat (single-player FFA).
            for (int i = 0; i < n; i++)
            {
                FactionId id = MatchConfig.SeatId(i);
                Civ civ = MatchConfig.CivFor(i);
                Vector3 basePos = _basePoints[i];

                int bonus = civ == Civ.KanemBornu ? 100 : 0; // Kanem-Bornu trade perk: extra starting resources
                int start = GameDebug.TestMode ? 99999 : 0;  // test mode: visibly stocked resource bar
                var econ = new Economy(220 + bonus + start, 220 + bonus + start, 120 + bonus + start, civ);
                if (GameDebug.TestMode) { econ.Cowries = start; econ.Knowledge = start; }
                Match.Register(id, econ);

                SpawnNodeCluster(basePos);
                BuildingFactory.Spawn(BuildingKind.TownCentre, basePos, id);
                StartVillagers(id, basePos);

                if (MatchConfig.IsAI(i))
                {
                    var aiGo = new GameObject(id + " AI");
                    var ai = aiGo.AddComponent<EnemyAI>();
                    ai.Owner = id;
                    ai.basePos = basePos;
                }
            }

            Decorate();
            SeedHiddenResources();
            SpawnOilWells();
            Destroy(gameObject);
        }

        // Lake centres (Unity XZ) + radius, from the Blender terrain. Blender (x,y) maps to Unity (x,-y)
        // under the terrain's RotX -90. Bases and scatter decor avoid these so nothing spawns in water.
        static readonly (Vector2 c, float r)[] Lakes =
        {
            (new Vector2(-52f, -42f), 28f),  // LakeWater_0
            (new Vector2( 62f,  58f), 34f),  // LakeWater_1
            (new Vector2( 24f, -78f), 21f),  // LakeWater_2
        };

        // True if p (XZ) is within `margin` of any lake — used to keep bases/decor out of the water.
        static bool InLake(Vector3 p, float margin)
        {
            var xz = new Vector2(p.x, p.z);
            foreach (var l in Lakes)
                if ((xz - l.c).magnitude < l.r + margin) return true;
            return false;
        }

        // Pick a random spawn point per empire: spread across the playable square (inset from the
        // shoreline) but never closer than MinBaseSpacing to another base, so nobody starts on top of a
        // rival (and rushes take time). Rejection-samples; falls back to corners if it can't place all.
        Vector3[] GenerateBasePoints(int n)
        {
            const float minSpacing = 95f;     // empires kept well apart on the ~240-unit map
            float inset = MapBounds.Half - MapBounds.BaseInset; // keep bases off the shore/forest
            var pts = new System.Collections.Generic.List<Vector3>();

            int attempts = 0;
            while (pts.Count < n && attempts < 600)
            {
                attempts++;
                var p = new Vector3(Random.Range(-inset, inset), 0f, Random.Range(-inset, inset));
                if (p.magnitude < 32f) continue; // keep clear of the central iron mountain at the origin
                if (InLake(p, 22f)) continue;    // never start a base in or near a lake (generous margin so the TC + its platform stay on dry land)
                bool ok = true;
                foreach (var q in pts) if (Vector3.Distance(p, q) < minSpacing) { ok = false; break; }
                if (ok) pts.Add(p);
            }

            // If tight constraints starved the sampler, top up from the four far corners (always dry land).
            Vector3[] corners =
            {
                new Vector3(-inset, 0f, -inset), new Vector3(inset, 0f, inset),
                new Vector3(inset, 0f, -inset), new Vector3(-inset, 0f, inset),
            };
            for (int c = 0; c < corners.Length && pts.Count < n; c++)
                pts.Add(corners[c]);

            return pts.ToArray();
        }

        // Scatter hidden deposits across the map (away from the bases). Each stays invisible until the
        // Player's fog of war reveals its tile, then it triggers a one-time economy bonus + a gatherable
        // node. Seeded here so map content lives in one place.
        void SeedHiddenResources()
        {
            float range = MapBounds.Half - 10f;
            int target = 8;
            int placed = 0, attempts = 0;
            while (placed < target && attempts < target * 12)
            {
                attempts++;
                Vector3 p = new Vector3(Random.Range(-range, range), 0f, Random.Range(-range, range));

                if (InLake(p, 4f)) continue; // no hidden deposits in the water

                bool nearBase = false;
                for (int b = 0; b < _activeBases; b++)
                    if (Vector3.Distance(p, _basePoints[b]) < 16f) { nearBase = true; break; }
                if (nearBase) continue;

                var go = new GameObject("HiddenDeposit");
                go.transform.position = p;
                var hr = go.AddComponent<HiddenResource>();
                hr.Type = (ResourceType)Random.Range(0, 3);
                placed++;
            }
        }

        // Oil wells: a RARE surface resource scattered across the map (≥35). An Oil Pump can only be
        // built ON a well (see BuildPlacer), and wells are plotted on the minimap so the player finds
        // them by scouting. Kept out of the lakes, the central iron mountain, and starting bases.
        void SpawnOilWells()
        {
            const int target = 38;
            float range = MapBounds.Half - 8f;
            int placed = 0, attempts = 0;
            while (placed < target && attempts < target * 25)
            {
                attempts++;
                Vector3 p = new Vector3(Random.Range(-range, range), 0f, Random.Range(-range, range));
                if (InLake(p, 4f)) continue;          // no oil in the water
                if (p.magnitude < 30f) continue;      // clear of the central iron mountain
                bool nearBase = false;
                for (int b = 0; b < _activeBases; b++)
                    if (Vector3.Distance(p, _basePoints[b]) < 18f) { nearBase = true; break; }
                if (nearBase) continue;

                var go = new GameObject("OilWell");
                go.transform.position = p;
                go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                go.AddComponent<OilWell>();
                BuildOilWellVisual(go.transform);
                placed++;
            }
        }

        // A dark oily seep on the ground + a couple of glistening bubbles (cheap primitives).
        void BuildOilWellVisual(Transform parent)
        {
            Color oil   = new Color(0.07f, 0.06f, 0.09f);
            Color sheen = new Color(0.22f, 0.16f, 0.30f);
            Prim(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.04f, 0f), new Vector3(2.4f, 0.04f, 2.4f), oil);
            Prim(PrimitiveType.Sphere, parent, new Vector3(0.45f, 0.12f, 0.25f), new Vector3(0.55f, 0.3f, 0.55f), sheen);
            Prim(PrimitiveType.Sphere, parent, new Vector3(-0.5f, 0.1f, -0.35f), new Vector3(0.4f, 0.24f, 0.4f), oil);
        }

        void BuildGround()
        {
            // Hand-crafted terrain from Blender — routed through ModelLibrary so it gets the exact
            // same orientation/material handling as the buildings (single-mesh FBX, RotX -90).
            var ground = new GameObject("Ground");
            var terrainModel = ModelLibrary.CreateModel("Terrain", ground.transform, Color.white);
            if (terrainModel != null)
            {
                // Flat gameplay collider at y=0 so units/clicks stay on the flat plane.
                var col = ground.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, -0.05f, 0f);
                col.size = new Vector3(MapBounds.Size, 0.1f, MapBounds.Size);
            }
            else
            {
                // Fallback (no Blender terrain): procedural mesh + a dark forest floor underneath so
                // the void beyond the island reads as woodland rather than blue skybox.
                Destroy(ground);
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "ForestFloor";
                floor.transform.position = new Vector3(0f, -0.4f, 0f);
                float floorScale = (MapBounds.Size * 1.6f) / 10f; // Unity Plane is 10x10 at scale 1
                floor.transform.localScale = new Vector3(floorScale, 1f, floorScale);
                var fc = floor.GetComponent<Collider>(); if (fc) Destroy(fc);
                MaterialUtil.SetColor(floor.GetComponent<Renderer>(), new Color(0.13f, 0.20f, 0.12f));
                TerrainBuilder.Build();
            }

            // Ring the whole playable square with a dense forest so the edge reads as woodland.
            BuildForestBorder();
        }

        // A thick band of trees surrounding the playable square — the map's natural border. Trees are
        // pure decor (no colliders / gameplay); a mix of leafy forest trees and palms for variety.
        void BuildForestBorder()
        {
            float inner = MapBounds.Half - 4f;   // start just inside the shore so the forest frames the island
            float outer = MapBounds.Half + 40f;  // forest extends well past the edge for a deep surround
            const float step = 5.5f;             // tighter spacing than before = denser wall of trees
            for (float x = -outer; x <= outer; x += step)
            {
                for (float z = -outer; z <= outer; z += step)
                {
                    if (Mathf.Abs(x) < inner && Mathf.Abs(z) < inner) continue; // skip the playable interior
                    var holder = new GameObject("Forest");
                    holder.transform.position = new Vector3(x + Random.Range(-2.4f, 2.4f), 0f, z + Random.Range(-2.4f, 2.4f));
                    holder.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    holder.transform.localScale = Vector3.one * Random.Range(0.85f, 1.35f);
                    BuildDecor(PickForestTree(), holder.transform);
                }
            }
        }

        static string PickForestTree()
        {
            float r = Random.value;
            if (r < 0.40f) return "ForestTree";
            if (r < 0.70f) return "ForestTreeB";
            if (r < 0.90f) return "Tree";          // palm, for savanna-meets-forest variety
            return "TreePalmBend";
        }

        void BuildLight()
        {
            var go = new GameObject("Sun");
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.1f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Neutral flat ambient so the terrain shows its true savanna colours instead of being
            // washed blue by the default skybox's ambient (the cause of the "blue map").
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.46f, 0.46f, 0.48f);
        }

        void BuildCamera(Vector3 focus)
        {
            var go = new GameObject("RTSCamera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 16f;
            // Solid clear (not the default blue Skybox) so beyond the forest reads as deep-woodland haze.
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.15f, 0.11f);
            go.transform.rotation = Quaternion.Euler(40f, 45f, 0f);
            go.transform.position = focus - go.transform.forward * 34f;
            go.AddComponent<RTSCameraController>();
        }

        void BuildManagers()
        {
            var mgr = new GameObject("_Managers");
            mgr.AddComponent<SelectionManager>();
            mgr.AddComponent<BuildPlacer>();
            mgr.AddComponent<BrandedHud>();
            mgr.AddComponent<TerritoryManager>(); // influence-zone territory coloring (implemented by Agent C)
            mgr.AddComponent<FogOfWar>();         // per-faction vision; renders the Player's fog overlay
            mgr.AddComponent<AgeProgression>();   // visibly tiers-up each empire's buildings on age advance
            mgr.AddComponent<TestModeController>(); // F9 test mode: unlimited resources / free build+advance
        }

        void StartVillagers(FactionId faction, Vector3 basePos)
        {
            var e = Match.Econ(faction);
            for (int i = 0; i < 4; i++)
            {
                // Spread the starting villagers into a 2x2 grid (3 units apart) so each is easy to click.
                Vector3 p = basePos + new Vector3(4f + (i % 2) * 3f, 0f, 2.5f + (i / 2) * 3f);
                var go = UnitFactory.Spawn(UnitType.Villager, p, faction);
                if (e != null) e.AddPop(1);
                var v = go.GetComponent<Villager>();
                var n = NearestNode(p);
                if (v != null && n != null) v.Gather(n);
            }
        }

        void SpawnNodeCluster(Vector3 c)
        {
            SpawnNode(ResourceType.Yam, c + new Vector3(5f, 0f, -2f), new Color(0.92f, 0.82f, 0.2f));
            SpawnNode(ResourceType.Timber, c + new Vector3(-4f, 0f, 5f), new Color(0.36f, 0.6f, 0.25f));
            // Iron is a mountain — place it CLOSE to the base (15u, INWARD toward the map centre) so it's
            // unmissable in the starting camera view and always on playable ground. (A far corner-ward
            // offset pushed it off-screen / into the forest border = the "no iron mountain" reports.)
            Vector3 inward = c.sqrMagnitude > 1f ? -((Vector3)c).normalized : new Vector3(-0.7f, 0f, -0.7f);
            Vector3 ironPos = c + inward * 15f;
            if (InLake(ironPos, 6f)) ironPos = c + new Vector3(inward.z, 0f, -inward.x) * 15f; // sidestep a lake
            Debug.Log($"[NE] Iron mountain (base {c}) spawning at {ironPos}");
            SpawnNode(ResourceType.Iron, ironPos, new Color(0.62f, 0.64f, 0.68f));
        }

        // A gatherable node: a scale-1 root with a box collider + stylized visuals per type.
        void SpawnNode(ResourceType type, Vector3 pos, Color color)
        {
            var root = new GameObject(type + "Node");
            root.transform.position = new Vector3(pos.x, 0f, pos.z);
            var col = root.AddComponent<BoxCollider>();

            if (type == ResourceType.Iron)
            {
                // Iron is mined from a MOUNTAIN — click target sized to the ~4.5u model.
                col.center = new Vector3(0f, 2.2f, 0f);
                col.size = new Vector3(5f, 4.5f, 5f);
                var ironNode = root.AddComponent<ResourceNode>();
                ironNode.Type = type;
                ironNode.DisplayName = "Iron Mountain";
                ironNode.Configure(6000, true); // big reserve; shrinks as mined, leaves a husk when spent
                BuildIronMountain(root.transform);
                return;
            }

            col.center = new Vector3(0f, 0.7f, 0f);
            col.size = new Vector3(1.8f, 1.6f, 1.8f);
            var node = root.AddComponent<ResourceNode>();
            node.Type = type;
            node.DisplayName = type == ResourceType.Timber ? "Timber Grove" : "Yam Field";

            if (type == ResourceType.Timber) BuildTree(root.transform);
            else BuildFarm(root.transform); // Yam plot
        }

        // The iron mountain: the custom Blender model (NE_IronMountain) — now that the node spawns ON
        // SCREEN near the base (it was off-screen before, which is why it looked "missing"), the model
        // renders fine. Falls back to a primitive crag only if the model is missing.
        void BuildIronMountain(Transform parent)
        {
            if (ModelLibrary.CreateModel("IronMountain", parent, Color.white) != null) return;

            var model = new GameObject("Model");
            model.transform.SetParent(parent, false);

            Color stone = new Color(0.42f, 0.42f, 0.46f);
            Color stoneDark = new Color(0.30f, 0.30f, 0.34f);
            Color snow = new Color(0.82f, 0.84f, 0.88f);
            Color iron = new Color(0.62f, 0.36f, 0.20f); // rusty ore veins

            // Broad craggy base + a taller offset peak with a light cap so it reads as a mountain.
            Prim(PrimitiveType.Sphere, model.transform, new Vector3(0f, 1.6f, 0f), new Vector3(8f, 3.6f, 8f), stone);
            Prim(PrimitiveType.Sphere, model.transform, new Vector3(0.6f, 3.8f, -0.4f), new Vector3(4.6f, 6.0f, 4.6f), stoneDark);
            Prim(PrimitiveType.Sphere, model.transform, new Vector3(-2.6f, 1.4f, 1.8f), new Vector3(3.4f, 2.8f, 3.4f), stone);
            Prim(PrimitiveType.Sphere, model.transform, new Vector3(2.4f, 1.2f, 2.2f), new Vector3(3.0f, 2.2f, 3.0f), stoneDark);
            Prim(PrimitiveType.Sphere, model.transform, new Vector3(0.7f, 5.9f, -0.4f), new Vector3(2.2f, 1.4f, 2.2f), snow); // snowy cap
            // Iron ore hints on the slopes.
            Prim(PrimitiveType.Cube, model.transform, new Vector3(1.6f, 2.8f, 1.6f), new Vector3(0.8f, 0.8f, 0.8f), iron);
            Prim(PrimitiveType.Cube, model.transform, new Vector3(-1.8f, 2.0f, -1.2f), new Vector3(0.7f, 0.7f, 0.7f), iron);
            Prim(PrimitiveType.Cube, model.transform, new Vector3(2.6f, 1.6f, -0.4f), new Vector3(0.6f, 0.6f, 0.6f), iron);
        }

        void Decorate()
        {
            // Spread decor across the whole map, inset from the shoreline. Count scales with area so
            // a bigger map doesn't look bare.
            float range = MapBounds.Half - 6f;
            int count = Mathf.RoundToInt(32f * (MapBounds.Size * MapBounds.Size) / (80f * 80f));
            for (int i = 0; i < count; i++)
            {
                Vector3 p = new Vector3(Random.Range(-range, range), 0f, Random.Range(-range, range));
                if (InLake(p, 3f)) continue; // no trees/rocks floating in the lakes
                bool nearBase = false;
                for (int b = 0; b < _activeBases; b++)
                    if (Vector3.Distance(p, _basePoints[b]) < 9f) { nearBase = true; break; }
                if (nearBase) continue;
                var holder = new GameObject("Decor");
                holder.transform.position = p;
                holder.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f); // vary facing so clones don't line up
                float r = Random.value;
                if (r < 0.40f) MakeTreeNode(holder);                   // choppable palms (timber)
                else if (r < 0.55f) BuildRocks(holder.transform);
                else if (r < 0.75f) BuildDecor("Grass", holder.transform);
                else if (r < 0.90f) BuildDecor("Bush", holder.transform);
                else MakeTreeNode(holder);                             // choppable bent palm (timber)
            }
        }

        // Pure savanna decor (no gameplay): drop the model, skip silently if the asset is missing.
        void BuildDecor(string key, Transform parent)
        {
            ModelLibrary.CreateModel(key, parent, Color.white);
        }

        void BuildTree(Transform parent)
        {
            if (ModelLibrary.CreateModel("Tree", parent, Color.white) != null) return;
            Prim(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.5f, 0f), new Vector3(0.22f, 0.5f, 0.22f), new Color(0.4f, 0.28f, 0.16f));
            Prim(PrimitiveType.Sphere, parent, new Vector3(0f, 1.45f, 0f), Vector3.one * 1.5f, new Color(0.2f, 0.45f, 0.2f));
        }

        // A scattered tree the player can actually chop: a Timber ResourceNode with a click collider
        // plus the tree visual. (Most map trees were pure decor, so "gather wood" appeared not to work —
        // only the one timber node per base was choppable. Now every forest palm is a real wood source.)
        void MakeTreeNode(GameObject holder)
        {
            var col = holder.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.0f, 0f);
            col.size = new Vector3(1.4f, 2.0f, 1.4f);
            var tn = holder.AddComponent<ResourceNode>();
            tn.Type = ResourceType.Timber;
            tn.DisplayName = "Timber Grove";
            BuildTree(holder.transform);
        }

        void BuildFarm(Transform parent)
        {
            // Yam Field node uses the custom Blender Farm model (same yam-mound plot as the building).
            if (ModelLibrary.CreateModel("Farm", parent, Color.white) != null) return;
            FarmVisual.Build(parent, 1.8f); // primitive fallback
        }

        void BuildRocks(Transform parent)
        {
            if (ModelLibrary.CreateModel("Rocks", parent, Color.white) != null) return;
            Prim(PrimitiveType.Sphere, parent, new Vector3(0f, 0.4f, 0f), Vector3.one * 0.9f, new Color(0.55f, 0.57f, 0.6f));
            Prim(PrimitiveType.Sphere, parent, new Vector3(0.5f, 0.3f, 0.3f), Vector3.one * 0.6f, new Color(0.5f, 0.52f, 0.55f));
            Prim(PrimitiveType.Sphere, parent, new Vector3(-0.4f, 0.3f, -0.3f), Vector3.one * 0.55f, new Color(0.6f, 0.62f, 0.66f));
        }

        void Prim(PrimitiveType t, Transform parent, Vector3 localPos, Vector3 localScale, Color c)
        {
            var g = GameObject.CreatePrimitive(t);
            var col = g.GetComponent<Collider>(); if (col) Destroy(col);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = localScale;
            MaterialUtil.SetColor(g.GetComponent<Renderer>(), c);
        }

        ResourceNode NearestNode(Vector3 p)
        {
            ResourceNode best = null;
            float bestSqr = float.MaxValue;
            foreach (var n in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                float d = (n.transform.position - p).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = n; }
            }
            return best;
        }
    }
}
