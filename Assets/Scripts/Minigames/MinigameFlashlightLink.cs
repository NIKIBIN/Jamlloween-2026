using Jamlloween.Flashlight;
using UnityEngine;

/// <summary>
/// Put one in a scene where the minigames need the flashlight. Without the flashlight no minigame can be entered
/// ("Precisa da lanterna"), except the ones in <see cref="withoutFlashlight"/> (the radio, played before the
/// flashlight is picked up). Once the player has it, the beam is held on while a minigame is played, which is
/// what lights the work (no work lamps needed). On leaving, the beam goes back to the F toggle.
/// </summary>
public class MinigameFlashlightLink : MonoBehaviour
{
    [SerializeField] PlayerFlashlight flashlight;
    [SerializeField] string blockedPrompt = "Precisa da lanterna";
    [Tooltip("Minigames the player can enter without the flashlight (ex.: the radio, in the lit trailer).")]
    [SerializeField] Minigame[] withoutFlashlight = new Minigame[0];

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
        if (Minigame.EntryBlocker == (System.Func<Minigame, string>)Blocker) Minigame.EntryBlocker = null;
        Minigame.AnyStarted -= OnStarted;
        Minigame.AnyExited -= OnExited;
        if (flashlight != null) flashlight.SetWorkMode(false);
    }

    string Blocker(Minigame m) => flashlight.IsOwned || System.Array.IndexOf(withoutFlashlight, m) >= 0 ? null : blockedPrompt;

    void OnStarted(Minigame _) => flashlight.SetWorkMode(true);
    void OnExited(Minigame _) => flashlight.SetWorkMode(false);
}
