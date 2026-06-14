// Assets/Tests/EditMode/AutotileTests.cs
// NUnit tests for the pure TerrainAutotiler logic.
// Runs under tools/check.sh (headless .NET 8) and Unity EditMode test runner.
using NUnit.Framework;
using UnityEngine;
using SkyHarvest.Island;
using System.Linq;

[TestFixture]
public class AutotileTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static IslandData SingleCellIsland(Vector2Int pos, TerrainType terrain = TerrainType.FertileValley)
    {
        var island = new IslandData(seed: 0);
        island.Cells[pos] = MakeCell(pos, terrain);
        return island;
    }

    private static IslandData IslandWithNeighbours(
        TerrainType centerTerrain,
        TerrainType neighbourTerrain,
        bool addAllEightNeighbours = true)
    {
        var center = new Vector2Int(0, 0);
        var island = new IslandData(seed: 0);
        island.Cells[center] = MakeCell(center, centerTerrain);

        if (addAllEightNeighbours)
        {
            foreach (var off in TerrainAutotiler.NeighbourOffsets)
            {
                var nPos = center + off;
                island.Cells[nPos] = MakeCell(nPos, neighbourTerrain);
            }
        }
        return island;
    }

    private static IslandCell MakeCell(Vector2Int pos, TerrainType terrain = TerrainType.FertileValley)
    {
        return new IslandCell
        {
            GridPos   = pos,
            Terrain   = terrain,
            Elevation = 0f,
            Soil      = new SoilState(terrain),
        };
    }

    // -----------------------------------------------------------------------
    // Bitmask tests
    // -----------------------------------------------------------------------

    [Test]
    public void Compute_AllSameNeighbours_BitmaskIsZero()
    {
        var island = IslandWithNeighbours(TerrainType.FertileValley, TerrainType.FertileValley);
        var cell   = island.GetCell(new Vector2Int(0, 0))!;

        var result = TerrainAutotiler.Compute(cell, island);

        Assert.AreEqual(0, result.Bitmask, "All same-terrain neighbours → no blending needed");
        Assert.IsFalse(result.HasAnyBlend);
        Assert.AreEqual(0, result.Samples.Count);
    }

    [Test]
    public void Compute_AllAbsentNeighbours_BitmaskHasAllBitsSet()
    {
        // Only the center cell exists — all 8 neighbours are absent.
        var island = SingleCellIsland(new Vector2Int(0, 0));
        var cell   = island.GetCell(new Vector2Int(0, 0))!;

        var result = TerrainAutotiler.Compute(cell, island);

        // All 8 bits set = 0xFF = 255
        Assert.AreEqual(0xFF, result.Bitmask, "All absent neighbours → all 8 bits set");
        Assert.IsTrue(result.HasAnyBlend);
    }

    [Test]
    public void Compute_OneDifferentOrthogonalNeighbour_BitSetAndSamplePresent()
    {
        // Center = FertileValley. All neighbours = FertileValley except neighbour[2] (offset +1,0) = RockyPlateau.
        var island = IslandWithNeighbours(TerrainType.FertileValley, TerrainType.FertileValley);
        var diffPos = new Vector2Int(0, 0) + TerrainAutotiler.NeighbourOffsets[2];
        island.Cells[diffPos] = MakeCell(diffPos, TerrainType.RockyPlateau);

        var cell   = island.GetCell(new Vector2Int(0, 0))!;
        var result = TerrainAutotiler.Compute(cell, island);

        Assert.IsTrue((result.Bitmask & (1 << 2)) != 0, "Bit 2 should be set for the different neighbour");

        // The orthogonal sample should be present with the rocky colour reference
        var sample = result.Samples.FirstOrDefault(s => s.NeighbourOffset == TerrainAutotiler.NeighbourOffsets[2]);
        Assert.IsNotNull(sample, "Expected a BlendSample for the different orthogonal neighbour");
        // Use struct default comparison — the struct has NeighbourOffset
        bool found = false;
        foreach (var s in result.Samples)
            if (s.NeighbourOffset == TerrainAutotiler.NeighbourOffsets[2]) { found = true; break; }
        Assert.IsTrue(found, "BlendSample for index-2 offset must be present");
    }

    [Test]
    public void Compute_DifferentNeighbours_SamplesCountMatchesOrthogonalDifferences()
    {
        // All 8 neighbours are absent → center has 4 orthogonal + up to 4 diagonal samples.
        var island = SingleCellIsland(new Vector2Int(0, 0));
        var cell   = island.GetCell(new Vector2Int(0, 0))!;

        var result = TerrainAutotiler.Compute(cell, island);

        // 4 orthogonal always included; diagonals only if their flanking orthogonals also differ.
        // With all absent, all orthogonals differ so all diagonals qualify → 8 samples.
        Assert.AreEqual(8, result.Samples.Count, "Single cell surrounded by void should have 8 blend samples");
    }

    [Test]
    public void Compute_DiagonalOnly_NotIncludedWhenFlanksMatch()
    {
        // Center = FertileValley. Place ONLY a diagonal neighbour as different, keep flanks same.
        // Diagonal index 1 = offset (+1,-1). Flanks are index 0 (+0,-1) and index 2 (+1,0).
        var island = IslandWithNeighbours(TerrainType.FertileValley, TerrainType.FertileValley);

        // Make the diagonal [1] different:
        var diagPos = new Vector2Int(0, 0) + TerrainAutotiler.NeighbourOffsets[1];
        island.Cells[diagPos] = MakeCell(diagPos, TerrainType.CliffEdge);

        // Ensure both flanks [0] and [2] are SAME terrain:
        var flank0Pos = new Vector2Int(0, 0) + TerrainAutotiler.NeighbourOffsets[0];
        var flank2Pos = new Vector2Int(0, 0) + TerrainAutotiler.NeighbourOffsets[2];
        island.Cells[flank0Pos] = MakeCell(flank0Pos, TerrainType.FertileValley);
        island.Cells[flank2Pos] = MakeCell(flank2Pos, TerrainType.FertileValley);

        var cell   = island.GetCell(new Vector2Int(0, 0))!;
        var result = TerrainAutotiler.Compute(cell, island);

        // Bit 1 should be set (the diagonal IS different)...
        Assert.IsTrue((result.Bitmask & (1 << 1)) != 0, "Bitmask bit for different diagonal should be set");

        // ...but NO blend sample should be added for it (flanks are same).
        bool diagSamplePresent = false;
        foreach (var s in result.Samples)
            if (s.NeighbourOffset == TerrainAutotiler.NeighbourOffsets[1]) { diagSamplePresent = true; break; }
        Assert.IsFalse(diagSamplePresent,
            "A diagonal-only difference with matching flanks should NOT produce a BlendSample");
    }

    [Test]
    public void Compute_AbsentNeighbour_NeighbourTerrainIsNull()
    {
        // A single cell with all neighbours absent: samples should have null NeighbourTerrain.
        var island = SingleCellIsland(new Vector2Int(0, 0));
        var cell   = island.GetCell(new Vector2Int(0, 0))!;

        var result = TerrainAutotiler.Compute(cell, island);

        foreach (var s in result.Samples)
            Assert.IsNull(s.NeighbourTerrain,
                "Off-island neighbours should produce samples with null NeighbourTerrain");
    }

    [Test]
    public void Compute_PresentDifferentNeighbour_NeighbourTerrainIsSet()
    {
        var island = IslandWithNeighbours(TerrainType.FertileValley, TerrainType.FertileValley);
        var diffPos = new Vector2Int(0, 0) + TerrainAutotiler.NeighbourOffsets[0];
        island.Cells[diffPos] = MakeCell(diffPos, TerrainType.CliffEdge);

        var cell   = island.GetCell(new Vector2Int(0, 0))!;
        var result = TerrainAutotiler.Compute(cell, island);

        bool found = false;
        foreach (var s in result.Samples)
        {
            if (s.NeighbourOffset == TerrainAutotiler.NeighbourOffsets[0])
            {
                Assert.AreEqual(TerrainType.CliffEdge, s.NeighbourTerrain,
                    "Sample for a present different neighbour should carry its terrain");
                found = true;
                break;
            }
        }
        Assert.IsTrue(found, "Expected a sample for the different present neighbour");
    }

    // -----------------------------------------------------------------------
    // Weight tests
    // -----------------------------------------------------------------------

    [Test]
    public void Compute_OrthogonalSample_HasFullWeight()
    {
        var island = SingleCellIsland(new Vector2Int(0, 0));
        var cell   = island.GetCell(new Vector2Int(0, 0))!;

        var result = TerrainAutotiler.Compute(cell, island);

        // Orthogonal indices are even: 0, 2, 4, 6
        foreach (var s in result.Samples)
        {
            // Identify as orthogonal by checking if it's an even index
            bool isOrthogonal = false;
            for (int i = 0; i < 8; i += 2)
                if (s.NeighbourOffset == TerrainAutotiler.NeighbourOffsets[i]) { isOrthogonal = true; break; }

            if (isOrthogonal)
                Assert.AreEqual(1.0f, s.Weight, 0.001f, "Orthogonal samples should have weight 1.0");
        }
    }

    [Test]
    public void Compute_DiagonalSample_HasReducedWeight()
    {
        var island = SingleCellIsland(new Vector2Int(0, 0));
        var cell   = island.GetCell(new Vector2Int(0, 0))!;

        var result = TerrainAutotiler.Compute(cell, island);

        // Diagonal indices are odd: 1, 3, 5, 7
        foreach (var s in result.Samples)
        {
            bool isDiagonal = false;
            for (int i = 1; i < 8; i += 2)
                if (s.NeighbourOffset == TerrainAutotiler.NeighbourOffsets[i]) { isDiagonal = true; break; }

            if (isDiagonal)
                Assert.Less(s.Weight, 1.0f, "Diagonal samples should have weight < 1.0");
        }
    }

    // -----------------------------------------------------------------------
    // AffectedPositions tests
    // -----------------------------------------------------------------------

    [Test]
    public void AffectedPositions_ReturnsNinePositions()
    {
        var pos = new Vector2Int(3, 5);
        var affected = new System.Collections.Generic.HashSet<Vector2Int>(
            TerrainAutotiler.AffectedPositions(pos));

        // Center + 8 neighbours = 9 unique positions
        Assert.AreEqual(9, affected.Count, "AffectedPositions should return center + 8 neighbours");
    }

    [Test]
    public void AffectedPositions_IncludesCenterAndAllNeighbours()
    {
        var pos = new Vector2Int(0, 0);
        var affected = new System.Collections.Generic.HashSet<Vector2Int>(
            TerrainAutotiler.AffectedPositions(pos));

        Assert.IsTrue(affected.Contains(pos), "Center position must be included");
        foreach (var off in TerrainAutotiler.NeighbourOffsets)
            Assert.IsTrue(affected.Contains(pos + off), $"Neighbour at offset {off} must be included");
    }

    // -----------------------------------------------------------------------
    // World direction sanity tests
    // -----------------------------------------------------------------------

    [Test]
    public void NeighbourOffsets_Has8Entries()
    {
        Assert.AreEqual(8, TerrainAutotiler.NeighbourOffsets.Length);
    }

    [Test]
    public void NeighbourOffsets_AreAllDistinct()
    {
        var seen = new System.Collections.Generic.HashSet<Vector2Int>();
        foreach (var off in TerrainAutotiler.NeighbourOffsets)
            Assert.IsTrue(seen.Add(off), $"Duplicate neighbour offset: {off}");
    }
}
