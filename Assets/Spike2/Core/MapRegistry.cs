using System.Collections.Generic;
using UnityEngine;

namespace Spike2.Core
{
    /// <summary>
    /// Lookup of map objects by id and by kind. Filled by MapId/MapRoot on Awake, so it is complete before any
    /// Start(). Systems adopt their objects in Start() (e.g. AddComponent&lt;ServicePanel&gt;() on All("machine")).
    /// </summary>
    public static class MapRegistry
    {
        static readonly Dictionary<string, MapId> byId = new Dictionary<string, MapId>();
        static readonly Dictionary<string, List<MapId>> byKind = new Dictionary<string, List<MapId>>();
        static readonly List<string> duplicates = new List<string>();
        static readonly List<MapId> empty = new List<MapId>();

        /// <summary>Ids registered twice by different objects (should be empty; checked by the smoke test).</summary>
        public static IReadOnlyList<string> Duplicates => duplicates;
        public static int Count => byId.Count;
        public static IEnumerable<MapId> Everything => byId.Values;

        public static void Register(MapId m)
        {
            if (m == null || string.IsNullOrEmpty(m.id)) return;
            if (byId.TryGetValue(m.id, out var existing) && existing != null && existing != m)
            {
                if (!duplicates.Contains(m.id)) duplicates.Add(m.id);
                Debug.LogWarning($"[MapRegistry] duplicate id '{m.id}' ({existing.name} / {m.name})", m);
            }
            else if (existing == m) return;
            byId[m.id] = m;
            string k = m.kind ?? "";
            if (!byKind.TryGetValue(k, out var list)) byKind[k] = list = new List<MapId>();
            if (!list.Contains(m)) list.Add(m);
        }

        public static void Unregister(MapId m)
        {
            if (m == null || string.IsNullOrEmpty(m.id)) return;
            if (byId.TryGetValue(m.id, out var cur) && cur == m) byId.Remove(m.id);
            if (byKind.TryGetValue(m.kind ?? "", out var list)) list.Remove(m);
        }

        /// <summary>The MapId with this id, or null.</summary>
        public static MapId Get(string id)
        {
            if (id != null && byId.TryGetValue(id, out var m) && m != null) return m;
            return null;
        }

        public static bool Has(string id) => Get(id) != null;

        public static GameObject GameObject(string id) => Get(id)?.gameObject;

        /// <summary>World position of an id (waypoint, scripted mark, machine...). Fallback if missing.</summary>
        public static Vector3 Position(string id, Vector3 fallback = default)
        {
            var m = Get(id);
            return m != null ? m.transform.position : fallback;
        }

        /// <summary>Component (or interface) T on the object with this id, searching its children too. Null if absent.</summary>
        public static T Get<T>(string id) where T : class
        {
            var m = Get(id);
            if (m == null) return null;
            var c = m.GetComponent<T>();
            if (c == null || (c is Object o && o == null)) c = m.GetComponentInChildren<T>(true);
            if (c is Object uo && uo == null) return null;
            return c;
        }

        /// <summary>Like Get&lt;T&gt; but never null: falls back to the null object registered in Services for T.</summary>
        public static T Resolve<T>(string id) where T : class => Get<T>(id) ?? Services.NullFor<T>(id);

        /// <summary>Every object of a kind (machine, door, camera...). Never null.</summary>
        public static IReadOnlyList<MapId> All(string kind)
        {
            if (kind != null && byKind.TryGetValue(kind, out var list))
            {
                list.RemoveAll(x => x == null);
                return list;
            }
            return empty;
        }

        /// <summary>Every component T found on objects of a kind.</summary>
        public static List<T> All<T>(string kind) where T : class
        {
            var res = new List<T>();
            foreach (var m in All(kind))
            {
                var c = m.GetComponent<T>();
                if (c is Object o && o == null) c = null;
                if (c != null) res.Add(c);
            }
            return res;
        }

        public static IEnumerable<string> Kinds => byKind.Keys;

        public static void Clear() { byId.Clear(); byKind.Clear(); duplicates.Clear(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Clear();
    }
}
