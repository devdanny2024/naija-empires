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

        // Team colours per empire (mirror GameConfig.UnitConfig.CivColor)
        public static readonly Color Benin      = Hex(0x3389F2); // indigo-blue
        public static readonly Color Oyo        = Hex(0xE65242); // terracotta red
        public static readonly Color Sokoto     = Hex(0x45B866); // green
        public static readonly Color KanemBornu = Hex(0xEBBD33); // gold

        // ---- Type ----------------------------------------------------------------------
        public const int TitleSize = 26;
        public const int LabelSize = 20;
        public const int BodySize  = 17;
        public const int SmallSize = 14;

        // Body text — a humanist sans (less generic than Arial), loaded from the OS so no asset import.
        static Font _font;
        public static Font Font => _font ??= LoadOS(new[] { "Trebuchet MS", "Segoe UI", "Verdana", "Arial" });

        // Display / wordmark / headers — a heritage serif for the "empire" feel.
        static Font _display;
        public static Font Display => _display ??= LoadOS(new[] { "Palatino Linotype", "Book Antiqua", "Georgia", "Constantia", "Trebuchet MS" });

        static Font LoadOS(string[] names)
        {
            // Fully-qualified: the property named 'Font' on this class shadows the Font type here.
            var f = UnityEngine.Font.CreateDynamicFontFromOSFont(names, 32);
            return f != null ? f
                : (Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf"));
        }

        // ---- Surfaces ------------------------------------------------------------------
        static Sprite _round, _roundSoft, _pill, _disc, _ring;
        public static Sprite Round     => _round     ??= RoundedSprite(16); // panels
        public static Sprite RoundSoft => _roundSoft ??= RoundedSprite(10); // buttons/cards
        public static Sprite Pill      => _pill      ??= RoundedSprite(24); // resource bar
        public static Sprite Disc      => _disc      ??= DiscSprite();       // solid circle (badge body)
        public static Sprite Ring      => _ring      ??= RingSprite();       // bronze rim of a badge

        // ---- Icons ---------------------------------------------------------------------
        static Sprite _popIcon;
        public static Sprite PopIcon => _popIcon ??= Resources.Load<Sprite>("NE/Icons/pop"); // person glyph

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

        /// A solid antialiased white disc — the body of a floating resource badge (tint in code).
        static Sprite DiscSprite()
        {
            const int size = 96; float r = size * 0.5f, c = r - 0.5f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d - 0.5f));
            }
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// A hollow white annulus — the carved bronze rim around a badge (tint in code).
        static Sprite RingSprite()
        {
            const int size = 96; float r = size * 0.5f, c = r - 0.5f, inner = r - 7f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01(r - d - 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
