using UnityEngine;

namespace NaijaEmpires
{
    /// The Barracks reads as a West-African war-camp instead of the old tombstone-like gate slab:
    /// a stone training hall (the swappable "Model" child) flanked by a wood-fence yard and a banner.
    ///
    /// The hall is the child named "Model" so Upgradeable.SwapModel can grow it through tiers; the
    /// fence + banner live under a separate "WarCampProps" child so they PERSIST across upgrades.
    /// All scales/offsets are eyeball-tunable first guesses (tune in-editor like the rest of the kit).
    public static class BarracksVisual
    {
        /// Returns the hall ("Model") so BuildingFactory treats it like any other building model
        /// (null -> caller falls back to a primitive hut, so the game never breaks if a mesh is missing).
        public static GameObject Build(Transform parent)
        {
            var hall = ModelLibrary.CreateModel("BarracksHall", parent, Color.white);
            if (hall == null) return null;

            var props = new GameObject("WarCampProps");
            props.transform.SetParent(parent, false);

            // A short wood-fence frontage either side of a central entrance gap.
            Fence(props.transform, new Vector3(-1.9f, 0f, 2.2f), 0f);
            Fence(props.transform, new Vector3( 1.9f, 0f, 2.2f), 0f);
            // Side runs of the yard.
            Fence(props.transform, new Vector3( 2.6f, 0f, 0.2f), 90f);
            Fence(props.transform, new Vector3(-2.6f, 0f, 0.2f), 90f);

            // Banner at the back corner so the compound reads as a military camp.
            var flag = ModelLibrary.CreateModel("BarracksFlag", props.transform, Color.white);
            if (flag != null) { flag.name = "Banner"; flag.transform.localPosition = new Vector3(2.4f, 0f, -2.2f); }

            return hall;
        }

        static void Fence(Transform parent, Vector3 pos, float rotY)
        {
            var seg = ModelLibrary.CreateModel("BarracksFence", parent, Color.white);
            if (seg == null) return;
            seg.name = "Fence"; // CreateModel names every piece "Model"; rename props so only the hall is "Model"
            seg.transform.localPosition = pos;
            seg.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        }
    }
}
