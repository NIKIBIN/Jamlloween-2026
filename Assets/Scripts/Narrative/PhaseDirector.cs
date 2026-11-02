using UnityEngine;

namespace Circus
{
    /// <summary>
    /// OBSERVAR (after M2): the Zatan stands still in his booth whenever the player is near it, and otherwise
    /// shows up at distant points, vanishing when approached. Never attacks.
    /// BRINCAR (after TR1): slow patrol, attacks only at very short range.
    /// CAÇAR (after M4 or M5, once the Death card is read or the delay runs out): full behaviour.
    /// With the director disabled, M2 wakes the Zatan straight into full behaviour.
    /// </summary>
    public class PhaseDirector : MonoBehaviour
    {
        public static PhaseDirector I { get; private set; }

        [SerializeField] string zatanMachineId = "M2_zatan";
        [SerializeField] string generatorMachineId = "M3_generator";
        [SerializeField] string[] huntMachineIds = { "M4_monitors", "M5_carousel_panel" };
        [SerializeField] Transform[] appearPoints;
        [SerializeField] float appearMinDistance = 16f, appearMaxDistance = 34f;
        [SerializeField] float appearDuration = 10f;
        [SerializeField] string plazaCircuit = "plaza";

        bool zatanAwake;
        bool huntPending;
        float huntAt = float.MaxValue;
        float nextAppear, vanishAt;
        bool apparitionVisible;

        void Awake() => I = this;
        void OnDestroy() { if (I == this) I = null; }

        void OnEnable() => EventBus.MachineRepaired += OnRepaired;
        void OnDisable() => EventBus.MachineRepaired -= OnRepaired;

        bool On => Tunables.I.phaseDirector;

        void OnRepaired(string id)
        {
            if (id == zatanMachineId)
            {
                zatanAwake = true;
                if (On) StartObserve();
                else GameState.I?.SetPhase(GamePhase.Hunt);   // ZatanBrain activates himself (full behaviour)
            }
            if (id == generatorMachineId && On && !Tunables.I.scriptedEvents && GameState.I.Phase == GamePhase.Observe)
                EnterPlay();   // without scripted events there is no TR1: go straight to BRINCAR
            foreach (var h in huntMachineIds)
                if (id == h && On && !huntPending && GameState.I.Phase != GamePhase.Hunt)
                {
                    huntPending = true;
                    huntAt = Time.time + Tunables.I.huntDelayAfterCard;
                }
        }

        void Update()
        {
            ZatanBrain.ExternalActivation = On;
            var gs = GameState.I;
            var z = ZatanBrain.I;
            if (!On || gs == null || z == null) return;

            if (huntPending && Time.time >= huntAt) EnterHunt();

            if (gs.Phase == GamePhase.Observe) UpdateObserve(z);
        }

        // ------------------------------------------------------------------ phases

        void StartObserve()
        {
            GameState.I.SetPhase(GamePhase.Observe);
            var z = ZatanBrain.I;
            z.EnterObserve();
            z.Passive = true;
            var plaza = LightCircuit.Find(plazaCircuit);
            if (plaza) plaza.Flicker(3f);
            nextAppear = Time.time + Tunables.I.observeAppearInterval;
        }

        public void EnterPlay()
        {
            var z = ZatanBrain.I;
            if (z == null) return;
            GameState.I.SetPhase(GamePhase.Play);
            z.Passive = false;
            z.SpeedMult = Tunables.I.brincarSpeedMult;
            z.AttackDistance = Tunables.I.brincarAttackDistance;
            z.SetEyes(true);
            // he is no longer in the booth: reappear far away as a walking agent
            var far = FarPoint(FirstPersonController.I.transform.position, 25f, 60f);
            if (far.HasValue) z.PlaceAt(far.Value, FirstPersonController.I.transform.position);
            z.Activate(ZatanState.Patrol);
        }

        public void EnterHunt()
        {
            huntPending = false;
            var z = ZatanBrain.I;
            if (z == null || GameState.I.Phase == GamePhase.Hunt) return;
            if (GameState.I.Phase == GamePhase.Observe || GameState.I.Phase == GamePhase.Intro) EnterPlay();
            GameState.I.SetPhase(GamePhase.Hunt);
            z.Passive = false;
            z.SpeedMult = 1f;
            z.AttackDistance = float.PositiveInfinity;
            EventBus.RaiseMessage("<color=#ff5555>...algo mudou no circo.</color>", 4f);
        }

        /// <summary>Called when the Death card is read: the hunt starts now.</summary>
        public void HuntNow() { if (huntPending) huntAt = Time.time; }

        public void ForcePhase(GamePhase p)
        {
            var z = ZatanBrain.I;
            if (!zatanAwake) { zatanAwake = true; z.SetEyes(true); }
            huntPending = false;
            switch (p)
            {
                case GamePhase.Intro:
                    z.Passive = false; z.SpeedMult = 1; z.AttackDistance = float.PositiveInfinity;
                    z.GoDormant(); GameState.I.SetPhase(GamePhase.Intro); break;
                case GamePhase.Observe: StartObserve(); break;
                case GamePhase.Play: EnterPlay(); break;
                case GamePhase.Hunt: EnterPlay(); EnterHunt(); break;
            }
        }

        // ------------------------------------------------------------------ OBSERVAR

        void UpdateObserve(ZatanBrain z)
        {
            var p = FirstPersonController.I;
            if (!p) return;
            var t = Tunables.I;
            float toBooth = Vector3.Distance(p.transform.position, z.HomePos);

            if (toBooth < t.observeBoothRadius)
            {
                // "he never left": back in the booth, still, facing the opening
                if (apparitionVisible || Vector3.Distance(z.transform.position, z.HomePos) > 0.1f)
                {
                    z.transform.position = z.HomePos;
                    z.transform.rotation = Quaternion.Euler(0, 270, 0);
                    apparitionVisible = false;
                }
                z.SetVisible(true);
                nextAppear = Time.time + t.observeAppearInterval * 0.5f;
                return;
            }

            if (apparitionVisible)
            {
                float d = Vector3.Distance(p.transform.position, z.transform.position);
                if (d < t.observeVanishDistance || Time.time > vanishAt)
                {
                    z.SetVisible(false);
                    apparitionVisible = false;
                    nextAppear = Time.time + t.observeAppearInterval;
                }
                else
                {
                    var look = p.transform.position - z.transform.position; look.y = 0;
                    z.transform.rotation = Quaternion.LookRotation(look);
                }
                return;
            }

            z.SetVisible(false);
            if (Time.time >= nextAppear)
            {
                var pos = FarPoint(p.transform.position, appearMinDistance, appearMaxDistance);
                if (pos.HasValue)
                {
                    z.PlaceAt(pos.Value, p.transform.position);
                    z.SetVisible(true);
                    apparitionVisible = true;
                    vanishAt = Time.time + appearDuration;
                }
                else nextAppear = Time.time + 2f;
            }
        }

        Vector3? FarPoint(Vector3 from, float min, float max)
        {
            if (appearPoints == null || appearPoints.Length == 0) return null;
            for (int tries = 0; tries < 12; tries++)
            {
                var pt = appearPoints[Random.Range(0, appearPoints.Length)].position;
                float d = Vector3.Distance(from, pt);
                if (d >= min && d <= max) return pt;
            }
            return null;
        }
    }
}
