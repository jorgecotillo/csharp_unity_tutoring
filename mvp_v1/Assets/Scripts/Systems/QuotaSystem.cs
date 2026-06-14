using System;
using UnityEngine;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// Tracks gold looted this raid against the level quota (spec section 5).
    /// The win number. Banking (reaching extraction) is handled by RaidManager —
    /// this system only knows how much has been looted and whether quota is met.
    /// </summary>
    public class QuotaSystem : MonoBehaviour
    {
        [SerializeField] private int quota = 100;
        [SerializeField] private int looted;

        /// <summary>Fires when looted gold changes. Args: (looted, quota).</summary>
        public event Action<int, int> OnGoldChanged;

        /// <summary>Fires once when the quota is first reached.</summary>
        public event Action OnQuotaReached;

        public int Quota => quota;
        public int Looted => looted;
        public int Surplus => Mathf.Max(0, looted - quota);
        public bool QuotaMet => looted >= quota;

        private bool _reachedFired;

        public void Configure(int levelQuota)
        {
            quota = Mathf.Max(1, levelQuota);
            looted = 0;
            _reachedFired = false;
            OnGoldChanged?.Invoke(looted, quota);
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            looted += amount;
            OnGoldChanged?.Invoke(looted, quota);

            if (QuotaMet && !_reachedFired)
            {
                _reachedFired = true;
                OnQuotaReached?.Invoke();
            }
        }
    }
}
