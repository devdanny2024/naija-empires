using UnityEngine;
using UnityEngine.UI;

namespace NaijaEmpires
{
    /// Flat pictographic icons composed from primitive uGUI shapes (rounded rects, discs, soft
    /// diamonds) — no font emoji (which Unity can't render reliably) and no imported sprites. Used by
    /// the BUILD and TRAIN tile grids to give each building / unit a distinct glyph, Figma-style.
    public static class Glyph
    {
        public static void Building(Transform parent, BuildingKind k, float box, Color ink)
        {
            var b = Holder(parent, box);
            switch (k)
            {
                case BuildingKind.House:      House(b, ink); break;
                case BuildingKind.Barracks:   Swords(b, ink); break;
                case BuildingKind.Stable:     Arch(b, ink); break;
                case BuildingKind.Tower:      TowerGlyph(b, ink); break;
                case BuildingKind.Wall:       WallGlyph(b, ink); break;
                case BuildingKind.Farm:       FarmGlyph(b, ink); break;
                case BuildingKind.University: Cap(b, ink); break;
                case BuildingKind.Market:     MarketGlyph(b, ink); break;
                default:                      Rect(b, .5f, .5f, .55f, .55f, ink); break;
            }
        }

        public static void Unit(Transform parent, UnitType t, float box, Color ink)
        {
            var b = Holder(parent, box);
            switch (t)
            {
                case UnitType.Villager: Villager(b, ink); break;
                case UnitType.Spearman: Spear(b, ink); break;
                case UnitType.Archer:   Bow(b, ink); break;
                case UnitType.Cavalry:  HorseRider(b, ink); break;
                case UnitType.Scholar:  ScholarG(b, ink); break;
                case UnitType.Caravan:  CaravanG(b, ink); break;
                default:                Villager(b, ink); break;
            }
        }

        // ---- resource / stat badge icons --------------------------------------------------------

        public static void Resource(Transform parent, ResourceType r, float box)
        {
            var b = Holder(parent, box);
            switch (r)
            {
                case ResourceType.Yam: Yam(b); break;
                case ResourceType.Timber: Log(b); break;
                case ResourceType.Iron: Gear(b); break;
                case ResourceType.Cowries: Cowrie(b); break;
                case ResourceType.Knowledge: Scroll(b); break;
            }
        }

        public static void Pop(Transform parent, float box) { People(Holder(parent, box)); }
        public static void Age(Transform parent, float box) { StarGlyph(Holder(parent, box)); }

        static readonly Color YamC  = new Color(0.91f, 0.76f, 0.29f);
        static readonly Color LeafC = new Color(0.45f, 0.66f, 0.25f);
        static readonly Color WoodC = new Color(0.58f, 0.40f, 0.22f);
        static readonly Color IronC = new Color(0.67f, 0.70f, 0.75f);
        static readonly Color PopC  = new Color(0.64f, 0.47f, 0.80f);
        static readonly Color StarC = new Color(0.91f, 0.75f, 0.34f);

        static void Yam(RectTransform b)            // a tuber with a sprout
        {
            Rect(b, .46f, .42f, .5f, .74f, YamC, 20f);
            Rect(b, .64f, .82f, .08f, .26f, LeafC, 20f);
        }

        static void Log(RectTransform b)            // stacked logs with end-grain
        {
            Rect(b, .5f, .4f, .84f, .34f, WoodC);
            Dot(b, .5f, .4f, .28f, Dark(WoodC));
            Dot(b, .5f, .4f, .13f, WoodC);
            Rect(b, .5f, .68f, .6f, .22f, Dark(WoodC));
        }

