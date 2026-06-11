// Forge — fuel-consuming metalwork. 4-frame ember glow animation.
// Consumes FuelItemId/FuelAmount from player inventory before starting.
using UnityEngine;
using SkyHarvest.Data;
using SkyHarvest.Player;

namespace SkyHarvest.Workshop
{
    public class Forge : WorkshopBase
    {
        protected override void Start()
        {
            base.Start();
            LoadFrames("Sprites/structures/forge", 128);
            _workshopId = "forge";
        }

        /// <summary>
        /// Check and consume fuel before starting. Returns false if insufficient fuel.
        /// </summary>
        protected override bool CheckAndConsumeFuel(RecipeDef recipe, Inventory inv)
        {
            if (string.IsNullOrEmpty(recipe.FuelItemId)) return true;
            if (!inv.Has(recipe.FuelItemId, recipe.FuelAmount)) return false;
            inv.TryRemove(recipe.FuelItemId, recipe.FuelAmount);
            return true;
        }

        public override WorkshopType GetWorkshopType() => WorkshopType.Forge;
    }
}
