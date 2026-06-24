using UnityEngine;

namespace NaijaEmpires
{
    /// Naija Empires brand identity — the single source of truth for UI colour, type and surfaces.
    /// "Bronze & Indigo": Benin bronze-plaque gold on Yoruba adire indigo. See BRAND.md.
    public static class Theme
    {
        // ---- Palette — "Midnight & Gold": charcoal-glass surfaces + a single warm-gold accent. Clean,
        // high-contrast, minimal ornament. (Names kept stable so the whole HUD restyles from here.)
        public static readonly Color Bronze      = Hex(0xE8B24A); // primary accent / actions (gold)
        public static readonly Color BronzeLight = Hex(0xF6D58A); // hover / highlight / headers
        public static readonly Color BronzeDeep  = Hex(0xA9781F); // pressed / rule lines

        public static readonly Color Night   = Hex(0x0B0E13);     // app background tone (near-black)
        public static readonly Color Panel   = Hex(0x151A23);     // panel fill (charcoal glass)
        public static readonly Color PanelHi = Hex(0x202734);     // raised / selected fill

        public static readonly Color Ivory = Hex(0xF3F1EB);       // primary text (off-white)
        public static readonly Color Muted = Hex(0x9AA1AD);       // secondary text
        public static readonly Color Faint = Hex(0x596070);       // disabled text

        public static readonly Color Confirm     = Hex(0x53B96A); // affordable / win
        public static readonly Color ConfirmLit  = Hex(0x6BD183); // confirm button top of gradient
        public static readonly Color ConfirmDeep = Hex(0x2F7A43); // confirm button base / borders
        public static readonly Color Danger      = Hex(0xE0574A); // can't afford / defeat / enemy
        public static readonly Color DangerLit   = Hex(0xEC6F5E); // danger button top of gradient
        public static readonly Color DangerDeep  = Hex(0x9E3326); // danger button base / borders

        // Panel gradient stops — a soft charcoal gradient (top slightly lifted) over Night.
        public static readonly Color PanelTop = Hex(0x1B2230);    // top of a panel gradient
        public static readonly Color PanelBot = Hex(0x10151E);    // bottom of a panel gradient

        // Resource accents (also the lozenge swatch colours)
        public static readonly Color Yam    = Hex(0xF0B83E);
        public static readonly Color Timber = Hex(0x5FBE74);
        public static readonly Color Iron   = Hex(0xAEB6C2);

        // Team colours per empire (mirror GameConfig.UnitConfig.CivColor)
        public static readonly Color Benin      = Hex(0x3389F2); // indigo-blue
        public static readonly Color Oyo        = Hex(0xE65242); // terracotta red
        public static readonly Color Sokoto     = Hex(0x45B866); // green
        public static readonly Color KanemBornu = Hex(0xEBBD33); // gold

        // ---- Type ---------------------------------------------------------------------- (bumped up
        // for legibility; the HUD canvas also scales ~1.35x, so on-screen these are clearly readable)
        public const int TitleSize = 30;
        public const int LabelSize = 23;
        public const int BodySize  = 20;
        public const int SmallSize = 16;

        // Body text & numerals — the Figma uses 'Inter' (a neutral humanist sans). Inter isn't an OS
        // font, so we approximate with the closest stock UI sans; numerals stay clean and tabular-ish.
        static Font _font;
        public static Font Font => _font ??= LoadOS(new[] { "Segoe UI", "Tahoma", "Trebuchet MS", "Verdana", "Arial" });

        // Display / wordmark / headers — the Figma uses 'Cinzel', an inscriptional Roman serif (wide
        // caps, engraved feel). Cinzel isn't an OS font; we approximate with a heritage serif and always
        // pair it with UPPERCASE + wide letter-spacing (see UI.Header / titles) to read as "Cinzel".
        static Font _display;
        public static Font Display => _display ??= LoadOS(new[] { "Constantia", "Palatino Linotype", "Book Antiqua", "Georgia", "Cambria", "Trebuchet MS" });

