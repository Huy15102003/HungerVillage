namespace Game
{
    public interface IInteractable
    {
        public string InteractMessage { get; }
        public bool CanInteract { get; }
        public void Interact();

    }
}