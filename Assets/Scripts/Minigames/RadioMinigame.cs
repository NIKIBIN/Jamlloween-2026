using System.Collections;
using System.Globalization;
using HeyHeyThere.PSXCarnivalKit;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Conserto do radio (walkie-talkie dos anos 80). Primeiro conserto do jogo, tutorial das interacoes:
///  1. Arrastar a tampa traseira para fora.
///  2. Arrastar as 2 pilhas velhas (vazadas) para fora.
///  3. Arrastar as 2 pilhas novas para os encaixes. A polaridade (+/-) esta desenhada no fundo do
///     compartimento; pilha invertida encaixa, mas o radio nao liga. Botao direito, R ou a roda do mouse viram a pilha na mao.
///  4. Arrastar a tampa de volta ate encaixar (o radio vira sozinho para mostrar a frente).
///  5. Girar o botao de volume ate o clique: o LED acende e toca estatica.
///  6. Puxar a antena para cima.
///  7. Girar o dial. A estatica diminui perto da frequencia certa (que esta num bilhete do patrao no cenario).
///     Existe uma frequencia falsa que toca musica de carrossel distorcida por alguns segundos.
///  8. Segurar o botao lateral (push-to-talk) e soltar: o patrao responde.
/// Clicar no corpo do radio vira ele (por exemplo, para reabrir a tampa se as pilhas estiverem invertidas).
/// As pilhas novas vem do inventario do jogador (item requiredItem), entao precisam ser achadas no mapa.
/// Tudo e fisico: nao ha HUD. O progresso fica guardado no componente, entao sair no meio nao reseta nada.
/// </summary>
public class RadioMinigame : Minigame
{
    enum Drag { None, Cover, Battery, Volume, Dial, Antenna, Talk }
    enum CoverState { Closed, Free }

    [Header("Pecas")]
    [SerializeField] Transform radio;
    [SerializeField] Transform loose;
    [SerializeField] RadioPart body, cover, volumeKnob, dialKnob, antennaTip, pushToTalk;
    [SerializeField] Transform coverClosedAnchor;
    [SerializeField] Transform antennaBase, antennaRod, needle;
    [SerializeField] Transform[] slots;
    [Tooltip("Para cada encaixe: o polo + deve apontar para +Z (true) ou -Z (false). E o que esta desenhado no fundo.")]
    [SerializeField] bool[] slotPlusUp = { true, false };
    [SerializeField] RadioPart[] oldBatteries;
    [SerializeField] RadioPart[] newBatteries;
    [SerializeField] Transform[] newBatteryHomes;
    [Tooltip("Onde cada pilha velha descansa depois de tirada (prato do lado direito).")]
    [SerializeField] Transform[] oldBatteryRests;
    [Tooltip("Onde a tampa descansa quando esta solta.")]
    [SerializeField] Transform coverRest;
    [Header("Enquadramentos")]
    [Tooltip("Costas do radio (tampa, pilhas) e bancada.")]
    [SerializeField] Transform viewBack;
    [Tooltip("Frente do radio (botoes, dial, antena).")]
    [SerializeField] Transform viewFront;
    [SerializeField] Renderer led;
    [SerializeField] Material ledOnMaterial, ledOffMaterial;
    [SerializeField] RadioAudio radioAudio;
    [SerializeField] Color hoverColor = new Color(0.32f, 0.3f, 0.12f);

    [Header("Sintonia")]
    [SerializeField] float minFrequency = 88f;
    [SerializeField] float maxFrequency = 108f;
    [Tooltip("Frequencia onde o patrao responde (a mesma do bilhete).")]
    [SerializeField] float correctFrequency = 97.3f;
    [Tooltip("Frequencia falsa: toca musica de carrossel distorcida por alguns segundos. Some depois que o radio conecta.")]
    [SerializeField] float fakeFrequency = 92.1f;
    [Tooltip("Distancia (em MHz) da frequencia certa em que a sintonia pega.")]
    [SerializeField] float tolerance = 0.4f;
    [SerializeField] float fakeTolerance = 0.5f;
    [Tooltip("Distancia (em MHz) alem da tolerancia ate a estatica chegar ao maximo.")]
    [SerializeField] float staticFalloff = 3f;
    [SerializeField] float carouselSeconds = 1.5f;
    [Tooltip("Qualidade do sinal sem a antena puxada (1 = igual a antena puxada).")]
    [SerializeField, Range(0f, 1f)] float weakSignal = 0.35f;
    [SerializeField] float minimumTalkSeconds = 0.3f;

