// Assets/Tests/EditMode/TierGateTests.cs
// Two-tier walkability + mineable-stair gate (designed starter island).
// Pure logic, runs under tools/check.sh.
using NUnit.Framework;
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Island;

[TestFixture]
public class TierGateTests
{
    private static readonly Vector2Int Front = StarterIsland.FrontStairCell;   // (1,2) tier 0
    private static readonly Vector2Int Back  = StarterIsland.BackStairCell;    // (1,1) tier 1

    [Test]
    public void Starter_Has_Twelve_Cells_Across_Two_Tiers()
    {
        var island = StarterIsland.Build();
        Assert.AreEqual(StarterIsland.Width * StarterIsland.Depth, island.Cells.Count);
        Assert.AreEqual(0, island.Tier(Front), "front stair cell is the lower tier");
        Assert.AreEqual(1, island.Tier(Back),  "back stair cell is the raised tier");
    }

    [Test]
    public void Within_Same_Tier_Is_Always_Traversable()
    {
        var island = StarterIsland.Build();
        // (0,2) -> (1,2): both lower tier.
        Assert.IsTrue(island.CanTraverse(new Vector2Int(0, 2), new Vector2Int(1, 2)));
        // (0,0) -> (1,0): both raised tier.
        Assert.IsTrue(island.CanTraverse(new Vector2Int(0, 0), new Vector2Int(1, 0)));
    }

    [Test]
    public void Cross_Tier_Off_The_Stair_Is_Always_Blocked()
    {
        var island = StarterIsland.Build();
        island.CarveStairs(Front);   // even fully carved...
        // (0,2) lower -> (0,1) raised is NOT the stair column: still a cliff.
        Assert.IsFalse(island.CanTraverse(new Vector2Int(0, 2), new Vector2Int(0, 1)));
        Assert.IsFalse(island.CanTraverse(new Vector2Int(2, 2), new Vector2Int(2, 1)));
    }

    [Test]
    public void Stair_Edge_Is_Locked_Until_Carved()
    {
        var island = StarterIsland.Build();
        Assert.IsFalse(island.StairsCarved);
        Assert.IsFalse(island.CanTraverse(Front, Back), "cannot climb before mining");
        Assert.IsFalse(island.CanTraverse(Back, Front), "blocked both directions");
    }

    [Test]
    public void Stair_Edge_Opens_Both_Directions_After_Carving()
    {
        var island = StarterIsland.Build();
        island.CarveStairs(Front);
        Assert.IsTrue(island.StairsCarved);
        Assert.IsTrue(island.CanTraverse(Front, Back), "can climb up after mining");
        Assert.IsTrue(island.CanTraverse(Back, Front), "and back down");
    }

    [Test]
    public void CarveStairs_Fires_Event_Once_And_Is_Idempotent()
    {
        var island = StarterIsland.Build();
        int fires = 0;
        Vector2Int reported = default;
        void Handler(StairsCarvedEvent e) { fires++; reported = new Vector2Int(e.StairX, e.StairY); }
        EventBus.Subscribe<StairsCarvedEvent>(Handler);
        try
        {
            island.CarveStairs(Front);
            island.CarveStairs(Front);   // second call must be a no-op
        }
        finally
        {
            EventBus.Unsubscribe<StairsCarvedEvent>(Handler);
        }
        Assert.AreEqual(1, fires, "carving fires exactly once");
        Assert.AreEqual(Front, reported);
    }

    [Test]
    public void Traverse_To_Nonexistent_Cell_Is_Blocked()
    {
        var island = StarterIsland.Build();
        Assert.IsFalse(island.CanTraverse(new Vector2Int(0, 0), new Vector2Int(-1, 0)));
    }
}
