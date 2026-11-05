using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds do jogo para o itch.io: Windows (zip com o .exe) e Web (HTML5). Saem em Builds/ (fora do git).
/// O resultado de cada build fica em Builds/&lt;alvo&gt;_build_result.txt e no Console como [BUILD].
/// </summary>
public static class JamBuild
{
    const string Scene = "Assets/Scenes/Level_Design.unity";
    const string WindowsPath = "Builds/Windows/Jamlloween 2026.exe";
    const string WebPath = "Builds/WebGL";

    [MenuItem("Jamlloween/Build/Windows")]
    public static void BuildWindows() => Schedule(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, WindowsPath, "windows");

    [MenuItem("Jamlloween/Build/Web (HTML5)")]
    public static void BuildWeb()
    {
        // O itch.io não manda o cabeçalho Content-Encoding dos arquivos .br/.gz: sem o fallback o jogo não abre.
        PlayerSettings.WebGL.decompressionFallback = true;
        // O plugin TextMate do pacote com.unity.dt.app-ui (dependência do AI Assistant) usa embind:
        // sem -lembind o link do WebGL falha com "undefined symbol: _embind_register_class".
        // Mesmo com -lembind sobram símbolos _emval_* sem definição (ordem do link). O jogo nunca chama esse
        // código (é do editor do AI Assistant), então o link pode aceitar símbolos indefinidos.
        foreach (var arg in new[] { "-lembind", "-sERROR_ON_UNDEFINED_SYMBOLS=0" })
            if (!PlayerSettings.WebGL.emscriptenArgs.Contains(arg))
                PlayerSettings.WebGL.emscriptenArgs = (PlayerSettings.WebGL.emscriptenArgs + " " + arg).Trim();
        Schedule(BuildTarget.WebGL, BuildTargetGroup.WebGL, WebPath, "webgl");
    }

    static void Schedule(BuildTarget target, BuildTargetGroup group, string path, string tag)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Pare o Play Mode antes do build.");
        Directory.CreateDirectory("Builds");
        string resultFile = $"Builds/{tag}_build_result.txt";
        if (File.Exists(resultFile)) File.Delete(resultFile);
        WhenIdle(() => Run(target, group, path, resultFile));
    }

    /// <summary>Roda quando o editor não está compilando nem importando (senão o build falha "scripts are compiling").</summary>
    static void WhenIdle(Action action)
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) WhenIdle(action);
            else action();
        };
    }

    static void Run(BuildTarget target, BuildTargetGroup group, string path, string resultFile)
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.isDirty) EditorSceneManager.SaveScene(scene);
        if (EditorUserBuildSettings.activeBuildTarget != target)
            EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { Scene },
            locationPathName = path,
            target = target,
            options = BuildOptions.None
        });
        var s = report.summary;
        string line = $"{s.result} errors={s.totalErrors} warnings={s.totalWarnings} size={s.totalSize / (1024 * 1024)}MB time={s.totalTime}";
        File.WriteAllText(resultFile, line);
        if (s.result == BuildResult.Succeeded) Debug.Log($"[BUILD] {target}: {line}");
        else Debug.LogError($"[BUILD] {target}: {line}");
    }
}