    [Header("Pilhas novas")]
    [Tooltip("Item do inventario do jogador que representa as pilhas novas.")]
    [SerializeField] string requiredItem = "battery";
    [SerializeField] int requiredCount = 2;
    [Tooltip("Onde as pilhas ficam no mapa. Se preenchido junto com o prefab, o pickup nasce neste ponto.")]
    [SerializeField] Transform batteryLocation;
    [SerializeField] GameObject batteryPickupPrefab;

    [Header("Bilhete do patrao")]
    [Tooltip("Pickup do bilhete no cenario: o texto dele e preenchido com a frequencia certa.")]
    [SerializeField] Pickup frequencyNote;
    [SerializeField, TextArea] string noteText = "Radio: {0} MHz.\nMe chama quando pegar. - Patrao";

    [Header("Audio (opcional)")]
    [SerializeField] AudioClip bossVoiceClip;
    [SerializeField] AudioClip carouselClip;

    // --- estado do conserto (persiste ao sair) ---
    CoverState coverState = CoverState.Closed;
    bool faceUp, flipping;
    bool knobOn, antennaLocked, connected;
    float volumeAngle, dialAngle, antennaT;
    RadioPart[] occupant;
    bool newBatteriesShown;
    System.Collections.Generic.List<string> playerItems;

    // --- arrasto ---
    Drag drag;
    RadioPart dragPart, hovered;
    Vector3 grabLocal;
    Ray lastRay;
    float coverSlideStartZ, batteryYaw, knobPrevAngle, antennaGrabDelta, talkStart;
    Vector3[] homePos;
    float[] homeYaw;
    Vector3 pttRestLocal;

    // --- sintonia / audio ---
    bool carouselArmed = true;
    float carouselTimer, carouselLevel;

    // medidas (espaco do objeto raiz, em metros)
    const float TableY = 0f;
    const float LiftY = 0.06f;
    const float CoverRestY = 0.002f;
    const float BatteryRestY = 0.011f;
    const float OpenDistance = 0.06f;
    const float SnapDistance = 0.04f;
    const float SlotCatch = 0.04f;
    const float RestYBack = 0.0345f, RestYFront = 0.053f;
    const float AntennaMin = 0.03f, AntennaMax = 0.15f;

    public float CorrectFrequency => correctFrequency;
    public bool Connected => connected;
    public bool CoverIsOpen => coverState == CoverState.Free;
    public bool FacingFront => faceUp;
    public bool IsPowered => Powered;
    public bool IsTuned => Tuned;
    public bool AntennaExtended => antennaLocked;
    public float CurrentFrequency => Frequency;
    /// <summary>Quantas pilhas estao nos encaixes agora.</summary>
    public int BatteriesInSlots { get { int n = 0; foreach (var o in occupant) if (o != null) n++; return n; } }

    float Frequency => Mathf.Lerp(minFrequency, maxFrequency, dialAngle / 270f);
    bool Powered => coverState == CoverState.Closed && BatteriesCorrect() && knobOn;
    bool Tuned => Powered && antennaLocked && Mathf.Abs(Frequency - correctFrequency) <= tolerance;
    float Volume01 => Mathf.Clamp01((volumeAngle - 25f) / 245f);

    // =================================================================

