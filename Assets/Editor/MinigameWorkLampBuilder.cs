using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Gera o prefab da luminaria de obra dos minigames e a coloca ao lado de cada minigame da cena aberta.
/// Menus: Circus > Build Minigame Work Lamp Prefab, e Circus > Add Work Lamps To Open Scene.
/// A luminaria fica de pe no chao, do lado direito de quem joga, e aponta para o minigame.
/// Depois e so mexer nos objetos "WorkLamp_*" da cena para ajustar posicao e altura.
/// </summary>
public static class MinigameWorkLampBuilder
{
    const string PrefabPath = "Assets/Prefabs/Minigames/MinigameWorkLamp.prefab";
    const string MatDir = "Assets/Materials/Minigames/WorkLamp";
    const float PoleRadius = 0.012f;

    [MenuItem("Circus/Build Minigame Work Lamp Prefab")]
    public static void BuildPrefab()
    {
        Directory.CreateDirectory(MatDir);
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        var body = Mat("WorkLampBody", new Color(0.95f, 0.75f, 0.1f), 0.35f, 0.2f);
        var steel = Mat("WorkLampSteel", new Color(0.55f, 0.57f, 0.6f), 0.5f, 0.8f);
        var dark = Mat("WorkLampBase", new Color(0.12f, 0.12f, 0.13f), 0.3f, 0.5f);
        var on = Mat("WorkLampBulbOn", new Color(1f, 0.9f, 0.65f), 0.5f, 0f, new Color(5f, 3.8f, 2.2f));
        var off = Mat("WorkLampBulbOff", new Color(0.5f, 0.45f, 0.35f), 0.7f, 0f);

        var root = new GameObject("MinigameWorkLamp");
        var lamp = root.AddComponent<MinigameWorkLamp>();

        // Base e haste (a altura e ajustada ao colocar na cena: Pole escala em Y, Head sobe).
        Prim(PrimitiveType.Cylinder, "Base", root.transform, new Vector3(0f, 0.015f, 0f), new Vector3(0.34f, 0.015f, 0.34f), dark);
        var pole = Prim(PrimitiveType.Cylinder, "Pole", root.transform, new Vector3(0f, 0.6f, 0f), new Vector3(PoleRadius * 2f, 0.6f, PoleRadius * 2f), steel);
        for (int i = 0; i < 3; i++)   // pernas do tripe
        {
            var leg = Prim(PrimitiveType.Cylinder, "Leg" + i, root.transform, Vector3.zero, new Vector3(0.02f, 0.14f, 0.02f), steel);
            leg.transform.localRotation = Quaternion.Euler(0f, i * 120f, 0f) * Quaternion.Euler(60f, 0f, 0f);
            leg.transform.localPosition = Quaternion.Euler(0f, i * 120f, 0f) * new Vector3(0f, 0.1f, 0.1f);
        }

        // Cabeca: gira para apontar para o minigame. A frente e +Z.
        var head = new GameObject("Head").transform;
        head.SetParent(root.transform, false);
        head.localPosition = new Vector3(0f, 1.2f, 0f);
        var shade = Prim(PrimitiveType.Cylinder, "Shade", head, new Vector3(0f, 0f, 0.03f), new Vector3(0.14f, 0.045f, 0.14f), body);
        shade.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Prim(PrimitiveType.Cube, "Yoke", head, new Vector3(0f, -0.065f, -0.02f), new Vector3(0.025f, 0.06f, 0.025f), steel);
        var bulb = Prim(PrimitiveType.Sphere, "Bulb", head, new Vector3(0f, 0f, 0.055f), Vector3.one * 0.08f, off);

        var lightGo = new GameObject("Spot");
        lightGo.transform.SetParent(head, false);
        lightGo.transform.localPosition = new Vector3(0f, 0f, 0.07f);
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Spot;
        l.color = new Color(1f, 0.86f, 0.62f);
        l.range = 4f;
        l.spotAngle = 90f;
        l.innerSpotAngle = 40f;
        l.shadows = LightShadows.None;
        l.intensity = 0f;
        l.enabled = false;

        var so = new SerializedObject(lamp);
        so.FindProperty("spot").objectReferenceValue = l;
        so.FindProperty("bulb").objectReferenceValue = bulb.GetComponent<Renderer>();
        so.FindProperty("bulbOn").objectReferenceValue = on;
        so.FindProperty("bulbOff").objectReferenceValue = off;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("[MinigameWorkLampBuilder] prefab gerado em " + PrefabPath);
    }

