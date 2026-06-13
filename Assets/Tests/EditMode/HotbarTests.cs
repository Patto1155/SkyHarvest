// Assets/Tests/EditMode/HotbarTests.cs
// Unit tests for the unified-hotbar selection model (HotbarModel).
// Owned by: harness agent.
using NUnit.Framework;
using SkyHarvest.Player;
using SkyHarvest.Core;

[TestFixture]
public class HotbarTests
{
    private Inventory _inv = null!;
    private HotbarModel _bar = null!;

    [SetUp]
    public void SetUp()
    {
        EventBus.Clear();
        _inv = new Inventory(slotCount: 20);
        _inv.TryAdd("wheat_seed", 5);    // inventory slot 0
        _inv.TryAdd("herb_seed", 2);     // inventory slot 1
        _bar = new HotbarModel(_inv, itemSlots: 6);
    }

    [Test]
    public void SlotCount_Is_Tools_Plus_ItemSlots()
    {
        Assert.AreEqual(4 + 6, _bar.SlotCount);
        Assert.AreEqual(4, _bar.ToolCount);
    }

    [Test]
    public void Defaults_To_First_Tool_Slot()
    {
        Assert.AreEqual(0, _bar.SelectedIndex);
        Assert.IsTrue(_bar.IsToolSelected);
        Assert.AreEqual(ToolType.Hoe, _bar.SelectedTool);
        Assert.IsNull(_bar.HeldItemId);
    }

    [Test]
    public void Tool_Slots_Map_To_Tools_In_Order()
    {
        Assert.AreEqual(ToolType.Hoe,         _bar.ToolAt(0));
        Assert.AreEqual(ToolType.WateringCan, _bar.ToolAt(1));
        Assert.AreEqual(ToolType.Sickle,      _bar.ToolAt(2));
        Assert.AreEqual(ToolType.Hammer,      _bar.ToolAt(3));
        Assert.AreEqual(ToolType.None,        _bar.ToolAt(4));   // item slot
    }

    [Test]
    public void Selecting_Tool_Slot_Equips_That_Tool()
    {
        Assert.IsTrue(_bar.Select(1));
        Assert.IsTrue(_bar.IsToolSelected);
        Assert.AreEqual(ToolType.WateringCan, _bar.SelectedTool);
        Assert.IsNull(_bar.HeldItemId);
    }

    [Test]
    public void Selecting_Item_Slot_Holds_That_Item()
    {
        // unified index 4 == inventory slot 0 == wheat_seed
        Assert.IsTrue(_bar.Select(4));
        Assert.IsFalse(_bar.IsToolSelected);
        Assert.AreEqual(ToolType.None, _bar.SelectedTool);
        Assert.AreEqual("wheat_seed", _bar.HeldItemId);
    }

    [Test]
    public void Item_Slot_Maps_To_Correct_Inventory_Slot()
    {
        Assert.AreEqual(0, _bar.InventoryIndexFor(4));
        Assert.AreEqual(1, _bar.InventoryIndexFor(5));
        Assert.AreEqual(-1, _bar.InventoryIndexFor(0));   // tool slot

        Assert.AreEqual("herb_seed", _bar.ItemIdAt(5));
        Assert.AreEqual(2, _bar.CountAt(5));
    }

    [Test]
    public void Empty_Item_Slot_Has_Null_Item()
    {
        Assert.IsTrue(_bar.Select(6));    // inventory slot 2 is empty
        Assert.IsNull(_bar.HeldItemId);
        Assert.AreEqual(0, _bar.CountAt(6));
    }

    [Test]
    public void Select_Out_Of_Range_Is_Ignored()
    {
        Assert.IsFalse(_bar.Select(-1));
        Assert.IsFalse(_bar.Select(_bar.SlotCount));
        Assert.AreEqual(0, _bar.SelectedIndex);
    }

    [Test]
    public void Select_Same_Slot_Returns_False()
    {
        Assert.IsFalse(_bar.Select(0));   // already on slot 0
        Assert.IsTrue(_bar.Select(2));
        Assert.IsFalse(_bar.Select(2));
    }

    [Test]
    public void Held_Item_Clears_When_Stack_Emptied()
    {
        _bar.Select(4);                   // holding wheat_seed
        Assert.AreEqual("wheat_seed", _bar.HeldItemId);
        _inv.TryRemove("wheat_seed", 5);  // deplete the stack
        Assert.IsNull(_bar.HeldItemId);   // cursor stays, item is gone
    }
}
