using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Monta o minigame de abastecer o gerador no PF_generator_trailer da cena aberta (tampa, funil, medidor, corda, galao
/// que inclina, respingo) e salva como prefab em Assets/Prefabs/Minigames/GeneratorFuelMinigame.prefab.
/// Tudo e feito de primitivas sobre o modelo do gerador. Medidas no espaco local do gerador (o modelo gira 270 graus em Y).
/// Menu: Jamlloween/Build Generator Fuel Minigame. Se o objeto ja existe na cena, nao faz nada.
/// </summary>
public static class GeneratorFuelBuilder
{
    const string PrefabPath = "Assets/Prefabs/Minigames/GeneratorFuelMinigame.prefab";
    const string MeshPath = "Assets/Prefabs/Minigames/Meshes/GeneratorFunnel.asset";
    const string MatDir = "Assets/Materials/Minigames/Generator";

    // Espaco local do gerador: unidade de x -1.1 a 1.1, z -0.6 a 0.6, topo em y 2.375. A face z = -0.62 olha para o lado do jogador.
    static readonly Vector3 Mouth = new Vector3(-0.1f, 2.375f, -0.2f);
    static readonly Vector3 CanSpout = new Vector3(0f, 0.5f, -0.125f);   // bico no espaco do modelo do galao

    [MenuItem("Jamlloween/Build Generator Fuel Minigame")]
    public static void Build()
    {
        var gen = GameObject.Find("PF_generator_trailer");
        if (gen == null) { Debug.LogError("[GeneratorFuelBuilder] PF_generator_trailer nao esta na cena aberta."); return; }
        if (gen.transform.Find("GeneratorFuelMinigame") != null) { Debug.Log("[GeneratorFuelBuilder] ja existe."); return; }
        var jerry = GameObject.Find("PF_jerry_can")?.transform.Find("jerry_can");
        if (jerry == null) { Debug.LogError("[GeneratorFuelBuilder] PF_jerry_can nao esta na cena aberta."); return; }

        Directory.CreateDirectory(MatDir);
        Directory.CreateDirectory(Path.GetDirectoryName(MeshPath));

        var metal = Mat("GenMetal", new Color(0.16f, 0.16f, 0.17f), Color.black);
        var cap = Mat("GenCap", new Color(0.08f, 0.08f, 0.09f), Color.black);
        var funnelMat = Mat("GenFunnel", new Color(0.85f, 0.45f, 0.08f), Color.black);
        var dial = Mat("GenDial", new Color(0.85f, 0.83f, 0.74f), Color.black);
        var needle = Mat("GenNeedle", new Color(0.8f, 0.1f, 0.08f), new Color(0.15f, 0f, 0f));
        var green = Mat("GenGreen", new Color(0.1f, 0.6f, 0.15f), new Color(0f, 0.15f, 0f));
        var cord = Mat("GenCord", new Color(0.05f, 0.05f, 0.05f), Color.black);
        var handle = Mat("GenHandle", new Color(0.75f, 0.1f, 0.08f), Color.black);
        var fuel = Mat("GenFuel", new Color(0.78f, 0.62f, 0.18f), new Color(0.25f, 0.18f, 0.03f));
        var splashMat = SplashMat();

        var root = new GameObject("GeneratorFuelMinigame").transform;
        root.SetParent(gen.transform, false);
        var mg = root.gameObject.AddComponent<GeneratorFuelMinigame>();
        var vol = root.gameObject.AddComponent<BoxCollider>();   // volume de interacao: o raio do jogador acerta o minigame, nao o modelo do gerador
        vol.isTrigger = true;
        vol.center = new Vector3(0f, 1.5f, 0f);
        vol.size = new Vector3(2.5f, 2f, 1.5f);

        // Boca do tanque
        var mouth = New("TankMouth", root, Mouth);
        Prim(PrimitiveType.Cylinder, "Neck", mouth, new Vector3(0f, 0.01f, 0f), new Vector3(0.13f, 0.01f, 0.13f), metal, false);

        // Tampa (gira em Y local, a partir da boca)
        var capPart = New("Cap", root, Mouth).gameObject.AddComponent<GeneratorPart>();
        capPart.kind = GeneratorPart.Kind.Cap;
        Prim(PrimitiveType.Cylinder, "Disc", capPart.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.17f, 0.02f, 0.17f), cap, false);
        Prim(PrimitiveType.Cube, "Grip", capPart.transform, new Vector3(0f, 0.045f, 0f), new Vector3(0.12f, 0.02f, 0.025f), cap, false);
        var capBox = capPart.gameObject.AddComponent<BoxCollider>();
        capBox.center = new Vector3(0f, 0.03f, 0f);
        capBox.size = new Vector3(0.18f, 0.06f, 0.18f);
        var capRest = New("CapRest", root, Mouth + new Vector3(-0.4f, 0.02f, 0f));

        // Funil
        var funnelPart = New("Funnel", root, Mouth + new Vector3(0.5f, 0f, -0.05f)).gameObject.AddComponent<GeneratorPart>();
        funnelPart.kind = GeneratorPart.Kind.Funnel;
        var fm = Prim(PrimitiveType.Cube, "Mesh", funnelPart.transform, Vector3.zero, Vector3.one, funnelMat, false);
        fm.GetComponent<MeshFilter>().sharedMesh = FunnelMesh();
        var fb = funnelPart.gameObject.AddComponent<BoxCollider>();
        fb.center = new Vector3(0f, 0.07f, 0f);
        fb.size = new Vector3(0.18f, 0.14f, 0.18f);
        var funnelHome = New("FunnelHome", root, funnelPart.transform.localPosition);
        var funnelSeat = New("FunnelSeat", root, Mouth + new Vector3(0f, 0.02f, 0f));

        // Galao que inclina: pivo no bico. O corpo se estende para o lado do jogador (-Z do gerador).
        var canHold = New("CanHoldPose", root, Mouth + new Vector3(0f, 0.55f, -0.55f));
        var canPour = New("CanPourPose", root, Mouth + new Vector3(0f, 0.22f, 0f));
        canHold.localRotation = canPour.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var pivot = New("PourCan", root, canPour.localPosition);
        pivot.localRotation = canPour.localRotation;
        var model = new GameObject("Model").transform;
        model.SetParent(pivot, false);
        model.localPosition = -CanSpout;
        model.gameObject.AddComponent<MeshFilter>().sharedMesh = jerry.GetComponent<MeshFilter>().sharedMesh;
        model.gameObject.AddComponent<MeshRenderer>().sharedMaterials = jerry.GetComponent<MeshRenderer>().sharedMaterials;
        var streamTarget = New("StreamTarget", root, Mouth + new Vector3(0f, 0.1f, 0f));
        var stream = Prim(PrimitiveType.Cube, "Stream", root, Mouth, Vector3.one * 0.01f, fuel, false);
        stream.gameObject.SetActive(false);

        // Respingo
        var splashGo = new GameObject("Splash");
        splashGo.transform.SetParent(root, false);
        splashGo.transform.localPosition = Mouth + new Vector3(0f, 0.17f, 0f);
        splashGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        var splash = splashGo.AddComponent<ParticleSystem>();
        ConfigureSplash(splash, splashMat);

        // Medidor analogico na face do jogador (+Z do objeto aponta para fora do gerador)
        var gauge = New("Gauge", root, new Vector3(0.4f, 2.12f, -0.62f));
        gauge.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var disc = Prim(PrimitiveType.Cylinder, "Dial", gauge, Vector3.zero, new Vector3(0.2f, 0.004f, 0.2f), dial, false);
        disc.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Prim(PrimitiveType.Cylinder, "Rim", gauge, new Vector3(0f, 0f, 0.001f), new Vector3(0.215f, 0.002f, 0.215f), metal, false).localRotation = Quaternion.Euler(90f, 0f, 0f);
        PlaceMark(gauge, "MarkEmpty", 70f, needle);
        PlaceMark(gauge, "MarkFull", -70f, green);
        var needlePivot = New("NeedlePivot", gauge, new Vector3(0f, 0f, 0.008f));
        Prim(PrimitiveType.Cube, "Needle", needlePivot, new Vector3(0f, 0.035f, 0f), new Vector3(0.012f, 0.075f, 0.004f), needle, false);
        Prim(PrimitiveType.Cylinder, "Hub", needlePivot, Vector3.zero, new Vector3(0.025f, 0.006f, 0.025f), metal, false).localRotation = Quaternion.Euler(90f, 0f, 0f);

        // Corda de partida
        var anchor = New("CordAnchor", root, new Vector3(0.75f, 1.0f, -0.63f));
        Prim(PrimitiveType.Cylinder, "StarterHousing", anchor, Vector3.zero, new Vector3(0.16f, 0.03f, 0.16f), metal, false).localRotation = Quaternion.Euler(90f, 0f, 0f);
        var cordRest = New("CordRest", root, anchor.localPosition + new Vector3(0.12f, 0f, -0.1f));
        var cordEnd = New("CordEnd", root, anchor.localPosition + new Vector3(0.6f, -0.05f, -0.4f));
        var handlePart = New("CordHandle", root, cordRest.localPosition).gameObject.AddComponent<GeneratorPart>();
        handlePart.kind = GeneratorPart.Kind.Cord;
        Prim(PrimitiveType.Cube, "Grip", handlePart.transform, Vector3.zero, new Vector3(0.1f, 0.03f, 0.03f), handle, false);
        var hb = handlePart.gameObject.AddComponent<BoxCollider>();
        hb.size = new Vector3(0.14f, 0.09f, 0.09f);
        var cordLine = Prim(PrimitiveType.Cube, "CordLine", root, anchor.localPosition, new Vector3(0.012f, 0.012f, 0.1f), cord, false);

        var spent = New("CanSpentPoint", root, new Vector3(0.2f, 0f, -1.6f));

        // Liga as referencias
        var so = new SerializedObject(mg);
        Ref(so, "tankMouth", mouth); Ref(so, "cap", capPart); Ref(so, "capRest", capRest);
        Ref(so, "funnel", funnelPart); Ref(so, "funnelHome", funnelHome); Ref(so, "funnelSeat", funnelSeat);
        Ref(so, "pourCan", pivot); Ref(so, "canHoldPose", canHold); Ref(so, "canPourPose", canPour);
        Ref(so, "stream", stream); Ref(so, "streamTarget", streamTarget); Ref(so, "canSpentPoint", spent);
        Ref(so, "gaugeNeedle", needlePivot);
        Ref(so, "cordHandle", handlePart); Ref(so, "cordRest", cordRest); Ref(so, "cordEnd", cordEnd);
        Ref(so, "cordAnchor", anchor); Ref(so, "cordLine", cordLine);
        Ref(so, "engineBody", gen.transform.Find("generator_trailer_unit"));
        Ref(so, "splash", splash);

        // Camera: de cima, pelo canto do lado do jogador, vendo o topo, o medidor e a corda.
        var view = new GameObject("ViewPoint").transform;
        view.SetParent(root, false);
        view.position = gen.transform.TransformPoint(new Vector3(1.0f, 3.6f, -1.8f));
        view.rotation = Quaternion.LookRotation(gen.transform.TransformPoint(new Vector3(0.1f, 2.0f, -0.25f)) - view.position, Vector3.up);
        Ref(so, "viewPoint", view);
        var playing = so.FindProperty("activeWhilePlaying");
        playing.arraySize = 1;
        playing.GetArrayElementAtIndex(0).objectReferenceValue = pivot.gameObject;
        so.FindProperty("prompt").stringValue = "Abastecer gerador";
        so.ApplyModifiedProperties();

        // Galao do mundo e carregador do jogador
        var jerryRoot = GameObject.Find("PF_jerry_can");
        var can = jerryRoot.GetComponent<GasCan>() ?? jerryRoot.AddComponent<GasCan>();
        var so2 = new SerializedObject(mg);
        Ref(so2, "gasCan", can);
        so2.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject, PrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(gen.scene);
        Debug.Log("[GeneratorFuelBuilder] minigame do gerador criado. Salve a cena.");
    }

    // ---------- util ----------

    static void Ref(SerializedObject so, string field, Object value)
    {
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogError("[GeneratorFuelBuilder] campo " + field + " nao existe"); return; }
        p.objectReferenceValue = value;
    }

    static Transform New(string name, Transform parent, Vector3 local)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = local;
        return t;
    }

    static Transform Prim(PrimitiveType type, string name, Transform parent, Vector3 local, Vector3 scale, Material mat, bool collider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go.transform;
    }

    static void PlaceMark(Transform gauge, string name, float angle, Material mat)
    {
        var m = Prim(PrimitiveType.Cube, name, gauge, Quaternion.AngleAxis(angle, Vector3.forward) * new Vector3(0f, 0.085f, 0.005f), new Vector3(0.012f, 0.02f, 0.003f), mat, false);
        m.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    static Material Mat(string name, Color color, Color emission)
    {
        string path = $"{MatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        // O mundo e iluminado por shader (ver MinigameMaterialConverter): o Lit comum fica preto.
        m = new Material(Shader.Find("Jamlloween/Flashlight Fog Surface"));
        m.SetColor("_BaseColor", color);
        m.SetColor("_EmissionColor", emission);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static Material SplashMat()
    {
        string path = $"{MatDir}/GenSplash.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        m.SetColor("_BaseColor", new Color(0.85f, 0.7f, 0.25f));
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void ConfigureSplash(ParticleSystem ps, Material mat)
    {
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 0.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.03f);
        main.startColor = new Color(0.9f, 0.75f, 0.3f);
        main.gravityModifier = 1.4f;
        var em = ps.emission;
        em.rateOverTime = 45f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 40f;
        shape.radius = 0.06f;
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    /// <summary>Funil: tronco de cone aberto, pivo no centro da base, dos dois lados (fino na ponta, largo na boca).</summary>
    static Mesh FunnelMesh()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (existing != null) return existing;

        const int n = 20;
        const float h = 0.14f, rBottom = 0.025f, rTop = 0.09f;
        var verts = new Vector3[n * 2];
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            verts[i] = dir * rBottom;
            verts[n + i] = dir * rTop + Vector3.up * h;
        }
        var tris = new System.Collections.Generic.List<int>();
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            tris.AddRange(new[] { i, n + i, n + j, i, n + j, j });          // fora
            tris.AddRange(new[] { i, n + j, n + i, i, j, n + j });          // dentro
        }
        var mesh = new Mesh { name = "GeneratorFunnel", vertices = verts, triangles = tris.ToArray() };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, MeshPath);
        return mesh;
    }
}
