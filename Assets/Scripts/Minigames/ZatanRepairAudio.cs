using UnityEngine;

/// <summary>
/// Som do conserto do Zatan: compressor com respiracao em loop e cliques de solenoide, clunk da chave,
/// rangido da manivela, vazamento de piche, esfregar e o suspiro final. Tudo e gerado por codigo, entao
/// funciona sem arquivos de audio. Para sons de verdade, arraste clipes nos campos opcionais.
/// O som e 3D e segue o estado do conserto mesmo com o jogador fora do minigame.
/// </summary>
public class ZatanRepairAudio : MonoBehaviour
{
    const int Rate = 22050;

    [Header("Clipes opcionais (vazio = som sintetico)")]
    [Tooltip("Loop de respiracao: chiado de ar, pausa, chiado.")]
    [SerializeField] AudioClip breathClip;
    [Tooltip("Clique curto do solenoide.")]
    [SerializeField] AudioClip solenoidClip;
    [Tooltip("Batida pesada da chave geral.")]
    [SerializeField] AudioClip clunkClip;
    [Tooltip("Chiado longo do compressor ao chegar no zero.")]
    [SerializeField] AudioClip sighClip;
    [Tooltip("Rangido da manivela (toca em loop enquanto se gira).")]
    [SerializeField] AudioClip creakClip;
    [Tooltip("Vazamento de gas (chiado em loop enquanto uma junta vaza).")]
    [SerializeField] AudioClip leakClip;

