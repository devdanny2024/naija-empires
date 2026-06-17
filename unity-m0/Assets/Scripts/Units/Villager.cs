using UnityEngine;

namespace NaijaEmpires
{
    /// Gatherer: walk to a resource, harvest until full, carry back to the nearest friendly
    /// Town Centre, repeat. Deposits into its own faction's economy.
    public class Villager : Unit
    {
        enum State { Idle, ToResource, Gathering, ToDropoff, ToBuild, Building }

        State _state = State.Idle;
        ResourceNode _node;
        Construction _site;
        ResourceType _carryType;
        int _carry;

        const int Capacity = 10;
        const float GatherInterval = 0.4f;
        float _gatherTimer;
        ModelAnimator _anim;

        /// True when the villager has nothing to do (used by the AI to assign work).
        public bool Idle => _state == State.Idle && moveTarget == null;

        protected override void Start()
        {
            base.Start();
            _anim = GetComponent<ModelAnimator>();
        }

        static Color ResColor(ResourceType t) => t switch
        {
            ResourceType.Yam => new Color(0.92f, 0.82f, 0.2f),
            ResourceType.Timber => new Color(0.4f, 0.6f, 0.25f),
            _ => new Color(0.62f, 0.64f, 0.68f),
        };

        public void Gather(ResourceNode node)
        {
            if (node == null) return;
            if (node != _node) LeaveWork();
            // Farms (and any capped node) only accept up to N workers; if full, don't crowd it.
            if (!node.TryClaimWorker(this)) { GoIdleNear(); return; }
            _node = node;
            _carryType = node.Type;
            _state = State.ToResource;
            SetTarget(node.transform.position);
        }

        /// Send this villager to build (or help build) a construction site. More villagers on a site
        /// build it faster; the villager returns to idle when it's done.
        public void Build(Construction site)
        {
            if (site == null || site.Complete) return;
            LeaveWork();
            _site = site;
            _state = State.ToBuild;
            SetTarget(site.transform.position);
        }

        public override void MoveTo(Vector3 pos)
        {
            LeaveWork();
            _state = State.Idle;
            _node = null;
            SetTarget(pos);
        }

        // Drop any current job (release a node's worker slot / a site's builder slot) before taking a new one.
        void LeaveWork()
        {
            if (_node != null) _node.ReleaseWorker(this);
            if (_site != null) { _site.RemoveBuilder(this); _site = null; }
        }

        protected override void Update()
        {
            switch (_state)
            {
                case State.Idle:
                    MoveStep();
                    break;

                case State.ToResource:
                    if (_node == null) { _state = State.Idle; break; }
                    if (MoveStep()) { _state = State.Gathering; _gatherTimer = 0f; }
                    break;

                case State.Gathering:
                    if (_node == null) { ReturnToDropoff(); break; }
                    _gatherTimer += Time.deltaTime;
                    if (_gatherTimer >= GatherInterval) { _gatherTimer = 0f; _carry += _node.Extract(1); }
                    if (_carry >= Capacity || _node == null) ReturnToDropoff();
                    break;

                case State.ToDropoff:
                    if (MoveStep())
                    {
                        if (_carry > 0)
                        {
                            var e = Match.Econ(Owner);
                            if (e != null) e.Add(_carryType, _carry);
                            _carry = 0;
                        }
                        if (_node != null && _node.Amount > 0) { _state = State.ToResource; SetTarget(_node.transform.position); }
                        else GoIdleNear();
                    }
                    break;

                case State.ToBuild:
                    if (_site == null || _site.Complete) { GoIdleNear(); break; }
                    if (MoveStep()) { _site.AddBuilder(this); _state = State.Building; }
                    break;

                case State.Building:
                    if (_site == null || _site.Complete) GoIdleNear(); // done (or site gone) -> idle
                    break;
            }

            if (_anim != null)
            {
                _anim.SetGathering(_state == State.Gathering || _state == State.Building);
                _anim.SetCarrying(_carry > 0, ResColor(_carryType));
            }
        }

        void ReturnToDropoff()
        {
            var tc = TownCentre.Nearest(Owner, transform.position);
            if (tc == null) { _state = State.Idle; return; }
            SetTarget(tc.transform.position);
            _state = State.ToDropoff;
        }

        // When a villager has no more work, step it a few units off the drop-off to a clear spot so
        // idle villagers stay visible on the map (not stacked inside the Town Centre) and easy to click.
        void GoIdleNear()
        {
            LeaveWork();
            _node = null;
            _state = State.Idle;
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir == Vector2.zero) dir = Vector2.right;
            SetTarget(transform.position + new Vector3(dir.x, 0f, dir.y) * Random.Range(3f, 5.5f));
        }
    }
}
