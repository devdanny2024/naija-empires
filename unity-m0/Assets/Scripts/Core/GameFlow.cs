using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace NaijaEmpires
{
    /// Front-end flow (uGUI, built entirely from code — same pattern as BrandedHud):
    /// Splash → Main Menu → Match Setup, then writes MatchConfig and loads the "Skirmish" scene.
    /// Landscape, ScreenSpaceOverlay canvas at 1920x1080. Drop this on one GameObject in the Menu scene.
    public class GameFlow : MonoBehaviour
    {
        /// The app logo (Assets/Art/AppIcon.png). The editor script (MenuSceneEditor) assigns this
        /// from the asset, since the PNG lives outside Resources/ and so can't be Resources.Load'd.
        /// If left null, the splash falls back to a wordmark-only treatment.
        public Texture2D Logo;

        const string MatchScene = "Skirmish";
        const float SplashSeconds = 1.5f;
        const float SplashFade = 0.4f;

        // ---- match-setup working state (committed to MatchConfig on Start) ---------------
        static readonly Civ[] Civs = { Civ.Benin, Civ.Oyo, Civ.Sokoto, Civ.KanemBornu };
        Civ _playerCiv = Civ.Benin;
        int _opponentCount = 3;                  // 1..3
        int _difficulty = 1;                     // 0 Easy, 1 Normal, 2 Hard
        // -1 == "Random" (resolved at launch); otherwise an index into Civs.
        readonly int[] _oppPick = { -1, -1, -1 };

        // ---- screens --------------------------------------------------------------------
        GameObject _splash, _menu, _setup, _lobby;
        CanvasGroup _splashGroup;
        Image _splashBar;        // loading-bar fill (animated during the splash hold)
        Text _splashHint;        // "Tap to begin" prompt, blinks once the bar is full
        float _splashT, _fadeT;
        bool _fadingOut;

        // setup widgets we restyle on selection
        readonly List<(Button btn, Civ civ)> _playerCivBtns = new();
        Text _countLabel;
        readonly List<(Button btn, int d)> _diffBtns = new();
        // one row of 5 option buttons per opponent slot (Random + 4 civs)
        readonly List<List<(Button btn, int pick)>> _oppBtns = new();
        readonly List<GameObject> _oppRows = new();

        void Awake()
        {
            EnsureEventSystem();
            EnsureCamera();
            var canvas = BuildCanvas();
            BuildSplash(canvas);
            BuildMenu(canvas);
            BuildSetup(canvas);
            BuildLobby(canvas);

            ShowOnly(_splash);
            _splashGroup.alpha = 1f;
            _splashT = 0f;
        }

        void Update()
        {
            if (_splash.activeSelf)
            {
                _splashT += Time.unscaledDeltaTime;
                bool done = _splashT >= SplashSeconds;
                bool tapped = Input.GetMouseButtonDown(0) || Input.touchCount > 0;

                // animate the loading bar across the hold; once full, blink the "tap to begin" hint.
                if (_splashBar != null)
                    _splashBar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(_splashT / SplashSeconds), 1f);
                if (_splashHint != null)
                {
                    bool full = _splashT >= SplashSeconds * 0.98f;
                    _splashHint.color = Theme.Alpha(Theme.BronzeLight,
                        full ? 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) : 0f);
                }

                if (done || _fadingOut)
                {
                    // once the hold elapses (or on tap) fade the splash out, then reveal the menu
                    if (!_fadingOut) { _fadingOut = true; _fadeT = 0f; }
                    _fadeT += Time.unscaledDeltaTime;
                    _splashGroup.alpha = Mathf.Clamp01(1f - _fadeT / SplashFade);
                    if (_splashGroup.alpha <= 0f) GoMenu();
                }
                else if (tapped)
                {
                    _fadingOut = true; _fadeT = 0f; // tap skips the remaining hold
                }
            }
        }

        // ============================================================ infra (mirrors BrandedHud)
        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // The Menu scene is pure ScreenSpaceOverlay UI — without a Camera, Unity shows the
        // "Display 1 — No cameras rendering" warning and leaves an undefined framebuffer behind the UI.
        // A plain solid-colour camera clears that to the brand night tone.
        static void EnsureCamera()
        {
            if (Camera.main != null) return;
            var cam = new GameObject("MenuCamera", typeof(Camera)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Theme.Night;
            cam.orthographic = true;
        }

        Transform BuildCanvas()
        {
            var go = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            s.matchWidthOrHeight = 1f; // match height — stable for landscape
            return go.transform;
        }

        void ShowOnly(GameObject screen)
        {
            _splash.SetActive(screen == _splash);
            _menu.SetActive(screen == _menu);
            _setup.SetActive(screen == _setup);
            _lobby.SetActive(screen == _lobby);
        }

        // ============================================================ Splash
        // Figma SplashScreen: radial backdrop + corner ornaments, glowing logo, wide-tracked Cinzel
        // wordmark, "Rise · Conquer · Reign" tagline, a bronze loading bar and a blinking enter prompt.
        void BuildSplash(Transform root)
        {
            var bg = Brand.Backdrop(root, "Splash");
            var t = bg.transform;
            _splash = bg.gameObject;
            _splashGroup = _splash.AddComponent<CanvasGroup>();
            Brand.ScreenCorners(t);

            // Logo (if the editor assigned the AppIcon texture); else a crest-style medallion.
            if (Logo != null)
            {
                var sprite = Sprite.Create(Logo, new Rect(0, 0, Logo.width, Logo.height),
                                           new Vector2(0.5f, 0.5f), 100f);
                var icon = UI.Icon(t, sprite, 200, Color.white);
                UI.Set(icon.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f),
                       new Vector2(0, 150), new Vector2(200, 200));
            }
            else
            {
                Brand.Crest(t, Theme.Sokoto, 160, V(.5f, .5f), new Vector2(0, 150));
            }

            var word = UI.Label(t, UI.Track("NAIJA EMPIRES"), 72, Theme.BronzeLight, TextAnchor.MiddleCenter, true, Theme.Display);
            UI.Shadow(word, Theme.Alpha(Theme.Bronze, 0.7f), new Vector2(0f, 0f));      // faux glow
            UI.Set(word.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f),
                   new Vector2(0, 20), new Vector2(1400, 90));

            var tagline = UI.Label(t, UI.Track("RISE  ·  CONQUER  ·  REIGN"), Theme.LabelSize,
                                   Theme.Muted, TextAnchor.MiddleCenter, false, Theme.Display);
            UI.Set(tagline.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f),
                   new Vector2(0, -40), new Vector2(1000, 30));

            // ---- loading bar (left-anchored fill grown in Update) ----
            var track = UI.Panel(t, Theme.Pill, Theme.Alpha(Color.black, 0.5f));
            UI.Set(track.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), new Vector2(0, -120), new Vector2(360, 8));
            UI.Border(track, Theme.Pill, Theme.Alpha(Theme.BronzeDeep, 0.9f));
            _splashBar = UI.Panel(track.transform, Theme.Pill, Theme.Bronze);
            _splashBar.rectTransform.anchorMin = new Vector2(0, 0);
            _splashBar.rectTransform.anchorMax = new Vector2(0, 1);
            _splashBar.rectTransform.pivot = new Vector2(0, 0.5f);
            _splashBar.rectTransform.offsetMin = Vector2.zero; _splashBar.rectTransform.offsetMax = Vector2.zero;

            _splashHint = UI.Label(t, UI.Track("◈  TAP ANYWHERE TO BEGIN  ◈"), Theme.SmallSize,
                                   Theme.Alpha(Theme.BronzeLight, 0f), TextAnchor.MiddleCenter, true, Theme.Display);
            UI.Set(_splashHint.rectTransform, V(.5f, 0), V(.5f, 0), V(.5f, 0), new Vector2(0, 60), new Vector2(900, 26));
        }

        // ============================================================ Main Menu
        // Figma MainMenu: a split layout — an ambient art half on the left (night sky, moon, fortress
        // silhouette) and a menu half on the right (logo, wordmark, divider, the variant buttons,
        // version footer). Approximated in uGUI with solid silhouettes + a bronze divider seam.
        void BuildMenu(Transform root)
        {
            var bg = Brand.Backdrop(root, "MainMenu");
            var t = bg.transform;
            _menu = bg.gameObject;

            // ---- left ambient art half ----
            var art = UI.Panel(t, Theme.Round, Theme.Hex2(0x0E1A22));
            UI.Set(art.rectTransform, V(0, 0), V(0.5f, 1), V(0, 0), Vector2.zero, Vector2.zero);
            art.rectTransform.offsetMin = Vector2.zero; art.rectTransform.offsetMax = Vector2.zero;
            // moon
            var moon = UI.Image(art.transform, Theme.Disc, Theme.Alpha(Theme.BronzeLight, 0.9f));
            UI.Set(moon.rectTransform, V(0.7f, 0.85f), V(0.7f, 0.85f), V(.5f, .5f), Vector2.zero, new Vector2(90, 90));
            // ground band + fortress block silhouette
            var ground = UI.Swatch(art.transform, Theme.Hex2(0x0A1208), 0);
            UI.Set(ground.rectTransform, V(0, 0), V(1, 0), V(0.5f, 0), Vector2.zero, new Vector2(0, 220));
            ground.rectTransform.anchorMax = new Vector2(1, 0); ground.rectTransform.offsetMin = new Vector2(0, 0);
            ground.rectTransform.offsetMax = new Vector2(0, 220);
            Silhouette(art.transform);
            // watermark
            var wm = UI.Label(art.transform, UI.Track("WEST AFRICA · AGE OF EMPIRES"), 12,
                              Theme.Alpha(Theme.Muted, 0.6f), TextAnchor.LowerLeft, true, Theme.Display);
            UI.Set(wm.rectTransform, V(0, 0), V(0, 0), V(0, 0), new Vector2(40, 36), new Vector2(600, 24));
            // bronze divider seam on the right edge of the art half
            var seam = UI.Swatch(art.transform, Theme.Bronze, 0);
            UI.Set(seam.rectTransform, V(1, 0), V(1, 1), V(1, .5f), new Vector2(-1, 0), new Vector2(3, 0));
            seam.rectTransform.offsetMin = new Vector2(-3, 0); seam.rectTransform.offsetMax = new Vector2(0, 0);

            // ---- right menu half ----
            var menuCol = new GameObject("MenuCol", typeof(RectTransform));
            menuCol.transform.SetParent(t, false);
            UI.Set((RectTransform)menuCol.transform, V(0.75f, 0.5f), V(0.75f, 0.5f), V(0.5f, 0.5f),
                   new Vector2(0, 0), new Vector2(380, 560));
            var col = UI.Col(menuCol.transform, 14, new RectOffset(0, 0, 0, 0));
            col.childAlignment = TextAnchor.UpperCenter;

            if (Logo != null)
            {
                var sprite = Sprite.Create(Logo, new Rect(0, 0, Logo.width, Logo.height), new Vector2(0.5f, 0.5f), 100f);
                var icon = UI.Icon(col.transform, sprite, 90, Color.white);
                UI.LayoutHeight(icon.gameObject, 90);
            }
            var title = UI.Label(col.transform, UI.Track("NAIJA EMPIRES"), 38, Theme.BronzeLight, TextAnchor.MiddleCenter, true, Theme.Display);
            UI.Shadow(title, Theme.Alpha(Theme.Bronze, 0.6f), Vector2.zero);
            UI.LayoutHeight(title.gameObject, 50);
            UI.Divider(col.transform, 2);
            var gap = new GameObject("Gap", typeof(RectTransform)); gap.transform.SetParent(col.transform, false);
            UI.LayoutHeight(gap, 8);

            MakeMenuButton(col.transform, "PLAY", UI.BtnKind.Primary, GoSetup);
            MakeMenuButton(col.transform, "MULTIPLAYER", UI.BtnKind.Secondary, GoLobby);
            MakeMenuButton(col.transform, "SETTINGS", UI.BtnKind.Secondary, null);
            MakeMenuButton(col.transform, "QUICK BATTLE", UI.BtnKind.Secondary, QuickBattle);
            MakeMenuButton(col.transform, "QUIT GAME", UI.BtnKind.Danger, Quit);

            var foot = UI.Label(t, "v0.9.4 Alpha   ·   © 2026 Naija Studios", Theme.SmallSize,
                                Theme.Alpha(Theme.Faint, 0.9f), TextAnchor.LowerRight);
            UI.Set(foot.rectTransform, V(1, 0), V(1, 0), V(1, 0), new Vector2(-30, 18), new Vector2(600, 24));
        }

        /// A blocky fortress + acacia-tree silhouette for the menu art half (flat dark shapes).
        void Silhouette(Transform art)
        {
            void Block(float ax, float w, float h) {
                var b = UI.Swatch(art, Theme.Hex2(0x0A1208), 0);
                UI.Set(b.rectTransform, V(ax, 0), V(ax, 0), V(0.5f, 0), new Vector2(0, 120), new Vector2(w, h));
            }
            Block(0.5f, 150, 150);  // keep
            Block(0.42f, 28, 200);  // left tower
            Block(0.58f, 28, 200);  // right tower
            Block(0.5f, 70, 230);   // central spire
            // acacia tree canopies (discs on thin trunks)
            void Tree(float ax, float trunkH, float canopy) {
                var tr = UI.Swatch(art, Theme.Hex2(0x0A0E06), 0);
                UI.Set(tr.rectTransform, V(ax, 0), V(ax, 0), V(0.5f, 0), new Vector2(0, 120), new Vector2(8, trunkH));
                var cp = UI.Image(art, Theme.Disc, Theme.Hex2(0x0D1A0A));
                UI.Set(cp.rectTransform, V(ax, 0), V(ax, 0), V(0.5f, 0), new Vector2(0, 120 + trunkH), new Vector2(canopy, canopy * 0.5f));
            }
            Tree(0.12f, 70, 80); Tree(0.22f, 50, 60); Tree(0.82f, 80, 90); Tree(0.9f, 60, 70);
        }

        void MakeMenuButton(Transform parent, string text, UI.BtnKind kind, System.Action onClick)
        {
            var (btn, label) = UI.Button(parent, text, onClick);
            UI.Variant(btn, label, kind);
            label.fontSize = Theme.LabelSize; label.font = Theme.Display;
            if (onClick == null) btn.interactable = false; // SETTINGS placeholder until wired
            UI.LayoutHeight(btn.gameObject, 60);
        }

        // ============================================================ Match Setup
        // Figma MatchSetup: a "Your Empire" panel of crest cards (name + perk + team stripe), an
        // "Opponents" panel with a count stepper + per-AI empire pickers, and Start/Back actions.
        void BuildSetup(Transform root)
        {
            var bg = Brand.Backdrop(root, "MatchSetup");
            var t = bg.transform;
            _setup = bg.gameObject;
            Brand.ScreenCorners(t);

            ScreenTitle(t, "MATCH SETUP");

            // ---- left: YOUR EMPIRE panel ----
            var (eP, eContent) = UI.TitledPanel(t, "Your Empire");
            UI.Set(eP.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), new Vector2(-330, -10), new Vector2(620, 720));
            var civRow = Row(eContent, 0);
            var hl = civRow.GetComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(16, 16, 16, 16); hl.spacing = 12;
            UI.Stretch((RectTransform)civRow, 0, 0, 0, 0);
            foreach (var c in Civs)
            {
                var civ = c;
                var (btn, _) = UI.Button(civRow, "", () => { _playerCiv = civ; RefreshSetup(); }, blank: true);
                EmpireCardContent(btn.transform, civ, big: true);
                _playerCivBtns.Add((btn, civ));
            }

            // ---- right column ----
            var (oP, oContent) = UI.TitledPanel(t, "Opponents");
            UI.Set(oP.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), new Vector2(330, 90), new Vector2(560, 520));
            var ocol = UI.Col(oContent, 10, new RectOffset(24, 24, 18, 18));

            // number-of-opponents stepper:  [−]   N   [+]
            UI.Header(ocol.transform, "Number of Opponents");
            var stepRow = Row(ocol.transform, 62);
            var (minusBtn, minusLbl) = UI.Button(stepRow, "–", () => { _opponentCount = Mathf.Max(1, _opponentCount - 1); RefreshSetup(); });
            minusLbl.fontSize = Theme.TitleSize; StepWidth(minusBtn, 62);
            _countLabel = UI.Label(stepRow, "3", 40, Theme.BronzeLight, TextAnchor.MiddleCenter, true, Theme.Display);
            UI.Shadow(_countLabel, Theme.Alpha(Theme.Bronze, 0.5f), Vector2.zero);
            var clLE = _countLabel.gameObject.AddComponent<LayoutElement>(); clLE.flexibleWidth = 1f;
            var (plusBtn, plusLbl) = UI.Button(stepRow, "+", () => { _opponentCount = Mathf.Min(3, _opponentCount + 1); RefreshSetup(); });
            plusLbl.fontSize = Theme.TitleSize; StepWidth(plusBtn, 62);

            // difficulty toggles
            UI.Header(ocol.transform, "Difficulty");
            var diffRow = Row(ocol.transform, 46);
            string[] diffs = { "EASY", "NORMAL", "HARD" };
            for (int d = 0; d < diffs.Length; d++)
            {
                int dd = d;
                var (db, dl) = UI.Button(diffRow, diffs[d], () => { _difficulty = dd; RefreshSetup(); });
                dl.fontSize = Theme.SmallSize;
                _diffBtns.Add((db, dd));
            }

            UI.Header(ocol.transform, "OPPONENT EMPIRES");
            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                var rowWrap = Row(ocol.transform, 46);
                _oppRows.Add(rowWrap.gameObject);

                var caption = UI.Label(rowWrap, $"AI {slot + 1}", Theme.SmallSize, Theme.Muted, TextAnchor.MiddleLeft, true, Theme.Display);
                var capLE = caption.gameObject.AddComponent<LayoutElement>();
                capLE.preferredWidth = 54; capLE.flexibleWidth = 0;

                var picks = new List<(Button, int)>();
                var (rndBtn, rndLabel) = UI.Button(rowWrap, "⁇ Rnd", () => { _oppPick[slot] = -1; RefreshSetup(); });
                rndLabel.fontSize = Theme.SmallSize;
                picks.Add((rndBtn, -1));
                for (int ci = 0; ci < Civs.Length; ci++)
                {
                    int pick = ci;
                    var (btn, lbl) = UI.Button(rowWrap, ShortName(Civs[ci]), () => { _oppPick[slot] = pick; RefreshSetup(); });
                    lbl.fontSize = Theme.SmallSize;
                    picks.Add((btn, pick));
                }
                _oppBtns.Add(picks);
            }

            // ---- actions panel (Start + Back) ----
            var actCol = new GameObject("Actions", typeof(RectTransform));
            actCol.transform.SetParent(t, false);
            UI.Set((RectTransform)actCol.transform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), new Vector2(330, -250), new Vector2(560, 150));
            var acol = UI.Col(actCol.transform, 12, new RectOffset(0, 0, 0, 0));
            var (startBtn, startLabel) = UI.Button(acol.transform, "⚔  START MATCH", StartMatch);
            UI.Variant(startBtn, startLabel, UI.BtnKind.Primary);
            startLabel.fontSize = Theme.LabelSize; startLabel.font = Theme.Display;
            UI.LayoutHeight(startBtn.gameObject, 66);
            var (backBtn, backLabel) = UI.Button(acol.transform, "←  BACK", GoMenu);
            UI.Variant(backBtn, backLabel, UI.BtnKind.Secondary);
            backLabel.font = Theme.Display;
            UI.LayoutHeight(backBtn.gameObject, 52);

            RefreshSetup();
        }

        /// A centred screen title with a faded bronze divider underneath (Figma screen header).
        void ScreenTitle(Transform t, string text)
        {
            var title = UI.Label(t, UI.Track(text), 40, Theme.BronzeLight, TextAnchor.MiddleCenter, true, Theme.Display);
            UI.Shadow(title, Theme.Alpha(Theme.Bronze, 0.6f), Vector2.zero);
            UI.Set(title.rectTransform, V(.5f, 1), V(.5f, 1), V(.5f, 1), new Vector2(0, -50), new Vector2(1400, 56));
            var rule = UI.Swatch(t, Theme.Alpha(Theme.Bronze, 0.6f), 0);
            UI.Set(rule.rectTransform, V(.5f, 1), V(.5f, 1), V(.5f, 1), new Vector2(0, -86), new Vector2(420, 2));
        }

        static string ShortName(Civ c) => c == Civ.KanemBornu ? "Kanem" : c.ToString();

        /// A horizontal layout row with even spacing, fixed height, transparent background.
        Transform Row(Transform parent, float height)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            h.childAlignment = TextAnchor.MiddleCenter;
            UI.LayoutHeight(go, height);
            return go.transform;
        }

        // Fix a stepper button to a square width inside a row (so the number takes the middle).
        static void StepWidth(Button b, float w)
        {
            var le = b.gameObject.GetComponent<LayoutElement>() ?? b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w; le.minWidth = w; le.flexibleWidth = 0f;
        }

        /// Figma EmpireCard: a crest disc, the empire name, the perk blurb and a team-colour stripe
        /// across the bottom, laid inside a (blank) selectable button.
        void EmpireCardContent(Transform btn, Civ civ, bool big)
        {
            Color col = UnitConfig.CivColor(civ);
            Brand.Crest(btn, col, big ? 64 : 40, V(0.5f, 1), new Vector2(0, big ? -16 : -8));

            var name = UI.Label(btn, FullName(civ), Theme.SmallSize, Theme.Ivory, TextAnchor.UpperCenter, true, Theme.Display);
            UI.Set(name.rectTransform, V(0, 1), V(1, 1), V(0.5f, 1), new Vector2(0, big ? -90 : -56), new Vector2(0, 22));
            name.horizontalOverflow = HorizontalWrapMode.Wrap;

            if (big)
            {
                var perk = UI.Label(btn, Perk(civ), 12, Theme.Muted, TextAnchor.UpperCenter);
                UI.Set(perk.rectTransform, V(0, 1), V(1, 1), V(0.5f, 1), new Vector2(0, -118), new Vector2(-16, 70));
                perk.horizontalOverflow = HorizontalWrapMode.Wrap;
                perk.verticalOverflow = VerticalWrapMode.Truncate;
            }

            // team-colour stripe pinned to the card's bottom edge
            var stripe = UI.Swatch(btn, col, 0);
            UI.Set(stripe.rectTransform, V(0, 0), V(1, 0), V(0.5f, 0), Vector2.zero, new Vector2(0, 3));
            stripe.rectTransform.offsetMin = new Vector2(0, 0); stripe.rectTransform.offsetMax = new Vector2(0, 3);
        }

        static string FullName(Civ c) => c switch
        {
            Civ.Benin => "Kingdom of Benin",
            Civ.Oyo => "Oyo Empire",
            Civ.Sokoto => "Sokoto Caliphate",
            Civ.KanemBornu => "Kanem-Bornu",
            _ => c.ToString(),
        };

        // Perk blurbs mirror the Figma constants.ts EMPIRES table (flavour only — not gameplay).
        static string Perk(Civ c) => c switch
        {
            Civ.Benin => "Bronze smiths grant +20% Yam yield per age",
            Civ.Oyo => "Cavalry units move 25% faster on open terrain",
            Civ.Sokoto => "Iron production doubled from Age III onward",
            Civ.KanemBornu => "Spearmen radiate the Shield of Kanuri aura",
            _ => "",
        };

        // ---- selection styling -----------------------------------------------------------
        void RefreshSetup()
        {
            foreach (var (btn, civ) in _playerCivBtns) StyleSelectable(btn, civ == _playerCiv);
            if (_countLabel != null) _countLabel.text = _opponentCount.ToString();
            foreach (var (btn, d) in _diffBtns) StyleSelectable(btn, d == _difficulty);

            for (int i = 0; i < _oppRows.Count; i++)
            {
                bool active = i < _opponentCount;
                _oppRows[i].SetActive(active);
                if (!active) continue;
                foreach (var (btn, pick) in _oppBtns[i]) StyleSelectable(btn, pick == _oppPick[i]);
            }
        }

        void StyleSelectable(Button btn, bool selected)
        {
            // Labelled (gradient) buttons swap their fill sprite — gold when selected, indigo otherwise —
            // so the gloss is preserved. Blank crest cards have no gradient/frame, so they just tint.
            var frame = btn.transform.Find("Frame");
            if (frame != null)
            {
                btn.image.sprite = selected ? Theme.BtnPrimary : Theme.BtnSecondary;
                btn.image.color = Color.white;
                frame.GetComponent<Image>().color = selected ? Theme.BronzeDeep : Theme.Alpha(Theme.Bronze, 0.5f);
            }
            else
            {
                btn.image.color = selected ? Theme.Bronze : Theme.PanelHi;
            }
            var label = btn.GetComponentInChildren<Text>();
            if (label != null) label.color = selected ? Theme.Night : Theme.BronzeLight;
        }

        // ============================================================ Multiplayer Lobby (stub screen)
        // Figma MultiplayerLobby: a Create/Join column (room code + actions) and a party-seat panel
        // with 4 player rows (crest, name, empire, ready badge). This is a visual stub — seats are
        // static placeholders; START runs a normal single-player skirmish for now.
        //
        // PHASE 2 HOOK (Photon): replace the static seats with live lobby state, wire Create/Join to
        // matchmaking, and on host-start build MatchConfig from the seats then LoadScene(MatchScene).
        void BuildLobby(Transform root)
        {
            var bg = Brand.Backdrop(root, "MultiplayerLobby");
            var t = bg.transform;
            _lobby = bg.gameObject;
            Brand.ScreenCorners(t);
            ScreenTitle(t, "MULTIPLAYER LOBBY");

            // ---- left: create-party column ----
            var (cP, cContent) = UI.TitledPanel(t, "Room Code");
            UI.Set(cP.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), new Vector2(-360, 70), new Vector2(420, 360));
            var ccol = UI.Col(cContent, 14, new RectOffset(28, 28, 22, 22));
            var code = UI.Label(ccol.transform, UI.Track("BENIN-7492"), Theme.TitleSize, Theme.BronzeLight, TextAnchor.MiddleCenter, true, Theme.Display);
            UI.Shadow(code, Theme.Alpha(Theme.Bronze, 0.6f), Vector2.zero);
            UI.LayoutHeight(code.gameObject, 50);
            var hint = UI.Label(ccol.transform, "Share this code with friends", Theme.SmallSize, Theme.Muted, TextAnchor.MiddleCenter);
            UI.LayoutHeight(hint.gameObject, 22);
            var (copyBtn, copyLabel) = UI.Button(ccol.transform, "COPY CODE", () => GUIUtility.systemCopyBuffer = "BENIN-7492");
            UI.Variant(copyBtn, copyLabel, UI.BtnKind.Secondary); copyLabel.font = Theme.Display;
            UI.LayoutHeight(copyBtn.gameObject, 48);

            // ---- left: action buttons ----
            var actCol = new GameObject("LobbyActions", typeof(RectTransform));
            actCol.transform.SetParent(t, false);
            UI.Set((RectTransform)actCol.transform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), new Vector2(-360, -210), new Vector2(420, 180));
            var acol = UI.Col(actCol.transform, 10, new RectOffset(0, 0, 0, 0));
            var (startBtn, startLabel) = UI.Button(acol.transform, "▶  START (HOST)", StartMatch);
            UI.Variant(startBtn, startLabel, UI.BtnKind.Confirm); startLabel.font = Theme.Display; startLabel.fontSize = Theme.LabelSize;
            UI.LayoutHeight(startBtn.gameObject, 60);
            var (readyBtn, readyLabel) = UI.Button(acol.transform, "○  NOT READY", null);
            UI.Variant(readyBtn, readyLabel, UI.BtnKind.Secondary); readyLabel.font = Theme.Display;
            UI.LayoutHeight(readyBtn.gameObject, 48);
            var (backBtn, backLabel) = UI.Button(acol.transform, "←  BACK", GoMenu);
            UI.Variant(backBtn, backLabel, UI.BtnKind.Secondary); backLabel.font = Theme.Display;
            UI.LayoutHeight(backBtn.gameObject, 44);

            // ---- right: party seats ----
            var (sP, sContent) = UI.TitledPanel(t, "Party — 4 Players");
            UI.Set(sP.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), new Vector2(320, 30), new Vector2(560, 460));
            var scol = UI.Col(sContent, 10, new RectOffset(18, 18, 16, 16));
            Seat(scol.transform, 1, "Adaeze_Nwosu", Civ.Benin, ready: true, ai: false);
            Seat(scol.transform, 2, "Emeka_Okafor", Civ.Oyo, ready: false, ai: false);
            Seat(scol.transform, 3, "Iron_Guard_AI", Civ.Sokoto, ready: true, ai: true);
            Seat(scol.transform, 4, null, Civ.Benin, ready: false, ai: false);
        }

        /// One party-seat row: slot index, crest avatar, name + empire, and a ready/AI badge.
        void Seat(Transform parent, int idx, string name, Civ civ, bool ready, bool ai)
        {
            bool empty = name == null;
            var row = UI.Panel(parent, Theme.RoundSoft, empty ? Theme.Alpha(Color.black, 0.25f) : Theme.Alpha(Theme.PanelHi, 0.5f));
            UI.Border(row, Theme.RoundSoft, Theme.Alpha(Theme.Bronze, empty ? 0.2f : 0.35f));
            UI.LayoutHeight(row.gameObject, 64);

            var num = UI.Label(row.transform, idx.ToString(), Theme.BodySize, Theme.Muted, TextAnchor.MiddleCenter, true, Theme.Display);
            UI.Set(num.rectTransform, V(0, .5f), V(0, .5f), V(.5f, .5f), new Vector2(28, 0), new Vector2(28, 28));

            if (!empty)
            {
                Brand.Crest(row.transform, UnitConfig.CivColor(civ), 40, V(0, .5f), new Vector2(70, 0));
                var nm = UI.Label(row.transform, name, Theme.BodySize, Theme.Ivory, TextAnchor.LowerLeft, true);
                UI.Set(nm.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(102, 4), new Vector2(260, 22));
                var em = UI.Label(row.transform, FullName(civ), Theme.SmallSize, UnitConfig.CivColor(civ), TextAnchor.UpperLeft, false, Theme.Display);
                UI.Set(em.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(102, -14), new Vector2(260, 20));

                // ready / AI badge
                var badge = UI.Panel(row.transform, Theme.RoundSoft,
                    Theme.Alpha(ready ? Theme.Confirm : Theme.Danger, 0.18f));
                UI.Set(badge.rectTransform, V(1, .5f), V(1, .5f), V(1, .5f), new Vector2(-14, 0), new Vector2(ai ? 60 : 96, 28));
                UI.Border(badge, Theme.RoundSoft, ready ? Theme.Confirm : Theme.Danger);
                var bl = UI.Label(badge.transform, ai ? "AI" : (ready ? "✓ READY" : "○ WAIT"),
                                  Theme.SmallSize, ready ? Theme.Confirm : Theme.Danger, TextAnchor.MiddleCenter, true, Theme.Display);
                UI.Stretch(bl.rectTransform, 0, 0, 0, 0);
            }
            else
            {
                var nm = UI.Label(row.transform, "Empty Seat", Theme.BodySize, Theme.Faint, TextAnchor.MiddleLeft, true);
                UI.Set(nm.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(70, 0), new Vector2(260, 22));
            }
        }

        // ============================================================ navigation / launch
        void GoMenu() { ShowOnly(_menu); }
        void GoSetup() { ShowOnly(_setup); RefreshSetup(); }
        void GoLobby() { ShowOnly(_lobby); }

        // Quick Battle: skip Match Setup and drop straight into a default FFA (your last/Benin pick vs
        // 3 random AI) — the Figma's one-tap "instant action" entry.
        void QuickBattle() => StartMatch();

        void StartMatch()
        {
            // Resolve opponents: take the first _opponentCount slots; "Random" picks any civ
            // (including possibly the player's — an FFA mirror match is allowed).
            var opponents = new Civ[_opponentCount];
            for (int i = 0; i < _opponentCount; i++)
                opponents[i] = _oppPick[i] >= 0 ? Civs[_oppPick[i]] : Civs[Random.Range(0, Civs.Length)];

            MatchConfig.PlayerCiv = _playerCiv;
            MatchConfig.Opponents = opponents;
            MatchConfig.Difficulty = _difficulty;
            SceneManager.LoadScene(MatchScene);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static Vector2 V(float a, float b) => new Vector2(a, b);
    }
}
