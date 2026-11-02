using Spike2.Core;
using UnityEngine;

namespace Spike2.Zatan
{
    /// <summary>ZatanDirector + MenaceGauge + blackout (SPIKE2.md 5.3, 5.7, 6). Seeded in wave 0; owned by agent D.</summary>
    [CreateAssetMenu(menuName = "Spike2/Tunables/Director", fileName = "DirectorTunables")]
    public class DirectorTunables : TunablesBase
    {
        public override string Tab => "Diretor";

        [Header("Menace (0-100)")]
        [Range(0f, 30f)] public float nearGain = 6f;
        [Range(1f, 30f)] public float nearDistance = 12f;
        [Range(0f, 30f)] public float mutualSightGain = 10f;
        [Range(0f, 30f)] public float chaseGain = 8f;
        [Range(0f, 20f)] public float decay = 4f;
        [Range(50f, 100f)] public float retreatThreshold = 85f;
        [Range(5f, 90f)] public float retreatMin = 25f;
        [Range(5f, 90f)] public float retreatMax = 40f;
        [Range(0f, 50f)] public float boredThreshold = 15f;
        [Range(5f, 180f)] public float boredTime = 45f;

        [Header("Aproximação")]
        [Range(2f, 40f)] public float approachMin = 12f;
        [Range(2f, 60f)] public float approachMax = 25f;
        [Range(2f, 10f)] public float ambushWarningLead = 2f;
        [Range(5f, 40f)] public float boredInvestigateMin = 15f;
        [Range(5f, 40f)] public float boredInvestigateMax = 25f;

        [Header("Apagão de zona")]
        [Range(5f, 60f)] public float blackoutDuration = 20f;
        [Range(10f, 300f)] public float blackoutMinInterval = 90f;
        [Range(0f, 5f)] public float blackoutAnnounceLead = 2f;
    }
}
