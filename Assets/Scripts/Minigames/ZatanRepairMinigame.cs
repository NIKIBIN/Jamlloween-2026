using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Conserto do Zatan: o painel nas costas do animatronico (pneumatico, anos 80). Termina num jumpscare.
///  1. Abrir o painel: arrastar a tampa para fora (encaixe por pressao: precisa de um puxao firme).
///  2. Limpar: pegar a estopa e esfregar (arrastar em vai-e-vem) sobre o mostrador e a engrenagem.
///  3. Reconectar o ar: arrastar a mangueira ate a valvula (encaixa com snap).
///  4. Ligar a chave geral (clique). O compressor liga e respira. Depois de ligada, a chave pode ser
///     virada para OFF e ON a vontade: ela muda so visualmente e o compressor continua funcionando.
///  5. Girar: arrastar a manivela ate o eixo (snap), depois segurar e arrastar em circulo (sentido horario).
///     Fica mais pesado perto do fim, e a respiracao e os cliques aceleram.
///  6. Travas de piche: em angulos configuraveis, uma junta vaza e a manivela trava ate a junta ser esfregada.
///  7. Zero: a engrenagem trava com um clique, o compressor suspira e tudo fica em silencio.
///  8. Jumpscare: apos o silencio a camera sai do foco e mostra a cabeca ja virada 180 graus, parada.
///     Depois da pausa, dispara os eventos finais para outros sistemas (ex.: comecar a perseguicao).
/// Do passo 7 em diante o jogador nao consegue sair; a saida e pelo proprio jumpscare.
///
/// Sem HUD: tudo vem do proprio painel. Posicoes no espaco local deste transform, que olha para +Z
/// (a frente do painel fica em -Z). O progresso fica nos campos do componente, entao sair no meio nao reseta nada.
/// O jogo nao pausa: o compressor e o vazamento continuam enquanto o jogador esta fora.
/// </summary>
public class ZatanRepairMinigame : Minigame
{
    enum Drag { None, Cover, Rag, Hose, CrankCarry, CrankTurn }

    [System.Serializable]
    public class TarLock
    {
        [Tooltip("Angulo da cabeca (graus, 0 a 180) em que a manivela trava.")]
        [Range(1f, 179f)] public float angle = 60f;
        [Tooltip("Distancia (m) de estopa necessaria para limpar este vazamento. Maior = mais esfregadas.")]
        public float scrubDistance = 0.3f;
        [Tooltip("A junta que vaza. Comeca limpa e escondida; o piche aparece quando a manivela trava.")]
        public ZatanScrubArea joint;
        [Tooltip("Opcional. Fumaca que sai da junta enquanto ela vaza.")]
        public ParticleSystem smoke;
        [HideInInspector] public bool done;
    }

    const float TotalAngle = 180f;
    const float GearRatio = 2f;           // a engrenagem gira o dobro da cabeca
    const float CoverLiftZ = -0.2f;      // altura (z) em que a tampa flutua enquanto e carregada
    const float RagZ = -0.11f, HoseZ = -0.09f, CrankZ = -0.1f;
    const float RagRadius = 0.025f;

    [Header("Tampa")]
    [SerializeField] ZatanRepairPart cover;
    [SerializeField] Transform coverAnchor;
    [Tooltip("Onde a tampa descansa depois de tirada.")]
    [SerializeField] Transform coverRest;
    [SerializeField] float coverOpenDistance = 0.04f;

    [Header("Limpeza")]
    [SerializeField] ZatanRepairPart rag;
    [Tooltip("Onde a estopa fica presa na lateral do painel.")]
    [SerializeField] Transform ragHome;
    [SerializeField] ZatanScrubArea dialTar;
    [SerializeField] ZatanScrubArea gearTar;
    [Tooltip("Distancia (m) de estopa necessaria para limpar o mostrador.")]
    [SerializeField] float dialScrubDistance = 0.7f;
    [Tooltip("Distancia (m) de estopa necessaria para limpar a engrenagem do pescoco.")]
    [SerializeField] float gearScrubDistance = 0.7f;

