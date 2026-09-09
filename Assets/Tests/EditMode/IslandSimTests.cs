// Assets/Tests/EditMode/IslandSimTests.cs
// The offline island simulation (spec 2026-09-09 §3-4): devices share one water network and
// power grid; the run saturates on buffers instead of ever destroying anything.
using NUnit.Framework;
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Data;
using SkyHarvest.Farming;
using SkyHarvest.Island;
using SkyHarvest.Player;
using SkyHarvest.Sim;
using SkyHarvest.Workshop;

[TestFixture]
public class IslandSimTests
{
    private IslandSim _sim = null!;

    [SetUp]
    public void SetUp()
    {
        EventBus.Clear();
        _sim = new IslandSim(new WaterNetwork(), new PowerGrid()) { Rng = new System.Random(1) };
    }

    private SimPlot AddPlot(int x, int y, string? cropId = null, float water = 0f, bool ripe = false)
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.Till();
        if (water > 0f) soil.AddWater(water);
        CropState? crop = null;
        if (cropId != null)
        {
            var def = GameDatabase.GetCrop(cropId)!;
            crop = ripe
                ? new CropState(def.CropId, def.GrowthTimeMinutes, def.GrowthStages, def.WaterConsumptionPerMinute, 1f, 1f)
                : new CropState(def.CropId, def.GrowthTimeMinutes, def.GrowthStages, def.WaterConsumptionPerMinute);
        }
        var plot = new SimPlot { GridPos = new Vector2Int(x, y), Soil = soil, Crop = crop, LastCropId = cropId };
        _sim.Plots.Add(plot);
        return plot;
    }

    private SimStorage AddStorage(int x, int y, int slots = 10)
    {
        var s = new SimStorage { GridPos = new Vector2Int(x, y), Inventory = new Inventory(slots) };
        _sim.Storages.Add(s);
        return s;
    }

    private void Finish() => _sim.RebuildDeviceIndex();

    // ---- capacities -------------------------------------------------------

    [Test]
    public void Tanks_And_Batteries_Raise_Capacities()
    {
        _sim.Devices.Add(new WaterTank(new Vector2Int(0, 0)));
        _sim.Devices.Add(new Battery(new Vector2Int(1, 0)));
        _sim.Devices.Add(new Battery(new Vector2Int(2, 0)));
        Finish();
        Assert.AreEqual(WaterNetwork.BaseCapacity + WaterTank.CapacityBonus, _sim.Water.Capacity, 0.001f);
        Assert.AreEqual(PowerGrid.BaseCapacity + 2 * Battery.CapacityBonus, _sim.Power.Capacity, 0.001f);
    }

    [Test]
    public void Windmill_On_Wind_Corridor_Generates_Double()
    {
        _sim.Devices.Add(new Windmill(new Vector2Int(0, 0), onWindCorridor: true));
        _sim.Devices.Add(new Windmill(new Vector2Int(5, 5), onWindCorridor: false));
        Finish();
        Assert.AreEqual(Windmill.BaseGenerationPerSecond * 3f, _sim.GenerationPerSecond, 0.0001f);

        _sim.Step(10f, SimEnvironment.Offline, tickCrops: false, tickWorkshops: false, new OfflineReport());
        Assert.AreEqual(Windmill.BaseGenerationPerSecond * 3f * 10f, _sim.Power.Stored, 0.001f);
    }

    // ---- sprinkler --------------------------------------------------------

    [Test]
    public void Sprinkler_Waters_Covered_Plots_From_Network_Only()
    {
        var inRange  = AddPlot(1, 1, "sky_moss");
        var outRange = AddPlot(4, 4, "sky_moss");
        _sim.Devices.Add(new Sprinkler(new Vector2Int(0, 0)));
        Finish();
        _sim.Water.Add(50f);

        _sim.Step(60f, SimEnvironment.Offline, tickCrops: false, tickWorkshops: false, new OfflineReport());

        Assert.Greater(inRange.Soil.WaterLevel, 0f);
        Assert.AreEqual(0f, outRange.Soil.WaterLevel, 0.001f);
        Assert.Less(_sim.Water.Stored, 50f);
    }

    [Test]
    public void Sprinkler_Without_Water_Marks_Network_Empty_And_Crops_Pause()
    {
        var plot = AddPlot(0, 1, "sky_moss");
        _sim.Devices.Add(new Sprinkler(new Vector2Int(0, 0)));
        Finish();

        var report = new OfflineReport();
        _sim.Step(60f, SimEnvironment.Offline, tickCrops: true, tickWorkshops: false, report);

        Assert.AreEqual(0f, plot.Crop!.GrowthProgress, 0.001f);   // paused, not dead
        Assert.IsFalse(plot.Crop.IsDead);
        Assert.GreaterOrEqual(report.WaterEmptyAt, 0f);
    }

    // ---- growth -----------------------------------------------------------

    [Test]
    public void Offline_Growth_Reaches_Harvestable_Given_Water()
    {
        var plot = AddPlot(0, 0, "sky_moss", water: 100f);   // 2-minute crop
        Finish();

        var report = _sim.Advance(elapsedSeconds: 600, capped: false, stepSeconds: 60f);

        Assert.IsTrue(plot.Crop!.IsHarvestable);
        Assert.AreEqual(600, report.ElapsedSeconds);
    }

    [Test]
    public void Offline_Environment_Has_No_Wind_So_Nothing_Dies()
    {
        var plot = AddPlot(0, 0, "sky_moss");
        Finish();
        _sim.Advance(4 * 3600, capped: true);
        Assert.IsFalse(plot.Crop!.IsDead);
        Assert.AreEqual(1f, plot.Crop.Health, 0.001f);
    }

    [Test]
    public void Wind_Totem_Shields_Covered_Plots_During_Storms()
    {
        var shielded = AddPlot(1, 0, "sky_moss", water: 50f);
        var exposed  = AddPlot(5, 5, "sky_moss", water: 50f);
        _sim.Devices.Add(new WindTotem(new Vector2Int(0, 0)));
        Finish();

        var storm = new SimEnvironment { SunExposure = 0.2f, WindDamage = 0.8f };
        _sim.Step(60f, storm, tickCrops: true, tickWorkshops: false, new OfflineReport());

        Assert.AreEqual(1f, shielded.Crop!.Health, 0.001f);
        Assert.Less(exposed.Crop!.Health, 1f);
        Assert.IsTrue(_sim.IsWindProtected(new Vector2Int(1, 1)));
        Assert.IsFalse(_sim.IsWindProtected(new Vector2Int(2, 2)));
    }

    // ---- tender's post ----------------------------------------------------

    [Test]
    public void Tenders_Post_Harvests_Into_Adjacent_Storage_And_Replants()
    {
        var plot = AddPlot(1, 1, "storm_wheat", ripe: true);
        var crate = AddStorage(0, -1);           // adjacent to the post at (0,0)
        crate.Inventory.TryAdd("wheat_seed", 3);
        _sim.Devices.Add(new TendersPost(new Vector2Int(0, 0)));
        Finish();
        _sim.Power.Add(20f);

        var report = new OfflineReport();
        _sim.Step(60f, SimEnvironment.Offline, tickCrops: false, tickWorkshops: false, report);

        Assert.GreaterOrEqual(crate.Inventory.GetCount("wheat"), 2);
        Assert.AreEqual(2, crate.Inventory.GetCount("wheat_seed"));
        Assert.IsNotNull(plot.Crop);
        Assert.AreEqual("storm_wheat", plot.Crop!.CropId);
        Assert.AreEqual(0f, plot.Crop.GrowthProgress, 0.001f);
        Assert.AreEqual(1, report.Replanted);
        Assert.Greater(report.TotalHarvested, 0);
        Assert.Less(_sim.Power.Stored, 20f);
    }

    [Test]
    public void Tenders_Post_Replants_Empty_Tilled_Plot_From_Any_Seed()
    {
        var plot = AddPlot(0, 1);                       // tilled, empty, no history
        var crate = AddStorage(1, 0);
        crate.Inventory.TryAdd("herb_seed", 1);
        _sim.Devices.Add(new TendersPost(new Vector2Int(0, 0)));
        Finish();
        _sim.Power.Add(20f);

        _sim.Step(60f, SimEnvironment.Offline, false, false, new OfflineReport());

        Assert.AreEqual("herb_plant", plot.Crop!.CropId);
        Assert.AreEqual(0, crate.Inventory.GetCount("herb_seed"));
    }

    [Test]
    public void Tenders_Post_Ignores_Non_Adjacent_Storage()
    {
        AddPlot(1, 1, "storm_wheat", ripe: true);
        var far = AddStorage(3, 3);
        _sim.Devices.Add(new TendersPost(new Vector2Int(0, 0)));
        Finish();
        _sim.Power.Add(20f);

        var report = new OfflineReport();
        _sim.Step(60f, SimEnvironment.Offline, false, false, report);

        Assert.AreEqual(0, far.Inventory.GetCount("wheat"));
        Assert.AreEqual(0, report.TotalHarvested);
    }

    [Test]
    public void Full_Storage_Leaves_Crop_Ripe_And_Is_Reported()
    {
        var plot = AddPlot(1, 1, "storm_wheat", ripe: true);
        var crate = AddStorage(0, 1, slots: 1);
        crate.Inventory.TryAdd("stone", 99);            // the only slot is full
        _sim.Devices.Add(new TendersPost(new Vector2Int(0, 0)));
        Finish();
        _sim.Power.Add(20f);

        var report = new OfflineReport();
        _sim.Step(60f, SimEnvironment.Offline, false, false, report);

        Assert.IsTrue(plot.Crop!.IsHarvestable);
        Assert.GreaterOrEqual(report.StorageFullAt, 0f);
        Assert.AreEqual(0, report.TotalHarvested);
    }

    [Test]
    public void Unpowered_Post_Does_Nothing_And_Is_Reported()
    {
        var plot = AddPlot(1, 1, "storm_wheat", ripe: true);
        AddStorage(0, 1);
        _sim.Devices.Add(new TendersPost(new Vector2Int(0, 0)));
        Finish();

        var report = new OfflineReport();
        _sim.Step(60f, SimEnvironment.Offline, false, false, report);

        Assert.IsTrue(plot.Crop!.IsHarvestable);
        Assert.GreaterOrEqual(report.PowerEmptyAt, 0f);
    }

    // ---- composter --------------------------------------------------------

    [Test]
    public void Composter_Collects_Waste_From_Harvests_And_Feeds_Depleted_Soil()
    {
        var plot = AddPlot(1, 1, "storm_wheat", ripe: true);
        plot.Soil.SetState(water: 0f, nutrients: 30f);
        AddStorage(0, 1);
        var composter = new Composter(new Vector2Int(2, 2));
        _sim.Devices.Add(new TendersPost(new Vector2Int(0, 0)));
        _sim.Devices.Add(composter);
        Finish();
        _sim.Power.Add(20f);

        _sim.Step(60f, SimEnvironment.Offline, false, false, new OfflineReport());

        // harvest gave +1 waste (after RecordHarvest depleted soil further), compost then spent it
        Assert.AreEqual(0, composter.Stock);
        Assert.Greater(plot.Soil.Nutrients, 30f - 15f);
    }

    [Test]
    public void Composter_Skips_Healthy_Soil()
    {
        var plot = AddPlot(1, 1);
        var composter = new Composter(new Vector2Int(0, 0));
        composter.AddWaste(3);
        _sim.Devices.Add(composter);
        Finish();

        _sim.Step(60f, SimEnvironment.Offline, false, false, new OfflineReport());

        Assert.AreEqual(3, composter.Stock);
        Assert.AreEqual(Constants.MaxSoilNutrients, plot.Soil.Nutrients, 0.001f);
    }

    // ---- workshops --------------------------------------------------------

    [Test]
    public void Workshops_Finish_Their_Batch_Offline()
    {
        var process = new WorkshopProcess();
        process.Start("wheat_to_flour", "flour", 2, totalSeconds: 15f);
        _sim.Workshops.Add(new SimWorkshop { GridPos = Vector2Int.zero, Process = process });
        Finish();

        var report = _sim.Advance(120, capped: false);

        Assert.IsTrue(process.IsComplete);
        Assert.AreEqual(1, report.BatchesCompleted);
    }

    // ---- full loop --------------------------------------------------------

    [Test]
    public void Module_Cycles_Repeatedly_While_Away()
    {
        // Sprinkler + Tender's Post + crate with seeds + windmill: a self-running farm module.
        var plot = AddPlot(1, 1, "sky_moss", water: 20f);
        var crate = AddStorage(0, 1);
        crate.Inventory.TryAdd("sky_moss_seed", 20);
        _sim.Devices.Add(new Sprinkler(new Vector2Int(0, 0)));
        _sim.Devices.Add(new TendersPost(new Vector2Int(0, 0)));
        _sim.Devices.Add(new Windmill(new Vector2Int(3, 3), false));
        _sim.Devices.Add(new WaterTank(new Vector2Int(4, 4)));
        Finish();
        _sim.Water.Add(250f);
        _sim.Power.Add(20f);

        var report = _sim.Advance(3600, capped: false);

        Assert.Greater(report.Replanted, 3, "moss is a 2-minute crop; an hour should cycle many times");
        Assert.Greater(crate.Inventory.GetCount("sky_moss"), 3);
        Assert.Less(crate.Inventory.GetCount("sky_moss_seed"), 20);
        Assert.IsNotNull(plot.Crop);
    }
}
