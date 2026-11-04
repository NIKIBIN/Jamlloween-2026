using UnityEngine;

namespace Jamlloween.Zatan
{
    /// <summary>Everything Zatan sounds like except his footsteps: loops per state, one-shots per transition, volume by tension.</summary>
    public sealed class ZatanAudio : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] ZatanBehaviourBrain brain;
        [SerializeField] ZatanTension tension;
        [SerializeField] AudioSource idleLoopSource;
        [SerializeField] AudioSource chaseLoopSource;
        [SerializeField] AudioSource nearTickSource;
        [SerializeField] AudioSource oneShotSource;

        [Header("Loops")]
        [SerializeField] AudioClip clockworkLoop;
        [SerializeField] AudioClip chaseLoop;
        [SerializeField] AudioClip nearTickLoop;

        [Header("Transições")]
        [SerializeField] AudioClip alertSting;
        [SerializeField] AudioClip investigateServo;
        [SerializeField] AudioClip lostWindDown;
        [SerializeField] AudioClip caughtSting;
        [SerializeField] AudioClip vanishCards;
        [SerializeField] AudioClip reappearMusicBox;
        [SerializeField] AudioClip[] voiceMutters;

        [Header("Ajustes")]
        [SerializeField] float nearTickDistance = 10f;
        [SerializeField] float loopFadeSpeed = 2.5f;
        [SerializeField] float voiceIntervalMin = 15f;
        [SerializeField] float voiceIntervalMax = 35f;

        public AudioClip LastOneShot { get; private set; }

        ZatanBehaviourState previous;
        float voiceTimer;

        void Awake()
        {
            if (brain == null) throw new MissingReferenceException(nameof(brain));
            if (tension == null) throw new MissingReferenceException(nameof(tension));
            if (idleLoopSource == null) throw new MissingReferenceException(nameof(idleLoopSource));
            if (chaseLoopSource == null) throw new MissingReferenceException(nameof(chaseLoopSource));
            if (nearTickSource == null) throw new MissingReferenceException(nameof(nearTickSource));
            if (oneShotSource == null) throw new MissingReferenceException(nameof(oneShotSource));
            if (clockworkLoop == null) throw new MissingReferenceException(nameof(clockworkLoop));
            if (chaseLoop == null) throw new MissingReferenceException(nameof(chaseLoop));
            if (nearTickLoop == null) throw new MissingReferenceException(nameof(nearTickLoop));
            if (alertSting == null) throw new MissingReferenceException(nameof(alertSting));
            if (investigateServo == null) throw new MissingReferenceException(nameof(investigateServo));
            if (lostWindDown == null) throw new MissingReferenceException(nameof(lostWindDown));
            if (caughtSting == null) throw new MissingReferenceException(nameof(caughtSting));
            if (vanishCards == null) throw new MissingReferenceException(nameof(vanishCards));
            if (reappearMusicBox == null) throw new MissingReferenceException(nameof(reappearMusicBox));
            if (voiceMutters == null || voiceMutters.Length == 0) throw new MissingReferenceException(nameof(voiceMutters));

            StartLoop(idleLoopSource, clockworkLoop);
            StartLoop(chaseLoopSource, chaseLoop);
            StartLoop(nearTickSource, nearTickLoop);
            previous = brain.State;
            ResetVoiceTimer();
        }

        void OnEnable() => brain.StateChanged += OnStateChanged;

        void OnDisable() => brain.StateChanged -= OnStateChanged;

        void Update()
        {
            ZatanBehaviourState state = brain.State;
            bool audible = state != ZatanBehaviourState.Vanished && state != ZatanBehaviourState.Caught;
            bool chasing = brain.IsHunting;
            float noise = tension.Tuning.FootstepVolume;

            Fade(idleLoopSource, audible && !chasing ? noise : 0f);
            Fade(chaseLoopSource, chasing ? noise : 0f);
            float closeness = Mathf.Clamp01(1f - brain.DistanceToPlayer / nearTickDistance);
            Fade(nearTickSource, audible ? closeness : 0f);

            if (state != ZatanBehaviourState.Patrol && state != ZatanBehaviourState.Search) return;
            voiceTimer -= Time.deltaTime;
            if (voiceTimer > 0f) return;
            ResetVoiceTimer();
            PlayOneShot(voiceMutters[Random.Range(0, voiceMutters.Length)], noise);
        }

        void OnStateChanged(ZatanBehaviourState next)
        {
            float noise = tension.Tuning.FootstepVolume;
            switch (next)
            {
                case ZatanBehaviourState.Spotted: PlayOneShot(alertSting, 1f); break;
                case ZatanBehaviourState.Investigate: PlayOneShot(investigateServo, noise); break;
                case ZatanBehaviourState.Search: PlayOneShot(lostWindDown, noise); break;
                case ZatanBehaviourState.Caught: PlayOneShot(caughtSting, 1f); break;
                case ZatanBehaviourState.Vanished: PlayOneShot(vanishCards, noise); break;
                case ZatanBehaviourState.Patrol:
                    if (previous == ZatanBehaviourState.Vanished) PlayOneShot(reappearMusicBox, noise);
                    break;
            }
            previous = next;
        }

        void PlayOneShot(AudioClip clip, float volume)
        {
            LastOneShot = clip;
            oneShotSource.PlayOneShot(clip, volume);
        }

        void Fade(AudioSource source, float volume) =>
            source.volume = Mathf.MoveTowards(source.volume, volume, loopFadeSpeed * Time.deltaTime);

        void ResetVoiceTimer() => voiceTimer = Random.Range(voiceIntervalMin, voiceIntervalMax);

        static void StartLoop(AudioSource source, AudioClip clip)
        {
            source.clip = clip;
            source.loop = true;
            source.volume = 0f;
            source.Play();
        }
    }
}
