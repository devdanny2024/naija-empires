using UnityEngine;

namespace NaijaEmpires
{
    /// Makes a unit selectable and shows a green ring when selected.
    public class Selectable : MonoBehaviour
    {
        GameObject _ring;
        public bool IsSelected { get; private set; }

        void Awake()
        {
            _ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _ring.name = "SelectionRing";
            var col = _ring.GetComponent<Collider>();
            if (col) Destroy(col);
            _ring.transform.SetParent(transform, false);
            _ring.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            _ring.transform.localScale = new Vector3(1.5f, 0.03f, 1.5f);
            MaterialUtil.SetColor(_ring.GetComponent<Renderer>(), new Color(0.2f, 1f, 0.45f));
            _ring.SetActive(false);
        }

        public void SetSelected(bool value)
        {
            IsSelected = value;
            if (_ring) _ring.SetActive(value);
        }
    }
}
