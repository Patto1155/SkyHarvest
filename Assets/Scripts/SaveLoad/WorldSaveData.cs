using System;
using System.Collections.Generic;

namespace SkyHarvest.SaveLoad
{
    [Serializable]
    public class WorldSaveData
    {
        public int Version = 1;
        public float GameTimeMinutes;
        public string WeatherState = "ClearSkies";
        public float WeatherTimeRemaining;
        public IslandSaveData Island = new();
        public PlayerSaveData Player = new();
    }

    [Serializable]
    public class IslandSaveData
    {
        public int Seed;
        public int Radius = Core.Constants.DefaultIslandRadius;
        public List<CellSaveData> ModifiedCells = new();
        public List<StructureSaveData> Structures = new();
        public List<CropSaveData> Crops = new();
        public List<StorageSaveData> Storages = new();
        public List<SkynetSaveData> Skynets = new();
        public List<WorkshopSaveData> Workshops = new();
    }

    [Serializable]
    public class CellSaveData
    {
        public int X, Y;
        public float SoilQuality, WaterLevel, Nutrients;
        public string LastCrop = "";
    }

    [Serializable]
    public class StructureSaveData
    {
        public string StructureId = "";
        public int GridX, GridY;
        // Staged building: true while still a ConstructionSite; Delivered holds
        // materials already turned in. Absent in old saves → finished structure.
        public bool Constructing;
        public List<SlotSaveData> Delivered = new();
    }

    [Serializable]
    public class CropSaveData
    {
        public string CropId = "";
        public int GridX, GridY;
        public float GrowthProgress, Health;
    }

    [Serializable]
    public class StorageSaveData
    {
        public int GridX, GridY;
        public List<SlotSaveData> Slots = new();
    }

    [Serializable]
    public class SkynetSaveData
    {
        public int GridX, GridY;
        public long LastCollectedUnixTime;
        public List<SlotSaveData> Buffer = new();
    }

    [Serializable]
    public class WorkshopSaveData
    {
        public int GridX, GridY;
        public string RecipeId = "";
        public string OutputItemId = "";
        public int OutputAmount;
        public float TotalSeconds;
        public float ElapsedSeconds;
        public string State = "Idle";
    }

    [Serializable]
    public class SlotSaveData
    {
        public string ItemId = "";
        public int Count;
    }

    [Serializable]
    public class PlayerSaveData
    {
        public float PosX, PosY, PosZ;
        public string EquippedTool = "";
        public List<SlotSaveData> InventorySlots = new();
    }
}
