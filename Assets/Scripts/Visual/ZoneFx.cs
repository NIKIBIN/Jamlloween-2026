using UnityEngine;

namespace Circus
{
    /// <summary>Small zone-identity effects: light flicker, spinning beacon, chasing marquee bulbs.</summary>
    public class ZoneFx : MonoBehaviour
    {
        public enum Kind { Flicker, Spin, Chase, Pulse }

        public Kind kind;
        public Light targetLight;
        [Tooltip("Flicker/Pulse: fraction of intensity that varies.")] public float amount = 0.25f;
        public float speed = 1f;
        [Tooltip("Spin: degrees per second.")] public float spinSpeed = 180f;
        public Renderer[] bulbs;
        public Material bulbOn, bulbOff;

        float baseIntensity;
        int lastChase = -1;

        void Start() { if (targetLight) baseIntensity = targetLight.intensity; }

        void Update()
        {
            switch (kind)
            {
                case Kind.Flicker:
                    if (!targetLight) return;
                    float n = Mathf.PerlinNoise(Time.time * 9f * speed, 0.3f);
                    bool drop = Mathf.PerlinNoise(Time.time * 2.3f * speed, 7f) > 0.82f;   // rare tube stutter
                    targetLight.intensity = baseIntensity * (1f - amount * n) * (drop ? 0.35f : 1f);
                    break;
                case Kind.Pulse:
                    if (targetLight) targetLight.intensity = baseIntensity * (1f - amount + amount * (0.5f + 0.5f * Mathf.Sin(Time.time * speed * 6.28f)));
                    break;
                case Kind.Spin:
                    transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
                    break;
                case Kind.Chase:
                    if (bulbs == null || bulbs.Length == 0) return;
                    int step = Mathf.FloorToInt(Time.time * speed * 6f);
                    if (step == lastChase) return;
                    lastChase = step;
                    for (int i = 0; i < bulbs.Length; i++)
                        bulbs[i].sharedMaterial = (i + step) % 3 == 0 ? bulbOff : bulbOn;
                    break;
            }
        }
    }
}
