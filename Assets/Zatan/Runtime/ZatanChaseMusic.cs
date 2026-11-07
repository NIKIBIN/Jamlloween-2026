using System;
using UnityEngine;

namespace Jamlloween.Zatan
{
    /// <summary>
    /// A música da perseguição: quando o Zatan avista o jogador, toca a abertura (uma vez) e, emendado no fim dela pelo
    /// relógio de áudio, o tema em loop enquanto ele persegue ou procura. Sai com um fade quando a perseguição acaba (ele
    /// desiste, some, prende o jogador ou começa uma cena). Uma perseguição nova, depois que a música terminou de sair,
    /// começa de novo pela abertura; se ela voltar durante o fade, a música só volta a subir, sem recomeçar.
    /// Uma cena pode adiantar a música com <see cref="Cue"/> (ex.: o Zatan aparece se contorcendo antes de correr).
    /// </summary>
    public sealed class ZatanChaseMusic : MonoBehaviour
    {
        [SerializeField] ZatanBehaviourBrain brain;
        [Tooltip("Fonte 2D com a abertura da perseguição (toca uma vez, no começo de cada perseguição).")]
        [SerializeField] AudioSource intro;
        [Tooltip("Fonte 2D com o tema da perseguição sem a introdução (loop), emendado no fim da abertura.")]
        [SerializeField] AudioSource music;
        [SerializeField, Range(0f, 1f)] float volume = 0.8f;
        [Tooltip("Segundos para a música entrar.")]
        [SerializeField, Min(0f)] float fadeIn = 0.6f;
        [Tooltip("Segundos para a música sair.")]
        [SerializeField, Min(0.01f)] float fadeOut = 2.5f;
        [Tooltip("Procurando o jogador (Search) também conta como perseguição.")]
        [SerializeField] bool keepWhileSearching = true;

        bool chasing, cued, active;
        float level;

        void Awake()
        {
            if (brain == null) throw new InvalidOperationException($"[{name}] ZatanChaseMusic sem 'brain'.");
            if (intro == null) throw new InvalidOperationException($"[{name}] ZatanChaseMusic sem 'intro'.");
            if (music == null) throw new InvalidOperationException($"[{name}] ZatanChaseMusic sem 'music'.");
            if (intro.clip == null) throw new InvalidOperationException($"[{name}] a fonte '{intro.name}' não tem a abertura.");
            if (music.clip == null) throw new InvalidOperationException($"[{name}] a fonte '{music.name}' não tem o tema.");
            if (intro == music) throw new InvalidOperationException($"[{name}] abertura e tema precisam de fontes diferentes.");
            Setup(intro, false);
            Setup(music, true);
        }

        static void Setup(AudioSource source, bool loop)
        {
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
        }

        void OnEnable()
        {
            brain.StateChanged += OnStateChanged;
            OnStateChanged(brain.State);
        }

        void OnDisable()
        {
            brain.StateChanged -= OnStateChanged;
            StopAll();
        }

        /// <summary>A perseguição começa agora, mesmo que ele ainda esteja numa cena (Scripted): a música já entra.</summary>
        public void Cue()
        {
            cued = true;
            OnStateChanged(brain.State);
        }

        void OnStateChanged(ZatanBehaviourState state)
        {
            bool hunting = state == ZatanBehaviourState.Spotted || state == ZatanBehaviourState.Chase
                           || (keepWhileSearching && state == ZatanBehaviourState.Search);
            if (!hunting && state != ZatanBehaviourState.Scripted) cued = false;   // a perseguição adiantada acabou
            chasing = hunting || (cued && state == ZatanBehaviourState.Scripted);
            if (chasing && !active) Begin();
        }

        /// <summary>The opening now, the theme scheduled on the audio clock to start exactly when the opening ends.</summary>
        void Begin()
        {
            active = true;
            double start = AudioSettings.dspTime + 0.05;
            double introLength = (double)intro.clip.samples / intro.clip.frequency;
            intro.time = 0f;
            intro.PlayScheduled(start);
            music.time = 0f;
            music.PlayScheduled(start + introLength);
        }

        void StopAll()
        {
            active = false;
            level = 0f;
            intro.Stop();
            music.Stop();
            intro.volume = 0f;
            music.volume = 0f;
        }

        void Update()
        {
            if (!active) return;
            float target = chasing ? volume : 0f;
            float speed = volume / (chasing ? Mathf.Max(fadeIn, 0.01f) : fadeOut);
            level = Mathf.MoveTowards(level, target, speed * Time.deltaTime);
            intro.volume = level;
            music.volume = level;
            if (!chasing && level <= 0f) StopAll();
        }
    }
}
