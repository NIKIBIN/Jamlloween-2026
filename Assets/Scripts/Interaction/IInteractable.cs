public interface IInteractable
{
    /// <summary>Texto mostrado no HUD quando o jogador mira no objeto.</summary>
    string Prompt { get; }

    /// <summary>Chamado quando o jogador aperta E mirando no objeto.</summary>
    void Interact(PlayerInteractor interactor);
}
