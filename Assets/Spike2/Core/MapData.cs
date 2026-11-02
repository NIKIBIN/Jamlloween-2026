using System.Collections.Generic;
using UnityEngine;

namespace Spike2.Core
{
    /// <summary>
    /// The parsed circus_map_v2.json at runtime (loaded by MapRoot). Gives the global sections that are not
    /// objects in the scene: zones, acts, zatan.act_zones, light_circuits. Empty (never null) without a map.
    /// </summary>
    public static class MapData
    {
        public static Dictionary<string, object> Root { get; private set; }
        public static bool Loaded => Root != null;

        static readonly List<Zone> zones = new List<Zone>();
        public static IReadOnlyList<Zone> Zones => zones;

        public static void Load(string json)
        {
            Root = Json.Parse(json) as Dictionary<string, object>;
            zones.Clear();
            foreach (var z in Section("zones")) zones.Add(Zone.From(z));
        }

        public static void Clear() { Root = null; zones.Clear(); }

        /// <summary>Top-level list section ("machines", "cameras"...) as objects. Empty if absent.</summary>
        public static List<Dictionary<string, object>> Section(string key) => JsonData.Objects(Root, key);

        /// <summary>Top-level object section ("tent", "carousel", "zatan", "light_circuits"...). Null if absent.</summary>
        public static Dictionary<string, object> Object(string key) => JsonData.Obj(JsonData.Get(Root, key));

        /// <summary>zatan.act_zones[act].anchor (waypoint ids where the director may place the Zatan).</summary>
        public static List<string> ActAnchors(int act) => JsonData.Strings(ActZone(act), "anchor");

        /// <summary>zatan.act_zones[act].patrol (default patrol route, waypoint ids).</summary>
        public static List<string> ActPatrol(int act) => JsonData.Strings(ActZone(act), "patrol");

        static Dictionary<string, object> ActZone(int act)
        {
            var az = JsonData.Obj(JsonData.Get(Object("zatan"), "act_zones"));
            return JsonData.Obj(JsonData.Get(az, act.ToString()));
        }

        /// <summary>light_circuits[id] (on_at_start, on_after...).</summary>
        public static Dictionary<string, object> Circuit(string id) => JsonData.Obj(JsonData.Get(Object("light_circuits"), id));

        public static IEnumerable<string> CircuitIds
        {
            get
            {
                var lc = Object("light_circuits");
                if (lc != null) foreach (var k in lc.Keys) yield return k;
            }
        }

        // ------------------------------------------------------------------ zones

        /// <summary>First zone (JSON order) containing the point, or null.</summary>
        public static string ZoneAt(Vector3 p)
        {
            foreach (var z in zones) if (z.Contains(p)) return z.id;
            return null;
        }

        public static IEnumerable<string> ZonesAt(Vector3 p)
        {
            foreach (var z in zones) if (z.Contains(p)) yield return z.id;
        }

        public static Zone GetZone(string id) => zones.Find(z => z.id == id);

        public class Zone
        {
            public string id, label;
            public enum Shape { Rect, Circle, Ring }
            public Shape shape;
            public Vector2 min, max, center;
            public float rMin, rMax;
            public bool losExempt;

            public Vector3 Center3 => shape == Shape.Rect ? new Vector3((min.x + max.x) / 2f, 0f, (min.y + max.y) / 2f) : new Vector3(center.x, 0f, center.y);

            public bool Contains(Vector3 p)
            {
                var q = new Vector2(p.x, p.z);
                switch (shape)
                {
                    case Shape.Rect: return q.x >= min.x && q.x <= max.x && q.y >= min.y && q.y <= max.y;
                    case Shape.Circle: return (q - center).sqrMagnitude <= rMax * rMax;
                    default: float d = (q - center).magnitude; return d >= rMin && d <= rMax;
                }
            }

            public static Zone From(Dictionary<string, object> d)
            {
                var z = new Zone { id = JsonData.Str(d, "id"), label = JsonData.Str(d, "label"), losExempt = JsonData.Bool(d, "los_exempt") };
                if (JsonData.Get(d, "rect") is List<object> r && r.Count == 2)
                {
                    z.shape = Shape.Rect;
                    var a = JsonData.Vec2(r[0]); var b = JsonData.Vec2(r[1]);
                    z.min = Vector2.Min(a, b); z.max = Vector2.Max(a, b);
                }
                else if (JsonData.Obj(JsonData.Get(d, "circle")) is Dictionary<string, object> c)
                {
                    z.shape = Shape.Circle; z.center = JsonData.Vec2(c, "center"); z.rMax = JsonData.Float(c, "radius");
                }
                else if (JsonData.Obj(JsonData.Get(d, "ring")) is Dictionary<string, object> g)
                {
                    z.shape = Shape.Ring; z.center = JsonData.Vec2(g, "center");
                    z.rMin = JsonData.Float(g, "r_min"); z.rMax = JsonData.Float(g, "r_max");
                }
                return z;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Clear();
    }
}
