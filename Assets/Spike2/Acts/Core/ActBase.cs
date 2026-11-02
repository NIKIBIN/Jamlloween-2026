using System;
using System.Collections;
using System.Collections.Generic;
using Spike2.Core;
using UnityEngine;

namespace Spike2.Acts
{
    /// <summary>
    /// One act of the ActDirector (SPIKE2.md 5.1). Subclass per act (Act2Generator : ActBase), ship it in a prefab
    /// under Acts/&lt;ActX&gt;/Resources/Systems/ (SystemsBootstrap instantiates it; the act registers itself).
    ///
    /// Beats are coroutines registered in DefineBeats() in order; the act runs them one after another and finishes
    /// after the last one (or when it calls Finish()). F1 can start the act at any beat: WorldStateSetup builds the
    /// world (consequences of earlier acts + PrepareBeat) and RunFrom skips the earlier beats.
    /// Acts never edit the scene: everything goes through MapRegistry ids and the Core contracts.
    /// </summary>
    public abstract class ActBase : MonoBehaviour
    {
        public delegate IEnumerator BeatRoutine();

        public struct BeatDef
        {
            public string id, label;
            public BeatRoutine run;
        }

        public abstract ActId Act { get; }
        public virtual string Title => Act.ToString();

        readonly List<BeatDef> beats = new List<BeatDef>();
        readonly List<Coroutine> background = new List<Coroutine>();
        readonly List<IDisposable> waits = new List<IDisposable>();
        bool defined;

        public bool IsRunning { get; private set; }
        public string CurrentBeat { get; private set; }

        public IReadOnlyList<BeatDef> Beats { get { EnsureDefined(); return beats; } }

        // ------------------------------------------------------------------ to override

        /// <summary>Call AddBeat(...) once per beat, in order (ids like "2.1", "2.11a").</summary>
        protected abstract void DefineBeats();

        /// <summary>What is true in the world after this act was completed (used when F1 jumps past it).
        /// The defaults (target machine done, its circuits on...) are already applied by WorldStateSetup.</summary>
        public virtual void DeclareConsequences(WorldState w) { }

        /// <summary>Extra state needed to start straight at `beatId` (e.g. 2.8 needs the fuse in the pocket).</summary>
        public virtual void PrepareBeat(string beatId, WorldState w) { }

        /// <summary>Entering the act (after the director's generic setup), before the first beat runs.</summary>
        protected virtual void OnEnter() { }

        /// <summary>Leaving the act (finished, jumped away, restarted). Remove runtime props, release the Zatan.</summary>
        protected virtual void OnExit() { }

        // ------------------------------------------------------------------ lifecycle (driven by ActDirector)

        protected virtual void Awake() => ActDirector.Register(this);
        protected virtual void OnDestroy() => ActDirector.Unregister(this);

        protected void AddBeat(string id, string label, BeatRoutine run) => beats.Add(new BeatDef { id = id, label = label, run = run });

        void EnsureDefined()
        {
            if (defined) return;
            defined = true;
            DefineBeats();
        }

        public int IndexOfBeat(string beatId)
        {
            EnsureDefined();
            if (string.IsNullOrEmpty(beatId)) return 0;
            int i = beats.FindIndex(b => b.id == beatId);
            return i < 0 ? 0 : i;
        }

        internal void Begin(string beatId)
        {
            EnsureDefined();
            IsRunning = true;
            OnEnter();
            StartCoroutine(RunFrom(IndexOfBeat(beatId)));
        }

        internal void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            StopAllCoroutines();
            background.Clear();
            foreach (var w in waits) w.Dispose();
            waits.Clear();
            try { OnExit(); }
            catch (Exception e) { Debug.LogException(e, this); }
        }

        IEnumerator RunFrom(int index)
        {
            for (int i = index; i < beats.Count && IsRunning; i++)
            {
                SetBeat(beats[i].id);
                var routine = beats[i].run();
                if (routine != null) yield return StartCoroutine(routine);
            }
            if (IsRunning) Finish();
        }

        /// <summary>The act is over: the director moves to the next one.</summary>
        protected void Finish()
        {
            if (!IsRunning) return;
            ActDirector.I?.ActFinished(this);
        }

        /// <summary>Mark a beat (also used for beats that happen inside another beat's coroutine).</summary>
        protected void SetBeat(string beatId)
        {
            CurrentBeat = beatId;
            GameContext.Beat = beatId;
            Bus.Publish(new BeatStarted { act = Act, beat = beatId });
        }

