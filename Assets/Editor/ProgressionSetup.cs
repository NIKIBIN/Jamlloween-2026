using System.Collections.Generic;
using System.IO;
using Jamlloween.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Cria as etapas padrão (Assets/Data/Progression) e liga o diretor na cena aberta.
/// Idempotente: etapas e objetos que já existem não são recriados nem sobrescritos.
/// </summary>
public static class ProgressionSetup
{
    const string Folder = "Assets/Data/Progression";
    const string MissingFuseText = "Falta um fusível aqui.";

    // ------------------------------------------------------------------ etapas

    [MenuItem("Jamlloween/Progression/Create Default Steps")]
    public static void CreateSteps()
    {
        Directory.CreateDirectory(Folder);
        Step("Step_00_Lanterna", "Pegar a lanterna", new[] { "FlashlightTaken" }, active: new[] { "flashlight" },
            onComplete: new[] { Task("radio", TaskState.Current) });
        Step("Step_01_Radio", "Consertar o rádio", new[] { "RadioFixed" }, active: new[] { "radio" },
            onComplete: new[]
            {
                Say("Até que enfim. Escuta: o circo tá cobrando, tudo tem que funcionar até amanhã. Ninguém vai te buscar antes de terminar. Começa pela cabine da cartomante."),
                Task("zatan", TaskState.Current)
            });
        Step("Step_02_Zatan", "Consertar o Zatan", new[] { "ZatanFixed" }, active: new[] { "zatan_repair" },
            onComplete: new[] { Lights("park", false) });
        // Fala que acontece e manda abastecer o gerador.
        Step("Step_03_FalaGerador", "Reportar: abastecer o gerador", new[] { ProgressionDirector.RadioIdleEvent },
            onStart: new[] { Say("Caiu tudo? Tem um gerador reserva, mas tá sem combustível. O pessoal da pirotecnia guarda gasolina no fundo da tenda. Pega lá.") },
            onComplete: new[] { Task("generator", TaskState.Current), Task("gas", TaskState.Current) });
        Step("Step_04_PegarGasolina", "Pegar a gasolina", new[] { "GasCanTaken" }, active: new[] { "gas_can" },
            onComplete: new[] { Evt(ProgressionDirector.ZatanReleasedEvent) });
        Step("Step_05_Gerador", "Abastecer e ligar o gerador", new[] { "GeneratorStarted" }, active: new[] { "generator" },
            onComplete: new[] { Lights("park", true), Evt(ProgressionDirector.GeneratorRestoredEvent) });
        Step("Step_06_FalaCarrossel", "Reportar: ir ao carrossel", new[] { ProgressionDirector.RadioIdleEvent },
            onStart: new[] { Say("Saiu da caixa? Para de besteira, alguém deve ter tirado ele dali. Vai pro carrossel e termina logo.") },
            onComplete: new[] { Task("carousel", TaskState.Current) });
        Step("Step_07_IrAoCarrossel", "Ir ao carrossel", new[] { "FuseBoxInspected" },
            intercepted: new[] { ("fusebox", MissingFuseText) },
            onComplete: new[]
            {
                Say("Falta fusível? O pessoal do circo disse que pode ter um na casa de diversão dos palhaços. Tenta lá primeiro."),
                Task("fuse_find", TaskState.Current)
            });
        Step("Step_08_AcharFusivel", "Achar o fusível", new[] { "FuseTaken" }, active: new[] { "fuse_pickup" },
            onComplete: new[] { Task("fuse_fix", TaskState.Current) });
        Step("Step_09_QuadroDeFusiveis", "Consertar o quadro de fusíveis", new[] { "FuseBoxFixed" }, active: new[] { "fusebox" });
        Step("Step_10_Fim", "Fim provisório", new string[0]);   // acrescente etapas depois desta (ou reordene a lista do diretor)
        AssetDatabase.SaveAssets();
        Debug.Log($"[ProgressionSetup] etapas em {Folder}");
    }

    /// <summary>As linhas da prancheta. As sublinhas são reveladas pelas etapas.</summary>
    static List<ClipboardTask> DefaultTasks() => new List<ClipboardTask>
    {
        new ClipboardTask { id = "flashlight", text = "Pegar a lanterna", initialState = TaskState.Current, doneEvent = "FlashlightTaken" },
        new ClipboardTask { id = "radio", text = "Consertar o rádio", initialState = TaskState.Future, doneEvent = "RadioFixed" },
        new ClipboardTask { id = "zatan", text = "Consertar o Zatan", initialState = TaskState.Future, doneEvent = "ZatanFixed" },
        new ClipboardTask { id = "generator", text = "Abastecer e ligar o gerador", initialState = TaskState.Hidden, doneEvent = "GeneratorStarted" },
        new ClipboardTask { id = "gas", text = "Pegar a gasolina", indent = true, initialState = TaskState.Hidden, doneEvent = "GasCanTaken" },
        new ClipboardTask { id = "carousel", text = "Consertar o carrossel", initialState = TaskState.Future, doneEvent = "FuseBoxFixed" },
        new ClipboardTask { id = "fuse_find", text = "Achar um fusível", indent = true, doneEvent = "FuseTaken" },
        new ClipboardTask { id = "fuse_fix", text = "Consertar o quadro de fusíveis", indent = true, doneEvent = "FuseBoxFixed" },
    };

