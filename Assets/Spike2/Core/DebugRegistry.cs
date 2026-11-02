using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spike2.Core
{
    /// <summary>
    /// F1 API (SPIKE2.md 7). Each system registers its own items in its own tab; nobody edits DebugPanel.cs.
    /// Sliders come for free from the [Range] fields of each Tunables (TunablesHub).
    /// Pass `owner` (usually `this`) so entries disappear when the owner is destroyed.
    /// </summary>
    public static class DebugRegistry
    {
        public enum EntryKind { Toggle, Action, Text, Custom, Overlay, Gizmo }

        public class Entry
        {
            public EntryKind kind;
            public string tab, label;
            public Func<bool> get;
            public Action<bool> set;
            public Action run;
            public Func<string> text;
            public Action draw;
            public Action<Rect> drawHud;
            public float hudHeight;
            public Action<DebugGL> drawGizmo;
            /// <summary>Overlay/gizmo visible (user toggles it in the tab).</summary>
            public bool visible;
            internal UnityEngine.Object owner;
            internal bool hasOwner;
            public bool Dead => hasOwner && owner == null;
        }

        static readonly List<Entry> entries = new List<Entry>();
        static readonly List<string> tabs = new List<string>();

        /// <summary>Set by SystemsBootstrap: the F1 panel draws the spike 2 tabs instead of the spike 1 sections.</summary>
        public static bool Spike2Mode;

        public static IReadOnlyList<string> Tabs
        {
            get
            {
                Prune();
                return tabs;
            }
        }

        public static IEnumerable<Entry> In(string tab)
        {
            foreach (var e in entries) if (!e.Dead && e.tab == tab) yield return e;
        }

        public static IEnumerable<Entry> Overlays()
        {
            foreach (var e in entries) if (!e.Dead && e.kind == EntryKind.Overlay && e.visible) yield return e;
        }

        public static IEnumerable<Entry> Gizmos()
        {
            foreach (var e in entries) if (!e.Dead && e.kind == EntryKind.Gizmo && e.visible) yield return e;
        }

        /// <summary>Checkbox. Use it for each mechanic's `enabled` flag.</summary>
        public static Entry AddToggle(string tab, string label, Func<bool> get, Action<bool> set, UnityEngine.Object owner = null)
            => Add(new Entry { kind = EntryKind.Toggle, tab = tab, label = label, get = get, set = set }, owner);

        /// <summary>Button.</summary>
        public static Entry AddAction(string tab, string label, Action run, UnityEngine.Object owner = null)
            => Add(new Entry { kind = EntryKind.Action, tab = tab, label = label, run = run }, owner);

        /// <summary>Live text line in the tab (state, values).</summary>
        public static Entry AddText(string tab, Func<string> text, UnityEngine.Object owner = null)
            => Add(new Entry { kind = EntryKind.Text, tab = tab, label = "", text = text }, owner);

        /// <summary>Free GUILayout drawing inside the tab (button rows, pickers).</summary>
        public static Entry AddCustom(string tab, Action drawGuiLayout, UnityEngine.Object owner = null)
            => Add(new Entry { kind = EntryKind.Custom, tab = tab, label = "", draw = drawGuiLayout }, owner);

        /// <summary>HUD overlay drawn on screen (even with F1 closed) while its checkbox is on. drawHud gets its rect.</summary>
        public static Entry AddOverlay(string tab, string label, Action<Rect> drawHud, float height = 24f, bool visibleByDefault = false, UnityEngine.Object owner = null)
            => Add(new Entry { kind = EntryKind.Overlay, tab = tab, label = label, drawHud = drawHud, hudHeight = height, visible = visibleByDefault }, owner);

        /// <summary>Text overlay shortcut.</summary>
        public static Entry AddOverlay(string tab, string label, Func<string> text, bool visibleByDefault = false, UnityEngine.Object owner = null)
            => AddOverlay(tab, label, r => GUI.Label(r, text()), 22f, visibleByDefault, owner);

        /// <summary>World-space lines (vision cone, noise radius...) drawn with GL while its checkbox is on.</summary>
        public static Entry AddGizmo(string tab, string label, Action<DebugGL> draw, bool visibleByDefault = false, UnityEngine.Object owner = null)
            => Add(new Entry { kind = EntryKind.Gizmo, tab = tab, label = label, drawGizmo = draw, visible = visibleByDefault }, owner);

        public static void Remove(Entry e) { entries.Remove(e); Prune(); }

        public static void RemoveOwner(UnityEngine.Object owner)
        {
            entries.RemoveAll(e => e.hasOwner && (e.owner == owner || e.owner == null));
            Prune();
        }

        static Entry Add(Entry e, UnityEngine.Object owner)
        {
            if (owner != null) { e.owner = owner; e.hasOwner = true; }
            entries.Add(e);
            if (!tabs.Contains(e.tab)) tabs.Add(e.tab);
            return e;
        }

        static void Prune()
        {
            entries.RemoveAll(e => e.Dead);
            tabs.RemoveAll(t => !entries.Exists(e => e.tab == t));
        }

        public static void Clear() { entries.Clear(); tabs.Clear(); Spike2Mode = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Clear();
    }
}
