namespace Circus
{
    /// <summary>Pressed with E.</summary>
    public interface IInteractable
    {
        bool CanInteract { get; }
        string Prompt { get; }
        void Interact(Interactor who);
    }

    /// <summary>Optional secondary action, held with Q (e.g. lock a door).</summary>
    public interface IHoldInteractable
    {
        bool CanHold { get; }
        string HoldPrompt { get; }
        float HoldTime { get; }
        void HoldComplete(Interactor who);
    }
}
