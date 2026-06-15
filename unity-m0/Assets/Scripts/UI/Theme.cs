using UnityEngine;

namespace NaijaEmpires
{
    /// Naija Empires brand identity — the single source of truth for UI colour, type and surfaces.
    /// "Bronze & Indigo": Benin bronze-plaque gold on Yoruba adire indigo. See BRAND.md.
    public static class Theme
    {
        // ---- Palette (hex in comments) -------------------------------------------------
        public static readonly Color Bronze      = Hex(0xC8901E); // primary accent / actions
        public static readonly Color BronzeLight = Hex(0xE6BC63); // hover / highlight
        public static readonly Color BronzeDeep  = Hex(0x8A5E16); // pressed / rule lines

        public static readonly Color Night   = Hex(0x121829);     // app background tone
        public static readonly Color Panel   = Hex(0x1B2238);     // panel fill
        public static readonly Color PanelHi = Hex(0x27314F);     // raised / selected fill

        public static readonly Color Ivory = Hex(0xF3E7CC);       // primary text
        public static readonly Color Muted = Hex(0xB9A98A);       // secondary text
        public static readonly Color Faint = Hex(0x6E6A5E);       // disabled text

        public static readonly Color Confirm = Hex(0x5BA84F);     // affordable / win
        public static readonly Color Danger  = Hex(0xC0492F);     // can't afford / defeat / enemy

        // Resource accents (also the lozenge swatch colours)
        public static readonly Color Yam    = Hex(0xE9C24A);
        public static readonly Color Timber = Hex(0x5E9B47);
        public static readonly Color Iron   = Hex(0x9AA3AE);

        // Team colours (color-blind-safe; mirror GameConfig)
        public static readonly Color Benin = Hex(0x3389F2);
        public static readonly Color Oyo   = Hex(0xE65242);

        // ---- Type ----------------------------------------------------------------------
        public const int TitleSize = 26;
        public const int LabelSize = 20;
        public const int BodySize  = 17;
        public const int SmallSize = 14;

        static Font _font;
        public static Font Font
        {
            get
            {
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        // ---- Surfaces ------------------------------------------------------------------
        static Sprite _round, _roundSoft, _pill;
        public static Sprite Round     => _round     ??= RoundedSprite(16); // panels
        public static Sprite RoundSoft => _roundSoft ??= RoundedSprite(10); // buttons/cards
        public static Sprite Pill      => _pill      ??= RoundedSprite(24); // resource bar

        static Color Hex(uint rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        public static Color Alpha(Color c, float a) { c.a = a; return c; }

        /// A white rounded-rect texture for 9-slice tinting. Corners are the border so they never stretch.
        static Sprite RoundedSprite(int r)
        {
            int size = r * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[size * size];
            float cx0 = r, cy0 = r, cx1 = size - 1 - r, cy1 = size - 1 - r;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a = 1f;
                float dx = x < cx0 ? cx0 - x : (x > cx1 ? x - cx1 : 0f);
                float dy = y < cy0 ? cy0 - y : (y > cy1 ? y - cy1 : 0f);
                if (dx > 0f || dy > 0f)
                {
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    a = Mathf.Clamp01(r - d + 0.5f); // 1px antialiased edge
                }
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            var border = new Vector4(r, r, r, r);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
    }
}
