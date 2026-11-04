using System.Linq;
using HeyHeyThere.PSXCarnivalKit;
using Jamlloween.FerrisWheel;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Monta a roda-gigante do final (Assets/Prefabs/FerrisWheelRide.prefab): a base e o anel do PSX Carnival Kit, o
/// FerrisWheelRide (estados e giro), o carrinho do jogador (porta, assento) e o motor com o minigame provisorio.
/// Pecas novas usam as malhas PSX (Meshes/Kit) e os recortes do atlas (Materials/Minigames/Atlas), no padrao do kit.
///  - Jamlloween > Ferris Wheel > Build Prefab: (re)cria o prefab. Rodar de novo refaz tudo.
///  - Jamlloween > Ferris Wheel > Place In Open Scene: troca a base e o anel soltos do kit na cena aberta pelo prefab,
///    na mesma posicao. Numa cena nova, basta arrastar o prefab.
/// Medidas no espaco da base do kit: eixo a 7.6 m, raio dos carrinhos 6 m, pernas da frente em z = -1.18.
/// </summary>
public static class FerrisWheelBuilder
{
    public const string PrefabPath = "Assets/Prefabs/FerrisWheelRide.prefab";
    const string KitPrefabs = "Assets/PSXCarnivalKit/Prefabs/";
    const string Meshes = "Assets/Prefabs/Minigames/Meshes/Kit/";
    const string Atlas = "Assets/Materials/Minigames/Atlas/";
    const int PlayerCar = 6;   // ferris_wheel_ring_car_7

    // Carrinho (espaco local do carro, pivo no ponto de pendurar): cacamba de y -1.275 a ~-0.7, 1.21 em x, 1.70 em z.
    // Assento na ponta da frente (-Z): a haste que pendura o carro fica atras do jogador.
    static readonly Vector3 SeatFloor = new Vector3(0f, -1.18f, -0.45f);
    static readonly Vector3 DoorHinge = new Vector3(-0.38f, -0.86f, -0.87f);
    const float DoorWidth = 0.76f, DoorHeight = 0.34f;

    // Motor: ao lado da perna direita da frente, virado para -Z (de onde o jogador chega).
    static readonly Vector3 MotorPos = new Vector3(4.6f, 0f, -0.4f);

    [MenuItem("Jamlloween/Ferris Wheel/Build Prefab")]
    public static string BuildPrefab()
    {
        var root = new GameObject("FerrisWheelRide");
        try
        {
            var ride = root.AddComponent<FerrisWheelRide>();
            var baseGo = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(KitPrefabs + "PF_ferris_wheel_base.prefab"), root.transform);
            var ring = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(KitPrefabs + "PF_ferris_wheel_ring.prefab"), root.transform);

            // O FerrisWheelRide gira o anel: o Machine e o Animation do kit sairiam girando sozinhos com o E do LevelWalker.
            Object.DestroyImmediate(ring.GetComponent<Machine>());
            Object.DestroyImmediate(ring.GetComponent<Animation>());

            var hub = ring.transform.Find("ferris_wheel_ring/ferris_wheel_ring_spokes");
            var cars = Enumerable.Range(1, 8).Select(i => hub.Find($"ferris_wheel_ring_car_{i}")).ToArray();
            var bulbs = hub.Find("ferris_wheel_ring_bulbs").GetComponent<Renderer>();

            var seat = BuildSeat(cars[PlayerCar]);
            var motor = BuildMotor(root.transform);

