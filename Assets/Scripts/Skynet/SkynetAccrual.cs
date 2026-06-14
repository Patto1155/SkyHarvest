// Pure offline-accrual math for the Skynet, split out so it can be unit-tested
// without a Unity runtime (runs under tools/check.sh against the stubs).
// The Skynet "passively catches drifting debris, checked like a mailbox" (spec §3):
// while the player is away it accrues one catch per OfflineRollPeriod of elapsed
// real time, capped at the buffer size.
namespace SkyHarvest.Skynet
{
    public static class SkynetAccrual
    {
        /// <summary>
        /// Number of catches a Skynet should credit for time spent offline:
        /// floor(elapsedSeconds / rollPeriodSeconds), clamped to [0, maxStacks].
        /// Returns 0 for non-positive elapsed/period/cap (clock skew, bad data).
        /// </summary>
        public static int OfflineRollCount(long elapsedSeconds, float rollPeriodSeconds, int maxStacks)
        {
            if (elapsedSeconds <= 0 || rollPeriodSeconds <= 0f || maxStacks <= 0) return 0;

            long rolls = (long)System.Math.Floor(elapsedSeconds / (double)rollPeriodSeconds);
            if (rolls <= 0) return 0;

            return (int)System.Math.Min(rolls, maxStacks);
        }
    }
}
