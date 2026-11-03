using UnityEngine;

namespace Jamlloween.Zatan
{
    /// <summary>The eye beams: white toward the ground, yellow when he hunts, red by the time the chase slowdown begins.</summary>
    public sealed class ZatanEyeLights : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int BeamColorId = Shader.PropertyToID("_BeamColor");
        static readonly int FogResistanceId = Shader.PropertyToID("_FogResistance");
        static readonly int LightOnId = Shader.PropertyToID("_ZatanEyeOn");
        static readonly int LightCosInnerId = Shader.PropertyToID("_ZatanEyeCosInner");
        static readonly int LightClipMarginId = Shader.PropertyToID("_ZatanEyeClipMargin");
        static readonly int LightFogCutId = Shader.PropertyToID("_ZatanEyeFogCut");
        static readonly int LightMinAlbedoId = Shader.PropertyToID("_ZatanEyeMinAlbedo");
        static readonly int LightPositionId =Shader.PropertyToID("_ZatanEyePosition");
        static readonly int LightDirectionId = Shader.PropertyToID("_ZatanEyeDirection");
        static readonly int LightColorId = Shader.PropertyToID("_ZatanEyeColor");
        static readonly int LightClipPlaneId = Shader.PropertyToID("_ZatanEyeClipPlane");
        static readonly Vector4 NoClip = new Vector4(0f, 0f, 0f, 1f);

        [SerializeField] ZatanConfig config;
        [SerializeField] ZatanBehaviourBrain brain;
        [SerializeField] Transform[] pivots;
        [SerializeField] Renderer[] volumes;
        [SerializeField] Renderer[] eyes;
        [Tooltip("Nunca bloqueiam o feixe: o próprio Zatan e o jogador.")]
        [SerializeField] Transform[] beamIgnored;

        public Color CurrentColor { get; private set; }
        public float CurrentPitch { get; private set; }
        public bool IsLit { get; private set; } = true;
        public Vector4 WallPlane { get; private set; } = NoClip;

        public float BeamLengthOf(int eye) => lengths[eye];

        readonly RaycastHit[] hits = new RaycastHit[16];
        MaterialPropertyBlock eyeBlock, volumeBlock;
        Color huntColor;
        float huntBlend;
        float[] lengths, targetLengths, probeDistances;
        float probeTimer, coneSlope, cosOuter, cosInner, maxLength;

        void Awake()
        {
            if (config == null) throw new MissingReferenceException(nameof(config));
            if (brain == null) throw new MissingReferenceException(nameof(brain));
            if (pivots == null || pivots.Length == 0) throw new MissingReferenceException(nameof(pivots));
            if (volumes == null || volumes.Length != pivots.Length) throw new MissingReferenceException(nameof(volumes));
            if (eyes == null || eyes.Length == 0) throw new MissingReferenceException(nameof(eyes));
            if (beamIgnored == null || beamIgnored.Length == 0) throw new MissingReferenceException(nameof(beamIgnored));

            eyeBlock = new MaterialPropertyBlock();
            volumeBlock = new MaterialPropertyBlock();
            huntColor = config.beamAlertColor;
            SetShape(0f);
            lengths = new float[pivots.Length];
            targetLengths = new float[pivots.Length];
            probeDistances = new float[config.beamFanRays + 1];

            for (int i = 0; i < lengths.Length; i++) SetLength(i, config.beamLength);
        }

        void OnEnable()
        {
            brain.StateChanged += OnStateChanged;
            Shader.SetGlobalFloat(LightClipMarginId, config.beamWallClipMargin);
            Shader.SetGlobalFloat(LightFogCutId, config.beamSurfaceFogCut);
            Shader.SetGlobalFloat(LightMinAlbedoId, config.beamSurfaceMinAlbedo);
            Shader.SetGlobalFloat(LightOnId, IsLit ? 1f : 0f);
        }

        void OnDisable()
        {
            brain.StateChanged -= OnStateChanged;
            Shader.SetGlobalFloat(LightOnId, 0f);
        }

        void Start()
        {
            OnStateChanged(brain.State);
            Apply(0f);
            SnapLengths();
            PublishLight();
        }

