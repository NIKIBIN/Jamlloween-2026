using System.Collections;
using UnityEngine;

/// <summary>Peca clicavel do radio.</summary>
public class RadioPart : MonoBehaviour
{
    public enum Kind { Body, Cover, Battery, Volume, Dial, Antenna, Talk }

    public Kind kind;
    /// <summary>Pilha nova (true) ou velha e vazada (false). So para Kind.Battery.</summary>
    public bool isNew;
    /// <summary>Encaixe onde a pilha esta (-1 = solta na mesa).</summary>
    [HideInInspector] public int slot = -1;
    [System.NonSerialized] public Coroutine glide;

    Renderer[] renderers;

    public void SetHighlight(bool on, Color color)
    {
        if (renderers == null) renderers = GetComponentsInChildren<Renderer>(true);
        PartHighlight.Set(renderers, on, color);
    }
}