        static Font LoadOS(string[] names)
        {
            // Fully-qualified: the property named 'Font' on this class shadows the Font type here.
            var f = UnityEngine.Font.CreateDynamicFontFromOSFont(names, 32);
            return f != null ? f
                : (Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf"));
        }

        // ---- Surfaces ------------------------------------------------------------------
        static Sprite _round, _roundSoft, _pill, _disc, _ring, _tri, _shineV, _frame;
        public static Sprite Round     => _round     ??= RoundedSprite(22); // panels (softer, modern)
        public static Sprite RoundSoft => _roundSoft ??= RoundedSprite(14); // buttons/cards
        public static Sprite RoundFrame => _frame    ??= RoundedFrameSprite(14, 2); // thin hollow outline

        // Glossy 3-stop vertical-gradient button fills (light top → mid → dark bottom), 9-sliced so they
        // round + gloss at any size — the Figma GameButton look. Tint stays white (gradient is baked in).
        static Sprite _btnP, _btnS, _btnD, _btnC;
        // Near-flat fills with the faintest top-light (modern, not the old heavy gloss).
        public static Sprite BtnPrimary   => _btnP ??= GradientSprite(Hex(0xF1C863), Hex(0xE8B24A), Hex(0xD49C34));
        public static Sprite BtnSecondary => _btnS ??= GradientSprite(Hex(0x242C3A), Hex(0x1E2532), Hex(0x18202B));
        public static Sprite BtnDanger    => _btnD ??= GradientSprite(Hex(0xE9665A), Hex(0xE0574A), Hex(0xC8453A));
        public static Sprite BtnConfirm   => _btnC ??= GradientSprite(Hex(0x62C77A), Hex(0x53B96A), Hex(0x43A055));
        public static Sprite Pill      => _pill      ??= RoundedSprite(24); // resource bar
        public static Sprite Disc      => _disc      ??= DiscSprite();       // solid circle (badge body)
        public static Sprite Ring      => _ring      ??= RingSprite();       // bronze rim of a badge
        public static Sprite Tri       => _tri       ??= TriangleSprite();   // bronze corner ornament
        // Vertical white→transparent gradient (top bright). Tint + layer over a fill for the
        // "inner shine / glossy button" feel of the Figma GameButton/Panel without real gradients.
        public static Sprite ShineV    => _shineV    ??= VShineSprite();

        // ---- Icons ---------------------------------------------------------------------
        static Sprite _popIcon;
        public static Sprite PopIcon => _popIcon ??= Resources.Load<Sprite>("NE/Icons/pop"); // person glyph

        static Color Hex(uint rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        /// Public hex → Color for one-off scene-dressing tones (silhouettes, art tints) not in the palette.
        public static Color Hex2(uint rgb) => Hex(rgb);

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

        /// A hollow rounded-rectangle outline (9-sliced) — a tile/card border, tinted in code. The
        /// frame = a filled rounded rect minus an inset filled rounded rect, so only a `thickness`-px
        /// ring is opaque; 9-slice border = corner radius so it stretches to any tile size cleanly.
        static Sprite RoundedFrameSprite(int r, int thickness)
        {
            int size = r * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[size * size];
            float cx0 = r, cy0 = r, cx1 = size - 1 - r, cy1 = size - 1 - r;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x < cx0 ? cx0 - x : (x > cx1 ? x - cx1 : 0f);
                float dy = y < cy0 ? cy0 - y : (y > cy1 ? y - cy1 : 0f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float outer = Mathf.Clamp01(r - d + 0.5f);            // inside the rounded rect
                float inner = Mathf.Clamp01((r - thickness) - d + 0.5f); // inside the inset rect
                float a = Mathf.Clamp01(outer - inner);                // the ring between them
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            var border = new Vector4(r, r, r, r);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        /// A 9-sliced rounded sprite with a baked 3-stop vertical gradient (bottom→mid→top). The middle
        /// scales with the button height so the gloss reads correctly at any size; corners stay rounded.
        static Sprite GradientSprite(Color top, Color mid, Color bottom)
        {
            int r = 10, w = r * 2 + 4, h = 80;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            float cx0 = r, cy0 = r, cx1 = w - 1 - r, cy1 = h - 1 - r;
            for (int y = 0; y < h; y++)
            {
                float v = y / (float)(h - 1); // 0 bottom → 1 top
                Color col = v >= 0.5f ? Color.Lerp(mid, top, (v - 0.5f) * 2f) : Color.Lerp(bottom, mid, v * 2f);
                for (int x = 0; x < w; x++)
                {
                    float dx = x < cx0 ? cx0 - x : (x > cx1 ? x - cx1 : 0f);
                    float dy = y < cy0 ? cy0 - y : (y > cy1 ? y - cy1 : 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * w + x] = new Color(col.r, col.g, col.b, Mathf.Clamp01(r - d + 0.5f));
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
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

        /// A right-triangle filling the lower-left half — the carved bronze corner ornament.
        /// Rotate via RectTransform to place on any corner (see UI.Corners).
        static Sprite TriangleSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // keep the top-left triangle: x + (size-1-y) <= size  ->  x <= y
                float edge = y - x; // >0 inside, ~0 at the hypotenuse
                px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(edge + 0.5f));
            }
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// A vertical white gradient, opaque at the top fading to clear at ~45% down — overlaid
        /// (tinted, low alpha) on buttons/cards to fake the Figma "inner top shine".
        static Sprite VShineSprite()
        {
            const int w = 4, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float top = y / (float)(h - 1);          // 0 at bottom, 1 at top
                float a = Mathf.Clamp01((top - 0.55f) / 0.45f); // clear below 55%, ramp to top
                for (int x = 0; x < w; x++) px[y * w + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px); tex.Apply();
            // 9-slice border kept 0 — it stretches as a simple sliced fill across any width.
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(0, 0, 0, 0));
        }
    }
}
