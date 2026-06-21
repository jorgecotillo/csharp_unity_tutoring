using System;
using System.Collections;
using GoblinSiege.Core;
using UnityEngine;

namespace GoblinSiege.Units
{
    /// <summary>
    /// Base for every combatant (goblin or human). Holds HP, stats, and simple
    /// auto-attack-the-nearest-enemy behaviour. Movement targets are set by squad
    /// orders (goblins) or the FSM AI (humans). 2D, top-down, WebGL-safe.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class Unit : MonoBehaviour, IDamageable
    {
        [SerializeField] protected Team team;
        [SerializeField] protected float maxHp = 30f;
        [SerializeField] protected float hp = 30f;
        [SerializeField] protected float moveSpeed = 3f;
        [SerializeField] protected float damage = 6f;
        [SerializeField] protected float attackRange = 1.2f;
        [SerializeField] protected float attackInterval = 1f;

        protected Rigidbody2D Body;
        protected Vector2 MoveTarget;
        protected bool HasMoveTarget;
        private float _attackTimer;
        private Unit _currentEnemy;

        // ─────────────────────────────────────────────────────────────────────
        // CHANGE A: Cached SpriteRenderer for visual effects (hit-flash, etc.)
        // Nullable because a unit prefab COULD theoretically lack a sprite.
        // ─────────────────────────────────────────────────────────────────────
        protected SpriteRenderer Sprite;

        // ─────────────────────────────────────────────────────────────────────
        // CHANGE B: Hit-flash bookkeeping.
        // We store the running coroutine handle AND the base color captured
        // BEFORE we turned it white. If a second hit lands mid-flash, we must
        // NOT re-capture "white" as the restore color — we keep the original
        // and just restart the timer.
        // ─────────────────────────────────────────────────────────────────────
        private Coroutine _hitFlashCoroutine;
        private Color _hitFlashBaseColor;

        public Team Team => team;
        public bool IsAlive => hp > 0f;
        public float Hp => hp;
        public float MaxHp => maxHp;

        // ─────────────────────────────────────────────────────────────────────
        // CHANGE D (T5#3): Subclass hook for objective counting.
        // Most raiders (goblins) count toward the "looters/extractors" win
        // condition tracked by CombatRegistry.FindNearestGoblin. However, the
        // Warlord proxy (a purely-combatant unit) should NOT count — if it did,
        // standing in the extraction zone would trigger a false win.
        // Subclasses override this to false to opt out.
        // ─────────────────────────────────────────────────────────────────────
        protected virtual bool CountsAsRaiderObjective => true;

        /// <summary>Fires when this unit dies. Arg: this unit.</summary>
        public event Action<Unit> OnDied;

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Body.gravityScale = 0f;
            Body.freezeRotation = true;

            // ─────────────────────────────────────────────────────────────────
            // CHANGE A: Cache the SpriteRenderer for hit-flash and death-pop.
            // GetComponent returns null if none exists — that's fine; all the
            // visual-effect code guards with `if (Sprite == null) return`.
            // ─────────────────────────────────────────────────────────────────
            Sprite = GetComponent<SpriteRenderer>();
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

        public void OrderMoveTo(Vector2 worldPos)
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
                Body.linearVelocity = Vector2.zero;
                TickAttack();
                return;
            }

            Vector2 dest = _currentEnemy != null ? (Vector2)_currentEnemy.transform.position
                         : HasMoveTarget ? MoveTarget
                         : (Vector2)transform.position;

