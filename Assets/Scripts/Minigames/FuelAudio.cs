using UnityEngine;

/// <summary>
/// Sons de gasolina e de motor gerados em codigo. Sao o padrao (como em CarouselAudio e ProceduralAudio):
/// quem tem um clipe de verdade o arrasta no Inspector e estes nao sao usados.
/// </summary>
public static class FuelAudio
{
    const int Rate = 22050;

    static AudioClip Make(string name, float seconds, System.Func<float, float> f)
    {
        int n = Mathf.CeilToInt(seconds * Rate);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
        var clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>Liquido balancando dentro do galao: ruido grave com ondulacao.</summary>
    public static AudioClip Slosh(int seed = 3)
    {
        var rng = new System.Random(seed);
        float lp = 0f;
        return Make("gas_slosh", 0.45f, t =>
        {
            float n = (float)rng.NextDouble() * 2f - 1f;
            lp += (n - lp) * 0.07f;
            float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.45f));
            float wave = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * (5f + seed) * t);
            return lp * 2.2f * env * wave;
        });
    }

    /// <summary>Gasolina caindo no funil e no tanque: gorgolejo continuo (loop).</summary>
    public static AudioClip PourLoop()
    {
        var rng = new System.Random(11);
        float lp = 0f;
        return Make("gas_pour", 1.2f, t =>
        {
            float n = (float)rng.NextDouble() * 2f - 1f;
            lp += (n - lp) * 0.12f;
            float bubble = Mathf.Sin(2f * Mathf.PI * (180f + 90f * Mathf.Sin(t * 23f)) * t);
            float fade = Mathf.Min(1f, t / 0.05f) * Mathf.Min(1f, (1.2f - t) / 0.05f);
            return (lp * 1.4f + bubble * 0.15f) * 0.6f * fade;
        });
    }

    /// <summary>Gasolina escorrendo no chao: chiado mais agudo e espalhado (loop).</summary>
    public static AudioClip SpillLoop()
    {
        var rng = new System.Random(17);
        float lp = 0f;
        return Make("gas_spill", 1.0f, t =>
        {
            float n = (float)rng.NextDouble() * 2f - 1f;
            lp += (n - lp) * 0.35f;
            float fade = Mathf.Min(1f, t / 0.05f) * Mathf.Min(1f, (1f - t) / 0.05f);
            return lp * 0.5f * fade * (0.7f + 0.3f * Mathf.Sin(t * 40f));
        });
    }

    /// <summary>Estalo seco da tampa ou do funil encaixando.</summary>
    public static AudioClip Click()
    {
        return Make("gen_click", 0.12f, t => Mathf.Sin(2f * Mathf.PI * 900f * t) * Mathf.Exp(-t * 55f) * 0.5f);
    }

    /// <summary>Puxada da corda: raspar rapido.</summary>
    public static AudioClip CordPull()
    {
        var rng = new System.Random(5);
        float hp = 0f, prev = 0f;
        return Make("gen_cord", 0.4f, t =>
        {
            float n = (float)rng.NextDouble() * 2f - 1f;
            hp = 0.85f * (hp + n - prev); prev = n;
            float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.4f));
            return hp * 0.3f * env * (0.6f + 0.4f * Mathf.Sin(t * 120f));
        });
    }

    /// <summary>Motor engasgando. longer = engasga mais tempo e quase pega.</summary>
    public static AudioClip Sputter(bool longer)
    {
        float len = longer ? 2.4f : 1.2f;
        var rng = new System.Random(longer ? 9 : 2);
        float lp = 0f;
        return Make(longer ? "gen_sputter_long" : "gen_sputter", len, t =>
        {
            // pulsos de explosao que ficam mais rapidos e mais fracos (ou, no longo, quase engatam)
            float rate = longer ? 6f + 9f * (t / len) : 5f + 2f * (t / len);
            float phase = (t * rate) % 1f;
            float pulse = Mathf.Exp(-phase * 9f);
            float n = (float)rng.NextDouble() * 2f - 1f;
            lp += (n - lp) * 0.15f;
            float body = Mathf.Sin(2f * Mathf.PI * (55f + 25f * pulse) * t) * 0.8f + lp * 0.9f;
            float level = longer ? Mathf.Lerp(0.5f, 0.9f, t / len) : Mathf.Lerp(0.8f, 0.2f, t / len);
            return body * pulse * level;
        });
    }

    /// <summary>Motor funcionando (loop).</summary>
    public static AudioClip EngineLoop()
    {
        var rng = new System.Random(23);
        float lp = 0f;
        return Make("gen_run", 1.0f, t =>
        {
            float n = (float)rng.NextDouble() * 2f - 1f;
            lp += (n - lp) * 0.1f;
            float f = 48f;
            float tone = Mathf.Sin(2f * Mathf.PI * f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * f * 2f * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * f * 3f * t);
            return (tone * 0.3f + lp * 0.35f) * (0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 12f * t));
        });
    }
}
