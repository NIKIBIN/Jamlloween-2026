using System.Collections.Generic;
using UnityEngine;

namespace Circus
{
    /// <summary>Teleport destination for the debug panel.</summary>
    public class ZoneMarker : MonoBehaviour
    {
        public static readonly List<ZoneMarker> All = new List<ZoneMarker>();
        public string label;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
    }
}
