using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Organo.EditorTools
{
    /// <summary>
    /// Model organ detail per bagian (untuk anak SD): tiap bagian punya warna sendiri + titik info.
    /// Acuan bentuk: model Sketchfab yang diberikan (usus, ginjal, hati, otak, pankreas).
    /// Target ±30–45 ribu segitiga per model supaya ringan di HP.
    /// </summary>
    public static partial class OrganModelBuilder
    {
        static float Noise3(Vector3 p, float f)
        {
            return Mathf.Sin(p.x * f + Mathf.Sin(p.y * f * 0.7f) * 1.7f) *
                   Mathf.Sin(p.y * f * 1.1f + Mathf.Sin(p.z * f * 0.8f) * 1.5f) *
                   Mathf.Sin(p.z * f * 0.9f + Mathf.Sin(p.x * f * 0.6f) * 1.9f);
        }

        // ================================================================ MULUT

        [MenuItem("Organo/Model 3D/Buat Ulang Model Mulut", priority = 34)]
        public static void BuildMouth() => OrganNewModels.BuildMouth();

        static Vector4[] Arc(float halfWidth, float y, float z, float r, int n)
        {
            var a = new Vector4[n];
            for (int i = 0; i < n; i++)
            {
                float t = (i / (float)(n - 1)) * 2f - 1f;
                a[i] = new Vector4(t * halfWidth, y, z + 0.07f * t * t, r);
            }
            return a;
        }

        // ================================================================ KERONGKONGAN

        [MenuItem("Organo/Model 3D/Buat Ulang Model Kerongkongan", priority = 35)]
        public static void BuildEsophagus()
        {
            var sdf = new CompositeSdf(0.04f);
            // 0 faring, 1 otot kerongkongan, 2 cincin otot, 3 sfingter, 4 lambung (konteks)
            sdf.RoundCone(new Vector3(0f, 0.66f, 0.02f), 0.15f, new Vector3(0f, 0.44f, 0f), 0.08f, 0);
            Func<Vector3, int> bands = p => p.y < 0.42f && p.y > -0.46f && Mathf.Repeat(p.y * 11f, 1f) < 0.22f ? 2 : 1;
            sdf.Tube(new[]
            {
                new Vector4(0f, 0.46f, 0f, 0.075f), new Vector4(0.02f, 0.2f, 0f, 0.075f),
                new Vector4(0.0f, 0.04f, 0f, 0.105f), new Vector4(-0.02f, -0.1f, 0f, 0.075f),
                new Vector4(0.02f, -0.36f, 0f, 0.074f), new Vector4(0.06f, -0.5f, 0f, 0.075f)
            }, 1, materialFn: bands);
            sdf.Tube(new[] { new Vector4(0.06f, -0.51f, 0f, 0.092f), new Vector4(0.08f, -0.56f, 0f, 0.092f) }, 3, smooth: 0.02f);
            sdf.Sphere(new Vector3(0.2f, -0.72f, 0.02f), 0.2f, 4, scale: new Vector3(1.2f, 0.9f, 0.8f));

            Build("kerongkongan", "Kerongkongan", sdf, new Bounds(new Vector3(0.08f, -0.06f, 0f), new Vector3(0.85f, 1.65f, 0.5f)), 0.0095f,
                new[]
                {
                    ("Faring", new Color32(0xF0, 0xA0, 0xAA, 0xFF), 0.55f),
                    ("Otot", new Color32(0xE8, 0x70, 0x7E, 0xFF), 0.6f),
                    ("Cincin", new Color32(0xD2, 0x55, 0x68, 0xFF), 0.6f),
                    ("Sfingter", new Color32(0xB8, 0x3A, 0x50, 0xFF), 0.6f),
                    ("Lambung", new Color32(0xF5, 0x8C, 0xA3, 0xFF), 0.55f),
                },
                new[]
                {
                    ("Tenggorokan", "Jalan masuk dari mulut", new Vector3(0f, 0.6f, -0.12f)),
                    ("Gumpalan makanan", "Didorong otot ke bawah", new Vector3(0f, 0.04f, -0.1f)),
                    ("Otot kerongkongan", "Meremas makanan turun", new Vector3(0.02f, -0.25f, -0.07f)),
                    ("Pintu lambung", "Menutup agar makanan tidak naik", new Vector3(0.08f, -0.54f, -0.09f)),
                }, new Vector3(0f, -10f, 0f), 1f);
        }

        // ================================================================ USUS HALUS

        [MenuItem("Organo/Model 3D/Buat Ulang Model Usus Halus", priority = 36)]
        public static void BuildSmallIntestine()
        {
            var sdf = new CompositeSdf(0.012f);
            // 0 duodenum, 1 jejunum, 2 ileum
            sdf.Tube(new[]
            {
                new Vector4(0.12f, 0.5f, 0f, 0.055f), new Vector4(0.3f, 0.44f, 0f, 0.055f), new Vector4(0.36f, 0.3f, 0f, 0.052f),
                new Vector4(0.28f, 0.2f, -0.02f, 0.05f), new Vector4(0.15f, 0.2f, -0.03f, 0.05f)
            }, 0, sub: 6);
            // Gulungan usus: jalur berkelok-kelok (lengkungan kecil seperti per) yang mengisi bantalan membulat,
            // dua lapis (depan & belakang) supaya terlihat padat dari segala arah.
            var jej = new List<Vector4> { new Vector4(0.15f, 0.2f, -0.03f, 0.05f) };
            var ile = new List<Vector4>();
            const float R = 0.044f;
            float HalfW(float y) => Mathf.Max(0.12f, 0.36f * Mathf.Sqrt(Mathf.Max(0f, 1f - (y * y) / (0.42f * 0.42f))));
            void Row(List<Vector4> list, float y, float z, bool toLeft, int r, float freq)
            {
                float hw = HalfW(y);
                const int N = 44;
                for (int k = 1; k <= N; k++)
                {
                    float u = k / (float)N;
                    float env = Mathf.Sin(u * Mathf.PI);
                    float ph = u * Mathf.PI * 2f * freq + r * 1.3f;
                    float x = (toLeft ? Mathf.Lerp(hw, -hw, u) : Mathf.Lerp(-hw, hw, u)) + 0.032f * Mathf.Sin(ph) * env;
                    float yy = y + 0.036f * Mathf.Cos(ph) * env + 0.01f * Mathf.Sin(u * 7f + r);
                    float zz = z + 0.035f * Mathf.Sin(u * Mathf.PI * 2.4f + r * 0.8f) * env;
                    list.Add(new Vector4(x, yy, zz, R));
                }
            }
            // lapis depan bagian atas (jejunum), lalu lapis belakang (jejunum), lalu depan bagian bawah (ileum)
            float[] ys = { 0.2f, 0.1f, 0.0f, -0.1f, -0.2f, -0.29f };
            Row(jej, ys[0], -0.075f, true, 0, 5.5f);
            Row(jej, ys[1], -0.08f, false, 1, 6.5f);
            Row(jej, ys[2], -0.075f, true, 2, 6f);
            // pindah ke belakang dan naik
            jej.Add(new Vector4(-0.36f, -0.02f, 0.0f, R));
            float[] back = { 0.0f, 0.12f, 0.22f };
            for (int i = 0; i < back.Length; i++) Row(jej, back[i] - 0.05f, 0.07f, i % 2 == 1, 3 + i, 5f + i * 0.5f);
            ile.Add(jej[jej.Count - 1]);
            Row(ile, -0.06f, 0.075f, true, 6, 5.5f);
            Row(ile, -0.17f, 0.07f, false, 7, 6f);
            // kembali ke depan, bagian bawah
            ile.Add(new Vector4(0.3f, -0.15f, 0.0f, R));
            Row(ile, ys[3], -0.075f, true, 8, 6f);
            Row(ile, ys[4], -0.07f, false, 9, 6.5f);
            Row(ile, ys[5], -0.06f, true, 10, 5f);
            // ujung ileum menuju usus besar (kanan bawah tubuh = kiri layar)
            ile.Add(new Vector4(-0.36f, -0.36f, -0.03f, R));
            ile.Add(new Vector4(-0.43f, -0.42f, 0.0f, R));
            sdf.Tube(jej.ToArray(), 1, sub: 2);
            sdf.Tube(ile.ToArray(), 2, sub: 2);
            // Pembuluh darah mesenterium di sisi belakang: satu batang utama melengkung turun di balik gulungan, bercabang ke
            // kiri-kanan, dan tiap cabang berakhir dua ranting yang menempel ke dinding usus (lengkungan "arkade").
            // Arteri (merah) dan vena (biru) berjalan berdampingan. Semuanya tetap di dalam garis luar gulungan, jadi dari
            // depan tidak ada yang mencuat. (Dulu: batang lurus memancar dari satu titik di atas usus dan melayang di belakangnya.)
            Vector3 Trunk(float t) => new Vector3(-0.02f + 0.05f * Mathf.Sin(t * Mathf.PI), Mathf.Lerp(0.15f, -0.22f, t), 0.158f);
            // ujung cabang tetap di dalam garis luar gulungan (supaya tidak terlihat dari depan)
            Vector3 Inside(Vector3 p) => new Vector3(Mathf.Clamp(p.x, -0.75f * HalfW(p.y), 0.75f * HalfW(p.y)), Mathf.Clamp(p.y, -0.25f, 0.18f), p.z);
            void Pair(Vector3[] path, float ra, float rv)
            {
                var art = new Vector4[path.Length];
                var vein = new Vector4[path.Length];
                for (int i = 0; i < path.Length; i++)
                {
                    var dir = path[Mathf.Min(i + 1, path.Length - 1)] - path[Mathf.Max(i - 1, 0)];
                    var side = new Vector3(-dir.y, dir.x, 0f).normalized * 0.016f;   // tegak lurus arah pembuluh
                    float k = path.Length > 1 ? i / (float)(path.Length - 1) : 0f;
                    float shrink = Mathf.Lerp(1f, 0.8f, k);
                    art[i] = new Vector4(path[i].x - side.x, path[i].y - side.y, path[i].z, ra * shrink);
                    vein[i] = new Vector4(path[i].x + side.x, path[i].y + side.y, path[i].z + 0.002f, rv * shrink);
                }
                sdf.Tube(art, 3, smooth: 0.004f, sub: 3);
                sdf.Tube(vein, 4, smooth: 0.004f, sub: 3);
            }
            var trunk = new Vector3[7];
            for (int i = 0; i < trunk.Length; i++) trunk[i] = Trunk(i / 6f);
            Pair(trunk, 0.017f, 0.019f);
            float[] at = { 0.12f, 0.3f, 0.46f, 0.62f, 0.78f, 0.92f };
            for (int i = 0; i < at.Length; i++)
            {
                float sgn = i % 2 == 0 ? -1f : 1f;
                var a = Trunk(at[i]);
                float reach = 0.2f - 0.05f * Mathf.Abs(at[i] - 0.5f);
                var end = Inside(new Vector3(a.x + sgn * reach, a.y - 0.04f, 0.145f));
                var mid = Vector3.Lerp(a, end, 0.5f) + new Vector3(0f, 0.025f, 0.006f);
                Pair(new[] { a, mid, end }, 0.013f, 0.0145f);
                // dua ranting menempel ke usus
                foreach (float dy in new[] { 0.06f, -0.06f })
                {
                    var tip = Inside(end + new Vector3(sgn * 0.05f, dy, -0.012f));
                    Pair(new[] { end, Vector3.Lerp(end, tip, 0.5f) + new Vector3(0f, 0f, 0.004f), tip }, 0.0105f, 0.0115f);
                }
            }

            Build("usushalus", "UsusHalus", sdf, new Bounds(new Vector3(-0.02f, 0.04f, 0.02f), new Vector3(1.0f, 1.08f, 0.5f)), 0.0115f,
                new[]
                {
                    ("Duodenum", new Color32(0xF2, 0xA0, 0x7B, 0xFF), 0.6f),
                    ("Jejunum", new Color32(0xF5, 0x8E, 0x7A, 0xFF), 0.6f),
                    ("Ileum", new Color32(0xE8, 0x7A, 0x72, 0xFF), 0.6f),
                    ("Arteri", new Color32(0xE0, 0x3A, 0x45, 0xFF), 0.6f),
                    ("Vena", new Color32(0x3E, 0x5F, 0xD6, 0xFF), 0.6f),
                },
                new[]
                {
                    ("Usus dua belas jari", "Bagian awal, tersambung ke lambung", new Vector3(0.34f, 0.36f, -0.06f)),
                    ("Jejunum", "Menyerap sari-sari makanan", new Vector3(0.18f, 0.1f, -0.13f)),
                    ("Ileum", "Bagian akhir, menuju usus besar", new Vector3(0.05f, -0.2f, -0.12f)),
                }, new Vector3(4f, -8f, 0f), 1f);
        }

        // ================================================================ USUS BESAR

        [MenuItem("Organo/Model 3D/Buat Ulang Model Usus Besar", priority = 37)]
        public static void BuildLargeIntestine()
        {
            var sdf = new CompositeSdf(0.025f);
            // 0 sekum, 1 usus buntu, 2 kolon naik, 3 kolon melintang, 4 kolon turun, 5 rektum, 6 pita otot (taenia)
            sdf.Sphere(new Vector3(-0.41f, -0.33f, 0f), 0.12f, 0, scale: new Vector3(1f, 1.05f, 0.9f));
            sdf.Tube(new[] { new Vector4(-0.38f, -0.43f, -0.02f, 0.026f), new Vector4(-0.32f, -0.52f, -0.04f, 0.021f), new Vector4(-0.35f, -0.6f, -0.05f, 0.019f) }, 1, smooth: 0.02f);
            var ctrl = new[]
            {
                new Vector3(-0.41f, -0.26f, 0f), new Vector3(-0.43f, 0.0f, 0f), new Vector3(-0.42f, 0.24f, 0f), new Vector3(-0.33f, 0.36f, -0.01f),
                new Vector3(-0.1f, 0.34f, -0.03f), new Vector3(0.12f, 0.37f, -0.02f), new Vector3(0.34f, 0.37f, 0f), new Vector3(0.44f, 0.27f, 0.01f),
                new Vector3(0.45f, 0.0f, 0.02f), new Vector3(0.44f, -0.22f, 0.01f), new Vector3(0.36f, -0.34f, 0f), new Vector3(0.18f, -0.33f, -0.02f),
                new Vector3(0.07f, -0.4f, -0.01f),
            };
            // titik pusat & bagian tiap titik (naik 2, melintang 3, turun 4, sigmoid 5)
            int[] section = { 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 5, 5, 5 };
            var core = new List<Vector4>();
            var pts = new List<(Vector3 p, Vector3 t, int sec)>();
            for (int i = 0; i < ctrl.Length - 1; i++)
            {
                var p0 = ctrl[Mathf.Max(0, i - 1)]; var p1 = ctrl[i]; var p2 = ctrl[i + 1]; var p3 = ctrl[Mathf.Min(ctrl.Length - 1, i + 2)];
                int steps = Mathf.Max(2, Mathf.RoundToInt(Vector3.Distance(p1, p2) / 0.055f));
                for (int k = 0; k < steps; k++)
                {
                    float u = k / (float)steps, u2 = u * u, u3 = u2 * u;
                    var q = 0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2 + (-p0 + 3f * p1 - 3f * p2 + p3) * u3);
                    var tan = (0.5f * ((-p0 + p2) + 2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * u + 3f * (-p0 + 3f * p1 - 3f * p2 + p3) * u2)).normalized;
                    pts.Add((q, tan, section[i]));
                    core.Add(new Vector4(q.x, q.y, q.z, 0.058f));
                }
            }
            core.Add(new Vector4(0.07f, -0.4f, -0.01f, 0.058f));
            sdf.Tube(core.ToArray(), 6, smooth: 0.02f);
            // tiga baris kantong (haustra) di sekeliling pipa, di antaranya terlihat pita otot pucat
            for (int i = 0; i < pts.Count; i++)
            {
                var (q, tan, sec) = pts[i];
                var n1 = Vector3.Cross(tan, Vector3.forward).normalized;
                if (n1.sqrMagnitude < 0.01f) n1 = Vector3.right;
                var n2 = Vector3.Cross(tan, n1).normalized;
                for (int a2 = 0; a2 < 3; a2++)
                {
                    float ang = a2 * Mathf.PI * 2f / 3f + (i % 2) * 0.35f;
                    var off = (n1 * Mathf.Cos(ang) + n2 * Mathf.Sin(ang)) * 0.042f;
                    sdf.Sphere(q + off, 0.052f, sec);
                }
            }
            sdf.Tube(new[] { new Vector4(0.07f, -0.4f, -0.01f, 0.07f), new Vector4(0.04f, -0.5f, 0f, 0.078f), new Vector4(0.03f, -0.64f, 0f, 0.07f), new Vector4(0.03f, -0.7f, 0f, 0.05f) }, 5, smooth: 0.03f);

            Build("ususbesar", "UsusBesar", sdf, new Bounds(new Vector3(0.01f, -0.12f, 0f), new Vector3(1.15f, 1.25f, 0.34f)), 0.0105f,
                new[]
                {
                    ("Sekum", new Color32(0xF0, 0x8C, 0x80, 0xFF), 0.55f),
                    ("UsusBuntu", new Color32(0xF6, 0xA8, 0x92, 0xFF), 0.55f),
                    ("KolonNaik", new Color32(0xE9, 0x63, 0x6E, 0xFF), 0.6f),
                    ("KolonMelintang", new Color32(0xEE, 0x72, 0x7A, 0xFF), 0.6f),
                    ("KolonTurun", new Color32(0xE2, 0x5A, 0x66, 0xFF), 0.6f),
                    ("Rektum", new Color32(0xD0, 0x4C, 0x5C, 0xFF), 0.6f),
                    ("Taenia", new Color32(0xF5, 0xC7, 0xCB, 0xFF), 0.5f),
                },
                new[]
                {
                    ("Usus buntu", "Kantong kecil berisi bakteri baik", new Vector3(-0.33f, -0.55f, -0.06f)),
                    ("Haustra", "Kantong-kantong usus besar", new Vector3(0f, 0.37f, -0.1f)),
                    ("Kolon turun", "Membawa sisa makanan ke bawah", new Vector3(0.45f, 0.0f, -0.09f)),
                    ("Rektum", "Tempat sisa makanan sebelum dibuang", new Vector3(0.03f, -0.58f, -0.08f)),
                }, new Vector3(4f, -6f, 0f), 1f);
        }

        /// <summary>Tabung dengan tonjolan berurutan (haustra) seperti usus besar.</summary>
        static Vector4[] Haustra(Vector3 a, Vector3 b, float r, int bulges, float bend = 0f)
        {
            var list = new List<Vector4>();
            int n = bulges * 4;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                var p = Vector3.Lerp(a, b, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * bend);
                float rr = r * (0.82f + 0.2f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * bulges)));
                list.Add(new Vector4(p.x, p.y, p.z, rr));
            }
            return list.ToArray();
        }

        // ================================================================ GINJAL (sepasang)

        [MenuItem("Organo/Model 3D/Buat Ulang Model Ginjal", priority = 33)]
        public static void BuildKidney()
        {
            var sdf = new CompositeSdf(0.04f);
            // 0 ginjal, 1 arteri (merah), 2 vena (biru), 3 ureter, 4 kelenjar adrenal
            Func<Vector3, float> lobes = p => 0.008f * Noise3(p, 15f);
            foreach (var side in new[] { -1f, 1f })
            {
                var c = new Vector3(side * 0.3f, side > 0 ? 0.04f : 0f, 0.04f);
                sdf.Sphere(c, 0.24f, 0, scale: new Vector3(0.78f, 1.25f, 0.55f), disp: lobes, dispMax: 0.01f);
            }
            sdf.Subtract(p => Vector3.Scale(p - new Vector3(-0.12f, 0f, 0.04f), new Vector3(1f, 0.8f, 1f)).magnitude - 0.1f, 0.06f);
            sdf.Subtract(p => Vector3.Scale(p - new Vector3(0.12f, 0.04f, 0.04f), new Vector3(1f, 0.8f, 1f)).magnitude - 0.1f, 0.06f);
            // aorta (merah) & vena cava (biru) di tengah
            sdf.Tube(new[] { new Vector4(0.035f, 0.5f, 0.09f, 0.04f), new Vector4(0.035f, -0.62f, 0.09f, 0.037f) }, 1, smooth: 0.01f);
            sdf.Tube(new[] { new Vector4(-0.045f, 0.5f, 0.07f, 0.046f), new Vector4(-0.045f, -0.62f, 0.07f, 0.044f) }, 2, smooth: 0.01f);
            foreach (var side in new[] { -1f, 1f })
            {
                float y0 = side > 0 ? 0.04f : 0f;
                sdf.Tube(new[] { new Vector4(0.035f, y0 + 0.07f, 0.09f, 0.02f), new Vector4(side * 0.2f, y0 + 0.06f, 0.06f, 0.02f) }, 1, smooth: 0.015f);
                sdf.Tube(new[] { new Vector4(-0.045f, y0 + 0.01f, 0.07f, 0.024f), new Vector4(side * 0.2f, y0 + 0.0f, 0.05f, 0.024f) }, 2, smooth: 0.015f);
                sdf.Tube(new[]
                {
                    new Vector4(side * 0.2f, y0 - 0.06f, 0.04f, 0.026f), new Vector4(side * 0.17f, y0 - 0.2f, 0.03f, 0.018f),
                    new Vector4(side * 0.13f, -0.42f, 0.02f, 0.016f), new Vector4(side * 0.08f, -0.62f, 0.0f, 0.016f)
                }, 3, smooth: 0.02f);
                sdf.Sphere(new Vector3(side * 0.28f, y0 + 0.31f, 0.04f), 0.1f, 4, scale: new Vector3(1.1f, 0.5f, 0.55f));
            }

            Build("ginjal", "Ginjal", sdf, new Bounds(new Vector3(0f, -0.06f, 0.05f), new Vector3(1.0f, 1.3f, 0.45f)), 0.0105f,
                new[]
                {
                    ("Ginjal", new Color32(0xB5, 0x2E, 0x3A, 0xFF), 0.65f),
                    ("Arteri", new Color32(0xE0, 0x3A, 0x45, 0xFF), 0.65f),
                    ("Vena", new Color32(0x3E, 0x5F, 0xD6, 0xFF), 0.65f),
                    ("Ureter", new Color32(0xF2, 0xA8, 0x4B, 0xFF), 0.55f),
                    ("Adrenal", new Color32(0xF2, 0xC1, 0x4E, 0xFF), 0.5f),
                },
                new[]
                {
                    ("Ginjal", "Menyaring darah jadi bersih", new Vector3(-0.36f, 0.05f, -0.08f)),
                    ("Aorta", "Membawa darah kotor masuk", new Vector3(0.04f, 0.35f, 0.04f)),
                    ("Vena", "Membawa darah bersih keluar", new Vector3(-0.05f, -0.35f, 0.02f)),
                    ("Ureter", "Saluran urine ke kandung kemih", new Vector3(0.14f, -0.42f, 0.0f)),
                }, new Vector3(4f, 0f, 0f), 1f);
        }

        // ================================================================ HATI

        [MenuItem("Organo/Model 3D/Buat Ulang Model Hati", priority = 38)]
        public static void BuildLiver()
        {
            var sdf = new CompositeSdf(0.1f);
            // 0 lobus kanan, 1 lobus kiri, 2 ligamen, 3 kantong empedu, 4 vena, 5 arteri
            Func<Vector3, int> lobes = p => Mathf.Abs(p.x - 0.04f + 0.12f * p.y) < 0.022f && p.z < 0f ? 2 : p.x < 0.04f - 0.12f * p.y ? 0 : 1;
            sdf.Sphere(new Vector3(-0.18f, 0.0f, 0f), 0.28f, 0, scale: new Vector3(1.15f, 0.95f, 0.75f), materialFn: lobes);
            sdf.Sphere(new Vector3(-0.32f, -0.06f, 0f), 0.22f, 0, scale: new Vector3(1f, 1f, 0.75f), materialFn: lobes);
            sdf.Tube(new[]
            {
                new Vector4(0.0f, 0.06f, -0.01f, 0.2f), new Vector4(0.22f, 0.08f, -0.01f, 0.13f),
                new Vector4(0.42f, 0.07f, 0f, 0.07f), new Vector4(0.52f, 0.06f, 0f, 0.035f)
            }, 1, materialFn: lobes);
            sdf.Subtract(p => p.y + 0.22f + 0.15f * p.x, 0.06f); // potong bagian bawah supaya tepinya lebih rata
            sdf.RoundCone(new Vector3(-0.08f, -0.24f, -0.12f), 0.07f, new Vector3(-0.02f, -0.12f, -0.07f), 0.035f, 3);
            sdf.Tube(new[] { new Vector4(-0.02f, 0.18f, 0.06f, 0.045f), new Vector4(-0.02f, 0.42f, 0.06f, 0.042f) }, 4, smooth: 0.03f);
            sdf.Tube(new[] { new Vector4(0.06f, 0.16f, 0.07f, 0.032f), new Vector4(0.07f, 0.4f, 0.07f, 0.03f) }, 5, smooth: 0.03f);

            Build("hati", "Hati", sdf, new Bounds(new Vector3(0.04f, 0.02f, 0f), new Vector3(1.25f, 0.95f, 0.5f)), 0.0095f,
                new[]
                {
                    ("LobusKanan", new Color32(0xC8, 0x41, 0x4B, 0xFF), 0.7f),
                    ("LobusKiri", new Color32(0xD4, 0x55, 0x5D, 0xFF), 0.7f),
                    ("Ligamen", new Color32(0xF2, 0xA8, 0x3B, 0xFF), 0.6f),
                    ("KantongEmpedu", new Color32(0x3E, 0x9B, 0x4F, 0xFF), 0.65f),
                    ("Vena", new Color32(0x3E, 0x5F, 0xD6, 0xFF), 0.6f),
                    ("Arteri", new Color32(0xE0, 0x3A, 0x45, 0xFF), 0.6f),
                },
                new[]
                {
                    ("Lobus kanan", "Bagian hati yang paling besar", new Vector3(-0.3f, 0.02f, -0.2f)),
                    ("Lobus kiri", "Bagian hati yang lebih kecil", new Vector3(0.28f, 0.07f, -0.1f)),
                    ("Kantong empedu", "Menyimpan cairan empedu", new Vector3(-0.08f, -0.24f, -0.19f)),
                    ("Pembuluh darah", "Darah masuk untuk dibersihkan", new Vector3(0.02f, 0.36f, 0.0f)),
                }, new Vector3(6f, -8f, 0f), 1f);
        }

        // ================================================================ PANKREAS

        /// <summary>Garis tengah pankreas: prosesus unsinatus → kepala → leher → badan → ekor (x+ = kiri tubuh, ke arah limpa).</summary>
        internal static readonly Vector4[] PancreasCore =
        {
            new Vector4(-0.13f, -0.17f, 0.02f, 0.048f), new Vector4(-0.21f, -0.1f, 0.0f, 0.095f), new Vector4(-0.24f, 0.02f, -0.01f, 0.112f),
            new Vector4(-0.1f, 0.07f, -0.02f, 0.07f), new Vector4(0.08f, 0.1f, -0.01f, 0.076f), new Vector4(0.26f, 0.14f, 0.0f, 0.066f),
            new Vector4(0.42f, 0.2f, 0.02f, 0.05f), new Vector4(0.5f, 0.24f, 0.03f, 0.034f),
        };

        /// <summary>Usus dua belas jari berbentuk huruf C yang memeluk kepala pankreas.</summary>
        internal static readonly Vector3[] PancreasDuodenum =
        {
            new Vector3(0.02f, 0.25f, 0.02f), new Vector3(-0.14f, 0.23f, 0.02f), new Vector3(-0.33f, 0.18f, 0.02f), new Vector3(-0.44f, 0.03f, 0.02f),
            new Vector3(-0.41f, -0.16f, 0.02f), new Vector3(-0.25f, -0.275f, 0.02f), new Vector3(-0.06f, -0.27f, 0.02f), new Vector3(0.07f, -0.21f, 0.03f),
            new Vector3(0.13f, -0.14f, 0.04f),
        };

        /// <summary>Saluran pankreas (Wirsung) dari ekor ke papila di dinding usus dua belas jari.</summary>
        internal static readonly Vector3[] PancreasDuct =
        {
            new Vector3(0.48f, 0.23f, 0.03f), new Vector3(0.3f, 0.15f, 0.01f), new Vector3(0.1f, 0.1f, 0.0f), new Vector3(-0.08f, 0.06f, 0.0f),
            new Vector3(-0.2f, 0.0f, 0.0f), new Vector3(-0.3f, -0.04f, 0.0f), new Vector3(-0.38f, -0.05f, 0.02f),
        };

        static Vector3 CatmullRom3(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        [MenuItem("Organo/Model 3D/Buat Ulang Model Pankreas", priority = 39)]
        public static void BuildPancreas()
        {
            var sdf = new CompositeSdf(0.03f);
            // 0 kepala, 1 usus dua belas jari, 2 limpa, 3 arteri, 4 vena, 5 kantong & saluran empedu, 6 badan, 7 ekor
            Func<Vector3, int> region = p => p.x < -0.14f ? 0 : p.x < 0.27f ? 6 : 7;
            sdf.Tube(PancreasCore, 0, materialFn: region, sub: 6);
            // permukaan berlobus: tonjolan-tonjolan kecil membulat di sepanjang kelenjar (seperti aslinya, tapi lembut)
            var core = PancreasCore;
            int bump = 0;
            for (int i = 0; i < core.Length - 1; i++)
            {
                var p0 = (Vector3)core[Mathf.Max(0, i - 1)]; var p1 = (Vector3)core[i]; var p2 = (Vector3)core[i + 1]; var p3 = (Vector3)core[Mathf.Min(core.Length - 1, i + 2)];
                int steps = Mathf.Max(2, Mathf.RoundToInt(Vector3.Distance(p1, p2) / 0.032f));
                for (int k = 0; k < steps; k++)
                {
                    float t = k / (float)steps;
                    var c = CatmullRom3(p0, p1, p2, p3, t);
                    float r = Mathf.Lerp(core[i].w, core[i + 1].w, t);
                    var tan = (CatmullRom3(p0, p1, p2, p3, Mathf.Min(1f, t + 0.05f)) - c).normalized;
                    if (tan.sqrMagnitude < 0.01f) tan = Vector3.right;
                    var n1 = Vector3.Cross(tan, Vector3.forward).normalized;
                    var n2 = Vector3.Cross(tan, n1).normalized;
                    for (int a = 0; a < 4; a++)
                    {
                        float ang = a * Mathf.PI * 0.5f + bump * 0.9f;
                        var off = (n1 * Mathf.Cos(ang) + n2 * Mathf.Sin(ang)) * r * 0.72f;
                        sdf.Sphere(c + off, r * (0.42f + 0.08f * Mathf.Sin(bump * 2.3f)), 0, materialFn: region);
                    }
                    bump++;
                }
            }
            // usus dua belas jari (huruf C) memeluk kepala pankreas
            var duo = new List<Vector4>();
            foreach (var q in PancreasDuodenum) duo.Add(new Vector4(q.x, q.y, q.z, 0.056f));
            duo[0] = new Vector4(duo[0].x, duo[0].y, duo[0].z, 0.064f);
            sdf.Tube(duo.ToArray(), 1, smooth: 0.02f, sub: 6);
            // kantong empedu (buah pir hijau) & saluran empedu yang turun di belakang kepala pankreas ke usus
            sdf.RoundCone(new Vector3(-0.47f, 0.43f, 0.03f), 0.058f, new Vector3(-0.36f, 0.31f, 0.05f), 0.024f, 5);
            sdf.Tube(new[]
            {
                new Vector4(-0.36f, 0.31f, 0.05f, 0.016f), new Vector4(-0.3f, 0.22f, 0.09f, 0.016f), new Vector4(-0.3f, 0.08f, 0.1f, 0.017f),
                new Vector4(-0.34f, -0.04f, 0.07f, 0.016f), new Vector4(-0.39f, -0.05f, 0.04f, 0.015f)
            }, 5, smooth: 0.01f);
            // limpa (kacang ungu lembut) di ujung ekor
            sdf.Sphere(new Vector3(0.65f, 0.3f, 0.06f), 0.13f, 2, scale: new Vector3(0.6f, 1f, 0.55f));
            // arteri limpa berkelok di tepi atas badan pankreas
            var art = new List<Vector4>();
            for (int i = 0; i <= 12; i++)
            {
                float t = i / 12f;
                float x = Mathf.Lerp(-0.04f, 0.58f, t);
                float y = Mathf.Lerp(0.2f, 0.31f, t) + 0.018f * Mathf.Sin(t * Mathf.PI * 5f);
                art.Add(new Vector4(x, y, 0.07f, 0.017f));
            }
            sdf.Tube(art.ToArray(), 3, smooth: 0.01f);
            // arteri mesenterika (merah) & vena mesenterika/porta (biru) lewat di belakang leher pankreas
            sdf.Tube(new[] { new Vector4(0.01f, 0.24f, 0.11f, 0.022f), new Vector4(0.02f, 0.0f, 0.1f, 0.022f), new Vector4(0.04f, -0.38f, 0.08f, 0.02f) }, 3, smooth: 0.01f);
            sdf.Tube(new[] { new Vector4(-0.05f, -0.38f, 0.09f, 0.026f), new Vector4(-0.06f, 0.0f, 0.11f, 0.027f), new Vector4(-0.09f, 0.1f, 0.1f, 0.03f), new Vector4(-0.16f, 0.33f, 0.1f, 0.03f) }, 4, smooth: 0.01f);
            // vena limpa di belakang badan pankreas
            sdf.Tube(new[] { new Vector4(0.57f, 0.27f, 0.08f, 0.02f), new Vector4(0.3f, 0.17f, 0.09f, 0.022f), new Vector4(0.08f, 0.12f, 0.1f, 0.024f), new Vector4(-0.08f, 0.09f, 0.1f, 0.027f) }, 4, smooth: 0.01f);

            Build("pankreas", "Pankreas", sdf, new Bounds(new Vector3(0.12f, 0.04f, 0.01f), new Vector3(1.42f, 0.98f, 0.42f)), 0.0095f,
                new[]
                {
                    ("Pankreas", new Color32(0xF0, 0xBE, 0x86, 0xFF), 0.45f),
                    ("Duodenum", new Color32(0xEB, 0x86, 0x84, 0xFF), 0.55f),
                    ("Limpa", new Color32(0x9B, 0x5C, 0x8F, 0xFF), 0.5f),
                    ("Arteri", new Color32(0xE0, 0x3A, 0x45, 0xFF), 0.6f),
                    ("Vena", new Color32(0x4A, 0x6C, 0xD8, 0xFF), 0.6f),
                    ("KantongEmpedu", new Color32(0x5C, 0xB8, 0x6A, 0xFF), 0.6f),
                    ("BadanPankreas", new Color32(0xF3, 0xC9, 0x97, 0xFF), 0.45f),
                    ("EkorPankreas", new Color32(0xF6, 0xD6, 0xAA, 0xFF), 0.45f),
                },
                new[]
                {
                    ("Kepala pankreas", "Dipeluk usus dua belas jari", new Vector3(-0.27f, 0.02f, -0.13f)),
                    ("Badan pankreas", "Membuat cairan pencernaan & insulin", new Vector3(0.12f, 0.11f, -0.1f)),
                    ("Ekor pankreas", "Dekat limpa", new Vector3(0.44f, 0.21f, -0.07f)),
                    ("Usus dua belas jari", "Tempat cairan pankreas masuk", new Vector3(-0.46f, 0.02f, -0.04f)),
                    ("Limpa", "Tetangga pankreas, menyaring darah", new Vector3(0.66f, 0.32f, -0.03f)),
                }, new Vector3(8f, -10f, 0f), 1f);
        }

        // ================================================================ OTAK

        [MenuItem("Organo/Model 3D/Buat Ulang Model Otak", priority = 40)]
        public static void BuildBrain()
        {
            var sdf = new CompositeSdf(0.02f);
            // 0 lobus depan, 1 lobus atas, 2 lobus samping, 3 lobus belakang, 4 otak kecil, 5 batang otak
            Func<Vector3, float> gyri = p =>
            {
                float g = Mathf.Abs(Noise3(p, 17f)) + 0.6f * Mathf.Abs(Noise3(p + new Vector3(0.3f, 0.1f, 0.2f), 29f));
                return 0.016f * g - 0.01f;
            };
            Func<Vector3, int> lobes = p =>
            {
                if (p.z < -0.12f && p.y > -0.08f) return 0;
                if (p.z > 0.2f) return 3;
                if (p.y < 0.0f) return 2;
                return 1;
            };
            foreach (var side in new[] { -1f, 1f })
                sdf.Sphere(new Vector3(side * 0.115f, 0.06f, 0.02f), 0.31f, 1, scale: new Vector3(0.58f, 0.8f, 1.15f), disp: gyri, dispMax: 0.02f, materialFn: lobes);
            sdf.Groove(new Vector3(0f, 0f, 0f), Vector3.right, 0.05f, new Vector3(0f, 0.18f, 0f), 0.42f);
            sdf.Sphere(new Vector3(0f, -0.2f, 0.26f), 0.2f, 4, scale: new Vector3(1.35f, 0.62f, 0.85f),
                disp: p => 0.006f * Mathf.Sin(p.y * 90f + p.z * 20f), dispMax: 0.007f);
            sdf.Tube(new[] { new Vector4(0f, -0.14f, 0.12f, 0.075f), new Vector4(0f, -0.32f, 0.16f, 0.065f), new Vector4(0f, -0.46f, 0.2f, 0.055f) }, 5, smooth: 0.03f);

            Build("otak", "Otak", sdf, new Bounds(new Vector3(0f, -0.06f, 0.04f), new Vector3(0.7f, 0.95f, 0.95f)), 0.0115f,
                new[]
                {
                    ("LobusDepan", new Color32(0xF2, 0x9B, 0xAE, 0xFF), 0.45f),
                    ("LobusAtas", new Color32(0xF6, 0xB0, 0x8E, 0xFF), 0.45f),
                    ("LobusSamping", new Color32(0xA9, 0xC4, 0xF2, 0xFF), 0.45f),
                    ("LobusBelakang", new Color32(0xB9, 0xA7, 0xF0, 0xFF), 0.45f),
                    ("OtakKecil", new Color32(0xF2, 0xC4, 0x6B, 0xFF), 0.45f),
                    ("BatangOtak", new Color32(0xE7, 0x93, 0x93, 0xFF), 0.5f),
                },
                new[]
                {
                    ("Lobus depan", "Berpikir & merencanakan", new Vector3(0.12f, 0.12f, -0.36f)),
                    ("Lobus atas", "Merasakan sentuhan", new Vector3(-0.1f, 0.3f, 0.02f)),
                    ("Lobus samping", "Mendengar", new Vector3(-0.25f, -0.08f, -0.05f)),
                    ("Lobus belakang", "Melihat", new Vector3(0.12f, 0.1f, 0.36f)),
                    ("Otak kecil", "Menjaga keseimbangan", new Vector3(0.14f, -0.22f, 0.3f)),
                    ("Batang otak", "Mengatur napas & detak jantung", new Vector3(0f, -0.36f, 0.12f)),
                }, new Vector3(8f, -55f, 0f), 1f);
        }
    }
}
