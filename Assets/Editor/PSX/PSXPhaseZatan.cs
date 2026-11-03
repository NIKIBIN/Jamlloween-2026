using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Fase 4 do plano: ZatanRepairPanel no padrao do PSX Carnival Kit. Pode rodar de novo sem duplicar nada.
///  - Texturas principais (Materials/Minigames/ZatanRepair, geradas por Tools/psx_textures/zatan.py): mostrador
///    (escala 0-180, numeros, zero impresso riscado e zero a mao), manometro (arco com zona vermelha), placa do
///    interruptor (ON/OFF), painel de fundo e tampa (ZATAN, MERRIMAN). Piche e fumaca em 64 e 32 px, filtro point.
///  - Saem do prefab os objetos que viraram pintura: 19 tracos, 5 numeros, zero impresso, 2 riscos e zero a mao do
///    mostrador; 9 tracos do manometro; ON e OFF. Nenhum e referenciado por script.
///  - Paleta: os azuis, verde-agua e laranja saturados viram metal pintado, latao e ferrugem do atlas; vermelho so
///    na valvula, nos ponteiros e no anel da mangueira.
///  - Lampadas (ZatanRepairMinigame.cs:602), piche (ZatanScrubArea, shader transparente com _BaseColor) e particulas
///    continuam nos mesmos assets.
///  - O ponteiro do manometro passa de 4 para 6 mm de largura: com 4 mm ele some no vertex snap.
/// Menu: Tools > PSX > Fase 4 - Painel do Zatan.
/// </summary>
public static class PSXPhaseZatan
{
    const string PrefabPath = "Assets/Prefabs/Minigames/ZatanRepairPanel.prefab";
    const string Dir = "Assets/Materials/Minigames/ZatanRepair";

    // ZatanDialFace: material antigo sem uso que, reaproveitado, nao desenhava a face no editor (copia identica
    // desenhava). O mostrador usa um material novo, ZatanDial.
    static readonly string[] Unused = { "ZatanDialFace", "ZatanBrass", "ZatanHousing", "ZatanKnob", "ZatanNeedle", "ZatanSteel", "ZatanTarDrip", "ZatanTarInterior" };

    [MenuItem("Tools/PSX/Fase 4 - Painel do Zatan")]
    static void Menu()
    {
        if (!EditorUtility.DisplayDialog("PSX", "Converter o ZatanRepairPanel (texturas, materiais e PSXBake)?", "Converter", "Cancelar")) return;
        Debug.Log("[PSX Fase 4] " + Run());
    }

