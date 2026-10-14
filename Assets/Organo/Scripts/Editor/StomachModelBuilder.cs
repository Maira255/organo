using System.Collections.Generic;
using System.IO;
using Organo.Data;
using Organo.ThreeD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Organo.EditorTools
{
    /// <summary>
    /// Membuat model 3D lambung langsung di Unity (tanpa software 3D): bentuk anatomi "J" — kerongkongan, fundus,
    /// badan lambung, antrum, pilorus, dan duodenum — dari signed distance field yang dihaluskan, lalu dijadikan mesh
    /// dengan algoritma surface nets. Hasil: prefab di Assets/Organo/Models/Lambung yang otomatis masuk ke slot model lambung.
    /// </summary>
    [InitializeOnLoad]
    public static class StomachModelBuilder
    {
        const string Dir = "Assets/Organo/Models/Lambung";
        const string PrefabPath = Dir + "/Lambung.prefab";
        const string OrganPath = "Assets/Organo/Data/Organ_lambung.asset";

        static StomachModelBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(PrefabPath)) return;
                if (!File.Exists(OrganPath)) return;
                Build();
            };
        }

        [MenuItem("Organo/Model 3D/Buat Ulang Model Lambung", priority = 30)]
        public static void Build()
        {
            EnsureFolder(Dir);
            var sdf = new StomachSdf();
            var mesh = SurfaceNets.Build(sdf, new Bounds(new Vector3(-0.05f, 0.02f, 0f), new Vector3(1.8f, 1.85f, 0.95f)), 0.0135f);
            mesh.name = "Lambung";

            var meshPath = Dir + "/Lambung_Mesh.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            // Buat ulang aset mesh (CopySerialized tidak memperbarui pembagian submesh dengan benar).
            if (old != null) AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);

            var wall = MakeMaterial(Dir + "/Lambung_Dinding.mat", new Color32(0xD6, 0x5F, 0x58, 0xFF), 0.62f);
            var inner = MakeMaterial(Dir + "/Lambung_Dalam.mat", new Color32(0x7E, 0x24, 0x2A, 0xFF), 0.35f);
            var kardia = MakeMaterial(Dir + "/Lambung_Kardia.mat", new Color32(0xE8, 0x87, 0x7F, 0xFF), 0.62f);
            var fundus = MakeMaterial(Dir + "/Lambung_Fundus.mat", new Color32(0xE4, 0x70, 0x6A, 0xFF), 0.62f);
            var pilorus = MakeMaterial(Dir + "/Lambung_Pilorus.mat", new Color32(0xBF, 0x4A, 0x4E, 0xFF), 0.62f);
            var duo = MakeMaterial(Dir + "/Lambung_Duodenum.mat", new Color32(0xEF, 0x9A, 0x7C, 0xFF), 0.6f);
            if (inner.HasProperty("_Cull")) { inner.SetFloat("_Cull", 0f); inner.doubleSidedGI = true; }

            var root = new GameObject("Lambung");
            var body = new GameObject("Mesh", typeof(MeshFilter), typeof(MeshRenderer));
            body.transform.SetParent(root.transform, false);
            body.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = body.GetComponent<MeshRenderer>();
            mr.sharedMaterials = new[] { wall, inner, kardia, fundus, pilorus, duo };
            mr.shadowCastingMode = ShadowCastingMode.On;

            // Titik info (depan model menghadap -Z, ke arah kamera close-up)
            Hotspot(root, "Asam Lambung", "Membantu hancurkan makanan", new Vector3(0.22f, -0.08f, -0.29f));
            Hotspot(root, "Otot lambung", "Mengaduk makanan", new Vector3(0.43f, 0.22f, -0.17f));
            Hotspot(root, "Pintu masuk", "Dari kerongkongan", new Vector3(-0.1f, 0.66f, -0.07f));
            Hotspot(root, "Pilorus", "Pintu ke usus halus", new Vector3(-0.4f, -0.32f, -0.1f));

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            // versi ringan untuk tampilan tubuh
            var lowMesh = SurfaceNets.Build(sdf, new Bounds(new Vector3(-0.05f, 0.02f, 0f), new Vector3(1.8f, 1.85f, 0.95f)), 0.035f);
            lowMesh.name = "Lambung_Low";
            var lowPath = Dir + "/Lambung_Low_Mesh.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(lowPath) != null) AssetDatabase.DeleteAsset(lowPath);
            AssetDatabase.CreateAsset(lowMesh, lowPath);
            var lowRoot = new GameObject("Lambung_Low");
            var lowBody = new GameObject("Mesh", typeof(MeshFilter), typeof(MeshRenderer));
            lowBody.transform.SetParent(lowRoot.transform, false);
            lowBody.GetComponent<MeshFilter>().sharedMesh = lowMesh;
            lowBody.GetComponent<MeshRenderer>().sharedMaterials = new[] { wall, inner, kardia, fundus, pilorus, duo };
            PrefabUtility.SaveAsPrefabAsset(lowRoot, Dir + "/Lambung_Low.prefab");
            Object.DestroyImmediate(lowRoot);
            var lowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "/Lambung_Low.prefab");

            var organ = AssetDatabase.LoadAssetAtPath<OrganInfo>(OrganPath);
            if (organ != null)
            {
                organ.lowModelPrefab = lowPrefab;
                organ.modelPrefab = prefab;
                organ.modelEuler = new Vector3(4f, -12f, 0f);
                organ.modelScale = 1.05f;
                EditorUtility.SetDirty(organ);
            }
            AssetDatabase.SaveAssets();
            RenderPreview(prefab);
            Debug.Log("[Organo] Model 3D lambung dibuat: " + PrefabPath + " (" + mesh.vertexCount + " verteks, " +
                      TriCount(mesh) + " segitiga) dan dipasang ke slot Model Prefab lambung.");
        }

        public static long TriCount(Mesh m)
        {
            long n = 0;
            for (int i = 0; i < m.subMeshCount; i++) n += m.GetIndexCount(i);
            return n / 3;
        }

        static void Hotspot(GameObject root, string title, string desc, Vector3 pos)
        {
            var go = new GameObject("Hotspot " + title, typeof(OrganHotspot));
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            var h = go.GetComponent<OrganHotspot>();
            h.title = title;
            h.description = desc;
        }

        public static Material MakeMaterial(string path, Color color, float smooth)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", 1f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Render pratinjau ke Temp/OrganoPreview supaya bentuknya bisa dicek tanpa membuka scene.</summary>
        public static void RenderPreview(GameObject prefab, string prefix = "lambung")
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var lightGo = new GameObject("Light", typeof(Light));
                EditorSceneManager.MoveGameObjectToScene(lightGo, scene);
                var light = lightGo.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.3f;
                lightGo.transform.rotation = Quaternion.Euler(35f, -25f, 0f);
                var camGo = new GameObject("Cam", typeof(Camera));
                EditorSceneManager.MoveGameObjectToScene(camGo, scene);
                var cam = camGo.GetComponent<Camera>();
                cam.scene = scene;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(1f, 0.945f, 0.957f);
                cam.fieldOfView = 30f;
                var rt = new RenderTexture(640, 640, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cam.targetTexture = rt;
                Directory.CreateDirectory("Library/OrganoPreview");
                foreach (var (name, yaw) in new[] { ("front", -12f), ("threequarter", 35f), ("side", 80f) })
                {
                    inst.transform.rotation = Quaternion.Euler(4f, yaw, 0f);
                    camGo.transform.position = new Vector3(0f, 0f, -3.9f);
                    camGo.transform.rotation = Quaternion.identity;
                    cam.Render();
                    var prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    tex.Apply();
                    RenderTexture.active = prev;
                    File.WriteAllBytes("Library/OrganoPreview/" + prefix + "_" + name + ".png", tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                }
                cam.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);
            }
            catch (System.Exception e) { Debug.LogWarning("[Organo] pratinjau lambung gagal: " + e.Message); }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }

    // ------------------------------------------------------------------------------------------------

    public interface ISdf
    {
        float Eval(Vector3 p);
        /// <summary>Indeks material (submesh) untuk titik di permukaan.</summary>
        int Material(Vector3 p);
        int MaterialCount { get; }
    }

    /// <summary>Bentuk lambung: tabung spline dengan jari-jari berubah + gelembung fundus, ujung terbuka.</summary>
    public class StomachSdf : ISdf
    {
        readonly List<Vector4> m_Path = new List<Vector4>(); // xyz + radius (sudah dihaluskan)
        readonly Vector4[] m_Blobs;
        readonly Vector3 m_TopEnd, m_TopDir, m_BotEnd, m_BotDir;
        const float Flatten = 1.36f;   // pipihkan ke arah depan-belakang
        const float Wall = 0.026f;     // tebal dinding di ujung terbuka
        const float HoleDepth = 0.03f; // kedalaman cekungan lubang

        public StomachSdf()
        {
            // x kanan, y atas, depan = -z. Diukur dari model contoh.
            var ctrl = new[]
            {
                new Vector4(-0.12f, 0.80f, 0.02f, 0.072f),  // kerongkongan (ujung atas)
                new Vector4(-0.11f, 0.62f, 0.00f, 0.076f),
                new Vector4(-0.08f, 0.45f, -0.01f, 0.082f),
                new Vector4(-0.02f, 0.29f, -0.01f, 0.11f),  // kardia
                new Vector4(0.12f, 0.15f, 0.00f, 0.24f),
                new Vector4(0.29f, -0.02f, 0.00f, 0.355f),  // badan lambung
                new Vector4(0.24f, -0.27f, 0.00f, 0.33f),
                new Vector4(0.03f, -0.42f, 0.01f, 0.25f),   // antrum
                new Vector4(-0.21f, -0.41f, 0.03f, 0.165f),
                new Vector4(-0.38f, -0.33f, 0.05f, 0.098f), // pilorus
                new Vector4(-0.51f, -0.27f, 0.07f, 0.094f), // duodenum
                new Vector4(-0.63f, -0.38f, 0.09f, 0.094f),
                new Vector4(-0.61f, -0.54f, 0.09f, 0.09f),
                new Vector4(-0.50f, -0.63f, 0.07f, 0.088f), // ujung duodenum
            };
            for (int i = 0; i < ctrl.Length - 1; i++)
            {
                var p0 = ctrl[Mathf.Max(0, i - 1)]; var p1 = ctrl[i]; var p2 = ctrl[i + 1]; var p3 = ctrl[Mathf.Min(ctrl.Length - 1, i + 2)];
                for (int s = 0; s < 10; s++)
                {
                    float t = s / 10f;
                    m_Path.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }
            m_Path.Add(ctrl[ctrl.Length - 1]);

            m_Blobs = new[]
            {
                new Vector4(0.23f, 0.33f, 0.00f, 0.255f),  // fundus (menggembung di atas kanan)
                new Vector4(0.37f, 0.14f, 0.00f, 0.30f),   // kelengkungan besar
            };

            m_TopEnd = ctrl[0]; m_TopDir = ((Vector3)ctrl[0] - (Vector3)ctrl[1]).normalized;
            m_BotEnd = ctrl[ctrl.Length - 1]; m_BotDir = ((Vector3)ctrl[ctrl.Length - 1] - (Vector3)ctrl[ctrl.Length - 2]).normalized;
        }

        static Vector4 CatmullRom(Vector4 p0, Vector4 p1, Vector4 p2, Vector4 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        static float SMin(float a, float b, float k)
        {
            float h = Mathf.Max(k - Mathf.Abs(a - b), 0f) / k;
            return Mathf.Min(a, b) - h * h * k * 0.25f;
        }

        float Tube(Vector3 p)
        {
            float d = float.MaxValue;
            for (int i = 0; i < m_Path.Count - 1; i++)
            {
                Vector3 a = m_Path[i], b = m_Path[i + 1];
                var ab = b - a;
                float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-8f));
                float r = Mathf.Lerp(m_Path[i].w, m_Path[i + 1].w, t);
                float dd = (p - (a + ab * t)).magnitude - r;
                if (dd < d) d = dd;
            }
            return d;
        }

        static float Capsule(Vector3 p, Vector3 a, Vector3 b, float r)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + ab * t)).magnitude - r;
        }

        // Potong ujung jadi rata + lubang ke dalam
        static float Opening(float d, Vector3 p, Vector3 end, Vector3 dir, float radius)
        {
            float cut = Mathf.Max(-Vector3.Dot(p - end, dir), (p - end).magnitude - radius * 2.2f);
            d = Mathf.Max(d, -cut);
            float hole = Capsule(p, end + dir * 0.05f, end - dir * HoleDepth, radius - Wall);
            return Mathf.Max(d, -hole);
        }

        public float Eval(Vector3 p)
        {
            var q = new Vector3(p.x, p.y, p.z * Flatten);
            float d = Tube(q);
            foreach (var b in m_Blobs) d = SMin(d, ((Vector3)b - q).magnitude - b.w, 0.14f);
            // sedikit relief (lipatan halus) supaya tidak terlihat seperti plastik
            d += 0.0012f * Mathf.Sin(q.x * 31f + q.y * 7f) * Mathf.Sin(q.y * 27f - q.z * 11f);
            d = Opening(d, q, m_TopEnd, m_TopDir, 0.072f);
            d = Opening(d, q, m_BotEnd, m_BotDir, 0.088f);
            return d / Flatten * 1.1f;
        }

        public int MaterialCount => 6;

        /// <summary>0 badan, 1 bagian dalam lubang, 2 pintu masuk (kardia), 3 fundus, 4 pilorus/antrum, 5 usus dua belas jari.</summary>
        public int Material(Vector3 p)
        {
            if (IsInterior(p)) return 1;
            if (p.y > 0.3f) return 2;
            if (p.y > 0.12f && p.x > 0.0f) return 3;
            if (p.x < -0.44f) return 5;
            if (p.x < -0.12f && p.y < -0.15f) return 4;
            return 0;
        }

        public bool IsInterior(Vector3 p)
        {
            var q = new Vector3(p.x, p.y, p.z * Flatten);
            return Capsule(q, m_TopEnd + m_TopDir * 0.05f, m_TopEnd - m_TopDir * HoleDepth, 0.072f - Wall) < 0.008f ||
                   Capsule(q, m_BotEnd + m_BotDir * 0.05f, m_BotEnd - m_BotDir * HoleDepth, 0.088f - Wall) < 0.008f;
        }
    }

    /// <summary>Surface nets: ubah SDF jadi mesh halus (verteks bersama, normal dari gradien SDF).</summary>
    public static class SurfaceNets
    {
        public static Mesh Build(ISdf sdf, Bounds bounds, float cell)
        {
            int nx = Mathf.CeilToInt(bounds.size.x / cell), ny = Mathf.CeilToInt(bounds.size.y / cell), nz = Mathf.CeilToInt(bounds.size.z / cell);
            var min = bounds.min;
            int sx = nx + 1, sy = ny + 1, sz = nz + 1;
            var f = new float[sx * sy * sz];
            int Idx(int i, int j, int k) => i + sx * (j + sy * k);
            Vector3 Pos(int i, int j, int k) => min + new Vector3(i, j, k) * cell;

            for (int k = 0; k < sz; k++)
            {
                EditorUtility.DisplayProgressBar("Organo", "Membuat model lambung…", (float)k / sz);
                for (int j = 0; j < sy; j++)
                    for (int i = 0; i < sx; i++)
                        f[Idx(i, j, k)] = sdf.Eval(Pos(i, j, k));
            }
            EditorUtility.ClearProgressBar();

            var verts = new List<Vector3>();
            var cellVert = new int[nx * ny * nz];
            int CIdx(int i, int j, int k) => i + nx * (j + ny * k);
            var corner = new Vector3Int[8];
            for (int c = 0; c < 8; c++) corner[c] = new Vector3Int(c & 1, (c >> 1) & 1, (c >> 2) & 1);
            int[,] edges = { { 0, 1 }, { 2, 3 }, { 4, 5 }, { 6, 7 }, { 0, 2 }, { 1, 3 }, { 4, 6 }, { 5, 7 }, { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };
            var vals = new float[8];

            for (int k = 0; k < nz; k++)
                for (int j = 0; j < ny; j++)
                    for (int i = 0; i < nx; i++)
                    {
                        int mask = 0;
                        for (int c = 0; c < 8; c++)
                        {
                            vals[c] = f[Idx(i + corner[c].x, j + corner[c].y, k + corner[c].z)];
                            if (vals[c] < 0) mask |= 1 << c;
                        }
                        if (mask == 0 || mask == 255) { cellVert[CIdx(i, j, k)] = -1; continue; }
                        var sum = Vector3.zero; int n = 0;
                        for (int e = 0; e < 12; e++)
                        {
                            int a = edges[e, 0], b = edges[e, 1];
                            if ((vals[a] < 0) == (vals[b] < 0)) continue;
                            float t = vals[a] / (vals[a] - vals[b]);
                            sum += Vector3.Lerp(corner[a], corner[b], t);
                            n++;
                        }
                        cellVert[CIdx(i, j, k)] = verts.Count;
                        verts.Add(Pos(i, j, k) + sum / n * cell);
                    }

            // Proyeksikan verteks ke permukaan (2 langkah Newton) untuk hasil lebih halus
            var normals = new List<Vector3>(verts.Count);
            for (int v = 0; v < verts.Count; v++)
            {
                var p = verts[v];
                for (int it = 0; it < 2; it++)
                {
                    var g = Gradient(sdf, p, cell * 0.5f);
                    float d = sdf.Eval(p);
                    if (g.sqrMagnitude > 1e-8f) p -= g.normalized * d;
                }
                verts[v] = p;
                normals.Add(Gradient(sdf, p, cell * 0.5f).normalized);
            }

            var lists = new List<int>[sdf.MaterialCount];
            for (int m = 0; m < lists.Length; m++) lists[m] = new List<int>();
            void Quad(int a, int b, int c, int d)
            {
                if (a < 0 || b < 0 || c < 0 || d < 0) return;
                Tri(a, b, c); Tri(a, c, d);
            }
            void Tri(int a, int b, int c)
            {
                var pa = verts[a]; var pb = verts[b]; var pc = verts[c];
                var nrm = Vector3.Cross(pb - pa, pc - pa);
                var g = Gradient(sdf, (pa + pb + pc) / 3f, 0.004f);
                var list = lists[Mathf.Clamp(sdf.Material((pa + pb + pc) / 3f), 0, lists.Length - 1)];
                if (Vector3.Dot(nrm, g) >= 0) { list.Add(a); list.Add(b); list.Add(c); }
                else { list.Add(a); list.Add(c); list.Add(b); }
            }

            for (int k = 0; k < sz; k++)
                for (int j = 0; j < sy; j++)
                    for (int i = 0; i < sx; i++)
                    {
                        bool s0 = f[Idx(i, j, k)] < 0;
                        if (i < nx && j > 0 && k > 0 && j < ny && k < nz && s0 != (f[Idx(i + 1, j, k)] < 0))
                            Quad(cellVert[CIdx(i, j - 1, k - 1)], cellVert[CIdx(i, j, k - 1)], cellVert[CIdx(i, j, k)], cellVert[CIdx(i, j - 1, k)]);
                        if (j < ny && i > 0 && k > 0 && i < nx && k < nz && s0 != (f[Idx(i, j + 1, k)] < 0))
                            Quad(cellVert[CIdx(i - 1, j, k - 1)], cellVert[CIdx(i, j, k - 1)], cellVert[CIdx(i, j, k)], cellVert[CIdx(i - 1, j, k)]);
                        if (k < nz && i > 0 && j > 0 && i < nx && j < ny && s0 != (f[Idx(i, j, k + 1)] < 0))
                            Quad(cellVert[CIdx(i - 1, j - 1, k)], cellVert[CIdx(i, j - 1, k)], cellVert[CIdx(i, j, k)], cellVert[CIdx(i - 1, j, k)]);
                    }

            var mesh = new Mesh { indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            var uv = new List<Vector2>(verts.Count);
            foreach (var p in verts) uv.Add(new Vector2(p.x + 0.5f, p.y + 0.5f));
            mesh.SetUVs(0, uv);
            int total = 0;
            foreach (var l in lists) total += l.Count;
            if (total / 3 > 0 && verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.subMeshCount = lists.Length;
            for (int m = 0; m < lists.Length; m++) mesh.SetTriangles(lists[m], m);
            mesh.RecalculateBounds();
            // tanpa tangent: tidak ada normal map, jadi menghemat ukuran aplikasi
            return mesh;
        }

        static Vector3 Gradient(ISdf sdf, Vector3 p, float h)
        {
            return new Vector3(
                sdf.Eval(p + new Vector3(h, 0, 0)) - sdf.Eval(p - new Vector3(h, 0, 0)),
                sdf.Eval(p + new Vector3(0, h, 0)) - sdf.Eval(p - new Vector3(0, h, 0)),
                sdf.Eval(p + new Vector3(0, 0, h)) - sdf.Eval(p - new Vector3(0, 0, h)));
        }
    }
}
