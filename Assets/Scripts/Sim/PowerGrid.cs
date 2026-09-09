// Shared island power buffer: windmills generate, batteries add capacity, powered devices draw.
// Pure C#.
namespace SkyHarvest.Sim
{
    public sealed class PowerGrid
    {
        public const float BaseCapacity = 20f;

        public float Capacity { get; private set; } = BaseCapacity;
        public float Stored   { get; private set; }

        public bool IsEmpty => Stored <= 0f;
        public float Fill   => Capacity > 0f ? Stored / Capacity : 0f;

        public void SetCapacity(float capacity)
        {
            Capacity = System.Math.Max(0f, capacity);
            if (Stored > Capacity) Stored = Capacity;
        }

        public void Add(float amount)
        {
            if (amount <= 0f) return;
            Stored = System.Math.Min(Stored + amount, Capacity);
        }

        /// <summary>All-or-nothing draw so a device either runs this step or doesn't.</summary>
        public bool TryDraw(float amount)
        {
            if (amount <= 0f) return true;
            if (Stored < amount) return false;
            Stored -= amount;
            return true;
        }

        public void Restore(float stored) => Stored = System.Math.Clamp(stored, 0f, Capacity);
    }
}
