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
        static readonly int LightRightId = Shader.PropertyToID("_ZatanEyeRight");
        static readonly int LightUpId = Shader.PropertyToID("_ZatanEyeUp");
        static readonly int LightOcclusionId = Shader.PropertyToID("_ZatanEyeOcclusion");

        // The scenery light's own "shadow map": the distance to the first obstacle along a few directions of the cone, the axis
        // and two rings around it. Must match ZATAN_EYE_RING_RAYS and the ring radii in ZatanEyeLight.hlsl.
        public const int OcclusionRingRays = 12;
        static readonly float[] OcclusionRings = { 0.5f, 1f };
        public const int OcclusionSamples = 1 + 12 * 2;

        [SerializeField] ZatanConfig config;
        [SerializeField] ZatanBehaviourBrain brain;
        [SerializeField] Transform[] pivots;
        [SerializeField] Renderer[] volumes;
        [SerializeField] Renderer[] eyes;
        [Tooltip("Nunca bloqueiam o feixe: o próprio Zatan e o jogador.")]
        [SerializeField] Transform[] beamIgnored;
        [Tooltip("Mirando no jogador (AimAt), o feixe aponta este tanto abaixo da câmera dele (m): ilumina o jogador sem ir só no olho.")]
        [SerializeField, Min(0f)] float aimDrop = 0.35f;

        public Color CurrentColor { get; private set; }
        public float CurrentPitch { get; private set; }
        public bool IsLit { get; private set; } = true;

        public float BeamLengthOf(int eye) => lengths[eye];

        readonly RaycastHit[] hits = new RaycastHit[16];
        MaterialPropertyBlock eyeBlock, volumeBlock;
        Color huntColor;
        float huntBlend;
        float[] lengths, targetLengths;
        readonly float[] occlusion = new float[OcclusionSamples], targetOcclusion = new float[OcclusionSamples];
        float probeTimer, coneSlope, cosOuter, cosInner, maxLength;
        Transform aimTarget;
        Vector3 aimPoint;
        float aimBlend;

        /// <summary>The beams turn from their pitch to point at this target (the player's camera) until StopAim.</summary>
        public void AimAt(Transform target)
        {
            if (target == null) throw new System.ArgumentNullException(nameof(target));
            aimTarget = target;
            aimPoint = target.position + Vector3.down * aimDrop;
        }

        /// <summary>Back to the state's own pitch, blending over the beam transition time.</summary>
        public void StopAim() => aimTarget = null;

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

            for (int i = 0; i < lengths.Length; i++) SetLength(i, config.beamLength);
            for (int i = 0; i < OcclusionSamples; i++) occlusion[i] = targetOcclusion[i] = maxLength;
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

            bool hunting = (brain.State == ZatanBehaviourState.Spotted || brain.IsHunting) && !brain.CalmEyes;
            if (hunting)
                huntColor = Color.Lerp(config.beamAlertColor, config.beamDangerColor, config.BeamDanger(brain.DistanceToPlayer));

            float step = Time.deltaTime / config.beamTransitionTime;
            Apply(Mathf.MoveTowards(huntBlend, hunting ? 1f : 0f, step));
            ApplyAim(step);
            UpdateLengths();
            PublishLight();
        }

        /// <summary>Over the pitch Apply just set: turn the pivots toward the aim point, blended in and out.</summary>
        void ApplyAim(float step)
        {
            if (aimTarget != null) aimPoint = aimTarget.position + Vector3.down * aimDrop;
            aimBlend = Mathf.MoveTowards(aimBlend, aimTarget != null ? 1f : 0f, step);
            if (aimBlend <= 0f) return;
            foreach (Transform p in pivots)
            {
                Vector3 to = aimPoint - p.position;
                if (to.sqrMagnitude < 0.0001f) continue;
                p.rotation = Quaternion.Slerp(p.rotation, Quaternion.LookRotation(to, Vector3.up), aimBlend);
            }
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
                ProbeOcclusion();
                float reach = ConeReach();
                for (int i = 0; i < pivots.Length; i++) targetLengths[i] = reach;
            }
            for (int i = 0; i < lengths.Length; i++)
            {
                float speed = targetLengths[i] < lengths[i] ? config.beamShrinkSpeed : config.beamGrowSpeed;
                SetLength(i, Mathf.MoveTowards(lengths[i], targetLengths[i], speed * Time.deltaTime));
            }
            for (int i = 0; i < OcclusionSamples; i++)
            {
                float speed = targetOcclusion[i] < occlusion[i] ? config.beamShrinkSpeed : config.beamGrowSpeed;
                occlusion[i] = Mathf.MoveTowards(occlusion[i], targetOcclusion[i], speed * Time.deltaTime);
            }
        }

        void SnapLengths()
        {
            probeTimer = config.beamProbeInterval;
            ProbeOcclusion();
            System.Array.Copy(targetOcclusion, occlusion, OcclusionSamples);
            float reach = ConeReach();
            for (int i = 0; i < pivots.Length; i++)
            {
                targetLengths[i] = reach;
                SetLength(i, reach);
            }
        }

        /// <summary>
        /// The visible cone reaches as far as its longest clear ray. The beam shader fades each part of it behind whatever blocks
        /// that direction, so the cone is only cut where something is really in the way.
        /// </summary>
        float ConeReach()
        {
            float longest = 0f;
            foreach (float d in targetOcclusion) longest = Mathf.Max(longest, d);
            return Mathf.Clamp(longest + config.beamWallClipMargin, config.beamMinLength, maxLength);
        }

        /// <summary>
        /// Distance to the first obstacle along the axis of the scenery light and two rings around it. The shader blends these, so
        /// the light ends behind the obstacle that is really in the way, softly, instead of along a straight cut.
        /// </summary>
        void ProbeOcclusion()
        {
            Vector3 origin = EyeCentre();
            Transform basis = pivots[0];
            targetOcclusion[0] = BlockDistance(origin, basis.forward);
            for (int ring = 0; ring < OcclusionRings.Length; ring++)
            {
                float offset = coneSlope * OcclusionRings[ring];
                for (int r = 0; r < OcclusionRingRays; r++)
                {
                    float angle = r * Mathf.PI * 2f / OcclusionRingRays;
                    Vector3 around = basis.right * Mathf.Cos(angle) + basis.up * Mathf.Sin(angle);
                    targetOcclusion[1 + ring * OcclusionRingRays + r] = BlockDistance(origin, (basis.forward + around * offset).normalized);
                }
            }
        }

        /// <summary>
        /// The nearest obstacle along the ray, or the full range. Floors don't count: the floor under the beam is what it lights,
        /// and nothing past the point where a ray meets the ground can be seen anyway.
        /// </summary>
        float BlockDistance(Vector3 origin, Vector3 direction)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, hits, maxLength, config.beamBlockers, QueryTriggerInteraction.Ignore);
            float nearest = maxLength;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].distance >= nearest || hits[i].normal.y > config.beamFloorNormalY || IsIgnored(hits[i].transform)) continue;
                nearest = hits[i].distance;
            }
            return nearest;
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
            Transform basis = pivots[0];
            Vector3 forward = basis.forward, right = basis.right, up = basis.up;
            Shader.SetGlobalVector(LightPositionId, new Vector4(position.x, position.y, position.z, maxLength));
            Shader.SetGlobalVector(LightDirectionId, new Vector4(forward.x, forward.y, forward.z, cosOuter));
            // w: the cone's tangent, so the shader finds where a pixel sits among the occlusion rings.
            Shader.SetGlobalVector(LightRightId, new Vector4(right.x, right.y, right.z, coneSlope));
            Shader.SetGlobalVector(LightUpId, new Vector4(up.x, up.y, up.z, 0f));
            Shader.SetGlobalFloatArray(LightOcclusionId, occlusion);
            Shader.SetGlobalFloat(LightCosInnerId, cosInner);
            Shader.SetGlobalVector(LightColorId, CurrentColor * config.beamSurfaceIntensity);
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
