using UnityEngine;

namespace Jamlloween.Flashlight
{
    /// <summary>Flashlight tunables. Distances are where exp2 fog leaves 5% of the scene visible.</summary>
    [CreateAssetMenu(menuName = "Jamlloween/Flashlight Config", fileName = "FlashlightConfig")]
    public class FlashlightConfig : ScriptableObject
    {
        /// <summary>sqrt(-ln(0.05)): fog density x distance at which 5% of the scene remains.</summary>
        const float FogEdge = 1.7308f;

        [Header("Fog")]
        [Tooltip("Exp2 density around the player, light off or outside the beam.")]
        [Range(0.01f, 0.5f)] public float baseFogDensity = 0.23f;
        [Tooltip("Exp2 density inside the beam: lower sees farther.")]
        [Range(0.01f, 0.5f)] public float coneFogDensity = 0.102f;
        [Range(0.05f, 2f)] public float transitionSeconds = 0.2f;

        [Header("Darkness")]
        [Tooltip("Multiplies the scene's unlit colours around the player.")]
        [Range(0f, 1f)] public float baseBrightness = 0.45f;
        [Tooltip("Multiplies the scene's unlit colours inside the beam.")]
        [Range(0f, 1f)] public float coneBrightness = 0.8f;

        [Header("Light")]
        [Range(0f, 20f)] public float intensity = 2.5f;
        [Range(10f, 120f)] public float spotAngle = 38f;
        [Range(1f, 120f)] public float innerSpotAngle = 18f;

        public float BaseDistance => FogEdge / baseFogDensity;
        public float ConeDistance => FogEdge / coneFogDensity;
    }
}
