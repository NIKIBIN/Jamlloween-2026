using Jamlloween.Flashlight;
using UnityEngine;

/// <summary>
/// Put one in a scene where the minigames need the flashlight (Zatan_Test). Without the flashlight no minigame can be
/// entered ("Precisa da lanterna"); once the player has it, the beam is held on while a minigame is played, which is
/// what lights the work (no work lamps needed). On leaving, the beam goes back to the F toggle.
/// </summary>
public class MinigameFlashlightLink : MonoBehaviour
{
    [SerializeField] PlayerFlashlight flashlight;
    [SerializeField] string blockedPrompt = "Precisa da lanterna";

    void Awake()
    {
        if (flashlight == null) throw new MissingReferenceException(nameof(flashlight));
    }

    void OnEnable()
    {
        Minigame.EntryBlocker = Blocker;
        Minigame.AnyStarted += OnStarted;
        Minigame.AnyExited += OnExited;
    }

    void OnDisable()
    {
        if (Minigame.EntryBlocker == (System.Func<string>)Blocker) Minigame.EntryBlocker = null;
        Minigame.AnyStarted -= OnStarted;
        Minigame.AnyExited -= OnExited;
        if (flashlight != null) flashlight.SetWorkMode(false);
    }

    string Blocker() => flashlight.IsOwned ? null : blockedPrompt;

    void OnStarted(Minigame _) => flashlight.SetWorkMode(true);
    void OnExited(Minigame _) => flashlight.SetWorkMode(false);
}
