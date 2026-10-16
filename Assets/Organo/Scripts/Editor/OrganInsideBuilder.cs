using System;
using System.Collections.Generic;
using System.Linq;
using Organo.Data;
using Organo.ThreeD;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Organo.EditorTools
{
    /// <summary>
    /// SDF organ yang dipotong (tampilan "Lihat dalam"): bagian depan dibuang oleh bidang potong,
    /// rongga di dalamnya dilubangi, permukaan potongan diwarnai per lapisan (mis. korteks / medula ginjal),
    /// dan struktur di dalam (katup, pohon bronkus, saluran) ditambahkan.
    /// </summary>
    public class CutawaySdf : ISdf
    {
        public ISdf Outer;
        /// <summary>Jarak bertanda ke bidang potong: &gt; 0 disimpan, &lt; 0 dibuang.</summary>
        public Func<Vector3, float> Cut;
        /// <summary>Rongga tambahan (&lt; 0 = berongga), mis. bilik jantung.</summary>
        public Func<Vector3, float> Carve;
        /// <summary>Dinding organ berongga setebal ini (0 = organ padat).</summary>
        public float Shell;
        public Func<Vector3, float> ShellDisp;
        public CompositeSdf Interior;
        /// <summary>Rongga juga melubangi struktur dalam (mis. lumen di pohon bronkus). Katup/konka: false.</summary>
        public bool CarveInterior;
        public Func<Vector3, int> FaceMaterial;
        public Func<Vector3, int> CavityMaterial;
        public float Band = 0.012f;
        readonly int m_Count;
        public int MaterialCount => m_Count;
        public CutawaySdf(int count) { m_Count = count; }

        float CarveAt(Vector3 p, float d0)
        {
            float c = float.MaxValue;
            if (Shell > 0f) c = d0 + Shell + (ShellDisp != null ? ShellDisp(p) : 0f);
            if (Carve != null) c = Mathf.Min(c, Carve(p));
            return c;
        }

        public float Eval(Vector3 p)
        {
            float d0 = Outer.Eval(p);
            float carve = CarveAt(p, d0);
            float cut = Cut(p);
            float d = Mathf.Max(Mathf.Max(d0, -carve), -cut);
            if (Interior != null) d = Mathf.Min(d, Mathf.Max(CarveInterior ? Mathf.Max(Interior.Eval(p), -carve) : Interior.Eval(p), -cut));
            return d;
        }

        public int Material(Vector3 p)
        {
            float d0 = Outer.Eval(p);
            float carve = CarveAt(p, d0);
            float cut = Cut(p);
            float body = Mathf.Max(Mathf.Max(d0, -carve), -cut);
            if (Interior != null)
            {
                float di = Interior.Eval(p);
                if (di < body + 0.003f && di < Band) return Interior.Material(p);
            }
            if (cut < Band && d0 < -Band * 0.5f && carve > Band * 0.5f)
                return FaceMaterial != null ? FaceMaterial(p) : Outer.Material(p);
            if (carve < Band && d0 < 0f) return CavityMaterial != null ? CavityMaterial(p) : Outer.Material(p);
            return Outer.Material(p);
        }
    }

    public static class OrganInsideBuilder
    {
        const string ModelsDir = "Assets/Organo/Models";

        // ---------------------------------------------------------------- bentuk dasar

        static float Ell(Vector3 p, Vector3 c, Vector3 r)
        {
            var q = new Vector3((p.x - c.x) / r.x, (p.y - c.y) / r.y, (p.z - c.z) / r.z);
            return (q.magnitude - 1f) * Mathf.Min(r.x, Mathf.Min(r.y, r.z));
        }

        static float Seg(Vector3 p, Vector3 a, Vector3 b, float r)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-8f));
            return (p - (a + ab * t)).magnitude - r;
        }

        static float Poly(Vector3 p, Vector3[] pts, float r)
        {
            float d = float.MaxValue;
            for (int i = 0; i < pts.Length - 1; i++) d = Mathf.Min(d, Seg(p, pts[i], pts[i + 1], r));
            return d;
        }

        static Vector4[] Ring(Vector3 c, float radius, float tube, Vector3 axis, int n = 14)
        {
            var u = Vector3.Cross(axis, Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            var v = Vector3.Cross(axis, u).normalized;
            var pts = new Vector4[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                var p = c + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * radius;
                pts[i] = new Vector4(p.x, p.y, p.z, tube);
            }
            return pts;
        }

        static (CompositeSdf sdf, Bounds bounds, float cell, (string name, Color32 color, float smooth)[] colors, CompositeSdf inner) Get(string id, Action build)
        {
            if (!OrganModelBuilder.Built.ContainsKey(id)) build();
            return OrganModelBuilder.Built[id];
        }

        static (string, Color32, float) C(string name, string hex, float smooth = 0.5f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return (name, (Color32)c, smooth);
        }

        /// <summary>Simpan mesh potongan sebagai prefab "{folder}_Dalam" dan pasang ke slot insidePrefab organ.</summary>
        static GameObject Save(string organId, string folder, CutawaySdf sdf, Bounds bounds, float cell,
            (string name, Color32 color, float smooth)[] colors, (string title, string desc, Vector3 pos)[] hotspots, Vector3 insideEuler)
        {
            sdf.Interior?.SetMaterialCount(colors.Length);
            var dir = ModelsDir + "/" + folder;
            StomachModelBuilder.EnsureFolder(dir);
            var mats = new Material[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                mats[i] = OrganModelBuilder.MakeMat(dir + "/" + folder + "_" + colors[i].name + ".mat", colors[i].color, colors[i].smooth);
            var name = folder + "_Dalam";
            var mesh = SurfaceNets.Build(sdf, bounds, cell);
            mesh.name = name;
            var meshPath = dir + "/" + name + "_Mesh.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null) AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);
            var root = new GameObject(name);
            var body = new GameObject("Mesh", typeof(MeshFilter), typeof(MeshRenderer));
            body.transform.SetParent(root.transform, false);
            body.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = body.GetComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = ShadowCastingMode.On;
            foreach (var (title, desc, pos) in hotspots)
            {
                var go = new GameObject("Hotspot " + title, typeof(OrganHotspot));
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = pos;
                var h = go.GetComponent<OrganHotspot>();
                h.title = title; h.description = desc;
            }
            var prefabPath = dir + "/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var organ = AssetDatabase.LoadAssetAtPath<OrganInfo>("Assets/Organo/Data/Organ_" + organId + ".asset");
            if (organ != null)
            {
                organ.insidePrefab = prefab;
                organ.insideEuler = insideEuler;
                EditorUtility.SetDirty(organ);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Organo] Model dalam " + name + " dibuat (" + mesh.vertexCount + " verteks, " + StomachModelBuilder.TriCount(mesh) + " segitiga).");
            return prefab;
        }

        static (string, Color32, float)[] With((string name, Color32 color, float smooth)[] baseColors, params (string, Color32, float)[] extra) =>
            baseColors.Concat(extra).ToArray();

        // ================================================================ semua

        [MenuItem("Organo/Model 3D/Buat Model Lihat Dalam (potongan organ)", priority = 41)]
        public static void BuildAll()
        {
            OrganoSetup.EnsureDatabase();
            Heart(); Lungs(); Stomach(); SmallIntestine(); LargeIntestine(); Esophagus();
            Kidney(); Liver(); Pancreas(); Brain(); Tooth(); EyeInside(); NoseInside(); ThroatInside();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Bangun ulang usus halus (model, potongan dalam) dan gambar bagian organ.</summary>
        public static void RebuildSmallIntestine()
        {
            OrganoSetup.EnsureDatabase();
            OrganModelBuilder.BuildSmallIntestine();
            SmallIntestine();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DetailAssetsBuilder.RenderPartImages();
            AssetDatabase.SaveAssets();

        }

        public static void RebuildPancreas()
        {
            OrganModelBuilder.BuildPancreas();
            Pancreas();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>Bangun ulang organ tertentu beserta potongannya (lebih cepat dari BuildAll).</summary>
        public static void RebuildMouthEyeIntestine()
        {
            OrganNewModels.BuildMouth();
            OrganNewModels.BuildEye();
            EyeInside();
            SmallIntestine();
            AssetDatabase.SaveAssets();
        }

        // ================================================================ JANTUNG: empat ruang + katup

        static void Heart()
        {
            var b = Get("jantung", OrganModelBuilder.BuildHeart);
            var aorta = new[] { new Vector3(0.0f, 0.18f, 0.02f), new Vector3(-0.01f, 0.4f, 0f), new Vector3(0.05f, 0.56f, 0.02f), new Vector3(0.18f, 0.6f, 0.06f), new Vector3(0.29f, 0.5f, 0.12f), new Vector3(0.3f, 0.32f, 0.18f) };
            Func<Vector3, float> chambers = p => Mathf.Min(Mathf.Min(Mathf.Min(
                Ell(p, new Vector3(-0.2f, 0.13f, 0.01f), new Vector3(0.11f, 0.1f, 0.1f)),          // serambi kanan
                Ell(p, new Vector3(0.18f, 0.15f, 0.05f), new Vector3(0.1f, 0.09f, 0.09f))),       // serambi kiri
                Mathf.Min(Ell(p, new Vector3(-0.1f, -0.12f, -0.01f), new Vector3(0.11f, 0.16f, 0.1f)),  // bilik kanan
                          Ell(p, new Vector3(0.14f, -0.15f, 0.01f), new Vector3(0.1f, 0.21f, 0.1f)))), // bilik kiri
                Mathf.Min(Mathf.Min(Seg(p, new Vector3(0.11f, 0.0f, 0.01f), new Vector3(0.0f, 0.2f, 0.02f), 0.045f), Poly(p, aorta, 0.058f)),
                          Seg(p, new Vector3(-0.22f, 0.6f, 0.06f), new Vector3(-0.21f, 0.16f, 0.03f), 0.045f)));
            var valves = new CompositeSdf(0.01f);
            valves.Tube(Ring(new Vector3(-0.13f, 0.035f, 0.0f), 0.055f, 0.012f, Vector3.up), 10, smooth: 0.005f);
            valves.Tube(Ring(new Vector3(0.14f, 0.055f, 0.03f), 0.05f, 0.012f, Vector3.up), 10, smooth: 0.005f);
            valves.Tube(Ring(new Vector3(0.04f, 0.17f, 0.02f), 0.045f, 0.01f, new Vector3(-0.45f, 1f, 0f).normalized), 10, smooth: 0.005f);
            var sdf = new CutawaySdf(11)
            {
                Outer = b.sdf, Cut = p => p.z + 0.02f, Carve = chambers, Interior = valves,
                FaceMaterial = _ => 9,
                // kanan (biru, darah kurang oksigen) / kiri (merah, darah kaya oksigen); atas serambi, bawah bilik
                CavityMaterial = p => p.x < 0.0f ? (p.y > 0.03f ? 5 : 6) : (p.y > 0.05f ? 7 : 8),
            };
            Save("jantung", "Jantung", sdf, b.bounds, b.cell,
                With(b.colors, C("SerambiKanan", "#8FB0F0", 0.55f), C("BilikKanan", "#5B7FE0", 0.55f), C("SerambiKiri", "#F08A94", 0.55f),
                    C("BilikKiri", "#E04450", 0.55f), C("PotonganOtot", "#C9475B", 0.4f), C("Katup", "#F7E3C5", 0.6f)),
                new[]
                {
                    ("Serambi kanan", "Menerima darah dari tubuh", new Vector3(-0.21f, 0.14f, -0.05f)),
                    ("Bilik kanan", "Memompa darah ke paru-paru", new Vector3(-0.1f, -0.13f, -0.05f)),
                    ("Serambi kiri", "Menerima darah dari paru-paru", new Vector3(0.18f, 0.16f, -0.05f)),
                    ("Bilik kiri", "Memompa darah ke seluruh tubuh", new Vector3(0.14f, -0.17f, -0.05f)),
                    ("Katup", "Pintu satu arah antar-ruang", new Vector3(-0.13f, 0.035f, -0.05f)),
                }, new Vector3(5f, -6f, 0f));
        }

        // ================================================================ PARU: jaringan seperti spons + saluran udara berongga

        static void Lungs()
        {
            var b = Get("paru", OrganModelBuilder.BuildLungs);
            var tree = b.inner;
            var airway = new[] { new Vector3(0f, 0.2f, 0.035f), new Vector3(0f, 0.72f, 0.025f), new Vector3(0f, 0.86f, 0.045f), new Vector3(0f, 1.0f, 0.08f), new Vector3(0f, 1.13f, 0.07f) };
            Func<Vector3, float> lumen = p => Mathf.Min(Poly(p, airway, 0.04f), tree != null ? tree.Eval(p) + 0.013f : 1f);
            var sdf = new CutawaySdf(b.colors.Length + 2)
            {
                Outer = b.sdf,
                // hidung (di depan atas) tetap utuh, sisanya dipotong sedikit di belakang garis tengah trakea
                Cut = p => p.y > 1.05f && p.z < 0.02f ? 1f : p.z - 0.025f,
                Carve = lumen, Interior = tree, CarveInterior = true,
                FaceMaterial = p => b.sdf.Material(p) is var m && m != 0 ? m : b.colors.Length,
                CavityMaterial = _ => b.colors.Length + 1,
            };
            Save("paru", "ParuParu", sdf, b.bounds, b.cell,
                With(b.colors, C("JaringanParu", "#F6AEBB", 0.35f), C("Lumen", "#9E3A55", 0.5f)),
                new[]
                {
                    ("Rongga trakea", "Jalan udara yang selalu terbuka", new Vector3(0f, 0.5f, -0.01f)),
                    ("Bronkus", "Bercabang seperti ranting pohon", new Vector3(-0.12f, 0.13f, -0.01f)),
                    ("Jaringan paru", "Penuh kantong udara alveolus", new Vector3(-0.34f, -0.2f, -0.01f)),
                }, new Vector3(4f, -12f, 0f));
        }

        // ================================================================ LAMBUNG: dinding tebal berlipat + bubur makanan

        static void Stomach()
        {
            var outer = new StomachSdf();
            var baseColors = new (string, Color32, float)[]
            {
                ("Dinding", new Color32(0xD6, 0x5F, 0x58, 0xFF), 0.62f), ("Dalam", new Color32(0x7E, 0x24, 0x2A, 0xFF), 0.35f),
                ("Kardia", new Color32(0xE8, 0x87, 0x7F, 0xFF), 0.62f), ("Fundus", new Color32(0xE4, 0x70, 0x6A, 0xFF), 0.62f),
                ("Pilorus", new Color32(0xBF, 0x4A, 0x4E, 0xFF), 0.62f), ("Duodenum", new Color32(0xEF, 0x9A, 0x7C, 0xFF), 0.6f),
            };
            var food = new CompositeSdf(0.04f);
            food.Sphere(new Vector3(0.2f, -0.22f, 0.06f), 0.085f, 8);
            food.Sphere(new Vector3(0.04f, -0.3f, 0.07f), 0.065f, 8);
            food.Sphere(new Vector3(0.32f, -0.06f, 0.09f), 0.06f, 8);
            var sdf = new CutawaySdf(9)
            {
                Outer = outer, Cut = p => p.z, Shell = 0.045f,
                ShellDisp = p => 0.013f * Mathf.Max(0f, Mathf.Sin((p.x * 0.55f - p.y) * 38f)),
                Interior = food, FaceMaterial = _ => 7, CavityMaterial = _ => 6,
            };
            Save("lambung", "Lambung", sdf, new Bounds(new Vector3(-0.05f, 0.02f, 0f), new Vector3(1.8f, 1.85f, 0.95f)), 0.0135f,
                With(baseColors, C("Lipatan", "#E9828D", 0.55f), C("DindingOtot", "#C04A50", 0.4f), C("Makanan", "#F4D58A", 0.45f)),
                new[]
                {
                    ("Lipatan lambung", "Membuat lambung bisa melar", new Vector3(0.36f, 0.16f, -0.04f)),
                    ("Dinding berotot", "Meremas & mengaduk makanan", new Vector3(0.6f, -0.05f, -0.04f)),
                    ("Bubur makanan", "Makanan yang sedang dicerna", new Vector3(0.2f, -0.22f, -0.04f)),
                }, new Vector3(4f, -10f, 0f));
        }

        // ================================================================ USUS: berongga, dinding dalam berjonjot

        static void SmallIntestine()
        {
            var b = Get("usushalus", OrganModelBuilder.BuildSmallIntestine);
            var sdf = new CutawaySdf(b.colors.Length + 2)
            {
                Outer = b.sdf, Cut = p => p.z + 0.07f, Shell = 0.015f,
                ShellDisp = p => 0.005f * Mathf.Sin(p.x * 70f) * Mathf.Sin(p.y * 70f) * Mathf.Sin(p.z * 70f),
                FaceMaterial = _ => b.colors.Length + 1, CavityMaterial = _ => b.colors.Length,
            };
            Save("usushalus", "UsusHalus", sdf, b.bounds, b.cell * 1.3f,
                With(b.colors, C("ViliDalam", "#F7B9A0", 0.4f), C("DindingUsus", "#D2645F", 0.45f)),
                new[]
                {
                    ("Vili", "Jonjot penyerap sari makanan", new Vector3(0.05f, 0.1f, -0.1f)),
                    ("Rongga usus", "Jalan bubur makanan", new Vector3(-0.15f, -0.1f, -0.1f)),
                }, new Vector3(4f, -8f, 0f));
        }

        static void LargeIntestine()
        {
            var b = Get("ususbesar", OrganModelBuilder.BuildLargeIntestine);
            var sdf = new CutawaySdf(b.colors.Length + 2)
            {
                Outer = b.sdf, Cut = p => p.z, Shell = 0.026f,
                FaceMaterial = _ => b.colors.Length + 1, CavityMaterial = _ => b.colors.Length,
            };
            Save("ususbesar", "UsusBesar", sdf, b.bounds, b.cell,
                With(b.colors, C("DalamUsus", "#EBA49C", 0.45f), C("DindingUsus", "#C24E5A", 0.45f)),
                new[]
                {
                    ("Rongga usus besar", "Air diserap dari sisa makanan", new Vector3(-0.43f, 0.05f, -0.03f)),
                    ("Kantong haustra", "Kantong-kantong di dinding", new Vector3(0.1f, 0.36f, -0.03f)),
                }, new Vector3(4f, -6f, 0f));
        }

        static void Esophagus()
        {
            var b = Get("kerongkongan", OrganModelBuilder.BuildEsophagus);
            var bolus = new CompositeSdf(0.02f);
            bolus.Sphere(new Vector3(0.0f, 0.04f, 0.01f), 0.062f, b.colors.Length + 2, scale: new Vector3(1f, 1.2f, 1f));
            var sdf = new CutawaySdf(b.colors.Length + 3)
            {
                Outer = b.sdf, Cut = p => p.z, Shell = 0.03f,
                ShellDisp = p => 0.009f * Mathf.Max(0f, Mathf.Sin(Mathf.Atan2(p.z, p.x - 0.02f) * 9f)),
                Interior = bolus,
                FaceMaterial = _ => b.colors.Length + 1, CavityMaterial = _ => b.colors.Length,
            };
            Save("kerongkongan", "Kerongkongan", sdf, b.bounds, b.cell,
                With(b.colors, C("LipatanDalam", "#F3A6B1", 0.5f), C("DindingOtot", "#C8506A", 0.45f), C("Gumpalan", "#F4D58A", 0.45f)),
                new[]
                {
                    ("Gumpalan makanan", "Didorong turun oleh otot", new Vector3(0f, 0.04f, -0.05f)),
                    ("Lipatan dalam", "Membuat kerongkongan bisa melebar", new Vector3(0.02f, -0.25f, -0.03f)),
                    ("Dinding otot", "Meremas seperti ombak", new Vector3(0.08f, 0.25f, -0.03f)),
                }, new Vector3(0f, -10f, 0f));
        }

        // ================================================================ GINJAL: korteks, medula (piramida), pelvis

        static void Kidney()
        {
            var b = Get("ginjal", OrganModelBuilder.BuildKidney);
            int n = b.colors.Length;
            Func<Vector3, float> pelvis = p =>
            {
                float d = float.MaxValue;
                foreach (var side in new[] { -1f, 1f })
                {
                    float y0 = side > 0 ? 0.04f : 0f;
                    d = Mathf.Min(d, Ell(p, new Vector3(side * 0.2f, y0 - 0.03f, 0.045f), new Vector3(0.05f, 0.075f, 0.045f)));
                    d = Mathf.Min(d, Seg(p, new Vector3(side * 0.2f, y0 - 0.06f, 0.04f), new Vector3(side * 0.17f, y0 - 0.2f, 0.03f), 0.009f));
                }
                return d;
            };
            var sdf = new CutawaySdf(n + 3)
            {
                Outer = b.sdf, Cut = p => p.z - 0.04f, Carve = pelvis, CavityMaterial = _ => n + 2,
                FaceMaterial = p =>
                {
                    int m = b.sdf.Material(p);
                    if (m != 0) return m;
                    float side = p.x < 0 ? -1f : 1f;
                    float y0 = side > 0 ? 0.04f : 0f;
                    float d0 = b.sdf.Eval(p);
                    if (d0 > -0.032f) return n; // korteks
                    var v = p - new Vector3(side * 0.2f, y0 - 0.03f, 0.04f);
                    float ang = Mathf.Atan2(v.y, v.x * side);
                    return Mathf.Sin(ang * 6f) > -0.15f && v.magnitude > 0.07f ? n + 1 : n;
                },
            };
            Save("ginjal", "Ginjal", sdf, b.bounds, b.cell,
                With(b.colors, C("Korteks", "#D45A47", 0.45f), C("Medula", "#8C2C3E", 0.45f), C("Pelvis", "#F5CB7A", 0.5f)),
                new[]
                {
                    ("Korteks", "Lapisan luar, tempat nefron menyaring", new Vector3(-0.42f, 0.14f, 0.0f)),
                    ("Medula", "Piramida pengumpul urine", new Vector3(-0.33f, -0.02f, 0.0f)),
                    ("Pelvis ginjal", "Corong urine menuju ureter", new Vector3(-0.2f, -0.03f, 0.0f)),
                }, new Vector3(4f, 0f, 0f));
        }

        // ================================================================ HATI: jaringan + pembuluh & saluran empedu

        static void Liver()
        {
            var b = Get("hati", OrganModelBuilder.BuildLiver);
            int n = b.colors.Length;
            var vessels = new CompositeSdf(0.012f);
            var hub = new Vector3(-0.03f, -0.04f, 0.0f);
            var ends = new[] { new Vector3(-0.36f, 0.06f, 0f), new Vector3(-0.27f, -0.13f, 0f), new Vector3(-0.17f, 0.17f, 0f), new Vector3(0.25f, 0.09f, 0f), new Vector3(0.42f, 0.07f, 0f) };
            foreach (var e in ends)
            {
                var mid = Vector3.Lerp(hub, e, 0.5f) + new Vector3(0f, 0.03f, 0f);
                vessels.Tube(new[] { new Vector4(hub.x, hub.y, 0f, 0.026f), new Vector4(mid.x, mid.y, 0f, 0.02f), new Vector4(e.x, e.y, 0f, 0.012f) }, 4, smooth: 0.01f);
                vessels.Tube(new[] { new Vector4(hub.x, hub.y + 0.03f, 0f, 0.012f), new Vector4(mid.x, mid.y + 0.03f, 0f, 0.01f), new Vector4(e.x, e.y + 0.025f, 0f, 0.007f) }, 5, smooth: 0.006f);
                vessels.Tube(new[] { new Vector4(hub.x, hub.y - 0.03f, 0f, 0.011f), new Vector4(mid.x, mid.y - 0.025f, 0f, 0.009f), new Vector4(e.x, e.y - 0.022f, 0f, 0.006f) }, 3, smooth: 0.006f);
            }
            var sdf = new CutawaySdf(n + 1) { Outer = b.sdf, Cut = p => p.z, Interior = vessels, FaceMaterial = _ => n };
            Save("hati", "Hati", sdf, b.bounds, b.cell, With(b.colors, C("JaringanHati", "#B93A45", 0.45f)),
                new[]
                {
                    ("Sel-sel hati", "Membersihkan darah & membuat empedu", new Vector3(-0.3f, -0.05f, -0.03f)),
                    ("Pembuluh darah", "Membawa darah masuk ke hati", new Vector3(-0.2f, 0.07f, -0.03f)),
                    ("Saluran empedu", "Mengalirkan empedu (hijau)", new Vector3(0.12f, 0.02f, -0.03f)),
                }, new Vector3(6f, -8f, 0f));
        }

        // ================================================================ PANKREAS: saluran + pulau Langerhans

        static void Pancreas()
        {
            var b = Get("pankreas", OrganModelBuilder.BuildPancreas);
            int n = b.colors.Length;
            var inside = new CompositeSdf(0.01f);
            var duct = new List<Vector4>();
            for (int i = 0; i < OrganModelBuilder.PancreasDuct.Length; i++)
            {
                var q = OrganModelBuilder.PancreasDuct[i];
                duct.Add(new Vector4(q.x, q.y, q.z, Mathf.Lerp(0.01f, 0.018f, i / (float)(OrganModelBuilder.PancreasDuct.Length - 1))));
            }
            inside.Tube(duct.ToArray(), n, smooth: 0.008f);
            foreach (var q in new[] { new Vector3(0.4f, 0.21f, 0.02f), new Vector3(0.3f, 0.1f, 0.01f), new Vector3(0.18f, 0.14f, 0.0f), new Vector3(0.04f, 0.05f, 0.0f),
                                      new Vector3(-0.12f, 0.1f, 0.0f), new Vector3(-0.2f, -0.06f, 0.0f), new Vector3(-0.27f, 0.07f, 0.0f), new Vector3(-0.15f, -0.13f, 0.01f) })
                inside.Sphere(q, 0.017f, n + 2);
            var duo = OrganModelBuilder.PancreasDuodenum;
            var sdf = new CutawaySdf(n + 4)
            {
                Outer = b.sdf, Cut = p => p.z - 0.005f, Carve = p => Poly(p, duo, 0.036f), Interior = inside,
                FaceMaterial = p => { int m = b.sdf.Material(p); return m == 0 || m == 6 || m == 7 ? n + 1 : m; },
                CavityMaterial = _ => n + 3,
            };
            Save("pankreas", "Pankreas", sdf, b.bounds, b.cell,
                With(b.colors, C("Saluran", "#8FCB6E", 0.5f), C("JaringanPankreas", "#F3D5B0", 0.4f), C("PulauLangerhans", "#F28FAB", 0.5f), C("RonggaUsus", "#E58A90", 0.45f)),
                new[]
                {
                    ("Saluran pankreas", "Mengalirkan cairan pencernaan ke usus", new Vector3(0.1f, 0.1f, -0.03f)),
                    ("Pulau Langerhans", "Kelompok sel pembuat insulin", new Vector3(0.3f, 0.1f, -0.03f)),
                    ("Rongga usus", "Tempat cairan pankreas masuk", new Vector3(-0.44f, 0.03f, -0.03f)),
                }, new Vector3(6f, -6f, 0f));
        }

        // ================================================================ OTAK: potongan tengah (abu-abu, putih, korpus kalosum)

        static void Brain()
        {
            var b = Get("otak", OrganModelBuilder.BuildBrain);
            int n = b.colors.Length;
            var sdf = new CutawaySdf(n + 5)
            {
                Outer = b.sdf, Cut = p => -0.02f - p.x,
                FaceMaterial = p =>
                {
                    int m = b.sdf.Material(p);
                    float d0 = b.sdf.Eval(p);
                    if (m == 4) return d0 > -0.018f || Mathf.Sin(p.y * 70f + p.z * 25f) > 0.55f ? 4 : n + 3; // otak kecil: "pohon hidup"
                    if (m == 5) return n + 4;
                    float e = Mathf.Pow((p.z - 0.03f) / 0.2f, 2f) + Mathf.Pow((p.y - 0.0f) / 0.1f, 2f);
                    if (e > 0.62f && e < 1f && p.y > -0.01f) return n + 2; // korpus kalosum
                    return d0 > -0.03f ? n : n + 1;
                },
            };
            Save("otak", "Otak", sdf, b.bounds, b.cell,
                With(b.colors, C("MateriAbu", "#E3A1B0", 0.4f), C("MateriPutih", "#FBF0E6", 0.4f), C("KorpusKalosum", "#F4DA8E", 0.45f),
                    C("PohonHidup", "#FFF4D6", 0.4f), C("BatangDalam", "#E9A3A3", 0.45f)),
                new[]
                {
                    ("Materi abu-abu", "Lapisan luar tempat berpikir", new Vector3(0.01f, 0.3f, 0.0f)),
                    ("Materi putih", "Kabel penghubung pesan", new Vector3(0.01f, 0.12f, -0.17f)),
                    ("Korpus kalosum", "Jembatan otak kiri & kanan", new Vector3(0.01f, 0.08f, 0.03f)),
                    ("Otak kecil", "Menjaga keseimbangan", new Vector3(0.01f, -0.2f, 0.27f)),
                    ("Batang otak", "Mengatur napas & detak jantung", new Vector3(0.01f, -0.33f, 0.16f)),
                }, new Vector3(6f, 72f, 0f));
        }

        // ================================================================ GIGI (dalam mulut): email, dentin, pulpa, akar

        static void Tooth()
        {
            // satu gigi geraham besar terbelah dua, tertanam di gusi & tulang rahang
            Func<Vector3, float> toothD = p =>
            {
                float crown = Ell(p, new Vector3(0f, 0.24f, 0f), new Vector3(0.2f, 0.16f, 0.17f));
                foreach (var cx in new[] { -0.09f, 0.09f })
                    foreach (var cz in new[] { -0.07f, 0.07f })
                        crown = Mathf.Min(crown, (p - new Vector3(cx, 0.36f, cz)).magnitude - 0.075f);
                float neck = Seg(p, new Vector3(0f, 0.18f, 0f), new Vector3(0f, 0.04f, 0f), 0.15f);
                float roots = Mathf.Min(Seg(p, new Vector3(-0.08f, 0.06f, 0f), new Vector3(-0.11f, -0.46f, 0f), 0.07f),
                                        Seg(p, new Vector3(0.08f, 0.06f, 0f), new Vector3(0.11f, -0.46f, 0f), 0.07f));
                return Mathf.Min(Mathf.Min(crown, neck), roots);
            };
            Func<Vector3, float> pulp = p => Mathf.Min(Ell(p, new Vector3(0f, 0.14f, 0f), new Vector3(0.09f, 0.08f, 0.08f)),
                Mathf.Min(Seg(p, new Vector3(-0.06f, 0.1f, 0f), new Vector3(-0.1f, -0.44f, 0f), 0.022f), Seg(p, new Vector3(0.06f, 0.1f, 0f), new Vector3(0.1f, -0.44f, 0f), 0.022f)));
            Func<Vector3, float> nerve = p => Mathf.Min(Ell(p, new Vector3(0f, 0.14f, 0f), new Vector3(0.035f, 0.03f, 0.03f)),
                Mathf.Min(Seg(p, new Vector3(-0.06f, 0.1f, 0f), new Vector3(-0.1f, -0.44f, 0f), 0.008f), Seg(p, new Vector3(0.06f, 0.1f, 0f), new Vector3(0.1f, -0.44f, 0f), 0.008f)));
            var tooth = new ToothSdf(toothD);
            var sdf = new CutawaySdf(7)
            {
                Outer = tooth, Cut = p => p.z,
                FaceMaterial = p =>
                {
                    if (p.y < -0.02f && tooth.Gum(p) < 0f && toothD(p) > 0f) return 3;
                    if (tooth.Bone(p) < 0f && toothD(p) > 0f) return 4;
                    if (pulp(p) < 0f) return nerve(p) < 0f ? 5 : 2;
                    float d = toothD(p);
                    if (p.y > 0.13f) return d > -0.04f ? 0 : 1;
                    return d > -0.018f ? 6 : 1;
                },
            };
            Save("mulut", "Mulut", sdf, new Bounds(new Vector3(0f, -0.06f, 0.03f), new Vector3(0.86f, 0.98f, 0.5f)), 0.0075f,
                new[]
                {
                    C("Email", "#FFFFFF", 0.8f), C("Dentin", "#F7E7C4", 0.5f), C("Pulpa", "#F28FAB", 0.45f), C("GusiDalam", "#F2A0A8", 0.45f),
                    C("TulangRahang", "#EDE3D6", 0.35f), C("SarafGigi", "#E0435A", 0.5f), C("Sementum", "#E9D6A8", 0.45f),
                },
                new[]
                {
                    ("Email gigi", "Lapisan terkeras di tubuhmu", new Vector3(0.12f, 0.33f, -0.03f)),
                    ("Dentin", "Lapisan kuat di bawah email", new Vector3(-0.12f, 0.2f, -0.03f)),
                    ("Pulpa", "Berisi saraf & pembuluh darah", new Vector3(0f, 0.14f, -0.03f)),
                    ("Akar gigi", "Menancap kuat di tulang rahang", new Vector3(0.11f, -0.3f, -0.03f)),
                    ("Gusi", "Memeluk leher gigi", new Vector3(-0.3f, -0.05f, -0.03f)),
                }, new Vector3(8f, -14f, 0f));
        }

        /// <summary>Gigi + gusi + tulang rahang (untuk potongan gigi).</summary>
        class ToothSdf : ISdf
        {
            readonly Func<Vector3, float> m_Tooth;
            public ToothSdf(Func<Vector3, float> tooth) { m_Tooth = tooth; }
            public float Gum(Vector3 p) => Ell(p, new Vector3(0f, -0.04f, 0f), new Vector3(0.38f, 0.1f, 0.2f));
            public float Bone(Vector3 p) => Mathf.Max(Ell(p, new Vector3(0f, -0.32f, 0f), new Vector3(0.36f, 0.28f, 0.19f)), p.y + 0.04f);
            public float Eval(Vector3 p) => Mathf.Min(m_Tooth(p), Mathf.Min(Gum(p), Bone(p)));
            public int Material(Vector3 p)
            {
                float t = m_Tooth(p), g = Gum(p), b = Bone(p);
                if (t <= g && t <= b) return p.y > 0.08f ? 0 : 6;
                return g <= b ? 3 : 4;
            }
            public int MaterialCount => 7;
        }

        // ================================================================ MATA (potongan samping)

        static void EyeInside()
        {
            var b = Get("mata", OrganNewModels.BuildEye);
            int n = b.colors.Length; // 0 sklera, 1 kornea, 2 iris, 3 pupil, 4 otot, 5 saraf
            var sdf = new CutawaySdf(n + 4)
            {
                Outer = b.sdf, Cut = p => -p.x,
                FaceMaterial = p =>
                {
                    int m = b.sdf.Material(p);
                    if (m == 4 || m == 5) return m;
                    if (Ell(p, new Vector3(0f, 0f, -0.235f), new Vector3(0.13f, 0.13f, 0.07f)) < 0f) return n;          // lensa
                    float ry = Mathf.Abs(p.y);
                    if (Mathf.Abs(p.z + 0.31f) < 0.018f && ry > 0.07f && ry < 0.22f) return 2;                          // iris
                    float d0 = b.sdf.Eval(p);
                    if (p.z < -0.3f) return d0 > -0.02f ? 1 : n + 3;                                                    // kornea + cairan depan
                    if (d0 > -0.022f) return 0;                                                                         // sklera
                    if (d0 > -0.045f && p.z > -0.22f) return n + 1;                                                     // retina
                    return n + 2;                                                                                       // badan kaca
                },
            };
            Save("mata", "Mata", sdf, b.bounds, b.cell,
                With(b.colors, C("Lensa", "#F7E7A6", 0.75f), C("Retina", "#F28C6B", 0.45f), C("BadanKaca", "#DCEBFB", 0.6f), C("CairanDepan", "#EAF6FF", 0.7f)),
                new[]
                {
                    ("Kornea", "Jendela bening di depan mata", new Vector3(0.03f, 0.0f, -0.44f)),
                    ("Lensa", "Memfokuskan cahaya", new Vector3(0.03f, 0.0f, -0.235f)),
                    ("Badan kaca", "Jeli bening pengisi bola mata", new Vector3(0.03f, 0.05f, 0.05f)),
                    ("Retina", "Layar penangkap cahaya", new Vector3(0.03f, 0.2f, 0.26f)),
                    ("Saraf mata", "Mengirim gambar ke otak", new Vector3(0.03f, 0.0f, 0.6f)),
                }, new Vector3(6f, 70f, 0f));
        }

        // ================================================================ HIDUNG (potongan tengah: konka & daerah penciuman)

        static void NoseInside()
        {
            var b = Get("hidung", OrganNewModels.BuildNose);
            int n = b.colors.Length;
            var sdf = new CutawaySdf(n + 2)
            {
                Outer = b.sdf, Cut = p => -0.012f - p.x, Carve = OrganNewModels.NoseCavity, Interior = b.inner,
                FaceMaterial = p => b.sdf.Material(p) == 0 ? n + 1 : b.sdf.Material(p),
                CavityMaterial = _ => n,
            };
            Save("hidung", "Hidung", sdf, b.bounds, b.cell,
                With(b.colors, C("DindingRongga", "#F4A3AE", 0.5f), C("PotonganHidung", "#F7CDB8", 0.4f)),
                new[]
                {
                    ("Lubang hidung", "Pintu masuk udara", new Vector3(0.01f, -0.03f, -0.25f)),
                    ("Konka", "Rak berlekuk penghangat udara", new Vector3(0.01f, 0.12f, 0.06f)),
                    ("Daerah penciuman", "Sel pencium bau", new Vector3(0.01f, 0.3f, 0.08f)),
                    ("Ke tenggorokan", "Udara turun ke tenggorokan", new Vector3(0.01f, -0.12f, 0.31f)),
                }, new Vector3(4f, 74f, 0f));
        }

        // ================================================================ TENGGOROKAN (potongan tengah: epiglotis & pita suara)

        static void ThroatInside()
        {
            var b = Get("tenggorokan", OrganNewModels.BuildThroat);
            int n = b.colors.Length;
            var cords = new CompositeSdf(0.01f);
            cords.Sphere(new Vector3(-0.022f, -0.02f, -0.01f), 0.04f, 7, scale: new Vector3(0.6f, 0.28f, 1.1f));
            cords.Sphere(new Vector3(0.022f, -0.02f, -0.01f), 0.04f, 7, scale: new Vector3(0.6f, 0.28f, 1.1f));
            var sdf = new CutawaySdf(n + 2)
            {
                Outer = b.sdf, Cut = p => -0.005f - p.x, Carve = OrganNewModels.ThroatLumen, Interior = cords,
                FaceMaterial = p => b.sdf.Material(p) is var m && (m == 3 || m == 4) ? 4 : m,
                CavityMaterial = p => p.z > 0.09f && p.y < 0.06f ? n + 1 : n,
            };
            Save("tenggorokan", "Tenggorokan", sdf, b.bounds, b.cell,
                With(b.colors, C("RonggaNapas", "#F2A7B4", 0.5f), C("RonggaMakanan", "#F7C08A", 0.5f)),
                new[]
                {
                    ("Faring", "Persimpangan udara & makanan", new Vector3(0.01f, 0.3f, 0.12f)),
                    ("Epiglotis", "Tutup saat menelan", new Vector3(0.01f, 0.19f, 0.02f)),
                    ("Pita suara", "Bergetar saat bicara", new Vector3(0.01f, -0.02f, -0.02f)),
                    ("Trakea", "Udara ke paru-paru", new Vector3(0.01f, -0.38f, 0.0f)),
                    ("Kerongkongan", "Makanan ke lambung", new Vector3(0.01f, -0.38f, 0.13f)),
                }, new Vector3(4f, 74f, 0f));
        }
    }
}
