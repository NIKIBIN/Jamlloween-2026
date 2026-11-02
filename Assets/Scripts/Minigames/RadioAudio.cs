using UnityEngine;

/// <summary>
/// Som do radio, todo sintetizado (nao ha clipes no projeto): estatica, musica de carrossel distorcida,
/// voz do patrao e cliques. A estatica e o carrossel sao somados em OnAudioFilterRead; clipes de verdade
/// podem substituir a voz e o carrossel (bossVoice / carouselClip).
/// O som sai do proprio radio (3D), entao continua audivel depois que o jogador sai do minigame.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class RadioAudio : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] float spatialBlend = 0.65f;
    [SerializeField] float minDistance = 0.4f;
    [SerializeField] float maxDistance = 10f;

    AudioSource source, carouselSource;
    AudioClip bossClip, carouselClip, mumble, click, burst;
    int sampleRate;

    // Lidos na thread de audio
    volatile float staticLevel, carouselLevel;
    uint rng = 2463534242u;
    float lowpass, crackle, carouselLow;
    double phase;
    float wowPhase, vibPhase;
    int noteIndex, noteSamples;

    static readonly float[] Melody =
    {
        523.25f, 659.25f, 783.99f, 659.25f, 523.25f, 392.00f, 440.00f, 523.25f,
        587.33f, 493.88f, 392.00f, 493.88f, 523.25f, 0f
    };
    const float NoteSeconds = 0.21f;

    public void Configure(AudioClip bossVoice, AudioClip carousel)
    {
        bossClip = bossVoice;
        carouselClip = carousel;
        if (carouselClip != null && carouselSource == null)
        {
            carouselSource = gameObject.AddComponent<AudioSource>();
            Setup3D(carouselSource);
            carouselSource.clip = carouselClip;
            carouselSource.loop = true;
            carouselSource.volume = 0f;
            carouselSource.Play();
        }
    }

    void Awake()
    {
        sampleRate = AudioSettings.outputSampleRate;
        source = GetComponent<AudioSource>();
        Setup3D(source);
        var bed = AudioClip.Create("radio-bed", sampleRate, 1, sampleRate, false);
        bed.SetData(new float[sampleRate], 0);   // silencio em loop: so existe para o filtro rodar
        source.clip = bed;
        source.loop = true;
        source.Play();
    }

    void Setup3D(AudioSource s)
    {
        s.playOnAwake = false;
        s.spatialBlend = spatialBlend;
        s.minDistance = minDistance;
        s.maxDistance = maxDistance;
        s.rolloffMode = AudioRolloffMode.Linear;
    }

    /// <summary>Niveis finais (ja com volume) da estatica e do carrossel.</summary>
    public void SetLevels(float staticAmount, float carouselAmount)
    {
        staticLevel = staticAmount;
        if (carouselClip != null)
        {
            carouselLevel = 0f;
            if (carouselSource != null) carouselSource.volume = carouselAmount;
        }
        else carouselLevel = carouselAmount;
    }

    /// <summary>Toca a voz do patrao. Devolve a duracao.</summary>
    public float PlayBoss(float volume)
    {
        var clip = bossClip != null ? bossClip : (mumble = mumble != null ? mumble : MakeMumble(sampleRate));
        source.PlayOneShot(clip, volume);
        return clip.length;
    }

    public void PlayClick(float volume = 1f)
    {
        if (click == null) click = MakeClick(sampleRate);
        source.PlayOneShot(click, volume);
    }

    /// <summary>Rajada de ruido (squelch) ao soltar o botao sem ninguem do outro lado.</summary>
    public void PlayBurst(float volume = 1f)
    {
        if (burst == null) burst = MakeBurst(sampleRate);
        source.PlayOneShot(burst, volume);
    }

    // ---------- Sintese em tempo real ----------

    float Noise()
    {
        rng ^= rng << 13; rng ^= rng >> 17; rng ^= rng << 5;
        return rng / (float)uint.MaxValue * 2f - 1f;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        float st = staticLevel, car = carouselLevel;
        if (st <= 0.0005f && car <= 0.0005f) return;
        int noteLen = (int)(sampleRate * NoteSeconds);

        for (int i = 0; i < data.Length; i += channels)
        {
            float mix = 0f;

            if (st > 0.0005f)
            {
                float n = Noise();
                if ((rng & 0x1FFF) == 0) crackle = 1f;
                crackle *= 0.994f;
                n += crackle * Noise() * 2f;
                lowpass += (n - lowpass) * 0.55f;
                mix += lowpass * st * 0.35f;
            }

            if (car > 0.0005f)
            {
                float f = Melody[noteIndex];
                float t = noteSamples / (float)noteLen;
                float s = 0f;
                if (f > 0f)
                {
                    float wow = 1f + 0.07f * Mathf.Sin(wowPhase * 6.2832f);
                    float vib = 1f + 0.025f * Mathf.Sin(vibPhase * 6.2832f);
                    phase += f * wow * vib / sampleRate;
                    phase -= System.Math.Floor(phase);
                    float square = phase < 0.5 ? 1f : -1f;
                    float saw = (float)(2.0 * phase - 1.0);
                    s = (square * 0.55f + saw * 0.45f) * Mathf.Exp(-3.5f * t);
                    s = Mathf.Round(s * 6f) / 6f;                 // bitcrush
                    carouselLow += (s - carouselLow) * 0.3f;      // abafa
                    s = Mathf.Clamp(carouselLow * 2.2f, -1f, 1f); // satura
                }
                wowPhase += 0.45f / sampleRate;
                vibPhase += 5.5f / sampleRate;
                if (++noteSamples >= noteLen) { noteSamples = 0; noteIndex = (noteIndex + 1) % Melody.Length; }
                mix += s * car * 0.28f;
            }

            for (int c = 0; c < channels; c++) data[i + c] += mix;
        }
    }

    // ---------- Clipes gerados ----------

    static AudioClip MakeClick(int sr)
    {
        int n = (int)(sr * 0.06f);
        var d = new float[n];
        var r = new System.Random(3);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sr;
            float noise = (float)(r.NextDouble() * 2 - 1) * Mathf.Exp(-t * 120f);
            float thump = Mathf.Sin(2f * Mathf.PI * 190f * t) * Mathf.Exp(-t * 70f);
            d[i] = (noise * 0.6f + thump * 0.7f) * 0.8f;
        }
        var c = AudioClip.Create("radio-click", n, 1, sr, false);
        c.SetData(d, 0);
        return c;
    }

    static AudioClip MakeBurst(int sr)
    {
        int n = (int)(sr * 0.3f);
        var d = new float[n];
        var r = new System.Random(5);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sr;
            d[i] = (float)(r.NextDouble() * 2 - 1) * Mathf.Exp(-t * 14f) * 0.5f;
        }
        var c = AudioClip.Create("radio-burst", n, 1, sr, false);
        c.SetData(d, 0);
        return c;
    }

    /// <summary>Placeholder da voz do patrao: silabas com formantes variando, filtradas como radio. Troque por um clipe real.</summary>
    static AudioClip MakeMumble(int sr)
    {
        int total = (int)(sr * 2.6f);
        var d = new float[total];
        var rnd = new System.Random(11);
        int pos = 0;
        var gains = new float[28];
        while (pos < total)
        {
            int len = (int)(sr * (0.13f + 0.12f * (float)rnd.NextDouble()));
            bool pause = rnd.NextDouble() < 0.16;
            float pitch = 100f * (0.9f + 0.35f * (float)rnd.NextDouble());
            float f1 = 320f + 480f * (float)rnd.NextDouble();
            float f2 = 900f + 1500f * (float)rnd.NextDouble();
            for (int h = 1; h < gains.Length; h++)
            {
                float fh = pitch * h;
                float a = (fh - f1) / 170f, b = (fh - f2) / 260f;
                gains[h] = (Mathf.Exp(-a * a) + 0.7f * Mathf.Exp(-b * b) + 0.04f) / Mathf.Sqrt(h);
            }
            for (int i = 0; i < len && pos + i < total; i++)
            {
                float u = i / (float)len;
                float env = pause ? 0f : Mathf.Sin(Mathf.PI * u);
                double time = (pos + i) / (double)sr;
                float s = 0f;
                for (int h = 1; h < gains.Length; h++)
                    s += gains[h] * (float)System.Math.Sin(2.0 * System.Math.PI * pitch * h * time);
                d[pos + i] = s * env * 0.09f;
            }
            pos += len;
        }
        // cara de radio: reduz a resolucao e passa um ruido por cima
        for (int i = 0; i < total; i++)
        {
            float s = Mathf.Round(d[i] * 14f) / 14f;
            d[i] = Mathf.Clamp(s + ((float)rnd.NextDouble() - 0.5f) * 0.03f, -1f, 1f);
        }
        var c = AudioClip.Create("boss-placeholder", total, 1, sr, false);
        c.SetData(d, 0);
        return c;
    }
}
