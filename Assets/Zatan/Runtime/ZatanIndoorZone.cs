using UnityEngine;

namespace Jamlloween.Zatan
{
    /// <summary>
    /// Marks an interior: while the player is inside, tension falls.
    /// Polls the bounds instead of trigger events so a teleport out of the zone is never missed.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ZatanIndoorZone : MonoBehaviour
    {
        [SerializeField] ZatanTension tension;
        [SerializeField] ZatanTarget target;
        [SerializeField] BoxCollider area;

        bool occupied;

        void Awake()
        {
            if (tension == null) throw new MissingReferenceException(nameof(tension));
            if (target == null) throw new MissingReferenceException(nameof(target));
            if (area == null) throw new MissingReferenceException(nameof(area));
        }

        void Update()
        {
            bool inside = area.bounds.Contains(target.ChestPosition);
            if (inside == occupied) return;
            occupied = inside;
            if (inside) tension.EnterIndoorZone();
            else tension.ExitIndoorZone();
        }
    }
}
