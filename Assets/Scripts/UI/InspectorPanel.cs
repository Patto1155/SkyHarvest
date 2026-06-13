using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Building;
using SkyHarvest.Core;
using SkyHarvest.Data;
using SkyHarvest.Farming;
using SkyHarvest.Player;
using SkyHarvest.Workshop;

namespace SkyHarvest.UI
{
    /// <summary>
    /// On-demand floating panel for crop/soil, workshop, and structure details.
    /// Toggle with Q while facing a nearby interactable (see InteractionSystem).
    /// </summary>
    public class InspectorPanel : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        private GameObject? _panel;
        private Text? _titleText;
        private Text? _bodyText;
        private Slider? _barA;
        private Slider? _barB;
        private Text? _barALabel;
        private Text? _barBLabel;

        private InteractionSystem? _interactSys;
        private PlayerInventoryComponent? _playerInv;

        public void Initialize(GameObject panel, InteractionSystem interact, PlayerInventoryComponent? inv)
        {
            _panel      = panel;
            _interactSys = interact;
            _playerInv  = inv;
            _panel.SetActive(false);
        }

        public void SetWidgets(Text title, Text body, Slider barA, Slider barB, Text barALabel, Text barBLabel)
        {
            _titleText  = title;
            _bodyText   = body;
            _barA       = barA;
            _barB       = barB;
            _barALabel  = barALabel;
            _barBLabel  = barBLabel;
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (_panel == null || !HasInspectableTarget()) return;
            IsOpen = true;
            _panel.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            IsOpen = false;
            _panel?.SetActive(false);
        }

        private void Update()
        {
            if (_interactSys == null) return;

            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (IsOpen) Close();
                else if (HasInspectableTarget()) Open();
            }

            if (IsOpen)
            {
                if (!HasInspectableTarget())
                    Close();
                else
                    Refresh();
            }
        }

        private bool HasInspectableTarget() => ResolveTarget() != null;

        private object? ResolveTarget()
        {
            var target = _interactSys?.CurrentTarget;
            if (target == null) return null;

            if (target is CropPlot or WorkshopBase or Structure)
                return target;

            return null;
        }

        private void Refresh()
        {
            var target = ResolveTarget();
            if (target == null) { Close(); return; }

            switch (target)
            {
                case CropPlot plot:
                    RefreshCrop(plot);
                    break;
                case WorkshopBase workshop:
                    RefreshWorkshop(workshop);
                    break;
                case Structure structure:
                    RefreshStructure(structure);
                    break;
            }
        }

        private void RefreshCrop(CropPlot plot)
        {
            var soil = plot.Soil;
            if (_titleText != null)
            {
                string cropName = plot.HasCrop ? plot.Crop!.CropId.Replace("_", " ") : "Empty plot";
                _titleText.text = cropName;
            }

            if (_bodyText != null)
            {
                string till = soil.IsTilled ? "Tilled" : "Untilled";
                string moist = soil.IsWet ? "Moist" : soil.IsDry ? "Dry" : "Damp";
                _bodyText.text =
                    $"Soil quality {soil.Quality:F0}%\n" +
                    $"Water {soil.WaterLevel:F0}/{Constants.MaxSoilWater:F0}  ({moist})\n" +
                    $"Nutrients {soil.Nutrients:F0}/{Constants.MaxSoilNutrients:F0}\n" +
                    $"Terrain {soil.Terrain}  ({till})";
            }

            if (plot.HasCrop && plot.Crop != null)
            {
                SetBar(_barA, _barALabel, "Growth", plot.Crop.GrowthProgress);
                SetBar(_barB, _barBLabel, "Health", plot.Crop.Health);
            }
            else
            {
                SetBar(_barA, _barALabel, "Soil water", soil.WaterLevel / Constants.MaxSoilWater);
                SetBar(_barB, _barBLabel, "Nutrients", soil.Nutrients / Constants.MaxSoilNutrients);
            }
        }

        private void RefreshWorkshop(WorkshopBase workshop)
        {
            if (_titleText != null)
                _titleText.text = workshop.Def?.DisplayName ?? "Workshop";

            string state = workshop.IsComplete ? "Complete — collect output"
                : workshop.IsProcessing ? "Processing"
                : "Idle";

            if (_bodyText != null)
            {
                _bodyText.text = $"Status: {state}\n{GetFuelLine(workshop)}";
            }

            SetBar(_barA, _barALabel, "Progress", workshop.Progress);
            SetBar(_barB, _barBLabel, "Condition", 1f);
        }

        private string GetFuelLine(WorkshopBase workshop)
        {
            if (workshop.GetWorkshopType() != WorkshopType.Forge || _playerInv == null)
                return "Fuel: —";

            int coal = _playerInv.Inventory.GetCount("coal");
            return $"Fuel (coal in pack): {coal}";
        }

        private void RefreshStructure(Structure structure)
        {
            if (_titleText != null)
                _titleText.text = structure.Def?.DisplayName ?? "Structure";

            if (_bodyText != null)
                _bodyText.text = $"Grid ({structure.GridPosition.x}, {structure.GridPosition.y})";

            SetBar(_barA, _barALabel, "Condition", 1f);
            HideBar(_barB, _barBLabel);
        }

        private static void SetBar(Slider? bar, Text? label, string name, float value01)
        {
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.text = $"{name} {value01 * 100f:F0}%";
            }
            if (bar != null)
            {
                bar.gameObject.SetActive(true);
                bar.minValue = 0f;
                bar.maxValue = 1f;
                bar.value    = Mathf.Clamp01(value01);
            }
        }

        private static void HideBar(Slider? bar, Text? label)
        {
            if (label != null) label.gameObject.SetActive(false);
            if (bar != null) bar.gameObject.SetActive(false);
        }
    }
}
