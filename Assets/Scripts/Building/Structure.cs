// Base class for all placed structures.
// Holds StructureDef reference and grid position, registered with StructureRegistry.
// Demolish() refunds 50% (floor) of build cost, publishes StructureDemolishedEvent.
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Data;
using SkyHarvest.Player;

namespace SkyHarvest.Building
{
    public class Structure : MonoBehaviour, IInteractable
    {
        public StructureDef Def { get; private set; }
        public Vector2Int GridPosition { get; private set; }

        public virtual string InteractionPrompt => Def?.DisplayName ?? "Structure";

        /// <summary>
        /// Called by BuildModeController after instantiation.
        /// </summary>
        public virtual void Initialize(StructureDef def, Vector2Int gridPos)
        {
            Def = def;
            GridPosition = gridPos;

            // Apply sorting order based on world position (CONVENTIONS §Sorting)
            var worldPos = GridMath.GridToWorld(gridPos);
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = Mathf.RoundToInt(-worldPos.y * Constants.SortingOrderScale);
        }

        private void OnEnable()  => InteractableRegistry.Register(this);
        private void OnDisable() => InteractableRegistry.Unregister(this);
        private void OnDestroy() => InteractableRegistry.Unregister(this);

        public virtual void Interact(PlayerController player)
        {
            if (TryDemolishWithHammer(player)) return;
            // Base: no-op; subclasses override
        }

        /// <summary>
        /// When the hammer is equipped, demolish this structure (50% material refund).
        /// Subclasses that override Interact should call this first.
        /// </summary>
        protected bool TryDemolishWithHammer(PlayerController player)
        {
            if (player == null) return false;
            if (!player.TryGetComponent<ToolSystem>(out var tools)) return false;
            if (tools.EquippedTool != ToolType.Hammer) return false;

            Demolish(player.Inventory);
            return true;
        }

        /// <summary>
        /// Demolish this structure, refunding floor(50%) of build materials,
        /// and publishing StructureDemolishedEvent.
        /// </summary>
        public void Demolish(Inventory playerInventory)
        {
            if (Def != null)
            {
                // StructureDef.BuildCosts is BuildCost[] with .ItemId / .Amount
                foreach (var cost in Def.BuildCosts)
                {
                    int refund = Mathf.FloorToInt(cost.Amount * 0.5f);
                    if (refund > 0)
                        playerInventory?.TryAdd(cost.ItemId, refund);
                }

                EventBus.Publish(new StructureDemolishedEvent { StructureId = Def.StructureId });
            }

            StructureRegistry.Instance?.Unregister(this);
            Destroy(gameObject);
        }
    }
}
