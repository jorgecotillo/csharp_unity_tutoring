using GoblinSiege.Systems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GoblinSiege.Player
{
    /// <summary>
    /// The Warlord hero (spec section 3). Moves with the new Input System and owns the
    /// once-per-raid "Warhorn" — the player's single safety valve that drops the alarm.
    /// NEW INPUT SYSTEM ONLY: all input flows through a PlayerInput component's actions.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInput))]
    public class WarlordController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float warhornAlarmReduction = 10f;
        [SerializeField] private AlarmSystem alarm;

        private Rigidbody _body;
        private PlayerInput _input;
        private InputAction _moveAction;
        private InputAction _warhornAction;
        private Vector2 _moveInput;

        // ── "Open the door" gesture ──────────────────────────────────────────
        // The Warlord heaves the barracks door open, and ONLY THEN does the fight
        // begin (the human garrison wakes — see Door + CombatGate + HumanUnit).
        // The animation is a short, art-agnostic, physics-legal move: face the
        // door, a forward "push" lunge (a sin curve, so it returns to the start —
        // net-zero displacement), plus a gentle effort-stretch. Works on the cyan
        // capsule fallback AND any future model, and is WebGL-safe (no Animator
        // clips required).
        private bool _openingDoor;
        private float _openTimer;
        private Vector3 _openStartPos;
        private Vector3 _openDir;
        private Vector3 _openStartScale;
        private const float OpenGestureDuration = 0.5f;  // seconds
        private const float OpenPushDistance    = 0.5f;  // metres the Warlord lunges at the door
        private const float OpenStretchAmount   = 0.12f; // squash-stretch "effort" pulse (12%)

        /// <summary>True while the Warlord is playing the open-door animation.</summary>
        public bool IsOpeningDoor => _openingDoor;

        public bool WarhornUsed { get; private set; }

        /// <summary>Runtime injection for RaidBootstrap (no scene wiring).</summary>
        public void SetAlarm(AlarmSystem alarmSys) => alarm = alarmSys;

        private void Awake()
        {
            // 3D MIGRATION: the hero body is now a 3D Rigidbody on the flat XZ plane.
            // Same constraints as every Unit (gravity off, Y position frozen, no
            // tipping; yaw allowed for facing) so the Warlord glides on the board.
            // Get-or-add (see Unit.Awake): don't depend on [RequireComponent] firing
            // for a runtime-built object — guarantees the body exists before use.
            _body = GetComponent<Rigidbody>();
            if (_body == null) _body = gameObject.AddComponent<Rigidbody>();
            _body.useGravity = false;
            _body.constraints = RigidbodyConstraints.FreezePositionY
                              | RigidbodyConstraints.FreezeRotationX
                              | RigidbodyConstraints.FreezeRotationZ;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            // Bug fix (pass-through): ContinuousDynamic sweeps the Warlord against
            // static wall/gate/door colliders so he is always physically blocked,
            // instead of the old ContinuousSpeculative mode that could let a
            // frozen-Y body slip through thin barriers. Matches Unit.cs.
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Add a CapsuleCollider so the Warlord is physically blocked by walls,
            // gates, and doors — same setup as Unit.Awake (G2 flat gameplay).
            var cap = GetComponent<CapsuleCollider>();
            if (cap == null) cap = gameObject.AddComponent<CapsuleCollider>();
            cap.radius = 0.35f;
            cap.height = 1.8f;
            cap.center = new Vector3(0f, 0.9f, 0f);
            // Layer 8 = "Units": unit-vs-unit collision is globally ignored, but
            // static scene geometry (layer 0) still blocks the Warlord (G2).
            gameObject.layer = 8;

            _input = GetComponent<PlayerInput>();
        }

        private void OnEnable()
        {
            _moveAction = _input.actions["Move"];
            _warhornAction = _input.actions["Warhorn"];

            _moveAction.performed += OnMove;
            _moveAction.canceled += OnMove;
            _warhornAction.performed += OnWarhorn;
        }

        private void OnDisable()
        {
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMove;
                _moveAction.canceled -= OnMove;
            }
            if (_warhornAction != null)
                _warhornAction.performed -= OnWarhorn;
        }

        private void OnMove(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();

        private void OnWarhorn(InputAction.CallbackContext ctx)
        {
            if (WarhornUsed || alarm == null) return;
            WarhornUsed = true;
            alarm.Reduce(warhornAlarmReduction);
        }

        private void FixedUpdate()
        {
            // While heaving the door open, ignore movement input and play the
            // scripted gesture instead (see BeginDoorOpen / TickDoorOpen).
            if (_openingDoor)
            {
                TickDoorOpen();
                return;
            }

            // WASD/arrows give a 2D input vector; map it to XZ world velocity
            // (input.x → world X, input.y → world Z). All motion is in FixedUpdate
            // and scaled by moveSpeed only — frame-rate independent (G5).
            if (_moveInput.sqrMagnitude > 0.01f)
            {
                Vector2 dir = _moveInput.normalized;
                _body.linearVelocity = new Vector3(dir.x, 0f, dir.y) * moveSpeed;
                // Cheap yaw-only facing toward travel direction.
                _body.MoveRotation(Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y), Vector3.up));
            }
            else
            {
                _body.linearVelocity = Vector3.zero;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Open-the-door animation (spec: the Warlord opens the door, THEN we fight)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Start the "heave the door open" animation, facing <paramref name="doorPos"/>.
        /// Called by <see cref="GoblinSiege.Systems.Door"/> when the Warlord walks up.
        /// During the gesture the Warlord ignores movement input, turns to the door,
        /// and does a short forward "push" lunge with a little effort-stretch — a clear,
        /// art-agnostic animation that reads as "opening the door" on any model.
        /// </summary>
        public void BeginDoorOpen(Vector3 doorPos)
        {
            if (_openingDoor) return;

            _openStartPos   = _body != null ? _body.position : transform.position;
            _openStartScale = transform.localScale;

            // Flat (XZ) direction to the door; fall back to current facing if we're
            // basically standing on it (avoids a zero-length LookRotation).
            Vector3 flat = doorPos - _openStartPos;
            flat.y = 0f;
            _openDir = flat.sqrMagnitude > 0.0001f ? flat.normalized : transform.forward;

            _openTimer   = 0f;
            _openingDoor = true;
        }

        // Advances the door-open gesture one physics step. Frame-rate independent
        // (Time.fixedDeltaTime) and physics-legal: a sin-curve forward lunge via
        // MovePosition (returns to start — net-zero), a yaw to face the door, and a
        // gentle scale "heave". Restores the pose and hands control back when done.
        private void TickDoorOpen()
        {
            _body.linearVelocity = Vector3.zero; // driven by MovePosition, not velocity

            _openTimer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(_openTimer / OpenGestureDuration);
            float pulse = Mathf.Sin(t * Mathf.PI); // 0 → 1 → 0: one smooth heave

            _body.MovePosition(_openStartPos + _openDir * (pulse * OpenPushDistance));
            _body.MoveRotation(Quaternion.LookRotation(_openDir, Vector3.up));
            transform.localScale = _openStartScale * (1f + pulse * OpenStretchAmount);

            if (_openTimer >= OpenGestureDuration)
            {
                // Snap cleanly back to the start pose; player control resumes next step.
                _body.MovePosition(_openStartPos);
                transform.localScale = _openStartScale;
                _openingDoor = false;
            }
        }
    }
}
