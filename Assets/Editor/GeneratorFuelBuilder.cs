using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Monta o minigame de abastecer o gerador no PF_generator_trailer da cena aberta e salva como prefab em
/// Assets/Prefabs/Minigames/GeneratorFuelMinigame.prefab. Tudo fica na face da frente do gerador (a de z = -0.62, do
/// lado do jogador): bocal lateral com tampa, medidor, lampada piloto e corda de partida. O galao entra pela esquerda
/// e, com o bocal aberto, segue o cursor.
/// Se o objeto ja existe, so as pecas (filhos) sao refeitas: a raiz, o componente, o StepInteraction e as referencias
/// da cena a ele continuam valendo. Medidas no espaco local do gerador (o modelo gira 270 graus em Y).
/// Menu: Jamlloween/Build Generator Fuel Minigame.
/// </summary>
public static class GeneratorFuelBuilder
{
    const string PrefabPath = "Assets/Prefabs/Minigames/GeneratorFuelMinigame.prefab";
    const string MatDir = "Assets/Materials/Minigames/Generator";

    // Face da frente: x de -1.1 a 1.1, y de 0.63 (base) a 1.88 (topo), z = -0.62. A grade de ventilacao ocupa x -0.08..0.9, y 1.06..1.56.
    const float Face = -0.62f;
    static readonly Vector3 NeckBase = new Vector3(-0.55f, 1.42f, Face);
    static readonly Quaternion NeckRot = Quaternion.Euler(-50f, 0f, 0f);   // +Y sai da face, 40 graus para cima
    const float NeckLength = 0.08f;
    static readonly Vector3 GaugePos = new Vector3(-0.2f, 1.68f, Face - 0.005f);
    static readonly Vector3 LampPos = new Vector3(-0.86f, 1.7f, Face - 0.01f);
    static readonly Vector3 StarterPos = new Vector3(-0.55f, 0.98f, Face - 0.015f);
    static readonly Vector3 CanSpout = new Vector3(0f, 0.5f, -0.125f);   // bico no espaco do modelo do galao

