using Jamlloween.Core;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Abastecer e ligar o gerador, diegetico. Tudo fica na face da frente do gerador: bocal lateral (como num carro),
/// tampa, medidor e corda de partida. So abre com o galao na mao enquanto o tanque nao esta cheio. Passos:
///  1. Abrir o bocal: clicar na tampa. Ela desenrosca sozinha e vai para o gancho.
///  2. Despejar: o galao passa a seguir o cursor (a ponta do bico fica no cursor). Segurar o clique em qualquer lugar
///     inclina o galao e a gasolina cai na vertical, saindo do bico; soltar endireita. Com o bico sobre a boca o tanque
///     enche; fora dela a gasolina cai no chao (respingo, som e barulho no NoiseSystem). O bocal engole numa vazao
///     fixa: despejando mais rapido que isso a espuma sobe na boca e, na borda, transborda (tambem faz barulho).
///  3. Fechar o bocal: clicar na tampa. O galao volta para a lateral.
///  4. Dar partida: puxar a corda ate o fim. A 1a puxada engasga e solta fumaca, a 2a liga (configuravel).
/// Sem HUD: so a peca do passo atual reage ao mouse. O progresso fica nos campos, entao sair no meio nao reseta nada.
/// </summary>
public class GeneratorFuelMinigame : Minigame
{
    enum Drag { None, Cord }

    [Header("Bocal e tampa")]
    [Tooltip("Boca do bocal. +Y aponta para fora do bocal (eixo de giro da tampa).")]
    [SerializeField] Transform tankMouth;
    [SerializeField] GeneratorPart cap;
    [Tooltip("Onde a tampa fica pendurada quando solta.")]
    [SerializeField] Transform capRest;
    [SerializeField] float capTurns = 1.5f;
    [Tooltip("Quanto a tampa sai ao desenroscar, ao longo do bocal (m).")]
    [SerializeField] float capLift = 0.03f;
    [Tooltip("Duracao da animacao de abrir ou fechar a tampa (s).")]
    [SerializeField] float capSeconds = 0.8f;

    [Header("Galao")]
    [Tooltip("O galao do minigame (pivo no bico). Fica visivel so durante o minigame, com o galao na mao.")]
    [SerializeField] GeneratorPart pourCan;
    [Tooltip("Galao em pe, na lateral da tela (antes de abrir e depois de encher).")]
    [SerializeField] Transform canHoldPose;
    [Tooltip("Orientacao do galao em pe enquanto segue o cursor.")]
    [SerializeField] Transform canPourPose;
    [Tooltip("Distancia (m) a frente da boca do plano em que o bico segue o cursor.")]
    [SerializeField] float followPlaneOffset = 0f;
    [Tooltip("Limites do bico no espaco local do minigame (x min/max, y min/max).")]
    [SerializeField] Vector4 followBounds = new Vector4(-1.3f, 0.3f, 1.0f, 2.0f);
    [SerializeField] float followSharpness = 18f;
    [Tooltip("Raio (m, na horizontal) em que o fio cai dentro da boca.")]
    [SerializeField] float mouthRadius = 0.045f;
    [Tooltip("Raio (m) do ima que puxa o bico para cima da boca.")]
    [SerializeField] float magnetRadius = 0.07f;
    [SerializeField] float maxTiltDegrees = 100f;
    [Tooltip("Segundos segurando o clique para inclinar o galao ao maximo.")]
    [SerializeField] float secondsToFullTilt = 2f;
    [Tooltip("Velocidade (inclinacao por segundo) com que o galao endireita ao soltar.")]
    [SerializeField] float returnSpeed = 3f;
    [Tooltip("Inclinacao (0 a 1) a partir da qual comeca a sair gasolina.")]
    [SerializeField, Range(0f, 0.9f)] float pourStartTilt = 0.25f;
    [Tooltip("Fio de gasolina. Cai na vertical, do bico ate a boca ou o chao.")]
    [SerializeField] Transform stream;
    [Tooltip("Sem uso: mantido para o prefab antigo.")]
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
    [SerializeField] int pullsToStart = 2;
    [SerializeField] float shakeAmount = 0.01f;

