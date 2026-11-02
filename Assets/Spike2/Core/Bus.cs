using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spike2.Core
{
    /// <summary>
    /// Typed event bus with struct events (cross-system events live in CoreEvents.cs; a system's internal events
    /// stay in its own folder). Handlers that throw are logged and do not stop the others.
    /// Subscribe in OnEnable, Unsubscribe in OnDisable. The spike 1 EventBus keeps working alongside.
    /// </summary>
    public static class Bus
    {
        static readonly Dictionary<Type, List<Delegate>> handlers = new Dictionary<Type, List<Delegate>>();

        /// <summary>Every published event, for telemetry/debug (PlaytestLog listens here).</summary>
        public static event Action<object> Any;

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            if (!handlers.TryGetValue(typeof(T), out var list)) handlers[typeof(T)] = list = new List<Delegate>();
            if (!list.Contains(handler)) list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler != null && handlers.TryGetValue(typeof(T), out var list)) list.Remove(handler);
        }

        public static void Publish<T>(T evt) where T : struct
        {
            if (handlers.TryGetValue(typeof(T), out var list) && list.Count > 0)
            {
                var snapshot = list.ToArray();
                foreach (var d in snapshot)
                {
                    try { ((Action<T>)d)(evt); }
                    catch (Exception e) { Debug.LogException(e); }
                }
            }
            if (Any != null)
            {
                try { Any(evt); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public static int HandlerCount<T>() where T : struct => handlers.TryGetValue(typeof(T), out var l) ? l.Count : 0;

        public static void Clear() { handlers.Clear(); Any = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Clear();
    }
}
