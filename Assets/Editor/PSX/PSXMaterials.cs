using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Materiais dos minigames no padrao do PSX Carnival Kit.
///  - Tools > PSX > Criar biblioteca de materiais do atlas: um material PSX Unlit por recorte do T_atlas, em
///    Materials/Minigames/Atlas. Os recortes foram tirados dos proprios props do kit (gerador, quadro de forca,
///    galao, tambor) e da fileira de amostras de 16 px (y = 768) do atlas.
///  - Tools > PSX > Migrar materiais dos minigames para PSX Unlit: troca o Flashlight Fog Surface pelo shader do kit,
///    mantendo a cor (a emissao vira cor clara, sem HDR).
/// </summary>
public static class PSXMaterials
{
    public const string ShaderName = "PSXCarnivalKit/PSX Unlit";
    public const string AtlasPath = "Assets/PSXCarnivalKit/Textures/T_atlas.png";
    public const string LibraryFolder = "Assets/Materials/Minigames/Atlas";
    const float AtlasSize = 2048f;
    const float OffTint = 0.35f;   // lampada/LED apagado: o mesmo recorte, escurecido

    /// <summary>Recortes em pixels do atlas, origem no canto de cima (x, y, largura, altura).</summary>
    public static readonly (string name, RectInt rect, bool hasOffState)[] Swatches =
    {
        ("AcoEscuro",        new RectInt( 736, 736, 32, 32), false),   // generator_trailer, chassi
        ("MetalCinza",       new RectInt( 800, 736, 32, 32), false),   // ride_panel, poste
        ("MetalGalvanizado", new RectInt(1568, 704, 32, 32), false),   // trash_can
        ("AcoClaro",         new RectInt(1568, 768, 16, 16), false),   // amostra 16 px
        ("Ferrugem",         new RectInt( 192, 640, 64, 64), false),   // oil_drum_rusted
        ("PinturaVerde",     new RectInt(1920, 448, 64, 64), false),   // power_panel, lateral
        ("AmareloGerador",   new RectInt(1440, 736, 32, 32), false),   // generator_trailer_unit
        ("VermelhoGalao",    new RectInt(1504, 704, 24, 32), false),   // jerry_can
        ("Madeira",          new RectInt(1152, 640, 64, 64), false),   // table_pine
        ("Latao",            new RectInt(1632, 768, 16, 16), false),
        ("BorrachaPreta",    new RectInt(1584, 768, 16, 16), false),
        ("Papel",            new RectInt(1600, 768, 16, 16), false),
        ("Lampada",          new RectInt(1664, 768, 16, 16), true),    // lamp_carnival_bulbs
        ("LedVermelho",      new RectInt(1888, 768, 16, 16), true),
        ("LedVerde",         new RectInt(1840, 768, 16, 16), true),
        ("LedAmbar",         new RectInt(1920, 768, 16, 16), true),
    };

