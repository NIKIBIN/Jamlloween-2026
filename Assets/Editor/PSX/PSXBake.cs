using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Converte um minigame montado com primitivas do Unity para a organizacao do PSX Carnival Kit
/// (guideline: "Organizacao da geometria"):
///   1. Cylinder e Sphere viram prisma de 6/8 lados e esfera de 80 tris; Cube vira caixa com UV empilhado.
///   2. Em cada "parte" (objeto protegido, ver abaixo), tudo o que fica parado vira UMA malha, "<parte>_mesh".
///   3. Cor de vertice cinza, gravada em sRGB entre 0.702 e 1.0 como no kit: topo claro, base escura,
///      gradiente de baixo para cima em cada peca e escurecimento onde uma peca encosta na outra.
///
/// Um objeto e protegido (fica separado, nunca e fundido) quando: e a raiz; tem qualquer componente alem de
/// Transform/MeshFilter/MeshRenderer (script, collider, luz, particula, texto); e referenciado por algum campo
/// serializado de um componente do prefab; comeca desligado (o codigo liga e desliga); tem nome em KeepNames; ou
/// contem, em qualquer nivel abaixo dele, um objeto protegido.
/// Renderers protegidos que usam primitiva tambem trocam de malha e ganham cor de vertice, mas ficam onde estao.
///
/// Menu: Tools > PSX > Converter prefab selecionado. A API (Bake / BakeHierarchy) pode ser chamada por um builder
/// antes de salvar o prefab, para o resultado sobreviver a um rebuild.
/// </summary>
public static class PSXBake
{
    public const string MeshRoot = "Assets/Prefabs/Minigames/Meshes/PSX";

    /// <summary>Objetos que scripts procuram pelo nome com transform.Find. FuseBoxMinigame.cs:84 procura "Glass".</summary>
    public static readonly HashSet<string> KeepNames = new() { "Glass" };

    // Cor de vertice, valores sRGB (o PSX Unlit converte para linear). Kit: piso 0.702, topo ~0.90, laterais ~0.88, base ~0.81.
    const float ShadeTop = 0.96f, ShadeSide = 0.92f, ShadeBottom = 0.86f;
    const float GradientLow = 0.90f;   // pe de cada peca; o topo fica em 1
    const float ContactShade = 0.82f;  // vertice encostado/enterrado em outra peca da mesma malha
    const float ShadeFloor = 0.702f;   // = 0.451 linear, o minimo do kit
    const float ContactReach = 0.004f; // 4 mm em volta da outra peca

    public class Report
    {
        public string prefab;
        public int renderersBefore, renderersAfter, merged, swapped, meshesWritten, removedObjects;
        public readonly List<string> groups = new();
        public override string ToString() =>
            $"{prefab}: renderers {renderersBefore} -> {renderersAfter}; {merged} fundidos em {groups.Count} malhas; " +
            $"{swapped} primitivas protegidas trocadas; {removedObjects} objetos vazios removidos; {meshesWritten} assets de malha.\n  " +
            string.Join("\n  ", groups);
    }

    [MenuItem("Tools/PSX/Converter prefab selecionado (PSX Bake)")]
    static void BakeSelected()
    {
        var paths = Selection.objects.Select(AssetDatabase.GetAssetPath).Where(p => p.EndsWith(".prefab")).ToList();
        if (paths.Count == 0) { EditorUtility.DisplayDialog("PSX Bake", "Selecione um ou mais prefabs no Project.", "OK"); return; }
        if (!EditorUtility.DisplayDialog("PSX Bake",
                $"Converter {paths.Count} prefab(s) para o padrao do PSX Carnival Kit?\n\n{string.Join("\n", paths)}\n\nO prefab e salvo por cima (use o git para voltar).",
                "Converter", "Cancelar")) return;
        foreach (var p in paths) Debug.Log("[PSXBake] " + Bake(p, true));
    }

