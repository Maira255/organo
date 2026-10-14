using System;
using System.Collections.Generic;
using Organo.ThreeD;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Organo.EditorTools
{
    /// <summary>
    /// Rangka anak (sistem gerak). Dibuat langsung di koordinat model anak 3D (kaki di y=0, puncak kepala ±1,29),
    /// jadi prefab yang sama dipakai untuk close-up organ "Rangka" dan sebagai tulang tembus pandang di mode "Tubuh".
    /// Gaya ramah anak: tulang membulat warna krem, rongga mata dangkal (tidak gelap menyeramkan), tulang rawan biru muda.
    /// Jumlah bagian mengikuti kenyataan (12 pasang rusuk, 27 tulang tangan disederhanakan), ruas tulang belakang disederhanakan.
    /// Model "Dalam": tulang paha dibelah memanjang (tulang keras, tulang spons, sumsum merah & kuning, tulang rawan sendi).
    /// </summary>
    public static class OrganSkeletonModel
    {
        static (string, Color32, float) C(string name, string hex, float smooth = 0.45f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return (name, (Color32)c, smooth);
        }

        static Vector4 V(float x, float y, float z, float r) => new Vector4(x, y, z, r);
        static Vector4 V(Vector3 p, float r) => new Vector4(p.x, p.y, p.z, r);

        static float Ell(Vector3 p, Vector3 c, Vector3 r)
        {
            var q = new Vector3((p.x - c.x) / r.x, (p.y - c.y) / r.y, (p.z - c.z) / r.z);
            return (q.magnitude - 1f) * Mathf.Min(r.x, Mathf.Min(r.y, r.z));
        }

        // material: 0 tengkorak, 1 gigi, 2 tulang belakang, 3 tulang rawan, 4 tulang rusuk, 5 tulang dada, 6 bahu (selangka & belikat),
        //           7 lengan atas, 8 lengan bawah, 9 tangan, 10 panggul, 11 tulang paha, 12 tempurung lutut, 13 tulang kering & betis, 14 kaki
        static readonly (string, Color32, float)[] Colors =
        {
            C("Tengkorak", "#F4E8CC"), C("Gigi", "#FFFFFF", 0.7f), C("TulangBelakang", "#EFE0BE"), C("TulangRawan", "#BFE3F0", 0.55f),
            C("TulangRusuk", "#F2E3C3"), C("TulangDada", "#EEDDB8"), C("Bahu", "#F1E1C0"), C("LenganAtas", "#F3E5C6"),
            C("LenganBawah", "#F0E0BD"), C("Tangan", "#F2E4C4"), C("Panggul", "#EFDFBB"), C("TulangPaha", "#F3E5C6"),
            C("TempurungLutut", "#EADAB4"), C("TulangKering", "#F1E2C1"), C("Kaki", "#F0E0BD"),
        };

        /// <summary>Lengkung tulang belakang (z) di ketinggian y.</summary>
        static float SpineZ(float y)
        {
            var ys = new[] { 1.0f, 0.93f, 0.82f, 0.7f, 0.6f, 0.52f, 0.44f };
            var zs = new[] { 0.02f, 0.03f, 0.062f, 0.058f, 0.035f, 0.05f, 0.07f };
            if (y >= ys[0]) return zs[0];
            for (int i = 0; i < ys.Length - 1; i++)
                if (y <= ys[i] && y >= ys[i + 1]) return Mathf.Lerp(zs[i], zs[i + 1], (ys[i] - y) / (ys[i] - ys[i + 1]));
            return zs[zs.Length - 1];
        }

        static void Skull(CompositeSdf sdf)
        {
            sdf.Sphere(new Vector3(0f, 1.14f, 0.015f), 0.125f, 0, scale: new Vector3(0.92f, 1f, 1.08f));
            sdf.Sphere(new Vector3(0f, 1.065f, -0.07f), 0.075f, 0, scale: new Vector3(1f, 0.95f, 0.75f));
            foreach (var sx in new[] { -1f, 1f })
                sdf.Tube(new[] { V(sx * 0.05f, 1.088f, -0.1f, 0.012f), V(sx * 0.085f, 1.083f, -0.06f, 0.012f), V(sx * 0.1f, 1.09f, -0.01f, 0.011f) }, 0, smooth: 0.012f);
            // rongga mata & hidung dangkal (berwarna tulang, bukan lubang gelap)
            sdf.Subtract(p => Mathf.Min(Mathf.Min(Ell(p, new Vector3(-0.045f, 1.115f, -0.128f), new Vector3(0.03f, 0.026f, 0.022f)),
                                                  Ell(p, new Vector3(0.045f, 1.115f, -0.128f), new Vector3(0.03f, 0.026f, 0.022f))),
                                        Ell(p, new Vector3(0f, 1.073f, -0.13f), new Vector3(0.012f, 0.017f, 0.014f))), 0.012f);
            // rahang bawah
            sdf.Tube(new[] { V(-0.075f, 1.08f, 0f, 0.013f), V(-0.07f, 1.02f, -0.03f, 0.014f), V(-0.045f, 0.99f, -0.09f, 0.015f), V(0f, 0.98f, -0.115f, 0.017f),
                             V(0.045f, 0.99f, -0.09f, 0.015f), V(0.07f, 1.02f, -0.03f, 0.014f), V(0.075f, 1.08f, 0f, 0.013f) }, 0, smooth: 0.015f);
            // gigi atas & bawah: deretan rapi (garis pemisah tiap gigi)
            foreach (var y in new[] { 1.035f, 1.012f })
            {
                var arc = new List<Vector4>();
                for (int i = 0; i <= 12; i++)
                {
                    float th = Mathf.Lerp(-1.05f, 1.05f, i / 12f);
                    arc.Add(V(0.056f * Mathf.Sin(th), y, -0.072f - 0.05f * Mathf.Cos(th), 0.0095f));
                }
                sdf.Tube(arc.ToArray(), 1, smooth: 0.004f, sub: 4,
                    materialFn: p => Mathf.Repeat(Mathf.Atan2(p.x, -(p.z + 0.072f)) * 7.5f, 1f) < 0.12f ? 0 : 1);
            }
        }

        static void Spine(CompositeSdf sdf)
        {
            // (ruas, jumlah, y atas, y bawah, setengah lebar, setengah tinggi, setengah tebal)
            var groups = new (int n, float top, float bottom, float hx, float hy, float hz)[]
            {
                (5, 0.995f, 0.935f, 0.016f, 0.0055f, 0.014f),   // leher
                (9, 0.918f, 0.71f, 0.019f, 0.0085f, 0.017f),    // punggung
                (4, 0.69f, 0.575f, 0.024f, 0.013f, 0.021f),     // pinggang
            };
            float prevY = -1f, prevHy = 0f, prevHx = 0f, prevHz = 0f;
            foreach (var g in groups)
                for (int i = 0; i < g.n; i++)
                {
                    float y = g.n == 1 ? g.top : Mathf.Lerp(g.top, g.bottom, i / (g.n - 1f));
                    float z = SpineZ(y);
                    sdf.Box(new Vector3(0f, y, z), new Vector3(g.hx, g.hy, g.hz), Mathf.Min(g.hy, g.hz) * 0.8f, 2, smooth: 0.004f);
                    // taju belakang & samping
                    sdf.RoundCone(new Vector3(0f, y, z + g.hz), 0.0065f, new Vector3(0f, y - 0.012f, z + g.hz + 0.024f), 0.0055f, 2);
                    sdf.Tube(new[] { V(-g.hx - 0.012f, y, z + 0.008f, 0.0055f), V(g.hx + 0.012f, y, z + 0.008f, 0.0055f) }, 2, smooth: 0.004f);
                    // bantalan tulang rawan di antara ruas
                    if (prevY > 0f)
                    {
                        float top = y + g.hy, bottom = prevY - prevHy;
                        if (bottom - top > 0.001f)
                        {
                            float my = (top + bottom) * 0.5f;
                            sdf.Box(new Vector3(0f, my, SpineZ(my)), new Vector3(Mathf.Min(g.hx, prevHx) * 0.9f, (bottom - top) * 0.5f + 0.0015f, Mathf.Min(g.hz, prevHz) * 0.9f), 0.002f, 3, smooth: 0.002f);
                        }
                    }
                    prevY = y; prevHy = g.hy; prevHx = g.hx; prevHz = g.hz;
                }
            // tulang kelangkang (menyatu) & tulang ekor
            sdf.RoundCone(new Vector3(0f, 0.555f, SpineZ(0.555f)), 0.03f, new Vector3(0f, 0.46f, 0.072f), 0.012f, 2);
            for (int i = 0; i < 3; i++) sdf.Sphere(new Vector3(0f, 0.448f - i * 0.011f, 0.07f - i * 0.006f), 0.007f - i * 0.0012f, 2);
        }

        static void Ribs(CompositeSdf sdf)
        {
            const float frontZ = -0.095f;
            float[] radius = { 0.07f, 0.092f, 0.11f, 0.124f, 0.134f, 0.14f, 0.142f, 0.14f, 0.134f, 0.124f, 0.112f, 0.1f };
            Vector3 FrontOf(int i, float sx)
            {
                float yb = 0.915f - i * 0.0195f;
                float drop = 0.05f + i * 0.006f;
                return new Vector3(sx * 0.018f, yb - drop - 0.004f, frontZ - 0.003f);
            }
            foreach (var sx in new[] { -1f, 1f })
                for (int i = 0; i < 12; i++)
                {
                    float yb = 0.915f - i * 0.0195f;
                    float zb = SpineZ(yb) + 0.004f;
                    float zc = (zb + frontZ) * 0.5f, dz = (zb - frontZ) * 0.5f;
                    float drop = 0.05f + i * 0.006f;
                    float end = i < 7 ? 0.86f : i < 10 ? 0.8f : 0.55f;   // rusuk sejati, palsu, melayang
                    var pts = new List<Vector4>();
                    for (int k = 0; k <= 10; k++)
                    {
                        float th = Mathf.PI * end * k / 10f;
                        pts.Add(V(sx * (0.022f + (radius[i] - 0.022f) * Mathf.Sin(th)), yb - drop * (1f - Mathf.Cos(th)) * 0.5f, zc + dz * Mathf.Cos(th), 0.0065f));
                    }
                    sdf.Tube(pts.ToArray(), 4, smooth: 0.003f, sub: 4);
                    if (i >= 10) continue;
                    // tulang rawan rusuk: ke tulang dada (rusuk 1–7) atau ke tulang rawan rusuk di atasnya (rusuk 8–10)
                    var last = (Vector3)pts[pts.Count - 1];
                    var to = i < 7 ? FrontOf(i, sx) : FrontOf(6, sx) + new Vector3(sx * (0.02f + (i - 7) * 0.012f), -0.012f - (i - 7) * 0.01f, 0.002f);
                    var mid = Vector3.Lerp(last, to, 0.5f) + new Vector3(0f, -0.006f, -0.006f);
                    sdf.Tube(new[] { V(last, 0.006f), V(mid, 0.0058f), V(to, 0.0055f) }, 3, smooth: 0.003f, sub: 4);
                }
            // tulang dada: hulu, badan, taju pedang
            sdf.Box(new Vector3(0f, 0.884f, -0.098f), new Vector3(0.02f, 0.018f, 0.008f), 0.006f, 5);
            sdf.Box(new Vector3(0f, 0.815f, -0.1f), new Vector3(0.014f, 0.05f, 0.007f), 0.006f, 5);
            sdf.RoundCone(new Vector3(0f, 0.763f, -0.099f), 0.008f, new Vector3(0f, 0.744f, -0.096f), 0.0045f, 5);
        }

        static void ShouldersAndArms(CompositeSdf sdf)
        {
            foreach (var sx in new[] { -1f, 1f })
            {
                // tulang selangka & belikat
                sdf.Tube(new[] { V(sx * 0.018f, 0.905f, -0.095f, 0.0085f), V(sx * 0.07f, 0.915f, -0.085f, 0.0085f), V(sx * 0.12f, 0.905f, -0.04f, 0.009f), V(sx * 0.155f, 0.905f, 0f, 0.0095f) }, 6, smooth: 0.006f);
                sdf.Sphere(new Vector3(sx * 0.1f, 0.835f, 0.088f), 0.062f, 6, scale: new Vector3(0.82f, 1.12f, 0.15f));
                sdf.Tube(new[] { V(sx * 0.055f, 0.872f, 0.098f, 0.0075f), V(sx * 0.12f, 0.892f, 0.085f, 0.008f), V(sx * 0.155f, 0.9f, 0.03f, 0.009f) }, 6, smooth: 0.006f);
                // lengan atas (sendi peluru di bahu)
                sdf.Sphere(new Vector3(sx * 0.165f, 0.88f, 0.012f), 0.021f, 7);
                sdf.Tube(new[] { V(sx * 0.168f, 0.87f, 0.01f, 0.015f), V(sx * 0.2f, 0.79f, 0.005f, 0.0105f), V(sx * 0.235f, 0.71f, 0f, 0.0115f), V(sx * 0.25f, 0.677f, 0f, 0.017f) }, 7, smooth: 0.008f);
                sdf.Sphere(new Vector3(sx * 0.252f, 0.668f, 0f), 0.018f, 7, scale: new Vector3(1.3f, 0.8f, 0.9f));
                // lengan bawah: pengumpil & hasta
                sdf.Tube(new[] { V(sx * 0.259f, 0.652f, -0.008f, 0.0085f), V(sx * 0.29f, 0.56f, -0.013f, 0.008f), V(sx * 0.322f, 0.472f, -0.02f, 0.0115f) }, 8, smooth: 0.004f);
                sdf.Tube(new[] { V(sx * 0.249f, 0.66f, 0.012f, 0.0105f), V(sx * 0.281f, 0.56f, 0.008f, 0.008f), V(sx * 0.309f, 0.472f, 0.002f, 0.009f) }, 8, smooth: 0.004f);
                // tangan: pergelangan, telapak, jari (+ ibu jari)
                for (int i = 0; i < 4; i++) sdf.Sphere(new Vector3(sx * (0.308f + i * 0.0085f), 0.455f - (i % 2) * 0.006f, -0.012f), 0.0085f, 9);
                for (int f = 0; f < 4; f++)
                {
                    float x0 = 0.304f + f * 0.0105f, x1 = 0.298f + f * 0.016f;
                    sdf.Tube(new[] { V(sx * x0, 0.446f, -0.014f, 0.0058f), V(sx * x1, 0.408f, -0.018f, 0.0058f) }, 9, smooth: 0.003f);
                    float len = f == 0 || f == 3 ? 0.04f : 0.048f;
                    sdf.Sphere(new Vector3(sx * x1, 0.405f, -0.018f), 0.0068f, 9);
                    sdf.Tube(new[] { V(sx * x1, 0.402f, -0.018f, 0.0055f), V(sx * (x1 + (f - 1.5f) * 0.004f), 0.402f - len * 0.55f, -0.021f, 0.005f), V(sx * (x1 + (f - 1.5f) * 0.006f), 0.402f - len, -0.024f, 0.0048f) }, 9, smooth: 0.003f);
                }
                sdf.Tube(new[] { V(sx * 0.3f, 0.448f, -0.022f, 0.006f), V(sx * 0.29f, 0.425f, -0.036f, 0.0058f), V(sx * 0.284f, 0.4f, -0.046f, 0.0052f) }, 9, smooth: 0.003f);
            }
        }

        /// <summary>
        /// Sayap tulang panggul berbentuk mangkuk: kulit tipis elips yang bagian depan-tengah, bawah & tengah-belakangnya dibuka.
        /// Harus ditambahkan PERTAMA karena pemotongan (Subtract) berlaku untuk bagian-bagian sebelumnya.
        /// </summary>
        static void PelvisBowl(CompositeSdf sdf)
        {
            var c = new Vector3(0f, 0.53f, 0.03f);
            var r = new Vector3(0.115f, 0.095f, 0.075f);
            sdf.Sphere(c, 1f, 10, scale: r);
            sdf.Subtract(p => Ell(p, c, r - Vector3.one * 0.0085f), 0.004f);              // dinding tipis
            sdf.Subtract(p => 0.548f - p.y, 0.004f);                                       // puncak (krista)
            sdf.Subtract(p => p.y - 0.455f, 0.004f);                                       // bawah
            sdf.Subtract(p => Mathf.Max(p.z, Mathf.Abs(p.x) - 0.09f), 0.004f);             // depan terbuka
            sdf.Subtract(p => Mathf.Abs(p.x) - 0.032f, 0.004f);                            // tengah belakang (tempat kelangkang)
            foreach (var sx in new[] { -1f, 1f })
            {
                var rim = new List<Vector4>();
                for (int i = 0; i <= 10; i++)
                {
                    float a = Mathf.Lerp(0.29f, 1.95f, i / 10f);
                    rim.Add(V(sx * 0.112f * Mathf.Sin(a), 0.546f, 0.03f + 0.073f * Mathf.Cos(a), 0.0065f));
                }
                sdf.Tube(rim.ToArray(), 10, smooth: 0.004f, sub: 4);
            }
        }

        static void PelvisAndLegs(CompositeSdf sdf)
        {
            foreach (var sx in new[] { -1f, 1f })
            {
                // cincin panggul, tulang duduk & kemaluan (sayap panggul dibuat di PelvisBowl)
                sdf.Tube(new[] { V(sx * 0.03f, 0.52f, 0.068f, 0.011f), V(sx * 0.08f, 0.455f, 0.04f, 0.0115f), V(sx * 0.092f, 0.415f, 0f, 0.012f), V(sx * 0.06f, 0.39f, -0.045f, 0.011f), V(sx * 0.012f, 0.385f, -0.066f, 0.0105f) }, 10, smooth: 0.008f);
                sdf.Tube(new[] { V(sx * 0.085f, 0.402f, 0.012f, 0.0105f), V(sx * 0.07f, 0.368f, 0.02f, 0.0125f), V(sx * 0.045f, 0.368f, -0.02f, 0.0095f), V(sx * 0.03f, 0.379f, -0.055f, 0.009f) }, 10, smooth: 0.006f);
                // tulang paha: kepala bulat masuk ke mangkuk panggul
                sdf.Sphere(new Vector3(sx * 0.086f, 0.4f, 0f), 0.019f, 11);
                sdf.Tube(new[] { V(sx * 0.088f, 0.399f, 0f, 0.011f), V(sx * 0.112f, 0.386f, 0.003f, 0.012f) }, 11, smooth: 0.006f);
                sdf.Sphere(new Vector3(sx * 0.118f, 0.392f, 0.008f), 0.014f, 11);
                sdf.Tube(new[] { V(sx * 0.113f, 0.38f, 0.003f, 0.0135f), V(sx * 0.1f, 0.3f, -0.004f, 0.0105f), V(sx * 0.09f, 0.226f, 0f, 0.0125f) }, 11, smooth: 0.006f);
                sdf.Sphere(new Vector3(sx * 0.09f, 0.216f, 0.004f), 0.018f, 11, scale: new Vector3(1.35f, 0.8f, 1.1f));
                // tulang rawan lutut, tempurung lutut
                sdf.Box(new Vector3(sx * 0.09f, 0.201f, 0.004f), new Vector3(0.021f, 0.0028f, 0.016f), 0.0025f, 3, smooth: 0.002f);
                sdf.Sphere(new Vector3(sx * 0.09f, 0.213f, -0.025f), 0.0125f, 12, scale: new Vector3(1f, 1.15f, 0.62f));
                // tulang kering & tulang betis
                sdf.Sphere(new Vector3(sx * 0.09f, 0.19f, 0.004f), 0.018f, 13, scale: new Vector3(1.35f, 0.65f, 1.1f));
                sdf.Tube(new[] { V(sx * 0.09f, 0.186f, 0f, 0.0125f), V(sx * 0.09f, 0.12f, -0.002f, 0.0098f), V(sx * 0.088f, 0.075f, 0f, 0.0115f) }, 13, smooth: 0.006f);
                sdf.Sphere(new Vector3(sx * 0.079f, 0.068f, 0f), 0.0095f, 13);
                sdf.Tube(new[] { V(sx * 0.108f, 0.18f, 0.008f, 0.0072f), V(sx * 0.108f, 0.12f, 0.006f, 0.006f), V(sx * 0.105f, 0.07f, 0.004f, 0.0078f) }, 13, smooth: 0.004f);
                // tulang kaki: pergelangan, tumit, telapak, jari
                sdf.Sphere(new Vector3(sx * 0.09f, 0.055f, 0f), 0.0135f, 14);
                sdf.Sphere(new Vector3(sx * 0.09f, 0.034f, 0.03f), 0.016f, 14, scale: new Vector3(0.9f, 0.9f, 1.3f));
                sdf.Sphere(new Vector3(sx * 0.09f, 0.04f, -0.028f), 0.016f, 14, scale: new Vector3(1.3f, 0.7f, 1.2f));
                for (int t = 0; t < 5; t++)
                {
                    float x0 = 0.074f + t * 0.0075f, x1 = 0.068f + t * 0.011f;
                    sdf.Tube(new[] { V(sx * x0, 0.035f, -0.045f, 0.006f), V(sx * x1, 0.02f, -0.093f, 0.0058f) }, 14, smooth: 0.003f);
                    sdf.Tube(new[] { V(sx * x1, 0.019f, -0.097f, 0.0055f), V(sx * x1, 0.016f, -0.117f + t * 0.004f, 0.005f) }, 14, smooth: 0.003f);
                }
            }
            // sambungan tulang kemaluan (tulang rawan)
            sdf.Box(new Vector3(0f, 0.385f, -0.066f), new Vector3(0.0065f, 0.014f, 0.008f), 0.004f, 3, smooth: 0.004f);
        }

        [MenuItem("Organo/Model 3D/Buat Ulang Model Rangka", priority = 50)]
        public static void BuildAll()
        {
            OrganoSetup.EnsureDatabase();
            AssetDatabase.SaveAssets();
            BuildSkeleton();
            BuildInside();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Model "Dalam" rangka (tulang pipa, pipih, pendek) + gambar bagian organ (untuk command line).</summary>
        public static void BuildInsideAndImages()
        {
            OrganoSetup.EnsureDatabase();
            BuildInside();
            DetailAssetsBuilder.RenderPartImages();
            AssetDatabase.SaveAssets();
        }

        public static void BuildSkeleton()
        {
            var sdf = new CompositeSdf(0.008f);
            PelvisBowl(sdf);
            Skull(sdf);
            Spine(sdf);
            Ribs(sdf);
            ShouldersAndArms(sdf);
            PelvisAndLegs(sdf);
            OrganModelBuilder.Build("rangka", "Rangka", sdf, new Bounds(new Vector3(0f, 0.64f, 0f), new Vector3(0.76f, 1.32f, 0.3f)), 0.0055f,
                Colors,
                new[]
                {
                    ("Tengkorak", "Melindungi otak", new Vector3(0f, 1.2f, -0.09f)),
                    ("Tulang rusuk", "Melindungi jantung & paru-paru", new Vector3(0.13f, 0.8f, -0.06f)),
                    ("Tulang belakang", "Menegakkan tubuh", new Vector3(0f, 0.76f, 0.09f)),
                    ("Tulang panggul", "Menopang tubuh bagian atas", new Vector3(0.11f, 0.47f, -0.01f)),
                    ("Tulang paha", "Tulang terpanjang", new Vector3(0.11f, 0.3f, -0.015f)),
                    ("Tulang tangan", "27 tulang kecil", new Vector3(0.33f, 0.42f, -0.03f)),
                }, new Vector3(4f, -16f, 0f), 1f);
        }

        // ================================================================ BAGIAN DALAM TULANG (tiga jenis tulang dibelah)

        // Materi IPA kelas 5 SD: bentuk tulang (pipa, pipih, pendek), tulang keras & rawan, sumsum tulang.
        // Ketiganya dibelah supaya terlihat bedanya: tulang pipa berongga berisi sumsum kuning, tulang pipih seperti roti lapis
        // (dua lapis tulang keras mengapit tulang spons), tulang pendek hampir semuanya tulang spons.
        // material: tulang pipa (paha) 0 selaput tulang, 1 tulang rawan sendi, 2 tulang keras, 3 tulang spons, 4 sumsum merah (dipakai
        // ketiganya), 5 sumsum kuning; tulang pipih 6 luar, 7 tulang keras, 8 tulang spons; tulang pendek 9 luar, 10 tulang keras, 11 tulang spons
        static readonly Vector2 PipihArc = new Vector2(0.43f, -0.27f);
        const float PipihR = 0.52f, PipihT = 0.036f;

        /// <summary>Pola tulang spons: rongga kecil berisi sumsum merah.</summary>
        static bool Marrow(Vector3 p) =>
            Mathf.Sin(p.x * 120f + Mathf.Sin(p.y * 55f) * 1.6f) * Mathf.Sin(p.y * 120f + Mathf.Sin(p.x * 50f) * 1.6f) > 0.3f;

        public static void BuildInside()
        {
            var colors = new[]
            {
                C("SelaputTulang", "#F3E6C8"), C("TulangRawanSendi", "#BFE3F0", 0.55f), C("TulangKeras", "#FAF4E6", 0.4f),
                C("TulangSpons", "#E9D3A6", 0.35f), C("SumsumMerah", "#E5737F", 0.4f), C("SumsumKuning", "#F5D27A", 0.45f),
                C("PipihLuar", "#F3E6C8"), C("PipihKeras", "#FAF4E6", 0.4f), C("PipihSpons", "#E9D3A6", 0.35f),
                C("PendekLuar", "#F3E6C8"), C("PendekKeras", "#FAF4E6", 0.4f), C("PendekSpons", "#E9D3A6", 0.35f),
            };
            var bone = new CompositeSdf(0.05f);
            // tulang pipih (mis. sepotong tengkorak): lempeng melengkung. Dibuat PERTAMA karena dipotong (Subtract).
            bone.Box(new Vector3(0.43f, 0.215f, 0f), new Vector3(0.19f, 0.1f, 0.12f), 0.03f, 6, smooth: 0.004f);
            bone.Subtract(p => new Vector2(p.x - PipihArc.x, p.y - PipihArc.y).magnitude - (PipihR - PipihT), 0.006f);
            bone.Subtract(p => (PipihR + PipihT) - new Vector2(p.x - PipihArc.x, p.y - PipihArc.y).magnitude, 0.006f);
            // tulang pendek (mis. ruas tulang belakang / pergelangan): hampir kotak
            bone.Box(new Vector3(0.43f, -0.22f, 0f), new Vector3(0.12f, 0.085f, 0.12f), 0.045f, 9, smooth: 0.004f);

            // tulang pipa: tulang paha (kepala bulat, batang, dua bonggol lutut)
            var head = new Vector3(-0.13f, 0.4f, 0f);
            var condL = new Vector3(-0.055f, -0.4f, 0.01f); var condR = new Vector3(0.05f, -0.4f, 0.01f);
            Func<Vector3, int> outerMat = p =>
                (p - head).magnitude > 0.06f && p.x < -0.1f && p.y > 0.36f ? 1 :
                p.y < -0.43f ? 1 : 0;
            bone.Sphere(head, 0.085f, 0, materialFn: outerMat);
            bone.Tube(new[] { V(-0.11f, 0.385f, 0f, 0.055f), V(-0.02f, 0.33f, 0f, 0.062f) }, 0, materialFn: outerMat);
            bone.Sphere(new Vector3(0.07f, 0.36f, 0f), 0.065f, 0, scale: new Vector3(1f, 1.2f, 0.9f), materialFn: outerMat);
            bone.Tube(new[] { V(0.02f, 0.3f, 0f, 0.06f), V(0f, 0f, 0f, 0.046f), V(-0.01f, -0.3f, 0f, 0.056f) }, 0, materialFn: outerMat);
            bone.Sphere(condL, 0.066f, 0, materialFn: outerMat);
            bone.Sphere(condR, 0.06f, 0, materialFn: outerMat);
            bone.SetMaterialCount(colors.Length);

            var sdf = new CutawaySdf(colors.Length)
            {
                Outer = bone, Cut = p => p.z,
                FaceMaterial = p =>
                {
                    float d0 = bone.Eval(p);
                    if (p.x > 0.2f && p.y > -0.02f)
                    {
                        // pipih: tulang keras di kedua sisi lengkungan, tulang spons & sumsum merah di tengah
                        float rr = new Vector2(p.x - PipihArc.x, p.y - PipihArc.y).magnitude;
                        if (rr > PipihR + PipihT - 0.011f || rr < PipihR - PipihT + 0.011f || d0 > -0.008f) return 7;
                        return Marrow(p) ? 4 : 8;
                    }
                    if (p.x > 0.2f)
                    {
                        // pendek: kulit tulang keras tipis, isinya tulang spons
                        if (d0 > -0.01f) return 10;
                        return Marrow(p) ? 4 : 11;
                    }
                    bool shaft = p.y < 0.24f && p.y > -0.26f;
                    if (bone.Material(p) == 1 && d0 > -0.012f) return 1;
                    if (d0 > (shaft ? -0.02f : -0.009f)) return 2;
                    if (shaft) return 5;
                    return Marrow(p) ? 4 : 3;
                },
            };
            var dir = "Assets/Organo/Models/Rangka";
            StomachModelBuilder.EnsureFolder(dir);
            var mats = new Material[colors.Length];
            for (int i = 0; i < colors.Length; i++) mats[i] = OrganModelBuilder.MakeMat(dir + "/RangkaDalam_" + colors[i].Item1 + ".mat", colors[i].Item2, colors[i].Item3);
            var mesh = SurfaceNets.Build(sdf, new Bounds(new Vector3(0.205f, 0f, 0f), new Vector3(0.95f, 1.06f, 0.3f)), 0.0055f);
            mesh.name = "Rangka_Dalam";
            var meshPath = dir + "/Rangka_Dalam_Mesh.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null) AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);
            var root = new GameObject("Rangka_Dalam");
            var body = new GameObject("Mesh", typeof(MeshFilter), typeof(MeshRenderer));
            body.transform.SetParent(root.transform, false);
            body.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = body.GetComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = ShadowCastingMode.On;
            foreach (var (title, desc, pos) in new[]
                     {
                         ("Tulang pipa", "Contoh: tulang paha & lengan", new Vector3(-0.02f, -0.33f, 0f)),
                         ("Tulang pipih", "Contoh: tengkorak & rusuk", new Vector3(0.43f, 0.25f, 0f)),
                         ("Tulang pendek", "Contoh: ruas tulang belakang", new Vector3(0.43f, -0.22f, 0f)),
                         ("Tulang rawan sendi", "Pelapis licin ujung tulang", new Vector3(-0.17f, 0.46f, 0f)),
                         ("Tulang spons", "Berongga, ringan tapi kuat", new Vector3(-0.1f, 0.38f, 0f)),
                         ("Sumsum merah", "Membuat sel darah", new Vector3(0.06f, 0.36f, 0f)),
                         ("Sumsum kuning", "Simpanan lemak", new Vector3(0f, 0f, 0f)),
                         ("Tulang keras", "Lapisan luar yang padat", new Vector3(0.045f, -0.1f, 0f)),
                     })
            {
                var go = new GameObject("Hotspot " + title, typeof(OrganHotspot));
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = pos;
                var h = go.GetComponent<OrganHotspot>();
                h.title = title; h.description = desc;
            }
            var prefabPath = dir + "/Rangka_Dalam.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            var organ = AssetDatabase.LoadAssetAtPath<Organo.Data.OrganInfo>("Assets/Organo/Data/Organ_rangka.asset");
            if (organ != null)
            {
                organ.insidePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                organ.insideEuler = new Vector3(4f, -8f, 0f);
                EditorUtility.SetDirty(organ);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Organo] Model dalam Rangka_Dalam (tulang pipa, pipih, pendek) dibuat (" + mesh.vertexCount + " verteks, " + StomachModelBuilder.TriCount(mesh) + " segitiga).");
        }
    }
}
