using UnityEngine;

namespace NaijaEmpires
{
    /// Builds the whole M1 skirmish from code: ground, camera, two faction bases (Player = Benin,
    /// Enemy = Oyo + AI), resources, and managers. Drop on one empty GameObject and press Play.
    public class Bootstrap : MonoBehaviour
    {
        void Awake()
        {
            Match.Reset();
            Match.Register(FactionId.Player, new Economy(220, 220, 120, Civ.Benin));
            Match.Register(FactionId.Enemy, new Economy(220, 220, 120, Civ.Oyo));

            BuildGround();
            BuildLight();

            Vector3 playerBase = new Vector3(-16f, 0f, -16f);
            Vector3 enemyBase = new Vector3(16f, 0f, 16f);

            BuildCamera(playerBase);
            BuildManagers();

            // Resources first so starting villagers can be auto-assigned.
            SpawnNodeCluster(playerBase);
            SpawnNodeCluster(enemyBase);
            SpawnNode(ResourceType.Iron, Vector3.zero, Color.gray);
            Decorate();

            // Player base
            var playerTC = BuildingFactory.Spawn(BuildingKind.TownCentre, playerBase, FactionId.Player);
            StartVillagers(FactionId.Player, playerBase);

            // Enemy base + AI
            BuildingFactory.Spawn(BuildingKind.TownCentre, enemyBase, FactionId.Enemy);
            StartVillagers(FactionId.Enemy, enemyBase);

            var aiGo = new GameObject("EnemyAI");
            var ai = aiGo.AddComponent<EnemyAI>();
            ai.Owner = FactionId.Enemy;
            ai.basePos = enemyBase;
            ai.enemyTarget = playerTC.transform;

            Destroy(gameObject);
        }

        void BuildGround()
        {
            // Water surrounding the land mass (no collider so clicks fall through to land only).
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Water";
            water.transform.position = new Vector3(0f, -0.15f, 0f);
            water.transform.localScale = new Vector3(14f, 1f, 14f);
            var wc = water.GetComponent<Collider>(); if (wc) Destroy(wc);
            MaterialUtil.SetColor(water.GetComponent<Renderer>(), new Color(0.12f, 0.28f, 0.42f));

            // The island / land mass.
            var land = GameObject.CreatePrimitive(PrimitiveType.Plane);
            land.name = "Ground";
            land.transform.localScale = new Vector3(8f, 1f, 8f); // 80 x 80
            MaterialUtil.SetColor(land.GetComponent<Renderer>(), new Color(0.36f, 0.5f, 0.26f));
        }

        void BuildLight()
        {
            var go = new GameObject("Sun");
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.1f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        void BuildCamera(Vector3 focus)
        {
            var go = new GameObject("RTSCamera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 16f;
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
        }

        void StartVillagers(FactionId faction, Vector3 basePos)
        {
            var e = Match.Econ(faction);
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = basePos + new Vector3(3f + i * 1.1f, 0f, 2f);
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
            for (int i = 0; i < 18; i++)
            {
                Vector3 p = new Vector3(Random.Range(-34f, 34f), 0f, Random.Range(-34f, 34f));
                if (Vector3.Distance(p, new Vector3(-16f, 0f, -16f)) < 9f) continue;
                if (Vector3.Distance(p, new Vector3(16f, 0f, 16f)) < 9f) continue;
                var holder = new GameObject("Decor");
                holder.transform.position = p;
                if (Random.value < 0.7f) BuildTree(holder.transform);
                else BuildRocks(holder.transform);
            }
        }

        void BuildTree(Transform parent)
        {
            if (ModelLibrary.CreateModel("Tree", parent, Color.white) != null) return;
            Prim(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.5f, 0f), new Vector3(0.22f, 0.5f, 0.22f), new Color(0.4f, 0.28f, 0.16f));
            Prim(PrimitiveType.Sphere, parent, new Vector3(0f, 1.45f, 0f), Vector3.one * 1.5f, new Color(0.2f, 0.45f, 0.2f));
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
