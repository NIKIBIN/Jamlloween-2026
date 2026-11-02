using UnityEngine;

namespace Circus
{
    /// <summary>Caught = Zatan gets within catchDistance of a visible (not hidden) player.</summary>
    [RequireComponent(typeof(ZatanBrain))]
    public class ZatanCatch : MonoBehaviour
    {
        ZatanBrain brain;

        void Awake() => brain = GetComponent<ZatanBrain>();

        void Update()
        {
            var p = FirstPersonController.I;
            if (!p || brain.Passive || HidingSpot.Occupied != null) return;
            var s = brain.State;
            if (s == ZatanState.Dormant || s == ZatanState.Observe) return;
            var d = p.transform.position - transform.position; d.y = 0;
            if (d.magnitude < Tunables.I.catchDistance) GameState.I?.Caught();
        }
    }
}
