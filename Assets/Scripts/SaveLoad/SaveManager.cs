using UnityEngine;
using System.IO;
using System.Collections.Generic;
using SkyHarvest.Core;
using SkyHarvest.Building;
using SkyHarvest.Farming;
using SkyHarvest.Workshop;

namespace SkyHarvest.SaveLoad
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager? Instance { get; private set; }

        private string SavePath => Path.Combine(Application.persistentDataPath, "saves");
        private const string SaveFileName = "save.json";
        private string FullPath => Path.Combine(SavePath, SaveFileName);

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            Directory.CreateDirectory(SavePath);
        }

        public bool HasSave() => File.Exists(FullPath);

        public void Save()
        {
            var data = BuildSaveData();
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FullPath, json);
            EventBus.Publish(new GameSavedEvent());
            Debug.Log($"[Save] Saved to {FullPath}");
        }

        public WorldSaveData? Load()
        {
            if (!File.Exists(FullPath)) return null;
            string json = File.ReadAllText(FullPath);
            return JsonUtility.FromJson<WorldSaveData>(json);
        }

        public void DeleteSave()
        {
            if (File.Exists(FullPath)) File.Delete(FullPath);
        }

        private WorldSaveData BuildSaveData()
        {
            var gm = GameManager.Instance;
            var wm = Weather.WeatherManager.Instance;

            var data = new WorldSaveData
            {
                GameTimeMinutes      = gm != null ? gm.Clock.TotalMinutes : 0f,
                WeatherState         = wm != null ? wm.CurrentWeather.ToString() : "ClearSkies",
                WeatherTimeRemaining = wm?.StateMachine?.MinutesRemaining ?? 5f,
            };

            if (gm?.CurrentIsland != null)
            {
                data.Island.Seed   = gm.CurrentIsland.Seed;
                data.Island.Radius = gm.CurrentIsland.Radius;

                foreach (var kvp in gm.CurrentIsland.Cells)
                {
                    var cell = kvp.Value;
                    data.Island.ModifiedCells.Add(new CellSaveData
                    {
                        X = cell.GridPos.x, Y = cell.GridPos.y,
                        WaterLevel = cell.Soil.WaterLevel,
                        Nutrients  = cell.Soil.Nutrients
                    });
                }
            }

            if (StructureRegistry.Instance != null)
            {
                foreach (var s in StructureRegistry.Instance.AllStructures)
                {
                    var structSd = new StructureSaveData
                    {
                        StructureId = s.Def?.StructureId ?? "",
                        GridX = s.GridPosition.x, GridY = s.GridPosition.y
                    };
                    if (s is ConstructionSite site)
                    {
                        structSd.Constructing = true;
                        foreach (var (itemId, count) in site.Progress.DeliveredItems())
                            structSd.Delivered.Add(new SlotSaveData { ItemId = itemId, Count = count });
                    }
                    data.Island.Structures.Add(structSd);

                    if (s is Storage.StorageContainer sc)
                    {
                        var ssd = new StorageSaveData { GridX = s.GridPosition.x, GridY = s.GridPosition.y };
                        foreach (var slot in sc.Storage.Slots)
                            if (!slot.IsEmpty)
                                ssd.Slots.Add(new SlotSaveData { ItemId = slot.ItemId!, Count = slot.Count });
                        data.Island.Storages.Add(ssd);
                    }

                    if (s is Skynet.Skynet skynet)
                    {
                        var snd = new SkynetSaveData
                        {
                            GridX = s.GridPosition.x, GridY = s.GridPosition.y,
                            LastCollectedUnixTime = skynet.LastCollectedUnixTime
                        };
                        foreach (var (itemId, amount) in skynet.GetBufferContents())
                            snd.Buffer.Add(new SlotSaveData { ItemId = itemId, Count = amount });
                        data.Island.Skynets.Add(snd);
                    }

                    if (s is WorkshopBase wb && wb.ProcessState != WorkshopProcess.State.Idle)
                    {
                        data.Island.Workshops.Add(new WorkshopSaveData
                        {
                            GridX = s.GridPosition.x, GridY = s.GridPosition.y,
                            RecipeId = wb.ActiveRecipeId ?? "",
                            OutputItemId = wb.ActiveOutputItemId ?? "",
                            OutputAmount = wb.ActiveOutputAmount,
                            TotalSeconds = wb.ActiveTotalSeconds,
                            ElapsedSeconds = wb.ActiveElapsedSeconds,
                            State = wb.ProcessState.ToString()
                        });
                    }
                }
            }

            foreach (var plot in Object.FindObjectsOfType<CropPlot>())
            {
                if (plot.Crop == null) continue;
                data.Island.Crops.Add(new CropSaveData
                {
                    CropId = plot.Crop.CropId,
                    GridX = plot.GridPos.x, GridY = plot.GridPos.y,
                    GrowthProgress = plot.Crop.GrowthProgress,
                    Health = plot.Crop.Health
                });
            }

            var player = Object.FindObjectOfType<Player.PlayerController>();
            if (player != null)
            {
                var pos = player.transform.position;
                data.Player.PosX = pos.x; data.Player.PosY = pos.y; data.Player.PosZ = pos.z;
                data.Player.EquippedTool = player.GetComponent<Player.ToolSystem>()?.EquippedToolId ?? "";

                var pic = player.GetComponent<Player.PlayerInventoryComponent>();
                if (pic != null)
                    foreach (var slot in pic.Inventory.Slots)
                        if (!slot.IsEmpty)
                            data.Player.InventorySlots.Add(new SlotSaveData
                                { ItemId = slot.ItemId!, Count = slot.Count });
            }

            return data;
        }

        public void ApplySaveData(WorldSaveData data, Island.IslandData island)
        {
            if (System.Enum.TryParse<WeatherType>(data.WeatherState, out var wt))
                Weather.WeatherManager.Instance?.StateMachine?.SetState(wt, data.WeatherTimeRemaining);

            GameManager.Instance?.Clock.SetTime(data.GameTimeMinutes);

            foreach (var cd in data.Island.ModifiedCells)
            {
                var cell = island.GetCell(new Vector2Int(cd.X, cd.Y));
                cell?.Soil.SetState(cd.WaterLevel, cd.Nutrients);
            }
        }
    }
}
