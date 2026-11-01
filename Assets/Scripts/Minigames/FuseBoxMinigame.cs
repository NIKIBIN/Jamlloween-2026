using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Painel de fusiveis diegetico. Passos:
///  1. Segurar o botao esquerdo em cada um dos 4 parafusos ate solta-los.
///  2. Clicar na tampa para tirar.
///  3. Achar o fusivel queimado (escuro) e clicar para remover.
///  4. Pegar o fusivel novo na prateleira e clicar no encaixe vazio.
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

    [Header("Visual")]
    [SerializeField] Renderer statusLed;
    [SerializeField] Material ledOkMaterial;
    [SerializeField] Material burntFuseMaterial;
    [SerializeField] Color hoverColor = new Color(0.45f, 0.4f, 0.15f);

    [Header("Ajustes")]
    [Tooltip("Qual fusivel esta queimado. -1 sorteia ao iniciar a cena.")]
    [SerializeField] int burntIndex = -1;
    [SerializeField] float unscrewTime = 0.7f;
    [SerializeField] float unscrewDistance = 0.03f;
    [SerializeField] float unscrewTurns = 2f;

    bool[] screwOut;
    bool[] fusePresent;
    bool[] shaking;
    Vector3[] screwHome, fuseHome;
    Vector3 spareHomePos;
    Quaternion spareHomeRot;
    bool coverOff, burntRemoved, holdingSpare, inserted;
    FuseBoxPart hovered;
    Coroutine spareRoutine;

    public bool CoverRemoved => coverOff;
    public bool BurntFuseRemoved => burntRemoved;
    public int BurntIndex => burntIndex;

    protected override void Awake()
    {
        base.Awake();
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

        if (burntIndex < 0 || burntIndex >= fuses.Length) burntIndex = Random.Range(0, fuses.Length);
        var glass = fuses[burntIndex].transform.Find("Glass");
        if (glass != null && burntFuseMaterial != null) glass.GetComponent<Renderer>().sharedMaterial = burntFuseMaterial;
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
        }
        return false;
    }

    // ---------- Loop ----------

    protected override void Tick(Camera viewCamera)
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Ray ray = viewCamera.ScreenPointToRay(mouse.position.ReadValue());
        bool down = mouse.leftButton.wasPressedThisFrame;
        bool held = mouse.leftButton.isPressed;

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
        }
    }

    protected override void OnExitRequested()
    {
        SetHover(null);
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
        if (hovered != null) hovered.SetHighlight(false, hoverColor);
        hovered = p;
        if (hovered != null) hovered.SetHighlight(true, hoverColor);
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
        Disable(p);
        StartCoroutine(Fall(p.transform));
    }

    // ---------- Tampa / fusiveis ----------

    void RemoveCover()
    {
        coverOff = true;
        Disable(cover);
        StartCoroutine(Slide(cover.transform, new Vector3(0f, -0.4f, -0.5f), Quaternion.Euler(35f, 0f, 0f), 0.5f, hideAtEnd: true));
    }

    void PullBurnt()
    {
        var f = fuses[burntIndex];
        fusePresent[burntIndex] = false;
        burntRemoved = true;
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
        t.position = Vector3.Lerp(t.position, transform.TransformPoint(local), 20f * Time.deltaTime);
        t.rotation = Quaternion.Slerp(t.rotation, transform.rotation, 12f * Time.deltaTime);
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
        if (statusLed != null && ledOkMaterial != null) statusLed.sharedMaterial = ledOkMaterial;
        Complete();
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
