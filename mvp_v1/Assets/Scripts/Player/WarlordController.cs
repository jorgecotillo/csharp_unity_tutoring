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

        public bool WarhornUsed { get; private set; }

        /// <summary>Runtime injection for RaidBootstrap (no scene wiring).</summary>
        public void SetAlarm(AlarmSystem alarmSys) => alarm = alarmSys;

        private void Awake()
        {
            // 3D MIGRATION: the hero body is now a 3D Rigidbody on the flat XZ plane.
            // Same constraints as every Unit (gravity off, Y position frozen, no
            // tipping; yaw allowed for facing) so the Warlord glides on the board.
            _body = GetComponent<Rigidbody>();
            _body.useGravity = false;
            _body.constraints = RigidbodyConstraints.FreezePositionY
                              | RigidbodyConstraints.FreezeRotationX
                              | RigidbodyConstraints.FreezeRotationZ;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
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
    }
}
