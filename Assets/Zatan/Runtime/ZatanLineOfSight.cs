using UnityEngine;

namespace Jamlloween.Zatan
{
    public static class ZatanLineOfSight
    {
        static readonly RaycastHit[] Hits = new RaycastHit[32];

        /// <summary>True when nothing but the two given hierarchies blocks the segment. Triggers never block.</summary>
        public static bool IsClear(Vector3 from, Vector3 to, Transform ignoreA, Transform ignoreB)
        {
            Vector3 dir = to - from;
            float distance = dir.magnitude;
            if (distance < 0.001f) return true;
            int count = Physics.RaycastNonAlloc(from, dir / distance, Hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Transform hit = Hits[i].transform;
                if (!hit.IsChildOf(ignoreA) && !hit.IsChildOf(ignoreB)) return false;
            }
            return true;
        }
    }
}
