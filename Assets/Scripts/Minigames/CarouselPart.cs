using UnityEngine;

/// <summary>Peca clicavel dos minigames do carrossel. Guarda o tipo e cuida do realce ao passar o mouse.</summary>
public class CarouselPart : MonoBehaviour
{
    public enum Kind { Head, Cover, StuckObject, Belt, Tensor, Lever }

    public Kind kind;

    [Tooltip("Renderers realcados no hover. Vazio = todos os filhos.")]
    [SerializeField] Renderer[] highlightTargets;

    Renderer[] renderers;

    public void SetHighlight(bool on, Color color)
    {
        if (renderers == null)
            renderers = highlightTargets != null && highlightTargets.Length > 0 ? highlightTargets : GetComponentsInChildren<Renderer>(true);
        PartHighlight.Set(renderers, on, color);
    }

    /// <summary>Chamado quando os renderers do objeto mudam (ex.: objeto preso trocado em tempo de execucao).</summary>
    public void RefreshRenderers() => renderers = null;
}
