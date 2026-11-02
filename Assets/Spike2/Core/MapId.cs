using System.Collections.Generic;
using UnityEngine;

namespace Spike2.Core
{
    /// <summary>
    /// Identity of an object that came from circus_map_v2.json. Added by the map builder (agent A) to every
    /// object built from a JSON entry. Registers itself in MapRegistry on Awake (MapRoot also registers inactive
    /// children). Systems and acts find objects only through MapRegistry, never by name or hierarchy.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public class MapId : MonoBehaviour
    {
        [Tooltip("The entry's `id` in the JSON (M3, d_gerador, cam_sala...). Entries without id: <circuit>_<n>.")]
        public string id;
        [Tooltip("JSON key in the singular: machine, door, item, item_candidate, hiding_spot, camera, switch, lamp, " +
                 "bottle_pile, poster, cutout, speaker, tarot, capture_stage, scripted_mark, trigger, zone, waypoint, " +
                 "horse, room, block, fence...")]
        public string kind;
        [Tooltip("The raw JSON of the entry (serialized by the builder with Spike2.Core.Json.Serialize).")]
        [TextArea(1, 6)] public string data;

        Dictionary<string, object> parsed;
        bool parsedOnce;

        /// <summary>The entry as a parsed JSON object (null if the builder stored no data).</summary>
        public Dictionary<string, object> Data
        {
            get
            {
                if (!parsedOnce)
                {
                    parsedOnce = true;
                    if (!string.IsNullOrEmpty(data))
                    {
                        try { parsed = Json.Parse(data) as Dictionary<string, object>; }
                        catch (System.Exception e) { Debug.LogError($"[MapId] bad data on '{id}': {e.Message}", this); }
                    }
                }
                return parsed;
            }
        }

        public string Str(string key, string fallback = null) => JsonData.Str(Data, key, fallback);
        public float Float(string key, float fallback = 0f) => JsonData.Float(Data, key, fallback);
        public bool Bool(string key, bool fallback = false) => JsonData.Bool(Data, key, fallback);
        public Vector3 Position => transform.position;

        void Awake() => MapRegistry.Register(this);
        void OnDestroy() => MapRegistry.Unregister(this);

        /// <summary>For tests and runtime-created objects (e.g. capture props).</summary>
        public static MapId Add(GameObject go, string id, string kind, string data = null)
        {
            var m = go.GetComponent<MapId>() ?? go.AddComponent<MapId>();
            m.id = id; m.kind = kind; m.data = data; m.parsedOnce = false;
            MapRegistry.Register(m);
            return m;
        }
    }
}
