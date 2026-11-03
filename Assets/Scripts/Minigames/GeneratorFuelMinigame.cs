using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Abastecer e ligar o gerador, diegetico. Tudo fica na face da frente do gerador: bocal lateral (como num carro),
/// tampa, medidor e corda de partida. So abre com o galao na mao enquanto o tanque nao esta cheio. Passos:
///  1. Abrir o bocal: segurar a tampa e girar o mouse em circulo (anti-horario) ate ela sair.
///  2. Despejar: segurar o clique no galao. Quanto mais tempo segurando, mais ele inclina e mais rapido sai. Soltar
///     volta o galao. O bocal engole numa vazao fixa: despejando mais rapido que isso, a espuma sobe na boca e, quando
///     chega na borda, transborda (respingo, som e barulho no NoiseSystem). O medidor mostra o nivel.
///  3. Fechar o bocal: girar a tampa de volta (sentido horario).
///  4. Dar partida: puxar a corda ate o fim. Engasga na 1a e na 2a (cada vez mais perto de pegar), liga na 3a.
/// Sem HUD: so a peca do passo atual reage ao mouse. O progresso fica nos campos, entao sair no meio nao reseta nada.
/// </summary>
public class GeneratorFuelMinigame : Minigame
{
    enum Drag { None, Cap, Pour, Cord }

    [Header("Bocal e tampa")]
    [Tooltip("Boca do bocal. +Y aponta para fora do bocal (eixo de giro da tampa).")]
    [SerializeField] Transform tankMouth;
    [SerializeField] GeneratorPart cap;
    [Tooltip("Onde a tampa fica pendurada quando solta.")]
    [SerializeField] Transform capRest;
    [SerializeField] float capTurns = 1f;
    [Tooltip("Quanto a tampa sai ao desenroscar, ao longo do bocal (m).")]
    [SerializeField] float capLift = 0.03f;

    [Header("Galao")]
    [Tooltip("O galao do minigame (pivo no bico, com GeneratorPart Can). Fica visivel so durante o minigame.")]
    [SerializeField] GeneratorPart pourCan;
    [Tooltip("Galao em pe, entrando pela lateral da tela.")]
    [SerializeField] Transform canHoldPose;
    [Tooltip("Bico sobre a boca do bocal, ainda em pe. A inclinacao gira em torno do +Z deste minigame (o eixo que aponta para dentro do gerador).")]
    [SerializeField] Transform canPourPose;
    [SerializeField] float maxTiltDegrees = 100f;
    [Tooltip("Segundos segurando o clique para inclinar o galao ao maximo.")]
    [SerializeField] float secondsToFullTilt = 2f;
    [Tooltip("Velocidade (inclinacao por segundo) com que o galao volta ao soltar.")]
    [SerializeField] float returnSpeed = 3f;
    [Tooltip("Inclinacao (0 a 1) a partir da qual comeca a sair gasolina.")]
    [SerializeField, Range(0f, 0.9f)] float pourStartTilt = 0.25f;
    [Tooltip("Fio de gasolina. Estica do bico ate a boca.")]
    [SerializeField] Transform stream;
    [SerializeField] Transform streamTarget;
    [Tooltip("O galao do mundo (o carregado). Ao ligar o gerador ele fica vazio neste ponto.")]
    [SerializeField] GasCan gasCan;
    [SerializeField] Transform canSpentPoint;

    [Header("Vazao")]
    [Tooltip("Quanto o bocal engole por segundo (1 = tanque cheio).")]
    [SerializeField] float intakePerSecond = 0.25f;
    [Tooltip("Vazao do galao na inclinacao maxima, em multiplos do que o bocal engole. Acima de 1 a espuma sobe.")]
    [SerializeField] float maxFlowRatio = 1.8f;
    [Tooltip("Quanto cabe na boca do bocal antes de transbordar (mesma unidade do tanque).")]
    [SerializeField] float mouthCapacity = 0.12f;
    [Tooltip("Espuma na boca. Cresce e sobe conforme a boca enche.")]
    [SerializeField] Transform foam;
    [SerializeField] float foamRise = 0.03f;

