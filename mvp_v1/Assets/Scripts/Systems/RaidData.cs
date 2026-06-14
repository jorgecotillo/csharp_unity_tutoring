using System;
using System.Collections.Generic;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// Serializable DTOs matching the raid-NN.json schema (spec section 8).
    /// Loaded with JsonUtility (WebGL-safe) from a TextAsset — never System.IO.File.
    /// </summary>
    [Serializable]
    public class RaidData
    {
        public int id;
        public string name;
        public int act;
        public int quota;
        public float alarmFillPerSecond = 1f;
        public string[] garrisonRoster;
        public ReinforceIntervals reinforceIntervalByThreshold;
        public List<CacheSpawn> caches = new();
        public List<GateSpawn> gates = new();
        public string extractionEdge = "south";
    }

    [Serializable]
    public class ReinforceIntervals
    {
        public float alerted = 18f;
        public float mobilized = 10f;
    }

    [Serializable]
    public class CacheSpawn
    {
        public string type;   // Crate | Chest | Granary | Vault
        public float[] pos;   // [x, y]
        public int guards;
        public bool inKeep;
    }

    [Serializable]
    public class GateSpawn
    {
        public float[] pos;   // [x, y]
        public float hp = 100f;
    }
}
