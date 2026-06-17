using UnityEngine;

namespace NaijaEmpires
{
    /// Builds the whole M1 skirmish from code: ground, camera, two faction bases (Player = Benin,
    /// Enemy = Oyo + AI), resources, and managers. Drop on one empty GameObject and press Play.
    public class Bootstrap : MonoBehaviour
    {
        // The 4 corner spawn points, pushed to the far corners of the playable square (inset from the
        // shoreline). Derived from MapBounds so the bases spread to match the map size. The first
        // MatchConfig.Count of them are used this match.
        static readonly float Corner = MapBounds.Half - MapBounds.BaseInset;
        static readonly Vector3[] BasePoints =
        {
            new Vector3(-Corner, 0f, -Corner),
            new Vector3( Corner, 0f,  Corner),
            new Vector3( Corner, 0f, -Corner),
            new Vector3(-Corner, 0f,  Corner),
        };
        int _activeBases;

        void Awake()
        {
            Match.Reset();
            BuildGround();
            BuildLight();

            int n = MatchConfig.Count;
            _activeBases = n;

            BuildCamera(BasePoints[0]); // camera starts on the human's base (seat 0)
            BuildManagers();

            // Central contested iron node.
            SpawnNode(ResourceType.Iron, Vector3.zero, Color.gray);

            // One base per empire: economy, resources, Town Centre, starting villagers, and an AI bot
            // for every non-human seat (single-player FFA).
            for (int i = 0; i < n; i++)
            {
                FactionId id = MatchConfig.SeatId(i);
                Civ civ = MatchConfig.CivFor(i);
                Vector3 basePos = BasePoints[i];

                int bonus = civ == Civ.KanemBornu ? 100 : 0; // Kanem-Bornu trade perk: extra starting resources
                Match.Register(id, new Economy(220 + bonus, 220 + bonus, 120 + bonus, civ));

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
            Destroy(gameObject);
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

                bool nearBase = false;
                for (int b = 0; b < _activeBases; b++)
                    if (Vector3.Distance(p, BasePoints[b]) < 16f) { nearBase = true; break; }
                if (nearBase) continue;

                var go = new GameObject("HiddenDeposit");
                go.transform.position = p;
                var hr = go.AddComponent<HiddenResource>();
                hr.Type = (ResourceType)Random.Range(0, 3);
                placed++;
            }
        }

        void BuildGround()
        {
            // Water surrounding the land mass (no collider so clicks fall through to land only).
            // Sized well past the playable square so the shoreline is always framed by water.
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Water";
            water.transform.position = new Vector3(0f, -0.6f, 0f);
            float waterScale = (MapBounds.Size * 1.4f) / 10f; // Unity Plane is 10x10 at scale 1
            water.transform.localScale = new Vector3(waterScale, 1f, waterScale);
            var wc = water.GetComponent<Collider>(); if (wc) Destroy(wc);
            MaterialUtil.SetColor(water.GetComponent<Renderer>(), new Color(0.12f, 0.28f, 0.42f));

            // The island / land mass: stylized low-poly terrain mesh (visual) with a flat y=0
            // gameplay collider baked in by TerrainBuilder, so units/clicks stay on the flat plane.
            TerrainBuilder.Build();
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
            // Solid clear (not the default blue Skybox) so beyond-map reads as a calm dark void.
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.09f);
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
            SpawnNode(ResourceType.Iron, c + new Vector3(-5f, 0f, -4f), new Color(0.62f, 0.64f, 0.68f));
        }

        // A gatherable node: a scale-1 root with a box collider + stylized visuals per type.
        void SpawnNode(ResourceType type, Vector3 pos, Color color)
        {
            var root = new GameObject(type + "Node");
            root.transform.position = new Vector3(pos.x, 0f, pos.z);
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.7f, 0f);
            col.size = new Vector3(1.8f, 1.6f, 1.8f);
            root.AddComponent<ResourceNode>().Type = type;

            if (type == ResourceType.Timber) BuildTree(root.transform);
            else if (type == ResourceType.Yam) BuildFarm(root.transform);
            else BuildRocks(root.transform);
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
                bool nearBase = false;
                for (int b = 0; b < _activeBases; b++)
                    if (Vector3.Distance(p, BasePoints[b]) < 9f) { nearBase = true; break; }
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
            holder.AddComponent<ResourceNode>().Type = ResourceType.Timber;
            BuildTree(holder.transform);
        }

        void BuildFarm(Transform parent)
        {
            Prim(PrimitiveType.Cube, parent, new Vector3(0f, 0.1f, 0f), new Vector3(1.8f, 0.2f, 1.8f), new Color(0.45f, 0.3f, 0.18f));
            for (int i = -1; i <= 1; i++)
                Prim(PrimitiveType.Cube, parent, new Vector3(i * 0.5f, 0.26f, 0f), new Vector3(0.18f, 0.18f, 1.5f), new Color(0.5f, 0.66f, 0.2f));
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
