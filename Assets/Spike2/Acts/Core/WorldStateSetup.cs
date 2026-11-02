using System;
using System.Collections.Generic;
using System.Linq;
using Spike2.Core;
using UnityEngine;

namespace Spike2.Acts
{
    /// <summary>
    /// The world state an act or beat needs. Built by WorldStateSetup.For(act, beat) and applied through the
    /// Core contracts (IMachine.ForceComplete, circuits, pocket, doors, cameras). Acts add to it in
    /// ActBase.DeclareConsequences / PrepareBeat.
    /// </summary>
    public class WorldState
    {
        public readonly HashSet<string> machinesDone = new HashSet<string>();
        /// <summary>circuit id -> on. Missing = derived from on_at_start / on_after.</summary>
        public readonly Dictionary<string, bool> circuits = new Dictionary<string, bool>();
        public readonly HashSet<string> pocket = new HashSet<string>();
        /// <summary>door id -> locked. Missing = starts_locked from the JSON.</summary>
        public readonly Dictionary<string, bool> doorsLocked = new Dictionary<string, bool>();
        public readonly Dictionary<string, bool> doorsOpen = new Dictionary<string, bool>();
        /// <summary>item / pile id -> pickup visible.</summary>
        public readonly Dictionary<string, bool> pickupsActive = new Dictionary<string, bool>();
        public readonly HashSet<string> camerasOn = new HashSet<string>();
        public bool camerasReport;
        public bool carouselOn;
        /// <summary>false = Zatan dormant in the booth (acts 0-1).</summary>
        public bool zatanActive;
        public int bottles;
        /// <summary>Free-form flags for acts and systems (e.g. "head_mounted", "tarot:t_lua").</summary>
        public readonly HashSet<string> flags = new HashSet<string>();

        public void Done(params string[] machineIds) { foreach (var m in machineIds) machinesDone.Add(m); }
    }

    public static class WorldStateSetup
    {
        /// <summary>Raised after Apply, so systems can react (tarot cards shown, posters...).</summary>
        public static event Action<WorldState> Applied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Applied = null;

        /// <summary>Target machine of an act (acts[n].target in the JSON: M1..M6).</summary>
        public static string TargetMachine(ActId act)
        {
            foreach (var a in MapData.Section("acts"))
                if (JsonData.Int(a, "id", -1) == (int)act) return JsonData.Str(a, "target");
            return null;
        }

        /// <summary>Pocket items of an act (acts[n].items).</summary>
        public static List<string> ActItems(ActId act)
        {
            foreach (var a in MapData.Section("acts"))
                if (JsonData.Int(a, "id", -1) == (int)act) return JsonData.Strings(a, "items");
            return new List<string>();
        }

        /// <summary>State at the very start of the run.</summary>
        public static WorldState Baseline() => new WorldState();

        /// <summary>State to start `act` at `beat`: consequences of every earlier act, then the act's PrepareBeat.</summary>
        public static WorldState For(ActId act, string beat, Func<ActId, ActBase> actLookup)
        {
            var w = Baseline();
            for (var a = ActId.Act0; a < act && a <= ActId.Final; a++)
            {
                DefaultConsequences(a, w);
                actLookup?.Invoke(a)?.DeclareConsequences(w);
            }
            if (act >= ActId.Act2) w.zatanActive = true;
            actLookup?.Invoke(act)?.PrepareBeat(beat, w);
            return w;
        }

        /// <summary>What the spec says is true after each act (SPIKE2.md 4, "Consequência").</summary>
        public static void DefaultConsequences(ActId act, WorldState w)
        {
            var target = TargetMachine(act);
            if (!string.IsNullOrEmpty(target)) w.machinesDone.Add(target);
            switch (act)
            {
                case ActId.Act1:
                    w.pickupsActive["garrafas_cabine"] = true;
                    w.flags.Add("tarot:t_lua");
                    break;
                case ActId.Act3:
                    foreach (var c in MapRegistry.All("camera")) w.camerasOn.Add(c.id);
                    w.camerasReport = true;
                    w.flags.Add("tarot:t_louco");
                    break;
                case ActId.Act4:
                    w.carouselOn = true;
                    w.flags.Add("head_mounted");
                    w.flags.Add("tarot:t_torre");
                    break;
                case ActId.Final:
                    w.doorsLocked["d_coxia"] = false;
                    break;
            }
            if (act == ActId.Act0) w.flags.Add("tarot:t_sacerdotisa");
            if (act == ActId.Act2) w.flags.Add("tarot:t_sol");
        }

        /// <summary>Is circuit `id` on in state w (explicit override, else on_at_start / on_after a done machine)?</summary>
        public static bool CircuitOn(WorldState w, string id)
        {
            if (w.circuits.TryGetValue(id, out bool on)) return on;
            var c = MapData.Circuit(id);
            if (JsonData.Bool(c, "on_at_start")) return true;
            var after = JsonData.Str(c, "on_after");
            return !string.IsNullOrEmpty(after) && w.machinesDone.Contains(after);
        }

        /// <summary>Apply a state through the contracts. Safe with stubs (they ignore what they can't do).</summary>
        public static void Apply(WorldState w)
        {
            // machines
            var panels = Services.Get<IPanelSystem>();
            foreach (var m in MapRegistry.All("machine"))
            {
                var machine = panels.Get(m.id);
                if (machine.LayoutId != m.id) machine.SetLayout(m.id);
                if (w.machinesDone.Contains(m.id)) machine.ForceComplete(publish: false);
                else machine.ResetAll();
            }

            // lights
            foreach (var id in MapData.CircuitIds)
            {
                var c = Circus.LightCircuit.Find(id);
                if (c) c.SwitchOn = CircuitOn(w, id);
            }

            // pocket and pickups
            var pocket = Services.Get<IPocket>();
            foreach (var item in pocket.Items.ToList()) pocket.ReturnToOrigin(item);
            foreach (var item in w.pocket) pocket.Give(item);
            foreach (var kv in w.pickupsActive)
            {
                if (MapRegistry.Get(kv.Key)?.kind == "bottle_pile") Services.Get<IThrowables>().SetPileActive(kv.Key, kv.Value);
                else pocket.SetPickupActive(kv.Key, kv.Value);
            }
            var bottles = Services.Get<IThrowables>();
            bottles.Clear();
            if (w.bottles > 0) bottles.Give(w.bottles);

            // doors
            foreach (var d in MapRegistry.All("door"))
            {
                var door = MapRegistry.Resolve<IDoor>(d.id);
                bool locked = w.doorsLocked.TryGetValue(d.id, out var l) ? l : d.Bool("starts_locked");
                door.SetLocked(locked);
                if (!locked) door.SetOpen(w.doorsOpen.TryGetValue(d.id, out var o) && o, false);
            }

            // cameras
            var cams = Services.Get<ICameraNetwork>();
            foreach (var id in cams.CameraIds.ToList()) cams.SetOn(id, w.camerasOn.Contains(id));
            cams.ReportsToZatan = w.camerasReport;

            // carousel
            var car = Circus.CarouselController.I;
            if (car) { if (w.carouselOn) car.SetOn(true, false); else car.ForceOff(); }

            // zatan
            if (!w.zatanActive) Services.Get<IZatan>().Deactivate();

            Applied?.Invoke(w);
        }
    }
}