    [Header("Retorno")]
    [Tooltip("Respingo: na boca quando transborda, no chao quando o fio erra a boca.")]
    [SerializeField] ParticleSystem splash;
    [Tooltip("Fumaca: um sopro a cada engasgo, continua com o motor ligado.")]
    [SerializeField] ParticleSystem exhaustSmoke;
    [Tooltip("Lampada piloto na face do gerador. Pisca no ultimo engasgo e acende com o motor ligado.")]
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

    [Header("Passos (susto ao entrar)")]
    [Tooltip("Toca em 2D alguns segundos depois de entrar no minigame. Vazio = nada.")]
    [SerializeField] AudioClip stepsClip;
    [SerializeField] float stepsDelay = 3f;
    [SerializeField, Range(0f, 1f)] float stepsVolume = 1f;
    [Tooltip("So na primeira vez que o som chega a tocar. Sair antes do tempo cancela e a proxima entrada tenta de novo.")]
    [SerializeField] bool stepsOnce = true;

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
    float tilt, needleAngle, spillTimer, follow;   // follow: 0 = galao na lateral, 1 = seguindo o cursor
    bool busy, pouring;
    Vector3 spout;                    // ponta do bico (mundo) enquanto segue o cursor
    Vector3 engineHome;
    Quaternion needleHome;
    Vector2 cordPress, cordAxis; float cordAxisLength, cordProgress;
    AudioSource sfx, pourSrc, spillSrc, runSrc, stepsSrc;
    Coroutine cordRoutine, stepsRoutine;
    bool stepsPlayed;

    public float Fuel => fuel;
    public bool TankOpen => capProgress >= 1f;
    public bool TankSealed => sealedTank;
    public int Pulls => pulls;
    public bool EngineRunning => running;