    /// <summary>Apaga as etapas e recria com os valores padrão (perde edições manuais) e reaplica no diretor da cena.</summary>
    [MenuItem("Jamlloween/Progression/Reset Steps And Tasks (overwrites)")]
    public static void ResetData()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:ProgressionStep", new[] { Folder }))
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
        CreateSteps();
        var director = Object.FindFirstObjectByType<ProgressionDirector>(FindObjectsInactive.Include);
        if (director != null) ApplyToDirector(director);
        Debug.Log("[ProgressionSetup] etapas e tarefas recriadas.");
    }

    static void ApplyToDirector(ProgressionDirector d)
    {
        var steps = new List<ProgressionStep>();
        foreach (var guid in AssetDatabase.FindAssets("t:ProgressionStep", new[] { Folder }))
            steps.Add(AssetDatabase.LoadAssetAtPath<ProgressionStep>(AssetDatabase.GUIDToAssetPath(guid)));
        steps.Sort((a, b) => string.CompareOrdinal(a.name, b.name));   // Step_00..Step_10

        var so = new SerializedObject(d);
        var list = so.FindProperty("steps");
        list.arraySize = steps.Count;
        for (int i = 0; i < steps.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = steps[i];

        var tasks = DefaultTasks();
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

    static void Step(string file, string label, string[] completion, string[] active = null,
        (string id, string text)[] intercepted = null, StepAction[] onStart = null, StepAction[] onComplete = null)
    {
        string path = $"{Folder}/{file}.asset";
        if (AssetDatabase.LoadAssetAtPath<ProgressionStep>(path) != null) return;
        var s = ScriptableObject.CreateInstance<ProgressionStep>();
        s.label = label;
        s.completionEvents.AddRange(completion);
        if (active != null) s.activeInteractions.AddRange(active);
        if (intercepted != null)
            foreach (var (id, text) in intercepted) s.interceptedInteractions.Add(new InterceptedInteraction { interactionId = id, text = text });
        if (onStart != null) s.onStart.AddRange(onStart);
        if (onComplete != null) s.onComplete.AddRange(onComplete);
        AssetDatabase.CreateAsset(s, path);
    }

    static StepAction Say(string text) => new StepAction { kind = StepAction.Kind.RadioLine, line = new RadioLine { text = text } };
    static StepAction Lights(string id, bool on) => new StepAction { kind = StepAction.Kind.LightGroup, targetId = id, state = on };
    static StepAction Evt(string name) => new StepAction { kind = StepAction.Kind.Event, targetId = name };
    static StepAction Task(string id, TaskState state) => new StepAction { kind = StepAction.Kind.Task, targetId = id, taskState = state };

    // ------------------------------------------------------------------ cena

    [MenuItem("Jamlloween/Progression/Setup Open Scene")]
    public static void SetupScene()
    {
        CreateSteps();
        var root = GameObject.Find("Progression") ?? new GameObject("Progression");
        var directorGo = root.transform.Find("ProgressionDirector")?.gameObject;
        if (directorGo == null)
        {
            directorGo = new GameObject("ProgressionDirector");
            directorGo.transform.SetParent(root.transform, false);
            directorGo.AddComponent<RadioDialogue>();
            directorGo.AddComponent<Clipboard>();
            ApplyToDirector(directorGo.AddComponent<ProgressionDirector>());
        }

        // Interações que já existem na cena.
        Gate<FlashlightPickup>("flashlight", "FlashlightTaken");
        Gate<RadioMinigame>("radio", "RadioFixed");
        Gate<ZatanRepairMinigame>("zatan_repair", "ZatanFixed");
        Gate<GasCan>("gas_can", "GasCanTaken");
        Gate<GeneratorFuelMinigame>("generator", "GeneratorStarted");
        Gate<FuseBoxMinigame>("fusebox", "FuseBoxFixed", MissingFuseText);

        // Marcador que precisa de posição na cena (placeholder).
        Place(root, "LightGroup_Park", o => Set(o.AddComponent<LightGroup>(), "groupId", "park"));

        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("[ProgressionSetup] diretor ligado. Atribua as luzes ao LightGroup_Park, depois salve a cena.");
    }

    static void Gate<T>(string id, string completion, string blocked = null) where T : Component
    {
        foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include))
        {
            if (c.GetComponent<StepInteraction>() != null) continue;
            var gate = c.gameObject.AddComponent<StepInteraction>();
            Set(gate, "interactionId", id);
            Set(gate, "completionEvent", completion);
            if (blocked != null) Set(gate, "blockedText", blocked);
        }
    }

    static void Place(GameObject root, string name, System.Action<GameObject> build)
    {
        if (root.transform.Find(name) != null) return;
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        build(go);
    }

    static void Set(Object target, string field, string value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).stringValue = value;
        so.ApplyModifiedProperties();
    }
}
