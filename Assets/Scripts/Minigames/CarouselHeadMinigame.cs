using System.Collections;
using System.Collections.Generic;
using HeyHeyThere.PSXCarnivalKit;
using Jamlloween.Progression;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Fase 1 do conserto do carrossel: a cabeca do cavalo. Este componente fica no proprio cavalo sem cabeca.
///  1. Trazer a cabeca (ela esta num ponto do mapa, configuravel) e arrastar ate o pescoco do cavalo.
///  2. Girar a cabeca (arrastar em circulo) ate o pino dela alinhar com o do pescoco. Trava com um clique.
/// Sem HUD: o pescoco do cavalo mostra onde a cabeca encaixa e o pino mostra o alinhamento.
/// O progresso fica nos campos do componente, entao sair no meio nao reseta nada.
/// As posicoes sao no espaco local do pai da cabeca (o cavalo); o eixo +Z do pescoco e o eixo de giro e aponta para fora do cavalo.
/// </summary>
public class CarouselHeadMinigame : Minigame
{
    enum Drag { None, Move, Rotate }

    const float PlaneRange = 1.3f;          // quanto a cabeca pode se afastar do pescoco ao ser arrastada (m)

    [Header("Pecas")]
    [SerializeField] CarouselPart head;
    [Tooltip("Onde a cabeca encaixa no pescoco. O eixo +Z dele e o eixo de giro, apontando para fora do cavalo.")]
    [SerializeField] Transform neckMount;
    [Tooltip("Onde a cabeca fica carregada, em frente ao pescoco. Define o plano em que ela e arrastada.")]
    [SerializeField] Transform carryPoint;
    [Tooltip("Onde a cabeca descansa quando solta longe do pescoco.")]
    [SerializeField] Transform headHome;
    [SerializeField] CarouselAudio carouselAudio;
    [SerializeField] Color hoverColor = new Color(0.32f, 0.3f, 0.12f);

    [Header("Alinhamento")]
    [Tooltip("Margem (graus) para o pino contar como alinhado.")]
    [SerializeField, Range(1f, 45f)] float alignTolerance = 8f;
    [Tooltip("Tempo (s) alinhado para a cabeca travar, evita travar sem querer ao passar direto.")]
    [SerializeField] float lockSeconds = 0.25f;
    [Tooltip("Giro minimo (graus) com que a cabeca encaixa, para nunca nascer alinhada.")]
    [SerializeField, Range(30f, 170f)] float minInitialRoll = 80f;
    [Tooltip("Distancia ao pescoco para a cabeca encaixar ao soltar.")]
    [SerializeField] float snapDistance = 0.25f;
    [Tooltip("Quanto a cabeca afunda no pescoco ao travar.")]
    [SerializeField] float seatDepth = 0.015f;

    [Header("Cabeca")]
    [Tooltip("Item do inventario do jogador que representa a cabeca.")]
    [SerializeField] string requiredItem = "horse_head";
    [Tooltip("Onde a cabeca fica no mapa. Se preenchido junto com o prefab, o pickup nasce neste ponto.")]
    [SerializeField] Transform headLocation;
    [SerializeField] GameObject headPickupPrefab;
    [Header("Progressao")]
    [Tooltip("Id da interacao de pegar a cabeca nas etapas do ProgressionDirector. Vazio = o pickup nasce sem controle do diretor.")]
    [SerializeField] string headPickupInteractionId = "head_pickup";
    [Tooltip("Evento enviado ao diretor ao pegar a cabeca.")]
    [SerializeField] string headPickupCompletionEvent = "HeadPicked";

    // --- estado do conserto (persiste ao sair) ---
    bool mounted, locked, docking;
    float headRoll;
    List<string> playerItems;

    // --- arrasto ---
    Drag drag;
    Vector3 grabOffset;
    float prevMouseAngle, lockTimer;
    bool hoverOn;
    Coroutine glide;

    /// <summary>Espaco do cavalo: o pai da cabeca. As posicoes locais de pescoco, ponto de carga e descanso sao nele.</summary>
    Transform Frame => head.transform.parent;

    public bool HeadMounted => mounted;
    public bool HeadLocked => locked;
    bool HasHead => playerItems == null || playerItems.Contains(requiredItem);
    float AlignError => Mathf.Abs(Mathf.DeltaAngle(headRoll, 0f));