        void LateUpdate()
        {
            if (!IsLit) return;

            bool hunting = brain.State == ZatanBehaviourState.Spotted || brain.State == ZatanBehaviourState.Chase;
            if (hunting)
                huntColor = Color.Lerp(config.beamAlertColor, config.beamDangerColor, config.BeamDanger(brain.DistanceToPlayer));

            float step = Time.deltaTime / config.beamTransitionTime;
            Apply(Mathf.MoveTowards(huntBlend, hunting ? 1f : 0f, step));
            UpdateLengths();
            PublishLight();
        }

        void OnStateChanged(ZatanBehaviourState state)
        {
            // In the catch sequence the world is black: only the eyes themselves stay on, full red.
            if (state == ZatanBehaviourState.Caught)
            {
                huntColor = config.beamDangerColor;
                Apply(1f);
            }

            bool lit = state != ZatanBehaviourState.Vanished && state != ZatanBehaviourState.Caught;
            if (lit == IsLit) return;

            IsLit = lit;
            foreach (Renderer v in volumes) v.enabled = lit;
            Shader.SetGlobalFloat(LightOnId, lit ? 1f : 0f);
            // He comes back calm: no fade from the colour he left with.
            if (lit)
            {
                Apply(0f);
                SnapLengths();
                PublishLight();
            }
        }

        void UpdateLengths()
        {
            probeTimer -= Time.deltaTime;
            if (probeTimer <= 0f)
            {
                probeTimer = config.beamProbeInterval;
                WallPlane = ProbeWall();
                for (int i = 0; i < pivots.Length; i++) targetLengths[i] = ClampToWall(pivots[i], ProbeLength(pivots[i]));
            }
            for (int i = 0; i < lengths.Length; i++)
            {
                float speed = targetLengths[i] < lengths[i] ? config.beamShrinkSpeed : config.beamGrowSpeed;
                SetLength(i, Mathf.MoveTowards(lengths[i], targetLengths[i], speed * Time.deltaTime));
            }
        }

        void SnapLengths()
        {
            probeTimer = config.beamProbeInterval;
            WallPlane = ProbeWall();
            for (int i = 0; i < pivots.Length; i++)
            {
                targetLengths[i] = ClampToWall(pivots[i], ProbeLength(pivots[i]));
                SetLength(i, targetLengths[i]);
            }
        }

        /// <summary>Aimed at the ground, the beam's own rays stop at the floor: the wall plane still ends the visible cone.</summary>
        float ClampToWall(Transform pivot, float length)
        {
            Vector3 normal = new Vector3(WallPlane.x, WallPlane.y, WallPlane.z);
            float approach = Vector3.Dot(normal, pivot.forward);
            if (approach >= -0.01f) return length;
            float toWall = -(Vector3.Dot(normal, pivot.position) + WallPlane.w) / approach;
            return Mathf.Max(Mathf.Min(length, toWall), config.beamMinLength);
        }

        /// <summary>Where the beam ends: the distance at which enough of the fan is blocked, so a thin pole doesn't cut it.</summary>
        float ProbeLength(Transform pivot)
        {
            Vector3 origin = pivot.position, forward = pivot.forward;
            probeDistances[0] = BlockDistance(origin, forward);
            float offset = coneSlope * config.beamFanSpread;
            for (int r = 0; r < config.beamFanRays; r++)
            {
                float angle = r * Mathf.PI * 2f / config.beamFanRays;
                Vector3 around = pivot.right * Mathf.Cos(angle) + pivot.up * Mathf.Sin(angle);
                probeDistances[r + 1] = BlockDistance(origin, (forward + around * offset).normalized);
            }
            System.Array.Sort(probeDistances);
            int index = Mathf.Clamp(Mathf.CeilToInt(probeDistances.Length * config.beamBlockFraction) - 1, 0, probeDistances.Length - 1);
            return Mathf.Max(probeDistances[index], config.beamMinLength);
        }

        float BlockDistance(Vector3 origin, Vector3 direction)
        {
            bool found = TryNearestHit(origin, direction, out RaycastHit nearest);
            if (!found || nearest.normal.y > config.beamFloorNormalY) return maxLength;
            return nearest.distance;
        }

