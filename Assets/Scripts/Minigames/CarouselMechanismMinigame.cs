using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Fase 2 do conserto do carrossel: o mecanismo do eixo central. Um objeto ficou preso nas engrenagens,
/// o motor forcou e a correia saltou. Este componente fica no painel da base do eixo.
///  1. Abrir o painel: arrastar a tampa para fora (encaixe por pressao: precisa de um puxao firme).
///  2. Tirar o objeto preso: arrastar para fora das engrenagens. Toca um rangido alto (e emite barulho).
///  3. Recolocar a correia: arrastar ate a polia do motor e depois ate a polia do eixo.
///  4. Esticar a correia: girar o tensor (arrastar em circulo) ate ficar firme.
///  5. Fechar o painel: arrastar a tampa de volta ate encaixar.
///  6. Puxar a alavanca de partida.
/// Regra de ordem: se a correia for recolocada com o objeto ainda preso, ela encaixa normalmente, mas ao
/// puxar a alavanca o motor forca e a correia salta de novo (e e preciso refazer os passos 3 e 4).
/// Sem HUD: tudo vem do proprio painel (correia, tensor, LED). Todas as posicoes sao no espaco local deste
/// transform, que olha para +Z. O progresso fica nos campos do componente, entao sair no meio nao reseta nada.
/// </summary>
public class CarouselMechanismMinigame : Minigame
{
    enum Drag { None, Cover, Object, Belt, Tensor, Lever }
    enum BeltState { Fallen, OnMotor, Mounted }

    const float PanelHalfX = 0.24f, PanelMinY = -0.15f, PanelMaxY = 0.16f;   // area util dentro do painel
    const float BeltPlaneZ = 0.07f;
    const float CoverLiftZ = -0.09f;       // altura (z) em que a tampa flutua enquanto e carregada

    [Header("Tampa")]
    [SerializeField] CarouselPart cover;
    [SerializeField] Transform coverAnchor;
    [Tooltip("Onde a tampa descansa depois de tirada.")]
    [SerializeField] Transform coverRest;

    [Header("Objeto preso")]
    [SerializeField] CarouselPart stuckObject;
    [Tooltip("Posicao do objeto entalado nas engrenagens.")]
    [SerializeField] Transform objectSlot;
    [Tooltip("Onde o objeto fica depois de tirado.")]
    [SerializeField] Transform objectRest;
    [Tooltip("Opcional. Prefab do objeto que fica preso nas engrenagens (qualquer prop). Vazio = o objeto padrao do prefab.")]
    [SerializeField] GameObject stuckObjectPrefab;
    [Tooltip("Tamanho (m) da maior dimensao do objeto preso.")]
    [SerializeField] float stuckObjectSize = 0.05f;

    [Header("Correia")]
    [SerializeField] CarouselPart beltGrip;
    [SerializeField] LineRenderer beltLine;
    [SerializeField] Transform motorPulley;
    [SerializeField] Transform shaftPulley;
    [SerializeField] float motorRadius = 0.034f;
    [SerializeField] float shaftRadius = 0.064f;
    [Tooltip("Onde a correia fica caida, depois que saltou.")]
    [SerializeField] Transform beltRest;

    [Header("Tensor")]
    [SerializeField] CarouselPart tensor;

    [Header("Alavanca e LED")]
    [Tooltip("Gira em torno do eixo Z local. Em repouso aponta para cima; puxa-se no sentido horario ate apontar para baixo.")]
    [SerializeField] CarouselPart lever;
    [SerializeField] Renderer statusLed;
    [SerializeField] Material ledBrokenMaterial, ledReadyMaterial, ledRunningMaterial;
    [Tooltip("Engrenagens e polias que giram quando o motor liga.")]
    [SerializeField] Transform[] spinners;

    [SerializeField] CarouselAudio carouselAudio;
    [SerializeField] Color hoverColor = new Color(0.32f, 0.3f, 0.12f);