    Quaternion MountRot => neckMount.localRotation;
    Quaternion RollRot(float roll) => neckMount.localRotation * Quaternion.AngleAxis(roll, Vector3.forward);

    protected override void Awake()
    {
        base.Awake();
        head.gameObject.SetActive(false);
        SpawnHeadPickup();
    }

    void SpawnHeadPickup()
    {
        if (headPickupPrefab == null || headLocation == null) return;
        var go = Instantiate(headPickupPrefab, headLocation.position, headLocation.rotation);
        go.name = "Cabeca do cavalo";
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
        pk.amount = 1;
        if (pk.title == "") pk.title = "Cabeca do cavalo";
        if (!string.IsNullOrEmpty(headPickupInteractionId))
            go.AddComponent<StepInteraction>().Configure(headPickupInteractionId, headPickupCompletionEvent, null, true);
    }

    /// <summary>Usado pelo pulo de etapas do debug: a cabeca ja aparece encaixada no cavalo.</summary>
    public override void ForceComplete()
    {
        if (IsCompleted) return;
        mounted = locked = true;
        headRoll = 0f;
        head.gameObject.SetActive(true);
        head.transform.SetLocalPositionAndRotation(neckMount.localPosition + SeatOffset(seatDepth), RollRot(0f));
        var col = head.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        base.ForceComplete();
    }

    protected override void OnBegin(PlayerInteractor interactor)
    {
        var walker = interactor.GetComponent<LevelWalker>();
        playerItems = walker != null ? walker.items : null;
        // A cabeca so aparece se o jogador a trouxe.
        if (!mounted && HasHead && !head.gameObject.activeSelf)
        {
            head.gameObject.SetActive(true);
            head.transform.SetLocalPositionAndRotation(headHome.localPosition, headHome.localRotation);
        }
    }

    // =================================================================
    // Loop
    // =================================================================

