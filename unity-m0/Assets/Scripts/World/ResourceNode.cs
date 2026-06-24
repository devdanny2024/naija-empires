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

        /// Friendly name shown when the player clicks the node (e.g. "Iron Mountain", "Rare Gold Vein").
        /// Falls back to the resource type. Set by the spawner.
        public string DisplayName;

        // Iron mountains are big mining sites: they shrink as they're mined and, when exhausted, leave a
        // mined-out husk instead of popping out of existence (set via Configure).
        bool _shrink;
        int _startAmount;
        Transform _model;
        Vector3 _baseScale = Vector3.one; // the model's FITTED scale (set by ModelLibrary.FitAndGround)
        bool _baseCaptured;

        // Resolve the "Model" child and capture its fitted scale ONCE, before we start shrinking it.
        void EnsureModel()
        {
            if (_model == null) _model = transform.Find("Model");
            if (_model != null && !_baseCaptured) { _baseScale = _model.localScale; _baseCaptured = true; }
        }

        /// One-line label for the click-to-inspect tag: name + remaining amount (renewable = workplace).
        public string Label()
        {
            string n = string.IsNullOrEmpty(DisplayName) ? Type.ToString() : DisplayName;
            return renewable ? n + "  (workplace)" : $"{n}  ·  {amount} left";
        }

        /// Set a custom reserve size and (for mountains) make the node shrink as it's mined + leave a
        /// husk when exhausted rather than being destroyed.
        public void Configure(int capacity, bool shrinkAsMined)
        {
            amount = Mathf.Max(1, capacity);
            _startAmount = amount;
            _shrink = shrinkAsMined;
        }

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
            if (_shrink) ShrinkToRemaining();
            if (amount <= 0) Deplete();
            return taken;
        }

        // Visibly shrink the mountain's mesh as it's mined down (feedback that it's being depleted).
        void ShrinkToRemaining()
        {
            EnsureModel();
            if (_model == null || _startAmount <= 0) return;
            float frac = Mathf.Clamp01((float)amount / _startAmount);
            // Scale RELATIVE to the fitted base scale — never Vector3.one (that collapsed the auto-fit
            // mountain to its tiny native size and made it vanish on the first gather).
            _model.localScale = _baseScale * Mathf.Lerp(0.55f, 1f, frac); // mined down but still a hill
        }

        // Exhausted: trees/fields are removed; a mountain leaves a small mined-out husk (scenery) and
        // stops yielding — removing the ResourceNode makes assigned villagers (which see the node go
        // null) move on, and dropping the collider makes it non-interactable.
        void Deplete()
        {
            if (!_shrink) { Destroy(gameObject); return; }
            EnsureModel();
            if (_model != null) _model.localScale = _baseScale * 0.3f;
            name = (string.IsNullOrEmpty(DisplayName) ? "Iron Mountain" : DisplayName) + " (Depleted)";
            var col = GetComponent<Collider>(); if (col) Destroy(col);
            Destroy(this);
        }
    }
}
