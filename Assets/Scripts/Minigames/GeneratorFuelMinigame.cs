using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Abastecer e ligar o gerador, diegetico. So abre se o jogador estiver carregando o galao. Passos:
///  1. Abrir o tanque: segurar a tampa e girar o mouse em circulo (anti-horario) ate ela soltar.
///  2. Colocar o funil: arrastar o funil ate a boca do tanque. Encaixa ao soltar perto dela.
///  3. Despejar: segurar o botao e arrastar para baixo para inclinar o galao. A inclinacao define a vazao; passou do
///     limite o funil transborda (respingo, som no chao e barulho). O medidor do gerador mostra o nivel; cheio, o despejo para.
///  4. Fechar o tanque: girar a tampa de volta (sentido horario).
///  5. Dar partida: puxar a corda para tras, rapido. Engasga na 1a e na 2a puxadas, liga na 3a (configuravel).
/// Sem HUD: tudo aparece no gerador. O progresso fica nos campos, entao sair no meio nao reseta nada. Nada pausa o jogo.
/// </summary>
public class GeneratorFuelMinigame : Minigame
{
    enum Drag { None, Cap, Funnel, Pour, Cord }

    [Header("Pecas")]
    [Tooltip("Boca do tanque. +Y aponta para cima.")]
    [SerializeField] Transform tankMouth;
    [SerializeField] GeneratorPart cap;
    [Tooltip("Onde a tampa fica quando solta.")]
    [SerializeField] Transform capRest;
    [SerializeField] GeneratorPart funnel;
    [Tooltip("Onde o funil descansa.")]
    [SerializeField] Transform funnelHome;
    [Tooltip("Onde o funil fica encaixado na boca do tanque.")]
    [SerializeField] Transform funnelSeat;
    [SerializeField] Color hoverColor = new Color(0.45f, 0.4f, 0.15f);

    [Header("Galao no minigame")]
    [Tooltip("O galao inclinando. Aparece so durante o minigame (ligue-o em 'Ligados so durante o minigame').")]
    [SerializeField] Transform pourCan;
    [Tooltip("Pose do galao em pe, na mao do jogador.")]
    [SerializeField] Transform canHoldPose;
    [Tooltip("Pose do galao com o bico sobre o funil.")]
    [SerializeField] Transform canPourPose;
    [Tooltip("Fio de gasolina. Estica do bico ate o funil.")]
    [SerializeField] Transform stream;
    [Tooltip("Aonde o fio de gasolina chega.")]
    [SerializeField] Transform streamTarget;
    [Tooltip("O galao do mundo (o carregado). Ao ligar o gerador ele fica vazio neste ponto.")]
    [SerializeField] GasCan gasCan;
    [SerializeField] Transform canSpentPoint;

    [Header("Medidor")]
    [SerializeField] Transform gaugeNeedle;
    [Tooltip("Angulo do ponteiro (graus, em torno do eixo Z local) com o tanque vazio e cheio.")]
    [SerializeField] float gaugeEmptyAngle = 70f;
    [SerializeField] float gaugeFullAngle = -70f;

    [Header("Corda")]
    [SerializeField] GeneratorPart cordHandle;
    [SerializeField] Transform cordRest, cordEnd, cordAnchor;
    [Tooltip("Fio da corda (esticado do ancoradouro ate o puxador).")]
    [SerializeField] Transform cordLine;
    [Tooltip("Corpo do gerador. Treme quando o motor engasga ou funciona.")]
    [SerializeField] Transform engineBody;

    [Header("Efeitos")]
    [SerializeField] ParticleSystem splash;

    [Header("Abrir e fechar a tampa")]
    [SerializeField] float capTurns = 1.5f;
    [Tooltip("Quanto a tampa sobe ao desenroscar (m).")]
    [SerializeField] float capLift = 0.04f;

    [Header("Funil")]
    [Tooltip("Distancia (m) da boca do tanque para o funil encaixar ao ser solto.")]
    [SerializeField] float snapDistance = 0.14f;
    [SerializeField] float funnelDragHeight = 0.12f;

