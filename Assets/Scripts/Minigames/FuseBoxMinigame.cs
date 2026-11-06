using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Painel de fusiveis diegetico. Passos:
///  1. Segurar o botao esquerdo em cada um dos 4 parafusos ate solta-los.
///  2. Clicar na tampa para tirar.
///  3. Achar o fusivel queimado (escuro) e clicar para remover.
///  4. Pegar o fusivel novo na prateleira e clicar no encaixe vazio. Ele so aparece na prateleira ao abrir o minigame
///     (o jogador chega com o fusivel achado no parque); antes disso a prateleira fica vazia.
///  5. Arrastar a alavanca para baixo ate o fim (se soltar antes, ela volta).
/// O painel nao fecha de volta (e um jogo de terror).
/// </summary>
public class FuseBoxMinigame : Minigame
{
    [Header("Pecas")]
    [SerializeField] FuseBoxPart[] screws;
    [SerializeField] FuseBoxPart cover;
    [SerializeField] FuseBoxPart[] slots;
    [Tooltip("Um fusivel por encaixe, na mesma ordem.")]
    [SerializeField] FuseBoxPart[] fuses;
    [SerializeField] FuseBoxPart spareFuse;
    [Tooltip("Alavanca final. Gira em torno do eixo X local e fica travada ate o fusivel encaixar.")]
    [SerializeField] FuseBoxPart lever;

    [Header("Visual")]
    [SerializeField] Renderer statusLed;
    [SerializeField] Material ledReadyMaterial;
    [SerializeField] Material ledOkMaterial;
    [SerializeField] Material burntFuseMaterial;

    [Header("Ajustes")]
    [Tooltip("Qual fusivel esta queimado. -1 sorteia ao iniciar a cena.")]
    [SerializeField] int burntIndex = -1;
    [SerializeField] float unscrewTime = 0.7f;
    [SerializeField] float unscrewDistance = 0.03f;
    [SerializeField] float unscrewTurns = 2f;
    [Tooltip("Quanto a alavanca gira de ponta a ponta, em graus.")]
    [SerializeField] float leverAngle = 160f;
    [Tooltip("Quanto do tamanho da tela o mouse precisa descer para puxar a alavanca ate o fim.")]
    [SerializeField] float leverDragScreenFraction = 0.35f;

    [Header("Som (vazio = sem som)")]
    [Tooltip("Parafuso solto caindo.")]
    [SerializeField] AudioClip screwClip;
    [Tooltip("Tampa de metal saindo.")]
    [SerializeField] AudioClip coverClip;
    [Tooltip("Fusivel saindo e entrando no encaixe.")]
    [SerializeField] AudioClip fuseClip;
    [Tooltip("Alavanca chegando ao fim (a energia volta).")]
    [SerializeField] AudioClip leverClip;
    [SerializeField, Range(0f, 1f)] float volume = 1f;
    [SerializeField] float audioMaxDistance = 12f;

    bool[] screwOut;
    bool[] fusePresent;
    bool[] shaking;
    Vector3[] screwHome, fuseHome;
    Vector3 spareHomePos;
    Quaternion spareHomeRot;
    bool coverOff, burntRemoved, holdingSpare, inserted, fuseSeated, leverDone, leverDragging;
    float leverDragStartY, leverDragStartProgress;
    Quaternion leverHomeRot;
    Coroutine leverRoutine;
    FuseBoxPart hovered;
    FuseBoxPart[] parts;
    Coroutine spareRoutine;
    AudioSource sfx;

    public bool CoverRemoved => coverOff;
    public bool BurntFuseRemoved => burntRemoved;
    public int BurntIndex => burntIndex;

