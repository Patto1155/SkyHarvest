// Assets/Tests/EditMode/CropGrowthTests.cs
// Verbatim from plan Task 13, plus restore-constructor test.
using NUnit.Framework;
using SkyHarvest.Farming;
using SkyHarvest.Island;
using SkyHarvest.Core;

public class CropGrowthTests
{
    [SetUp]
    public void SetUp() => EventBus.Clear();

    [Test]
    public void Crop_Grows_Over_Time_In_Good_Soil()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.AddWater(80f);
        var crop = new CropState("wheat", growthTimeMinutes: 5f, stages: 4, waterPerMinute: 2f);
        crop.Tick(2.5f, soil, sunExposure: 1f, windDamage: 0f);
        Assert.AreEqual(1, crop.CurrentStage);
    }

    [Test]
    public void Crop_Does_Not_Grow_Without_Water()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        // No water
        var crop = new CropState("wheat", growthTimeMinutes: 5f, stages: 4, waterPerMinute: 2f);
        crop.Tick(5f, soil, sunExposure: 1f, windDamage: 0f);
        Assert.AreEqual(0, crop.CurrentStage);
    }

    [Test]
    public void Crop_Reaches_Harvestable_State()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.AddWater(100f);
        var crop = new CropState("wheat", growthTimeMinutes: 5f, stages: 4, waterPerMinute: 2f);
        crop.Tick(6f, soil, sunExposure: 1f, windDamage: 0f);
        Assert.IsTrue(crop.IsHarvestable);
    }

    [Test]
    public void Wind_Damage_Reduces_Health()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.AddWater(50f);
        var crop = new CropState("wheat", growthTimeMinutes: 5f, stages: 4, waterPerMinute: 2f);
        crop.Tick(1f, soil, sunExposure: 1f, windDamage: 0.5f);
        Assert.Less(crop.Health, 1f);
    }

    [Test]
    public void Dead_Crop_Is_Not_Harvestable()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        var crop = new CropState("wheat", growthTimeMinutes: 5f, stages: 4, waterPerMinute: 2f);
        for (int i = 0; i < 20; i++)
            crop.Tick(0.5f, soil, sunExposure: 0f, windDamage: 1f);
        Assert.IsTrue(crop.IsDead);
        Assert.IsFalse(crop.IsHarvestable);
    }

    [Test]
    public void CropReadyEvent_Fires_When_Crop_Fully_Grown()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.AddWater(100f);
        string? readyId = null;
        EventBus.Subscribe<CropReadyEvent>(e => readyId = e.CropId);

        var crop = new CropState("sky_moss", growthTimeMinutes: 2f, stages: 4, waterPerMinute: 1f);
        crop.Tick(10f, soil, sunExposure: 1f, windDamage: 0f);

        Assert.AreEqual("sky_moss", readyId);
    }

    [Test]
    public void CropDiedEvent_Fires_When_Crop_Dies()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        string? diedId = null;
        EventBus.Subscribe<CropDiedEvent>(e => diedId = e.CropId);

        var crop = new CropState("sky_moss", growthTimeMinutes: 2f, stages: 4, waterPerMinute: 1f);
        for (int i = 0; i < 30; i++)
            crop.Tick(0.5f, soil, sunExposure: 0f, windDamage: 1f);

        Assert.AreEqual("sky_moss", diedId);
    }

    [Test]
    public void Restore_Constructor_Preserves_Progress_And_Health()
    {
        var saved = new CropState("wheat", 5f, 4, 2f, savedProgress: 0.75f, savedHealth: 0.6f);
        Assert.AreEqual(0.75f, saved.GrowthProgress, 0.001f);
        Assert.AreEqual(0.6f,  saved.Health,         0.001f);
    }
}