    [Header("Ar")]
    [SerializeField] ZatanRepairPart hose;
    [SerializeField] LineRenderer hoseLine;
    [Tooltip("Ponta fixa da mangueira.")]
    [SerializeField] Transform hoseAnchor;
    [Tooltip("Onde a ponta solta da mangueira fica antes de ser encaixada.")]
    [SerializeField] Transform hoseRest;
    [Tooltip("Onde a ponta da mangueira encaixa, na valvula.")]
    [SerializeField] Transform valveSocket;
    [SerializeField] float hoseSnapDistance = 0.07f;
    [SerializeField] float hoseSag = 0.07f;
    [Tooltip("Marcador visivel do bocal da valvula. Pulsa em destaque enquanto a mangueira esta na mao.")]
    [SerializeField] Renderer[] socketMarker;

    [Header("Chave geral")]
    [SerializeField] ZatanRepairPart mainSwitch;
    [Tooltip("Gira em torno de Z: 0 = ON (alavanca para cima), 180 = OFF.")]
    [SerializeField] Transform switchLever;
    [SerializeField] Renderer[] lamps;
    [SerializeField] Material[] lampOnMaterials;
    [SerializeField] Material lampOffMaterial;
    [Tooltip("Raio do barulho do clunk da chave, ouvido por quem escuta o NoiseSystem (0 = silencioso).")]
    [SerializeField] float clunkNoiseRadius = 12f;

    [Header("Mostrador e engrenagem")]
    [Tooltip("Ponteiro do mostrador. Gira em Z: aponta para a esquerda em 0 graus reais e para a direita em 180 (onde a marca '0' foi refeita a mao).")]
    [SerializeField] Transform pointer;
    [SerializeField] Transform gearVisual;

    [Header("Maquina viva (so visual)")]
    [Tooltip("Haste do pistao. Sobe e desce no ritmo da respiracao.")]
    [SerializeField] Transform pistonRod;
    [SerializeField] float pistonStroke = 0.06f;
    [Tooltip("Ponteiro do manometro. Gira em Z e treme; treme mais conforme a manivela avanca.")]
    [SerializeField] Transform gaugeNeedle;
    [Tooltip("Graus do ponteiro do manometro em pressao zero e maxima.")]
    [SerializeField] Vector2 gaugeRange = new Vector2(125f, -125f);
    [Tooltip("Jatos de vapor (nas juntas). A vazao segue a respiracao e acelera com o angulo.")]
    [SerializeField] ParticleSystem[] steamJets;
    [SerializeField] float steamMaxRate = 16f;

    [Header("Manivela")]
    [SerializeField] ZatanRepairPart crank;
    [SerializeField] Transform crankHome;
    [Tooltip("Eixo da engrenagem, onde a manivela encaixa.")]
    [SerializeField] Transform axleSlot;
    [SerializeField] float crankSnapDistance = 0.08f;

