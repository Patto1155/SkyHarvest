// Assets/Tests/EditMode/ExpansionTests.cs
// Island outward-growth via scaffolding (spec §3): placing scaffolding adds new
// Scaffold cells in every empty orthogonal direction and demotes the scaffold
// cell from the edge. Pure-static logic, runs under tools/check.sh.
using NUnit.Framework;
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Island;

[TestFixture]
public class ExpansionTests
{
    private static IslandData IslandWithCell(Vector2Int pos, bool isEdge = true)
    {
        var island = new IslandData(seed: 0);
        island.Cells[pos] = new IslandCell
        {
            GridPos   = pos,
            Terrain   = TerrainType.CliffEdge,
            Elevation = 1.5f,
            Soil      = new SoilState(TerrainType.CliffEdge),
            IsEdge    = isEdge
        };
        return island;
    }

    [Test]
    public void Expand_From_Isolated_Cell_Adds_Four_Scaffold_Cells()
    {
        var origin = new Vector2Int(0, 0);
        var island = IslandWithCell(origin);

        var added = IslandExpansion.Expand(island, origin);

        Assert.AreEqual(4, added.Count);
        foreach (var cell in added)
        {
            Assert.AreEqual(TerrainType.Scaffold, cell.Terrain);
            Assert.IsTrue(cell.IsEdge);
            Assert.AreEqual(1.5f, cell.Elevation, 0.001f, "inherits scaffold-cell elevation");
            Assert.IsTrue(island.IsValidPosition(cell.GridPos), "new cell is in the island");
        }
    }

    [Test]
    public void Expand_Skips_Already_Occupied_Directions()
    {
        var origin = new Vector2Int(0, 0);
        var island = IslandWithCell(origin);
        // Occupy the +x neighbour so only 3 empty directions remain.
        island.Cells[new Vector2Int(1, 0)] = new IslandCell { GridPos = new Vector2Int(1, 0) };

        var added = IslandExpansion.Expand(island, origin);

        Assert.AreEqual(3, added.Count);
        CollectionAssert.DoesNotContain(
            added.ConvertAll(c => c.GridPos), new Vector2Int(1, 0));
    }

    [Test]
    public void Expand_Demotes_Scaffold_Cell_From_Edge()
    {
        var origin = new Vector2Int(0, 0);
        var island = IslandWithCell(origin, isEdge: true);

        IslandExpansion.Expand(island, origin);

        Assert.IsFalse(island.GetCell(origin).IsEdge,
            "the scaffolded cell is now interior, not the outermost edge");
    }

    [Test]
    public void Expand_Publishes_IslandExpandedEvent_Once_With_Count()
    {
        var origin = new Vector2Int(0, 0);
        var island = IslandWithCell(origin);

        int fires = 0;
        int reported = 0;
        void Handler(IslandExpandedEvent e) { fires++; reported = e.NewCellCount; }
        EventBus.Subscribe<IslandExpandedEvent>(Handler);
        try
        {
            IslandExpansion.Expand(island, origin);
        }
        finally
        {
            EventBus.Unsubscribe<IslandExpandedEvent>(Handler);
        }

        Assert.AreEqual(1, fires, "expansion fires the event exactly once");
        Assert.AreEqual(4, reported);
    }
}
