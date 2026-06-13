// Assets/Tests/EditMode/RecipeTests.cs
// Recipe validation tests from plan Task 16.
using NUnit.Framework;
using SkyHarvest.Workshop;
using SkyHarvest.Player;

[TestFixture]
public class RecipeTests
{
    [Test]
    public void CanCraft_Returns_True_When_Inventory_Has_Materials()
    {
        var inv = new Inventory(20);
        inv.TryAdd("wheat", 3);
        var inputs = new[] { ("wheat", 2) };
        Assert.IsTrue(WorkshopLogic.CanCraft(inv, inputs));
    }

    [Test]
    public void CanCraft_Returns_False_When_Missing_Materials()
    {
        var inv = new Inventory(20);
        inv.TryAdd("wheat", 1);
        var inputs = new[] { ("wheat", 2) };
        Assert.IsFalse(WorkshopLogic.CanCraft(inv, inputs));
    }

    [Test]
    public void ConsumeInputs_Removes_Items()
    {
        var inv = new Inventory(20);
        inv.TryAdd("wheat", 5);
        WorkshopLogic.ConsumeInputs(inv, new[] { ("wheat", 3) });
        Assert.AreEqual(2, inv.GetCount("wheat"));
    }
}
