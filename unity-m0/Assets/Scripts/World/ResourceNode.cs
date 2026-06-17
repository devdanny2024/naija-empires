using System.Collections.Generic;
using UnityEngine;

namespace NaijaEmpires
{
    /// A gatherable resource pile (Yam / Timber / Iron). Natural nodes deplete and remove themselves.
    /// A renewable node (a Farm) never depletes and caps how many villagers can work it at once.
    public class ResourceNode : MonoBehaviour
    {
        public ResourceType Type = ResourceType.Yam;
        [SerializeField] int amount = 1200; // larger so nodes (esp. trees) don't deplete too fast
        [SerializeField] bool renewable = false; // farms: yield forever, never destroyed
        [SerializeField] int maxWorkers = 0;      // 0 = unlimited (natural nodes); farms cap this

        // Villagers currently assigned. Used only to enforce maxWorkers; pruned of dead villagers on query.
        readonly HashSet<Villager> _workers = new();

        public int Amount => renewable ? int.MaxValue : amount;

        /// Turn this node into a renewable workplace (Farms call this on spawn).
        public void MakeRenewable(ResourceType type, int workerCap)
        {
            Type = type;
            renewable = true;
            maxWorkers = workerCap;
        }

        /// Reserve a worker slot for `v`. Returns false if a capped node is already full.
        public bool TryClaimWorker(Villager v)
        {
            _workers.RemoveWhere(w => w == null); // drop villagers that died/left without releasing
            if (maxWorkers > 0 && !_workers.Contains(v) && _workers.Count >= maxWorkers) return false;
            _workers.Add(v);
            return true;
        }

        public void ReleaseWorker(Villager v) => _workers.Remove(v);

        /// Take up to `n` units; returns how much was actually extracted. Renewable nodes never deplete.
        public int Extract(int n)
        {
            if (renewable) return n;
            int taken = Mathf.Min(n, amount);
            amount -= taken;
            if (amount <= 0) Destroy(gameObject);
            return taken;
        }
    }
}
