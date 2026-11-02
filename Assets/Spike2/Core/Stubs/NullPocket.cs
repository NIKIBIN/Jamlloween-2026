using System.Collections.Generic;
using UnityEngine;

namespace Spike2.Core.Stubs
{
    /// <summary>Functional pocket without world pickups (set of ids), so acts can be tested before agent B.</summary>
    public class NullPocket : IPocket
    {
        readonly HashSet<string> items = new HashSet<string>();
        public IReadOnlyCollection<string> Items => items;
        public bool Has(string itemId) => items.Contains(itemId);
        public void Give(string itemId) { if (items.Add(itemId)) Bus.Publish(new ItemPicked { itemId = itemId }); }
        public bool Take(string itemId) => items.Remove(itemId);
        public void ReturnToOrigin(string itemId) { if (items.Remove(itemId)) Bus.Publish(new ItemReturned { itemId = itemId }); }
        public void SetPickupActive(string itemId, bool active) { }
        public void MovePickup(string itemId, Vector3 pos) { }
    }

    public class NullThrowables : IThrowables
    {
        public int Held { get; private set; }
        public int Max => 2;
        public void Give(int count) => Held = Mathf.Clamp(Held + count, 0, Max);
        public void Clear() => Held = 0;
        public GameObject SpawnLoose(Vector3 pos) { NullServices.Log($"SpawnLoose bottle at {pos}"); return null; }
        public void SetPileActive(string pileId, bool active) { var go = MapRegistry.GameObject(pileId); if (go) go.SetActive(active); }
    }

    public class NullClipboard : IClipboard
    {
        public bool IsUp => false;
        public bool Available { get; set; } = true;
        public void AddBossLine(string lineId, string text) => NullServices.Log($"clipboard boss line {lineId}: {text}");
        public void SetLineDone(string lineId, bool done) { }
        public void AddZatanLine(string text) => NullServices.Log($"clipboard zatan line: {text}");
        public void SetManual(string machineId) { }
        public void Clear() { }
    }
}