    protected override void Awake()
    {
        base.Awake();

        occupant = new RadioPart[slots.Length];
        for (int i = 0; i < oldBatteries.Length && i < slots.Length; i++)
        {
            occupant[i] = oldBatteries[i];
            oldBatteries[i].slot = i;
        }

        // Pilhas novas: posicao de origem e polaridade sorteada (uma ou outra costuma vir invertida)
        homePos = new Vector3[newBatteries.Length];
        homeYaw = new float[newBatteries.Length];
        var rnd = new System.Random(Mathf.RoundToInt(correctFrequency * 100f));
        for (int i = 0; i < newBatteries.Length; i++)
        {
            homePos[i] = newBatteryHomes[i].localPosition;
            homeYaw[i] = rnd.NextDouble() < 0.5 ? 0f : 180f;
            newBatteries[i].transform.SetParent(loose, false);
            newBatteries[i].transform.localPosition = homePos[i];
            newBatteries[i].transform.localRotation = Quaternion.Euler(0f, homeYaw[i], 0f);
            newBatteries[i].gameObject.SetActive(false);
        }

        pttRestLocal = pushToTalk.transform.localPosition;
        ApplyAntenna();
        radioAudio.Configure(bossVoiceClip, carouselClip);

        if (frequencyNote != null)
            frequencyNote.text = string.Format(noteText, correctFrequency.ToString("0.0", CultureInfo.InvariantCulture));
        SpawnBatteryPickup();
    }

