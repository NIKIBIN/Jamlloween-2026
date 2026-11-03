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

    /// <summary>Atalho para o realce compartilhado (HighlightTint).</summary>
    public static void Tint(Renderer[] rs, bool on, Color color) => HighlightTint.Tint(rs, on, color);
}
