using UnityEngine;
using UnityEngine.AI;

namespace Jamlloween.Zatan
{
    /// <summary>Mechanical footsteps: volume follows tension, and above a tension threshold the steps get heavier.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class ZatanFootsteps : MonoBehaviour
    {
        [SerializeField] ZatanTension tension;
        [SerializeField] NavMeshAgent agent;
        [SerializeField] AudioSource source;
        [SerializeField] AudioClip[] steps;
        [SerializeField] AudioClip[] heavySteps;
        [SerializeField] float stepLength = 1.4f;
        [SerializeField, Range(0f, 1f)] float heavyFromTension = 0.6f;

        public float LastVolume { get; private set; }
        public AudioClip LastClip { get; private set; }

        float travelled;
        int lastIndex;

        void Awake()
        {
            if (tension == null) throw new MissingReferenceException(nameof(tension));
            if (agent == null) throw new MissingReferenceException(nameof(agent));
            if (source == null) throw new MissingReferenceException(nameof(source));
            if (steps == null || steps.Length < 2) throw new MissingReferenceException(nameof(steps));
            if (heavySteps == null || heavySteps.Length < 2) throw new MissingReferenceException(nameof(heavySteps));
        }

        void Update()
        {
            travelled += agent.velocity.magnitude * Time.deltaTime;
            if (travelled < stepLength) return;
            travelled = 0f;
            LastVolume = tension.Tuning.FootstepVolume;
            LastClip = Pick(tension.Normalized >= heavyFromTension ? heavySteps : steps);
            source.PlayOneShot(LastClip, LastVolume);
        }

        // Never the same index twice in a row, so the steps do not sound like a machine gun.
        AudioClip Pick(AudioClip[] clips)
        {
            int index = (lastIndex + Random.Range(1, clips.Length)) % clips.Length;
            lastIndex = index;
            return clips[index];
        }
    }
}
