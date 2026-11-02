using System.Collections.Generic;
using UnityEngine;

namespace Circus
{
    /// <summary>A group of lights. On = switch on AND (power OR battery). Also answers "is this point lit?".</summary>
    public class LightCircuit : MonoBehaviour
    {
        public static readonly List<LightCircuit> All = new List<LightCircuit>();

        public string id;
        public bool batteryBacked;
        [SerializeField] Light[] lights;
        [SerializeField] Renderer[] bulbs;
        [SerializeField] Material bulbOn, bulbOff;
        [SerializeField] float litRadiusFactor = 0.75f;
        [SerializeField] float powerCostPerLight = 1f;

        public bool SwitchOn { get; set; } = true;
        public bool IsOn { get; private set; }
        public float PowerCost => SwitchOn ? powerCostPerLight * lights.Length : 0f;
        public int LightCount => lights.Length;

        float flickerUntil;
        bool applied, appliedState;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void Flicker(float seconds) => flickerUntil = Time.time + seconds;

        void Update()
        {
            bool on = SwitchOn && (batteryBacked || PowerGrid.HasPower);
            if (Time.time < flickerUntil) on = Mathf.PerlinNoise(Time.time * 14f, id.Length) > 0.5f;
            IsOn = on;
            if (applied && appliedState == on) return;
            applied = true; appliedState = on;
            foreach (var l in lights) if (l) l.enabled = on;
            foreach (var b in bulbs) if (b) b.sharedMaterial = on ? bulbOn : bulbOff;
        }

        public bool Illuminates(Vector3 p)
        {
            if (!IsOn) return false;
            foreach (var l in lights)
            {
                if (!l) continue;
                var d = l.transform.position - p; d.y = 0;
                if (d.magnitude < l.range * litRadiusFactor) return true;
            }
            return false;
        }

        public static bool IsPointLit(Vector3 p)
        {
            foreach (var c in All) if (c.Illuminates(p)) return true;
            return false;
        }

        public static LightCircuit Find(string circuitId) => All.Find(c => c.id == circuitId);
    }
}
