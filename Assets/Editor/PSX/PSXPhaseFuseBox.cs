using UnityEditor;
using UnityEngine;

/// <summary>
/// Fase 2 do plano: FuseBoxMinigame no padrao do PSX Carnival Kit. Pode rodar de novo sem duplicar nada.
///  - Texturas principais (Materials/Minigames/FuseBox): tampa (T_FuseBoxCover, verde do power_panel com DANGER e
///    FUSES), painel interno (T_FuseBoxPanel, borda verde, MAIN FUSES e numeros 1-5 sobre os slots) e parafuso
///    (T_FuseScrew, com a fenda pintada). Geradas por Tools/psx_textures/fusebox.py.
///  - Os 4 HeadSlot (fenda em cubo de 6 mm, que o vertex snap fazia sumir) saem do prefab. Com isso o material Dark
///    volta a ter snap.
///  - Materiais que scripts trocam continuam sendo os mesmos assets, refeitos no lugar: LEDs (FuseBoxMinigame.cs:296,
///    :343), fusivel queimado (:85). FuseNew e FuseCap tambem pintam o fusivel solto da Zatan_Test.
///  - O LED de status vira uma caixinha, como o _lamp dos props do kit.
/// Menu: Tools > PSX > Fase 2 - Quadro de fusiveis.
/// </summary>
public static class PSXPhaseFuseBox
{
    const string PrefabPath = "Assets/Prefabs/Minigames/FuseBoxMinigame.prefab";
    const string Dir = "Assets/Materials/Minigames/FuseBox";

    [MenuItem("Tools/PSX/Fase 2 - Quadro de fusiveis")]
    static void Menu()
    {
        if (!EditorUtility.DisplayDialog("PSX", "Converter o FuseBoxMinigame (texturas, materiais e PSXBake)?", "Converter", "Cancelar")) return;
        Debug.Log("[PSX Fase 2] " + Run());
    }

    public static string Run()
    {
        Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/{n}.mat");
        Texture2D T(string n) => PSXMaterials.ImportKitTexture($"{Dir}/{n}.png");

        PSXMaterials.ApplyTexture(M("Cover"), T("T_FuseBoxCover"), Color.white);
        PSXMaterials.ApplyTexture(M("Housing"), T("T_FuseBoxPanel"), Color.white);
        PSXMaterials.ApplyTexture(M("Screw"), T("T_FuseScrew"), Color.white);
        PSXMaterials.ApplySwatch(M("Dark"), "AcoEscuro", Color.white);
        PSXMaterials.ApplySwatch(M("FuseGood"), "LedAmbar", new Color(0.95f, 0.62f, 0.35f));
        PSXMaterials.ApplySwatch(M("FuseNew"), "LedAmbar", new Color(1.1f, 0.95f, 0.6f));
        PSXMaterials.ApplySwatch(M("FuseBurnt"), "BorrachaPreta", new Color(0.7f, 0.6f, 0.55f));
        PSXMaterials.ApplySwatch(M("FuseCap"), "Latao", Color.white);
        PSXMaterials.ApplySwatch(M("LedRed"), "LedVermelho", Color.white);
        PSXMaterials.ApplySwatch(M("LedAmber"), "LedAmbar", Color.white);
        PSXMaterials.ApplySwatch(M("LedGreen"), "LedVerde", Color.white);
        PSXMaterials.ApplySwatch(M("LeverArm"), "AcoClaro", Color.white);
        PSXMaterials.ApplySwatch(M("LeverKnob"), "VermelhoGalao", Color.white);
        AssetDatabase.SaveAssets();

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        string log;
        try
        {
            for (int i = 0; i < 4; i++)
            {
                var slot = root.transform.Find($"Screw_{i}/HeadSlot");
                if (slot != null) Object.DestroyImmediate(slot.gameObject);
            }
            SetMaterial(root, "Shelf", PSXMaterials.Library("PinturaVerde"));
            SetMaterial(root, "LeverPlate", PSXMaterials.Library("PinturaVerde"));
            SetMaterial(root, "LeverHub", PSXMaterials.Library("AcoClaro"));

            // LED de status: caixinha no lugar da esfera
            var led = root.transform.Find("StatusLed");
            var mf = led != null ? led.GetComponent<MeshFilter>() : null;
            if (mf != null && mf.sharedMesh != null && mf.sharedMesh.name == "Sphere")
            {
                mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                led.localScale = new Vector3(0.04f, 0.028f, 0.02f);
            }

            // Base da alavanca separada do corpo, para mexer isolada no editor: placa, trilho, eixo e a propria Lever
            // dentro de LeverBase, com o pivo no eixo. A Lever gira por localRotation a partir da rotacao inicial
            // (FuseBoxMinigame.cs:81, :323), entao trocar o pai sem rotacao nao muda o comportamento.
            var leverBase = root.transform.Find("LeverBase");
            if (leverBase == null)
            {
                var hub = root.transform.Find("LeverHub");
                leverBase = new GameObject("LeverBase").transform;
                leverBase.SetParent(root.transform, false);
                leverBase.localPosition = hub != null ? hub.localPosition : new Vector3(0.62f, 0f, -0.04f);
                foreach (var n in new[] { "LeverPlate", "LeverTrack", "LeverHub", "Lever" })
                {
                    var t = root.transform.Find(n);
                    if (t != null) t.SetParent(leverBase, true);
                }
            }

            var report = PSXBake.BakeHierarchy(root, $"{PSXBake.MeshRoot}/FuseBoxMinigame", leverBase.gameObject);
            report.prefab = PrefabPath;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            log = report.ToString();
        }
        finally { PSXBake.UnloadContents(root); }
        AssetDatabase.SaveAssets();
        return log;
    }

    static void SetMaterial(GameObject root, string path, Material m)
    {
        var t = root.transform.Find(path);
        var r = t != null ? t.GetComponent<MeshRenderer>() : null;
        if (r != null && m != null) r.sharedMaterial = m;
    }
}
