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

    /// A pulsing highlight ring laid on the ground under a clicked resource — the in-world half of
    /// "what is this?" (the name/amount text now lives in the HUD's bottom resource panel). Only one is
    /// shown at a time; Hide() clears it when the panel times out, and a new click replaces it.
    public class ResourceHighlight : MonoBehaviour
    {
        static ResourceHighlight _current;

        public static void Show(ResourceNode node)
        {
            if (node == null) return;
            if (_current != null) Destroy(_current.gameObject);

            var go = new GameObject("ResourceHighlight");
            go.transform.position = new Vector3(node.transform.position.x, 0.12f, node.transform.position.z);
            var h = go.AddComponent<ResourceHighlight>();
            h.Build(ResColor(node.Type));
            _current = h;
        }

        public static void Hide() { if (_current != null) Destroy(_current.gameObject); }

        Material _mat;
        Transform _ring;
        Color _color;
        const float BaseScale = 2.8f;

        void Build(Color color)
        {
            _color = color;
            // A flat translucent disc (same transparent-unlit setup as CommandPing, which is known-good
            // under URP) that sits just above the ground and gently pulses to read as a selection glow.
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Ring";
            var col = disc.GetComponent<Collider>(); if (col) Destroy(col);
            disc.transform.SetParent(transform, false);
            disc.transform.localScale = new Vector3(BaseScale, 0.02f, BaseScale);
            _ring = disc.transform;

            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            _mat = new Material(sh);
            if (_mat.HasProperty("_Surface")) _mat.SetFloat("_Surface", 1f);
            if (_mat.HasProperty("_SrcBlend")) _mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (_mat.HasProperty("_DstBlend")) _mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (_mat.HasProperty("_ZWrite")) _mat.SetFloat("_ZWrite", 0f);
            _mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            disc.GetComponent<Renderer>().material = _mat;
        }

        void Update()
        {
            float pulse = Mathf.Sin(Time.time * 4f);
            var c = _color; c.a = 0.4f + 0.22f * pulse;
            if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", c);
            if (_mat.HasProperty("_Color")) _mat.SetColor("_Color", c);
            float s = BaseScale + 0.18f * pulse;
            _ring.localScale = new Vector3(s, 0.02f, s);
        }

        void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
            if (_current == this) _current = null;
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
