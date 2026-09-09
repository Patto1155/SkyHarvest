// Assets/Tests/EditMode/NetworkBufferTests.cs
// WaterNetwork / PowerGrid — the two island buffers that bound offline progress.
using NUnit.Framework;
using SkyHarvest.Sim;

[TestFixture]
public class NetworkBufferTests
{
    [Test]
    public void Water_Add_Clamps_To_Capacity()
    {
        var w = new WaterNetwork();
        w.Add(1000f);
        Assert.AreEqual(WaterNetwork.BaseCapacity, w.Stored, 0.001f);
    }

    [Test]
    public void Water_Draw_Returns_Partial_When_Short()
    {
        var w = new WaterNetwork();
        w.Add(10f);
        Assert.AreEqual(10f, w.Draw(25f), 0.001f);
        Assert.IsTrue(w.IsEmpty);
        Assert.AreEqual(0f, w.Draw(5f), 0.001f);
    }

    [Test]
    public void Water_Shrinking_Capacity_Spills_Excess()
    {
        var w = new WaterNetwork();
        w.SetCapacity(300f);
        w.Add(250f);
        w.SetCapacity(100f);
        Assert.AreEqual(100f, w.Stored, 0.001f);
    }

    [Test]
    public void Power_Draw_Is_All_Or_Nothing()
    {
        var p = new PowerGrid();
        p.Add(5f);
        Assert.IsFalse(p.TryDraw(6f));
        Assert.AreEqual(5f, p.Stored, 0.001f);
        Assert.IsTrue(p.TryDraw(5f));
        Assert.IsTrue(p.IsEmpty);
    }

    [Test]
    public void Power_Zero_Draw_Always_Succeeds()
    {
        var p = new PowerGrid();
        Assert.IsTrue(p.TryDraw(0f));
    }

    [Test]
    public void Restore_Clamps_Into_Range()
    {
        var p = new PowerGrid();
        p.Restore(999f);
        Assert.AreEqual(PowerGrid.BaseCapacity, p.Stored, 0.001f);
        p.Restore(-5f);
        Assert.AreEqual(0f, p.Stored, 0.001f);
    }
}