    void SpawnBatteryPickup()
    {
        if (batteryPickupPrefab == null || batteryLocation == null) return;
        var go = Instantiate(batteryPickupPrefab, batteryLocation.position, batteryLocation.rotation);
        go.name = "Pilhas novas (radio)";
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

    protected override Transform EntryViewPoint => faceUp ? viewFront : viewBack;

    protected override void OnBegin(PlayerInteractor interactor)
    {
        var walker = interactor.GetComponent<LevelWalker>();
        playerItems = walker != null ? walker.items : null;
        if (!newBatteriesShown && HasNewBatteries()) newBatteriesShown = true;
        foreach (var b in newBatteries) b.gameObject.SetActive(newBatteriesShown);
    }

    bool HasNewBatteries() =>
        playerItems == null || playerItems.FindAll(i => i == requiredItem).Count >= requiredCount;

    // =================================================================
    // Entrada
    // =================================================================

    protected override void Tick(Camera viewCamera)
    {
        var mouse = Mouse.current;
        if (mouse == null) return;
        Vector2 mp = mouse.position.ReadValue();
        Ray ray = viewCamera.ScreenPointToRay(mp);
        lastRay = ray;
        bool down = mouse.leftButton.wasPressedThisFrame;
        bool held = mouse.leftButton.isPressed;

        if (drag != Drag.None)
        {
            SetHover(dragPart);
            UpdateDrag(viewCamera, ray, mp, held);
            return;
        }
        if (flipping) { SetHover(null); return; }

        var hit = Pick(ray);
        var usable = hit != null && CanUse(hit) ? hit : null;
        SetHover(usable);
        if (down && usable != null) BeginDrag(viewCamera, usable, ray, mp);
    }

    protected override void OnExitRequested()
    {
        SetHover(null);
        CancelDrag();
    }

    RadioPart Pick(Ray ray)
    {
        var hits = Physics.RaycastAll(ray, 5f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            var p = h.collider.GetComponent<RadioPart>();
            if (p != null) return p;
        }
        return null;
    }

    void SetHover(RadioPart p)
    {
        if (hovered == p) return;
        if (hovered != null) hovered.SetHighlight(false, hoverColor);
        hovered = p;
        if (hovered != null) hovered.SetHighlight(true, hoverColor);
    }

    bool CanUse(RadioPart p)
    {
        switch (p.kind)
        {
            case RadioPart.Kind.Body: return true;
            case RadioPart.Kind.Cover: return coverState == CoverState.Free || !faceUp;
            case RadioPart.Kind.Battery:
                if (p.slot >= 0) return !faceUp && coverState == CoverState.Free;
                return true;
            case RadioPart.Kind.Volume:
            case RadioPart.Kind.Dial:
            case RadioPart.Kind.Talk: return faceUp;
            case RadioPart.Kind.Antenna: return !antennaLocked;
        }
        return false;
    }

    // =================================================================
    // Arrasto
    // =================================================================

    bool Pointer(Ray ray, float localY, out Vector3 world)
    {
        var plane = new Plane(transform.up, transform.TransformPoint(0f, localY, 0f));
        if (plane.Raycast(ray, out float d)) { world = ray.GetPoint(d); return true; }
        world = default;
        return false;
    }

    Vector3 ToLocal(Vector3 world) => transform.InverseTransformPoint(world);

    void BeginDrag(Camera cam, RadioPart part, Ray ray, Vector2 mp)
    {
        dragPart = part;
        switch (part.kind)
        {
            case RadioPart.Kind.Body:
                dragPart = null;
                StartFlip(!faceUp);
                break;

            case RadioPart.Kind.Cover:
                drag = Drag.Cover;
                StopGlide(cover);
                if (coverState == CoverState.Closed)
                {
                    if (Pointer(ray, ToLocal(cover.transform.position).y, out var p)) coverSlideStartZ = ToLocal(p).z;
                }
                else GrabLoose(cover, ray);
                break;

            case RadioPart.Kind.Battery:
                drag = Drag.Battery;
                StopGlide(part);
                if (part.slot >= 0)
                {
                    occupant[part.slot] = null;
                    part.slot = -1;
                    part.transform.SetParent(loose, true);
                }
                GrabLoose(part, ray);
                batteryYaw = YawOf(part);
                radioAudio.PlayClick(0.5f);
                break;

            case RadioPart.Kind.Volume:
            case RadioPart.Kind.Dial:
                drag = part.kind == RadioPart.Kind.Volume ? Drag.Volume : Drag.Dial;
                knobPrevAngle = ScreenAngle(cam, part.transform.position, mp);
                break;

            case RadioPart.Kind.Antenna:
                drag = Drag.Antenna;
                if (Pointer(ray, ToLocal(antennaTip.transform.position).y, out var ap))
                    antennaGrabDelta = ToLocal(ap).z - ToLocal(antennaTip.transform.position).z;
                break;

            case RadioPart.Kind.Talk:
                drag = Drag.Talk;
                talkStart = Time.time;
                pushToTalk.transform.localPosition = pttRestLocal + new Vector3(0.004f, 0f, 0f);
                radioAudio.PlayClick(0.8f);
                break;
        }
    }

    /// <summary>O item levantado fica sempre sob o cursor (sem deslocamento por paralaxe).</summary>
    void GrabLoose(RadioPart part, Ray ray) => grabLocal = Vector3.zero;

    void UpdateDrag(Camera cam, Ray ray, Vector2 mp, bool held)
    {
        switch (drag)
        {
            case Drag.Cover: UpdateCover(ray, held); break;
            case Drag.Battery: UpdateBattery(ray, held); break;
            case Drag.Volume: UpdateKnob(cam, mp, held, volumeKnob, ref volumeAngle, true); break;
            case Drag.Dial: UpdateKnob(cam, mp, held, dialKnob, ref dialAngle, false); break;
            case Drag.Antenna: UpdateAntenna(ray, held); break;
            case Drag.Talk: if (!held) ReleaseTalk(true); break;
        }
    }

    void EndDrag()
    {
        drag = Drag.None;
        dragPart = null;
    }

    /// <summary>Sair no meio: solta o que estiver na mao sem desfazer o progresso.</summary>
    void CancelDrag()
    {
        switch (drag)
        {
            case Drag.Cover:
                if (coverState == CoverState.Free) DropCover(lastRay);
                else { GlideTo(cover, coverClosedAnchor.localPosition, Quaternion.identity, 0.12f); EndDrag(); }
                break;
            case Drag.Battery: DropBattery(lastRay); break;
            case Drag.Talk: ReleaseTalk(false); break;
            default: EndDrag(); break;
        }
    }

    // ---------- Tampa ----------

    void UpdateCover(Ray ray, bool held)
    {
        if (coverState == CoverState.Closed)
        {
            if (!held)   // largou antes de soltar: a tampa volta ao encaixe
            {
                GlideTo(cover, coverClosedAnchor.localPosition, Quaternion.identity, 0.12f);
                EndDrag();
                return;
            }
            if (!Pointer(ray, ToLocal(cover.transform.position).y, out var p)) return;
            float slide = Mathf.Clamp(coverSlideStartZ - ToLocal(p).z, 0f, OpenDistance * 1.3f);
            cover.transform.localPosition = coverClosedAnchor.localPosition + Vector3.back * slide;
            if (slide >= OpenDistance)   // soltou da pressao: sai na mao
            {
                cover.transform.SetParent(loose, true);
                coverState = CoverState.Free;
                GrabLoose(cover, ray);
                radioAudio.PlayClick(1f);
            }
            return;
        }

        if (!held) { DropCover(ray); return; }
        MoveHeld(cover, ray, Quaternion.identity);
    }

    void MoveHeld(RadioPart part, Ray ray, Quaternion localYaw)
    {
        if (!Pointer(ray, LiftY, out var p)) return;
        Vector3 l = ToLocal(p) + grabLocal;
        l.y = LiftY;
        l = ClampToTable(l);
        part.transform.position = transform.TransformPoint(l);
        part.transform.rotation = Quaternion.Slerp(part.transform.rotation, transform.rotation * localYaw, 18f * Time.deltaTime);
    }

    void DropCover(Ray ray)
    {
        Vector3 l = ToLocal(cover.transform.position);
        Vector3 closed = ToLocal(coverClosedAnchor.position);
        // o encaixe e decidido por onde o cursor aponta na altura do encaixe
        Vector3 aim = Pointer(ray, closed.y, out var pc) ? ToLocal(pc) : l;
        bool near = !faceUp && Vector2.Distance(new Vector2(aim.x, aim.z), new Vector2(closed.x, closed.z)) < SnapDistance;
        if (near && CoverCanClose()) CloseCover();
        else
        {
            GlideTo(cover, coverRest.localPosition, Quaternion.identity, 0.2f);   // vai para o lugar dela na bancada
        }
        EndDrag();
    }

    bool CoverCanClose()
    {
        foreach (var o in occupant) if (o == null || !o.isNew) return false;
        return true;
    }

    void CloseCover()
    {
        coverState = CoverState.Closed;
        cover.transform.SetParent(radio, true);
        GlideTo(cover, coverClosedAnchor.localPosition, coverClosedAnchor.localRotation, 0.12f);
        radioAudio.PlayClick(1f);
        StartCoroutine(AutoFlip());
    }

    IEnumerator AutoFlip()
    {
        yield return new WaitForSeconds(0.5f);
        if (!faceUp && !flipping && coverState == CoverState.Closed) StartFlip(true);
    }

    // ---------- Pilhas ----------

    void UpdateBattery(Ray ray, bool held)
    {
        var k = Keyboard.current;
        var m = Mouse.current;
        if ((k != null && k.rKey.wasPressedThisFrame) || m.rightButton.wasPressedThisFrame || Mathf.Abs(m.scroll.ReadValue().y) > 0.1f)
        {
            batteryYaw += 180f;
            radioAudio.PlayClick(0.4f);
        }
        if (!held) { DropBattery(ray); return; }
        MoveHeld(dragPart, ray, Quaternion.Euler(0f, batteryYaw, 0f));
    }

    void DropBattery(Ray ray)
    {
        var b = dragPart;
        int s = NearestEmptySlot(ray);
        if (s >= 0 && coverState == CoverState.Free && !faceUp) InsertBattery(b, s);
        else if (b.isNew) ReturnHome(b);
        else
        {
            int i = System.Array.IndexOf(oldBatteries, b);
            GlideTo(b, oldBatteryRests[i].localPosition, oldBatteryRests[i].localRotation, 0.2f);
        }
        EndDrag();
    }

    /// <summary>Encaixe vazio mais proximo de onde o cursor aponta (na altura dos encaixes).</summary>
    int NearestEmptySlot(Ray ray)
    {
        int best = -1;
        float bestD = SlotCatch;
        for (int i = 0; i < slots.Length; i++)
        {
            if (occupant[i] != null) continue;
            Vector3 sl = ToLocal(slots[i].position);
            if (!Pointer(ray, sl.y, out var hit)) continue;
            Vector3 l = ToLocal(hit);
            float d = Vector2.Distance(new Vector2(l.x, l.z), new Vector2(sl.x, sl.z));
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    void InsertBattery(RadioPart b, int slot)
    {
        bool plusUp = PlusUp(b);
        occupant[slot] = b;
        b.slot = slot;
        b.transform.SetParent(slots[slot], true);
        Quaternion localRot = Quaternion.Inverse(slots[slot].rotation) * transform.rotation * Quaternion.Euler(0f, plusUp ? 0f : 180f, 0f);
        GlideTo(b, Vector3.zero, localRot, 0.1f);
        radioAudio.PlayClick(0.9f);
    }

    void ReturnHome(RadioPart b)
    {
        int i = System.Array.IndexOf(newBatteries, b);
        GlideTo(b, homePos[i], Quaternion.Euler(0f, YawOf(b), 0f), 0.2f);
    }

    bool PlusUp(RadioPart b) => Vector3.Dot(b.transform.forward, transform.forward) > 0f;

    float YawOf(RadioPart b) => PlusUp(b) ? 0f : 180f;

    bool BatteriesCorrect()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var b = occupant[i];
            if (b == null || !b.isNew) return false;
            if (PlusUp(b) != slotPlusUp[i]) return false;
        }
        return true;
    }

    // ---------- Botoes ----------

    static float ScreenAngle(Camera cam, Vector3 world, Vector2 mouse)
    {
        Vector2 c = cam.WorldToScreenPoint(world);
        Vector2 d = mouse - c;
        return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
    }

    void UpdateKnob(Camera cam, Vector2 mp, bool held, RadioPart knob, ref float value, bool isVolume)
    {
        if (!held) { EndDrag(); return; }
        Vector2 c = cam.WorldToScreenPoint(knob.transform.position);
        if ((mp - c).magnitude < 12f) return;
        float a = ScreenAngle(cam, knob.transform.position, mp);
        float delta = Mathf.DeltaAngle(knobPrevAngle, a);
        knobPrevAngle = a;
        float before = value;
        value = Mathf.Clamp(value - delta, 0f, 270f);   // sentido horario aumenta
        knob.transform.localRotation = Quaternion.Euler(0f, value, 0f);

        if (isVolume)
        {
            if (!knobOn && before < 25f && value >= 25f) { knobOn = true; radioAudio.PlayClick(1f); }
            else if (knobOn && before > 12f && value <= 12f) { knobOn = false; radioAudio.PlayClick(1f); }
        }
        else if (Mathf.FloorToInt(before / 13.5f) != Mathf.FloorToInt(value / 13.5f))
            radioAudio.PlayClick(0.12f);   // dial com tic de catraca
    }

    void UpdateAntenna(Ray ray, bool held)
    {
        if (!held) { EndDrag(); return; }
        if (!Pointer(ray, ToLocal(antennaTip.transform.position).y, out var p)) return;
        float length = ToLocal(p).z - antennaGrabDelta - ToLocal(antennaBase.position).z;
        antennaT = Mathf.Clamp01((length - AntennaMin) / (AntennaMax - AntennaMin));
        if (antennaT >= 0.92f)
        {
            antennaT = 1f;
            antennaLocked = true;
            radioAudio.PlayClick(1f);
            EndDrag();
        }
        ApplyAntenna();
    }

    void ApplyAntenna()
    {
        float len = Mathf.Lerp(AntennaMin, AntennaMax, antennaT);
        antennaRod.localPosition = new Vector3(0f, 0f, len * 0.5f);
        antennaRod.localScale = new Vector3(antennaRod.localScale.x, len * 0.5f, antennaRod.localScale.z);
        antennaTip.transform.localPosition = new Vector3(0f, 0f, len);
    }

    void ReleaseTalk(bool transmit)
    {
        pushToTalk.transform.localPosition = pttRestLocal;
        float held = Time.time - talkStart;
        EndDrag();
        if (!transmit) return;

        if (Tuned && !connected && held >= minimumTalkSeconds) Connect();
        else radioAudio.PlayBurst(0.8f * Mathf.Lerp(0.3f, 1f, Volume01));
    }

    void Connect()
    {
        connected = true;
        if (playerItems != null)
            for (int i = 0; i < requiredCount; i++) playerItems.Remove(requiredItem);   // as pilhas novas ficam no radio
        float length = radioAudio.PlayBoss(Mathf.Lerp(0.5f, 1f, Volume01));
        Complete(length + 1.2f);
    }

    // ---------- Virar o radio ----------

    void StartFlip(bool toFront)
    {
        if (flipping || faceUp == toFront) return;
        StartCoroutine(FlipRoutine(toFront));
    }

    IEnumerator FlipRoutine(bool toFront)
    {
        flipping = true;
        Quaternion from = radio.localRotation;
        Quaternion to = Quaternion.Euler(0f, 0f, toFront ? 0f : 180f);
        float y0 = radio.localPosition.y;
        float y1 = toFront ? RestYFront : RestYBack;
        radioAudio.PlayClick(0.5f);
        GlideView(toFront ? viewFront : viewBack, 0.6f);
        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.5f)
        {
            float s = Mathf.SmoothStep(0f, 1f, k);
            radio.localRotation = Quaternion.Slerp(from, to, s);
            var pos = radio.localPosition;
            pos.y = Mathf.Lerp(y0, y1, s) + Mathf.Sin(k * Mathf.PI) * 0.05f;
            radio.localPosition = pos;
            yield return null;
        }
        radio.localRotation = to;
        var end = radio.localPosition;
        end.y = y1;
        radio.localPosition = end;
        faceUp = toFront;
        flipping = false;
        radioAudio.PlayClick(0.7f);
        GlideView(toFront ? viewFront : viewBack, 0.5f);
    }