    /// <summary>Converte o prefab em disco. save = false roda tudo sem gravar nada (para conferir o relatorio).</summary>
    public static Report Bake(string prefabPath, bool save)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            string folder = save ? $"{MeshRoot}/{Path.GetFileNameWithoutExtension(prefabPath)}" : null;
            var report = BakeHierarchy(root, folder);
            report.prefab = prefabPath;
            if (save) PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return report;
        }
        finally { PSXBake.UnloadContents(root); }
    }

    /// <summary>
    /// Descarrega um prefab aberto com LoadPrefabContents. UnloadPrefabContents (e ClosePreviewScene) nao chamam o
    /// OnDisable dos componentes: um TextMeshPro continua inscrito no TMPro_EventManager e gera
    /// MissingReferenceException na proxima mudanca de fonte. Desligar a raiz antes dispara o OnDisable.
    /// </summary>
    public static void UnloadContents(GameObject root)
    {
        if (root != null) root.SetActive(false);
        PrefabUtility.UnloadPrefabContents(root);
    }

    /// <summary>
    /// Converte uma hierarquia ja carregada. meshFolder = null deixa as malhas so na memoria (preview).
    /// extraParts: objetos que viram parte propria (malha separada) mesmo sem script nem referencia, para poder mexer
    /// neles isolados no editor. Ex.: a base da alavanca do quadro de fusiveis.
    /// </summary>
    public static Report BakeHierarchy(GameObject root, string meshFolder, params GameObject[] extraParts)
    {
        var report = new Report { prefab = root.name, renderersBefore = root.GetComponentsInChildren<MeshRenderer>(true).Length };
        var keep = FindProtected(root);
        foreach (var go in extraParts)
            if (go != null)
                for (var t = go.transform; t != null && keep.Add(t.gameObject); t = t.parent) { }
        Vector3 up = root.transform.up;

        // Agrupa os renderers fundiveis pela parte protegida mais proxima acima deles.
        var groups = new Dictionary<Transform, List<MeshRenderer>>();
        var protectedPrims = new List<MeshRenderer>();
        foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || PrimitiveOf(mf.sharedMesh) == null) continue;
            if (keep.Contains(r.gameObject)) { protectedPrims.Add(r); continue; }
            var g = r.transform.parent;
            while (!keep.Contains(g.gameObject)) g = g.parent;
            if (!groups.TryGetValue(g, out var list)) groups[g] = list = new List<MeshRenderer>();
            list.Add(r);
        }

        if (meshFolder != null) EnsureFolder(meshFolder);

        foreach (var (group, renderers) in groups)
        {
            var mesh = Merge(group, renderers, up, out var materials);
            mesh.name = $"{group.name}_mesh";
            var go = new GameObject(mesh.name);
            go.layer = group.gameObject.layer;
            go.transform.SetParent(group, false);
            go.AddComponent<MeshFilter>().sharedMesh = Save(mesh, meshFolder, PathName(root.transform, group) + "_mesh", report);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = materials;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            report.merged += renderers.Count;
            report.groups.Add($"{PathName(root.transform, group)}: {renderers.Count} pecas, {mesh.triangles.Length / 3} tris, {materials.Length} material(is)");
            foreach (var r in renderers) RemoveRenderer(r, report);
        }

        foreach (var r in protectedPrims)
        {
            var mf = r.GetComponent<MeshFilter>();
            var mesh = Merge(r.transform, new List<MeshRenderer> { r }, up, out _);
            mesh.name = $"{r.name}_psx";
            mf.sharedMesh = Save(mesh, meshFolder, PathName(root.transform, r.transform) + "_psx", report);
            report.swapped++;
        }

        RemoveEmptyLeaves(root.transform, keep, report);
        report.renderersAfter = root.GetComponentsInChildren<MeshRenderer>(true).Length;
        return report;
    }

    // ---------------------------------------------------------------- protecao

    static HashSet<GameObject> FindProtected(GameObject root)
    {
        var keep = new HashSet<GameObject> { root };
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            var go = t.gameObject;
            if (!go.activeSelf || KeepNames.Contains(go.name)) keep.Add(go);
            foreach (var c in go.GetComponents<Component>())
                if (c == null || !(c is Transform || c is MeshFilter || c is MeshRenderer)) { keep.Add(go); break; }
        }
        foreach (var c in root.GetComponentsInChildren<Component>(true))
        {
            if (c == null || c is Transform || c is MeshFilter || c is MeshRenderer) continue;
            var it = new SerializedObject(c).GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue == null) continue;
                var o = it.objectReferenceValue;
                var g = o as GameObject ?? (o as Component)?.gameObject;
                if (g != null && g.transform.IsChildOf(root.transform)) keep.Add(g);
            }
        }
        // Quem contem uma peca protegida tambem e uma parte: a cena ou o codigo podem mover o conjunto.
        // Ex.: o Head da work lamp, que leva a luz e e girado por override em cada cena.
        foreach (var go in keep.ToList())
            for (var p = go.transform.parent; p != null && keep.Add(p.gameObject); p = p.parent) { }
        return keep;
    }

    // ---------------------------------------------------------------- malha

    enum Prim { Cube, Cylinder, Sphere }

    static Prim? PrimitiveOf(Mesh m)
    {
        if (m == null || AssetDatabase.GetAssetPath(m) != "Library/unity default resources") return null;
        return m.name switch { "Cube" => Prim.Cube, "Cylinder" => Prim.Cylinder, "Sphere" => Prim.Sphere, _ => (Prim?)null };
    }

    static readonly Dictionary<string, Mesh> templates = new();

    static Mesh Template(Prim p, Transform t)
    {
        string key; System.Func<Mesh> make;
        switch (p)
        {
            case Prim.Cube: key = "box"; make = PSXMeshFactory.Box; break;
            case Prim.Sphere: key = "sphere"; make = () => PSXMeshFactory.Sphere(); break;
            default:
                Vector3 s = t.lossyScale;
                int sides = PSXMeshFactory.PrismSidesFor(Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z)));
                key = "prism" + sides; make = () => PSXMeshFactory.Prism(sides); break;
        }
        if (!templates.TryGetValue(key, out var m) || m == null) templates[key] = m = make();
        return m;
    }

    /// <summary>Junta as pecas no espaco de 'space', um submesh por material, e pinta a cor de vertice.</summary>
    static Mesh Merge(Transform space, List<MeshRenderer> renderers, Vector3 worldUp, out Material[] materials)
    {
        var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>();
        var island = new List<int>(); var islandBounds = new List<Bounds>();
        var byMat = new Dictionary<Material, List<int>>();
        var order = new List<Material>();

        for (int k = 0; k < renderers.Count; k++)
        {
            var r = renderers[k];
            var src = Template(PrimitiveOf(r.GetComponent<MeshFilter>().sharedMesh).Value, r.transform);
            Matrix4x4 m = space.worldToLocalMatrix * r.transform.localToWorldMatrix;
            Matrix4x4 nm = m.inverse.transpose;
            bool flip = m.determinant < 0f;
            int baseIndex = verts.Count;
            var b = new Bounds(m.MultiplyPoint3x4(src.vertices[0]), Vector3.zero);
            foreach (var p in src.vertices) { var q = m.MultiplyPoint3x4(p); verts.Add(q); b.Encapsulate(q); island.Add(k); }
            foreach (var n in src.normals) norms.Add(nm.MultiplyVector(n).normalized);
            uvs.AddRange(src.uv);
            islandBounds.Add(b);

            var mat = r.sharedMaterial;
            if (mat == null) mat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            if (!byMat.TryGetValue(mat, out var tris)) { byMat[mat] = tris = new List<int>(); order.Add(mat); }
            var st = src.triangles;
            for (int i = 0; i < st.Length; i += 3)
            {
                tris.Add(baseIndex + st[i]);
                tris.Add(baseIndex + (flip ? st[i + 2] : st[i + 1]));
                tris.Add(baseIndex + (flip ? st[i + 1] : st[i + 2]));
            }
        }

        // Cor de vertice
        Vector3 up = space.InverseTransformDirection(worldUp).normalized;
        float reach = ContactReach / Mathf.Max(1e-4f, space.lossyScale.magnitude / 1.732f);
        var colors = new Color[verts.Count];
        for (int i = 0; i < verts.Count; i++)
        {
            float d = Vector3.Dot(norms[i], up);
            float shade = d >= 0f ? Mathf.Lerp(ShadeSide, ShadeTop, d) : Mathf.Lerp(ShadeSide, ShadeBottom, -d);

            var ib = islandBounds[island[i]];
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var c in Corners(ib)) { float h = Vector3.Dot(c, up); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            float t = hi - lo > 1e-5f ? Mathf.InverseLerp(lo, hi, Vector3.Dot(verts[i], up)) : 1f;
            shade *= Mathf.Lerp(GradientLow, 1f, t);

            for (int k = 0; k < islandBounds.Count; k++)
            {
                if (k == island[i]) continue;
                var ob = islandBounds[k]; ob.Expand(reach * 2f);
                if (ob.Contains(verts[i])) { shade *= ContactShade; break; }
            }
            float v = Mathf.Clamp(shade, ShadeFloor, 1f);
            colors[i] = new Color(v, v, v, 1f);
        }

        var mesh = new Mesh();
        if (verts.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.colors = colors;
        mesh.subMeshCount = order.Count;
        for (int s = 0; s < order.Count; s++) mesh.SetTriangles(byMat[order[s]], s);
        mesh.RecalculateBounds();
        materials = order.ToArray();
        return mesh;
    }

    static IEnumerable<Vector3> Corners(Bounds b)
    {
        for (int i = 0; i < 8; i++)
            yield return b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
    }

    // ---------------------------------------------------------------- limpeza e disco

    /// <summary>Objetos que perderam o renderer na fusao, mais os containers entre eles e a parte ("Visual", por exemplo).
    /// So estes podem ser apagados: um Transform vazio que ja existia antes (ancora, pose) fica.</summary>
    static readonly HashSet<GameObject> removable = new();

    static void RemoveRenderer(MeshRenderer r, Report report)
    {
        var go = r.gameObject;
        Object.DestroyImmediate(r);
        var mf = go.GetComponent<MeshFilter>();
        if (mf != null) Object.DestroyImmediate(mf);
        removable.Add(go);
        for (var p = go.transform.parent; p != null && !removable.Contains(p.gameObject); p = p.parent) removable.Add(p.gameObject);
    }

    /// <summary>Apaga, de baixo para cima, os objetos removiveis nao protegidos que ficaram sem filhos e so com o Transform.</summary>
    static void RemoveEmptyLeaves(Transform root, HashSet<GameObject> keep, Report report)
    {
        bool removed;
        do
        {
            removed = false;
            foreach (var t in root.GetComponentsInChildren<Transform>(true).Reverse())
            {
                var go = t.gameObject;
                if (!removable.Contains(go) || keep.Contains(go) || t.childCount > 0 || go.GetComponents<Component>().Length > 1) continue;
                removable.Remove(go);
                Object.DestroyImmediate(go);
                report.removedObjects++;
                removed = true;
            }
        } while (removed);
        removable.Clear();
    }

    static Mesh Save(Mesh mesh, string folder, string fileName, Report report)
    {
        if (folder == null) return mesh;
        string path = $"{folder}/{fileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) { CopyMesh(mesh, existing); EditorUtility.SetDirty(existing); mesh = existing; }
        else AssetDatabase.CreateAsset(mesh, path);
        report.meshesWritten++;
        return mesh;
    }

    /// <summary>
    /// Reescreve uma malha existente pela API da Mesh. EditorUtility.CopySerialized gravava os dados novos no asset,
    /// mas o editor continuava desenhando a malha antiga ate reiniciar (submeshes novas sobre vertices velhos).
    /// </summary>
    public static void CopyMesh(Mesh src, Mesh dst)
    {
        dst.Clear();
        dst.name = src.name;
        dst.indexFormat = src.indexFormat;
        dst.SetVertices(src.vertices);
        dst.SetNormals(src.normals);
        dst.SetUVs(0, src.uv);
        dst.colors = src.colors;
        dst.subMeshCount = src.subMeshCount;
        for (int s = 0; s < src.subMeshCount; s++) dst.SetTriangles(src.GetTriangles(s), s);
        dst.RecalculateBounds();
    }

    static string PathName(Transform root, Transform t)
    {
        var parts = new List<string>();
        for (var x = t; x != null && x != root; x = x.parent) parts.Add(x.name);
        parts.Add(root.name);
        parts.Reverse();
        return string.Join("_", parts).Replace(' ', '_').Replace('(', '_').Replace(')', '_');
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        EnsureFolder(Path.GetDirectoryName(folder).Replace('\\', '/'));
        AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\', '/'), Path.GetFileName(folder));
    }
}
