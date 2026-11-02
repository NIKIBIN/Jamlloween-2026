using System.Collections.Generic;
using UnityEngine;

/// <summary>Peca clicavel do conserto do Zatan. Guarda o tipo e cuida do realce ao passar o mouse.</summary>
public class ZatanRepairPart : MonoBehaviour
{
    public enum Kind { Cover, Rag, Hose, Switch, Crank }

    public Kind kind;

    [Tooltip("Renderers realcados no hover. Vazio = todos os filhos.")]
    [SerializeField] Renderer[] highlightTargets;

    Renderer[] renderers;

    public void SetHighlight(bool on, Color color)
    {
        if (renderers == null || renderers.Length == 0)
            renderers = highlightTargets != null && highlightTargets.Length > 0 ? highlightTargets : GetComponentsInChildren<Renderer>(true);
        Tint(renderers, on, color);
    }

    // ---------- realce por instancia de material ----------
    // MaterialPropertyBlock nao aparece com o GPU Resident Drawer ligado, entao cada renderer realcado
    // ganha uma instancia propria do material (criada na primeira vez) e a cor-base volta ao desligar.

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    class Saved { public Material[] mats; public Color[] baseColors; public Color[] emission; public bool[] hadEmission; }
    static readonly Dictionary<Renderer, Saved> cache = new Dictionary<Renderer, Saved>();

    /// <summary>Realca (ou restaura) os renderers com a cor dada: puxa a cor-base para ela e soma emissao.</summary>
    public static void Tint(Renderer[] rs, bool on, Color color)
    {
        if (rs == null) return;
        foreach (var r in rs)
        {
            if (r == null) continue;
            if (!cache.TryGetValue(r, out var s))
            {
                if (!on) continue;
                var mats = r.materials;   // instancias
                s = new Saved { mats = mats, baseColors = new Color[mats.Length], emission = new Color[mats.Length], hadEmission = new bool[mats.Length] };
                for (int i = 0; i < mats.Length; i++)
                {
                    s.baseColors[i] = mats[i].HasProperty(BaseColor) ? mats[i].GetColor(BaseColor) : Color.white;
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
                        m.SetColor(BaseColor, Color.Lerp(s.baseColors[i], new Color(color.r, color.g, color.b, s.baseColors[i].a), 0.55f));
                    m.EnableKeyword("_EMISSION");
                    m.SetColor(EmissionColor, color * 0.9f);
                }
                else
                {
                    if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, s.baseColors[i]);
                    if (!s.hadEmission[i]) m.DisableKeyword("_EMISSION");
                    m.SetColor(EmissionColor, s.emission[i]);
                }
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => cache.Clear();
}
