using System.Collections.Generic;
using UnityEngine;

namespace Circus
{
    public static class NoiseSystem
    {
        public const float GizmoLifetime = 1.5f;
        public static readonly List<NoiseEvent> Recent = new List<NoiseEvent>();

        public static void Emit(Vector3 pos, float radius, NoiseSource source, bool byZatan = false)
        {
            if (radius <= 0f) return;
            var e = new NoiseEvent { pos = pos, radius = radius, source = source, time = Time.time, byZatan = byZatan };
            Recent.Add(e);
            for (int i = Recent.Count - 1; i >= 0; i--)
                if (Time.time - Recent[i].time > GizmoLifetime) Recent.RemoveAt(i);
            EventBus.RaiseNoise(e);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Recent.Clear();
    }
}