    protected override void Awake()
    {
        base.Awake();
        parts = GetComponentsInChildren<FuseBoxPart>(true);
        screwOut = new bool[screws.Length];
        screwHome = new Vector3[screws.Length];
        for (int i = 0; i < screws.Length; i++) screwHome[i] = screws[i].transform.localPosition;

        fusePresent = new bool[fuses.Length];
        shaking = new bool[fuses.Length];
        fuseHome = new Vector3[fuses.Length];
        for (int i = 0; i < fuses.Length; i++)
        {
            fusePresent[i] = true;
            fuseHome[i] = fuses[i].transform.localPosition;
        }

        spareHomePos = spareFuse.transform.localPosition;
        spareHomeRot = spareFuse.transform.localRotation;
        // O fusivel novo e o que o jogador acha no parque: so aparece na prateleira quando ele chega com ele (ao abrir o minigame).
        spareFuse.gameObject.SetActive(false);
        leverHomeRot = lever.transform.localRotation;

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 1f;
        sfx.rolloffMode = AudioRolloffMode.Linear;
        sfx.minDistance = 1f;
        sfx.maxDistance = audioMaxDistance;

        if (burntIndex < 0 || burntIndex >= fuses.Length) burntIndex = Random.Range(0, fuses.Length);
        var glass = fuses[burntIndex].transform.Find("Glass");
        if (glass != null && burntFuseMaterial != null) glass.GetComponent<Renderer>().sharedMaterial = burntFuseMaterial;
    }

    protected override void OnBegin(PlayerInteractor interactor) => spareFuse.gameObject.SetActive(true);

    public override void ForceComplete()
    {
        base.ForceComplete();
        spareFuse.gameObject.SetActive(true);
    }

    // ---------- Regras: o que pode ser usado agora ----------

    bool AllScrewsOut()
    {
        foreach (var o in screwOut) if (!o) return false;
        return true;
    }

    bool CanUse(FuseBoxPart p)
    {
        switch (p.kind)
        {
            case FuseBoxPart.Kind.Screw: return !screwOut[p.index];
            case FuseBoxPart.Kind.Cover: return AllScrewsOut() && !coverOff;
            case FuseBoxPart.Kind.Fuse: return coverOff && fusePresent[p.index];
            case FuseBoxPart.Kind.Slot: return holdingSpare && !fusePresent[p.index];
            case FuseBoxPart.Kind.SpareFuse: return burntRemoved && !holdingSpare && !inserted;
            case FuseBoxPart.Kind.Lever: return fuseSeated && !leverDone;
        }
        return false;
    }

    // ---------- Loop ----------

    protected override void Tick(Camera viewCamera)
    {
        TickInput(viewCamera);
        UpdateHints();
    }

    /// <summary>O proximo passo. Entre os fusiveis, so o queimado (o que precisa sair).</summary>
    bool IsNextStep(FuseBoxPart p)
    {
        if (leverDragging || !CanUse(p)) return false;
        if (p.kind == FuseBoxPart.Kind.Fuse) return p.index == burntIndex && !burntRemoved;
        return true;
    }

    void UpdateHints()
    {
        float level = MinigameHint.Level;
        foreach (var p in parts)
            if (p != hovered) p.SetHint(IsNextStep(p) ? level : 0f, HighlightTint.Contrast);
    }

    void ClearHints()
    {
        foreach (var p in parts) if (p != hovered) p.SetHint(0f, HighlightTint.Contrast);
    }

    void TickInput(Camera viewCamera)
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Ray ray = viewCamera.ScreenPointToRay(mouse.position.ReadValue());
        bool down = mouse.leftButton.wasPressedThisFrame;
        bool held = mouse.leftButton.isPressed;

        if (leverDragging)
        {
            SetHover(lever);
            if (held) DragLever(mouse.position.ReadValue().y);
            else ReleaseLever();   // no frame da soltura vale a posicao do frame anterior
            return;
        }

        FuseBoxPart hit = Pick(ray);
        FuseBoxPart usable = hit != null && CanUse(hit) ? hit : null;
        SetHover(usable);

        if (holdingSpare)
        {
            MoveSpare(ray);
            if (!down) return;
            if (usable != null && usable.kind == FuseBoxPart.Kind.Slot) Insert(usable.index);
            else if (hit != null && hit.kind == FuseBoxPart.Kind.Fuse) Shake(hit.index);
            else DropSpare();
            return;
        }