    // ---------- Mesa ----------

    static Vector3 ClampToTable(Vector3 l)
    {
        l.x = Mathf.Clamp(l.x, -0.34f, 0.34f);
        l.z = Mathf.Clamp(l.z, -0.2f, 0.24f);
        return l;
    }

    void GlideTo(RadioPart p, Vector3 localPos, Quaternion localRot, float duration)
    {
        StopGlide(p);
        p.glide = StartCoroutine(Glide(p, localPos, localRot, duration));
    }

    void StopGlide(RadioPart p)
    {
        if (p.glide != null) StopCoroutine(p.glide);
        p.glide = null;
    }

    IEnumerator Glide(RadioPart p, Vector3 localPos, Quaternion localRot, float duration)
    {
        var t = p.transform;
        Vector3 p0 = t.localPosition;
        Quaternion r0 = t.localRotation;
        for (float k = 0f; k < 1f; k += Time.deltaTime / Mathf.Max(duration, 0.01f))
        {
            float s = Mathf.SmoothStep(0f, 1f, k);
            t.localPosition = Vector3.Lerp(p0, localPos, s);
            t.localRotation = Quaternion.Slerp(r0, localRot, s);
            yield return null;
        }
        t.localPosition = localPos;
        t.localRotation = localRot;
        p.glide = null;
    }

