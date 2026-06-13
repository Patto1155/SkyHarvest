// Pure-C# state machine for staged building (spec §2 "Blueprint ghost system":
// place a ghost → deliver materials → construction completes).
// Kept free of UnityEngine API so tools/check.sh NUnit tests cover it.
using System.Collections.Generic;
using SkyHarvest.Data;

namespace SkyHarvest.Building
{
    public class ConstructionProgress
    {
        private readonly Dictionary<string, int> _required  = new();
        private readonly Dictionary<string, int> _delivered = new();

        public ConstructionProgress(IEnumerable<BuildCost> costs)
        {
            if (costs == null) return;
            foreach (var c in costs)
            {
                if (string.IsNullOrEmpty(c.ItemId) || c.Amount <= 0) continue;
                _required.TryGetValue(c.ItemId, out int existing);
                _required[c.ItemId] = existing + c.Amount;
            }
        }

        public bool IsComplete
        {
            get
            {
                foreach (var kvp in _required)
                    if (Delivered(kvp.Key) < kvp.Value) return false;
                return true;
            }
        }

        public int Required(string itemId) =>
            _required.TryGetValue(itemId, out int v) ? v : 0;

        public int Delivered(string itemId) =>
            _delivered.TryGetValue(itemId, out int v) ? v : 0;

        public int Remaining(string itemId) => Required(itemId) - Delivered(itemId);

        /// <summary>Items still needed, with remaining amounts (stable order not guaranteed).</summary>
        public IEnumerable<(string itemId, int remaining)> RemainingCosts()
        {
            foreach (var kvp in _required)
            {
                int rem = kvp.Value - Delivered(kvp.Key);
                if (rem > 0) yield return (kvp.Key, rem);
            }
        }

        /// <summary>Everything delivered so far (for save + demolish refund).</summary>
        public IEnumerable<(string itemId, int count)> DeliveredItems()
        {
            foreach (var kvp in _delivered)
                if (kvp.Value > 0) yield return (kvp.Key, kvp.Value);
        }

        /// <summary>
        /// Deliver up to <paramref name="count"/> of an item.
        /// Returns how many were accepted (0 if the item isn't needed or already met).
        /// </summary>
        public int Deliver(string itemId, int count)
        {
            if (count <= 0) return 0;
            int remaining = Remaining(itemId);
            if (remaining <= 0) return 0;

            int accepted = count < remaining ? count : remaining;
            _delivered.TryGetValue(itemId, out int existing);
            _delivered[itemId] = existing + accepted;
            return accepted;
        }
    }
}
