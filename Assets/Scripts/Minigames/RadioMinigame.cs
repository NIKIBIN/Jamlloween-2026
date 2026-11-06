using System.Collections;
using System.Globalization;
using HeyHeyThere.PSXCarnivalKit;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Conserto do radio (walkie-talkie dos anos 80). Primeiro conserto do jogo, serve de tutorial.
/// O radio esta sem pilhas e tem um bilhete do patrao colado na frente com a frequencia certa.
///  1. Clicar no radio para vira-lo e ver as costas.
///  2. Arrastar a tampa traseira para fora (encaixe por pressao: precisa de um puxao firme).
///  3. Arrastar as pilhas ate o compartimento (encaixam direto). Elas vem do inventario do jogador.
///  4. Arrastar a tampa de volta ate encaixar. O radio liga, toca estatica e vira sozinho para a frente.
///  5. Girar o dial (arrastar em circulo). A estatica diminui perto da frequencia do bilhete.
///     Ao acertar, o patrao fala e o minigame termina logo (a fala continua com o jogador ja andando).
///     O jogador leva o radio: ele sai da mesa ao devolver o controle.
/// Sem HUD: tudo vem do proprio objeto (LED, estatica, bilhete, escala do dial).
/// O progresso fica nos campos do componente, entao sair no meio nao reseta nada.
/// </summary>
public class RadioMinigame : Minigame
{
    enum Drag { None, Cover, Batteries, Dial }

    const float MinFrequency = 88f;
    const float MaxFrequency = 108f;
    const float DialRange = 270f;           // graus de giro do dial, de MinFrequency a MaxFrequency
    const float RestYFront = 0.05f;         // altura do centro do radio sobre a mesa, deitado de frente (apoiado na tampa)
    const float RestYBack = 0.035f;         // deitado de costas (apoiado no dial)
    const float DragHeight = 0.1f;          // altura em que as pecas arrastadas flutuam
    const float OpenDistance = 0.05f;       // quanto puxar a tampa para ela soltar
    const float SnapDistance = 0.04f;       // distancia ao encaixe para a peca encaixar ao soltar
    static readonly Vector2 TableHalf = new Vector2(0.38f, 0.26f);

    [Header("Pecas")]
    [SerializeField] Transform radio;
    [Tooltip("Pai das pecas soltas na mesa. Fica na origem do minigame, sem rotacao.")]
    [SerializeField] Transform loose;
    [SerializeField] RadioPart body, cover, batteries, dial;
    [SerializeField] Transform coverAnchor;
    [Tooltip("Onde a tampa descansa depois de tirada.")]
    [SerializeField] Transform coverRest;
    [Tooltip("Onde as pilhas ficam na mesa quando o jogador as trouxe.")]
    [SerializeField] Transform batteriesHome;
    [Tooltip("Posicao das pilhas dentro do compartimento.")]
    [SerializeField] Transform batterySlot;
    [SerializeField] Renderer led;
    [SerializeField] Material ledOnMaterial, ledOffMaterial;
    [SerializeField] TMP_Text noteLabel;
    [SerializeField] RadioAudio radioAudio;
    [SerializeField] Color hoverColor = new Color(0.32f, 0.3f, 0.12f);

    [Header("Sintonia")]
    [Tooltip("Frequencia escrita no bilhete, onde o patrao responde (MHz).")]
    [SerializeField, Range(MinFrequency, MaxFrequency)] float correctFrequency = 97.3f;
    [Tooltip("Distancia (MHz) da frequencia certa em que a sintonia pega.")]
    [SerializeField, Range(0.05f, 3f)] float tolerance = 0.4f;
    [Tooltip("Distancia (MHz) alem da tolerancia ate a estatica chegar ao maximo.")]
    [SerializeField] float staticFalloff = 4f;
    [Tooltip("Tempo (s) dentro da tolerancia para a sintonia travar, evita acertar sem querer ao passar direto.")]
    [SerializeField] float lockSeconds = 0.3f;
    [Tooltip("Pausa (s) entre acertar a sintonia e devolver o controle. A fala do patrao continua tocando depois.")]
    [SerializeField, Range(0f, 1f)] float exitDelayAfterTune = 0.7f;

