using System.Collections.Generic;
using UnityEngine;

namespace Jamlloween.Progression
{
    /// <summary>
    /// A story light that guides the player to an objective (the carousel, the generator, the Ferris wheel).
    /// It is lit from the step <see cref="from"/> until the step <see cref="until"/> starts (empty: to the end), so a
    /// debug jump shows the right lights. Lit, it lights the scenery around it (WorldBeacons.hlsl: computed in the
    /// shaders, seen through the fog from far away) and shows its <see cref="renderers"/> (bulbs, a little lamp).
    /// At most 48 lights (beacons, or each emitFrom point) are lit at once; plan the step ranges so they do not overlap more.
    /// With a <see cref="powerGroup"/>, the beacon is dark while that LightGroup is off (a blackout), in any step.
    /// Before <see cref="brokenUntil"/>, the beacon is broken: a weak flicker, and only some bulbs flicker on.
    /// </summary>
    [DefaultExecutionOrder(100)]   // after the director changes step
    public sealed class StepBeacon : MonoBehaviour
    {
        public enum Pattern { Steady, Blink, Flicker, Sputter }

        const int Max = 48;   // = WORLD_BEACON_MAX em WorldBeacons.hlsl
        static readonly List<StepBeacon> all = new List<StepBeacon>();
        static readonly Vector4[] posRange = new Vector4[Max];
        static readonly Vector4[] colors = new Vector4[Max];
        static int pushedFrame = -1;
        static readonly int CountId = Shader.PropertyToID("_BeaconCount");
        static readonly int FogId = Shader.PropertyToID("_BeaconFogDensity");
        static readonly int PosId = Shader.PropertyToID("_BeaconPosRange48");
        static readonly int ColorId = Shader.PropertyToID("_BeaconColor48");
        const int AmbientMax = 8;   // = WORLD_AMBIENT_MAX em WorldBeacons.hlsl
        static readonly Vector4[] ambientPos = new Vector4[AmbientMax];
        static readonly Vector4[] ambientColors = new Vector4[AmbientMax];
        static readonly int AmbientCountId = Shader.PropertyToID("_AmbientLightCount");
        static readonly int AmbientPosId = Shader.PropertyToID("_AmbientLightPosRange");
        static readonly int AmbientColorId = Shader.PropertyToID("_AmbientLightColor");
        static readonly int BulbLevelId = Shader.PropertyToID("_BulbLevel");
        static readonly int BulbBrokenId = Shader.PropertyToID("_BulbBroken");
        static readonly int BulbVariationId = Shader.PropertyToID("_BulbVariation");
        static readonly int BulbSpeedId = Shader.PropertyToID("_BulbSpeed");
        static readonly int BulbMinId = Shader.PropertyToID("_BulbMin");
        static readonly int SegmentAxisId = Shader.PropertyToID("_SegmentAxis");
        static readonly int SegmentCenterId = Shader.PropertyToID("_SegmentCenter");

