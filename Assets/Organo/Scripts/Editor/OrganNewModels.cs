using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Organo.EditorTools
{
    /// <summary>
    /// Model organ baru (mata, hidung, tenggorokan) dan mulut versi lengkung gigi.
    /// Acuan isi: KidsHealth (Your Eyes / Your Nose / Mouth and Teeth) dan CHOP (Throat Anatomy).
    /// Gaya: warna lembut, tanpa urat/darah yang menyeramkan, bentuk membulat supaya ramah anak SD.
    /// Depan model = -z, atas = +y.
    /// </summary>
    public static class OrganNewModels
    {
        static (string, Color32, float) C(string name, string hex, float smooth = 0.5f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return (name, (Color32)c, smooth);
        }

        static float Seg(Vector3 p, Vector3 a, Vector3 b, float r)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-8f));
            return (p - (a + ab * t)).magnitude - r;
        }

        static float RoundBox(Vector3 p, Vector3 c, Vector3 half, float r)
        {
            var q = new Vector3(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y), Mathf.Abs(p.z - c.z)) - (half - Vector3.one * r);
            var m = new Vector3(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f), Mathf.Max(q.z, 0f));
            return m.magnitude + Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y, q.z)), 0f) - r;
        }

        // ================================================================ MATA

        [MenuItem("Organo/Model 3D/Buat Ulang Model Mata", priority = 44)]
        public static void BuildEye()
        {
            var sdf = new CompositeSdf(0.02f);
            // 0 sklera (putih mata), 1 kornea, 2 iris, 3 pupil, 4 otot mata, 5 saraf mata
            Func<Vector3, int> front = p =>
            {
                float rad = new Vector2(p.x, p.y).magnitude;
                if (p.z < -0.29f && rad < 0.2f) return rad < 0.072f ? 3 : rad < 0.17f ? 2 : 1;
                return 0;
            };
            sdf.Sphere(Vector3.zero, 0.4f, 0, materialFn: front);
            sdf.Sphere(new Vector3(0f, 0f, -0.24f), 0.215f, 1, materialFn: front);
            // empat otot lurus mata: dari depan bola mata ke belakang, berkumpul di sekitar saraf mata
            foreach (var u in new[] { Vector2.up, Vector2.down, Vector2.right, Vector2.left })
            {
                Vector4 P(float k, float z, float r) => new Vector4(u.x * k, u.y * k, z, r);
                sdf.Tube(new[] { P(0.37f, -0.1f, 0.048f), P(0.425f, 0.12f, 0.05f), P(0.31f, 0.38f, 0.042f), P(0.11f, 0.64f, 0.03f) }, 4, smooth: 0.015f);
            }
            sdf.Tube(new[] { new Vector4(0f, -0.02f, 0.34f, 0.07f), new Vector4(0.02f, 0f, 0.6f, 0.066f), new Vector4(0.05f, 0.02f, 0.82f, 0.06f) }, 5, smooth: 0.02f);

            OrganModelBuilder.Build("mata", "Mata", sdf, new Bounds(new Vector3(0f, 0f, 0.17f), new Vector3(1.1f, 1.1f, 1.42f)), 0.0138f,
                new[]
                {
                    C("Sklera", "#FBFBFD", 0.7f), C("Kornea", "#D9ECFF", 0.9f), C("Iris", "#4D9DE0", 0.65f),
                    C("Pupil", "#1F1D33", 0.85f), C("OtotMata", "#F29BAE", 0.45f), C("SarafMata", "#F2C46B", 0.45f),
                },
                new[]
                {
                    ("Kornea", "Jendela bening di depan mata", new Vector3(0.06f, 0.1f, -0.47f)),
                    ("Iris", "Bagian berwarna, mengatur cahaya", new Vector3(0.13f, -0.05f, -0.42f)),
                    ("Bagian putih", "Lapisan pelindung bola mata", new Vector3(-0.3f, 0.18f, -0.22f)),
                    ("Otot mata", "Menggerakkan bola mata", new Vector3(0.0f, 0.44f, 0.08f)),
                    ("Saraf mata", "Mengirim gambar ke otak", new Vector3(0.05f, 0.04f, 0.78f)),
                }, new Vector3(8f, -28f, 0f), 1f);
        }

        // ================================================================ HIDUNG

        /// <summary>Rongga udara di dalam hidung (dipakai untuk potongan "Lihat dalam").</summary>
        public static float NoseCavity(Vector3 p) =>
            Mathf.Min(Mathf.Min(
                RoundBox(p, new Vector3(0f, 0.16f, 0.06f), new Vector3(0.085f, 0.15f, 0.22f), 0.07f),
                Mathf.Min(Seg(p, new Vector3(-0.042f, -0.05f, -0.255f), new Vector3(-0.04f, 0.05f, -0.15f), 0.026f),
                          Seg(p, new Vector3(0.042f, -0.05f, -0.255f), new Vector3(0.04f, 0.05f, -0.15f), 0.026f))),
                Mathf.Min(Seg(p, new Vector3(0f, 0.12f, 0.26f), new Vector3(0f, -0.05f, 0.3f), 0.05f), Seg(p, new Vector3(0f, -0.05f, 0.3f), new Vector3(0f, -0.3f, 0.29f), 0.05f)));

        [MenuItem("Organo/Model 3D/Buat Ulang Model Hidung", priority = 45)]
        public static void BuildNose()
        {
            var sdf = new CompositeSdf(0.03f);
            // 0 hidung luar (kulit), 1 rongga hidung (bening), 2 konka, 3 sekat hidung, 4 saraf penciuman, 5 bulu hidung, 6 jalan ke tenggorokan
            sdf.RoundCone(new Vector3(0f, 0.34f, -0.17f), 0.045f, new Vector3(0f, 0.04f, -0.29f), 0.085f, 0);
            foreach (var sx in new[] { -1f, 1f })
                sdf.Sphere(new Vector3(sx * 0.085f, 0.01f, -0.22f), 0.062f, 0, scale: new Vector3(1f, 0.85f, 1.1f));
            sdf.Subtract(p => Mathf.Min((p - new Vector3(-0.042f, -0.05f, -0.255f)).magnitude, (p - new Vector3(0.042f, -0.05f, -0.255f)).magnitude) - 0.03f, 0.01f);
            sdf.Box(new Vector3(0f, 0.16f, 0.06f), new Vector3(0.105f, 0.175f, 0.245f), 0.085f, 1);
            sdf.Tube(new[] { new Vector4(0f, 0.12f, 0.26f, 0.07f), new Vector4(0f, -0.05f, 0.3f, 0.072f), new Vector4(0f, -0.3f, 0.29f, 0.068f) }, 6, smooth: 0.03f);
            sdf.Sphere(new Vector3(0f, 0.385f, 0.1f), 0.04f, 4, scale: new Vector3(1f, 0.6f, 1.6f));
            sdf.Tube(new[] { new Vector4(0f, 0.4f, 0.14f, 0.02f), new Vector4(0f, 0.47f, 0.2f, 0.018f), new Vector4(0f, 0.52f, 0.28f, 0.016f) }, 4, smooth: 0.01f);

            // bagian dalam yang tetap terlihat menembus rongga bening
            var inner = new CompositeSdf(0.015f);
            foreach (var sx in new[] { -1f, 1f })
            {
                inner.Tube(new[] { new Vector4(sx * 0.07f, 0.25f, -0.0f, 0.022f), new Vector4(sx * 0.073f, 0.262f, 0.1f, 0.024f), new Vector4(sx * 0.075f, 0.255f, 0.19f, 0.02f) }, 2, smooth: 0.01f);
                inner.Tube(new[] { new Vector4(sx * 0.065f, 0.16f, -0.08f, 0.026f), new Vector4(sx * 0.07f, 0.168f, 0.06f, 0.032f), new Vector4(sx * 0.075f, 0.16f, 0.21f, 0.026f) }, 2, smooth: 0.01f);
                inner.Tube(new[] { new Vector4(sx * 0.06f, 0.06f, -0.14f, 0.03f), new Vector4(sx * 0.068f, 0.068f, 0.05f, 0.036f), new Vector4(sx * 0.075f, 0.06f, 0.25f, 0.03f) }, 2, smooth: 0.01f);
                for (int i = 0; i < 4; i++)
                {
                    float a = i * 1.6f;
                    var root = new Vector3(sx * 0.042f + Mathf.Cos(a) * 0.017f, -0.035f, -0.25f + Mathf.Sin(a) * 0.012f);
                    inner.Tube(new[] { new Vector4(root.x, root.y, root.z, 0.009f), new Vector4(root.x * 0.9f, root.y + 0.04f, root.z + 0.03f, 0.007f) }, 5, smooth: 0.004f);
                }
            }
            inner.Box(new Vector3(0f, 0.16f, 0.05f), new Vector3(0.007f, 0.15f, 0.22f), 0.005f, 3, smooth: 0.004f);
            inner.Sphere(new Vector3(0f, 0.305f, 0.08f), 0.07f, 4, scale: new Vector3(1.3f, 0.25f, 1.5f));

            OrganModelBuilder.Build("hidung", "Hidung", sdf, new Bounds(new Vector3(0f, 0.12f, 0.0f), new Vector3(0.42f, 0.88f, 0.84f)), 0.0085f,
                new[]
                {
                    C("HidungLuar", "#F2B89A", 0.4f), C("RonggaHidung", "#F49AA878", 0.55f), C("Konka", "#E86A7A", 0.5f), C("Sekat", "#F5C2CF", 0.45f),
                    C("Penciuman", "#F2C46B", 0.5f), C("BuluHidung", "#8B6A55", 0.3f), C("JalanTenggorokan", "#E98F86", 0.5f),
                },
                new[]
                {
                    ("Lubang hidung", "Pintu masuk udara", new Vector3(0.05f, -0.06f, -0.29f)),
                    ("Rongga hidung", "Udara dihangatkan & dibersihkan", new Vector3(0.11f, 0.25f, 0.1f)),
                    ("Konka", "Rak berlekuk di dinding rongga", new Vector3(0.1f, 0.07f, 0.12f)),
                    ("Saraf penciuman", "Mengirim bau ke otak", new Vector3(0f, 0.44f, 0.15f)),
                    ("Ke tenggorokan", "Udara turun ke tenggorokan", new Vector3(0f, -0.2f, 0.36f)),
                }, new Vector3(6f, -42f, 0f), 1f, inner, null, true);
        }

        // ================================================================ TENGGOROKAN

        /// <summary>Saluran udara & makanan di dalam tenggorokan (untuk potongan "Lihat dalam").</summary>
        public static float ThroatLumen(Vector3 p)
        {
            float d = Seg(p, new Vector3(0f, 0.6f, 0.1f), new Vector3(0f, 0.35f, 0.11f), 0.05f);
            d = Mathf.Min(d, Seg(p, new Vector3(0f, 0.35f, 0.11f), new Vector3(0f, 0.12f, 0.1f), 0.05f));
            d = Mathf.Min(d, Seg(p, new Vector3(0f, 0.12f, 0.1f), new Vector3(0f, 0.05f, 0.06f), 0.04f));
            d = Mathf.Min(d, Seg(p, new Vector3(0f, 0.12f, 0.0f), new Vector3(0f, -0.12f, 0.0f), 0.034f));
            d = Mathf.Min(d, Seg(p, new Vector3(0f, -0.12f, 0.0f), new Vector3(0f, -0.66f, 0.02f), 0.044f));
            d = Mathf.Min(d, Seg(p, new Vector3(0f, 0.06f, 0.08f), new Vector3(0f, 0.02f, 0.13f), 0.03f));
            d = Mathf.Min(d, Seg(p, new Vector3(0f, 0.02f, 0.13f), new Vector3(0f, -0.66f, 0.14f), 0.026f));
            return d;
        }

        [MenuItem("Organo/Model 3D/Buat Ulang Model Tenggorokan", priority = 46)]
        public static void BuildThroat()
        {
            var sdf = new CompositeSdf(0.025f);
            // 0 faring, 1 epiglotis, 2 laring, 3 trakea, 4 cincin tulang rawan, 5 kerongkongan, 6 amandel, 7 pita suara
            sdf.Tube(new[] { new Vector4(0f, 0.6f, 0.1f, 0.08f), new Vector4(0f, 0.35f, 0.12f, 0.085f), new Vector4(0f, 0.12f, 0.12f, 0.08f), new Vector4(0f, 0.02f, 0.12f, 0.07f) }, 0);
            foreach (var sx in new[] { -1f, 1f })
                sdf.Sphere(new Vector3(sx * 0.085f, 0.36f, 0.06f), 0.045f, 6, scale: new Vector3(0.8f, 1.2f, 0.8f));
            sdf.Sphere(new Vector3(0f, 0.2f, 0.02f), 0.07f, 1, scale: new Vector3(1f, 1.3f, 0.3f));
            sdf.Box(new Vector3(0f, 0.02f, -0.01f), new Vector3(0.1f, 0.08f, 0.075f), 0.035f, 2);
            sdf.Sphere(new Vector3(0f, 0.07f, -0.08f), 0.035f, 2);
            sdf.Box(new Vector3(0f, -0.1f, 0.01f), new Vector3(0.07f, 0.03f, 0.065f), 0.03f, 2);
            Func<Vector3, int> rings = p => p.y < -0.13f && Mathf.Repeat(p.y * 14f, 1f) < 0.38f ? 4 : 3;
            sdf.Tube(new[] { new Vector4(0f, -0.12f, 0f, 0.065f), new Vector4(0f, -0.4f, 0.01f, 0.064f), new Vector4(0f, -0.66f, 0.02f, 0.062f) }, 3, materialFn: rings);
            sdf.Tube(new[] { new Vector4(0f, 0.02f, 0.13f, 0.055f), new Vector4(0f, -0.3f, 0.14f, 0.052f), new Vector4(0f, -0.66f, 0.14f, 0.05f) }, 5, smooth: 0.015f);

            OrganModelBuilder.Build("tenggorokan", "Tenggorokan", sdf, new Bounds(new Vector3(0f, -0.02f, 0.05f), new Vector3(0.38f, 1.4f, 0.42f)), 0.0085f,
                new[]
                {
                    C("Faring", "#E98F86", 0.5f), C("Epiglotis", "#F29BAE", 0.5f), C("Laring", "#CDBFF5", 0.55f), C("Trakea", "#9C84EC", 0.55f),
                    C("Cincin", "#EEEAFB", 0.5f), C("Kerongkongan", "#F0A85C", 0.5f), C("Amandel", "#F27C8C", 0.45f), C("PitaSuara", "#FFFFFF", 0.6f),
                },
                new[]
                {
                    ("Faring", "Persimpangan udara & makanan", new Vector3(0f, 0.46f, 0.02f)),
                    ("Amandel", "Penjaga dari kuman", new Vector3(0.1f, 0.36f, 0.0f)),
                    ("Epiglotis", "Tutup saat menelan", new Vector3(0f, 0.23f, -0.02f)),
                    ("Laring", "Kotak suara", new Vector3(0f, 0.07f, -0.12f)),
                    ("Trakea", "Udara ke paru-paru", new Vector3(0f, -0.35f, -0.07f)),
                    ("Kerongkongan", "Makanan ke lambung", new Vector3(0.05f, -0.35f, 0.18f)),
                }, new Vector3(4f, -35f, 0f), 1f);
        }

        // ================================================================ MULUT (lengkung gigi atas & bawah)

        public static void BuildMouth()
        {
            var sdf = new CompositeSdf(0.012f);
            // 0 bibir, 1 gigi seri, 2 gigi taring, 3 gigi geraham, 4 gusi, 5 lidah, 6 langit-langit, 7 anak tekak, 8 rongga (belakang)
            Vector3 Arch(float th, float y) => new Vector3(0.3f * Mathf.Sin(th), y, -0.3f * Mathf.Cos(th) + 0.04f);
            var teeth = new (float th, int kind)[] { (0.1f, 0), (0.29f, 0), (0.48f, 1), (0.68f, 2), (0.88f, 2), (1.1f, 3), (1.34f, 3) };
            foreach (var jaw in new[] { 1f, -1f })
            {
                foreach (var side in new[] { -1f, 1f })
                    foreach (var (th, kind) in teeth)
                    {
                        var c = Arch(th * side, jaw * 0.2f);
                        switch (kind)
                        {
                            case 0: // gigi seri: pipih seperti pahat
                                sdf.Box(c, new Vector3(0.026f, 0.056f, 0.02f), 0.012f, 1, smooth: 0.004f);
                                break;
                            case 1: // gigi taring: runcing
                                sdf.RoundCone(c + new Vector3(0f, jaw * 0.04f, 0f), 0.03f, c - new Vector3(0f, jaw * 0.062f, 0f), 0.01f, 2);
                                break;
                            case 2: // geraham kecil: dua tonjolan
                                sdf.Sphere(c, 0.044f, 3, scale: new Vector3(0.85f, 1.15f, 0.85f));
                                sdf.Sphere(c - new Vector3(0f, jaw * 0.045f, 0.012f), 0.022f, 3);
                                sdf.Sphere(c - new Vector3(0f, jaw * 0.045f, -0.012f), 0.022f, 3);
                                break;
                            default: // geraham besar: empat tonjolan
                                sdf.Sphere(c, 0.05f, 3, scale: new Vector3(0.9f, 1.0f, 1.15f));
                                foreach (var o in new[] { new Vector2(-0.016f, -0.02f), new Vector2(0.016f, -0.02f), new Vector2(-0.016f, 0.02f), new Vector2(0.016f, 0.02f) })
                                    sdf.Sphere(c + new Vector3(o.x, -jaw * 0.042f, o.y), 0.022f, 3);
                                break;
                        }
                    }
                // gusi mengikuti lengkung
                var gum = new List<Vector4>();
                for (int i = 0; i <= 16; i++)
                {
                    float th = Mathf.Lerp(-1.45f, 1.45f, i / 16f);
                    var p = Arch(th, jaw * 0.268f);
                    gum.Add(new Vector4(p.x, p.y, p.z, 0.046f));
                }
                sdf.Tube(gum.ToArray(), 4, smooth: 0.02f);
            }
            // langit-langit dengan lipatan melintang, anak tekak & dinding belakang
            sdf.Sphere(new Vector3(0f, 0.29f, 0.07f), 0.26f, 6, scale: new Vector3(1f, 0.28f, 1f), disp: p => 0.004f * Mathf.Sin(p.z * 60f), dispMax: 0.004f);
            sdf.RoundCone(new Vector3(0f, 0.27f, 0.27f), 0.03f, new Vector3(0f, 0.15f, 0.28f), 0.027f, 7);
            sdf.Sphere(new Vector3(0f, 0.0f, 0.3f), 0.27f, 8, scale: new Vector3(1f, 0.9f, 0.12f));
            // lidah dengan bintil pengecap & alur tengah
            sdf.Sphere(new Vector3(0f, -0.21f, 0.06f), 0.24f, 5, scale: new Vector3(0.82f, 0.3f, 1f),
                disp: p => 0.0035f * Mathf.Sin(p.x * 110f) * Mathf.Sin(p.z * 110f), dispMax: 0.004f);
            sdf.Groove(new Vector3(0f, 0f, 0f), Vector3.right, 0.012f, new Vector3(0f, -0.15f, -0.02f), 0.14f);
            // bibir: cincin lembut di depan
            var lip = new List<Vector4>();
            for (int i = 0; i <= 40; i++)
            {
                float a = i / 40f * Mathf.PI * 2f;
                float x = Mathf.Cos(a) * 0.43f, y = Mathf.Sin(a) * 0.38f;
                float r = 0.03f + 0.018f * Mathf.Abs(Mathf.Sin(a));
                if (y > 0) y -= 0.02f * Mathf.Exp(-x * x * 90f);
                lip.Add(new Vector4(x, y, -0.33f + 0.13f * Mathf.Cos(a) * Mathf.Cos(a), r));
            }
            sdf.Tube(lip.ToArray(), 0, smooth: 0.02f, sub: 2);

            OrganModelBuilder.Build("mulut", "Mulut", sdf, new Bounds(new Vector3(0f, 0f, 0.0f), new Vector3(1.06f, 0.94f, 0.86f)), 0.0125f,
                new[]
                {
                    C("Bibir", "#E8506A", 0.65f), C("GigiSeri", "#FFFFFF", 0.8f), C("GigiTaring", "#FFF6E6", 0.8f), C("GigiGeraham", "#FAEFD6", 0.8f),
                    C("Gusi", "#F28C9B", 0.55f), C("Lidah", "#F06A80", 0.5f), C("LangitLangit", "#F5A3B0", 0.5f), C("AnakTekak", "#EF7F92", 0.5f),
                    C("Rongga", "#B5505F", 0.3f),
                },
                new[]
                {
                    ("Gigi seri", "Memotong makanan", new Vector3(0f, 0.2f, -0.31f)),
                    ("Gigi taring", "Merobek makanan", new Vector3(0.14f, 0.18f, -0.26f)),
                    ("Gigi geraham", "Menggiling makanan", new Vector3(0.27f, -0.19f, -0.12f)),
                    ("Lidah", "Merasakan & mendorong makanan", new Vector3(0f, -0.16f, -0.06f)),
                    ("Anak tekak", "Menutup jalan ke hidung saat menelan", new Vector3(0f, 0.17f, 0.22f)),
                }, new Vector3(14f, -16f, 0f), 1f);
        }
    }
}
