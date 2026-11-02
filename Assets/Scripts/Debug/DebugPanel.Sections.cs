using System;
using System.Linq;
using UnityEngine;

namespace Circus
{
    public partial class DebugPanel
    {
        static readonly ZatanState[] ForceableStates = { ZatanState.Dormant, ZatanState.Patrol, ZatanState.Investigate, ZatanState.Chase };

        partial void DrawPhaseSections(Tunables t)
        {
            DrawZatan(t);
            DrawRepair(t);
            DrawExtraSections(t);
        }

        partial void DrawExtraSections(Tunables t);

        void DrawZatan(Tunables t)
        {
            if (!Section("Zatan", true)) return;
            var b = ZatanBrain.I;
            var perc = ZatanPerception.I;
            if (b)
                GUILayout.Label($"Estado: {b.State}{(b.IsFrozenByGaze ? " (CONGELADO)" : "")}  vê player: {(perc && perc.CanSeePlayer ? "SIM" : "não")}  alcance: {(perc ? perc.CurrentRange : 0):0}m");
            GUILayout.BeginHorizontal();
            foreach (var s in ForceableStates)
                if (GUILayout.Button(s.ToString()) && b) b.ForceState(s);
            GUILayout.EndHorizontal();
            t.zatanPatrolSpeed = Slider("Vel. patrulha", t.zatanPatrolSpeed, 0.5f, 6f);
            t.zatanInvestigateSpeed = Slider("Vel. investigar", t.zatanInvestigateSpeed, 0.5f, 7f);
            t.zatanChaseSpeed = Slider("Vel. caça", t.zatanChaseSpeed, 1f, 9f);
            t.visionRange = Slider("Visão (normal)", t.visionRange, 2f, 40f);
            t.visionRangeFlashlight = Slider("Visão (lanterna)", t.visionRangeFlashlight, 2f, 50f);
            t.visionRangeDark = Slider("Visão (escuro)", t.visionRangeDark, 1f, 30f);
            t.visionAngle = Slider("Ângulo da visão", t.visionAngle, 20f, 180f, "0");
            t.hearingMult = Slider("Audição (mult.)", t.hearingMult, 0f, 3f, "0.00");
            t.losePlayerTime = Slider("Tempo p/ perder player", t.losePlayerTime, 0.5f, 15f);
            t.investigateLookTime = Slider("Tempo investigando", t.investigateLookTime, 0.5f, 15f);
            t.catchDistance = Slider("Distância de captura", t.catchDistance, 0.5f, 3f, "0.00");
            t.runNoiseRadius = Slider("Barulho de correr (m)", t.runNoiseRadius, 0f, 30f);
            t.walkNoiseRadius = Slider("Barulho de andar (m)", t.walkNoiseRadius, 0f, 10f);
        }

        void DrawRepair(Tunables t)
        {
            if (!Section("Conserto", false)) return;
            GUILayout.Label("Variante:");
            t.repairVariant = (RepairVariantOverride)GUILayout.Toolbar((int)t.repairVariant, new[] { "Por máquina", "Hold", "Sequence" });
            t.repairDurationMult = Slider("Duração (mult.)", t.repairDurationMult, 0.1f, 3f, "0.00");
            t.repairNoiseMult = Slider("Barulho do conserto (mult.)", t.repairNoiseMult, 0f, 4f, "0.00");
            t.sequenceKeysPerStep = SliderInt("Teclas por etapa", t.sequenceKeysPerStep, 1, 8);
            foreach (var m in RepairableMachine.All.OrderBy(x => x.order).ThenBy(x => x.id))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{m.id} {(m.IsRepaired ? "OK" : $"{m.Progress01 * 100:0}%")} ({m.EffectiveVariant})", GUILayout.Width(250));
                if (GUILayout.Button("Completar")) m.Complete();
                if (GUILayout.Button("Reset")) m.ResetRepair();
                GUILayout.EndHorizontal();
            }
        }
    }
}
