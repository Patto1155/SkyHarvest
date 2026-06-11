// Assets/Tests/EditMode/SoilTests.cs
// Verbatim from plan Task 6, adapted for SoilState (not SoilPatch MonoBehaviour).
using NUnit.Framework;
using SkyHarvest.Island;

public class SoilTests
{
    [Test]
    public void New_Soil_Has_Terrain_Based_Quality()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        Assert.AreEqual(80f, soil.Quality, 0.1f);
    }

    [Test]
    public void Watering_Increases_Water_Level()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.AddWater(30f);
        Assert.AreEqual(30f, soil.WaterLevel, 0.1f);
    }

    [Test]
    public void Water_Clamped_To_Max()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.AddWater(200f);
        Assert.AreEqual(100f, soil.WaterLevel, 0.1f);
    }

    [Test]
    public void Planting_Same_Crop_Depletes_Nutrients()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.RecordHarvest("wheat");
        soil.RecordHarvest("wheat");
        Assert.Less(soil.Nutrients, 100f);
    }

    [Test]
    public void Composting_Restores_Nutrients()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.RecordHarvest("wheat");
        soil.RecordHarvest("wheat");
        float depleted = soil.Nutrients;
        soil.ApplyCompost(20f);
        Assert.Greater(soil.Nutrients, depleted);
    }

    [Test]
    public void Crop_Rotation_Reduces_Depletion()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.RecordHarvest("wheat");
        float afterWheat = soil.Nutrients;
        soil.RecordHarvest("beans"); // different crop
        float afterBeans = soil.Nutrients;

        float wheatDepletion = 100f - afterWheat;
        float beansDepletion = afterWheat - afterBeans;
        Assert.Less(beansDepletion, wheatDepletion);
    }

    [Test]
    public void SetState_Restores_Water_And_Nutrients()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.SetState(42f, 77f);
        Assert.AreEqual(42f, soil.WaterLevel, 0.01f);
        Assert.AreEqual(77f, soil.Nutrients,  0.01f);
    }
}
