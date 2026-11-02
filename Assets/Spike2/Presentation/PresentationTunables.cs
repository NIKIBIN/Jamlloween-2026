using Spike2.Core;
using UnityEngine;

namespace Spike2.Presentation
{
    /// <summary>Announcer, cutouts, tarot, double (SPIKE2.md 5.7, 5.10, 5.11). Seeded in wave 0; owned by agent E.</summary>
    [CreateAssetMenu(menuName = "Spike2/Tunables/Presentation", fileName = "PresentationTunables")]
    public class PresentationTunables : TunablesBase
    {
        public override string Tab => "Apresentação";

        [Header("Anúncios")]
        public bool announcements = true;
        [Range(0f, 1f)] public float announceInterference = 0.3f;
        [Range(1f, 10f)] public float subtitleSeconds = 4f;

        [Header("Emboscada")]
        public bool cutoutsAsWarning = true;
        [Range(0f, 1f)] public float ambushInterference = 0.3f;

        [Header("Duplo")]
        [Range(2f, 30f)] public float doubleVanishDistance = 10f;
        [Range(2f, 30f)] public float cutoutZatanWakeDistance = 8f;
    }
}
