using Jamlloween.Progression;
using UnityEngine;

/// <summary>
/// A cabeca do cavalo do carrossel. E (interagir) pega: ela vai para a mao do jogador e fica visivel, sem deixar ele
/// mais lento. Com ela na mao, o <see cref="HorseNeckSocket"/> do cavalo encaixa.
/// Com um StepInteraction no mesmo objeto, pegar conclui a etapa e fora dela mostra o texto de bloqueio.
/// </summary>
public class HorseHead : MonoBehaviour, ICollectible, IStepForceCompletable
{
    [SerializeField] string prompt = "Pegar cabeça do cavalo";
    [Tooltip("Pose na mao, relativa a camera do jogador.")]
    [SerializeField] Vector3 heldPosition = new Vector3(0.28f, -0.22f, 0.6f);
    [SerializeField] Vector3 heldEuler = new Vector3(-10f, -110f, 0f);
    [SerializeField] float heldScale = 0.6f;

    StepInteraction gate;
    Collider[] colliders;

    public bool IsHeld { get; private set; }
    public bool Mounted { get; private set; }
    public string Prompt => IsHeld || Mounted ? string.Empty : prompt;

    void Awake()
    {
        gate = GetComponent<StepInteraction>();
        colliders = GetComponentsInChildren<Collider>(true);
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (IsHeld || Mounted) return;
        if (gate != null && !gate.TryUse()) return;
        Hold(interactor.ViewCamera);
        if (gate != null) gate.NotifyCompleted();
    }

    void Hold(Transform view)
    {
        IsHeld = true;
        SetColliders(false);
        transform.SetParent(view, false);
        transform.localPosition = heldPosition;
        transform.localRotation = Quaternion.Euler(heldEuler);
        transform.localScale = Vector3.one * heldScale;
    }

    /// <summary>Sai da mao e fica presa no pescoco, na pose do mount (que ja tem a escala do cavalo).</summary>
    public void Mount(Transform mount)
    {
        IsHeld = false;
        Mounted = true;
        SetColliders(false);
        transform.SetParent(mount, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    void SetColliders(bool on)
    {
        foreach (var c in colliders) if (c != null) c.enabled = on;
    }

    /// <summary>Pulo de etapas do debug: o jogador passa a segurar a cabeca.</summary>
    public void ForceComplete()
    {
        if (IsHeld || Mounted) return;
        var pi = FindFirstObjectByType<PlayerInteractor>();
        if (pi != null) Hold(pi.ViewCamera);
    }
}
