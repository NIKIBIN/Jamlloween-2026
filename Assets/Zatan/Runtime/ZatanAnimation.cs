using UnityEngine;
using UnityEngine.AI;

namespace Jamlloween.Zatan
{
    /// <summary>
    /// Feeds the walk/run blend tree from his real speed, with the cadence scaled so the feet keep pace.
    ///
    /// Two setups:
    /// - Old model (procedural clips, Zatan_Test): while caught the catch sequence owns his head and jaw, so the animator
    ///   stops and the rest pose of <see cref="animated"/> comes back.
    /// - Rigged model (<see cref="rigged"/>, Zatan_Rig.controller): the animator keeps running and plays its own poses:
    ///   Stare while he spots the player or the director makes him stare, Stalk, Catch while caught, and a Bang on
    ///   each knock on a door.
    /// </summary>
    public sealed class ZatanAnimation : MonoBehaviour
    {
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int CadenceId = Animator.StringToHash("Cadence");
        static readonly int StareId = Animator.StringToHash("Stare");
        static readonly int StalkId = Animator.StringToHash("Stalk");
        static readonly int CaughtId = Animator.StringToHash("Caught");
        static readonly int BangId = Animator.StringToHash("Bang");
        const float StandingSpeed = 0.2f;

        [SerializeField] ZatanConfig config;
        [SerializeField] ZatanBehaviourBrain brain;
        [SerializeField] NavMeshAgent agent;
        [SerializeField] Animator animator;
        [Tooltip("Rigged model: the animator has Stare/Stalk/Caught/Bang and is never stopped.")]
        [SerializeField] bool rigged;
        [Tooltip("Old model only. Every transform the clips move. Their rest pose comes back when the catch takes over.")]
        [SerializeField] Transform[] animated;
        [Tooltip("Speed (m/s) the walk clip covers without sliding. 0 = the config's animWalkSpeed.")]
        [SerializeField, Min(0f)] float walkClipSpeed;
        [Tooltip("Speed (m/s) the run clip covers without sliding. 0 = the config's animRunSpeed.")]
        [SerializeField, Min(0f)] float runClipSpeed;

        float speed;
        bool stare, stalk;
        Vector3[] restPositions;
        Quaternion[] restRotations;

        float WalkSpeed => walkClipSpeed > 0f ? walkClipSpeed : config.animWalkSpeed;
        float RunSpeed => runClipSpeed > 0f ? runClipSpeed : config.animRunSpeed;

        void Awake()
        {
            if (config == null) throw new MissingReferenceException(nameof(config));
            if (brain == null) throw new MissingReferenceException(nameof(brain));
            if (agent == null) throw new MissingReferenceException(nameof(agent));
            if (animator == null) throw new MissingReferenceException(nameof(animator));
            if (rigged) return;
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

        /// <summary>Director: he stands rigid and stares (first encounter). Rigged model only.</summary>
        public void SetStare(bool on) => stare = on;

        /// <summary>Director: the slow, hunched walk toward the cornered player. Rigged model only.</summary>
        public void SetStalk(bool on) => stalk = on;

        /// <summary>One knock on a door (ZatanSafeRoom's onBang). Rigged model only.</summary>
        public void PlayBang()
        {
            if (rigged) animator.SetTrigger(BangId);
        }

        void Update()
        {
            if (!animator.enabled) return;

            bool standing = brain.State == ZatanBehaviourState.Spotted || brain.State == ZatanBehaviourState.TipReaction;
            float target = standing ? 0f : agent.velocity.magnitude;
            speed = Mathf.Lerp(speed, target, 1f - Mathf.Exp(-config.animSpeedSmoothing * Time.deltaTime));

            // Between the walk and run thresholds the blend already matches the stride; outside them, the clip
            // closest in speed plays faster or slower so the feet don't skate.
            float nominal = Mathf.Clamp(speed, WalkSpeed, RunSpeed);
            float cadence = speed < StandingSpeed ? 1f : Mathf.Clamp(speed / nominal, 0.4f, 1.5f);
            animator.SetFloat(SpeedId, speed);
            animator.SetFloat(CadenceId, cadence);

            if (!rigged) return;
            animator.SetBool(StareId, stare || brain.State == ZatanBehaviourState.Spotted);
            animator.SetBool(StalkId, stalk);
            animator.SetBool(CaughtId, brain.State == ZatanBehaviourState.Caught);
        }

        void OnStateChanged(ZatanBehaviourState state)
        {
            bool caught = state == ZatanBehaviourState.Caught;
            if (rigged)
            {
                // Set at once (not next Update) so the catch's first frame already has the grab pose.
                animator.SetBool(CaughtId, caught);
                if (caught) speed = 0f;
                if (state == ZatanBehaviourState.Vanished) stare = stalk = false;
                return;
            }

            if (caught == !animator.enabled) return;

            animator.enabled = !caught;
            if (!caught) return;

            speed = 0f;
            for (int i = 0; i < animated.Length; i++)
                animated[i].SetLocalPositionAndRotation(restPositions[i], restRotations[i]);
        }
    }
}
