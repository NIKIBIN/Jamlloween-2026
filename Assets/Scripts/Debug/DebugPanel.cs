using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Circus
{
    /// <summary>IMGUI debug panel, F1. Edits the Tunables asset live and triggers debug actions.</summary>
    public partial class DebugPanel : MonoBehaviour
    {
        Rect window = new Rect(10, 10, 430, 640);
        Vector2 scroll;
        readonly Dictionary<string, bool> foldouts = new Dictionary<string, bool>();
        GUIStyle header;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) DebugPanelState.IsOpen = !DebugPanelState.IsOpen;
        }

        void OnGUI()
        {
            if (!DebugPanelState.IsOpen) return;
            GUI.depth = -100;
            window.height = Mathf.Min(Screen.height - 20, 760);
            if (Spike2.Core.DebugRegistry.Spike2Mode) window.width = Mathf.Max(window.width, 480);
            window = GUILayout.Window(9001, window, DrawWindow, "DEBUG (F1)");
        }

        void DrawWindow(int wid)
        {
            header ??= new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            scroll = GUILayout.BeginScrollView(scroll);
            var t = Tunables.I;

            if (Section("Geral", true))
            {
                t.godMode = GUILayout.Toggle(t.godMode, " God mode");
                t.showGizmos = GUILayout.Toggle(t.showGizmos, " Mostrar gizmos (cone, barulho, rota)");
                if (!Spike2.Core.DebugRegistry.Spike2Mode)
                    t.mapOnTab = GUILayout.Toggle(t.mapOnTab, " Mapa no Tab (desligado = só no trailer)");
                GUILayout.Label($"Fase: {(GameState.I ? GameState.I.Phase.ToString() : "-")}   t={Time.timeSinceLevelLoad:0}s");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Reiniciar cena")) GameState.I?.Restart();
                if (GUILayout.Button("Sair do jogo")) Application.Quit();
                GUILayout.EndHorizontal();
            }

            if (Spike2.Core.DebugRegistry.Spike2Mode)
            {
                // Spike 2: the systems register their own tabs (Spike2.Core.DebugRegistry); spike 1 sections stay out.
                DrawVisual(t);
                Spike2.Core.DebugRegistryGUI.DrawTabs();
                GUILayout.EndScrollView();
                GUI.DragWindow(new Rect(0, 0, 10000, 20));
                return;
            }

            DrawPhaseSections(t);

            if (Section("Teleporte", false))
            {
                foreach (var z in ZoneMarker.All)
                    if (GUILayout.Button(z.label) && FirstPersonController.I)
                    {
                        if (FirstPersonController.I.CurrentMode == FirstPersonController.Mode.Free)
                            FirstPersonController.I.Teleport(z.transform.position, z.transform.eulerAngles.y);
                    }
            }

            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        partial void DrawPhaseSections(Tunables t);

        bool Section(string name, bool defaultOpen)
        {
            if (!foldouts.TryGetValue(name, out bool open)) open = defaultOpen;
            if (GUILayout.Button((open ? "▼ " : "► ") + name, header)) open = !open;
            foldouts[name] = open;
            return open;
        }

        static float Slider(string label, float v, float min, float max, string fmt = "0.0")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {v.ToString(fmt)}", GUILayout.Width(220));
            v = GUILayout.HorizontalSlider(v, min, max);
            GUILayout.EndHorizontal();
            return v;
        }

        static int SliderInt(string label, int v, int min, int max)
            => Mathf.RoundToInt(Slider(label, v, min, max, "0"));
    }
}