    [MenuItem("Jamlloween/Build Generator Fuel Minigame")]
    public static void Build()
    {
        if (!PSXLegacyGuard.Confirm("Build Generator Fuel Minigame")) return;
        var gen = GameObject.Find("PF_generator_trailer");
        if (gen == null) { Debug.LogError("[GeneratorFuelBuilder] PF_generator_trailer nao esta na cena aberta."); return; }
        var gasCan = Object.FindFirstObjectByType<GasCan>(FindObjectsInactive.Include);
        var canMesh = gasCan != null ? gasCan.GetComponentInChildren<MeshFilter>() : null;
        if (canMesh == null) { Debug.LogError("[GeneratorFuelBuilder] nenhum GasCan com malha na cena aberta."); return; }

        Directory.CreateDirectory(MatDir);
        var metal = Mat("GenMetal", new Color(0.16f, 0.16f, 0.17f), Color.black);
        var capMat = Mat("GenCap", new Color(0.08f, 0.08f, 0.09f), Color.black);
        var dial = Mat("GenDial", new Color(0.85f, 0.83f, 0.74f), Color.black);
        var needle = Mat("GenNeedle", new Color(0.8f, 0.1f, 0.08f), new Color(0.15f, 0f, 0f));
        var green = Mat("GenGreen", new Color(0.1f, 0.6f, 0.15f), new Color(0f, 0.15f, 0f));
        var cord = Mat("GenCord", new Color(0.05f, 0.05f, 0.05f), Color.black);
        var handle = Mat("GenHandle", new Color(0.75f, 0.1f, 0.08f), Color.black);
        var fuel = Mat("GenFuel", new Color(0.78f, 0.62f, 0.18f), new Color(0.25f, 0.18f, 0.03f));
        var foamMat = Mat("GenFoam", new Color(0.95f, 0.9f, 0.7f), new Color(0.2f, 0.18f, 0.1f));
        var lampOff = Mat("GenLampOff", new Color(0.25f, 0.12f, 0.05f), Color.black);
        var lampOn = Mat("GenLampOn", new Color(1f, 0.75f, 0.3f), new Color(2.5f, 1.6f, 0.4f));
        lampOn.EnableKeyword("_EMISSION");
        var splashMat = ParticleMat("GenSplash", new Color(0.85f, 0.7f, 0.25f));
        var smokeMat = ParticleMat("GenSmoke", new Color(0.75f, 0.75f, 0.75f));

        // Raiz: reaproveita a existente (componente, StepInteraction, referencias) e refaz so as pecas.
        var rootT = gen.transform.Find("GeneratorFuelMinigame");
        GeneratorFuelMinigame mg;
        if (rootT != null)
        {
            var go = rootT.gameObject;
            if (PrefabUtility.IsPartOfPrefabInstance(go))
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(go), PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            for (int i = rootT.childCount - 1; i >= 0; i--) Object.DestroyImmediate(rootT.GetChild(i).gameObject);
            mg = go.GetComponent<GeneratorFuelMinigame>();
        }
        else
        {
            rootT = new GameObject("GeneratorFuelMinigame").transform;
            rootT.SetParent(gen.transform, false);
            mg = rootT.gameObject.AddComponent<GeneratorFuelMinigame>();
            var vol = rootT.gameObject.AddComponent<BoxCollider>();   // volume de interacao: o raio do jogador acerta o minigame, nao o modelo
            vol.isTrigger = true;
            vol.center = new Vector3(0f, 1.3f, -0.2f);
            vol.size = new Vector3(2.4f, 1.6f, 1.4f);
        }
        var root = rootT;

        // Bocal: pescoco inclinado saindo da face, como o de um carro.
        var axis = NeckRot * Vector3.up;
        var neck = Prim(PrimitiveType.Cylinder, "Neck", root, NeckBase + axis * (NeckLength * 0.5f), new Vector3(0.1f, NeckLength * 0.5f, 0.1f), metal);
        neck.localRotation = NeckRot;
        var mouth = New("TankMouth", root, NeckBase + axis * NeckLength);
        mouth.localRotation = NeckRot;

        // Tampa (gira em Y local, a partir da boca)
        var capPart = New("Cap", root, mouth.localPosition).gameObject.AddComponent<GeneratorPart>();
        capPart.kind = GeneratorPart.Kind.Cap;
        capPart.transform.localRotation = NeckRot;
        Prim(PrimitiveType.Cylinder, "Disc", capPart.transform, new Vector3(0f, 0.012f, 0f), new Vector3(0.14f, 0.012f, 0.14f), capMat);
        Prim(PrimitiveType.Cube, "Grip", capPart.transform, new Vector3(0f, 0.03f, 0f), new Vector3(0.1f, 0.022f, 0.025f), capMat);
        var capBox = capPart.gameObject.AddComponent<BoxCollider>();
        capBox.center = new Vector3(0f, 0.025f, 0f);
        capBox.size = new Vector3(0.17f, 0.06f, 0.17f);
        // Gancho onde a tampa fica pendurada, abaixo do bocal, deitada na face.
        var capRest = New("CapRest", root, new Vector3(NeckBase.x, NeckBase.y - 0.2f, Face - 0.03f));
        capRest.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        Prim(PrimitiveType.Cube, "CapHook", root, new Vector3(NeckBase.x, NeckBase.y - 0.12f, Face - 0.01f), new Vector3(0.02f, 0.02f, 0.02f), metal);

        // Espuma na boca e respingo do transbordo
        var foam = Prim(PrimitiveType.Cylinder, "Foam", root, mouth.localPosition, new Vector3(0.05f, 0.01f, 0.05f), foamMat);
        foam.localRotation = NeckRot;
        foam.gameObject.SetActive(false);
        var splashGo = new GameObject("Splash");
        splashGo.transform.SetParent(root, false);
        splashGo.transform.localPosition = mouth.localPosition + axis * 0.02f;
        splashGo.transform.localRotation = Quaternion.LookRotation(axis + Vector3.down * 0.3f, Vector3.up);
        var splash = splashGo.AddComponent<ParticleSystem>();
        ConfigureSplash(splash, splashMat);

        // Galao: pivo no bico. Na lateral fica em pe; seguindo o cursor fica pre-inclinado 60 graus, com o corpo para a
        // esquerda e o bico embaixo, para nao tapar o bocal enquanto o jogador mira.
        var canHold = New("CanHoldPose", root, new Vector3(-1.2f, 1.62f, Face - 0.2f));
        var canPour = New("CanPourPose", root, mouth.localPosition + new Vector3(-0.13f, 0.15f, -0.05f));
        canHold.localRotation = Quaternion.Euler(0f, -90f, 0f);   // -Z do modelo (lado do bico) aponta para o bocal
        canPour.localRotation = Quaternion.Euler(0f, 0f, -60f) * canHold.localRotation;
        var pivot = New("PourCan", root, canHold.localPosition);
        pivot.localRotation = canHold.localRotation;
        var canPart = pivot.gameObject.AddComponent<GeneratorPart>();
        canPart.kind = GeneratorPart.Kind.Can;
        var model = new GameObject("Model").transform;
        model.SetParent(pivot, false);
        model.localPosition = -CanSpout;
        model.gameObject.AddComponent<MeshFilter>().sharedMesh = canMesh.sharedMesh;
        model.gameObject.AddComponent<MeshRenderer>().sharedMaterials = canMesh.GetComponent<MeshRenderer>().sharedMaterials;
        pivot.gameObject.SetActive(false);   // sem colisor: ele segue o cursor e o fio vertical nao pode bater nele
        var streamTarget = New("StreamTarget", root, mouth.localPosition);
        var stream = Prim(PrimitiveType.Cube, "Stream", root, mouth.localPosition, Vector3.one * 0.01f, fuel);
        stream.gameObject.SetActive(false);

        // Medidor: vazio a esquerda, cheio a direita (do ponto de vista do jogador).
        var gauge = New("Gauge", root, GaugePos);
        gauge.localRotation = Quaternion.Euler(0f, 180f, 0f);   // +Z sai da face
        Prim(PrimitiveType.Cylinder, "Dial", gauge, Vector3.zero, new Vector3(0.2f, 0.004f, 0.2f), dial).localRotation = Quaternion.Euler(90f, 0f, 0f);
        Prim(PrimitiveType.Cylinder, "Rim", gauge, new Vector3(0f, 0f, 0.001f), new Vector3(0.215f, 0.002f, 0.215f), metal).localRotation = Quaternion.Euler(90f, 0f, 0f);
        const float emptyAngle = -70f, fullAngle = 70f;
        PlaceMark(gauge, "MarkEmpty", emptyAngle, needle);
        PlaceMark(gauge, "MarkFull", fullAngle, green);
        var needlePivot = New("NeedlePivot", gauge, new Vector3(0f, 0f, 0.008f));
        Prim(PrimitiveType.Cube, "Needle", needlePivot, new Vector3(0f, 0.035f, 0f), new Vector3(0.012f, 0.075f, 0.004f), needle);
        Prim(PrimitiveType.Cylinder, "Hub", needlePivot, Vector3.zero, new Vector3(0.025f, 0.006f, 0.025f), metal).localRotation = Quaternion.Euler(90f, 0f, 0f);

        // Lampada piloto
        var lamp = Prim(PrimitiveType.Sphere, "PilotLamp", root, LampPos, Vector3.one * 0.045f, lampOff);
        Prim(PrimitiveType.Cylinder, "LampBase", root, LampPos + new Vector3(0f, 0f, 0.008f), new Vector3(0.06f, 0.006f, 0.06f), metal).localRotation = Quaternion.Euler(90f, 0f, 0f);

        // Fumaca: sai pela grade de ventilacao, que fica dentro do enquadramento.
        var smokeGo = new GameObject("ExhaustSmoke");
        smokeGo.transform.SetParent(root, false);
        smokeGo.transform.localPosition = new Vector3(0.4f, 1.3f, Face - 0.03f);
        smokeGo.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, 0.6f, -1f), Vector3.up);
        var smoke = smokeGo.AddComponent<ParticleSystem>();
        ConfigureSmoke(smoke, smokeMat);

        // Corda de partida: puxa para a direita, na horizontal, sem sair da tela.
        var anchor = New("CordAnchor", root, StarterPos + new Vector3(0f, 0f, -0.02f));
        Prim(PrimitiveType.Cylinder, "StarterHousing", root, StarterPos, new Vector3(0.18f, 0.015f, 0.18f), metal).localRotation = Quaternion.Euler(90f, 0f, 0f);
        var cordRest = New("CordRest", root, StarterPos + new Vector3(0.12f, 0f, -0.05f));
        var cordEnd = New("CordEnd", root, StarterPos + new Vector3(0.62f, 0f, -0.2f));
        var handlePart = New("CordHandle", root, cordRest.localPosition).gameObject.AddComponent<GeneratorPart>();
        handlePart.kind = GeneratorPart.Kind.Cord;
        Prim(PrimitiveType.Cube, "Grip", handlePart.transform, Vector3.zero, new Vector3(0.03f, 0.11f, 0.035f), handle);
        var hb = handlePart.gameObject.AddComponent<BoxCollider>();
        hb.size = new Vector3(0.07f, 0.15f, 0.08f);
        var cordLine = Prim(PrimitiveType.Cube, "CordLine", root, anchor.localPosition, new Vector3(0.008f, 0.008f, 0.1f), cord);

        var spent = New("CanSpentPoint", root, new Vector3(-1.4f, 0f, -0.9f));

        // Camera: uma so, frontal e levemente pela esquerda, enquadrando bocal, medidor, lampada e corda.
        var view = New("ViewPoint", root, new Vector3(-0.62f, 1.42f, -1.6f));
        view.localRotation = Quaternion.LookRotation(new Vector3(-0.38f, 1.32f, Face) - view.localPosition, Vector3.up);

        var so = new SerializedObject(mg);
        Ref(so, "tankMouth", mouth); Ref(so, "cap", capPart); Ref(so, "capRest", capRest);
        Ref(so, "pourCan", canPart); Ref(so, "canHoldPose", canHold); Ref(so, "canPourPose", canPour);
        Ref(so, "stream", stream); Ref(so, "streamTarget", streamTarget); Ref(so, "canSpentPoint", spent);
        Ref(so, "gasCan", gasCan); Ref(so, "foam", foam);
        Ref(so, "gaugeNeedle", needlePivot);
        so.FindProperty("gaugeEmptyAngle").floatValue = emptyAngle;
        so.FindProperty("gaugeFullAngle").floatValue = fullAngle;
        Ref(so, "cordHandle", handlePart); Ref(so, "cordRest", cordRest); Ref(so, "cordEnd", cordEnd);
        Ref(so, "cordAnchor", anchor); Ref(so, "cordLine", cordLine);
        Ref(so, "engineBody", gen.transform.Find("generator_trailer/generator_trailer_unit") ?? gen.transform.Find("generator_trailer_unit"));
        Ref(so, "splash", splash); Ref(so, "exhaustSmoke", smoke);
        Ref(so, "pilotLamp", lamp.GetComponent<Renderer>()); Ref(so, "lampOffMaterial", lampOff); Ref(so, "lampOnMaterial", lampOn);
        Ref(so, "viewPoint", view);
        so.FindProperty("maxTiltDegrees").floatValue = 80f;   // somados aos 60 da pose: o corpo termina acima do bico
        so.FindProperty("pullsToStart").intValue = 2;
        so.FindProperty("activeWhilePlaying").arraySize = 0;   // o galao e ligado pelo proprio minigame, so quando o jogador o traz
        so.FindProperty("prompt").stringValue = "Abastecer gerador";
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject, PrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(gen.scene);
        Debug.Log("[GeneratorFuelBuilder] minigame do gerador montado. Salve a cena.");
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

    static Transform Prim(PrimitiveType type, string name, Transform parent, Vector3 local, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go.transform;
    }

    static void PlaceMark(Transform gauge, string name, float angle, Material mat)
    {
        var m = Prim(PrimitiveType.Cube, name, gauge, Quaternion.AngleAxis(angle, Vector3.forward) * new Vector3(0f, 0.085f, 0.005f), new Vector3(0.014f, 0.022f, 0.003f), mat);
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

    static Material ParticleMat(string name, Color color)
    {
        string path = $"{MatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool created = m == null;
        if (created) m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        // Recorte (alpha clip) com a textura redonda padrao: sem textura cada particula vira um quadrado, e o
        // renderer da cena nao desenha particulas transparentes.
        m.SetColor("_BaseColor", color);
        m.SetTexture("_BaseMap", AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd"));
        m.SetFloat("_Surface", 0f);
        m.SetFloat("_ZWrite", 1f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
        m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetFloat("_AlphaClip", 1f);
        m.SetFloat("_Cutoff", 0.35f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        if (created) AssetDatabase.CreateAsset(m, path);
        else EditorUtility.SetDirty(m);
        return m;
    }

    static void ConfigureSplash(ParticleSystem ps, Material mat)
    {
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 0.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.03f);
        main.startColor = new Color(0.9f, 0.75f, 0.3f);
        main.gravityModifier = 1.4f;
        var em = ps.emission;
        em.rateOverTime = 50f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.04f;
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    static void ConfigureSmoke(ParticleSystem ps, Material mat)
    {
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
        main.startColor = new Color(0.7f, 0.7f, 0.7f);
        main.gravityModifier = -0.04f;
        var em = ps.emission;
        em.rateOverTime = 0f;   // sopros por Emit(); o minigame liga uma vazao baixa com o motor ligado
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.5f, 0.3f, 0.02f);
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.5f, 1.4f), new Keyframe(1f, 0f)));   // cresce e some encolhendo
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
