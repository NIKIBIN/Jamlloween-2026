using System.Collections;
using Jamlloween.Progression;
using UnityEngine;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// The door of the player's Ferris wheel car. Closed until the story opens it (Open, wired in the relay); it swings
    /// open once the car is down at the bottom (at once if it already is). Then E boards the car and starts the ride.
    /// Lives on the same object as its StepInteraction, with a collider over the car.
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
        [Tooltip("Said right when the story asks the door to open (the final chase starts): the player sees Zatan coming.")]
        [SerializeField] RadioLine chaseLine = new RadioLine { speaker = "", text = "Ele tá vindo... não tem saída!" };
        [Tooltip("Said when the door swings open (the car may come down some seconds after the story asks).")]
        [SerializeField] RadioLine openLine = new RadioLine { speaker = "", text = "A porta daquela gôndola abriu..." };

        StepInteraction gate;
        Quaternion closed;
        bool open, wanted;

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

        /// <summary>
        /// The final chase started (Trigger_FinalBarreiras): the player thinks they are cornered, then the door swings open
        /// with a creak once the car is down. Until this is called the car can't be boarded.
        /// </summary>
        public void Open()
        {
            if (open || wanted) return;
            wanted = true;
            var radio = ProgressionDirector.Instance != null ? ProgressionDirector.Instance.Radio : null;
            if (radio != null) radio.Enqueue(chaseLine);
            StartCoroutine(OpenWhenDown());
        }

        IEnumerator OpenWhenDown()
        {
            while (!ride.CarDown) yield return null;
            open = true;
            source.PlayOneShot(openClip);
            var radio = ProgressionDirector.Instance != null ? ProgressionDirector.Instance.Radio : null;
            if (radio != null) radio.Enqueue(openLine);
            yield return Swing();
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
            if (!open || ride.Riding || !ride.CarDown) return;
            if (!gate.TryUse()) return;
            StartCoroutine(Close());
            ride.Board();
            gate.NotifyCompleted();
        }

        /// <summary>Debug jump: the door is open and the player is on the ride.</summary>
        public void ForceComplete()
        {
            StopAllCoroutines();
            open = wanted = true;
            if (!ride.CarDown && !ride.Riding) ride.ForceCarDown();
            hinge.localRotation = closed;
            ride.Board();
        }

        /// <summary>Shuts the door behind the player.</summary>
        IEnumerator Close()
        {
            Quaternion from = hinge.localRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / openSeconds)
            {
                hinge.localRotation = Quaternion.Slerp(from, closed, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            hinge.localRotation = closed;
        }
    }
}
