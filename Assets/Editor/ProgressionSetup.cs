using System.Collections.Generic;
using System.IO;
using Jamlloween.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Cria as 12 etapas padrão (Assets/Data/Progression) e liga o diretor na cena aberta.
/// Idempotente: etapas e objetos que já existem não são recriados nem sobrescritos.
/// </summary>
public static class ProgressionSetup
{
    const string Folder = "Assets/Data/Progression";
    const string HeadText = "Preciso achar a cabeça desse cavalo primeiro.";

    // ------------------------------------------------------------------ etapas

    [MenuItem("Jamlloween/Progression/Create Default Steps")]
    public static void CreateSteps()
    {
        Directory.CreateDirectory(Folder);
        Step("Step_00_Lanterna", "Pegar a lanterna", "FlashlightTaken", active: new[] { "flashlight" });
        Step("Step_01_Radio", "Consertar o rádio", "RadioFixed", active: new[] { "radio" },
            onComplete: new[] { Say("Até que enfim. Escuta: o circo tá cobrando, tudo tem que funcionar até amanhã. Ninguém vai te buscar antes de terminar. Começa pela cabine da cartomante.") });
        Step("Step_02_Zatan", "Consertar o Zatan", "ZatanFixed", active: new[] { "zatan_repair" },
            onComplete: new[] { Lights("park", false) });
        Step("Step_03_ReportarQueda", "Reportar a queda de luz", ProgressionDirector.RadioIdleEvent,
            onStart: new[] { Say("Caiu tudo? Tem um gerador reserva atrás do depósito. Liga ele.") });
        Step("Step_04_IrAoGerador", "Ir até o gerador", "GeneratorReached",
            onStart: new[] { Trig("glass_break", true), Trig("generator_zone", true) },
            onComplete: new[] { Trig("glass_break", false), Trig("generator_zone", false) });
        Step("Step_05_LigarGerador", "Ligar o gerador", "GeneratorStarted", active: new[] { "generator" },
            onComplete: new[] { Lights("park", true), Evt(ProgressionDirector.GeneratorRestoredEvent) });
        Step("Step_06_Reportar", "Reportar", ProgressionDirector.RadioIdleEvent,
            onStart: new[] { Say("Virou a cabeça? Para de besteira, a máquina faz isso. Vai pro carrossel e termina logo.") });
        Step("Step_07_IrAoCarrossel", "Ir ao carrossel", "CarouselInspected",
            intercepted: new[] { ("carousel_mechanism", HeadText), ("carousel_horse", HeadText) },
            onComplete: new[] { Say("Tá faltando a cabeça? O pessoal do circo disse que pode estar na casa de diversão dos palhaços. Tenta lá primeiro.") });
        Step("Step_08_AcharCabeca", "Achar a cabeça do cavalo", "HeadPicked", active: new[] { "head_pickup" });
        Step("Step_09_EncaixarCabeca", "Encaixar a cabeça no cavalo", "HeadMounted", active: new[] { "carousel_horse" });
        Step("Step_10_Mecanismo", "Consertar o mecanismo do carrossel", "MechanismFixed", active: new[] { "carousel_mechanism" });
        Step("Step_11_Fim", "", "");   // fim provisório: acrescente etapas depois desta (ou antes, reordenando a lista do diretor)
        AssetDatabase.SaveAssets();
        Debug.Log($"[ProgressionSetup] etapas em {Folder}");
    }

    static void Step(string file, string task, string completion, string[] active = null,
        (string id, string text)[] intercepted = null, StepAction[] onStart = null, StepAction[] onComplete = null)
    {
        string path = $"{Folder}/{file}.asset";
        if (AssetDatabase.LoadAssetAtPath<ProgressionStep>(path) != null) return;
        var s = ScriptableObject.CreateInstance<ProgressionStep>();
        s.taskText = task;
        s.completionEvent = completion;
        if (active != null) s.activeInteractions.AddRange(active);
        if (intercepted != null)
            foreach (var (id, text) in intercepted) s.interceptedInteractions.Add(new InterceptedInteraction { interactionId = id, text = text });
        if (onStart != null) s.onStart.AddRange(onStart);
        if (onComplete != null) s.onComplete.AddRange(onComplete);
        AssetDatabase.CreateAsset(s, path);
    }

    static StepAction Say(string text) => new StepAction { kind = StepAction.Kind.RadioLine, line = new RadioLine { text = text } };
    static StepAction Lights(string id, bool on) => new StepAction { kind = StepAction.Kind.LightGroup, targetId = id, state = on };
    static StepAction Trig(string id, bool on) => new StepAction { kind = StepAction.Kind.Trigger, targetId = id, state = on };
    static StepAction Evt(string name) => new StepAction { kind = StepAction.Kind.Event, targetId = name };

    // ------------------------------------------------------------------ cena

    [MenuItem("Jamlloween/Progression/Setup Open Scene")]
    public static void SetupScene()
    {
        CreateSteps();
        var steps = new List<ProgressionStep>();
        foreach (var guid in AssetDatabase.FindAssets("t:ProgressionStep", new[] { Folder }))
            steps.Add(AssetDatabase.LoadAssetAtPath<ProgressionStep>(AssetDatabase.GUIDToAssetPath(guid)));
        steps.Sort((a, b) => string.CompareOrdinal(a.name, b.name));   // Step_00..Step_11

        var root = GameObject.Find("Progression") ?? new GameObject("Progression");
        var directorGo = root.transform.Find("ProgressionDirector")?.gameObject;
        if (directorGo == null)
        {
            directorGo = new GameObject("ProgressionDirector");
            directorGo.transform.SetParent(root.transform, false);
            directorGo.AddComponent<RadioDialogue>();
            directorGo.AddComponent<Clipboard>();
            var d = directorGo.AddComponent<ProgressionDirector>();
            var so = new SerializedObject(d);
            var list = so.FindProperty("steps");
            list.arraySize = steps.Count;
            for (int i = 0; i < steps.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = steps[i];
            so.ApplyModifiedProperties();
        }

        // Interações que já existem na cena.
        Gate<FlashlightPickup>("flashlight", "FlashlightTaken");
        Gate<RadioMinigame>("radio", "RadioFixed");
        Gate<ZatanRepairMinigame>("zatan_repair", "ZatanFixed");
        Gate<CarouselHeadMinigame>("carousel_horse", "HeadMounted", HeadText);
        Gate<CarouselMechanismMinigame>("carousel_mechanism", "MechanismFixed");
        Gate<FuseBoxMinigame>("generator", "GeneratorStarted");   // painel do gerador (etapa 5)

        // Marcadores que precisam de posição na cena (placeholders).
        Place(root, "LightGroup_Park", o => Set(o.AddComponent<LightGroup>(), "groupId", "park"));
        Place(root, "Trigger_GlassBreak", o => { o.AddComponent<BoxCollider>().isTrigger = true; Set(o.AddComponent<StepTrigger>(), "triggerId", "glass_break"); });
        Place(root, "Trigger_GeneratorZone", o =>
        {
            o.AddComponent<BoxCollider>().isTrigger = true;
            var t = o.AddComponent<StepTrigger>();
            Set(t, "triggerId", "generator_zone");
            Set(t, "completionEvent", "GeneratorReached");
        });

        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("[ProgressionSetup] diretor ligado. Posicione os triggers, atribua luzes ao LightGroup_Park e o clipe de vidro ao Trigger_GlassBreak, depois salve a cena.");
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
