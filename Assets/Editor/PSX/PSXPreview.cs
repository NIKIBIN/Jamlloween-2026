using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Renderiza um prefab numa cena de preview (fora das cenas abertas) e salva um PNG. Serve para comparar antes e
/// depois da conversao PSX sem tocar em cena nenhuma. A camera usa o filho "ViewPoint" do minigame quando existe.
/// bake = true roda o PSXBake na copia em memoria antes de renderizar (nada e gravado).
/// </summary>
public static class PSXPreview
{
    public static string Render(string prefabPath, string pngPath, bool bake, int width = 640, int height = 480) =>
        Render(prefabPath, pngPath, bake, null, width, height);

    /// <summary>tweak: mexe na copia antes de renderizar (ex.: trocar a fonte para testar). Nada volta para o prefab.</summary>
    public static string Render(string prefabPath, string pngPath, bool bake, System.Action<GameObject> tweak, int width = 640, int height = 480)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return $"prefab {prefabPath} nao encontrado";

        var scene = EditorSceneManager.NewPreviewScene();
        var go = (GameObject)Object.Instantiate(prefab);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        string report = "";
        try
        {
            foreach (var l in go.GetComponentsInChildren<Light>(true)) l.enabled = false;
            if (bake) report = PSXBake.BakeHierarchy(go, null).ToString();
            tweak?.Invoke(go);

            var camGo = new GameObject("PreviewCamera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, scene);
            var cam = camGo.AddComponent<Camera>();
            cam.scene = scene;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.01f;

            var bounds = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) if (r.enabled && !(r is ParticleSystemRenderer)) bounds.Encapsulate(r.bounds);
            var view = FindDeep(go.transform, "ViewPoint");
            if (view != null) cam.transform.SetPositionAndRotation(view.position, view.rotation);
            else
            {
                float dist = bounds.extents.magnitude / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                cam.transform.position = bounds.center + new Vector3(0.6f, 0.5f, -1f).normalized * dist;
                cam.transform.LookAt(bounds.center);
            }

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            Directory.CreateDirectory(Path.GetDirectoryName(pngPath));
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        return $"{pngPath} ok. {report}";
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t) { var r = FindDeep(c, name); if (r != null) return r; }
        return null;
    }
}
