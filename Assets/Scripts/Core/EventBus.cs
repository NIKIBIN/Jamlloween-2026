using System;
using UnityEngine;

namespace Circus
{
    public enum GamePhase { Intro, Observe, Play, Hunt }
    public enum ZatanState { Dormant, Patrol, Investigate, Chase, Observe, ForceDoor }
    public enum NoiseSource { Walk, Run, Repair, Door, Machine, Carousel, Other }

    public struct NoiseEvent
    {
        public Vector3 pos;
        public float radius;
        public NoiseSource source;
        public float time;
        public bool byZatan;
    }

    /// <summary>Static events. Subscribers must unsubscribe in OnDisable.</summary>
    public static class EventBus
    {
        public static event Action<string> MachineRepaired;          // machine id
        public static event Action<NoiseEvent> Noise;
        public static event Action PlayerCaught;
        public static event Action<GamePhase> PhaseChanged;
        public static event Action<ZatanState> ZatanStateChanged;
        public static event Action<Vector3, string> PlayerSpotted;    // position, source id
        public static event Action<bool> PowerChanged;
        public static event Action<string, float> Message;            // text, seconds
        public static event Action<string> ScriptedEvent;             // trigger / event id

        public static void RaiseMachineRepaired(string id) => MachineRepaired?.Invoke(id);
        public static void RaiseNoise(NoiseEvent e) => Noise?.Invoke(e);
        public static void RaisePlayerCaught() => PlayerCaught?.Invoke();
        public static void RaisePhaseChanged(GamePhase p) => PhaseChanged?.Invoke(p);
        public static void RaiseZatanState(ZatanState s) => ZatanStateChanged?.Invoke(s);
        public static void RaisePlayerSpotted(Vector3 p, string src) => PlayerSpotted?.Invoke(p, src);
        public static void RaisePowerChanged(bool on) => PowerChanged?.Invoke(on);
        public static void RaiseMessage(string text, float seconds = 5f) => Message?.Invoke(text, seconds);
        public static void RaiseScriptedEvent(string id) => ScriptedEvent?.Invoke(id);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            MachineRepaired = null; Noise = null; PlayerCaught = null; PhaseChanged = null;
            ZatanStateChanged = null; PlayerSpotted = null; PowerChanged = null; Message = null;
            ScriptedEvent = null;
        }
    }
}
