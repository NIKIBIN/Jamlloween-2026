using Jamlloween.Progression;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A prancheta na mesa do trailer, o primeiro minigame (aquece os controles antes do radio).
/// A folha de cima e a ordem de servico do cliente; o jogador puxa a folha para baixo, para fora do clipe, e embaixo
/// aparece a lista de tarefas (a mesma do <see cref="Clipboard"/>). Tirada a folha, o minigame termina; ao voltar ao
/// controle, o jogador fica com a prancheta (o Tab passa a funcionar) e a da mesa some.
/// A folha anda no -Y local de <see cref="page"/> (para longe do clipe). Dica: alem do realce que respira, de tempos
/// em tempos a folha escorrega um pouco para baixo e volta, mostrando para onde ela sai.
/// </summary>
public class ClipboardMinigame : Minigame
{
    [SerializeField] Clipboard clipboard;

    [Header("Folha")]
    [Tooltip("A ordem de servico (com o texto). Sai do clipe andando no -Y local.")]
    [UnityEngine.Serialization.FormerlySerializedAs("pageHinge")]
    [SerializeField] Transform page;
    [Tooltip("Collider da ordem de servico (o jogador clica nele para arrastar).")]
    [SerializeField] Collider pageCollider;
    [Tooltip("O texto da lista de tarefas, na folha de baixo.")]
    [SerializeField] TMPro.TMP_Text tasksText;
    [Tooltip("Tudo o que some da mesa quando o jogador fica com a prancheta.")]
    [SerializeField] GameObject tableVisual;

    [Header("Arrasto")]
    [Tooltip("Quanto a folha anda (m) ate sair inteira do clipe.")]
    [SerializeField] float pullOutDistance = 0.3f;
    [Tooltip("Fracao da altura da tela que o mouse desce para tirar a folha inteira.")]
    [SerializeField, Range(0.1f, 1f)] float dragScreenFraction = 0.4f;
    [Tooltip("Metros por segundo da folha voltando ao clipe ou terminando de sair ao soltar.")]
    [SerializeField] float settleSpeed = 0.9f;
    [Tooltip("Ao sair, a folha desce ate a mesa (m, no Z local; positivo = para a mesa).")]
    [SerializeField] float restDrop = 0.016f;

    [Header("Dica")]
    [Tooltip("Segundos entre um escorregao de dica e outro, com a folha parada.")]
    [SerializeField] float nudgeEvery = 2.2f;
    [SerializeField] float nudgeSeconds = 0.6f;
    [Tooltip("Quanto a folha escorrega na dica (m).")]
    [SerializeField] float nudgeDistance = 0.04f;
    [SerializeField] Color hoverColor = new Color(1f, 0.85f, 0.35f);
    [SerializeField] AudioClip flipClip;

    Renderer[] pageRenderers;
    Vector3 pageHome;
    float pull, startPull, startMouseY, idle, nudge;
    bool dragging, hovered, pulledOut;
    AudioSource sfx;

    protected override void Awake()
    {
        base.Awake();
        if (clipboard == null) throw new MissingReferenceException(nameof(clipboard));
        if (page == null) throw new MissingReferenceException(nameof(page));
        if (pageCollider == null) throw new MissingReferenceException(nameof(pageCollider));
        if (tasksText == null) throw new MissingReferenceException(nameof(tasksText));
        if (tableVisual == null) throw new MissingReferenceException(nameof(tableVisual));
        pageRenderers = page.GetComponentsInChildren<Renderer>(true);
        pageHome = page.localPosition;
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f;
        onExited.AddListener(OnExited);
        ApplyPage();
    }

    protected override void OnBegin(PlayerInteractor interactor) => tasksText.text = clipboard.TaskListText();

    protected override void Tick(Camera viewCamera)
    {
        var mouse = Mouse.current;
        if (mouse == null || pulledOut) return;
        float my = mouse.position.ReadValue().y;

        if (dragging)
        {
            // Mouse para baixo = folha para baixo, para fora do clipe.
            float delta = (startMouseY - my) / (Screen.height * dragScreenFraction) * pullOutDistance;
            pull = Mathf.Clamp(startPull + delta, 0f, pullOutDistance);
            idle = 0f;
            if (!mouse.leftButton.isPressed) dragging = false;
        }
        else
        {
            // Solta: passou da metade, termina de sair; senao, a folha volta para o clipe.
            float target = pull > pullOutDistance * 0.5f ? pullOutDistance : 0f;
            pull = Mathf.MoveTowards(pull, target, settleSpeed * Time.deltaTime);
            if (pull >= pullOutDistance) { PullOut(); return; }

            Ray ray = viewCamera.ScreenPointToRay(mouse.position.ReadValue());
            bool over = pageCollider.Raycast(ray, out _, 5f);
            SetHover(over);
            if (over && mouse.leftButton.wasPressedThisFrame)
            {
                dragging = true;
                startPull = pull;
                startMouseY = my;
                nudge = 0f;
            }
        }
        UpdateNudge();
        ApplyPage();
        // Dica do proximo passo: a folha respira enquanto o mouse nao esta nela.
        if (!hovered && !dragging) HighlightTint.Tint(pageRenderers, MinigameHint.Level, hoverColor);
    }

    /// <summary>Dica: com a folha parada no clipe, ela escorrega um pouco para baixo e volta, de tempos em tempos.</summary>
    void UpdateNudge()
    {
        if (dragging || pull > 0.001f) { idle = 0f; nudge = 0f; return; }
        idle += Time.deltaTime;
        float t = idle - nudgeEvery;
        if (t < 0f) { nudge = 0f; return; }
        if (t >= nudgeSeconds) { idle = 0f; nudge = 0f; return; }
        nudge = Mathf.Sin(t / nudgeSeconds * Mathf.PI) * nudgeDistance;
    }

    void PullOut()
    {
        pulledOut = true;
        nudge = 0f;
        SetHover(false);
        HighlightTint.Tint(pageRenderers, 0f, hoverColor);
        if (flipClip != null) sfx.PlayOneShot(flipClip);
        Complete();
        tasksText.text = clipboard.TaskListText();   // a ordem de servico ja conta como lida
        ApplyPage();
    }

    protected override void OnExitRequested()
    {
        dragging = false;
        nudge = 0f;
        SetHover(false);
        HighlightTint.Tint(pageRenderers, 0f, hoverColor);
    }

    void SetHover(bool on)
    {
        if (hovered == on) return;
        hovered = on;
        HighlightTint.Tint(pageRenderers, on, hoverColor);
    }

    /// <summary>A folha desce no -Y local; tirada, ela fica deitada na mesa, abaixo da prancheta.</summary>
    void ApplyPage() => page.localPosition = pageHome + new Vector3(0f, -(pull + nudge), pulledOut ? restDrop : 0f);

    void OnExited()
    {
        if (!IsCompleted) return;
        clipboard.Acquire(openNow: true);
        tableVisual.SetActive(false);
    }

    /// <summary>Usado pelo pulo de etapas do debug.</summary>
    public override void ForceComplete()
    {
        base.ForceComplete();
        pulledOut = true;
        pull = pullOutDistance;
        ApplyPage();
        clipboard.Acquire(openNow: false);
        tableVisual.SetActive(false);
    }
}
