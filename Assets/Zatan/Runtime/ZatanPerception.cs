using System;
using UnityEngine;

namespace Jamlloween.Zatan
{
    /// <summary>Zatan's sight and hearing, plus whether the player can see him. Ranges come from the tension brain.</summary>
    public sealed class ZatanPerception : MonoBehaviour
    {
        [SerializeField] ZatanConfig config;
        [SerializeField] ZatanTarget target;
        [SerializeField] ZatanTension tension;
        [SerializeField] Transform eye;
        [SerializeField] Transform bodyCenter;

        public event Action<Vector3> Heard = delegate { };

        public bool CanSeePlayer { get; private set; }
        public bool SightConfirmed { get; private set; }
        public bool PlayerSeesZatan { get; private set; }
        public Vector3 LastSeenPosition { get; private set; }
        public Transform Eye => eye;

        /// <summary>Off while vanished: no senses at all.</summary>
        public bool Sensing { get; set; } = true;

        float sightTime, noiseTimer;

        void Awake()
        {
            if (config == null) throw new MissingReferenceException(nameof(config));
            if (target == null) throw new MissingReferenceException(nameof(target));
            if (tension == null) throw new MissingReferenceException(nameof(tension));
            if (eye == null) throw new MissingReferenceException(nameof(eye));
            if (bodyCenter == null) throw new MissingReferenceException(nameof(bodyCenter));
        }

        void Update()
        {
            if (!Sensing)
            {
                CanSeePlayer = SightConfirmed = PlayerSeesZatan = false;
                sightTime = 0f;
                return;
            }

            CanSeePlayer = CheckSight();
            if (CanSeePlayer)
            {
                LastSeenPosition = target.Position;
                sightTime += Time.deltaTime;
            }
            else sightTime = 0f;
            SightConfirmed = sightTime >= config.sightConfirmTime;

            PlayerSeesZatan = IsVisibleToPlayer(bodyCenter.position);
            UpdateHearing();
        }

        bool CheckSight()
        {
            Vector3 to = target.ChestPosition - eye.position;
            if (to.magnitude > tension.Tuning.VisionRange) return false;
            Vector3 flat = Vector3.ProjectOnPlane(to, Vector3.up);
            if (Vector3.Angle(transform.forward, flat) > tension.Tuning.VisionAngle * 0.5f) return false;
            return ZatanLineOfSight.IsClear(eye.position, target.ChestPosition, transform, target.transform);
        }

        void UpdateHearing()
        {
            noiseTimer -= Time.deltaTime;
            if (noiseTimer > 0f || !target.IsMoving) return;
            noiseTimer = config.noiseInterval;
            ReportNoise(target.Position, target.IsRunning ? config.runNoiseRadius : config.walkNoiseRadius);
        }

        /// <summary>Any noise in the world, for example a thrown object. Heard if inside radius x hearing multiplier.</summary>
        public void ReportNoise(Vector3 position, float radius)
        {
            if (!Sensing) return;
            if (Vector3.Distance(transform.position, position) <= radius * tension.Tuning.HearingMult) Heard(position);
        }

        public bool IsVisibleToPlayer(Vector3 worldPoint)
        {
            Camera view = target.View;
            Vector3 viewport = view.WorldToViewportPoint(worldPoint);
            if (viewport.z <= 0f || viewport.z > config.playerViewDistance) return false;
            if (viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) return false;
            return ZatanLineOfSight.IsClear(view.transform.position, worldPoint, target.transform, transform);
        }

        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = CanSeePlayer ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, tension.Tuning.VisionRange);
            Vector3 left = Quaternion.Euler(0f, -tension.Tuning.VisionAngle * 0.5f, 0f) * transform.forward;
            Vector3 right = Quaternion.Euler(0f, tension.Tuning.VisionAngle * 0.5f, 0f) * transform.forward;
            Gizmos.DrawRay(eye.position, left * tension.Tuning.VisionRange);
            Gizmos.DrawRay(eye.position, right * tension.Tuning.VisionRange);
        }
    }
}
