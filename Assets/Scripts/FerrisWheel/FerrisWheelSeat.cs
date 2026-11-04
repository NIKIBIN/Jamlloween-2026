using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jamlloween.FerrisWheel
{
    /// <summary>
    /// The player's car. While the wheel waits, E boards it: the player's movement is switched off for good, the body
    /// rides on the car, the mouse still looks all around (the final scare needs a look back), and the door shuts.
    /// Then the wheel is told to launch. Lives on a child of the car, with a trigger collider over it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FerrisWheelSeat : MonoBehaviour, IInteractable
    {
        [SerializeField] string prompt = "Entrar no carrinho";
        [Tooltip("Where the player's feet go, facing the way the player looks at first. Child of the car.")]
        [SerializeField] Transform anchor;
        [Tooltip("Eye height above the anchor while seated.")]
        [SerializeField] float seatedEyeHeight = 1.0f;

        [Header("Porta")]
        [SerializeField] Transform door;
        [Tooltip("Door turn about its local Y when open, degrees.")]
        [SerializeField] float doorOpenAngle = -105f;
        [SerializeField] float doorSeconds = 0.6f;

        [Header("Olhar")]
        [Tooltip("Same scale as the kit's LevelWalker: delta x 0.1 x mouse.")]
        [SerializeField] float mouseSensitivity = 2f;
        [SerializeField] float pitchLimit = 80f;
        [SerializeField] float boardSeconds = 0.8f;

        FerrisWheelRide ride;
        Collider trigger;
        Quaternion doorClosed;
        Coroutine doorRoutine;
        Transform player, view;
        float pitch;

        public bool Occupied => player != null;
        public Transform Passenger => player;

        public string Prompt => ride != null && ride.CurrentState == FerrisWheelRide.State.Waiting && !Occupied ? prompt : string.Empty;

        public void Init(FerrisWheelRide owner)
        {
            ride = owner;
            trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
            if (door != null) doorClosed = door.localRotation;
        }

        public void SetDoorOpen(bool open)
        {
            if (door == null) return;
            if (doorRoutine != null) StopCoroutine(doorRoutine);
            doorRoutine = StartCoroutine(TurnDoor(open ? Quaternion.Euler(0f, doorOpenAngle, 0f) : Quaternion.identity));
        }

        IEnumerator TurnDoor(Quaternion offset)
        {
            Quaternion from = door.localRotation, to = doorClosed * offset;
            for (float t = 0f; t < 1f; t += Time.deltaTime / doorSeconds)
            {
                door.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            door.localRotation = to;
            doorRoutine = null;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (string.IsNullOrEmpty(Prompt)) return;
            StartCoroutine(Board(interactor));
        }

        IEnumerator Board(PlayerInteractor interactor)
        {
            player = interactor.transform;
            view = interactor.ViewCamera;
            trigger.enabled = false;
            Lock(player.gameObject);

            // Glide into the seat, then ride with the car.
            Vector3 eyeOffset = Vector3.up * (seatedEyeHeight - view.localPosition.y);
            Vector3 p0 = player.position;
            Quaternion r0 = player.rotation;
            float pitch0 = NormalizedPitch(view.localEulerAngles.x);
            for (float t = 0f; t < 1f; t += Time.deltaTime / boardSeconds)
            {
                float k = Mathf.SmoothStep(0f, 1f, t);
                player.SetPositionAndRotation(Vector3.Lerp(p0, anchor.position + anchor.rotation * eyeOffset, k), Quaternion.Slerp(r0, anchor.rotation, k));
                pitch = Mathf.Lerp(pitch0, 0f, k);
                view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                yield return null;
            }
            player.SetParent(anchor, true);
            player.localPosition = eyeOffset;
            player.localRotation = Quaternion.identity;

            SetDoorOpen(false);
            yield return new WaitForSeconds(doorSeconds);
            ride.Launch();
        }

        /// <summary>Switches off what moves the player and what lets them act. The rest (Zatan's target, the flashlight) keeps working.</summary>
        static void Lock(GameObject p)
        {
            var cc = p.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            foreach (var b in p.GetComponents<Behaviour>())
            {
                string n = b.GetType().Name;
                if (n == "LevelWalker" || n == "FirstPersonController" || n == "PlayerInteractor" || n == "GasCanCarrier" || n == "Inspect")
                    b.enabled = false;
            }
        }

        void Update()
        {
            if (player == null || player.parent != anchor) return;
            var mouse = Mouse.current;
            if (mouse == null) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Vector2 d = mouse.delta.ReadValue() * 0.1f * mouseSensitivity;
            player.Rotate(0f, d.x, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - d.y, -pitchLimit, pitchLimit);
            view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        static float NormalizedPitch(float x) => x > 180f ? x - 360f : x;

        void OnDrawGizmosSelected()
        {
            if (anchor == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(anchor.position, 0.1f);
            Gizmos.DrawRay(anchor.position, anchor.forward * 0.5f);
            Gizmos.DrawRay(anchor.position + anchor.up * seatedEyeHeight, anchor.forward * 0.3f);
        }
    }
}