    [Header("Tensor: ajuste")]
    [Tooltip("Margem (graus) em volta do ponto firme para o tensor contar como esticado.")]
    [SerializeField, Range(1f, 60f)] float tensorTolerance = 14f;
    [Tooltip("Quantos graus o tensor gira ate a correia ficar firme.")]
    [SerializeField, Range(90f, 720f)] float tensorTargetAngle = 300f;
    [Tooltip("Tempo (s) dentro da margem para o tensor travar, evita travar sem querer ao passar direto.")]
    [SerializeField] float tensorLockSeconds = 0.3f;

    [Header("Ajustes")]
    [SerializeField] float coverOpenDistance = 0.035f;
    [SerializeField] float coverSnapDistance = 0.12f;
    [Tooltip("Quanto puxar o objeto ate ele soltar das engrenagens (e o rangido tocar).")]
    [SerializeField] float objectBreakDistance = 0.03f;
    [Tooltip("Distancia das engrenagens a partir da qual o objeto conta como tirado.")]
    [SerializeField] float objectClearDistance = 0.11f;
    [SerializeField] float beltSnapDistance = 0.06f;
    [SerializeField] float beltHang = 0.17f;
    [Tooltip("Quanto a correia frouxa cai no meio, em metros.")]
    [SerializeField] float maxBeltSag = 0.05f;
    [SerializeField] float leverAngle = 160f;

    [Header("Barulho")]
    [Tooltip("Raio do barulho do rangido ao tirar o objeto. Chega a quem escuta o NoiseSystem.")]
    [SerializeField] float creakNoiseRadius = 18f;
    [Tooltip("Disparado a cada rangido, com a posicao do painel.")]
    public UnityEvent onCreak;

    // --- estado do conserto (persiste ao sair) ---
    bool coverOff, objectRemoved, tensorLocked, leverDone, busy, motorRunning;
    BeltState belt = BeltState.Fallen;
    float tensorAngle, leverProgress;

    // --- arrasto ---
    Drag drag;
    CarouselPart hovered;
    Vector3 grabOffset, dragStart;
    float prevMouseAngle, lockTimer, leverAccum;
    bool objectBroken;
    Coroutine coverGlide, objectGlide;

    // --- correia (desenho) ---
    Vector2 beltA, beltB, beltTargetA, beltTargetB;
    float beltRa, beltRb, beltTargetRa, beltTargetRb;
    bool beltInit;
    Collider beltGripCollider;

    public bool PanelOpen => coverOff;
    public bool ObjectRemoved => objectRemoved;
    public bool BeltMounted => belt == BeltState.Mounted;
    public bool TensorLocked => tensorLocked;
    bool Ready => !coverOff && belt == BeltState.Mounted && tensorLocked;

    Vector2 MotorPos => motorPulley.localPosition;
    Vector2 ShaftPos => shaftPulley.localPosition;
    Vector2 HangPos => MotorPos + new Vector2(0.02f, -beltHang);

    protected override void Awake()
    {
        base.Awake();
        beltGripCollider = beltGrip.GetComponent<Collider>();
        ApplyStuckObjectPrefab();
        tensor.transform.localRotation = Quaternion.identity;
        SetBeltTargets(snap: true);
        RefreshLed();
    }

    // =================================================================
    // Objeto preso configuravel
    // =================================================================

