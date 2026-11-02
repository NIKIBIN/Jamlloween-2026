using Spike2.Core;
using UnityEngine;

namespace Spike2.Zatan
{
    /// <summary>The Zatan agent (SPIKE2.md 5.2, 6). Seeded in wave 0; owned by agent D.</summary>
    [CreateAssetMenu(menuName = "Spike2/Tunables/Zatan", fileName = "ZatanTunables")]
    public class ZatanTunables : TunablesBase
    {
        public override string Tab => "Zatan";

        [Header("Velocidade")]
        [Range(0.5f, 6f)] public float patrolSpeed = 2.2f;
        [Range(0.5f, 8f)] public float investigateSpeed = 3.4f;
        [Range(1f, 10f)] public float chaseSpeed = 6.0f;
        public bool turnPenalty = true;
        [Range(0.1f, 1f)] public float turnPenaltyMult = 0.55f;
        [Range(10f, 120f)] public float turnPenaltyAngle = 50f;
        [Range(30f, 360f)] public float turnRate = 140f;
        [Range(1f, 20f)] public float acceleration = 7f;

        [Header("Trancos")]
        [Range(0.2f, 3f)] public float lurchIntervalMin = 0.6f;
        [Range(0.2f, 3f)] public float lurchIntervalMax = 1.2f;
        [Range(0f, 0.6f)] public float lurchPauseMin = 0.12f;
        [Range(0f, 0.6f)] public float lurchPauseMax = 0.2f;

        [Header("Visão")]
        [Range(20f, 180f)] public float visionAngle = 90f;
        [Range(2f, 50f)] public float visionRangeFlashlight = 25f;
        [Range(2f, 40f)] public float visionRangeLit = 15f;
        [Range(1f, 30f)] public float visionRangeDark = 7f;
        [Range(1f, 2.5f)] public float eyeHeight = 1.9f;

        [Header("Perseguição")]
        [Range(0.5f, 15f)] public float losePlayerTime = 4f;
        [Range(0.5f, 10f)] public float losePlayerTimeShort = 2f;
        [Range(5f, 90f)] public float shortenLoseAfter = 30f;
        [Range(0.5f, 3f)] public float catchDistance = 1.2f;
        [Range(0.5f, 15f)] public float investigateLookTime = 4f;
        [Range(0f, 3f)] public float hearingMult = 1f;
        [Range(0.2f, 3f)] public float sawHidingGrace = 1.2f;
        [Range(1f, 15f)] public float searchHidingTime = 6f;

        [Header("Portas")]
        [Range(1f, 20f)] public float doorForceTime = 8f;
        [Range(1f, 10f)] public float doorHoldToGiveUp = 4f;
        [Range(5f, 40f)] public float doorForceNoiseRadius = 22f;
        [Range(0.2f, 3f)] public float unlockedDoorOpenTime = 1f;

        [Header("Câmeras")]
        [Range(5f, 40f)] public float cameraRange = 20f;
        [Range(0.5f, 6f)] public float cameraReportTime = 2f;
        [Range(0f, 20f)] public float cameraReportCooldown = 6f;
    }
}
