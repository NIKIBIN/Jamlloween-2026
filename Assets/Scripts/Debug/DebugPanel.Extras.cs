using UnityEngine;

namespace Circus
{
    public partial class DebugPanel
    {
        partial void DrawExtraSections(Tunables t)
        {
            DrawMechanics(t);
            DrawNarrative(t);
            DrawVisual(t);
        }

        partial void DrawNarrativeSection(Tunables t);
        void DrawNarrative(Tunables t) => DrawNarrativeSection(t);

        void DrawMechanics(Tunables t)
        {
            if (!Section("Mecânicas P1", false)) return;
            t.camerasTrack = GUILayout.Toggle(t.camerasTrack, " Câmeras rastreiam e denunciam");
            t.carouselDistracts = GUILayout.Toggle(t.carouselDistracts, " Carrossel distrai (barulho)");
            t.zatanUsesCarousel = GUILayout.Toggle(t.zatanUsesCarousel, " Zatan liga o carrossel (CAÇAR)");
            t.lightsAffectVision = GUILayout.Toggle(t.lightsAffectVision, " Luzes afetam a visão do Zatan");
            t.lockableDoors = GUILayout.Toggle(t.lockableDoors, " Portas trancáveis");
            t.powerGrid = GUILayout.Toggle(t.powerGrid, " Energia com orçamento");
            t.vhsInterference = GUILayout.Toggle(t.vhsInterference, " Interferência VHS (proximidade/câmeras)");
            t.cameraRange = Slider("Alcance da câmera", t.cameraRange, 5f, 40f);
            t.cameraReportTime = Slider("Tempo p/ denunciar", t.cameraReportTime, 0.5f, 6f);
            t.carouselNoiseRadius = Slider("Barulho do carrossel", t.carouselNoiseRadius, 5f, 60f);
            t.doorForceTime = Slider("Tempo p/ arrombar", t.doorForceTime, 1f, 15f);
            t.powerBudget = Slider("Orçamento de energia", t.powerBudget, 5f, 40f);
            t.outageDuration = Slider("Duração da queda", t.outageDuration, 2f, 30f);

            var pg = PowerGrid.I;
            if (pg) GUILayout.Label($"Energia: {(pg.GeneratorOn ? "gerador ON" : "gerador OFF")}  carga {pg.Load:0.0}/{t.powerBudget:0}  {(pg.InOutage ? $"QUEDA {pg.OutageRemaining:0}s" : "")}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ligar gerador") && pg) pg.ForceGenerator(true);
            if (GUILayout.Button("Forçar queda") && pg) pg.TripOutage();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ativar câmeras")) foreach (var c in SecurityCamera.All) c.SetActivated(true);
            var car = CarouselController.I;
            if (car && GUILayout.Button(car.IsOn ? "Desligar carrossel" : "Ligar carrossel")) car.SetOn(!car.IsOn, false);
            if (car && GUILayout.Button("Zatan liga carrossel")) car.SetOn(true, true);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Trancar todas")) foreach (var d in Door.All) if (d.CanBeLocked) d.SetLocked(true);
            if (GUILayout.Button("Destrancar/abrir todas")) foreach (var d in Door.All) { d.SetLocked(false); d.SetOpen(true, false); }
            GUILayout.EndHorizontal();
        }

        void DrawVisual(Tunables t)
        {
            if (!Section("Visual / VHS", false)) return;
            t.vhsFilter = GUILayout.Toggle(t.vhsFilter, " Filtro VHS");
            t.internalHeight = SliderInt("Resolução interna (linhas)", t.internalHeight, 120, 1080);
            t.vhsBaseIntensity = Slider("Intensidade base VHS", t.vhsBaseIntensity, 0f, 1f, "0.00");
            t.fogDensity = Slider("Neblina", t.fogDensity, 0f, 0.2f, "0.000");
            GUILayout.Label($"Interferência atual: {VHSGlobals.Total:0.00}");
        }
    }
}