        if (usable == null) return;
        switch (usable.kind)
        {
            case FuseBoxPart.Kind.Screw: if (held) TurnScrew(usable.index); break;
            case FuseBoxPart.Kind.Cover: if (down) RemoveCover(); break;
            case FuseBoxPart.Kind.Fuse:
                if (down) { if (usable.index == burntIndex) PullBurnt(); else Shake(usable.index); }
                break;
            case FuseBoxPart.Kind.SpareFuse: if (down) PickUpSpare(); break;
            case FuseBoxPart.Kind.Lever: if (down) GrabLever(mouse.position.ReadValue().y); break;
        }
    }

    protected override void OnExitRequested()
    {
        SetHover(null);
        ClearHints();
        if (leverDragging) ReleaseLever();   // solta a alavanca: se nao chegou ao fim, ela volta
        if (holdingSpare) DropSpare(instant: true);   // o resto do progresso fica como esta
    }

    /// <summary>A primeira peca atingida pelo raio. Pecas na frente escondem as de tras (tampa sobre fusiveis).</summary>
    FuseBoxPart Pick(Ray ray)
    {
        var hits = Physics.RaycastAll(ray, 5f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            var p = h.collider.GetComponent<FuseBoxPart>();
            if (p == null) continue;
            if (holdingSpare && p == spareFuse) continue;
            if (p.kind == FuseBoxPart.Kind.Slot && fusePresent[p.index]) continue;   // encaixe ocupado: o fusivel e que conta
            return p;
        }
        return null;
    }

    void SetHover(FuseBoxPart p)
    {
        if (hovered == p) return;
        if (hovered != null) hovered.SetHighlight(false, HighlightTint.Contrast);
        hovered = p;
        if (hovered != null) hovered.SetHighlight(true, HighlightTint.Contrast);
    }

    // ---------- Parafusos ----------

    void TurnScrew(int i)
    {
        var p = screws[i];
        p.progress = Mathf.Min(1f, p.progress + Time.deltaTime / unscrewTime);
        p.transform.localPosition = screwHome[i] + Vector3.back * (p.progress * unscrewDistance);
        p.transform.localRotation = Quaternion.Euler(0f, 0f, p.progress * 360f * unscrewTurns);
        if (p.progress < 1f) return;

        screwOut[i] = true;
        Play(screwClip);
        Disable(p);
        StartCoroutine(Fall(p.transform));
    }

    // ---------- Tampa / fusiveis ----------

    void RemoveCover()
    {
        coverOff = true;
        Play(coverClip);
        Disable(cover);
        StartCoroutine(Slide(cover.transform, new Vector3(0f, -0.4f, -0.5f), Quaternion.Euler(35f, 0f, 0f), 0.5f, hideAtEnd: true));
    }

    void PullBurnt()
    {
        var f = fuses[burntIndex];
        fusePresent[burntIndex] = false;
        burntRemoved = true;
        Play(fuseClip);
        Disable(f);
        StartCoroutine(Slide(f.transform, fuseHome[burntIndex] + new Vector3(0f, 0.04f, -0.25f), f.transform.localRotation, 0.35f, hideAtEnd: true));
    }

    void Shake(int i)
    {
        if (shaking[i]) return;
        StartCoroutine(ShakeRoutine(i));
    }

    IEnumerator ShakeRoutine(int i)
    {
        shaking[i] = true;
        var t = fuses[i].transform;
        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.25f)
        {
            t.localPosition = fuseHome[i] + Vector3.right * (Mathf.Sin(k * 60f) * 0.004f * (1f - k));
            yield return null;
        }
        t.localPosition = fuseHome[i];
        shaking[i] = false;
    }

    // ---------- Fusivel novo ----------

    void PickUpSpare()
    {
        if (spareRoutine != null) StopCoroutine(spareRoutine);
        holdingSpare = true;
        SetHover(null);
        spareFuse.GetComponent<Collider>().enabled = false;
    }

    void MoveSpare(Ray ray)
    {
        var plane = new Plane(transform.forward, transform.TransformPoint(0f, 0f, -0.3f));
        if (!plane.Raycast(ray, out float d)) return;
        Vector3 local = transform.InverseTransformPoint(ray.GetPoint(d));
        local.x = Mathf.Clamp(local.x, -0.45f, 0.45f);
        local.y = Mathf.Clamp(local.y, -0.45f, 0.4f);
        var t = spareFuse.transform;
        // Suavização que não depende do FPS (com 20 * deltaTime, o fusível ficava "mole" com o jogo lento).
        t.position = Vector3.Lerp(t.position, transform.TransformPoint(local), 1f - Mathf.Exp(-30f * Time.deltaTime));
        t.rotation = Quaternion.Slerp(t.rotation, transform.rotation, 1f - Mathf.Exp(-12f * Time.deltaTime));
    }

    void DropSpare(bool instant = false)
    {
        holdingSpare = false;
        var t = spareFuse.transform;
        if (instant)
        {
            t.localPosition = spareHomePos;
            t.localRotation = spareHomeRot;
            spareFuse.GetComponent<Collider>().enabled = true;
            return;
        }
        spareRoutine = StartCoroutine(Slide(t, spareHomePos, spareHomeRot, 0.25f, hideAtEnd: false, reenable: spareFuse.GetComponent<Collider>()));
    }

    void Insert(int slot)
    {
        holdingSpare = false;
        inserted = true;
        fusePresent[slot] = true;
        SetHover(null);
        spareRoutine = StartCoroutine(InsertRoutine(slot));
    }

    IEnumerator InsertRoutine(int slot)
    {
        yield return Slide(spareFuse.transform, fuseHome[slot], Quaternion.identity, 0.2f, hideAtEnd: false);
        Play(fuseClip);
        fuseSeated = true;   // destrava a alavanca; o LED ambar avisa que tem energia
        if (statusLed != null && ledReadyMaterial != null) statusLed.sharedMaterial = ledReadyMaterial;
    }

    // ---------- Alavanca ----------

    void GrabLever(float mouseY)
    {
        if (leverRoutine != null) StopCoroutine(leverRoutine);
        leverDragging = true;
        leverDragStartY = mouseY;
        leverDragStartProgress = lever.progress;
    }

    void DragLever(float mouseY)
    {
        float pixels = Mathf.Max(1f, Screen.height * leverDragScreenFraction);
        lever.progress = Mathf.Clamp01(leverDragStartProgress + (leverDragStartY - mouseY) / pixels);
        ApplyLever();
    }

    void ReleaseLever()
    {
        leverDragging = false;
        bool pulled = lever.progress >= 0.9f;
        leverRoutine = StartCoroutine(LeverTo(pulled ? 1f : 0f));
    }

    void ApplyLever() => lever.transform.localRotation = leverHomeRot * Quaternion.Euler(-leverAngle * lever.progress, 0f, 0f);

    IEnumerator LeverTo(float target)
    {
        if (target >= 1f)
        {
            leverDone = true;
            Disable(lever);
        }
        float from = lever.progress;
        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.2f)
        {
            lever.progress = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, k));
            ApplyLever();
            yield return null;
        }
        lever.progress = target;
        ApplyLever();
        if (target < 1f) yield break;

        Play(leverClip);
        if (statusLed != null && ledOkMaterial != null) statusLed.sharedMaterial = ledOkMaterial;
        Complete();
    }

    void Play(AudioClip clip)
    {
        if (clip == null) return;
        sfx.pitch = Random.Range(0.95f, 1.05f);
        sfx.PlayOneShot(clip, volume);
    }

    // ---------- Animacoes simples ----------

    static void Disable(FuseBoxPart p)
    {
        p.SetHighlight(false, Color.black);
        var c = p.GetComponent<Collider>();
        if (c != null) c.enabled = false;
    }

    IEnumerator Fall(Transform t)
    {
        Vector3 start = t.localPosition;
        float velocity = 0f;
        for (float elapsed = 0f; elapsed < 0.7f; elapsed += Time.deltaTime)
        {
            velocity += 3f * Time.deltaTime;
            start += new Vector3(0f, -velocity * Time.deltaTime, 0f);
            t.localPosition = start;
            yield return null;
        }
        t.gameObject.SetActive(false);
    }

    IEnumerator Slide(Transform t, Vector3 localPos, Quaternion localRot, float duration, bool hideAtEnd, Collider reenable = null)
    {
        Vector3 p0 = t.localPosition;
        Quaternion r0 = t.localRotation;
        for (float k = 0f; k < 1f; k += Time.deltaTime / duration)
        {
            float s = Mathf.SmoothStep(0f, 1f, k);
            t.localPosition = Vector3.Lerp(p0, localPos, s);
            t.localRotation = Quaternion.Slerp(r0, localRot, s);
            yield return null;
        }
        t.localPosition = localPos;
        t.localRotation = localRot;
        if (reenable != null) reenable.enabled = true;
        if (hideAtEnd) t.gameObject.SetActive(false);
    }
}
