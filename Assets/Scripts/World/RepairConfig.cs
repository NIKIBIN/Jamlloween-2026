using UnityEngine;

namespace Circus
{
    public enum RepairVariant { Hold, Sequence }

    [CreateAssetMenu(menuName = "Circus/Repair Config", fileName = "RepairConfig")]
    public class RepairConfig : ScriptableObject
    {
        public RepairVariant variant = RepairVariant.Hold;
        [Tooltip("Hold: seconds holding E.")] public float durationSeconds = 4f;
        [Tooltip("Sequence: number of stages (each stage = N keys).")] public int steps = 5;
        [Tooltip("Noise radius emitted while working.")] public float noiseRadius = 4f;
        public bool locksCamera;
        [TextArea] public string onRepairedNote;
    }
}