    [MenuItem("Tools/PSX/Criar biblioteca de materiais do atlas")]
    public static void CreateLibrary()
    {
        var shader = Shader.Find(ShaderName);
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        if (shader == null || atlas == null) { Debug.LogError("[PSXMaterials] shader ou atlas do kit nao encontrado"); return; }
        EnsureFolder(LibraryFolder);

        int count = 0;
        foreach (var (name, rect, hasOff) in Swatches)
        {
            Write($"M_Atlas_{name}", shader, atlas, rect, Color.white); count++;
            if (hasOff) { Write($"M_Atlas_{name}_Apagado", shader, atlas, rect, new Color(OffTint, OffTint, OffTint, 1f)); count++; }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[PSXMaterials] {count} materiais em {LibraryFolder}");
    }

    static void Write(string name, Shader shader, Texture2D atlas, RectInt r, Color tint)
    {
        string path = $"{LibraryFolder}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        PointAtAtlas(m, shader, atlas, r, tint);
    }

    /// <summary>
    /// Faz um material existente usar um recorte da biblioteca, mantendo o asset (e todas as referencias a ele).
    /// Usado nos materiais que scripts trocam ou referenciam, como as lampadas acesa/apagada.
    /// </summary>
    public static void ApplySwatch(Material m, string swatch, Color tint)
    {
        var shader = Shader.Find(ShaderName);
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        foreach (var (name, rect, _) in Swatches)
            if (name == swatch) { PointAtAtlas(m, shader, atlas, rect, tint); return; }
        throw new System.ArgumentException($"recorte {swatch} nao existe em PSXMaterials.Swatches");
    }

    /// <summary>Um material da biblioteca, pelo nome do recorte (ex.: "AcoEscuro").</summary>
    public static Material Library(string swatch) => AssetDatabase.LoadAssetAtPath<Material>($"{LibraryFolder}/M_Atlas_{swatch}.mat");

    static void PointAtAtlas(Material m, Shader shader, Texture2D atlas, RectInt r, Color tint)
    {
        m.shader = shader;
        m.SetTexture("_MainTex", atlas);
        // Meio pixel para dentro de cada borda: com filtro point, a borda exata do UV pode pegar o vizinho do atlas.
        const float inset = 0.5f;
        m.SetTextureScale("_MainTex", new Vector2((r.width - 2 * inset) / AtlasSize, (r.height - 2 * inset) / AtlasSize));
        m.SetTextureOffset("_MainTex", new Vector2((r.x + inset) / AtlasSize, 1f - (r.y + r.height - inset) / AtlasSize));
        SetKitDefaults(m, tint);
        EditorUtility.SetDirty(m);
    }

    /// <summary>Material com textura propria (recorte principal de um minigame), no PSX Unlit.</summary>
    public static void ApplyTexture(Material m, Texture2D tex, Color tint)
    {
        m.shader = Shader.Find(ShaderName);
        m.SetTexture("_MainTex", tex);
        m.SetTextureScale("_MainTex", Vector2.one);
        m.SetTextureOffset("_MainTex", Vector2.zero);
        SetKitDefaults(m, tint);
        EditorUtility.SetDirty(m);
    }

    /// <summary>Import no padrao do kit: filtro point, sem mipmap, sem compressao.</summary>
    public static Texture2D ImportKitTexture(string path)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Default;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.sRGBTexture = true;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void SetKitDefaults(Material m, Color tint)
    {
        m.SetColor("_Color", tint);
        m.SetFloat("_SnapRows", 240f);
        m.SetFloat("_Affine", 0f);
        m.SetFloat("_Cutoff", 0f);
    }

    [MenuItem("Tools/PSX/Migrar materiais dos minigames para PSX Unlit")]
    public static void MigrateMenu()
    {
        if (!EditorUtility.DisplayDialog("PSX", "Trocar o shader Flashlight Fog Surface por PSX Unlit em Assets/Materials/Minigames?", "Migrar", "Cancelar")) return;
        Debug.Log("[PSXMaterials] " + string.Join("\n", Migrate()));
    }

    /// <summary>Migra os materiais opacos de Materials/Minigames. Devolve uma linha por material migrado.</summary>
    public static List<string> Migrate()
    {
        var shader = Shader.Find(ShaderName);
        var log = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials/Minigames" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null || m.shader == null || m.shader.name != "Jamlloween/Flashlight Fog Surface") continue;
            Color c = m.GetColor("_BaseColor");
            Color e = m.GetColor("_EmissionColor");
            var flat = LitColor(c, e);
            m.shader = shader;
            m.SetTexture("_MainTex", null);
            SetKitDefaults(m, flat);
            EditorUtility.SetDirty(m);
            log.Add($"{path}: #{ColorUtility.ToHtmlStringRGB(c)} + emissao {e.maxColorComponent:0.##} -> #{ColorUtility.ToHtmlStringRGB(flat)}");
        }
        AssetDatabase.SaveAssets();
        return log;
    }

    /// <summary>
    /// Cor sem HDR (guideline, regra 6). Emissao forte (lampada acesa, >= 1): a propria cor clareada ate o canal mais
    /// forte chegar a 1, mantendo o tom. Emissao fraca (brilho de liquido, ponteiro): somada a cor, ate 1.
    /// </summary>
    public static Color LitColor(Color c, Color e)
    {
        if (e.maxColorComponent >= 1f && c.maxColorComponent > 0f) { var k = c / c.maxColorComponent; k.a = 1f; return k; }
        return new Color(Mathf.Clamp01(c.r + e.r), Mathf.Clamp01(c.g + e.g), Mathf.Clamp01(c.b + e.b), 1f);
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
    }
}
