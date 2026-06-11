// Pure workshop logic — no MonoBehaviour, no UnityEngine. Testable headless.
// CanCraft / ConsumeInputs helpers used by WorkshopBase and tests.
using SkyHarvest.Player;

namespace SkyHarvest.Workshop
{
    public static class WorkshopLogic
    {
        public static bool CanCraft(Inventory inv, (string itemId, int amount)[] inputs)
        {
            if (inputs == null) return true;
            foreach (var (itemId, amount) in inputs)
                if (!inv.Has(itemId, amount)) return false;
            return true;
        }

        public static void ConsumeInputs(Inventory inv, (string itemId, int amount)[] inputs)
        {
            if (inputs == null) return;
            foreach (var (itemId, amount) in inputs)
                inv.TryRemove(itemId, amount);
        }
    }

    // ---------------------------------------------------------------------------
    // WorkshopProcess — pure processing state machine, wrapped by WorkshopBase.
    // Kept UnityEngine-free so it compiles/tests headless.
    // ---------------------------------------------------------------------------
    public class WorkshopProcess
    {
        public enum State { Idle, Processing, Complete, Ruined }

        public State CurrentState { get; private set; } = State.Idle;
        public float Progress { get; private set; }      // 0..1
        public float ElapsedSeconds { get; private set; }

        // Active recipe info (set on Start)
        public string RecipeId       { get; private set; }
        public string OutputItemId   { get; private set; }
        public int    OutputAmount   { get; private set; }
        public float  TotalSeconds   { get; private set; }

        public bool IsIdle       => CurrentState == State.Idle;
        public bool IsProcessing => CurrentState == State.Processing;
        public bool IsComplete   => CurrentState == State.Complete;
        public bool IsRuined     => CurrentState == State.Ruined;

        /// <summary>
        /// Begin a new batch. Caller must have already consumed inputs.
        /// </summary>
        public bool Start(string recipeId, string outputItemId, int outputAmount, float totalSeconds)
        {
            if (!IsIdle) return false;

            RecipeId      = recipeId;
            OutputItemId  = outputItemId;
            OutputAmount  = outputAmount;
            TotalSeconds  = totalSeconds;
            ElapsedSeconds = 0f;
            Progress       = 0f;
            CurrentState   = State.Processing;
            return true;
        }

        /// <summary>
        /// Advance by deltaSeconds. Returns true when state changes.
        /// </summary>
        public bool Tick(float deltaSeconds)
        {
            if (!IsProcessing) return false;

            ElapsedSeconds += deltaSeconds;
            Progress = System.Math.Min(ElapsedSeconds / TotalSeconds, 1f);

            if (Progress >= 1f)
            {
                CurrentState = State.Complete;
                return true;
            }
            return false;
        }

        public void Ruin()
        {
            CurrentState = State.Ruined;
        }

        public void Reset()
        {
            CurrentState   = State.Idle;
            Progress       = 0f;
            ElapsedSeconds = 0f;
            RecipeId       = null;
            OutputItemId   = null;
            OutputAmount   = 0;
            TotalSeconds   = 0f;
        }
    }
}
