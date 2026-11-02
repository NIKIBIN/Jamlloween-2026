using System.Collections.Generic;
using UnityEngine;

namespace Spike2.Core.Stubs
{
    /// <summary>Machines without a panel: no UI, but ForceComplete/CompleteCurrentStep work and publish events.</summary>
    public class NullPanelSystem : IPanelSystem
    {
        readonly Dictionary<string, NullMachine> machines = new Dictionary<string, NullMachine>();
        readonly List<IMachine> all = new List<IMachine>();

        public IMachine Get(string machineId)
        {
            if (machineId == null) machineId = "";
            if (!machines.TryGetValue(machineId, out var m))
            {
                machines[machineId] = m = new NullMachine(machineId);
                all.Add(m);
            }
            return m;
        }

        public string ManualFor(string machineId) => "";
        public IReadOnlyList<IMachine> All => all;
        public IMachine Current => null;
        public IMachine Create(GameObject host, string machineId, string layoutId)
        {
            var m = (NullMachine)Get(machineId);
            m.SetLayout(layoutId);
            m.host = host ? host.transform : null;
            return m;
        }
    }

    public class NullMachine : IMachine
    {
        internal Transform host;
        public NullMachine(string id) { Id = id; LayoutId = id; }
        public string Id { get; }
        public string LayoutId { get; private set; }
        public Transform Transform => host ? host : MapRegistry.Get(Id)?.transform;
        public bool IsDone { get; private set; }
        public bool IsOpen => false;
        public int CurrentStep { get; private set; }
        public int StepCount => 1;
        public bool Interactable { get; set; } = true;
        public string BlockedMessage { get; set; }

        public void SetLayout(string layoutId, bool resetProgress = true)
        {
            LayoutId = layoutId;
            if (resetProgress) { CurrentStep = 0; IsDone = false; }
        }

        public void CompleteCurrentStep()
        {
            Bus.Publish(new MachineStepDone { machineId = Id, layoutId = LayoutId, step = CurrentStep, stepCount = StepCount });
            CurrentStep++;
            if (CurrentStep >= StepCount) ForceComplete();
        }

        public void ForceComplete(bool publish = true)
        {
            IsDone = true; CurrentStep = StepCount;
            if (publish) Bus.Publish(new MachineDone { machineId = Id, layoutId = LayoutId });
        }

        public void ResetStep() { }
        public void ResetAll() { IsDone = false; CurrentStep = 0; }
        public void Close() { }
    }

    /// <summary>Forwards to the spike 1 Door on the map object if there is one; inert otherwise.</summary>
    public class NullDoor : IDoor
    {
        readonly string id;
        public NullDoor(string id) { this.id = id; }
        Circus.Door D => MapRegistry.Get<Circus.Door>(id);

        public string Id => id;
        public bool IsOpen => D && D.IsOpen;
        public bool IsLocked => D && D.IsLocked;
        public bool Lockable => D && D.lockable;
        public string RequiredItem => null;
        public Vector3 Center => D ? D.Center : MapRegistry.Position(id);
        public bool IsHeldByPlayer => false;
        public bool BeingForced { get; set; }
        public float ForceProgress01 { get; set; }
        public void SetOpen(bool open, bool byZatan) { if (D) D.SetOpen(open, byZatan); }
        public void SetLocked(bool locked) { if (D) D.SetLocked(locked); }
        public void Break() { if (D) D.Break(); }
        public void Knock(float strength01) => Circus.NoiseSystem.Emit(Center, 10f * strength01, Circus.NoiseSource.Door, true);
    }
}