    [Header("Despejo")]
    [Tooltip("Quanto o tanque enche por segundo com o despejo no limite (1 = tanque cheio).")]
    [SerializeField] float fillPerSecond = 0.3f;
    [Tooltip("Vazao (0 a 1) acima da qual o funil transborda. A vazao que passa disso e perdida.")]
    [SerializeField, Range(0.1f, 1f)] float overflowLimit = 0.65f;
    [Tooltip("Inclinacao (0 a 1) a partir da qual comeca a sair gasolina.")]
    [SerializeField, Range(0f, 0.9f)] float pourStartTilt = 0.3f;
    [Tooltip("Fracao da altura da tela que o mouse precisa descer para inclinar o galao ao maximo.")]
    [SerializeField] float tiltDragScreenFraction = 0.3f;
    [SerializeField] float tiltSpeed = 3f;
    [Tooltip("Inclinacao maxima do galao (graus).")]
    [SerializeField] float maxTiltDegrees = 105f;
    [Tooltip("+1 ou -1: sentido em que o galao inclina.")]
    [SerializeField] float tiltSign = -1f;

    [Header("Partida")]
    [SerializeField] int pullsToStart = 3;
    [Tooltip("Tempo maximo (s) para a puxada completa. Mais lento que isso nao conta.")]
    [SerializeField] float pullMaxSeconds = 0.6f;
    [SerializeField] float shakeAmount = 0.01f;

    [Header("Barulho (NoiseSystem)")]
    [Tooltip("Raio do barulho do transbordo, repetido a cada intervalo enquanto transborda.")]
    [SerializeField] float spillNoiseRadius = 14f;
    [SerializeField] float spillNoiseInterval = 0.7f;
    [Tooltip("Raio do barulho de cada puxada da corda.")]
    [SerializeField] float pullNoiseRadius = 20f;

    [Header("Audio (vazio = som gerado em codigo)")]
    [SerializeField] AudioClip pourClip;
    [SerializeField] AudioClip spillClip;
    [SerializeField] AudioClip clickClip;
    [SerializeField] AudioClip cordClip;
    [Tooltip("Um por puxada que falha (1a, 2a...). Se faltar, usa o ultimo.")]
    [SerializeField] AudioClip[] sputterClips;
    [SerializeField] AudioClip runClip;
    [SerializeField, Range(0f, 1f)] float volume = 0.8f;
    [SerializeField] float audioMaxDistance = 30f;

    [Header("Foco")]
    [Tooltip("Sem foco na janela do jogo o botao do mouse conta como solto (nao fica despejando sozinho). O jogo nao pausa.")]
    [SerializeField] bool releaseOnFocusLoss = true;

    [Header("Textos")]
    [SerializeField] string missingCanText = "Preciso de combustível.";

    // --- estado (persiste ao sair) ---
    float capProgress;               // 0 = fechada no tanque, 1 = solta e de lado
    bool funnelSeated, full, sealedTank;
    float fuel;                      // 0 a 1
    int pulls;
    bool running;

    // --- temporarios ---
    Drag drag;
    GeneratorPart hovered;
    GasCanCarrier carrier;
    float prevAngle, pourStartY, tilt, tiltTarget, needleAngle, spillTimer;
    bool busy;
    Vector3 funnelHomePos, engineHome, cordHandleRestLocal;
    Quaternion funnelHomeRot, needleHome;
    Vector2 cordPress, cordAxis; float cordAxisLength, cordProgress, cordStartTime;
    AudioSource sfx, pourSrc, spillSrc, runSrc;
    Coroutine funnelRoutine, cordRoutine;

    public float Fuel => fuel;
    public bool TankOpen => capProgress >= 1f;
    public bool FunnelSeated => funnelSeated;
    public bool TankSealed => sealedTank;
    public int Pulls => pulls;
    public bool EngineRunning => running;

    bool CanPour => funnelSeated && !full;
    bool FunnelAway => !funnelSeated;

    protected override void Awake()
    {
        base.Awake();
        funnelHomePos = funnel.transform.position;
        funnelHomeRot = funnel.transform.rotation;
        if (engineBody != null) engineHome = engineBody.localPosition;
        if (gaugeNeedle != null) needleHome = gaugeNeedle.localRotation;
        needleAngle = gaugeEmptyAngle;

        sfx = NewSource(false);
        pourSrc = NewSource(true); pourSrc.clip = pourClip != null ? pourClip : FuelAudio.PourLoop();
        spillSrc = NewSource(true); spillSrc.clip = spillClip != null ? spillClip : FuelAudio.SpillLoop();
        runSrc = NewSource(true); runSrc.clip = runClip != null ? runClip : FuelAudio.EngineLoop();
        onExited.AddListener(OnLeft);
        if (splash != null) splash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ApplyAll();
    }

