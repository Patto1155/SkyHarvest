// Stone Mill — indoor, reliable. 4-frame sail-turning animation.
using UnityEngine;
using SkyHarvest.Data;

namespace SkyHarvest.Workshop
{
    public class StoneMill : WorkshopBase
    {
        protected override void Start()
        {
            base.Start();
            LoadFrames("Sprites/structures/stone_mill", 128);
            _workshopId = "stone_mill";
        }

        // No special behaviour — base CanContinue returns true always.
        public override WorkshopType GetWorkshopType() => WorkshopType.StoneMill;
    }
}
