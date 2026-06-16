// Assets/Tests/EditMode/InventoryCursorTests.cs
using NUnit.Framework;
using SkyHarvest.Player;

[TestFixture]
public class InventoryCursorTests
{
    private Inventory _inv = null!;

    [SetUp]
    public void SetUp() => _inv = new Inventory(20);

    [Test]
    public void Click_Empty_Cursor_On_Filled_Slot_Picks_Up()
    {
        _inv.TryAdd("wood", 4);
        var cursor = InventoryCursorModel.ClickSlot(_inv, default, 0);
        Assert.AreEqual("wood", cursor.ItemId);
        Assert.AreEqual(4, cursor.Count);
        Assert.IsTrue(_inv.Slots[0].IsEmpty);
    }

    [Test]
    public void Click_With_Cursor_On_Empty_Slot_Places()
    {
        _inv.Slots[0].ItemId = "wood";
        _inv.Slots[0].Count  = 3;
        var held = new CursorStack { ItemId = "wood", Count = 3 };
        var cursor = InventoryCursorModel.ClickSlot(_inv, held, 1);
        Assert.IsTrue(cursor.IsEmpty);
        Assert.AreEqual(3, _inv.Slots[1].Count);
    }

    [Test]
    public void Click_With_Cursor_On_Same_Item_Merges()
    {
        _inv.Slots[0].ItemId = "wood";
        _inv.Slots[0].Count  = 2;
        var held = new CursorStack { ItemId = "wood", Count = 3 };
        var cursor = InventoryCursorModel.ClickSlot(_inv, held, 0);
        Assert.IsTrue(cursor.IsEmpty);
        Assert.AreEqual(5, _inv.Slots[0].Count);
    }

    [Test]
    public void Click_With_Cursor_On_Different_Item_Swaps()
    {
        _inv.Slots[1].ItemId = "scrap";
        _inv.Slots[1].Count  = 1;
        var held = new CursorStack { ItemId = "wood", Count = 2 };
        var cursor = InventoryCursorModel.ClickSlot(_inv, held, 1);
        Assert.AreEqual("scrap", cursor.ItemId);
        Assert.AreEqual(1, cursor.Count);
        Assert.AreEqual("wood", _inv.Slots[1].ItemId);
        Assert.AreEqual(2, _inv.Slots[1].Count);
    }

    [Test]
    public void Two_Clicks_Move_Stack_Between_Slots()
    {
        _inv.TryAdd("wheat_seed", 5);
        var cursor = InventoryCursorModel.ClickSlot(_inv, default, 0);
        cursor = InventoryCursorModel.ClickSlot(_inv, cursor, 5);
        Assert.IsTrue(cursor.IsEmpty);
        Assert.IsTrue(_inv.Slots[0].IsEmpty);
        Assert.AreEqual(5, _inv.Slots[5].Count);
        Assert.AreEqual("wheat_seed", _inv.Slots[5].ItemId);
    }
}
