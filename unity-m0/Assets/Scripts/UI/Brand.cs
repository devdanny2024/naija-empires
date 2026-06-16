using UnityEngine;
using UnityEngine.UI;

namespace NaijaEmpires
{
    /// Shared on-brand screen chrome built on top of UIKit — the bigger "Figma" pieces reused across
    /// the front-end (GameFlow) and the in-match HUD (BrandedHud): full-screen backdrops with an
    /// adire-style weave, screen corner ornaments, empire crest discs and empire selection cards.
    /// Pure presentation; no gameplay coupling.
    public static class Brand
    {
        static Vector2 V(float a, float b) => new Vector2(a, b);

        // -------------------------------------------------------------- backdrop
        /// A full-screen layered backdrop: a Night base, a softer raised centre glow (faking the
        /// Figma radial gradient) and a faint bronze weave overlay. Returns the root for content.
        public static Image Backdrop(Transform root, string name)
        {
            var bg = UI.Panel(root, Theme.Round, Theme.Night);
            bg.gameObject.name = name;
            UI.Stretch(bg.rectTransform, 0, 0, 0, 0);

            // centre glow — a large soft disc tinted slightly indigo-bright, low alpha.
            var glow = UI.Image(bg.transform, Theme.Disc, Theme.Alpha(Theme.PanelHi, 0.5f));
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = V(0.5f, 0.62f);
            glow.rectTransform.pivot = V(0.5f, 0.5f);
            glow.rectTransform.sizeDelta = new Vector2(1700, 1300);

            Weave(bg.transform, 0.05f);
            return bg;
        }

        /// A faint diagonal bronze weave (the "adire" texture), tiled across the parent. Uses the
        /// tri sprite rotated as a cheap diagonal motif; very low alpha so it reads as a subtle grain.
        public static void Weave(Transform parent, float alpha)
        {
            var go = new GameObject("Weave", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = Theme.ShineV; img.type = UnityEngine.UI.Image.Type.Tiled; img.color = Theme.Alpha(Theme.BronzeLight, alpha);
            img.raycastTarget = false;
            UI.Stretch(img.rectTransform, 0, 0, 0, 0);
        }

        /// Four bronze L-bracket ornaments hugging the screen corners (Figma splash/setup chrome).
        public static void ScreenCorners(Transform root, float inset = 26f, float len = 46f, float thick = 3f)
        {
            (Vector2 a, int sx, int sy)[] c =
            {
                (V(0, 0), +1, +1), (V(1, 0), -1, +1), (V(0, 1), +1, -1), (V(1, 1), -1, -1),
            };
            foreach (var (a, sx, sy) in c)
            {
                var h = UI.Swatch(root, Theme.Alpha(Theme.Bronze, 0.6f), 0);
                UI.Set(h.rectTransform, a, a, a, new Vector2(sx * inset, sy * inset), new Vector2(len, thick));
                var v = UI.Swatch(root, Theme.Alpha(Theme.Bronze, 0.6f), 0);
                UI.Set(v.rectTransform, a, a, a, new Vector2(sx * inset, sy * inset), new Vector2(thick, len));
                var dot = UI.Image(root, Theme.Disc, Theme.BronzeLight);
                UI.Set(dot.rectTransform, a, a, a, new Vector2(sx * inset, sy * inset), new Vector2(8, 8));
            }
        }

        // -------------------------------------------------------------- empire crest
        /// The circular empire crest from the Figma: a colour-tinted disc, a double bronze rim and a
        /// bronze diamond "device" at the centre. Returns the disc Image so callers can retint it.
        public static Image Crest(Transform parent, Color civColor, float size, Vector2 anchor, Vector2 pos)
        {
            var disc = UI.Image(parent, Theme.Disc, Theme.Alpha(civColor, 0.85f));
            UI.Set(disc.rectTransform, anchor, anchor, anchor, pos, new Vector2(size, size));
            UI.Shadow(disc, Theme.Alpha(Theme.Night, 0.6f), new Vector2(0f, -3f));

            var rim = UI.Image(disc.transform, Theme.Ring, Theme.Bronze);
            UI.Stretch(rim.rectTransform, -3, -3, -3, -3);
            var rim2 = UI.Image(disc.transform, Theme.Ring, Theme.Alpha(civColor, 0.5f));
            UI.Stretch(rim2.rectTransform, -6, -6, -6, -6);

            // top shine arc
            var shine = UI.Image(disc.transform, Theme.Disc, Theme.Alpha(Color.white, 0.10f));
            shine.rectTransform.anchorMin = shine.rectTransform.anchorMax = V(0.5f, 0.7f);
            shine.rectTransform.pivot = V(0.5f, 0.5f);
            shine.rectTransform.sizeDelta = new Vector2(size * 0.7f, size * 0.45f);

            // bronze diamond device
            var dRim = UI.Swatch(disc.transform, Theme.Alpha(Theme.Bronze, 0.7f), size * 0.34f);
            Center(dRim.rectTransform); dRim.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            var dev = UI.Swatch(disc.transform, Theme.Alpha(Theme.Ivory, 0.95f), size * 0.24f);
            Center(dev.rectTransform); dev.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            return disc;
        }

        static void Center(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = V(0.5f, 0.5f); rt.pivot = V(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero;
        }
    }
}
