using UnityEngine;

/// <summary>
/// Realce de pecas clicaveis. Mistura a cor-base com a cor de realce e tambem soma emissao
/// (quando o shader renderiza emissao). Usa MaterialPropertyBlock, entao nao mexe nos materiais.
/// </summary>
public static class PartHighlight
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    static MaterialPropertyBlock block;

    public static void Set(Renderer[] renderers, bool on, Color color)
    {
        if (block == null) block = new MaterialPropertyBlock();
        foreach (var r in renderers)
        {
            if (r == null) continue;
            if (!on) { r.SetPropertyBlock(null); continue; }   // volta ao que o material define

            r.GetPropertyBlock(block);
            var mat = r.sharedMaterial;
            if (mat != null && mat.HasProperty(BaseColor))
                block.SetColor(BaseColor, Color.Lerp(mat.GetColor(BaseColor), new Color(color.r, color.g, color.b, 1f), 0.45f) + color * 0.35f);
            block.SetColor(EmissionColor, color);
            r.SetPropertyBlock(block);
        }
    }
}
