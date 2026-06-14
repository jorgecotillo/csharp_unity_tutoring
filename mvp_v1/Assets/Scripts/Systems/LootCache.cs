using System;
using GoblinSiege.Core;
using GoblinSiege.Units;
using UnityEngine;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// A lootable container — the core greed-vs-alarm decision made physical (spec section 6).
    /// When a goblin squad is in range, looting progresses. On completion it grants gold
    /// AND spikes the alarm. Big caches (Vault) give the most gold but the biggest spike.
    /// </summary>
    public class LootCache : MonoBehaviour
    {
        [SerializeField] private CacheType cacheType = CacheType.Crate;
        [SerializeField] private float lootRadius = 1.5f;
        [SerializeField] private float progress;   // 0..1
        [SerializeField] private bool looted;

        /// <summary>Fires while looting. Args: (this cache, progress 0..1).</summary>
        public event Action<LootCache, float> OnLootProgress;

        /// <summary>Fires once when fully looted. Args: (gold gained, alarm added).</summary>
        public event Action<int, float> OnLooted;

        public CacheType CacheType => cacheType;
        public bool Looted => looted;
        public int Gold => Balance.CacheGold(cacheType);
        public float AlarmCost => Balance.CacheAlarmCost(cacheType);

        public void Init(CacheType type)
        {
            cacheType = type;
            progress = 0f;
            looted = false;
        }

        /// <summary>
        /// Drive looting from a controller each frame. Returns true the moment it completes.
        /// </summary>
        public bool Tick(float deltaTime, bool goblinInRange)
        {
            if (looted || !goblinInRange) return false;

            float duration = Balance.CacheLootSeconds(cacheType);
            progress += deltaTime / Mathf.Max(0.01f, duration);
            OnLootProgress?.Invoke(this, Mathf.Clamp01(progress));

            if (progress >= 1f)
            {
                looted = true;
                OnLooted?.Invoke(Gold, AlarmCost);
                return true;
            }
            return false;
        }

        /// <summary>True if any living goblin sits within loot range.</summary>
        public bool HasGoblinInRange()
        {
            Unit nearest = CombatRegistry.FindNearestGoblin(transform.position);
            if (nearest == null) return false;
            return ((Vector2)nearest.transform.position - (Vector2)transform.position).sqrMagnitude
                   <= lootRadius * lootRadius;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, lootRadius);
        }
    }
}
