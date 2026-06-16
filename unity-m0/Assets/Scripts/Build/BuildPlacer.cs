using System.Collections.Generic;
using UnityEngine;

namespace NaijaEmpires
{
    /// Places buildings for the player. A ghost follows the cursor; left-click builds (if age +
    /// resources allow), right-click / Esc cancels.
    ///
    /// WALL MODE: selecting Wall switches to a continuous placer. Press-and-drag the mouse to lay a
    /// straight line of wall segments (spaced one segment apart). Releasing the mouse leaves the line
    /// pending; Enter / ConfirmWall() builds it (charging per segment, capped by what you can afford),
    /// Esc / CancelWall() discards it. A future UI bar can drive this via InWallMode / ConfirmWall /
    /// CancelWall.
    public class BuildPlacer : MonoBehaviour
    {
        public static BuildPlacer Instance { get; private set; }
        public bool Placing { get; private set; }

        /// True while the Wall continuous-placer is active (single-building placement is not in this mode).
        public bool InWallMode => Placing && _kind == BuildingKind.Wall;

        Camera _cam;
        GameObject _ghost;                       // single-building ghost (non-wall kinds)
        BuildingKind _kind;

        // Wall mode state
        readonly List<GameObject> _wallGhosts = new();
        readonly List<Vector3> _wallPoints = new();
        bool _wallDragging;
        bool _wallPending;                       // line laid, awaiting confirm/cancel
        Vector3 _wallAnchor;

        void Awake() { Instance = this; _cam = Camera.main; }

        public void BeginPlace(BuildingKind kind)
        {
            var e = Match.Econ(FactionId.Player);
            if (e == null || e.Age < BuildingConfig.AgeRequired(kind)) return;

            Cancel();
            _kind = kind;
            Placing = true;

            if (kind == BuildingKind.Wall) return; // wall uses drag ghosts, not a single follow-ghost

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

            BuildingFactory.Spawn(_kind, _ghost.transform.position, FactionId.Player);
            Cancel();
        }

        // ---- Wall continuous placement ----------------------------------------------------------

        void UpdateWall()
        {
            // Esc cancels the whole wall mode; right-click does the same.
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) { CancelWall(); return; }

            // Enter confirms a pending (released) line.
            if (_wallPending && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                ConfirmWall();
                return;
            }

            if (Input.GetMouseButtonDown(0) && !_wallPending)
            {
                if (GroundPoint(out var p)) { _wallDragging = true; _wallAnchor = p; }
            }

            if (_wallDragging && Input.GetMouseButton(0))
            {
                if (GroundPoint(out var p)) RebuildWallGhosts(_wallAnchor, p);
            }

            if (_wallDragging && Input.GetMouseButtonUp(0))
            {
                _wallDragging = false;
                _wallPending = _wallPoints.Count > 0;   // keep the ghosts up awaiting confirm
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

        /// Build every pending wall segment the player can still afford (per-segment charge), then exit.
        /// Public so a UI bar's confirm button can call it.
        public void ConfirmWall()
        {
            var e = Match.Econ(FactionId.Player);
            if (e != null)
            {
                Cost per = BuildingConfig.CostOf(BuildingKind.Wall, e.Civ);
                foreach (var p in _wallPoints)
                {
                    if (!e.Spend(per)) break;   // out of resources -> stop building the rest
                    BuildingFactory.Spawn(BuildingKind.Wall, p, FactionId.Player);
                }
            }
            Cancel();
        }

        /// Discard the pending wall line and leave wall mode. Public for a UI bar's cancel button.
        public void CancelWall() => Cancel();

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
            _wallPending = false;
            if (_ghost) Destroy(_ghost);
            ClearWallGhosts();
        }
    }
}
