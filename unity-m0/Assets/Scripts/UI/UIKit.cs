using UnityEngine;
using UnityEngine.UI;

namespace NaijaEmpires
{
    /// Tiny code-first uGUI builder used by BrandedHud. Keeps element creation terse and on-brand.
    public static class UI
    {
        public static void Set(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        /// Full-stretch to parent with per-edge insets (left, bottom, right, top).
        public static void Stretch(RectTransform rt, float l, float b, float r, float t)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        public static Image Panel(Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject("Panel", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite; img.type = Image.Type.Sliced; img.color = color;
            return img;
        }

        /// A bronze hairline accent near the top inside edge of a panel — the brand's "rule".
        public static void Border(Image fill, Sprite sprite, Color color)
        {
            var go = new GameObject("Accent", typeof(Image));
            go.transform.SetParent(fill.transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = Theme.RoundSoft; img.type = Image.Type.Sliced; img.color = color; img.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(14, -4); rt.offsetMax = new Vector2(-14, 0);
        }

        public static VerticalLayoutGroup Col(Transform parent, float spacing, RectOffset padding)
        {
            var go = new GameObject("Col", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform, 0, 0, 0, 0);
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = padding;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            v.childAlignment = TextAnchor.UpperCenter;
            return v;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor, bool bold = false)
        {
            var go = new GameObject("Label", typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Theme.Font; t.text = text; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Text Header(Transform parent, string text)
        {
            var t = Label(parent, text, Theme.LabelSize, Theme.Bronze, TextAnchor.MiddleLeft, true);
            LayoutHeight(t.gameObject, 30);
            return t;
        }

        public static (Button, Text) Button(Transform parent, string text, System.Action onClick, bool blank = false)
        {
            var go = new GameObject("Button", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = Theme.RoundSoft; img.type = Image.Type.Sliced; img.color = Theme.PanelHi;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.fadeDuration = 0.08f;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(1f, 1f, 1f, 0.38f);
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            Text label = null;
            if (!blank)
            {
                label = Label(go.transform, text, Theme.BodySize, Theme.Ivory, TextAnchor.MiddleCenter, true);
                Stretch(label.rectTransform, 0, 0, 0, 0);
            }
            return (btn, label);
        }

        public static Image Swatch(Transform parent, Color color, float size)
        {
            var go = new GameObject("Swatch", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = Theme.RoundSoft; img.type = Image.Type.Sliced; img.color = color; img.raycastTarget = false;
            if (size > 0f) ((RectTransform)go.transform).sizeDelta = new Vector2(size, size);
            return img;
        }

        public static void LayoutHeight(GameObject go, float h)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.minHeight = h; le.preferredHeight = h;
        }
    }
}
