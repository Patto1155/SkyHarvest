// Shared island water buffer: rain catchers and springs fill it, sprinklers draw from it.
// Capacity grows with water tanks. Pure C#.
namespace SkyHarvest.Sim
{
    public sealed class WaterNetwork
    {
        public const float BaseCapacity = 50f;

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

        /// <summary>Draw up to <paramref name="amount"/>; returns what was actually drawn.</summary>
        public float Draw(float amount)
        {
            if (amount <= 0f || Stored <= 0f) return 0f;
            float taken = System.Math.Min(amount, Stored);
            Stored -= taken;
            return taken;
        }

        public void Restore(float stored) => Stored = System.Math.Clamp(stored, 0f, Capacity);
    }
}
