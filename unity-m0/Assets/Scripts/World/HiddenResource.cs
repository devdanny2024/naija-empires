using UnityEngine;

namespace NaijaEmpires
{
    /// A hidden deposit seeded across the map (by Bootstrap.SeedHiddenResources). It is invisible and
    /// inert until the Player's fog of war reveals its tile (FogOfWar.IsVisible). On discovery it
    /// fires once: a one-time resource bonus into the Player's economy AND a gatherable ResourceNode
    /// spawned in place, then it removes itself.
    ///
    /// Polls the fog a few times a second rather than every frame (fog itself only updates ~4x/sec).
    public class HiddenResource : MonoBehaviour
    {
        public ResourceType Type = ResourceType.Iron;

        /// One-time economy bonus granted to the Player on discovery.
        public int DiscoveryBonus = 120;

        const float PollInterval = 0.4f;
        float _timer;
        bool _found;

        void Update()
        {
            if (_found) return;
            _timer += Time.deltaTime;
            if (_timer < PollInterval) return;
            _timer = 0f;

            var fog = FogOfWar.Instance;
            if (fog == null) return; // fog not up yet; stay hidden
            if (!fog.IsVisible(transform.position)) return;

            Discover();
        }

        void Discover()
        {
            _found = true;

            // One-time bonus to the Player's economy.
            var econ = Match.Econ(FactionId.Player);
            if (econ != null) econ.Add(Type, DiscoveryBonus);

            // Spawn a gatherable node in place so villagers can harvest the find.
            SpawnNode();

            Destroy(gameObject);
        }

        void SpawnNode()
        {
            var root = new GameObject(Type + "Node (Discovered)");
            root.transform.position = new Vector3(transform.position.x, 0f, transform.position.z);

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.7f, 0f);
            col.size = new Vector3(1.8f, 1.6f, 1.8f);

            var node = root.AddComponent<ResourceNode>();
            node.Type = Type;
            node.DisplayName = "Rare " + RareName(Type); // rare deposit — flavourful name on click

            // Minimal self-contained visual (Bootstrap's richer builders are gone by play time).
            // URP-safe colouring via MaterialUtil so it never renders magenta.
            BuildVisual(root.transform);
        }

        void BuildVisual(Transform parent)
        {
            switch (Type)
            {
                case ResourceType.Timber:
                    Prim(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.5f, 0f),
                        new Vector3(0.22f, 0.5f, 0.22f), new Color(0.4f, 0.28f, 0.16f));
                    Prim(PrimitiveType.Sphere, parent, new Vector3(0f, 1.45f, 0f),
                        Vector3.one * 1.5f, new Color(0.2f, 0.45f, 0.2f));
                    break;
                case ResourceType.Yam:
                    Prim(PrimitiveType.Cube, parent, new Vector3(0f, 0.1f, 0f),
                        new Vector3(1.8f, 0.2f, 1.8f), new Color(0.45f, 0.3f, 0.18f));
                    Prim(PrimitiveType.Cube, parent, new Vector3(0f, 0.26f, 0f),
                        new Vector3(0.18f, 0.18f, 1.5f), new Color(0.5f, 0.66f, 0.2f));
                    break;
                default: // Iron: rocks
                    Prim(PrimitiveType.Sphere, parent, new Vector3(0f, 0.4f, 0f),
                        Vector3.one * 0.9f, new Color(0.55f, 0.57f, 0.6f));
                    Prim(PrimitiveType.Sphere, parent, new Vector3(0.5f, 0.3f, 0.3f),
                        Vector3.one * 0.6f, new Color(0.5f, 0.52f, 0.55f));
                    break;
            }
        }

        // A rare-deposit flavour name per resource type (shown when the player clicks a discovered find).
        static string RareName(ResourceType t) => t switch
        {
            ResourceType.Iron => "Iron Lode",
            ResourceType.Timber => "Ironwood Grove",
            ResourceType.Yam => "Fertile Plot",
            _ => t.ToString(),
        };

        static void Prim(PrimitiveType t, Transform parent, Vector3 localPos, Vector3 localScale, Color c)
        {
            var g = GameObject.CreatePrimitive(t);
            var col = g.GetComponent<Collider>(); if (col) Destroy(col);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = localScale;
            MaterialUtil.SetColor(g.GetComponent<Renderer>(), c);
        }
    }
}
