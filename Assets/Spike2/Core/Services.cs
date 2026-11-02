using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spike2.Core
{
    /// <summary>
    /// Service locator. Every contract interface has a null implementation (Core/Stubs) registered by default,
    /// so Get&lt;T&gt;() never returns null and everything runs before the real system exists.
    /// Real systems call Register in Awake and Unregister in OnDestroy.
    /// </summary>
    public static class Services
    {
        static readonly Dictionary<Type, object> real = new Dictionary<Type, object>();
        static readonly Dictionary<Type, object> nulls = new Dictionary<Type, object>();
        static readonly Dictionary<Type, Func<string, object>> nullFactories = new Dictionary<Type, Func<string, object>>();
        static bool defaultsRegistered;

        public static void Register<T>(T impl) where T : class
        {
            EnsureDefaults();
            if (impl == null) return;
            if (real.TryGetValue(typeof(T), out var cur) && Alive(cur) && !ReferenceEquals(cur, impl))
                Debug.LogWarning($"[Services] {typeof(T).Name} replaced: {cur} -> {impl}");
            real[typeof(T)] = impl;
        }

        public static void Unregister<T>(T impl) where T : class
        {
            if (real.TryGetValue(typeof(T), out var cur) && ReferenceEquals(cur, impl)) real.Remove(typeof(T));
        }

        /// <summary>The real implementation, or the null stub. Never null for contract interfaces.</summary>
        public static T Get<T>() where T : class
        {
            EnsureDefaults();
            if (real.TryGetValue(typeof(T), out var o) && Alive(o)) return (T)o;
            return nulls.TryGetValue(typeof(T), out var n) ? (T)n : null;
        }

        /// <summary>True if a real (non-stub) implementation is registered.</summary>
        public static bool Has<T>() where T : class
        {
            EnsureDefaults();
            return real.TryGetValue(typeof(T), out var o) && Alive(o);
        }

        public static void RegisterNull<T>(T nullImpl) where T : class => nulls[typeof(T)] = nullImpl;

        /// <summary>Null objects per map id (IDoor, IMachine) used by MapRegistry.Resolve.</summary>
        public static void RegisterNullFactory<T>(Func<string, T> factory) where T : class => nullFactories[typeof(T)] = id => factory(id);

        public static T NullFor<T>(string id) where T : class
        {
            EnsureDefaults();
            if (nullFactories.TryGetValue(typeof(T), out var f)) return (T)f(id);
            return nulls.TryGetValue(typeof(T), out var n) ? (T)n : null;
        }

        static bool Alive(object o) => o != null && !(o is UnityEngine.Object uo && uo == null);

        static void EnsureDefaults()
        {
            if (defaultsRegistered) return;
            defaultsRegistered = true;
            Stubs.NullServices.RegisterAll();
        }

        public static void Reset()
        {
            real.Clear(); nulls.Clear(); nullFactories.Clear();
            defaultsRegistered = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reset();
    }
}
