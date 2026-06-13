// Assets/Tests/EditMode/ConstructionProgressTests.cs
// Staged-building state machine (spec §2 blueprint ghost system):
// place at 0 materials → deliver → complete when all costs met.
using NUnit.Framework;
using SkyHarvest.Building;
using SkyHarvest.Data;

[TestFixture]
public class ConstructionProgressTests
{
    private static ConstructionProgress NewShelterSite() =>
        new ConstructionProgress(new[]
        {
            new BuildCost { ItemId = "wood",  Amount = 5 },
            new BuildCost { ItemId = "scrap", Amount = 3 }
        });

    [Test]
    public void New_Site_Is_Not_Complete_And_Needs_Full_Costs()
    {
        var p = NewShelterSite();
        Assert.IsFalse(p.IsComplete);
        Assert.AreEqual(5, p.Remaining("wood"));
        Assert.AreEqual(3, p.Remaining("scrap"));
    }

    [Test]
    public void Deliver_Accepts_Up_To_Remaining()
    {
        var p = NewShelterSite();
        Assert.AreEqual(5, p.Deliver("wood", 8));   // over-delivery clamped
        Assert.AreEqual(0, p.Remaining("wood"));
        Assert.AreEqual(0, p.Deliver("wood", 1));   // already met
    }

    [Test]
    public void Partial_Deliveries_Accumulate()
    {
        var p = NewShelterSite();
        Assert.AreEqual(2, p.Deliver("wood", 2));
        Assert.AreEqual(3, p.Deliver("wood", 3));
        Assert.AreEqual(5, p.Delivered("wood"));
        Assert.IsFalse(p.IsComplete);               // scrap still missing
    }

    [Test]
    public void Complete_When_All_Costs_Met()
    {
        var p = NewShelterSite();
        p.Deliver("wood", 5);
        p.Deliver("scrap", 3);
        Assert.IsTrue(p.IsComplete);
    }

    [Test]
    public void Unneeded_Items_Are_Rejected()
    {
        var p = NewShelterSite();
        Assert.AreEqual(0, p.Deliver("flour", 10));
        Assert.AreEqual(0, p.Required("flour"));
    }

    [Test]
    public void DeliveredItems_Reports_For_Save_And_Refund()
    {
        var p = NewShelterSite();
        p.Deliver("wood", 4);
        p.Deliver("scrap", 1);

        int wood = 0, scrap = 0;
        foreach (var (itemId, count) in p.DeliveredItems())
        {
            if (itemId == "wood")  wood  = count;
            if (itemId == "scrap") scrap = count;
        }
        Assert.AreEqual(4, wood);
        Assert.AreEqual(1, scrap);
    }

    [Test]
    public void RemainingCosts_Lists_Only_Outstanding_Items()
    {
        var p = NewShelterSite();
        p.Deliver("wood", 5);

        int entries = 0;
        foreach (var (itemId, remaining) in p.RemainingCosts())
        {
            entries++;
            Assert.AreEqual("scrap", itemId);
            Assert.AreEqual(3, remaining);
        }
        Assert.AreEqual(1, entries);
    }

    [Test]
    public void Duplicate_Cost_Entries_Are_Summed()
    {
        var p = new ConstructionProgress(new[]
        {
            new BuildCost { ItemId = "wood", Amount = 2 },
            new BuildCost { ItemId = "wood", Amount = 3 }
        });
        Assert.AreEqual(5, p.Required("wood"));
    }

    [Test]
    public void Zero_Cost_Structure_Is_Immediately_Complete()
    {
        var p = new ConstructionProgress(new BuildCost[0]);
        Assert.IsTrue(p.IsComplete);
    }
}
