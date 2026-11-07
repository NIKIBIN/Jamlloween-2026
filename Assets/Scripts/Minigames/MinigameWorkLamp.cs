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

    // Dois slots de globais (MinigameWorkLamp.hlsl): ate duas luminarias acesas iluminam o PSX Unlit ao mesmo tempo.
    static readonly int[] PositionId = { Shader.PropertyToID("_WorkLampPosition"), Shader.PropertyToID("_WorkLamp2Position") };
    static readonly int[] DirectionId = { Shader.PropertyToID("_WorkLampDirection"), Shader.PropertyToID("_WorkLamp2Direction") };
    static readonly int[] ColorId = { Shader.PropertyToID("_WorkLampColor"), Shader.PropertyToID("_WorkLamp2Color") };
    static readonly int[] CosInnerId = { Shader.PropertyToID("_WorkLampCosInner"), Shader.PropertyToID("_WorkLamp2CosInner") };
    static readonly MinigameWorkLamp[] slots = new MinigameWorkLamp[2];   // a luminaria que escreve em cada slot

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
        Release();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { slots[0] = null; slots[1] = null; }

    int Slot => slots[0] == this ? 0 : slots[1] == this ? 1 : -1;

    /// <summary>Pega um slot livre. Sem slot livre, o mais novo a acender toma o slot 0 (a luz do minigame em uso vale mais).</summary>
    void Claim(bool force)
    {
        if (Slot >= 0) return;
        int free = slots[0] == null ? 0 : slots[1] == null ? 1 : -1;
        if (free < 0 && !force) return;
        slots[free < 0 ? 0 : free] = this;
    }

    void Release()
    {
        int s = Slot;
        if (s < 0) return;
        slots[s] = null;
        Shader.SetGlobalVector(ColorId[s], Vector4.zero);
    }

    public void TurnOn()
    {
        wanted = true;
        Claim(force: true);
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
        if (level > 0f) Claim(force: false);   // ex.: a sempre acesa retoma um slot quando outra apaga
    }

    // Depois de tudo mexer, publica a luz para os shaders. Sem lampada acesa os globais ficam em zero.
    void LateUpdate()
    {
        if (Slot < 0) return;
        PublishToShaders();
        if (level <= 0f && !wanted && !alwaysOn) Release();
    }

    void PublishToShaders()
    {
        int s = Slot;
        if (spot == null || s < 0) return;
        Transform t = spot.transform;
        float outer = Mathf.Cos(spot.spotAngle * 0.5f * Mathf.Deg2Rad);
        float inner = Mathf.Cos(spot.innerSpotAngle * 0.5f * Mathf.Deg2Rad);
        Color c = spot.color * (shaderIntensity * level);
        Shader.SetGlobalVector(PositionId[s], new Vector4(t.position.x, t.position.y, t.position.z, spot.range));
        Shader.SetGlobalVector(DirectionId[s], new Vector4(t.forward.x, t.forward.y, t.forward.z, outer));
        Shader.SetGlobalVector(ColorId[s], new Vector4(c.r, c.g, c.b, 0f));
        Shader.SetGlobalFloat(CosInnerId[s], inner);
    }

    /// <summary>Acende ou apaga na hora, sem esperar o fade. Usado ao montar a cena no editor.</summary>
    public void ForceState(bool on)
    {
        wanted = on;
        level = on ? 1f : 0f;
        if (on) Claim(force: true);
        Apply();
        if (on) PublishToShaders();
        else Release();
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