    [Header("Pilhas")]
    [Tooltip("Item do inventario do jogador que representa as pilhas.")]
    [SerializeField] string requiredItem = "battery";
    [SerializeField] int requiredCount = 2;
    [Tooltip("Onde as pilhas ficam no mapa. Se preenchido junto com o prefab, o pickup nasce neste ponto.")]
    [SerializeField] Transform batteryLocation;
    [SerializeField] GameObject batteryPickupPrefab;

    // --- estado do conserto (persiste ao sair) ---
    bool faceFront = true, flipping;
    bool coverOff, batteriesIn, connected;
    float dialAngle;
    System.Collections.Generic.List<string> playerItems;

    // --- arrasto ---
    Drag drag;
    RadioPart dragPart, hovered;
    Vector3 dragStart, grabOffset;
    float dragPlaneY, prevMouseAngle, lockTimer;
    Coroutine coverGlide, batteriesGlide;
    bool wasPowered;

    bool Powered => !coverOff && batteriesIn;
    float Frequency => Mathf.Lerp(MinFrequency, MaxFrequency, dialAngle / DialRange);
    bool Tuned => Mathf.Abs(Frequency - correctFrequency) <= tolerance;
    bool HasBatteries => playerItems == null || playerItems.FindAll(i => i == requiredItem).Count >= requiredCount;

    void OnValidate() => correctFrequency = Mathf.Clamp(correctFrequency, MinFrequency, MaxFrequency);

    protected override void Awake()
    {
        base.Awake();
        onExited.AddListener(() => { if (IsCompleted) TakeRadio(); });
        noteLabel.text = correctFrequency.ToString("0.0", CultureInfo.InvariantCulture) + " MHz";
        batteries.gameObject.SetActive(false);
        led.sharedMaterial = ledOffMaterial;
        ApplyDial();
        SpawnBatteryPickup();
    }