    // =================================================================
    // Visual e audio: rodam sempre, o radio continua ligado fora do minigame
    // =================================================================

    void LateUpdate()
    {
        bool powered = Powered;
        if (led != null) led.sharedMaterial = powered ? ledOnMaterial : ledOffMaterial;

        float norm = dialAngle / 270f;
        var np = needle.localPosition;
        np.x = Mathf.Lerp(-0.034f, 0.034f, norm);
        needle.localPosition = np;

        if (!powered)
        {
            carouselLevel = 0f;
            carouselTimer = 0f;
            radioAudio.SetLevels(0f, 0f);
            return;
        }

        float freq = Frequency;
        float dist = Mathf.Abs(freq - correctFrequency);
        float quality = dist <= tolerance ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (dist - tolerance) / Mathf.Max(0.01f, staticFalloff));
        float reception = antennaLocked ? 1f : Mathf.Lerp(weakSignal, 1f, antennaT);
        float staticAmount = connected ? 0.08f : Mathf.Lerp(1f, 0.05f, quality * reception);

        // Frequencia falsa: musica de carrossel por alguns segundos, uma vez por passada. Some depois de conectar.
        bool inFake = !connected && Mathf.Abs(freq - fakeFrequency) <= fakeTolerance;
        if (inFake && carouselArmed) { carouselTimer = carouselSeconds; carouselArmed = false; }
        if (!inFake) carouselArmed = true;
        carouselTimer -= Time.deltaTime;
        float target = inFake && carouselTimer > 0f ? 1f : 0f;
        carouselLevel = Mathf.MoveTowards(carouselLevel, target, Time.deltaTime * 6f);
        staticAmount *= Mathf.Lerp(1f, 0.4f, carouselLevel);

        float volume = Mathf.Lerp(0.3f, 1f, Volume01);
        float receive = drag == Drag.Talk ? 0f : 1f;   // transmitindo, nao se ouve o que chega
        radioAudio.SetLevels(staticAmount * volume * receive, carouselLevel * volume * receive);
    }
}
