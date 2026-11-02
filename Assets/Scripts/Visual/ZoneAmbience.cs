using UnityEngine;

namespace Circus
{
    /// <summary>Looping 3D ambience that identifies a zone by ear. Clips generated in code.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class ZoneAmbience : MonoBehaviour
    {
        public enum Kind { FluorescentBuzz, FortuneChimes, MonitorWhine, GeneratorHum, CanvasWind }

        public Kind kind;
        public float volume = 0.5f;
        public float maxDistance = 30f;
        [Tooltip("Optional: volume multiplier once this machine is repaired.")] public string louderAfterMachine;
        public float repairedVolumeMult = 1.6f;

        AudioSource src;

        void Start()
        {
            src = GetComponent<AudioSource>();
            src.clip = Make(kind);
            src.loop = true;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1.5f;
            src.maxDistance = maxDistance;
            src.dopplerLevel = 0f;
            src.volume = volume;
            src.time = Random.value * src.clip.length * 0.9f;
            src.Play();
        }

        void Update()
        {
            if (src == null || string.IsNullOrEmpty(louderAfterMachine)) return;
            src.volume = volume * (RepairableMachine.IsDone(louderAfterMachine) ? repairedVolumeMult : 1f);
        }

        const int Rate = 22050;

        static AudioClip Make(Kind k)
        {
            float len = k == Kind.FortuneChimes ? 9f : 4f;
            int n = (int)(len * Rate);
            var d = new float[n];
            var rng = new System.Random((int)k + 11);
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                float v = 0f;
                switch (k)
                {
                    case Kind.FluorescentBuzz:   // 120 Hz mains buzz, rich odd harmonics, slight crackle
                        v = 0.30f * Mathf.Sign(Mathf.Sin(2 * Mathf.PI * 120f * t)) * 0.4f
                            + 0.15f * Mathf.Sin(2 * Mathf.PI * 240f * t) + 0.08f * Mathf.Sin(2 * Mathf.PI * 360f * t)
                            + (rng.NextDouble() < 0.0008 ? noise * 0.8f : noise * 0.02f);
                        break;
                    case Kind.FortuneChimes:     // sparse detuned bells
                    {
                        float[] times = { 0.2f, 2.1f, 3.4f, 5.9f, 7.3f };
                        float[] freqs = { 880f, 1046.5f, 932.3f, 1174.7f, 830.6f };
                        for (int b = 0; b < times.Length; b++)
                        {
                            float dt = t - times[b];
                            if (dt < 0f || dt > 3f) continue;
                            float e = Mathf.Exp(-dt * 1.6f);
                            v += e * (Mathf.Sin(2 * Mathf.PI * freqs[b] * dt) + 0.4f * Mathf.Sin(2 * Mathf.PI * freqs[b] * 2.76f * dt)) * 0.25f;
                        }
                        lp += (noise - lp) * 0.01f;
                        v += lp * 0.15f;
                        break;
                    }
                    case Kind.MonitorWhine:      // CRT flyback whine + TV static
                        v = 0.10f * Mathf.Sin(2 * Mathf.PI * 7800f * t) + noise * 0.12f * (0.7f + 0.3f * Mathf.Sin(t * 3f));
                        break;
                    case Kind.GeneratorHum:      // low hum 47 Hz + harmonics, wobble
                    {
                        float wob = 1f + 0.03f * Mathf.Sin(2 * Mathf.PI * 0.5f * t);
                        v = 0.55f * Mathf.Sin(2 * Mathf.PI * 47f * wob * t) + 0.25f * Mathf.Sin(2 * Mathf.PI * 94f * t)
                            + 0.12f * Mathf.Sin(2 * Mathf.PI * 141f * t);
                        lp += (noise - lp) * 0.05f;
                        v += lp * 0.4f;
                        break;
                    }
                    case Kind.CanvasWind:        // low-passed wind swells + canvas flaps
                    {
                        lp += (noise - lp) * 0.02f;
                        lp2 += (lp - lp2) * 0.1f;
                        float swell = 0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * 0.25f * t);
                        float flap = Mathf.Repeat(t, 1.37f) < 0.05f ? noise * 0.4f : 0f;
                        v = lp2 * 3f * swell + flap;
                        break;
                    }
                }
                d[i] = Mathf.Clamp(v, -1f, 1f);
            }
            // crossfade the loop seam
            int fade = Rate / 10;
            for (int i = 0; i < fade; i++) { float a = i / (float)fade; d[i] = d[i] * a + d[n - fade + i] * (1 - a); }
            var clip = AudioClip.Create("amb_" + k, n - fade, 1, Rate, false);
            var trimmed = new float[n - fade];
            System.Array.Copy(d, trimmed, n - fade);
            clip.SetData(trimmed, 0);
            return clip;
        }
    }
}
