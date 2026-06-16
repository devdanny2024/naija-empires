using UnityEngine;

namespace NaijaEmpires
{
    /// RTS camera: keyboard/edge pan + scroll zoom (editor) and two-finger pan + pinch zoom (touch).
    /// Attach to the orthographic camera. Movement is along the ground plane regardless of tilt.
    public class RTSCameraController : MonoBehaviour
    {
        public float panSpeed = 14f;
        public float zoomSpeed = 4f;
        public float minZoom = 4f;
        public float maxZoom = 55f;     // raised so the ~120-unit map fits
        public float edgeSize = 18f;
        public bool edgeScroll = false; // off by default so it doesn't fight testing
        public float startZoom = 30f;   // default zoom-out for the larger map

        Camera _cam;
        Vector3 _dragPrev;
        bool _dragPan;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam != null) _cam.orthographicSize = Mathf.Clamp(startZoom, minZoom, maxZoom);
        }

        void Update()
        {
            if (Input.touchCount >= 2) { HandleTouch(); return; }

            Vector3 move = Vector3.zero;
            if (Key(KeyCode.W, KeyCode.UpArrow)) move += Forward();
            if (Key(KeyCode.S, KeyCode.DownArrow)) move -= Forward();
            if (Key(KeyCode.D, KeyCode.RightArrow)) move += Right();
            if (Key(KeyCode.A, KeyCode.LeftArrow)) move -= Right();

            if (edgeScroll)
            {
                Vector3 m = Input.mousePosition;
                if (m.x < edgeSize) move -= Right();
                if (m.x > Screen.width - edgeSize) move += Right();
                if (m.y < edgeSize) move -= Forward();
                if (m.y > Screen.height - edgeSize) move += Forward();
            }

            transform.position += move.normalized * panSpeed * Time.deltaTime;

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f) SetZoom(_cam.orthographicSize - scroll * zoomSpeed);

            // Right-mouse DRAG grabs and pans the map (explore by dragging). A right CLICK with no drag
            // stays a unit command (SelectionManager only commands when the right button didn't move).
            if (Input.GetMouseButtonDown(1)) { _dragPrev = Input.mousePosition; _dragPan = true; }
            if (Input.GetMouseButtonUp(1)) _dragPan = false;
            if (_dragPan && Input.GetMouseButton(1))
            {
                Vector3 d = Input.mousePosition - _dragPrev;
                _dragPrev = Input.mousePosition;
                float k = _cam.orthographicSize * 0.0026f; // scale with zoom so the drag feels consistent
                transform.position -= (Right() * d.x + Forward() * d.y) * k;
            }

            // Snap back to your village (Town Centre) — easy to get lost otherwise.
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Home))
            {
                var tc = TownCentre.Nearest(FactionId.Player, transform.position);
                if (tc != null) FocusOn(tc.transform.position);
            }
        }

        /// Centre the camera on a ground point (used by the "go to base" hotkey / minimap).
        public void FocusOn(Vector3 groundPoint)
        {
            groundPoint.y = 0f;
            transform.position = groundPoint - transform.forward * 34f;
        }

        void HandleTouch()
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);

            Vector2 prevMid = ((t0.position - t0.deltaPosition) + (t1.position - t1.deltaPosition)) * 0.5f;
            Vector2 nowMid = (t0.position + t1.position) * 0.5f;
            Vector2 pan = nowMid - prevMid;
            transform.position -= (Right() * pan.x + Forward() * pan.y) * 0.02f;

            float prevDist = ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;
            float nowDist = (t0.position - t1.position).magnitude;
            SetZoom(_cam.orthographicSize - (nowDist - prevDist) * 0.02f);
        }

        void SetZoom(float z) => _cam.orthographicSize = Mathf.Clamp(z, minZoom, maxZoom);

        static bool Key(KeyCode a, KeyCode b) => Input.GetKey(a) || Input.GetKey(b);

        Vector3 Forward() { Vector3 f = transform.forward; f.y = 0; return f.normalized; }
        Vector3 Right() { Vector3 r = transform.right; r.y = 0; return r.normalized; }
    }
}
