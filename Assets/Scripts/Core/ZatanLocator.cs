using UnityEngine;

namespace Circus
{
    /// <summary>Decoupled access to the Zatan for world objects (doors, interference, cameras).</summary>
    public static class ZatanLocator
    {
        public static Transform Transform;
        public static bool IsActiveAgent;   // walking around (not dormant / not hidden)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Transform = null; IsActiveAgent = false; }
    }
}
