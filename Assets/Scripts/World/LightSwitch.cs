using UnityEngine;

namespace Circus
{
    public class LightSwitch : MonoBehaviour, IInteractable
    {
        public string id;
        public string label;
        public string[] circuitIds;
        [SerializeField] float noiseRadius = 3f;

        public bool CanInteract => true;

        bool AnyOn()
        {
            foreach (var cid in circuitIds) { var c = LightCircuit.Find(cid); if (c && c.SwitchOn) return true; }
            return false;
        }

        public string Prompt => $"LUZ {id}: {(AnyOn() ? "DESLIGAR" : "LIGAR")}" + (PowerGrid.HasPower ? "" : " (SEM ENERGIA)");

        public void Interact(Interactor who)
        {
            bool target = !AnyOn();
            foreach (var cid in circuitIds) { var c = LightCircuit.Find(cid); if (c) c.SwitchOn = target; }
            NoiseSystem.Emit(transform.position, noiseRadius, NoiseSource.Other);
            PowerGrid.I?.NotifyLoadChanged(this);
        }
    }
}
