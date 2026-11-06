using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the trailer's music radio: a small low-poly 1950s tabletop radio (wood case, cloth grille, tuning dial, two knobs,
/// carry handle) as one mesh on a 64x64 pixel atlas with the project's PSX Unlit shader.
/// Output: Assets/Prefabs/Props/RadioMusica/ (texture, material, mesh, PF_RadioMusica prefab). Pivot at the bottom centre,
/// front facing +Z. Run again to rebuild in place: assets keep their GUIDs, so scene instances update.
/// </summary>
public static class RadioMusicaBuilder
{
    const string Folder = "Assets/Prefabs/Props/RadioMusica";
    const string TexturePath = Folder + "/T_RadioMusica.png";
    const string MaterialPath = Folder + "/M_RadioMusica.mat";
    const string MeshPath = Folder + "/RadioMusica_mesh.asset";
    const string PrefabPath = Folder + "/PF_RadioMusica.prefab";
    const string ShaderName = "PSXCarnivalKit/PSX Unlit";

    const int Cells = 4, CellPixels = 16, Size = Cells * CellPixels;

    // Atlas cells (column, row from the bottom).
    static readonly Vector2Int Wood = new Vector2Int(0, 0);
    static readonly Vector2Int WoodDark = new Vector2Int(1, 0);
    static readonly Vector2Int Grille = new Vector2Int(2, 0);
    static readonly Vector2Int Dial = new Vector2Int(3, 0);
    static readonly Vector2Int Knob = new Vector2Int(0, 1);
    static readonly Vector2Int Metal = new Vector2Int(1, 1);

