// Stair corridor walkability on the starter island.
using NUnit.Framework;
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Island;

[TestFixture]
public class StairWalkTests
{
    private static readonly Vector2Int Front = StarterIsland.FrontStairCell;
    private static readonly Vector2Int Back  = StarterIsland.BackStairCell;

    [Test]
    public void Corridor_Links_Lower_And_Upper_Stair_Diamonds()
    {
        var island = StarterIsland.Build();
        island.CarveStairs(Front);

        Vector2 lowCentre  = GridMath.DiamondCentre(Front, 0);
        Vector2 highCentre = GridMath.DiamondCentre(Back, 1);

        Assert.IsTrue(island.IsWalkableAt(lowCentre, 0));
        Assert.IsTrue(island.IsWalkableAt(highCentre, 1));

        Vector2 mid = Vector2.Lerp(lowCentre, highCentre, 0.5f);
        Assert.IsTrue(island.IsWalkableAt(mid, 0));
        Assert.IsTrue(island.IsWalkableAt(mid, 1), "corridor is tier-agnostic while climbing");
    }

    [Test]
    public void Corridor_Allows_Exit_Onto_Upper_Neighbour()
    {
        var island = StarterIsland.Build();
        island.CarveStairs(Front);

        Vector2 upperNeighbour = GridMath.DiamondCentre(new Vector2Int(0, 1), 1);
        Assert.IsTrue(island.IsWalkableAt(upperNeighbour, 1));
        Assert.IsFalse(StairWalkMath.InCorridor(upperNeighbour, Front, Back, island));
    }

    [Test]
    public void ClampToCorridor_Pulls_Off_Axis_Step_Back_Into_Band()
    {
        var island = StarterIsland.Build();
        StairWalkMath.ResolveEnds(Front, Back, island,
            out var low, out int lowTier, out var high, out int highTier);

        StairWalkMath.CorridorSegment(low, lowTier, high, highTier, out var start, out var end);
        Vector2 mid = Vector2.Lerp(start, end, 0.55f);
        Vector2 off   = mid + StairWalkMath.CorridorNormal(Front, Back, island) * 0.4f;

        Assert.IsFalse(StairWalkMath.InCorridor(off, Front, Back, island));
        Vector2 clamped = StairWalkMath.ClampToCorridor(off, low, lowTier, high, highTier);
        Assert.IsTrue(StairWalkMath.InCorridor(clamped, Front, Back, island));
    }
}