    void ApplyStuckObjectPrefab()
    {
        if (stuckObjectPrefab == null) return;

        // Troca o visual padrao pelo prefab. O objeto "Visual" e tudo que o prefab construiu como desenho.
        for (int i = stuckObject.transform.childCount - 1; i >= 0; i--)
            Destroy(stuckObject.transform.GetChild(i).gameObject);

        var inst = Instantiate(stuckObjectPrefab, stuckObject.transform);
        inst.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        inst.transform.localScale = Vector3.one;
        foreach (var c in inst.GetComponentsInChildren<Collider>()) Destroy(c);
        foreach (var m in inst.GetComponentsInChildren<MonoBehaviour>()) if (m != null) m.enabled = false;

        var rs = inst.GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            float largest = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (largest > 0.0001f) inst.transform.localScale = Vector3.one * (stuckObjectSize / largest);
            b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            inst.transform.position += stuckObject.transform.position - b.center;
        }
        stuckObject.RefreshRenderers();
    }

    // =================================================================
    // Regras: o que pode ser usado agora
    // =================================================================

    bool CanUse(CarouselPart p)
    {
        if (leverDone || busy) return false;
        switch (p.kind)
        {
            case CarouselPart.Kind.Cover: return true;
            case CarouselPart.Kind.StuckObject: return coverOff && !objectRemoved;
            case CarouselPart.Kind.Belt: return coverOff && belt != BeltState.Mounted;
            case CarouselPart.Kind.Tensor: return coverOff && belt == BeltState.Mounted && !tensorLocked;
            case CarouselPart.Kind.Lever: return Ready;
        }
        return false;
    }

    // =================================================================
    // Loop
    // =================================================================

    // O desenho da correia e o LED seguem o estado mesmo sem o jogador estar no minigame.
    void LateUpdate()
    {
        DrawBelt();
        if (motorRunning && spinners != null)
            foreach (var s in spinners) if (s != null) s.Rotate(0f, 0f, 120f * Time.deltaTime, Space.Self);
    }

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
            switch (drag)
            {
                case Drag.Cover: SetHover(cover); UpdateCover(ray, held); break;
                case Drag.Object: SetHover(stuckObject); UpdateObject(ray, held); break;
                case Drag.Belt: SetHover(beltGrip); UpdateBelt(ray, held); break;
                case Drag.Tensor: SetHover(tensor); UpdateTensor(viewCamera, mp, held); break;
                case Drag.Lever: SetHover(lever); UpdateLever(viewCamera, mp, held); break;
            }
            return;
        }

        var hit = Pick(ray);
        var usable = hit != null && CanUse(hit) ? hit : null;
        SetHover(usable);
        if (down && usable != null) BeginDrag(viewCamera, usable, ray, mp);
    }

    protected override void OnExitRequested()
    {
        SetHover(null);
        // Solta o que estiver na mao: o resto do progresso fica como esta.
        switch (drag)
        {
            case Drag.Cover: ReleaseCover(instant: true); break;
            case Drag.Object: ReleaseObject(instant: true); break;
            case Drag.Belt: ReleaseBelt(); break;
            case Drag.Tensor: EndTensor(); break;
            case Drag.Lever: ReleaseLever(); break;
        }
    }

    CarouselPart Pick(Ray ray)
    {
        var hits = Physics.RaycastAll(ray, 5f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            var p = h.collider.GetComponent<CarouselPart>();
            if (p != null && p.transform.IsChildOf(transform)) return p;
        }
        return null;
    }

    void SetHover(CarouselPart p)
    {
        if (hovered == p) return;
        if (hovered != null) hovered.SetHighlight(false, hoverColor);
        hovered = p;
        if (hovered != null) hovered.SetHighlight(true, hoverColor);
    }

    void BeginDrag(Camera cam, CarouselPart part, Ray ray, Vector2 mp)
    {
        switch (part.kind)
        {
            case CarouselPart.Kind.Cover:
                StopGlide(ref coverGlide);
                drag = Drag.Cover;
                if (coverOff) { GrabOffset(cover.transform, ray, CoverLiftZ); }
                else if (LocalPoint(ray, coverAnchor.localPosition.z, out var p)) dragStart = p;
                break;

            case CarouselPart.Kind.StuckObject:
                StopGlide(ref objectGlide);
                drag = Drag.Object;
                objectBroken = false;
                GrabOffset(stuckObject.transform, ray, objectSlot.localPosition.z);
                break;

            case CarouselPart.Kind.Belt:
                drag = Drag.Belt;
                // Caida: a correia vem pela ponta do motor, que fica no mouse. Pendurada: pega-se a ponta livre sem pular.
                if (belt == BeltState.Fallen) grabOffset = Vector3.zero;
                else GrabOffset((Vector3)beltB, ray, BeltPlaneZ);
                break;

            case CarouselPart.Kind.Tensor:
                drag = Drag.Tensor;
                lockTimer = 0f;
                if (!MouseAngle(cam, mp, tensor.transform.position, out prevMouseAngle)) prevMouseAngle = 0f;
                break;

            case CarouselPart.Kind.Lever:
                drag = Drag.Lever;
                leverAccum = leverProgress * leverAngle;
                if (!MouseAngle(cam, mp, lever.transform.position, out prevMouseAngle)) prevMouseAngle = 0f;
                break;
        }
    }

    // =================================================================
    // Passo 1 e 5: tampa (abrir e fechar)
    // =================================================================

    void UpdateCover(Ray ray, bool held)
    {
        if (!coverOff) PullCover(ray); else FollowMouse(cover.transform, ray, CoverLiftZ);
        if (!held) ReleaseCover(instant: false);
    }

    /// <summary>Encaixe por pressao: a tampa acompanha o mouse com resistencia ate o puxao vencer, ai ela solta.</summary>
    void PullCover(Ray ray)
    {
        if (!LocalPoint(ray, coverAnchor.localPosition.z, out var p)) return;
        Vector3 delta = p - dragStart;
        delta.z = 0f;
        if (delta.magnitude < coverOpenDistance)
        {
            cover.transform.localPosition = coverAnchor.localPosition + Vector3.ClampMagnitude(delta * 0.5f, 0.02f);
            return;
        }
        coverOff = true;
        GrabOffset(cover.transform, ray, CoverLiftZ);
        if (carouselAudio != null) carouselAudio.Click();
        RefreshLed();
    }

    void ReleaseCover(bool instant)
    {
        drag = Drag.None;
        float time = instant ? 0f : 0.15f;
        if (!coverOff)   // soltou antes de vencer a resistencia: volta para o lugar
        {
            Glide(ref coverGlide, cover.transform, coverAnchor.localPosition, coverAnchor.localRotation, time, null);
            return;
        }
        Vector3 d = cover.transform.localPosition - coverAnchor.localPosition;
        d.z = 0f;
        if (d.magnitude <= coverSnapDistance)   // passo 5: fecha o painel
        {
            coverOff = false;
            Glide(ref coverGlide, cover.transform, coverAnchor.localPosition, coverAnchor.localRotation, time, () =>
            {
                if (carouselAudio != null) carouselAudio.Click();
                RefreshLed();
            });
        }
        else Glide(ref coverGlide, cover.transform, coverRest.localPosition, coverRest.localRotation, time, null);
    }

    // =================================================================
    // Passo 2: tirar o objeto preso
    // =================================================================

    void UpdateObject(Ray ray, bool held)
    {
        if (LocalPoint(ray, objectSlot.localPosition.z, out var p))
        {
            Vector3 want = p + grabOffset;
            want.z = objectSlot.localPosition.z;
            Vector3 fromSlot = want - objectSlot.localPosition;
            fromSlot.z = 0f;
            Transform t = stuckObject.transform;

            if (!objectBroken)
            {
                // Preso: acompanha o mouse com muita resistencia ate o puxao vencer.
                t.localPosition = objectSlot.localPosition + Vector3.ClampMagnitude(fromSlot * 0.35f, objectBreakDistance * 0.8f);
                if (fromSlot.magnitude >= objectBreakDistance)
                {
                    objectBroken = true;
                    Creak();
                }
            }
            else
            {
                Vector3 clamped = ClampToPanel(want);
                clamped.z = Mathf.Lerp(t.localPosition.z, objectSlot.localPosition.z + CoverLiftZ * 0.5f, 1f - Mathf.Exp(-8f * Time.deltaTime));
                t.localPosition = Vector3.Lerp(t.localPosition, clamped, 1f - Mathf.Exp(-30f * Time.deltaTime));
            }
        }
        if (!held) ReleaseObject(instant: false);
    }

    void ReleaseObject(bool instant)
    {
        drag = Drag.None;
        float time = instant ? 0f : 0.2f;
        Vector3 d = stuckObject.transform.localPosition - objectSlot.localPosition;
        d.z = 0f;
        if (objectBroken && d.magnitude >= objectClearDistance)
        {
            objectRemoved = true;
            stuckObject.GetComponent<Collider>().enabled = false;
            Glide(ref objectGlide, stuckObject.transform, objectRest.localPosition, objectRest.localRotation, time, () =>
            {
                if (carouselAudio != null) carouselAudio.Click();
            });
        }
        else   // soltou perto das engrenagens: o objeto volta a entalar
            Glide(ref objectGlide, stuckObject.transform, objectSlot.localPosition, objectSlot.localRotation, time, null);
        objectBroken = false;
    }

    void Creak()
    {
        if (carouselAudio != null) carouselAudio.Creak();
        Circus.NoiseSystem.Emit(transform.position, creakNoiseRadius, Circus.NoiseSource.Machine);
        onCreak?.Invoke();
    }

    // =================================================================
    // Passo 3: recolocar a correia
    // =================================================================

    void UpdateBelt(Ray ray, bool held)
    {
        if (LocalPoint(ray, BeltPlaneZ, out var p))
        {
            Vector2 want = ClampToPanel(p + grabOffset);
            if (belt == BeltState.Fallen)
            {
                // Pega pela correia toda: uma ponta segue o mouse e a outra pende.
                beltTargetA = want;
                beltTargetB = want + new Vector2(0.02f, -beltHang);
                beltTargetRa = motorRadius; beltTargetRb = shaftRadius * 0.7f;
            }
            else
            {
                beltTargetB = want;   // ponta livre segue o mouse; a outra continua na polia do motor
            }
        }
        if (!held) ReleaseBelt();
    }

    void ReleaseBelt()
    {
        drag = Drag.None;
        if (belt == BeltState.Fallen)
        {
            if (Vector2.Distance(beltTargetA, MotorPos) <= beltSnapDistance)
            {
                belt = BeltState.OnMotor;
                if (carouselAudio != null) carouselAudio.Click();
            }
        }
        else if (belt == BeltState.OnMotor)
        {
            if (Vector2.Distance(beltTargetB, ShaftPos) <= beltSnapDistance)
            {
                belt = BeltState.Mounted;
                if (carouselAudio != null) carouselAudio.Click();
            }
        }
        SetBeltTargets(snap: false);
        RefreshLed();
    }

    void SetBeltTargets(bool snap)
    {
        switch (belt)
        {
            case BeltState.Fallen:
                beltTargetA = (Vector2)beltRest.localPosition - new Vector2(0.06f, 0f);
                beltTargetB = (Vector2)beltRest.localPosition + new Vector2(0.06f, 0f);
                beltTargetRa = beltTargetRb = 0.012f;
                break;
            case BeltState.OnMotor:
                beltTargetA = MotorPos;
                beltTargetB = HangPos;
                beltTargetRa = motorRadius; beltTargetRb = shaftRadius * 0.7f;
                break;
            case BeltState.Mounted:
                beltTargetA = MotorPos;
                beltTargetB = ShaftPos;
                beltTargetRa = motorRadius; beltTargetRb = shaftRadius;
                break;
        }
        if (snap || !beltInit)
        {
            beltA = beltTargetA; beltB = beltTargetB; beltRa = beltTargetRa; beltRb = beltTargetRb;
            beltInit = true;
        }
    }

    // =================================================================
    // Passo 4: esticar a correia
    // =================================================================

    void UpdateTensor(Camera cam, Vector2 mp, bool held)
    {
        if (held && MouseAngle(cam, mp, tensor.transform.position, out float a))
        {
            // Horario na tela estica. No Unity, giro positivo em torno de um eixo que se afasta de quem olha e anti-horario.
            tensorAngle = Mathf.Clamp(tensorAngle + Mathf.DeltaAngle(prevMouseAngle, a), 0f, tensorTargetAngle * 1.35f);
            prevMouseAngle = a;
            tensor.transform.localRotation = Quaternion.Euler(0f, 0f, -tensorAngle);
        }

        bool firm = Mathf.Abs(tensorAngle - tensorTargetAngle) <= tensorTolerance;
        lockTimer = firm ? lockTimer + Time.deltaTime : 0f;
        if (firm && (lockTimer >= tensorLockSeconds || !held)) { LockTensor(); return; }
        if (!held) EndTensor();
    }

    void EndTensor()
    {
        drag = Drag.None;
        if (Mathf.Abs(tensorAngle - tensorTargetAngle) <= tensorTolerance) LockTensor();
    }

    void LockTensor()
    {
        drag = Drag.None;
        tensorLocked = true;
        tensorAngle = tensorTargetAngle;
        tensor.transform.localRotation = Quaternion.Euler(0f, 0f, -tensorAngle);
        tensor.GetComponent<Collider>().enabled = false;
        SetHover(null);
        if (carouselAudio != null) carouselAudio.Click();
        RefreshLed();
    }

    // =================================================================
    // Passo 6: alavanca de partida
    // =================================================================

    void UpdateLever(Camera cam, Vector2 mp, bool held)
    {
        if (held && MouseAngle(cam, mp, lever.transform.position, out float a))
        {
            leverAccum = Mathf.Clamp(leverAccum + Mathf.DeltaAngle(prevMouseAngle, a), 0f, leverAngle);
            prevMouseAngle = a;
            leverProgress = leverAccum / leverAngle;
            ApplyLever();
        }
        if (!held) ReleaseLever();
    }

    void ReleaseLever()
    {
        drag = Drag.None;
        if (leverProgress >= 0.9f) StartCoroutine(LeverPulled());
        else StartCoroutine(LeverTo(0f, 0.2f));
    }

    void ApplyLever() => lever.transform.localRotation = Quaternion.Euler(0f, 0f, -leverAngle * leverProgress);   // horario na tela

    IEnumerator LeverTo(float target, float duration)
    {
        float from = leverProgress;
        for (float k = 0f; k < 1f; k += Time.deltaTime / duration)
        {
            leverProgress = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, k));
            ApplyLever();
            yield return null;
        }
        leverProgress = target;
        ApplyLever();
    }

    IEnumerator LeverPulled()
    {
        busy = true;
        SetHover(null);
        yield return LeverTo(1f, 0.1f);
        if (carouselAudio != null) carouselAudio.Click();

        if (objectRemoved)
        {
            // Mecanismo livre: o motor pega e a correia gira.
            leverDone = true;
            motorRunning = true;
            lever.GetComponent<Collider>().enabled = false;
            if (carouselAudio != null) carouselAudio.SetMotor(true);
            busy = false;
            RefreshLed();
            Complete();
            yield break;
        }

        // Objeto ainda preso: o motor forca, a tampa chacoalha e a correia salta.
        float length = carouselAudio != null ? carouselAudio.Strain() : 1.2f;
        for (float t = 0f; t < length; t += Time.deltaTime)
        {
            float shake = Mathf.Lerp(0.001f, 0.004f, t / length);
            cover.transform.localPosition = coverAnchor.localPosition + new Vector3(Random.Range(-shake, shake), Random.Range(-shake, shake), 0f);
            lever.transform.localRotation = Quaternion.Euler(0f, 0f, -leverAngle * leverProgress + Random.Range(-2f, 2f));
            yield return null;
        }
        cover.transform.localPosition = coverAnchor.localPosition;
        if (carouselAudio != null) carouselAudio.Snap();

        belt = BeltState.Fallen;
        SetBeltTargets(snap: true);
        tensorAngle = 0f;
        tensorLocked = false;
        tensor.transform.localRotation = Quaternion.identity;
        tensor.GetComponent<Collider>().enabled = true;
        RefreshLed();

        yield return new WaitForSeconds(0.25f);
        yield return LeverTo(0f, 0.35f);
        busy = false;
    }

    // =================================================================
    // Correia (desenho)
    // =================================================================

    void DrawBelt()
    {
        float k = 1f - Mathf.Exp(-16f * Time.deltaTime);
        beltA = Vector2.Lerp(beltA, beltTargetA, k);
        beltB = Vector2.Lerp(beltB, beltTargetB, k);
        beltRa = Mathf.Lerp(beltRa, beltTargetRa, k);
        beltRb = Mathf.Lerp(beltRb, beltTargetRb, k);

        float slack = belt == BeltState.Mounted ? 1f - Mathf.Clamp01(tensorAngle / tensorTargetAngle) : 0f;
        float over = belt == BeltState.Mounted ? Mathf.Max(0f, tensorAngle / tensorTargetAngle - 1f) : 0f;
        float tremor = over * 0.012f * Mathf.Sin(Time.time * 70f);
        BuildStadium(beltA, beltRa, beltB, beltRb, slack * maxBeltSag, tremor);

        // O ponto de pegar a correia acompanha o desenho.
        bool grabbable = belt != BeltState.Mounted;
        if (beltGrip != null)
        {
            beltGripCollider.enabled = grabbable;
            Vector2 at = belt == BeltState.Fallen ? (beltA + beltB) * 0.5f : beltB;
            if (drag != Drag.Belt) beltGrip.transform.localPosition = new Vector3(at.x, at.y, BeltPlaneZ);
        }
    }

    const int BeltRun = 10, BeltArc = 12;
    readonly Vector3[] beltPoints = new Vector3[(BeltRun + 1) * 2 + (BeltArc - 1) * 2];
    int beltCount;

    /// <summary>Contorno de uma correia ao redor de dois circulos (tangentes externas), com a parte de baixo caindo por causa da folga.</summary>
    void BuildStadium(Vector2 a, float ra, Vector2 b, float rb, float sag, float tremor)
    {
        const int run = BeltRun, arc = BeltArc;
        beltCount = 0;
        Vector2 d = b - a;
        float len = d.magnitude;
        if (len < 0.001f) { d = Vector2.right; len = 0.001f; }
        Vector2 dir = d / len;
        float sinPhi = Mathf.Clamp((ra - rb) / len, -0.99f, 0.99f);
        float alpha = Mathf.Acos(sinPhi);   // = 90 graus - phi
        float theta = Mathf.Atan2(dir.y, dir.x);

        // Corrida de cima, de a para b.
        Vector2 uTop = new Vector2(Mathf.Cos(theta + alpha), Mathf.Sin(theta + alpha));
        Vector2 uBot = new Vector2(Mathf.Cos(theta - alpha), Mathf.Sin(theta - alpha));
        Vector2 down = Vector2.down;
        for (int i = 0; i <= run; i++)
        {
            float s = i / (float)run;
            Vector2 pt = Vector2.Lerp(a + ra * uTop, b + rb * uTop, s) + down * (sag * 0.25f * Mathf.Sin(s * Mathf.PI)) + Vector2.up * (tremor * Mathf.Sin(s * Mathf.PI));
            beltPoints[beltCount++] = new Vector3(pt.x, pt.y, BeltPlaneZ);
        }
        // Arco em volta de b, de cima para baixo pelo lado de fora.
        for (int i = 1; i < arc; i++)
        {
            float ang = Mathf.Lerp(theta + alpha, theta - alpha, i / (float)arc);
            Vector2 pt = b + rb * new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            beltPoints[beltCount++] = new Vector3(pt.x, pt.y, BeltPlaneZ);
        }
        // Corrida de baixo, de b para a, com a folga.
        for (int i = 0; i <= run; i++)
        {
            float s = i / (float)run;
            Vector2 pt = Vector2.Lerp(b + rb * uBot, a + ra * uBot, s) + down * (sag * Mathf.Sin(s * Mathf.PI)) + Vector2.down * (tremor * Mathf.Sin(s * Mathf.PI));
            beltPoints[beltCount++] = new Vector3(pt.x, pt.y, BeltPlaneZ);
        }
        // Arco em volta de a, passando pelo lado oposto a b.
        for (int i = 1; i < arc; i++)
        {
            float ang = Mathf.Lerp(theta - alpha, theta + alpha - 2f * Mathf.PI, i / (float)arc);
            Vector2 pt = a + ra * new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            beltPoints[beltCount++] = new Vector3(pt.x, pt.y, BeltPlaneZ);
        }

        beltLine.positionCount = beltCount;
        beltLine.SetPositions(beltPoints);
    }

    // =================================================================
    // LED
    // =================================================================

    void RefreshLed()
    {
        if (statusLed == null) return;
        Material m = motorRunning ? ledRunningMaterial : Ready ? ledReadyMaterial : ledBrokenMaterial;
        if (m != null && statusLed.sharedMaterial != m) statusLed.sharedMaterial = m;
    }

    // =================================================================
    // Utilitarios
    // =================================================================

    /// <summary>Ponto do raio no plano z (local) do painel, em coordenadas locais.</summary>
    bool LocalPoint(Ray ray, float localZ, out Vector3 local)
    {
        var plane = new Plane(transform.forward, transform.TransformPoint(0f, 0f, localZ));
        if (plane.Raycast(ray, out float d)) { local = transform.InverseTransformPoint(ray.GetPoint(d)); return true; }
        local = default;
        return false;
    }

    void GrabOffset(Transform t, Ray ray, float planeZ)
        => grabOffset = LocalPoint(ray, planeZ, out var p) ? t.localPosition - p : Vector3.zero;

    void GrabOffset(Vector3 localPoint, Ray ray, float planeZ)
        => grabOffset = LocalPoint(ray, planeZ, out var p) ? localPoint - p : Vector3.zero;

    void FollowMouse(Transform t, Ray ray, float planeZ)
    {
        if (!LocalPoint(ray, planeZ, out var p)) return;
        Vector3 target = p + grabOffset;
        target.z = planeZ;
        // A tampa pode passear mais longe que o miolo do painel.
        target.x = Mathf.Clamp(target.x, -0.6f, 0.6f);
        target.y = Mathf.Clamp(target.y, -0.5f, 0.4f);
        t.localPosition = Vector3.Lerp(t.localPosition, target, 1f - Mathf.Exp(-30f * Time.deltaTime));
    }

    static Vector3 ClampToPanel(Vector3 p)
    {
        p.x = Mathf.Clamp(p.x, -PanelHalfX, PanelHalfX);
        p.y = Mathf.Clamp(p.y, PanelMinY, PanelMaxY);
        return p;
    }

    static Vector2 ClampToPanel(Vector2 p) => ClampToPanel((Vector3)p);

    /// <summary>Angulo do mouse em volta de um ponto 3D na tela, em graus, no sentido horario a partir de cima.</summary>
    static bool MouseAngle(Camera cam, Vector2 mp, Vector3 worldCenter, out float angle)
    {
        Vector2 center = cam.WorldToScreenPoint(worldCenter);
        Vector2 v = mp - center;
        angle = Mathf.Atan2(v.x, v.y) * Mathf.Rad2Deg;
        return v.sqrMagnitude > 100f;   // perto demais do centro o angulo fica instavel
    }

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
            t.SetLocalPositionAndRotation(localPos, localRot);
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
            t.SetLocalPositionAndRotation(Vector3.Lerp(p0, localPos, s), Quaternion.Slerp(r0, localRot, s));
            yield return null;
        }
        t.SetLocalPositionAndRotation(localPos, localRot);
        done?.Invoke();
    }
}
