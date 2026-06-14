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
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput))]
    public class WarlordController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float warhornAlarmReduction = 10f;
        [SerializeField] private AlarmSystem alarm;

        private Rigidbody2D _body;
        private PlayerInput _input;
        private InputAction _moveAction;
        private InputAction _warhornAction;
        private Vector2 _moveInput;

        public bool WarhornUsed { get; private set; }

        /// <summary>Runtime injection for RaidBootstrap (no scene wiring).</summary>
        public void SetAlarm(AlarmSystem alarmSys) => alarm = alarmSys;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
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
            _body.linearVelocity = _moveInput.sqrMagnitude > 0.01f
                ? _moveInput.normalized * moveSpeed
                : Vector2.zero;
        }
    }
}
