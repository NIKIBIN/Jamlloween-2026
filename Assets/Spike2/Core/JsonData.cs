using System.Collections.Generic;
using UnityEngine;

namespace Spike2.Core
{
    /// <summary>Typed reads over the parsed JSON (Dictionary/List/double). Missing keys return the fallback.</summary>
    public static class JsonData
    {
        public static Dictionary<string, object> Obj(object o) => o as Dictionary<string, object>;
        public static List<object> Arr(object o) => o as List<object>;

        public static object Get(Dictionary<string, object> d, string key)
            => d != null && d.TryGetValue(key, out var v) ? v : null;

        public static bool Has(Dictionary<string, object> d, string key) => d != null && d.ContainsKey(key);

        public static string Str(Dictionary<string, object> d, string key, string fallback = null)
        {
            var v = Get(d, key);
            if (v == null) return fallback;
            if (v is double n) return n.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return v.ToString();
        }

        public static float Float(Dictionary<string, object> d, string key, float fallback = 0f)
            => Get(d, key) is double n ? (float)n : fallback;

        public static int Int(Dictionary<string, object> d, string key, int fallback = 0)
            => Get(d, key) is double n ? (int)n : fallback;

        public static bool Bool(Dictionary<string, object> d, string key, bool fallback = false)
            => Get(d, key) is bool b ? b : fallback;

        /// <summary>[x, z] array -> Vector2.</summary>
        public static Vector2 Vec2(object o, Vector2 fallback = default)
        {
            var a = Arr(o);
            if (a == null || a.Count < 2 || !(a[0] is double x) || !(a[1] is double z)) return fallback;
            return new Vector2((float)x, (float)z);
        }

        public static Vector2 Vec2(Dictionary<string, object> d, string key, Vector2 fallback = default) => Vec2(Get(d, key), fallback);

        /// <summary>[x, z] array -> world position on the ground (y = given height).</summary>
        public static Vector3 Pos(Dictionary<string, object> d, string key, float y = 0f)
        {
            var v = Vec2(d, key);
            return new Vector3(v.x, y, v.y);
        }

        public static List<string> Strings(Dictionary<string, object> d, string key)
        {
            var res = new List<string>();
            var a = Arr(Get(d, key));
            if (a != null) foreach (var o in a) if (o != null) res.Add(o is double n ? n.ToString(System.Globalization.CultureInfo.InvariantCulture) : o.ToString());
            return res;
        }

        public static List<Dictionary<string, object>> Objects(Dictionary<string, object> d, string key)
        {
            var res = new List<Dictionary<string, object>>();
            var a = Arr(Get(d, key));
            if (a != null) foreach (var o in a) if (o is Dictionary<string, object> od) res.Add(od);
            return res;
        }
    }
}
