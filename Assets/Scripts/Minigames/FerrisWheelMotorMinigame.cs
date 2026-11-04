using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Motor da roda-gigante. PROVISORIO: um interruptor so; clicar nele liga o motor e conclui o minigame.
/// O minigame de verdade entra depois, na mesma base (Minigame): so a parte de Tick muda.
/// Concluir avisa o FerrisWheelRide (Roteiro), que traz o carrinho do jogador para baixo.
/// </summary>
public class FerrisWheelMotorMinigame : Minigame
{
    [Header("Pecas")]
    [Tooltip("Alavanca do interruptor. Precisa de collider. A rotacao da cena e a de desligado (para cima).")]
    [SerializeField] Transform switchHandle;
    [Tooltip("Graus que a alavanca desce, em torno do X local, de desligado a ligado.")]
    [SerializeField] float switchAngle = 70f;
    [SerializeField] Renderer lamp;
    [SerializeField] Material lampOff, lampOn;

    Renderer[] handleRenderers;
    Quaternion handleOff;
    bool hovered, thrown;

    Quaternion HandleOn => handleOff * Quaternion.Euler(-switchAngle, 0f, 0f);

    protected override void Awake()
    {
        base.Awake();
        handleRenderers = switchHandle.GetComponentsInChildren<Renderer>();
        handleOff = switchHandle.localRotation;
        if (lamp != null && lampOff != null) lamp.sharedMaterial = lampOff;
    }

    public override void ForceComplete()
    {
        base.ForceComplete();
        SetThrown();
    }

    protected override void Tick(Camera viewCamera)
    {
        var mouse = Mouse.current;
        if (mouse == null || thrown) return;

        Ray ray = viewCamera.ScreenPointToRay(mouse.position.ReadValue());
        bool over = Physics.Raycast(ray, out var hit, 5f, ~0, QueryTriggerInteraction.Ignore) && hit.transform.IsChildOf(switchHandle);
        SetHover(over);
        if (over && mouse.leftButton.wasPressedThisFrame) StartCoroutine(Throw());
    }

    protected override void OnExitRequested() => SetHover(false);

    void SetHover(bool on)
    {
        if (hovered == on) return;
        hovered = on;
        HighlightTint.Tint(handleRenderers, on, HighlightTint.Contrast);
    }

    IEnumerator Throw()
    {
        thrown = true;
        SetHover(false);
        Quaternion from = switchHandle.localRotation, to = HandleOn;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.18f)
        {
            switchHandle.localRotation = Quaternion.Slerp(from, to, t * t);
            yield return null;
        }
        SetThrown();
        Complete();
    }

    void SetThrown()
    {
        thrown = true;
        switchHandle.localRotation = HandleOn;
        if (lamp != null && lampOn != null) lamp.sharedMaterial = lampOn;
    }
}
