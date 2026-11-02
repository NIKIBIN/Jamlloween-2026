using UnityEngine;

/// <summary>Peca clicavel do radio. Guarda o tipo e cuida do realce ao passar o mouse.</summary>
public class RadioPart : MonoBehaviour
{
    public enum Kind { Body, Cover, Batteries, Dial }

    public Kind kind;

    Renderer[] renderers;

    public void SetHighlight(bool on, Color color)
    {
        if (renderers == null) renderers = GetComponentsInChildren<Renderer>(true);
        PartHighlight.Set(renderers, on, color);
    }
}
