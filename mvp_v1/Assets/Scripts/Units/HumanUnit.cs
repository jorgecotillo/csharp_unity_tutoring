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

        private Vector3 _postPosition;
        private IState _current;
        private GuardState _guard;
        private AlertState _alert;
        private bool _forceAlert;

        // 3D MIGRATION (Phase C): the Paladin prefab carries a UnitAlertIndicator
        // (red ground ring) so Guard/Alert reads well on a textured model. The
        // primitive-capsule fallback has NO indicator — we detect that (null) and
        // fall back to the original flat-colour tint language (G4). Cached in Init.
        private UnitAlertIndicator _alertIndicator;

        public HumanType HumanType => humanType;

        public void Init(HumanType type, Vector3 post)
        {
            humanType = type;
            _postPosition = post;
            ApplyStats(Team.Human, Balance.HumanStats(type));

            // Cache the (optional) alert indicator BEFORE we enter the Guard state,
            // because GuardState.Enter immediately drives the state visual.
            _alertIndicator = GetComponentInChildren<UnitAlertIndicator>(true);

            _guard = new GuardState(this);
            _alert = new AlertState(this);
            TransitionTo(_guard);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Phase C: surface Guard vs Alert.
        //   • TEXTURED PALADIN (indicator present): show/hide a red ground ring and
        //     keep the body almost un-tinted (a whisper of red on Alert) so the art
        //     stays readable. Cheap — just a SetActive + one MPB push (G3).
        //   • PRIMITIVE FALLBACK (indicator null): preserve the EXACT original
        //     dim-grey-red (Guard) / bright-red (Alert) capsule tint language (G4).
        // Called only on state transitions, never per frame.
        // ─────────────────────────────────────────────────────────────────────
        internal void ApplyStateVisual(bool alerted)
        {
            if (_alertIndicator != null)
            {
                _alertIndicator.SetAlerted(alerted);
                // Subtle body tint on the textured model: white when guarding,
                // a faint warm-red when alerted. The ring carries the loud signal.
                SetVisualTint(alerted ? new Color(1f, 0.82f, 0.82f) : Color.white);
            }
            else
            {
                // Primitive capsule: original readability tints, unchanged.
                SetVisualTint(alerted ? new Color(1.0f, 0.2f, 0.2f)
                                      : new Color(0.55f, 0.28f, 0.28f));
            }
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

        internal Vector3 Post => _postPosition;
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
        // The two systems cooperate without fighting over the display color.
        //
        // IMPLEMENTATION (3D MIGRATION):
        // The old 2D version tinted a SpriteRenderer. In 3D the body is a primitive
        // MeshRenderer (or, later, an art prefab) that SHARES one material per role
        // (G3). So tinting routes through Unit.SetVisualTint, which uses a
        // MaterialPropertyBlock — a per-renderer color override with NO material
        // allocation. The shared material stays shared; only THIS human's color
        // changes. The renderer is cached in Unit.Awake() before Init() runs.
        // ─────────────────────────────────────────────────────────────────────
        internal void SetTint(Color c)
        {
            // 3D MIGRATION: tinting now routes through Unit.SetVisualTint, which
            // uses a MaterialPropertyBlock (no per-instance material allocation —
            // G3) and works on the 3D primitive's MeshRenderer. The Guard/Alert
            // readability language (G4) is preserved exactly. Null-safe if the unit
            // somehow has no renderer.
            SetVisualTint(c);
        }

        // --- States ---

        private class GuardState : IState
        {
            private readonly HumanUnit _h;
            public GuardState(HumanUnit h) => _h = h;

            public void Enter()
            {
                // T2: Guard look. On the textured Paladin this hides the red ground
                // ring and keeps the body un-tinted; on the primitive capsule it is
                // the original DIM GREY-RED. (See HumanUnit.ApplyStateVisual.)
                // WHY SUBTLE: guards at their posts are passive threats — they should
                // recede so the player focuses on active dangers, until they wake up.
                _h.ApplyStateVisual(false);

                _h.OrderMoveTo(_h.Post);
            }

            public void Execute()
            {
                Unit raider = _h.NearestRaider();
                if (raider == null) return;
                float distSqr = CombatRegistry.FlatSqr(raider.transform.position, _h.transform.position);
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
                // T2: Alert look. On the textured Paladin this LIGHTS the red ground
                // ring (loud "DANGER!" cue that doesn't muddy the art) plus a faint
                // warm body tint; on the primitive capsule it is the original BRIGHT
                // RED. (See HumanUnit.ApplyStateVisual.)
                //
                // WHY: the garrison just woke up — this human is now ACTIVELY HUNTING
                // the player's goblins, so it must read instantly. Combined with T3's
                // white hit-flash, the battle stays legible even in chaos.
                //
                // TEACHING MOMENT: state-based visual cues are a classic arcade trick
                // (Pac-Man ghosts change colour when vulnerable). Cheap, and enormously
                // effective at communicating AI state without UI clutter.
                _h.ApplyStateVisual(true);
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
                    float distSqr = CombatRegistry.FlatSqr(raider.transform.position, _h.Post);
                    float leash = _h.GuardLeash * 2.5f;
                    if (distSqr > leash * leash) _h.TransitionTo(_h._guard);
                }
            }

            public void Exit() { }
        }
    }
}