    AudioSource NewSource(bool loop)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 1f;
        s.maxDistance = audioMaxDistance;
        return s;
    }

    // ---------- Entrada ----------

    protected override string MissingRequirementText(PlayerInteractor interactor)
    {
        if (IsCompleted) return null;
        var c = interactor.GetComponent<GasCanCarrier>();
        return c != null && c.IsCarrying ? null : missingCanText;
    }

    protected override void OnBegin(PlayerInteractor interactor)
    {
        carrier = interactor.GetComponent<GasCanCarrier>();
        if (carrier != null) carrier.SetHeldVisible(false);   // o galao do minigame toma o lugar do da mao
        tilt = tiltTarget = 0f;
        ApplyAll();
    }

    void OnLeft()
    {
        if (carrier != null && carrier.IsCarrying) carrier.SetHeldVisible(true);
    }

    protected override void OnExitRequested()
    {
        SetHover(null);
        if (drag == Drag.Funnel) ReleaseFunnel();
        else if (drag == Drag.Cord) ReleaseCord();
        drag = Drag.None;
        tiltTarget = 0f;
        StopPourEffects();
    }

    // ---------- Regras: o que pode ser usado agora ----------

    bool CanUse(GeneratorPart p)
    {
        if (busy) return false;
        switch (p.kind)
        {
            case GeneratorPart.Kind.Cap:
                return (!full && !funnelSeated && capProgress < 1f) || (full && FunnelAway && !sealedTank);
            case GeneratorPart.Kind.Funnel: return capProgress >= 1f && !funnelSeated && !full;
            case GeneratorPart.Kind.Cord: return sealedTank && !running;
        }
        return false;
    }

    // ---------- Loop ----------

    protected override void Tick(Camera viewCamera)
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 pos = mouse.position.ReadValue();
        bool down = mouse.leftButton.wasPressedThisFrame;
        bool held = mouse.leftButton.isPressed && (Application.isFocused || !releaseOnFocusLoss);   // sem foco, o botao conta como solto
        Ray ray = viewCamera.ScreenPointToRay(pos);

        UpdatePour(Time.deltaTime);

        if (drag != Drag.None)
        {
            ContinueDrag(viewCamera, ray, pos, held);
            return;
        }

        var hit = Pick(ray);
        var usable = hit != null && CanUse(hit) ? hit : null;
        SetHover(usable);
        if (!down) return;

        if (usable != null) BeginPart(usable, viewCamera, ray, pos);
        else if (CanPour && !busy)
        {
            drag = Drag.Pour;
            pourStartY = pos.y;
        }
    }

    GeneratorPart Pick(Ray ray)
    {
        var hits = Physics.RaycastAll(ray, 6f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            var p = h.collider.GetComponentInParent<GeneratorPart>();
            if (p != null) return p;
        }
        return null;
    }

    void SetHover(GeneratorPart p)
    {
        if (hovered == p) return;
        if (hovered != null) hovered.SetHighlight(false, hoverColor);
        hovered = p;
        if (hovered != null) hovered.SetHighlight(true, hoverColor);
    }

    void BeginPart(GeneratorPart p, Camera cam, Ray ray, Vector2 pos)
    {
        SetHover(null);
        switch (p.kind)
        {
            case GeneratorPart.Kind.Cap:
                drag = Drag.Cap;
                prevAngle = MouseAngle(cam, cap.transform.position, pos);
                break;
            case GeneratorPart.Kind.Funnel:
                drag = Drag.Funnel;
                if (funnelRoutine != null) StopCoroutine(funnelRoutine);
                break;
            case GeneratorPart.Kind.Cord:
                drag = Drag.Cord;
                if (cordRoutine != null) StopCoroutine(cordRoutine);
                Vector3 a = cam.WorldToScreenPoint(cordRest.position), b = cam.WorldToScreenPoint(cordEnd.position);
                cordAxis = (Vector2)(b - a);
                cordAxisLength = Mathf.Max(1f, cordAxis.magnitude);
                cordAxis /= cordAxisLength;
                cordPress = pos;
                cordProgress = 0f;
                cordStartTime = -1f;
                break;
        }
    }

    void ContinueDrag(Camera cam, Ray ray, Vector2 pos, bool held)
    {
        switch (drag)
        {
            case Drag.Cap: DragCap(cam, pos, held); break;
            case Drag.Funnel: DragFunnel(ray, held); break;
            case Drag.Pour:
                tiltTarget = held ? Mathf.Clamp01((pourStartY - pos.y) / Mathf.Max(1f, Screen.height * tiltDragScreenFraction)) : 0f;
                if (!held) drag = Drag.None;
                break;
            case Drag.Cord: DragCord(pos, held); break;
        }
    }

    // ---------- Tampa ----------

    static float MouseAngle(Camera cam, Vector3 world, Vector2 mouse)
    {
        Vector2 c = cam.WorldToScreenPoint(world);
        return Mathf.Atan2(mouse.y - c.y, mouse.x - c.x) * Mathf.Rad2Deg;
    }

    void DragCap(Camera cam, Vector2 pos, bool held)
    {
        if (!held) { drag = Drag.None; return; }
        Vector2 c = cam.WorldToScreenPoint(cap.transform.position);
        if ((pos - c).sqrMagnitude < 64f) return;   // perto demais do centro: o angulo seria instavel

        float a = Mathf.Atan2(pos.y - c.y, pos.x - c.x) * Mathf.Rad2Deg;
        float delta = Mathf.DeltaAngle(prevAngle, a);   // + = anti-horario na tela
        prevAngle = a;
        capProgress = Mathf.Clamp01(capProgress + delta / (capTurns * 360f));
        ApplyCap();

        if (!full && capProgress >= 1f)
        {
            capProgress = 1f;
            Click();
            drag = Drag.None;
        }
        else if (full && capProgress <= 0f)
        {
            capProgress = 0f;
            sealedTank = true;
            Click();
            drag = Drag.None;
        }
    }

    void ApplyCap()
    {
        float p = capProgress;
        Vector3 lifted = tankMouth.position + tankMouth.up * (capLift * Mathf.Min(1f, p / 0.6f));
        float slide = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((p - 0.6f) / 0.4f));
        cap.transform.position = Vector3.Lerp(lifted, capRest.position, slide);
        cap.transform.rotation = tankMouth.rotation * Quaternion.Euler(0f, -p * capTurns * 360f, 0f);
    }

    // ---------- Funil ----------

    void DragFunnel(Ray ray, bool held)
    {
        if (held)
        {
            var plane = new Plane(Vector3.up, funnelSeat.position + Vector3.up * funnelDragHeight);
            if (plane.Raycast(ray, out float d))
            {
                Vector3 p = ray.GetPoint(d);
                Vector3 off = p - funnelSeat.position;
                off.y = 0f;
                if (off.magnitude > 1.5f) p = funnelSeat.position + Vector3.up * funnelDragHeight + off.normalized * 1.5f;
                funnel.transform.position = Vector3.Lerp(funnel.transform.position, p, 25f * Time.deltaTime);
            }
            funnel.transform.rotation = Quaternion.Slerp(funnel.transform.rotation, funnelSeat.rotation, 12f * Time.deltaTime);
            return;
        }
        drag = Drag.None;
        ReleaseFunnel();
    }

    void ReleaseFunnel()
    {
        Vector3 off = funnel.transform.position - funnelSeat.position;
        off.y = 0f;
        if (off.magnitude <= snapDistance)
        {
            funnelSeated = true;
            Click();
            if (funnelRoutine != null) StopCoroutine(funnelRoutine);
            funnelRoutine = StartCoroutine(MoveTo(funnel.transform, funnelSeat.position, funnelSeat.rotation, 0.12f, null));
        }
        else
        {
            if (funnelRoutine != null) StopCoroutine(funnelRoutine);
            funnelRoutine = StartCoroutine(MoveTo(funnel.transform, funnelHomePos, funnelHomeRot, 0.25f, null));
        }
    }

    // ---------- Despejo ----------

    void UpdatePour(float dt)
    {
        if (drag != Drag.Pour) tiltTarget = 0f;
        tilt = Mathf.MoveTowards(tilt, tiltTarget, tiltSpeed * dt);
        ApplyCan();

        float flow = CanPour ? Mathf.InverseLerp(pourStartTilt, 1f, tilt) : 0f;
        bool overflow = flow > overflowLimit;

        if (flow > 0f)
        {
            fuel = Mathf.Min(1f, fuel + fillPerSecond * Mathf.Min(flow, overflowLimit) * dt);
            if (fuel >= 1f) FinishFilling();
        }

        ApplyStream(flow);
        SetLoop(pourSrc, flow > 0f, Mathf.Lerp(0.3f, 1f, flow) * volume);
        SetLoop(spillSrc, overflow, volume);
        if (splash != null)
        {
            if (overflow && !splash.isEmitting) splash.Play();
            else if (!overflow && splash.isEmitting) splash.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (overflow)
        {
            spillTimer -= dt;
            if (spillTimer <= 0f)
            {
                spillTimer = spillNoiseInterval;
                Circus.NoiseSystem.Emit(funnelSeat.position, spillNoiseRadius, Circus.NoiseSource.Machine);
            }
        }
        else spillTimer = 0f;

        UpdateGauge(dt);
    }

    void FinishFilling()
    {
        full = true;
        fuel = 1f;
        tiltTarget = 0f;
        if (drag == Drag.Pour) drag = Drag.None;
        Click();
        // O funil sai da boca para o tanque poder ser fechado.
        if (funnelRoutine != null) StopCoroutine(funnelRoutine);
        funnelRoutine = StartCoroutine(RemoveFunnelRoutine());
    }

    IEnumerator RemoveFunnelRoutine()
    {
        yield return new WaitForSeconds(0.8f);
        funnelSeated = false;
        yield return MoveTo(funnel.transform, funnelHomePos, funnelHomeRot, 0.4f, null);
    }

    void ApplyCan()
    {
        if (pourCan == null) return;
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tilt / 0.3f));
        pourCan.position = Vector3.Lerp(canHoldPose.position, canPourPose.position, blend);
        pourCan.rotation = Quaternion.Slerp(canHoldPose.rotation, canPourPose.rotation, blend)
                           * Quaternion.Euler(tiltSign * tilt * maxTiltDegrees, 0f, 0f);
    }

    void ApplyStream(float flow)
    {
        if (stream == null) return;
        bool on = flow > 0f && streamTarget != null;
        if (stream.gameObject.activeSelf != on) stream.gameObject.SetActive(on);
        if (!on) return;
        Vector3 a = pourCan.position, b = streamTarget.position;
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.001f) return;
        float w = Mathf.Lerp(0.008f, 0.03f, flow);
        stream.position = (a + b) * 0.5f;
        stream.rotation = Quaternion.LookRotation(dir / len);
        stream.localScale = new Vector3(w, w, len);
    }

    void UpdateGauge(float dt)
    {
        if (gaugeNeedle == null) return;
        float target = Mathf.Lerp(gaugeEmptyAngle, gaugeFullAngle, fuel);
        needleAngle = Mathf.MoveTowards(needleAngle, target, 120f * dt);
        gaugeNeedle.localRotation = needleHome * Quaternion.AngleAxis(needleAngle, Vector3.forward);
    }

    void SetLoop(AudioSource s, bool on, float vol)
    {
        if (on)
        {
            s.volume = vol;
            if (!s.isPlaying) s.Play();
        }
        else if (s.isPlaying) s.Stop();
    }

    void StopPourEffects()
    {
        pourSrc.Stop();
        spillSrc.Stop();
        if (splash != null) splash.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (stream != null) stream.gameObject.SetActive(false);
    }

    // ---------- Corda ----------

    void DragCord(Vector2 pos, bool held)
    {
        if (!held) { drag = Drag.None; ReleaseCord(); return; }

        cordProgress = Mathf.Clamp01(Vector2.Dot(pos - cordPress, cordAxis) / cordAxisLength);
        if (cordStartTime < 0f && cordProgress > 0.08f) cordStartTime = Time.time;
        ApplyCord(cordProgress);
        if (cordProgress < 1f) return;

        drag = Drag.None;
        bool fastEnough = cordStartTime >= 0f && Time.time - cordStartTime <= pullMaxSeconds;
        if (fastEnough) StartCoroutine(PullRoutine());
        else ReleaseCord();   // devagar demais: a corda volta e nao conta
    }

    void ApplyCord(float p)
    {
        if (cordHandle == null) return;
        cordHandle.transform.position = Vector3.Lerp(cordRest.position, cordEnd.position, p);
        if (cordLine == null || cordAnchor == null) return;
        Vector3 a = cordAnchor.position, b = cordHandle.transform.position;
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.001f) return;
        cordLine.position = (a + b) * 0.5f;
        cordLine.rotation = Quaternion.LookRotation(dir / len);
        cordLine.localScale = new Vector3(0.012f, 0.012f, len);
    }

    void ReleaseCord()
    {
        if (cordRoutine != null) StopCoroutine(cordRoutine);
        cordRoutine = StartCoroutine(RetractCord(cordProgress, 0.2f));
    }

    IEnumerator RetractCord(float from, float seconds)
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
        {
            cordProgress = Mathf.Lerp(from, 0f, Mathf.SmoothStep(0f, 1f, t));
            ApplyCord(cordProgress);
            yield return null;
        }
        cordProgress = 0f;
        ApplyCord(0f);
    }

    IEnumerator PullRoutine()
    {
        busy = true;
        pulls++;
        Circus.NoiseSystem.Emit(transform.position, pullNoiseRadius, Circus.NoiseSource.Machine);
        sfx.PlayOneShot(cordClip != null ? cordClip : FuelAudio.CordPull(), volume);
        yield return RetractCord(1f, 0.15f);

        if (pulls < pullsToStart)
        {
            // 1a puxada: engasga curto e morre. 2a em diante: engasga mais longo, quase pega.
            AudioClip clip = SputterClip(pulls);
            sfx.PlayOneShot(clip, volume);
            yield return Shake(clip.length * 0.9f, shakeAmount * (pulls == 1 ? 1f : 1.6f));
            busy = false;
            yield break;
        }

        StartEngine();
        yield return Shake(0.8f, shakeAmount * 1.2f);
        FinishCan();
        busy = false;
        Complete();
    }

    AudioClip SputterClip(int pull)
    {
        if (sputterClips != null && sputterClips.Length > 0) return sputterClips[Mathf.Min(pull - 1, sputterClips.Length - 1)];
        return FuelAudio.Sputter(longer: pull > 1);
    }

    IEnumerator Shake(float seconds, float amount)
    {
        if (engineBody == null) { yield return new WaitForSeconds(seconds); yield break; }
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float k = 1f - t / seconds;
            engineBody.localPosition = engineHome + Random.insideUnitSphere * (amount * (0.4f + 0.6f * k));
            yield return null;
        }
        engineBody.localPosition = engineHome;
    }

    void StartEngine()
    {
        running = true;
        if (!runSrc.isPlaying) { runSrc.volume = volume; runSrc.Play(); }
    }

    /// <summary>O galao fica vazio no chao, perto do gerador, e o jogador fica de maos livres.</summary>
    void FinishCan()
    {
        var can = gasCan != null ? gasCan : carrier != null ? carrier.Held : null;
        if (can == null) return;
        if (can.IsHeld && carrier != null) carrier.TakeFromHands();
        if (canSpentPoint != null) can.Spend(canSpentPoint.position, canSpentPoint.rotation);
        else can.Spend(can.transform.position, can.transform.rotation);
        if (pourCan != null) pourCan.gameObject.SetActive(false);
    }

    // Funcionando, o gerador vibra de leve mesmo fora do minigame.
    void LateUpdate()
    {
        if (!running || busy || engineBody == null) return;
        engineBody.localPosition = engineHome + Random.insideUnitSphere * (shakeAmount * 0.15f);
    }

    // ---------- Estado e visual ----------

    void ApplyAll()
    {
        ApplyCap();
        if (funnelSeated) funnel.transform.SetPositionAndRotation(funnelSeat.position, funnelSeat.rotation);
        ApplyCan();
        ApplyCord(0f);
        needleAngle = Mathf.Lerp(gaugeEmptyAngle, gaugeFullAngle, fuel);
        if (gaugeNeedle != null) gaugeNeedle.localRotation = needleHome * Quaternion.AngleAxis(needleAngle, Vector3.forward);
        if (stream != null) stream.gameObject.SetActive(false);
    }

    public override void ForceComplete()
    {
        base.ForceComplete();
        capProgress = 0f;
        fuel = 1f;
        full = sealedTank = true;
        funnelSeated = false;
        pulls = pullsToStart;
        if (carrier == null) carrier = FindFirstObjectByType<GasCanCarrier>();
        FinishCan();
        ApplyAll();
        funnel.transform.SetPositionAndRotation(funnelHomePos, funnelHomeRot);
        StartEngine();
    }

    void Click() => sfx.PlayOneShot(clickClip != null ? clickClip : FuelAudio.Click(), volume);

    static IEnumerator MoveTo(Transform t, Vector3 pos, Quaternion rot, float seconds, System.Action done)
    {
        Vector3 p0 = t.position;
        Quaternion r0 = t.rotation;
        for (float k = 0f; k < 1f; k += Time.deltaTime / seconds)
        {
            float s = Mathf.SmoothStep(0f, 1f, k);
            t.SetPositionAndRotation(Vector3.Lerp(p0, pos, s), Quaternion.Slerp(r0, rot, s));
            yield return null;
        }
        t.SetPositionAndRotation(pos, rot);
        done?.Invoke();
    }
}
