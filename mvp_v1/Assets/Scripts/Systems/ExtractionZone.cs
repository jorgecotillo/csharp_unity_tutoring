using System;
using GoblinSiege.Units;
using UnityEngine;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// The edge of the map the player must reach to BANK looted gold and win (spec section 5).
    /// Touching it with the quota met ends the raid in victory. Reaching it without the quota
    /// simply does nothing (the player must loot more or risk the alarm).
    /// </summary>
    public class ExtractionZone : MonoBehaviour
    {
        [SerializeField] private float radius = 2.5f;

        /// <summary>True if any living goblin is inside the extraction zone.</summary>
        public bool AnyGoblinInside()
        {
            Unit nearest = CombatRegistry.FindNearestGoblin(transform.position);
            if (nearest == null) return false;
            return ((Vector2)nearest.transform.position - (Vector2)transform.position).sqrMagnitude
                   <= radius * radius;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
