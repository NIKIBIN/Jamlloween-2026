using UnityEngine;

namespace Circus
{
    /// <summary>
    /// Carousel: rotates and plays music when on (needs M5 + power). Emits a large continuous noise
    /// ("carrossel distrai"). In the HUNT phase the Zatan may switch it on by himself ("Zatan liga carrossel");
    /// his own noise does not attract him, and the music masks his footsteps.
    /// </summary>
    public class CarouselController : MonoBehaviour, PowerGrid.IConsumer
    {
        public static CarouselController I { get; private set; }

        [SerializeField] Transform rotor;
        [SerializeField] string panelMachineId = "M5_carousel_panel";
        [SerializeField] float noiseInterval = 1f;
        [SerializeField] float powerCost = 4f;
        [SerializeField] string circuitToDimWhenZatanPowers = "ring";

        public bool IsOn { get; private set; }
        public bool StartedByZatan { get; private set; }
        public float PowerCost => IsOn ? powerCost : 0f;

        float noiseTimer, zatanTimer, speed;

        void Awake() { I = this; ResetZatanTimer(); }
        void OnDestroy() { if (I == this) I = null; }
        void OnEnable() => PowerGrid.Consumers.Add(this);
        void OnDisable() => PowerGrid.Consumers.Remove(this);

        public bool PanelRepaired => RepairableMachine.IsDone(panelMachineId);

        public void SetOn(bool on, bool byZatan)
        {
            if (on && !PowerGrid.HasPower) return;
            if (on && byZatan && Tunables.I.powerGrid && PowerGrid.I && PowerGrid.I.Load + powerCost > Tunables.I.powerBudget)
            {
                var c = LightCircuit.Find(circuitToDimWhenZatanPowers);
                if (c) c.SwitchOn = false;   // the Zatan makes room for his music: the ring goes dark
            }
            IsOn = on;
            StartedByZatan = on && byZatan;
            if (!byZatan) PowerGrid.I?.NotifyLoadChanged(this);
            if (on && byZatan) EventBus.RaiseMessage("♪ ... a música do carrossel começou sozinha ... ♪", 4f);
        }

        public void ForceOff() { IsOn = false; StartedByZatan = false; }

        void ResetZatanTimer() => zatanTimer = Random.Range(Tunables.I.zatanCarouselMinInterval, Tunables.I.zatanCarouselMaxInterval);

        void Update()
        {
            var t = Tunables.I;
            if (IsOn && !PowerGrid.HasPower) IsOn = false;

            float targetSpeed = IsOn ? t.carouselRpm * 6f : 0f;   // deg/s
            speed = Mathf.MoveTowards(speed, targetSpeed, 8f * Time.deltaTime);
            if (rotor) rotor.Rotate(0f, speed * Time.deltaTime, 0f, Space.World);

            if (IsOn && t.carouselDistracts)
            {
                noiseTimer -= Time.deltaTime;
                if (noiseTimer <= 0f)
                {
                    noiseTimer = noiseInterval;
                    NoiseSystem.Emit(transform.position, t.carouselNoiseRadius, NoiseSource.Carousel, StartedByZatan);
                }
            }

            // Zatan switches it on during the hunt
            bool hunt = GameState.I && GameState.I.Phase == GamePhase.Hunt;
            if (t.zatanUsesCarousel && hunt && !IsOn && PanelRepaired && PowerGrid.HasPower)
            {
                zatanTimer -= Time.deltaTime;
                if (zatanTimer <= 0f) { ResetZatanTimer(); SetOn(true, true); }
            }
        }
    }
}
