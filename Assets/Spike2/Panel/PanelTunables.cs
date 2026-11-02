using Spike2.Core;
using UnityEngine;

namespace Spike2.Panel
{
    /// <summary>ServicePanel (SPIKE2.md 5.4, 6). Seeded in wave 0; owned by agent C.</summary>
    [CreateAssetMenu(menuName = "Spike2/Tunables/Panel", fileName = "PanelTunables")]
    public class PanelTunables : TunablesBase
    {
        public override string Tab => "Painel";

        [Header("Câmera")]
        [Range(20f, 70f)] public float panelFov = 45f;
        [Range(0.05f, 1.5f)] public float zoomTime = 0.35f;
        [Range(0f, 1f)] public float vignette = 0.3f;

        [Header("Erro")]
        [Range(0f, 40f)] public float errorNoiseRadius = 18f;
        [Range(0f, 1f)] public float errorInterference = 0.6f;
        [Range(0.1f, 3f)] public float errorInterferenceTime = 1f;

        [Header("Cabos por cor")]
        public bool colorsNeedFlashlight = true;
    }
}
