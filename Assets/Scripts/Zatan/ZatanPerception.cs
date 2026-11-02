using UnityEngine;

namespace Circus
{
    /// <summary>
    /// Vision cone (angle/range from Tunables). Range: flashlight on > lit > dark.
    /// Hearing lives in ZatanBrain (it reacts to NoiseSystem events).
    /// </summary>
    public class ZatanPerception : MonoBehaviour
    {
        public static ZatanPerception I { get; private set; }

        [SerializeField] Transform eye;
        [SerializeField] float targetHeightOffset = -0.35f;

        public bool CanSeePlayer { get; private set; }
        public float LastSeenTime { get; private set; } = -999f;
        public Vector3 LastSeenPos { get; private set; }
        public float CurrentRange { get; private set; }
        public Transform Eye => eye;

        readonly RaycastHit[] hits = new RaycastHit[16];

        void Awake() => I = this;
        void OnDestroy() { if (I == this) I = null; }

        public bool SeenRecently(float seconds) => CanSeePlayer || Time.time - LastSeenTime <= seconds;

        public float ComputeRange()
        {
            var t = Tunables.I;
            var p = FirstPersonController.I;
            if (Flashlight.IsOn) return t.visionRangeFlashlight;
            if (t.lightsAffectVision && p && !LightCircuit.IsPointLit(p.transform.position)) return t.visionRangeDark;
            return t.visionRange;
        }

        void Update()
        {
            CurrentRange = ComputeRange();
            CanSeePlayer = Check();
            if (CanSeePlayer)
            {
                LastSeenTime = Time.time;
                LastSeenPos = FirstPersonController.I.transform.position;
            }
        }

        bool Check()
        {
            var p = FirstPersonController.I;
            if (!p || HidingSpot.Occupied != null || p.CurrentMode == FirstPersonController.Mode.Dead) return false;
            var brain = ZatanBrain.I;
            if (brain && brain.State == ZatanState.Dormant) return false;
            Vector3 target = p.HeadPosition + Vector3.up * targetHeightOffset;
            Vector3 to = target - eye.position;
            float d = to.magnitude;
            if (d > CurrentRange) return false;
            Vector3 flat = to; flat.y = 0;
            if (Vector3.Angle(transform.forward, flat) > Tunables.I.visionAngle * 0.5f) return false;
            return HasLineOfSight(eye.position, target, transform, p.transform);
        }

        /// <summary>True if nothing but `ignore` or `target` blocks the segment.</summary>
        public bool HasLineOfSight(Vector3 from, Vector3 to, Transform ignore, Transform target)
        {
            Vector3 dir = to - from; float d = dir.magnitude;
            int n = Physics.RaycastNonAlloc(from, dir / d, hits, d, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; Transform bestT = null;
            for (int i = 0; i < n; i++)
            {
                var ht = hits[i].transform;
                if (ignore && ht.IsChildOf(ignore)) continue;
                if (hits[i].distance < best) { best = hits[i].distance; bestT = ht; }
            }
            return bestT == null || (target && bestT.IsChildOf(target));
        }
    }
}
