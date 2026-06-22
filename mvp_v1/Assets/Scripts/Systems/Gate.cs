using System;
using GoblinSiege.Core;
using GoblinSiege.Units;
using UnityEngine;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// A breachable gate/wall segment. Goblins damage it by attacking; Sappers breach it
    /// far faster. When destroyed it opens the path and spikes the alarm (spec section 3).
    /// </summary>
    public class Gate : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float hp = 100f;
        [SerializeField] private float breachRadius = 1.5f;

        /// <summary>Fires while damaged. Args: (this gate, hp fraction 0..1).</summary>
        public event Action<Gate, float> OnDamaged;

        /// <summary>Fires once when breached.</summary>
        public event Action<Gate> OnBreached;

        public bool IsAlive => hp > 0f;
        public bool IsBreached => hp <= 0f;

        public void Init(float hitPoints)
        {
            maxHp = Mathf.Max(1f, hitPoints);
            hp = maxHp;
        }

        /// <summary>
        /// Sapper-driven breaching: applies steady breach damage while a sapper is in range.
        /// Returns true the moment the gate breaks.
        /// </summary>
        public bool TickSapperBreach(float deltaTime)
        {
            if (IsBreached) return false;
            GoblinUnit nearest = FindSapperInRange();
            if (nearest == null) return false;

            // Whole-bar breach over Balance.GateBreachSeconds.
            float dps = maxHp / Balance.GateBreachSeconds;
            return TakeDamage(dps * deltaTime);
        }

        public bool TakeDamage(float amount)
        {
            if (IsBreached) return false;
            hp -= amount;
            if (hp <= 0f)
            {
                hp = 0f;
                OnDamaged?.Invoke(this, 0f);
                OnBreached?.Invoke(this);
                return true;
            }
            OnDamaged?.Invoke(this, hp / maxHp);
            return false;
        }

        private GoblinUnit FindSapperInRange()
        {
            Unit nearest = CombatRegistry.FindNearestGoblin(transform.position);
            if (nearest is not GoblinUnit g || !g.IsSapper) return null;
            // XZ-plane breach range (G2).
            float sqr = CombatRegistry.FlatSqr(g.transform.position, transform.position);
            return sqr <= breachRadius * breachRadius ? g : null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, breachRadius);
        }
    }
}
