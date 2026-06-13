// Assets/Tests/EditMode/SaveLoadTests.cs
using NUnit.Framework;
using SkyHarvest.SaveLoad;
using SkyHarvest.Workshop;
using UnityEngine;

public class SaveLoadTests
{
    [Test]
    public void WorldSaveData_Serializes_To_Json_And_Back()
    {
        var save = new WorldSaveData
        {
            GameTimeMinutes = 123.5f,
            WeatherState = "ClearSkies",
            WeatherTimeRemaining = 4.2f,
            Island = new IslandSaveData { Seed = 42 },
            Player = new PlayerSaveData { PosX = 1f, PosY = 2f, PosZ = 3f }
        };

        string json = JsonUtility.ToJson(save);
        var loaded = JsonUtility.FromJson<WorldSaveData>(json);

        Assert.AreEqual(123.5f, loaded.GameTimeMinutes, 0.01f);
        Assert.AreEqual(42, loaded.Island.Seed);
        Assert.AreEqual(1f, loaded.Player.PosX, 0.01f);
    }

    [Test]
    public void CropSaveData_Preserves_Growth()
    {
        var crop = new CropSaveData
        {
            CropId = "wheat",
            GrowthProgress = 0.75f,
            Health = 0.9f,
            GridX = 3,
            GridY = -2
        };

        string json = JsonUtility.ToJson(crop);
        var loaded = JsonUtility.FromJson<CropSaveData>(json);

        Assert.AreEqual("wheat", loaded.CropId);
        Assert.AreEqual(0.75f, loaded.GrowthProgress, 0.01f);
    }

    [Test]
    public void WorkshopSaveData_Preserves_InProgress_Batch()
    {
        var workshop = new WorkshopSaveData
        {
            GridX = 2,
            GridY = -1,
            RecipeId = "wheat_to_flour",
            OutputItemId = "flour",
            OutputAmount = 2,
            TotalSeconds = 15f,
            ElapsedSeconds = 7.5f,
            State = "Processing"
        };

        string json = JsonUtility.ToJson(workshop);
        var loaded = JsonUtility.FromJson<WorkshopSaveData>(json);

        Assert.AreEqual("wheat_to_flour", loaded.RecipeId);
        Assert.AreEqual(7.5f, loaded.ElapsedSeconds, 0.01f);
        Assert.AreEqual("Processing", loaded.State);
    }

    [Test]
    public void WorkshopProcess_Restore_Resumes_Progress()
    {
        var process = new WorkshopProcess();
        process.Restore("wheat_to_flour", "flour", 2, 15f, 7.5f, WorkshopProcess.State.Processing);

        Assert.AreEqual(WorkshopProcess.State.Processing, process.CurrentState);
        Assert.AreEqual(0.5f, process.Progress, 0.01f);
        Assert.AreEqual("flour", process.OutputItemId);
    }
}
