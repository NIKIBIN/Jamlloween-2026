using System.Collections.Generic;
using System.Linq;
using Jamlloween.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Etapas do roteiro do jogo (cena Level_Design), em Assets/Data/Progression/Roteiro. Separadas das etapas do time
/// (Assets/Data/Progression, usadas na Zatan_Test), que nao mudam.
/// Menu: selecione o ProgressionDirector da cena e use Jamlloween/Progression/Roteiro: Rebuild Steps On Selected Director.
/// Rebuild reescreve os assets desta pasta mantendo o arquivo (e o GUID) de cada etapa que continua na lista, entao as
/// ligacoes do ProgressionEventRelay a etapas continuam valendo. Etapas que sairam da lista sao apagadas.
/// Edicoes feitas a mao nos assets se perdem: mude aqui.
/// Ids de eventos e de triggers usados pela cena estao nas constantes abaixo.
/// </summary>
public static class RoteiroSetup
{
    public const string Folder = "Assets/Data/Progression/Roteiro";
    const string Roteiro = "Assets/Audio/Roteiro/";

    // Eventos de conclusao enviados pela cena.
    public const string ClipboardTakenEvent = "ClipboardTaken"; // ClipboardMinigame na mesa do trailer
    public const string ZatanEscapedEvent = "ZatanEscaped";     // trigger da volta com a gasolina
    public const string ZatanGaveUpEvent = "ZatanGaveUp";       // ZatanSafeRoom: ele desistiu na porta do trailer
    public const string WheelFixedEvent = "WheelFixed";         // FerrisWheelMotorMinigame do motor da roda-gigante
    public const string GondolaBoardedEvent = "GondolaBoarded"; // GondolaDoor
    public const string WheelStoppedEvent = "WheelStopped";     // FerrisWheelRide: a roda parou no alto
    public const string FinalAttackDoneEvent = "FinalAttackDone"; // ZatanDirector.OnFinaleFinished
    // Eventos nomeados emitidos pelas etapas (a cena reage pelo ProgressionEventRelay).
    public const string PowerLostEvent = "PowerLost";
    /// <summary>O jogador viu que falta o fusível: abre o acesso ao lado leste (labirinto e roda-gigante).</summary>
    public const string EastAccessOpenedEvent = "AcessoLesteAberto";
    // Triggers.
    public const string GeneratorNoiseTrigger = "gen_noise";
    public const string ZatanEscapeTrigger = "zatan_escape";

    const string MissingFuseText = "Falta um fusível aqui.";

    [MenuItem("Jamlloween/Progression/Roteiro: Rebuild Steps On Selected Director")]
    static void RebuildOnSelection()
    {
        var go = Selection.activeGameObject;
        var director = go != null ? go.GetComponent<ProgressionDirector>() : null;
        if (director == null) { Debug.LogError("[RoteiroSetup] selecione o objeto com o ProgressionDirector."); return; }
        Rebuild(director);
    }