        /// <summary>
        /// The wall ahead, as a plane the light stops at. No shadow pass exists here, and aimed at the ground the beam's own rays
        /// reach the floor first, so these rays run level and skip it.
        /// </summary>
        Vector4 ProbeWall()
        {
            Vector3 origin = EyeCentre() + Vector3.down * config.beamWallProbeDrop;
            Vector3 forward = transform.forward;
            int blocked = 0;
            float nearestDistance = float.PositiveInfinity;
            Vector3 nearestPoint = Vector3.zero, nearestNormal = Vector3.zero;

            for (int r = 0; r < config.beamWallRays; r++)
            {
                float yaw = Mathf.Lerp(-0.5f, 0.5f, r / (config.beamWallRays - 1f)) * config.beamWallSpread;
                Vector3 direction = Quaternion.AngleAxis(yaw, Vector3.up) * forward;
                if (!TryNearestHit(origin, direction, out RaycastHit hit) || Mathf.Abs(hit.normal.y) > config.beamFloorNormalY) continue;
                blocked++;
                if (hit.distance >= nearestDistance) continue;
                nearestDistance = hit.distance;
                nearestPoint = hit.point;
                // Upright, or a slanted face would tilt the plane across the floor.
                nearestNormal = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
            }

            if (blocked < config.beamWallMinHits) return NoClip;
            return new Vector4(nearestNormal.x, nearestNormal.y, nearestNormal.z, -Vector3.Dot(nearestNormal, nearestPoint));
        }

        bool TryNearestHit(Vector3 origin, Vector3 direction, out RaycastHit nearest)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, hits, maxLength, config.beamBlockers, QueryTriggerInteraction.Ignore);
            nearest = default;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].distance >= nearestDistance || IsIgnored(hits[i].transform)) continue;
                nearestDistance = hits[i].distance;
                nearest = hits[i];
            }
            return nearestDistance < float.PositiveInfinity;
        }

        bool IsIgnored(Transform hit)
        {
            foreach (Transform t in beamIgnored)
                if (hit.IsChildOf(t)) return true;
            return false;
        }

        void SetLength(int eye, float length)
        {
            lengths[eye] = length;
            float radius = length * coneSlope;
            volumes[eye].transform.localScale = new Vector3(radius, radius, length);
        }

        Vector3 EyeCentre()
        {
            Vector3 sum = Vector3.zero;
            foreach (Transform p in pivots) sum += p.position;
            return sum / pivots.Length;
        }

        /// <summary>The eyes sit too close together to tell two pools apart, so the scenery gets one light from between them.</summary>
        void PublishLight()
        {
            Vector3 position = EyeCentre();
            Vector3 forward = pivots[0].forward;
            Shader.SetGlobalVector(LightPositionId, new Vector4(position.x, position.y, position.z, maxLength));
            Shader.SetGlobalVector(LightDirectionId, new Vector4(forward.x, forward.y, forward.z, cosOuter));
            Shader.SetGlobalFloat(LightCosInnerId, cosInner);
            Shader.SetGlobalVector(LightColorId, CurrentColor * config.beamSurfaceIntensity);
            Shader.SetGlobalVector(LightClipPlaneId, WallPlane);
        }

        /// <summary>Wider and longer as he hunts: 0 = patrol beam, 1 = chase beam.</summary>
        void SetShape(float blend)
        {
            float outer = Mathf.Lerp(config.beamConeAngle, config.beamChaseConeAngle, blend) * 0.5f * Mathf.Deg2Rad;
            float inner = Mathf.Lerp(config.beamInnerConeAngle, config.beamChaseInnerConeAngle, blend) * 0.5f * Mathf.Deg2Rad;
            coneSlope = Mathf.Tan(outer);
            cosOuter = Mathf.Cos(outer);
            cosInner = Mathf.Cos(inner);
            maxLength = Mathf.Lerp(config.beamLength, config.beamChaseLength, blend);
        }

        void Apply(float blend)
        {
            huntBlend = blend;
            CurrentColor = Color.Lerp(config.beamIdleColor, huntColor, blend);
            CurrentPitch = Mathf.Lerp(config.beamIdlePitch, config.beamChasePitch, blend);
            SetShape(blend);

            Quaternion rotation = Quaternion.Euler(CurrentPitch, 0f, 0f);
            foreach (Transform p in pivots) p.localRotation = rotation;

            volumeBlock.SetColor(BeamColorId, CurrentColor * config.beamOpacity);
            volumeBlock.SetFloat(FogResistanceId, config.beamFogResistance);
            foreach (Renderer v in volumes) v.SetPropertyBlock(volumeBlock);

            eyeBlock.SetColor(BaseColorId, CurrentColor);
            foreach (Renderer e in eyes) e.SetPropertyBlock(eyeBlock);
        }
    }
}
