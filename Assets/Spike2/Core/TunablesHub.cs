using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spike2.Core
{
    /// <summary>
    /// Loads every TunablesBase asset from Resources/Tunables/ and hands out runtime copies (Instantiate).
    /// Fixes the spike 1 bug where F1 changes were saved into the asset.
    /// </summary>
    public static class TunablesHub
    {
        static readonly Dictionary<Type, TunablesBase> copies = new Dictionary<Type, TunablesBase>();
        static readonly List<TunablesBase> ordered = new List<TunablesBase>();
        static bool loadedAll;

        /// <summary>Runtime copy of T (from Resources/Tunables/T.asset, or defaults if the asset is missing).</summary>
        public static T Get<T>() where T : TunablesBase
        {
            if (copies.TryGetValue(typeof(T), out var c) && c != null) return (T)c;
            var asset = Resources.Load<T>("Tunables/" + typeof(T).Name);
            return (T)Adopt(typeof(T), asset);
        }

        /// <summary>Every copy loaded so far (LoadAll is called by SystemsBootstrap).</summary>
        public static IReadOnlyList<TunablesBase> All
        {
            get { ordered.RemoveAll(x => x == null); return ordered; }
        }

        /// <summary>Copy every TunablesBase asset under any Resources/Tunables/ folder.</summary>
        public static void LoadAll()
        {
            if (loadedAll) return;
            loadedAll = true;
            foreach (var asset in Resources.LoadAll<TunablesBase>("Tunables"))
                if (!copies.TryGetValue(asset.GetType(), out var c) || c == null) Adopt(asset.GetType(), asset);
        }

        /// <summary>Back to the values in the asset (or the code defaults).</summary>
        public static void ResetToDefaults(TunablesBase copy)
        {
            if (copy == null) return;
            var t = copy.GetType();
            var asset = Resources.Load("Tunables/" + t.Name, t) as TunablesBase;
            var src = asset != null ? asset : (TunablesBase)ScriptableObject.CreateInstance(t);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(src), copy);
            if (asset == null) UnityEngine.Object.DestroyImmediate(src);
        }

        static TunablesBase Adopt(Type t, TunablesBase asset)
        {
            var copy = asset != null ? UnityEngine.Object.Instantiate(asset) : (TunablesBase)ScriptableObject.CreateInstance(t);
            copy.name = t.Name + " (runtime)";
            copy.hideFlags = HideFlags.DontSave;
            copies[t] = copy;
            ordered.Add(copy);
            ordered.Sort((a, b) => string.CompareOrdinal(a.Tab, b.Tab));
            return copy;
        }

        public static void Clear() { copies.Clear(); ordered.Clear(); loadedAll = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Clear();
    }
}
