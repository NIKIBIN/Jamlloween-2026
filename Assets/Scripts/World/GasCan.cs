using Jamlloween.Progression;
using UnityEngine;

/// <summary>
/// O galao de gasolina: um item carregavel. Interagir (E) pega; o <see cref="GasCanCarrier"/> do jogador o leva na mao.
/// Largado, fica no chao onde caiu e pode ser pego de novo.
/// Com um StepInteraction no mesmo objeto, a PRIMEIRA vez que e pego conclui a etapa de pegar a gasolina (e fora da
/// etapa mostra o texto de bloqueio). Depois de pego uma vez, pega-lo de novo nunca e bloqueado.
/// </summary>
public class GasCan : MonoBehaviour, ICollectible, IStepForceCompletable
{
    [SerializeField] string prompt = "Pegar galão";

    StepInteraction gate;
    Collider[] colliders;
    bool taken;

    public bool IsHeld { get; private set; }
    /// <summary>True depois de usado no gerador: fica vazio no chao e nao e mais pegavel.</summary>
    public bool Spent { get; private set; }

    public string Prompt => IsHeld || Spent ? string.Empty : prompt;

    void Awake()
    {
        gate = GetComponent<StepInteraction>();
        colliders = GetComponentsInChildren<Collider>(true);
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (IsHeld || Spent) return;
        var carrier = interactor.GetComponent<GasCanCarrier>();
        if (carrier == null || carrier.IsCarrying) return;
        if (!taken && gate != null && !gate.TryUse()) return;   // fora da etapa: mostra o texto e nao pega
        carrier.PickUp(this);
        if (!taken)
        {
            taken = true;
            if (gate != null) gate.NotifyCompleted();
        }
    }

    /// <summary>Usado pelo pulo de etapas do debug: o jogador passa a carregar o galao.</summary>
    public void ForceComplete()
    {
        taken = true;
        if (Spent || IsHeld) return;
        var carrier = FindFirstObjectByType<GasCanCarrier>();
        if (carrier != null && !carrier.IsCarrying) carrier.PickUp(this);
    }

    internal void SetHeld(bool held)
    {
        IsHeld = held;
        foreach (var c in colliders) if (c != null) c.enabled = !held;
    }

    /// <summary>Poe o galao vazio no chao, em outro lugar, e o tira de circulacao.</summary>
    public void Spend(Vector3 position, Quaternion rotation)
    {
        Spent = true;
        transform.SetPositionAndRotation(position, rotation);
        foreach (var c in colliders) if (c != null) c.enabled = false;
    }
}
