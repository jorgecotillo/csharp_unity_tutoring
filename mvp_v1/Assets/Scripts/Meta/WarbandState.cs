using System.Collections.Generic;
using GoblinSiege.Core;
using UnityEngine;

namespace GoblinSiege.Meta
{
    /// <summary>
    /// Persistent war-band that carries BETWEEN raids (spec section 5: progression).
    /// Holds surviving squads, the squad cap, banked surplus gold, and campaign progress.
    /// Survives scene loads as a DontDestroyOnLoad singleton; persisted to PlayerPrefs
    /// (WebGL-safe — no System.IO).
    /// </summary>
    public class WarbandState : MonoBehaviour
    {
        public static WarbandState Instance { get; private set; }

        [System.Serializable]
        public class SquadRecord
        {
            public GoblinType type;
            public bool alive = true;
            public int raidsServed; // veterancy hook (not used in MVP combat math)

            public SquadRecord(GoblinType t) { type = t; }
        }

        [SerializeField] private List<SquadRecord> squads = new();
        [SerializeField] private int squadCap = Balance.StartingSquadCap;
        [SerializeField] private int surplusGold;
        [SerializeField] private int highestRaidCleared;

        public IReadOnlyList<SquadRecord> Squads => squads;
        public int SquadCap => squadCap;
        public int SurplusGold => surplusGold;
        public int HighestRaidCleared => highestRaidCleared;

        // Bumped v1 -> v2 so older 3-squad saves (no Sapper) are discarded and every
        // player gets the new starting band WITH a Sapper on next load.
        private const string PrefKey = "goblinsiege.warband.v2";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        /// <summary>Reset to a fresh starting war-band (New Campaign).</summary>
        public void NewCampaign()
        {
            squads.Clear();
            squadCap = Balance.StartingSquadCap;
            surplusGold = 0;
            highestRaidCleared = 0;
            // Starting band: 2 Grunt squads + 1 Spearthrower + 1 Sapper (breaches gates).
            squads.Add(new SquadRecord(GoblinType.Grunt));
            squads.Add(new SquadRecord(GoblinType.Grunt));
            squads.Add(new SquadRecord(GoblinType.Spearthrower));
            squads.Add(new SquadRecord(GoblinType.Sapper));
            Save();
        }

        /// <summary>The squad types that should deploy into the next raid (alive only).</summary>
        public List<GoblinType> DeployableTypes()
        {
            var result = new List<GoblinType>();
            for (int i = 0; i < squads.Count; i++)
                if (squads[i].alive) result.Add(squads[i].type);
            return result;
        }

        public int AliveSquadCount()
        {
            int n = 0;
            for (int i = 0; i < squads.Count; i++)
                if (squads[i].alive) n++;
            return n;
        }

        /// <summary>Record the outcome of a raid: which deployed squads survived.</summary>
        public void ApplyRaidOutcome(List<bool> survivedByIndex)
        {
            int deployIdx = 0;
            for (int i = 0; i < squads.Count; i++)
            {
                if (!squads[i].alive) continue;
                if (deployIdx < survivedByIndex.Count)
                {
                    bool survived = survivedByIndex[deployIdx];
                    squads[i].alive = survived;
                    if (survived) squads[i].raidsServed++;
                }
                deployIdx++;
            }
            Save();
        }

        public void BankSurplus(int amount)
        {
            surplusGold += Mathf.Max(0, amount);
            Save();
        }

        public void MarkRaidCleared(int raidId)
        {
            if (raidId > highestRaidCleared) highestRaidCleared = raidId;
            Save();
        }

        // --- War-Camp spending ---

        public bool TryRecruitSquad(GoblinType type)
        {
            if (AliveSquadCount() >= squadCap) return false;
            if (surplusGold < Balance.RecruitSquadCost) return false;
            surplusGold -= Balance.RecruitSquadCost;
            // Reuse a dead slot if available, else add.
            for (int i = 0; i < squads.Count; i++)
            {
                if (!squads[i].alive)
                {
                    squads[i] = new SquadRecord(type);
                    Save();
                    return true;
                }
            }
            squads.Add(new SquadRecord(type));
            Save();
            return true;
        }

        public bool TryReviveSquad()
        {
            if (surplusGold < Balance.ReviveSquadCost) return false;
            for (int i = 0; i < squads.Count; i++)
            {
                if (!squads[i].alive)
                {
                    surplusGold -= Balance.ReviveSquadCost;
                    squads[i].alive = true;
                    Save();
                    return true;
                }
            }
            return false;
        }

        // --- Persistence (PlayerPrefs JSON, WebGL-safe) ---

        [System.Serializable]
        private class SaveBlob
        {
            public List<SquadRecord> squads;
            public int squadCap;
            public int surplusGold;
            public int highestRaidCleared;
        }

        public void Save()
        {
            var blob = new SaveBlob
            {
                squads = squads,
                squadCap = squadCap,
                surplusGold = surplusGold,
                highestRaidCleared = highestRaidCleared
            };
            PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(blob));
            PlayerPrefs.Save();
        }

        public void Load()
        {
            if (!PlayerPrefs.HasKey(PrefKey))
            {
                NewCampaign();
                return;
            }
            var blob = JsonUtility.FromJson<SaveBlob>(PlayerPrefs.GetString(PrefKey));
            if (blob?.squads == null)
            {
                NewCampaign();
                return;
            }
            squads = blob.squads;
            squadCap = Mathf.Max(Balance.StartingSquadCap, blob.squadCap);
            surplusGold = blob.surplusGold;
            highestRaidCleared = blob.highestRaidCleared;
        }
    }
}
