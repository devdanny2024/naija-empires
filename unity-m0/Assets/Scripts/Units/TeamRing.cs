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

    /// The building equivalent of TeamRing: a flat faction-coloured band around a building's base so you
    /// can tell whose building it is at a glance. Sized from the building's collider footprint. Call
    /// Refresh() to recolour if ownership changes (capture).
    public class BuildingTeamBand : MonoBehaviour
    {
        Renderer _rend;

        void Start() { Build(); Refresh(); }

        void Build()
        {
            float foot = 2f;
            var bc = GetComponent<BoxCollider>();
            if (bc != null) foot = Mathf.Max(bc.size.x, bc.size.z);

            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "TeamBand";
            var col = disc.GetComponent<Collider>(); if (col) Destroy(col);
            disc.transform.SetParent(transform, false);
            disc.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            disc.transform.localScale = new Vector3(foot * 1.35f, 0.02f, foot * 1.35f);
            _rend = disc.GetComponent<Renderer>();
        }

        public void Refresh()
        {
            var f = GetComponent<Faction>();
            Color c = UnitConfig.BodyColor(f != null ? f.Id : FactionId.Player);
            c.a = 0.9f;
            if (_rend != null) MaterialUtil.SetColor(_rend, c);
        }
    }
}
