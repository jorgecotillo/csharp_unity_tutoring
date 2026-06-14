using System;
using System.Collections.Generic;
using GoblinSiege.Core;
using UnityEngine;

namespace GoblinSiege.Units
{
    /// <summary>
    /// A goblin squad: a handful of <see cref="GoblinUnit"/>s the player commands as one.
    /// The player issues orders to squads (bands), not to individual units (spec section 9).
    /// A squad is "destroyed" when all its members are dead.
    /// </summary>
    public class Squad : MonoBehaviour
    {
        [SerializeField] private GoblinType squadType = GoblinType.Grunt;
        [SerializeField] private List<GoblinUnit> members = new();
        [SerializeField] private float formationSpacing = 0.7f;

        private bool _selected;

        public GoblinType SquadType => squadType;
        public bool IsSelected => _selected;
        public bool IsDestroyed { get; private set; }
        public IReadOnlyList<GoblinUnit> Members => members;

        /// <summary>Fires when the squad loses its last member. Arg: this squad.</summary>
        public event Action<Squad> OnDestroyed;

        /// <summary>
        /// Spawn <paramref name="count"/> goblins of <paramref name="type"/> as this squad's members.
        /// </summary>
        public void Build(GoblinType type, int count, GoblinUnit unitPrefab, Vector2 origin)
        {
            squadType = type;
            members.Clear();
            IsDestroyed = false;

            for (int i = 0; i < count; i++)
            {
                Vector2 offset = FormationOffset(i, count);
                GoblinUnit unit = Instantiate(unitPrefab, origin + offset, Quaternion.identity, transform);
                unit.Init(type);
                unit.OnDied += HandleMemberDied;
                members.Add(unit);
            }
        }

        private Vector2 FormationOffset(int index, int count)
        {
            int perRow = Mathf.CeilToInt(Mathf.Sqrt(count));
            int row = index / perRow;
            int col = index % perRow;
            float cx = (perRow - 1) * 0.5f;
            return new Vector2((col - cx) * formationSpacing, (row - cx) * formationSpacing);
        }

        public void SetSelected(bool selected) => _selected = selected;

        /// <summary>Order every living member toward a destination in formation.</summary>
        public void OrderMoveTo(Vector2 worldPos)
        {
            int alive = 0;
            for (int i = 0; i < members.Count; i++)
                if (members[i] != null && members[i].IsAlive) alive++;

            int placed = 0;
            for (int i = 0; i < members.Count; i++)
            {
                GoblinUnit m = members[i];
                if (m == null || !m.IsAlive) continue;
                m.OrderMoveTo(worldPos + FormationOffset(placed, Mathf.Max(1, alive)));
                placed++;
            }
        }

        public Vector2 CenterOfMass()
        {
            Vector2 sum = Vector2.zero;
            int alive = 0;
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i] == null || !members[i].IsAlive) continue;
                sum += (Vector2)members[i].transform.position;
                alive++;
            }
            return alive > 0 ? sum / alive : (Vector2)transform.position;
        }

        public int AliveCount()
        {
            int n = 0;
            for (int i = 0; i < members.Count; i++)
                if (members[i] != null && members[i].IsAlive) n++;
            return n;
        }

        private void HandleMemberDied(Unit _)
        {
            if (IsDestroyed) return;
            if (AliveCount() > 0) return;
            IsDestroyed = true;
            OnDestroyed?.Invoke(this);
        }
    }
}
