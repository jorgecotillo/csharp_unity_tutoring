using System.Collections.Generic;
using UnityEngine;
using GoblinSiege.Core;

namespace GoblinSiege.Units
{
    /// <summary>
    /// Lightweight global registry of living units, so combatants can find the
    /// nearest enemy without FindObjectsByType every frame. Cleared per raid.
    /// </summary>
    public static class CombatRegistry
    {
        private static readonly List<Unit> Units = new();

        public static void Register(Unit u)
        {
            if (!Units.Contains(u)) Units.Add(u);
        }

        public static void Unregister(Unit u) => Units.Remove(u);

        public static void Clear() => Units.Clear();

        // ═══════════════════════════════════════════════════════════════════
        // FlatSqr — XZ-plane squared distance (3D_MIGRATION_SPEC G2)
        // ═══════════════════════════════════════════════════════════════════
        // ALL targeting/range/proximity math in the game runs through this helper
        // so vertical offset is IGNORED. After the 3D flip, units rest at different
        // Y heights (a 1.8m human capsule vs a 1.1m goblin), and props/ground sit at
        // various Y. If we used full 3D distance, a tall model would read as
        // "farther away" than a short one standing in the same spot — breaking
        // attack ranges, loot radii, and the extraction check. Zeroing the Y delta
        // keeps every distance purely on the flat board, exactly as 2D behaved.
        // ═══════════════════════════════════════════════════════════════════
        /// <summary>Squared distance between two points on the XZ plane (Y ignored).</summary>
        public static float FlatSqr(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        // ═══════════════════════════════════════════════════════════════════
        // FindNearestEnemy — INTENTIONALLY UNCHANGED (T5)
        // ═══════════════════════════════════════════════════════════════════
        // This method finds the nearest unit on the OPPOSING team. Humans use
        // it to acquire targets. The Warlord is on Team.Goblin, so humans
        // WILL find and target the Warlord via this method. That's correct!
        // The Warlord being a valid combat target is the whole point of T5.
        //
        // DO NOT add INonObjectiveRaider filtering here — that would make
        // humans ignore the Warlord, defeating the purpose of T5.
        // ═══════════════════════════════════════════════════════════════════
        /// <summary>Nearest living unit on the opposing team, or null.</summary>
        public static Unit FindNearestEnemy(Unit self)
        {
            Unit best = null;
            float bestSqr = float.MaxValue;
            Vector3 from = self.transform.position;

            for (int i = 0; i < Units.Count; i++)
            {
                Unit u = Units[i];
                if (u == null || !u.IsAlive || u.Team == self.Team) continue;
                float sqr = FlatSqr(u.transform.position, from);
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = u;
                }
            }
            return best;
        }

        // ═══════════════════════════════════════════════════════════════════
        // DamageEnemiesInArc — the Warlord's powerful melee SLASH (player action)
        // ═══════════════════════════════════════════════════════════════════
        // Deals <paramref name="damage"/> to EVERY living enemy of the attacker
        // that stands within <paramref name="range"/> metres AND inside a forward
        // arc (a wide cone in front of the swing). The arc is defined by
        // <paramref name="minDot"/> = cos(halfAngle): 1 = straight ahead only,
        // 0 = a 180° half-circle, -1 = all around. All distance/angle math is on
        // the flat XZ plane (G2), matching the rest of the game.
        //
        // Iterating BACKWARDS is defensive: TakeDamage can kill a unit, and a
        // dying unit may later Unregister; walking the list high→low keeps our
        // indices valid regardless. Returns how many foes were struck.
        // ═══════════════════════════════════════════════════════════════════
        /// <summary>Hits every enemy of <paramref name="attacker"/> inside a forward arc. Returns hit count.</summary>
        public static int DamageEnemiesInArc(Unit attacker, Vector3 forward, float range, float minDot, float damage)
        {
            if (attacker == null) return 0;

            Vector3 origin = attacker.transform.position;
            Vector3 fwd = forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = attacker.transform.forward;
            fwd.Normalize();

            float rangeSqr = range * range;
            int hits = 0;

            for (int i = Units.Count - 1; i >= 0; i--)
            {
                Unit u = Units[i];
                if (u == null || !u.IsAlive || u.Team == attacker.Team) continue;

                Vector3 to = u.transform.position - origin; to.y = 0f;
                float sqr = to.x * to.x + to.z * to.z;
                if (sqr > rangeSqr) continue;
                // Angle test (skip when standing right on top of us to avoid NaN).
                if (sqr > 0.0001f && Vector3.Dot(fwd, to / Mathf.Sqrt(sqr)) < minDot) continue;

                u.TakeDamage(damage);
                hits++;
            }
            return hits;
        }

