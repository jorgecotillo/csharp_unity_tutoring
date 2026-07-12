namespace GoblinSiege.Core
{
    /// <summary>
    /// Static tuning values from the design spec (warren_game_design_v2.md, section 6).
    /// One place to balance the economy. Playtest starting points, NOT gospel.
    /// </summary>
    public static class Balance
    {
        // daemon-test marker (temporary; reverted after measuring build time)
        // --- Loot caches: gold reward and alarm cost ---
        public static int CacheGold(CacheType type) => type switch
        {
            CacheType.Crate => 10,
            CacheType.Chest => 25,
            CacheType.Granary => 40,
            CacheType.Vault => 100,
            _ => 0
        };

        /// <summary>Alarm added (in percent points, 0-100) when this cache is fully looted.</summary>
        public static float CacheAlarmCost(CacheType type) => type switch
        {
            CacheType.Crate => 3f,
            CacheType.Chest => 6f,
            CacheType.Granary => 8f,
            CacheType.Vault => 20f,
            _ => 0f
        };

        public static float CacheLootSeconds(CacheType type) => type switch
        {
            CacheType.Crate => 2f,
            CacheType.Chest => 3f,
            CacheType.Granary => 4f,
            CacheType.Vault => 6f,
            _ => 2f
        };

        // --- Alarm spikes from combat / breaching ---
        public const float AlarmPerHumanKilled = 2f;
        public const float AlarmPerGateBreached = 8f;

        // --- Goblin unit stats (per individual; a squad is several of these) ---
        public static UnitStats GoblinStats(GoblinType type) => type switch
        {
            GoblinType.Grunt => new UnitStats(hp: 30, damage: 6, speed: 3f, attackRange: 1.2f, attackInterval: 1f),
            GoblinType.Spearthrower => new UnitStats(hp: 20, damage: 8, speed: 2.5f, attackRange: 5f, attackInterval: 1.4f),
            GoblinType.Sapper => new UnitStats(hp: 18, damage: 4, speed: 2f, attackRange: 1.2f, attackInterval: 1.2f),
            _ => new UnitStats(30, 6, 3f, 1.2f, 1f)
        };

        // --- Human garrison stats ---
        public static UnitStats HumanStats(HumanType type) => type switch
        {
            HumanType.Militia => new UnitStats(hp: 40, damage: 8, speed: 2.6f, attackRange: 1.2f, attackInterval: 1f),
            HumanType.Crossbow => new UnitStats(hp: 25, damage: 12, speed: 2.2f, attackRange: 6f, attackInterval: 1.6f),
            HumanType.Pikeman => new UnitStats(hp: 45, damage: 10, speed: 2.2f, attackRange: 1.6f, attackInterval: 1.1f),
            _ => new UnitStats(40, 8, 2.6f, 1.2f, 1f)
        };

        // --- Sapper breach ---
        public const float GateBreachSeconds = 8f;

        // --- Starting war-band ---
        // Cap of 8 so Warren can build a big horde (recruit up to 8 squads).
        // The starting band still fields 4 (2 Grunt + 1 Spearthrower + 1 Sapper).
        public const int StartingSquadCap = 8;
        public const int UnitsPerSquad = 5;

        // --- War-Camp costs (spent from surplus gold) ---
        public const int RecruitSquadCost = 60;
        public const int ReviveSquadCost = 40;

        // --- Alarm thresholds (percent) ---
        public const float AlertedAt = 34f;
        public const float MobilizedAt = 67f;
        public const float FullSallyAt = 100f;

        public static AlarmThreshold ThresholdFor(float alarmPercent)
        {
            if (alarmPercent >= FullSallyAt) return AlarmThreshold.FullSally;
            if (alarmPercent >= MobilizedAt) return AlarmThreshold.Mobilized;
            if (alarmPercent >= AlertedAt) return AlarmThreshold.Alerted;
            return AlarmThreshold.Unaware;
        }
    }

    /// <summary>Immutable stat block for a unit.</summary>
    public readonly struct UnitStats
    {
        public readonly float MaxHp;
        public readonly float Damage;
        public readonly float Speed;
        public readonly float AttackRange;
        public readonly float AttackInterval;

        public UnitStats(float hp, float damage, float speed, float attackRange, float attackInterval)
        {
            MaxHp = hp;
            Damage = damage;
            Speed = speed;
            AttackRange = attackRange;
            AttackInterval = attackInterval;
        }
    }
}
