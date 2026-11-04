using HeyHeyThere.PSXCarnivalKit;
using Jamlloween.Progression;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// The finale ride on the park's Ferris wheel (the kit's PF_ferris_wheel_ring, a Powered Machine that loops 'turn').
    /// Board: the player is locked in the gondola (no walking, a narrow look range) and the wheel starts turning.
    /// When the gondola reaches the top the wheel stops, a tension loop starts and the look range opens up.
    /// After a short wait, looking back over the shoulder fires OnLookedBack (wired to Zatan's finale attack).
    /// The gondola cars stay upright while the wheel turns, so the player only follows the seat's position.
    /// </summary>
    [DefaultExecutionOrder(1000)]   // after the wheel animation and the player
    public sealed class FerrisWheelRide : MonoBehaviour
    {
        enum Phase { Idle, Riding, Stopped, Armed, Done }

        [Header("Player")]
        [SerializeField] LevelWalker walker;
        [SerializeField] CharacterController body;
        [Tooltip("The player's camera (child of the player).")]
        [SerializeField] Transform view;

        [Header("Wheel")]
        [SerializeField] Machine wheel;
        [Tooltip("Wheel centre (the axle); its forward is the axle direction. The ride stops when the seat is near the top of its circle.")]
        [SerializeField] Transform axle;
        [Tooltip("The gondola car the seat hangs in (it turns around the axle and stays upright).")]
        [SerializeField] Transform gondola;
        [Tooltip("Eye point inside the gondola, child of the gondola car. Its forward is where the player faces (the park).")]
        [SerializeField] Transform seat;
        [Tooltip("Moved behind the player when they look back; Zatan's finale attack starts there.")]
        [SerializeField] Transform attackPoint;

        [Header("Look")]
        [SerializeField] float rideYawLimit = 50f;
        [SerializeField] float stoppedYawLimit = 175f;
        [SerializeField] float pitchMin = -50f;
        [SerializeField] float pitchMax = 30f;
        [Tooltip("Yaw away from the seat's forward that counts as looking back.")]
        [SerializeField] float lookBackAngle = 125f;
        [SerializeField] float lookBackHold = 0.2f;

        [Header("Timing")]
        [Tooltip("The ride lasts at least this long before it can stop.")]
        [SerializeField] float minRideSeconds = 6f;
        [Tooltip("Stops when the seat is this high on the circle (1 = the very top).")]
        [SerializeField, Range(0.5f, 1f)] float topFraction = 0.97f;
        [Tooltip("Seconds after the stop before looking back triggers the attack.")]
        [SerializeField] float armDelay = 3f;
        [Tooltip("Seconds after the stop before a creak behind the player hints them to look back.")]
        [SerializeField] float creakDelay = 7f;

        [Header("Finale attack point")]
        [SerializeField] float attackDistance = 2.2f;
        [Tooltip("How far below the eye his feet are (his mouth ends up near the player's face).")]
        [SerializeField] float attackDrop = 2.7f;

        [Header("Audio")]
        [SerializeField] AudioSource rideSource;
        [SerializeField] AudioClip rideLoop;
        [SerializeField] AudioClip stopClip;
        [SerializeField] AudioSource tensionSource;
        [SerializeField] AudioClip tensionLoop;
        [Tooltip("Plays behind the player, at the attack point.")]
        [SerializeField] AudioSource creakSource;
        [SerializeField] AudioClip creakClip;

        [Header("Events")]
        [Tooltip("Completion event sent to the progression director when the wheel stops.")]
        [SerializeField] string stoppedEvent = "WheelStopped";
        [SerializeField] UnityEvent onBoarded = new UnityEvent();
        [SerializeField] UnityEvent onStopped = new UnityEvent();
        [SerializeField] UnityEvent onLookedBack = new UnityEvent();

        Phase phase = Phase.Idle;
        Vector3 viewLocal;
        float yaw, pitch, timer, lookBackTime, topHeight, lastY;
        bool creaked;

        public bool Riding => phase != Phase.Idle;
        public UnityEvent OnLookedBack => onLookedBack;

        void Awake()
        {
            if (walker == null) throw new MissingReferenceException(nameof(walker));
            if (body == null) throw new MissingReferenceException(nameof(body));
            if (view == null) throw new MissingReferenceException(nameof(view));
            if (wheel == null) throw new MissingReferenceException(nameof(wheel));
            if (axle == null) throw new MissingReferenceException(nameof(axle));
            if (gondola == null) throw new MissingReferenceException(nameof(gondola));
            if (seat == null) throw new MissingReferenceException(nameof(seat));
            if (attackPoint == null) throw new MissingReferenceException(nameof(attackPoint));
            if (rideSource == null) throw new MissingReferenceException(nameof(rideSource));
            if (rideLoop == null) throw new MissingReferenceException(nameof(rideLoop));
            if (stopClip == null) throw new MissingReferenceException(nameof(stopClip));
            if (tensionSource == null) throw new MissingReferenceException(nameof(tensionSource));
            if (tensionLoop == null) throw new MissingReferenceException(nameof(tensionLoop));
            if (creakSource == null) throw new MissingReferenceException(nameof(creakSource));
            if (creakClip == null) throw new MissingReferenceException(nameof(creakClip));
        }

        /// <summary>Locks the player in the gondola and starts the wheel.</summary>
        public void Board()
        {
            if (phase != Phase.Idle) return;
            viewLocal = walker.transform.InverseTransformPoint(view.position);
            walker.enabled = false;
            body.enabled = false;
            yaw = 0f;
            pitch = 0f;
            timer = 0f;
            // Highest the seat gets: the gondola's circle radius (in the wheel's plane) plus the seat's fixed offset from it.
            float circle = Vector3.ProjectOnPlane(gondola.position - axle.position, axle.forward).magnitude;
            topHeight = circle + (seat.position.y - gondola.position.y);
            lastY = seat.position.y;
            phase = Phase.Riding;
            wheel.SetOn(true);
            rideSource.clip = rideLoop;
            rideSource.loop = true;
            rideSource.Play();
            onBoarded.Invoke();
        }

        void LateUpdate()
        {
            if (phase == Phase.Idle || phase == Phase.Done) return;
            timer += Time.deltaTime;

            switch (phase)
            {
                case Phase.Riding:
                    // Stops at the top: once the seat is high on the circle and starts coming down again.
                    float y = seat.position.y;
                    bool high = y - axle.position.y >= topHeight * topFraction;
                    if (timer >= minRideSeconds && high && y < lastY) Stop();
                    lastY = y;
                    break;
                case Phase.Stopped:
                    if (timer >= armDelay) phase = Phase.Armed;
                    break;
            }
            if (phase == Phase.Armed && !creaked && timer >= creakDelay)
            {
                creaked = true;
                PlaceAttackPoint();
                creakSource.transform.position = attackPoint.position + Vector3.up * attackDrop;
                creakSource.PlayOneShot(creakClip);
            }

            Look();
            Follow();

            if (phase != Phase.Armed) return;
            lookBackTime = Mathf.Abs(yaw) >= lookBackAngle ? lookBackTime + Time.deltaTime : 0f;
            if (lookBackTime >= lookBackHold) Attack();
        }

        void Stop()
        {
            wheel.SetOn(false);
            rideSource.Stop();
            rideSource.PlayOneShot(stopClip);
            tensionSource.clip = tensionLoop;
            tensionSource.loop = true;
            tensionSource.Play();
            phase = Phase.Stopped;
            timer = 0f;
            onStopped.Invoke();
            ProgressionDirector.RaiseEvent(stoppedEvent);
        }

        void Attack()
        {
            phase = Phase.Done;
            PlaceAttackPoint();
            onLookedBack.Invoke();
        }

        void PlaceAttackPoint()
        {
            Vector3 forward = Vector3.ProjectOnPlane(seat.forward, Vector3.up).normalized;
            attackPoint.position = seat.position - forward * attackDistance - Vector3.up * attackDrop;
            attackPoint.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        void Look()
        {
            Vector2 delta = Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue() * (0.1f * walker.mouse);
            float limit = phase == Phase.Riding ? rideYawLimit : stoppedYawLimit;
            yaw = Mathf.Clamp(yaw + delta.x, -limit, limit);
            pitch = Mathf.Clamp(pitch - delta.y, pitchMin, pitchMax);
        }

        void Follow()
        {
            float seatYaw = Quaternion.LookRotation(Vector3.ProjectOnPlane(seat.forward, Vector3.up).normalized, Vector3.up).eulerAngles.y;
            Quaternion bodyRotation = Quaternion.Euler(0f, seatYaw + yaw, 0f);
            walker.transform.SetPositionAndRotation(seat.position - bodyRotation * viewLocal, bodyRotation);
            view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        /// <summary>Debug jump: the player is put in the gondola at the top, with the wheel stopped.</summary>
        public void ForceStopped()
        {
            if (phase == Phase.Idle) Board();
            if (phase == Phase.Riding) Stop();
        }
    }
}