    [Header("Medidor")]
    [SerializeField] Transform gaugeNeedle;
    [Tooltip("Angulo do ponteiro (graus, em torno do eixo Z local) com o tanque vazio e cheio.")]
    [SerializeField] float gaugeEmptyAngle = 70f;
    [SerializeField] float gaugeFullAngle = -70f;

    [Header("Corda")]
    [SerializeField] GeneratorPart cordHandle;
    [SerializeField] Transform cordRest, cordEnd, cordAnchor;
    [SerializeField] Transform cordLine;
    [Tooltip("Corpo do gerador. Treme quando o motor engasga ou funciona.")]
    [SerializeField] Transform engineBody;
    [SerializeField] int pullsToStart = 3;
    [SerializeField] float shakeAmount = 0.01f;

    [Header("Retorno")]
    [SerializeField] ParticleSystem splash;
    [Tooltip("Fumaca do escapamento: um sopro a cada engasgo, continua com o motor ligado.")]
    [SerializeField] ParticleSystem exhaustSmoke;
    [Tooltip("Lampada piloto na face do gerador. Pisca no 2o engasgo e acende com o motor ligado.")]
    [SerializeField] Renderer pilotLamp;
    [SerializeField] Material lampOffMaterial, lampOnMaterial;

    [Header("Barulho (NoiseSystem)")]
    [SerializeField] float spillNoiseRadius = 14f;
    [SerializeField] float spillNoiseInterval = 0.7f;
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
    float capProgress;               // 0 = fechada no bocal, 1 = solta e pendurada
    bool full, sealedTank;
    float fuel, mouth;               // tanque e boca (mesma unidade)
    int pulls;
    bool running;

    // --- temporarios ---
    Drag drag;
    GeneratorPart hovered;
    GasCanCarrier carrier;
    float prevAngle, tilt, needleAngle, spillTimer, capWrongTurn;
    bool busy;
    Vector3 engineHome;
    Quaternion needleHome;
    Vector2 cordPress, cordAxis; float cordAxisLength, cordProgress;
    AudioSource sfx, pourSrc, spillSrc, runSrc;
    Coroutine cordRoutine;

    public float Fuel => fuel;
    public bool TankOpen => capProgress >= 1f;
    public bool TankSealed => sealedTank;
    public int Pulls => pulls;
    public bool EngineRunning => running;

    bool CanPour => capProgress >= 1f && !full;

