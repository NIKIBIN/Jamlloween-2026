using System;
using UnityEngine;

namespace Jamlloween.Lighting
{
    /// <summary>
    /// One look of the world at night: fog, brightness of the unlit scenery and ambient light.
    /// <see cref="WorldLighting"/> applies one profile at a time and fades between them.
    /// Distances are where exp2 fog leaves 5% of the scene visible.
    /// </summary>
    [CreateAssetMenu(menuName = "Jamlloween/Lighting Profile", fileName = "LightingProfile")]
    public class LightingProfile : ScriptableObject
    {
        /// <summary>sqrt(-ln(0.05)): fog density x distance at which 5% of the scene remains.</summary>
        public const float FogEdge = 1.7308f;

        [Header("Fog")]
        [Tooltip("Fog colour. The player camera background takes the same colour.")]
        public Color fogColor = new Color(0.02f, 0.022f, 0.035f);
        [Tooltip("Exp2 density around the player, outside the flashlight beam. Lower sees farther.")]
        [Range(0.01f, 0.5f)] public float fogDensity = 0.15f;

        [Header("Darkness")]
        [Tooltip("Multiplies the colours of the unlit scenery (PSX Unlit, Flashlight Fog Surface) outside the beam.")]
        [Range(0f, 1.5f)] public float brightness = 0.75f;
        [Tooltip("Flat ambient light, for the few materials that use the URP lighting.")]
        public Color ambientColor = new Color(0.05f, 0.05f, 0.07f);

        /// <summary>Distance in metres at which 5% of the scene stays visible.</summary>
        public float VisibleDistance => FogEdge / fogDensity;

        /// <summary>Raised when a value changes in the Inspector, so the scene shows it at once.</summary>
        public static event Action<LightingProfile> Changed;

        void OnValidate() => Changed?.Invoke(this);
    }
}
