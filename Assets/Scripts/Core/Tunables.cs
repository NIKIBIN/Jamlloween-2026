using UnityEngine;

namespace Circus
{
    public enum RepairVariantOverride { PerMachine, Hold, Sequence }

    /// <summary>
    /// Every tuning value and feature flag of the spike. Lives in Resources/Tunables.asset.
    /// In Play, Tunables.I is a runtime copy (Instantiate): panel edits never touch the asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Circus/Tunables", fileName = "Tunables")]
    public class Tunables : ScriptableObject
    {
        static Tunables _i;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _i = null;

        public static Tunables I
        {
            get
            {
                if (_i == null)
                {
                    var asset = Resources.Load<Tunables>("Tunables");
                    // Spike 2 fix: in Play the panel edits a runtime copy, never the asset on disk.
                    if (asset != null && Application.isPlaying) { _i = Instantiate(asset); _i.name = asset.name + " (runtime)"; }
                    else _i = asset;
                    if (_i == null) _i = CreateInstance<Tunables>();
                }
                return _i;
            }
        }

        [Header("Player")]
        public float walkSpeed = 4f;
        public float runSpeed = 6f;
        public float mouseSensitivity = 0.12f;
        public float gravity = -20f;
        public float interactDistance = 2.6f;
        public float walkNoiseRadius = 2f;
        public float runNoiseRadius = 12f;
        public float footstepNoiseInterval = 0.35f;
        public float flashlightRange = 18f;
        public float flashlightIntensity = 2.5f;
        public float flashlightAngle = 55f;

        [Header("Repair")]
        public RepairVariantOverride repairVariant = RepairVariantOverride.PerMachine;
        public float repairDurationMult = 1.25f;
        public float repairNoiseMult = 1f;
        public float repairNoiseInterval = 0.5f;
        public int sequenceKeysPerStep = 3;
        public float holdSecondsPerSequenceStep = 1.5f;
        public float sequenceStepsPerHoldSecond = 0.5f;

        [Header("Zatan")]
        public float zatanPatrolSpeed = 2.2f;
        public float zatanInvestigateSpeed = 3.2f;
        public float zatanChaseSpeed = 5.2f;
        public float visionAngle = 90f;
        public float visionRange = 15f;
        public float visionRangeFlashlight = 25f;
        public float visionRangeDark = 7f;
        public float hearingMult = 1f;
        public float losePlayerTime = 4f;
        public float investigateLookTime = 4f;
        public float catchDistance = 1.3f;
        public float sawHidingGrace = 1.2f;

        [Header("Feature flags (P1/P2)")]
        public bool camerasTrack = true;
        public bool carouselDistracts = true;
        public bool zatanUsesCarousel = true;
        public bool lightsAffectVision = true;
        public bool lockableDoors = true;
        public bool powerGrid = true;
        public bool vhsInterference = true;
        public bool phaseDirector = true;
        public bool scriptedEvents = true;
        public bool moveOnlyWhenUnobserved = false;
        public bool vhsFilter = true;
        public bool vertexSnap = false;
        public bool mapOnTab = true;

        [Header("Debug")]
        public bool godMode = false;
        public bool showGizmos = false;

        [Header("Visual")]
        public int internalHeight = 300;
        public float vhsBaseIntensity = 0.35f;
        public float fogDensity = 0.055f;

        [Header("Cameras / Monitors")]
        public float cameraRange = 20f;
        public float cameraFov = 70f;
        public float cameraReportTime = 2f;
        public float cameraReportCooldown = 6f;
        public float cameraTurnSpeed = 60f;

        [Header("Carousel")]
        public float carouselNoiseRadius = 30f;
        public float carouselRpm = 6f;
        public float zatanCarouselMinInterval = 60f;
        public float zatanCarouselMaxInterval = 100f;

        [Header("Doors")]
        public float doorLockTime = 1f;
        public float doorForceTime = 6f;
        public float doorForceNoiseRadius = 22f;

        [Header("Power")]
        public float powerBudget = 23f;
        public float outageDuration = 10f;

        [Header("Interference")]
        public float interferenceNearDistance = 14f;
        public float interferenceProximityMax = 0.65f;
        public float interferenceCameraWatch = 0.35f;

        [Header("Phases")]
        public float brincarSpeedMult = 0.6f;
        public float brincarAttackDistance = 4f;
        public float observeBoothRadius = 14f;
        public float observeVanishDistance = 11f;
        public float observeAppearInterval = 14f;
        public float huntDelayAfterCard = 25f;
    }
}
