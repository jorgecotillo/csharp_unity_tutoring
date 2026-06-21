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

        // ─────────────────────────────────────────────────────────────────────
        // T2: State-Tint Helper
        // ─────────────────────────────────────────────────────────────────────
        // WHY WE TINT BY STATE:
        // Visual clarity is king in an arcade game. Idle defenders blending
        // into the scenery is FINE — they're passive, not a threat yet. But
        // the moment a human "wakes up" and enters Alert, the player NEEDS
        // to see that instantly. A bright-red sprite screams "I'm coming for
        // you!" while the dull grey-red of Guard says "I'm just standing here."
        //
        // This also dovetails with the hit-flash (T3) in the base Unit class:
        // when a unit takes damage, we flash white for ~0.08s then RESTORE to
        // _hitFlashBaseColor — which is captured at flash-start. So if a human
        // is in Alert (bright red) and gets hit, the flash restores to bright
        // red. If they're in Guard (dull grey-red), the flash restores to dull.
        // The two systems cooperate without fighting over Sprite.color.
        //
        // IMPLEMENTATION:
        // Nested state classes can access protected members of the enclosing
        // instance (_h.Sprite), but keeping a small helper centralizes the
        // null-check and documents intent. Sprite is set by Unit.Awake(), so
        // it's guaranteed assigned before Init() runs (spawners call Init()
        // after Instantiate, which fires Awake). Still, we guard for safety.
        // ─────────────────────────────────────────────────────────────────────
        internal void SetTint(Color c)
        {
            if (Sprite != null) Sprite.color = c;
        }

        // --- States ---

        private class GuardState : IState
        {
            private readonly HumanUnit _h;
            public GuardState(HumanUnit h) => _h = h;

            public void Enter()
            {
                // ─────────────────────────────────────────────────────────────
                // T2: Guard tint = DIM GREY-RED (0.55, 0.28, 0.28).
                // ─────────────────────────────────────────────────────────────
                // WHY DIM: Guards standing at their posts are passive threats —
                // they won't move until provoked. A muted color lets them fade
                // into the background so the player focuses on active dangers.
                // The moment they transition to Alert, they brighten (see below).
                // ─────────────────────────────────────────────────────────────
                _h.SetTint(new Color(0.55f, 0.28f, 0.28f));

                _h.OrderMoveTo(_h.Post);
            }

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

            public void Enter()
            {
                // ─────────────────────────────────────────────────────────────
                // T2: Alert tint = BRIGHT RED (1.0, 0.2, 0.2).
                // ─────────────────────────────────────────────────────────────
                // WHY BRIGHT: The garrison just woke up — this human is now
                // ACTIVELY HUNTING the player's goblins. A vivid red screams
                // "DANGER!" and lets the player instantly distinguish charging
                // defenders from passive guards. Combined with T3's hit-flash
                // (white on damage), the battle reads clearly even in chaos.
                //
                // TEACHING MOMENT: State-based tinting is a classic arcade
                // trick (Pac-Man ghosts change color when vulnerable). It's
                // cheap (one color assignment) and enormously effective at
                // communicating AI state without UI clutter.
                // ─────────────────────────────────────────────────────────────
                _h.SetTint(new Color(1.0f, 0.2f, 0.2f));
            }

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
