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
        Text _yam, _timber, _iron, _pop, _age, _civ;
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

        // ---------------------------------------------------------------- resource bar
        void BuildResourceBar(Transform root)
        {
            var bar = UI.Panel(root, Theme.Pill, Theme.Alpha(Theme.Panel, 0.96f));
            UI.Set(bar.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                   new Vector2(0, -14), new Vector2(-28, 84));
            UI.Border(bar, Theme.Pill, Theme.Alpha(Theme.Bronze, 0.5f));
            var t = bar.transform;

            var title = UI.Label(t, "NAIJA EMPIRES", Theme.TitleSize, Theme.Bronze, TextAnchor.MiddleLeft, true, Theme.Display);
            UI.Shadow(title, Theme.Alpha(Theme.Night, 0.7f), new Vector2(1.5f, -1.5f));
            UI.Set(title.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(26, 13), new Vector2(260, 30));
            _civ = UI.Label(t, "Benin Empire", Theme.SmallSize, Theme.Muted, TextAnchor.MiddleLeft);
            UI.Set(_civ.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(26, -15), new Vector2(260, 20));

            var diamond = UI.Swatch(t, Theme.Bronze, 12);
            UI.Set(diamond.rectTransform, V(0, .5f), V(0, .5f), V(.5f, .5f), new Vector2(290, 0), new Vector2(12, 12));
            diamond.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);

            float x = 320;
            _yam = Chip(t, ref x, Theme.Yam, "YAM");
            _timber = Chip(t, ref x, Theme.Timber, "TIMBER");
            _iron = Chip(t, ref x, Theme.Iron, "IRON");
            x += 14;
            _pop = Chip(t, ref x, Theme.Ivory, "POP", Theme.PopIcon);
            _age = Chip(t, ref x, Theme.BronzeLight, "AGE");

            (_ageBtn, _ageBtnLabel) = UI.Button(t, "Advance Age", () => Ages.TryAdvance(FactionId.Player));
            UI.Set(_ageBtn.GetComponent<RectTransform>(), V(1, .5f), V(1, .5f), V(1, .5f),
                   new Vector2(-18, 0), new Vector2(252, 50));
            _ageBtn.image.color = Theme.Bronze;
            _ageBtnLabel.color = Theme.Night;
        }

        Text Chip(Transform bar, ref float x, Color swatch, string caption, Sprite icon = null)
        {
            if (icon != null)
            {
                var ic = UI.Icon(bar, icon, 20, swatch);
                UI.Set(ic.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(x - 1, 8), new Vector2(20, 20));
            }
            else
            {
                // No honest icon for this resource — keep an on-brand diamond gem with a bronze rim.
                var rim = UI.Swatch(bar, Theme.Alpha(Theme.Bronze, 0.55f), 0);
                UI.Set(rim.rectTransform, V(0, .5f), V(0, .5f), V(.5f, .5f), new Vector2(x + 9, 8), new Vector2(18, 18));
                rim.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                var sw = UI.Swatch(bar, swatch, 0);
                UI.Set(sw.rectTransform, V(0, .5f), V(0, .5f), V(.5f, .5f), new Vector2(x + 9, 8), new Vector2(13, 13));
                sw.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            }
            var val = UI.Label(bar, "0", Theme.TitleSize, Theme.Ivory, TextAnchor.MiddleLeft, true);
            UI.Set(val.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(x + 28, 8), new Vector2(96, 30));
            var cap = UI.Label(bar, caption, Theme.SmallSize, Theme.Muted, TextAnchor.MiddleLeft);
            UI.Set(cap.rectTransform, V(0, .5f), V(0, .5f), V(0, .5f), new Vector2(x + 1, -15), new Vector2(120, 20));
            x += 152;
            return val;
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
                _yam.text = e.Yam.ToString();
                _timber.text = e.Timber.ToString();
                _iron.text = e.Iron.ToString();
                _pop.text = $"{e.PopUsed}/{e.PopCap}";
                _age.text = e.Age.ToString();
                _civ.text = e.Civ + " Empire";

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

            var title = UI.Label(panel.transform, "MAP", Theme.SmallSize, Theme.Bronze, TextAnchor.UpperLeft, true);
            UI.Set(title.rectTransform, V(0, 1), V(0, 1), V(0, 1), new Vector2(16, -8), new Vector2(80, 18));

            var land = UI.Swatch(panel.transform, new Color(0.21f, 0.33f, 0.19f, 1f), 0); // island
            UI.Set(land.rectTransform, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), new Vector2(0, -8), new Vector2(214, 196));
            _miniArea = land.rectTransform;

            _camMarker = UI.Swatch(_miniArea.transform, Theme.Alpha(Theme.BronzeLight, 0.35f), 18);
            _camMarker.rectTransform.anchorMin = _camMarker.rectTransform.anchorMax = V(0.5f, 0.5f);
            _camMarker.rectTransform.pivot = V(0.5f, 0.5f);
        }

        void UpdateMinimap()
        {
            if (_miniArea == null) return;
            int i = 0;
            foreach (var f in FindObjectsByType<Faction>(FindObjectsSortMode.None))
            {
                bool isUnit = f.GetComponent<Unit>() != null;
                Color col = f.Id == FactionId.Player ? Theme.Benin : Theme.Oyo;
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