    bool CanUse() => !locked && !docking && head.gameObject.activeSelf;

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
            SetHover(true);
            if (drag == Drag.Move) UpdateMove(ray, held);
            else UpdateRotate(viewCamera, mp, held);
            return;
        }

        bool over = CanUse() && IsOverHead(ray);
        SetHover(over);
        if (!down || !over) return;
        if (mounted) BeginRotate(viewCamera, mp);
        else BeginMove(ray);
    }

    protected override void OnExitRequested()
    {
        SetHover(false);
        switch (drag)
        {
            case Drag.Move: ReleaseHead(instant: true); break;
            case Drag.Rotate: EndRotate(); break;
        }
    }

    bool IsOverHead(Ray ray)
    {
        var hits = Physics.RaycastAll(ray, 6f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            var p = h.collider.GetComponent<CarouselPart>();
            if (p != null) return p == head;
        }
        return false;
    }

    void SetHover(bool on)
    {
        if (hoverOn == on) return;
        hoverOn = on;
        head.SetHighlight(on, hoverColor);
    }

    // =================================================================
    // Passo 1: levar a cabeca ao pescoco
    // =================================================================

    Plane CarryPlane => new Plane(neckMount.forward, carryPoint.position);

    void BeginMove(Ray ray)
    {
        StopGlide();
        drag = Drag.Move;
        grabOffset = CarryPlane.Raycast(ray, out float d) ? head.transform.position - ray.GetPoint(d) : Vector3.zero;
    }

    void UpdateMove(Ray ray, bool held)
    {
        if (CarryPlane.Raycast(ray, out float d))
        {
            Vector3 world = ray.GetPoint(d) + grabOffset;
            Vector3 local = Frame.InverseTransformPoint(world);
            Vector3 anchor = carryPoint.localPosition;
            Vector3 off = Vector3.ClampMagnitude(local - anchor, PlaneRange);
            // Fica no plano: so mexe nos eixos paralelos ao pescoco.
            Vector3 normalLocal = Frame.InverseTransformDirection(neckMount.forward);
            off -= Vector3.Project(off, normalLocal);
            Vector3 target = anchor + off;
            float k = 1f - Mathf.Exp(-30f * Time.deltaTime);
            head.transform.localPosition = Vector3.Lerp(head.transform.localPosition, target, k);
            head.transform.localRotation = Quaternion.Slerp(head.transform.localRotation, MountRot, 1f - Mathf.Exp(-10f * Time.deltaTime));
        }
        if (!held) ReleaseHead(instant: false);
    }

    void ReleaseHead(bool instant)
    {
        drag = Drag.None;
        float time = instant ? 0f : 0.2f;
        Vector3 off = Vector3.ProjectOnPlane(head.transform.position - carryPoint.position, neckMount.forward);
        if (off.magnitude <= snapDistance)
        {
            Dock(instant);
            return;
        }
        Glide(headHome.localPosition, headHome.localRotation, time, null);
    }

    void Dock(bool instant)
    {
        mounted = true;
        docking = true;
        headRoll = Random.Range(minInitialRoll, 180f) * (Random.value < 0.5f ? -1f : 1f);
        if (playerItems != null) playerItems.Remove(requiredItem);
        Glide(neckMount.localPosition + SeatOffset(0f), RollRot(headRoll), instant ? 0f : 0.35f, () =>
        {
            docking = false;
            if (carouselAudio != null) carouselAudio.Click();
        });
    }

    /// <summary>Deslocamento ao longo do eixo do pescoco: positivo para fora, negativo para dentro.</summary>
    Vector3 SeatOffset(float depth) => Frame.InverseTransformDirection(neckMount.forward) * -depth;

    // =================================================================
    // Passo 2: girar ate o pino alinhar
    // =================================================================

    void BeginRotate(Camera cam, Vector2 mp)
    {
        drag = Drag.Rotate;
        lockTimer = 0f;
        if (!MouseAngle(cam, mp, out prevMouseAngle)) prevMouseAngle = 0f;
    }

    void UpdateRotate(Camera cam, Vector2 mp, bool held)
    {
        if (held && MouseAngle(cam, mp, out float a))
        {
            // Angulo no sentido horario na tela. O eixo +Z do pescoco aponta para quem olha, e giro positivo no Unity e horario nesse caso.
            headRoll = Mathf.DeltaAngle(0f, headRoll + Mathf.DeltaAngle(prevMouseAngle, a));
            prevMouseAngle = a;
            head.transform.localRotation = RollRot(headRoll);
        }

        bool aligned = AlignError <= alignTolerance;
        lockTimer = aligned ? lockTimer + Time.deltaTime : 0f;
        if (aligned && (lockTimer >= lockSeconds || !held)) { Lock(); return; }
        if (!held) EndRotate();
    }

    void EndRotate() => drag = Drag.None;

    /// <summary>Angulo do mouse em volta da cabeca na tela, em graus, no sentido horario a partir de cima.</summary>
    bool MouseAngle(Camera cam, Vector2 mp, out float angle)
    {
        Vector2 center = cam.WorldToScreenPoint(neckMount.position);
        Vector2 v = mp - center;
        angle = Mathf.Atan2(v.x, v.y) * Mathf.Rad2Deg;
        return v.sqrMagnitude > 100f;   // perto demais do centro o angulo fica instavel
    }

    void Lock()
    {
        locked = true;
        drag = Drag.None;
        headRoll = 0f;
        SetHover(false);
        head.GetComponent<Collider>().enabled = false;
        Glide(neckMount.localPosition + SeatOffset(seatDepth), RollRot(0f), 0.12f, () =>
        {
            if (carouselAudio != null) carouselAudio.Click();
        });
        Complete();
    }

    // =================================================================
    // Utilitarios
    // =================================================================

    void StopGlide()
    {
        if (glide != null) StopCoroutine(glide);
        glide = null;
    }

    void Glide(Vector3 localPos, Quaternion localRot, float duration, System.Action done)
    {
        StopGlide();
        if (duration <= 0f)
        {
            head.transform.SetLocalPositionAndRotation(localPos, localRot);
            done?.Invoke();
            return;
        }
        glide = StartCoroutine(GlideRoutine(localPos, localRot, duration, done));
    }

    IEnumerator GlideRoutine(Vector3 localPos, Quaternion localRot, float duration, System.Action done)
    {
        var t = head.transform;
        Vector3 p0 = t.localPosition;
        Quaternion r0 = t.localRotation;
        for (float k = 0f; k < 1f; k += Time.deltaTime / duration)
        {
            float s = Mathf.SmoothStep(0f, 1f, k);
            t.SetLocalPositionAndRotation(Vector3.Lerp(p0, localPos, s), Quaternion.Slerp(r0, localRot, s));
            yield return null;
        }
        t.SetLocalPositionAndRotation(localPos, localRot);
        done?.Invoke();
    }
}
