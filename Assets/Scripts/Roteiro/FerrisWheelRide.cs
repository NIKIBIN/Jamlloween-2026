using System.Collections;
using HeyHeyThere.PSXCarnivalKit;
using Jamlloween.Progression;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// The finale ride on the Ferris wheel (prefab Assets/Prefabs/FerrisWheelRide.prefab: the kit's base and ring, without
    /// the kit's Machine and Animation, plus the motor and the player's car). This script turns the hub itself and the
    /// cars hang upright.
    ///
    /// Flow: Off (no power) -> the motor minigame repairs it -> the wheel turns slowly a few cars and stops with the
    /// player's car at the bottom -> the gondola door opens when the story asks (GondolaDoor) -> E boards: the player is
    /// locked in the car with a narrow look range -> the wheel turns the car slowly up to the top and the power fails
    /// there (its lights go out, the story blacks out the park) -> a tension loop, the look range opens up -> after a
    /// short wait, looking back over the shoulder fires OnLookedBack (wired to Zatan's finale attack).
    /// </summary>
    [DefaultExecutionOrder(1000)]   // after the player
    public sealed class FerrisWheelRide : MonoBehaviour
    {
        enum Phase { Idle, Riding, Stopped, Armed, Done }

        [Header("Player")]
        [SerializeField] LevelWalker walker;
        [SerializeField] CharacterController body;
        [Tooltip("The player's camera (child of the player).")]
        [SerializeField] Transform view;

        [Header("Wheel")]
        [Tooltip("The part that turns: ferris_wheel_ring_spokes. Turns about its local Z.")]
        [SerializeField] Transform hub;
        [Tooltip("Every car, children of the hub. They turn back against the hub to hang upright.")]
        [SerializeField] Transform[] cars;
        [Tooltip("Index in cars of the car the player rides.")]
        [SerializeField] int playerCar = 6;
        [Tooltip("Lit while the wheel has power.")]
        [SerializeField] Renderer[] lights;
        [Tooltip("Eye point inside the player's car, child of the car. Its forward is where the player faces (the park).")]
        [SerializeField] Transform seat;
        [Tooltip("Moved behind the player when they look back; Zatan's finale attack starts there.")]
        [SerializeField] Transform attackPoint;

        [Header("Repair")]
        [Tooltip("Minigame that repairs the motor. Completing it brings the player's car down.")]
        [SerializeField] Minigame repairMinigame;
        [Tooltip("Cars that pass the bottom after the repair before the player's car stops there. The wheel starts this many cars back.")]
        [SerializeField, Min(1)] int repairCars = 2;
        [Tooltip("Seconds from the repair to the car stopped at the bottom. The speed follows from it.")]
        [SerializeField] float repairSpinSeconds = 6f;

        [Header("Turn")]
        [Tooltip("Top speed, degrees per second, of the ride up to the top.")]
        [SerializeField] float degreesPerSecond = 12f;
        [Tooltip("Seconds to reach the top speed and to stop.")]
        [SerializeField] float easeSeconds = 2.5f;

        [Header("Look")]
        [SerializeField] float rideYawLimit = 50f;
        [SerializeField] float stoppedYawLimit = 175f;
        [SerializeField] float pitchMin = -50f;
        [SerializeField] float pitchMax = 30f;
        [Tooltip("Yaw away from the seat's forward that counts as looking back.")]
        [SerializeField] float lookBackAngle = 125f;
        [SerializeField] float lookBackHold = 0.2f;

        [Header("Timing")]
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
        [Tooltip("Completion event sent to the progression director when the wheel stops at the top.")]
        [SerializeField] string stoppedEvent = "WheelStopped";
        [SerializeField] UnityEvent onRepaired = new UnityEvent();
        [Tooltip("The player's car stopped at the bottom, ready to board.")]
        [SerializeField] UnityEvent onCarDown = new UnityEvent();
        [SerializeField] UnityEvent onBoarded = new UnityEvent();
        [Tooltip("The wheel stopped at the top: the power failed. Its own lights go out here; the park blackout comes from the story (WheelStopped).")]
        [SerializeField] UnityEvent onStopped = new UnityEvent();
        [SerializeField] UnityEvent onLookedBack = new UnityEvent();

        Phase phase = Phase.Idle;
        Vector3 viewLocal;
        float yaw, pitch, timer, lookBackTime;
        bool creaked;
        Quaternion hubHome;
        Quaternion[] carHome;
        float[] carPhase;
        Coroutine spin;
        StepInteraction repairGate;

        public bool Riding => phase != Phase.Idle;
        public bool Repaired { get; private set; }
        /// <summary>The player's car waits at the bottom: the door can open and the player can board.</summary>
        public bool CarDown { get; private set; }
        /// <summary>Degrees the hub has turned, clockwise seen from -Z.</summary>
        public float Angle { get; private set; }
        public UnityEvent OnLookedBack => onLookedBack;
        public UnityEvent OnCarDown => onCarDown;
        public UnityEvent OnBoarded => onBoarded;

        void Awake()
        {
            if (walker == null) throw new MissingReferenceException(nameof(walker));
            if (body == null) throw new MissingReferenceException(nameof(body));
            if (view == null) throw new MissingReferenceException(nameof(view));
            if (hub == null) throw new MissingReferenceException(nameof(hub));
            if (cars == null || playerCar < 0 || playerCar >= cars.Length) throw new MissingReferenceException(nameof(cars));
            if (seat == null) throw new MissingReferenceException(nameof(seat));
            if (attackPoint == null) throw new MissingReferenceException(nameof(attackPoint));
            if (repairMinigame == null) throw new MissingReferenceException(nameof(repairMinigame));
            if (rideSource == null) throw new MissingReferenceException(nameof(rideSource));
            if (rideLoop == null) throw new MissingReferenceException(nameof(rideLoop));
            if (stopClip == null) throw new MissingReferenceException(nameof(stopClip));
            if (tensionSource == null) throw new MissingReferenceException(nameof(tensionSource));
            if (tensionLoop == null) throw new MissingReferenceException(nameof(tensionLoop));
            if (creakSource == null) throw new MissingReferenceException(nameof(creakSource));
            if (creakClip == null) throw new MissingReferenceException(nameof(creakClip));

            hubHome = hub.localRotation;
            carHome = new Quaternion[cars.Length];
            carPhase = new float[cars.Length];
            for (int i = 0; i < cars.Length; i++)
            {
                carHome[i] = cars[i].localRotation;
                Vector3 p = cars[i].localPosition;
                carPhase[i] = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
            }
            repairMinigame.onCompleted.AddListener(Repair);
            // A debug jump past the repair step puts the car down at once.
            repairGate = repairMinigame.GetComponent<StepInteraction>();
            if (repairGate != null) repairGate.onForceCompleted.AddListener(ForceCarDown);
        }

        void Start()
        {
            // The wheel waits repairCars cars back, so after the repair it turns slowly that far and stops with the player's car at the bottom.
            ApplyAngle(BottomAngle(playerCar) - repairCars * CarStep);
            SetLights(false);
        }

        float CarStep => 360f / cars.Length;

        void OnDestroy()
        {
            if (repairMinigame != null) repairMinigame.onCompleted.RemoveListener(Repair);
            if (repairGate != null) repairGate.onForceCompleted.RemoveListener(ForceCarDown);
        }

        /// <summary>The motor works again: the wheel turns and stops with the player's car at the bottom.</summary>
        public void Repair()
        {
            if (Repaired) return;
            Repaired = true;
            SetLights(true);
            PlayRideLoop();
            onRepaired.Invoke();
            SpinTo(BottomAngle(playerCar), 0, repairSpinSeconds, () =>
            {
                rideSource.Stop();
                rideSource.PlayOneShot(stopClip);
                CarDown = true;
                onCarDown.Invoke();
            });
        }

        /// <summary>Locks the player in the car and turns it up to the top. GondolaDoor calls it.</summary>
        public void Board()
        {
            if (phase != Phase.Idle || !CarDown) return;
            CarDown = false;
            viewLocal = walker.transform.InverseTransformPoint(view.position);
            walker.enabled = false;
            body.enabled = false;
            yaw = 0f;
            pitch = 0f;
            timer = 0f;
            phase = Phase.Riding;
            PlayRideLoop();
            onBoarded.Invoke();
            SpinTo(TopAngle(playerCar), 0, 0f, Stop);
        }

        void PlayRideLoop()
        {
            rideSource.clip = rideLoop;
            rideSource.loop = true;
            rideSource.Play();
        }

        void LateUpdate()
        {
            if (phase == Phase.Idle || phase == Phase.Done) return;
            timer += Time.deltaTime;

            if (phase == Phase.Stopped && timer >= armDelay) phase = Phase.Armed;
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
            rideSource.Stop();
            rideSource.PlayOneShot(stopClip);
            SetLights(false);   // the power fails at the top: that is why the wheel stops
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

        /// <summary>Hub angle with car i at the top (clockwise turn).</summary>
        float TopAngle(int i) => carPhase[i] - 90f;
        /// <summary>Hub angle with car i at the bottom.</summary>
        float BottomAngle(int i) => carPhase[i] + 90f;

        /// <summary>Turns forward to target. With seconds > 0 the turn takes that long (the speed follows); else it turns at degreesPerSecond.</summary>
        void SpinTo(float target, int extraTurns, float seconds, System.Action then)
        {
            if (spin != null) StopCoroutine(spin);
            float delta = Mathf.Repeat(target - Angle, 360f) + 360f * Mathf.Max(0, extraTurns);
            spin = StartCoroutine(Spin(delta, seconds, then));
        }

        IEnumerator Spin(float delta, float seconds, System.Action then)
        {
            float from = Angle;
            // Trapezoid speed: ease in, cruise, ease out.
            float ease = easeSeconds, speed = degreesPerSecond;
            if (seconds > 0f)
            {
                ease = Mathf.Min(easeSeconds, seconds / 2f);
                speed = delta / Mathf.Max(0.01f, seconds - ease);
            }
            ease = Mathf.Min(ease, delta / speed);   // short turns never reach the top speed
            float cruise = Mathf.Max(0f, delta / speed - ease);
            float total = cruise + 2f * ease;
            for (float t = 0f; t < total; t += Time.deltaTime)
            {
                ApplyAngle(from + delta * Travel(t, ease, cruise));
                yield return null;
            }
            ApplyAngle(from + delta);
            spin = null;
            then?.Invoke();
        }

        /// <summary>Share of the turn done at time t, for a speed that ramps up for ease seconds, holds, and ramps down.</summary>
        static float Travel(float t, float ease, float cruise)
        {
            float area = ease + cruise;   // distance in "top speed x seconds"
            if (area <= 0f) return 1f;
            float d;
            if (t < ease) d = 0.5f * t * t / ease;
            else if (t < ease + cruise) d = 0.5f * ease + (t - ease);
            else { float r = Mathf.Min(t - ease - cruise, ease); d = 0.5f * ease + cruise + r - 0.5f * r * r / ease; }
            return Mathf.Clamp01(d / area);
        }

        void ApplyAngle(float angle)
        {
            Angle = Mathf.Repeat(angle, 360f);
            hub.localRotation = hubHome * Quaternion.Euler(0f, 0f, -Angle);
            for (int i = 0; i < cars.Length; i++) cars[i].localRotation = carHome[i] * Quaternion.Euler(0f, 0f, Angle);
        }

        void SetLights(bool on)
        {
            if (lights == null) return;
            foreach (var r in lights) if (r != null) r.enabled = on;
        }

        /// <summary>Debug jump: the motor repaired and the player's car at the bottom at once.</summary>
        public void ForceCarDown()
        {
            if (CarDown || phase != Phase.Idle) return;
            Repaired = true;
            if (spin != null) StopCoroutine(spin);
            spin = null;
            ApplyAngle(BottomAngle(playerCar));
            SetLights(true);
            CarDown = true;
            onCarDown.Invoke();
        }

        /// <summary>Debug jump: the player is put in the car at the top, with the wheel stopped.</summary>
        public void ForceStopped()
        {
            if (!CarDown && phase == Phase.Idle) ForceCarDown();
            if (phase == Phase.Idle) Board();
            if (phase != Phase.Riding) return;
            if (spin != null) StopCoroutine(spin);
            spin = null;
            ApplyAngle(TopAngle(playerCar));
            Stop();
        }
    }
}
