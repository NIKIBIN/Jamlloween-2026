using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Fase 3 do plano: RadioMinigame no padrao do PSX Carnival Kit. Pode rodar de novo sem duplicar nada.
///  - Texturas principais (Materials/Minigames/Radio, geradas por Tools/psx_textures/radio.py): frente do radio
///    (grade, escala 88-108 e numeros pintados), bilhete (papel, fitas, FREQ), botao do dial (ponteiro pintado) e
///    tampa da pilha (garras pintadas; o radio vira 180 graus em z para mostra-la, entao o PNG vem girado).
///  - Saem do prefab os objetos que viraram pintura: 6 fendas da grade, 21 tracinhos, 4 numeros TMP, ponteiro do
///    dial, 2 fitas, titulo do bilhete e 3 garras da tampa. Nenhum e referenciado por script.
///  - A frequencia do bilhete (noteLabel, RadioMinigame.cs:96) continua TMP, agora com a fonte OSD_Pixel.
///  - A moldura de tras ganha um material proprio (RadioCase): o RadioBody agora tem a frente pintada.
///  - LED (RadioMinigame.cs:98, :164) vira caixinha; os materiais aceso/apagado continuam os mesmos assets.
/// Menu: Tools > PSX > Fase 3 - Radio.
/// </summary>
public static class PSXPhaseRadio
{
    const string PrefabPath = "Assets/Prefabs/Minigames/RadioMinigame.prefab";
    const string Dir = "Assets/Materials/Minigames/Radio";

    static readonly string[] PaintedOver =
    {
        "Radio/Grille_0", "Radio/Grille_1", "Radio/Grille_2", "Radio/Grille_3", "Radio/Grille_4", "Radio/Grille_5",
        "Radio/Label_90", "Radio/Label_95", "Radio/Label_100", "Radio/Label_105",
        "Radio/Dial/Pointer", "Radio/Note/Tape", "Radio/Note/NoteTitle",
        "Radio/Cover/Grip_0", "Radio/Cover/Grip_1", "Radio/Cover/Grip_2",
    };

    [MenuItem("Tools/PSX/Fase 3 - Radio")]
    static void Menu()
    {
        if (!EditorUtility.DisplayDialog("PSX", "Converter o RadioMinigame (texturas, materiais e PSXBake)?", "Converter", "Cancelar")) return;
        Debug.Log("[PSX Fase 3] " + Run());
    }

    public static string Run()
    {
        Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/{n}.mat");
        Texture2D T(string n) => PSXMaterials.ImportKitTexture($"{Dir}/{n}.png");

        var caseMat = M("RadioCase");
        if (caseMat == null) { caseMat = new Material(Shader.Find(PSXMaterials.ShaderName)); AssetDatabase.CreateAsset(caseMat, $"{Dir}/RadioCase.mat"); }
        PSXMaterials.ApplySwatch(caseMat, "Papel", new Color(0.82f, 0.86f, 0.66f));
        PSXMaterials.ApplyTexture(M("RadioBody"), T("T_RadioFront"), Color.white);
        PSXMaterials.ApplyTexture(M("RadioNote"), T("T_RadioNote"), Color.white);
        PSXMaterials.ApplyTexture(M("RadioKnob"), T("T_RadioKnob"), Color.white);
        PSXMaterials.ApplyTexture(M("RadioCover"), T("T_RadioCover"), Color.white);
        PSXMaterials.ApplySwatch(M("RadioDark"), "AcoEscuro", Color.white);
        PSXMaterials.ApplySwatch(M("RadioMetal"), "AcoClaro", Color.white);
        PSXMaterials.ApplySwatch(M("RadioLedOn"), "LedVerde", Color.white);
        PSXMaterials.ApplySwatch(M("RadioLedOff"), "LedVerde", new Color(0.3f, 0.3f, 0.3f));
        PSXMaterials.ApplySwatch(M("BatteryNewBody"), "LedAmbar", new Color(1f, 0.62f, 0.3f));
        PSXMaterials.ApplySwatch(M("BatteryNewBand"), "BorrachaPreta", Color.white);
        PSXMaterials.ApplySwatch(M("WorkMat"), "PinturaVerde", new Color(0.55f, 0.6f, 0.55f));
        AssetDatabase.SaveAssets();

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        string log;
        try
        {
            int removed = 0;
            foreach (var path in PaintedOver)
                for (var t = root.transform.Find(path); t != null; t = root.transform.Find(path)) { Object.DestroyImmediate(t.gameObject); removed++; }
            foreach (var t in root.transform.Find("Radio").GetComponentsInChildren<Transform>(true))
                if (t != null && t.name.StartsWith("Tick_")) { Object.DestroyImmediate(t.gameObject); removed++; }

            foreach (Transform t in root.transform.Find("Radio"))
                if (t.name.StartsWith("BackFrame")) t.GetComponent<MeshRenderer>().sharedMaterial = caseMat;

            // As tres cenas sobrescrevem o WorkMat para y = 0 (o prefab tem -0.003). O bake funde o WorkMat na malha
            // da raiz e o override fica sem alvo: sem isto o tapete desce 3 mm e encosta no tampo da mesa (z-fighting).
            var workMat = root.transform.Find("WorkMat");
            if (workMat != null) workMat.localPosition = new Vector3(workMat.localPosition.x, 0f, workMat.localPosition.z);
            else
            {
                var baked = root.transform.Find("RadioMinigame_mesh");   // prefab ja convertido: sobe a malha fundida
                if (baked != null) baked.localPosition = new Vector3(0f, 0.003f, 0f);
            }

            var led = root.transform.Find("Radio/Led");
            var mf = led.GetComponent<MeshFilter>();
            if (mf.sharedMesh != null && mf.sharedMesh.name == "Sphere")
            {
                mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                led.localScale = new Vector3(0.012f, 0.006f, 0.012f);
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PSXPixelFont.OutputPath);
            var freq = root.transform.Find("Radio/Note/NoteFrequency").GetComponent<TMP_Text>();
            freq.font = font;
            freq.fontSharedMaterial = font.material;
            freq.color = new Color(0.27f, 0.24f, 0.27f);

            var report = PSXBake.BakeHierarchy(root, $"{PSXBake.MeshRoot}/RadioMinigame");
            report.prefab = PrefabPath;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            log = $"{removed} objetos viraram pintura. " + report;
        }
        finally { PSXBake.UnloadContents(root); }

        foreach (var unused in new[] { "RadioPanel", "RadioPointer", "RadioTape" })
            if (M(unused) != null) AssetDatabase.DeleteAsset($"{Dir}/{unused}.mat");
        AssetDatabase.SaveAssets();
        return log;
    }
}
