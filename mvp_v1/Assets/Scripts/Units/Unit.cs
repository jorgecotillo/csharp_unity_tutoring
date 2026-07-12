using System;
using System.Collections;
using GoblinSiege.Core;
using UnityEngine;

namespace GoblinSiege.Units
{
    /// <summary>
    /// Base for every combatant (goblin or human). Holds HP, stats, and simple
    /// auto-attack-the-nearest-enemy behaviour. Movement targets are set by squad
    /// orders (goblins) or the FSM AI (humans).
    ///
    /// 3D MIGRATION (3D_MIGRATION_SPEC Phase A): this used to be a 2D unit
    /// (Rigidbody2D / SpriteRenderer / Vector2). It is now a full-3D unit that lives
    /// on a FLAT XZ ground plane:
    ///   • Physics body is a 3D <see cref="Rigidbody"/> with gravity off and Y frozen,
    ///     so units glide on the plane and never tip over (G2 "flat gameplay").
    ///   • All distance/targeting math ignores Y (see <see cref="CombatRegistry.FlatSqr"/>),
    ///     so a tall model is never considered "farther" than a short one (G2).
    ///   • Movement happens in <see cref="FixedUpdate"/> via <c>Body.linearVelocity</c>
    ///     (frame-rate independent — G5). No per-frame transform nudging.
    ///   • Per-instance tinting (hit-flash, Guard/Alert) is done with a
    ///     MaterialPropertyBlock so the role's SHARED material stays shared and we
    ///     never allocate a material per frame (G3).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public abstract class Unit : MonoBehaviour, IDamageable
    {
        [SerializeField] protected Team team;
        [SerializeField] protected float maxHp = 30f;
        [SerializeField] protected float hp = 30f;
        [SerializeField] protected float moveSpeed = 3f;
        [SerializeField] protected float damage = 6f;
        [SerializeField] protected float attackRange = 1.2f;
        [SerializeField] protected float attackInterval = 1f;

        // 3D physics body (was Rigidbody2D). Movement is velocity-driven on XZ.
        protected Rigidbody Body;
        // Move destination is now a 3D world point; only its XZ matters (G2).
        protected Vector3 MoveTarget;
        protected bool HasMoveTarget;
        private float _attackTimer;
        private Unit _currentEnemy;

        // ─────────────────────────────────────────────────────────────────────
        // VISUAL TINTING (replaces the old SpriteRenderer hit-flash)
        // ─────────────────────────────────────────────────────────────────────
        // In 3D the body is a primitive MeshRenderer (or, later, an art prefab)
        // sharing ONE material per role (G3). To tint a SINGLE instance — the
        // white hit-flash, or a human's Guard-dim / Alert-bright state — we use a
        // MaterialPropertyBlock. An MPB overrides shader properties for THIS
        // renderer only, with NO new Material allocation, so the shared material
        // stays shared and there are zero per-frame allocations (G3).
        //
        // We set BOTH "_Color" (Built-in Standard) and "_BaseColor" (URP/Lit) so
        // the tint works regardless of render pipeline.
        // ─────────────────────────────────────────────────────────────────────
        protected Renderer BodyRenderer;
        private MaterialPropertyBlock _mpb;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // The unit's current logical color (role tint or Guard/Alert tint). The
        // hit-flash restores to THIS after flashing white, so it never clobbers a
        // human's Alert tint. Defaults to white until the spawner calls SetVisualTint.
        private Color _displayColor = Color.white;

        // Hit-flash bookkeeping (see StartHitFlash).
        private Coroutine _hitFlashCoroutine;
        private Color _hitFlashBaseColor;

        public Team Team => team;
        public bool IsAlive => hp > 0f;
        public float Hp => hp;
        public float MaxHp => maxHp;

        // ─────────────────────────────────────────────────────────────────────
        // Subclass hook for objective counting (unchanged from 2D).
        // The Warlord proxy overrides this to false so it never counts as a
        // looter/extractor (see WarlordUnit + INonObjectiveRaider).
        // ─────────────────────────────────────────────────────────────────────
        protected virtual bool CountsAsRaiderObjective => true;

        /// <summary>Fires when this unit dies. Arg: this unit.</summary>
        public event Action<Unit> OnDied;

        protected virtual void Awake()
        {
            // 3D body setup (was: gravityScale = 0, freezeRotation = true on a Rigidbody2D).
            // GET-OR-ADD the body — do NOT rely on [RequireComponent] here. Units are
            // built at runtime by AddComponent-ing this script onto an already-active
            // primitive (the VisualLibrary fallback). Runtime AddComponent does not
            // reliably add a [RequireComponent] dependency declared on this ABSTRACT
            // BASE class before Awake runs, which left Body null and threw a
            // MissingComponentException ("no 'Rigidbody' attached"). Get-or-add is
            // bulletproof across the primitive path, art prefabs, and the editor.
            Body = GetComponent<Rigidbody>();
            if (Body == null) Body = gameObject.AddComponent<Rigidbody>();
            Body.useGravity = false;
            // Keep units glued to the XZ plane and upright: freeze vertical motion
            // and tipping (X/Z rotation), but ALLOW yaw (Y rotation) so they can
            // face their travel direction. This is the heart of G2 "flat gameplay".
            Body.constraints = RigidbodyConstraints.FreezePositionY
                             | RigidbodyConstraints.FreezeRotationX
                             | RigidbodyConstraints.FreezeRotationZ;
            Body.interpolation = RigidbodyInterpolation.Interpolate; // smooth visuals
            // Bug fix (goblins passing through walls): ContinuousDynamic sweeps the
            // body against STATIC wall/gate colliders between physics steps so a unit
            // can never seep through a thin (0.45 m) palisade. The previous
            // ContinuousSpeculative mode is known to let bodies with a frozen-Y
            // position constraint ghost through thin static geometry — exactly our
            // case (G2 freezes Y). Dynamic sweeping is the reliable fix.
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Add a CapsuleCollider so units are physically blocked by walls and gates.
            // Get-or-add: reuse the primitive's existing collider if present.
            // VisualLibrary keeps colliders on unit primitives (stripCollider:false for
            // Goblin/Human/Warlord) so GetComponent always succeeds on the fallback
            // path. Art prefabs may or may not carry one — the null branch handles that.
            // This avoids DestroyImmediate (unreliable during Awake in play mode) and
            // the old Object.Destroy deferred-race that let goblins pass through walls.
            var cap = GetComponent<CapsuleCollider>();
            if (cap == null) cap = gameObject.AddComponent<CapsuleCollider>();
            // Scale collider to WORLD-space dimensions so a 0.55× goblin primitive and
            // a 0.95× Warlord both get the same physical footprint (~0.35 m radius).
            Vector3 s = transform.localScale;
            float sx = Mathf.Max(s.x, s.z, 0.01f);
            float sy = Mathf.Max(s.y, 0.01f);
            cap.radius = 0.35f / sx;                   // 0.35 m world radius
            cap.height = 1.8f  / sy;                   // 1.8 m world height
            cap.center = new Vector3(0f, 0.9f / sy, 0f); // base sits on the ground
            gameObject.layer = 8; // "Units" layer
            Physics.IgnoreLayerCollision(8, 8, true); // units slide past each other; walls still block

            // Cache the renderer (MeshRenderer for primitives) and an MPB for tinting.
            BodyRenderer = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        protected void ApplyStats(Team unitTeam, UnitStats stats)
        {
            team = unitTeam;
            maxHp = stats.MaxHp;
            hp = stats.MaxHp;
            damage = stats.Damage;
            moveSpeed = stats.Speed;
            attackRange = stats.AttackRange;
            attackInterval = stats.AttackInterval;
            _attackTimer = 0f;
            _currentEnemy = null;
            HasMoveTarget = false;
        }

        /// <summary>
        /// Sets this unit's base display color (role tint). The spawner calls this
        /// right after Init so the hit-flash has a correct color to restore to and
        /// the readability language (G4) is applied per-instance via MPB.
        /// </summary>
        public void SetVisualTint(Color c)
        {
            _displayColor = c;
            PushColor(c);
        }

        // Order to a world point. Only XZ is used by movement (G2); we keep the
        // full Vector3 so callers can pass transform.position directly.
        public void OrderMoveTo(Vector3 worldPos)
        {
            MoveTarget = worldPos;
            HasMoveTarget = true;
        }

        protected virtual void FixedUpdate()
        {
            if (!IsAlive) return;
            AcquireEnemyIfNeeded();

            if (_currentEnemy != null && InAttackRange(_currentEnemy))
            {
                Body.linearVelocity = Vector3.zero;
                TickAttack();
                return;
            }

            // Pick a destination: chase the enemy, else honor a move order, else stay put.
            Vector3 dest = _currentEnemy != null ? _currentEnemy.transform.position
                         : HasMoveTarget ? MoveTarget
                         : transform.position;

            // Travel only on XZ — zero the Y delta so vertical offset (model height,
            // ground rest height) never affects movement (G2).
            Vector3 toDest = dest - transform.position;
            toDest.y = 0f;

            if (toDest.sqrMagnitude > 0.01f)
            {
                Vector3 dir = toDest.normalized;
                Body.linearVelocity = new Vector3(dir.x, 0f, dir.z) * moveSpeed;
                FaceDirection(dir);
            }
            else
            {
                Body.linearVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Cheap yaw-only facing: rotate the body to look along its flat travel
        /// direction. LookRotation of an XZ vector yields a pure Y rotation, which
        /// is exactly what the FreezeRotationX/Z constraints permit.
        /// </summary>
        private void FaceDirection(Vector3 flatDir)
        {
            if (flatDir.sqrMagnitude < 0.0001f) return;
            Body.MoveRotation(Quaternion.LookRotation(new Vector3(flatDir.x, 0f, flatDir.z), Vector3.up));
        }

        private void AcquireEnemyIfNeeded()
        {
            if (_currentEnemy != null && _currentEnemy.IsAlive) return;
            _currentEnemy = CombatRegistry.FindNearestEnemy(this);
        }

        private bool InAttackRange(Unit enemy)
        {
            float r = attackRange + 0.4f;
            // XZ distance only (G2): a tall human is not "out of range" vertically.
            return CombatRegistry.FlatSqr(enemy.transform.position, transform.position) <= r * r;
        }

        private void TickAttack()
        {
            _attackTimer -= Time.fixedDeltaTime;
            if (_attackTimer > 0f) return;
            _attackTimer = attackInterval;
            if (_currentEnemy != null && _currentEnemy.IsAlive)
                _currentEnemy.TakeDamage(damage);
        }

        public virtual bool TakeDamage(float amount)
        {
            if (!IsAlive) return false;
            hp -= amount;
            if (hp <= 0f)
            {
                hp = 0f;
                Die();
                return true;
            }

            // Survived → flash white briefly for combat readability (T3).
            StartHitFlash();
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Hit-flash: turn the body white for ~0.08s then restore the display color.
        // Robust to overlapping hits: if a flash is already running we keep the
        // ORIGINAL base color (never re-capture "white"), restart the timer.
        // Null-safe for units with no renderer.
        // ─────────────────────────────────────────────────────────────────────
        private void StartHitFlash()
        {
            if (BodyRenderer == null) return;

            if (_hitFlashCoroutine == null)
                _hitFlashBaseColor = _displayColor;  // capture the real tint to restore to
            else
                StopCoroutine(_hitFlashCoroutine);   // keep the existing _hitFlashBaseColor

            _hitFlashCoroutine = StartCoroutine(HitFlashCoroutine());
        }

        private IEnumerator HitFlashCoroutine()
        {
            const float flashDuration = 0.08f;

            PushColor(Color.white);

            // Frame-rate-independent wait (G5).
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            PushColor(_hitFlashBaseColor);
            _hitFlashCoroutine = null;
        }

        /// <summary>
        /// Pushes a color onto this single renderer via MaterialPropertyBlock
        /// (no material allocation — G3). Sets both Standard and URP color slots.
        /// </summary>
        private void PushColor(Color c)
        {
            if (BodyRenderer == null) return;
            // Do NOT tint real skinned character models (Paladin/Maw) — a persistent
            // color multiply would wipe out their textures, which the user explicitly
            // wants to keep ("I want to add visual art so it looks nice"). State cues
            // for models come from the UnitAlertIndicator ring instead. Primitive
            // fallbacks (MeshRenderer) still tint for the G4 readability language.
            if (BodyRenderer is SkinnedMeshRenderer) return;
            BodyRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorId, c);
            _mpb.SetColor(BaseColorId, c);
            BodyRenderer.SetPropertyBlock(_mpb);
        }

        protected virtual void Die()
        {
            // Fire OnDied FIRST so RaidManager/alarm/score logic reacts immediately,
            // before the visual animation (keeps existing game-logic timing intact).
            OnDied?.Invoke(this);

            // Stop all movement and disable physics so the corpse stays put.
            if (Body != null)
            {
                Body.linearVelocity = Vector3.zero;
                Body.isKinematic = true;
            }
            // Disable the collider so dead bodies don't block living units.
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            StartCoroutine(DeathCoroutine());
        }

        /// <summary>
        /// Death animation: the unit topples forward (rotates 90° on X) over 0.3s,
        /// lies on the ground for 2s so the player can see the fallen enemy, then
        /// deactivates. Frame-rate independent (G5).
        /// </summary>
        private IEnumerator DeathCoroutine()
        {
            const float tiltDuration = 0.3f;
            const float lingerDuration = 2.0f;

            // Topple: rotate 90° forward in the unit's local space.
            Quaternion startRot = transform.rotation;
            Quaternion endRot = startRot * Quaternion.Euler(90f, 0f, 0f);
            float elapsed = 0f;
            while (elapsed < tiltDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / tiltDuration);
                transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
            transform.rotation = endRot;

            // Lie on the ground so the player clearly sees the defeated enemy.
            yield return new WaitForSeconds(lingerDuration);

            gameObject.SetActive(false);
        }

        protected virtual void OnEnable() => CombatRegistry.Register(this);
        protected virtual void OnDisable() => CombatRegistry.Unregister(this);
    }
}
