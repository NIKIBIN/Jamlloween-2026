namespace Circus
{
    /// <summary>Lets gameplay code check the debug panel without depending on it.</summary>
    public static class DebugPanelState
    {
        public static bool IsOpen;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsOpen = false;
    }
}
