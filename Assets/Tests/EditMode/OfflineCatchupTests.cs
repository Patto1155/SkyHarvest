// Assets/Tests/EditMode/OfflineCatchupTests.cs
// Pure offline-time math (spec 2026-09-09 §3): elapsed is clamped to the offline cap and
// split into whole steps + remainder so no time is dropped.
using NUnit.Framework;
using SkyHarvest.Sim;

[TestFixture]
public class OfflineCatchupTests
{
    private const long Cap = 4 * 3600;

    [Test]
    public void Elapsed_Within_Cap_Is_Returned_Uncapped()
    {
        var (elapsed, capped) = OfflineCatchup.ClampElapsed(1000, 1000 + 900, Cap);
        Assert.AreEqual(900, elapsed);
        Assert.IsFalse(capped);
    }

    [Test]
    public void Elapsed_Beyond_Cap_Is_Clamped_And_Flagged()
    {
        var (elapsed, capped) = OfflineCatchup.ClampElapsed(1000, 1000 + 86400, Cap);
        Assert.AreEqual(Cap, elapsed);
        Assert.IsTrue(capped);
    }

    [Test]
    public void Clock_Skew_Or_Missing_Stamp_Yields_Zero()
    {
        Assert.AreEqual(0, OfflineCatchup.ClampElapsed(5000, 4000, Cap).elapsed);
        Assert.AreEqual(0, OfflineCatchup.ClampElapsed(0, 4000, Cap).elapsed);
        Assert.AreEqual(0, OfflineCatchup.ClampElapsed(1000, 4000, 0).elapsed);
    }

    [Test]
    public void SplitSteps_Keeps_The_Remainder()
    {
        var (steps, remainder) = OfflineCatchup.SplitSteps(150, 60f);
        Assert.AreEqual(2, steps);
        Assert.AreEqual(30f, remainder, 0.001f);
    }

    [Test]
    public void SplitSteps_Handles_Exact_Multiples_And_Zero()
    {
        Assert.AreEqual((3, 0f), OfflineCatchup.SplitSteps(180, 60f));
        Assert.AreEqual((0, 0f), OfflineCatchup.SplitSteps(0, 60f));
    }
}
