using UnityEngine;
using UnityEngine.AI;

namespace Jamlloween.Zatan
{
    /// <summary>
    /// Feeds the walk/run blend tree from his real speed, with the cadence scaled so the feet keep pace.
    /// While caught the catch sequence owns his head and jaw: the animator stops and the rest pose comes back.
    /// </summary>
    public sealed class ZatanAnimation : MonoBehaviour
    {
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int CadenceId = Animator.StringToHash("Cadence");
        const float StandingSpeed = 0.2f;

        [SerializeField] ZatanConfig config;
        [SerializeField] ZatanBehaviourBrain brain;
        [SerializeField] NavMeshAgent agent;
        [SerializeField] Animator animator;
        [Tooltip("Every transform the clips move. Their rest pose comes back when the catch takes over.")]
        [SerializeField] Transform[] animated;

        float speed;
        Vector3[] restPositions;
        Quaternion[] restRotations;

        void Awake()
        {
            if (config == null) throw new MissingReferenceException(nameof(config));
            if (brain == null) throw new MissingReferenceException(nameof(brain));
            if (agent == null) throw new MissingReferenceException(nameof(agent));
            if (animator == null) throw new MissingReferenceException(nameof(animator));
            if (animated == null || animated.Length == 0) throw new MissingReferenceException(nameof(animated));

            restPositions = new Vector3[animated.Length];
            restRotations = new Quaternion[animated.Length];
            for (int i = 0; i < animated.Length; i++)
            {
                restPositions[i] = animated[i].localPosition;
                restRotations[i] = animated[i].localRotation;
            }
        }

        void OnEnable() => brain.StateChanged += OnStateChanged;

        void OnDisable() => brain.StateChanged -= OnStateChanged;

        void Update()
        {
            if (!animator.enabled) return;

            float target = brain.State == ZatanBehaviourState.Spotted ? 0f : agent.velocity.magnitude;
            speed = Mathf.Lerp(speed, target, 1f - Mathf.Exp(-config.animSpeedSmoothing * Time.deltaTime));

            // Between the walk and run thresholds the blend already matches the stride; outside them, the clip
            // closest in speed plays faster or slower so the feet don't skate.
            float nominal = Mathf.Clamp(speed, config.animWalkSpeed, config.animRunSpeed);
            float cadence = speed < StandingSpeed ? 1f : Mathf.Clamp(speed / nominal, 0.4f, 1.5f);
            animator.SetFloat(SpeedId, speed);
            animator.SetFloat(CadenceId, cadence);
        }

        void OnStateChanged(ZatanBehaviourState state)
        {
            bool caught = state == ZatanBehaviourState.Caught;
            if (caught == !animator.enabled) return;

            animator.enabled = !caught;
            if (!caught) return;

            speed = 0f;
            for (int i = 0; i < animated.Length; i++)
                animated[i].SetLocalPositionAndRotation(restPositions[i], restRotations[i]);
        }
    }
}
