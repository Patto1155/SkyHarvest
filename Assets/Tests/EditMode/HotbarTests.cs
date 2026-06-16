// Assets/Tests/EditMode/HotbarTests.cs
using NUnit.Framework;
using SkyHarvest.Player;
using SkyHarvest.Core;

[TestFixture]
public class HotbarTests
{
    private Inventory _hotbarItems = null!;
    private HotbarModel _bar = null!;

    [SetUp]
    public void SetUp()
    {
        EventBus.Clear();
        _hotbarItems = new Inventory(PlayerInventoryComponent.TotalSlots);
        _hotbarItems.Slots[0].ItemId = ToolItems.Hoe;
        _hotbarItems.Slots[0].Count  = 1;
        _hotbarItems.Slots[1].ItemId = ToolItems.WateringCan;
        _hotbarItems.Slots[1].Count  = 1;
        _hotbarItems.Slots[2].ItemId = "wheat_seed";
        _hotbarItems.Slots[2].Count  = 5;
        _bar = new HotbarModel(_hotbarItems);
    }

    [Test]
    public void SlotCount_Matches_Storage()
    {
        Assert.AreEqual(10, _bar.SlotCount);
    }

    [Test]
    public void Defaults_To_First_Slot()
    {
        Assert.AreEqual(0, _bar.SelectedIndex);
        Assert.AreEqual(ToolType.Hoe, _bar.SelectedTool);
        Assert.AreEqual(ToolItems.Hoe, _bar.HeldItemId);
    }

    [Test]
    public void Selecting_Tool_Item_Equips_That_Tool()
    {
        Assert.IsTrue(_bar.Select(1));
        Assert.AreEqual(ToolType.WateringCan, _bar.SelectedTool);
        Assert.AreEqual(ToolItems.WateringCan, _bar.HeldItemId);
    }

    [Test]
    public void Selecting_Seed_Slot_Holds_Seed_Not_Tool()
    {
        Assert.IsTrue(_bar.Select(2));
        Assert.AreEqual(ToolType.None, _bar.SelectedTool);
        Assert.AreEqual("wheat_seed", _bar.HeldItemId);
        Assert.AreEqual(5, _bar.CountAt(2));
    }

    [Test]
    public void TryConsumeHeldItem_Rejects_Tools()
    {
        _bar.Select(0);
        Assert.IsFalse(_bar.TryConsumeHeldItem(1));
    }

    [Test]
    public void TryConsumeHeldItem_Depletes_Seed_Stack()
    {
        _bar.Select(2);
        Assert.IsTrue(_bar.TryConsumeHeldItem(1));
        Assert.AreEqual(4, _bar.CountAt(2));
    }
}