        [Header("When")]
        [Tooltip("Lit from the start of this step. Empty: from the start of the game.")]
        [SerializeField] ProgressionStep from;
        [Tooltip("Off from the start of this step. Empty: lit to the end.")]
        [SerializeField] ProgressionStep until;
        [Tooltip("LightGroup id that powers this beacon (e.g. park). While that group is off, the beacon is dark. Empty: no power needed.")]
        [SerializeField] string powerGroup = "";
        [Tooltip("Quebrado até o começo desta etapa: a luz fica fraca e vai e volta devagar entre um mínimo e um máximo. Vazio: nunca quebrado.")]
        [SerializeField] ProgressionStep brokenUntil;
        [Tooltip("Força da luz no máximo da variação, quebrado (1 = a força consertado).")]
        [SerializeField, Range(0f, 1f)] float brokenLevel = 0.3f;
        [Tooltip("Brilho das lâmpadas no máximo da variação, quebrado (1 = o brilho consertado).")]
        [SerializeField, Range(0f, 1f)] float brokenBulbLevel = 0.4f;
        [Tooltip("Mínimo da variação, quebrado, como parte do máximo (0.3 = cai até 30% do máximo).")]
        [SerializeField, Range(0f, 1f)] float brokenMin = 0.3f;
        [Tooltip("Ciclos por segundo da variação, quebrado (0.4 = vai e volta a cada ~2,5 s).")]
        [SerializeField, Min(0.01f)] float brokenSpeed = 0.4f;
        [Tooltip("Funcionando: quanto a luz e algumas lâmpadas oscilam (0 = firme, 0.15 = até 15% mais fraca).")]
        [SerializeField, Range(0f, 0.5f)] float workingVariation = 0.12f;
        [Tooltip("Ciclos por segundo da oscilação funcionando.")]
        [SerializeField, Min(0.01f)] float workingSpeed = 0.6f;
        [Tooltip("Beacons with the same seed (not 0) flicker together, e.g. all the lights of one ride. 0: a random seed.")]
        [SerializeField] int flickerSeed;

        [Header("Light")]
        [Tooltip("Decor light (a booth, a lamp post): it does not take a story slot. The ambient lights nearest to the camera share 8 slots of their own and fade out at the edge.")]
        [SerializeField] bool ambient;
        [SerializeField] Color color = new Color(1f, 0.8f, 0.45f);
        [SerializeField, Min(0f)] float intensity = 2f;
        [Tooltip("Metres the light reaches around the beacon.")]
        [SerializeField, Min(0.1f)] float range = 6f;
        [Tooltip("De onde a luz sai (as lâmpadas). Cada ponto é uma luz com a intensidade dividida entre eles. Vazio: sai deste objeto.")]
        [SerializeField] Transform[] emitFrom = new Transform[0];
        [SerializeField] Pattern pattern = Pattern.Steady;
        [Tooltip("Seconds of one on-off cycle (Blink).")]
        [SerializeField, Min(0.05f)] float blinkPeriod = 1f;
        [Tooltip("Share of the cycle that is on (Blink).")]
        [SerializeField, Range(0.05f, 0.95f)] float blinkOn = 0.5f;

        [Tooltip("Shortest and longest seconds between two dropouts (Sputter).")]
        [SerializeField] Vector2 sputterGap = new Vector2(1.2f, 3.5f);
        [Header("Shown while lit")]
        [Tooltip("Bulbs or a lamp, on while the beacon is lit (and blinking with it).")]
        [SerializeField] Renderer[] renderers = new Renderer[0];
        [Tooltip("On: the renderers always show and only their glow changes (Beacon Bulb: a dead bulb when dark). Off: they hide when dark.")]
        [SerializeField] bool bulbsStayVisible;

        [Tooltip("Thin fog of every beacon's light, per metre from the camera. 0.012: about 30% left at 100 m.")]
        [SerializeField, Min(0f)] float fogDensity = 0.012f;

        float seed, level;
        MaterialPropertyBlock block;

        /// <summary>Broken now (before <see cref="brokenUntil"/>).</summary>
        public bool Broken { get; private set; }

        /// <summary>Lit now (in its step range and powered), whatever the blink phase.</summary>
        public bool Lit { get; private set; }

        public void SetSteady() => pattern = Pattern.Steady;
        public void SetBlink() => pattern = Pattern.Blink;
        public void SetFlicker() => pattern = Pattern.Flicker;

        void Awake() => seed = flickerSeed != 0 ? flickerSeed * 7.31f : Random.value * 100f;

        static float Hash(float n) => Mathf.Repeat(Mathf.Sin(n * 12.9898f + 1.7f) * 43758.5453f, 1f);

