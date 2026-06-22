using UnityEngine;

namespace NaijaEmpires
{
    /// A quick expanding + fading ring at a clicked ground point — confirms a command landed
    /// (move / gather / build / attack) so the player gets feedback that the click registered.
    public class CommandPing : MonoBehaviour
    {
        public static void Spawn(Vector3 worldPos, Color color)
        {
            var go = new GameObject("CommandPing");
            go.transform.position = new Vector3(worldPos.x, 0.12f, worldPos.z);
            var p = go.AddComponent<CommandPing>();
            p._color = color;
            p.Build();
        }

        Color _color;
        Material _mat;
        Transform _ring;
        float _t;
        const float Dur = 0.55f, StartScale = 0.7f, EndScale = 3.4f;

        void Build()
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Ring";
            var col = disc.GetComponent<Collider>(); if (col) Destroy(col);
            disc.transform.SetParent(transform, false);
            disc.transform.localScale = new Vector3(StartScale, 0.02f, StartScale);
            _ring = disc.transform;

            // Transparent unlit so the ring can fade out (same setup the fog overlay uses).
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            _mat = new Material(sh);
            if (_mat.HasProperty("_Surface")) _mat.SetFloat("_Surface", 1f);
            if (_mat.HasProperty("_SrcBlend")) _mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (_mat.HasProperty("_DstBlend")) _mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (_mat.HasProperty("_ZWrite")) _mat.SetFloat("_ZWrite", 0f);
            _mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            SetColor(_color);
            disc.GetComponent<Renderer>().material = _mat;
        }

        void SetColor(Color c)
        {
            if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", c);
            if (_mat.HasProperty("_Color")) _mat.SetColor("_Color", c);
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / Dur);
            float s = Mathf.Lerp(StartScale, EndScale, k);
            _ring.localScale = new Vector3(s, 0.02f, s);
            var c = _color; c.a = 1f - k;
            SetColor(c);
            if (k >= 1f) { Destroy(_mat); Destroy(gameObject); }
        }
    }

    /// A floating name tag shown when the player clicks a resource — "what is this?". Self-destructs
    /// after a few seconds. Only one is shown at a time (the previous tag is cleared on a new click).
    public class ResourceTag : MonoBehaviour
    {
        static ResourceTag _current;

        public static void Show(ResourceNode node)
        {
            if (node == null) return;
            if (_current != null) Destroy(_current.gameObject);

            var go = new GameObject("ResourceTag");
            go.transform.position = node.transform.position + Vector3.up * (node.Type == ResourceType.Iron ? 6.5f : 2.6f);
            var t = go.AddComponent<ResourceTag>();
            t.Build(node.Label(), ResColor(node.Type));
            _current = t;
        }

        Transform _label;
        float _life;
        const float Life = 3f;

        void Build(string text, Color color)
        {
            var tm = gameObject.AddComponent<TextMesh>();
            tm.text = text;
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 64;
            tm.characterSize = 0.11f; // larger so the resource name is legible in-world
            tm.color = color;
            var font = Theme.Font;
            if (font != null) tm.font = font;
            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (font != null && font.material != null) mr.sharedMaterial = font.material;
                mr.sortingOrder = 5000;
            }
            _label = transform;
        }

        void LateUpdate()
        {
            if (Camera.main != null) _label.rotation = Camera.main.transform.rotation;
            _life += Time.deltaTime;
            if (_life >= Life) { if (_current == this) _current = null; Destroy(gameObject); }
        }

        static Color ResColor(ResourceType t) => t switch
        {
            ResourceType.Yam => new Color(0.95f, 0.85f, 0.35f),
            ResourceType.Timber => new Color(0.55f, 0.8f, 0.4f),
            ResourceType.Iron => new Color(0.8f, 0.82f, 0.88f),
            _ => Color.white,
        };
    }
}
