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
        Badge _yam, _timber, _iron, _cowries, _knowledge, _pop, _age;
        Text _civ, _ageName; Image _crest, _crestRim;
        Button _ageBtn; Text _ageBtnLabel;
        Transform _ageCostRow; Cost _ageCostShown = new Cost(-1, -1, -1); bool _ageCostInit;
        float _incomeTimer;
        readonly List<Card> _build = new();
        Image _trainDock; Transform _trainList; Text _trainTitle;
        ProductionBuilding _shownBuilding;
        readonly List<(UnitType type, Card card)> _trainCards = new();
        GameObject _banner; Text _bannerText;
        GameObject _buildModal;   // centered build menu (opened by the + button)
        GameObject _confirmBar;   // floating ✓/✕ bar over the build ghost
        GameObject _toast; Text _toastText; float _toastTimer; int _lastAge = -1; // age-up notification

        // Singleton so SelectionManager can hand resource clicks to the bottom info panel.
        public static BrandedHud Instance { get; private set; }

        // Scoreboard: an always-on list of every empire's age + core resources. Rival rows stay masked
        // until you've scouted them (fog-gated), and fire a toast each time a discovered rival ages up.
        Transform _scorePanel;
        readonly System.Collections.Generic.Dictionary<FactionId, ScoreRow> _scoreRows = new();
        readonly System.Collections.Generic.HashSet<FactionId> _discovered = new();
        float _scoreScanTimer;
        static readonly FactionId[] Seats = { FactionId.Player, FactionId.Enemy, FactionId.Faction3, FactionId.Faction4 };
        class ScoreRow { public Image swatch; public Text name; public Text detail; public int lastAge = -1; }

        // Resource click-to-inspect bottom panel.
        GameObject _resPanel; Text _resName, _resAmt; Transform _resIcon; float _resTimer;

        RectTransform _miniArea; Image _camMarker;
        readonly List<Image> _blips = new();
        // Playable half-extent for minimap mapping. Another agent is adding World/MapBounds.cs with
        // `public static float Half`; HalfExtent() prefers it when present so the minimap stays in sync.
        // If MapBounds isn't there yet this const is the one-line swap target (keep in sync by hand).
        const float WorldHalf = 42f;

        // BUILD dock buildables — iterate the BuildingKind enum dynamically so new building types appear
        // automatically. TownCentre is included now (found extra cities, age-gated + capped per age).
        // Not cached as a field: built once in BuildBuildDock and reused via _buildKinds for refresh.
        static System.Collections.Generic.List<BuildingKind> EnumerateBuildables()
        {
            var list = new System.Collections.Generic.List<BuildingKind>();
            foreach (BuildingKind k in System.Enum.GetValues(typeof(BuildingKind)))
                list.Add(k);
            return list;
        }
        readonly System.Collections.Generic.List<BuildingKind> _buildKinds = EnumerateBuildables();

        class Card { public Button btn; public Text label; public Text cost; public Image border; public GameObject locked; }

        /// A floating bronze-rimmed resource badge with an animated count and a "+N" gather pop.
        /// `shown` ticks toward `target`; `pop` is the green delta label, fading + rising when set.
        class Badge
        {
            public RectTransform root;     // for the idle float
            public Text value;             // the big count
            public Text pop;               // "+N" income text (inside the green pill)
            public GameObject popPill;      // green income pill (shown when income > 0)
            public int gainAccum;           // gathered since the last income tick
            public float shown, target;    // animated count (resources only)
            public bool seeded;            // first reading sets the count silently
            public float phase;            // float-bob offset so badges don't bob in unison
            public float baseY;            // resting Y for the bob
        }

        GameObject _pause; // pause/settings overlay

        void Awake()
        {
            Instance = this;
            EnsureEventSystem();
            var canvas = BuildCanvas();
            BuildResourceBar(canvas);
            BuildBuildDock(canvas);
            BuildConfirmBar(canvas);
            BuildToast(canvas);
            BuildScoreboard(canvas);
            BuildResourceInfo(canvas);
            BuildMinimap(canvas);
            BuildTrainDock(canvas);
            BuildHint(canvas);
            BuildPause(canvas);
            BuildBanner(canvas);

            // ───────────────────────── WAVE-2 SEAMS (other agents wire these in a follow-up pass) ─────
            // These features' gameplay APIs do not exist yet — placeholders only, intentionally unbuilt:
            //
            //  (a) UPGRADE BUTTON — when a building is selected (see RefreshTrain's `pb`), show an
            //      "Upgrade" action in/below the TRAIN dock that calls the building's upgrade API.
            //  (b) WALL-MODE ✓/✕ BAR — see the hook left in BuildBuildDock(); a confirm/cancel bar
            //      shown while wall-draw mode is active.
            //  (c) UNIVERSITY RESEARCH PANEL — when a University is selected, swap the TRAIN dock for a
            //      research panel listing techs (cost + progress). Hook: branch in RefreshTrain on the
            //      selected building kind once a Research API exists.
            //  (d) MINIMAP FOG TINT — overlay a fog texture/tint on _miniArea driven by explored state.
            //      Hook: see UpdateMinimap(); add a fog Image child of _miniArea and tint per fog grid.
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
            // Smaller reference than the screen → the whole HUD (panels AND text together) scales up
            // ~1.35x so in-game text is actually legible (was 1920x1080 = everything too small).
            s.referenceResolution = new Vector2(1420, 800);
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

            // Resource badges live in ONE bronze-bordered indigo panel pinned to the TOP-CENTRE, exactly
            // like the Figma InGameHUD. Each badge is a 64px disc (icon above count) with a caption under it.
            const int n = 7;
            const float bw = 70f, gap = 6f;
            float cluster = n * bw + (n - 1) * gap;
            float panelW = cluster + 28f;

            var panel = UI.Panel(root, Theme.Round, Theme.Alpha(Theme.Panel, 0.92f));
            UI.Shine(panel, Theme.Alpha(Theme.BronzeLight, 0.10f));
            UI.Border(panel, Theme.Round, Theme.Alpha(Theme.BronzeDeep, 0.95f));
            UI.Shadow(panel, Theme.Alpha(Theme.Night, 0.6f), new Vector2(0f, -3f));
            UI.Set(panel.rectTransform, V(.5f, 1), V(.5f, 1), V(.5f, 1), new Vector2(0, -14), new Vector2(panelW, 104));

            float x = -(cluster - bw) * 0.5f; // centre the row of badges in the panel
            _yam       = MakeBadge(panel.transform, "YAM",     x, t => Glyph.Resource(t, ResourceType.Yam, 28f),    true); x += bw + gap;
            _timber    = MakeBadge(panel.transform, "TIMBER",  x, t => Glyph.Resource(t, ResourceType.Timber, 28f), true); x += bw + gap;
            _iron      = MakeBadge(panel.transform, "IRON",    x, t => Glyph.Resource(t, ResourceType.Iron, 28f),   true); x += bw + gap;
            _cowries   = MakeBadge(panel.transform, "COWRIES", x, t => Glyph.Resource(t, ResourceType.Cowries, 28f),   true); x += bw + gap;
            _knowledge = MakeBadge(panel.transform, "WISDOM",  x, t => Glyph.Resource(t, ResourceType.Knowledge, 28f), true); x += bw + gap;
            _pop       = MakeBadge(panel.transform, "POP",     x, t => Glyph.Pop(t, 28f),  false); x += bw + gap;
            _age       = MakeBadge(panel.transform, "AGE",     x, t => Glyph.Age(t, 26f),  false);

            // Advance Age — a prominent gold (Primary) button with the next-age cost shown as chips beneath.
            (_ageBtn, _ageBtnLabel) = UI.Button(root, UI.Track("▲ ADVANCE AGE"), () => Ages.TryAdvance(FactionId.Player));
            UI.Set(_ageBtn.GetComponent<RectTransform>(), V(1, 1), V(1, 1), V(1, 1),
                   new Vector2(-18, -22), new Vector2(232, 52));
            UI.Variant(_ageBtn, _ageBtnLabel, UI.BtnKind.Primary, track: false);

            // cost-chip row, right-aligned just under the button
            var chipRow = new GameObject("AgeCost", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            chipRow.transform.SetParent(root, false);
            UI.Set((RectTransform)chipRow.transform, V(1, 1), V(1, 1), V(1, 1), new Vector2(-18, -80), new Vector2(232, 22));
            var hl = chipRow.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 5; hl.childAlignment = TextAnchor.MiddleRight;
            hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = false;
            _ageCostRow = chipRow.transform;
        }

        // The player's empire crest: a bronze-rimmed disc tinted with the empire colour,
        // a small bronze diamond device, and the empire name.
        void BuildCrest(Transform root)
        {
            // Figma InGameHUD: a bronze-bordered indigo panel at top-left holding the empire crest,
            // the empire name (in its team colour) and the current age name.
            var panel = UI.Panel(root, Theme.Round, Theme.Alpha(Theme.Panel, 0.92f));
            UI.Shine(panel, Theme.Alpha(Theme.BronzeLight, 0.10f));
            UI.Border(panel, Theme.Round, Theme.Alpha(Theme.BronzeDeep, 0.95f));
            UI.Shadow(panel, Theme.Alpha(Theme.Night, 0.6f), new Vector2(0f, -3f));
            UI.Set(panel.rectTransform, V(0, 1), V(0, 1), V(0, 1), new Vector2(14, -14), new Vector2(290, 76));

            _crest = UI.Image(panel.transform, Theme.Disc, Theme.Benin);
            UI.Set(_crest.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(44, 0), new Vector2(56, 56));
            _crestRim = UI.Image(_crest.transform, Theme.Ring, Theme.Bronze);
            UI.Stretch(_crestRim.rectTransform, -3, -3, -3, -3);

            var device = UI.Swatch(_crest.transform, Theme.Alpha(Theme.Ivory, 0.92f), 18);
            Center(device.rectTransform, V(.5f, .5f));
            device.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);

            _civ = UI.Label(panel.transform, "Kingdom of Benin", Theme.BodySize, Theme.Benin, TextAnchor.LowerLeft, true, Theme.Display);
            UI.Shadow(_civ, Theme.Alpha(Theme.Night, 0.7f), new Vector2(1f, -1f));
            UI.Set(_civ.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(84, 11), new Vector2(196, 22));
            _ageName = UI.Label(panel.transform, "Stone Age", Theme.SmallSize, Theme.Muted, TextAnchor.UpperLeft, false, Theme.Display);
            UI.Set(_ageName.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(86, -11), new Vector2(196, 18));
        }

        Badge MakeBadge(Transform panel, string caption, float x, System.Action<Transform> drawIcon, bool income)
        {
            // Compact Figma badge: a 64px disc (icon above count) with a caption beneath. Resource
            // badges also carry a green "+N" income pill at the top-right. `x` positions it about centre.
            const float baseY = 4f;
            var go = new GameObject("Badge", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = V(.5f, .5f);
            rt.pivot = V(.5f, .5f);
            rt.sizeDelta = new Vector2(66, 90);
            rt.anchoredPosition = new Vector2(x, baseY);

            // 64px disc body + carved bronze rim, pinned to the top of the container.
            var disc = UI.Image(go.transform, Theme.Disc, Theme.PanelHi);
            disc.rectTransform.anchorMin = disc.rectTransform.anchorMax = V(.5f, 1);
            disc.rectTransform.pivot = V(.5f, 1);
            disc.rectTransform.anchoredPosition = Vector2.zero;
            disc.rectTransform.sizeDelta = new Vector2(64, 64);
            UI.Shadow(disc, Theme.Alpha(Theme.Night, 0.6f), new Vector2(0f, -3f));
            var rim = UI.Image(disc.transform, Theme.Ring, Theme.Bronze);
            UI.Stretch(rim.rectTransform, -3, -3, -3, -3);

            // glyph in the upper half of the disc
            var iconHolder = new GameObject("Icon", typeof(RectTransform));
            iconHolder.transform.SetParent(disc.transform, false);
            Center((RectTransform)iconHolder.transform, V(.5f, .64f));
            drawIcon(iconHolder.transform);

            // count in the lower half of the disc (bold ivory, tabular)
            var val = UI.Label(disc.transform, "0", 18, Theme.Ivory, TextAnchor.MiddleCenter, true);
            Center(val.rectTransform, V(.5f, .28f)); val.rectTransform.sizeDelta = new Vector2(60, 20);

            // caption beneath the disc (wide-tracked Cinzel, muted gold)
            var cap = UI.Label(go.transform, caption, 12, Theme.Alpha(Theme.Muted, 0.95f), TextAnchor.UpperCenter, true, Theme.Display);
            cap.rectTransform.anchorMin = cap.rectTransform.anchorMax = V(.5f, 0);
            cap.rectTransform.pivot = V(.5f, 0);
            cap.rectTransform.anchoredPosition = new Vector2(0, 2);
            cap.rectTransform.sizeDelta = new Vector2(70, 14);

            // green income pill at the disc's top-right (resources only), hidden until income > 0
            GameObject pill = null; Text pop = null;
            if (income)
            {
                var pillImg = UI.Image(go.transform, Theme.RoundSoft, Theme.Alpha(Theme.ConfirmDeep, 0.96f));
                pillImg.type = UnityEngine.UI.Image.Type.Sliced;
                pillImg.rectTransform.anchorMin = pillImg.rectTransform.anchorMax = V(.5f, 1);
                pillImg.rectTransform.pivot = V(.5f, 1);
                pillImg.rectTransform.anchoredPosition = new Vector2(24, 4);
                pillImg.rectTransform.sizeDelta = new Vector2(34, 16);
                UI.Border(pillImg, Theme.RoundSoft, Theme.Alpha(Theme.Confirm, 0.8f));
                pop = UI.Label(pillImg.transform, "", 13, Theme.Hex2(0x8FE87A), TextAnchor.MiddleCenter, true);
                UI.Stretch(pop.rectTransform, 2, 0, 2, 0);
                pill = pillImg.gameObject;
                pill.SetActive(false);
            }

            return new Badge { root = rt, value = val, pop = pop, popPill = pill, phase = x * 0.013f, baseY = baseY };
        }

        static void Center(RectTransform rt, Vector2 at)
        {
            rt.anchorMin = rt.anchorMax = at; rt.pivot = V(.5f, .5f); rt.anchoredPosition = Vector2.zero;
        }

        // Full empire names + age names mirror the Figma constants.ts (EMPIRES + AGES).
        static readonly string[] AgeNames = { "Stone Age", "Iron Age", "Bronze Age", "Golden Age", "Modern Age" };
        static string AgeName(int age) => (age >= 1 && age <= AgeNames.Length) ? AgeNames[age - 1] : $"Age {age}";
        static string FullName(Civ c) => c switch
        {
            Civ.Benin => "Kingdom of Benin",
            Civ.Oyo => "Oyo Empire",
            Civ.Sokoto => "Sokoto Caliphate",
            Civ.KanemBornu => "Kanem-Bornu",
            _ => c + " Empire",
        };

        // Tick a resource badge's displayed count toward its true value, accumulating how much was
        // gathered (increases only) so the income pill can show a per-second "+N" rate.
        void TickBadge(Badge b, int value, float dt)
        {
            if (!b.seeded) { b.seeded = true; b.shown = b.target = value; b.value.text = value.ToString(); return; }
            if (value > b.target) b.gainAccum += value - Mathf.RoundToInt(b.target);
            b.target = value;
            // ease the shown number toward the target (snap if very close so it lands clean)
            b.shown = Mathf.Abs(b.target - b.shown) < 0.6f ? b.target
                                                           : Mathf.Lerp(b.shown, b.target, 1f - Mathf.Exp(-12f * dt));
            b.value.text = Mathf.RoundToInt(b.shown).ToString();
        }

        // Rebuild the Advance-Age cost chips when the next-age cost changes (only on age-up — cheap).
        void RefreshAgeCost(Cost c)
        {
            if (_ageCostRow == null) return;
            if (_ageCostInit && c.Yam == _ageCostShown.Yam && c.Timber == _ageCostShown.Timber && c.Iron == _ageCostShown.Iron) return;
            _ageCostShown = c; _ageCostInit = true;
            foreach (Transform child in _ageCostRow) Destroy(child.gameObject);
            if (c.Yam > 0) UI.Chip(_ageCostRow, Theme.Yam, c.Yam.ToString());
            if (c.Timber > 0) UI.Chip(_ageCostRow, Theme.Timber, c.Timber.ToString());
            if (c.Iron > 0) UI.Chip(_ageCostRow, Theme.Iron, c.Iron.ToString());
        }

        // Once a second, roll each resource badge's accumulated gather into its green income pill.
        void UpdateIncome(Badge b)
        {
            if (b == null || b.popPill == null) return;
            if (b.gainAccum > 0) { b.pop.text = "+" + b.gainAccum; b.popPill.SetActive(true); }
            else b.popPill.SetActive(false);
            b.gainAccum = 0;
        }

        // Idle float of every badge.
        void AnimateBadges(float dt)
        {
            float t = Time.unscaledTime;
            foreach (var b in new[] { _yam, _timber, _iron, _cowries, _knowledge, _pop, _age })
            {
                if (b?.root != null)
                {
                    var p = b.root.anchoredPosition;
                    p.y = b.baseY + Mathf.Sin(t * 1.7f + b.phase * 6.283f) * 2.2f;
                    b.root.anchoredPosition = p;
                }
            }
        }

        // ---------------------------------------------------------------- build menu (+ button → modal)
        void BuildBuildDock(Transform root)
        {
            // The build menu opens from a single round "+" button at the bottom-left.
            var (plusBtn, plusLbl) = UI.Button(root, "+", ToggleBuildModal);
            UI.Variant(plusBtn, plusLbl, UI.BtnKind.Primary, track: false);
            UI.Set(plusBtn.GetComponent<RectTransform>(), V(0, 0), V(0, 0), V(0, 0), new Vector2(48, 46), new Vector2(72, 72));
            plusLbl.fontSize = 44;

            // Dim full-screen scrim; clicking it (outside the box) closes the menu.
            var scrim = UI.Panel(root, Theme.Round, Theme.Alpha(Theme.Night, 0.55f));
            UI.Stretch(scrim.rectTransform, 0, 0, 0, 0);
            var scrimBtn = scrim.gameObject.AddComponent<UnityEngine.UI.Button>();
            scrimBtn.transition = UnityEngine.UI.Selectable.Transition.None;
            scrimBtn.onClick.AddListener(() => SetBuildModal(false));
            _buildModal = scrim.gameObject;

            const int cols = 4;
            const float cell = 84f, gap = 10f, pad = 22f;
            int rows = Mathf.CeilToInt(_buildKinds.Count / (float)cols);
            float w = cols * cell + (cols - 1) * gap + pad * 2;
            float h = 52f + rows * 96f + (rows - 1) * gap + pad;

            var box = UI.Panel(scrim.transform, Theme.Round, Theme.Alpha(Theme.PanelTop, 0.98f));
            UI.Shine(box, Theme.Alpha(Theme.BronzeLight, 0.10f));
            UI.Border(box, Theme.Round, Theme.Bronze);
            UI.Corners(box);
            UI.Set(box.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), Vector2.zero, new Vector2(w, h));

            var title = UI.Header(box.transform, "BUILD");
            UI.Set(title.rectTransform, V(0, 1), V(0, 1), V(0, 1), new Vector2(24, -16), new Vector2(220, 26));

            var (closeBtn, closeLbl) = UI.Button(box.transform, "X", () => SetBuildModal(false));
            UI.Variant(closeBtn, closeLbl, UI.BtnKind.Danger, track: false);
            UI.Set(closeBtn.GetComponent<RectTransform>(), V(1, 1), V(1, 1), V(1, 1), new Vector2(-14, -14), new Vector2(40, 40));

            var gridGo = new GameObject("BuildGrid", typeof(RectTransform));
            gridGo.transform.SetParent(box.transform, false);
            UI.Stretch((RectTransform)gridGo.transform, pad, pad, pad, 50);
            var grid = gridGo.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            grid.cellSize = new Vector2(cell, 96f);
            grid.spacing = new Vector2(gap, gap);
            grid.childAlignment = TextAnchor.UpperCenter;

            foreach (var k in _buildKinds)
            {
                var kind = k;
                _build.Add(MakeTile(grid.transform, t => Glyph.Building(t, kind, 32f, Theme.Ivory),
                    () =>
                    {
                        if (BuildPlacer.Instance != null) BuildPlacer.Instance.BeginPlace(kind);
                        SetBuildModal(false); // pick → close menu → ghost appears centre-screen
                    }));
            }

            _buildModal.SetActive(false);
        }

        void ToggleBuildModal() { if (_buildModal != null) SetBuildModal(!_buildModal.activeSelf); }

        void SetBuildModal(bool on)
        {
            if (_buildModal == null) return;
            // Opening the menu cancels any in-progress placement so a fresh pick starts clean.
            if (on && BuildPlacer.Instance != null && BuildPlacer.Instance.Placing) BuildPlacer.Instance.CancelPlace();
            if (on) _buildModal.transform.SetAsLastSibling(); // draw over the docks/minimap while open
            _buildModal.SetActive(on);
        }

        // Floating ✓ / ✕ bar that hovers over the build ghost while you position a building.
        void BuildConfirmBar(Transform root)
        {
            var bar = new GameObject("ConfirmBar", typeof(RectTransform));
            bar.transform.SetParent(root, false);
            var rt = (RectTransform)bar.transform;
            rt.anchorMin = rt.anchorMax = V(0, 0); rt.pivot = V(.5f, .5f);
            rt.sizeDelta = new Vector2(150, 64);
            _confirmBar = bar;

            // Per the request: "+" confirms the build, "X" declines it.
            var (yes, yl) = UI.Button(bar.transform, "+",
                () => { if (BuildPlacer.Instance != null) BuildPlacer.Instance.Confirm(); });
            UI.Variant(yes, yl, UI.BtnKind.Confirm, track: false);
            UI.Set(yes.GetComponent<RectTransform>(), V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), new Vector2(-38, 0), new Vector2(62, 62));
            yl.fontSize = 40;

            var (no, nl) = UI.Button(bar.transform, "X",
                () => { if (BuildPlacer.Instance != null) BuildPlacer.Instance.CancelPlace(); });
            UI.Variant(no, nl, UI.BtnKind.Danger, track: false);
            UI.Set(no.GetComponent<RectTransform>(), V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), new Vector2(38, 0), new Vector2(62, 62));
            nl.fontSize = 34;

            _confirmBar.SetActive(false);
        }

        // Keep the ✓/✕ bar shown + positioned over the ghost while a building is being placed.
        void UpdatePlacement()
        {
            var bp = BuildPlacer.Instance;
            bool show = bp != null && bp.Centered;
            if (_confirmBar.activeSelf != show) _confirmBar.SetActive(show);
            if (!show) return;

            var cam = Camera.main;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(bp.GhostWorld);
            var canvas = _confirmBar.GetComponentInParent<Canvas>();
            float sf = canvas != null ? canvas.scaleFactor : 1f;
            ((RectTransform)_confirmBar.transform).anchoredPosition = new Vector2(sp.x / sf, sp.y / sf + 64f);
        }

        // ---------------------------------------------------------------- age-up toast
        // A banner under the resource bar announcing a new age + what it unlocked. Auto-hides.
        void BuildToast(Transform root)
        {
            var panel = UI.Panel(root, Theme.Round, Theme.Alpha(Theme.PanelTop, 0.98f));
            UI.Shine(panel, Theme.Alpha(Theme.BronzeLight, 0.10f));
            UI.Border(panel, Theme.Round, Theme.Bronze);
            UI.Set(panel.rectTransform, V(.5f, 1), V(.5f, 1), V(.5f, 1), new Vector2(0, -132), new Vector2(660, 76));
            _toastText = UI.Label(panel.transform, "", Theme.BodySize, Theme.BronzeLight, TextAnchor.MiddleCenter, true, Theme.Display);
            _toastText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UI.Stretch(_toastText.rectTransform, 16, 8, 16, 8);
            _toast = panel.gameObject;
            _toast.SetActive(false);
        }

        void ShowAgeToast(int age) =>
            ShowToast($"▲  {AgeName(age).ToUpperInvariant()} REACHED\nUnlocked: {Unlocks(age)}");

        // Generic toast (also used for rival age-up alerts). Single-slot; the newest message wins.
        void ShowToast(string msg)
        {
            if (_toast == null) return;
            _toastText.text = msg;
            _toast.transform.SetAsLastSibling();
            _toast.SetActive(true);
            _toastTimer = 6f;
        }

        // Everything whose AgeRequired equals this age — i.e. what advancing to it just made available.
        static string Unlocks(int age)
        {
            var items = new System.Collections.Generic.List<string>();
            foreach (UnitType u in System.Enum.GetValues(typeof(UnitType)))
                if (UnitConfig.AgeRequired(u) == age) items.Add(u.ToString());
            foreach (BuildingKind b in System.Enum.GetValues(typeof(BuildingKind)))
                if (BuildingConfig.AgeRequired(b) == age) items.Add(b.ToString());
            return items.Count > 0 ? string.Join(", ", items) : "grander buildings & a stronger empire";
        }

        // ---------------------------------------------------------------- scoreboard (all empires)
        // An always-on panel on the left listing every empire's age + core resources. A rival's row stays
        // masked ("Unknown Empire") until you've scouted them; once known it shows live data.
        void BuildScoreboard(Transform root)
        {
            var panel = UI.Panel(root, Theme.Round, Theme.Alpha(Theme.Panel, 0.92f));
            UI.Shine(panel, Theme.Alpha(Theme.BronzeLight, 0.10f));
            UI.Border(panel, Theme.Round, Theme.Alpha(Theme.BronzeDeep, 0.95f));
            UI.Shadow(panel, Theme.Alpha(Theme.Night, 0.6f), new Vector2(0f, -3f));
            UI.Set(panel.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(14, 24), new Vector2(220, 200));

            var title = UI.Header(panel.transform, "EMPIRES");
            UI.Set(title.rectTransform, V(0, 1), V(0, 1), V(0, 1), new Vector2(16, -10), new Vector2(180, 22));
            _scorePanel = panel.transform;
        }

        ScoreRow MakeScoreRow(int index)
        {
            const float top = 40f, rowH = 38f;
            var rowGo = new GameObject("Row", typeof(RectTransform));
            rowGo.transform.SetParent(_scorePanel, false);
            UI.Set((RectTransform)rowGo.transform, V(0, 1), V(0, 1), V(0, 1),
                   new Vector2(12, -(top + index * rowH)), new Vector2(196, rowH));

            var sw = UI.Image(rowGo.transform, Theme.Disc, Color.white);
            UI.Set(sw.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(9, 0), new Vector2(15, 15));

            var name = UI.Label(rowGo.transform, "", 14, Theme.Ivory, TextAnchor.LowerLeft, true, Theme.Display);
            UI.Set(name.rectTransform, V(0, 1), V(0, 1), V(0, 1), new Vector2(30, -2), new Vector2(164, 18));

            var detail = UI.Label(rowGo.transform, "", 12, Theme.Muted, TextAnchor.UpperLeft, false);
            UI.Set(detail.rectTransform, V(0, 0), V(0, 0), V(0, 0), new Vector2(30, 1), new Vector2(164, 16));

            return new ScoreRow { swatch = sw, name = name, detail = detail };
        }

        void RefreshScoreboard(float dt)
        {
            if (_scorePanel == null) return;
            _scoreScanTimer -= dt;
            bool scan = _scoreScanTimer <= 0f;
            if (scan) _scoreScanTimer = 0.5f; // throttle the (cheap but not free) discovery scan

            int index = 0;
            foreach (var id in Seats)
            {
                var e = Match.Econ(id);
                if (e == null) continue; // seat not in this match
                if (!_scoreRows.TryGetValue(id, out var row)) { row = MakeScoreRow(index); _scoreRows[id] = row; }
                index++;

                bool known = id == FactionId.Player || IsDiscovered(id, scan);
                if (known)
                {
                    Color col = UnitConfig.BodyColor(id);
                    row.swatch.color = col;
                    row.name.text = FullName(e.Civ);
                    row.name.color = col;
                    row.detail.text = $"{AgeName(e.Age)}  ·  Y{e.Yam} T{e.Timber} I{e.Iron}";
                    row.detail.color = Theme.Muted;

                    if (id != FactionId.Player)
                    {
                        if (row.lastAge < 0) row.lastAge = e.Age;
                        else if (e.Age > row.lastAge) { ShowToast($"{FullName(e.Civ)} reached the {AgeName(e.Age)}"); row.lastAge = e.Age; }
                    }
                }
                else
                {
                    row.swatch.color = Theme.Alpha(Theme.Faint, 0.85f);
                    row.name.text = "Unknown Empire";
                    row.name.color = Theme.Muted;
                    row.detail.text = "not yet scouted";
                    row.detail.color = Theme.Alpha(Theme.Faint, 0.9f);
                    row.lastAge = -1; // so the first age seen after discovery doesn't fire a stale toast
                }
            }
        }

        // An empire becomes "discovered" the first time any of its units is in your vision or any of its
        // buildings sits on an explored tile (mirrors the minimap's reveal rule). Sticky once true.
        bool IsDiscovered(FactionId id, bool doScan)
        {
            if (_discovered.Contains(id)) return true;
            if (!doScan) return false;
            var fog = FogOfWar.Instance;
            if (fog == null) { _discovered.Add(id); return true; }
            foreach (var f in Faction.All)
            {
                if (f == null || f.Id != id) continue;
                bool isUnit = f.GetComponent<Unit>() != null;
                bool seen = isUnit ? fog.IsVisible(f.transform.position) : fog.IsExplored(f.transform.position);
                if (seen) { _discovered.Add(id); return true; }
            }
            return false;
        }

        // ---------------------------------------------------------------- resource click-to-inspect
        // A themed panel that drops in at the bottom-centre when you tap a resource: its icon, name and
        // amount left — paired with the pulsing highlight ring on the node (ResourceHighlight).
        void BuildResourceInfo(Transform root)
        {
            var panel = UI.Panel(root, Theme.Round, Theme.Alpha(Theme.PanelTop, 0.98f));
            UI.Shine(panel, Theme.Alpha(Theme.BronzeLight, 0.10f));
            UI.Border(panel, Theme.Round, Theme.Bronze);
            UI.Corners(panel);
            UI.Set(panel.rectTransform, V(.5f, 0), V(.5f, 0), V(.5f, 0), new Vector2(0, 80), new Vector2(360, 92));

            var disc = UI.Image(panel.transform, Theme.Disc, Theme.PanelHi);
            UI.Set(disc.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(52, 0), new Vector2(60, 60));
            var rim = UI.Image(disc.transform, Theme.Ring, Theme.Bronze);
            UI.Stretch(rim.rectTransform, -3, -3, -3, -3);
            var iconHolder = new GameObject("Icon", typeof(RectTransform));
            iconHolder.transform.SetParent(disc.transform, false);
            Center((RectTransform)iconHolder.transform, V(.5f, .5f));
            _resIcon = iconHolder.transform;

            _resName = UI.Label(panel.transform, "", 22, Theme.Ivory, TextAnchor.LowerLeft, true, Theme.Display);
            UI.Set(_resName.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(94, 9), new Vector2(248, 26));
            _resAmt = UI.Label(panel.transform, "", 15, Theme.Muted, TextAnchor.UpperLeft, false);
            UI.Set(_resAmt.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(96, -12), new Vector2(248, 20));

            _resPanel = panel.gameObject;
            _resPanel.SetActive(false);
        }

        /// Show the bottom resource panel + node highlight (called by SelectionManager on a resource tap).
        public void ShowResourceInfo(ResourceNode node)
        {
            if (node == null || _resPanel == null) return;
            foreach (Transform c in _resIcon) Destroy(c.gameObject); // clear the previous glyph
            Glyph.Resource(_resIcon, node.Type, 30f);
            _resName.text = string.IsNullOrEmpty(node.DisplayName) ? node.Type.ToString() : node.DisplayName;
            _resName.color = ResColor(node.Type);
            _resAmt.text = node.Amount == int.MaxValue ? "Renewable workplace" : $"{node.Amount} left  ·  {node.Type}";
            _resPanel.transform.SetAsLastSibling();
            _resPanel.SetActive(true);
            _resTimer = 4.5f;
            ResourceHighlight.Show(node);
        }

        // ---------------------------------------------------------------- train dock
        void BuildTrainDock(Transform root)
        {
            _trainDock = UI.Panel(root, Theme.Round, Theme.Alpha(Theme.PanelTop, 0.97f));
            UI.Shine(_trainDock, Theme.Alpha(Theme.BronzeLight, 0.10f));
            UI.Corners(_trainDock);
            // sits above the minimap (which occupies the bottom-right corner)
            UI.Set(_trainDock.rectTransform, V(1, 0), V(1, 0), V(1, 0), new Vector2(-14, 276), new Vector2(330, 162));
            UI.Border(_trainDock, Theme.Round, Theme.Alpha(Theme.Bronze, 0.5f));

            _trainTitle = UI.Header(_trainDock.transform, "TRAIN");
            UI.Set(_trainTitle.rectTransform, V(0, 1), V(0, 1), V(0, 1), new Vector2(18, -14), new Vector2(280, 24));

            var gridGo = new GameObject("TrainGrid", typeof(RectTransform));
            gridGo.transform.SetParent(_trainDock.transform, false);
            UI.Stretch((RectTransform)gridGo.transform, 14, 14, 14, 44);
            var grid = gridGo.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            grid.cellSize = new Vector2(72f, 88f);
            grid.spacing = new Vector2(6f, 6f);
            grid.childAlignment = TextAnchor.UpperLeft;
            _trainList = gridGo.transform;

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
                    _trainCards.Add((type, MakeTile(_trainList, t => Glyph.Unit(t, type, 30f, Theme.Ivory),
                        () => pb.Train(type))));
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

        // ---------------------------------------------------------------- shared tile (Figma BuildingCard)
        // A 72x88 icon tile: a glyph on top, the name, and the cost — on an indigo fill with a rounded
        // border tinted by state (bronze affordable / red unaffordable / grey locked). `drawIcon` paints
        // the building/unit glyph into the icon area.
        Card MakeTile(Transform parent, System.Action<Transform> drawIcon, System.Action onClick)
        {
            var (btn, _) = UI.Button(parent, "", onClick, blank: true);
            btn.image.sprite = Theme.RoundSoft;
            btn.image.color = Theme.PanelTop; // indigo gradient-ish fill (shine added by UI.Button)

            // rounded outline border (recoloured per state in StyleCard)
            var border = UI.Image(btn.transform, Theme.RoundFrame, Theme.Alpha(Theme.Bronze, 0.5f));
            border.type = UnityEngine.UI.Image.Type.Sliced;
            UI.Stretch(border.rectTransform, 0, 0, 0, 0);

            // glyph, pinned near the top
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(btn.transform, false);
            var irt = (RectTransform)iconGo.transform;
            irt.anchorMin = irt.anchorMax = V(.5f, 1f); irt.pivot = V(.5f, 1f);
            irt.anchoredPosition = new Vector2(0, -8); irt.sizeDelta = new Vector2(32, 32);
            drawIcon(iconGo.transform);

            var name = UI.Label(btn.transform, "", 12, Theme.Ivory, TextAnchor.UpperCenter, true, Theme.Display);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            UI.Set(name.rectTransform, V(.5f, 1), V(.5f, 1), V(.5f, 1), new Vector2(0, -44), new Vector2(70, 24));

            var cost = UI.Label(btn.transform, "", 13, Theme.Confirm, TextAnchor.LowerCenter, true);
            UI.Set(cost.rectTransform, V(.5f, 0), V(.5f, 0), V(.5f, 0), new Vector2(0, 6), new Vector2(70, 14));

            // dim scrim shown when the tile is age-locked
            var lockScrim = UI.Image(btn.transform, Theme.RoundSoft, Theme.Alpha(Theme.Night, 0.5f));
            lockScrim.type = UnityEngine.UI.Image.Type.Sliced;
            UI.Stretch(lockScrim.rectTransform, 2, 2, 2, 2);
            lockScrim.gameObject.SetActive(false);

            return new Card { btn = btn, label = name, cost = cost, border = border, locked = lockScrim.gameObject };
        }

        void StyleCard(Card card, bool ok, bool ageOk, Cost c, Economy e)
        {
            card.btn.interactable = ok;
            bool afford = ageOk && e.CanAfford(c);
            card.border.color = !ageOk ? Theme.Alpha(Theme.Faint, 0.5f)
                                       : (afford ? Theme.Alpha(Theme.Bronze, 0.85f) : Theme.Alpha(Theme.Danger, 0.7f));
            card.label.color = ageOk ? Theme.Ivory : Theme.Faint;
            card.cost.color = !ageOk ? Theme.Muted : (afford ? Theme.Confirm : Theme.Danger);
            if (card.locked != null) card.locked.SetActive(!ageOk);
        }

        // ---------------------------------------------------------------- hint + banner
        // Pause / settings overlay (hidden until the ❚❚ button); Resume + Quit-to-Menu.
        void BuildPause(Transform root)
        {
            var scrim = UI.Panel(root, Theme.Round, Theme.Alpha(Theme.Night, 0.9f));
            UI.Stretch(scrim.rectTransform, 0, 0, 0, 0);
            _pause = scrim.gameObject;

            var box = UI.Panel(scrim.transform, Theme.Round, Theme.Alpha(Theme.Panel, 0.98f));
            UI.Set(box.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), Vector2.zero, new Vector2(420, 280));
            UI.Border(box, Theme.Round, Theme.Bronze);
            var col = UI.Col(box.transform, 12, new RectOffset(28, 28, 26, 26));

            var title = UI.Label(col.transform, "PAUSED", 40, Theme.Bronze, TextAnchor.MiddleCenter, true, Theme.Display);
            UI.LayoutHeight(title.gameObject, 60);
            var (resume, _) = UI.Button(col.transform, "Resume", () => SetPaused(false));
            UI.LayoutHeight(resume.gameObject, 52);
            var (toMenu, _) = UI.Button(col.transform, "Quit to Menu",
                () => { Time.timeScale = 1f; UnityEngine.SceneManagement.SceneManager.LoadScene("Menu"); });
            UI.LayoutHeight(toMenu.gameObject, 52);

            _pause.SetActive(false);

            // Small pause toggle button, just left of the Advance-Age button.
            var (pauseBtn, _) = UI.Button(root, "II", () => SetPaused(true));
            UI.Set(pauseBtn.GetComponent<RectTransform>(), V(1, 1), V(1, 1), V(1, 1),
                   new Vector2(-282, -22), new Vector2(48, 48));
            pauseBtn.image.sprite = Theme.RoundSoft; pauseBtn.image.color = Theme.PanelHi;
        }

        void SetPaused(bool paused)
        {
            if (_pause) _pause.SetActive(paused);
            Time.timeScale = paused ? 0f : 1f;
        }

        void BuildHint(Transform root)
        {
            var t = UI.Label(root, "Drag-select units   ·   Right-click: move / gather / attack   ·   WASD pan · scroll zoom · Space: go to base",
                             Theme.SmallSize, Theme.Alpha(Theme.Muted, 0.9f), TextAnchor.LowerCenter);
            UI.Set(t.rectTransform, V(.5f, 0), V(.5f, 0), V(.5f, 0), new Vector2(0, 12), new Vector2(940, 26));
        }

        // Figma VictoryDefeat: full-screen scrim + a bronze-framed summary panel — twin empire crests,
        // a big tracked title + subtitle, a MATCH SUMMARY stat table, and Rematch / Main Menu actions.
        Image _bannerCrestL, _bannerCrestR; Text _bannerSub;
        Text _stDuration, _stAge, _stPop, _stYam, _stTimber, _stIron;
        float _matchStart; int _peakPop;
        void BuildBanner(Transform root)
        {
            _matchStart = Time.time;

            var scrim = UI.Panel(root, Theme.Round, Theme.Alpha(Theme.Night, 0.82f));
            _banner = scrim.gameObject;
            UI.Stretch(scrim.rectTransform, 0, 0, 0, 0);

            var p = UI.Panel(scrim.transform, Theme.Round, Theme.Alpha(Theme.PanelTop, 0.98f));
            UI.Set(p.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), Vector2.zero, new Vector2(680, 480));
            UI.Shine(p, Theme.Alpha(Theme.BronzeLight, 0.10f));
            UI.Border(p, Theme.Round, Theme.Bronze);
            UI.Corners(p, 22);

            _bannerCrestL = Brand.Crest(p.transform, Theme.Sokoto, 60, V(.5f, 1), new Vector2(-150, -54));
            _bannerCrestR = Brand.Crest(p.transform, Theme.Sokoto, 60, V(.5f, 1), new Vector2(150, -54));

            _bannerText = UI.Label(p.transform, UI.Track("VICTORY"), 52, Theme.Confirm, TextAnchor.MiddleCenter, true, Theme.Display);
            UI.Shadow(_bannerText, Theme.Alpha(Theme.Night, 0.8f), new Vector2(2f, -2f));
            UI.Set(_bannerText.rectTransform, V(.5f, 1), V(.5f, 1), V(.5f, 1), new Vector2(0, -52), new Vector2(420, 64));

            _bannerSub = UI.Label(p.transform, UI.Track("THE EMPIRE HAS RISEN SUPREME"), Theme.SmallSize, Theme.Muted, TextAnchor.MiddleCenter, true, Theme.Display);
            UI.Set(_bannerSub.rectTransform, V(.5f, 1), V(.5f, 1), V(.5f, 1), new Vector2(0, -110), new Vector2(620, 22));

            // ---- MATCH SUMMARY stat table (two columns) ----
            var sumHdr = UI.Label(p.transform, UI.Track("MATCH SUMMARY"), 14, Theme.Alpha(Theme.BronzeLight, 0.9f), TextAnchor.MiddleCenter, true, Theme.Display);
            UI.Set(sumHdr.rectTransform, V(.5f, 1), V(.5f, 1), V(.5f, 1), new Vector2(0, -148), new Vector2(620, 18));
            var rule = UI.Swatch(p.transform, Theme.Alpha(Theme.Bronze, 0.45f), 0);
            UI.Set(rule.rectTransform, V(.5f, 1), V(.5f, 1), V(.5f, 1), new Vector2(0, -166), new Vector2(560, 1));

            var left = StatCol(p.transform, new Vector2(-150, -176));
            var right = StatCol(p.transform, new Vector2(150, -176));
            _stAge      = StatRow(left,  "Age Reached");
            _stPop      = StatRow(left,  "Peak Population");
            _stDuration = StatRow(left,  "War Duration");
            _stYam      = StatRow(right, "Yam");
            _stTimber   = StatRow(right, "Timber");
            _stIron     = StatRow(right, "Iron");

            // ---- actions ----
            var (rematch, rl) = UI.Button(p.transform, "↻  REMATCH",
                () => { Time.timeScale = 1f; UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name); });
            UI.Variant(rematch, rl, UI.BtnKind.Primary);
            UI.Set(rematch.GetComponent<RectTransform>(), V(.5f, 0), V(.5f, 0), V(.5f, 0), new Vector2(-110, 34), new Vector2(200, 54));
            var (menu, ml) = UI.Button(p.transform, "⌂  MAIN MENU",
                () => { Time.timeScale = 1f; UnityEngine.SceneManagement.SceneManager.LoadScene("Menu"); });
            UI.Variant(menu, ml, UI.BtnKind.Secondary);
            UI.Set(menu.GetComponent<RectTransform>(), V(.5f, 0), V(.5f, 0), V(.5f, 0), new Vector2(110, 34), new Vector2(200, 54));

            _banner.SetActive(false);
        }

        // A vertical container for stat rows, anchored at (offset) from the panel's top-centre.
        Transform StatCol(Transform parent, Vector2 offset)
        {
            var go = new GameObject("StatCol", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            UI.Set((RectTransform)go.transform, V(.5f, 1), V(.5f, 1), V(.5f, 1), offset, new Vector2(260, 120));
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = 6; v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return go.transform;
        }

        // A "Label .......... value" row; returns the value Text to fill in on match end.
        Text StatRow(Transform col, string label)
        {
            var row = new GameObject("Stat", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(col, false);
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = true; h.spacing = 6;
            UI.LayoutHeight(row, 24);
            UI.Label(row.transform, label, Theme.SmallSize, Theme.Muted, TextAnchor.MiddleLeft, false, Theme.Display);
            return UI.Label(row.transform, "—", Theme.BodySize, Theme.Ivory, TextAnchor.MiddleRight, true);
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
                TickBadge(_cowries, e.Cowries, dt);
                TickBadge(_knowledge, e.Knowledge, dt);
                _pop.value.text = $"{e.PopUsed}/{e.PopCap}";
                _age.value.text = e.Age.ToString();
                if (e.PopUsed > _peakPop) _peakPop = e.PopUsed;

                // Announce a new age (skip the very first reading so it doesn't fire at match start).
                if (_lastAge < 0) _lastAge = e.Age;
                else if (e.Age > _lastAge) { ShowAgeToast(e.Age); _lastAge = e.Age; }

                Color civ = UnitConfig.CivColor(e.Civ);
                _civ.text = FullName(e.Civ);
                _civ.color = civ;
                _ageName.text = AgeName(e.Age);
                _crest.color = civ;
                _crestRim.color = Color.Lerp(Theme.Bronze, civ, 0.25f);

                AnimateBadges(dt);

                _incomeTimer += dt;
                if (_incomeTimer >= 1f)
                {
                    _incomeTimer = 0f;
                    UpdateIncome(_yam); UpdateIncome(_timber); UpdateIncome(_iron);
                    UpdateIncome(_cowries); UpdateIncome(_knowledge);
                }

                if (e.Age < Ages.Max)
                {
                    Cost c = Ages.CostFor(e.Age + 1);
                    _ageBtn.interactable = e.CanAfford(c);
                    _ageBtnLabel.text = UI.Track("▲ ADVANCE AGE");
                    RefreshAgeCost(c);
                }
                else
                {
                    _ageBtnLabel.text = UI.Track(AgeName(e.Age).ToUpperInvariant()); // max age reached
                    _ageBtn.interactable = false;
                    RefreshAgeCost(new Cost(0, 0, 0));
                }

                for (int i = 0; i < _buildKinds.Count; i++)
                {
                    var k = _buildKinds[i];
                    var card = _build[i];
                    bool ageOk = e.Age >= BuildingConfig.AgeRequired(k);
                    Cost c = BuildingConfig.CostOf(k, e.Civ);

                    // Town Centres found new cities — capped per age (one more city each age advanced).
                    bool capOk = true;
                    if (k == BuildingKind.TownCentre)
                    {
                        int have = Match.TownCentreCount(FactionId.Player);
                        int max = BuildingConfig.MaxTownCentres(e.Age);
                        capOk = have < max;
                        card.label.text = "New City";
                        card.cost.text = !ageOk ? $"Age {BuildingConfig.AgeRequired(k)}"
                                       : capOk ? Fmt(c) : $"Limit {have}/{max}";
                    }
                    else
                    {
                        card.label.text = k.ToString();
                        card.cost.text = ageOk ? Fmt(c) : $"Age {BuildingConfig.AgeRequired(k)}";
                    }

                    StyleCard(card, ageOk && capOk && e.CanAfford(c), ageOk, c, e);
                }
            }

            RefreshTrain(SelectionManager.Instance != null ? SelectionManager.Instance.SelectedBuilding : null, e);
            UpdateMinimap();
            UpdatePlacement();

            if (_toast != null && _toast.activeSelf)
            {
                _toastTimer -= Time.unscaledDeltaTime;
                if (_toastTimer <= 0f) _toast.SetActive(false);
            }

            RefreshScoreboard(Time.unscaledDeltaTime);

            if (_resPanel != null && _resPanel.activeSelf)
            {
                _resTimer -= Time.unscaledDeltaTime;
                if (_resTimer <= 0f) { _resPanel.SetActive(false); ResourceHighlight.Hide(); }
            }

            if (Match.Over && !_banner.activeSelf)
            {
                bool win = Match.Winner == FactionId.Player;
                _bannerText.text = UI.Track(win ? "VICTORY" : "DEFEAT");
                _bannerText.color = win ? Theme.Confirm : Theme.Danger;
                _bannerSub.text = UI.Track(win ? "THE EMPIRE HAS RISEN SUPREME" : "THE KINGDOM HAS FALLEN");
                _bannerSub.color = win ? Theme.Muted : Theme.Alpha(Theme.Danger, 0.85f);
                Color civ = e != null ? UnitConfig.CivColor(e.Civ) : Theme.Sokoto;
                if (_bannerCrestL != null) _bannerCrestL.color = Theme.Alpha(civ, 0.85f);
                if (_bannerCrestR != null) _bannerCrestR.color = Theme.Alpha(civ, 0.85f);

                // fill the MATCH SUMMARY
                int secs = Mathf.RoundToInt(Time.time - _matchStart);
                _stDuration.text = $"{secs / 60}m {secs % 60:00}s";
                _stAge.text = e != null ? AgeName(e.Age) : "—";
                _stPop.text = _peakPop.ToString();
                if (e != null)
                {
                    _stYam.text = e.Yam.ToString();
                    _stTimber.text = e.Timber.ToString();
                    _stIron.text = e.Iron.ToString();
                }
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
            var fog = FogOfWar.Instance;
            int i = 0;
            foreach (var f in FindObjectsByType<Faction>(FindObjectsSortMode.None))
            {
                bool isUnit = f.GetComponent<Unit>() != null;
                // Rivals are hidden on the minimap until you've scouted them: enemy units only show
                // while in your current vision; enemy buildings show once their tile has been explored.
                if (f.Id != FactionId.Player && fog != null)
                {
                    bool known = isUnit ? fog.IsVisible(f.transform.position) : fog.IsExplored(f.transform.position);
                    if (!known) continue;
                }
                Color col = UnitConfig.BodyColor(f.Id); // per-empire team colour (4-faction FFA)
                PlaceBlip(GetBlip(i++), f.transform.position, isUnit ? 6f : 12f, col);
            }
            foreach (var n in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                if (fog != null && !fog.IsExplored(n.transform.position)) continue; // undiscovered resources stay hidden
                PlaceBlip(GetBlip(i++), n.transform.position, 5f, ResColor(n.Type));
            }
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
            float half = HalfExtent();
            rt.anchoredPosition = new Vector2(world.x / (half * 2f) * w, world.z / (half * 2f) * h);
        }

        // Prefer World/MapBounds.Half if that type exists (another agent is adding it); otherwise fall
        // back to the local WorldHalf const. Resolved by reflection so this compiles with or without it.
        static float? _halfCache;
        static float HalfExtent()
        {
            if (_halfCache.HasValue) return _halfCache.Value;
            float half = WorldHalf;
            var tp = System.Type.GetType("NaijaEmpires.MapBounds");
            var f = tp?.GetField("Half", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (f != null && f.FieldType == typeof(float)) half = (float)f.GetValue(null);
            _halfCache = half;
            return half;
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
            ResourceType.Cowries => Theme.BronzeLight,
            ResourceType.Knowledge => Theme.Benin,
            _ => Theme.Muted,
        };

        static Vector2 V(float a, float b) => new Vector2(a, b);

        static string Fmt(Cost c)
        {
            string s = "";
            if (c.Yam > 0) s += c.Yam + "Y ";
            if (c.Timber > 0) s += c.Timber + "T ";
            if (c.Iron > 0) s += c.Iron + "I ";
            if (c.Cowries > 0) s += c.Cowries + "C ";
            if (c.Knowledge > 0) s += c.Knowledge + "K";
            return s.Trim();
        }
    }
}
