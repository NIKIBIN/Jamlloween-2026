using Jamlloween.Progression;
using UnityEngine;

/// <summary>
/// Faíscas no painel das costas da cabine do Zatan, antes do conserto: estalos elétricos em áudio 3D, um jorro
/// pequeno de faíscas e um clarão (StepBeacon) que segue o volume do estalo, quadro a quadro. Serve para o jogador
/// achar o painel ATRÁS da cabine pelo som e pela luz.
///
/// Funciona só enquanto a etapa do conserto está ativa (StepInteraction do minigame), o minigame está fechado
/// e o painel ainda não foi consertado. Sem clips no Inspector, os estalos são sintetizados aqui.
/// </summary>
public class ZatanPanelSparks : MonoBehaviour
{
    const int Rate = 44100;
    const int EnvelopeWindow = 220;   // ~5 ms por amostra do envelope que move a luz

    [SerializeField] ZatanRepairMinigame minigame;
    [SerializeField] ParticleSystem sparks;
    [Tooltip("Clarão (StepBeacon com padrão Driven). A cena é quase toda unlit: um Light comum não aparece nela.")]
    [SerializeField] StepBeacon glow;
    [SerializeField] AudioSource source;
    [Tooltip("Opcional. Estalos gravados; vazio = estalos sintetizados.")]
    [SerializeField] AudioClip[] clips;

    [Header("Ritmo")]
    [Tooltip("Pausa entre uma rajada de estalos e a próxima (segundos, mínimo e máximo).")]
    [SerializeField] Vector2 pauseRange = new Vector2(1.2f, 3.5f);
    [Tooltip("Estalos por rajada (mínimo e máximo).")]
    [SerializeField] Vector2Int crackleCount = new Vector2Int(1, 3);
    [Tooltip("Pausa entre os estalos de uma rajada (segundos, mínimo e máximo).")]
    [SerializeField] Vector2 crackleGap = new Vector2(0.03f, 0.18f);

    [Header("Efeito")]
    [SerializeField, Range(0f, 1f)] float volume = 0.45f;
    [Tooltip("Variação de volume entre um estalo e outro (fração do volume, mínimo e máximo).")]
    [SerializeField] Vector2 volumeJitter = new Vector2(0.6f, 1f);
    [Tooltip("Força do clarão no pico do estalo (0 a 1; a cor, a intensidade e o alcance ficam no StepBeacon).")]
    [SerializeField, Range(0f, 1f)] float glowLevel = 1f;
    [Tooltip("Faíscas soltas no começo de cada estalo (mínimo e máximo).")]
    [SerializeField] Vector2Int burstParticles = new Vector2Int(6, 16);
    [Tooltip("Envelope acima disto (0 a 1) solta mais algumas faíscas durante o estalo.")]
    [SerializeField, Range(0f, 1f)] float peakThreshold = 0.6f;

    AudioClip[] bank;
    float[][] envelopes;
    StepInteraction step;
    float wait;
    int left;

    void Awake()
    {
        if (minigame == null) minigame = GetComponentInParent<ZatanRepairMinigame>();
        step = minigame != null ? minigame.GetComponent<StepInteraction>() : null;
        if (source == null) source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;

        bank = clips != null && clips.Length > 0 ? clips : new[] { MakeCrackle(1, 0.22f), MakeCrackle(2, 0.35f), MakeCrackle(3, 0.14f), MakeZap(4) };
        envelopes = new float[bank.Length][];
        for (int i = 0; i < bank.Length; i++) envelopes[i] = Envelope(bank[i]);

        SetGlow(0f);
        wait = Random.Range(0.3f, pauseRange.y);
    }

    bool Active => minigame != null && !minigame.IsCompleted && minigame.CurrentState == Minigame.State.Idle
                   && (step == null || step.IsActive);

    void Update()
    {
        if (!Active)
        {
            if (source.isPlaying) source.Stop();
            SetGlow(0f);
            left = 0;
            return;
        }

        if (source.isPlaying)
        {
            Follow();
            return;
        }
        SetGlow(0f);

        wait -= Time.deltaTime;
        if (wait > 0f) return;

        if (left <= 0) left = Random.Range(crackleCount.x, crackleCount.y + 1);
        Crackle();
        left--;
        wait = left > 0 ? Random.Range(crackleGap.x, crackleGap.y) : Random.Range(pauseRange.x, pauseRange.y);
    }

    int current;

    void Crackle()
    {
        current = Random.Range(0, bank.Length);
        source.clip = bank[current];
        source.pitch = Random.Range(0.9f, 1.15f);
        source.volume = volume * Random.Range(volumeJitter.x, volumeJitter.y);
        source.Play();
        if (sparks != null) sparks.Emit(Random.Range(burstParticles.x, burstParticles.y + 1));
    }

