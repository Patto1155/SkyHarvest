// Pure offline-time math shared by the catch-up runner and its tests.
namespace SkyHarvest.Sim
{
    public static class OfflineCatchup
    {
        /// <summary>Real seconds away, clamped to [0, capSeconds]. Clock skew yields 0.</summary>
        public static (long elapsed, bool capped) ClampElapsed(long lastSeenUnix, long nowUnix, long capSeconds)
        {
            if (lastSeenUnix <= 0 || capSeconds <= 0) return (0, false);
            long raw = nowUnix - lastSeenUnix;
            if (raw <= 0) return (0, false);
            return raw > capSeconds ? (capSeconds, true) : (raw, false);
        }

        /// <summary>Whole steps plus the fractional remainder so no time is lost.</summary>
        public static (int steps, float remainder) SplitSteps(long elapsedSeconds, float stepSeconds)
        {
            if (elapsedSeconds <= 0 || stepSeconds <= 0f) return (0, 0f);
            int steps = (int)(elapsedSeconds / stepSeconds);
            float remainder = elapsedSeconds - steps * stepSeconds;
            return (steps, remainder);
        }
    }
}
