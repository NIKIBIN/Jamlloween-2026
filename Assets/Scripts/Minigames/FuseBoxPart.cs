using UnityEngine;

/// <summary>Peca clicavel do painel de fusiveis. Guarda o tipo, o indice e o realce ao passar o mouse.</summary>
public class FuseBoxPart : MonoBehaviour
{
    public enum Kind { Screw, Cover, Fuse, Slot, SpareFuse }

    public Kind kind;
    public int index;
    /// <summary>Progresso do parafuso (0 = preso, 1 = solto).</summary>
    [HideInInspector] public float progress;

    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    static MaterialPropertyBlock block;
    Renderer[] renderers;

    public void SetHighlight(bool on, Color color)
    {
        if (renderers == null) renderers = GetComponentsInChildren<Renderer>();
        if (block == null) block = new MaterialPropertyBlock();
        foreach (var r in renderers)
        {
            if (on)
            {
                r.GetPropertyBlock(block);
                block.SetColor(EmissionColor, color);
                r.SetPropertyBlock(block);
            }
            else r.SetPropertyBlock(null);   // volta ao que o material define
        }
    }
}
