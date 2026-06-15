using UnityEngine;

namespace NaijaEmpires
{
    /// Places any building kind for the player: a ghost follows the cursor, left-click builds
    /// (if age + resources allow), right-click / Esc cancels.
    public class BuildPlacer : MonoBehaviour
    {
        public static BuildPlacer Instance { get; private set; }
        public bool Placing { get; private set; }

        Camera _cam;
        GameObject _ghost;
        BuildingKind _kind;

        void Awake() { Instance = this; _cam = Camera.main; }

        public void BeginPlace(BuildingKind kind)
        {
            var e = Match.Econ(FactionId.Player);
            if (e == null || e.Age < BuildingConfig.AgeRequired(kind)) return;

            Cancel();
            _kind = kind;
            Placing = true;

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

        void Cancel()
        {
            Placing = false;
            if (_ghost) Destroy(_ghost);
        }
    }
}
