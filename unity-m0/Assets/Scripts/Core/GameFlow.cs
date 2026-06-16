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
        // -1 == "Random" (resolved at launch); otherwise an index into Civs.
        readonly int[] _oppPick = { -1, -1, -1 };

        // ---- screens --------------------------------------------------------------------
        GameObject _splash, _menu, _setup, _comingSoon;
        CanvasGroup _splashGroup;
        float _splashT, _fadeT;
        bool _fadingOut;

        // setup widgets we restyle on selection
        readonly List<(Button btn, Civ civ)> _playerCivBtns = new();
        readonly List<(Button btn, int n)> _countBtns = new();
        // one row of 5 option buttons per opponent slot (Random + 4 civs)
        readonly List<List<(Button btn, int pick)>> _oppBtns = new();
        readonly List<GameObject> _oppRows = new();

        void Awake()
        {
            EnsureEventSystem();
            var canvas = BuildCanvas();
            BuildSplash(canvas);
            BuildMenu(canvas);
            BuildSetup(canvas);
            BuildComingSoon(canvas);

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

        /// A full-screen Night backdrop that fills its parent. Returns the panel transform.
        Transform FullScreen(Transform root, string name, Color fill)
        {
            var p = UI.Panel(root, Theme.Round, fill);
            p.gameObject.name = name;
            UI.Stretch(p.rectTransform, 0, 0, 0, 0);
            return p.transform;
        }

        void ShowOnly(GameObject screen)
        {
            _splash.SetActive(screen == _splash);
            _menu.SetActive(screen == _menu);
            _setup.SetActive(screen == _setup);
            // _comingSoon is an overlay on top of the menu, toggled separately
            if (screen != _menu) _comingSoon.SetActive(false);
        }

        // ============================================================ Splash
        void BuildSplash(Transform root)
        {
            var t = FullScreen(root, "Splash", Theme.Night);
            _splash = t.gameObject;
            _splashGroup = _splash.AddComponent<CanvasGroup>();

            // Logo (if the editor assigned the AppIcon texture); else just the wordmark.
            if (Logo != null)
            {
                var sprite = Sprite.Create(Logo, new Rect(0, 0, Logo.width, Logo.height),
                                           new Vector2(0.5f, 0.5f), 100f);
                var icon = UI.Icon(t, sprite, 320, Color.white);
                UI.Set(icon.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f),
                       new Vector2(0, 70), new Vector2(320, 320));
            }

            var word = UI.Label(t, "NAIJA EMPIRES", 72, Theme.Bronze, TextAnchor.MiddleCenter, true, Theme.Display);
            UI.Shadow(word, Theme.Alpha(Color.black, 0.7f), new Vector2(2f, -2f));
            UI.Set(word.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f),
                   new Vector2(0, Logo != null ? -150 : 0), new Vector2(1200, 90));

            var tagline = UI.Label(t, "Bronze & Indigo  ·  An African RTS", Theme.LabelSize,
                                   Theme.Muted, TextAnchor.MiddleCenter);
            UI.Set(tagline.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f),
                   new Vector2(0, Logo != null ? -210 : -64), new Vector2(900, 30));
        }

        // ============================================================ Main Menu
        void BuildMenu(Transform root)
        {
            var t = FullScreen(root, "MainMenu", Theme.Night);
            _menu = t.gameObject;

            var title = UI.Label(t, "NAIJA EMPIRES", 64, Theme.Bronze, TextAnchor.MiddleCenter, true, Theme.Display);
            UI.Shadow(title, Theme.Alpha(Color.black, 0.7f), new Vector2(2f, -2f));
            UI.Set(title.rectTransform, V(.5f, 1), V(.5f, 1), V(.5f, 1), new Vector2(0, -150), new Vector2(1200, 90));

            // centred button column
            var panel = UI.Panel(t, Theme.Round, Theme.Alpha(Theme.Panel, 0.96f));
            UI.Set(panel.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), new Vector2(0, -30), new Vector2(420, 320));
            UI.Border(panel, Theme.Round, Theme.Alpha(Theme.Bronze, 0.5f));
            var col = UI.Col(panel.transform, 14, new RectOffset(28, 28, 28, 28));

            MakeMenuButton(col.transform, "Play", Theme.Bronze, Theme.Night, GoSetup);
            MakeMenuButton(col.transform, "Multiplayer", Theme.PanelHi, Theme.Ivory, () => _comingSoon.SetActive(true));
            MakeMenuButton(col.transform, "Quit", Theme.PanelHi, Theme.Ivory, Quit);

            var foot = UI.Label(t, "v0  ·  single-player skirmish", Theme.SmallSize,
                                Theme.Alpha(Theme.Muted, 0.8f), TextAnchor.LowerCenter);
            UI.Set(foot.rectTransform, V(.5f, 0), V(.5f, 0), V(.5f, 0), new Vector2(0, 18), new Vector2(900, 24));
        }

        void MakeMenuButton(Transform parent, string text, Color fill, Color textColor, System.Action onClick)
        {
            var (btn, label) = UI.Button(parent, text, onClick);
            btn.image.color = fill;
            label.color = textColor;
            UI.LayoutHeight(btn.gameObject, 64);
        }

        // ============================================================ Match Setup
        void BuildSetup(Transform root)
        {
            var t = FullScreen(root, "MatchSetup", Theme.Night);
            _setup = t.gameObject;

            var title = UI.Label(t, "MATCH SETUP", 48, Theme.Bronze, TextAnchor.MiddleCenter, true, Theme.Display);
            UI.Shadow(title, Theme.Alpha(Color.black, 0.7f), new Vector2(2f, -2f));
            UI.Set(title.rectTransform, V(.5f, 1), V(.5f, 1), V(.5f, 1), new Vector2(0, -40), new Vector2(1200, 64));

            var panel = UI.Panel(t, Theme.Round, Theme.Alpha(Theme.Panel, 0.96f));
            UI.Set(panel.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), new Vector2(0, 10), new Vector2(1100, 720));
            UI.Border(panel, Theme.Round, Theme.Alpha(Theme.Bronze, 0.5f));
            var col = UI.Col(panel.transform, 12, new RectOffset(36, 36, 26, 26));

            // ---- your empire
            UI.Header(col.transform, "YOUR EMPIRE");
            var civRow = Row(col.transform, 64);
            foreach (var c in Civs)
            {
                var civ = c;
                var (btn, _) = UI.Button(civRow, "", () => { _playerCiv = civ; RefreshSetup(); }, blank: true);
                CivCardContent(btn.transform, civ);
                _playerCivBtns.Add((btn, civ));
            }

            // ---- opponent count
            UI.Header(col.transform, "OPPONENTS");
            var countRow = Row(col.transform, 56);
            for (int n = 1; n <= 3; n++)
            {
                int count = n;
                var (btn, label) = UI.Button(countRow, n.ToString(), () => { _opponentCount = count; RefreshSetup(); });
                label.fontSize = Theme.TitleSize;
                _countBtns.Add((btn, count));
            }

            // ---- per-opponent empire picker (3 rows; we show 1..count)
            UI.Header(col.transform, "OPPONENT EMPIRES");
            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                var rowWrap = Row(col.transform, 52);
                _oppRows.Add(rowWrap.gameObject);

                var caption = UI.Label(rowWrap, $"AI {slot + 1}", Theme.BodySize, Theme.Muted, TextAnchor.MiddleLeft, true);
                var capLE = caption.gameObject.AddComponent<LayoutElement>();
                capLE.preferredWidth = 80; capLE.flexibleWidth = 0;

                var picks = new List<(Button, int)>();
                // -1 == Random, then 0..3 for each civ
                var (rndBtn, _) = UI.Button(rowWrap, "Random", () => { _oppPick[slot] = -1; RefreshSetup(); });
                picks.Add((rndBtn, -1));
                for (int ci = 0; ci < Civs.Length; ci++)
                {
                    int pick = ci;
                    var (btn, _) = UI.Button(rowWrap, "", () => { _oppPick[slot] = pick; RefreshSetup(); }, blank: true);
                    CivCardContent(btn.transform, Civs[ci]);
                    picks.Add((btn, pick));
                }
                _oppBtns.Add(picks);
            }

            // ---- actions
            var actions = Row(col.transform, 70);
            var (backBtn, backLabel) = UI.Button(actions, "Back", GoMenu);
            backBtn.image.color = Theme.PanelHi; backLabel.color = Theme.Ivory;
            var (startBtn, startLabel) = UI.Button(actions, "Start Match", StartMatch);
            startBtn.image.color = Theme.Bronze; startLabel.color = Theme.Night;
            startLabel.fontSize = Theme.LabelSize;

            RefreshSetup();
        }

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

        /// Swatch + civ name laid inside a (blank) button — used for both the player and opponent pickers.
        void CivCardContent(Transform btn, Civ civ)
        {
            var sw = UI.Swatch(btn, UnitConfig.CivColor(civ), 0);
            UI.Set(sw.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(14, 0), new Vector2(20, 20));
            var name = UI.Label(btn, civ.ToString(), Theme.BodySize, Theme.Ivory, TextAnchor.MiddleCenter, true);
            UI.Stretch(name.rectTransform, 26, 0, 6, 0);
        }

        // ---- selection styling -----------------------------------------------------------
        void RefreshSetup()
        {
            foreach (var (btn, civ) in _playerCivBtns) StyleSelectable(btn, civ == _playerCiv);
            foreach (var (btn, n) in _countBtns) StyleSelectable(btn, n == _opponentCount);

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
            // selected = bronze-tinted raised fill; otherwise the default panel fill.
            btn.image.color = selected ? Theme.Bronze : Theme.PanelHi;
            var label = btn.GetComponentInChildren<Text>();
            if (label != null) label.color = selected ? Theme.Night : Theme.Ivory;
        }

        // ============================================================ Coming-soon overlay
        void BuildComingSoon(Transform root)
        {
            // Dim scrim over the whole screen + a centred panel. Shown atop the Main Menu.
            var t = FullScreen(root, "ComingSoon", Theme.Alpha(Theme.Night, 0.82f));
            _comingSoon = t.gameObject;

            var panel = UI.Panel(t, Theme.Round, Theme.Panel);
            UI.Set(panel.rectTransform, V(.5f, .5f), V(.5f, .5f), V(.5f, .5f), Vector2.zero, new Vector2(620, 300));
            UI.Border(panel, Theme.Round, Theme.Bronze);
            var col = UI.Col(panel.transform, 14, new RectOffset(36, 36, 34, 30));

            var head = UI.Label(col.transform, "MULTIPLAYER", Theme.TitleSize, Theme.Bronze, TextAnchor.MiddleCenter, true, Theme.Display);
            UI.LayoutHeight(head.gameObject, 40);
            var body = UI.Label(col.transform,
                "Online empires are coming in a future update.\nFor now, sharpen your strategy in single-player skirmish.",
                Theme.BodySize, Theme.Muted, TextAnchor.MiddleCenter);
            UI.LayoutHeight(body.gameObject, 80);

            // PHASE 2 HOOK: replace this "Coming soon" panel with the real online lobby/matchmaking
            // flow (Photon). On a successful connect, build MatchConfig from the lobby seats and
            // SceneManager.LoadScene(MatchScene) — see StartMatch() for the single-player equivalent.

            var (ok, okLabel) = UI.Button(col.transform, "Got it", () => _comingSoon.SetActive(false));
            ok.image.color = Theme.Bronze; okLabel.color = Theme.Night;
            UI.LayoutHeight(ok.gameObject, 56);

            _comingSoon.SetActive(false);
        }

        // ============================================================ navigation / launch
        void GoMenu() { ShowOnly(_menu); }
        void GoSetup() { ShowOnly(_setup); RefreshSetup(); }

        void StartMatch()
        {
            // Resolve opponents: take the first _opponentCount slots; "Random" picks any civ
            // (including possibly the player's — an FFA mirror match is allowed).
            var opponents = new Civ[_opponentCount];
            for (int i = 0; i < _opponentCount; i++)
                opponents[i] = _oppPick[i] >= 0 ? Civs[_oppPick[i]] : Civs[Random.Range(0, Civs.Length)];

            MatchConfig.PlayerCiv = _playerCiv;
            MatchConfig.Opponents = opponents;
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
