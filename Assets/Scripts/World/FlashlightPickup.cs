using Jamlloween.Flashlight;
using Jamlloween.Progression;
using UnityEngine;

/// <summary>
/// A flashlight lying around. Interact (E) to take it: the player gets <see cref="PlayerFlashlight"/> and this object
/// disappears. Needs a collider so the player's interaction ray can hit it.
/// </summary>
public class FlashlightPickup : MonoBehaviour, IInteractable, IStepForceCompletable
{
    [SerializeField] PlayerFlashlight flashlight;
    [SerializeField] string prompt = "Pegar lanterna";

    public string Prompt => flashlight != null && flashlight.IsOwned ? string.Empty : prompt;

    void Awake()
    {
        if (flashlight == null) throw new MissingReferenceException(nameof(flashlight));
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (flashlight.IsOwned) return;
        var gate = GetComponent<StepInteraction>();
        if (gate != null && !gate.TryUse()) return;   // fora da etapa: mostra o texto e nao pega
        flashlight.Acquire();
        gameObject.SetActive(false);
        if (gate != null) gate.NotifyCompleted();
    }

    /// <summary>Usado pelo pulo de etapas do debug.</summary>
    public void ForceComplete()
    {
        flashlight.Acquire();
        gameObject.SetActive(false);
    }
}
