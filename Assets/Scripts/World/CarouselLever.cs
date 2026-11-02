using UnityEngine;

namespace Circus
{
    /// <summary>The M5 panel becomes an on/off lever once repaired.</summary>
    [RequireComponent(typeof(RepairableMachine))]
    public class CarouselLever : MonoBehaviour, IInteractable
    {
        RepairableMachine machine;
        void Awake() => machine = GetComponent<RepairableMachine>();

        CarouselController C => CarouselController.I;

        public bool CanInteract => machine.IsRepaired && C != null;
        public string Prompt => !PowerGrid.HasPower ? "CARROSSEL (SEM ENERGIA)" : C.IsOn ? "DESLIGAR CARROSSEL" : "LIGAR CARROSSEL";

        public void Interact(Interactor who)
        {
            if (!PowerGrid.HasPower) return;
            C.SetOn(!C.IsOn, false);
        }
    }
}
