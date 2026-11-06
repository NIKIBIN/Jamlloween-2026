using System;
using UnityEngine;

namespace Jamlloween.Zatan
{
    /// <summary>
    /// A música da perseguição: entra quando o Zatan avista o jogador e toca enquanto ele persegue ou procura.
    /// Sai com um fade quando a perseguição acaba (ele desiste, some, prende o jogador ou começa uma cena).
    /// Uma perseguição nova, depois que a música terminou de sair, começa a trilha do início.
    /// </summary>
    public sealed class ZatanChaseMusic : MonoBehaviour
    {
        [SerializeField] ZatanBehaviourBrain brain;
        [Tooltip("Fonte 2D com a trilha da perseguição (loop).")]
        [SerializeField] AudioSource music;
        [SerializeField, Range(0f, 1f)] float volume = 0.8f;
        [Tooltip("Segundos para a música entrar.")]
        [SerializeField, Min(0f)] float fadeIn = 0.6f;
        [Tooltip("Segundos para a música sair.")]
        [SerializeField, Min(0.01f)] float fadeOut = 2.5f;
        [Tooltip("Procurando o jogador (Search) também conta como perseguição.")]
        [SerializeField] bool keepWhileSearching = true;

        bool chasing;

        void Awake()
        {
            if (brain == null) throw new InvalidOperationException($"[{name}] ZatanChaseMusic sem 'brain'.");
            if (music == null) throw new InvalidOperationException($"[{name}] ZatanChaseMusic sem 'music'.");
            if (music.clip == null) throw new InvalidOperationException($"[{name}] a fonte '{music.name}' não tem a trilha.");
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            music.volume = 0f;
        }

        void OnEnable()
        {
            brain.StateChanged += OnStateChanged;
            OnStateChanged(brain.State);
        }

        void OnDisable()
        {
            brain.StateChanged -= OnStateChanged;
            music.Stop();
        }

        void OnStateChanged(ZatanBehaviourState state)
        {
            chasing = state == ZatanBehaviourState.Spotted || state == ZatanBehaviourState.Chase
                      || (keepWhileSearching && state == ZatanBehaviourState.Search);
            if (chasing && !music.isPlaying)
            {
                music.time = 0f;
                music.Play();
            }
        }

        void Update()
        {
            if (!music.isPlaying) return;
            float target = chasing ? volume : 0f;
            float speed = volume / (chasing ? Mathf.Max(fadeIn, 0.01f) : fadeOut);
            music.volume = Mathf.MoveTowards(music.volume, target, speed * Time.deltaTime);
            if (!chasing && music.volume <= 0f) music.Stop();
        }
    }
}
