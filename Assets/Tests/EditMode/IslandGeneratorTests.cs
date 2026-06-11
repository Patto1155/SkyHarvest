// Assets/Tests/EditMode/IslandGeneratorTests.cs
// Tests: same seed → identical island; different seeds differ;
//        all islands have ≥1 cliff-edge cell and ≥30 cells at radius 12;
//        expansion adds cells.
using NUnit.Framework;
using System.Linq;
using SkyHarvest.Island;
using UnityEngine;

public class IslandGeneratorTests
{
    private const int TestRadius = 12;

    [Test]
    public void Same_Seed_Produces_Identical_Island()
    {
        var a = IslandGenerator.Generate(42, TestRadius);
        var b = IslandGenerator.Generate(42, TestRadius);

        Assert.AreEqual(a.Cells.Count, b.Cells.Count,
            "Cell counts must match for the same seed.");

        foreach (var pos in a.Cells.Keys)
        {
            Assert.IsTrue(b.Cells.ContainsKey(pos),
                $"Cell {pos} present in A but missing in B.");
            Assert.AreEqual(a.Cells[pos].Terrain, b.Cells[pos].Terrain,
                $"Terrain type mismatch at {pos}.");
        }
    }

    [Test]
    public void Different_Seeds_Produce_Different_Islands()
    {
        var a = IslandGenerator.Generate(1, TestRadius);
        var b = IslandGenerator.Generate(2, TestRadius);

        // Very unlikely to have identical cell sets with different seeds
        bool same = a.Cells.Count == b.Cells.Count &&
                    a.Cells.Keys.All(k => b.Cells.ContainsKey(k));
        Assert.IsFalse(same,
            "Seeds 1 and 2 should produce different islands.");
    }

    [Test]
    public void Island_Has_At_Least_One_CliffEdge_Cell()
    {
        var island = IslandGenerator.Generate(12345, TestRadius);
        bool hasCliff = island.Cells.Values.Any(c => c.Terrain == TerrainType.CliffEdge);
        Assert.IsTrue(hasCliff, "Island should contain at least one cliff-edge cell.");
    }

    [Test]
    public void Island_Has_At_Least_30_Cells_At_Radius_12()
    {
        var island = IslandGenerator.Generate(99999, TestRadius);
        Assert.GreaterOrEqual(island.Cells.Count, 30,
            "A radius-12 island should have at least 30 cells.");
    }

    [Test]
    public void Expansion_Adds_Cells()
    {
        var island = IslandGenerator.Generate(7, TestRadius);

        // Find an edge cell to scaffold
        var edgeCell = island.Cells.Values.FirstOrDefault(c => c.IsEdge);
        Assume.That(edgeCell, Is.Not.Null, "Island must have at least one edge cell.");

        int before   = island.Cells.Count;
        var newCells = IslandExpansion.Expand(island, edgeCell!.GridPos);
        int after    = island.Cells.Count;

        Assert.Greater(after, before,
            "Expansion should add at least one new cell.");
        Assert.Greater(newCells.Count, 0,
            "Expand() should return the newly created cells.");
    }

    [Test]
    public void Scaffold_Cells_Have_Scaffold_Terrain()
    {
        var island = IslandGenerator.Generate(7, TestRadius);
        var edgeCell = island.Cells.Values.First(c => c.IsEdge);
        var newCells = IslandExpansion.Expand(island, edgeCell.GridPos);

        foreach (var cell in newCells)
            Assert.AreEqual(TerrainType.Scaffold, cell.Terrain,
                "Newly expanded cells should be Scaffold terrain.");
    }
}
