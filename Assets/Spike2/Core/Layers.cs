using UnityEngine;

namespace Spike2.Core
{
    /// <summary>
    /// Layers configured once in ProjectSettings (SPIKE2_MAP.md 2). Vision raycasts (Zatan, cameras) use only
    /// LOSBlocker. Look them up by name so a missing layer shows up as -1 instead of a silent wrong index.
    /// </summary>
    public static class Layers
    {
        public const string LOSBlockerName = "LOSBlocker";   // blocks_los props, room walls, tent wall, tapume, closed doors, carousel column
        public const string LowPropName = "LowProp";         // low props, cages, windows, low fences, cutouts: collide, don't cut vision
        public const string PerimeterName = "Perimeter";     // chain-link perimeter fence
        public const string PlayerName = "Player";
        public const string ZatanName = "Zatan";
        public const string InteractableName = "Interactable"; // optional: interaction-only colliders

        public static int LOSBlocker => LayerMask.NameToLayer(LOSBlockerName);
        public static int LowProp => LayerMask.NameToLayer(LowPropName);
        public static int Perimeter => LayerMask.NameToLayer(PerimeterName);
        public static int Player => LayerMask.NameToLayer(PlayerName);
        public static int Zatan => LayerMask.NameToLayer(ZatanName);
        public static int Interactable => LayerMask.NameToLayer(InteractableName);

        /// <summary>Mask for vision raycasts: only LOSBlocker.</summary>
        public static int VisionMask => LayerMask.GetMask(LOSBlockerName);

        /// <summary>Everything solid for movement/physics queries.</summary>
        public static int SolidMask => LayerMask.GetMask("Default", LOSBlockerName, LowPropName, PerimeterName);
    }
}
