namespace Project.Interfaces
{
    public interface ISecondaryInteractable
    {
        string SecondaryInteractionPrompt { get; }
        bool CanSecondaryInteract { get; }
        void SecondaryInteract();
    }
}