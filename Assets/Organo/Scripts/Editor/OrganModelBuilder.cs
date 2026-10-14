using System;
using System.Collections.Generic;
using System.IO;
using Organo.Data;
using Organo.ThreeD;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Organo.EditorTools
{
    /// <summary>
    /// Model 3D organ yang dibangun langsung di Unity dari bentuk-bentuk halus (SDF), lalu dijadikan mesh
    /// (surface nets) dan dipasang ke slot Model Prefab organ: jantung, paru-paru, ginjal (lambung: StomachModelBuilder).
    /// </summary>
    [InitializeOnLoad]
    public static partial class OrganModelBuilder
    {
        const string ModelsDir = "Assets/Organo/Models";

        static OrganModelBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists("Assets/Organo/Data/OrganDatabase.asset")) return;
                bool missing = false;
                foreach (var f in new[] { "Jantung", "ParuParu", "Ginjal", "Mulut", "Kerongkongan", "UsusHalus", "UsusBesar", "Hati", "Pankreas", "Otak" })
                    missing |= !File.Exists(ModelsDir + "/" + f + "/" + f + ".prefab");
                missing |= !File.Exists("Assets/Organo/Data/Organ_otak.asset") || !File.Exists(ModelsDir + "/Tubuh/AnakLaki.prefab");
                if (missing) BuildAll();
            };
        }

        [MenuItem("Organo/Model 3D/Buat Ulang Semua Model", priority = 29)]
        public static void BuildAll()
        {
            OrganoSetup.EnsureDatabase();
            AssetDatabase.SaveAssets();
            StomachModelBuilder.Build();
            BuildHeart();
            BuildLungs();
            BuildKidney();
            BuildMouth();
            BuildEsophagus();
            BuildSmallIntestine();
            BuildLargeIntestine();
            BuildLiver();
            BuildPancreas();
            BuildBrain();
            OrganNewModels.BuildEye();
            OrganNewModels.BuildNose();
            OrganNewModels.BuildThroat();
            DetailAssetsBuilder.BuildAll();
            OrganInsideBuilder.BuildAll();
            OrganReproModels.BuildAll();
            OrganSkeletonModel.BuildAll();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Hanya organ baru & model potongan (lebih cepat).</summary>
        public static void BuildRound3()
        {
            OrganoSetup.EnsureDatabase();
            AssetDatabase.SaveAssets();
            BuildMouth();
            OrganNewModels.BuildEye();
            OrganNewModels.BuildNose();
            OrganNewModels.BuildThroat();
            OrganInsideBuilder.BuildAll();
            AssetDatabase.SaveAssets();
        }

        public static void BuildSmallIntestineAndPreview() { BuildSmallIntestine(); }

        public static void BuildRound2() { OrganoSetup.EnsureDatabase(); BuildLungs(); BuildSmallIntestine(); DetailAssetsBuilder.BuildMicroModels(); AssetDatabase.SaveAssets(); }

        public static void BuildMicroOnly() { DetailAssetsBuilder.BuildMicroModels(); AssetDatabase.SaveAssets(); }

        public static void BuildLungsAndMicro() { BuildLungs(); DetailAssetsBuilder.BuildMicroModels(); AssetDatabase.SaveAssets(); }

        public static void BuildLungsAndKids() { BuildLungs(); DetailAssetsBuilder.BuildKids(); AssetDatabase.SaveAssets(); }

        /// <summary>Organ yang punya mesh "isi" (bagian dalam yang terlihat/menyala): paru-paru, hidung, reproduksi laki-laki.</summary>
        public static void RebuildInnerOrgans()
        {
            OrganoSetup.EnsureDatabase();
            BuildLungs();
            OrganNewModels.BuildNose();
            OrganReproModels.BuildAll();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Hanya model organ yang baru/diperbarui (lebih cepat daripada membuat ulang semuanya).</summary>
        public static void BuildNewModels()
        {
            OrganoSetup.EnsureDatabase();
            AssetDatabase.SaveAssets();
            BuildKidney();
            BuildMouth();
            BuildEsophagus();
            BuildSmallIntestine();
            BuildLargeIntestine();
            BuildLiver();
            BuildPancreas();
            BuildBrain();
        }

        [MenuItem("Organo/Model 3D/Render Pratinjau Model", priority = 40)]
        public static void RenderPreviews()
        {
            foreach (var (id, folder) in new[]
                     {
                         ("lambung", "Lambung"), ("jantung", "Jantung"), ("paru", "ParuParu"), ("ginjal", "Ginjal"), ("mulut", "Mulut"),
                         ("kerongkongan", "Kerongkongan"), ("usushalus", "UsusHalus"), ("ususbesar", "UsusBesar"), ("hati", "Hati"),
                         ("pankreas", "Pankreas"), ("otak", "Otak"),
                         ("mata", "Mata"), ("hidung", "Hidung"), ("tenggorokan", "Tenggorokan"),
                         ("reprolaki", "ReproLaki"), ("reproperempuan", "ReproPerempuan"), ("rangka", "Rangka")
                     })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelsDir + "/" + folder + "/" + folder + ".prefab");
                Debug.Log("[Organo] pratinjau " + folder + ": " + (prefab != null ? "ok" : "prefab tidak ditemukan") +
                          (prefab != null ? " mesh=" + prefab.GetComponentInChildren<MeshFilter>()?.sharedMesh?.vertexCount : ""));
                if (prefab != null) StomachModelBuilder.RenderPreview(prefab, id);
                var inside = AssetDatabase.LoadAssetAtPath<GameObject>(ModelsDir + "/" + folder + "/" + folder + "_Dalam.prefab");
                if (inside != null) StomachModelBuilder.RenderPreview(inside, id + "_dalam");
            }
        }

        // ================================================================ JANTUNG

        [MenuItem("Organo/Model 3D/Buat Ulang Model Jantung", priority = 31)]
        public static void BuildHeart()
        {
            var sdf = new CompositeSdf(0.07f);
            // 0 otot jantung (ventrikel), 1 serambi, 2 aorta & cabang, 3 vena cava, 4 arteri pulmonalis
            sdf.RoundCone(new Vector3(0.0f, -0.02f, 0f), 0.35f, new Vector3(0.07f, -0.52f, -0.03f), 0.06f, 0, scaleY: 1f);
            sdf.Sphere(new Vector3(-0.06f, 0.06f, 0.02f), 0.32f, 0);
            sdf.Sphere(new Vector3(0.12f, 0.02f, 0.02f), 0.3f, 0);
            sdf.Sphere(new Vector3(-0.28f, 0.16f, 0.06f), 0.17f, 1);
            sdf.Sphere(new Vector3(0.27f, 0.18f, 0.12f), 0.15f, 1);
            // aorta: naik dari tengah, melengkung ke kanan lalu turun ke belakang
            sdf.Tube(new[]
            {
                new Vector4(0.0f, 0.18f, 0.02f, 0.095f), new Vector4(-0.01f, 0.4f, 0.0f, 0.095f),
                new Vector4(0.05f, 0.56f, 0.02f, 0.09f), new Vector4(0.18f, 0.6f, 0.06f, 0.085f),
                new Vector4(0.29f, 0.5f, 0.12f, 0.08f), new Vector4(0.3f, 0.32f, 0.18f, 0.075f)
            }, 2);
            sdf.Tube(new[] { new Vector4(0.03f, 0.58f, 0.02f, 0.042f), new Vector4(0.0f, 0.78f, 0.02f, 0.04f) }, 2, smooth: 0.03f);
            sdf.Tube(new[] { new Vector4(0.12f, 0.62f, 0.04f, 0.04f), new Vector4(0.13f, 0.8f, 0.04f, 0.038f) }, 2, smooth: 0.03f);
            sdf.Tube(new[] { new Vector4(0.21f, 0.6f, 0.07f, 0.036f), new Vector4(0.25f, 0.74f, 0.07f, 0.034f) }, 2, smooth: 0.03f);
            // vena cava superior (kiri layar)
            sdf.Tube(new[] { new Vector4(-0.23f, 0.18f, 0.06f, 0.075f), new Vector4(-0.24f, 0.42f, 0.06f, 0.072f), new Vector4(-0.22f, 0.6f, 0.06f, 0.07f) }, 3);
            sdf.Tube(new[] { new Vector4(-0.24f, 0.5f, 0.06f, 0.05f), new Vector4(-0.34f, 0.6f, 0.08f, 0.045f) }, 3, smooth: 0.03f);
            // arteri pulmonalis: menyilang di depan ke kanan
            sdf.Tube(new[]
            {
                new Vector4(0.02f, 0.1f, -0.2f, 0.075f), new Vector4(0.08f, 0.28f, -0.17f, 0.072f),
                new Vector4(0.24f, 0.36f, -0.12f, 0.066f), new Vector4(0.44f, 0.36f, -0.06f, 0.06f)
            }, 4);
            sdf.Tube(new[] { new Vector4(0.12f, 0.22f, -0.2f, 0.07f), new Vector4(0.3f, 0.2f, -0.17f, 0.09f), new Vector4(0.44f, 0.12f, -0.12f, 0.06f) }, 4, smooth: 0.04f);

            var colors = new[]
            {
                ("Otot", new Color32(0xC4, 0x24, 0x40, 0xFF), 0.72f),
                ("Serambi", new Color32(0xD9, 0x4A, 0x5C, 0xFF), 0.7f),
                ("Aorta", new Color32(0xF0, 0x9A, 0xA6, 0xFF), 0.75f),
                ("VenaCava", new Color32(0xE2, 0x4E, 0x46, 0xFF), 0.7f),
                ("Pulmonalis", new Color32(0xF0, 0x62, 0x4C, 0xFF), 0.72f),
            };
            var hotspots = new[]
            {
                ("Bilik jantung", "Memompa darah ke seluruh tubuh", new Vector3(0.05f, -0.2f, -0.33f)),
                ("Aorta", "Pembuluh darah terbesar", new Vector3(0.12f, 0.62f, -0.02f)),
                ("Serambi", "Tempat darah masuk", new Vector3(-0.32f, 0.18f, -0.08f)),
                ("Arteri paru-paru", "Membawa darah ke paru-paru", new Vector3(0.36f, 0.36f, -0.12f)),
            };
            Build("jantung", "Jantung", sdf, new Bounds(new Vector3(0.05f, 0.12f, 0f), new Vector3(1.1f, 1.5f, 0.9f)), 0.014f,
                colors, hotspots, new Vector3(4f, -14f, 0f), 1.0f);
        }

        // ================================================================ PARU-PARU

        [MenuItem("Organo/Model 3D/Buat Ulang Model Paru-paru", priority = 32)]
        public static void BuildLungs()
        {
            var sdf = LungsSdf();
            // saluran napas atas: hidung, faring; kerongkongan di belakang trakea (hanya model close-up)
            AddAirway(sdf, true);
            var lowSdf = LungsSdf();

            var colors = new[]
            {
                ("Paru", new Color32(0xE8, 0x6C, 0x7E, 0xFF), 0.5f),
                ("Trakea", new Color32(0xD4, 0x4E, 0x66, 0xFF), 0.55f),
                ("TulangRawan", new Color32(0xF4, 0xE4, 0xD4, 0xFF), 0.45f),
                ("Laring", new Color32(0xEE, 0xD9, 0xC2, 0xFF), 0.45f),
                ("Bronkus", new Color32(0xB8, 0x3A, 0x5C, 0xFF), 0.55f),
                ("Hidung", new Color32(0xF2, 0xB8, 0x9A, 0xFF), 0.4f),
                ("Faring", new Color32(0xE9, 0x8F, 0x86, 0xFF), 0.5f),
                ("Kerongkongan", new Color32(0xF0, 0xA8, 0x5C, 0xFF), 0.5f),
            };
            var hotspots = new[]
            {
                ("Hidung", "Udara masuk & disaring", new Vector3(0f, 1.17f, -0.33f)),
                ("Faring", "Persimpangan udara & makanan", new Vector3(0f, 1.06f, 0.0f)),
                ("Laring", "Kotak suara", new Vector3(0f, 0.8f, -0.09f)),
                ("Trakea", "Pipa udara ke paru-paru", new Vector3(0f, 0.5f, -0.07f)),
                ("Bronkus", "Cabang ke paru kanan & kiri", new Vector3(-0.12f, 0.13f, -0.04f)),
                ("Alveolus", "Tukar oksigen & karbon dioksida", new Vector3(-0.34f, -0.15f, -0.22f)),
            };
            // pohon bronkus di dalam paru (terlihat saat bagian Bronkus disorot)
            var tree = new CompositeSdf(0.02f);
            foreach (var side in new[] { -1f, 1f })
            {
                Vector4 V(float x, float y, float z, float r) => new Vector4(side * x, y, z, r);
                tree.Tube(new[] { new Vector4(0f, 0.3f, 0.04f, 0.05f), V(0.12f, 0.12f, 0.05f, 0.045f), V(0.22f, 0.04f, 0.05f, 0.038f) }, 4);
                // cabang lobus atas, tengah, bawah lalu ranting kecil
                var lobes = side < 0
                    ? new[] { new Vector3(-0.27f, 0.26f, 0.02f), new Vector3(-0.42f, 0.02f, -0.02f), new Vector3(-0.38f, -0.34f, 0.0f) }
                    : new[] { new Vector3(0.25f, 0.25f, 0.02f), new Vector3(0.36f, -0.06f, 0.0f), new Vector3(0.38f, -0.34f, 0.02f) };
                int n = 0;
                foreach (var l in lobes)
                {
                    var start = new Vector3(side * 0.22f, 0.04f, 0.05f);
                    var mid = Vector3.Lerp(start, l, 0.55f);
                    tree.Tube(new[] { new Vector4(start.x, start.y, start.z, 0.032f), new Vector4(mid.x, mid.y, mid.z, 0.026f), new Vector4(l.x, l.y, l.z, 0.02f) }, 4, smooth: 0.015f);
                    for (int k = 0; k < 3; k++)
                    {
                        float a = k * 2.1f + n * 0.9f;
                        var tip = l + new Vector3(Mathf.Cos(a) * 0.1f, Mathf.Sin(a) * 0.1f - 0.02f, Mathf.Sin(a * 1.7f) * 0.05f);
                        var tm = Vector3.Lerp(l, tip, 0.5f) + new Vector3(0f, 0.01f, 0f);
                        tree.Tube(new[] { new Vector4(l.x, l.y, l.z, 0.017f), new Vector4(tm.x, tm.y, tm.z, 0.013f), new Vector4(tip.x, tip.y, tip.z, 0.01f) }, 4, smooth: 0.01f);
                    }
                    n++;
                }
            }
            // kerongkongan juga di mesh dalam: terlihat utuh saat disorot (sebagian tertutup paru)
            tree.Tube(new[] { new Vector4(0f, 0.86f, 0.12f, 0.042f), new Vector4(0f, 0.4f, 0.15f, 0.04f), new Vector4(0f, -0.1f, 0.16f, 0.04f), new Vector4(0.04f, -0.5f, 0.15f, 0.04f) }, 7, smooth: 0.01f);
            Build("paru", "ParuParu", sdf, new Bounds(new Vector3(0f, 0.37f, -0.02f), new Vector3(1.4f, 2.0f, 0.86f)), 0.0135f,
                colors, hotspots, new Vector3(4f, -18f, 0f), 1.0f, tree, lowSdf);
        }

        /// <summary>Hidung, faring & kerongkongan di atas laring (bentuknya dipisah tegas, tidak melebur ke paru).</summary>
        static void AddAirway(CompositeSdf sdf, bool esophagus)
        {
            // faring: naik di belakang laring sampai belakang rongga hidung
            sdf.Tube(new[] { new Vector4(0f, 0.86f, 0.05f, 0.05f), new Vector4(0f, 1.0f, 0.08f, 0.056f), new Vector4(0f, 1.13f, 0.07f, 0.058f), new Vector4(0f, 1.2f, 0.02f, 0.055f) }, 6, smooth: 0.03f);
            // rongga hidung ke depan, lalu hidung luar
            sdf.Tube(new[] { new Vector4(0f, 1.2f, 0.02f, 0.055f), new Vector4(0f, 1.22f, -0.1f, 0.052f), new Vector4(0f, 1.2f, -0.22f, 0.05f) }, 5, smooth: 0.03f);
            // hidung luar: batang hidung menurun ke ujung, dengan dua cuping di samping lubang hidung
            sdf.RoundCone(new Vector3(0f, 1.3f, -0.2f), 0.028f, new Vector3(0f, 1.16f, -0.29f), 0.045f, 5);
            foreach (var side in new[] { -1f, 1f })
                sdf.Sphere(new Vector3(side * 0.04f, 1.14f, -0.25f), 0.036f, 5, scale: new Vector3(1f, 0.85f, 1.1f));
            if (esophagus)
                sdf.Tube(new[] { new Vector4(0f, 0.86f, 0.12f, 0.042f), new Vector4(0f, 0.55f, 0.14f, 0.04f), new Vector4(0f, 0.3f, 0.15f, 0.04f) }, 7, smooth: 0.02f);
        }

        static CompositeSdf LungsSdf()
        {
            var sdf = new CompositeSdf(0.16f);
            // 0 paru-paru, 1 trakea (otot), 2 tulang rawan (cincin), 3 laring
            // paru kanan (kiri layar) lebih besar, 3 lobus
            sdf.Sphere(new Vector3(-0.33f, -0.2f, 0f), 0.27f, 0, scale: new Vector3(1f, 1.05f, 0.85f));
            sdf.Sphere(new Vector3(-0.31f, 0.05f, 0f), 0.22f, 0, scale: new Vector3(1f, 1.1f, 0.85f));
            sdf.Sphere(new Vector3(-0.25f, 0.28f, 0f), 0.13f, 0, scale: new Vector3(1f, 1.35f, 0.9f));
            sdf.Sphere(new Vector3(-0.42f, -0.32f, 0.02f), 0.2f, 0);
            // paru kiri (kanan layar) dengan lekukan jantung
            sdf.Sphere(new Vector3(0.31f, -0.2f, 0f), 0.25f, 0, scale: new Vector3(1f, 1.05f, 0.85f));
            sdf.Sphere(new Vector3(0.29f, 0.05f, 0f), 0.2f, 0, scale: new Vector3(1f, 1.1f, 0.85f));
            sdf.Sphere(new Vector3(0.23f, 0.27f, 0f), 0.12f, 0, scale: new Vector3(1f, 1.35f, 0.9f));
            sdf.Sphere(new Vector3(0.4f, -0.32f, 0.02f), 0.19f, 0);
            sdf.Subtract(p => (p - new Vector3(0.1f, -0.22f, -0.12f)).magnitude - 0.12f, 0.08f);
            // celah antar lobus (alur dangkal di permukaan)
            sdf.Groove(new Vector3(-0.3f, -0.02f, 0f), new Vector3(0.45f, 1f, 0f).normalized, 0.03f, new Vector3(-0.32f, 0f, 0f), 0.34f);
            sdf.Groove(new Vector3(-0.32f, -0.18f, 0f), new Vector3(-0.2f, 1f, 0f).normalized, 0.03f, new Vector3(-0.42f, -0.18f, 0f), 0.2f);
            sdf.Groove(new Vector3(0.28f, -0.04f, 0f), new Vector3(-0.5f, 1f, 0f).normalized, 0.03f, new Vector3(0.32f, -0.04f, 0f), 0.34f);
            // trakea + bronkus
            Func<Vector3, int> rings = p => p.y > 0.24f && Mathf.Repeat(p.y * 15f, 1f) < 0.36f ? 2 : 1;
            sdf.Tube(new[] { new Vector4(0f, 0.2f, 0.04f, 0.06f), new Vector4(0f, 0.5f, 0.03f, 0.065f), new Vector4(0f, 0.7f, 0.02f, 0.068f) }, 1, materialFn: rings);
            sdf.Tube(new[] { new Vector4(0f, 0.22f, 0.04f, 0.055f), new Vector4(-0.12f, 0.12f, 0.05f, 0.048f), new Vector4(-0.2f, 0.06f, 0.06f, 0.04f) }, 4, smooth: 0.03f);
            sdf.Tube(new[] { new Vector4(0f, 0.22f, 0.04f, 0.055f), new Vector4(0.12f, 0.13f, 0.05f, 0.048f), new Vector4(0.19f, 0.07f, 0.06f, 0.04f) }, 4, smooth: 0.03f);
            // laring (jakun) di atas
            sdf.Sphere(new Vector3(0f, 0.8f, 0.0f), 0.1f, 3, scale: new Vector3(1.05f, 0.8f, 0.9f));
            sdf.Tube(new[] { new Vector4(-0.06f, 0.84f, -0.04f, 0.04f), new Vector4(0.06f, 0.84f, -0.04f, 0.04f) }, 3, smooth: 0.03f);
            return sdf;
        }

        // ================================================================ umum

        internal static void Build(string organId, string folderName, CompositeSdf sdf, Bounds bounds, float cell,
            (string name, Color32 color, float smooth)[] colors, (string title, string desc, Vector3 pos)[] hotspots, Vector3 euler, float scale,
            CompositeSdf inner = null, CompositeSdf lowSdf = null, bool innerVisible = false)
        {
            Built[organId] = (sdf, bounds, cell, colors, inner);
            var (prefab, low) = BuildPrefab(folderName, sdf, bounds, cell, colors, hotspots, null, inner, lowSdf, innerVisible);
            var organ = AssetDatabase.LoadAssetAtPath<OrganInfo>("Assets/Organo/Data/Organ_" + organId + ".asset");
            if (organ != null)
            {
                organ.modelPrefab = prefab;
                organ.lowModelPrefab = low;
                organ.modelEuler = euler;
                organ.modelScale = scale;
                EditorUtility.SetDirty(organ);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>SDF tiap organ yang terakhir dibuat (dipakai ulang untuk model "Lihat dalam").</summary>
        internal static readonly Dictionary<string, (CompositeSdf sdf, Bounds bounds, float cell, (string name, Color32 color, float smooth)[] colors, CompositeSdf inner)> Built =
            new Dictionary<string, (CompositeSdf, Bounds, float, (string, Color32, float)[], CompositeSdf)>();

        /// <summary>Material opak, atau transparan kalau alpha warna &lt; 255 (mis. rongga hidung yang bening).</summary>
        internal static Material MakeMat(string path, Color32 color, float smooth) =>
            color.a < 255 ? BodyModelBuilder.Transparent(path, color, smooth) : StomachModelBuilder.MakeMaterial(path, color, smooth);

        /// <summary>Mesh detail + mesh ringan (LOD, ±1/7 segitiga) dengan material yang sama.</summary>
        public static (GameObject prefab, GameObject low) BuildPrefab(string folderName, CompositeSdf sdf, Bounds bounds, float cell,
            (string name, Color32 color, float smooth)[] colors, (string title, string desc, Vector3 pos)[] hotspots, string dirOverride = null,
            CompositeSdf inner = null, CompositeSdf lowSdf = null, bool innerVisible = false)
        {
            sdf.SetMaterialCount(colors.Length);
            inner?.SetMaterialCount(colors.Length);
            lowSdf?.SetMaterialCount(colors.Length);
            var dir = dirOverride ?? ModelsDir + "/" + folderName;
            StomachModelBuilder.EnsureFolder(dir);
            var mats = new Material[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                mats[i] = MakeMat(dir + "/" + folderName + "_" + colors[i].name + ".mat", colors[i].color, colors[i].smooth);

            GameObject Save(string name, float c, bool withHotspots, CompositeSdf source)
            {
                var mesh = SurfaceNets.Build(source, bounds, c);
                mesh.name = name;
                var meshPath = dir + "/" + name + "_Mesh.asset";
                // Buat ulang aset mesh (CopySerialized tidak memperbarui pembagian submesh dengan benar).
                if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null) AssetDatabase.DeleteAsset(meshPath);
                AssetDatabase.CreateAsset(mesh, meshPath);
                var root = new GameObject(name);
                var body = new GameObject("Mesh", typeof(MeshFilter), typeof(MeshRenderer));
                body.transform.SetParent(root.transform, false);
                body.GetComponent<MeshFilter>().sharedMesh = mesh;
                var mr = body.GetComponent<MeshRenderer>();
                mr.sharedMaterials = mats;
                mr.shadowCastingMode = withHotspots ? ShadowCastingMode.On : ShadowCastingMode.Off;
                if (withHotspots && inner != null)
                {
                    // bagian dalam (mis. pohon bronkus di dalam paru): mesh terpisah, hanya tampil saat bagian disorot
                    var innerMesh = SurfaceNets.Build(inner, bounds, c * 0.85f);
                    // nama berbeda dari model potongan "{folder}_Dalam" supaya tidak saling menimpa
                    innerMesh.name = name + "_Isi";
                    var innerPath = dir + "/" + name + "_Isi_Mesh.asset";
                    if (AssetDatabase.LoadAssetAtPath<Mesh>(innerPath) != null) AssetDatabase.DeleteAsset(innerPath);
                    AssetDatabase.CreateAsset(innerMesh, innerPath);
                    // "Inner" = hanya saat disorot; "Detail" = selalu tampil (mis. konka di dalam rongga hidung yang bening)
                    var inGo = new GameObject(innerVisible ? "Detail" : "Inner", typeof(MeshFilter), typeof(MeshRenderer));
                    inGo.transform.SetParent(root.transform, false);
                    inGo.GetComponent<MeshFilter>().sharedMesh = innerMesh;
                    var imr = inGo.GetComponent<MeshRenderer>();
                    imr.sharedMaterials = mats;
                    imr.shadowCastingMode = ShadowCastingMode.Off;
                    imr.enabled = innerVisible;
                }
                if (withHotspots && hotspots != null)
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
                Debug.Log("[Organo] Model 3D " + name + " dibuat: " + prefabPath + " (" + mesh.vertexCount + " verteks, " + StomachModelBuilder.TriCount(mesh) + " segitiga).");
                return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }

            var prefab = Save(folderName, cell, true, sdf);
            var low = Save(folderName + "_Low", cell * 2.6f, false, lowSdf ?? sdf);
            return (prefab, low);
        }
    }

    /// <summary>Gabungan bentuk halus (smooth union) dengan material per bagian.</summary>
    public class CompositeSdf : ISdf
    {
        abstract class Part
        {
            public int material;
            public float smooth;
            public Func<Vector3, int> materialFn;
            public Func<Vector3, float> disp;
            public float dispMax;
            public Vector3 bmin, bmax;
            public abstract float Raw(Vector3 p, float cutoff);
            public float Eval(Vector3 p, float cutoff = float.MaxValue)
            {
                float d = Raw(p, cutoff + dispMax);
                return disp != null ? d + disp(p) : d;
            }
            /// <summary>Batas bawah jarak (untuk melewati bagian yang jauh).</summary>
            public float LowerBound(Vector3 p)
            {
                var q = Vector3.Max(Vector3.Max(bmin - p, p - bmax), Vector3.zero);
                return q.magnitude - dispMax;
            }
        }

        class BoxPart : Part
        {
            public Vector3 c, half; public float r;
            public override float Raw(Vector3 p, float cutoff)
            {
                var q = new Vector3(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y), Mathf.Abs(p.z - c.z)) - (half - Vector3.one * r);
                return Vector3.Max(q, Vector3.zero).magnitude + Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y, q.z)), 0f) - r;
            }
        }

        class SpherePart : Part
        {
            public Vector3 c, inv; public float r;
            public override float Raw(Vector3 p, float cutoff)
            {
                var q = Vector3.Scale(p - c, inv);
                float k = Mathf.Min(1f / inv.x, Mathf.Min(1f / inv.y, 1f / inv.z));
                return (q.magnitude - r) * k;
            }
        }

        class TubePart : Part
        {
            public readonly List<Vector4> path = new List<Vector4>();
            const int Chunk = 12;
            Vector3[] m_CMin, m_CMax;

            public void Finish()
            {
                int n = Mathf.Max(1, (path.Count - 1 + Chunk - 1) / Chunk);
                m_CMin = new Vector3[n]; m_CMax = new Vector3[n];
                bmin = Vector3.one * float.MaxValue; bmax = Vector3.one * float.MinValue;
                for (int c = 0; c < n; c++)
                {
                    var mn = Vector3.one * float.MaxValue; var mx = Vector3.one * float.MinValue;
                    for (int i = c * Chunk; i <= Mathf.Min(path.Count - 1, (c + 1) * Chunk); i++)
                    {
                        var r = Vector3.one * path[i].w;
                        mn = Vector3.Min(mn, (Vector3)path[i] - r); mx = Vector3.Max(mx, (Vector3)path[i] + r);
                    }
                    m_CMin[c] = mn; m_CMax[c] = mx;
                    bmin = Vector3.Min(bmin, mn); bmax = Vector3.Max(bmax, mx);
                }
            }

            public override float Raw(Vector3 p, float cutoff)
            {
                float d = float.MaxValue;
                for (int c = 0; c < m_CMin.Length; c++)
                {
                    var q = Vector3.Max(Vector3.Max(m_CMin[c] - p, p - m_CMax[c]), Vector3.zero);
                    float lb = q.magnitude;
                    if (lb > d || lb > cutoff) continue;
                    int end = Mathf.Min(path.Count - 1, (c + 1) * Chunk);
                    for (int i = c * Chunk; i < end; i++)
                    {
                        Vector3 a = path[i], b = path[i + 1];
                        var ab = b - a;
                        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-8f));
                        float dd = (p - (a + ab * t)).magnitude - Mathf.Lerp(path[i].w, path[i + 1].w, t);
                        if (dd < d) d = dd;
                    }
                }
                return d == float.MaxValue ? cutoff + 1f : d;
            }

            public float RawFull(Vector3 p)
            {
                float d = float.MaxValue;
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Vector3 a = path[i], b = path[i + 1];
                    var ab = b - a;
                    float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-8f));
                    float dd = (p - (a + ab * t)).magnitude - Mathf.Lerp(path[i].w, path[i + 1].w, t);
                    if (dd < d) d = dd;
                }
                return d;
            }
        }

        readonly List<Part> m_Parts = new List<Part>();
        readonly List<(Func<Vector3, float> f, float k, int after)> m_Subtract = new List<(Func<Vector3, float>, float, int)>();
        readonly List<(Vector3 point, Vector3 normal, float depth, Vector3 center, float radius)> m_Grooves = new List<(Vector3, Vector3, float, Vector3, float)>();
        readonly float m_K;
        int m_MaterialCount = 1;

        public CompositeSdf(float smooth) { m_K = smooth; }

        public int MaterialCount => m_MaterialCount;
        public void SetMaterialCount(int n) => m_MaterialCount = n;

        public void Sphere(Vector3 c, float r, int material, Vector3? scale = null, Func<Vector3, float> disp = null, float dispMax = 0f, Func<Vector3, int> materialFn = null)
        {
            var s = scale ?? Vector3.one;
            var ext = Vector3.Scale(s, Vector3.one * r);
            m_Parts.Add(new SpherePart
            {
                c = c, r = r, inv = new Vector3(1f / s.x, 1f / s.y, 1f / s.z), material = material, smooth = m_K,
                disp = disp, dispMax = dispMax, materialFn = materialFn, bmin = c - ext, bmax = c + ext
            });
        }

        public void Box(Vector3 c, Vector3 half, float r, int material, float smooth = -1f)
        {
            m_Parts.Add(new BoxPart { c = c, half = half, r = r, material = material, smooth = smooth < 0 ? m_K : smooth, bmin = c - half, bmax = c + half });
        }

        public void RoundCone(Vector3 a, float ra, Vector3 b, float rb, int material, float scaleY = 1f)
        {
            var t = new TubePart { material = material, smooth = m_K };
            for (int i = 0; i <= 28; i++)
            {
                float u = i / 28f;
                float r = Mathf.Lerp(ra, rb, Mathf.SmoothStep(0f, 1f, u * u));
                t.path.Add(new Vector4(Mathf.Lerp(a.x, b.x, u), Mathf.Lerp(a.y, b.y, u) * scaleY, Mathf.Lerp(a.z, b.z, u), r));
            }
            t.Finish();
            m_Parts.Add(t);
        }

        public void Tube(Vector4[] ctrl, int material, float smooth = -1f, Func<Vector3, int> materialFn = null, Func<Vector3, float> disp = null, float dispMax = 0f, int sub = 8)
        {
            var t = new TubePart { material = material, smooth = smooth < 0 ? m_K : smooth, materialFn = materialFn, disp = disp, dispMax = dispMax };
            for (int i = 0; i < ctrl.Length - 1; i++)
            {
                var p0 = ctrl[Mathf.Max(0, i - 1)]; var p1 = ctrl[i]; var p2 = ctrl[i + 1]; var p3 = ctrl[Mathf.Min(ctrl.Length - 1, i + 2)];
                for (int s = 0; s < sub; s++)
                {
                    float u = s / (float)sub, u2 = u * u, u3 = u2 * u;
                    t.path.Add(0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2 + (-p0 + 3f * p1 - 3f * p2 + p3) * u3));
                }
            }
            t.path.Add(ctrl[ctrl.Length - 1]);
            t.Finish();
            m_Parts.Add(t);
        }

        /// <summary>Kurangi bentuk dari bagian-bagian yang sudah ditambahkan sebelumnya (bagian sesudahnya tidak ikut terpotong).</summary>
        public void Subtract(Func<Vector3, float> f, float k) => m_Subtract.Add((f, k, m_Parts.Count));

        /// <summary>Alur dangkal (celah lobus) di sekitar bidang, hanya di dalam bola center/radius.</summary>
        public void Groove(Vector3 point, Vector3 normal, float depth, Vector3 center, float radius) =>
            m_Grooves.Add((point, normal, depth, center, radius));

        static float SMin(float a, float b, float k)
        {
            if (k <= 0f) return Mathf.Min(a, b);
            float h = Mathf.Max(k - Mathf.Abs(a - b), 0f) / k;
            return Mathf.Min(a, b) - h * h * k * 0.25f;
        }

        static float SMax(float a, float b, float k) => -SMin(-a, -b, k);

        public float Eval(Vector3 p)
        {
            float d = float.MaxValue;
            for (int i = 0; i <= m_Parts.Count; i++)
            {
                foreach (var (f, k, after) in m_Subtract)
                    if (after == i && d != float.MaxValue) d = SMax(d, -f(p), k);
                if (i == m_Parts.Count) break;
                var part = m_Parts[i];
                if (d == float.MaxValue) { d = part.Eval(p); continue; }
                if (part.LowerBound(p) > d + part.smooth) continue;
                d = SMin(d, part.Eval(p, d + part.smooth), part.smooth);
            }
            foreach (var g in m_Grooves)
            {
                float slab = Mathf.Abs(Vector3.Dot(p - g.point, g.normal)) - 0.007f;
                float zone = (p - g.center).magnitude - g.radius;
                float groove = Mathf.Max(Mathf.Max(slab, zone), -(d + g.depth));
                d = SMax(d, -groove, 0.01f);
            }
            return d;
        }

        public int Material(Vector3 p)
        {
            float best = float.MaxValue;
            Part bestPart = null;
            foreach (var part in m_Parts)
            {
                if (bestPart != null && part.LowerBound(p) > best) continue;
                float d = part.Eval(p, best);
                if (d < best) { best = d; bestPart = part; }
            }
            if (bestPart == null) return 0;
            return bestPart.materialFn != null ? bestPart.materialFn(p) : bestPart.material;
        }
    }
}