    public static void Rebuild(ProgressionDirector director)
    {
        if (director == null) throw new System.ArgumentNullException(nameof(director));
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Data/Progression", "Roteiro");
        written.Clear();

        var steps = new List<ProgressionStep>
        {
            // 1-2: no trailer. Prancheta (minigame da ordem de servico), radio e lanterna, na ordem que o jogador quiser;
            // a fala do patrao sai assim que o radio funciona.
            Step("R00_Trailer", "Trailer: prancheta, rádio e lanterna", new[] { ClipboardTakenEvent, "RadioFixed", "FlashlightTaken" },
                active: new[] { "clipboard", "radio", "flashlight" },
                onStart: new[] { Think("Esse rádio velho... preciso consertar pra falar com o patrão.", 3.5f) },
                onEvent: new[]
                {
                    ("RadioFixed", new[]
                    {
                        // A primeira fala do patrao fica dividida em duas legendas (b90c2d9), cada uma com o seu tempo.
                        Say("Até que enfim. Escuta: o circo tá cobrando, tudo tem que funcionar até amanhã. Ninguém vai te buscar antes de terminar.", 6.5f),
                        Say("Começa pela cabine da cartomante, na entrada do circo. E pega a lanterna, tá um breu aí fora.", 5f)
                    })
                },
                onComplete: new[] { Task("zatan", TaskState.Current) }),
            // 3-4: Zatan na entrada do circo. No caminho, o gerador faz barulho. Depois do conserto, a energia cai.
            Step("R02_Zatan", "Consertar o Zatan", new[] { "ZatanFixed" }, active: new[] { "zatan_repair" },
                onStart: new[] { Trigger(GeneratorNoiseTrigger, true) },
                onComplete: new[]
                {
                    Trigger(GeneratorNoiseTrigger, false), Lights("park", false), Evt(PowerLostEvent),
                    Snd(Roteiro + "SFX_Roteiro_PowerDown.wav")
                }),
            Step("R03_FalaGerador", "Reportar: abastecer o gerador", new[] { ProgressionDirector.RadioIdleEvent },
                onStart: new[] { Say("Caiu tudo? Tem um gerador reserva, mas tá sem combustível. O pessoal da pirotecnia guarda gasolina no fundo da tenda. Pega lá.") },
                onComplete: new[] { Task("generator", TaskState.Current), Task("gas", TaskState.Current) }),
            // 5-6: gasolina no fundo do circo; na volta, Zatan escapa da cabine.
            Step("R04_PegarGasolina", "Pegar a gasolina", new[] { "GasCanTaken" }, active: new[] { "gas_can" },
                onComplete: new[] { Trigger(ZatanEscapeTrigger, true) }),
            Step("R05_ZatanEscapa", "Voltar: Zatan escapa da cabine", new[] { ZatanEscapedEvent },
                onComplete: new[]
                {
                    Evt(ProgressionDirector.ZatanReleasedEvent),
                    Think("Que barulho foi esse?! Veio da cabine do Zatan...")
                }),
            // 7-8: volta para perto da safe zone e liga o gerador.
            Step("R06_Gerador", "Abastecer e ligar o gerador", new[] { "GeneratorStarted" }, active: new[] { "generator" },
                onComplete: new[] { Lights("park", true), Evt(ProgressionDirector.GeneratorRestoredEvent), Task("hide", TaskState.Current) }),
            // 9-10: Zatan aparece longe e persegue; o jogador se esconde no trailer, ele desiste e some.
            Step("R07_FugaTrailer", "Fugir para o trailer", new[] { ZatanGaveUpEvent },
                onStart: new[] { Think("É ele! Pro trailer, rápido!") }),
            Step("R08_FalaCarrossel", "Reportar: ir ao carrossel", new[] { ProgressionDirector.RadioIdleEvent },
                onStart: new[] { Say("Saiu da caixa? Para de besteira, alguém deve ter tirado ele dali. Vai pro carrossel e termina logo.") },
                onComplete: new[] { Task("carousel", TaskState.Current) }),

            // 11: carrossel. O quadro de fusiveis mostra que falta um fusivel.
            Step("R12_IrAoQuadro", "Ir ao quadro do carrossel", new[] { "FuseBoxInspected" },
                intercepted: new[] { ("fusebox", MissingFuseText) },
                onComplete: new[]
                {
                    Say("Falta fusível? Tem uma caixa de peças no meio do labirinto de sebes, do lado leste do parque. Vai lá e volta rápido."),
                    Task("fuse_find", TaskState.Current),
                    // Abre as barreiras do lado leste (labirinto e roda-gigante): o relay troca Barreiras por Barreiras_abertas.
                    Evt(EastAccessOpenedEvent)
                }),
            // 12-13: fusivel numa clareira no meio do labirinto. Pegar o fusivel conclui a etapa; a R14 comeca a perseguicao
            // (relay: EnsureChaseFrom) e abre de vez o portao leste (StepLockedMachine da Porta_2).
            Step("R13_Labirinto", "Achar o fusível no labirinto", new[] { "FuseTaken" }, active: new[] { "fuse_pickup" },
                onComplete: new[] { Task("hide_maze", TaskState.Current) },
                blocked: new[] { ("fusebox", MissingFuseText) }),
            // 14: foge para o trailer pela outra entrada (portao leste). O ZatanSafeRoom faz a desistencia.
            Step("R14_FugaTrailer2", "Fugir para o trailer pelo portão leste", new[] { ZatanGaveUpEvent },
                // PipocaTrocada: o relay troca os carrinhos de pipoca de Barreira_pipoca (1 sai, 2 entra).
                onStart: new[] { Think("Ele tá no labirinto! Sai pelo outro lado, pro trailer, pelo portão leste!"), Evt("PipocaTrocada") },
                onComplete: new[] { Task("fuse_fix", TaskState.Current), Think("Foi embora... Agora o carrossel.") },
                blocked: new[] { ("fusebox", "Primeiro sair daqui vivo.") }),
            // 15: o quadro de fusiveis liga o carrossel (CarouselRepair).
            Step("R15_QuadroDeFusiveis", "Consertar o quadro de fusíveis", new[] { "FuseBoxFixed" }, active: new[] { "fusebox" }),
            // 16: roda-gigante (PLACEHOLDER: painel na base da roda com conserto de segurar E).
            Step("R16_FalaRodaGigante", "Reportar: roda-gigante", new[] { ProgressionDirector.RadioIdleEvent },
                onStart: new[] { Say("Tô ouvindo a musiquinha do carrossel daqui. Falta só a roda-gigante. Conserta o motor dela e pode ir embora.") },
                onComplete: new[] { Task("wheel", TaskState.Current) }),
            Step("R17_ConsertarRodaGigante", "Consertar a roda-gigante", new[] { WheelFixedEvent }, active: new[] { "wheel_panel" }),
            // 18: depois do conserto da roda o jogador tenta ir embora. Perto das Barreiras, Trigger_FinalBarreiras chama
            // ZatanDirector.ChaseFrom(Zatan_ChaseSpawn_Final) e PlayerSeat.Open: só então a gôndola abre e o pensamento
            // "Ele tá vindo... não tem saída!" toca (GondolaDoor.chaseLine). Antes disso não dá para embarcar.
            Step("R18_Encurralado", "Encurralado: entrar na gôndola", new[] { GondolaBoardedEvent }, active: new[] { "gondola_door" },
                onStart: new[] { Trigger("fim_barreiras", true) },
                onComplete: new[] { Trigger("fim_barreiras", false) }),
            // 19: a roda gira e para no alto (FerrisWheelRide) porque a energia cai: apagao no parque inteiro
            // (LightGroup park: luzes, carrossel e musica). O som de tensao comeca.
            Step("R19_RodaGigante", "A roda-gigante gira", new[] { WheelStoppedEvent },
                onComplete: new[] { Lights("park", false), Snd(Roteiro + "SFX_Roteiro_PowerDown.wav") }),
            // 20: olhar para tras: Zatan ataca de fora da gondola (FerrisWheelRide -> ZatanDirector.FinalAttackFrom).
            Step("R20_OlharParaTras", "Zatan ataca na gôndola", new[] { FinalAttackDoneEvent },
                onStart: new[] { Think("Parou... A energia caiu de novo?!") }),
            // 21: creditos (relay: CreditsScreen.Show). Ultima etapa: nunca conclui.
            Step("R21_Creditos", "Créditos", new string[0]),
        };

        foreach (var guid in AssetDatabase.FindAssets("t:ProgressionStep", new[] { Folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!written.Contains(path)) AssetDatabase.DeleteAsset(path);
        }
        AssetDatabase.SaveAssets();
        Apply(director, steps, Tasks());
        Debug.Log($"[RoteiroSetup] {steps.Count} etapas em {Folder}, aplicadas em '{director.name}'.");
    }

    static readonly HashSet<string> written = new HashSet<string>();

    static List<ClipboardTask> Tasks() => new List<ClipboardTask>
    {
        new ClipboardTask { id = "clipboard", text = "Ler a ordem de serviço", initialState = TaskState.Current, doneEvent = ClipboardTakenEvent },
        new ClipboardTask { id = "radio", text = "Consertar o rádio", initialState = TaskState.Current, doneEvent = "RadioFixed" },
        new ClipboardTask { id = "flashlight", text = "Pegar a lanterna", initialState = TaskState.Current, doneEvent = "FlashlightTaken" },
        new ClipboardTask { id = "zatan", text = "Consertar o Zatan na tenda principal", initialState = TaskState.Future, doneEvent = "ZatanFixed" },
        new ClipboardTask { id = "generator", text = "Abastecer e ligar o gerador", initialState = TaskState.Hidden, doneEvent = "GeneratorStarted" },
        new ClipboardTask { id = "gas", text = "Pegar a gasolina no fundo da tenda principal", indent = true, initialState = TaskState.Hidden, doneEvent = "GasCanTaken" },
        new ClipboardTask { id = "hide", text = "Me esconder no trailer", initialState = TaskState.Hidden, doneEvent = ZatanGaveUpEvent },
        new ClipboardTask { id = "carousel", text = "Consertar o carrossel", initialState = TaskState.Future, doneEvent = "FuseBoxFixed" },
        new ClipboardTask { id = "fuse_find", text = "Achar um fusível no labirinto", indent = true, doneEvent = "FuseTaken" },
        new ClipboardTask { id = "hide_maze", text = "Voltar ao trailer", indent = true, doneEvent = ZatanGaveUpEvent },
        new ClipboardTask { id = "fuse_fix", text = "Consertar o quadro de fusíveis", indent = true, doneEvent = "FuseBoxFixed" },
        new ClipboardTask { id = "wheel", text = "Consertar a roda-gigante", initialState = TaskState.Future, doneEvent = WheelFixedEvent },
    };

    static void Apply(ProgressionDirector d, List<ProgressionStep> steps, List<ClipboardTask> tasks)
    {
        var so = new SerializedObject(d);
        var list = so.FindProperty("steps");
        list.arraySize = steps.Count;
        for (int i = 0; i < steps.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = steps[i];

        var tl = so.FindProperty("tasks");
        tl.arraySize = tasks.Count;
        for (int i = 0; i < tasks.Count; i++)
        {
            var e = tl.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("id").stringValue = tasks[i].id;
            e.FindPropertyRelative("text").stringValue = tasks[i].text;
            e.FindPropertyRelative("indent").boolValue = tasks[i].indent;
            e.FindPropertyRelative("initialState").enumValueIndex = (int)tasks[i].initialState;
            e.FindPropertyRelative("doneEvent").stringValue = tasks[i].doneEvent;
        }
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(d.gameObject.scene);
    }

    /// <summary>Writes the step into its asset, reusing the existing file (and GUID) when there is one.</summary>
    static ProgressionStep Step(string file, string label, string[] completion, string[] active = null,
        StepAction[] onStart = null, StepAction[] onComplete = null,
        (string id, string text)[] intercepted = null, (string id, string text)[] blocked = null,
        (string id, StepAction[] actions)[] onEvent = null)
    {
        var s = ScriptableObject.CreateInstance<ProgressionStep>();
        s.label = label;
        s.completionEvents.AddRange(completion);
        if (active != null) s.activeInteractions.AddRange(active);
        if (onStart != null) s.onStart.AddRange(onStart);
        if (onComplete != null) s.onComplete.AddRange(onComplete);
        if (onEvent != null)
            foreach (var (id, actions) in onEvent) s.onEvent.Add(new EventActions { eventId = id, actions = new List<StepAction>(actions) });
        if (intercepted != null)
            foreach (var (id, text) in intercepted) s.interceptedInteractions.Add(new InterceptedInteraction { interactionId = id, text = text });
        if (blocked != null)
            foreach (var (id, text) in blocked) s.blockedTexts.Add(new InterceptedInteraction { interactionId = id, text = text });

        string path = $"{Folder}/{file}.asset";
        written.Add(path);
        var existing = AssetDatabase.LoadAssetAtPath<ProgressionStep>(path);
        if (existing == null)
        {
            s.name = file;
            AssetDatabase.CreateAsset(s, path);
            return s;
        }
        s.name = existing.name;
        EditorUtility.CopySerialized(s, existing);
        Object.DestroyImmediate(s);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    /// <summary>Fala do patrao. seconds = 0: o tempo na tela sai do tamanho do texto.</summary>
    static StepAction Say(string text, float seconds = 0f) => new StepAction { kind = StepAction.Kind.RadioLine, line = new RadioLine { text = text, seconds = seconds } };
    static StepAction Think(string text, float seconds = 0f) => new StepAction { kind = StepAction.Kind.RadioLine, line = new RadioLine { speaker = "", text = text, seconds = seconds } };
    static StepAction Trigger(string id, bool on) => new StepAction { kind = StepAction.Kind.Trigger, targetId = id, state = on };
    static StepAction Lights(string id, bool on) => new StepAction { kind = StepAction.Kind.LightGroup, targetId = id, state = on };
    static StepAction Evt(string name) => new StepAction { kind = StepAction.Kind.Event, targetId = name };
    static StepAction Snd(string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) throw new System.IO.FileNotFoundException("[RoteiroSetup] som nao encontrado", path);
        return new StepAction { kind = StepAction.Kind.Sound, clip = clip };
    }
    static StepAction Task(string id, TaskState state) => new StepAction { kind = StepAction.Kind.Task, targetId = id, taskState = state };
}
