using UnityEngine;

namespace Spike2.Core
{
    /// <summary>Where the run is: current act and beat (set by the ActDirector), run start time. Read by anyone.</summary>
    public static class GameContext
    {
        public static ActId Act = ActId.Act0;
        public static string Beat = "";
        public static float RunStartTime;
        public static int Captures;

        public static float RunTime => Time.time - RunStartTime;

        /// <summary>Act number as used by zatan.act_zones and the JSON (Final = 5).</summary>
        public static int ActNumber => (int)Act;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Act = ActId.Act0; Beat = ""; RunStartTime = 0f; Captures = 0; }
    }
}
