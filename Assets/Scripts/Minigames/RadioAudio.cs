using UnityEngine;

/// <summary>
/// Som do radio: estatica em loop, clique de encaixe e a voz do patrao.
/// Tudo e gerado por codigo, entao funciona sem nenhum arquivo de audio.
/// Para usar uma voz de verdade, arraste um clipe em bossVoice.
/// O som e 3D: a estatica continua audivel perto do radio mesmo fora do minigame.
/// </summary>
public class RadioAudio : MonoBehaviour
{
    const int Rate = 22050;

    [Tooltip("Opcional. Se vazio, toca uma fala sintetica de placeholder.")]
    [SerializeField] AudioClip bossVoice;
    [Tooltip("Opcional. Estatica em loop (o volume segue a sintonia). Vazio = ruido sintetico.")]
    [SerializeField] AudioClip staticClip;
    [Tooltip("Opcional. Clique de encaixe. Vazio = clique sintetico.")]
    [SerializeField] AudioClip clickClip;
    [SerializeField] float maxDistance = 8f;
    [SerializeField, Range(0f, 1f)] float staticVolume = 0.6f;

    AudioSource hiss, sfx;
    AudioClip click, voice;

    void Awake()
    {
        hiss = NewSource();
        hiss.clip = staticClip != null ? staticClip : MakeNoise(2f);
        hiss.loop = true;
        sfx = NewSource();
        click = clickClip != null ? clickClip : MakeClick();
        voice = bossVoice != null ? bossVoice : MakeVoice(2.6f);
    }

    AudioSource NewSource()
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 0.5f;
        s.maxDistance = maxDistance;
        return s;
    }

    /// <summary>level 0 = sinal limpo (estatica baixa), 1 = estatica maxima. Negativo desliga.</summary>
    public void SetStatic(float level)
    {
        if (level < 0f)
        {
            if (hiss.isPlaying) hiss.Stop();
            return;
        }
        if (!hiss.isPlaying) hiss.Play();
        hiss.volume = staticVolume * Mathf.Lerp(0.04f, 1f, level);
    }

    public void Click() => sfx.PlayOneShot(click, 0.8f);

    /// <summary>Toca a fala do patrao. Devolve a duracao em segundos.</summary>
    public float PlayBoss()
    {
        sfx.PlayOneShot(voice, 1f);
        return voice.length;
    }

    static AudioClip Build(string name, float seconds, System.Func<int, float, float> sample)
    {
        var data = new float[Mathf.CeilToInt(seconds * Rate)];
        for (int i = 0; i < data.Length; i++) data[i] = sample(i, i / (float)Rate);
        var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip MakeNoise(float seconds)
    {
        var rnd = new System.Random(7);
        float last = 0f;
        return Build("RadioStatic", seconds, (i, t) =>
        {
            float white = (float)(rnd.NextDouble() * 2.0 - 1.0);
            last = Mathf.Lerp(last, white, 0.6f);
            return last * 0.7f;
        });
    }

    static AudioClip MakeClick()
    {
        var rnd = new System.Random(3);
        return Build("RadioClick", 0.06f, (i, t) => (float)(rnd.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 90f) * 0.8f);
    }

    /// <summary>Murmurio de voz grave com chiado, em silabas. So um placeholder.</summary>
    static AudioClip MakeVoice(float seconds)
    {
        var rnd = new System.Random(11);
        const int syllables = 9;
        float phase = 0f;
        return Build("RadioBossVoice", seconds, (i, t) =>
        {
            float syl = t / seconds * syllables;
            float local = syl - Mathf.Floor(syl);
            float env = Mathf.Sin(local * Mathf.PI) * (0.6f + 0.4f * Mathf.Sin(Mathf.Floor(syl) * 2.3f));
            float pitch = 105f + 25f * Mathf.Sin(Mathf.Floor(syl) * 1.7f) + 10f * (1f - local);
            phase += pitch / Rate;
            float v = 0f;
            for (int h = 1; h <= 6; h++) v += Mathf.Sin(2f * Mathf.PI * phase * h) / h;
            float hiss = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.25f;
            return Mathf.Clamp((v * 0.35f * env) + hiss * 0.5f, -1f, 1f) * 0.8f;
        });
    }
}
