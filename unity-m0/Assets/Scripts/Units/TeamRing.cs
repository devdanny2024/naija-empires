using UnityEngine;

namespace NaijaEmpires
{
    /// A flat faction-colored disc at a unit's feet — the always-on friend/foe signal,
    /// so the model itself no longer has to be flattened to one team colour. Reads the
    /// faction from the sibling Faction component. Sits just under the selection ring.
    public class TeamRing : MonoBehaviour
    {
        void Start()
        {
            var f = GetComponent<Faction>();
            Color c = UnitConfig.BodyColor(f != null ? f.Id : FactionId.Player);
            c.a = 0.85f;

            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "TeamRing";
            var col = disc.GetComponent<Collider>();
            if (col) Destroy(col);
            disc.transform.SetParent(transform, false);
            disc.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            disc.transform.localScale = new Vector3(1.25f, 0.02f, 1.25f);
            MaterialUtil.SetColor(disc.GetComponent<Renderer>(), c);
        }
    }
}
