using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Circus.EditorTools
{
    /// <summary>Adds the VHSRenderFeature to every URP renderer in Assets/Settings (idempotent).</summary>
    public static class VHSSetup
    {
        [MenuItem("Tools/Setup VHS Renderer Feature")]
        public static void Run()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/VHS.shader");
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets/Settings" }))
            {
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(guid));
                var existing = data.rendererFeatures.OfType<VHSRenderFeature>().FirstOrDefault();
                if (existing == null)
                {
                    existing = ScriptableObject.CreateInstance<VHSRenderFeature>();
                    existing.name = "VHS";
                    AssetDatabase.AddObjectToAsset(existing, data);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(existing, out _, out long localId);
                    var so = new SerializedObject(data);
                    var features = so.FindProperty("m_RendererFeatures");
                    var map = so.FindProperty("m_RendererFeatureMap");
                    features.arraySize++;
                    features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = existing;
                    map.arraySize++;
                    map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                var fso = new SerializedObject(existing);
                fso.FindProperty("shader").objectReferenceValue = shader;
                fso.ApplyModifiedPropertiesWithoutUndo();
                existing.SetActive(true);
                data.SetDirty();
                EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[VHSSetup] VHS feature present on all URP renderers.");
        }
    }
}
