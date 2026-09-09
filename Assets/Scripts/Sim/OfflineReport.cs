// What happened while the player was away — feeds the Welcome Back panel.
using System.Collections.Generic;

namespace SkyHarvest.Sim
{
    public sealed class OfflineReport
    {
        public long  ElapsedSeconds;
        public bool  CappedByOfflineLimit;

        public readonly Dictionary<string, int> Harvested = new();
        public int   Replanted;
        public int   BatchesCompleted;

        /// <summary>Sim-seconds into the run when each buffer first saturated; -1 = never.</summary>
        public float StorageFullAt = -1f;
        public float WaterEmptyAt  = -1f;
        public float PowerEmptyAt  = -1f;

        /// <summary>Sim-seconds elapsed so far in this run; advanced by IslandSim.Step.</summary>
        public float Clock;

        public int TotalHarvested
        {
            get { int n = 0; foreach (var kv in Harvested) n += kv.Value; return n; }
        }

        public bool HasAnythingToShow =>
            TotalHarvested > 0 || Replanted > 0 || BatchesCompleted > 0 || ElapsedSeconds >= 60;

        public void RecordHarvest(string itemId, int amount)
        {
            Harvested.TryGetValue(itemId, out int have);
            Harvested[itemId] = have + amount;
        }

        public void MarkStorageFull() { if (StorageFullAt < 0f) StorageFullAt = Clock; }
        public void MarkWaterEmpty()  { if (WaterEmptyAt  < 0f) WaterEmptyAt  = Clock; }
        public void MarkPowerEmpty()  { if (PowerEmptyAt  < 0f) PowerEmptyAt  = Clock; }

        public static string FormatDuration(float seconds)
        {
            if (seconds < 60f) return $"{(int)seconds}s";
            int m = (int)(seconds / 60f);
            if (m < 60) return $"{m}m";
            int h = m / 60; m %= 60;
            return m > 0 ? $"{h}h {m}m" : $"{h}h";
        }
    }
}
