using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Jamlloween.Zatan
{
    /// <summary>
    /// The story's hand on Zatan. Story events (wired in the Inspector, e.g. from the ProgressionEventRelay) call these
    /// public methods: keep him out of the world, let him roam, start a story chase from a far point, stalk the player
    /// slowly, or attack from a fixed point for the finale.
    /// Also owns what a catch costs: the player goes back to the current checkpoint and, if a story chase was running,
    /// it starts again from the same spot after a short pause. The finale catch costs nothing: it ends the game.
    /// </summary>
    public sealed class ZatanDirector : MonoBehaviour
    {
        [SerializeField] ZatanBehaviourBrain brain;
        [SerializeField] ZatanTension tension;
        [SerializeField] ZatanTarget target;
        [SerializeField] ZatanCatchSequence catchSequence;
        [Tooltip("Plays the stare and stalk poses of the rigged model.")]
        [SerializeField] ZatanAnimation animationDriver;

        [Tooltip("Where the player comes back after a catch. Story events can move it with SetCheckpoint.")]
        [SerializeField] Transform checkpoint;
        [Tooltip("Tension set when a story chase starts: he is louder and moves wider. He never loses the player in a story chase.")]
        [SerializeField, Range(0f, ZatanTension.Max)] float chaseTension = 80f;
        [Tooltip("Seconds after a catch before a story chase starts again from its spawn.")]
        [SerializeField] float rechaseDelay = 3f;

        [Header("Stalk (the player is cornered)")]
        [Tooltip("Walking speed while he closes in. Slow on purpose: the player has time to reach the way out.")]
        [SerializeField] float stalkSpeed = 1.3f;
        [Tooltip("He stops this far from the player and stares. He never catches while stalking.")]
        [SerializeField] float stalkStopDistance = 4f;
        [SerializeField] float stalkRepathInterval = 0.5f;
        [SerializeField, Range(0f, ZatanTension.Max)] float stalkTension = 60f;

        [Header("Stare (first encounter)")]
        [Tooltip("The player's camera: the stare ends when it looks at him.")]
        [SerializeField] Transform playerView;
        [Tooltip("The player counts as looking at him within this angle of the camera's forward (degrees).")]
        [SerializeField, Range(1f, 45f)] float stareLookAngle = 14f;
        [Tooltip("Seconds his beams stay yellow, staring, before he runs at the player.")]
        [SerializeField, Min(0f)] float stareAlertSeconds = 2f;
        [Tooltip("If the player never looks at him, the chase starts anyway after this many seconds.")]
        [SerializeField, Min(1f)] float stareFallbackSeconds = 20f;

        [SerializeField] UnityEvent onPlayerRespawned = new UnityEvent();
        [Tooltip("The finale catch (FinalAttackFrom) finished and the screen is black. Wire the credits here.")]
        [SerializeField] UnityEvent onFinaleFinished = new UnityEvent();

        /// <summary>The player was put back at the checkpoint after a catch.</summary>
        public event Action PlayerRespawned = delegate { };

        /// <summary>The Inspector event, for editor tools that wire it.</summary>
        public UnityEvent OnFinaleFinished => onFinaleFinished;
        /// <summary>The Inspector respawn event, for editor tools that wire it.</summary>
        public UnityEvent OnPlayerRespawned => onPlayerRespawned;

        Transform chaseSpawn;
        Coroutine rechase, stalk, stare;
        bool finale;

        /// <summary>True while a story chase is running (until EndChase or Sleep).</summary>
        public bool ChaseActive => chaseSpawn != null;
        public bool Stalking => stalk != null;

        void Awake()
        {
            if (brain == null) throw new MissingReferenceException(nameof(brain));
            if (tension == null) throw new MissingReferenceException(nameof(tension));
            if (target == null) throw new MissingReferenceException(nameof(target));
            if (catchSequence == null) throw new MissingReferenceException(nameof(catchSequence));
            if (animationDriver == null) throw new MissingReferenceException(nameof(animationDriver));
            if (checkpoint == null) throw new MissingReferenceException(nameof(checkpoint));
            if (playerView == null) throw new MissingReferenceException(nameof(playerView));
        }

        void OnEnable() => catchSequence.Finished += OnCatchFinished;

        void OnDisable()
        {
            catchSequence.Finished -= OnCatchFinished;
            rechase = null;
            stalk = null;
            stare = null;
        }

        /// <summary>Out of the world, for good, until the next story call.</summary>
        public void Sleep()
        {
            StopRoutines();
            chaseSpawn = null;
            brain.Sleep();
        }

        /// <summary>Back in the world at this point, patrolling and hunting on his own.</summary>
        public void RoamFrom(Transform point)
        {
            if (point == null) throw new ArgumentNullException(nameof(point));
            StopRoutines();
            chaseSpawn = null;
            brain.Respawn(point.position);
        }

        /// <summary>Appears at the point, stares at the player and chases them until a story beat (a safe room) ends it.</summary>
        public void ChaseFrom(Transform spawn)
        {
            if (spawn == null) throw new ArgumentNullException(nameof(spawn));
            StopRoutines();
            chaseSpawn = spawn;
            tension.SetValue(chaseTension);
            brain.AppearAndChase(spawn.position);
        }

        /// <summary>
        /// First encounter: he appears at the point and only watches the player, beams still white. When the player looks
        /// at him his beams turn yellow, he holds the stare, then chases them like ChaseFrom (a catch restarts it from the spawn).
        /// </summary>
        public void StareThenChase(Transform spawn)
        {
            if (spawn == null) throw new ArgumentNullException(nameof(spawn));
            StopRoutines();
            chaseSpawn = spawn;
            tension.SetValue(chaseTension);
            brain.Respawn(spawn.position);
            brain.BeginScripted(calmEyes: true);
            animationDriver.SetStare(true);
            stare = StartCoroutine(Stare());
        }

        IEnumerator Stare()
        {
            for (float t = 0f; ; t += Time.deltaTime)
            {
                if (brain.State != ZatanBehaviourState.Scripted) { stare = null; yield break; }   // something else took him
                brain.ScriptedFace(target.Position);
                if (PlayerLooksAtHim()) break;
                if (t >= stareFallbackSeconds)
                {
                    Debug.Log($"[ZatanDirector] the player never looked at Zatan in {stareFallbackSeconds:0} s: the chase starts anyway.", this);
                    break;
                }
                yield return null;
            }
            stare = null;
            animationDriver.SetStare(false);
            brain.SpotThenChase(stareAlertSeconds);
        }

        bool PlayerLooksAtHim()
        {
            Vector3 eye = brain.EyePosition;
            Vector3 to = eye - playerView.position;
            if (Vector3.Angle(playerView.forward, to) > stareLookAngle) return false;
            return ZatanLineOfSight.IsClear(playerView.position, eye, brain.transform, target.transform);
        }

        /// <summary>Starts a story chase from the spawn only if none is running (for example, the first one already ended).</summary>
        public void EnsureChaseFrom(Transform spawn)
        {
            if (ChaseActive) return;
            ChaseFrom(spawn);
        }

        /// <summary>The story chase is over (the player got away). A catch after this no longer restarts it.</summary>
        public void EndChase() => chaseSpawn = null;

        public void SetCheckpoint(Transform point)
        {
            if (point == null) throw new ArgumentNullException(nameof(point));
            checkpoint = point;
        }

        /// <summary>Appears at the point and walks slowly toward the player, then stands and stares. He never catches.</summary>
        public void StalkFrom(Transform spawn)
        {
            if (spawn == null) throw new ArgumentNullException(nameof(spawn));
            StopRoutines();
            chaseSpawn = null;
            tension.SetValue(stalkTension);
            brain.Respawn(spawn.position);
            brain.BeginScripted();
            animationDriver.SetStalk(true);
            stalk = StartCoroutine(Stalk());
        }

        /// <summary>The finale: he appears at the point and grabs the player. When the catch ends, OnFinaleFinished runs.</summary>
        public void FinalAttackFrom(Transform point)
        {
            if (point == null) throw new ArgumentNullException(nameof(point));
            StopRoutines();
            chaseSpawn = null;
            finale = true;
            brain.ScriptedCatch(point.position);
        }

        IEnumerator Stalk()
        {
            var wait = new WaitForSeconds(stalkRepathInterval);
            while (true)
            {
                Vector3 player = target.Position;
                Vector3 flat = Vector3.ProjectOnPlane(player - brain.transform.position, Vector3.up);
                if (flat.magnitude <= stalkStopDistance)
                {
                    brain.ScriptedFace(player);
                    yield return null;
                    continue;
                }
                brain.ScriptedMoveTo(player - flat.normalized * (stalkStopDistance * 0.8f), stalkSpeed);
                yield return wait;
            }
        }

        void OnCatchFinished()
        {
            if (finale)
            {
                finale = false;
                brain.Sleep();
                onFinaleFinished.Invoke();
                return;
            }
            target.Teleport(checkpoint.position, checkpoint.rotation);
            brain.Sleep();
            if (chaseSpawn != null) rechase = StartCoroutine(Rechase());
            PlayerRespawned();
            onPlayerRespawned.Invoke();
        }

        IEnumerator Rechase()
        {
            yield return new WaitForSeconds(rechaseDelay);
            rechase = null;
            tension.SetValue(chaseTension);
            brain.AppearAndChase(chaseSpawn.position);
        }

        void StopRoutines()
        {
            if (rechase != null) StopCoroutine(rechase);
            if (stalk != null) StopCoroutine(stalk);
            if (stare != null) StopCoroutine(stare);
            rechase = null;
            stalk = null;
            stare = null;
            animationDriver.SetStare(false);
            animationDriver.SetStalk(false);
        }
    }
}
