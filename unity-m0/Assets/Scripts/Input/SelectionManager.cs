using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NaijaEmpires
{
    /// Select your own units (click / drag-box) and command them: right-click ground = move,
    /// right-click a resource = gather (villagers), right-click an enemy = attack (combat units).
    /// Single-clicking a friendly production building selects it for the train menu.
    public class SelectionManager : MonoBehaviour
    {
        public static SelectionManager Instance { get; private set; }

        readonly List<Selectable> _selected = new();
        public ProductionBuilding SelectedBuilding { get; private set; }
        public IReadOnlyList<Selectable> Selected => _selected;

        Camera _cam;
        Vector2 _dragStart;
        bool _dragging;
        bool _tapMoved;
        Vector2 _rightDownPos;
        bool _rightDown;

        void Awake() { Instance = this; _cam = Camera.main; }

        void Update()
        {
            if (Match.Over) return;
            // Drop any selected units that have since died/been destroyed (avoids accessing a
            // destroyed Selectable when issuing commands).
            _selected.RemoveAll(s => s == null);
            if (BuildPlacer.Instance != null && BuildPlacer.Instance.Placing) return;

            if (Input.touchCount > 0) HandleTouch(); // mobile
            else HandleMouse();                       // desktop
        }

        // Desktop: left = select / drag-box, right = command.
        void HandleMouse()
        {
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButtonDown(0) && !overUI) { _dragStart = Input.mousePosition; _dragging = true; }
            if (Input.GetMouseButtonUp(0) && _dragging) { _dragging = false; EndSelect(Input.mousePosition); }
            // Right-CLICK (no drag) = command; a right-DRAG is a camera pan — so only command if the
            // button didn't move far between press and release.
            if (Input.GetMouseButtonDown(1) && !overUI) { _rightDownPos = Input.mousePosition; _rightDown = true; }
            if (Input.GetMouseButtonUp(1) && _rightDown)
            {
                _rightDown = false;
                if (Vector2.Distance(_rightDownPos, Input.mousePosition) < 12f) IssueCommand(Input.mousePosition);
            }
        }

        // Touch: one-finger tap = select own / command target; one-finger drag = box-select;
        // two+ fingers = camera (handled by RTSCameraController), ignored here.
        void HandleTouch()
        {
            if (Input.touchCount != 1) { _dragging = false; return; }
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId);
                _dragStart = t.position; _dragging = !overUI; _tapMoved = false;
            }
            else if (t.phase == TouchPhase.Moved)
            {
                if (Vector2.Distance(_dragStart, t.position) > 22f) _tapMoved = true;
            }
            else if ((t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) && _dragging)
            {
                _dragging = false;
                if (_tapMoved) EndSelect(t.position);  // drag = box-select
                else TapAt(t.position);                // tap = select-or-command
            }
        }

        // A tap on an owned unit/building selects it; otherwise, if units are selected, the tap is a
        // command on whatever was tapped (ground = move, enemy = attack, node = gather).
        void TapAt(Vector2 pos)
        {
            bool ownedHit = Physics.Raycast(_cam.ScreenPointToRay(pos), out var hit, 500f) && OwnedByPlayer(hit.collider);
            if (ownedHit) SingleSelect(pos);
            else if (_selected.Count > 0) IssueCommand(pos);
            else SingleSelect(pos);
        }

        bool OwnedByPlayer(Component c)
        {
            var f = c.GetComponentInParent<Faction>();
            return f != null && f.Id == FactionId.Player;
        }

        void EndSelect(Vector2 end)
        {
            if (Vector2.Distance(_dragStart, end) < 8f) { SingleSelect(end); return; }

            ClearSelection();
            Rect r = RectFrom(_dragStart, end);
            foreach (var u in FindObjectsByType<Unit>(FindObjectsSortMode.None))
            {
                if (!OwnedByPlayer(u)) continue;
                Vector3 sp = _cam.WorldToScreenPoint(u.transform.position);
                if (sp.z > 0 && r.Contains(sp))
                {
                    var s = u.GetComponent<Selectable>();
                    if (s) Add(s);
                }
            }
        }

        void SingleSelect(Vector2 pos)
        {
            ClearSelection();
            if (!Physics.Raycast(_cam.ScreenPointToRay(pos), out var hit, 500f)) return;

            // Click a resource (any owner) → show what it is (and how much is left / rare-mineral name)
            // in the HUD's bottom resource panel, plus a highlight ring on the node.
            var resNode = hit.collider.GetComponentInParent<ResourceNode>();
            if (resNode != null)
            {
                if (BrandedHud.Instance != null) BrandedHud.Instance.ShowResourceInfo(resNode);
                else ResourceHighlight.Show(resNode);
                return;
            }

            if (!OwnedByPlayer(hit.collider)) return;

            var building = hit.collider.GetComponentInParent<ProductionBuilding>();
            if (building != null)
            {
                SelectedBuilding = building;
                var bs = building.GetComponent<Selectable>();
                if (bs) { bs.SetSelected(true); _selected.Add(bs); }
                return;
            }

            var s = hit.collider.GetComponentInParent<Selectable>();
            if (s) Add(s);
        }

        void IssueCommand(Vector2 pos)
        {
            if (_selected.Count == 0) return;
            if (!Physics.Raycast(_cam.ScreenPointToRay(pos), out var hit, 500f)) return;

            var faction = hit.collider.GetComponentInParent<Faction>();
            var node = hit.collider.GetComponentInParent<ResourceNode>();
            var site = hit.collider.GetComponentInParent<Construction>();
            var health = hit.collider.GetComponentInParent<Health>();

            if (faction != null && faction.Id != FactionId.Player)
            {
                var targetHealth = faction.GetComponent<Health>();
                foreach (var s in _selected)
                {
                    var combat = s.GetComponent<CombatUnit>();
                    if (combat != null && targetHealth != null) combat.AttackTarget(targetHealth);
                    else { var u = s.GetComponent<Unit>(); if (u) u.MoveTo(hit.point); }
                }
                CommandPing.Spawn(hit.point, new Color(1f, 0.32f, 0.26f)); // red = attack/target
                return;
            }

            // Right-click your own damaged building (anything with Health but no Unit) → villagers repair it.
            bool repair = faction != null && faction.Id == FactionId.Player
                          && health != null && health.GetComponent<Unit>() == null && health.Current < health.Max;

            // A plain move (not gather/build/repair) fans the group into a formation instead of stacking.
            bool moveOnly = node == null && site == null && !repair;
            Vector3[] form = moveOnly ? Formation(_selected.Count) : null;

            int i = 0;
            foreach (var s in _selected)
            {
                var villager = s.GetComponent<Villager>();
                if (repair && villager != null) villager.Repair(health);                       // repair
                else if (site != null && !site.Complete && villager != null) villager.Build(site); // help build
                else if (node != null && villager != null) villager.Gather(node);              // gather
                else { var u = s.GetComponent<Unit>(); if (u) u.MoveTo(form != null ? hit.point + form[i] : hit.point); } // move (formation)
                i++;
            }

            // Feedback ring so the player sees the command landed (esp. "go gather this resource").
            Color ping = repair ? new Color(0.5f, 0.85f, 1f)                                 // blue = repair
                       : site != null && !site.Complete ? new Color(0.5f, 0.85f, 1f)         // blue = build
                       : node != null ? new Color(0.55f, 1f, 0.5f)                            // green = gather
                       : new Color(0.85f, 0.9f, 1f);                                          // pale = move
            CommandPing.Spawn(hit.point, ping);
        }

        // Grid offsets centred on the click so a group fans out into a formation instead of piling onto
        // a single point. Roughly square; ~1.9 units between slots.
        static Vector3[] Formation(int n, float spacing = 1.9f)
        {
            var offs = new Vector3[Mathf.Max(0, n)];
            if (n <= 0) return offs;
            int cols = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(n)));
            int rows = Mathf.CeilToInt(n / (float)cols);
            for (int i = 0; i < n; i++)
            {
                int r = i / cols, c = i % cols;
                offs[i] = new Vector3((c - (cols - 1) / 2f) * spacing, 0f, (r - (rows - 1) / 2f) * spacing);
            }
            return offs;
        }

        void Add(Selectable s)
        {
            if (_selected.Contains(s)) return;
            _selected.Add(s);
            s.SetSelected(true);
        }

        void ClearSelection()
        {
            foreach (var s in _selected) if (s) s.SetSelected(false);
            _selected.Clear();
            SelectedBuilding = null;
        }

        static Rect RectFrom(Vector2 a, Vector2 b) =>
            new Rect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        void OnGUI()
        {
            if (!_dragging) return;
            Vector2 cur = Input.mousePosition;
            Rect r = RectFrom(new Vector2(_dragStart.x, Screen.height - _dragStart.y),
                              new Vector2(cur.x, Screen.height - cur.y));
            var prev = GUI.color;
            GUI.color = new Color(0.2f, 1f, 0.45f, 0.18f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 1f, 0.45f, 0.85f);
            float t = 2f;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
}
