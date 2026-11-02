using System;
using UnityEngine;

namespace Circus
{
    /// <summary>Audio hook for repairs; the AudioDirector subscribes (keeps machines free of audio code).</summary>
    public static class RepairAudio
    {
        public static event Action<Vector3, float> OnBeep;
        public static void Beep(Vector3 pos, float progress) => OnBeep?.Invoke(pos, progress);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => OnBeep = null;
    }
}
