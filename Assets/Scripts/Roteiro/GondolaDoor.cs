using System.Collections;
using Jamlloween.Progression;
using UnityEngine;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// The door of the Ferris wheel gondola. Closed until the story opens it (Open, wired in the relay); then E
    /// boards the gondola and starts the ride. Lives on the same object as its StepInteraction.
    /// The door itself is a PLACEHOLDER box on a hinge: the kit's gondola has no door.
    /// </summary>
    [RequireComponent(typeof(StepInteraction))]
    public sealed class GondolaDoor : MonoBehaviour, IInteractable, IStepForceCompletable
    {
        [SerializeField] string prompt = "Entrar na gôndola";
        [SerializeField] FerrisWheelRide ride;
        [Tooltip("Hinge of the door; it swings around its local Y.")]
        [SerializeField] Transform hinge;
        [SerializeField] float openAngle = -100f;
        [SerializeField] float openSeconds = 0.8f;
        [SerializeField] AudioSource source;
        [SerializeField] AudioClip openClip;

        StepInteraction gate;
        Quaternion closed;
        bool open;

        public string Prompt => open && !ride.Riding ? prompt : string.Empty;

        void Awake()
        {
            gate = GetComponent<StepInteraction>();
            if (ride == null) throw new MissingReferenceException(nameof(ride));
            if (hinge == null) throw new MissingReferenceException(nameof(hinge));
            if (source == null) throw new MissingReferenceException(nameof(source));
            if (openClip == null) throw new MissingReferenceException(nameof(openClip));
            closed = hinge.localRotation;
        }

        /// <summary>Swings the door open with a creak. The story calls this when Zatan closes in.</summary>
        public void Open()
        {
            if (open) return;
            open = true;
            source.PlayOneShot(openClip);
            StartCoroutine(Swing());
        }

        IEnumerator Swing()
        {
            Quaternion target = closed * Quaternion.Euler(0f, openAngle, 0f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / openSeconds)
            {
                hinge.localRotation = Quaternion.Slerp(closed, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            hinge.localRotation = target;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!open || ride.Riding) return;
            if (!gate.TryUse()) return;
            ride.Board();
            gate.NotifyCompleted();
        }

        /// <summary>Debug jump: the door is open and the player is on the ride.</summary>
        public void ForceComplete()
        {
            if (!open)
            {
                open = true;
                hinge.localRotation = closed * Quaternion.Euler(0f, openAngle, 0f);
            }
            ride.Board();
        }
    }
}
