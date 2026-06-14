using GoblinSiege.Core;
using UnityEngine;

namespace GoblinSiege.Units
{
    /// <summary>
    /// A human garrison defender with a simple FSM (spec: Guard -> Alert -> Attack).
    /// - Guard: holds near its post until a goblin comes close OR the alarm escalates.
    /// - Alert: advances toward the raiders.
    /// Actual attacking is handled by the shared auto-attack in <see cref="Unit"/>.
    /// </summary>
    public class HumanUnit : Unit
    {
        [SerializeField] private HumanType humanType = HumanType.Militia;
        [SerializeField] private float guardLeashRadius = 6f;

        private Vector2 _postPosition;
        private IState _current;
        private GuardState _guard;
        private AlertState _alert;
        private bool _forceAlert;

        public HumanType HumanType => humanType;

        public void Init(HumanType type, Vector2 post)
        {
            humanType = type;
            _postPosition = post;
            ApplyStats(Team.Human, Balance.HumanStats(type));

            _guard = new GuardState(this);
            _alert = new AlertState(this);
            TransitionTo(_guard);
        }

        /// <summary>Called when the alarm reaches Mobilized/FullSally: every human pushes the attack.</summary>
        public void ForceAlert()
        {
            _forceAlert = true;
            if (_current != _alert) TransitionTo(_alert);
        }

        protected override void FixedUpdate()
        {
            _current?.Execute();
            base.FixedUpdate();
        }

        internal void TransitionTo(IState next)
        {
            if (_current == next) return;
            _current?.Exit();
            _current = next;
            _current?.Enter();
        }

        internal Vector2 Post => _postPosition;
        internal float GuardLeash => guardLeashRadius;
        internal bool ForcedAlert => _forceAlert;
        internal Unit NearestRaider() => CombatRegistry.FindNearestEnemy(this);

        // --- States ---

        private class GuardState : IState
        {
            private readonly HumanUnit _h;
            public GuardState(HumanUnit h) => _h = h;

            public void Enter() => _h.OrderMoveTo(_h.Post);

            public void Execute()
            {
                Unit raider = _h.NearestRaider();
                if (raider == null) return;
                float distSqr = ((Vector2)raider.transform.position - (Vector2)_h.transform.position).sqrMagnitude;
                if (_h.ForcedAlert || distSqr <= _h.GuardLeash * _h.GuardLeash)
                    _h.TransitionTo(_h._alert);
            }

            public void Exit() { }
        }

        private class AlertState : IState
        {
            private readonly HumanUnit _h;
            public AlertState(HumanUnit h) => _h = h;

            public void Enter() { }

            public void Execute()
            {
                Unit raider = _h.NearestRaider();
                if (raider == null)
                {
                    if (!_h.ForcedAlert) _h.TransitionTo(_h._guard);
                    return;
                }
                _h.OrderMoveTo(raider.transform.position);

                if (!_h.ForcedAlert)
                {
                    // Disengage back to post if raiders flee far beyond the leash.
                    float distSqr = ((Vector2)raider.transform.position - _h.Post).sqrMagnitude;
                    float leash = _h.GuardLeash * 2.5f;
                    if (distSqr > leash * leash) _h.TransitionTo(_h._guard);
                }
            }

            public void Exit() { }
        }
    }
}
