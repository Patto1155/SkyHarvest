// Assets/Tests/EditMode/GridMathTests.cs
// Round-trip grid→world→grid tests.
using NUnit.Framework;
using SkyHarvest.Core;
using UnityEngine;

public class GridMathTests
{
    private static readonly Vector2Int[] TestCells =
    {
        new Vector2Int( 0,  0),
        new Vector2Int( 5,  3),
        new Vector2Int(-4,  6),
        new Vector2Int( 7, -2),
        new Vector2Int(-8, -5),
    };

    [Test]
    public void GridToWorld_Origin_Is_Zero()
    {
        Vector2 w = GridMath.GridToWorld(Vector2Int.zero);
        Assert.AreEqual(0f, w.x, 0.001f);
        Assert.AreEqual(0f, w.y, 0.001f);
    }

    [Test]
    public void RoundTrip_Grid_World_Grid([ValueSource(nameof(TestCells))] Vector2Int cell)
    {
        Vector2 world     = GridMath.GridToWorld(cell);
        Vector2Int back   = GridMath.WorldToGrid(world);
        Assert.AreEqual(cell.x, back.x, $"x mismatch for {cell}");
        Assert.AreEqual(cell.y, back.y, $"y mismatch for {cell}");
    }

    [Test]
    public void GridToWorld_Positive_X_Moves_Right_And_Down()
    {
        // gx increases → worldX increases, worldY decreases (goes down screen)
        Vector2 origin = GridMath.GridToWorld(new Vector2Int(0, 0));
        Vector2 east   = GridMath.GridToWorld(new Vector2Int(1, 0));
        Assert.Greater(east.x, origin.x);
        Assert.Less(east.y,    origin.y);
    }

    [Test]
    public void GridToWorld_Positive_Y_Moves_Left_And_Down()
    {
        Vector2 origin = GridMath.GridToWorld(new Vector2Int(0, 0));
        Vector2 south  = GridMath.GridToWorld(new Vector2Int(0, 1));
        Assert.Less(south.x,   origin.x);
        Assert.Less(south.y,   origin.y);
    }

    [Test]
    public void Elevation_Shifts_WorldY_Up()
    {
        Vector2 flat    = GridMath.GridToWorld(new Vector2Int(3, 3), 0f);
        Vector2 raised  = GridMath.GridToWorld(new Vector2Int(3, 3), 1f);
        Assert.Greater(raised.y, flat.y,
            "Higher elevation should increase world-Y (moves sprite up-screen).");
    }

    [Test]
    public void SortingOrder_Lower_WorldY_Gets_Higher_Order()
    {
        // Sprite lower on screen (smaller worldY) sorts in FRONT of higher sprites
        int order1 = GridMath.SortingOrder(worldY:  1f);
        int order2 = GridMath.SortingOrder(worldY: -1f);
        Assert.Greater(order2, order1,
            "worldY=-1 (lower on screen) should have higher sorting order than worldY=1.");
    }

    [Test]
    public void SortingOrder_Bias_Is_Added()
    {
        int base_   = GridMath.SortingOrder(0f,  0);
        int biased  = GridMath.SortingOrder(0f, -10000);
        Assert.AreEqual(base_ - 10000, biased);
    }
}
