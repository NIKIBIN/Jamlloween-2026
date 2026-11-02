using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Spike2.Core.EditorTools
{
    /// <summary>One-time project setup for spike 2 (wave 0): layers and the Spike2_Circus scene skeleton.</summary>
    public static class Spike2Setup
    {
        public const string ScenePath = "Assets/Scenes/Spike2_Circus.unity";
        public const string MapJsonPath = "Assets/Spike2/Design/circus_map_v2.json";
        const string DebugLinesMat = "Assets/Data/Materials/debug_lines.mat";

        /// <summary>Fixed layer indices. Layers.cs looks them up by name.</summary>
        static readonly (int index, string name)[] RequiredLayers =
        {
            (8, Layers.LOSBlockerName), (9, Layers.LowPropName), (10, Layers.PerimeterName),
            (11, Layers.PlayerName), (12, Layers.ZatanName), (13, Layers.InteractableName),
        };

        [MenuItem("Tools/Spike2/Setup Layers")]
        public static void SetupLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            foreach (var (index, name) in RequiredLayers)
            {
                var p = layers.GetArrayElementAtIndex(index);
                if (p.stringValue == name) continue;
                if (!string.IsNullOrEmpty(p.stringValue)) Debug.LogWarning($"[Spike2Setup] layer {index} was '{p.stringValue}', now '{name}'");
                p.stringValue = name;
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[Spike2Setup] layers: " + string.Join(", ", RequiredLayers.Select(l => $"{l.index}={l.name}")));
        }

        /// <summary>Creates Spike2_Circus with SystemsBootstrap and an empty Map (MapRoot). Does not touch an existing Map.</summary>
        [MenuItem("Tools/Spike2/Create Scene Skeleton")]
        public static void CreateSceneSkeleton()
        {
            var scene = System.IO.File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var roots = scene.GetRootGameObjects();
            var sys = roots.FirstOrDefault(r => r.GetComponent<SystemsBootstrap>() != null);
            if (sys == null) sys = new GameObject("SystemsBootstrap", typeof(SystemsBootstrap));
            sys.GetComponent<SystemsBootstrap>().debugLineMaterial = AssetDatabase.LoadAssetAtPath<Material>(DebugLinesMat);

            var map = roots.FirstOrDefault(r => r.name == "Map");
            if (map == null) map = new GameObject("Map");
            var root = map.GetComponent<MapRoot>() ?? map.AddComponent<MapRoot>();
            root.source = AssetDatabase.LoadAssetAtPath<TextAsset>(MapJsonPath);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath, first: false);
            Debug.Log($"[Spike2Setup] scene skeleton at {ScenePath}");
        }

        public static void AddSceneToBuild(string path, bool first)
        {
            var list = EditorBuildSettings.scenes.ToList();
            if (list.Any(s => s.path == path)) return;
            var entry = new EditorBuildSettingsScene(path, true);
            if (first) list.Insert(0, entry); else list.Add(entry);
            EditorBuildSettings.scenes = list.ToArray();
        }

        /// <summary>Batch entry point: -executeMethod Spike2.Core.EditorTools.Spike2Setup.RunAll</summary>
        public static void RunAll()
        {
            SetupLayers();
            CreateSceneSkeleton();
        }
    }
}
