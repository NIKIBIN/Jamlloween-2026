using HeyHeyThere.PSXCarnivalKit;
using UnityEngine;

namespace Jamlloween.Progression
{
    /// <summary>
    /// Props of the kit that light up with the park power (booth bulbs, lamp posts): on while the LightGroup is on,
    /// off in a blackout. The kit's Powered machines start off, so without this their bulbs never show.
    /// With an empty list, it powers the Machine on its own object.
    /// </summary>
    [DefaultExecutionOrder(100)]   // after the director and the LightGroups
    public sealed class PoweredMachines : MonoBehaviour
    {
        [Tooltip("LightGroup id that powers these machines.")]
        [SerializeField] string groupId = "park";
        [SerializeField] Machine[] machines = new Machine[0];

        bool? applied;

        // Empty list: the machine on this object (a lamp post prefab powers itself).
        void Awake()
        {
            if (machines.Length == 0 && TryGetComponent(out Machine self)) machines = new[] { self };
        }

        void Update()
        {
            bool on = LightGroup.IsGroupOn(groupId);
            if (applied == on) return;
            applied = on;
            foreach (var m in machines)
                if (m != null && m.on != on) m.SetOn(on);
        }
    }
}
