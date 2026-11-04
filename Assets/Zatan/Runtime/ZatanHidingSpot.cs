using System;
using UnityEngine;

namespace Jamlloween.Zatan
{
    /// <summary>
    /// A trash can the player can hide in. Zatan can tip it over: it lies on its side from then on and can't hide anyone,
    /// and whoever was inside is thrown out on the far side.
    /// </summary>
    public sealed class ZatanHidingSpot : MonoBehaviour
    {
        [SerializeField] ZatanConfig config;
        [Tooltip("The can's body: its bounds give the radius it tips over on.")]
        [SerializeField] Renderer body;
        [Tooltip("The lid: it lifts a little while someone hides, leaving the gap they peek through.")]
        [SerializeField] Transform lid;
        [Tooltip("Where the hidden player's eyes go: under the lifted lid, looking out of the gap.")]
        [SerializeField] Transform viewPoint;
        [SerializeField] AudioSource source;
        [SerializeField] AudioClip tipClip;
        [SerializeField] AudioClip enterClip;
        [SerializeField] AudioClip exitClip;
        [Tooltip("Degrees the lid lifts while someone is inside.")]
        [SerializeField] float lidLift = 14f;

        /// <summary>The player got in.</summary>
        public event Action<ZatanHidingSpot> Entered = delegate { };
        /// <summary>The can was tipped with the player inside. The position is where the player lands.</summary>
        public event Action<Vector3> Ejected = delegate { };

        public bool IsTipped { get; private set; }
        public bool IsOccupied { get; private set; }
        public bool CanHide => !IsTipped && !IsOccupied;
        public Transform ViewPoint => viewPoint;
        public Vector3 Position => transform.position;

        Vector3 restPosition, pivot, axis;
        Quaternion restRotation, lidRest;
        float footRadius, tipTime = -1f;

        void Awake()
        {
            if (config == null) throw new MissingReferenceException(nameof(config));
            if (body == null) throw new MissingReferenceException(nameof(body));
            if (lid == null) throw new MissingReferenceException(nameof(lid));
            if (viewPoint == null) throw new MissingReferenceException(nameof(viewPoint));
            if (source == null) throw new MissingReferenceException(nameof(source));
            if (tipClip == null) throw new MissingReferenceException(nameof(tipClip));
            if (enterClip == null) throw new MissingReferenceException(nameof(enterClip));
            if (exitClip == null) throw new MissingReferenceException(nameof(exitClip));

            restPosition = transform.position;
            restRotation = transform.rotation;
            lidRest = lid.localRotation;
            Vector3 extents = body.bounds.extents;
            footRadius = Mathf.Max(extents.x, extents.z);
        }

        void Update()
        {
            if (tipTime < 0f) return;
            tipTime += Time.deltaTime;
            float t = Mathf.Clamp01(tipTime / config.tipDuration);
            // Falls slowly at first, then slams down.
            Quaternion fall = Quaternion.AngleAxis(90f * t * t, axis);
            transform.SetPositionAndRotation(pivot + fall * (restPosition - pivot), fall * restRotation);
            if (t >= 1f) tipTime = -1f;
        }

        public void Enter()
        {
            if (!CanHide) throw new InvalidOperationException($"{name} can't hide anyone now.");
            IsOccupied = true;
            lid.localRotation = lidRest * Quaternion.Euler(-lidLift, 0f, 0f);
            source.PlayOneShot(enterClip);
            Entered(this);
        }

        public void Exit()
        {
            if (!IsOccupied) throw new InvalidOperationException($"Nobody is hiding in {name}.");
            IsOccupied = false;
            lid.localRotation = lidRest;
            source.PlayOneShot(exitClip);
        }

        /// <summary>Where someone climbing out stands: in front of the can.</summary>
        public Vector3 ExitPoint => restPosition + Flat(restRotation * Vector3.forward) * config.ejectDistance;

        /// <summary>Tips the can away from <paramref name="from"/>. True when the player was inside and got thrown out.</summary>
        public bool Tip(Vector3 from)
        {
            if (IsTipped) throw new InvalidOperationException($"{name} is already tipped over.");
            IsTipped = true;
            Vector3 away = Flat(restPosition - from);
            if (away.sqrMagnitude < 0.0001f) away = Flat(restRotation * Vector3.forward);
            pivot = restPosition + away * footRadius;
            axis = Vector3.Cross(Vector3.up, away);
            tipTime = 0f;
            lid.localRotation = lidRest;
            source.PlayOneShot(tipClip);

            if (!IsOccupied) return false;
            IsOccupied = false;
            Ejected(restPosition + away * (footRadius + config.ejectDistance));
            return true;
        }

        /// <summary>Upright and empty again (the test scene restarts after a catch).</summary>
        public void ResetUpright()
        {
            tipTime = -1f;
            IsTipped = false;
            IsOccupied = false;
            transform.SetPositionAndRotation(restPosition, restRotation);
            lid.localRotation = lidRest;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.normalized;
        }
    }
}