            var so = new SerializedObject(ride);
            so.FindProperty("hub").objectReferenceValue = hub;
            var carsProp = so.FindProperty("cars");
            carsProp.arraySize = cars.Length;
            for (int i = 0; i < cars.Length; i++) carsProp.GetArrayElementAtIndex(i).objectReferenceValue = cars[i];
            var lightsProp = so.FindProperty("lights");
            lightsProp.arraySize = 1;
            lightsProp.GetArrayElementAtIndex(0).objectReferenceValue = bulbs;
            so.FindProperty("seat").objectReferenceValue = seat;
            so.FindProperty("playerCar").intValue = PlayerCar;
            so.FindProperty("repairMinigame").objectReferenceValue = motor;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool ok);
            return ok ? "prefab " + PrefabPath : "falhou ao salvar " + PrefabPath;
        }
        finally { Object.DestroyImmediate(root); }
    }

    [MenuItem("Jamlloween/Ferris Wheel/Place In Open Scene")]
    public static string PlaceInOpenScene()
    {
        var prefab = Load<GameObject>(PrefabPath);
        if (prefab == null) return "rode Build Prefab antes";
        var scene = EditorSceneManager.GetActiveScene();

        var existing = Object.FindAnyObjectByType<FerrisWheelRide>(FindObjectsInactive.Include);
        if (existing != null) return "a cena ja tem " + existing.name;

        // A base solta do kit define a posicao; o anel solto sai junto.
        GameObject oldBase = null, oldRing = null;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) continue;
            var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
            if (src == null) continue;
            if (src.name == "PF_ferris_wheel_base") oldBase = t.gameObject;
            if (src.name == "PF_ferris_wheel_ring") oldRing = t.gameObject;
        }
        if (oldBase == null) return "a cena nao tem PF_ferris_wheel_base para trocar";

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        inst.transform.SetParent(oldBase.transform.parent, false);
        inst.transform.SetPositionAndRotation(oldBase.transform.position, oldBase.transform.rotation);
        inst.transform.SetSiblingIndex(oldBase.transform.GetSiblingIndex());
        Undo.RegisterCreatedObjectUndo(inst, "Place Ferris Wheel Ride");
        Undo.DestroyObjectImmediate(oldBase);
        if (oldRing != null) Undo.DestroyObjectImmediate(oldRing);
        EditorSceneManager.MarkSceneDirty(scene);
        return $"{inst.name} em {inst.transform.position}";
    }

    // ------------------------------------------------------------------ carrinho

    static FerrisWheelSeat BuildSeat(Transform car)
    {
        var yellow = Load<Material>(Atlas + "M_Atlas_AmareloGerador.mat");

        var seatGo = new GameObject("PlayerSeat");
        seatGo.transform.SetParent(car, false);
        var box = seatGo.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(0f, -0.95f, 0f);
        box.size = new Vector3(1.3f, 0.8f, 1.8f);
        var seat = seatGo.AddComponent<FerrisWheelSeat>();

        // Pes no piso da cacamba, olhando para -Z (para fora da roda, para o parque).
        var anchor = New("SeatAnchor", seatGo.transform, SeatFloor);
        anchor.localRotation = Quaternion.Euler(0f, 180f, 0f);

        // Portinha: grade amarela na face -Z, dobradica do lado -X. Fechada = rotacao zero.
        var door = New("Door", seatGo.transform, DoorHinge);
        Part("Door_Top", door, new Vector3(DoorWidth / 2f, DoorHeight / 2f, 0f), new Vector3(DoorWidth, 0.04f, 0.04f), "PSX_Box", yellow);
        Part("Door_Bottom", door, new Vector3(DoorWidth / 2f, -DoorHeight / 2f, 0f), new Vector3(DoorWidth, 0.04f, 0.04f), "PSX_Box", yellow);
        for (int i = 0; i < 4; i++)
        {
            float x = Mathf.Lerp(0.02f, DoorWidth - 0.02f, i / 3f);
            Part($"Door_Bar_{i}", door, new Vector3(x, 0f, 0f), new Vector3(0.03f, DoorHeight, 0.03f), "PSX_Box", yellow);
        }

        var so = new SerializedObject(seat);
        so.FindProperty("anchor").objectReferenceValue = anchor;
        so.FindProperty("door").objectReferenceValue = door;
        so.FindProperty("doorOpenAngle").floatValue = 105f;   // abre para fora (-Z)
        so.ApplyModifiedPropertiesWithoutUndo();
        return seat;
    }

    // ------------------------------------------------------------------ motor

    static FerrisWheelMotorMinigame BuildMotor(Transform parent)
    {
        Material M(string n) => Load<Material>(Atlas + $"M_Atlas_{n}.mat");
        var dark = M("AcoEscuro"); var green = M("PinturaVerde"); var grey = M("MetalCinza");
        var brass = M("Latao"); var rubber = M("BorrachaPreta"); var red = M("VermelhoGalao"); var rust = M("Ferrugem");

        var motor = New("Motor", parent, MotorPos);
        var body = motor.gameObject.AddComponent<BoxCollider>();
        body.center = new Vector3(-0.1f, 0.45f, 0f);
        body.size = new Vector3(1.9f, 0.9f, 0.95f);

        // Base, pes e carcaca do motor (cilindro deitado em X).
        Part("Skid", motor, new Vector3(0f, 0.06f, 0f), new Vector3(1.5f, 0.12f, 0.9f), "PSX_Box", rust);
        Part("Housing", motor, new Vector3(0.1f, 0.5f, 0f), new Vector3(0.72f, 0.5f, 0.72f), "PSX_Prism8", green, Quaternion.Euler(0f, 0f, 90f));
        Part("Housing_CapA", motor, new Vector3(-0.42f, 0.5f, 0f), new Vector3(0.6f, 0.04f, 0.6f), "PSX_Prism8", dark, Quaternion.Euler(0f, 0f, 90f));
        Part("Housing_CapB", motor, new Vector3(0.62f, 0.5f, 0f), new Vector3(0.6f, 0.04f, 0.6f), "PSX_Prism8", dark, Quaternion.Euler(0f, 0f, 90f));
        Part("Housing_FootA", motor, new Vector3(-0.15f, 0.2f, 0f), new Vector3(0.12f, 0.18f, 0.7f), "PSX_Box", dark);
        Part("Housing_FootB", motor, new Vector3(0.35f, 0.2f, 0f), new Vector3(0.12f, 0.18f, 0.7f), "PSX_Box", dark);
        Part("Terminal_Box", motor, new Vector3(0.1f, 0.84f, 0f), new Vector3(0.3f, 0.16f, 0.26f), "PSX_Box", dark);

        // Eixo, polia e correia ate a caixa de reducao presa a perna da roda.
        Part("Shaft", motor, new Vector3(-0.56f, 0.5f, 0f), new Vector3(0.08f, 0.1f, 0.08f), "PSX_Prism8", grey, Quaternion.Euler(0f, 0f, 90f));
        Part("Pulley", motor, new Vector3(-0.68f, 0.5f, 0f), new Vector3(0.42f, 0.03f, 0.42f), "PSX_Prism8", brass, Quaternion.Euler(0f, 0f, 90f));
        Part("Gearbox", motor, new Vector3(-0.98f, 0.45f, 0.1f), new Vector3(0.36f, 0.9f, 0.5f), "PSX_Box", dark);
        Part("Gearbox_Shaft", motor, new Vector3(-0.76f, 1.05f, 0.1f), new Vector3(0.06f, 0.06f, 0.06f), "PSX_Prism8", grey, Quaternion.Euler(0f, 0f, 90f));
        Part("Gearbox_Pulley", motor, new Vector3(-0.68f, 1.05f, 0.1f), new Vector3(0.3f, 0.03f, 0.3f), "PSX_Prism8", brass, Quaternion.Euler(0f, 0f, 90f));
        // Correia no plano das duas polias (x = -0.68), pelas tangentes.
        Belt(motor, new Vector3(-0.68f, 0.5f, 0.19f), new Vector3(-0.68f, 1.05f, 0.24f), rubber);
        Belt(motor, new Vector3(-0.68f, 0.5f, -0.19f), new Vector3(-0.68f, 1.05f, -0.04f), rubber);

        // Painel de comando num poste, na frente do motor.
        Part("Panel_Post", motor, new Vector3(0.62f, 0.5f, -0.62f), new Vector3(0.08f, 0.5f, 0.08f), "PSX_Prism6", grey);
        var panel = Part("Panel_Box", motor, new Vector3(0.62f, 1.2f, -0.62f), new Vector3(0.4f, 0.5f, 0.18f), "PSX_Box", grey);
        panel.gameObject.AddComponent<BoxCollider>();
        const float face = -0.62f - 0.09f;
        Part("Switch_Plate", motor, new Vector3(0.62f, 1.15f, face - 0.01f), new Vector3(0.12f, 0.2f, 0.02f), "PSX_Box", rubber);

        var handle = New("Switch_Handle", motor, new Vector3(0.62f, 1.15f, face - 0.02f));
        handle.localRotation = Quaternion.Euler(-35f, 0f, 0f);   // desligado: para cima e para fora
        Part("Switch_Stick", handle, new Vector3(0f, 0.07f, 0f), new Vector3(0.035f, 0.07f, 0.035f), "PSX_Prism6", grey);
        Part("Switch_Knob", handle, new Vector3(0f, 0.15f, 0f), new Vector3(0.06f, 0.06f, 0.06f), "PSX_Box", red);
        var hcol = handle.gameObject.AddComponent<BoxCollider>();
        hcol.center = new Vector3(0f, 0.09f, 0f);
        hcol.size = new Vector3(0.08f, 0.2f, 0.08f);

        var lamp = Part("Lamp", motor, new Vector3(0.62f, 1.38f, face - 0.01f), new Vector3(0.07f, 0.015f, 0.07f), "PSX_Prism8", M("LedVerde_Apagado"), Quaternion.Euler(90f, 0f, 0f));

        // Camera de frente para o painel, um pouco acima.
        var view = New("ViewPoint", motor, new Vector3(0.62f, 1.3f, -1.25f));
        view.localRotation = Quaternion.Euler(10f, 0f, 0f);

        var mg = motor.gameObject.AddComponent<FerrisWheelMotorMinigame>();
        var so = new SerializedObject(mg);
        so.FindProperty("prompt").stringValue = "Consertar o motor";
        so.FindProperty("viewPoint").objectReferenceValue = view;
        so.FindProperty("switchHandle").objectReferenceValue = handle;
        so.FindProperty("lamp").objectReferenceValue = lamp.GetComponent<Renderer>();
        so.FindProperty("lampOff").objectReferenceValue = M("LedVerde_Apagado");
        so.FindProperty("lampOn").objectReferenceValue = M("LedVerde");
        so.ApplyModifiedPropertiesWithoutUndo();
        return mg;
    }

    static void Belt(Transform parent, Vector3 a, Vector3 b, Material mat)
    {
        var t = Part("Belt", parent, (a + b) / 2f, new Vector3(0.02f, Vector3.Distance(a, b), 0.05f), "PSX_Box", mat);
        t.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
    }

    // ------------------------------------------------------------------ util

    static T Load<T>(string path) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) throw new System.IO.FileNotFoundException(path);
        return a;
    }

    static Transform New(string name, Transform parent, Vector3 local)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = local;
        return t;
    }

    /// <summary>Uma peca PSX: malha do kit (mesmas medidas das primitivas do Unity) com um recorte do atlas, sem collider.</summary>
    static Transform Part(string name, Transform parent, Vector3 local, Vector3 scale, string mesh, Material mat, Quaternion? rot = null)
    {
        var t = New(name, parent, local);
        t.localScale = scale;
        if (rot.HasValue) t.localRotation = rot.Value;
        t.gameObject.AddComponent<MeshFilter>().sharedMesh = Load<Mesh>(Meshes + mesh + ".asset");
        t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return t;
    }
}