    protected override void Awake()
    {
        base.Awake();
        if (engineBody != null) engineHome = engineBody.localPosition;
        if (gaugeNeedle != null) needleHome = gaugeNeedle.localRotation;
        needleAngle = gaugeEmptyAngle;

        sfx = NewSource(false);
        pourSrc = NewSource(true); pourSrc.clip = pourClip != null ? pourClip : FuelAudio.PourLoop();
        spillSrc = NewSource(true); spillSrc.clip = spillClip != null ? spillClip : FuelAudio.SpillLoop();
        runSrc = NewSource(true); runSrc.clip = runClip != null ? runClip : FuelAudio.EngineLoop();
        onExited.AddListener(OnLeft);
        if (splash != null) splash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (exhaustSmoke != null) exhaustSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        SetLamp(false);
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

    // O galao so e preciso enquanto falta combustivel: com o tanque cheio da para voltar so para fechar e dar partida.
    protected override string MissingRequirementText(PlayerInteractor interactor)
    {
        if (IsCompleted || full) return null;
        var c = interactor.GetComponent<GasCanCarrier>();
        return c != null && c.IsCarrying ? null : missingCanText;
    }

    protected override void OnBegin(PlayerInteractor interactor)
    {
        carrier = interactor.GetComponent<GasCanCarrier>();
        bool hasCan = carrier != null && carrier.IsCarrying;
        if (hasCan) carrier.SetHeldVisible(false);   // o galao do minigame toma o lugar do da mao
        if (pourCan != null) pourCan.gameObject.SetActive(hasCan && !full);
        tilt = 0f;
        ApplyAll();
    }

    void OnLeft()
    {
        if (carrier != null && carrier.IsCarrying) carrier.SetHeldVisible(true);
        if (pourCan != null) pourCan.gameObject.SetActive(false);
    }

    protected override void OnExitRequested()
    {
        SetHover(null);
        if (drag == Drag.Cord) ReleaseCord();
        drag = Drag.None;
        StopPourEffects();
    }

    // ---------- Regras: o que pode ser usado agora ----------

    bool CanUse(GeneratorPart p)
    {
        if (busy) return false;
        switch (p.kind)
        {
            case GeneratorPart.Kind.Cap: return (!full && capProgress < 1f) || (full && !sealedTank);
            case GeneratorPart.Kind.Can: return CanPour && p.gameObject.activeInHierarchy;
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

        if (drag != Drag.None)
        {
            switch (drag)
            {
                case Drag.Cap: DragCap(viewCamera, pos, held); break;
                case Drag.Pour: if (!held || !CanPour) drag = Drag.None; break;
                case Drag.Cord: DragCord(pos, held); break;
            }
        }
        else
        {
            var hit = Pick(viewCamera.ScreenPointToRay(pos));
            var usable = hit != null && CanUse(hit) ? hit : null;
            SetHover(usable);
            if (down && usable != null) BeginPart(usable, viewCamera, pos);
        }

        UpdatePour(Time.deltaTime);
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
        if (hovered != null) hovered.SetHighlight(false, HighlightTint.Contrast);
        hovered = p;
        if (hovered != null) hovered.SetHighlight(true, HighlightTint.Contrast);
    }

    void BeginPart(GeneratorPart p, Camera cam, Vector2 pos)
    {
        switch (p.kind)
        {
            case GeneratorPart.Kind.Cap:
                drag = Drag.Cap;
                capWrongTurn = 0f;
                prevAngle = ScreenAngle(cam, CapCenter, pos);
                break;
            case GeneratorPart.Kind.Can:
                drag = Drag.Pour;   // a peca segue realcada enquanto o clique esta seguro
                return;
            case GeneratorPart.Kind.Cord:
                drag = Drag.Cord;
                if (cordRoutine != null) StopCoroutine(cordRoutine);
                Vector3 a = cam.WorldToScreenPoint(cordRest.position), b = cam.WorldToScreenPoint(cordEnd.position);
                cordAxis = (Vector2)(b - a);
                cordAxisLength = Mathf.Max(1f, cordAxis.magnitude);
                cordAxis /= cordAxisLength;
                cordPress = pos;
                cordProgress = 0f;
                break;
        }
        SetHover(null);
    }

    // ---------- Tampa ----------

    // Centro do giro na tela: a boca do bocal (a tampa sai do lugar ao abrir).
    Vector3 CapCenter => tankMouth.position;

    static float ScreenAngle(Camera cam, Vector3 world, Vector2 mouse)
    {
        Vector2 c = cam.WorldToScreenPoint(world);
        return Mathf.Atan2(mouse.y - c.y, mouse.x - c.x) * Mathf.Rad2Deg;
    }

    void DragCap(Camera cam, Vector2 pos, bool held)
    {
        if (!held) { EndCapDrag(); return; }
        Vector2 c = cam.WorldToScreenPoint(CapCenter);
        if ((pos - c).sqrMagnitude < 64f) return;   // perto demais do centro: o angulo seria instavel

        float a = Mathf.Atan2(pos.y - c.y, pos.x - c.x) * Mathf.Rad2Deg;
        float delta = Mathf.DeltaAngle(prevAngle, a);   // + = anti-horario na tela = desenroscar
        prevAngle = a;
        float step = delta / (capTurns * 360f);

        // Sentido errado: a tampa cede um pouco, resiste e estala uma vez, para o jogador entender que e para o outro lado.
        bool wrong = full ? step > 0f && capProgress <= 0.6f : step < 0f && capProgress <= 0f;
        if (wrong)
        {
            float before = capWrongTurn;
            capWrongTurn = Mathf.Clamp(capWrongTurn + Mathf.Abs(step), 0f, 0.04f);
            if (before < 0.04f && capWrongTurn >= 0.04f) Click();
            ApplyCap();
            return;
        }
        capWrongTurn = Mathf.MoveTowards(capWrongTurn, 0f, Mathf.Abs(step));

        float min = full ? 0f : capProgress;   // abrindo nao volta; fechando nao sai de novo
        capProgress = Mathf.Clamp(capProgress + step, full ? 0f : min, full ? capProgress : 1f);
        ApplyCap();

        if (!full && capProgress >= 1f)
        {
            capProgress = 1f;
            Click();
            drag = Drag.None;
            ApplyCap();
        }
        else if (full && capProgress <= 0f)
        {
            capProgress = 0f;
            sealedTank = true;
            Click();
            drag = Drag.None;
        }
    }

    void EndCapDrag()
    {
        drag = Drag.None;
        capWrongTurn = 0f;
        ApplyCap();
    }

    void ApplyCap()
    {
        float p = capProgress;
        // 0 a 0,6: desenrosca saindo pelo eixo do bocal. 0,6 a 1: vai para o gancho.
        Vector3 lifted = tankMouth.position + tankMouth.up * (capLift * Mathf.Min(1f, p / 0.6f));
        float slide = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((p - 0.6f) / 0.4f));
        float turn = (p - capWrongTurn * (full ? -1f : 1f)) * capTurns * 360f;
        Quaternion onMouth = tankMouth.rotation * Quaternion.Euler(0f, turn, 0f);
        cap.transform.position = Vector3.Lerp(lifted, capRest.position, slide);
        cap.transform.rotation = Quaternion.Slerp(onMouth, capRest.rotation, slide);
    }

    // ---------- Despejo ----------

    void UpdatePour(float dt)
    {
        bool pouring = drag == Drag.Pour && CanPour;
        tilt = pouring ? Mathf.MoveTowards(tilt, 1f, dt / Mathf.Max(0.1f, secondsToFullTilt))
                       : Mathf.MoveTowards(tilt, 0f, returnSpeed * dt);
        ApplyCan();

        float flow = CanPour ? Mathf.InverseLerp(pourStartTilt, 1f, tilt) * maxFlowRatio * intakePerSecond : 0f;   // por segundo
        bool overflow = false;
        if (CanPour || mouth > 0f)
        {
            mouth += flow * dt;
            float swallowed = Mathf.Min(mouth, intakePerSecond * dt);
            mouth -= swallowed;
            fuel = Mathf.Min(1f, fuel + swallowed);
            if (mouth > mouthCapacity)
            {
                overflow = true;     // o excesso cai fora do bocal
                mouth = mouthCapacity;
            }
            if (fuel >= 1f) FinishFilling();
        }

        ApplyStream(flow / (maxFlowRatio * intakePerSecond));
        ApplyFoam();
        SetLoop(pourSrc, flow > 0f, Mathf.Lerp(0.3f, 1f, flow / (maxFlowRatio * intakePerSecond)) * volume);
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
                Circus.NoiseSystem.Emit(tankMouth.position, spillNoiseRadius, Circus.NoiseSource.Machine);
            }
        }
        else spillTimer = 0f;