    [MenuItem("Jamlloween/Props/Build Radio Musica")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder);
        Texture2D texture = BuildTexture();
        Material material = BuildMaterial(texture);
        Mesh mesh = BuildMesh();
        BuildPrefab(mesh, material);
        AssetDatabase.SaveAssets();
        Debug.Log($"[RadioMusicaBuilder] {PrefabPath}: {mesh.vertexCount} verts, {mesh.triangles.Length / 3} tris.");
    }

    // ---------- texture ----------

    static Texture2D BuildTexture()
    {
        var pixels = new Color32[Size * Size];
        Paint(pixels, Wood, (x, y) =>
        {
            // Walnut: horizontal grain with a slow wobble.
            float grain = Mathf.Sin((y + Mathf.Sin(x * 0.7f) * 1.3f) * 1.9f) * 0.5f + 0.5f;
            return Color.Lerp(new Color(0.36f, 0.20f, 0.10f), new Color(0.52f, 0.31f, 0.15f), grain);
        });
        Paint(pixels, WoodDark, (x, y) => Color.Lerp(new Color(0.20f, 0.11f, 0.06f), new Color(0.27f, 0.15f, 0.08f), (x * 7 + y * 3) % 5 / 4f));
        Paint(pixels, Grille, (x, y) =>
        {
            // Cloth behind horizontal wooden slats, with a border.
            if (x == 0 || y == 0 || x == CellPixels - 1 || y == CellPixels - 1) return new Color(0.22f, 0.12f, 0.06f);
            if (y % 4 == 0) return new Color(0.45f, 0.27f, 0.13f);
            return (x + y) % 2 == 0 ? new Color(0.62f, 0.55f, 0.40f) : new Color(0.55f, 0.48f, 0.34f);
        });
        Paint(pixels, Dial, (x, y) =>
        {
            if (x == 0 || y == 0 || x == CellPixels - 1 || y == CellPixels - 1) return new Color(0.70f, 0.62f, 0.35f);   // brass bezel
            if (x == 10 && y > 2 && y < 13) return new Color(0.80f, 0.12f, 0.08f);                                      // needle
            if (y >= 9 && y <= 10 && x % 2 == 1) return new Color(0.25f, 0.18f, 0.10f);                                // tick marks
            if (y >= 5 && y <= 6 && x % 3 == 1) return new Color(0.30f, 0.22f, 0.12f);
            return new Color(0.93f, 0.84f, 0.58f);                                                                     // lit cream glass
        });
        Paint(pixels, Knob, (x, y) => x == 7 || x == 8 ? new Color(0.55f, 0.50f, 0.40f) : new Color(0.10f, 0.08f, 0.07f));
        Paint(pixels, Metal, (x, y) => Color.Lerp(new Color(0.55f, 0.55f, 0.52f), new Color(0.75f, 0.74f, 0.70f), (y % 4) / 3f));

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
    }

    static void Paint(Color32[] pixels, Vector2Int cell, System.Func<int, int, Color> colour)
    {
        for (int y = 0; y < CellPixels; y++)
        for (int x = 0; x < CellPixels; x++)
            pixels[(cell.y * CellPixels + y) * Size + cell.x * CellPixels + x] = colour(x, y);
    }

    // ---------- material ----------

    static Material BuildMaterial(Texture2D texture)
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null) throw new System.InvalidOperationException($"Shader '{ShaderName}' not found.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.shader = shader;
        material.SetTexture("_MainTex", texture);
        material.SetColor("_Color", Color.white);
        EditorUtility.SetDirty(material);
        return material;
    }

    // ---------- mesh ----------

    sealed class MeshBuilder
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector2> Uvs = new List<Vector2>();
        public readonly List<int> Triangles = new List<int>();

        /// <summary>A quad a-b-c-d (counter-clockwise seen from the front) mapped over a whole atlas cell.</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2Int cell)
        {
            int i = Vertices.Count;
            Vertices.Add(a); Vertices.Add(b); Vertices.Add(c); Vertices.Add(d);
            float inset = 0.5f / Size, u0 = (float)cell.x / Cells + inset, v0 = (float)cell.y / Cells + inset;
            float u1 = (cell.x + 1f) / Cells - inset, v1 = (cell.y + 1f) / Cells - inset;
            Uvs.Add(new Vector2(u0, v0)); Uvs.Add(new Vector2(u1, v0)); Uvs.Add(new Vector2(u1, v1)); Uvs.Add(new Vector2(u0, v1));
            Triangles.Add(i); Triangles.Add(i + 2); Triangles.Add(i + 1);
            Triangles.Add(i); Triangles.Add(i + 3); Triangles.Add(i + 2);
        }

        /// <summary>An axis-aligned box. The front (+Z) face can use its own cell; the bottom is left out (it sits on something).</summary>
        public void Box(Vector3 min, Vector3 max, Vector2Int cell, Vector2Int front, bool bottom = false)
        {
            Vector3 p000 = new Vector3(min.x, min.y, min.z), p100 = new Vector3(max.x, min.y, min.z);
            Vector3 p010 = new Vector3(min.x, max.y, min.z), p110 = new Vector3(max.x, max.y, min.z);
            Vector3 p001 = new Vector3(min.x, min.y, max.z), p101 = new Vector3(max.x, min.y, max.z);
            Vector3 p011 = new Vector3(min.x, max.y, max.z), p111 = new Vector3(max.x, max.y, max.z);
            Quad(p101, p001, p011, p111, front);   // +Z front
            Quad(p000, p100, p110, p010, cell);    // -Z back
            Quad(p100, p101, p111, p110, cell);    // +X
            Quad(p001, p000, p010, p011, cell);    // -X
            Quad(p010, p110, p111, p011, cell);    // top
            if (bottom) Quad(p001, p101, p100, p000, cell);
        }

        public void Box(Vector3 min, Vector3 max, Vector2Int cell) => Box(min, max, cell, cell);

        /// <summary>A knob: an n-sided prism along +Z from the face at z0, with a front cap.</summary>
        public void Knob(Vector3 centre, float radius, float depth, int sides, Vector2Int cell)
        {
            for (int s = 0; s < sides; s++)
            {
                float a0 = s * Mathf.PI * 2f / sides, a1 = (s + 1) * Mathf.PI * 2f / sides;
                Vector3 r0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * radius, r1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * radius;
                Vector3 back = centre, front = centre + Vector3.forward * depth;
                Quad(back + r1, back + r0, front + r0, front + r1, cell);   // outward
                int i = Vertices.Count;
                Vertices.Add(front); Vertices.Add(front + r1); Vertices.Add(front + r0);
                float u = (cell.x + 0.5f) / Cells, v = (cell.y + 0.5f) / Cells;
                Uvs.Add(new Vector2(u, v)); Uvs.Add(new Vector2(u + 0.1f / Cells, v)); Uvs.Add(new Vector2(u - 0.1f / Cells, v));
                Triangles.Add(i); Triangles.Add(i + 2); Triangles.Add(i + 1);   // facing +Z
            }
        }
    }

    static Mesh BuildMesh()
    {
        const float w = 0.15f, d = 0.055f;   // half width, half depth
        var b = new MeshBuilder();

        // Plinth and body.
        b.Box(new Vector3(-w - 0.006f, 0f, -d - 0.006f), new Vector3(w + 0.006f, 0.014f, d + 0.006f), WoodDark, WoodDark, bottom: true);
        b.Box(new Vector3(-w, 0.014f, -d), new Vector3(w, 0.150f, d), Wood);
        // Stepped top, so the silhouette reads as a rounded cabinet.
        b.Box(new Vector3(-w + 0.010f, 0.150f, -d + 0.008f), new Vector3(w - 0.010f, 0.168f, d - 0.008f), Wood);

        // Front: cloth grille on the left, tuning dial and two knobs on the right.
        const float face = d;
        b.Box(new Vector3(-0.132f, 0.030f, face - 0.001f), new Vector3(0.018f, 0.136f, face + 0.004f), WoodDark, Grille);
        b.Box(new Vector3(0.034f, 0.088f, face - 0.001f), new Vector3(0.132f, 0.136f, face + 0.004f), WoodDark, Dial);
        b.Knob(new Vector3(0.058f, 0.052f, face), 0.015f, 0.016f, 8, Knob);
        b.Knob(new Vector3(0.108f, 0.052f, face), 0.015f, 0.016f, 8, Knob);

        // Carry handle on top.
        b.Box(new Vector3(-0.092f, 0.168f, -0.006f), new Vector3(-0.082f, 0.196f, 0.006f), Metal);
        b.Box(new Vector3(0.082f, 0.168f, -0.006f), new Vector3(0.092f, 0.196f, 0.006f), Metal);
        b.Box(new Vector3(-0.092f, 0.196f, -0.007f), new Vector3(0.092f, 0.206f, 0.007f), Metal);

        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (mesh == null)
        {
            mesh = new Mesh { name = "RadioMusica_mesh" };
            AssetDatabase.CreateAsset(mesh, MeshPath);
        }
        mesh.Clear();
        mesh.SetVertices(b.Vertices);
        mesh.SetUVs(0, b.Uvs);
        mesh.SetTriangles(b.Triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    // ---------- prefab ----------

    static void BuildPrefab(Mesh mesh, Material material)
    {
        var go = new GameObject("PF_RadioMusica");
        try
        {
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
