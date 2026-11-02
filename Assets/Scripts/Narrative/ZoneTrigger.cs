using System.Collections.Generic;
using UnityEngine;

namespace Circus
{
    /// <summary>Box zone checked by position (no physics callbacks needed). Data from circus_map.json triggers.</summary>
    public class ZoneTrigger : MonoBehaviour
    {
        public static readonly List<ZoneTrigger> All = new List<ZoneTrigger>();

        public string id;
        [TextArea] public string condition;
        [TextArea] public string effect;
        public Vector3 size = new Vector3(4, 3, 4);

        public bool PlayerInside { get; private set; }
        public bool EnteredThisFrame { get; private set; }
        public bool ExitedThisFrame { get; private set; }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public bool Contains(Vector3 p)
        {
            var b = new Bounds(transform.position, size);
            return b.Contains(new Vector3(p.x, transform.position.y, p.z));
        }

        void Update()
        {
            var p = FirstPersonController.I;
            bool inside = p && Contains(p.transform.position);
            EnteredThisFrame = inside && !PlayerInside;
            ExitedThisFrame = !inside && PlayerInside;
            PlayerInside = inside;
        }

        public static ZoneTrigger Find(string zid) => All.Find(z => z.id == zid);
    }
}