    public static string Run()
    {
        Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/{n}.mat");
        Texture2D T(string n) => PSXMaterials.ImportKitTexture($"{Dir}/{n}.png");

        Material New(string n)
        {
            var m = M(n);
            if (m == null) { m = new Material(Shader.Find(PSXMaterials.ShaderName)); AssetDatabase.CreateAsset(m, $"{Dir}/{n}.mat"); }
            return m;
        }
        var switchMat = New("ZatanSwitchPlate");
        var dialMat = New("ZatanDial");

        PSXMaterials.ApplyTexture(dialMat, T("T_ZatanDial"), Color.white);
        PSXMaterials.ApplyTexture(M("ZatanWhite"), T("T_ZatanGauge"), Color.white);
        PSXMaterials.ApplyTexture(switchMat, T("T_ZatanSwitch"), Color.white);
        PSXMaterials.ApplyTexture(M("ZatanPaint"), T("T_ZatanBackplate"), Color.white);
        PSXMaterials.ApplyTexture(M("ZatanCover"), T("T_ZatanCover"), Color.white);

        PSXMaterials.ApplySwatch(M("ZatanBezel"), "PinturaVerde", new Color(0.6f, 0.65f, 0.6f));
        PSXMaterials.ApplySwatch(M("ZatanPlate"), "AcoEscuro", Color.white);
        PSXMaterials.ApplySwatch(M("ZatanDarkSteel"), "AcoEscuro", Color.white);
        PSXMaterials.ApplySwatch(M("ZatanChrome"), "AcoClaro", Color.white);
        PSXMaterials.ApplySwatch(M("ZatanNavy"), "Latao", Color.white);           // aro do mostrador
        PSXMaterials.ApplySwatch(M("ZatanOrange"), "Latao", Color.white);         // engrenagem e cabeca do pistao
        PSXMaterials.ApplySwatch(M("ZatanBlue"), "Ferrugem", Color.white);        // cano
        PSXMaterials.ApplySwatch(M("ZatanTeal"), "PinturaVerde", Color.white);    // cilindro do pistao
        PSXMaterials.ApplySwatch(M("ZatanYellow"), "AmareloGerador", Color.white);
        PSXMaterials.ApplySwatch(M("ZatanRed"), "VermelhoGalao", Color.white);
        PSXMaterials.ApplySwatch(M("ZatanInk"), "BorrachaPreta", Color.white);
        PSXMaterials.ApplySwatch(M("ZatanCream"), "Madeira", Color.white);        // punho da manivela
        PSXMaterials.ApplySwatch(M("ZatanCloth"), "Papel", new Color(1f, 0.97f, 0.9f));
        PSXMaterials.ApplySwatch(M("ZatanClothShade"), "Papel", new Color(0.78f, 0.75f, 0.68f));
        PSXMaterials.ApplySwatch(M("ZatanLampOff"), "Lampada", new Color(0.3f, 0.28f, 0.25f));
        PSXMaterials.ApplySwatch(M("ZatanLampAmber"), "LedAmbar", new Color(1.15f, 0.75f, 0.3f));
        PSXMaterials.ApplySwatch(M("ZatanLampGreen"), "LedVerde", Color.white);
        PSXMaterials.ApplySwatch(M("ZatanLampRed"), "LedVermelho", Color.white);
        PSXMaterials.ApplySwatch(M("ZatanHose"), "BorrachaPreta", Color.white);   // LineRenderer da mangueira
        PSXMaterials.ImportKitTexture($"{Dir}/TarBlob.png");
        PSXMaterials.ImportKitTexture($"{Dir}/SmokePuff.png");
        AssetDatabase.SaveAssets();

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        string log;
        try
        {
            int removed = 0;
            void RemoveWhere(string parent, System.Func<Transform, bool> pick)
            {
                var p = root.transform.Find(parent);
                if (p == null) return;
                foreach (var t in p.Cast<Transform>().Where(pick).ToList()) { Object.DestroyImmediate(t.gameObject); removed++; }
            }
            string[] dialPainted = { "PrintedZero", "Strike1", "Strike2", "HandZero" };
            RemoveWhere("Dial/Visual", t => t.name.StartsWith("Tick") || t.name.StartsWith("Num") || dialPainted.Contains(t.name));
            RemoveWhere("Gauge/Visual", t => t.name.StartsWith("GTick"));
            RemoveWhere("SwitchPlate/Visual", t => t.name == "On" || t.name == "Off");

            SetMaterial(root, "Dial/Visual/Face", dialMat);
            SetMaterial(root, "SwitchPlate/Visual/Plate", switchMat);

            // A frente do aro (z -0.012, 3 cm de espessura) e a da face (z -0.024, 6 mm) caem no mesmo plano, -0.027:
            // z-fighting, que o snap deixa visivel. A face vai 2 mm para a frente.
            var face = root.transform.Find("Dial/Visual/Face");
            if (face != null && face.localPosition.z > -0.026f) face.localPosition = new Vector3(face.localPosition.x, face.localPosition.y, -0.026f);

            var needle = root.transform.Find("GaugeNeedle/Visual/Needle");
            if (needle != null && needle.localScale.y < 0.006f) needle.localScale = new Vector3(needle.localScale.x, 0.006f, needle.localScale.z);

            var report = PSXBake.BakeHierarchy(root, $"{PSXBake.MeshRoot}/ZatanRepairPanel");
            report.prefab = PrefabPath;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            log = $"{removed} objetos viraram pintura. " + report;
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        foreach (var n in Unused)
            if (M(n) != null) AssetDatabase.DeleteAsset($"{Dir}/{n}.mat");
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
