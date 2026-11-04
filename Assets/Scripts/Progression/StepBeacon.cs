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

        [Header("When")]
        [Tooltip("Lit from the start of this step. Empty: from the start of the game.")]
        [SerializeField] ProgressionStep from;
        [Tooltip("Off from the start of this step. Empty: lit to the end.")]
        [SerializeField] ProgressionStep until;

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

        [Tooltip("Thin fog of every beacon's light, per metre from the camera. 0.012: about 30% left at 100 m.")]
        [SerializeField, Min(0f)] float fogDensity = 0.012f;

        float seed, level;

        /// <summary>Lit now (in its step range), whatever the blink phase.</summary>
        public bool Lit { get; private set; }

        public void SetSteady() => pattern = Pattern.Steady;
        public void SetBlink() => pattern = Pattern.Blink;
        public void SetFlicker() => pattern = Pattern.Flicker;

        void Awake() => seed = Random.value * 100f;

        void OnEnable() => all.Add(this);

        void OnDisable()
        {
            all.Remove(this);
            if (all.Count == 0) Shader.SetGlobalFloat(CountId, 0f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { all.Clear(); pushedFrame = -1; }

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
            Lit = InRange();
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
            if (!Lit) level = 0f;
            bool show = level > 0f;
            foreach (var r in renderers) if (r != null && r.enabled != show) r.enabled = show;
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
