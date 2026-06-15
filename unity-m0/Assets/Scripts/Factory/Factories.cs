using UnityEngine;

namespace NaijaEmpires
{
    /// Spawns fully wired units. Structure: a scale-1 root (collider + logic + selection ring)
    /// with a child "Model" that ModelAnimator animates (bob/lunge/chop), plus a type marker.
    public static class UnitFactory
    {
        public static GameObject Spawn(UnitType type, Vector3 pos, FactionId faction)
        {
            var root = new GameObject(faction + " " + type);
            root.transform.position = new Vector3(pos.x, 0f, pos.z);

            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.height = 1.8f;
            col.radius = 0.38f;

            root.AddComponent<Faction>().Id = faction;
            root.AddComponent<Health>().Init(UnitConfig.Hp(type));
            root.AddComponent<Selectable>();
            root.AddComponent<TeamRing>();

            // Real model (tinted by faction) with primitive fallback. Child must be named "Model".
            var model = ModelLibrary.CreateModel(type.ToString(), root.transform, UnitConfig.BodyColor(faction));
            if (model == null)
            {
                float s = type == UnitType.Cavalry ? 0.8f : 0.62f;
                var prim = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                var mc = prim.GetComponent<Collider>(); if (mc) Object.Destroy(mc);
                prim.name = "Model";
                prim.transform.SetParent(root.transform, false);
                prim.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                prim.transform.localScale = new Vector3(s, 0.85f, s);
                MaterialUtil.SetColor(prim.GetComponent<Renderer>(), UnitConfig.BodyColor(faction));

                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var mkc = marker.GetComponent<Collider>(); if (mkc) Object.Destroy(mkc);
                marker.transform.SetParent(prim.transform, false);
                marker.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                marker.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
                MaterialUtil.SetColor(marker.GetComponent<Renderer>(), UnitConfig.TypeColor(type));
            }

            var anim = root.AddComponent<ModelAnimator>();

            if (type == UnitType.Villager)
            {
                root.AddComponent<Villager>().speed = UnitConfig.Speed(type);
            }
            else
            {
                var c = root.AddComponent<CombatUnit>();
                c.Type = type;
                c.speed = UnitConfig.Speed(type);
                c.damage = UnitConfig.Damage(type);
                c.attackRange = UnitConfig.Range(type);
            }

            // After the role component exists so InitRig can pick role-specific clips (Run/Shoot).
            // Real animated FBX -> colour its parts + drive its rig; primitive fallback stays procedural.
            if (model != null)
            {
                root.AddComponent<CharacterColors>(); // FBX imports parts black -> set earthy tones in code
                anim.InitRig(ModelLibrary.LoadClips(type.ToString()));
            }

            return root;
        }
    }

    /// Spawns fully wired buildings: scale-1 root (collider + logic) with a child "Model" body
    /// and a diamond "roof" so it reads as a hut. Benin defences are configured via stats.
    public static class BuildingFactory
    {
        public static GameObject Spawn(BuildingKind kind, Vector3 pos, FactionId faction)
        {
            var econ = Match.Econ(faction);
            Civ civ = econ != null ? econ.Civ : Civ.Benin;
            Vector3 size = BuildingConfig.Size(kind);

            var root = new GameObject(faction + " " + kind);
            root.transform.position = new Vector3(pos.x, 0f, pos.z);

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, size.y / 2f, 0f);
            col.size = size;

            root.AddComponent<Faction>().Id = faction;
            root.AddComponent<Health>().Init(BuildingConfig.Hp(kind, civ));

            // Real model with primitive (body + diamond-roof hut) fallback. Child must be named "Model".
            var model = ModelLibrary.CreateModel(kind.ToString(), root.transform, Color.white);
            if (model == null)
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var bc = body.GetComponent<Collider>(); if (bc) Object.Destroy(bc);
                body.name = "Model";
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = new Vector3(0f, size.y / 2f, 0f);
                body.transform.localScale = size;
                MaterialUtil.SetColor(body.GetComponent<Renderer>(), BuildingConfig.ColorOf(kind, faction));

                var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var rc = roof.GetComponent<Collider>(); if (rc) Object.Destroy(rc);
                roof.transform.SetParent(body.transform, false);
                roof.transform.localPosition = new Vector3(0f, 0.62f, 0f);
                roof.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                roof.transform.localScale = new Vector3(1.05f, 0.5f, 1.05f);
                MaterialUtil.SetColor(roof.GetComponent<Renderer>(), new Color(0.45f, 0.22f, 0.16f));
            }

            root.AddComponent<ModelAnimator>(); // hit punch (static, so no walk bob)

            switch (kind)
            {
                case BuildingKind.TownCentre:
                    root.AddComponent<TownCentre>();
                    root.AddComponent<ProductionBuilding>().Trainable.Add(UnitType.Villager);
                    AddCap(root, BuildingConfig.PopCapBonus(kind));
                    root.AddComponent<Selectable>();
                    break;
                case BuildingKind.House:
                    AddCap(root, BuildingConfig.PopCapBonus(kind));
                    break;
                case BuildingKind.Barracks:
                {
                    var pb = root.AddComponent<ProductionBuilding>();
                    pb.Trainable.Add(UnitType.Spearman);
                    pb.Trainable.Add(UnitType.Archer);
                    root.AddComponent<Selectable>();
                    break;
                }
                case BuildingKind.Stable:
                {
                    var pb = root.AddComponent<ProductionBuilding>();
                    pb.Trainable.Add(UnitType.Cavalry);
                    root.AddComponent<Selectable>();
                    break;
                }
                case BuildingKind.Tower:
                    root.AddComponent<Tower>();
                    break;
            }
            return root;
        }

        static void AddCap(GameObject go, int amount)
        {
            if (amount <= 0) return;
            go.AddComponent<PopCapProvider>().amount = amount;
        }
    }
}
