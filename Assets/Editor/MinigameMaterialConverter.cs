using UnityEditor;
using UnityEngine;

/// <summary>
/// Passa os materiais Lit opacos dos minigames para o Jamlloween/Flashlight Fog Surface.
/// Motivo: o mundo deste projeto e iluminado por shader (lanterna, olhos do Zatan, luminaria de trabalho). O Lit comum
/// recebe so a luz ambiente da noite e fica preto, e luzes reais nao chegam nele. Mantem a cor e a emissao.
/// Menu: Circus > Convert Minigame Materials To Fog Surface.
/// </summary>
public static class MinigameMaterialConverter
{
    [MenuItem("Circus/Convert Minigame Materials To Fog Surface")]
    public static void Convert()
    {
        if (!PSXLegacyGuard.Confirm("Convert Minigame Materials To Fog Surface")) return;
        var target = Shader.Find("Jamlloween/Flashlight Fog Surface");
        if (target == null) { Debug.LogError("[MinigameMaterialConverter] shader Jamlloween/Flashlight Fog Surface nao encontrado"); return; }

        int converted = 0, skipped = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials/Minigames" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null || m.shader == null || m.shader.name != "Universal Render Pipeline/Lit") { skipped++; continue; }
            if (m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f) { skipped++; continue; }   // transparente: fica no Lit

            Color color = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
            Color emission = m.IsKeywordEnabled("_EMISSION") && m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
            m.shader = target;
            m.SetColor("_BaseColor", color);
            m.SetColor("_EmissionColor", emission);
            m.shaderKeywords = new string[0];
            EditorUtility.SetDirty(m);
            converted++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[MinigameMaterialConverter] {converted} material(is) convertido(s), {skipped} ignorado(s)");
    }
}
