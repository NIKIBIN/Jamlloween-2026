using UnityEngine;

/// <summary>Peca clicavel do gerador (tampa, funil, corda). Guarda o tipo e o realce ao passar o mouse.</summary>
public class GeneratorPart : MonoBehaviour
{
    public enum Kind { Cap, Funnel, Cord }

    public Kind kind;

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
