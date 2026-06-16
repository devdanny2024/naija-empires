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
            img.sprite = sprite; img.type = UnityEngine.UI.Image.Type.Sliced; img.color = color;
            return img;
        }

        /// A top-bright shine overlay (faked gradient) stretched over a fill — the Figma
        /// "inner top shine" on buttons/panels. Tint is usually white at low alpha. Non-interactive.
        public static Image Shine(Image fill, Color tint)
        {
            var go = new GameObject("Shine", typeof(Image));
            go.transform.SetParent(fill.transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = Theme.ShineV; img.type = UnityEngine.UI.Image.Type.Sliced; img.color = tint; img.raycastTarget = false;
            Stretch(img.rectTransform, 2, 2, 2, 2);
            img.transform.SetSiblingIndex(1); // just above the fill, below text/icons
            return img;
        }

        /// Four carved-bronze triangular corner ornaments inside a panel (Figma Panel anatomy).
        public static void Corners(Image panel, float size = 15f)
        {
            (Vector2 a, float rot)[] c =
            {
                (new Vector2(0, 1), 0f),    // top-left
                (new Vector2(1, 1), -90f),  // top-right
                (new Vector2(0, 0), 90f),   // bottom-left
                (new Vector2(1, 0), 180f),  // bottom-right
            };
            foreach (var (a, rot) in c)
            {
                var go = new GameObject("Corner", typeof(Image));
                go.transform.SetParent(panel.transform, false);
                var img = go.GetComponent<Image>();
                img.sprite = Theme.Tri; img.color = Theme.BronzeLight; img.raycastTarget = false; img.preserveAspect = true;
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = a; rt.pivot = a;
                rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(size, size);
                rt.localRotation = Quaternion.Euler(0, 0, rot);
            }
        }

        /// A titled, ornamented brand panel: gradient-ish fill + shine + corner triangles + a Figma
        /// title strip (uppercase, wide-tracked Cinzel-style). Returns the content area below the strip.
        public static (Image panel, Transform content) TitledPanel(Transform parent, string title)
        {
            var panel = Panel(parent, Theme.Round, Theme.Alpha(Theme.PanelTop, 0.98f));
            Shine(panel, Theme.Alpha(Theme.BronzeLight, 0.10f));
            Corners(panel);

            float top = 0f;
            if (!string.IsNullOrEmpty(title))
            {
                var strip = new GameObject("TitleStrip", typeof(Image));
                strip.transform.SetParent(panel.transform, false);
                var simg = strip.GetComponent<Image>();
                simg.sprite = Theme.RoundSoft; simg.type = UnityEngine.UI.Image.Type.Sliced;
                simg.color = Theme.Alpha(Theme.Bronze, 0.14f); simg.raycastTarget = false;
                var srt = simg.rectTransform;
                srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1); srt.pivot = new Vector2(0.5f, 1);
                srt.offsetMin = new Vector2(2, -40); srt.offsetMax = new Vector2(-2, -2);

                var hl = Label(strip.transform, Track(title.ToUpperInvariant()), Theme.SmallSize,
                               Theme.BronzeLight, TextAnchor.MiddleLeft, true, Theme.Display);
                Stretch(hl.rectTransform, 18, 0, 18, 0);

                var rule = new GameObject("Rule", typeof(Image));
                rule.transform.SetParent(panel.transform, false);
                var rimg = rule.GetComponent<Image>();
                rimg.color = Theme.Alpha(Theme.Bronze, 0.3f); rimg.raycastTarget = false;
                var rrt = rimg.rectTransform;
                rrt.anchorMin = new Vector2(0, 1); rrt.anchorMax = new Vector2(1, 1); rrt.pivot = new Vector2(0.5f, 1);
                rrt.offsetMin = new Vector2(2, -42); rrt.offsetMax = new Vector2(-2, -41);
                top = 44f;
            }

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(panel.transform, false);
            Stretch((RectTransform)content.transform, 0, 0, 0, top);
            return (panel, content.transform);
        }

        /// Wide letter-spacing for headers — uGUI Text has no tracking, so we inject spaces. Cinzel-ish.
        public static string Track(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new System.Text.StringBuilder(s.Length * 2);
            for (int i = 0; i < s.Length; i++) { sb.Append(s[i]); if (i < s.Length - 1) sb.Append(' '); }
            return sb.ToString();
        }

        /// A bronze hairline accent near the top inside edge of a panel — the brand's "rule".
        public static void Border(Image fill, Sprite sprite, Color color)
        {
            var go = new GameObject("Accent", typeof(Image));
            go.transform.SetParent(fill.transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = Theme.RoundSoft; img.type = UnityEngine.UI.Image.Type.Sliced; img.color = color; img.raycastTarget = false;
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

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor, bool bold = false, Font font = null)
        {
            var go = new GameObject("Label", typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font != null ? font : Theme.Font; t.text = text; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        // Section header — Cinzel-style: uppercase, wide-tracked, bronze-light with a soft glow shadow.
        public static Text Header(Transform parent, string text)
        {
            var t = Label(parent, Track(text.ToUpperInvariant()), Theme.SmallSize, Theme.BronzeLight,
                          TextAnchor.MiddleLeft, true, Theme.Display);
            Shadow(t, Theme.Alpha(Theme.Bronze, 0.5f), new Vector2(0f, 0f)); // faux text-glow
            LayoutHeight(t.gameObject, 26);
            return t;
        }

        /// Soft drop-shadow behind text for a little depth / premium feel.
        public static void Shadow(Graphic g, Color color, Vector2 dist)
        {
            var s = g.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            s.effectColor = color;
            s.effectDistance = dist;
        }

        public static (Button, Text) Button(Transform parent, string text, System.Action onClick, bool blank = false)
        {
            var go = new GameObject("Button", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = Theme.RoundSoft; img.type = UnityEngine.UI.Image.Type.Sliced; img.color = Theme.PanelHi;
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

            Shine(img, Theme.Alpha(Color.white, 0.12f)); // Figma "inner top shine"

            Text label = null;
            if (!blank)
            {
                label = Label(go.transform, text, Theme.BodySize, Theme.Ivory, TextAnchor.MiddleCenter, true);
                Stretch(label.rectTransform, 0, 0, 0, 0);
            }
            return (btn, label);
        }

        public enum BtnKind { Primary, Secondary, Danger, Confirm }

        /// Restyle a Button to a Figma GameButton variant (fill + border + label colour). The shine
        /// added in Button() stays, so it reads glossy. Label is centred + bold; tracking optional.
        public static Button Variant(Button btn, Text label, BtnKind kind, bool track = true)
        {
            Color fill, edge, txt;
            switch (kind)
            {
                case BtnKind.Primary: fill = Theme.Bronze;  edge = Theme.BronzeDeep;   txt = Theme.Night; break;
                case BtnKind.Danger:  fill = Theme.Danger;  edge = Theme.DangerDeep;   txt = Theme.Ivory; break;
                case BtnKind.Confirm: fill = Theme.Confirm; edge = Theme.ConfirmDeep;  txt = Theme.Night; break;
                default:              fill = Theme.PanelHi; edge = Theme.Alpha(Theme.Bronze, 0.5f); txt = Theme.BronzeLight; break;
            }
            btn.image.color = fill;
            Border(btn.image, Theme.RoundSoft, edge); // bronze rule near the top edge
            if (label != null)
            {
                label.color = txt;
                if (track) label.text = Track(label.text);
                if (kind == BtnKind.Primary || kind == BtnKind.Confirm)
                    Shadow(label, Theme.Alpha(Color.black, 0.25f), new Vector2(0f, -1f));
            }
            return btn;
        }

        /// A faded bronze divider line: transparent → bronze → transparent (Figma section rules).
        public static Image Divider(Transform parent, float height = 2f)
        {
            var go = new GameObject("Divider", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = Theme.Alpha(Theme.Bronze, 0.55f); img.raycastTarget = false;
            LayoutHeight(go, height);
            return img;
        }

        /// A Figma CostChip: a tiny dark pill with a colour dot + amount, tinted to the resource.
        public static GameObject Chip(Transform parent, Color resColor, string amount)
        {
            var go = new GameObject("Chip", typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(parent, false);
            var bg = go.GetComponent<Image>();
            bg.sprite = Theme.RoundSoft; bg.type = UnityEngine.UI.Image.Type.Sliced;
            bg.color = Theme.Alpha(Color.black, 0.4f); bg.raycastTarget = false;
            var h = go.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 4; h.padding = new RectOffset(7, 7, 3, 3);
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleCenter;
            var csf = go.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var dot = Swatch(go.transform, resColor, 0);
            dot.sprite = Theme.Disc;
            var dle = dot.gameObject.AddComponent<LayoutElement>();
            dle.preferredWidth = 9; dle.preferredHeight = 9;

            var amt = Label(go.transform, amount, 12, resColor, TextAnchor.MiddleCenter, true);
            return go;
        }

        public static Image Swatch(Transform parent, Color color, float size)
        {
            var go = new GameObject("Swatch", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = Theme.RoundSoft; img.type = UnityEngine.UI.Image.Type.Sliced; img.color = color; img.raycastTarget = false;
            if (size > 0f) ((RectTransform)go.transform).sizeDelta = new Vector2(size, size);
            return img;
        }

        /// A plain (non-sliced) tinted sprite image — for solid shapes like the badge disc/ring.
        public static Image Image(Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject("Image", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite; img.type = UnityEngine.UI.Image.Type.Simple; img.color = color; img.raycastTarget = false;
            return img;
        }

        /// A tinted sprite glyph (e.g. a Resources/NE/Icons sprite). Like Swatch but image-backed.
        public static Image Icon(Transform parent, Sprite sprite, float size, Color tint)
        {
            var go = new GameObject("Icon", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite; img.color = tint; img.preserveAspect = true; img.raycastTarget = false;
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