        /// <summary>Circuito com defeito: a luz vai e volta devagar entre <see cref="brokenMin"/> e 1, sem apagar. 0..1.</summary>
        float BrokenWave(float t) => Mathf.Lerp(brokenMin, 1f, Mathf.SmoothStep(0f, 1f, Mathf.PerlinNoise(t * brokenSpeed, seed)));

        /// <summary>Funcionando: oscilação pequena, de 1 até 1 - <see cref="workingVariation"/>.</summary>
        float WorkingWave(float t) => 1f - workingVariation * Mathf.PerlinNoise(t * workingSpeed, seed + 13.7f);

        /// <summary>
        /// A tired circuit: on most of the time, with a soft hum. Dropouts come at irregular gaps, last 0.1 to 0.45 s and
        /// stutter on and off. About one gap in four has no dropout. Not the broken mode: see <see cref="BrokenWave"/>. 0..1.
        /// </summary>
        float Sputter(float t)
        {
            float hum = Mathf.Lerp(0.8f, 1f, Mathf.PerlinNoise(t * 3f, seed));
            int index = Mathf.FloorToInt(t / sputterGap.y);
            float offset = Hash(seed + index) * (sputterGap.y - sputterGap.x);
            float local = t - index * sputterGap.y - offset;
            if (local < 0f || Hash(seed * 1.37f + index) < 0.25f) return hum;
            float drop = Mathf.Lerp(0.1f, 0.45f, Hash(seed * 2.11f + index));
            if (local > drop) return hum;
            int tick = Mathf.FloorToInt(local * 18f);
            return Hash(seed * 3.7f + index * 31f + tick) < 0.6f ? Mathf.Lerp(0f, 0.15f, Hash(seed + tick)) : hum * 0.7f;
        }

        void OnEnable() => all.Add(this);

