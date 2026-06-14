using UnityEngine;

namespace GoblinSiege.Core
{
    /// <summary>Which side a unit fights for.</summary>
    public enum Team
    {
        Goblin,
        Human
    }

    /// <summary>Goblin war-band archetypes available in the MVP.</summary>
    public enum GoblinType
    {
        Grunt,        // line breaker
        Spearthrower, // ranged
        Sapper        // breaches gates/walls
    }

    /// <summary>Human garrison archetypes. MVP introduces them across acts.</summary>
    public enum HumanType
    {
        Militia,  // Act 1
        Crossbow, // Act 2
        Pikeman   // Act 2
    }

    /// <summary>Loot containers. Gold vs. alarm-cost is the core trade-off.</summary>
    public enum CacheType
    {
        Crate,   // 10g  / +3% alarm
        Chest,   // 25g  / +6% alarm
        Granary, // 40g  / +8% alarm
        Vault    // 100g / +20% alarm (the greed trap)
    }

    /// <summary>Garrison response escalation, driven by the Alarm meter.</summary>
    public enum AlarmThreshold
    {
        Unaware,   // 0-33%
        Alerted,   // 34-66%
        Mobilized, // 67-99%
        FullSally  // 100% (soft deadline)
    }

    /// <summary>How a raid ended.</summary>
    public enum RaidResult
    {
        InProgress,
        Won,             // quota banked at extraction
        LostSquadWipe,   // all squads destroyed
        LostAlarmMaxed   // Full Sally reached before quota banked
    }
}
