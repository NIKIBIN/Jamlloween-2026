using UnityEngine;

/// <summary>
/// Luminaria de obra que ilumina um minigame. E um objeto do mundo (haste, cabeca e lampada visiveis);
/// o foco acende quando o jogador entra no minigame e apaga quando ele sai. Sem HUD, sem luz magica.
/// Ligue-a a um Minigame pelo campo "minigame" (o Circus > Add Work Lamps To Open Scene faz isso).
///
/// Neste projeto o mundo e iluminado por shader, nao por luzes reais: o PSX Unlit ignora luzes e o Lit comum fica preto.
/// Por isso a lampada tem duas partes: o spot de verdade (que o Flashlight Fog Surface ja le, como faz com a lanterna)
/// e os globais _WorkLamp* (que o PSX Unlit le, como faz com a luz dos olhos do Zatan).
/// </summary>
public class MinigameWorkLamp : MonoBehaviour
{
    [SerializeField] Minigame minigame;
    [SerializeField] Light spot;
    [Tooltip("Lampada. Brilha quando a luminaria esta acesa.")]
    [SerializeField] Renderer bulb;
    [SerializeField] Material bulbOn, bulbOff;

    [Header("Luz")]
    [Tooltip("Intensidade do spot de verdade quando acesa (o que o Flashlight Fog Surface le).")]
    [SerializeField] float intensity = 0.7f;
    [Tooltip("Intensidade da luz calculada no shader (o que o PSX Unlit le). Mesma escala do spot.")]
    [SerializeField] float shaderIntensity = 0.7f;
    [Tooltip("Segundos para acender e apagar por completo.")]
    [SerializeField] float fadeSeconds = 0.25f;
    [Tooltip("Deixa a luminaria sempre acesa, com ou sem o jogador no minigame.")]
    [SerializeField] bool alwaysOn;

    static readonly int PositionId = Shader.PropertyToID("_WorkLampPosition");
    static readonly int DirectionId = Shader.PropertyToID("_WorkLampDirection");
    static readonly int ColorId = Shader.PropertyToID("_WorkLampColor");
    static readonly int CosInnerId = Shader.PropertyToID("_WorkLampCosInner");
    static MinigameWorkLamp owner;   // a luminaria que esta escrevendo nos globais (a ultima a acender)

    float level;      // 0 = apagada, 1 = acesa
    bool wanted;

    public Minigame Minigame { get => minigame; set => minigame = value; }

    void OnEnable()
    {
        if (minigame != null)
        {
            minigame.onStarted.AddListener(TurnOn);
            minigame.onExited.AddListener(TurnOff);
            wanted = minigame.CurrentState != Minigame.State.Idle;
        }
        Apply();
    }

    void OnDisable()
    {
        if (minigame != null)
        {
            minigame.onStarted.RemoveListener(TurnOn);
            minigame.onExited.RemoveListener(TurnOff);
        }
        if (owner == this) { owner = null; Shader.SetGlobalVector(ColorId, Vector4.zero); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => owner = null;

    public void TurnOn()
    {
        wanted = true;
        owner = this;
    }
    public void TurnOff() => wanted = false;

    void Update()
    {
        float target = wanted || alwaysOn ? 1f : 0f;
        if (!Mathf.Approximately(level, target))
        {
            level = Mathf.MoveTowards(level, target, Time.deltaTime / Mathf.Max(fadeSeconds, 0.01f));
            Apply();
        }
        if (alwaysOn && level > 0f && owner == null) owner = this;
    }

    // Depois de tudo mexer, publica a luz para os shaders. Sem lampada acesa os globais ficam em zero.
    void LateUpdate()
    {
        if (owner != this) return;
        PublishToShaders();
        if (level <= 0f && !wanted) owner = null;
    }

    void PublishToShaders()
    {
        if (spot == null) return;
        Transform t = spot.transform;
        float outer = Mathf.Cos(spot.spotAngle * 0.5f * Mathf.Deg2Rad);
        float inner = Mathf.Cos(spot.innerSpotAngle * 0.5f * Mathf.Deg2Rad);
        Color c = spot.color * (shaderIntensity * level);
        Shader.SetGlobalVector(PositionId, new Vector4(t.position.x, t.position.y, t.position.z, spot.range));
        Shader.SetGlobalVector(DirectionId, new Vector4(t.forward.x, t.forward.y, t.forward.z, outer));
        Shader.SetGlobalVector(ColorId, new Vector4(c.r, c.g, c.b, 0f));
        Shader.SetGlobalFloat(CosInnerId, inner);
    }

    /// <summary>Acende ou apaga na hora, sem esperar o fade. Usado ao montar a cena no editor.</summary>
    public void ForceState(bool on)
    {
        wanted = on;
        level = on ? 1f : 0f;
        if (on) owner = this;
        Apply();
        if (on) PublishToShaders();
        else if (owner == this) { owner = null; Shader.SetGlobalVector(ColorId, Vector4.zero); }
    }

    void Apply()
    {
        if (spot != null)
        {
            spot.intensity = intensity * level;
            spot.enabled = level > 0.001f;
        }
        if (bulb != null)
        {
            var m = level > 0.5f ? bulbOn : bulbOff;
            if (m != null && bulb.sharedMaterial != m) bulb.sharedMaterial = m;
        }
    }
}
