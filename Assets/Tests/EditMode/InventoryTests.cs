// Assets/Tests/EditMode/InventoryTests.cs
// Smoke tests for the Inventory POCO — ported from plan Task 4.
// Owned by: harness agent.
using NUnit.Framework;
using SkyHarvest.Player;
using SkyHarvest.Core;

[TestFixture]
public class InventoryTests
{
    private Inventory _inv = null!;

    [SetUp]
    public void SetUp()
    {
        EventBus.Clear();
        _inv = new Inventory(slotCount: 20);
    }

    [Test]
    public void AddItem_To_Empty_Inventory_Returns_True()
    {
        Assert.IsTrue(_inv.TryAdd("wheat_seed", 5));
        Assert.AreEqual(5, _inv.GetCount("wheat_seed"));
    }

    [Test]
    public void AddItem_Stacks_Same_Item()
    {
        _inv.TryAdd("wheat_seed", 5);
        _inv.TryAdd("wheat_seed", 3);
        Assert.AreEqual(8, _inv.GetCount("wheat_seed"));
    }

    [Test]
    public void RemoveItem_Reduces_Count()
    {
        _inv.TryAdd("wheat_seed", 10);
        Assert.IsTrue(_inv.TryRemove("wheat_seed", 4));
        Assert.AreEqual(6, _inv.GetCount("wheat_seed"));
    }

    [Test]
    public void RemoveItem_Fails_If_Not_Enough()
    {
        _inv.TryAdd("wheat_seed", 3);
        Assert.IsFalse(_inv.TryRemove("wheat_seed", 5));
        Assert.AreEqual(3, _inv.GetCount("wheat_seed"));
    }

    [Test]
    public void HasItems_Returns_Correctly()
    {
        _inv.TryAdd("scrap", 10);
        Assert.IsTrue(_inv.Has("scrap", 10));
        Assert.IsTrue(_inv.Has("scrap", 5));
        Assert.IsFalse(_inv.Has("scrap", 11));
    }

    [Test]
    public void Full_Inventory_Rejects_New_Item()
    {
        var small = new Inventory(slotCount: 2);
        small.TryAdd("item_a", 1);
        small.TryAdd("item_b", 1);
        Assert.IsFalse(small.TryAdd("item_c", 1));
    }

    [Test]
    public void GetCount_Returns_Zero_For_Missing_Item()
    {
        Assert.AreEqual(0, _inv.GetCount("nonexistent"));
    }

    [Test]
    public void Multiple_Different_Items_Are_Tracked_Independently()
    {
        _inv.TryAdd("wood", 4);
        _inv.TryAdd("scrap", 2);
        _inv.TryAdd("iron_ore", 1);
        Assert.AreEqual(4, _inv.GetCount("wood"));
        Assert.AreEqual(2, _inv.GetCount("scrap"));
        Assert.AreEqual(1, _inv.GetCount("iron_ore"));
    }

    [Test]
    public void TryRemove_All_Of_Item_Clears_Slot()
    {
        _inv.TryAdd("wood", 3);
        _inv.TryRemove("wood", 3);
        Assert.AreEqual(0, _inv.GetCount("wood"));
    }

    [Test]
    public void InventoryChangedEvent_Published_On_TryAdd()
    {
        int eventCount = 0;
        EventBus.Subscribe<InventoryChangedEvent>(_ => eventCount++);
        _inv.TryAdd("wheat_seed", 1);
        Assert.Greater(eventCount, 0, "InventoryChangedEvent should be published when item is added");
    }

    [Test]
    public void InventoryChangedEvent_Published_On_TryRemove()
    {
        _inv.TryAdd("scrap", 5);
        int eventCount = 0;
        EventBus.Subscribe<InventoryChangedEvent>(_ => eventCount++);
        _inv.TryRemove("scrap", 2);
        Assert.Greater(eventCount, 0, "InventoryChangedEvent should be published when item is removed");
    }
}