    [Header("Volumes")]
    // Ambiente sutil: o painel soa como uma maquina velha ao fundo, nao como um trem. Suba com cuidado.
    [SerializeField] float maxDistance = 10f;
    [SerializeField, Range(0f, 1f)] float breathVolume = 0.14f;
    [SerializeField, Range(0f, 1f)] float solenoidVolume = 0.1f;
    [SerializeField, Range(0f, 1f)] float humVolume = 0.06f;
    [SerializeField, Range(0f, 1f)] float clunkVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] float sighVolume = 0.4f;
    [SerializeField, Range(0f, 1f)] float effortVolume = 0.28f;
    [SerializeField, Range(0f, 1f)] float leakVolume = 0.2f;

    [Header("Ritmo")]
    [Tooltip("Intervalo (s) entre cliques de solenoide no comeco e no fim do giro.")]
    [SerializeField] Vector2 solenoidInterval = new Vector2(1.6f, 0.55f);
    [Tooltip("Velocidade da respiracao (pitch) no comeco e no fim do giro.")]
    [SerializeField] Vector2 breathPitch = new Vector2(0.9f, 1.2f);

    AudioSource sfx, breath, hum, effort, leak, scrub, tape;
    AudioClip click, clunk, sigh, solenoid;
    bool running;
    float speed01, solenoidTimer, solenoidScale = 1f;

    void Awake()
    {
        sfx = NewSource(false);
        breath = NewSource(true);
        hum = NewSource(true);
        effort = NewSource(true);
        leak = NewSource(true);
        scrub = NewSource(true);
        tape = NewSource(true);

        click = MakeClick();
        solenoid = solenoidClip != null ? solenoidClip : MakeSolenoid();
        clunk = clunkClip != null ? clunkClip : MakeClunk();
        sigh = sighClip != null ? sighClip : MakeSigh();
        breath.clip = breathClip != null ? breathClip : MakeBreath();
        hum.clip = MakeHum();
        effort.clip = creakClip != null ? creakClip : MakeCreakLoop();
        leak.clip = leakClip != null ? leakClip : MakeLeakLoop();
        scrub.clip = MakeScrubLoop();
        tape.clip = scrub.clip;
    }

    AudioSource NewSource(bool loop)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 0.5f;
        s.maxDistance = maxDistance;
        return s;
    }

    void Update()
    {
        if (!running) return;
        solenoidTimer -= Time.deltaTime;
        if (solenoidTimer > 0f) return;
        solenoidTimer = Mathf.Lerp(solenoidInterval.x, solenoidInterval.y, speed01) * Random.Range(0.8f, 1.2f);
        sfx.pitch = 1f;
        sfx.PlayOneShot(solenoid, solenoidVolume * solenoidScale * Random.Range(0.7f, 1f));
    }

    /// <summary>Fase (0 a 1) do ciclo de respiracao. Serve para sincronizar pistao e vapor com o chiado.</summary>
    public float BreathPhase01 => running && breath.clip != null && breath.isPlaying ? breath.time / breath.clip.length : 0f;

    /// <summary>Quanto ar esta saindo agora (0 a 1), seguindo o desenho do chiado: um forte, pausa, um curto, pausa.</summary>
    public float BreathEnvelope
    {
        get
        {
            if (!running) return 0f;
            float t = BreathPhase01 * 1.6f;
            if (t < 0.6f) return Mathf.Pow(Mathf.Sin(t / 0.6f * Mathf.PI), 1.5f);
            if (t >= 0.9f && t < 1.4f) return 0.7f * Mathf.Pow(Mathf.Sin((t - 0.9f) / 0.5f * Mathf.PI), 1.5f);
            return 0f;
        }
    }

    // =================================================================
    // Controle
    // =================================================================

    public void Click() => sfx.PlayOneShot(click, 0.8f);

    public void Clunk(float volume = 1f) => sfx.PlayOneShot(clunk, clunkVolume * volume);

    /// <summary>Liga/desliga o compressor. speed01 (0 a 1) acelera a respiracao e os cliques.</summary>
    public void SetCompressor(bool on, float speed01 = 0f, float volumeScale = 1f)
    {
        this.speed01 = Mathf.Clamp01(speed01);
        solenoidScale = volumeScale;
        if (on != running)
        {
            running = on;
            solenoidTimer = 0.2f;
            if (on) { breath.Play(); hum.Play(); }
            else { breath.Stop(); hum.Stop(); }
        }
        if (!on) return;
        breath.volume = breathVolume * volumeScale;
        breath.pitch = Mathf.Lerp(breathPitch.x, breathPitch.y, this.speed01);
        hum.volume = humVolume * volumeScale * Mathf.Lerp(0.8f, 1.2f, this.speed01);
        hum.pitch = Mathf.Lerp(1f, 1.15f, this.speed01);
    }

    /// <summary>Esforco da manivela. level 0 desliga; pitch01 0..1 deixa o rangido mais grave quando fica pesado.</summary>
    public void SetEffort(float level, float heavy01)
    {
        Loop(effort, level, effortVolume * Mathf.Lerp(0.6f, 1f, heavy01), Mathf.Lerp(1.1f, 0.7f, heavy01));
    }

    /// <summary>Vazamento de gas. level 0 desliga.</summary>
    public void SetLeak(float level) => Loop(leak, level, leakVolume, 1f);

    /// <summary>Estopa raspando. level 0 desliga.</summary>
    public void SetScrub(float level) => Loop(scrub, level, 0.25f, Mathf.Lerp(0.9f, 1.2f, Mathf.Clamp01(level)));

    /// <summary>Fita sendo esticada sobre a junta (o mesmo loop do esfregar, mais agudo e chiado).</summary>
    public void SetTape(float level) => Loop(tape, level, 0.25f, Mathf.Lerp(1.5f, 1.9f, Mathf.Clamp01(level)));

    static void Loop(AudioSource s, float level, float maxVolume, float pitch)
    {
        if (level <= 0.01f)
        {
            if (s.isPlaying) s.Stop();
            return;
        }
        if (!s.isPlaying) s.Play();
        s.volume = maxVolume * Mathf.Clamp01(level);
        s.pitch = pitch;
    }

    /// <summary>Para tudo que esta em loop (usado no fim, antes do silencio).</summary>
    public void StopAll()
    {
        SetCompressor(false);
        SetEffort(0f, 0f);
        SetLeak(0f);
        SetScrub(0f);
        SetTape(0f);
    }

    /// <summary>Silencio total: para os loops e corta qualquer som avulso que ainda esteja tocando (queda de energia).</summary>
    public void Silence()
    {
        StopAll();
        sfx.Stop();
    }

    /// <summary>O compressor solta o ar de uma vez. Devolve a duracao em segundos.</summary>
    public float Sigh()
    {
        sfx.pitch = 1f;
        sfx.PlayOneShot(sigh, sighVolume);
        return sigh.length;
    }

    // =================================================================
    // Sintese
    // =================================================================

    static AudioClip Build(string name, float seconds, System.Func<int, float, float> sample)
    {
        var data = new float[Mathf.CeilToInt(seconds * Rate)];
        for (int i = 0; i < data.Length; i++) data[i] = sample(i, i / (float)Rate);
        var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float Noise(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);

    static AudioClip MakeClick()
    {
        var rnd = new System.Random(3);
        return Build("ZatanClick", 0.06f, (i, t) => Noise(rnd) * Mathf.Exp(-t * 90f) * 0.8f);
    }

    static AudioClip MakeSolenoid()
    {
        var rnd = new System.Random(14);
        return Build("ZatanSolenoid", 0.09f, (i, t) =>
            Noise(rnd) * Mathf.Exp(-t * 110f) * 0.5f + Mathf.Sin(t * 2f * Mathf.PI * 150f) * Mathf.Exp(-t * 60f) * 0.6f + Mathf.Sin(t * 2f * Mathf.PI * 2300f) * Mathf.Exp(-t * 150f) * 0.25f);
    }

    static AudioClip MakeClunk()
    {
        var rnd = new System.Random(9);
        return Build("ZatanClunk", 0.7f, (i, t) =>
            Mathf.Sin(t * 2f * Mathf.PI * 62f) * Mathf.Exp(-t * 8f) * 0.9f
            + Noise(rnd) * Mathf.Exp(-t * 45f) * 0.7f
            + Mathf.Sin(t * 2f * Mathf.PI * 190f) * Mathf.Exp(-t * 13f) * 0.4f
            + Mathf.Sin(t * 2f * Mathf.PI * 410f) * Mathf.Exp(-t * 20f) * 0.2f);
    }

    /// <summary>Ciclo de 1.6 s: chiado, pausa, chiado curto, pausa. Comeca e termina em silencio, entao emenda no loop.</summary>
    static AudioClip MakeBreath()
    {
        var rnd = new System.Random(31);
        float lp = 0f;
        return Build("ZatanBreath", 1.6f, (i, t) =>
        {
            float env = 0f;
            if (t < 0.6f) env = Mathf.Pow(Mathf.Sin(t / 0.6f * Mathf.PI), 1.5f);
            else if (t >= 0.9f && t < 1.4f) env = 0.7f * Mathf.Pow(Mathf.Sin((t - 0.9f) / 0.5f * Mathf.PI), 1.5f);
            lp = Mathf.Lerp(lp, Noise(rnd), 0.4f);
            return lp * env * 0.7f;
        });
    }

    static AudioClip MakeHum()
    {
        return Build("ZatanHum", 1f, (i, t) =>
            (Mathf.Sin(t * 2f * Mathf.PI * 50f) * 0.5f + Mathf.Sin(t * 2f * Mathf.PI * 100f) * 0.3f + Mathf.Sin(t * 2f * Mathf.PI * 25f) * 0.25f) * 0.6f);
    }

    static AudioClip MakeCreakLoop()
    {
        var rnd = new System.Random(21);
        float low = 0f;
        return Build("ZatanCreak", 1f, (i, t) =>
        {
            low = Mathf.Lerp(low, Noise(rnd), 0.1f);
            float scrape = low * (0.6f + 0.4f * Mathf.Sin(t * 2f * Mathf.PI * 7f));
            float tone = Mathf.Sin(t * 2f * Mathf.PI * 180f) * 0.12f * (0.5f + 0.5f * Mathf.Sin(t * 2f * Mathf.PI * 3f));
            return (scrape * 0.9f + tone) * 0.6f;
        });
    }

    /// <summary>Chiado de gas escapando: ruido claro (pouco filtrado) com uma oscilacao leve de pressao.</summary>
    static AudioClip MakeLeakLoop()
    {
        var rnd = new System.Random(52);
        float lp = 0f, prev = 0f;
        return Build("ZatanGasLeak", 1f, (i, t) =>
        {
            float n = Noise(rnd);
            lp = Mathf.Lerp(lp, n, 0.85f);
            float hi = lp - prev;   // realca os agudos: o chiado do gas
            prev = lp;
            float flutter = 0.8f + 0.2f * Mathf.Sin(t * 2f * Mathf.PI * 6f);
            return (hi * 0.9f + lp * 0.25f) * flutter * 0.55f;
        });
    }

    static AudioClip MakeScrubLoop()
    {
        var rnd = new System.Random(64);
        float lp = 0f;
        return Build("ZatanScrub", 1f, (i, t) =>
        {
            lp = Mathf.Lerp(lp, Noise(rnd), 0.25f);
            return lp * (0.55f + 0.45f * Mathf.Sin(t * 2f * Mathf.PI * 5f)) * 0.5f;
        });
    }

    /// <summary>Suspiro: ruido que abre rapido e vai se apagando, com o tom caindo, ate o silencio.</summary>
    static AudioClip MakeSigh()
    {
        var rnd = new System.Random(77);
        const float len = 3.4f;
        float lp = 0f, phase = 0f;
        return Build("ZatanSigh", len, (i, t) =>
        {
            float k = t / len;
            float env = Mathf.Clamp01(t / 0.12f) * Mathf.Pow(1f - k, 2.2f);
            lp = Mathf.Lerp(lp, Noise(rnd), Mathf.Lerp(0.55f, 0.08f, k));   // o chiado vai ficando grave
            phase += Mathf.Lerp(260f, 55f, k) / Rate;
            float whine = Mathf.Sin(phase * 2f * Mathf.PI) * 0.12f * (1f - k);
            return (lp * 0.8f + whine) * env;
        });
    }
}
