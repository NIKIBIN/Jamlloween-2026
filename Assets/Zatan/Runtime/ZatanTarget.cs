using UnityEngine;

namespace Jamlloween.Zatan
{
    /// <summary>What Zatan needs to know about the player. Each player rig supplies its own adapter.</summary>
    public abstract class ZatanTarget : MonoBehaviour
    {
        public abstract Vector3 Position { get; }
        public abstract Vector3 ChestPosition { get; }
        public abstract Vector3 HorizontalVelocity { get; }
        public abstract float WalkSpeed { get; }
        public abstract float RunSpeed { get; }
        public abstract Camera View { get; }

        /// <summary>Gives or takes away the player's control of the rig (the catch sequence holds the camera).</summary>
        public abstract void SetInputEnabled(bool enabled);

        /// <summary>Puts the player somewhere else (back at a checkpoint after a catch). Rigs with a CharacterController override this.</summary>
        public virtual void Teleport(Vector3 position, Quaternion rotation) => transform.SetPositionAndRotation(position, rotation);

        /// <summary>The hiding spot the player is inside, if any. Zatan can't see or hear a hidden player.</summary>
        public ZatanHidingSpot HidingSpot { get; private set; }
        public bool IsHidden => HidingSpot != null;

        /// <summary>Set by the player's hiding controller; null when the player comes out.</summary>
        public void SetHidingSpot(ZatanHidingSpot spot) => HidingSpot = spot;

        public bool IsMoving => HorizontalVelocity.sqrMagnitude > 0.01f;
        public bool IsRunning => HorizontalVelocity.magnitude > (WalkSpeed + RunSpeed) * 0.5f;
    }
}
