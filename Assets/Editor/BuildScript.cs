using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Jamlloween.EditorTools
{
    /// <summary>
    /// Builds the game from the scenes enabled in Build Settings (the first one is the one the game opens).
    /// CLI: unity build . --target StandaloneWindows64 --execute-method Jamlloween.EditorTools.BuildScript.BuildWindows
    /// </summary>
    public static class BuildScript
    {
        public const string Output = "Builds/Windows/Jamlloween.exe";

        [MenuItem("Jamlloween/Build Windows")]
        public static void BuildWindows()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildScript] No enabled scene in Build Settings.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Output,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[BuildScript] {report.summary.result} scenes={string.Join(", ", scenes)} size={report.summary.totalSize / (1024 * 1024)}MB errors={report.summary.totalErrors} -> {Output}");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
