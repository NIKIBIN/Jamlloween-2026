using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Spike2.Core
{
    /// <summary>
    /// Draws the DebugRegistry tabs and the automatic Tunables widgets inside the F1 window (called by the spike 1
    /// DebugPanel when DebugRegistry.Spike2Mode is on). Also draws the HUD overlays.
    /// </summary>
    public static class DebugRegistryGUI
    {
        static string selected;
        static readonly Dictionary<Type, FieldInfo[]> fieldCache = new Dictionary<Type, FieldInfo[]>();
        static readonly List<string> tabBuffer = new List<string>();

        /// <summary>Tabs from DebugRegistry plus tabs of the loaded Tunables, in registration order.</summary>
        public static List<string> AllTabs()
        {
            tabBuffer.Clear();
            foreach (var t in DebugRegistry.Tabs) if (!tabBuffer.Contains(t)) tabBuffer.Add(t);
            foreach (var t in TunablesHub.All) if (!tabBuffer.Contains(t.Tab)) tabBuffer.Add(t.Tab);
            return tabBuffer;
        }

        /// <summary>Tab picker + content. Call inside GUILayout (the F1 window's scroll view).</summary>
        public static void DrawTabs(int columns = 4)
        {
            var tabs = AllTabs();
            if (tabs.Count == 0) { GUILayout.Label("(nenhuma aba registrada)"); return; }
            int idx = Mathf.Max(0, tabs.IndexOf(selected));
            idx = GUILayout.SelectionGrid(idx, tabs.ToArray(), columns);
            selected = tabs[Mathf.Clamp(idx, 0, tabs.Count - 1)];
            GUILayout.Space(4);
            DrawTab(selected);
        }

        public static void DrawTab(string tab)
        {
            foreach (var e in DebugRegistry.In(tab))
            {
                try { DrawEntry(e); }
                catch (Exception ex) { GUILayout.Label($"<color=red>erro: {ex.Message}</color>"); }
            }
            foreach (var t in TunablesHub.All)
                if (t.Tab == tab) DrawTunables(t);
        }

        static void DrawEntry(DebugRegistry.Entry e)
        {
            switch (e.kind)
            {
                case DebugRegistry.EntryKind.Toggle:
                    bool v = e.get();
                    bool nv = GUILayout.Toggle(v, " " + e.label);
                    if (nv != v) e.set(nv);
                    break;
                case DebugRegistry.EntryKind.Action:
                    if (GUILayout.Button(e.label)) e.run();
                    break;
                case DebugRegistry.EntryKind.Text:
                    GUILayout.Label(e.text());
                    break;
                case DebugRegistry.EntryKind.Custom:
                    e.draw();
                    break;
                case DebugRegistry.EntryKind.Overlay:
                    e.visible = GUILayout.Toggle(e.visible, " [HUD] " + e.label);
                    break;
                case DebugRegistry.EntryKind.Gizmo:
                    e.visible = GUILayout.Toggle(e.visible, " [gizmo] " + e.label);
                    break;
            }
        }

        // ------------------------------------------------------------------ tunables by reflection

        static FieldInfo[] Fields(Type t)
        {
            if (!fieldCache.TryGetValue(t, out var f))
                fieldCache[t] = f = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
            return f;
        }

        public static void DrawTunables(TunablesBase t)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>{t.GetType().Name}</b>");
            if (GUILayout.Button("Padrões", GUILayout.Width(70))) TunablesHub.ResetToDefaults(t);
            GUILayout.EndHorizontal();
            foreach (var f in Fields(t.GetType()))
            {
                var header = f.GetCustomAttribute<HeaderAttribute>();
                if (header != null) GUILayout.Label($"<i>{header.header}</i>");
                var range = f.GetCustomAttribute<RangeAttribute>();
                if (f.FieldType == typeof(bool))
                {
                    bool b = (bool)f.GetValue(t);
                    bool nb = GUILayout.Toggle(b, " " + Nice(f.Name));
                    if (nb != b) f.SetValue(t, nb);
                }
                else if (range != null && f.FieldType == typeof(float))
                    f.SetValue(t, Slider(Nice(f.Name), (float)f.GetValue(t), range.min, range.max, "0.00"));
                else if (range != null && f.FieldType == typeof(int))
                    f.SetValue(t, Mathf.RoundToInt(Slider(Nice(f.Name), (int)f.GetValue(t), range.min, range.max, "0")));
            }
        }

        public static float Slider(string label, float v, float min, float max, string fmt = "0.0")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {v.ToString(fmt)}", GUILayout.Width(230));
            v = GUILayout.HorizontalSlider(v, min, max);
            GUILayout.EndHorizontal();
            return v;
        }

        static string Nice(string field)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < field.Length; i++)
            {
                char c = field[i];
                if (i > 0 && char.IsUpper(c)) sb.Append(' ');
                sb.Append(i == 0 ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ HUD overlays

        /// <summary>Stacked top-right HUD overlays. Called from OnGUI every frame (F1 open or not).</summary>
        public static void DrawOverlays()
        {
            float w = 320f, x = Screen.width - w - 10f, y = 10f;
            foreach (var e in DebugRegistry.Overlays())
            {
                var r = new Rect(x, y, w, e.hudHeight);
                var old = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.55f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = old;
                try { e.drawHud(new Rect(r.x + 4, r.y + 2, r.width - 8, r.height - 4)); }
                catch (Exception ex) { Debug.LogException(ex); e.visible = false; }
                y += e.hudHeight + 4f;
            }
        }
    }

    /// <summary>Draws the HUD overlays. Created by SystemsBootstrap.</summary>
    public class DebugOverlayHud : MonoBehaviour
    {
        void OnGUI()
        {
            GUI.depth = -90;
            DebugRegistryGUI.DrawOverlays();
        }
    }
}
