using UnityEngine;

namespace Jamlloween.Zatan
{
    /// <summary>
    /// Keeps the eyes, beams and mouth (children of this object) on the rig's head bone. The position follows the head
    /// exactly; the rotation takes the head's yaw fully but only part of its pitch, so the run's forward lean doesn't
    /// point the beams into the ground. Runs after the head turn of the hiding reactions and before the eye lights.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class ZatanHeadFollow : MonoBehaviour
    {
        [Tooltip("The rig's head bone.")]
        [SerializeField] Transform head;
        [Tooltip("Zatan's root: the body the eyes are aimed from.")]
        [SerializeField] Transform body;
        [Tooltip("Where this anchor sits, in the head bone's space (set by the editor tool at the rest pose).")]
        [SerializeField] Vector3 offset;
        [Tooltip("The head bone's rotation relative to the body at the rest pose: the head turns are measured from it.")]
        [SerializeField] Quaternion restRelative = Quaternion.identity;
        [SerializeField, Range(0f, 1f)] float yawFollow = 1f;
        [SerializeField, Range(0f, 1f)] float pitchFollow = 0.4f;

        void Awake()
        {
            if (head == null) throw new MissingReferenceException(nameof(head));
            if (body == null) throw new MissingReferenceException(nameof(body));
        }

        /// <summary>Editor setup: anchors here, at the head's current pose.</summary>
        public void Bind(Transform headBone, Transform bodyRoot)
        {
            head = headBone;
            body = bodyRoot;
            offset = head.InverseTransformPoint(transform.position);
            restRelative = Quaternion.Inverse(body.rotation) * head.rotation;
        }

        void LateUpdate()
        {
            Quaternion rel = Quaternion.Inverse(body.rotation) * head.rotation * Quaternion.Inverse(restRelative);
            Vector3 f = rel * Vector3.forward;
            float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f)) * Mathf.Rad2Deg;
            transform.SetPositionAndRotation(head.TransformPoint(offset),
                body.rotation * Quaternion.Euler(pitch * pitchFollow, yaw * yawFollow, 0f));
        }
    }
}