        // ═══════════════════════════════════════════════════════════════════
        // CountAlive — UPDATED (T5) to skip non-objective raiders
        // ═══════════════════════════════════════════════════════════════════
        // Currently unused, but defensive: if anyone counts Team.Goblin
        // alive units, the Warlord must NOT inflate that count. The Warlord
        // is a command proxy, not a combat unit that counts toward objectives.
        //
        // We check for INonObjectiveRaider (marker interface) because the
        // underlying CountsAsRaiderObjective property on Unit is `protected`
        // and inaccessible from this static class. The marker interface
        // provides a public, type-safe way to identify non-objective units
        // without editing Unit.cs.
        // ═══════════════════════════════════════════════════════════════════
        public static int CountAlive(Team team)
        {
            int n = 0;
            for (int i = 0; i < Units.Count; i++)
            {
                Unit u = Units[i];
                if (u == null || !u.IsAlive || u.Team != team) continue;

                // ───────────────────────────────────────────────────────────
                // CHANGE (T5): Skip non-objective raiders when counting goblins.
                // The Warlord is Team.Goblin but implements INonObjectiveRaider,
                // so it won't inflate goblin counts used for objective checks.
                // ───────────────────────────────────────────────────────────
                if (team == Team.Goblin && u is INonObjectiveRaider) continue;

                n++;
            }
            return n;
        }

        // ═══════════════════════════════════════════════════════════════════
        // FindNearestGoblin — UPDATED (T5) to skip non-objective raiders
        // ═══════════════════════════════════════════════════════════════════
        // This method is used by:
        //   - LootCache.HasGoblinInRange: detects nearby goblins for looting
        //   - ExtractionZone.AnyGoblinInside: checks if a goblin reached the
        //     extraction point to trigger the win condition
        //
        // CRITICAL BUG PREVENTION:
        // The Warlord is on Team.Goblin so humans target it (correct). But if
        // this method returned the Warlord, then:
        //   - LootCache would think the Warlord can loot (wrong — it can't)
        //   - ExtractionZone would trigger a win if the Warlord stands in it
        //     with quota met (wrong — only real goblin squads should win)
        //
        // SOLUTION: Skip units that implement INonObjectiveRaider. The
        // WarlordUnit class implements this marker interface, so it's
        // excluded from loot/extraction/win queries while still being
        // targetable by humans via FindNearestEnemy.
        //
        // WHY NOT CHECK CountsAsRaiderObjective DIRECTLY?
        // That property is `protected` on Unit (line 59 of Unit.cs). We
        // cannot access it from this static class, and we cannot edit
        // Unit.cs (owned by another development lane). The marker interface
        // INonObjectiveRaider provides the same semantic in a public,
        // compile-legal way.
        // ═══════════════════════════════════════════════════════════════════
        /// <summary>Nearest living goblin to a world position, or null.</summary>
        public static Unit FindNearestGoblin(Vector3 from)
        {
            Unit best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < Units.Count; i++)
            {
                Unit u = Units[i];
                if (u == null || !u.IsAlive || u.Team != Team.Goblin) continue;

                // ───────────────────────────────────────────────────────────
                // CHANGE (T5): Skip non-objective raiders (e.g., Warlord).
                // The Warlord is on Team.Goblin so humans target it, but it
                // must NOT count as a looter/extractor — skipping it here is
                // what prevents win-by-standing-in-the-extraction-zone.
                // ───────────────────────────────────────────────────────────
                if (u is INonObjectiveRaider) continue;

                float sqr = FlatSqr(u.transform.position, from);
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = u;
                }
            }
            return best;
        }

        // ═══════════════════════════════════════════════════════════════════
        // CountAttackingHumans — drives the battle music's tempo/intensity.
        // Counts living human defenders that are actively ENGAGED (Alert state,
        // once the fight has begun). The MusicManager polls this so the track
        // speeds up as more humans pile onto the player's goblins.
        // ═══════════════════════════════════════════════════════════════════
        public static int CountAttackingHumans()
        {
            int n = 0;
            for (int i = 0; i < Units.Count; i++)
                if (Units[i] is HumanUnit h && h.IsEngaged) n++;
            return n;
        }
    }
}