            Vector2 toDest = dest - (Vector2)transform.position;
            if (toDest.sqrMagnitude > 0.01f)
                Body.linearVelocity = toDest.normalized * moveSpeed;
            else
                Body.linearVelocity = Vector2.zero;
        }

        private void AcquireEnemyIfNeeded()
        {
            if (_currentEnemy != null && _currentEnemy.IsAlive) return;
            _currentEnemy = CombatRegistry.FindNearestEnemy(this);
        }

        private bool InAttackRange(Unit enemy)
        {
            float r = attackRange + 0.4f;
            return ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude <= r * r;
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

            // ─────────────────────────────────────────────────────────────────
            // CHANGE B (T3): Hit-flash for combat readability.
            // Flash the sprite white for ~0.08s then restore. We ONLY flash if
            // the unit survived (we reach this code only if hp > 0 after damage).
            //
            // ROBUSTNESS: If a second hit lands while we're mid-flash, we must
            // NOT capture "white" as the restore color. So we:
            //   1. If no flash is running → capture current color as base.
            //   2. If a flash IS running → stop it, but KEEP the old base color.
            //   3. Start a fresh flash coroutine either way.
            // At the end of the flash, we restore _hitFlashBaseColor and clear
            // the handle so the next hit knows no flash is active.
            // ─────────────────────────────────────────────────────────────────
            StartHitFlash();

            return false;
        }

        /// <summary>
        /// Begins (or restarts) the hit-flash effect. Null-safe for units
        /// without a SpriteRenderer.
        /// </summary>
        private void StartHitFlash()
        {
            // Guard: no sprite → nothing to flash.
            if (Sprite == null) return;

            if (_hitFlashCoroutine == null)
            {
                // No flash in progress → capture current color as the base.
                // This preserves T2's alert-tint (grey-red or bright-red) that
                // the human FSM may have applied.
                _hitFlashBaseColor = Sprite.color;
            }
            else
            {
                // Flash already running → stop it but KEEP _hitFlashBaseColor
                // so we don't accidentally capture "white".
                StopCoroutine(_hitFlashCoroutine);
            }

            _hitFlashCoroutine = StartCoroutine(HitFlashCoroutine());
        }

        /// <summary>
        /// Coroutine: turn sprite white, wait ~0.08s, restore base color.
        /// </summary>
        private IEnumerator HitFlashCoroutine()
        {
            const float flashDuration = 0.08f;

            // Turn white (full alpha so the flash is visible).
            Sprite.color = Color.white;

            // Accumulate deltaTime (frame-rate independent, WebGL-safe).
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Restore to the color we captured BEFORE flashing, so we don't
            // clobber the human's Alert tint (T2) or any other dynamic tint.
            Sprite.color = _hitFlashBaseColor;

            // Clear handle so next hit knows no flash is active.
            _hitFlashCoroutine = null;
        }

        protected virtual void Die()
        {
            // ─────────────────────────────────────────────────────────────────
            // CHANGE C (T4): Death-pop for "kills feel like kills".
            //
            // IMPORTANT: Fire OnDied FIRST so RaidManager/alarm/score logic
            // reacts immediately (before we start the visual pop). This keeps
            // existing game-logic timing intact.
            // ─────────────────────────────────────────────────────────────────
            OnDied?.Invoke(this);

            // ─────────────────────────────────────────────────────────────────
            // Start the death-pop coroutine: shrink to zero over ~0.12s, then
            // deactivate the GameObject. Deactivating is safe — nothing reuses
            // dead unit instances; Squad and GarrisonSpawner always Instantiate
            // fresh units. If the object is destroyed mid-coroutine, Unity
            // automatically stops the coroutine (no NRE risk).
            // ─────────────────────────────────────────────────────────────────
            StartCoroutine(DeathPopCoroutine());
        }

        /// <summary>
        /// Coroutine: scale down to zero over ~0.12s, then deactivate.
        /// </summary>
        private IEnumerator DeathPopCoroutine()
        {
            const float popDuration = 0.12f;

            // Capture the starting scale at coroutine start.
            Vector3 startScale = transform.localScale;

            // Accumulate deltaTime for frame-rate-independent lerp.
            float elapsed = 0f;
            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            // Ensure final scale is exactly zero (floating-point safety).
            transform.localScale = Vector3.zero;

            // Deactivate the GameObject. This is the "deactivate as today"
            // behavior — previously corpses sat inert; now they vanish cleanly.
            gameObject.SetActive(false);
        }

        protected virtual void OnEnable() => CombatRegistry.Register(this);
        protected virtual void OnDisable() => CombatRegistry.Unregister(this);
    }
}