        /// <summary>Coroutine that lives as long as the act (always-on beats like 1.6). Stopped on exit.</summary>
        protected Coroutine StartBackground(IEnumerator routine)
        {
            var c = StartCoroutine(routine);
            background.Add(c);
            return c;
        }

        // ------------------------------------------------------------------ services shortcuts

        protected static IPlayer Player => Services.Get<IPlayer>();
        protected static IPocket Pocket => Services.Get<IPocket>();
        protected static IThrowables Bottles => Services.Get<IThrowables>();
        protected static IClipboard Clipboard => Services.Get<IClipboard>();
        protected static IPanelSystem Panels => Services.Get<IPanelSystem>();
        protected static IZatan Zatan => Services.Get<IZatan>();
        protected static IZatanDirector ZDirector => Services.Get<IZatanDirector>();
        protected static ICameraNetwork Cameras => Services.Get<ICameraNetwork>();
        protected static IAnnouncer Announcer => Services.Get<IAnnouncer>();
        protected static ICaptureDirector Captures => Services.Get<ICaptureDirector>();
        protected static IMachine Machine(string id) => Panels.Get(id);
        protected static IDoor Door(string id) => MapRegistry.Resolve<IDoor>(id);
        protected static Vector3 Pos(string mapId) => MapRegistry.Position(mapId);

        protected static void SetCircuit(string circuitId, bool on)
        {
            var c = Circus.LightCircuit.Find(circuitId);
            if (c) c.SwitchOn = on;
        }

        // ------------------------------------------------------------------ waits (yield return these)

        /// <summary>Player entered a map trigger (true at once if already inside).</summary>
        protected CustomYieldInstruction WaitTrigger(string triggerId)
        {
            if (MapTrigger.IsInside(triggerId)) return new WaitUntil(() => true);
            return Track(new BusWait<TriggerEntered>(e => e.triggerId == triggerId));
        }

        /// <summary>A machine's layout completed (layoutId null = the machine's own layout, i.e. id == layout).</summary>
        protected CustomYieldInstruction WaitMachineDone(string machineId, string layoutId = null)
        {
            string layout = layoutId ?? machineId;
            if (layoutId == null && Machine(machineId).IsDone) return new WaitUntil(() => true);
            return Track(new BusWait<MachineDone>(e => e.machineId == machineId && e.layoutId == layout));
        }

        protected CustomYieldInstruction WaitPanelOpened(string machineId)
            => Track(new BusWait<PanelOpened>(e => e.machineId == machineId));

        /// <summary>Item in the pocket (true at once if already there).</summary>
        protected CustomYieldInstruction WaitItem(string itemId)
        {
            if (Pocket.Has(itemId)) return new WaitUntil(() => true);
            return Track(new BusWait<ItemPicked>(e => e.itemId == itemId));
        }

        /// <summary>Any Bus event matching the predicate. Read the event from .Last after it completes.</summary>
        protected BusWait<T> WaitEvent<T>(Func<T, bool> match = null) where T : struct => Track(new BusWait<T>(match));

        /// <summary>Player inside a zone of the map (MapData zones).</summary>
        protected static CustomYieldInstruction WaitZone(params string[] zoneIds)
            => new WaitUntil(() => Array.IndexOf(zoneIds, MapData.ZoneAt(Player.Position)) >= 0);

        /// <summary>Zatan line over the speakers; waits until it ends.</summary>
        protected static CustomYieldInstruction SayZatan(string text)
        {
            bool done = false;
            Announcer.Zatan(text, () => done = true);
            return new WaitUntil(() => done);
        }

        /// <summary>Boss line on the radio (and in the clipboard); waits until it ends.</summary>
        protected static CustomYieldInstruction SayBoss(string text)
        {
            bool done = false;
            Announcer.Boss(text, () => done = true);
            return new WaitUntil(() => done);
        }

        T Track<T>(T w) where T : IDisposable { waits.Add(w); return w; }
    }

    /// <summary>Waits for a Bus event. Unsubscribes when satisfied or disposed (the act disposes it on exit).</summary>
    public class BusWait<T> : CustomYieldInstruction, IDisposable where T : struct
    {
        readonly Func<T, bool> match;
        bool done, disposed;
        public T Last { get; private set; }

        public BusWait(Func<T, bool> match)
        {
            this.match = match;
            Bus.Subscribe<T>(OnEvent);
        }

        void OnEvent(T e)
        {
            if (done || (match != null && !match(e))) return;
            Last = e;
            done = true;
            Dispose();
        }

        public override bool keepWaiting => !done;

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Bus.Unsubscribe<T>(OnEvent);
        }
    }
}
