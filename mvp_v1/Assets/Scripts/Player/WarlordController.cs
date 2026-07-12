using GoblinSiege.Systems;
using GoblinSiege.UI;
using GoblinSiege.Units;
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
        private InputAction _slashAction;
        private Vector2 _moveInput;

        // ── Warlord's mighty melee SLASH ─────────────────────────────────────
        // A powerful, wide sweep that carves through every human standing in a
        // cone in front of the Warlord. It's the player's hands-on combat button
        // (Spacebar). The Warlord isn't a passive commander any more — lead the
        // charge and cut down the garrison yourself! ⚔️
        //
        // Numbers are deliberately BIG so the slash feels heroic: 50 damage one-
        // shots every human type (25–45 HP), 3.2m reach is far longer than a
        // normal 1.2m melee, and the ~120° cone lets one swing hit a whole cluster.
        // A short cooldown keeps it from being spammed into a lawnmower.
        [SerializeField] private float slashDamage      = 50f;   // one-shots every human
        [SerializeField] private float slashRange       = 3.2f;  // metres of reach
        [SerializeField] private float slashCooldown    = 0.6f;  // seconds between swings
        private const float SlashHalfAngleCos = 0.5f;            // cos(60°) → a 120° cone
        private const float SlashDuration     = 0.28f;           // swing animation length
        private const float SlashLungeDist    = 0.45f;           // forward step during the swing
        private const float SlashStretch      = 0.18f;           // squash-stretch "oomph"

        private WarlordUnit _warlordUnit;   // combat proxy on this same GameObject
        private ScreenShake _screenShake;   // cached camera shake for slash punch
        private float _slashCooldownTimer;
        private bool _slashing;
        private float _slashTimer;
        private Vector3 _slashStartPos;
        private Vector3 _slashDir;
        private Vector3 _slashStartScale;

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

            // The combat proxy lives on this same GameObject (added by RaidBootstrap).
            // We need it as the "attacker" identity for the slash so it only hits the
            // Warlord's enemies (humans), never friendly goblins.
            _warlordUnit = GetComponent<WarlordUnit>();
        }

        private void OnEnable()
        {
            _moveAction = _input.actions["Move"];
            _warhornAction = _input.actions["Warhorn"];
            _slashAction = _input.actions["Slash"];

            _moveAction.performed += OnMove;
            _moveAction.canceled += OnMove;
            _warhornAction.performed += OnWarhorn;
            _slashAction.performed += OnSlash;
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
            if (_slashAction != null)
                _slashAction.performed -= OnSlash;
        }

        private void OnMove(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();

        private void OnWarhorn(InputAction.CallbackContext ctx)
        {
            if (WarhornUsed || alarm == null) return;
            WarhornUsed = true;
            alarm.Reduce(warhornAlarmReduction);
        }

        // ── SLASH! The Warlord's powerful melee sweep ────────────────────────
        // Fires on Spacebar. Respects a short cooldown and won't interrupt the
        // open-door gesture. Damage lands the instant you swing (a wide forward
        // cone), then a quick lunge+stretch animation sells the impact, plus a
        // meaty BOOM and a camera shake so it feels devastating.
        private void OnSlash(InputAction.CallbackContext ctx)
        {
            if (_openingDoor || _slashing) return;
            if (_slashCooldownTimer > 0f) return;
            if (_warlordUnit == null || !_warlordUnit.IsAlive) return;

            _slashCooldownTimer = slashCooldown;

            // Swing in the direction the Warlord is facing (his travel/last facing).
            Vector3 dir = transform.forward; dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            dir.Normalize();

            // Carve every enemy in the forward cone. Damage is applied right away.
            CombatRegistry.DamageEnemiesInArc(_warlordUnit, dir, slashRange, SlashHalfAngleCos, slashDamage);

            // Juice: a heavy swing sound and a punchy camera shake.
            SfxManager.PlayExplosion(0.7f);
            EnsureScreenShake();
            _screenShake?.Shake();

            // Kick off the visible swing animation (see TickSlash).
            _slashStartPos   = _body != null ? _body.position : transform.position;
            _slashStartScale = transform.localScale;
            _slashDir        = dir;
            _slashTimer      = 0f;
            _slashing        = true;
        }

        // Lazily find the camera's ScreenShake once (cheap, and optional — the
        // slash still works fine if the scene has no ScreenShake component).
        private void EnsureScreenShake()
        {
            if (_screenShake == null)
                _screenShake = FindFirstObjectByType<ScreenShake>();
        }

        private void FixedUpdate()
        {
            // Tick down the slash cooldown every physics step (frame-rate safe, G5).
            if (_slashCooldownTimer > 0f) _slashCooldownTimer -= Time.fixedDeltaTime;

            // While heaving the door open, ignore movement input and play the
            // scripted gesture instead (see BeginDoorOpen / TickDoorOpen).
            if (_openingDoor)
            {
                TickDoorOpen();
                return;
            }

            // While mid-swing, play the slash animation and hold movement.
            if (_slashing)
            {
                TickSlash();
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
                // Stop idle spinning: the body allows yaw (Y rotation) so it can face
                // its travel direction, but that also lets a bump from a wall/door/human
                // leave leftover angular velocity that keeps the Warlord slowly spinning
                // forever while standing still. Zero it out whenever he's idle.
                _body.angularVelocity = Vector3.zero;
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

        // Advances the slash swing one physics step. Physics-legal like the door
        // gesture: a sin-curve forward lunge via MovePosition (returns to start —
        // net-zero displacement), keeps facing the swing direction, and a quick
        // squash-stretch "oomph". Damage was already applied on the button press
        // (OnSlash) — this is pure visual feedback. Restores the pose when done.
        private void TickSlash()
        {
            _body.linearVelocity = Vector3.zero; // driven by MovePosition, not velocity

            _slashTimer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(_slashTimer / SlashDuration);
            float pulse = Mathf.Sin(t * Mathf.PI); // 0 → 1 → 0: one crisp swing

            _body.MovePosition(_slashStartPos + _slashDir * (pulse * SlashLungeDist));
            _body.MoveRotation(Quaternion.LookRotation(_slashDir, Vector3.up));
            transform.localScale = _slashStartScale * (1f + pulse * SlashStretch);

            if (_slashTimer >= SlashDuration)
            {
                _body.MovePosition(_slashStartPos);
                transform.localScale = _slashStartScale;
                _slashing = false;
            }
        }
    }
}
