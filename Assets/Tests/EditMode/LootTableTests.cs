// Assets/Tests/EditMode/LootTableTests.cs
// Loot table roll tests from plan Task 17 (LootTableDef, not ScriptableObject).
using System;
using NUnit.Framework;
using SkyHarvest.Data;

[TestFixture]
public class LootTableTests
{
    [Test]
    public void Roll_Returns_Valid_Item()
    {
        var table = new LootTableDef
        {
            Entries = new[]
            {
                new LootEntry { ItemId = "scrap", Weight = 1f, MinAmount = 1, MaxAmount = 3 }
            }
        };

        var rng = new Random(42);
        var (itemId, amount) = table.Roll(rng);
        Assert.AreEqual("scrap", itemId);
        Assert.GreaterOrEqual(amount, 1);
        Assert.LessOrEqual(amount, 3);
    }

    [Test]
    public void Roll_Respects_Weights()
    {
        var table = new LootTableDef
        {
            Entries = new[]
            {
                new LootEntry { ItemId = "common", Weight = 90f, MinAmount = 1, MaxAmount = 1 },
                new LootEntry { ItemId = "rare", Weight = 10f, MinAmount = 1, MaxAmount = 1 }
            }
        };

        var rng = new Random(42);
        int commonCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            var (itemId, _) = table.Roll(rng);
            if (itemId == "common") commonCount++;
        }

        // Should be roughly 90% common
        Assert.Greater(commonCount, 800);
        Assert.Less(commonCount, 950);
    }
}
