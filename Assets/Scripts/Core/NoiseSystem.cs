using System;
using UnityEngine;

namespace Jamlloween.Core
{
    public enum NoiseSource { Walk, Run, Repair, Door, Machine, Carousel, Other }

    public struct NoiseEvent
    {
        public Vector3 pos;
        public float radius;
        public NoiseSource source;
        public float time;
    }

    /// <summary>
    /// A noise in the world that an AI can hear (a position and a radius). It plays no sound: the sound the player
    /// hears is a separate AudioSource. Nothing listens yet: decide if Zatan's ZatanPerception should hear these.
    /// </summary>
    public static class NoiseSystem
    {
        public static event Action<NoiseEvent> Emitted;

        public static void Emit(Vector3 pos, float radius, NoiseSource source)
        {
            if (radius <= 0f) return;
            Emitted?.Invoke(new NoiseEvent { pos = pos, radius = radius, source = source, time = Time.time });
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Emitted = null;
    }
}
