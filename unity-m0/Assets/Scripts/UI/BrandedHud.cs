using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace NaijaEmpires
{
    /// Branded landscape HUD (uGUI, built from code). Replaces the old IMGUI HUD.
    /// Bronze-&-Indigo identity per Theme/BRAND.md: resource bar + age, build dock,
    /// train dock (when a production building is selected), and a victory/defeat banner.
    public class BrandedHud : MonoBehaviour
    {
        Badge _yam, _timber, _iron, _pop, _age;
        Text _civ; Image _crest, _crestRim;
        Button _ageBtn; Text _ageBtnLabel;
        readonly List<Card> _build = new();
        Image _trainDock; Transform _trainList; Text _trainTitle;
        ProductionBuilding _shownBuilding;
        readonly List<(UnitType type, Card card)> _trainCards = new();
        GameObject _banner; Text _bannerText;

        RectTransform _miniArea; Image _camMarker;
        readonly List<Image> _blips = new();
        const float WorldHalf = 42f; // playable island half-extent in world units

        static readonly BuildingKind[] Buildables =
            { BuildingKind.House, BuildingKind.Barracks, BuildingKind.Tower, BuildingKind.Stable, BuildingKind.Wall };

        class Card { public Button btn; public Text label; public Text cost; }

        /// A floating bronze-rimmed resource badge with an animated count and a "+N" gather pop.
        /// `shown` ticks toward `target`; `pop` is the green delta label, fading + rising when set.
        class Badge
        {
            public RectTransform root;     // for the idle float
            public Text value;             // the big count
            public Text pop;               // "+N" gather flash
            public float shown, target;    // animated count (resources only)
            public bool seeded;            // first reading sets the count silently (no "+N")
            public float popTimer;         // counts down while the +N is visible
            public float phase;            // float-bob offset so badges don't bob in unison
            public float baseY;            // resting Y for the bob
        }

        void Awake()
        {
            EnsureEventSystem();
            var canvas = BuildCanvas();
            BuildResourceBar(canvas);
            BuildBuildDock(canvas);
            BuildMinimap(canvas);
            BuildTrainDock(canvas);
            BuildHint(canvas);
            BuildBanner(canvas);
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        Transform BuildCanvas()
        {
            var go = new GameObject("BrandedHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            s.matchWidthOrHeight = 1f; // match height — stable for landscape
            return go.transform;
        }

        // ---------------------------------------------------------------- resource HUD
        // A floating cluster of bronze-rimmed badges (no backing "document" bar) +
        // the player's empire crest at top-left and a prominent Advance-Age at top-right.
        void BuildResourceBar(Transform root)
        {
            BuildCrest(root);

            // Badges flow left→right from just right of the crest, top-anchored so they
            // hang from the screen edge like a game HUD.
            const float step = 122f, top = -18f;
            float x = 300f;
            _yam    = MakeBadge(root, "YAM",    Theme.Yam,         x,            top); x += step;
            _timber = MakeBadge(root, "TIMBER", Theme.Timber,      x,            top); x += step;
            _iron   = MakeBadge(root, "IRON",   Theme.Iron,        x,            top); x += step + 14f;
            _pop    = MakeBadge(root, "POP",    Theme.Ivory,       x,            top, Theme.PopIcon); x += step;
            _age    = MakeBadge(root, "AGE",    Theme.BronzeLight, x,            top);

            (_ageBtn, _ageBtnLabel) = UI.Button(root, "Advance Age", () => Ages.TryAdvance(FactionId.Player));
            UI.Set(_ageBtn.GetComponent<RectTransform>(), V(1, 1), V(1, 1), V(1, 1),
                   new Vector2(-18, -22), new Vector2(252, 56));
            _ageBtn.image.sprite = Theme.Pill;
            _ageBtn.image.color = Theme.Bronze;
            UI.Border(_ageBtn.image, Theme.Pill, Theme.Alpha(Theme.BronzeLight, 0.8f));
            _ageBtnLabel.color = Theme.Night;
            UI.Shadow(_ageBtnLabel, Theme.Alpha(Theme.BronzeLight, 0.4f), new Vector2(0f, -1f));
        }

        // The player's empire crest: a bronze-rimmed disc tinted with the empire colour,
        // a small bronze diamond device, and the empire name.
        void BuildCrest(Transform root)
        {
            const float top = -18f;
            _crest = UI.Image(root, Theme.Disc, Theme.Benin);
            UI.Set(_crest.rectTransform, V(0, 1), V(0, 1), V(0, 1), new Vector2(74, top - 36), new Vector2(72, 72));
            UI.Shadow(_crest, Theme.Alpha(Theme.Night, 0.6f), new Vector2(0f, -3f));
            _crestRim = UI.Image(_crest.transform, Theme.Ring, Theme.Bronze);
            UI.Stretch(_crestRim.rectTransform, -3, -3, -3, -3);

            var device = UI.Swatch(_crest.transform, Theme.Alpha(Theme.Ivory, 0.92f), 22);
            device.rectTransform.anchorMin = device.rectTransform.anchorMax = V(.5f, .5f);
            device.rectTransform.pivot = V(.5f, .5f);
            device.rectTransform.anchoredPosition = Vector2.zero;
            device.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);

            var title = UI.Label(root, "NAIJA EMPIRES", Theme.LabelSize, Theme.Bronze, TextAnchor.LowerLeft, true, Theme.Display);
            UI.Shadow(title, Theme.Alpha(Theme.Night, 0.7f), new Vector2(1.5f, -1.5f));
            UI.Set(title.rectTransform, V(0, 1), V(0, 1), V(0, 1), new Vector2(118, top - 36), new Vector2(220, 26));
            _civ = UI.Label(root, "Benin Empire", Theme.SmallSize, Theme.Muted, TextAnchor.UpperLeft);
            UI.Set(_civ.rectTransform, V(0, 1), V(0, 1), V(0, 1), new Vector2(120, top - 62), new Vector2(220, 20));
        }

        Badge MakeBadge(Transform root, string caption, Color accent, float x, float top, Sprite icon = null)
        {
            // Container so the whole badge (disc + rim + text + caption) bobs together.
            var go = new GameObject("Badge", typeof(RectTransform));
            go.transform.SetParent(root, false);
            var rt = (RectTransform)go.transform;
            UI.Set(rt, V(0, 1), V(0, 1), V(0, 1), new Vector2(x, top - 36), new Vector2(112, 72));

            // raised disc body + carved bronze rim
            var disc = UI.Image(go.transform, Theme.Disc, Theme.Alpha(Theme.Panel, 0.98f));
            UI.Set(disc.rectTransform, V(0, .5f), V(0, .5f), V(.5f, .5f), new Vector2(34, 0), new Vector2(64, 64));
            UI.Shadow(disc, Theme.Alpha(Theme.Night, 0.6f), new Vector2(0f, -3f));
            var rim = UI.Image(disc.transform, Theme.Ring, Theme.Bronze);
            UI.Stretch(rim.rectTransform, -3, -3, -3, -3);

            // resource glyph inside the disc
            if (icon != null)
            {
                var ic = UI.Icon(disc.transform, icon, 26, accent);
                ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = V(.5f, .62f);
                ic.rectTransform.pivot = V(.5f, .5f); ic.rectTransform.anchoredPosition = Vector2.zero;
            }
            else
            {
                // On-brand gem: a bronze-rimmed diamond, honest where no resource icon fits.
                var grim = UI.Swatch(disc.transform, Theme.Alpha(Theme.Bronze, 0.6f), 22);
                Center(grim.rectTransform, V(.5f, .62f)); grim.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                var gem = UI.Swatch(disc.transform, accent, 15);
                Center(gem.rectTransform, V(.5f, .62f)); gem.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            }

            // caption inside the lower arc of the disc
            var cap = UI.Label(disc.transform, caption, 11, Theme.Alpha(Theme.Muted, 0.95f), TextAnchor.LowerCenter, true);
            UI.Set(cap.rectTransform, V(.5f, 0), V(.5f, 0), V(.5f, 0), new Vector2(0, 8), new Vector2(70, 14));

            // big count to the right of the disc
            var val = UI.Label(go.transform, "0", Theme.TitleSize, Theme.Ivory, TextAnchor.MiddleLeft, true, Theme.Display);
            UI.Shadow(val, Theme.Alpha(Theme.Night, 0.7f), new Vector2(1f, -1f));
            UI.Set(val.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(70, 1), new Vector2(64, 30));

            // "+N" gather flash, hidden until a gain is detected (rises above the count)
            var pop = UI.Label(go.transform, "", Theme.BodySize, Theme.Confirm, TextAnchor.MiddleLeft, true);
            UI.Set(pop.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(72, 16), new Vector2(64, 24));
            UI.Shadow(pop, Theme.Alpha(Theme.Night, 0.7f), new Vector2(1f, -1f));
            var pc = pop.color; pc.a = 0f; pop.color = pc;

            return new Badge { root = rt, value = val, pop = pop, phase = x * 0.013f, baseY = top - 36 };
        }

        static void Center(RectTransform rt, Vector2 at)
        {
            rt.anchorMin = rt.anchorMax = at; rt.pivot = V(.5f, .5f); rt.anchoredPosition = Vector2.zero;
        }

        // Tick a resource badge's displayed count toward its true value and flash a green
        // "+N" whenever the value jumps up (gathering). Down-changes (spending) just settle.
        void TickBadge(Badge b, int value, float dt)
        {
            if (!b.seeded) { b.seeded = true; b.shown = b.target = value; b.value.text = value.ToString(); return; }
            if (value > b.target)
            {
                int gain = value - Mathf.RoundToInt(b.target);
                b.pop.text = "+" + gain;
                b.popTimer = 1.1f;
            }
            b.target = value;
            // ease the shown number toward the target (snap if very close so it lands clean)
            b.shown = Mathf.Abs(b.target - b.shown) < 0.6f ? b.target
                                                           : Mathf.Lerp(b.shown, b.target, 1f - Mathf.Exp(-12f * dt));
            b.value.text = Mathf.RoundToInt(b.shown).ToString();
        }

        // Idle float of every badge + fade/rise of any active "+N" pop.
        void AnimateBadges(float dt)
        {
            float t = Time.unscaledTime;
            foreach (var b in new[] { _yam, _timber, _iron, _pop, _age })
            {
                if (b?.root != null)
                {
                    var p = b.root.anchoredPosition;
                    p.y = b.baseY + Mathf.Sin(t * 1.7f + b.phase * 6.283f) * 2.2f;
                    b.root.anchoredPosition = p;
                }
                if (b != null && b.popTimer > 0f)
                {
                    b.popTimer -= dt;
                    float k = Mathf.Clamp01(b.popTimer / 1.1f);   // 1→0 over the lifetime
                    var c = b.pop.color; c.a = k; b.pop.color = c;
                    var pr = b.pop.rectTransform;
                    pr.anchoredPosition = new Vector2(72, 16 + (1f - k) * 22f); // rise as it fades
                    if (b.popTimer <= 0f) { c.a = 0f; b.pop.color = c; }
                }
            }
        }

        // ---------------------------------------------------------------- build dock
        void BuildBuildDock(Transform root)
        {
            var dock = UI.Panel(root, Theme.Round, Theme.Alpha(Theme.Panel, 0.96f));
            UI.Set(dock.rectTransform, V(0, 0), V(0, 0), V(0, 0), new Vector2(14, 14), new Vector2(330, 382));
            UI.Border(dock, Theme.Round, Theme.Alpha(Theme.Bronze, 0.5f));
            var col = UI.Col(dock.transform, 8, new RectOffset(16, 16, 14, 14));
            UI.Header(col.transform, "BUILD");

            foreach (var k in Buildables)
            {
                var kind = k;
                _build.Add(MakeCard(col.transform, BuildingColor(kind),
                    () => { if (BuildPlacer.Instance != null) BuildPlacer.Instance.BeginPlace(kind); }));
            }
        }

        // ---------------------------------------------------------------- train dock
        void BuildTrainDock(Transform root)
        {
            _trainDock = UI.Panel(root, Theme.Round, Theme.Alpha(Theme.Panel, 0.96f));
            // sits above the minimap (which occupies the bottom-right corner)
            UI.Set(_trainDock.rectTransform, V(1, 0), V(1, 0), V(1, 0), new Vector2(-14, 276), new Vector2(300, 290));
            UI.Border(_trainDock, Theme.Round, Theme.Alpha(Theme.Bronze, 0.5f));
            var col = UI.Col(_trainDock.transform, 8, new RectOffset(16, 16, 14, 14));
            _trainTitle = UI.Header(col.transform, "TRAIN");
            _trainList = col.transform;
            _trainDock.gameObject.SetActive(false);
        }

        void RefreshTrain(ProductionBuilding pb, Economy e)
        {
            bool show = pb != null && e != null;
            _trainDock.gameObject.SetActive(show);
            if (!show) { _shownBuilding = null; return; }

            if (pb != _shownBuilding)
            {
                _shownBuilding = pb;
                foreach (var (_, card) in _trainCards) Destroy(card.btn.gameObject);
                _trainCards.Clear();
                foreach (var u in pb.Trainable)
                {
                    var type = u;
                    _trainCards.Add((type, MakeCard(_trainList, Theme.BronzeLight, () => pb.Train(type))));
                }
            }

            foreach (var (type, card) in _trainCards)
            {
                Cost c = UnitConfig.CostOf(type);
                bool ageOk = e.Age >= UnitConfig.AgeRequired(type);
                bool ok = ageOk && pb.CanTrain(type) && e.CanAfford(c) && e.HasPop(1);
                card.label.text = type.ToString();
                card.cost.text = ageOk ? Fmt(c) : $"Age {UnitConfig.AgeRequired(type)}";
                StyleCard(card, ok, ageOk, c, e);
            }
            _trainTitle.text = pb.QueueCount > 0 ? $"TRAIN  ·  queue {pb.QueueCount}" : "TRAIN";
        }

        // ---------------------------------------------------------------- shared card
        Card MakeCard(Transform parent, Color accent, System.Action onClick)
        {
            var (btn, _) = UI.Button(parent, "", onClick, blank: true);
            UI.LayoutHeight(btn.gameObject, 52);

            var bar = UI.Swatch(btn.transform, accent, 0);
            UI.Set(bar.rectTransform, V(0, 0), V(0, 1), V(0, .5f), new Vector2(7, 0), new Vector2(6, -14));

            var name = UI.Label(btn.transform, "", Theme.BodySize, Theme.Ivory, TextAnchor.MiddleLeft, true);
            UI.Stretch(name.rectTransform, 22, 0, 96, 0);
            var cost = UI.Label(btn.transform, "", Theme.SmallSize, Theme.Confirm, TextAnchor.MiddleRight);
            UI.Stretch(cost.rectTransform, 0, 0, 14, 0);

            return new Card { btn = btn, label = name, cost = cost };
        }

        void StyleCard(Card card, bool ok, bool ageOk, Cost c, Economy e)
        {
            card.btn.interactable = ok;
            card.label.color = ageOk ? Theme.Ivory : Theme.Faint;
            card.cost.color = !ageOk ? Theme.Muted : (e.CanAfford(c) ? Theme.Confirm : Theme.Danger);
        }

        // ---------------------------------------------------------------- hint + banner
        void BuildHint(Transform root)
        {
            var t = UI.Label(root, "Drag-select units   ·   Right-click: move / gather / attack   ·   WASD pan · scroll zoom · Space: go to base",
                             Theme.SmallSize, Theme.Alpha(Theme.Muted, 0.9f), TextAnchor.LowerCenter);
            UI.Set(t.rectTransform, V(.5f, 0), V(.5f, 0), V(.5f, 0), new Vector2(0, 12), new Vector2(940, 26));
        }

        void BuildBanner(Transform root)
        {
            var p = UI.Panel(root, Theme.Round, Theme.Alpha(Theme.Night, 0.92f));
            _banner = p.gameObject;
            UI.Set(p.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), Vector2.zero, new Vector2(540, 210));
            UI.Border(p, Theme.Round, Theme.Bronze);
            var col = UI.Col(p.transform, 6, new RectOffset(0, 0, 36, 30));
            _bannerText = UI.Label(col.transform, "VICTORY", 64, Theme.Confirm, TextAnchor.MiddleCenter, true, Theme.Display);
            UI.Shadow(_bannerText, Theme.Alpha(Theme.Night, 0.8f), new Vector2(2f, -2f));
            UI.LayoutHeight(_bannerText.gameObject, 80);
            UI.Label(col.transform, "Press Play again to rematch", Theme.BodySize, Theme.Muted, TextAnchor.MiddleCenter);
            _banner.SetActive(false);
        }

        // ---------------------------------------------------------------- per-frame
        void Update()
        {
            var e = Match.Econ(FactionId.Player);
            if (e != null)
            {
                float dt = Time.unscaledDeltaTime;
                TickBadge(_yam, e.Yam, dt);
                TickBadge(_timber, e.Timber, dt);
                TickBadge(_iron, e.Iron, dt);
                _pop.value.text = $"{e.PopUsed}/{e.PopCap}";
                _age.value.text = e.Age.ToString();
                _civ.text = e.Civ + " Empire";

                Color civ = UnitConfig.CivColor(e.Civ);
                _crest.color = civ;
                _crestRim.color = Color.Lerp(Theme.Bronze, civ, 0.25f);

                AnimateBadges(dt);

                if (e.Age < Ages.Max)
                {
                    Cost c = Ages.CostFor(e.Age + 1);
                    _ageBtn.interactable = e.CanAfford(c);
                    _ageBtnLabel.text = $"Advance to Age {e.Age + 1}  ({Fmt(c)})";
                }
                else { _ageBtnLabel.text = "Max Age"; _ageBtn.interactable = false; }

                for (int i = 0; i < Buildables.Length; i++)
                {
                    var k = Buildables[i];
                    var card = _build[i];
                    bool ageOk = e.Age >= BuildingConfig.AgeRequired(k);
                    Cost c = BuildingConfig.CostOf(k, e.Civ);
                    card.label.text = k.ToString();
                    card.cost.text = ageOk ? Fmt(c) : $"Age {BuildingConfig.AgeRequired(k)}";
                    StyleCard(card, ageOk && e.CanAfford(c), ageOk, c, e);
                }
            }

            RefreshTrain(SelectionManager.Instance != null ? SelectionManager.Instance.SelectedBuilding : null, e);
            UpdateMinimap();

            if (Match.Over && !_banner.activeSelf)
            {
                bool win = Match.Winner == FactionId.Player;
                _bannerText.text = win ? "VICTORY" : "DEFEAT";
                _bannerText.color = win ? Theme.Confirm : Theme.Danger;
                _banner.SetActive(true);
            }
        }

        // ---------------------------------------------------------------- minimap
        void BuildMinimap(Transform root)
        {
            var panel = UI.Panel(root, Theme.Round, new Color(0.07f, 0.14f, 0.22f, 0.96f)); // water
            UI.Set(panel.rectTransform, V(1, 0), V(1, 0), V(1, 0), new Vector2(-14, 14), new Vector2(250, 250));
            UI.Border(panel, Theme.Round, Theme.Alpha(Theme.Bronze, 0.5f));
            MinimapCorners(panel.transform); // carved bronze L-brackets on each corner

            var title = UI.Label(panel.transform, "MAP", Theme.SmallSize, Theme.Bronze, TextAnchor.UpperLeft, true);
            UI.Set(title.rectTransform, V(0, 1), V(0, 1), V(0, 1), new Vector2(16, -8), new Vector2(80, 18));

            var land = UI.Swatch(panel.transform, new Color(0.21f, 0.33f, 0.19f, 1f), 0); // island
            UI.Set(land.rectTransform, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), new Vector2(0, -8), new Vector2(214, 196));
            _miniArea = land.rectTransform;

            _camMarker = UI.Swatch(_miniArea.transform, Theme.Alpha(Theme.BronzeLight, 0.35f), 18);
            _camMarker.rectTransform.anchorMin = _camMarker.rectTransform.anchorMax = V(0.5f, 0.5f);
            _camMarker.rectTransform.pivot = V(0.5f, 0.5f);
        }

        // Carved bronze L-brackets in each corner — frames the minimap like the badges.
        void MinimapCorners(Transform panel)
        {
            const float len = 26f, thick = 5f, pad = 7f;
            (Vector2 c, int sx, int sy)[] corners =
            {
                (V(0, 0), +1, +1), (V(1, 0), -1, +1), (V(0, 1), +1, -1), (V(1, 1), -1, -1),
            };
            foreach (var (c, sx, sy) in corners)
            {
                var h = UI.Swatch(panel, Theme.Bronze, 0); // horizontal arm
                UI.Set(h.rectTransform, c, c, c, new Vector2(sx * pad, sy * pad), new Vector2(len, thick));
                var v = UI.Swatch(panel, Theme.Bronze, 0); // vertical arm
                UI.Set(v.rectTransform, c, c, c, new Vector2(sx * pad, sy * pad), new Vector2(thick, len));
            }
        }

        void UpdateMinimap()
        {
            if (_miniArea == null) return;
            int i = 0;
            foreach (var f in FindObjectsByType<Faction>(FindObjectsSortMode.None))
            {
                bool isUnit = f.GetComponent<Unit>() != null;
                Color col = UnitConfig.BodyColor(f.Id); // per-empire team colour (4-faction FFA)
                PlaceBlip(GetBlip(i++), f.transform.position, isUnit ? 6f : 12f, col);
            }
            foreach (var n in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
                PlaceBlip(GetBlip(i++), n.transform.position, 5f, ResColor(n.Type));
            for (; i < _blips.Count; i++) _blips[i].gameObject.SetActive(false);

            var cam = Camera.main;
            if (cam != null) PlaceRect(_camMarker.rectTransform, CamGround(cam));
        }

        Image GetBlip(int i)
        {
            if (i < _blips.Count) return _blips[i];
            var b = UI.Swatch(_miniArea.transform, Color.white, 6);
            b.rectTransform.anchorMin = b.rectTransform.anchorMax = V(0.5f, 0.5f);
            b.rectTransform.pivot = V(0.5f, 0.5f);
            _blips.Add(b);
            return b;
        }

        void PlaceBlip(Image b, Vector3 world, float size, Color color)
        {
            b.gameObject.SetActive(true);
            b.color = color;
            b.rectTransform.sizeDelta = new Vector2(size, size);
            PlaceRect(b.rectTransform, world);
        }

        void PlaceRect(RectTransform rt, Vector3 world)
        {
            float w = _miniArea.rect.width, h = _miniArea.rect.height;
            rt.anchoredPosition = new Vector2(world.x / (WorldHalf * 2f) * w, world.z / (WorldHalf * 2f) * h);
        }

        Vector3 CamGround(Camera cam)
        {
            Ray r = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return new Plane(Vector3.up, 0f).Raycast(r, out float d) ? r.GetPoint(d) : Vector3.zero;
        }

        static Color ResColor(ResourceType t) => t switch
        {
            ResourceType.Yam => Theme.Yam,
            ResourceType.Timber => Theme.Timber,
            ResourceType.Iron => Theme.Iron,
            _ => Theme.Muted,
        };

        static Vector2 V(float a, float b) => new Vector2(a, b);

        static Color BuildingColor(BuildingKind k) => k switch
        {
            BuildingKind.House => Theme.Timber,
            BuildingKind.Barracks => Theme.Danger,
            BuildingKind.Tower => Theme.Iron,
            BuildingKind.Stable => Theme.BronzeLight,
            _ => Theme.Bronze,
        };

        static string Fmt(Cost c)
        {
            string s = "";
            if (c.Yam > 0) s += c.Yam + "Y ";
            if (c.Timber > 0) s += c.Timber + "T ";
            if (c.Iron > 0) s += c.Iron + "I";
            return s.Trim();
        }
    }
}
