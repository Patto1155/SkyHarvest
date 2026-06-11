namespace SkyHarvest.Player
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        void Interact(PlayerController player);
    }
}
