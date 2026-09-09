// Assets/Tests/EditMode/TapMoveTests.cs
// Tap-to-move pure logic: BFS over the tier-gated grid and tap → cell resolution.
using NUnit.Framework;
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Island;
using SkyHarvest.Player;

[TestFixture]
public class TapMoveTests
{
    // ---- GridPath -------------------------------------------------------------

    [Test]
    public void Path_Within_Tier_Is_Shortest_Manhattan()
    {
        var island = StarterIsland.Build();
        var path = GridPath.Find(island, new Vector2Int(0, 2), new Vector2Int(2, 3));
        Assert.IsNotNull(path);
        Assert.AreEqual(3, path!.Count);
        Assert.AreEqual(new Vector2Int(2, 3), path[^1]);
    }

    [Test]
    public void Path_To_Self_Is_Empty()
    {
        var island = StarterIsland.Build();
        var path = GridPath.Find(island, new Vector2Int(1, 2), new Vector2Int(1, 2));
        Assert.IsNotNull(path);
        Assert.AreEqual(0, path!.Count);
    }

    [Test]
    public void Path_Across_Uncarved_Stairs_Is_Unreachable()
    {
        var island = StarterIsland.Build();
        Assert.IsNull(GridPath.Find(island, StarterIsland.FrontStairCell, StarterIsland.BackStairCell));
    }

    [Test]
    public void Path_Uses_The_Stairs_Once_Carved()
    {
        var island = StarterIsland.Build();
        island.CarveStairs(StarterIsland.FrontStairCell);
        var path = GridPath.Find(island, new Vector2Int(0, 3), new Vector2Int(2, 0));
        Assert.IsNotNull(path);
        CollectionAssert.Contains(path!, StarterIsland.FrontStairCell);
        CollectionAssert.Contains(path!, StarterIsland.BackStairCell);
        for (int i = 1; i < path.Count; i++)
            Assert.IsTrue(island.CanTraverse(path[i - 1], path[i]), $"step {i} must be legal");
    }

    [Test]
    public void Path_Off_Island_Is_Null()
    {
        var island = StarterIsland.Build();
        Assert.IsNull(GridPath.Find(island, new Vector2Int(0, 2), new Vector2Int(9, 9)));
    }

    // ---- TapTargeting ---------------------------------------------------------

    [Test]
    public void Tap_On_Own_Tier_Resolves_Directly()
    {
        var island = StarterIsland.Build();
        var cell = new Vector2Int(2, 3);
        var world = GridMath.GridToWorld(cell, 0f);
        Assert.AreEqual(cell, TapTargeting.ResolveCell(island, world, preferredTier: 0));
    }

    [Test]
    public void Tap_On_Raised_Tier_Resolves_Through_Elevation()
    {
        var island = StarterIsland.Build();
        var raised = new Vector2Int(1, 0);                // tier 1 on the starter island
        var world = GridMath.GridToWorld(raised, island.GetCell(raised)!.Elevation);
        var hit = TapTargeting.ResolveCell(island, world, preferredTier: 0);
        Assert.IsNotNull(hit);
        Assert.AreEqual(1, island.Tier(hit!.Value));
    }

    [Test]
    public void Tap_In_The_Void_Resolves_To_Nothing()
    {
        var island = StarterIsland.Build();
        Assert.IsNull(TapTargeting.ResolveCell(island, new Vector2(40f, 40f), 0));
    }

    [Test]
    public void Reach_Uses_Interact_Radius()
    {
        var cell = new Vector2Int(0, 0);
        Assert.IsTrue(TapTargeting.IsWithinReach(new Vector2(0.5f, 0f), cell, 0f));
        Assert.IsFalse(TapTargeting.IsWithinReach(new Vector2(3f, 0f), cell, 0f));
    }

    [Test]
    public void World_Direction_Maps_To_Dimetric_Axes()
    {
        var (h, v) = TapTargeting.WorldDirToAxes(new Vector2(0.5f, 0f));   // one cell east-on-screen
        Assert.AreEqual(1f, h, 0.001f);
        Assert.AreEqual(0f, v, 0.001f);

        (h, v) = TapTargeting.WorldDirToAxes(new Vector2(0f, -0.25f));      // one cell down-screen
        Assert.AreEqual(0f, h, 0.001f);
        Assert.AreEqual(-1f, v, 0.001f);

        Assert.AreEqual((0f, 0f), TapTargeting.WorldDirToAxes(Vector2.zero));
    }
}
