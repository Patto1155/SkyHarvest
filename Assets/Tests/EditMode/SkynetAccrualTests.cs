// Assets/Tests/EditMode/SkynetAccrualTests.cs
// Pure offline-accrual math for the Skynet "mailbox" (spec §3): one catch per
// OfflineRollPeriod of elapsed real time, capped at the buffer size.
using NUnit.Framework;
using SkyHarvest.Skynet;

[TestFixture]
public class SkynetAccrualTests
{
    private const float Period = 150f; // matches Skynet.OfflineRollPeriod
    private const int   Max    = 6;    // matches Skynet.MaxBufferStacks

    [Test]
    public void No_Time_Elapsed_Yields_No_Catches()
    {
        Assert.AreEqual(0, SkynetAccrual.OfflineRollCount(0, Period, Max));
    }

    [Test]
    public void Less_Than_One_Period_Yields_No_Catches()
    {
        Assert.AreEqual(0, SkynetAccrual.OfflineRollCount(149, Period, Max));
    }

    [Test]
    public void Floors_To_Whole_Catches()
    {
        // 380s / 150s = 2.53 → 2 catches
        Assert.AreEqual(2, SkynetAccrual.OfflineRollCount(380, Period, Max));
    }

    [Test]
    public void Caps_At_Max_Buffer_Stacks()
    {
        // A full day offline still only fills the buffer.
        Assert.AreEqual(Max, SkynetAccrual.OfflineRollCount(86400, Period, Max));
    }

    [Test]
    public void Exactly_Max_Periods_Yields_Max()
    {
        Assert.AreEqual(Max, SkynetAccrual.OfflineRollCount((long)(Period * Max), Period, Max));
    }

    [Test]
    public void Negative_Elapsed_From_Clock_Skew_Yields_Zero()
    {
        Assert.AreEqual(0, SkynetAccrual.OfflineRollCount(-5000, Period, Max));
    }

    [Test]
    public void Non_Positive_Period_Or_Cap_Yields_Zero()
    {
        Assert.AreEqual(0, SkynetAccrual.OfflineRollCount(10000, 0f, Max));
        Assert.AreEqual(0, SkynetAccrual.OfflineRollCount(10000, Period, 0));
    }
}
