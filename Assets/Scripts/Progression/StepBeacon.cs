using System.Collections.Generic;
using UnityEngine;

namespace Jamlloween.Progression
{
    /// <summary>
    /// A story light that guides the player to an objective (the carousel, the generator, the Ferris wheel).
    /// It is lit from the step <see cref="from"/> until the step <see cref="until"/> starts (empty: to the end), so a
    /// debug jump shows the right lights. Lit, it lights the scenery around it (WorldBeacons.hlsl: computed in the
    /// shaders, seen through the fog from far away) and shows its <see cref="renderers"/> (bulbs, a little lamp).
    /// At most 8 beacons are lit at once; plan the step ranges so they do not overlap more.
    /// With a <see cref="powerGroup"/>, the beacon is dark while that LightGroup is off (a blackout), in any step.
    /// Before <see cref="brokenUntil"/>, the beacon is broken: a weak flicker, and only some bulbs flicker on.
    /// </summary>
    [DefaultExecutionOrder(100)]   // after the director changes step
    public sealed class StepBeacon : MonoBehaviour
    {
        public enum Pattern { Steady, Blink, Flicker }

        const int Max = 8;
        static readonly List<StepBeacon> all = new List<StepBeacon>();
        static readonly Vector4[] posRange = new Vector4[Max];
        static readonly Vector4[] colors = new Vector4[Max];
        static int pushedFrame = -1;
        static readonly int CountId = Shader.PropertyToID("_BeaconCount");
        static readonly int FogId = Shader.PropertyToID("_BeaconFogDensity");
        static readonly int PosId = Shader.PropertyToID("_BeaconPosRange");
        static readonly int ColorId = Shader.PropertyToID("_BeaconColor");
        static readonly int BulbLevelId = Shader.PropertyToID("_BulbLevel");
        static readonly int BulbBrokenId = Shader.PropertyToID("_BulbBroken");
        static readonly int SegmentAxisId = Shader.PropertyToID("_SegmentAxis");
        static readonly int SegmentCenterId = Shader.PropertyToID("_SegmentCenter");

        [Header("When")]
        [Tooltip("Lit from the start of this step. Empty: from the start of the game.")]
        [SerializeField] ProgressionStep from;
        [Tooltip("Off from the start of this step. Empty: lit to the end.")]
        [SerializeField] ProgressionStep until;
        [Tooltip("LightGroup id that powers this beacon (e.g. park). While that group is off, the beacon is dark. Empty: no power needed.")]
        [SerializeField] string powerGroup = "";
        [Tooltip("Broken until the start of this step: dark most of the time, with short weak bursts, and only some bulbs take part. Empty: never broken.")]
        [SerializeField] ProgressionStep brokenUntil;
        [Tooltip("Strength of the light pool at the peak of a broken burst.")]
        [SerializeField, Range(0f, 1f)] float brokenLevel = 0.3f;
        [Tooltip("Glow of the bulbs at the peak of a broken burst (1 = the glow when fixed).")]
        [SerializeField, Range(0f, 1f)] float brokenBulbLevel = 0.4f;
        [Tooltip("Shortest and longest seconds between two broken bursts.")]
        [SerializeField] Vector2 brokenGap = new Vector2(1f, 2.5f);
        [Tooltip("Beacons with the same seed (not 0) flicker together, e.g. all the lights of one ride. 0: a random seed.")]
        [SerializeField] int flickerSeed;

        [Header("Light")]
        [SerializeField] Color color = new Color(1f, 0.8f, 0.45f);
        [SerializeField, Min(0f)] float intensity = 2f;
        [Tooltip("Metres the light reaches around the beacon.")]
        [SerializeField, Min(0.1f)] float range = 6f;
        [SerializeField] Pattern pattern = Pattern.Steady;
        [Tooltip("Seconds of one on-off cycle (Blink).")]
        [SerializeField, Min(0.05f)] float blinkPeriod = 1f;
        [Tooltip("Share of the cycle that is on (Blink).")]
        [SerializeField, Range(0.05f, 0.95f)] float blinkOn = 0.5f;

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

        /// <summary>
        /// A faulty circuit: dark most of the time (on for about 10%). Bursts come at irregular gaps, last 0.15 to 0.8 s,
        /// stutter on and off, and reach only 60 to 100% of the peak. About one gap in five has no burst at all. 0..1.
        /// </summary>
        float BrokenFlicker(float t)
        {
            // Find the cycle that holds t: each cycle has its own random length, from the seed and its index.
            // Cycles lie on a grid of the longest gap; each burst starts late in its cycle by a random share.
            int index = Mathf.FloorToInt(t / brokenGap.y);
            float start = index * brokenGap.y;
            float offset = Hash(seed + index) * (brokenGap.y - brokenGap.x);
            float local = t - start - offset;
            if (local < 0f || Hash(seed * 1.37f + index) < 0.2f) return 0f;
            float burst = Mathf.Lerp(0.15f, 0.8f, Hash(seed * 2.11f + index));
            if (local > burst) return 0f;
            int tick = Mathf.FloorToInt(local * 22f);
            if (Hash(seed * 3.7f + index * 31f + tick) < 0.35f) return 0f;   // the stutter inside a burst
            return Mathf.Lerp(0.6f, 1f, Hash(seed * 5.3f + index * 17f + tick));
        }

        void OnEnable() => all.Add(this);

        void OnDisable()
        {
            all.Remove(this);
            if (all.Count == 0) Shader.SetGlobalFloat(CountId, 0f);
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
                default: level = 1f; break;
            }
            float bulb = level;
            if (Broken) { float f = BrokenFlicker(t); level = f * brokenLevel; bulb = f * brokenBulbLevel; }
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
                r.SetPropertyBlock(block);
            }
        }

        void LateUpdate()
        {
            if (pushedFrame == Time.frameCount) return;
            pushedFrame = Time.frameCount;
            int n = 0;
            foreach (var b in all)
            {
                if (b.level <= 0f || n >= Max) continue;
                Vector3 p = b.transform.position;
                posRange[n] = new Vector4(p.x, p.y, p.z, b.range);
                Color c = b.color * (b.intensity * b.level);
                colors[n] = new Vector4(c.r, c.g, c.b, 0f);
                n++;
            }
            Shader.SetGlobalFloat(CountId, n);
            Shader.SetGlobalFloat(FogId, fogDensity);
            Shader.SetGlobalVectorArray(PosId, posRange);
            Shader.SetGlobalVectorArray(ColorId, colors);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = color;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
