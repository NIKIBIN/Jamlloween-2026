using UnityEngine;

namespace Jamlloween.Flashlight
{
    /// <summary>Flashlight tunables. Distances are where exp2 fog leaves 5% of the scene visible.</summary>
    [CreateAssetMenu(menuName = "Jamlloween/Flashlight Config", fileName = "FlashlightConfig")]
    public class FlashlightConfig : ScriptableObject
    {
        /// <summary>sqrt(-ln(0.05)): fog density x distance at which 5% of the scene remains.</summary>
        const float FogEdge = 1.7308f;

        // The fog and brightness outside the beam live in the LightingProfile that WorldLighting applies.
        // Inside the beam the shader never shows less than outside it, so a bright profile is not darkened by the light.
        [Header("Fog")]
        [Tooltip("Exp2 density inside the beam: lower sees farther.")]
        [Range(0.01f, 0.5f)] public float coneFogDensity = 0.102f;
        [Range(0.05f, 2f)] public float transitionSeconds = 0.2f;

        [Header("Darkness")]
        [Tooltip("Multiplies the scene's unlit colours inside the beam.")]
        [Range(0f, 1f)] public float coneBrightness = 0.8f;

        [Header("Light")]
        [Range(0f, 20f)] public float intensity = 2.5f;
        [Range(10f, 120f)] public float spotAngle = 38f;
        [Range(1f, 120f)] public float innerSpotAngle = 18f;

        [Header("Work mode (minigames)")]
        [Tooltip("Outer angle of the beam while a minigame is played: wide, to light the whole machine.")]
        [Range(10f, 170f)] public float workSpotAngle = 120f;
        [Range(1f, 170f)] public float workInnerSpotAngle = 90f;
        [Tooltip("Beam intensity while a minigame is played. Lower than the normal one: the light is a few centimetres from the surface.")]
        [Range(0f, 20f)] public float workIntensity = 0.12f;
        [Tooltip("Cone brightness while a minigame is played. A soft lift, still dark.")]
        [Range(0f, 1f)] public float workConeBrightness = 0.58f;
        [Tooltip("Cone fog density while a minigame is played. Near the world fog: the dark stays around the machine.")]
        [Range(0.01f, 0.5f)] public float workConeFogDensity = 0.3f;

        public float ConeDistance => FogEdge / coneFogDensity;
    }
}
