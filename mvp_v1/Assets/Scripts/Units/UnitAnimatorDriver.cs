using UnityEngine;

namespace GoblinSiege.Units
{
    // ═══════════════════════════════════════════════════════════════════════════
    // UnitAnimatorDriver — turns physics velocity into an animation blend value.
    // (3D_MIGRATION_SPEC Phase C, "Do (code side)")
    // ═══════════════════════════════════════════════════════════════════════════
    // WHAT IT DOES
    //   Every frame it reads the unit's ACTUAL Rigidbody speed and feeds it to the
    //   Animator as a float parameter named "Speed". The Paladin's AnimatorController
    //   (PaladinAnimator.controller) uses that one number to blend Idle → Walk → Run.
    //   So when the gameplay code (Unit.FixedUpdate) makes the body move fast, the
    //   Paladin runs; when it stops to attack, it idles. The animation is purely
    //   COSMETIC — it never moves the unit (movement is physics-driven, root motion
    //   is OFF on the prefab's Animator).
    //
    // ─────────────────────────────────────────────────────────────────────────────
    // GUARDRAIL G5 — FRAME-RATE INDEPENDENCE (this is the whole reason this class
    // exists as its own component):
    //   The "Speed" we send is derived from REAL velocity in metres/second
    //   (Rigidbody.linearVelocity.magnitude). It is NOT a per-frame counter, NOT a
    //   "+= someStep each Update". Velocity is already a per-second quantity that the
    //   physics engine maintains in FixedUpdate, so reading it here is correct at any
    //   frame-rate. A 30 fps machine and a 144 fps machine compute the SAME blend.
    //
    //   We also OPTIONALLY damp the value for a smooth blend, and that damping is fed
    //   Time.deltaTime — so even the smoothing is frame-rate independent. (Animator.
    //   SetFloat's built-in damping is the standard frame-rate-safe way to do this.)
    //
    // ─────────────────────────────────────────────────────────────────────────────
    // GUARDRAIL G3 — WEBGL PERFORMANCE:
    //   • References (Animator, Rigidbody) are cached ONCE in Awake. No GetComponent,
    //     no Find* in Update (no per-frame allocations or lookups).
    //   • The "Speed" parameter name is hashed ONCE into a static int. Passing the
    //     hash to SetFloat avoids a string lookup (and the tiny GC pressure that the
    //     string overload can cause) every single frame.
    //   • If there is no Animator (e.g. the cheap primitive-capsule human used when
    //     no Paladin prefab is present), we simply DISABLE this component in Awake so
    //     Update never runs at all — zero cost on the fallback path.
    //
    // ─────────────────────────────────────────────────────────────────────────────
    // NULL-SAFETY / ART-SWAP SEAM:
    //   The primitive fallback human has a Rigidbody but NO Animator. This driver is
    //   designed to ride along harmlessly in that case: it detects the missing
    //   Animator and switches itself off. So the SAME gameplay works whether the
    //   "Human" visual is the animated Paladin prefab or a red capsule (§2 seam).
    // ═══════════════════════════════════════════════════════════════════════════
    [RequireComponent(typeof(Rigidbody))]
    public sealed class UnitAnimatorDriver : MonoBehaviour
    {
        // The float parameter the AnimatorController blends on. Must match the name
        // used when the controller is built (see Editor/PaladinSetup.cs).
        public const string SpeedParam = "Speed";

        // Hash the parameter name ONCE (G3). Animator.SetFloat(int,…) skips the
        // per-call string→hash lookup that the string overload would do every frame.
        private static readonly int SpeedHash = Animator.StringToHash(SpeedParam);

        // How quickly the blend value chases the real speed, in seconds. A small
        // damp removes the visual "pop" when velocity snaps from 0 to full (our
        // movement sets linearVelocity directly). Purely cosmetic; frame-rate safe
        // because we pass Time.deltaTime to SetFloat below.
        [SerializeField] private float blendDampTime = 0.10f;

        // Cached references (G3 — looked up once, never in Update).
        private Animator _animator;
        private Rigidbody _body;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();

            // The Animator typically lives on the imported Paladin root (same object
            // as this component on the prefab), but search children too so this also
            // works if the rig is nested. GetComponentInChildren includes self.
            _animator = GetComponentInChildren<Animator>();

            // NULL-SAFE FAST PATH: primitive humans have no Animator. Switch this
            // component OFF so Update is never even invoked (cheapest possible — G3).
            if (_animator == null)
            {
                enabled = false;
                return;
            }

            // Animation is cosmetic; movement is physics-driven. Guarantee root
            // motion can never fight the Rigidbody, regardless of how the prefab/clip
            // was authored (defensive — keeps G2 flat-plane motion authoritative).
            _animator.applyRootMotion = false;
        }

        private void Update()
        {
            // Defensive: an Animator can be destroyed (death-pop deactivates the
            // object) — bail quietly rather than throw. (Unity-null check.)
            if (_animator == null) return;

            // ── G5: blend value comes straight from real physics velocity ──────────
            // linearVelocity is in metres/second and is maintained by the physics
            // step, so this is correct at ANY frame-rate. We never accumulate a
            // per-frame counter. We ignore vertical speed (there is none — Y is
            // frozen — but zeroing it keeps us honest about "flat XZ speed", G2).
            Vector3 v = _body.linearVelocity;
            float planarSpeed = new Vector2(v.x, v.z).magnitude;

            // SetFloat with a damp time + Time.deltaTime is Unity's standard
            // frame-rate-independent smoothing. The hash overload avoids per-frame
            // string work (G3).
            _animator.SetFloat(SpeedHash, planarSpeed, blendDampTime, Time.deltaTime);
        }
    }
}
