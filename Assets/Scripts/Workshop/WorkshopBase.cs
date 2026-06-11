// MonoBehaviour wrapper for WorkshopProcess.
// Handles:
//   - GameTickEvent → advance processing (real seconds = deltaMinutes * SecondsPerGameMinute)
//   - Auto-pull missing inputs from adjacent storage (StorageProximity, radius 1.5u)
//   - SpriteAnimator switching between idle/working frames
//   - Publishing WorkshopStartedEvent, WorkshopCompletedEvent
// Subclasses: DryingRack, StoneMill, Forge
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Data;
using SkyHarvest.Player;
using SkyHarvest.Storage;

namespace SkyHarvest.Workshop
{
    public abstract class WorkshopBase : Building.Structure
    {
        protected WorkshopProcess _process = new WorkshopProcess();

        // Sprite frames: index 0 = idle, index 1+ = working
        protected SpriteRenderer _sr;
        protected Sprite[]       _frames;
        protected SpriteAnimator _anim;

        // Set by subclass Start()
        protected string _workshopId;

        public WorkshopProcess.State ProcessState => _process.CurrentState;
        public float Progress => _process.Progress;
        public bool IsProcessing => _process.IsProcessing;
        public bool IsComplete   => _process.IsComplete;

        public abstract WorkshopType GetWorkshopType();

        protected virtual void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        protected virtual void OnEnable()
        {
            EventBus.Subscribe<GameTickEvent>(OnTick);
        }

        protected virtual void OnDisable()
        {
            EventBus.Unsubscribe<GameTickEvent>(OnTick);
        }

        protected virtual void OnTick(GameTickEvent e)
        {
            if (!_process.IsProcessing) return;

            // Check weather-sensitive or fuel conditions
            if (!CanContinue())
            {
                OnBatchRuined();
                return;
            }

            float deltaSeconds = e.DeltaMinutes * Constants.SecondsPerGameMinute;
            bool finished = _process.Tick(deltaSeconds);

            if (finished)
                OnBatchCompleted();
        }

        // ---- API used by WorkshopUI ----

        /// <summary>
        /// Attempt to start a recipe. Pulls inputs from player inventory,
        /// falling back to adjacent storage within 1.5 units if short.
        /// </summary>
        public bool StartRecipe(RecipeDef recipe, Inventory playerInv)
        {
            if (!_process.IsIdle) return false;
            if (recipe == null) return false;

            // Check fuel first (Forge override handles this, base does nothing)
            if (!CheckAndConsumeFuel(recipe, playerInv)) return false;

            // Build inputs tuple array from RecipeInput[]
            var inputs = BuildInputTuples(recipe);

            // Try auto-pull from nearby storage for any shortfall
            AutoPullFromNearbyStorage(inputs, playerInv);

            if (!WorkshopLogic.CanCraft(playerInv, inputs)) return false;

            WorkshopLogic.ConsumeInputs(playerInv, inputs);

            string workshopId = _workshopId ?? (Def?.StructureId ?? "unknown");
            bool started = _process.Start(
                recipe.RecipeId,
                recipe.OutputItemId,
                recipe.OutputAmount,
                recipe.ProcessingTimeSeconds);

            if (started)
            {
                SetWorkingVisual();
                EventBus.Publish(new WorkshopStartedEvent
                {
                    RecipeId   = recipe.RecipeId,
                    WorkshopId = workshopId
                });
            }

            return started;
        }

        /// <summary>
        /// Collect completed output into player inventory.
        /// </summary>
        public bool CollectOutput(Inventory playerInv)
        {
            if (!_process.IsComplete) return false;

            playerInv.TryAdd(_process.OutputItemId, _process.OutputAmount);
            _process.Reset();
            SetIdleVisual();
            return true;
        }

        // ---- overridable hooks ----

        /// <summary>
        /// Return false to ruin the current batch (e.g. DryingRack in rain).
        /// </summary>
        protected virtual bool CanContinue() => true;

        /// <summary>
        /// Called before input consumption — return false to abort start (e.g. Forge needs fuel).
        /// </summary>
        protected virtual bool CheckAndConsumeFuel(RecipeDef recipe, Inventory inv) => true;

        // ---- internals ----

        protected void OnBatchCompleted()
        {
            string recipeId = _process.RecipeId;
            // NOTE: Do not reset yet — player must collect via CollectOutput

            EventBus.Publish(new WorkshopCompletedEvent
            {
                RecipeId   = recipeId,
                WorkshopId = Def?.StructureId ?? "unknown"
            });
        }

        protected void OnBatchRuined()
        {
            string recipeId = _process.RecipeId;
            _process.Ruin();
            _process.Reset();
            SetIdleVisual();

            EventBus.Publish(new WorkshopRuinedEvent
            {
                RecipeId   = recipeId,
                WorkshopId = Def?.StructureId ?? "unknown"
            });
        }

        private void AutoPullFromNearbyStorage((string, int)[] inputs, Inventory playerInv)
        {
            var myWorldPos = new Vector2(transform.position.x, transform.position.y);
            foreach (var (itemId, amount) in inputs)
            {
                int have = playerInv.GetCount(itemId);
                int need = amount - have;
                if (need <= 0) continue;

                // Pull from adjacent storage (registry-based, no physics)
                var storage = StorageProximity.FindNearest(myWorldPos, 1.5f);
                if (storage != null && storage.Storage.Has(itemId, need))
                {
                    storage.Storage.TryRemove(itemId, need);
                    playerInv.TryAdd(itemId, need);
                }
            }
        }

        private static (string, int)[] BuildInputTuples(RecipeDef recipe)
        {
            if (recipe.Inputs == null) return System.Array.Empty<(string, int)>();
            var result = new (string, int)[recipe.Inputs.Length];
            for (int i = 0; i < recipe.Inputs.Length; i++)
                result[i] = (recipe.Inputs[i].ItemId, recipe.Inputs[i].Amount);
            return result;
        }

        protected void SetIdleVisual()
        {
            if (_anim != null) _anim.enabled = false;
            if (_frames != null && _frames.Length > 0 && _sr != null)
                _sr.sprite = _frames[0];
        }

        protected void SetWorkingVisual()
        {
            if (_frames != null && _frames.Length > 1)
            {
                if (_anim == null) _anim = gameObject.AddComponent<SpriteAnimator>();
                _anim.Frames  = _frames;
                _anim.Fps     = 4f;
                _anim.Loop    = true;
                _anim.enabled = true;
            }
            else if (_frames != null && _frames.Length > 0 && _sr != null)
                _sr.sprite = _frames[0];
        }

        protected void LoadFrames(string path, int frameWidth)
        {
            _frames = SpriteLoader.LoadStrip(path, frameWidth);
        }

        public override void Interact(PlayerController player)
        {
            // Open WorkshopUI — UI agent handles this via event.
            EventBus.Publish(new WorkshopInteractEvent { Workshop = this, Player = player });
        }
    }

    /// <summary>Published when player interacts with a workshop so WorkshopUI can open.</summary>
    public struct WorkshopInteractEvent
    {
        public WorkshopBase Workshop;
        public PlayerController Player;
    }
}
