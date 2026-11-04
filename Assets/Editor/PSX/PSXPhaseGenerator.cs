using UnityEditor;
using UnityEngine;

/// <summary>
/// Fase 1 do plano: GeneratorFuelMinigame no padrao do PSX Carnival Kit. Pode rodar de novo sem duplicar nada.
///  - Mostrador do medidor: textura principal T_GeneratorGauge (64 px, marcas vazio/cheio pintadas). Os cubos
///    MarkEmpty e MarkFull saem do prefab. A tampa do prisma mostra a textura girada 180 graus; o PNG ja vem girado.
///    Gerado por Tools/psx_textures/gauge.py.
///  - Materiais que o script troca ou que ficam em objetos protegidos sao refeitos no lugar (o asset e as
///    referencias continuam): lampada piloto, ponteiro, jato, espuma, cordao, tampa, alca.
///  - Pecas paradas usam a biblioteca: bocal galvanizado, carcaca da partida no amarelo do gerador, resto aco escuro.
///  - Depois roda o PSXBake.
/// Menu: Tools > PSX > Fase 1 - Gerador.
/// </summary>
public static class PSXPhaseGenerator
{
    const string PrefabPath = "Assets/Prefabs/Minigames/GeneratorFuelMinigame.prefab";
    const string Dir = "Assets/Materials/Minigames/Generator";

    [MenuItem("Tools/PSX/Fase 1 - Gerador")]
    static void Menu()
    {
        if (!EditorUtility.DisplayDialog("PSX", "Converter o GeneratorFuelMinigame (materiais, mostrador e PSXBake)?", "Converter", "Cancelar")) return;
        Debug.Log("[PSX Fase 1] " + Run());
    }

    public static string Run()
    {
        Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/{n}.mat");

        // Materiais refeitos no lugar
        var gauge = PSXMaterials.ImportKitTexture($"{Dir}/T_GeneratorGauge.png");
        PSXMaterials.ApplyTexture(M("GenDial"), gauge, Color.white);
        PSXMaterials.ApplySwatch(M("GenMetal"), "AcoEscuro", Color.white);
        PSXMaterials.ApplySwatch(M("GenNeedle"), "VermelhoGalao", Color.white);
        PSXMaterials.ApplySwatch(M("GenCap"), "BorrachaPreta", Color.white);
        PSXMaterials.ApplySwatch(M("GenHandle"), "BorrachaPreta", Color.white);
        PSXMaterials.ApplySwatch(M("GenCord"), "BorrachaPreta", Color.white);
        PSXMaterials.ApplySwatch(M("GenLampOn"), "LedAmbar", new Color(1.15f, 0.95f, 0.6f));
        PSXMaterials.ApplySwatch(M("GenLampOff"), "LedAmbar", new Color(0.35f, 0.3f, 0.25f));
        PSXMaterials.ApplySwatch(M("GenFuel"), "LedAmbar", new Color(1f, 0.9f, 0.6f));
        PSXMaterials.ApplySwatch(M("GenFoam"), "Papel", new Color(1.15f, 1.15f, 1.05f));
        AssetDatabase.SaveAssets();

        // Prefab
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        string log;
        try
        {
            foreach (var mark in new[] { "Gauge/MarkEmpty", "Gauge/MarkFull" })
            {
                var t = root.transform.Find(mark);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }
            SetMaterial(root, "Neck", PSXMaterials.Library("MetalGalvanizado"));
            SetMaterial(root, "StarterHousing", PSXMaterials.Library("AmareloGerador"));

            var report = PSXBake.BakeHierarchy(root, $"{PSXBake.MeshRoot}/GeneratorFuelMinigame");
            report.prefab = PrefabPath;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            log = report.ToString();
        }
        finally { PSXBake.UnloadContents(root); }

        // GenGreen so servia ao MarkFull
        if (AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/GenGreen.mat") != null) AssetDatabase.DeleteAsset($"{Dir}/GenGreen.mat");
        AssetDatabase.SaveAssets();
        return log;
    }

    static void SetMaterial(GameObject root, string path, Material m)
    {
        var t = root.transform.Find(path);
        if (t == null || m == null) return;
        var r = t.GetComponent<MeshRenderer>();
        if (r != null) r.sharedMaterial = m;
    }
}
