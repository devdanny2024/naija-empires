using System.Collections.Generic;
using UnityEngine;

namespace NaijaEmpires
{
    /// Places buildings for the player. A ghost follows the cursor; left-click builds (if age +
    /// resources allow), right-click / Esc cancels.
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
        }

        void Update()
        {
            if (!Placing) return;

            if (Input.touchCount >= 2) { Cancel(); return; } // two-finger = cancel placement on touch

            if (_kind == BuildingKind.Wall) { UpdateWall(); return; }

            if (Physics.Raycast(_cam.ScreenPointToRay(Input.mousePosition), out var hit, 500f))
                _ghost.transform.position = new Vector3(hit.point.x, _ghost.transform.localScale.y / 2f, hit.point.z);

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) { Cancel(); return; }
            if (Input.GetMouseButtonDown(0)) TryPlace();
        }

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
