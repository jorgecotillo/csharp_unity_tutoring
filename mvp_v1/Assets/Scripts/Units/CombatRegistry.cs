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

        /// <summary>Nearest living unit on the opposing team, or null.</summary>
        public static Unit FindNearestEnemy(Unit self)
        {
            Unit best = null;
            float bestSqr = float.MaxValue;
            Vector2 from = self.transform.position;

            for (int i = 0; i < Units.Count; i++)
            {
                Unit u = Units[i];
                if (u == null || !u.IsAlive || u.Team == self.Team) continue;
                float sqr = ((Vector2)u.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = u;
                }
            }
            return best;
        }

        public static int CountAlive(Team team)
        {
            int n = 0;
            for (int i = 0; i < Units.Count; i++)
                if (Units[i] != null && Units[i].IsAlive && Units[i].Team == team) n++;
            return n;
        }

        /// <summary>Nearest living goblin to a world position, or null.</summary>
        public static Unit FindNearestGoblin(Vector2 from)
        {
            Unit best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < Units.Count; i++)
            {
                Unit u = Units[i];
                if (u == null || !u.IsAlive || u.Team != Team.Goblin) continue;
                float sqr = ((Vector2)u.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = u;
                }
            }
            return best;
        }
    }
}
