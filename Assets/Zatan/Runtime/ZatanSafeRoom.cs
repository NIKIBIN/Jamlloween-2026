using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Jamlloween.Zatan
{
    /// <summary>
    /// A room Zatan can't get into (the player's trailer). When the player is inside while he is after them, he gives up
    /// the chase: walks to the door, bangs on it a few times, prowls around the room, walks off and vanishes.
    /// If the player comes out while he is still there, the chase picks up again.
    /// </summary>
    public sealed class ZatanSafeRoom : MonoBehaviour
    {

        [SerializeField] ZatanBehaviourBrain brain;
        [SerializeField] ZatanTarget target;
        [SerializeField] ZatanDirector director;

        [Header("Room")]
        [Tooltip("Box that covers the inside of the room. Only its shape is used (it can be a trigger).")]
        [SerializeField] BoxCollider room;
        [Tooltip("Where he stands outside the door, on the NavMesh.")]
        [SerializeField] Transform doorStand;
        [Tooltip("What he faces and bangs on.")]
        [SerializeField] Transform door;

        [Header("Banging")]
        [SerializeField] AudioSource doorSource;
        [SerializeField] AudioClip[] bangClips;
        [SerializeField, Range(0f, 1f)] float bangVolume = 1f;
        [SerializeField, Min(1)] int bangCount = 4;
        [SerializeField] Vector2 bangInterval = new Vector2(0.55f, 1.1f);
        [Tooltip("Seconds before the first bang, after reaching the door.")]
        [SerializeField] float bangLead = 0.6f;

        [Header("Prowling and leaving")]
        [Tooltip("Speed on the way to the door. Not tied to the player's run: a player carrying something runs slower.")]
        [SerializeField] float approachSpeed = 6f;
        [Tooltip("Safety limit for the walk to the door. He also stops early if he gets stuck.")]
        [SerializeField] float approachTimeout = 45f;
        [SerializeField] float roamDuration = 8f;
        [SerializeField] float roamRadius = 7f;
        [SerializeField] float roamPause = 1.2f;
        [SerializeField] float walkSpeed = 1.6f;
        [Tooltip("How far he walks off before vanishing.")]
        [SerializeField] float leaveDistance = 22f;
        [SerializeField] float leaveTimeout = 10f;

        [SerializeField] UnityEvent onZatanGaveUp = new UnityEvent();

        /// <summary>He went away and vanished: the player is safe again.</summary>
        public event Action ZatanGaveUp = delegate { };

        /// <summary>The Inspector event, for editor tools that wire it.</summary>
        public UnityEvent OnZatanGaveUp => onZatanGaveUp;

        Coroutine routine;

        /// <summary>He is at the door or prowling around the room.</summary>
        public bool Busy => routine != null;

        public bool PlayerInside
        {
            get
            {
                Vector3 local = room.transform.InverseTransformPoint(target.Position + Vector3.up * 0.5f) - room.center;
                Vector3 half = room.size * 0.5f;
                return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
            }
        }

        void Awake()
        {
            if (brain == null) throw new MissingReferenceException(nameof(brain));
            if (target == null) throw new MissingReferenceException(nameof(target));
            if (director == null) throw new MissingReferenceException(nameof(director));
            if (room == null) throw new MissingReferenceException(nameof(room));
            if (doorStand == null) throw new MissingReferenceException(nameof(doorStand));
            if (door == null) throw new MissingReferenceException(nameof(door));
            if (doorSource == null) throw new MissingReferenceException(nameof(doorSource));
            if (bangClips == null || bangClips.Length == 0) throw new MissingReferenceException(nameof(bangClips));
        }

        void OnEnable() => brain.StateChanged += OnBrainStateChanged;

        void OnDisable()
        {
            brain.StateChanged -= OnBrainStateChanged;
            routine = null;
        }

        /// <summary>Something else took him out of the scripted routine (Sleep, a step jump): stop driving him.</summary>
        void OnBrainStateChanged(ZatanBehaviourState state)
        {
            if (routine == null || state == ZatanBehaviourState.Scripted) return;
            StopCoroutine(routine);
            routine = null;
        }

        void Update()
        {
            if (routine != null || !PlayerInside) return;
            if (brain.State == ZatanBehaviourState.Chase || brain.State == ZatanBehaviourState.Spotted || brain.State == ZatanBehaviourState.Search)
                routine = StartCoroutine(GiveUp());
        }

        IEnumerator GiveUp()
        {
            brain.BeginScripted();

            brain.ScriptedMoveTo(doorStand.position, approachSpeed);
            yield return Walk(approachTimeout);
            if (BrokeOut()) yield break;

            for (float t = 0f; t < bangLead; t += Time.deltaTime) { brain.ScriptedFace(door.position); yield return null; }
            for (int i = 0; i < bangCount; i++)
            {
                if (BrokeOut()) yield break;
                doorSource.PlayOneShot(bangClips[UnityEngine.Random.Range(0, bangClips.Length)], bangVolume);
                float wait = UnityEngine.Random.Range(bangInterval.x, bangInterval.y);
                for (float t = 0f; t < wait; t += Time.deltaTime) { brain.ScriptedFace(door.position); yield return null; }
            }

            Vector3 center = room.transform.TransformPoint(room.center);
            for (float elapsed = 0f; elapsed < roamDuration;)
            {
                brain.ScriptedMoveTo(RoamPoint(center), walkSpeed);
                float started = Time.time;
                yield return Walk(roamDuration - elapsed);
                if (BrokeOut()) yield break;
                for (float t = 0f; t < roamPause; t += Time.deltaTime) { brain.ScriptedFace(center); yield return null; }
                elapsed += Time.time - started + roamPause;
            }

            Vector3 away = Vector3.ProjectOnPlane(brain.transform.position - center, Vector3.up).normalized;
            if (away.sqrMagnitude < 0.01f) away = -door.forward;
            brain.ScriptedMoveTo(brain.PointNear(center + away * leaveDistance, 4f), walkSpeed);
            yield return Walk(leaveTimeout);
            if (BrokeOut()) yield break;

            routine = null;
            director.Sleep();
            ZatanGaveUp();
            onZatanGaveUp.Invoke();
        }

        /// <summary>A point around the room, never inside it (he can't get in).</summary>
        Vector3 RoamPoint(Vector3 center)
        {
            const int Attempts = 10;
            const float Margin = 1.5f;
            for (int i = 0; i < Attempts; i++)
            {
                Vector3 p = brain.PointNear(center, roamRadius);
                if (!InsideRoom(p, Margin)) return p;
            }
            return doorStand.position;
        }

        bool InsideRoom(Vector3 point, float margin)
        {
            Vector3 local = room.transform.InverseTransformPoint(point) - room.center;
            Vector3 half = room.size * 0.5f + Vector3.one * margin;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.z) <= half.z;
        }

        /// <summary>Waits until he gets there, gets stuck, the time runs out or the player comes out.</summary>
        IEnumerator Walk(float timeout)
        {
            const float StuckSeconds = 3f;
            const float StuckDistance = 0.3f;
            yield return null;   // the path needs a frame
            Vector3 lastPos = brain.transform.position;
            float stuck = 0f;
            for (float t = 0f; t < timeout && !brain.ReachedDestination && PlayerInside; t += Time.deltaTime)
            {
                stuck += Time.deltaTime;
                if ((brain.transform.position - lastPos).sqrMagnitude > StuckDistance * StuckDistance) { lastPos = brain.transform.position; stuck = 0f; }
                if (stuck >= StuckSeconds) yield break;
                yield return null;
            }
        }

        /// <summary>The player came out while he was still around: the chase goes on.</summary>
        bool BrokeOut()
        {
            if (PlayerInside) return false;
            routine = null;
            brain.ResumeChase(target.Position);
            return true;
        }
    }
}
