using UnityEngine;
using UnityEngine.InputSystem;

namespace GoblinSiege.Player
{
    /// <summary>
    /// Smooth scroll-wheel zoom for the RTS camera (GUARDRAIL G1 preserved).
    /// Changes the camera's Field of View — the tilt angle, position, and top-down
    /// RTS feel are NEVER touched, so the readable board layout stays intact.
    ///
    /// Controls:
    ///   Scroll wheel up  = zoom in  (smaller FOV)
    ///   Scroll wheel down = zoom out (larger FOV)
    ///
    /// GUARDRAIL G5: all time-dependent values use Time.deltaTime — frame-rate safe.
    /// WebGL-SAFE: only Camera, Mathf, and the new Input System (Mouse/scroll).
    /// </summary>
    public class CameraZoom : MonoBehaviour
    {
        [Tooltip("Minimum FOV (zoomed all the way in).")]
        [SerializeField] private float minFov = 20f;

        [Tooltip("Maximum FOV (zoomed all the way out).")]
        [SerializeField] private float maxFov = 80f;

        [Tooltip("How many FOV degrees each scroll notch moves the zoom target.")]
        [SerializeField] private float scrollSensitivity = 4f;

        [Tooltip("How fast the FOV lerps toward the target (higher = snappier).")]
        [SerializeField] private float smoothSpeed = 10f;

        private Camera _cam;
        private float  _targetFov;

        // Input System scroll action — built at runtime so no separate asset needed.
        private InputAction _scrollAction;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;

            _targetFov = _cam != null ? _cam.fieldOfView : 56f;

            // Bind mouse scroll without requiring a full InputActionAsset.
            _scrollAction = new InputAction("CameraZoom", InputActionType.Value,
                binding: "<Mouse>/scroll/y", expectedControlType: "Axis");
            _scrollAction.Enable();
        }

        private void OnDestroy()
        {
            _scrollAction?.Disable();
            _scrollAction?.Dispose();
        }

        private void Update()
        {
            if (_cam == null) return;

            // Read scroll input and move the FOV target.
            float scroll = _scrollAction.ReadValue<float>();
            if (Mathf.Abs(scroll) > 0.01f)
            {
                // scroll > 0 = wheel up = zoom IN = FOV decreases.
                _targetFov -= scroll * scrollSensitivity * 0.01f; // raw scroll is ±120
                _targetFov  = Mathf.Clamp(_targetFov, minFov, maxFov);
            }

            // Smooth the camera toward the target FOV (G5 — Time.deltaTime).
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, _targetFov,
                                           Time.deltaTime * smoothSpeed);
        }

        /// <summary>
        /// Called by RaidBootstrap to initialise with the camera's starting FOV
        /// so there's no pop on the first frame.
        /// </summary>
        public void SetInitialFov(float fov)
        {
            _targetFov = Mathf.Clamp(fov, minFov, maxFov);
            if (_cam != null) _cam.fieldOfView = _targetFov;
        }
    }
}