    [MenuItem("Circus/Add Work Lamps To Open Scene")]
    public static void AddToScene()
    {
        BuildPrefab();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var scene = EditorSceneManager.GetActiveScene();

        foreach (var go in scene.GetRootGameObjects())
            if (go.name.StartsWith("WorkLamp_")) Object.DestroyImmediate(go);

        int count = 0;
        foreach (var mg in Object.FindObjectsByType<Minigame>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var view = new SerializedObject(mg).FindProperty("viewPoint").objectReferenceValue as Transform;
            if (view == null) continue;
            Vector3 target = AimPoint(mg, view);

            // Do lado direito de quem joga, um pouco para tras, no chao.
            Vector3 right = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;
            Vector3 fwd = Vector3.ProjectOnPlane(view.forward, Vector3.up);
            fwd = fwd.sqrMagnitude < 0.01f ? Vector3.ProjectOnPlane(view.up, Vector3.up).normalized : fwd.normalized;
            Vector3 spot = target + right * 0.85f - fwd * 0.35f;
            float groundY = GroundBelow(spot, target.y, mg);

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = "WorkLamp_" + mg.name;
            inst.transform.position = new Vector3(spot.x, groundY, spot.z);
            inst.transform.rotation = Quaternion.LookRotation(fwd == Vector3.zero ? Vector3.forward : -fwd);

            float headHeight = Mathf.Max(target.y + 0.55f - groundY, 0.8f);
            var pole = inst.transform.Find("Pole");
            pole.localPosition = new Vector3(0f, headHeight * 0.5f, 0f);
            pole.localScale = new Vector3(PoleRadius * 2f, headHeight * 0.5f, PoleRadius * 2f);
            var head = inst.transform.Find("Head");
            head.localPosition = new Vector3(0f, headHeight, 0f);
            head.rotation = Quaternion.LookRotation(target - head.position);

            var lamp = inst.GetComponent<MinigameWorkLamp>();
            lamp.Minigame = mg;
            RemoveInvisibleWorkLights(mg);
            EditorUtility.SetDirty(lamp);
            count++;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[MinigameWorkLampBuilder] {count} luminaria(s) colocada(s) em {scene.name}");
    }

    /// <summary>
    /// Tira dos "activeWhilePlaying" do minigame as luzes invisiveis (um Light solto, sem objeto na cena). A luminaria
    /// substitui: a luz agora vem de um objeto que existe no mundo.
    /// </summary>
    static void RemoveInvisibleWorkLights(Minigame mg)
    {
        var so = new SerializedObject(mg);
        var list = so.FindProperty("activeWhilePlaying");
        var keep = new System.Collections.Generic.List<Object>();
        for (int i = 0; i < list.arraySize; i++)
        {
            var go = list.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (go == null) continue;
            var l = go.GetComponent<Light>();
            bool invisibleLight = l != null && go.GetComponent<Renderer>() == null && go.transform.childCount == 0;
            if (invisibleLight) go.SetActive(false); else keep.Add(go);
        }
        list.arraySize = keep.Count;
        for (int i = 0; i < keep.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = keep[i];
        so.ApplyModifiedProperties();
    }

    /// <summary>Onde o jogador olha: o ponto da linha de visao do minigame mais perto do centro do objeto.</summary>
    static Vector3 AimPoint(Minigame mg, Transform view)
    {
        var rs = mg.GetComponentsInChildren<Renderer>(false);
        Bounds b = new Bounds(mg.transform.position, Vector3.zero);
        bool any = false;
        foreach (var r in rs)
        {
            if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        Vector3 center = any ? b.center : mg.transform.position;
        float t = Mathf.Max(Vector3.Dot(center - view.position, view.forward), 0.3f);
        return view.position + view.forward * t;
    }

    /// <summary>Altura do chao (ou da mesa) abaixo do ponto, ignorando o proprio minigame.</summary>
    static float GroundBelow(Vector3 from, float targetY, Minigame mg)
    {
        var hits = Physics.RaycastAll(new Vector3(from.x, targetY, from.z), Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.NegativeInfinity;
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(mg.transform)) continue;
            if (h.point.y > best) best = h.point.y;
        }
        return float.IsNegativeInfinity(best) ? targetY - 1f : best;
    }

    // ---------- materiais ----------

    static Material Mat(string name, Color color, float smoothness, float metallic, Color? emission = null)
    {
        string path = $"{MatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Jamlloween/Flashlight Fog Surface"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = Shader.Find("Jamlloween/Flashlight Fog Surface");
        m.SetColor("_BaseColor", color);
        m.SetColor("_EmissionColor", emission ?? Color.black);
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
}
