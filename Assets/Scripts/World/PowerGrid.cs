using System.Collections.Generic;
using UnityEngine;

namespace Circus
{
    /// <summary>
    /// General power comes on with M3. With the "energia" flag on, consumers share a budget;
    /// going over it trips everything for outageDuration seconds, and the consumer that tripped it is switched off.
    /// </summary>
    public class PowerGrid : MonoBehaviour
    {
        public static PowerGrid I { get; private set; }
        // Spike 2: no PowerGrid in the scene = power always on (circuits are driven by SwitchOn).
        public static bool HasPower => I == null || (I.generatorOn && !I.InOutage);

        public interface IConsumer { float PowerCost { get; } void ForceOff(); }
        public static readonly List<IConsumer> Consumers = new List<IConsumer>();

        [SerializeField] string generatorMachineId = "M3_generator";

        bool generatorOn;
        float outageUntil = -1f;
        object lastChanged;
        public bool InOutage => Time.time < outageUntil;
        public float OutageRemaining => Mathf.Max(0, outageUntil - Time.time);
        public bool GeneratorOn => generatorOn;

        void Awake() => I = this;
        void OnDestroy() { if (I == this) I = null; Consumers.Clear(); }
        void OnEnable() => EventBus.MachineRepaired += OnRepaired;
        void OnDisable() => EventBus.MachineRepaired -= OnRepaired;

        void OnRepaired(string id)
        {
            if (id != generatorMachineId) return;
            generatorOn = true;
            EventBus.RaisePowerChanged(true);
        }

        public void ForceGenerator(bool on) { generatorOn = on; EventBus.RaisePowerChanged(HasPower); }

        public float Load
        {
            get
            {
                float l = 0f;
                foreach (var c in LightCircuit.All) if (!c.batteryBacked) l += c.PowerCost;
                foreach (var c in Consumers) l += c.PowerCost;
                return l;
            }
        }

        public void NotifyLoadChanged(object source) => lastChanged = source;

        void Update()
        {
            if (!generatorOn || InOutage || !Tunables.I.powerGrid) return;
            if (Load > Tunables.I.powerBudget) TripOutage();
        }

        public void TripOutage()
        {
            outageUntil = Time.time + Tunables.I.outageDuration;
            // switch off whatever tipped us over, so power can come back
            if (lastChanged is LightSwitch sw)
                foreach (var cid in sw.circuitIds) { var c = LightCircuit.Find(cid); if (c) c.SwitchOn = false; }
            else if (lastChanged is IConsumer con) con.ForceOff();
            lastChanged = null;
            EventBus.RaisePowerChanged(false);
            EventBus.RaiseMessage("*** QUEDA DE ENERGIA ***", 3f);
            Invoke(nameof(RestoreNotify), Tunables.I.outageDuration + 0.05f);
        }

        void RestoreNotify() { if (HasPower) EventBus.RaisePowerChanged(true); }
    }
}
