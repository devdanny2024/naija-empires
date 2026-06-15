using UnityEngine;

namespace NaijaEmpires
{
    /// Gatherer: walk to a resource, harvest until full, carry back to the nearest friendly
    /// Town Centre, repeat. Deposits into its own faction's economy.
    public class Villager : Unit
    {
        enum State { Idle, ToResource, Gathering, ToDropoff }

        State _state = State.Idle;
        ResourceNode _node;
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
            _node = node;
            _carryType = node.Type;
            _state = State.ToResource;
            SetTarget(node.transform.position);
        }

        public override void MoveTo(Vector3 pos)
        {
            _state = State.Idle;
            _node = null;
            SetTarget(pos);
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
                        else _state = State.Idle;
                    }
                    break;
            }

            if (_anim != null)
            {
                _anim.SetGathering(_state == State.Gathering);
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
    }
}