        UpdateGauge(dt);
    }

    void FinishFilling()
    {
        full = true;
        fuel = 1f;
        mouth = 0f;
        if (drag == Drag.Pour) drag = Drag.None;
        Click();
    }

    void ApplyCan()
    {
        if (pourCan == null) return;
        var t = pourCan.transform;
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tilt / 0.25f));
        t.position = Vector3.Lerp(canHoldPose.position, canPourPose.position, blend);
        t.rotation = Quaternion.AngleAxis(-tilt * maxTiltDegrees, transform.forward)
                     * Quaternion.Slerp(canHoldPose.rotation, canPourPose.rotation, blend);
    }

    void ApplyStream(float flow01)
    {
        if (stream == null) return;
        bool on = flow01 > 0f && streamTarget != null && pourCan != null;
        if (stream.gameObject.activeSelf != on) stream.gameObject.SetActive(on);
        if (!on) return;
        Vector3 a = pourCan.transform.position, b = streamTarget.position;
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.001f) return;
        float w = Mathf.Lerp(0.006f, 0.022f, flow01);
        stream.position = (a + b) * 0.5f;
        stream.rotation = Quaternion.LookRotation(dir / len);
        stream.localScale = new Vector3(w, w, len);
    }

    void ApplyFoam()
    {
        if (foam == null) return;
        float k = mouthCapacity > 0f ? Mathf.Clamp01(mouth / mouthCapacity) : 0f;
        bool on = k > 0.15f;
        if (foam.gameObject.activeSelf != on) foam.gameObject.SetActive(on);
        if (!on) return;
        foam.position = tankMouth.position + tankMouth.up * (foamRise * k - 0.01f);
        float s = Mathf.Lerp(0.05f, 0.11f, k);
        foam.localScale = new Vector3(s, 0.01f + 0.02f * k, s);
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

    // Qualquer puxada ate o fim conta, sem limite de tempo. Soltar antes faz a corda voltar.
    void DragCord(Vector2 pos, bool held)
    {
        if (!held) { drag = Drag.None; ReleaseCord(); return; }
        cordProgress = Mathf.Clamp01(Vector2.Dot(pos - cordPress, cordAxis) / cordAxisLength);
        ApplyCord(cordProgress);
        if (cordProgress < 1f) return;
        drag = Drag.None;
        StartCoroutine(PullRoutine());
    }

    void ApplyCord(float p)
    {
        if (cordHandle == null) return;
        cordHandle.transform.position = Vector3.Lerp(cordRest.position, cordEnd.position, p);
        if (cordLine == null || cordAnchor == null) return;
        Vector3 a = cordAnchor.position, b = cordHandle.transform.position;
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.001f) { cordLine.localScale = Vector3.zero; return; }
        cordLine.position = (a + b) * 0.5f;
        cordLine.rotation = Quaternion.LookRotation(dir / len);
        cordLine.localScale = new Vector3(0.008f, 0.008f, len);
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
            // Retorno crescente: cada engasgo e mais longo, sopra mais fumaca e, do 2o em diante, a lampada pisca.
            float k = (pulls - 1f) / Mathf.Max(1f, pullsToStart - 2f);
            AudioClip clip = SputterClip(pulls);
            sfx.PlayOneShot(clip, volume);
            Puff(Mathf.RoundToInt(Mathf.Lerp(6f, 16f, k)));
            if (pulls >= 2) StartCoroutine(FlickerLamp(clip.length * 0.8f));
            yield return Shake(clip.length * 0.9f, shakeAmount * Mathf.Lerp(1f, 1.8f, k));
            busy = false;
            yield break;
        }

        StartEngine();
        Puff(24);
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

    void Puff(int count)
    {
        if (exhaustSmoke != null) exhaustSmoke.Emit(count);
    }

    IEnumerator FlickerLamp(float seconds)
    {
        for (float t = 0f; t < seconds; t += 0.07f)
        {
            SetLamp(Random.value > 0.45f);
            yield return new WaitForSeconds(0.07f);
        }
        SetLamp(running);
    }

    void SetLamp(bool on)
    {
        if (pilotLamp == null) return;
        var m = on ? lampOnMaterial : lampOffMaterial;
        if (m != null) pilotLamp.sharedMaterial = m;
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
        SetLamp(true);
        if (exhaustSmoke != null)
        {
            var em = exhaustSmoke.emission;
            em.rateOverTime = 4f;
            exhaustSmoke.Play();
        }
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
        ApplyCan();
        ApplyCord(0f);
        ApplyFoam();
        needleAngle = Mathf.Lerp(gaugeEmptyAngle, gaugeFullAngle, fuel);
        if (gaugeNeedle != null) gaugeNeedle.localRotation = needleHome * Quaternion.AngleAxis(needleAngle, Vector3.forward);
        if (stream != null) stream.gameObject.SetActive(false);
    }

    public override void ForceComplete()
    {
        base.ForceComplete();
        capProgress = 0f;
        fuel = 1f;
        mouth = 0f;
        full = sealedTank = true;
        pulls = pullsToStart;
        if (carrier == null) carrier = FindFirstObjectByType<GasCanCarrier>();
        FinishCan();
        ApplyAll();
        StartEngine();
    }

    void Click() => sfx.PlayOneShot(clickClip != null ? clickClip : FuelAudio.Click(), volume);
}
