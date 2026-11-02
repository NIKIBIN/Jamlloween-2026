using UnityEngine;

namespace Circus
{
    /// <summary>Shared VHS values. The InterferenceDriver and game over write here; the VHS pass reads it.</summary>
    public static class VHSGlobals
    {
        public static float Interference;        // gameplay-driven 0..1
        public static float ExtraInterference;   // overrides (game over, scripted events)
        public static float CameraWatching;      // 0..1 how much a camera is watching the player

        public static float Total => Mathf.Clamp01(Mathf.Max(Interference, ExtraInterference));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Interference = 0; ExtraInterference = 0; CameraWatching = 0; }
    }
}
