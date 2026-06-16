using UnityEngine;

namespace NaijaEmpires
{
    /// A gatherable resource pile (Yam / Timber / Iron). Depletes and removes itself.
    public class ResourceNode : MonoBehaviour
    {
        public ResourceType Type = ResourceType.Yam;
        [SerializeField] int amount = 1200; // larger so nodes (esp. trees) don't deplete too fast

        public int Amount => amount;

        /// Take up to `n` units; returns how much was actually extracted.
        public int Extract(int n)
        {
            int taken = Mathf.Min(n, amount);
            amount -= taken;
            if (amount <= 0) Destroy(gameObject);
            return taken;
        }
    }
}
