using System;
using System.Collections.Generic;
using Organo.Data;
using Organo.ThreeD;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Organo.EditorTools
{
    /// <summary>
    /// Model organ reproduksi laki-laki & perempuan, potongan "Lihat dalam", serta sel sperma & sel telur.
    /// Gaya gambar buku pelajaran IPA: skematis, warna lembut, tanpa tekstur kulit yang realistis (ramah anak SD).
    /// Acuan isi: KidsHealth "Male / Female Reproductive System" dan buku IPA SD/SMP Kemendikbud.
    /// Depan model = -z, atas = +y.
    /// </summary>
    public static class OrganReproModels
    {
        const string ModelsDir = "Assets/Organo/Models";
        const string MicroDir = ModelsDir + "/Mikro";

        static (string, Color32, float) C(string name, string hex, float smooth = 0.5f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return (name, (Color32)c, smooth);
        }

        static Vector4 V(float x, float y, float z, float r) => new Vector4(x, y, z, r);

        static float Seg(Vector3 p, Vector3 a, Vector3 b, float r)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-8f));
            return (p - (a + ab * t)).magnitude - r;
        }

        static float Ell(Vector3 p, Vector3 c, Vector3 r)
        {
            var q = new Vector3((p.x - c.x) / r.x, (p.y - c.y) / r.y, (p.z - c.z) / r.z);
            return (q.magnitude - 1f) * Mathf.Min(r.x, Mathf.Min(r.y, r.z));
        }

        static float Poly(Vector3 p, Vector4[] pts, float r)
        {
            float d = float.MaxValue;
            for (int i = 0; i < pts.Length - 1; i++) d = Mathf.Min(d, Seg(p, pts[i], pts[i + 1], r));
            return d;
        }

        [MenuItem("Organo/Model 3D/Buat Ulang Model Organ Reproduksi", priority = 47)]
        public static void BuildAll()
        {
            OrganoSetup.EnsureDatabase();
            AssetDatabase.SaveAssets();
            BuildMale();
            BuildFemale();
            MaleInside();
            FemaleInside();
            BuildMicro();
            AssetDatabase.SaveAssets();
        }

        // ================================================================ LAKI-LAKI

        // material: 0 testis, 1 epididimis, 2 skrotum (tembus pandang), 3 saluran sperma, 4 vesikula seminalis, 5 prostat,
        //           6 uretra, 7 batang penis, 8 kandung kemih, 9 ureter, 10 kepala penis (glans), 11 arteri testis, 12 vena testis
        static readonly Vector3 Bladder = new Vector3(0f, 0.33f, 0.05f);
        static readonly Vector3 BladderR = new Vector3(0.2f, 0.171f, 0.18f);
        static readonly Vector3 Prostate = new Vector3(0f, 0.1f, 0.05f);
        static readonly Vector3 PenisRoot = new Vector3(0f, -0.06f, -0.1f), PenisEnd = new Vector3(0f, -0.27f, -0.152f);
        static Vector3 PenisDir => (PenisEnd - PenisRoot).normalized;
        /// <summary>Arah ke sisi bawah (ventral) batang penis; sisi atas/depan = kebalikannya.</summary>
        static Vector3 PenisBack => Vector3.Cross(PenisDir, Vector3.right).normalized;
        static Vector3 GlansAt => PenisEnd + PenisDir * 0.028f;
        static Vector3 GlansTip => PenisEnd + PenisDir * 0.072f;
        static Vector3 TestisAt(float sx) => new Vector3(sx * 0.076f, -0.42f, -0.02f);
        static readonly Vector3 TestisR = new Vector3(0.045f, 0.062f, 0.05f);

        static readonly Vector4[] UrethraPath =
        {
            V(0f, 0.19f, 0.045f, 0.021f), V(0f, 0.08f, 0.048f, 0.021f), V(0f, 0.01f, 0.03f, 0.021f), V(0f, -0.04f, -0.03f, 0.022f), V(0f, -0.065f, -0.085f, 0.023f),
        };

        static Vector4[] VasPath(float sx) => new[]
        {
            V(sx * 0.078f, -0.49f, 0.04f, 0.012f), V(sx * 0.088f, -0.47f, 0.065f, 0.013f), V(sx * 0.09f, -0.4f, 0.07f, 0.013f), V(sx * 0.088f, -0.32f, 0.045f, 0.014f),
            V(sx * 0.092f, -0.2f, 0.0f, 0.014f), V(sx * 0.12f, -0.08f, -0.05f, 0.014f), V(sx * 0.18f, 0.03f, -0.06f, 0.014f), V(sx * 0.235f, 0.13f, -0.02f, 0.014f),
            V(sx * 0.245f, 0.27f, 0.06f, 0.014f), V(sx * 0.2f, 0.36f, 0.16f, 0.014f), V(sx * 0.12f, 0.3f, 0.22f, 0.015f),
            // ampula: ujung saluran sperma yang melebar di belakang kandung kemih
            V(sx * 0.07f, 0.21f, 0.205f, 0.02f), V(sx * 0.045f, 0.17f, 0.18f, 0.021f), V(sx * 0.022f, 0.12f, 0.13f, 0.013f),
        };

        /// <summary>Pembuluh darah testis yang naik bersama saluran sperma (tali sperma).</summary>
        static Vector4[] CordVessel(float sx, float side, float r) => new[]
        {
            V(sx * (0.07f + side * 0.006f), -0.37f, 0.0f, r), V(sx * (0.075f + side * 0.012f), -0.32f, 0.03f + side * 0.008f, r),
            V(sx * (0.078f + side * 0.016f), -0.2f, -0.012f + side * 0.004f, r), V(sx * (0.108f + side * 0.016f), -0.08f, -0.062f + side * 0.006f, r),
            V(sx * (0.168f + side * 0.014f), 0.03f, -0.072f + side * 0.006f, r), V(sx * (0.222f + side * 0.012f), 0.13f, -0.034f + side * 0.006f, r),
            V(sx * (0.21f + side * 0.012f), 0.27f, 0.0f, r), V(sx * (0.19f + side * 0.01f), 0.44f, 0.03f, r),
        };

        static Vector4[] VesiclePath(float sx)
        {
            var a = new Vector3(sx * 0.05f, 0.13f, 0.17f);
            var b = new Vector3(sx * 0.2f, 0.225f, 0.17f);
            var pts = new Vector4[15];
            for (int i = 0; i < pts.Length; i++)
            {
                float t = i / (pts.Length - 1f);
                var p = Vector3.Lerp(a, b, t) + new Vector3(0f, 0.013f * Mathf.Sin(t * Mathf.PI * 5f), 0.012f * Mathf.Cos(t * Mathf.PI * 5f));
                float r = Mathf.Lerp(0.022f, 0.016f, t) + 0.005f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 7f));
                pts[i] = V(p.x, p.y, p.z, r);
            }
            return pts;
        }

        /// <summary>Kandung kemih, ureter, vesikula seminalis, prostat, uretra & penis.</summary>
        static void MaleUpper(CompositeSdf sdf)
        {
            sdf.Sphere(Bladder, 0.18f, 8, scale: new Vector3(1.12f, 0.95f, 1f),
                disp: p => 0.0025f * Mathf.Sin(p.x * 90f) * Mathf.Sin(p.y * 85f + p.z * 40f), dispMax: 0.0025f);
            sdf.RoundCone(new Vector3(0f, 0.19f, 0.045f), 0.06f, new Vector3(0f, 0.155f, 0.047f), 0.028f, 8);
            foreach (var sx in new[] { -1f, 1f })
            {
                sdf.Tube(new[] { V(sx * 0.1f, 0.26f, 0.19f, 0.016f), V(sx * 0.16f, 0.36f, 0.22f, 0.017f), V(sx * 0.2f, 0.52f, 0.2f, 0.017f), V(sx * 0.21f, 0.68f, 0.17f, 0.016f) }, 9, smooth: 0.012f);
                // vesikula seminalis: kantong berlekuk-lekuk di belakang kandung kemih
                sdf.Tube(VesiclePath(sx), 4, smooth: 0.012f, disp: p => 0.004f * Mathf.Sin(p.x * 130f) * Mathf.Sin(p.y * 120f), dispMax: 0.004f);
            }
            // prostat: sebesar kenari, ujung bawahnya meruncing
            sdf.Sphere(Prostate, 0.078f, 5, scale: new Vector3(1.25f, 0.9f, 1f), disp: p => 0.003f * Mathf.Sin(p.x * 70f) * Mathf.Sin(p.z * 70f), dispMax: 0.003f);
            sdf.RoundCone(new Vector3(0f, 0.085f, 0.048f), 0.07f, new Vector3(0f, 0.025f, 0.04f), 0.03f, 5);
            sdf.Tube(UrethraPath, 6, smooth: 0.012f);
            // penis: dua jaringan berongga di sisi atas + satu di sisi bawah (berisi uretra), ujungnya kepala penis
            var d = PenisDir; var back = PenisBack;
            sdf.Sphere(PenisRoot + back * 0.02f + new Vector3(0f, 0.012f, 0.02f), 0.03f, 7);
            foreach (var sx in new[] { -1f, 1f })
            {
                var side = Vector3.right * sx * 0.021f - back * 0.008f;
                var a = PenisRoot + side; var b = PenisEnd + side;
                sdf.Tube(new[] { V(a.x, a.y, a.z, 0.029f), V((a.x + b.x) / 2f, (a.y + b.y) / 2f, (a.z + b.z) / 2f, 0.03f), V(b.x, b.y, b.z, 0.028f) }, 7, smooth: 0.02f);
            }
            {
                var a = PenisRoot + back * 0.02f; var b = PenisEnd + back * 0.018f;
                sdf.Tube(new[] { V(a.x, a.y, a.z, 0.023f), V(b.x, b.y, b.z, 0.021f) }, 7, smooth: 0.02f);
            }
            sdf.Sphere(GlansAt, 0.05f, 10, scale: new Vector3(1.02f, 0.92f, 0.95f));
            // lingkar pembatas kepala penis (corona)
            var ring = new List<Vector4>();
            for (int i = 0; i <= 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f;
                var p = PenisEnd + d * 0.006f + (Vector3.right * Mathf.Cos(a) * 0.052f + back * Mathf.Sin(a) * 0.046f);
                ring.Add(V(p.x, p.y, p.z, 0.007f));
            }
            sdf.Tube(ring.ToArray(), 10, smooth: 0.012f, sub: 3);
            // lubang uretra di ujung kepala penis
            sdf.Subtract(p => Seg(p, GlansTip - back * 0.008f, GlansTip + back * 0.008f, 0.0035f), 0.004f);
        }

        static void Scrotum(CompositeSdf sdf, int mat)
        {
            foreach (var sx in new[] { -1f, 1f })
                sdf.Sphere(TestisAt(sx) + new Vector3(0f, -0.005f, 0.005f), 0.1f, mat, scale: new Vector3(0.95f, 1.12f, 1.0f));
        }

        /// <summary>Testis, epididimis, saluran sperma & pembuluh darah tali sperma (terlihat menembus skrotum).</summary>
        static void TestesAndDucts(CompositeSdf sdf, int fromVas = 0, float onlySide = 0f)
        {
            foreach (var sx in new[] { -1f, 1f })
            {
                if (onlySide != 0f && sx != onlySide) continue;
                var t = TestisAt(sx);
                if (fromVas == 0)
                {
                    sdf.Sphere(t, 0.05f, 0, scale: new Vector3(0.9f, 1.24f, 1.0f));
                    // epididimis: kepala di atas testis, badan di belakangnya, ekor di bawah (permukaannya bergulung)
                    Func<Vector3, float> coil = p => 0.0025f * Mathf.Sin(p.y * 260f) * Mathf.Sin(p.x * 240f + p.z * 200f);
                    sdf.Sphere(t + new Vector3(sx * 0.006f, 0.055f, 0.04f), 0.024f, 1, scale: new Vector3(0.95f, 1f, 0.85f), disp: coil, dispMax: 0.0025f);
                    sdf.Tube(new[] { V(sx * 0.084f, -0.375f, 0.035f, 0.015f), V(sx * 0.083f, -0.42f, 0.052f, 0.014f), V(sx * 0.08f, -0.465f, 0.048f, 0.015f) }, 1, smooth: 0.01f, disp: coil, dispMax: 0.0025f);
                    sdf.Sphere(new Vector3(sx * 0.078f, -0.483f, 0.036f), 0.017f, 1, disp: coil, dispMax: 0.0025f);
                }
                var vas = VasPath(sx);
                if (fromVas > 0) vas = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Skip(vas, fromVas));
                sdf.Tube(vas, 3, smooth: 0.006f);
                sdf.Tube(CordVessel(sx, 1f, 0.0065f), 11, smooth: 0.004f);
                sdf.Tube(CordVessel(sx, -1f, 0.008f), 12, smooth: 0.004f, disp: p => 0.002f * Mathf.Sin(p.y * 220f), dispMax: 0.002f);
            }
        }

        static readonly (string, Color32, float)[] MaleColors =
        {
            C("Testis", "#F5B7C6", 0.55f), C("Epididimis", "#E58FA6", 0.5f), C("Skrotum", "#F2B89A66", 0.45f), C("SaluranSperma", "#5DB7DE", 0.55f),
            C("Vesikula", "#F2C46B", 0.5f), C("Prostat", "#E9946F", 0.5f), C("Uretra", "#F6CF5E", 0.55f), C("Penis", "#EFB091", 0.4f),
            C("KandungKemih", "#F9E39A", 0.6f), C("Ureter", "#F0C85A", 0.55f), C("KepalaPenis", "#EE9E95", 0.45f),
            C("ArteriTestis", "#E46A6A", 0.55f), C("VenaTestis", "#6F8FE0", 0.55f),
        };

        /// <summary>Batch: model laki-laki versi baru + potongannya.</summary>
        public static void RebuildMale() { OrganoSetup.EnsureDatabase(); BuildMale(); MaleInside(); AssetDatabase.SaveAssets(); }

        [MenuItem("Organo/Model 3D/Buat Ulang Model Reproduksi Laki-laki", priority = 48)]
        public static void BuildMale()
        {
            var sdf = new CompositeSdf(0.02f);
            MaleUpper(sdf);
            Scrotum(sdf, 2);
            var inner = new CompositeSdf(0.012f);
            TestesAndDucts(inner);

            OrganModelBuilder.Build(ReproAccess.Boy, "ReproLaki", sdf, new Bounds(new Vector3(0f, 0.07f, 0.01f), new Vector3(0.76f, 1.34f, 0.6f)), 0.0085f,
                MaleColors,
                new[]
                {
                    ("Testis", "Membuat sel sperma", new Vector3(0.12f, -0.42f, -0.06f)),
                    ("Epididimis", "Sperma menjadi matang", new Vector3(0.09f, -0.36f, 0.07f)),
                    ("Tali sperma", "Saluran sperma & pembuluh darah", new Vector3(0.14f, -0.06f, -0.08f)),
                    ("Vesikula seminalis", "Cairan bertenaga", new Vector3(0.17f, 0.22f, 0.2f)),
                    ("Prostat", "Cairan pelindung sperma", new Vector3(0.05f, 0.08f, -0.03f)),
                    ("Kepala penis", "Ujung tempat lubang uretra", new Vector3(0f, -0.3f, -0.21f)),
                    ("Kandung kemih", "Menampung urine", new Vector3(0f, 0.42f, -0.12f)),
                }, new Vector3(6f, -26f, 0f), 1f, inner, null, true);
        }

        // ================================================================ PEREMPUAN

        // material: 0 rahim, 1 leher rahim, 2 tuba falopi, 3 fimbria, 4 ovarium, 5 vagina, 6 ligamen ovarium
        static Vector3 OvaryAt(float sx) => new Vector3(sx * 0.37f, 0.07f, 0f);
        static readonly Vector3 OvaryR = new Vector3(0.08f, 0.05f, 0.056f);
        static readonly Vector3 FunnelAt = new Vector3(0.47f, 0.14f, 0f);

        static Vector4[] TubePath(float sx) => new[]
        {
            V(sx * 0.13f, 0.27f, 0f, 0.02f), V(sx * 0.24f, 0.31f, 0f, 0.022f), V(sx * 0.34f, 0.31f, 0f, 0.026f),
            V(sx * 0.43f, 0.25f, 0f, 0.034f), V(sx * 0.47f, 0.15f, 0f, 0.044f),
        };

        static void Female(CompositeSdf sdf)
        {
            // rahim: buah pir terbalik yang pipih ke depan-belakang; bagian bawah = leher rahim
            Func<Vector3, int> uterusMat = p => p.y < -0.055f ? 1 : 0;
            sdf.Sphere(new Vector3(0f, 0.19f, 0f), 0.16f, 0, scale: new Vector3(1.05f, 0.82f, 0.68f), materialFn: uterusMat);
            sdf.Sphere(new Vector3(0f, 0.07f, 0f), 0.125f, 0, scale: new Vector3(1f, 1f, 0.7f), materialFn: uterusMat);
            sdf.Sphere(new Vector3(0f, -0.04f, 0f), 0.085f, 0, scale: new Vector3(1f, 1f, 0.8f), materialFn: uterusMat);
            sdf.Sphere(new Vector3(0f, -0.11f, 0f), 0.064f, 1, scale: new Vector3(1f, 1.05f, 0.85f));
            // vagina: saluran lentur dari leher rahim ke bawah
            sdf.Tube(new[] { V(0f, -0.15f, 0f, 0.058f), V(0f, -0.31f, -0.005f, 0.062f), V(0f, -0.46f, -0.01f, 0.058f) }, 5, smooth: 0.03f);
            foreach (var sx in new[] { -1f, 1f })
            {
                sdf.Tube(TubePath(sx), 2, smooth: 0.02f);
                // fimbria: jari-jari lembut di ujung corong tuba, menghadap ovarium
                var funnel = new Vector3(sx * FunnelAt.x, FunnelAt.y, FunnelAt.z);
                for (int i = 0; i < 7; i++)
                {
                    float a = Mathf.Lerp(-1.25f, 1.25f, i / 6f);
                    var dir = new Vector3(sx * Mathf.Sin(a) * 0.55f, -Mathf.Cos(a), Mathf.Sin(a * 2.3f) * 0.35f).normalized;
                    var tip = funnel + dir * 0.075f + new Vector3(0f, 0f, (i % 2 == 0 ? 1 : -1) * 0.012f);
                    sdf.Tube(new[] { V(funnel.x + dir.x * 0.02f, funnel.y + dir.y * 0.02f, funnel.z, 0.015f), V(tip.x, tip.y, tip.z, 0.008f) }, 3, smooth: 0.008f);
                }
                // ovarium: lonjong, permukaannya sedikit berbenjol (folikel)
                sdf.Sphere(OvaryAt(sx), 0.07f, 4, scale: new Vector3(1.15f, 0.72f, 0.8f),
                    disp: p => 0.005f * Mathf.Sin(p.x * 120f) * Mathf.Sin(p.y * 130f) * Mathf.Sin(p.z * 110f), dispMax: 0.005f);
                sdf.Tube(new[] { V(sx * 0.13f, 0.17f, 0f, 0.016f), V(sx * 0.22f, 0.12f, 0f, 0.014f), V(sx * 0.3f, 0.08f, 0f, 0.013f) }, 6, smooth: 0.012f);
            }
        }

        static readonly (string, Color32, float)[] FemaleColors =
        {
            C("Rahim", "#F49AAE", 0.5f), C("LeherRahim", "#E57A93", 0.5f), C("TubaFalopi", "#F7B39E", 0.5f), C("Fimbria", "#F0957F", 0.45f),
            C("Ovarium", "#FFD39E", 0.5f), C("Vagina", "#F2A3B6", 0.45f), C("Ligamen", "#F5C9D4", 0.45f),
        };

        [MenuItem("Organo/Model 3D/Buat Ulang Model Reproduksi Perempuan", priority = 49)]
        public static void BuildFemale()
        {
            var sdf = new CompositeSdf(0.03f);
            Female(sdf);
            OrganModelBuilder.Build(ReproAccess.Girl, "ReproPerempuan", sdf, new Bounds(new Vector3(0f, -0.07f, 0f), new Vector3(1.14f, 0.96f, 0.34f)), 0.0085f,
                FemaleColors,
                new[]
                {
                    ("Rahim", "Tempat bayi tumbuh", new Vector3(0f, 0.16f, -0.12f)),
                    ("Ovarium", "Menyimpan sel telur", new Vector3(0.37f, 0.07f, -0.05f)),
                    ("Tuba falopi", "Jalan sel telur", new Vector3(0.3f, 0.32f, -0.03f)),
                    ("Fimbria", "Menangkap sel telur", new Vector3(0.5f, 0.07f, -0.02f)),
                    ("Leher rahim", "Pintu bawah rahim", new Vector3(0f, -0.11f, -0.06f)),
                    ("Vagina", "Saluran ke luar tubuh", new Vector3(0f, -0.33f, -0.07f)),
                }, new Vector3(8f, -8f, 0f), 1f);
        }

        // ================================================================ POTONGAN "LIHAT DALAM"

        /// <summary>Gabungan beberapa bentuk terpotong (masing-masing dengan bidang potongnya sendiri).</summary>
        class UnionSdf : ISdf
        {
            readonly ISdf[] m_Parts;
            readonly Func<int, int>[] m_Remap;
            public int MaterialCount { get; }
            public UnionSdf(int count, ISdf[] parts, Func<int, int>[] remap) { MaterialCount = count; m_Parts = parts; m_Remap = remap; }

            public float Eval(Vector3 p)
            {
                float d = float.MaxValue;
                foreach (var s in m_Parts) d = Mathf.Min(d, s.Eval(p));
                return d;
            }

            public int Material(Vector3 p)
            {
                int best = 0; float bd = float.MaxValue;
                for (int i = 0; i < m_Parts.Length; i++)
                {
                    float d = m_Parts[i].Eval(p);
                    if (d < bd) { bd = d; best = i; }
                }
                int m = m_Parts[best].Material(p);
                return m_Remap != null && m_Remap[best] != null ? m_Remap[best](m) : m;
            }
        }

        static void SaveInside(string organId, string folder, ISdf sdf, Bounds bounds, float cell,
            (string name, Color32 color, float smooth)[] colors, (string title, string desc, Vector3 pos)[] hotspots, Vector3 insideEuler)
        {
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
            var organ = AssetDatabase.LoadAssetAtPath<OrganInfo>("Assets/Organo/Data/Organ_" + organId + ".asset");
            if (organ != null)
            {
                organ.insidePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                organ.insideEuler = insideEuler;
                EditorUtility.SetDirty(organ);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Organo] Model dalam " + name + " dibuat (" + mesh.vertexCount + " verteks, " + StomachModelBuilder.TriCount(mesh) + " segitiga).");
        }

        /// <summary>
        /// Laki-laki: bagian atas dipotong tepat di tengah (uretra dari kandung kemih, melewati prostat, sampai lubang di kepala penis;
        /// terlihat jaringan berongga penis), skrotum dipotong melewati tengah testis kanan (ruang-ruang berisi saluran pembuat sperma).
        /// </summary>
        public static void MaleInside()
        {
            int n = MaleColors.Length;
            // tambahan: 13 tubulus, 14 rongga kandung kemih, 15 jaringan berongga penis, 16 skrotum (tidak tembus pandang),
            //           17 jaringan skrotum, 18 selaput & sekat testis, 19 jaringan sekitar uretra (spongiosum)
            var colors = new List<(string, Color32, float)>(MaleColors)
            {
                C("Tubulus", "#E46F8E", 0.45f), C("RonggaKemih", "#FCEFC0", 0.5f), C("JaringanPenis", "#F4A7A0", 0.4f),
                C("SkrotumUtuh", "#F2B89A", 0.45f), C("JaringanSkrotum", "#F7D3C2", 0.4f), C("Selaput", "#FBEFF2", 0.5f), C("Spongiosum", "#F8C4BC", 0.4f),
            }.ToArray();

            var upper = new CompositeSdf(0.02f);
            MaleUpper(upper);
            var ducts = new CompositeSdf(0.012f);
            TestesAndDucts(ducts, 4, 1f);
            upper.SetMaterialCount(colors.Length);
            ducts.SetMaterialCount(colors.Length);

            var d = PenisDir; var back = PenisBack;
            var cavA = PenisRoot + Vector3.right * 0.021f - back * 0.008f; var cavB = PenisEnd + Vector3.right * 0.021f - back * 0.008f;
            var spA = PenisRoot + back * 0.02f; var spB = PenisEnd + back * 0.018f;
            Func<Vector3, float> bladderCavity = p => Ell(p, Bladder, BladderR - Vector3.one * 0.026f);
            Func<Vector3, float> urethraLumen = p => Mathf.Min(Poly(p, UrethraPath, 0.008f),
                Mathf.Min(Seg(p, spA, spB, 0.007f), Seg(p, spB, GlansTip, 0.007f)));
            Func<Vector3, float> ejaculatory = p => Seg(p, new Vector3(0.022f, 0.12f, 0.13f), new Vector3(0.004f, 0.07f, 0.05f), 0.005f);
            const float cut = 0.004f;
            var top = new CutawaySdf(colors.Length)
            {
                Outer = upper, Cut = p => p.x - cut, Interior = ducts,
                Carve = p => Mathf.Min(Mathf.Min(bladderCavity(p), urethraLumen(p)), ejaculatory(p)),
                FaceMaterial = p =>
                {
                    int m = upper.Material(p);
                    if (m == 7 || m == 10)
                    {
                        if (Seg(p, spA, spB, 0.022f) < 0f || m == 10) return 19;  // jaringan di sekitar uretra & kepala penis
                        if (Seg(p, cavA, cavB, 0.026f) < 0f) return 15;           // jaringan berongga
                        return 7;
                    }
                    return m;
                },
                CavityMaterial = p => bladderCavity(p) < Mathf.Min(urethraLumen(p), ejaculatory(p)) ? 14 : ejaculatory(p) < urethraLumen(p) ? 3 : 6,
            };

            var low = new CompositeSdf(0.02f);
            Scrotum(low, 16);
            low.SetMaterialCount(colors.Length);
            var right = TestisAt(1f);
            var mediastinum = right + new Vector3(0f, 0.01f, 0.038f);
            var bottom = new CutawaySdf(colors.Length)
            {
                Outer = low, Cut = p => p.x - right.x,
                FaceMaterial = p =>
                {
                    // epididimis & saluran sperma di belakang testis
                    if (Seg(p, new Vector3(right.x, -0.36f, 0.035f), new Vector3(right.x, -0.475f, 0.05f), 0.017f) < 0f) return 1;
                    if (Seg(p, new Vector3(right.x, -0.47f, 0.066f), new Vector3(right.x, -0.33f, 0.05f), 0.012f) < 0f) return 3;
                    float t = Ell(p, right, TestisR);
                    if (t < 0f)
                    {
                        if (t > -0.005f) return 18;                                        // selaput testis
                        var v = new Vector2(p.y - mediastinum.y, p.z - mediastinum.z);
                        float ang = Mathf.Atan2(v.y, v.x);
                        if (Mathf.Abs(Mathf.Sin(ang * 6f)) < 0.13f || v.magnitude < 0.008f) return 18; // sekat-sekat dari pusat testis
                        float w = Mathf.Sin(v.magnitude * 420f + Mathf.Sin(ang * 23f) * 2.4f);       // saluran bergulung di tiap ruang
                        return w > 0.2f ? 13 : 0;
                    }
                    return 17;
                },
            };
            var sdf = new UnionSdf(colors.Length, new ISdf[] { top, bottom }, null);
            SaveInside(ReproAccess.Boy, "ReproLaki", sdf, new Bounds(new Vector3(0f, 0.07f, 0.01f), new Vector3(0.76f, 1.34f, 0.6f)), 0.0075f, colors,
                new[]
                {
                    ("Tubulus seminiferus", "Pabrik sel sperma", new Vector3(right.x - 0.02f, -0.43f, -0.03f)),
                    ("Epididimis", "Sperma menjadi matang", new Vector3(right.x - 0.02f, -0.41f, 0.045f)),
                    ("Uretra", "Saluran di dalam penis", new Vector3(-0.02f, -0.2f, -0.12f)),
                    ("Jaringan berongga", "Bagian dalam batang penis", new Vector3(-0.02f, -0.16f, -0.135f)),
                    ("Prostat", "Mengelilingi uretra", new Vector3(-0.02f, 0.1f, 0.05f)),
                    ("Kandung kemih", "Rongga penampung urine", new Vector3(-0.02f, 0.33f, 0.04f)),
                }, new Vector3(6f, -62f, 0f));
        }

        /// <summary>Perempuan: dipotong tepat di tengah dari depan (terlihat rongga rahim, lapisan dindingnya, saluran & folikel).</summary>
        public static void FemaleInside()
        {
            // tambahan: 7 endometrium, 8 otot rahim, 9 folikel, 10 sel telur
            var colors = new List<(string, Color32, float)>(FemaleColors)
            {
                C("Endometrium", "#E35D84", 0.45f), C("OtotRahim", "#F6A2B6", 0.4f), C("Folikel", "#FFF2D2", 0.55f), C("SelTelur", "#F5A04B", 0.55f),
            }.ToArray();
            var outer = new CompositeSdf(0.03f);
            Female(outer);
            outer.SetMaterialCount(colors.Length);

            Func<Vector3, float> cavity = p =>
            {
                // rongga rahim berbentuk segitiga terbalik
                float d = Seg(p, new Vector3(-0.085f, 0.23f, 0f), new Vector3(0.085f, 0.23f, 0f), 0.02f);
                d = Mathf.Min(d, Seg(p, new Vector3(-0.085f, 0.23f, 0f), new Vector3(0f, 0.0f, 0f), 0.018f));
                d = Mathf.Min(d, Seg(p, new Vector3(0.085f, 0.23f, 0f), new Vector3(0f, 0.0f, 0f), 0.018f));
                d = Mathf.Min(d, Ell(p, new Vector3(0f, 0.15f, 0f), new Vector3(0.05f, 0.07f, 0.02f)));
                return d;
            };
            Func<Vector3, float> canal = p => Seg(p, new Vector3(0f, 0.0f, 0f), new Vector3(0f, -0.16f, 0f), 0.011f);
            Func<Vector3, float> vaginaLumen = p => Seg(p, new Vector3(0f, -0.16f, 0f), new Vector3(0f, -0.5f, -0.01f), 0.028f);
            Func<Vector3, float> tubeLumen = p => Mathf.Min(
                Mathf.Min(Poly(p, TubePath(-1f), 0.009f), Poly(p, TubePath(1f), 0.009f)),
                Mathf.Min(Seg(p, new Vector3(-0.47f, 0.15f, 0f), new Vector3(-0.47f, 0.09f, 0f), 0.02f), Seg(p, new Vector3(0.47f, 0.15f, 0f), new Vector3(0.47f, 0.09f, 0f), 0.02f)));

            // folikel di potongan ovarium: (posisi relatif terhadap pusat ovarium, jari-jari); yang besar berisi sel telur
            var follicles = new (Vector2 c, float r)[] { (new Vector2(0.03f, 0.008f), 0.022f), (new Vector2(-0.035f, 0.012f), 0.012f), (new Vector2(-0.01f, -0.022f), 0.009f), (new Vector2(0.065f, -0.012f), 0.008f), (new Vector2(-0.06f, -0.008f), 0.007f) };
            var interior = new CompositeSdf(0.006f);
            interior.Sphere(new Vector3(0.39f, 0.288f, 0.004f), 0.012f, 10);   // sel telur sedang berjalan di tuba kanan
            interior.SetMaterialCount(colors.Length);

            var sdf = new CutawaySdf(colors.Length)
            {
                Outer = outer, Cut = p => p.z, Interior = interior, CarveInterior = false,
                Carve = p => Mathf.Min(Mathf.Min(cavity(p), canal(p)), Mathf.Min(vaginaLumen(p), tubeLumen(p))),
                FaceMaterial = p =>
                {
                    int m = outer.Material(p);
                    if (m == 4)
                    {
                        float sx = Mathf.Sign(p.x);
                        var o = OvaryAt(sx);
                        foreach (var (c, r) in follicles)
                        {
                            var q = new Vector2(p.x - o.x - sx * c.x, p.y - o.y - c.y);
                            if (q.magnitude < r) return r > 0.02f && q.magnitude < 0.006f ? 10 : 9;
                        }
                        return 4;
                    }
                    if (m == 0) return cavity(p) < 0.03f ? 7 : 8;
                    return m;
                },
                CavityMaterial = p => cavity(p) < 0.01f ? 7 : canal(p) < 0.01f ? 1 : vaginaLumen(p) < 0.01f ? 5 : 2,
            };
            SaveInside(ReproAccess.Girl, "ReproPerempuan", sdf, new Bounds(new Vector3(0f, -0.07f, 0f), new Vector3(1.14f, 0.96f, 0.34f)), 0.0075f, colors,
                new[]
                {
                    ("Endometrium", "Lapisan yang menebal tiap bulan", new Vector3(0.06f, 0.14f, 0.0f)),
                    ("Otot rahim", "Dinding yang bisa melar", new Vector3(-0.13f, 0.18f, 0.0f)),
                    ("Folikel", "Kantong calon sel telur", new Vector3(0.4f, 0.08f, 0.0f)),
                    ("Sel telur", "Berjalan di tuba falopi", new Vector3(0.39f, 0.3f, 0.0f)),
                    ("Leher rahim", "Saluran kecil ke vagina", new Vector3(0f, -0.1f, 0.0f)),
                }, new Vector3(6f, -6f, 0f));
        }

        // ================================================================ SEL SPERMA & SEL TELUR (mikro)

        /// <summary>Batch: sel sperma & sel telur, lalu gambar bagian organ.</summary>
        public static void BuildMicroAndImages() { BuildMicro(); AssetDatabase.SaveAssets(); DetailAssetsBuilder.RenderPartImages(); }

        public static void BuildMicro()
        {
            StomachModelBuilder.EnsureFolder(MicroDir);
            // SEL SPERMA: kepala lonjong pipih (ujungnya bertudung), bagian tengah penghasil tenaga, ekor panjang bergelombang
            {
                var sdf = new CompositeSdf(0.02f);
                sdf.Sphere(new Vector3(-0.5f, 0f, 0f), 0.075f, 0, scale: new Vector3(1.4f, 1f, 0.55f), materialFn: p => p.x < -0.555f ? 1 : 0);
                sdf.Tube(new[] { V(-0.42f, 0f, 0f, 0.026f), V(-0.35f, 0f, 0f, 0.024f), V(-0.27f, 0f, 0f, 0.021f) }, 2, smooth: 0.02f,
                    disp: p => 0.004f * Mathf.Sin(p.x * 160f + Mathf.Atan2(p.y, p.z) * 2f), dispMax: 0.004f);
                var tail = new List<Vector4>();
                for (int i = 0; i <= 24; i++)
                {
                    float u = i / 24f;
                    float x = Mathf.Lerp(-0.27f, 0.64f, u);
                    tail.Add(V(x, Mathf.Sin(u * Mathf.PI * 2.4f) * 0.07f * u, Mathf.Cos(u * Mathf.PI * 1.6f) * 0.015f * u, Mathf.Lerp(0.016f, 0.006f, u)));
                }
                sdf.Tube(tail.ToArray(), 3, smooth: 0.01f, sub: 4);
                OrganModelBuilder.BuildPrefab("Sperma", sdf, new Bounds(new Vector3(0.02f, 0f, 0f), new Vector3(1.4f, 0.32f, 0.22f)), 0.0055f, new[]
                {
                    C("Kepala", "#7FA8EE", 0.6f), C("Tudung", "#B9D3FA", 0.6f), C("BagianTengah", "#F2C46B", 0.55f), C("Ekor", "#A9C4F5", 0.55f),
                }, new[]
                {
                    ("Kepala", "Berisi bahan pembawa sifat", new Vector3(-0.5f, 0.05f, -0.05f)),
                    ("Bagian tengah", "Penghasil tenaga", new Vector3(-0.35f, 0.03f, -0.03f)),
                    ("Ekor", "Untuk berenang", new Vector3(0.3f, 0.05f, -0.02f)),
                }, MicroDir);
            }
            // SEL TELUR: bola besar berlapis pelindung, dikelilingi sel-sel kecil; seperempatnya dibuka supaya inti sel terlihat
            {
                var sdf = new CompositeSdf(0.015f);
                sdf.Sphere(Vector3.zero, 0.3f, 0, materialFn: p => p.magnitude > 0.272f ? 1 : 0);
                sdf.Subtract(p => -Mathf.Min(p.x, -p.z), 0.01f);
                sdf.Sphere(new Vector3(0.06f, 0.05f, -0.05f), 0.085f, 3);
                for (int i = 0; i < 46; i++)
                {
                    float h = 1f - 2f * (i + 0.5f) / 46f, a = i * 2.39996f;
                    var d = new Vector3(Mathf.Cos(a) * Mathf.Sqrt(1f - h * h), h, Mathf.Sin(a) * Mathf.Sqrt(1f - h * h));
                    if (d.x > 0.05f && d.z < -0.05f) continue;
                    sdf.Sphere(d * 0.345f, 0.042f, 2, scale: new Vector3(1f, 1f, 1f));
                }
                OrganModelBuilder.BuildPrefab("SelTelur", sdf, new Bounds(Vector3.zero, new Vector3(0.9f, 0.9f, 0.9f)), 0.0115f, new[]
                {
                    C("Isi", "#FFE3A3", 0.55f), C("Pelindung", "#FFF8EC", 0.7f), C("SelPelindung", "#F7B98B", 0.45f), C("Inti", "#B48BE8", 0.6f),
                }, new[]
                {
                    ("Inti sel", "Berisi bahan pembawa sifat", new Vector3(0.1f, 0.08f, -0.13f)),
                    ("Lapisan pelindung", "Membungkus sel telur", new Vector3(-0.2f, 0.18f, -0.2f)),
                    ("Sel-sel pelindung", "Menempel di luar sel telur", new Vector3(-0.25f, -0.28f, -0.1f)),
                }, MicroDir);
            }
        }
    }
}
