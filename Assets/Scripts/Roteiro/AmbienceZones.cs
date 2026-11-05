using System;
using UnityEngine;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// A ambientação geral (2D, em loop). Toca sempre que o jogador está fora do trailer e some dentro dele;
    /// dentro da tenda do circo fica abafada (low-pass e um pouco mais baixa). As zonas são caixas: só a forma
    /// do BoxCollider é usada, então ele pode ficar desligado e não atrapalha raycasts.
    /// </summary>
    [RequireComponent(typeof(AudioSource), typeof(AudioLowPassFilter))]
    public sealed class AmbienceZones : MonoBehaviour
    {
        [Tooltip("O jogador (a raiz, nos pés).")]
        [SerializeField] Transform player;
        [Tooltip("Caixa do interior do trailer (a mesma do ZatanSafeRoom): dentro dela a ambientação some.")]
        [SerializeField] BoxCollider trailer;
        [Tooltip("Caixa que cobre a tenda do circo inteira.")]
        [SerializeField] BoxCollider bigTop;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] float volume = 0.8f;
        [SerializeField, Range(0f, 1f)] float bigTopVolume = 0.6f;
        [SerializeField] float openCutoff = 22000f;
        [SerializeField] float bigTopCutoff = 1000f;
        [Tooltip("Segundos para mudar de uma zona para outra.")]
        [SerializeField, Min(0.05f)] float fadeSeconds = 1.2f;

        AudioSource source;
        AudioLowPassFilter lowPass;
        float gain, cutoffT;

        Vector3 PlayerPoint => player.position + Vector3.up * 0.5f;
        public bool InTrailer => Inside(trailer, PlayerPoint);
        public bool InBigTop => Inside(bigTop, PlayerPoint);

        void Awake()
        {
            if (player == null) throw new InvalidOperationException($"[{name}] AmbienceZones sem 'player'.");
            if (trailer == null) throw new InvalidOperationException($"[{name}] AmbienceZones sem 'trailer'.");
            if (bigTop == null) throw new InvalidOperationException($"[{name}] AmbienceZones sem 'bigTop'.");
            source = GetComponent<AudioSource>();
            lowPass = GetComponent<AudioLowPassFilter>();
            if (source.clip == null) throw new InvalidOperationException($"[{name}] a fonte da ambientação não tem clip.");
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        void Start()
        {
            Apply(true);
            source.Play();
        }

        void Update() => Apply(false);

        void Apply(bool instant)
        {
            bool inTrailer = InTrailer;
            bool inBigTop = !inTrailer && InBigTop;
            float targetGain = inTrailer ? 0f : inBigTop ? bigTopVolume : volume;
            float targetCut = inBigTop ? 1f : 0f;
            float k = instant ? 1f : Mathf.Clamp01(Time.deltaTime / fadeSeconds);
            gain = Mathf.Lerp(gain, targetGain, instant ? 1f : k * 3f);
            cutoffT = Mathf.Lerp(cutoffT, targetCut, instant ? 1f : k * 3f);
            source.volume = gain;
            // Interpola em escala logarítmica: a mudança de timbre soa uniforme.
            lowPass.cutoffFrequency = Mathf.Exp(Mathf.Lerp(Mathf.Log(openCutoff), Mathf.Log(bigTopCutoff), cutoffT));
        }

        static bool Inside(BoxCollider box, Vector3 world)
        {
            Vector3 local = box.transform.InverseTransformPoint(world) - box.center;
            Vector3 half = box.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }
    }
}
