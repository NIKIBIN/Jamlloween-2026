using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Spike2.Core;
using UnityEngine;

namespace Spike2.Acts
{
    /// <summary>
    /// State machine of the run (SPIKE2.md 5.1): Act0 … Act4, Final, Ended. On entering an act it sets the Zatan's
    /// phase, the director's anchors and tools (IZatanDirector.ConfigureAct), the boss line and the Zatan's line on
    /// the clipboard and the manual page. Acts that are not implemented yet run as EmptyAct (wait for the target
    /// machine). F1 "Atos": jump to any act/beat (WorldStateSetup builds the world), restart, sandbox toggle.
    /// Lives in a prefab under Acts/Core/Resources/Systems (SystemsBootstrap instantiates it).
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class ActDirector : MonoBehaviour
    {
        public static ActDirector I { get; private set; }

        static readonly Dictionary<ActId, ActBase> registered = new Dictionary<ActId, ActBase>();

        [Tooltip("Off = sandbox: free map, every machine can be repaired, no acts.")]
        public bool directorEnabled = true;

        public ActId CurrentId { get; private set; } = ActId.Act0;
        public ActBase Current { get; private set; }
        public bool Sandbox => !directorEnabled;

        readonly Dictionary<ActId, float> actStart = new Dictionary<ActId, float>();
        readonly HashSet<string> zatanLines = new HashSet<string>();

        /// <summary>Zatan's handwritten line per act (SPIKE2.md 5.5). Acts may add it earlier with AddZatanLineOnce.</summary>
        public static readonly Dictionary<ActId, string> ZatanLines = new Dictionary<ActId, string>
        {
            { ActId.Act1, "ZATAN" },
            { ActId.Act2, "LUZES, POR FAVOR" },
            { ActId.Act3, "QUERO VER VOCÊ" },
            { ActId.Act4, "O CAVALINHO PERDEU A CABEÇA" },
            { ActId.Final, "O SHOW" },
        };

        public static ZatanPhase PhaseOf(ActId a)
        {
            switch (a)
            {
                case ActId.Act0: return ZatanPhase.Intro;
                case ActId.Act1: return ZatanPhase.Observe;
                case ActId.Act2: return ZatanPhase.Play;
                case ActId.Final: return ZatanPhase.Show;
                default: return ZatanPhase.Hunt;
            }
        }

        public static void Register(ActBase a)
        {
            if (a == null) return;
            if (registered.TryGetValue(a.Act, out var cur) && cur != null && cur != a && !(cur is EmptyAct))
                Debug.LogWarning($"[ActDirector] two acts for {a.Act}: {cur.name} / {a.name}");
            if (cur is EmptyAct && cur != a) Destroy(cur);
            registered[a.Act] = a;
        }

        public static void Unregister(ActBase a)
        {
            if (a != null && registered.TryGetValue(a.Act, out var cur) && cur == a) registered.Remove(a.Act);
        }

        public static ActBase Get(ActId id) => registered.TryGetValue(id, out var a) && a != null ? a : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => registered.Clear();

        // ------------------------------------------------------------------ lifecycle

        void Awake()
        {
            I = this;
            Bus.Subscribe<MachineDone>(OnMachineDone);
        }

        void OnDestroy()
        {
            if (I == this) I = null;
            Bus.Unsubscribe<MachineDone>(OnMachineDone);
            DebugRegistry.RemoveOwner(this);
        }

        IEnumerator Start()
        {
            yield return null;   // let every system adopt its objects (their Start) before touching the world
            for (var id = ActId.Act0; id <= ActId.Final; id++)
                if (Get(id) == null)
                {
                    var empty = gameObject.AddComponent<EmptyAct>();
                    empty.Init(id);
                    Register(empty);
                }
            RegisterDebug();
            GameContext.RunStartTime = Time.time;
            WorldStateSetup.Apply(WorldStateSetup.Baseline());
            if (directorEnabled) StartAct(ActId.Act0);
            else EnterSandbox();
        }

        // ------------------------------------------------------------------ control

        /// <summary>Start an act at a beat. setupWorld = build the world state it needs first (F1 jumps, restarts).</summary>
        public void StartAct(ActId id, string beat = null, bool setupWorld = false)
        {
            if (Current != null) Current.Stop();
            Current = null;

            if (setupWorld)
            {
                zatanLines.Clear();
                Services.Get<IClipboard>().Clear();
                WorldStateSetup.Apply(WorldStateSetup.For(id, beat, Get));
                for (var a = ActId.Act0; a < id; a++) AddActLines(a, alsoDone: true);
            }

            CurrentId = id;
            GameContext.Act = id;
            GameContext.Beat = beat ?? "";
            actStart[id] = Time.time;

            if (id == ActId.Ended)
            {
                Bus.Publish(new ActStarted { act = id });
                return;
            }

            var zatan = Services.Get<IZatan>();
            zatan.SetPhase(PhaseOf(id));
            zatan.MoveOnlyWhenUnobserved = false;
            Services.Get<IZatanDirector>().ConfigureAct((int)id);
            AddActLines(id, alsoDone: false);

            Bus.Publish(new ActStarted { act = id });
            Current = Get(id);
            if (Current != null) Current.Begin(beat);
        }

        /// <summary>F1 / captures: jump to an act and beat, building the world state first.</summary>
        public void JumpTo(ActId id, string beat = null) => StartAct(id, beat, setupWorld: true);

        /// <summary>"Captura = reinício do ato" mode.</summary>
        public void RestartCurrentAct() => JumpTo(CurrentId);

        internal void ActFinished(ActBase act)
        {
            if (act != Current) return;
            act.Stop();
            Bus.Publish(new ActEnded { act = act.Act });
            var next = act.Act == ActId.Final ? ActId.Ended : act.Act + 1;
            StartAct(next);
        }

        /// <summary>Seconds spent in each act so far.</summary>
        public float TimeIn(ActId id) => actStart.TryGetValue(id, out var t) ? Time.time - t : 0f;

        public void AddZatanLineOnce(string text)
        {
            if (string.IsNullOrEmpty(text) || !zatanLines.Add(text)) return;
            Services.Get<IClipboard>().AddZatanLine(text);
        }

        void AddActLines(ActId id, bool alsoDone)
        {
            var target = WorldStateSetup.TargetMachine(id);
            var clip = Services.Get<IClipboard>();
            if (!string.IsNullOrEmpty(target))
            {
                clip.AddBossLine(target, MachineName(target));
                if (alsoDone) clip.SetLineDone(target, true);
                if (!alsoDone) clip.SetManual(target);
            }
            if (ZatanLines.TryGetValue(id, out var line)) AddZatanLineOnce(line);
        }

        static string MachineName(string machineId)
        {
            var m = MapRegistry.Get(machineId);
            var name = m != null ? m.Str("name") : null;
            return (name ?? machineId).ToUpperInvariant();
        }

        void OnMachineDone(MachineDone e)
        {
            if (e.machineId == e.layoutId) Services.Get<IClipboard>().SetLineDone(e.machineId, true);
        }

        void EnterSandbox()
        {
            if (Current != null) Current.Stop();
            Current = null;
            var w = WorldStateSetup.Baseline();
            foreach (var id in MapData.CircuitIds) w.circuits[id] = true;
            w.zatanActive = true;
            w.doorsLocked["d_coxia"] = false;
            WorldStateSetup.Apply(w);
            foreach (var m in Services.Get<IPanelSystem>().All) { m.Interactable = true; m.BlockedMessage = null; }
            Services.Get<IZatanDirector>().ConfigureAct((int)ActId.Act4);
            GameContext.Beat = "sandbox";
        }

        // ------------------------------------------------------------------ F1

        void RegisterDebug()
        {
            const string tab = "Atos";
            DebugRegistry.AddToggle(tab, "ActDirector (desligado = sandbox: mapa livre, tudo consertável)", () => directorEnabled, v =>
            {
                directorEnabled = v;
                if (v) JumpTo(ActId.Act0); else EnterSandbox();
            }, this);
            DebugRegistry.AddText(tab, () => $"Ato {CurrentId} ({(Current ? Current.Title : "-")})  beat {GameContext.Beat}  no ato há {TimeIn(CurrentId):0}s", this);
            DebugRegistry.AddAction(tab, "Reiniciar ato atual (monta o estado)", RestartCurrentAct, this);
            DebugRegistry.AddCustom(tab, DrawJumpGrid, this);
        }

        readonly Dictionary<ActId, bool> fold = new Dictionary<ActId, bool>();

        void DrawJumpGrid()
        {
            GUILayout.Label("Pular para ato / beat (monta o estado do mundo):");
            for (var id = ActId.Act0; id <= ActId.Final; id++)
            {
                var act = Get(id);
                GUILayout.BeginHorizontal();
                fold.TryGetValue(id, out bool open);
                if (GUILayout.Button(open ? "▼" : "►", GUILayout.Width(26))) fold[id] = open = !open;
                if (GUILayout.Button($"{id}  {(act ? act.Title : "")}{(act is EmptyAct ? " (vazio)" : "")}")) JumpTo(id);
                GUILayout.EndHorizontal();
                if (!open || act == null) continue;
                var beats = act.Beats;
                for (int i = 0; i < beats.Count; i += 3)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    for (int j = i; j < Mathf.Min(i + 3, beats.Count); j++)
                        if (GUILayout.Button($"{beats[j].id} {beats[j].label}")) JumpTo(id, beats[j].id);
                    GUILayout.EndHorizontal();
                }
            }
        }
    }

    /// <summary>Placeholder for an act not implemented yet: waits for its target machine, then finishes.</summary>
    public class EmptyAct : ActBase
    {
        ActId act;
        public void Init(ActId id) => act = id;
        public override ActId Act => act;
        public override string Title => $"{act} (placeholder)";

        protected override void Awake() { }   // registered by the director after Init

        protected override void DefineBeats()
        {
            AddBeat("alvo", "consertar a máquina do ato", WaitTarget);
        }

        IEnumerator WaitTarget()
        {
            var target = WorldStateSetup.TargetMachine(act);
            if (string.IsNullOrEmpty(target)) yield break;
            yield return WaitMachineDone(target);
        }
    }
}
