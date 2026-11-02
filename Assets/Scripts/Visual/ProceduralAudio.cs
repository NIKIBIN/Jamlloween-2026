using UnityEngine;

namespace Circus
{
    /// <summary>Placeholder sounds generated in code (no audio assets).</summary>
    public static class ProceduralAudio
    {
        const int Rate = 22050;

        static AudioClip Make(string name, float seconds, System.Func<int, float, float> f)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i, i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Heavy thud: low sine burst + filtered noise.</summary>
        public static AudioClip Footstep()
        {
            var rng = new System.Random(1);
            float lp = 0;
            return Make("zatan_step", 0.28f, (i, t) =>
            {
                float env = Mathf.Exp(-t * 18f);
                float n = (float)rng.NextDouble() * 2 - 1;
                lp += (n - lp) * 0.08f;
                return (Mathf.Sin(2 * Mathf.PI * (70f - 40f * t) * t) * 0.9f + lp * 1.6f) * env;
            });
        }

        /// <summary>Metallic creak layered on the step (animatronic joints).</summary>
        public static AudioClip Creak()
        {
            return Make("zatan_creak", 0.35f, (i, t) =>
            {
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.35f));
                float f = 620f + 180f * Mathf.Sin(t * 37f);
                return Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * t)) * 0.12f * env * (0.6f + 0.4f * Mathf.Sin(t * 210f));
            });
        }

        public static AudioClip Beep()
        {
            return Make("repair_beep", 0.09f, (i, t) => Mathf.Sin(2 * Mathf.PI * 1200f * t) * 0.35f * Mathf.Clamp01(1f - t / 0.09f));
        }

        /// <summary>Looping tape hiss with a slow wow.</summary>
        public static AudioClip Hiss()
        {
            var rng = new System.Random(7);
            float hp = 0, prev = 0;
            return Make("tape_hiss", 4f, (i, t) =>
            {
                float n = (float)rng.NextDouble() * 2 - 1;
                hp = 0.92f * (hp + n - prev); prev = n;
                float hum = Mathf.Sin(2 * Mathf.PI * 60f * t) * 0.05f;
                return hp * 0.25f * (0.85f + 0.15f * Mathf.Sin(2 * Mathf.PI * 0.5f * t)) + hum;
            });
        }

        /// <summary>Detuned music-box waltz loop for the carousel.</summary>
        public static AudioClip CarouselMusic()
        {
            // a simple 3/4 phrase in A minor (semitones from A4)
            int[] melody = { 0, 3, 7, 12, 7, 3, 2, 5, 8, 14, 8, 5, -2, 3, 7, 10, 7, 3, -5, 2, 5, 9, 5, 2 };
            float noteLen = 0.28f;
            float total = melody.Length * noteLen;
            return Make("carousel_music", total, (i, t) =>
            {
                int k = Mathf.Min(melody.Length - 1, (int)(t / noteLen));
                float nt = t - k * noteLen;
                float wow = 1f + 0.012f * Mathf.Sin(2 * Mathf.PI * 0.7f * t);
                float f = 440f * Mathf.Pow(2f, melody[k] / 12f) * wow;
                float env = Mathf.Exp(-nt * 6f);
                float tone = Mathf.Sin(2 * Mathf.PI * f * t) + 0.5f * Mathf.Sin(2 * Mathf.PI * f * 2.01f * t) + 0.25f * Mathf.Sin(2 * Mathf.PI * f * 3.03f * t);
                float bass = (k % 3 == 0) ? Mathf.Sin(2 * Mathf.PI * f * 0.25f * t) * Mathf.Exp(-nt * 4f) * 0.6f : 0f;
                return (tone * env * 0.3f + bass * 0.4f) * 0.8f;
            });
        }
    }
}
