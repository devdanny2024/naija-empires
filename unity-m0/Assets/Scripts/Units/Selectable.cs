using UnityEngine;

namespace NaijaEmpires
{
    /// Makes a unit/building selectable. When selected it shows a bright pulsing ground ring AND a
    /// floating name label above it, so it's obvious what you've picked. Both are hidden until selected.
    public class Selectable : MonoBehaviour
    {
        static readonly Color RingColor = new Color(0.25f, 1f, 0.5f, 0.95f);

        GameObject _ring;
        Transform _label;       // billboarded name tag (parent of the TextMesh)
        float _basePulse = 1.5f;
        public bool IsSelected { get; private set; }

        /// Friendly name shown on the floating label (faction prefix stripped from the GameObject name).
        public string DisplayName { get; set; }

        void Awake()
        {
            if (string.IsNullOrEmpty(DisplayName)) DisplayName = DeriveName(gameObject.name);

            // Bright flat ring on the ground. Cylinder (thin disc) tinted with an unlit glow so it
            // stays vivid under any lighting; scaled wider for buildings is handled by the disc size.
            _ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _ring.name = "SelectionRing";
            var col = _ring.GetComponent<Collider>();
            if (col) Destroy(col);
            _ring.transform.SetParent(transform, false);
            _ring.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            _basePulse = SelectionScale();
            _ring.transform.localScale = new Vector3(_basePulse, 0.03f, _basePulse);
            MaterialUtil.SetGlow(_ring.GetComponent<Renderer>(), RingColor);
            _ring.SetActive(false);

            BuildLabel();
        }

        // A small floating name tag (3D TextMesh, billboarded each frame) above the object.
        void BuildLabel()
        {
            var go = new GameObject("NameLabel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, LabelHeight(), 0f);

            var tm = go.AddComponent<TextMesh>();
            tm.text = DisplayName;
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 64;                 // high res, scaled down by characterSize for crisp text
            tm.characterSize = 0.09f;         // larger so the name is legible in-world
            tm.color = new Color(0.93f, 0.98f, 0.95f);

            // A script-added TextMesh has no font/material by default (renders nothing) — wire the
            // brand font and its atlas material onto the MeshRenderer so the label actually shows.
            var font = Theme.Font;
            if (font != null) tm.font = font;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (font != null && font.material != null) mr.sharedMaterial = font.material;
                mr.sortingOrder = 5000; // draw over the world
            }

            _label = go.transform;
            go.SetActive(false);
        }

        public void SetSelected(bool value)
        {
            IsSelected = value;
            if (_ring) _ring.SetActive(value);
            if (_label) _label.gameObject.SetActive(value);
        }

        void LateUpdate()
        {
            if (!IsSelected) return;

            // Gentle ring pulse so the selection reads as "alive".
            if (_ring)
            {
                float s = _basePulse * (1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.07f);
                _ring.transform.localScale = new Vector3(s, 0.03f, s);
            }

            // Billboard the label to the camera so it's always upright and legible.
            if (_label && Camera.main != null)
                _label.rotation = Camera.main.transform.rotation;
        }

        // Bigger ring for buildings (BoxCollider) than for units (CapsuleCollider).
        float SelectionScale()
        {
            var box = GetComponent<BoxCollider>();
            if (box != null) return Mathf.Max(box.size.x, box.size.z) * 1.25f + 0.6f;
            return 1.6f;
        }

        float LabelHeight()
        {
            var box = GetComponent<BoxCollider>();
            if (box != null) return box.size.y + 0.8f;
            var cap = GetComponent<CapsuleCollider>();
            if (cap != null) return cap.height + 0.4f;
            return 2.4f;
        }

        // "Player Villager" -> "Villager", "Enemy Barracks" -> "Barracks".
        static string DeriveName(string raw)
        {
            foreach (var p in new[] { "Player ", "Enemy ", "Faction3 ", "Faction4 " })
                if (raw.StartsWith(p)) return raw.Substring(p.Length);
            return raw;
        }
    }
}
