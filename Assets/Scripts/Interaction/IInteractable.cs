public interface IInteractable
{
    /// <summary>
    /// Vazio = o objeto não aceita interação agora (sem prompt). Senão o HUD mostra "[E] Interagir"; só um
    /// <see cref="ICollectible"/> mostra este texto ("Pegar ...").
    /// </summary>
    string Prompt { get; }

    /// <summary>Chamado quando o jogador aperta E mirando no objeto.</summary>
    void Interact(PlayerInteractor interactor);
}

/// <summary>Marca um objeto que o jogador pega: o prompt mostra o texto dele ("Pegar ...") e não o "Interagir".</summary>
public interface ICollectible : IInteractable { }
