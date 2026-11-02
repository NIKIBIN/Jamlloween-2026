using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Circus.EditorTools
{
    /// <summary>CLI entry point: unity build . --target StandaloneWindows64 --execute-method Circus.EditorTools.BuildScript.BuildWindows</summary>
    public static class BuildScript
    {
        public const string Output = "Builds/Windows/ZatanSpike.exe";

        [MenuItem("Tools/Build Windows")]
        public static void BuildWindows()
        {
            var tunables = AssetDatabase.LoadAssetAtPath<Tunables>("Assets/Data/Resources/Tunables.asset");
            if (tunables && tunables.godMode) Debug.LogWarning("[BuildScript] Tunables.godMode is ON in the asset.");

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { CircusMapBuilder.ScenePath },
                locationPathName = Output,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[BuildScript] {report.summary.result} size={report.summary.totalSize / (1024 * 1024)}MB errors={report.summary.totalErrors} -> {Output}");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