        static void Gear(RectTransform b)           // iron gear
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f;
                float rx = .5f + Mathf.Cos(a * Mathf.Deg2Rad) * .34f;
                float ry = .5f + Mathf.Sin(a * Mathf.Deg2Rad) * .34f;
                Rect(b, rx, ry, .2f, .2f, IronC, a);
            }
            Dot(b, .5f, .5f, .54f, IronC);
            Dot(b, .5f, .5f, .2f, Dark(IronC));
        }

        static void People(RectTransform b)         // two figures
        {
            Dot(b, .35f, .66f, .3f, PopC); Rect(b, .35f, .33f, .34f, .42f, PopC);
            Dot(b, .65f, .66f, .3f, PopC); Rect(b, .65f, .33f, .34f, .42f, PopC);
        }

        static void StarGlyph(RectTransform b)      // 4-point sparkle star
        {
            Rect(b, .5f, .5f, .26f, .94f, StarC);
            Rect(b, .5f, .5f, .94f, .26f, StarC);
            Rect(b, .5f, .5f, .5f, .5f, StarC, 45f);
        }

        static readonly Color CowrieC = new Color(0.93f, 0.88f, 0.74f); // pale shell
        static readonly Color ScrollC = new Color(0.62f, 0.74f, 0.92f); // parchment-blue

        static void Cowrie(RectTransform b)         // a cowrie shell (oval + slit)
        {
            Dot(b, .5f, .5f, .82f, CowrieC);
            Rect(b, .5f, .5f, .5f, .12f, Dark(CowrieC)); // the shell's slit (rotated below)
            Rect(b, .5f, .5f, .1f, .56f, Dark(CowrieC));
        }

        static void Scroll(RectTransform b)         // a book/scroll of knowledge
        {
            Rect(b, .5f, .5f, .74f, .6f, ScrollC);       // pages
            Rect(b, .5f, .5f, .08f, .64f, Dark(ScrollC));// spine
            Rect(b, .3f, .5f, .02f, .4f, Dark(ScrollC)); // text lines
            Rect(b, .7f, .5f, .02f, .4f, Dark(ScrollC));
        }

        // ---- icon recipes (normalized coords: 0=left/bottom, 1=right/top) -----------------------

        static void House(RectTransform b, Color c)
        {
            Rect(b, .5f, .34f, .58f, .42f, c);          // body
            Rect(b, .5f, .66f, .42f, .42f, c, 45f);     // soft-diamond roof peak
            Rect(b, .5f, .26f, .16f, .22f, Dark(c));    // door
        }

        static void Swords(RectTransform b, Color c)
        {
            Rect(b, .5f, .5f, .12f, .92f, c, 45f);      // blade /
            Rect(b, .5f, .5f, .12f, .92f, c, -45f);     // blade \
            Rect(b, .5f, .5f, .5f, .12f, Dark(c));      // crossguard
        }

        static void Arch(RectTransform b, Color c)      // Stable: a stable arch
        {
            Rect(b, .5f, .5f, .8f, .8f, c);             // block
            Rect(b, .5f, .34f, .34f, .5f, Dark(c));     // arch opening (square part)
            Dot(b, .5f, .58f, .34f, Dark(c));           // arch top (round)
        }

        static void TowerGlyph(RectTransform b, Color c)
        {
            Rect(b, .5f, .42f, .42f, .78f, c);          // tall shaft
            Rect(b, .34f, .86f, .14f, .18f, c);         // battlements
            Rect(b, .5f, .86f, .14f, .18f, c);
            Rect(b, .66f, .86f, .14f, .18f, c);
        }

        static void WallGlyph(RectTransform b, Color c)
        {
            // two offset brick courses
            for (int i = 0; i < 3; i++) Rect(b, .22f + i * .28f, .62f, .24f, .22f, c);
            for (int i = 0; i < 2; i++) Rect(b, .36f + i * .28f, .36f, .24f, .22f, c);
        }

        static void FarmGlyph(RectTransform b, Color c)
        {
            Rect(b, .5f, .5f, .82f, .7f, Dark(c));      // tilled plot
            for (int i = 0; i < 3; i++) Rect(b, .5f, .34f + i * .18f, .68f, .08f, c); // furrows
        }

        static void Cap(RectTransform b, Color c)       // University: mortarboard
        {
            Rect(b, .5f, .58f, .82f, .26f, c, 45f);     // the board (soft diamond, flattened-ish)
            Rect(b, .5f, .4f, .3f, .26f, c);            // head/base
            Rect(b, .72f, .5f, .05f, .26f, c);          // tassel
        }

        static void Villager(RectTransform b, Color c)
        {
            Dot(b, .5f, .74f, .34f, c);                 // head
            Rect(b, .5f, .34f, .5f, .42f, c);           // body
            Rect(b, .74f, .44f, .1f, .5f, Dark(c), 20f);// tool
        }

        static void Spear(RectTransform b, Color c)
        {
            Dot(b, .42f, .74f, .3f, c);                 // head
            Rect(b, .42f, .34f, .42f, .42f, c);         // body
            Rect(b, .74f, .5f, .07f, .96f, Dark(c));    // spear shaft
        }

        static void Bow(RectTransform b, Color c)
        {
            Dot(b, .4f, .74f, .3f, c);                  // head
            Rect(b, .4f, .34f, .42f, .42f, c);          // body
            Rect(b, .66f, .5f, .07f, .8f, Dark(c), 0f); // bow stave
            Rect(b, .66f, .5f, .56f, .06f, Dark(c), 35f);// arrow
        }

        static void HorseRider(RectTransform b, Color c) // Cavalry
        {
            Rect(b, .5f, .42f, .72f, .26f, c);          // horse body
            Rect(b, .76f, .56f, .2f, .34f, c, 18f);     // neck/head
            Rect(b, .34f, .22f, .08f, .26f, Dark(c));   // legs
            Rect(b, .58f, .22f, .08f, .26f, Dark(c));
            Dot(b, .44f, .72f, .26f, c);                // rider head
        }

        static void ScholarG(RectTransform b, Color c)  // Scholar: figure with a scroll
        {
            Dot(b, .42f, .74f, .3f, c);                 // head
            Rect(b, .42f, .34f, .42f, .42f, c);         // robe
            Rect(b, .72f, .46f, .16f, .34f, Dark(c));   // scroll/book
        }

        static void CaravanG(RectTransform b, Color c)  // Caravan: a trade cart
        {
            Rect(b, .5f, .5f, .72f, .32f, c);           // cart body
            Rect(b, .5f, .74f, .56f, .18f, Dark(c));    // load
            Dot(b, .34f, .24f, .26f, Dark(c));          // wheel
            Dot(b, .66f, .24f, .26f, Dark(c));          // wheel
        }

        static void MarketGlyph(RectTransform b, Color c) // Market: a stall
        {
            Rect(b, .5f, .76f, .92f, .16f, c);          // awning
            Rect(b, .28f, .44f, .08f, .5f, c);          // left post
            Rect(b, .72f, .44f, .08f, .5f, c);          // right post
            Rect(b, .5f, .3f, .74f, .14f, Dark(c));     // counter
        }

        // ---- primitives -------------------------------------------------------------------------

        static RectTransform Holder(Transform parent, float box)
        {
            var go = new GameObject("Glyph", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.pivot = new Vector2(.5f, .5f);
            rt.sizeDelta = new Vector2(box, box); rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        static Image Rect(RectTransform box, float nx, float ny, float w, float h, Color c, float rot = 0f)
        {
            var img = UI.Swatch(box, c, 0);
            float B = box.sizeDelta.x;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.pivot = new Vector2(.5f, .5f);
            rt.sizeDelta = new Vector2(w * B, h * B);
            rt.anchoredPosition = new Vector2((nx - .5f) * B, (ny - .5f) * B);
            if (rot != 0f) rt.localRotation = Quaternion.Euler(0, 0, rot);
            return img;
        }

        static Image Dot(RectTransform box, float nx, float ny, float d, Color c)
        {
            var img = UI.Image(box, Theme.Disc, c);
            float B = box.sizeDelta.x;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.pivot = new Vector2(.5f, .5f);
            rt.sizeDelta = new Vector2(d * B, d * B);
            rt.anchoredPosition = new Vector2((nx - .5f) * B, (ny - .5f) * B);
            return img;
        }

        static Color Dark(Color c) => new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f, c.a);
    }
}
