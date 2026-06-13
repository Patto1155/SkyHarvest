// A placed-but-unbuilt structure (spec §2 staged building).
// Renders the target structure's sprite translucent; interact (E) delivers
// matching materials from the player's inventory; when all costs are met the
// site is replaced by the real structure via BuildModeController.PlaceStructure.
// Hammer-demolish refunds 100% of delivered materials (nothing was built yet).
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Data;
using SkyHarvest.Player;

namespace SkyHarvest.Building
{
    public class ConstructionSite : Structure
    {
        public ConstructionProgress Progress { get; private set; } = null!;

        private static readonly Color SiteTint = new Color(1f, 1f, 1f, 0.45f);

        public override string InteractionPrompt
        {
            get
            {
                var sb = new System.Text.StringBuilder($"Build {Def?.DisplayName}: ");
                bool any = false;
                if (Progress != null)
                    foreach (var (itemId, remaining) in Progress.RemainingCosts())
                    {
                        sb.Append($"{remaining}× {itemId}  ");
                        any = true;
                    }
                return any ? sb.ToString().TrimEnd() : $"Build {Def?.DisplayName}";
            }
        }

        public override void Initialize(StructureDef def, Vector2Int gridPos)
        {
            base.Initialize(def, gridPos);
            Progress = new ConstructionProgress(def.BuildCosts);

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = SiteTint;
        }

        public override void Interact(PlayerController player)
        {
            if (player == null) return;

            // Hammer cancels the site, refunding everything delivered.
            if (player.TryGetComponent<ToolSystem>(out var tools) &&
                tools.EquippedTool == ToolType.Hammer)
            {
                CancelAndRefund(player.Inventory);
                return;
            }

            DeliverFrom(player.Inventory);
        }

        /// <summary>
        /// Move every still-needed material the carrier holds into the site.
        /// Completes (replaces itself with the real structure) when costs are met,
        /// so every delivery path behaves the same.
        /// </summary>
        public void DeliverFrom(Inventory inventory)
        {
            if (inventory == null || Progress == null) return;

            bool deliveredAny = false;
            foreach (var (itemId, remaining) in System.Linq.Enumerable.ToList(Progress.RemainingCosts()))
            {
                int available = inventory.GetCount(itemId);
                if (available <= 0) continue;

                int accepted = Progress.Deliver(itemId, available < remaining ? available : remaining);
                if (accepted > 0 && inventory.TryRemove(itemId, accepted))
                    deliveredAny = true;
            }

            if (deliveredAny)
                EventBus.Publish(new ConstructionProgressEvent
                {
                    StructureId = Def?.StructureId ?? "",
                    Complete = Progress.IsComplete
                });

            if (Progress.IsComplete) Complete();
        }

        /// <summary>Restore delivered amounts from a save file (completes if already met).</summary>
        public void RestoreDelivered(System.Collections.Generic.IEnumerable<(string itemId, int count)> delivered)
        {
            foreach (var (itemId, count) in delivered)
                Progress.Deliver(itemId, count);
            if (Progress.IsComplete) Complete();
        }

        private void Complete()
        {
            var def = Def;
            var pos = GridPosition;
            StructureRegistry.Instance?.Unregister(this);
            Destroy(gameObject);
            BuildModeController.Instance?.PlaceStructure(pos, def);
        }

        private void CancelAndRefund(Inventory playerInventory)
        {
            foreach (var (itemId, count) in Progress.DeliveredItems())
                playerInventory?.TryAdd(itemId, count);

            StructureRegistry.Instance?.Unregister(this);
            Destroy(gameObject);
        }
    }
}
