using UnityEngine;

namespace GoblinSiege.Units
{
    // ═══════════════════════════════════════════════════════════════════════════
    // UnitAlertIndicator — Guard-vs-Alert readability for the TEXTURED Paladin.
    // (3D_MIGRATION_SPEC Phase C, "T2 state tint" — textured-model variant)
    // ═══════════════════════════════════════════════════════════════════════════
    // THE PROBLEM
    //   On the cheap primitive capsule, we showed AI state by tinting the whole body
    //   (dim grey-red = Guard, bright red = Alert). That reads great on a flat-colour
    //   capsule. But multiplying a fully-TEXTURED Paladin by red just muddies the art
    //   and reads poorly. Yet the player STILL must instantly see "this guard just
    //   woke up and is charging me".
    //
    // THE SOLUTION (cheap + clear, G3-friendly)
    //   A small RED GROUND RING under the unit that is HIDDEN while it passively
    //   guards and GLOWS while it is alerted. A ground ring is an arcade-classic
    //   "this unit is active/targeted" cue (think RTS selection rings). It does not
    //   fight the character textures, stays readable from the tilted RTS camera (G1),
    //   and is a single tiny emissive disc.
    //
    //   As a bonus we also try to push an emissive boost onto the body via a
    //   MaterialPropertyBlock, but the ring is the PRIMARY, always-reliable signal
    //   (emission on the body only shows if the body material has the _EMISSION
    //   keyword enabled, which we cannot force from an MPB).
    //
    // ─────────────────────────────────────────────────────────────────────────────
    // GUARDRAIL G3 — PERFORMANCE
    //   • SetAlerted is called ONLY on AI state transitions (Guard↔Alert), never per
    //     frame. There is no Update on this component at all.
    //   • The ring is a primitive disc with its collider stripped and ONE shared-ish
    //     emissive material; toggling it is just SetActive — no allocation.
    //   • The optional body emissive uses a single reused MaterialPropertyBlock — no
    //     new Material is ever created (mirrors the tinting strategy in Unit.cs).
    //
    // WIRING
    //   The Editor prefab builder (Editor/PaladinSetup.cs) creates the ring child,
    //   assigns it to <see cref="ringObject"/>, and finds the body renderer. HumanUnit
    //   calls SetAlerted(false/true) from its Guard/Alert states.
    // ═══════════════════════════════════════════════════════════════════════════
    public sealed class UnitAlertIndicator : MonoBehaviour
    {
        [Tooltip("Flat red disc placed at the unit's feet. Hidden while guarding, shown while alerted.")]
        [SerializeField] private GameObject ringObject;

        [Tooltip("Body renderer (SkinnedMeshRenderer) to receive an optional emissive boost when alerted.")]
        [SerializeField] private Renderer bodyRenderer;

        // Reused property block so the optional emissive boost allocates nothing (G3).
        private MaterialPropertyBlock _mpb;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        // The emissive colour pushed onto the body while alerted (a restrained red so
        // the Paladin art still reads; the ring carries the loud signal).
        private static readonly Color AlertEmission = new(0.40f, 0.02f, 0.02f);

        private bool _alerted;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();

            // Auto-resolve the body renderer if the prefab builder didn't assign one.
            if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<Renderer>();

            // Start in the calm/guard look.
            ApplyVisual(false);
        }

        /// <summary>
        /// Allows the prefab builder (or any spawner) to wire references at runtime.
        /// </summary>
        public void Configure(GameObject ring, Renderer body)
        {
            ringObject = ring;
            bodyRenderer = body;
        }

        /// <summary>
        /// Toggle the alerted look. Called by HumanUnit on Guard↔Alert transitions
        /// only (NOT per frame) — keeps it G3-cheap.
        /// </summary>
        public void SetAlerted(bool on)
        {
            if (_alerted == on) return; // no redundant work
            _alerted = on;
            ApplyVisual(on);
        }

        private void ApplyVisual(bool alerted)
        {
            // 1) The reliable signal: show/hide the red ground ring.
            if (ringObject != null) ringObject.SetActive(alerted);

            // 2) Bonus: nudge body emission via MPB (no material allocation, G3).
            //    Only renders if the body material has emission enabled; harmless
            //    otherwise. Null-safe for primitive fallbacks.
            if (bodyRenderer != null)
            {
                bodyRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissionColorId, alerted ? AlertEmission : Color.black);
                bodyRenderer.SetPropertyBlock(_mpb);
            }
        }
    }
}
