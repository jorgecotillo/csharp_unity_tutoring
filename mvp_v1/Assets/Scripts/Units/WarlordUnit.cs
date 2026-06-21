// ═══════════════════════════════════════════════════════════════════════════
// WarlordUnit.cs — T5 (Standout Mechanic): Combat Proxy for the Warlord
// ═══════════════════════════════════════════════════════════════════════════
// DESIGN GOAL:
// The player's Warlord becomes a valid combat TARGET — humans will chase and
// attack whichever is closest: a goblin squad OR the Warlord. If the Warlord
// dies, the raid ends in defeat ("leaderless warband scatters").
//
// WHY A SEPARATE COMPONENT?
// WarlordController handles input-driven movement and camera control. That's
// a player-input concern. This WarlordUnit component makes the Warlord
// participate in the combat system: it inherits Unit so CombatRegistry sees it,
// it's on Team.Goblin so humans consider it an enemy via FindNearestEnemy, and
// it implements IDamageable so attacks deal damage and can kill it.
//
// CRITICAL CORRECTNESS REQUIREMENT (the subtle bug this code must NOT introduce):
// The Warlord is on Team.Goblin so humans see it as a target. But it must NOT
// count toward loot/extraction/win queries:
//   - LootCache.HasGoblinInRange uses FindNearestGoblin to detect nearby looters
//   - ExtractionZone.AnyGoblinInside checks for goblins to trigger the win
//   - RaidManager.CheckWin fires only if AnyGoblinInside is true
// If FindNearestGoblin returned the Warlord, the player could WIN just by
// standing the Warlord in the extraction zone with quota met — that's a bug.
//
// SOLUTION: We override CountsAsRaiderObjective => false (line 59 of Unit.cs).
// However, that property is `protected`, so CombatRegistry (a separate class)
// cannot read it directly. Since we cannot edit Unit.cs (another lane owns it),
// we expose the concept publicly via a MARKER INTERFACE: INonObjectiveRaider.
// CombatRegistry.FindNearestGoblin skips any unit implementing this interface.
// This is type-safe, extensible (future non-objective raiders just implement
// the interface), and requires no changes to Unit.cs.
// ═══════════════════════════════════════════════════════════════════════════

using GoblinSiege.Core;
using GoblinSiege.Systems;
using UnityEngine;

namespace GoblinSiege.Units
{
    // ═══════════════════════════════════════════════════════════════════════
    // MARKER INTERFACE: INonObjectiveRaider
    // ═══════════════════════════════════════════════════════════════════════
    // Units on Team.Goblin that implement this interface are EXCLUDED from
    // FindNearestGoblin queries (loot range, extraction zone, win checks).
    // They still appear in FindNearestEnemy (humans can target them).
    //
    // WHY NOT USE A PUBLIC PROPERTY ON UNIT?
    // Unit.CountsAsRaiderObjective is `protected virtual` (line 59 of Unit.cs).
    // We cannot edit Unit.cs — it's owned by another development lane and
    // already committed. A marker interface is the cleanest compile-legal
    // solution: WarlordUnit implements it, CombatRegistry checks for it.
    // ═══════════════════════════════════════════════════════════════════════
    public interface INonObjectiveRaider
    {
        // Marker interface — no members needed. The presence of this interface
        // on a Unit signals "skip me in objective queries (loot, extraction)."
    }

    /// <summary>
    /// Combat proxy attached to the player's Warlord GameObject (alongside
    /// WarlordController). Makes the Warlord a valid target for human attacks
    /// and enables the "Warlord down = defeat" lose condition.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: This component does NOT handle movement or input. The
    /// WarlordController (a separate MonoBehaviour, implemented in lane #7)
    /// owns input-driven movement. This component only participates in combat:
    ///   1. Registers in CombatRegistry so humans can find/target it.
    ///   2. Receives damage via IDamageable (inherited from Unit).
    ///   3. Fires OnDied when HP reaches zero, which RaidManager listens to.
    ///   4. Implements INonObjectiveRaider so loot/extraction ignore it.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D))]
    public class WarlordUnit : Unit, INonObjectiveRaider
    {
        // ═══════════════════════════════════════════════════════════════════
        // CountsAsRaiderObjective OVERRIDE (the crux of T5 correctness)
        // ═══════════════════════════════════════════════════════════════════
        // Base class (Unit) returns true by default — regular goblins count
        // toward loot proximity and extraction zone checks. The Warlord must
        // NOT count (see header comment). We return false here.
        //
        // NOTE: CombatRegistry cannot see this protected property directly.
        // That's why we ALSO implement INonObjectiveRaider above — the
        // registry checks for that interface. This override exists for:
        //   1. Semantic clarity (self-documenting code)
        //   2. Future-proofing if Unit ever exposes a public accessor
        //   3. Consistency with the design intent in Unit.cs comments
        // ═══════════════════════════════════════════════════════════════════
        protected override bool CountsAsRaiderObjective => false;

