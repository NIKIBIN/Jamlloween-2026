using UnityEngine;

namespace Circus
{
    public partial class DebugPanel
    {
        static readonly GamePhase[] Phases = { GamePhase.Intro, GamePhase.Observe, GamePhase.Play, GamePhase.Hunt };
        static readonly string[] PhaseLabels = { "INTRO", "OBSERVAR", "BRINCAR", "CAÇAR" };

        partial void DrawNarrativeSection(Tunables t)
        {
            if (!Section("Fases / Eventos (P2)", false)) return;
            t.phaseDirector = GUILayout.Toggle(t.phaseDirector, " Diretor de fases");
            t.scriptedEvents = GUILayout.Toggle(t.scriptedEvents, " Eventos roteirizados (TR1–TR4)");
            t.moveOnlyWhenUnobserved = GUILayout.Toggle(t.moveOnlyWhenUnobserved, " Regra: só move quando não observado");
            GUILayout.Label("Forçar fase:");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Phases.Length; i++)
                if (GUILayout.Button(PhaseLabels[i]) && PhaseDirector.I) PhaseDirector.I.ForcePhase(Phases[i]);
            GUILayout.EndHorizontal();
            GUILayout.Label("Disparar evento:");
            GUILayout.BeginHorizontal();
            var ed = FindAnyObjectByType<EventDirector>();
            foreach (var id in new[] { "TR1", "TR2", "TR3", "TR4" })
                if (GUILayout.Button(id) && ed) ed.Fire(id);
            GUILayout.EndHorizontal();
            t.brincarSpeedMult = Slider("BRINCAR: vel. (mult.)", t.brincarSpeedMult, 0.2f, 1.5f, "0.00");
            t.brincarAttackDistance = Slider("BRINCAR: ataca a (m)", t.brincarAttackDistance, 1f, 15f);
            t.observeAppearInterval = Slider("OBSERVAR: aparece a cada (s)", t.observeAppearInterval, 3f, 40f);
            t.huntDelayAfterCard = Slider("Atraso da CAÇAR (s)", t.huntDelayAfterCard, 0f, 120f);
            t.zatanCarouselMinInterval = Slider("Zatan liga carrossel (mín s)", t.zatanCarouselMinInterval, 5f, 200f);
            t.zatanCarouselMaxInterval = Mathf.Max(t.zatanCarouselMinInterval, Slider("Zatan liga carrossel (máx s)", t.zatanCarouselMaxInterval, 5f, 300f));
        }
    }
}