    bool CanPour => capProgress >= 1f && !full && pourCan != null && pourCan.gameObject.activeInHierarchy;

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
        stepsSrc = gameObject.AddComponent<AudioSource>();
        stepsSrc.playOnAwake = false;
        stepsSrc.spatialBlend = 0f;
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
        follow = 0f;
        pouring = false;
        if (pourCan != null) spout = canHoldPose.position;
        ApplyAll();
        if (stepsClip != null && !(stepsOnce && stepsPlayed))
        {
            if (stepsRoutine != null) StopCoroutine(stepsRoutine);
            stepsRoutine = StartCoroutine(PlayStepsAfter(stepsDelay));
        }
    }

    IEnumerator PlayStepsAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        stepsRoutine = null;
        stepsPlayed = true;
        stepsSrc.PlayOneShot(stepsClip, stepsVolume);
    }

    void CancelSteps()
    {
        if (stepsRoutine == null) return;
        StopCoroutine(stepsRoutine);
        stepsRoutine = null;
    }

    void OnLeft()
    {
        if (carrier != null && carrier.IsCarrying) carrier.SetHeldVisible(true);
        if (pourCan != null) pourCan.gameObject.SetActive(false);
    }

    protected override void OnExitRequested()
    {
        CancelSteps();
        SetHover(null);
        if (drag == Drag.Cord) ReleaseCord();
        drag = Drag.None;
        pouring = false;
        StopPourEffects();
    }

    // ---------- Regras: o que pode ser usado agora ----------

    bool CanUse(GeneratorPart p)
    {
        if (busy) return false;
        switch (p.kind)
        {
            case GeneratorPart.Kind.Cap: return (!full && capProgress < 1f) || (full && !sealedTank && capProgress >= 1f);
            case GeneratorPart.Kind.Cord: return sealedTank && !running;
        }
        return false;   // o galao nao e clicado: ele segue o cursor
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

        if (CanPour)
        {
            // Etapa do despejo: o galao segue o cursor e segurar o clique (em qualquer lugar) inclina.
            SetHover(null);
            FollowCursor(ray);
            pouring = held;
        }
        else
        {
            pouring = false;
            if (drag == Drag.Cord) DragCord(pos, held);
            else
            {
                var hit = Pick(ray);
                var usable = hit != null && CanUse(hit) ? hit : null;
                SetHover(usable);
                if (down && usable != null) BeginPart(usable, viewCamera, pos);
            }
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
        SetHover(null);
        switch (p.kind)
        {
            case GeneratorPart.Kind.Cap:
                StartCoroutine(CapRoutine(full ? 0f : 1f));
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
                break;
        }
    }

    // ---------- Tampa ----------

    // Um clique: a tampa desenrosca (ou enrosca) sozinha.
    IEnumerator CapRoutine(float target)
    {
        busy = true;
        float from = capProgress;
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.05f, capSeconds))
        {
            capProgress = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t));
            ApplyCap();
            yield return null;
        }
        capProgress = target;
        ApplyCap();
        Click();
        if (target <= 0f) sealedTank = true;
        busy = false;
    }

    void ApplyCap()
    {
        float p = capProgress;
        // 0 a 0,6: desenrosca saindo pelo eixo do bocal. 0,6 a 1: vai para o gancho.
        Vector3 lifted = tankMouth.position + tankMouth.up * (capLift * Mathf.Min(1f, p / 0.6f));
        float slide = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((p - 0.6f) / 0.4f));
        Quaternion onMouth = tankMouth.rotation * Quaternion.Euler(0f, Mathf.Min(p, 0.6f) / 0.6f * capTurns * 360f, 0f);
        cap.transform.position = Vector3.Lerp(lifted, capRest.position, slide);
        cap.transform.rotation = Quaternion.Slerp(onMouth, capRest.rotation, slide);
    }

    // ---------- Galao e despejo ----------

    /// <summary>A ponta do bico vai para onde o cursor aponta, num plano paralelo a face, na altura da boca.</summary>
    void FollowCursor(Ray ray)
    {
        var plane = new Plane(-transform.forward, tankMouth.position - transform.forward * followPlaneOffset);
        if (!plane.Raycast(ray, out float d)) return;
        Vector3 local = transform.InverseTransformPoint(ray.GetPoint(d));
        local.x = Mathf.Clamp(local.x, followBounds.x, followBounds.y);
        local.y = Mathf.Clamp(local.y, followBounds.z, followBounds.w);
        Vector3 want = transform.TransformPoint(local);

        // Ima: perto de cima da boca, o bico encosta no prumo dela.
        Vector3 m = tankMouth.position;
        Vector3 rel = transform.InverseTransformVector(want - m);
        if (Mathf.Abs(rel.x) < magnetRadius && rel.y > 0f && rel.y < 0.3f)
            want = transform.TransformPoint(new Vector3(transform.InverseTransformPoint(m).x, local.y, local.z));

        spout = Vector3.Lerp(spout, want, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
    }

    /// <summary>Distancia horizontal (no plano da face) entre o bico e a boca. Usada para saber se o fio cai dentro.</summary>
    bool SpoutOverMouth()
    {
        Vector3 rel = transform.InverseTransformVector(spout - tankMouth.position);
        return Mathf.Abs(rel.x) <= mouthRadius && rel.y > 0f;
    }

    void UpdatePour(float dt)
    {
        bool tipping = pouring && CanPour;
        tilt = tipping ? Mathf.MoveTowards(tilt, 1f, dt / Mathf.Max(0.1f, secondsToFullTilt))
                       : Mathf.MoveTowards(tilt, 0f, returnSpeed * dt);
        follow = Mathf.MoveTowards(follow, CanPour ? 1f : 0f, dt * 3f);
        ApplyCan();

        float flow01 = CanPour ? Mathf.InverseLerp(pourStartTilt, 1f, tilt) : 0f;
        float flow = flow01 * maxFlowRatio * intakePerSecond;   // por segundo
        bool intoMouth = flow > 0f && SpoutOverMouth();
        bool groundSpill = flow > 0f && !intoMouth;
        bool overflow = false;

        if (intoMouth) mouth += flow * dt;
        if (mouth > 0f)
        {
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

        Vector3 streamEnd = StreamEnd(intoMouth);
        ApplyStream(flow01, streamEnd);
        ApplyFoam();
        bool spilling = overflow || groundSpill;
        SetLoop(pourSrc, intoMouth, Mathf.Lerp(0.3f, 1f, flow01) * volume);
        SetLoop(spillSrc, spilling, volume);
        if (splash != null)
        {
            if (spilling)
            {
                splash.transform.position = overflow ? tankMouth.position : streamEnd;
                if (!splash.isEmitting) splash.Play();
            }
            else if (splash.isEmitting) splash.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (spilling)
        {
            spillTimer -= dt;
            if (spillTimer <= 0f)
            {
                spillTimer = spillNoiseInterval;
                NoiseSystem.Emit(overflow ? tankMouth.position : streamEnd, spillNoiseRadius, NoiseSource.Machine);
            }
        }
        else spillTimer = 0f;

        UpdateGauge(dt);
    }

    /// <summary>Onde o fio vertical termina: na boca, se o bico estiver em cima dela, ou no chao.</summary>
    Vector3 StreamEnd(bool intoMouth)
    {
        if (intoMouth) return new Vector3(spout.x, tankMouth.position.y, spout.z);
        if (Physics.Raycast(spout + Vector3.down * 0.02f, Vector3.down, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore)) return hit.point;
        return spout + Vector3.down * 1.5f;
    }

    void FinishFilling()
    {
        full = true;
        fuel = 1f;
        mouth = 0f;
        pouring = false;
        Click();
    }

    void ApplyCan()
    {
        if (pourCan == null) return;
        var t = pourCan.transform;
        float k = Mathf.SmoothStep(0f, 1f, follow);
        t.position = Vector3.Lerp(canHoldPose.position, spout, k);
        // Inclina em torno do eixo que aponta para dentro do gerador: o bico (pivo) fica no lugar e o corpo sobe.
        t.rotation = Quaternion.AngleAxis(-tilt * maxTiltDegrees, transform.forward)
                     * Quaternion.Slerp(canHoldPose.rotation, canPourPose.rotation, k);
    }

    void ApplyStream(float flow01, Vector3 end)
    {
        if (stream == null) return;
        bool on = flow01 > 0f && pourCan != null;
        if (stream.gameObject.activeSelf != on) stream.gameObject.SetActive(on);
        if (!on) return;
        Vector3 a = spout, b = end;
        float len = Mathf.Max(0.01f, a.y - b.y);
        float w = Mathf.Lerp(0.006f, 0.022f, flow01);
        stream.position = (a + b) * 0.5f;
        stream.rotation = Quaternion.LookRotation(Vector3.down, transform.forward);
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
        NoiseSystem.Emit(transform.position, pullNoiseRadius, NoiseSource.Machine);
        sfx.PlayOneShot(cordClip != null ? cordClip : FuelAudio.CordPull(), volume);
        yield return RetractCord(1f, 0.15f);

        if (pulls < pullsToStart)
        {
            // Engasgo: tosse, sopro de fumaca e, na ultima antes de pegar, a lampada pisca.
            AudioClip clip = SputterClip(pulls);
            sfx.PlayOneShot(clip, volume);
            Puff(10 + 6 * (pulls - 1));
            if (pulls == pullsToStart - 1) StartCoroutine(FlickerLamp(clip.length * 0.8f));
            yield return Shake(clip.length * 0.9f, shakeAmount * (1f + 0.4f * (pulls - 1)));
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
