using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Circus
{
    /// <summary>
    /// One repair system for every machine. Variant Hold (hold E, release pauses, moving exits)
    /// or Sequence (stages of N arrow/WASD keys, a mistake restarts the stage, Esc exits).
    /// Progress is kept on the machine when the player leaves.
    /// </summary>
    public class RepairableMachine : MonoBehaviour, IInteractable
    {
        public static readonly List<RepairableMachine> All = new List<RepairableMachine>();
        public static RepairableMachine Active { get; private set; }

        public string id;
        public string label;
        public string zone;
        public int order;
        public RepairConfig config;
        [SerializeField] Renderer statusLed;
        [SerializeField] Material brokenMat, repairedMat;

        public float Progress01 { get; private set; }
        public bool IsRepaired { get; private set; }
        /// <summary>Other systems can block repairs (e.g. the Zatan once awake).</summary>
        public bool Available { get; set; } = true;

        // session
        public RepairVariant SessionVariant { get; private set; }
        public Key[] SeqKeys { get; private set; } = new Key[0];
        public int SeqIndex { get; private set; }
        public int SeqStage { get; private set; }
        public float WrongFlash { get; private set; }
        float noiseTimer, lastWorkTime = -99f;

        static readonly Key[] Arrows = { Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow };
        static readonly Key[] Wasd = { Key.W, Key.S, Key.A, Key.D };

        void OnEnable() => All.Add(this);
        void OnDisable() { All.Remove(this); if (Active == this) Active = null; }

        public RepairVariant EffectiveVariant
        {
            get
            {
                switch (Tunables.I.repairVariant)
                {
                    case RepairVariantOverride.Hold: return RepairVariant.Hold;
                    case RepairVariantOverride.Sequence: return RepairVariant.Sequence;
                    default: return config.variant;
                }
            }
        }

        public float HoldDuration
        {
            get
            {
                var t = Tunables.I;
                float baseDur = config.variant == RepairVariant.Hold ? config.durationSeconds : config.steps * t.holdSecondsPerSequenceStep;
                return Mathf.Max(0.5f, baseDur * t.repairDurationMult);
            }
        }

        public int SequenceStages
        {
            get
            {
                var t = Tunables.I;
                float baseSteps = config.variant == RepairVariant.Sequence ? config.steps : config.durationSeconds * t.sequenceStepsPerHoldSecond;
                return Mathf.Max(1, Mathf.RoundToInt(baseSteps * t.repairDurationMult));
            }
        }

        public bool IsWorking => Time.time - lastWorkTime < 0.6f;

        public bool CanInteract => !IsRepaired && Available && Active == null;
        public string Prompt => $"CONSERTAR {label.ToUpperInvariant()}";

        void Start() => RefreshLed();

        public void Interact(Interactor who)
        {
            Active = this;
            SessionVariant = EffectiveVariant;
            var p = FirstPersonController.I;
            p.SetMode(FirstPersonController.Mode.Busy);
            p.LookLocked = config.locksCamera;
            if (SessionVariant == RepairVariant.Sequence)
            {
                SeqStage = Mathf.FloorToInt(Progress01 * SequenceStages + 0.001f);
                NewStage();
            }
        }

        void NewStage()
        {
            int n = Mathf.Max(1, Tunables.I.sequenceKeysPerStep);
            SeqKeys = new Key[n];
            for (int i = 0; i < n; i++) SeqKeys[i] = Arrows[Random.Range(0, 4)];
            SeqIndex = 0;
        }

        void Update()
        {
            WrongFlash = Mathf.Max(0f, WrongFlash - Time.deltaTime);
            if (Active != this) return;
            var p = FirstPersonController.I;
            var kb = Keyboard.current;
            if (p == null || kb == null || p.CurrentMode == FirstPersonController.Mode.Dead) { EndSession(); return; }
            if (DebugPanelState.IsOpen) return;
            if (kb.escapeKey.wasPressedThisFrame) { EndSession(); return; }

            if (SessionVariant == RepairVariant.Hold)
            {
                if (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed) { EndSession(); return; }
                if (kb.eKey.isPressed)
                {
                    Progress01 = Mathf.Min(1f, Progress01 + Time.deltaTime / HoldDuration);
                    Work();
                }
            }
            else
            {
                for (int k = 0; k < 4; k++)
                {
                    if (!kb[Arrows[k]].wasPressedThisFrame && !kb[Wasd[k]].wasPressedThisFrame) continue;
                    Work(true);
                    if (Arrows[k] == SeqKeys[SeqIndex])
                    {
                        SeqIndex++;
                        if (SeqIndex >= SeqKeys.Length)
                        {
                            SeqStage++;
                            Progress01 = Mathf.Min(1f, (float)SeqStage / SequenceStages);
                            if (Progress01 < 1f) NewStage();
                        }
                    }
                    else
                    {
                        SeqIndex = 0;            // mistake restarts the current stage
                        WrongFlash = 0.35f;
                    }
                    break;
                }
            }

            if (Progress01 >= 1f) Complete();
        }

        void Work(bool keyPress = false)
        {
            lastWorkTime = Time.time;
            noiseTimer -= Time.deltaTime;
            var t = Tunables.I;
            if (noiseTimer <= 0f)
            {
                noiseTimer = t.repairNoiseInterval;
                NoiseSystem.Emit(transform.position, config.noiseRadius * t.repairNoiseMult, NoiseSource.Repair);
                RepairAudio.Beep(transform.position, Progress01);
            }
        }

        public void EndSession()
        {
            if (Active != this) return;
            Active = null;
            var p = FirstPersonController.I;
            if (p && p.CurrentMode == FirstPersonController.Mode.Busy) p.SetMode(FirstPersonController.Mode.Free);
        }

        public void Complete()
        {
            if (IsRepaired) return;
            Progress01 = 1f;
            IsRepaired = true;
            EndSession();
            RefreshLed();
            EventBus.RaiseMessage($"{label.ToUpperInvariant()}: CONSERTADO", 3f);
            EventBus.RaiseMachineRepaired(id);
        }

        public void ResetRepair()
        {
            EndSession();
            IsRepaired = false;
            Progress01 = 0f;
            RefreshLed();
        }

        void RefreshLed()
        {
            if (statusLed) statusLed.sharedMaterial = IsRepaired ? repairedMat : brokenMat;
        }

        public static RepairableMachine Find(string machineId) => All.Find(m => m.id == machineId);
        public static bool IsDone(string machineId) { var m = Find(machineId); return m != null && m.IsRepaired; }
    }
}
