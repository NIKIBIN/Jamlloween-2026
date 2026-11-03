using System.Collections;
using Jamlloween.Progression;
using UnityEngine;

/// <summary>
/// O pescoco do cavalo sem cabeca. Sem a cabeca na mao, interagir envia o evento de "notar o cavalo" (a mesma etapa
/// que a area em volta do carrossel conclui). Com a cabeca na mao, E encaixa: ela desliza ate o pescoco e trava.
/// Com um StepInteraction no mesmo objeto, encaixar so funciona na etapa dele e conclui a etapa.
/// </summary>
public class HorseNeckSocket : MonoBehaviour, IInteractable, IStepForceCompletable
{
    [SerializeField] HorseHead head;
    [Tooltip("Pose final da cabeca no pescoco. A cabeca vira filha deste transform.")]
    [SerializeField] Transform mount;
    [SerializeField] string noticeEvent = "HorseNoticed";
    [SerializeField] string examinePrompt = "Examinar cavalo";
    [SerializeField] string fitPrompt = "Encaixar cabeça";
    [SerializeField] float fitSeconds = 0.35f;
    [SerializeField] AudioClip fitClip;
    [SerializeField, Range(0f, 1f)] float volume = 0.8f;

    StepInteraction gate;
    bool fitted, noticed;

    public bool Fitted => fitted;

    void Awake() => gate = GetComponent<StepInteraction>();

    public string Prompt
    {
        get
        {
            if (fitted) return string.Empty;
            if (head != null && head.IsHeld) return fitPrompt;
            return noticed ? string.Empty : examinePrompt;
        }
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (fitted) return;
        if (head == null || !head.IsHeld)
        {
            noticed = true;
            ProgressionDirector.RaiseEvent(noticeEvent);
            return;
        }
        if (gate != null && !gate.TryUse()) return;
        StartCoroutine(Fit());
    }

    IEnumerator Fit()
    {
        fitted = true;
        var t = head.transform;
        t.SetParent(null, true);
        Vector3 p0 = t.position;
        Quaternion r0 = t.rotation;
        Vector3 s0 = t.localScale;
        for (float k = 0f; k < 1f; k += Time.deltaTime / Mathf.Max(0.01f, fitSeconds))
        {
            float s = Mathf.SmoothStep(0f, 1f, k);
            t.SetPositionAndRotation(Vector3.Lerp(p0, mount.position, s), Quaternion.Slerp(r0, mount.rotation, s));
            t.localScale = Vector3.Lerp(s0, mount.lossyScale, s);
            yield return null;
        }
        t.SetPositionAndRotation(mount.position, mount.rotation);
        head.Mount(mount);
        AudioSource.PlayClipAtPoint(fitClip != null ? fitClip : FuelAudio.Click(), mount.position, volume);
        if (gate != null) gate.NotifyCompleted();
    }

    /// <summary>Pulo de etapas do debug: a cabeca aparece encaixada.</summary>
    public void ForceComplete()
    {
        if (fitted || head == null) return;
        fitted = noticed = true;
        head.Mount(mount);
    }
}
