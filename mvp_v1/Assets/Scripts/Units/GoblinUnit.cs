using GoblinSiege.Core;
using UnityEngine;

namespace GoblinSiege.Units
{
    /// <summary>
    /// A single goblin combatant. Stats come from <see cref="Balance"/> by type.
    /// Sappers can breach gates (handled by the Gate/Sapper interaction).
    /// </summary>
    public class GoblinUnit : Unit
    {
        [SerializeField] private GoblinType goblinType = GoblinType.Grunt;

        public GoblinType GoblinType => goblinType;
        public bool IsSapper => goblinType == GoblinType.Sapper;

        public void Init(GoblinType type)
        {
            goblinType = type;
            ApplyStats(Team.Goblin, Balance.GoblinStats(type));
        }
    }
}