    /// <summary>A luz segue o volume do estalo que está tocando; os picos soltam mais faíscas.</summary>
    void Follow()
    {
        var env = envelopes[current];
        int i = Mathf.Clamp(source.timeSamples / EnvelopeWindow, 0, env.Length - 1);
        float e = env[i];
        SetGlow(glowLevel * e * Random.Range(0.75f, 1f));
        if (sparks != null && e > peakThreshold && Random.value < 0.5f) sparks.Emit(Random.Range(1, 4));
    }

    void SetGlow(float level)
    {
        if (glow != null) glow.DrivenLevel = level;
    }

    static float[] Envelope(AudioClip clip)
    {
        var data = new float[clip.samples * clip.channels];
        clip.GetData(data, 0);
        int frames = clip.samples;
        var env = new float[Mathf.Max(1, frames / EnvelopeWindow + 1)];
        float max = 0f;
        for (int w = 0; w < env.Length; w++)
        {
            float peak = 0f;
            int end = Mathf.Min(frames, (w + 1) * EnvelopeWindow);
            for (int f = w * EnvelopeWindow; f < end; f++)
                for (int c = 0; c < clip.channels; c++) peak = Mathf.Max(peak, Mathf.Abs(data[f * clip.channels + c]));
            env[w] = peak;
            max = Mathf.Max(max, peak);
        }
        if (max > 0f) for (int w = 0; w < env.Length; w++) env[w] /= max;
        return env;
    }

    // =================================================================
    // Síntese
    // =================================================================

    static AudioClip Build(string name, float seconds, System.Func<int, float, float> sample)
    {
        var data = new float[Mathf.CeilToInt(seconds * Rate)];
        for (int i = 0; i < data.Length; i++) data[i] = sample(i, i / (float)Rate);
        Normalize(data);
        var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    const float TargetRms = 0.12f, PeakCeiling = 0.5f;

    /// <summary>Todos os estalos com a mesma força (RMS), e nenhum pico acima do teto: nenhum estala mais alto que os outros.</summary>
    static void Normalize(float[] data)
    {
        double sum = 0;
        foreach (var x in data) sum += x * x;
        float rms = Mathf.Sqrt((float)(sum / data.Length));
        float gain = rms > 0f ? TargetRms / rms : 1f;
        for (int i = 0; i < data.Length; i++)
        {
            float x = data[i] * gain;
            // Limitador suave: perto do teto o pico se achata em vez de cortar seco.
            data[i] = PeakCeiling * (float)System.Math.Tanh(x / PeakCeiling);
        }
    }

    static float Noise(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);

    /// <summary>Estalo de faísca: uma série de estalinhos secos e irregulares que somem no fim.</summary>
    static AudioClip MakeCrackle(int seed, float seconds)
    {
        var rnd = new System.Random(seed);
        int n = Mathf.CeilToInt(seconds * Rate);
        var amp = new float[n];
        // Estalinhos: impulsos de ruído curtos (1 a 6 ms), mais densos no começo.
        int pos = 0;
        while (pos < n)
        {
            int len = (int)(Rate * (0.001 + rnd.NextDouble() * 0.005));
            float a = (float)(0.4 + rnd.NextDouble() * 0.6) * (1f - pos / (float)n * 0.7f);
            for (int k = 0; k < len && pos + k < n; k++) amp[pos + k] = Mathf.Max(amp[pos + k], a * Mathf.Exp(-k / (float)len * 3f));
            pos += len + (int)(Rate * rnd.NextDouble() * 0.012 * (1f + pos / (float)n * 2f));
        }
        float lp = 0f;
        return Build($"ZatanSpark{seed}", seconds, (i, t) =>
        {
            float x = Noise(rnd);
            lp += (x - lp) * 0.6f;                 // tira um pouco do chiado mais agudo
            return (x - lp * 0.5f) * amp[i] * 0.9f;
        });
    }

    /// <summary>"Zzzt": zumbido curto de arco elétrico (rede de 60 Hz com harmônicos) com estalos por cima.</summary>
    static AudioClip MakeZap(int seed)
    {
        var rnd = new System.Random(seed);
        const float seconds = 0.4f;
        return Build("ZatanZap", seconds, (i, t) =>
        {
            float env = Mathf.Clamp01(t / 0.01f) * Mathf.Clamp01((seconds - t) / 0.08f);
            float buzz = Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * 120f)) * 0.25f + Mathf.Sin(t * 2f * Mathf.PI * 360f) * 0.15f;
            float hiss = Noise(rnd) * (0.35f + 0.35f * Mathf.Abs(Mathf.Sin(t * 2f * Mathf.PI * 60f)));
            float crack = rnd.NextDouble() < 0.004 ? Noise(rnd) * 0.6f : 0f;
            return (buzz + hiss + crack) * env * 0.8f;
        });
    }
}