    [Header("Giro")]
    [Tooltip("Graus de cabeca por volta completa do mouse, no comeco (sem peso).")]
    [SerializeField] float degreesPerTurn = 75f;
    [Tooltip("Peso da manivela: multiplicador de degreesPerTurn (eixo X = progresso 0 a 1 ate o zero). Valores menores = mais pesado.")]
    [SerializeField] AnimationCurve weightCurve = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(0.6f, 0.65f), new Keyframe(1f, 0.18f));
    [Tooltip("Piso do multiplicador, para o fim do giro sempre ser alcancavel.")]
    [SerializeField, Range(0.01f, 0.5f)] float minWeight = 0.05f;

    [Header("Travas de piche")]
    [Tooltip("Em ordem crescente de angulo. A ultima parte do giro (depois da ultima trava) nao trava.")]
    [SerializeField] TarLock[] locks =
    {
        new TarLock { angle = 60f, scrubDistance = 0.25f },
        new TarLock { angle = 120f, scrubDistance = 0.9f },
    };

    [Header("Zero e jumpscare")]
    [Tooltip("Pivo da cabeca do Zatan. Vazio = procura um filho chamado 'Head' no Zatan da cena.")]
    [SerializeField] Transform headPivot;
    [Tooltip("Eixo local da cabeca em torno do qual ela gira.")]
    [SerializeField] Vector3 headTurnAxis = Vector3.up;
    [Tooltip("1 ou -1: sentido em que a cabeca gira.")]
    [SerializeField] float headTurnSign = 1f;
    [Tooltip("Eixo local da cabeca que aponta para o rosto.")]
    [SerializeField] Vector3 headFaceAxis = Vector3.forward;
    [Tooltip("Opcional. Onde a camera fica no jumpscare. Vazio = a jumpscareDistance do rosto, olhando para ele.")]
    [SerializeField] Transform jumpscareView;
    [SerializeField] float jumpscareDistance = 0.45f;
    [Tooltip("Silencio (s) entre o suspiro terminar e a camera sair do foco.")]
    [SerializeField] float silenceBeforeJumpscare = 1.5f;
    [Tooltip("Tempo (s) que a camera leva ate o rosto.")]
    [SerializeField] float jumpscareCameraTime = 0.3f;
    [Tooltip("Tempo (s) que a cabeca fica parada olhando antes de qualquer outra coisa acontecer.")]
    [SerializeField] float headStareSeconds = 1.5f;
    [Tooltip("Id enviado ao EventBus.ScriptedEvent no fim do jumpscare. Vazio = nao envia.")]
    [SerializeField] string finishedEventId = "zatan_repair_finished";
    [Tooltip("Depois do jumpscare o motor volta a funcionar (compressor, pistao, manometro, vapor e luzes) e segue assim.")]
    [SerializeField] bool machineRunsAfterFinish = true;
    [Tooltip("Ritmo do motor depois do jumpscare, de 0 (devagar, como no comeco do giro) a 1 (como no fim do giro).")]
    [SerializeField, Range(0f, 1f)] float finishedSpeed01 = 0.3f;
    [Tooltip("Volume do compressor depois do jumpscare, como fracao do volume normal.")]
    [SerializeField, Range(0f, 1f)] float finishedVolume = 0.5f;

    [SerializeField] ZatanRepairAudio repairAudio;
    [SerializeField] Color hoverColor = new Color(1f, 0.85f, 0.2f);

    [Header("Eventos")]
    [Tooltip("A engrenagem travou no zero. A saida do jogador passa a ser forcada.")]
    public UnityEvent onZeroReached;
    [Tooltip("A camera sai do foco rumo ao rosto do Zatan (ex.: acender os olhos).")]
    public UnityEvent onJumpscare;
    [Tooltip("Fim do jumpscare, depois da cabeca ficar parada. Ligue aqui a perseguicao.")]
    public UnityEvent onJumpscareFinished;

    // --- estado do conserto (persiste ao sair) ---
    Vector3 pistonRest;
    bool coverOff, hoseConnected, compressorOn, switchVisualOn, crankOnAxle, zeroReached, finished;
    float headAngle, crankVisual;
    int activeLock = -1;

    // --- arrasto ---
    Drag drag;
    ZatanRepairPart hovered;
    Vector3 grabOffset, dragStart;
    float prevMouseAngle, effortLevel, scrubLevel, switchAngle = 180f;
    Coroutine coverGlide, ragGlide, hoseGlide, crankGlide;
    readonly List<ZatanScrubArea> areas = new List<ZatanScrubArea>();

    // --- cabeca ---
    Quaternion headRestLocal;
    bool headResolved;
    Transform autoView;

    public ZatanRepairPart Hovered => hovered;
    public bool PanelOpen => coverOff;
    public bool HoseConnected => hoseConnected;
    public bool CompressorOn => compressorOn;
    /// <summary>Angulo real da cabeca, de 0 a 180.</summary>
    public float HeadAngle => headAngle;
    public bool ZeroReached => zeroReached;

    protected override bool PlayerCanExit => !zeroReached;

    void OnValidate()
    {
        degreesPerTurn = Mathf.Max(1f, degreesPerTurn);
        silenceBeforeJumpscare = Mathf.Max(0f, silenceBeforeJumpscare);
        headStareSeconds = Mathf.Max(0f, headStareSeconds);
    }

    protected override void Awake()
    {
        base.Awake();
        if (dialTar != null) { dialTar.RequiredDistance = dialScrubDistance; areas.Add(dialTar); }
        if (gearTar != null) { gearTar.RequiredDistance = gearScrubDistance; areas.Add(gearTar); }
        if (locks != null)
        {
            System.Array.Sort(locks, (a, b) => a.angle.CompareTo(b.angle));
            foreach (var l in locks)
            {
                if (l.joint == null) continue;
                l.joint.RequiredDistance = l.scrubDistance;
                areas.Add(l.joint);
            }
        }
        if (pistonRod != null) pistonRest = pistonRod.localPosition;
        SetLamps(false);
        ApplyAngle();
        ApplySwitch(true);
    }

    protected override void OnBegin(PlayerInteractor interactor) => ResolveHead();

    // =================================================================
    // Regras: o que pode ser usado agora
    // =================================================================

    bool CanUse(ZatanRepairPart p)
    {
        if (zeroReached) return false;
        switch (p.kind)
        {
            case ZatanRepairPart.Kind.Cover: return !coverOff;
            case ZatanRepairPart.Kind.Rag: return coverOff;
            case ZatanRepairPart.Kind.Hose: return coverOff && DialClean && GearClean && !hoseConnected;
            case ZatanRepairPart.Kind.Switch: return hoseConnected;
            case ZatanRepairPart.Kind.Crank: return compressorOn;
        }
        return false;
    }

    bool DialClean => dialTar == null || dialTar.IsClean;
    bool GearClean => gearTar == null || gearTar.IsClean;

    // =================================================================
    // Loop
    // =================================================================

    // Som, mangueira, chave e luzes seguem o estado mesmo com o jogador fora do minigame.
    void LateUpdate()
    {
        DrawHose();
        if (switchLever != null && !Mathf.Approximately(switchAngle, SwitchTarget))
        {
            switchAngle = Mathf.MoveTowards(switchAngle, SwitchTarget, 900f * Time.deltaTime);
            switchLever.localRotation = Quaternion.Euler(0f, 0f, switchAngle);
        }

        AnimateMachine();
        PulseSocket();

        if (repairAudio == null) return;
        repairAudio.SetCompressor(MotorRunning, MotorSpeed01, finished ? finishedVolume : 1f);
        repairAudio.SetEffort(drag == Drag.CrankTurn ? effortLevel : 0f, headAngle / TotalAngle);
        bool working = drag == Drag.CrankTurn || drag == Drag.Rag;
        float leak = activeLock >= 0 && locks[activeLock].joint != null ? locks[activeLock].joint.Coverage : 0f;
        repairAudio.SetLeak(working ? leak : 0f);
        repairAudio.SetScrub(drag == Drag.Rag ? scrubLevel : 0f);
    }

    // O motor para no zero (silencio do jumpscare) e, se configurado, volta ao maximo quando o minigame termina.
    bool MotorRunning => compressorOn && (!zeroReached || finished);
    float MotorSpeed01 => finished ? finishedSpeed01 : headAngle / TotalAngle;

    /// <summary>Pistao, manometro e vapor seguem a respiracao do compressor e aceleram com o angulo.</summary>
    void AnimateMachine()
    {
        bool running = MotorRunning && repairAudio != null;
        float env = running ? repairAudio.BreathEnvelope : 0f;
        float speed = MotorSpeed01;
        if (pistonRod != null)
            pistonRod.localPosition = pistonRest + new Vector3(0f, pistonStroke * env, 0f);
        if (gaugeNeedle != null)
        {
            float pressure = running ? Mathf.Lerp(0.35f, 0.85f, speed) + 0.1f * env : 0f;
            float tremble = running ? (Mathf.PerlinNoise(Time.time * 30f, 3.7f) - 0.5f) * 2f * Mathf.Lerp(2f, 14f, speed) : 0f;
            gaugeNeedle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(gaugeRange.x, gaugeRange.y, pressure) + tremble);
        }
        if (steamJets != null)
            foreach (var ps in steamJets)
            {
                if (ps == null) continue;
                var em = ps.emission;
                em.rateOverTime = steamMaxRate * env * Mathf.Lerp(0.5f, 1.4f, speed);
                if (running && !ps.isPlaying) ps.Play();
                else if (!running && ps.isPlaying) ps.Stop();
            }
    }

    void PulseSocket()
    {
        if (socketMarker == null || socketMarker.Length == 0) return;
        bool show = drag == Drag.Hose;
        var c = Color.Lerp(new Color(0.2f, 0.7f, 0.2f), new Color(0.6f, 1f, 0.5f), 0.5f + 0.5f * Mathf.Sin(Time.time * 8f));
        ZatanRepairPart.Tint(socketMarker, show, c);
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
                case Drag.Rag: SetHover(rag); UpdateRag(ray, held); break;
                case Drag.Hose: SetHover(hose); UpdateHose(ray, held); break;
                case Drag.CrankCarry: SetHover(crank); UpdateCrankCarry(ray, held); break;
                case Drag.CrankTurn: SetHover(crank); UpdateCrankTurn(viewCamera, mp, held); break;
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
            case Drag.Rag: ReleaseRag(instant: true); break;
            case Drag.Hose: ReleaseHose(instant: true); break;
            case Drag.CrankCarry: ReleaseCrank(instant: true); break;
            case Drag.CrankTurn: EndCrankTurn(); break;
        }
    }

    ZatanRepairPart Pick(Ray ray)
    {
        var hits = Physics.RaycastAll(ray, 5f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            var p = h.collider.GetComponent<ZatanRepairPart>();
            if (p != null && p.transform.IsChildOf(transform)) return p;
        }
        return null;
    }

    void SetHover(ZatanRepairPart p)
    {
        if (hovered == p) return;
        if (hovered != null) hovered.SetHighlight(false, hoverColor);
        hovered = p;
        if (hovered != null) hovered.SetHighlight(true, hoverColor);
    }

    void BeginDrag(Camera cam, ZatanRepairPart part, Ray ray, Vector2 mp)
    {
        switch (part.kind)
        {
            case ZatanRepairPart.Kind.Cover:
                StopGlide(ref coverGlide);
                drag = Drag.Cover;
                if (LocalPoint(ray, coverAnchor.localPosition.z, out var p)) dragStart = p;
                break;

            case ZatanRepairPart.Kind.Rag:
                StopGlide(ref ragGlide);
                drag = Drag.Rag;
                scrubLevel = 0f;
                GrabOffset(rag.transform, ray, RagZ);
                break;

            case ZatanRepairPart.Kind.Hose:
                StopGlide(ref hoseGlide);
                drag = Drag.Hose;
                GrabOffset(hose.transform, ray, HoseZ);
                break;

            case ZatanRepairPart.Kind.Switch:
                ToggleSwitch();
                break;

            case ZatanRepairPart.Kind.Crank:
                StopGlide(ref crankGlide);
                if (crankOnAxle)
                {
                    drag = Drag.CrankTurn;
                    effortLevel = 0f;
                    if (!MouseAngle(cam, mp, axleSlot.position, out prevMouseAngle)) prevMouseAngle = 0f;
                }
                else
                {
                    drag = Drag.CrankCarry;
                    GrabOffset(crank.transform, ray, CrankZ);
                }
                break;
        }
    }

    // =================================================================
    // Passo 1: abrir o painel
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
        if (repairAudio != null) repairAudio.Click();
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
        cover.GetComponent<Collider>().enabled = false;   // a tampa fica de lado: nao ha passo para fecha-la
        Glide(ref coverGlide, cover.transform, coverRest.localPosition, coverRest.localRotation, time, null);
    }

    // =================================================================
    // Passo 2: limpar com a estopa
    // =================================================================

    void UpdateRag(Ray ray, bool held)
    {
        scrubLevel = 0f;
        if (LocalPoint(ray, RagZ, out var p))
        {
            Vector3 want = p + grabOffset;
            want.z = RagZ;
            want.x = Mathf.Clamp(want.x, -0.6f, 0.6f);
            want.y = Mathf.Clamp(want.y, -0.4f, 0.4f);
            Vector3 prev = rag.transform.localPosition;
            Vector3 next = Vector3.Lerp(prev, want, 1f - Mathf.Exp(-30f * Time.deltaTime));
            rag.transform.localPosition = next;
            Vector3 d = next - prev;
            d.z = 0f;
            ScrubAt(next, d.magnitude);
        }
        if (!held) ReleaseRag(instant: false);
    }

    /// <summary>A estopa percorreu dist sobre a posicao pos (local do painel). Esfrega a area que estiver embaixo.</summary>
    void ScrubAt(Vector3 pos, float dist)
    {
        foreach (var area in areas)
        {
            if (!area.Active || area.IsClean || !area.Contains(pos, RagRadius)) continue;
            scrubLevel = Mathf.Clamp01(dist / Mathf.Max(Time.deltaTime, 0.0001f) / 0.5f);
            if (area.Scrub(dist)) OnAreaCleaned(area);
            return;
        }
    }

    void OnAreaCleaned(ZatanScrubArea area)
    {
        if (repairAudio != null) repairAudio.Click();
        if (activeLock >= 0 && locks[activeLock].joint == area)
        {
            locks[activeLock].done = true;
            if (locks[activeLock].smoke != null) locks[activeLock].smoke.Stop();
            activeLock = -1;
        }
    }

    void ReleaseRag(bool instant)
    {
        drag = Drag.None;
        scrubLevel = 0f;
        Glide(ref ragGlide, rag.transform, ragHome.localPosition, ragHome.localRotation, instant ? 0f : 0.2f, null);
    }

    // =================================================================
    // Passo 3: reconectar o ar
    // =================================================================

    void UpdateHose(Ray ray, bool held)
    {
        FollowMouse(hose.transform, ray, HoseZ);
        if (!held) ReleaseHose(instant: false);
    }

    void ReleaseHose(bool instant)
    {
        drag = Drag.None;
        float time = instant ? 0f : 0.15f;
        Vector3 d = hose.transform.localPosition - valveSocket.localPosition;
        d.z = 0f;
        if (d.magnitude <= hoseSnapDistance)
        {
            hoseConnected = true;
            hose.GetComponent<Collider>().enabled = false;
            Glide(ref hoseGlide, hose.transform, valveSocket.localPosition, valveSocket.localRotation, time, () =>
            {
                if (repairAudio != null) repairAudio.Click();
            });
        }
        else Glide(ref hoseGlide, hose.transform, hoseRest.localPosition, hoseRest.localRotation, time, null);
    }

    /// <summary>Mangueira como uma curva que cai entre a ponta fixa e a ponta solta.</summary>
    void DrawHose()
    {
        if (hoseLine == null || hoseAnchor == null || hose == null) return;
        int n = hoseLine.positionCount;
        Vector3 a = hoseAnchor.localPosition, b = hose.transform.localPosition;
        float slack = hoseConnected ? 0.5f : 1f;
        Vector3 c = (a + b) * 0.5f + new Vector3(0f, -hoseSag * slack - Vector3.Distance(a, b) * 0.15f, -0.015f);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            hoseLine.SetPosition(i, (1f - t) * (1f - t) * a + 2f * (1f - t) * t * c + t * t * b);
        }
    }

    // =================================================================
    // Passo 4: chave geral
    // =================================================================

    float SwitchTarget => switchVisualOn ? 0f : 180f;

    void ToggleSwitch()
    {
        switchVisualOn = !switchVisualOn;
        if (!compressorOn)
        {
            // Primeira vez: liga de verdade. Depois disso a chave e so visual.
            compressorOn = true;
            if (repairAudio != null) repairAudio.Clunk();
            Circus.NoiseSystem.Emit(transform.position, clunkNoiseRadius, Circus.NoiseSource.Machine);
            StartCoroutine(FlickerLamps());
        }
        else if (repairAudio != null) repairAudio.Clunk(0.8f);
    }

    void ApplySwitch(bool instant)
    {
        if (switchLever == null) return;
        if (instant) switchAngle = SwitchTarget;
        switchLever.localRotation = Quaternion.Euler(0f, 0f, switchAngle);
    }

    IEnumerator FlickerLamps()
    {
        for (int i = 0; i < 9; i++)
        {
            SetLamps(i % 2 == 0, Random.value);
            yield return new WaitForSeconds(Random.Range(0.04f, 0.14f));
        }
        if (!zeroReached) SetLamps(true);
    }

    /// <summary>Acende ou apaga as luzes. chance &lt; 1 acende so algumas (para piscar).</summary>
    void SetLamps(bool on, float chance = 1f)
    {
        if (lamps == null) return;
        for (int i = 0; i < lamps.Length; i++)
        {
            if (lamps[i] == null) continue;
            bool lit = on && (chance >= 1f || Random.value < chance);
            Material m = lit && lampOnMaterials != null && lampOnMaterials.Length > 0
                ? lampOnMaterials[i % lampOnMaterials.Length] : lampOffMaterial;
            if (m != null) lamps[i].sharedMaterial = m;
        }
    }

    // =================================================================
    // Passos 5 e 6: manivela
    // =================================================================

    void UpdateCrankCarry(Ray ray, bool held)
    {
        FollowMouse(crank.transform, ray, CrankZ);
        if (!held) ReleaseCrank(instant: false);
    }

    void ReleaseCrank(bool instant)
    {
        drag = Drag.None;
        float time = instant ? 0f : 0.15f;
        Vector3 d = crank.transform.localPosition - axleSlot.localPosition;
        d.z = 0f;
        if (d.magnitude <= crankSnapDistance)
        {
            crankOnAxle = true;
            Glide(ref crankGlide, crank.transform, axleSlot.localPosition, Quaternion.Euler(0f, 0f, -crankVisual), time, () =>
            {
                if (repairAudio != null) repairAudio.Click();
            });
        }
        else Glide(ref crankGlide, crank.transform, crankHome.localPosition, crankHome.localRotation, time, null);
    }

    void UpdateCrankTurn(Camera cam, Vector2 mp, bool held)
    {
        if (!held) { EndCrankTurn(); return; }
        if (!MouseAngle(cam, mp, axleSlot.position, out float a)) return;
        float delta = Mathf.DeltaAngle(prevMouseAngle, a);
        prevMouseAngle = a;
        if (Mathf.Abs(delta) > 60f) return;   // pulo do mouse, nao um giro

        // Soh o sentido horario avanca. Girar ao contrario nao faz nada.
        float forward = Mathf.Max(0f, delta);
        float speed = forward / Mathf.Max(Time.deltaTime, 0.0001f);
        effortLevel = Mathf.Lerp(effortLevel, Mathf.Clamp01(speed / 240f), 1f - Mathf.Exp(-12f * Time.deltaTime));
        if (forward <= 0f) return;

        if (activeLock >= 0)   // travada: o motor forca e nada se move
        {
            effortLevel = Mathf.Max(effortLevel, 0.6f);
            return;
        }

        float p01 = headAngle / TotalAngle;
        float weight = Mathf.Max(minWeight, weightCurve.Evaluate(p01));
        float next = headAngle + forward / 360f * degreesPerTurn * weight;
        crankVisual += forward;
        crank.transform.localRotation = Quaternion.Euler(0f, 0f, -crankVisual);

        for (int i = 0; i < locks.Length; i++)
        {
            if (locks[i].done || locks[i].angle <= headAngle || next < locks[i].angle) continue;
            next = locks[i].angle;
            headAngle = next;
            ApplyAngle();
            StartLock(i);
            return;
        }

        headAngle = Mathf.Min(next, TotalAngle);
        ApplyAngle();
        if (headAngle >= TotalAngle) Zero();
    }

    void EndCrankTurn()
    {
        drag = Drag.None;
        effortLevel = 0f;
    }

    void StartLock(int i)
    {
        activeLock = i;
        var l = locks[i];
        if (l.joint != null) l.joint.Activate(animate: true);
        if (l.smoke != null) l.smoke.Play();
    }

    void ApplyAngle()
    {
        if (pointer != null) pointer.localRotation = Quaternion.Euler(0f, 0f, TotalAngle - headAngle);
        if (gearVisual != null) gearVisual.localRotation = Quaternion.Euler(0f, 0f, -headAngle * GearRatio);
        var head = ResolveHead();
        if (head != null) head.localRotation = headRestLocal * Quaternion.AngleAxis(headTurnSign * headAngle, headTurnAxis);
    }

    // =================================================================
    // Passos 7 e 8: zero e jumpscare
    // =================================================================

    void Zero()
    {
        zeroReached = true;   // a partir daqui o jogador nao sai mais
        EndCrankTurn();
        SetHover(null);
        Debug.Log("[ZatanRepair] zero alcancado", this);
        onZeroReached?.Invoke();
        StartCoroutine(Finale());
    }

    IEnumerator Finale()
    {
        // A engrenagem trava com um clique, o compressor suspira e tudo fica em silencio.
        if (repairAudio != null)
        {
            repairAudio.StopAll();
            repairAudio.Click();
        }
        SetLamps(false);
        float sighLength = repairAudio != null ? repairAudio.Sigh() : 0f;
        yield return new WaitForSeconds(sighLength + silenceBeforeJumpscare);

        // A camera sai do foco rumo ao rosto, que ja esta virado 180 graus.
        onJumpscare?.Invoke();
        var view = JumpscareView();
        if (view != null) GlideView(view, jumpscareCameraTime);
        else Debug.LogWarning("[ZatanRepair] sem cabeca para o jumpscare: configure headPivot ou jumpscareView", this);
        yield return new WaitForSeconds(jumpscareCameraTime + headStareSeconds);

        // O motor volta antes dos eventos: um assinante que falhe nao pode deixa-lo parado.
        if (machineRunsAfterFinish)
        {
            finished = true;
            SetLamps(true);
            if (repairAudio != null) repairAudio.Clunk(0.5f);
        }

        // So agora, com a cabeca parada ha tempo, os outros sistemas ficam sabendo.
        try
        {
            if (!string.IsNullOrEmpty(finishedEventId)) Circus.EventBus.RaiseScriptedEvent(finishedEventId);
            onJumpscareFinished?.Invoke();
        }
        catch (System.Exception e) { Debug.LogException(e, this); }
        Complete(0.05f);
    }

    Transform JumpscareView()
    {
        if (jumpscareView != null) return jumpscareView;
        var head = ResolveHead();
        if (head == null) return null;
        if (autoView == null)
        {
            autoView = new GameObject("ZatanJumpscareView").transform;
            autoView.hideFlags = HideFlags.HideInHierarchy;
        }
        Vector3 face = head.TransformDirection(headFaceAxis).normalized;
        autoView.SetPositionAndRotation(head.position + face * jumpscareDistance, Quaternion.LookRotation(-face, Vector3.up));
        return autoView;
    }

    void OnDestroy()
    {
        if (autoView != null) Destroy(autoView.gameObject);
    }

    /// <summary>A cabeca do Zatan: o campo headPivot ou o filho "Head" do Zatan da cena. Guarda a rotacao de repouso na primeira vez.</summary>
    Transform ResolveHead()
    {
        if (headResolved) return headPivot;
        if (headPivot == null)
        {
            headPivot = FindNamed(Circus.ZatanLocator.Transform, "Head");
            if (headPivot == null && transform.root != transform) headPivot = FindNamed(transform.root, "Head");
        }
        if (headPivot == null) return null;
        headResolved = true;
        headRestLocal = headPivot.localRotation;
        return headPivot;
    }

    Transform FindNamed(Transform root, string childName)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == childName && !t.IsChildOf(transform)) return t;
        return null;
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

    void FollowMouse(Transform t, Ray ray, float planeZ)
    {
        if (!LocalPoint(ray, planeZ, out var p)) return;
        Vector3 target = p + grabOffset;
        target.z = planeZ;
        target.x = Mathf.Clamp(target.x, -0.6f, 0.6f);
        target.y = Mathf.Clamp(target.y, -0.5f, 0.4f);
        t.localPosition = Vector3.Lerp(t.localPosition, target, 1f - Mathf.Exp(-30f * Time.deltaTime));
    }

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