    void SpawnBatteryPickup()
    {
        if (batteryPickupPrefab == null || batteryLocation == null) return;
        var go = Instantiate(batteryPickupPrefab, batteryLocation.position, batteryLocation.rotation);
        go.name = "Pilhas (radio)";
        if (go.GetComponentInChildren<Collider>() == null)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                var box = go.AddComponent<BoxCollider>();
                box.center = go.transform.InverseTransformPoint(b.center);
                box.size = b.size;
            }
        }
        var pk = go.GetComponent<Pickup>();
        if (pk == null) pk = go.AddComponent<Pickup>();
        pk.kind = Pickup.Kind.Item;
        pk.item = requiredItem;
        pk.amount = requiredCount;
        if (pk.title == "") pk.title = "Pilhas";
    }

    protected override void OnBegin(PlayerInteractor interactor)
    {
        var walker = interactor.GetComponent<LevelWalker>();
        playerItems = walker != null ? walker.items : null;
        // As pilhas so aparecem na mesa se o jogador as trouxe.
        batteries.gameObject.SetActive(batteriesIn || HasBatteries);
    }

    // =================================================================
    // Regras: o que pode ser usado agora
    // =================================================================

    bool CanUse(RadioPart p)
    {
        if (connected || flipping) return false;
        switch (p.kind)
        {
            case RadioPart.Kind.Body: return faceFront && !Powered;   // 1: vira para ver as costas (a volta para a frente e automatica)
            case RadioPart.Kind.Cover: return !faceFront;
            case RadioPart.Kind.Batteries: return !faceFront && coverOff && !batteriesIn;
            case RadioPart.Kind.Dial: return faceFront && Powered;
        }
        return false;
    }

    // =================================================================
    // Loop
    // =================================================================

    // O som e o LED seguem o estado mesmo com o jogador fora do minigame. (Update e do Minigame.)
    void LateUpdate()
    {
        radioAudio.SetStatic(Powered && !connected ? StaticLevel() : -1f);
        if (Powered != wasPowered)
        {
            wasPowered = Powered;
            led.sharedMaterial = Powered ? ledOnMaterial : ledOffMaterial;
            radioAudio.Click();
        }
    }

    float StaticLevel() => Mathf.Clamp01((Mathf.Abs(Frequency - correctFrequency) - tolerance) / Mathf.Max(staticFalloff, 0.01f));

    protected override void Tick(Camera viewCamera)
    {
        var mouse = Mouse.current;
        if (mouse == null) return;
        Vector2 mp = mouse.position.ReadValue();
        Ray ray = viewCamera.ScreenPointToRay(mp);
        bool down = mouse.leftButton.wasPressedThisFrame;
        bool held = mouse.leftButton.isPressed;

        if (drag != Drag.None)
        {
            SetHover(dragPart);
            UpdateDrag(viewCamera, ray, mp, held);
            return;
        }

        var hit = Pick(ray, out Vector3 hitPoint);
        var usable = hit != null && CanUse(hit) ? hit : null;
        SetHover(usable);
        if (down && usable != null) BeginDrag(viewCamera, usable, ray, mp, hitPoint);
    }

    protected override void OnExitRequested()
    {
        SetHover(null);
        // Solta o que estiver na mao: o resto do progresso fica como esta.
        switch (drag)
        {
            case Drag.Cover: ReleaseCover(instant: true); break;
            case Drag.Batteries: ReleaseBatteries(instant: true); break;
            default: EndDrag(); break;
        }
    }

    RadioPart Pick(Ray ray, out Vector3 point)
    {
        var hits = Physics.RaycastAll(ray, 5f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            var p = h.collider.GetComponent<RadioPart>();
            if (p != null) { point = h.point; return p; }
        }
        point = default;
        return null;
    }

    void SetHover(RadioPart p)
    {
        if (hovered == p) return;
        if (hovered != null) hovered.SetHighlight(false, hoverColor);
        hovered = p;
        if (hovered != null) hovered.SetHighlight(true, hoverColor);
    }

    // =================================================================
    // Arrasto
    // =================================================================

    /// <param name="hit">O ponto da peça que o jogador clicou: ele fica embaixo do cursor durante o arrasto.</param>
    void BeginDrag(Camera cam, RadioPart part, Ray ray, Vector2 mp, Vector3 hit)
    {
        dragPart = part;
        switch (part.kind)
        {
            case RadioPart.Kind.Body:
                StartCoroutine(Flip(!faceFront));
                break;

            case RadioPart.Kind.Cover:
                StopGlide(ref coverGlide);
                drag = Drag.Cover;
                if (coverOff) { dragPlaneY = DragHeight; GrabOffset(cover.transform, hit); }
                else
                {
                    // Puxa no plano do ponto clicado: a tampa anda junto com ele, 1:1.
                    dragPlaneY = transform.InverseTransformPoint(hit).y;
                    dragStart = hit;
                }
                break;

            case RadioPart.Kind.Batteries:
                StopGlide(ref batteriesGlide);
                drag = Drag.Batteries;
                dragPlaneY = DragHeight;
                GrabOffset(batteries.transform, hit);
                break;

            case RadioPart.Kind.Dial:
                drag = Drag.Dial;
                lockTimer = 0f;
                if (!DialMouseAngle(cam, mp, out prevMouseAngle)) prevMouseAngle = 0f;
                break;
        }
    }

    void UpdateDrag(Camera cam, Ray ray, Vector2 mp, bool held)
    {
        switch (drag)
        {
            case Drag.Cover:
                if (!coverOff) PullCover(ray); else FollowMouse(cover.transform, ray);
                if (!held) ReleaseCover(instant: false);
                break;

            case Drag.Batteries:
                FollowMouse(batteries.transform, ray);
                if (!held) ReleaseBatteries(instant: false);
                break;

            case Drag.Dial:
                if (!held) { EndDrag(); break; }
                if (DialMouseAngle(cam, mp, out float a))
                {
                    dialAngle = Mathf.Clamp(dialAngle + Mathf.DeltaAngle(prevMouseAngle, a), 0f, DialRange);
                    prevMouseAngle = a;
                    ApplyDial();
                }
                lockTimer = Tuned ? lockTimer + Time.deltaTime : 0f;
                if (lockTimer >= lockSeconds) Connect();
                break;
        }
    }

    void EndDrag()
    {
        drag = Drag.None;
        dragPart = null;
    }

    // ---------- Tampa ----------

    /// <summary>A tampa acompanha o mouse 1:1 desde o clique; depois de <see cref="OpenDistance"/> ela solta com um clique.</summary>
    void PullCover(Ray ray)
    {
        if (!PlanePoint(ray, dragPlaneY, out var p)) return;
        Vector3 delta = Flat(p - dragStart);
        cover.transform.position = coverAnchor.position + delta;
        if (delta.magnitude < OpenDistance) return;
        coverOff = true;
        cover.transform.SetParent(loose, true);   // nao gira junto com o radio
        dragPlaneY = DragHeight;
        GrabOffset(cover.transform, p);           // p: o ponto clicado, que andou junto com a tampa
        radioAudio.Click();
    }

    void ReleaseCover(bool instant)
    {
        EndDrag();
        float time = instant ? 0f : 0.15f;
        if (!coverOff)   // soltou antes de vencer a resistencia: volta para o lugar
        {
            Glide(ref coverGlide, cover.transform, coverAnchor.localPosition, coverAnchor.localRotation, time, null);
            return;
        }
        if (Distance(cover.transform.position, coverAnchor.position) <= SnapDistance)
        {
            coverOff = false;
            cover.transform.SetParent(radio, true);
            Glide(ref coverGlide, cover.transform, coverAnchor.localPosition, coverAnchor.localRotation, time, () =>
            {
                radioAudio.Click();
                // Tampa fechada com as pilhas: o radio liga e vira sozinho para a frente, pronto para o dial.
                if (Powered && !faceFront && !flipping) StartCoroutine(Flip(true));
            });
        }
        else Glide(ref coverGlide, cover.transform, coverRest.localPosition, coverRest.localRotation, time, null);
    }

    // ---------- Pilhas ----------

    void ReleaseBatteries(bool instant)
    {
        EndDrag();
        float time = instant ? 0f : 0.15f;
        if (coverOff && !faceFront && Distance(batteries.transform.position, batterySlot.position) <= SnapDistance)
        {
            batteriesIn = true;
            if (playerItems != null)
                for (int i = 0; i < requiredCount; i++) playerItems.Remove(requiredItem);
            batteries.GetComponent<Collider>().enabled = false;
            batteries.transform.SetParent(radio, true);
            Glide(ref batteriesGlide, batteries.transform, batterySlot.localPosition, batterySlot.localRotation, time, radioAudio.Click);
        }
        else Glide(ref batteriesGlide, batteries.transform, batteriesHome.localPosition, batteriesHome.localRotation, time, null);
    }

    // ---------- Dial ----------

    void ApplyDial() => dial.transform.localRotation = Quaternion.Euler(0f, dialAngle - DialRange / 2f, 0f);

    /// <summary>Angulo do mouse em volta do dial na tela, em graus, no sentido horario a partir de cima.</summary>
    bool DialMouseAngle(Camera cam, Vector2 mp, out float angle)
    {
        Vector2 center = cam.WorldToScreenPoint(dial.transform.position);
        Vector2 v = mp - center;
        angle = Mathf.Atan2(v.x, v.y) * Mathf.Rad2Deg;
        return v.sqrMagnitude > 100f;   // perto demais do centro o angulo fica instavel
    }

    void Connect()
    {
        connected = true;
        EndDrag();
        SetHover(null);
        radioAudio.PlayBoss();
        Complete(exitDelayAfterTune);
    }

    /// <summary>Consertado, o jogador leva o radio com ele: o radio e as pecas soltas saem da mesa.</summary>
    void TakeRadio()
    {
        // O som fica na mesa: a fala do patrao continua tocando depois que o radio some.
        if (radioAudio != null && radioAudio.transform.IsChildOf(radio)) radioAudio.transform.SetParent(transform, true);
        if (radio != null) radio.gameObject.SetActive(false);
        if (loose != null) loose.gameObject.SetActive(false);
    }

    public override void ForceComplete()
    {
        base.ForceComplete();
        TakeRadio();
    }

    // ---------- Virar o radio ----------

    IEnumerator Flip(bool toFront)
    {
        flipping = true;
        SetHover(null);
        radioAudio.Click();
        float fromAngle = faceFront ? 0f : 180f;
        float toAngle = toFront ? 0f : 180f;
        float y0 = radio.localPosition.y, y1 = toFront ? RestYFront : RestYBack;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
        {
            float k = Mathf.SmoothStep(0f, 1f, t);
            radio.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(fromAngle, toAngle, k));
            radio.localPosition = new Vector3(0f, Mathf.Lerp(y0, y1, k) + 0.07f * Mathf.Sin(t * Mathf.PI), 0f);
            yield return null;
        }
        radio.localRotation = Quaternion.Euler(0f, 0f, toAngle);
        radio.localPosition = new Vector3(0f, y1, 0f);
        faceFront = toFront;
        flipping = false;
        radioAudio.Click();
    }

    // =================================================================
    // Utilitarios
    // =================================================================

    bool PlanePoint(Ray ray, float localY, out Vector3 point)
    {
        var plane = new Plane(transform.up, transform.TransformPoint(0f, localY, 0f));
        if (plane.Raycast(ray, out float d)) { point = ray.GetPoint(d); return true; }
        point = default;
        return false;
    }

    /// <summary>Guarda onde a peça foi pega (em coordenadas do minigame): esse ponto segue o cursor, sem salto.</summary>
    void GrabOffset(Transform t, Vector3 grabbed)
    {
        grabOffset = transform.InverseTransformPoint(t.position) - transform.InverseTransformPoint(grabbed);
    }

    /// <summary>
    /// A peça flutua com o centro em <see cref="dragPlaneY"/>. O ponto pego fica mais alto ou mais baixo que o centro;
    /// o raio do cursor corta o plano da altura desse ponto, então ele fica embaixo do cursor com a câmera inclinada.
    /// A peça sobe da mesa até a altura de arrasto aos poucos, pela suavização.
    /// </summary>
    void FollowMouse(Transform t, Ray ray)
    {
        if (!PlanePoint(ray, dragPlaneY - grabOffset.y, out var p)) return;
        Vector3 local = transform.InverseTransformPoint(p) + grabOffset;
        local.x = Mathf.Clamp(local.x, -TableHalf.x, TableHalf.x);
        local.z = Mathf.Clamp(local.z, -TableHalf.y, TableHalf.y);
        local.y = dragPlaneY;
        t.position = Vector3.Lerp(t.position, transform.TransformPoint(local), 1f - Mathf.Exp(-30f * Time.deltaTime));
    }

    /// <summary>Distancia no plano da mesa, ignorando a altura.</summary>
    float Distance(Vector3 a, Vector3 b) => Flat(a - b).magnitude;

    Vector3 Flat(Vector3 v) => Vector3.ProjectOnPlane(v, transform.up);

    void StopGlide(ref Coroutine c)
    {
        if (c != null) StopCoroutine(c);
        c = null;
    }

    void Glide(ref Coroutine slot, Transform t, Vector3 localPos, Quaternion localRot, float duration, System.Action done)
    {
        StopGlide(ref slot);
        if (duration <= 0f)
        {
            t.localPosition = localPos;
            t.localRotation = localRot;
            done?.Invoke();
            return;
        }
        slot = StartCoroutine(GlideRoutine(t, localPos, localRot, duration, done));
    }

    IEnumerator GlideRoutine(Transform t, Vector3 localPos, Quaternion localRot, float duration, System.Action done)
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
        done?.Invoke();
    }
}
