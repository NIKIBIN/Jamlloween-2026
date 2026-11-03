using HeyHeyThere.PSXCarnivalKit;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Liga o carrossel quando o quadro de fusiveis (FuseBoxMinigame) e consertado. Com o jogador de volta ao controle ele
/// gira com a musica distorcida, e o cavalo fica sempre virado para o jogador.
/// Sem HUD: o estado aparece no proprio carrossel (LED do quadro, giro e musica).
/// </summary>
public class CarouselRepair : MonoBehaviour
{
    /// <summary>True assim que as duas fases estao completas. Outros sistemas podem consultar.</summary>
    public static bool Repaired { get; private set; }
    /// <summary>Disparado uma unica vez quando o carrossel passa a girar.</summary>
    public static event System.Action Started;

    [Header("Quadro de fusiveis")]
    [Tooltip("O minigame que, ao ser concluido, liga o carrossel.")]
    [SerializeField] Minigame fuseBox;

    [Header("Carrossel do kit (Machine)")]
    [Tooltip("Machine do painel de forca. Fica travado ate o quadro de fusiveis ser consertado; ai a alavanca dele e puxada (clipe 'throw') e a lampada acende. Opcional.")]
    [SerializeField] Machine powerPanel;
    [Tooltip("Machine interno, sem visual, que liga quando o quadro de fusiveis e consertado. O Machine do carrossel (Powered) deve ter este e o painel de forca em Needs. Opcional.")]
    [SerializeField] Machine repairedGate;

    [Header("Carrossel proprio (sem Machine)")]
    [Tooltip("Parte que gira (plataforma, cavalos e teto). Vazio quando o carrossel gira por um Machine do kit.")]
    [SerializeField] Transform rotor;
    [Tooltip("O cavalo que ganha a cabeca. Depois de consertado ele acompanha o jogador com o olhar enquanto gira.")]
    [SerializeField] Transform repairedHorse;
    [Tooltip("Rotacoes por minuto.")]
    [SerializeField] float rpm = 4f;
    [SerializeField] float spinUpSeconds = 4f;
    [Tooltip("Quao rapido o cavalo vira para o jogador (1/s).")]
    [SerializeField] float horseTurnSharpness = 6f;

    [Header("Barulho")]
    [Tooltip("Raio do barulho continuo do carrossel girando (0 = silencioso para quem escuta o NoiseSystem).")]
    [SerializeField] float spinNoiseRadius = 20f;
    [SerializeField] float noiseInterval = 1f;

    [SerializeField] CarouselAudio carouselAudio;

    [Header("Eventos")]
    public UnityEvent onRepaired;

    public bool FuseBoxDone => fuseBox != null && fuseBox.IsCompleted;
    public bool Spinning { get; private set; }

    float speed, noiseTimer;
    Transform player;
    bool panelThrown;
    Machine.Kind panelKind;

    void Awake()
    {
        // O jogador nao pode puxar a alavanca do painel de forca antes da hora: Gauge ignora o uso (E).
        if (powerPanel != null)
        {
            panelKind = powerPanel.kind;
            powerPanel.kind = Machine.Kind.Gauge;
        }
    }

    void Update()
    {
        // Quadro consertado: a alavanca de forca do painel e puxada pelo proprio kit.
        if (!panelThrown && powerPanel != null && FuseBoxDone)
        {
            panelThrown = true;
            powerPanel.kind = panelKind;
            powerPanel.SetOn(true);
        }

        if (!Spinning)
        {
            if (FuseBoxDone && Idle(fuseBox)) StartCarousel();
            return;
        }

        float target = rpm * 6f;   // graus por segundo
        speed = Mathf.MoveTowards(speed, target, target / Mathf.Max(spinUpSeconds, 0.1f) * Time.deltaTime);
        if (rotor != null) rotor.Rotate(0f, speed * Time.deltaTime, 0f, Space.Self);

        if (spinNoiseRadius > 0f)
        {
            noiseTimer -= Time.deltaTime;
            if (noiseTimer <= 0f)
            {
                noiseTimer = noiseInterval;
                Circus.NoiseSystem.Emit(transform.position, spinNoiseRadius, Circus.NoiseSource.Carousel);
            }
        }
    }

    // O cavalo vira depois que o rotor girou neste frame, entao nao treme.
    void LateUpdate()
    {
        if (!Spinning || repairedHorse == null) return;
        if (player == null)
        {
            var cam = Camera.main;
            if (cam != null) player = cam.transform;
            else
            {
                var pi = FindFirstObjectByType<PlayerInteractor>();
                if (pi != null) player = pi.ViewCamera != null ? pi.ViewCamera : pi.transform;
            }
            if (player == null) return;
        }

        Vector3 to = player.position - repairedHorse.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return;
        var look = Quaternion.LookRotation(to.normalized, Vector3.up);
        repairedHorse.rotation = Quaternion.Slerp(repairedHorse.rotation, look, 1f - Mathf.Exp(-horseTurnSharpness * Time.deltaTime));
    }

    // Espera o jogador voltar ao controle antes de ligar, para a camera do minigame nao ficar apontando para um carrossel que ja saiu do lugar.
    static bool Idle(Minigame m) => m.CurrentState == Minigame.State.Idle;

    void StartCarousel()
    {
        Spinning = true;
        Repaired = true;
        noiseTimer = 0f;
        if (carouselAudio != null) carouselAudio.SetMusic(true);
        if (repairedGate != null) repairedGate.SetOn(true);   // o Machine do carrossel gira ao ter forca e este gate
        Debug.Log("[CarouselRepair] carrossel consertado e girando", this);
        Started?.Invoke();
        onRepaired?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Repaired = false; Started = null; }
}
