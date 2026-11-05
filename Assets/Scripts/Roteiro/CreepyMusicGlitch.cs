using System;
using UnityEngine;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// Deixa uma música "quebrada": o tom oscila devagar como fita gasta (wow/flutter), de vez em quando cai
    /// como um motor morrendo, a música falha (some ou gagueja por um instante) e o low-pass respira. Usa a
    /// AudioSource do mesmo objeto; os filtros deste objeto só afetam ela.
    /// </summary>
    [RequireComponent(typeof(AudioSource), typeof(AudioLowPassFilter), typeof(AudioDistortionFilter))]
    public sealed class CreepyMusicGlitch : MonoBehaviour
    {
        [Header("Tom")]
        [SerializeField, Range(0f, 0.2f)] float wow = 0.05f;
        [SerializeField, Range(0f, 0.05f)] float flutter = 0.012f;
        [Tooltip("Intervalo entre as quedas de tom (s).")]
        [SerializeField] Vector2 sagInterval = new Vector2(6f, 14f);
        [SerializeField, Range(0f, 0.6f)] float sagDepth = 0.35f;
        [SerializeField] float sagSeconds = 1.6f;

        [Header("Falhas")]
        [SerializeField] Vector2 dropoutInterval = new Vector2(3f, 9f);
        [SerializeField] Vector2 dropoutSeconds = new Vector2(0.06f, 0.35f);
        [Tooltip("Chance de a falha virar uma gagueira (a música volta um pedacinho).")]
        [SerializeField, Range(0f, 1f)] float stutterChance = 0.35f;

        [Header("Timbre")]
        [SerializeField] float cutoffHigh = 6000f;
        [SerializeField] float cutoffLow = 1400f;
        [SerializeField, Range(0f, 1f)] float distortion = 0.35f;

        AudioSource source;
        AudioLowPassFilter lowPass;
        float baseVolume, nextSag, sagStart = -99f, nextDrop, dropEnd, seed;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            lowPass = GetComponent<AudioLowPassFilter>();
            var dist = GetComponent<AudioDistortionFilter>();
            dist.distortionLevel = distortion;
            seed = UnityEngine.Random.value * 100f;
        }

        void OnEnable()
        {
            nextSag = Time.time + UnityEngine.Random.Range(sagInterval.x, sagInterval.y);
            nextDrop = Time.time + UnityEngine.Random.Range(dropoutInterval.x, dropoutInterval.y);
        }

        void Update()
        {
            if (!source.isPlaying) { baseVolume = 0f; return; }
            if (baseVolume <= 0f) baseVolume = source.volume;   // volume que quem deu Play escolheu
            float t = Time.time;

            // Tom: fita gasta + quedas ocasionais.
            float pitch = 1f + wow * (Mathf.PerlinNoise(seed, t * 0.35f) - 0.5f) * 2f
                             + flutter * Mathf.Sin(t * 37f + seed);
            if (t >= nextSag) { sagStart = t; nextSag = t + UnityEngine.Random.Range(sagInterval.x, sagInterval.y); }
            float s = (t - sagStart) / sagSeconds;
            if (s >= 0f && s <= 1f) pitch -= sagDepth * Mathf.Sin(s * Mathf.PI);
            source.pitch = Mathf.Max(0.3f, pitch);

            // Falhas: some por um instante; às vezes gagueja (volta um pedaço).
            if (t >= nextDrop)
            {
                dropEnd = t + UnityEngine.Random.Range(dropoutSeconds.x, dropoutSeconds.y);
                nextDrop = t + UnityEngine.Random.Range(dropoutInterval.x, dropoutInterval.y);
                if (UnityEngine.Random.value < stutterChance && source.clip != null)
                    source.time = Mathf.Max(0f, source.time - UnityEngine.Random.Range(0.08f, 0.25f));
            }
            source.volume = t < dropEnd ? baseVolume * 0.05f : baseVolume;

            // Timbre: o low-pass respira devagar.
            float breathe = Mathf.PerlinNoise(seed + 7f, t * 0.2f);
            lowPass.cutoffFrequency = Mathf.Lerp(cutoffLow, cutoffHigh, breathe);
        }

        void OnDisable() => baseVolume = 0f;
    }
}