        void OnDisable()
        {
            all.Remove(this);
            if (all.Count == 0) { Shader.SetGlobalFloat(CountId, 0f); Shader.SetGlobalFloat(AmbientCountId, 0f); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { all.Clear(); pushedFrame = -1; }

        bool Powered() => string.IsNullOrEmpty(powerGroup) || LightGroup.IsGroupOn(powerGroup);

        bool BeforeStep(ProgressionStep step)
        {
            var d = ProgressionDirector.Instance;
            if (step == null || d == null) return false;
            int i = d.IndexOf(step);
            return i >= 0 && d.CurrentIndex < i;
        }

        bool InRange()
        {
            var d = ProgressionDirector.Instance;
            if (d == null) return from == null;
            int now = d.CurrentIndex;
            int start = from == null ? 0 : d.IndexOf(from);
            int end = until == null ? int.MaxValue : d.IndexOf(until);
            if (start < 0) return false;
            if (end < 0) end = int.MaxValue;
            return now >= start && now < end;
        }

        void Update()
        {
            Lit = InRange() && Powered();
            Broken = BeforeStep(brokenUntil);
            float t = Time.time;
            switch (pattern)
            {
                case Pattern.Blink: level = Mathf.Repeat(t / blinkPeriod, 1f) < blinkOn ? 1f : 0f; break;
                case Pattern.Flicker:
                    float n = Mathf.PerlinNoise(t * 9f, seed);
                    level = n > 0.32f ? Mathf.Lerp(0.55f, 1f, n) : 0f;
                    break;
                case Pattern.Sputter: level = Sputter(t); break;
                default: level = 1f; break;
            }
            if (!Broken) level *= WorkingWave(t);
            float bulb = level;
            if (Broken) { float f = BrokenWave(t); level = f * brokenLevel; bulb = f * brokenBulbLevel; }
            if (!Lit) { level = 0f; bulb = 0f; }
            bool show = bulbsStayVisible || bulb > 0f;
            block ??= new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (r.enabled != show) r.enabled = show;
                r.GetPropertyBlock(block);
                // The ring of bulbs turns around the thinnest side of its mesh (the carousel: Y; the Ferris wheel: Z).
                if (r.TryGetComponent(out MeshFilter mf) && mf.sharedMesh != null)
                {
                    Bounds mb = mf.sharedMesh.bounds;
                    Vector3 e = mb.size;
                    float axis = e.x <= e.y && e.x <= e.z ? 0f : (e.y <= e.z ? 1f : 2f);
                    block.SetFloat(SegmentAxisId, axis);
                    block.SetVector(SegmentCenterId, mb.center);
                }
                // The bulbs flicker with the light pool; while broken, the shader only picks which bulbs stay dead.
                block.SetFloat(BulbLevelId, bulb);
                block.SetFloat(BulbBrokenId, Broken ? 1f : 0f);
                block.SetFloat(BulbVariationId, workingVariation);
                block.SetFloat(BulbSpeedId, Broken ? brokenSpeed : workingSpeed);
                block.SetFloat(BulbMinId, brokenMin);
                r.SetPropertyBlock(block);
            }
        }

        static void Push(ref int n, Vector3 p, float range, Color c)
        {
            if (n >= Max) return;
            posRange[n] = new Vector4(p.x, p.y, p.z, range);
            colors[n] = new Vector4(c.r, c.g, c.b, 0f);
            n++;
        }

        void LateUpdate()
        {
            if (pushedFrame == Time.frameCount) return;
            pushedFrame = Time.frameCount;
            int n = 0;
            foreach (var b in all)
            {
                if (b.ambient || b.level <= 0f || n >= Max) continue;
                if (b.emitFrom.Length == 0)
                {
                    Push(ref n, b.transform.position, b.range, b.color * (b.intensity * b.level));
                    continue;
                }
                Color share = b.color * (b.intensity * b.level / b.emitFrom.Length);
                foreach (var point in b.emitFrom)
                {
                    if (point == null) throw new MissingReferenceException($"[StepBeacon] '{b.name}': um ponto de 'emitFrom' está vazio.");
                    Push(ref n, point.position, b.range, share);
                }
            }
            Shader.SetGlobalFloat(CountId, n);
            Shader.SetGlobalFloat(FogId, fogDensity);
            Shader.SetGlobalVectorArray(PosId, posRange);
            Shader.SetGlobalVectorArray(ColorId, colors);
            PushAmbient();
        }

        // The ambient lights: the nearest to the camera take the slots. Each one fades out before the first one left out
        // (or before ambientFadeEnd), so a light never pops in or out at full strength.
        static readonly List<(float dist, StepBeacon b)> near = new List<(float, StepBeacon)>();
        const float AmbientFadeEnd = 40f, AmbientBlend = 8f;

        static void PushAmbient()
        {
            near.Clear();
            var cam = Camera.main;
            Vector3 eye = cam != null ? cam.transform.position : Vector3.zero;
            foreach (var b in all)
                if (b.ambient && b.level > 0f)
                {
                    float d = Vector3.Distance(eye, b.transform.position);
                    if (d < AmbientFadeEnd) near.Add((d, b));
                }
            near.Sort((x, y) => x.dist.CompareTo(y.dist));
            float cut = near.Count > AmbientMax ? near[AmbientMax].dist : AmbientFadeEnd;
            int n = 0;
            for (int i = 0; i < near.Count && n < AmbientMax; i++)
            {
                var (d, b) = near[i];
                float w = Mathf.Clamp01((cut - d) / AmbientBlend);
                if (w <= 0f) continue;
                Vector3 p = b.transform.position;
                ambientPos[n] = new Vector4(p.x, p.y, p.z, b.range);
                Color c = b.color * (b.intensity * b.level * w);
                ambientColors[n] = new Vector4(c.r, c.g, c.b, 0f);
                n++;
            }
            Shader.SetGlobalFloat(AmbientCountId, n);
            Shader.SetGlobalVectorArray(AmbientPosId, ambientPos);
            Shader.SetGlobalVectorArray(AmbientColorId, ambientColors);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = color;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
