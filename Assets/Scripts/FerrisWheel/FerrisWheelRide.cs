using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Jamlloween.FerrisWheel
{
    /// <summary>
    /// The ferris wheel of the final sequence. Drives the kit's ring itself (hub turns, cars stay upright), so the kit's
    /// Machine and Animation are removed from the ring in the prefab (Assets/Prefabs/FerrisWheelRide.prefab).
    ///
    /// States: Off (stopped, unlit) -> repair (motor minigame) -> Running, until the player's car is at the bottom ->
    /// Waiting (car open, "[E] Entrar no carrinho") -> the player boards, the door closes -> Running, until the car is at
    /// the top -> Off. The player stays in the car: there is no way out.
    /// Progression and the final scare hook into the events.
    /// </summary>
    public class FerrisWheelRide : MonoBehaviour
    {
        public enum State { Off, Waiting, Running }

        [Header("Roda (do kit)")]
        [Tooltip("The part that turns: ferris_wheel_ring_spokes. Turns about its local Z.")]
        [SerializeField] Transform hub;
        [Tooltip("Every car, children of the hub. They turn back against the hub to hang upright.")]
        [SerializeField] Transform[] cars;
        [Tooltip("Lit while the wheel has power (Waiting and Running).")]
        [SerializeField] Renderer[] lights;

        [Header("Jogador")]
        [SerializeField] FerrisWheelSeat seat;
        [Tooltip("Index in cars of the car the player rides.")]
        [SerializeField] int playerCar = 6;

        [Header("Conserto")]
        [Tooltip("Minigame that repairs the motor. Completing it starts the wheel.")]
        [SerializeField] Minigame repairMinigame;
        [Tooltip("Full turns before the player's car stops at the bottom, so the repair is seen.")]
        [SerializeField] int repairExtraTurns = 1;
        [Tooltip("Seconds from the repair to the car stopped at the bottom, door open. The speed follows from it.")]
        [SerializeField] float repairSpinSeconds = 10f;

        [Header("Giro")]
        [Tooltip("Top speed, degrees per second. The kit's loop turns 9.")]
        [SerializeField] float degreesPerSecond = 20f;
        [Tooltip("Seconds to reach the top speed and to stop.")]
        [SerializeField] float easeSeconds = 2.5f;

        [Header("Eventos")]
        public UnityEvent<State> onStateChanged;
        [Tooltip("The motor was repaired: the wheel starts to bring the player's car down.")]
        public UnityEvent onRepaired;
        [Tooltip("The player's car stopped at the bottom, door open.")]
        public UnityEvent onWaiting;
        [Tooltip("The player is in the car and the door is shut.")]
        public UnityEvent onBoarded;
        [Tooltip("The player's car stopped at the top. The wheel is Off again.")]
        public UnityEvent onReachedTop;

        public State CurrentState { get; private set; } = State.Off;
        public bool Repaired { get; private set; }
        /// <summary>Degrees the hub has turned, clockwise seen from -Z.</summary>
        public float Angle { get; private set; }
        public Transform PlayerCar => cars[playerCar];

        Quaternion hubHome;
        Quaternion[] carHome;
        float[] carPhase;
        Coroutine spin;

        void Awake()
        {
            hubHome = hub.localRotation;
            carHome = new Quaternion[cars.Length];
            carPhase = new float[cars.Length];
            for (int i = 0; i < cars.Length; i++)
            {
                carHome[i] = cars[i].localRotation;
                Vector3 p = cars[i].localPosition;
                carPhase[i] = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
            }
            if (repairMinigame != null) repairMinigame.onCompleted.AddListener(Repair);
            if (seat != null) seat.Init(this);
        }

        void Start()
        {
            ApplyAngle(0f);
            SetLights(false);
        }

        void OnDestroy()
        {
            if (repairMinigame != null) repairMinigame.onCompleted.RemoveListener(Repair);
        }

        /// <summary>The motor works again: turns at least repairExtraTurns times and stops with the player's car at the bottom.</summary>
        public void Repair()
        {
            if (Repaired) return;
            Repaired = true;
            onRepaired?.Invoke();
            SetLights(true);
            SpinTo(BottomAngle(playerCar), repairExtraTurns, () => SetState(State.Waiting), repairSpinSeconds);
        }

        /// <summary>Called by the seat once the player is in and the door is shut: up to the top, then Off.</summary>
        public void Launch()
        {
            if (CurrentState != State.Waiting) return;
            onBoarded?.Invoke();
            SpinTo(TopAngle(playerCar), 0, () =>
            {
                SetLights(false);
                SetState(State.Off);
                onReachedTop?.Invoke();
            });
        }

        /// <summary>Debug and progression skip: the car at the bottom at once, waiting.</summary>
        public void ForceWaiting()
        {
            Repaired = true;
            if (spin != null) StopCoroutine(spin);
            ApplyAngle(BottomAngle(playerCar));
            SetLights(true);
            SetState(State.Waiting);
        }

        /// <summary>Hub angle with car i at the top (clockwise turn).</summary>
        public float TopAngle(int i) => carPhase[i] - 90f;
        /// <summary>Hub angle with car i at the bottom.</summary>
        public float BottomAngle(int i) => carPhase[i] + 90f;

        /// <summary>Turns forward to target. With seconds > 0 the turn takes that long (the speed follows); else it turns at degreesPerSecond.</summary>
        void SpinTo(float target, int extraTurns, System.Action then, float seconds = 0f)
        {
            if (spin != null) StopCoroutine(spin);
            float delta = Mathf.Repeat(target - Angle, 360f) + 360f * Mathf.Max(0, extraTurns);
            spin = StartCoroutine(Spin(delta, then, seconds));
        }

        IEnumerator Spin(float delta, System.Action then, float seconds)
        {
            SetState(State.Running);
            float from = Angle;
            // Trapezoid speed: ease in, cruise, ease out. Total time = delta / speed + ease.
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

        void SetState(State s)
        {
            if (CurrentState == s) return;
            CurrentState = s;
            Debug.Log($"[FerrisWheel] {s}", this);
            onStateChanged?.Invoke(s);
            if (s == State.Waiting) onWaiting?.Invoke();
            if (seat != null) seat.SetDoorOpen(s == State.Waiting);
        }

        void SetLights(bool on)
        {
            if (lights == null) return;
            foreach (var r in lights) if (r != null) r.enabled = on;
        }
    }
}
