using Jamlloween.Flashlight;
using UnityEngine;

/// <summary>
/// A flashlight lying around. Interact (E) to take it: the player gets <see cref="PlayerFlashlight"/> and this object
/// disappears. Needs a collider so the player's interaction ray can hit it.
/// </summary>
public class FlashlightPickup : MonoBehaviour, IInteractable
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
        flashlight.Acquire();
        gameObject.SetActive(false);
    }
}
