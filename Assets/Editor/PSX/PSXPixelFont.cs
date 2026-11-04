using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Fonte do TextMeshPro em bitmap (raster, sem SDF) com filtro point, para texto que muda em tempo de jogo
/// (ex.: a frequencia do bilhete do RadioMinigame). Texto fixo vai pintado na textura (guideline, regra 7).
/// Gerada a partir do OSD.ttf do projeto (DejaVu Sans Mono) em 10 px: as letras saem serrilhadas como as placas do atlas.
/// Para trocar por uma fonte pixel de verdade, aponte SourceFont para o .ttf dela e rode o menu de novo.
/// Menu: Tools > PSX > Criar fonte pixel (TMP).
/// </summary>
public static class PSXPixelFont
{
    public const string SourceFont = "Assets/Data/Resources/Fonts/OSD.ttf";
    public const string OutputPath = "Assets/Data/Fonts/OSD_Pixel.asset";
    const int SamplingSize = 10;
    const string Characters =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
        "ÁÀÂÃÉÊÍÓÔÕÚÇáàâãéêíóôõúç°";

    [MenuItem("Tools/PSX/Criar fonte pixel (TMP)")]
    public static string Create()
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFont);
        if (font == null) return $"fonte {SourceFont} nao encontrada";

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath) != null) AssetDatabase.DeleteAsset(OutputPath);

        var asset = TMP_FontAsset.CreateFontAsset(font, SamplingSize, 1, GlyphRenderMode.RASTER_HINTED, 256, 256,
            AtlasPopulationMode.Dynamic, false);
        asset.name = "OSD_Pixel";
        AssetDatabase.CreateAsset(asset, OutputPath);

        var tex = asset.atlasTexture;
        tex.name = "OSD_Pixel Atlas";
        AssetDatabase.AddObjectToAsset(tex, asset);
        var mat = asset.material;
        mat.name = "OSD_Pixel Material";
        AssetDatabase.AddObjectToAsset(mat, asset);

        asset.TryAddCharacters(Characters, out string missing);
        tex.filterMode = FilterMode.Point;
        tex.Apply(false, false);
        asset.atlasPopulationMode = AtlasPopulationMode.Static;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(OutputPath);
        return $"{OutputPath}: {asset.characterTable.Count} caracteres, atlas {tex.width}x{tex.height}, shader {mat.shader.name}" +
               (string.IsNullOrEmpty(missing) ? "" : $", faltando: {missing}");
    }
}
