using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Realce compartilhado das pecas clicaveis dos minigames (extraido do ZatanRepairPart).
/// MaterialPropertyBlock nao aparece com o GPU Resident Drawer ligado, entao cada renderer realcado
/// ganha uma instancia propria do material (criada na primeira vez) e a cor-base volta ao desligar.
/// </summary>
public static class HighlightTint
{
    /// <summary>Cor de realce usada pelo gerador e pelos fusiveis: contrasta com o amarelo do gerador e o verde do quadro.</summary>
    public static readonly Color Contrast = new Color(0.4f, 0.9f, 1f);

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int KitColor = Shader.PropertyToID("_Color");   // shader PSX do kit (ex.: o galao)
    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    class Saved { public Material[] mats; public Color[] baseColors; public Color[] kitColors; public Color[] emission; public bool[] hadEmission; }
    static readonly Dictionary<Renderer, Saved> cache = new Dictionary<Renderer, Saved>();

    /// <summary>Realca (ou restaura) os renderers com a cor dada: puxa a cor-base para ela e soma emissao.</summary>
    public static void Tint(Renderer[] rs, bool on, Color color) => Tint(rs, on ? 1f : 0f, color);

    /// <summary>Realce com intensidade: 1 = o realce do hover, valores menores = mais suave, 0 = restaura.</summary>
    public static void Tint(Renderer[] rs, float amount, Color color)
    {
        bool on = amount > 0.001f;
        amount = Mathf.Clamp01(amount);
        if (rs == null) return;
        foreach (var r in rs)
        {
            if (r == null) continue;
            if (!cache.TryGetValue(r, out var s))
            {
                if (!on) continue;
                var mats = r.materials;   // instancias
                s = new Saved { mats = mats, baseColors = new Color[mats.Length], kitColors = new Color[mats.Length], emission = new Color[mats.Length], hadEmission = new bool[mats.Length] };
                for (int i = 0; i < mats.Length; i++)
                {
                    s.baseColors[i] = mats[i].HasProperty(BaseColor) ? mats[i].GetColor(BaseColor) : Color.white;
                    s.kitColors[i] = mats[i].HasProperty(KitColor) ? mats[i].GetColor(KitColor) : Color.white;
                    s.hadEmission[i] = mats[i].IsKeywordEnabled("_EMISSION");
                    s.emission[i] = mats[i].HasProperty(EmissionColor) ? mats[i].GetColor(EmissionColor) : Color.black;
                }
                cache[r] = s;
            }
            for (int i = 0; i < s.mats.Length; i++)
            {
                var m = s.mats[i];
                if (m == null) continue;
                if (on)
                {
                    if (m.HasProperty(BaseColor))
                        m.SetColor(BaseColor, Color.Lerp(s.baseColors[i], new Color(color.r, color.g, color.b, s.baseColors[i].a), 0.55f * amount));
                    else if (m.HasProperty(KitColor))
                    {
                        // _Color multiplica a textura e o shader nao tem emissao: so clarear aparece
                        var full = new Color(1.2f + color.r * 1.2f, 1.2f + color.g * 1.2f, 1.2f + color.b * 1.2f, s.kitColors[i].a);
                        m.SetColor(KitColor, Color.Lerp(s.kitColors[i], full, amount));
                    }
                    m.EnableKeyword("_EMISSION");
                    m.SetColor(EmissionColor, Color.Lerp(s.emission[i], color * 0.9f, amount));
                }
                else
                {
                    if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, s.baseColors[i]);
                    else if (m.HasProperty(KitColor)) m.SetColor(KitColor, s.kitColors[i]);
                    if (!s.hadEmission[i]) m.DisableKeyword("_EMISSION");
                    m.SetColor(EmissionColor, s.emission[i]);
                }
            }
        }
    }

    /// <summary>Esquece a instancia guardada de um renderer (chame se o material dele for trocado por fora).</summary>
    public static void Forget(Renderer r) => cache.Remove(r);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => cache.Clear();
}

/// <summary>
/// Dica do proximo passo, igual em todos os minigames: a peca que o jogador deve usar agora "respira" com um realce
/// suave (a mesma cor do hover, bem mais fraca). Passar o mouse por cima continua dando o realce forte.
/// </summary>
public static class MinigameHint
{
    /// <summary>Intensidade minima e maxima da respiracao (1 = realce do hover).</summary>
    public const float Min = 0.1f, Max = 0.45f;
    /// <summary>Respiracoes por segundo.</summary>
    public const float Rate = 0.8f;

    /// <summary>Intensidade da dica agora (sobe e desce devagar).</summary>
    public static float Level => Mathf.Lerp(Min, Max, 0.5f + 0.5f * Mathf.Sin(Time.time * Rate * 2f * Mathf.PI));
}
