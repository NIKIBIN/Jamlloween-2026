using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Malhas low-poly no padrao do PSX Carnival Kit, com as mesmas dimensoes das primitivas do Unity
/// (Cube: 1 m; Cylinder: 1 m de diametro por 2 m de altura; Sphere: 1 m de diametro). Assim uma troca pela outra
/// sem mexer em escala nem em posicao.
/// UV: cada face ocupa o quadrado 0..1 inteiro, empilhada sobre as outras, como as caixas do kit. O material escolhe
/// o recorte do atlas por tiling/offset.
/// </summary>
public static class PSXMeshFactory
{
    public const string KitMeshFolder = "Assets/Prefabs/Minigames/Meshes/Kit";

    /// <summary>Grava as malhas base como assets, para usar a mao no lugar das primitivas do Unity.</summary>
    [UnityEditor.MenuItem("Tools/PSX/Gerar malhas base (caixa, prismas, esfera)")]
    public static string SaveKitMeshes()
    {
        if (!UnityEditor.AssetDatabase.IsValidFolder(KitMeshFolder))
            UnityEditor.AssetDatabase.CreateFolder("Assets/Prefabs/Minigames/Meshes", "Kit");
        var made = new List<string>();
        foreach (var m in new[] { Box(), Prism(6), Prism(8), Prism(3), Sphere() })
        {
            string path = $"{KitMeshFolder}/{m.name}.asset";
            var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { UnityEditor.EditorUtility.CopySerialized(m, existing); UnityEditor.EditorUtility.SetDirty(existing); }
            else UnityEditor.AssetDatabase.CreateAsset(m, path);
            made.Add($"{m.name} ({m.triangles.Length / 3} tris)");
        }
        UnityEditor.AssetDatabase.SaveAssets();
        return string.Join(", ", made);
    }

    /// <summary>Lados do prisma que substitui o Cylinder: 6 para pecas finas (pino, parafuso, bateria), 8 para o resto.</summary>
    public static int PrismSidesFor(float worldDiameter) => worldDiameter < 0.03f ? 6 : 8;

    public static Mesh Box()
    {
        var b = new Builder("PSX_Box");
        Vector3 h = Vector3.one * 0.5f;
        // +X, -X, +Y, -Y, +Z, -Z
        b.Quad(new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, -h.y, h.z), new Vector3(h.x, h.y, h.z), new Vector3(h.x, h.y, -h.z), Vector3.right);
        b.Quad(new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, h.y, -h.z), new Vector3(-h.x, h.y, h.z), Vector3.left);
        b.Quad(new Vector3(-h.x, h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z), Vector3.up);
        b.Quad(new Vector3(-h.x, -h.y, h.z), new Vector3(h.x, -h.y, h.z), new Vector3(h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, -h.z), Vector3.down);
        b.Quad(new Vector3(h.x, -h.y, h.z), new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, h.y, h.z), new Vector3(h.x, h.y, h.z), Vector3.forward);
        b.Quad(new Vector3(-h.x, -h.y, -h.z), new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(-h.x, h.y, -h.z), Vector3.back);
        return b.Build();
    }

    /// <summary>Prisma de n lados no lugar do Cylinder (raio 0.5, y de -1 a 1). Laterais com normal suave, tampas planas.</summary>
    public static Mesh Prism(int sides)
    {
        var b = new Builder($"PSX_Prism{sides}");
        var ring = new Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            float a = (i + 0.5f) / sides * Mathf.PI * 2f;   // meio lado de giro: uma face fica de frente para +Z
            ring[i] = new Vector3(Mathf.Sin(a) * 0.5f, 0f, Mathf.Cos(a) * 0.5f);
        }
        for (int i = 0; i < sides; i++)
        {
            Vector3 p0 = ring[i], p1 = ring[(i + 1) % sides];
            Vector3 n0 = p0.normalized, n1 = p1.normalized;
            b.QuadSmooth(p0 + Vector3.down, p1 + Vector3.down, p1 + Vector3.up, p0 + Vector3.up, n0, n1, n1, n0, (n0 + n1).normalized);
        }
        b.Cap(ring, 1f, Vector3.up);
        b.Cap(ring, -1f, Vector3.down);
        return b.Build();
    }

    /// <summary>Esfera de raio 0.5 com 8 meridianos e 6 faixas: 80 triangulos, como o maximo da guideline.</summary>
    public static Mesh Sphere(int segments = 8, int rings = 6)
    {
        var b = new Builder("PSX_Sphere");
        Vector3 P(int r, int s)
        {
            float v = (float)r / rings * Mathf.PI, u = (float)s / segments * Mathf.PI * 2f;
            return new Vector3(Mathf.Sin(v) * Mathf.Sin(u), Mathf.Cos(v), Mathf.Sin(v) * Mathf.Cos(u)) * 0.5f;
        }
        for (int r = 0; r < rings; r++)
            for (int s = 0; s < segments; s++)
            {
                Vector3 a = P(r, s), c = P(r + 1, s), d = P(r + 1, s + 1), e = P(r, s + 1);
                Vector3 mid = (a + c + d + e).normalized;
                if (r == 0) b.TriSmooth(a, c, d, a.normalized, c.normalized, d.normalized, mid);
                else if (r == rings - 1) b.TriSmooth(a, c, e, a.normalized, c.normalized, e.normalized, mid);
                else b.QuadSmooth(a, c, d, e, a.normalized, c.normalized, d.normalized, e.normalized, mid);
            }
        return b.Build();
    }

    /// <summary>Monta triangulos sempre virados para fora: confere o produto vetorial contra a normal desejada e inverte se precisar.</summary>
    class Builder
    {
        readonly string name;
        readonly List<Vector3> v = new(), n = new();
        readonly List<Vector2> uv = new();
        readonly List<int> t = new();
        public Builder(string name) { this.name = name; }

        int Add(Vector3 p, Vector3 normal, Vector2 tex) { v.Add(p); n.Add(normal); uv.Add(tex); return v.Count - 1; }

        void Tri(int a, int b, int c, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[c] - v[a]), outward) < 0f) { t.Add(a); t.Add(c); t.Add(b); }
            else { t.Add(a); t.Add(b); t.Add(c); }
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal) =>
            QuadSmooth(a, b, c, d, normal, normal, normal, normal, normal);

        public void QuadSmooth(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd, Vector3 outward)
        {
            int ia = Add(a, na, new Vector2(0, 0)), ib = Add(b, nb, new Vector2(1, 0)), ic = Add(c, nc, new Vector2(1, 1)), id = Add(d, nd, new Vector2(0, 1));
            Tri(ia, ib, ic, outward);
            Tri(ia, ic, id, outward);
        }

        public void TriSmooth(Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc, Vector3 outward)
        {
            int ia = Add(a, na, new Vector2(0, 0)), ib = Add(b, nb, new Vector2(1, 0)), ic = Add(c, nc, new Vector2(0.5f, 1));
            Tri(ia, ib, ic, outward);
        }

        /// <summary>Tampa plana em leque, UV planar (x, z) + 0.5.</summary>
        public void Cap(Vector3[] ring, float y, Vector3 normal)
        {
            int first = v.Count;
            foreach (var p in ring) Add(new Vector3(p.x, y, p.z), normal, new Vector2(p.x + 0.5f, p.z + 0.5f));
            for (int i = 1; i < ring.Length - 1; i++) Tri(first, first + i, first + i + 1, normal);
        }

        public Mesh Build()
        {
            var m = new Mesh { name = name };
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
