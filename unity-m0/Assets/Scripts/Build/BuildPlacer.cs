using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NaijaEmpires
{
    /// Places buildings for the player. Non-wall buildings drop a ghost in the MIDDLE of the screen
    /// that you drag into place, then confirm (✓) or cancel (✕) via the HUD bar floating over it.
    ///
    /// WALL MODE: selecting Wall keeps a follow-ghost on the cursor for feedback. A single LEFT-CLICK
    /// lays one wall segment at the cursor; PRESS-AND-DRAG lays a straight line of segments and builds
    /// the whole line the moment you release (charging per segment, capped by what you can afford).
    /// You stay in wall mode after each placement so you can keep building; right-click / Esc exits.
    public class BuildPlacer : MonoBehaviour
    {
        public static BuildPlacer Instance { get; private set; }
        public bool Placing { get; private set; }

        /// True while the Wall continuous-placer is active (single-building placement is not in this mode).
        public bool InWallMode => Placing && _kind == BuildingKind.Wall;

        /// True while a single building is in centered drag-to-place mode (the HUD shows its ✓/✕ bar).
        public bool Centered => Placing && _kind != BuildingKind.Wall;

        /// World position of the follow/placement ghost (used by the HUD to float the ✓/✕ bar over it).
        public Vector3 GhostWorld => _ghost != null ? _ghost.transform.position : Vector3.zero;

        Camera _cam;
        GameObject _ghost;                       // follow-ghost (all kinds, wall included)
        BuildingKind _kind;

        // Wall mode state
        readonly List<GameObject> _wallGhosts = new();
        readonly List<Vector3> _wallPoints = new();
        bool _wallDragging;
        Vector3 _wallAnchor;

        void Awake() { Instance = this; _cam = Camera.main; }

        public void BeginPlace(BuildingKind kind)
        {
            var e = Match.Econ(FactionId.Player);
            if (e == null || e.Age < BuildingConfig.AgeRequired(kind)) return;

            // Cities are capped per age — refuse to found a new Town Centre once you're at the limit.
            if (kind == BuildingKind.TownCentre &&
                Match.TownCentreCount(FactionId.Player) >= BuildingConfig.MaxTownCentres(e.Age)) return;

            Cancel();
            _kind = kind;
            Placing = true;

            // Every kind (Wall included) gets a follow-ghost so there is immediate visual feedback the
            // moment the build button is pressed.
            Vector3 size = BuildingConfig.Size(kind);
            _ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ghost.name = "BuildGhost";
            var col = _ghost.GetComponent<Collider>(); if (col) Destroy(col);
            _ghost.transform.localScale = size;
            MaterialUtil.SetColor(_ghost.GetComponent<Renderer>(), new Color(0.5f, 0.85f, 1f, 0.55f));

            // Non-wall buildings start in the middle of the screen for the drag-then-confirm flow.
            if (kind != BuildingKind.Wall)
            {
                Vector3 c = ScreenCenterGround();
                _ghost.transform.position = new Vector3(c.x, size.y / 2f, c.z);
            }
        }

        void Update()
        {
            if (!Placing) return;

            if (_kind == BuildingKind.Wall)
            {
                if (Input.touchCount >= 2) { Cancel(); return; } // two-finger = cancel wall placement
                UpdateWall();
                return;
            }

            UpdateCentered();
        }

        // Drag-then-confirm placement: the ghost sits centre-screen; holding the pointer over open
        // ground (mouse or one finger) drags it to where you want to build. Confirm/cancel come from
        // the HUD ✓/✕ bar (Confirm()/CancelPlace()); Esc also cancels on desktop.
        void UpdateCentered()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { Cancel(); return; }

            bool held, overUI;
            if (Input.touchCount == 1)
            {
                // Mobile: test the UI with the touch's OWN fingerId. IsPointerOverGameObject() with no
                // argument tests the (nonexistent) mouse pointer on iOS and wrongly returns false — which
                // let the ghost (and the ✓/✕ bar that follows it) jump under your finger the instant you
                // tapped Confirm/Cancel, moving the button away before the tap landed so it never fired.
                held = true;
                var t = Input.GetTouch(0);
                overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId);
            }
            else
            {
                held = Input.GetMouseButton(0);
                overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            }

            if (held && !overUI && GroundPoint(out var p))
                _ghost.transform.position = new Vector3(p.x, _ghost.transform.localScale.y / 2f, p.z);
        }

        // Ground point under the centre of the screen (where the ghost first appears).
        Vector3 ScreenCenterGround()
        {
            Ray r = _cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            return new Plane(Vector3.up, 0f).Raycast(r, out float d) ? r.GetPoint(d) : Vector3.zero;
        }

        /// Confirm the centered placement (called by the HUD ✓ button).
        public void Confirm() { if (Centered) TryPlace(); }

        /// Cancel any placement (called by the HUD ✕ button).
        public void CancelPlace() => Cancel();

        void TryPlace()
        {
            var e = Match.Econ(FactionId.Player);
            if (e == null) { Cancel(); return; }
            if (!e.Spend(BuildingConfig.CostOf(_kind, e.Civ))) { Cancel(); return; }

            // Player buildings go up as construction sites that villagers must build (the AI + walls
            // build instantly). Selected villagers start it; if none are selected, the nearest one does.
            var go = BuildingFactory.Spawn(_kind, _ghost.transform.position, FactionId.Player);
            var site = go.AddComponent<Construction>();
            site.Begin(_kind);
            SendBuilders(site);
            Cancel();
        }

        void SendBuilders(Construction site)
        {
            bool any = false;
            var sm = SelectionManager.Instance;
            if (sm != null)
                foreach (var s in sm.Selected)
                {
                    if (s == null) continue;
                    var v = s.GetComponent<Villager>();
                    if (v != null) { v.Build(site); any = true; }
                }

            if (any) return;

            // Nobody selected — grab the nearest idle-ish player villager so the site still rises.
            Villager nearest = null; float best = float.MaxValue;
            foreach (var v in Object.FindObjectsByType<Villager>(FindObjectsSortMode.None))
            {
                var f = v.GetComponent<Faction>();
                if (f == null || f.Id != FactionId.Player) continue;
                float d = (v.transform.position - site.transform.position).sqrMagnitude;
                if (d < best) { best = d; nearest = v; }
            }
            if (nearest != null) nearest.Build(site);
        }

        // ---- Wall continuous placement ----------------------------------------------------------

        void UpdateWall()
        {
            // Right-click / Esc exits wall mode entirely.
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) { Cancel(); return; }

            bool haveGround = GroundPoint(out var cur);

            // Press starts a (possible) drag line from the anchor.
            if (Input.GetMouseButtonDown(0) && haveGround) { _wallDragging = true; _wallAnchor = cur; }

            if (_wallDragging && haveGround)
            {
                // While dragging, preview the line of segments; hide the single follow-ghost.
                if (_ghost) _ghost.SetActive(false);
                RebuildWallGhosts(_wallAnchor, cur);
            }
            else if (haveGround && _ghost)
            {
                // Idle: a single follow-ghost sits under the cursor so you can see where a click lands.
                _ghost.SetActive(true);
                Vector3 sz = _ghost.transform.localScale;
                _ghost.transform.position = new Vector3(cur.x, sz.y / 2f, cur.z);
            }

            // Release builds the previewed segments immediately (per-segment charge). A plain click with
            // no real drag previews a single segment, so it lays exactly one wall. We stay in wall mode.
            if (_wallDragging && Input.GetMouseButtonUp(0))
            {
                _wallDragging = false;
                BuildPreviewedWall();
                ClearWallGhosts();
            }
        }

        bool GroundPoint(out Vector3 point)
        {
            if (Physics.Raycast(_cam.ScreenPointToRay(Input.mousePosition), out var hit, 500f))
            {
                point = new Vector3(hit.point.x, 0f, hit.point.z);
                return true;
            }
            point = default;
            return false;
        }

        // Lay out evenly-spaced segment centres from a -> b and (re)build the preview ghosts.
        void RebuildWallGhosts(Vector3 a, Vector3 b)
        {
            ClearWallGhosts();

            Vector3 size = BuildingConfig.Size(BuildingKind.Wall);
            float spacing = Mathf.Max(0.1f, size.x); // one segment apart
            Vector3 delta = b - a;
            float len = delta.magnitude;
            int count = Mathf.Max(1, Mathf.RoundToInt(len / spacing) + 1);
            Vector3 dir = len > 0.001f ? delta / len : Vector3.forward;

            for (int i = 0; i < count; i++)
            {
                Vector3 c = a + dir * (spacing * i);
                _wallPoints.Add(c);

                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.name = "WallGhost";
                var col = g.GetComponent<Collider>(); if (col) Destroy(col);
                g.transform.localScale = size;
                g.transform.position = new Vector3(c.x, size.y / 2f, c.z);
                if (len > 0.001f) g.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                MaterialUtil.SetColor(g.GetComponent<Renderer>(), new Color(0.5f, 0.85f, 1f, 0.55f));
                _wallGhosts.Add(g);
            }
        }

        // Build every previewed wall segment the player can still afford (per-segment charge).
        void BuildPreviewedWall()
        {
            var e = Match.Econ(FactionId.Player);
            if (e == null) return;
            Cost per = BuildingConfig.CostOf(BuildingKind.Wall, e.Civ);
            foreach (var p in _wallPoints)
            {
                if (!e.Spend(per)) break;   // out of resources -> stop building the rest
                BuildingFactory.Spawn(BuildingKind.Wall, p, FactionId.Player);
            }
        }

        void ClearWallGhosts()
        {
            foreach (var g in _wallGhosts) if (g) Destroy(g);
            _wallGhosts.Clear();
            _wallPoints.Clear();
        }

        void Cancel()
        {
            Placing = false;
            _wallDragging = false;
            if (_ghost) Destroy(_ghost);
            ClearWallGhosts();
        }
    }
}
