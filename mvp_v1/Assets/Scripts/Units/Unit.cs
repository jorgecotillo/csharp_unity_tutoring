using System;
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

        public Team Team => team;
        public bool IsAlive => hp > 0f;
        public float Hp => hp;
        public float MaxHp => maxHp;

        /// <summary>Fires when this unit dies. Arg: this unit.</summary>
        public event Action<Unit> OnDied;

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Body.gravityScale = 0f;
            Body.freezeRotation = true;
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
            return false;
        }

        protected virtual void Die()
        {
            OnDied?.Invoke(this);
        }

        protected virtual void OnEnable() => CombatRegistry.Register(this);
        protected virtual void OnDisable() => CombatRegistry.Unregister(this);
    }
}
