using HeyHeyThere.PSXCarnivalKit;
using UnityEngine;

namespace Jamlloween.Zatan.TestScene
{
    /// <summary>Adapter from the PSX kit player to what Zatan reads.</summary>
    public sealed class LevelWalkerZatanTarget : ZatanTarget
    {
        [SerializeField] LevelWalker walker;
        [SerializeField] CharacterController body;
        [SerializeField] Camera view;

        void Awake()
        {
            if (walker == null) throw new MissingReferenceException(nameof(walker));
            if (body == null) throw new MissingReferenceException(nameof(body));
            if (view == null) throw new MissingReferenceException(nameof(view));
        }

        public override Vector3 Position => transform.position;
        public override Vector3 ChestPosition => transform.position + Vector3.up * (body.height * 0.7f);
        public override Vector3 HorizontalVelocity => Vector3.ProjectOnPlane(body.velocity, Vector3.up);
        public override float WalkSpeed => walker.walk;
        public override float RunSpeed => walker.run;
        public override Camera View => view;
        public override void SetInputEnabled(bool enabled) => walker.enabled = enabled;
    }
}
