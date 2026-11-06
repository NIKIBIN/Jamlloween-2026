using UnityEngine;

/// <summary>
/// Som do conserto do carrossel: clique de encaixe, rangido alto, motor forcando, estouro da correia,
/// zumbido do motor e a musica distorcida do carrossel. Tudo e gerado por codigo, entao funciona
/// sem nenhum arquivo de audio. Para usar sons de verdade, arraste clipes nos campos opcionais.
/// O som e 3D: o carrossel e ouvido de longe.
/// </summary>
public class CarouselAudio : MonoBehaviour
{
    const int Rate = 22050;

    [Header("Clipes opcionais (vazio = som sintetico)")]
    [SerializeField] AudioClip creakClip;
    [SerializeField] AudioClip musicClip;
    [SerializeField] AudioClip clickClip;
    [Tooltip("Motor forcando contra as engrenagens travadas.")]
    [SerializeField] AudioClip strainClip;
    [Tooltip("Correia estourando.")]
    [SerializeField] AudioClip snapClip;
    [Tooltip("Zumbido do motor (loop).")]
    [SerializeField] AudioClip humClip;

    [Header("Volumes")]
    [SerializeField] float maxDistance = 30f;
    [SerializeField, Range(0f, 1f)] float sfxVolume = 0.9f;
    [SerializeField, Range(0f, 1f)] float creakVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] float humVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] float musicVolume = 0.7f;

    [Header("Musica distorcida")]
    [Tooltip("Quanto o tom oscila para baixo e para cima (0 = afinado).")]
    [SerializeField, Range(0f, 0.3f)] float musicPitchWobble = 0.1f;
    [Tooltip("Opcional: fonte própria da música (num filho, com filtros e falhas). Quem controla o tom é ela; sem isto a música usa uma fonte criada aqui.")]
    [SerializeField] AudioSource musicSource;

    AudioSource sfx, hum, music;
    AudioClip click, creak, strain, snap, humLoop, musicLoop;
    bool musicOn;

    void Awake()
    {
        sfx = NewSource(false);
        hum = NewSource(true);
        music = musicSource != null ? musicSource : NewSource(true);
        click = clickClip != null ? clickClip : MakeClick();
        creak = creakClip != null ? creakClip : MakeCreak();
        strain = strainClip != null ? strainClip : MakeStrain();
        snap = snapClip != null ? snapClip : MakeSnap();
        humLoop = humClip != null ? humClip : MakeHum();
        hum.clip = humLoop;
        hum.volume = humVolume;
    }

    AudioSource NewSource(bool loop)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 1f;
        s.maxDistance = maxDistance;
        return s;
    }

    void Update()
    {
        if (!musicOn || musicSource != null) return;   // a fonte própria cuida do tom
        float t = Time.time;
        music.pitch = 1f - musicPitchWobble * 0.5f + musicPitchWobble * (0.6f * Mathf.Sin(t * 1.3f) + 0.4f * Mathf.Sin(t * 4.1f + 1f));
    }

    public void Click() => sfx.PlayOneShot(click, sfxVolume * 0.8f);

    /// <summary>Rangido de metal contra metal. Devolve a duracao em segundos.</summary>
    public float Creak()
    {
        sfx.PlayOneShot(creak, creakVolume);
        return creak.length;
    }

    /// <summary>O motor forca contra as engrenagens travadas.</summary>
    public float Strain()
    {
        sfx.PlayOneShot(strain, sfxVolume);
        return strain.length;
    }

    /// <summary>A correia estoura e salta.</summary>
    public void Snap() => sfx.PlayOneShot(snap, sfxVolume);

    public void SetMotor(bool on)
    {
        if (on && !hum.isPlaying) hum.Play();
        else if (!on && hum.isPlaying) hum.Stop();
    }

    public void SetMusic(bool on)
    {
        if (on == musicOn) return;
        musicOn = on;
        if (!on) { music.Stop(); return; }
        if (musicLoop == null) musicLoop = musicClip != null ? musicClip : MakeMusic();
        music.clip = musicLoop;
        music.volume = musicVolume;
        music.Play();
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
        return Build("CarouselClick", 0.08f, (i, t) => Noise(rnd) * Mathf.Exp(-t * 70f) * 0.8f + Mathf.Sin(t * 2f * Mathf.PI * 900f) * Mathf.Exp(-t * 90f) * 0.4f);
    }

    /// <summary>Metal raspando de leve: ruido suavizado com um tom baixo que cai, curto e sem saturacao.</summary>
    static AudioClip MakeCreak()
    {
        var rnd = new System.Random(21);
        const float len = 0.7f;
        float low = 0f;
        return Build("CarouselCreak", len, (i, t) =>
        {
            low = Mathf.Lerp(low, Noise(rnd), 0.12f);                                     // passa-baixa: tira o chiado agudo
            float scrape = low * (0.6f + 0.4f * Mathf.Sin(t * 2f * Mathf.PI * 14f));     // vai e volta, como metal arrastando
            float tone = Mathf.Sin(t * 2f * Mathf.PI * (330f - 90f * t / len)) * 0.15f;
            float env = Mathf.Clamp01(t / 0.08f) * Mathf.Clamp01((len - t) / 0.3f);
            return (scrape * 0.8f + tone) * env * 0.5f;
        });
    }

    /// <summary>Motor forcando: gemido que sobe de tom misturado com batidas das engrenagens.</summary>
    static AudioClip MakeStrain()
    {
        var rnd = new System.Random(5);
        const float len = 1.3f;
        float phase = 0f;
        return Build("CarouselStrain", len, (i, t) =>
        {
            float k = t / len;
            phase += Mathf.Lerp(110f, 430f, k * k) / Rate;
            float saw = 2f * (phase - Mathf.Floor(phase)) - 1f;
            float knock = Mathf.Exp(-Mathf.Repeat(t, 0.11f) * 60f) * Noise(rnd);
            float env = Mathf.Clamp01(t / 0.1f) * Mathf.Clamp01((len - t) / 0.08f);
            return (float)System.Math.Tanh((saw * 0.5f + knock * 0.8f) * 2f) * env * 0.8f;
        });
    }

    static AudioClip MakeSnap()
    {
        var rnd = new System.Random(8);
        return Build("CarouselSnap", 0.6f, (i, t) =>
            Noise(rnd) * Mathf.Exp(-t * 16f) * 0.9f + Mathf.Sin(t * 2f * Mathf.PI * 170f) * Mathf.Exp(-t * 9f) * 0.7f + Mathf.Sin(t * 2f * Mathf.PI * 1300f) * Mathf.Exp(-t * 25f) * 0.3f);
    }

    static AudioClip MakeHum()
    {
        return Build("CarouselHum", 1f, (i, t) =>
            (Mathf.Sin(t * 2f * Mathf.PI * 50f) * 0.5f + Mathf.Sin(t * 2f * Mathf.PI * 100f) * 0.3f + Mathf.Sin(t * 2f * Mathf.PI * 150f) * 0.15f + Mathf.Sin(t * 2f * Mathf.PI * 7f) * 0.1f) * 0.6f);
    }

    /// <summary>
    /// Valsa de caixa de musica em la menor, torta: cada nota sai desafinada, o tempo oscila (wow/flutter)
    /// e o sinal e esmagado em poucos bits. Dura 8 compassos, o fim e silencio para emendar no loop.
    /// </summary>
    static AudioClip MakeMusic()
    {
        int[] melody =
        {
            12, 15, 19,   17, 15, 12,   14, 17, 21,   19, 17, 14,
            12, 16, 19,   21, 19, 16,   15, 14, 12,   7, -99, -99,
        };
        int[] bass = { -12, -12, -7, -7, -12, -12, -5, -12 };
        const float beat = 0.38f;
        int beats = melody.Length;
        float length = beats * beat;
        var rnd = new System.Random(77);
        var detune = new float[beats];
        for (int n = 0; n < beats; n++) detune[n] = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.4f;   // semitons
        var crackle = new System.Random(4);

        float Freq(float semi) => 220f * Mathf.Pow(2f, semi / 12f);
        float Box(float f, float tt) => (Mathf.Sin(2f * Mathf.PI * f * tt) + 0.5f * Mathf.Sin(4f * Mathf.PI * f * tt) + 0.25f * Mathf.Sin(6f * Mathf.PI * f * tt * 1.01f)) * Mathf.Exp(-tt * 4.2f);

        return Build("CarouselMusic", length, (i, t) =>
        {
            float tw = t + 0.014f * Mathf.Sin(2f * Mathf.PI * (8f / length) * t) + 0.004f * Mathf.Sin(2f * Mathf.PI * (37f / length) * t);   // wow/flutter, periodico no loop
            int b = Mathf.FloorToInt(tw / beat);
            float v = 0f;
            for (int k = b; k >= b - 3 && k >= 0; k--)
            {
                int n = k % beats;
                float tt = tw - k * beat;
                int semi = melody[n];
                if (semi > -90) v += Box(Freq(semi + detune[n]), tt) * 0.5f;
                int bar = n / 3, step = n % 3;
                if (step == 0) v += Box(Freq(bass[bar]), tt) * 0.7f;
                else v += Box(Freq(bass[bar] + 15f + detune[n] * 0.5f), tt) * 0.28f;
            }
            const float levels = 14f;
            v = Mathf.Round(v * 0.55f * levels) / levels;                      // saturacao em poucos bits
            v += (float)(crackle.NextDouble() * 2.0 - 1.0) * 0.015f;            // chiado de fita
            return Mathf.Clamp(v, -1f, 1f);
        });
    }
}
