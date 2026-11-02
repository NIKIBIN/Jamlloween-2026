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

        public bool IsMoving => HorizontalVelocity.sqrMagnitude > 0.01f;
        public bool IsRunning => HorizontalVelocity.magnitude > (WalkSpeed + RunSpeed) * 0.5f;
    }
}
