// WorkshopType enum is defined in Assets/Scripts/Data/Defs.cs (namespace SkyHarvest.Data).
// This file provides a using alias so Workshop-namespace code can write WorkshopType directly.
// The actual enum: SkyHarvest.Data.WorkshopType { DryingRack, StoneMill, Forge }
//
// NOTE: Do NOT define WorkshopType here again — it lives in Defs.cs (Data agent).

// This file intentionally left as a namespace bridge only.
namespace SkyHarvest.Workshop
{
    // Re-export for convenience (typedef pattern)
    using WorkshopType = SkyHarvest.Data.WorkshopType;
}