        // ═══════════════════════════════════════════════════════════════════
        // FixedUpdate OVERRIDE (intentionally empty)
        // ═══════════════════════════════════════════════════════════════════
        // Base Unit.FixedUpdate does three things:
        //   1. Acquires the nearest enemy and chases it
        //   2. Stops and attacks when in range
        //   3. Otherwise moves toward MoveTarget
        //
        // The Warlord must NOT do any of this:
        //   - Movement is controlled by WarlordController (player input)
        //   - The Warlord has 0 damage and never attacks
        //   - We don't want auto-chase behavior overriding player intent
        //
        // By overriding and leaving the body empty (no base.FixedUpdate call),
        // we disable all automatic movement/attack behavior. The Warlord
        // stands still unless the player moves it via WarlordController.
        // ═══════════════════════════════════════════════════════════════════
        protected override void FixedUpdate()
        {
            // Intentionally empty: WarlordController owns movement.
            // DO NOT call base.FixedUpdate() — that would enable auto-chase.
        }

        // ═══════════════════════════════════════════════════════════════════
        // Init — Called by RaidBootstrap (lane #7) after instantiation
        // ═══════════════════════════════════════════════════════════════════
        // Sets up the Warlord's combat stats:
        //   - Team.Goblin: so humans treat it as an enemy (FindNearestEnemy)
        //   - 50 HP: survivable but not invincible; positioning matters
        //   - 0 damage: the Warlord commands, it doesn't fight directly
        //   - 0 speed: movement is external (WarlordController)
        //   - 0 attackRange: no auto-attack (reinforces 0 damage)
        //   - 1f attackInterval: doesn't matter at 0 damage, but non-zero
        //     avoids division-by-zero edge cases in base class timer logic
        //
        // WHY NOT SET IN INSPECTOR?
        // The Warlord prefab may be used in non-combat contexts (tutorials,
        // menus). Explicit Init() keeps combat participation opt-in and
        // makes the setup visible in code rather than hidden in prefab data.
        // ═══════════════════════════════════════════════════════════════════
        public void Init()
        {
            // ApplyStats is a protected method inherited from Unit.
            // It sets team, maxHp, hp, damage, moveSpeed, attackRange, and attackInterval.
            //
            // UnitStats constructor (from Balance.cs):
            //   UnitStats(float hp, float damage, float speed, float attackRange, float attackInterval)
            ApplyStats(
                Team.Goblin,
                new UnitStats(
                    hp: 50f,            // Warlord has moderate HP — not a tank, not glass
                    damage: 0f,         // Cannot deal damage (leader, not fighter)
                    speed: 0f,          // Movement is external (WarlordController)
                    attackRange: 0f,    // No auto-attack
                    attackInterval: 1f  // Placeholder (non-zero to avoid edge cases)
                )
            );
        }

        // ═══════════════════════════════════════════════════════════════════
        // Die OVERRIDE — Notify RaidManager when the Warlord falls
        // ═══════════════════════════════════════════════════════════════════
        // Base Unit.Die fires OnDied and starts the death-pop coroutine.
        // We call base.Die() to preserve that behavior (subscribers like
        // RaidBootstrap or debug listeners may want the event), then ALSO
        // notify RaidManager directly to trigger the LostWarlordDown result.
        //
        // WHY NOTIFY RAIDMANAGER DIRECTLY?
        // RaidManager already listens to Squad.OnDestroyed for squad wipes.
        // For the Warlord, we could either:
        //   A) Have RaidBootstrap subscribe to OnDied and relay to RaidManager
        //   B) Have WarlordUnit call RaidManager.NotifyWarlordDown directly
        //
        // Option B is simpler and keeps the "Warlord death = defeat" logic
        // explicit in this class rather than scattered across files. The
        // coupling to RaidManager is acceptable — this is a game-critical
        // event, not a generic "some unit died" callback.
        //
        // SAFETY: FindFirstObjectByType is a Unity 6 API (not the deprecated
        // FindObjectOfType). It's called once on death, not per-frame.
        // ═══════════════════════════════════════════════════════════════════
        protected override void Die()
        {
            // Let base class fire OnDied event and run death-pop animation.
            base.Die();

            // Notify RaidManager that the Warlord fell — triggers LostWarlordDown.
            // FindFirstObjectByType is Unity 6's replacement for FindObjectOfType.
            // It's acceptable here: called once on death, not per-frame.
            RaidManager raidManager = FindFirstObjectByType<RaidManager>();
            if (raidManager != null)
            {
                raidManager.NotifyWarlordDown();
            }
            else
            {
                // No RaidManager in scene (e.g., testing in isolation).
                // Log a warning but don't crash — the death-pop still runs.
                Debug.LogWarning("[WarlordUnit] Warlord died but no RaidManager found to notify.");
            }
        }
    }
}
