using System;
using System.Collections.Generic;
using GoblinSiege.Core;
using GoblinSiege.Visual;
using UnityEngine;

namespace GoblinSiege.Units
{
    /// <summary>
    /// A goblin squad: a handful of <see cref="GoblinUnit"/>s the player commands as one.
    /// The player issues orders to squads (bands), not to individual units (spec section 9).
    /// A squad is "destroyed" when all its members are dead.
    ///
    /// 3D MIGRATION (Phase A + B): formation math and orders moved to the flat XZ
    /// plane (G2), and members are now spawned THROUGH <see cref="VisualLibrary"/>
    /// (key "Goblin") instead of instantiating a sprite prefab — so a real goblin
    /// art prefab dropped in Resources/Prefabs/Goblin.prefab is used automatically
    /// with no code change (art seam, §2). The gameplay components (GoblinUnit +
    /// its required Rigidbody) are attached by THIS spawner, regardless of whether
    /// the visual is a primitive or real art.
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
        /// Spawn <paramref name="count"/> goblins of <paramref name="type"/> as this
        /// squad's members, on the XZ plane around <paramref name="origin"/>.
        /// Visuals come from VisualLibrary("Goblin"); gameplay is attached here.
        /// </summary>
        public void Build(GoblinType type, int count, Vector3 origin)
        {
            squadType = type;
            members.Clear();
            IsDestroyed = false;

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPos = origin + FormationOffset(i, count);

                // ART SEAM: ask the VisualLibrary for the "Goblin" visual (real
                // prefab if present, else a green capsule primitive). Then attach
                // the gameplay component (which RequireComponent-adds a Rigidbody).
                GameObject go = VisualLibrary.Spawn(VisualLibrary.KeyGoblin, spawnPos,
                    Quaternion.identity, transform);
                GoblinUnit unit = Ensure<GoblinUnit>(go);

                unit.Init(type);
                unit.SetVisualTint(VisualLibrary.GoblinGreen); // G4: goblins are green
                unit.OnDied += HandleMemberDied;
                members.Add(unit);
            }
        }

        // Get-or-add: future art prefabs (Phase C+) may already carry the gameplay
        // component, so we never double-add it.
        private static T Ensure<T>(GameObject go) where T : Component
            => go.GetComponent<T>() ?? go.AddComponent<T>();

        // Formation offset on the XZ plane (was a Vector2 in 2D). Y stays 0 so the
        // offset never lifts/sinks a unit off the ground (G2).
        private Vector3 FormationOffset(int index, int count)
        {
            int perRow = Mathf.CeilToInt(Mathf.Sqrt(count));
            int row = index / perRow;
            int col = index % perRow;
            float cx = (perRow - 1) * 0.5f;
            return new Vector3((col - cx) * formationSpacing, 0f, (row - cx) * formationSpacing);
        }

        public void SetSelected(bool selected) => _selected = selected;

        /// <summary>Order every living member toward a destination in formation (XZ).</summary>
        public void OrderMoveTo(Vector3 worldPos)
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

        public Vector3 CenterOfMass()
        {
            Vector3 sum = Vector3.zero;
            int alive = 0;
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i] == null || !members[i].IsAlive) continue;
                sum += members[i].transform.position;
                alive++;
            }
            return alive > 0 ? sum / alive : transform.position;
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
