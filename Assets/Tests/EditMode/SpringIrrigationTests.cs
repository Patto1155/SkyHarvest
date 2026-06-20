// Natural spring passive irrigation (spec §4 G9 MVP).
using NUnit.Framework;
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Farming;
using SkyHarvest.Island;

[TestFixture]
public class SpringIrrigationTests
{
    [Test]
    public void Spring_Waters_Adjacent_Tilled_Soil_Each_Tick()
    {
        var island = new IslandData(1);
        var spring = new Vector2Int(2, 2);
        var farm   = new Vector2Int(2, 3);

        island.Cells[spring] = MakeCell(spring, TerrainType.NaturalSpring);
        island.Cells[farm]   = MakeCell(farm, TerrainType.FertileValley);
        island.Cells[farm].Soil.Till();
        island.Cells[farm].IsTilled = true;

        float before = island.Cells[farm].Soil.WaterLevel;
        SpringIrrigation.WaterAdjacentPlots(island, 5f);
        Assert.Greater(island.Cells[farm].Soil.WaterLevel, before);
    }

    [Test]
    public void Spring_Does_Not_Water_NonAdjacent_Cells()
    {
        var island = new IslandData(1);
        var spring = new Vector2Int(0, 0);
        var far    = new Vector2Int(2, 2);

        island.Cells[spring] = MakeCell(spring, TerrainType.NaturalSpring);
        island.Cells[far]    = MakeCell(far, TerrainType.FertileValley);
        island.Cells[far].Soil.Till();
        island.Cells[far].IsTilled = true;

        SpringIrrigation.WaterAdjacentPlots(island, 5f);
        Assert.AreEqual(0f, island.Cells[far].Soil.WaterLevel);
    }

    [Test]
    public void Manual_Water_Bonus_When_Plot_Adjacent_To_Spring()
    {
        var island = new IslandData(1);
        var spring = new Vector2Int(1, 1);
        var farm   = new Vector2Int(1, 2);
        island.Cells[spring] = MakeCell(spring, TerrainType.NaturalSpring);
        island.Cells[farm]   = MakeCell(farm, TerrainType.FertileValley);
        island.Cells[farm].Soil.Till();

        var plot = new GameObject("plot").AddComponent<CropPlot>();
        plot.Soil    = island.Cells[farm].Soil;
        plot.GridPos = farm;

        FarmingActions.Water(plot, island);
        Assert.Greater(plot.Soil.WaterLevel, 35f);
    }

    private static IslandCell MakeCell(Vector2Int pos, TerrainType terrain) => new()
    {
        GridPos   = pos,
        Terrain   = terrain,
        Elevation = 0f,
        Soil      = new SoilState(terrain),
    };
}
