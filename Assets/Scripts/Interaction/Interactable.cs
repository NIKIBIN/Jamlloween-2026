using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Objeto interagivel generico. Ligue o evento onInteract no Inspector
/// (ex.: abrir um painel de minigame).
/// </summary>
public class Interactable : MonoBehaviour, IInteractable
{
    [SerializeField] string prompt = "Interagir";
    [SerializeField] bool singleUse;
    [SerializeField] UnityEvent onInteract;

    bool used;

    public string Prompt => used && singleUse ? string.Empty : prompt;

    public void Interact(PlayerInteractor interactor)
    {
        if (used && singleUse) return;
        used = true;
        Debug.Log($"[Interactable] {name} interagido", this);
        onInteract?.Invoke();
    }
}
