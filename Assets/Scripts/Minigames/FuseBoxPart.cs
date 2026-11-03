using UnityEngine;

/// <summary>Peca clicavel do painel de fusiveis. Guarda o tipo, o indice e o realce ao passar o mouse.</summary>
public class FuseBoxPart : MonoBehaviour
{
    public enum Kind { Screw, Cover, Fuse, Slot, SpareFuse, Lever }

    public Kind kind;
    public int index;
    /// <summary>Progresso do parafuso (0 = preso, 1 = solto).</summary>
    [HideInInspector] public float progress;

    Renderer[] renderers;

    public void SetHighlight(bool on, Color color)
    {
        if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>();
        HighlightTint.Tint(renderers, on, color);
    }
}
