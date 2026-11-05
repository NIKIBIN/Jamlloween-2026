using System;
using System.Collections;
using Jamlloween.Progression;
using UnityEngine;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// Toca um som em loop desde o começo do jogo até o diretor emitir um evento nomeado, e então o desliga
    /// com um fade curto. Uso: o gerador funcionando enquanto o parque tem energia, até o apagão (PowerLost).
    /// Também vale no pulo de etapa do debug (F4), porque o diretor reemite os eventos das etapas puladas.
    /// </summary>
    public sealed class LoopUntilNamedEvent : MonoBehaviour
    {
        [SerializeField] ProgressionDirector director;
        [Tooltip("Fonte já configurada (clip, loop, 3D). Começa a tocar no Start.")]
        [SerializeField] AudioSource source;
        [SerializeField] string stopEvent = "PowerLost";
        [SerializeField, Min(0f)] float fadeSeconds = 1.5f;

        bool stopped;

        void Awake()
        {
            if (director == null) throw new InvalidOperationException($"[{name}] LoopUntilNamedEvent sem 'director'.");
            if (source == null) throw new InvalidOperationException($"[{name}] LoopUntilNamedEvent sem 'source'.");
            if (source.clip == null) throw new InvalidOperationException($"[{name}] a fonte '{source.name}' não tem clip.");
            if (string.IsNullOrEmpty(stopEvent)) throw new InvalidOperationException($"[{name}] LoopUntilNamedEvent sem 'stopEvent'.");
            source.loop = true;
            source.playOnAwake = false;
        }

        void OnEnable() => director.OnNamedEvent += OnNamedEvent;

        void OnDisable() => director.OnNamedEvent -= OnNamedEvent;

        void Start()
        {
            if (!stopped) source.Play();
        }

        void OnNamedEvent(string id)
        {
            if (stopped || id != stopEvent) return;
            stopped = true;
            StartCoroutine(FadeOut());
        }

        IEnumerator FadeOut()
        {
            float start = source.volume;
            for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
            {
                source.volume = Mathf.Lerp(start, 0f, t / fadeSeconds);
                yield return null;
            }
            source.Stop();
            source.volume = start;
        }
    }
}
