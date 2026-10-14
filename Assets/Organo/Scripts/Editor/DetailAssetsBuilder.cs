using Func = System.Func<UnityEngine.Vector3, int>;
using System.Collections.Generic;
using System.IO;
using Organo.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Organo.EditorTools
{
    /// <summary>
    /// Aset untuk Library yang lebih detail:
    /// - model mikro (alveolus, vili, nefron, neuron),
    /// - model anak 3D (laki-laki & perempuan) bergaya kartun mengikuti ilustrasi Library, plus versi pudar,
    /// - gambar tiap bagian organ (bagian disorot, lainnya pudar) untuk kartu & halaman Info Lengkap.
    /// </summary>
    public static class DetailAssetsBuilder
    {
        const string MicroDir = "Assets/Organo/Models/Mikro";
        const string KidDir = "Assets/Organo/Models/Tubuh";
        const string ImgDir = "Assets/Organo/Images/Parts";

        [MenuItem("Organo/Model 3D/Buat Aset Library (anak 3D, mikro, gambar bagian)", priority = 42)]
        public static void BuildAll()
        {
            BuildMicroModels();
            BuildKids();
            AssetDatabase.SaveAssets();
        }

        // ================================================================ model mikro

        public static Dictionary<string, GameObject> BuildMicroModels()
        {
            var result = new Dictionary<string, GameObject>();
            StomachModelBuilder.EnsureFolder(MicroDir);

            // ALVEOLUS: bronkiolus bercabang, ujungnya gugus kantong udara seperti anggur, dibungkus kapiler
            {
                var sdf = new CompositeSdf(0.03f);
                sdf.Tube(new[] { new Vector4(0f, 0.55f, 0f, 0.055f), new Vector4(0f, 0.15f, 0f, 0.05f) }, 0);
                var ends = new[] { new Vector3(-0.26f, -0.12f, 0f), new Vector3(0.26f, -0.1f, 0.02f), new Vector3(0.0f, -0.28f, -0.04f) };
                int seed = 1;
                foreach (var e in ends)
                {
                    sdf.Tube(new[] { new Vector4(0f, 0.15f, 0f, 0.045f), new Vector4(e.x * 0.6f, e.y * 0.5f + 0.06f, e.z, 0.035f), new Vector4(e.x, e.y + 0.08f, e.z, 0.03f) }, 0, smooth: 0.02f);
                    for (int i = 0; i < 9; i++)
                    {
                        float a = i * 2.39996f + seed, h = (i / 8f) * 2f - 1f;
                        var off = new Vector3(Mathf.Cos(a) * Mathf.Sqrt(1 - h * h), h, Mathf.Sin(a) * Mathf.Sqrt(1 - h * h)) * 0.085f;
                        sdf.Sphere(e + off, 0.062f, 1);
                    }
                    for (int k = 0; k < 2; k++)
                    {
                        var arc = new List<Vector4>();
                        for (int j = 0; j <= 10; j++)
                        {
                            float t = j / 10f * Mathf.PI * 1.6f + k * 1.4f;
                            arc.Add(new Vector4(e.x + Mathf.Cos(t) * 0.15f, e.y + Mathf.Sin(t) * 0.12f, e.z - 0.05f + k * 0.1f, 0.012f));
                        }
                        sdf.Tube(arc.ToArray(), k == 0 ? 2 : 3, smooth: 0.008f);
                    }
                    seed += 2;
                }
                result["alveolus"] = Micro("Alveolus", sdf, new Bounds(new Vector3(0f, 0.08f, 0f), new Vector3(0.95f, 1.05f, 0.5f)), 0.009f, new[]
                {
                    ("Bronkiolus", new Color32(0xC9, 0x4E, 0x6E, 0xFF), 0.55f),
                    ("Alveolus", new Color32(0xF2, 0x9B, 0xAE, 0xFF), 0.45f),
                    ("KapilerMerah", new Color32(0xE0, 0x3A, 0x45, 0xFF), 0.6f),
                    ("KapilerBiru", new Color32(0x3E, 0x5F, 0xD6, 0xFF), 0.6f),
                }, new[]
                {
                    ("Bronkiolus", "Cabang udara terkecil", new Vector3(0f, 0.4f, -0.06f)),
                    ("Alveolus", "Kantong udara tempat tukar oksigen", new Vector3(-0.26f, -0.1f, -0.1f)),
                    ("Kapiler", "Pembuluh darah super kecil", new Vector3(0.38f, -0.1f, -0.05f)),
                });
            }

            // VILI: dinding usus dengan jari-jari kecil, berisi pembuluh darah
            {
                var sdf = new CompositeSdf(0.04f);
                sdf.Box(new Vector3(0f, -0.36f, 0f), new Vector3(0.48f, 0.08f, 0.26f), 0.05f, 0);
                for (int x = 0; x < 5; x++)
                    for (int z = 0; z < 3; z++)
                    {
                        float px = -0.36f + x * 0.18f + (z % 2) * 0.06f, pz = -0.16f + z * 0.16f;
                        float h = 0.22f + 0.06f * Mathf.Sin(x * 1.7f + z);
                        sdf.Tube(new[] { new Vector4(px, -0.3f, pz, 0.065f), new Vector4(px, -0.3f + h, pz, 0.058f), new Vector4(px, -0.3f + h + 0.04f, pz, 0.05f) }, 1, smooth: 0.04f);
                        if (z == 0)
                        {
                            sdf.Tube(new[] { new Vector4(px - 0.025f, -0.3f, pz - 0.055f, 0.01f), new Vector4(px - 0.02f, -0.3f + h, pz - 0.05f, 0.01f) }, 2, smooth: 0.004f);
                            sdf.Tube(new[] { new Vector4(px + 0.025f, -0.3f, pz - 0.055f, 0.01f), new Vector4(px + 0.02f, -0.3f + h, pz - 0.05f, 0.01f) }, 3, smooth: 0.004f);
                        }
                    }
                result["vili"] = Micro("Vili", sdf, new Bounds(new Vector3(0f, -0.1f, 0f), new Vector3(1.1f, 0.75f, 0.62f)), 0.012f, new[]
                {
                    ("Dinding", new Color32(0xE8, 0x7E, 0x7A, 0xFF), 0.5f),
                    ("Vili", new Color32(0xF4, 0xA3, 0x9B, 0xFF), 0.45f),
                    ("KapilerMerah", new Color32(0xE0, 0x3A, 0x45, 0xFF), 0.6f),
                    ("KapilerBiru", new Color32(0x3E, 0x5F, 0xD6, 0xFF), 0.6f),
                }, new[]
                {
                    ("Vili", "Menyerap sari makanan", new Vector3(0f, 0.0f, -0.18f)),
                    ("Kapiler", "Membawa sari makanan ke darah", new Vector3(-0.38f, -0.15f, -0.24f)),
                    ("Dinding usus", "Tempat vili tumbuh", new Vector3(0.36f, -0.36f, -0.26f)),
                });
            }

            // NEFRON: glomerulus (gumpalan pembuluh), kapsul, saluran berkelok & lengkung Henle
            {
                var sdf = new CompositeSdf(0.02f);
                for (int i = 0; i < 14; i++)
                {
                    float a = i * 2.39996f, h = (i / 13f) * 2f - 1f;
                    var off = new Vector3(Mathf.Cos(a) * Mathf.Sqrt(1 - h * h), h, Mathf.Sin(a) * Mathf.Sqrt(1 - h * h)) * 0.06f;
                    sdf.Sphere(new Vector3(-0.3f, 0.32f, 0f) + off, 0.04f, 0);
                }
                sdf.Tube(new[] { new Vector4(-0.55f, 0.5f, 0f, 0.025f), new Vector4(-0.36f, 0.38f, 0f, 0.022f) }, 0, smooth: 0.01f);
                sdf.Sphere(new Vector3(-0.3f, 0.32f, 0.03f), 0.15f, 1, scale: new Vector3(1f, 1f, 0.5f));
                sdf.Subtract(p => (p - new Vector3(-0.3f, 0.32f, -0.05f)).magnitude - 0.13f, 0.02f);
                var tub = new List<Vector4>();
                for (int k = 0; k <= 16; k++)
                {
                    float t = k / 16f;
                    tub.Add(new Vector4(-0.16f + t * 0.24f, 0.3f + 0.08f * Mathf.Sin(t * Mathf.PI * 4f), 0.04f * Mathf.Cos(t * Mathf.PI * 3f), 0.035f));
                }
                tub.Add(new Vector4(0.1f, 0.1f, 0f, 0.033f));
                tub.Add(new Vector4(0.1f, -0.4f, 0f, 0.03f));
                sdf.Tube(tub.ToArray(), 2, smooth: 0.02f);
                sdf.Tube(new[] { new Vector4(0.1f, -0.4f, 0f, 0.03f), new Vector4(0.17f, -0.5f, 0f, 0.03f), new Vector4(0.24f, -0.4f, 0f, 0.03f), new Vector4(0.24f, 0.15f, 0f, 0.032f) }, 3, smooth: 0.02f);
                var dist = new List<Vector4>();
                for (int k = 0; k <= 10; k++)
                {
                    float t = k / 10f;
                    dist.Add(new Vector4(0.24f + t * 0.16f, 0.15f + 0.07f * Mathf.Sin(t * Mathf.PI * 3f), 0.03f * Mathf.Sin(t * Mathf.PI * 2f), 0.032f));
                }
                sdf.Tube(dist.ToArray(), 3, smooth: 0.02f);
                sdf.Tube(new[] { new Vector4(0.4f, 0.15f, 0f, 0.04f), new Vector4(0.45f, 0.4f, 0f, 0.045f), new Vector4(0.45f, -0.6f, 0f, 0.05f) }, 4, smooth: 0.02f);
                result["nefron"] = Micro("Nefron", sdf, new Bounds(new Vector3(-0.04f, -0.04f, 0f), new Vector3(1.15f, 1.25f, 0.4f)), 0.008f, new[]
                {
                    ("Glomerulus", new Color32(0xD9, 0x3A, 0x4B, 0xFF), 0.6f),
                    ("Kapsul", new Color32(0xF2, 0xC4, 0x6B, 0xFF), 0.5f),
                    ("SaluranAwal", new Color32(0xF7, 0xD2, 0x7A, 0xFF), 0.5f),
                    ("LengkungHenle", new Color32(0xF2, 0xA8, 0x4B, 0xFF), 0.5f),
                    ("SaluranPengumpul", new Color32(0xE0, 0x86, 0x3A, 0xFF), 0.5f),
                }, new[]
                {
                    ("Glomerulus", "Gumpalan pembuluh penyaring", new Vector3(-0.3f, 0.32f, -0.12f)),
                    ("Saluran berkelok", "Menyerap kembali yang berguna", new Vector3(0f, 0.3f, -0.06f)),
                    ("Saluran pengumpul", "Mengalirkan urine", new Vector3(0.45f, -0.3f, -0.06f)),
                });
            }

            // NEURON: badan sel, dendrit bercabang, akson berselubung, ujung akson
            {
                var sdf = new CompositeSdf(0.03f);
                sdf.Sphere(new Vector3(-0.3f, 0.1f, 0f), 0.13f, 0);
                for (int i = 0; i < 6; i++)
                {
                    float a = 1.2f + i * 0.75f;
                    var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0.2f * Mathf.Sin(i * 2f));
                    var p1 = new Vector3(-0.3f, 0.1f, 0f) + dir * 0.28f;
                    sdf.Tube(new[] { new Vector4(-0.3f, 0.1f, 0f, 0.04f), new Vector4(p1.x, p1.y, p1.z, 0.018f) }, 0, smooth: 0.03f);
                    foreach (var sgn in new[] { -1f, 1f })
                    {
                        var d2 = Quaternion.Euler(0, 0, sgn * 30f) * dir;
                        var p2 = p1 + d2 * 0.14f;
                        sdf.Tube(new[] { new Vector4(p1.x, p1.y, p1.z, 0.016f), new Vector4(p2.x, p2.y, p2.z, 0.01f) }, 0, smooth: 0.01f);
                    }
                }
                sdf.Tube(new[] { new Vector4(-0.2f, 0.04f, 0f, 0.03f), new Vector4(0.2f, -0.12f, 0f, 0.024f), new Vector4(0.55f, -0.3f, 0f, 0.022f) }, 0, smooth: 0.03f);
                for (int i = 0; i < 5; i++)
                {
                    float t = 0.15f + i * 0.17f;
                    var p = Vector3.Lerp(new Vector3(-0.15f, 0.02f, 0f), new Vector3(0.5f, -0.28f, 0f), t);
                    var q = Vector3.Lerp(new Vector3(-0.15f, 0.02f, 0f), new Vector3(0.5f, -0.28f, 0f), t + 0.12f);
                    sdf.Tube(new[] { new Vector4(p.x, p.y, p.z, 0.045f), new Vector4(q.x, q.y, q.z, 0.045f) }, 1, smooth: 0.005f);
                }
                foreach (var a in new[] { -40f, 0f, 40f })
                {
                    var d = Quaternion.Euler(0, 0, a) * new Vector3(0.9f, -0.45f, 0f).normalized;
                    var e = new Vector3(0.55f, -0.3f, 0f) + d * 0.14f;
                    sdf.Tube(new[] { new Vector4(0.55f, -0.3f, 0f, 0.016f), new Vector4(e.x, e.y, e.z, 0.012f) }, 0, smooth: 0.01f);
                    sdf.Sphere(e, 0.03f, 2);
                }
                result["neuron"] = Micro("Neuron", sdf, new Bounds(new Vector3(0.06f, 0.06f, 0f), new Vector3(1.35f, 1.0f, 0.4f)), 0.008f, new[]
                {
                    ("Sel", new Color32(0xA8, 0x8B, 0xF0, 0xFF), 0.5f),
                    ("Selubung", new Color32(0xF2, 0xC4, 0x6B, 0xFF), 0.5f),
                    ("Ujung", new Color32(0xF2, 0x9B, 0xAE, 0xFF), 0.5f),
                }, new[]
                {
                    ("Badan sel", "Pusat sel saraf", new Vector3(-0.3f, 0.1f, -0.13f)),
                    ("Dendrit", "Menerima pesan", new Vector3(-0.45f, 0.38f, -0.03f)),
                    ("Akson", "Mengirim pesan listrik", new Vector3(0.18f, -0.12f, -0.05f)),
                    ("Ujung akson", "Meneruskan pesan ke sel lain", new Vector3(0.68f, -0.36f, -0.03f)),
                });
            }
            // SEL DARAH: potongan pembuluh darah (setengah terbuka) berisi sel darah merah, sel darah putih & keping darah
            {
                var sdf = new CompositeSdf(0.02f);
                // bagian dalam pembuluh "milik" tabung (jarak negatif), jadi warna sel di dalamnya ditentukan di sini
                var wbc = new Vector3(-0.3f, 0.1f, 0.06f);
                var platelets = new[] { new Vector3(0.12f, -0.15f, 0.1f), new Vector3(0.15f, 0.14f, 0.12f), new Vector3(-0.52f, -0.12f, 0.08f) };
                Func vesselMat = p =>
                {
                    if (new Vector2(p.y, p.z).magnitude > 0.228f) return 0;
                    if ((p - wbc).magnitude < 0.11f) return 2;
                    foreach (var k in platelets) if ((p - k).magnitude < 0.05f) return 3;
                    return 1;
                };
                sdf.Tube(new[] { new Vector4(-0.62f, 0f, 0f, 0.27f), new Vector4(0.62f, 0f, 0f, 0.27f) }, 0, smooth: 0.01f, materialFn: vesselMat);
                sdf.Subtract(p => new Vector2(p.y, p.z).magnitude - 0.235f, 0.01f);
                sdf.Subtract(p => p.z + 0.02f, 0.01f);
                sdf.Subtract(p => 0.6f - Mathf.Abs(p.x), 0.01f);
                // sel darah merah: cakram pipih dengan lekukan di tengah (menghadap ke berbagai arah)
                var cells = new (Vector3 c, int axis)[]
                {
                    (new Vector3(-0.42f, 0.03f, 0.1f), 2), (new Vector3(-0.2f, -0.1f, 0.12f), 1), (new Vector3(0.02f, 0.08f, 0.1f), 2),
                    (new Vector3(0.24f, -0.06f, 0.11f), 0), (new Vector3(0.45f, 0.06f, 0.1f), 2), (new Vector3(-0.08f, -0.13f, 0.02f), 2),
                    (new Vector3(0.36f, -0.13f, 0.03f), 1),
                };
                foreach (var (c, axis) in cells)
                {
                    var flat = axis == 0 ? new Vector3(0.36f, 1f, 1f) : axis == 1 ? new Vector3(1f, 0.36f, 1f) : new Vector3(1f, 1f, 0.36f);
                    sdf.Sphere(c, 0.1f, 1, scale: flat);
                    var n = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
                    var cc = c;
                    sdf.Subtract(p => Mathf.Min((p - (cc + n * 0.062f)).magnitude, (p - (cc - n * 0.062f)).magnitude) - 0.05f, 0.02f);
                }
                // sel darah putih (bulat, permukaan berbintil)
                sdf.Sphere(wbc, 0.085f, 2, disp: p => 0.008f * Mathf.Sin(p.x * 90f) * Mathf.Sin(p.y * 85f) * Mathf.Sin(p.z * 95f), dispMax: 0.008f);
                // keping darah (kecil-kecil)
                foreach (var k in platelets)
                    sdf.Sphere(k, 0.028f, 3, scale: new Vector3(1.3f, 0.7f, 1f));
                result["seldarah"] = Micro("SelDarah", sdf, new Bounds(new Vector3(0f, 0f, 0.13f), new Vector3(1.3f, 0.62f, 0.36f)), 0.011f, new[]
                {
                    ("Pembuluh", new Color32(0xF2, 0xA7, 0xA7, 0xFF), 0.45f),
                    ("SelDarahMerah", new Color32(0xD9, 0x2B, 0x3A, 0xFF), 0.6f),
                    ("SelDarahPutih", new Color32(0xF1, 0xEE, 0xF8, 0xFF), 0.4f),
                    ("KepingDarah", new Color32(0xB9, 0x9C, 0xE8, 0xFF), 0.4f),
                }, new[]
                {
                    ("Sel darah merah", "Membawa oksigen", new Vector3(0.02f, 0.08f, 0.06f)),
                    ("Sel darah putih", "Melawan kuman", new Vector3(-0.3f, 0.1f, -0.03f)),
                    ("Keping darah", "Menutup luka", new Vector3(0.15f, 0.14f, 0.09f)),
                    ("Pembuluh darah", "Jalan darah mengalir", new Vector3(0.55f, 0.2f, -0.02f)),
                });
            }

            return result;
        }

        static GameObject Micro(string name, CompositeSdf sdf, Bounds bounds, float cell, (string, Color32, float)[] colors, (string, string, Vector3)[] hotspots)
        {
            var (prefab, _) = OrganModelBuilder.BuildPrefab(name, sdf, bounds, cell, colors, hotspots, MicroDir);
            return prefab;
        }

        // ================================================================ anak 3D

        public static void BuildKids()
        {
            StomachModelBuilder.EnsureFolder(KidDir);
            var db = AssetDatabase.LoadAssetAtPath<OrganDatabase>("Assets/Organo/Data/OrganDatabase.asset");
            var (boy, boyFade) = BuildKid("AnakLaki", false);
            var (girl, girlFade) = BuildKid("AnakPerempuan", true);
            if (db != null)
            {
                db.bodyBoyPrefab = boy; db.bodyGirlPrefab = girl;
                db.bodyBoyFaded = boyFade; db.bodyGirlFaded = girlFade;
                if (db.ghostMaterial == null)
                    db.ghostMaterial = BodyModelBuilder.Transparent(KidDir + "/Organ_Pudar.mat", new Color(0.86f, 0.84f, 0.92f, 0.28f), 0.3f);
                EditorUtility.SetDirty(db);
            }
        }

        static (GameObject, Material[]) BuildKid(string name, bool girl)
        {
            var sdf = new CompositeSdf(0.035f);
            // 0 kulit, 1 rambut, 2 baju, 3 celana/rok, 4 sepatu, 5 putih mata, 6 gelap (pupil, mulut), 7 pipi/pita
            if (girl)
            {
                // rok mengembang; bagian bawahnya dipotong rata (potongan hanya mengenai bentuk sebelum ini).
                // Pinggang rok di pinggul (±0.49), bukan di perut, supaya usus di mode "Tubuh" tampak di perut, bukan di dalam rok.
                sdf.Sphere(new Vector3(0f, 0.37f, 0f), 0.22f, 3, scale: new Vector3(1f, 0.8f, 0.64f));
                sdf.Subtract(p => p.y - 0.265f, 0.01f);
            }
            var head = new Vector3(0f, 1.12f, 0f);
            // senyum dilukis langsung di kulit wajah (lebih rapi daripada tabung tipis)
            Func face = p =>
            {
                if (p.z > -0.12f || Mathf.Abs(p.x) > 0.042f) return 0;
                float smile = 1.052f + 5.5f * p.x * p.x;
                return Mathf.Abs(p.y - smile) < 0.0065f ? 6 : 0;
            };
            sdf.Sphere(head, 0.165f, 0, scale: new Vector3(1f, 1.05f, 0.97f), materialFn: face);
            foreach (var s in new[] { -1f, 1f })
                sdf.Sphere(new Vector3(s * 0.163f, 1.11f, 0.01f), 0.035f, 0, scale: new Vector3(0.6f, 1f, 0.9f));
            sdf.Sphere(new Vector3(0f, 1.1f, -0.158f), 0.02f, 0);
            sdf.Tube(new[] { new Vector4(0f, 0.92f, 0f, 0.055f), new Vector4(0f, 1.0f, 0f, 0.055f) }, 0);
            // baju
            sdf.Sphere(new Vector3(0f, 0.75f, 0f), 0.2f, 2, scale: new Vector3(1f, 1.2f, 0.62f));
            sdf.Sphere(new Vector3(0f, 0.55f, 0f), 0.18f, 2, scale: new Vector3(1f, 0.95f, 0.63f));
            sdf.Tube(new[] { new Vector4(-0.06f, 0.94f, -0.01f, 0.03f), new Vector4(0f, 0.92f, -0.05f, 0.03f), new Vector4(0.06f, 0.94f, -0.01f, 0.03f) }, 2, smooth: 0.02f);
            foreach (var s in new[] { -1f, 1f })
            {
                sdf.Tube(new[] { new Vector4(s * 0.16f, 0.88f, 0f, 0.065f), new Vector4(s * 0.24f, 0.76f, 0f, 0.058f) }, 2);
                sdf.Tube(new[] { new Vector4(s * 0.25f, 0.74f, 0f, 0.046f), new Vector4(s * 0.31f, 0.56f, -0.01f, 0.042f), new Vector4(s * 0.33f, 0.44f, -0.02f, 0.04f) }, 0);
                sdf.Sphere(new Vector3(s * 0.335f, 0.4f, -0.02f), 0.05f, 0, scale: new Vector3(0.8f, 1f, 0.8f));
                sdf.Tube(new[] { new Vector4(s * 0.085f, 0.36f, 0f, 0.06f), new Vector4(s * 0.09f, 0.18f, 0f, 0.055f), new Vector4(s * 0.09f, 0.06f, 0f, 0.05f) }, 0);
                sdf.Sphere(new Vector3(s * 0.09f, 0.03f, -0.03f), 0.06f, 4, scale: new Vector3(1f, 0.65f, 1.55f));
            }
            // celana pendek / rok
            if (!girl)
            {
                // pinggang celana di pinggul (atas ±0.49, dulu ±0.54 = setinggi perut)
                sdf.Box(new Vector3(0f, 0.365f, 0f), new Vector3(0.175f, 0.065f, 0.115f), 0.06f, 3);
                foreach (var s in new[] { -1f, 1f })
                    sdf.Tube(new[] { new Vector4(s * 0.085f, 0.355f, 0f, 0.078f), new Vector4(s * 0.09f, 0.25f, 0f, 0.074f) }, 3);
            }
            // wajah
            foreach (var s in new[] { -1f, 1f })
            {
                // mata putih dengan pupil dilukis di depannya
                float ex = s * 0.057f;
                sdf.Sphere(new Vector3(ex, 1.135f, -0.135f), 0.03f, 5, scale: new Vector3(1f, 1.15f, 0.7f),
                    materialFn: p => p.z < -0.148f && new Vector2(p.x - ex - s * 0.004f, (p.y - 1.13f) / 1.15f).magnitude < 0.0175f ? 6 : 5);
                sdf.Sphere(new Vector3(s * 0.095f, 1.075f, -0.128f), 0.03f, 7, scale: new Vector3(1f, 0.7f, 0.4f));
            }
            // rambut
            sdf.Sphere(new Vector3(0f, 1.18f, 0.025f), 0.175f, 1, scale: new Vector3(1.04f, 0.78f, 1.02f));
            for (int i = 0; i < 4; i++)
                sdf.Sphere(new Vector3(-0.09f + i * 0.06f, 1.24f, -0.12f), 0.05f, 1, scale: new Vector3(1f, 0.7f, 0.8f));
            if (girl)
            {
                foreach (var s in new[] { -1f, 1f })
                    sdf.Tube(new[] { new Vector4(s * 0.15f, 1.15f, 0.03f, 0.065f), new Vector4(s * 0.17f, 1.02f, 0.05f, 0.06f), new Vector4(s * 0.16f, 0.92f, 0.06f, 0.05f) }, 1);
                sdf.Sphere(new Vector3(0.1f, 1.29f, -0.02f), 0.045f, 7, scale: new Vector3(1.5f, 0.85f, 0.8f));
            }
            var colors = new[]
            {
                ("Kulit", new Color32(0xF2, 0xC4, 0xA0, 0xFF), 0.35f),
                ("Rambut", new Color32(0x5A, 0x46, 0x36, 0xFF), 0.45f),
                ("Baju", girl ? new Color32(0xF7, 0xA8, 0xC0, 0xFF) : new Color32(0x6C, 0xCF, 0xC2, 0xFF), 0.3f),
                ("Bawahan", girl ? new Color32(0xA9, 0x8B, 0xEB, 0xFF) : new Color32(0xFF, 0xEB, 0xC2, 0xFF), 0.3f),
                ("Sepatu", new Color32(0x4F, 0x5D, 0x9C, 0xFF), 0.4f),
                ("Mata", new Color32(0xFF, 0xFF, 0xFF, 0xFF), 0.7f),
                ("Gelap", new Color32(0x2E, 0x2A, 0x4A, 0xFF), 0.8f),
                ("Pipi", new Color32(0xFF, 0x8A, 0x7A, 0xFF), 0.4f),
            };
            var (prefab, _) = OrganModelBuilder.BuildPrefab(name, sdf, new Bounds(new Vector3(0f, 0.68f, 0f), new Vector3(0.82f, 1.42f, 0.5f)), 0.0095f, colors, null, KidDir);
            // versi pudar (transparan) untuk melihat organ di dalam tubuh
            var faded = new Material[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                Color c = colors[i].Item2;
                c.a = i <= 1 ? 0.18f : i <= 4 ? 0.14f : 0.35f;
                faded[i] = BodyModelBuilder.Transparent(KidDir + "/" + name + "_Pudar_" + colors[i].Item1 + ".mat", c, 0.3f);
            }
            return (prefab, faded);
        }

        /// <summary>Pratinjau semua model + gambar bagian (jalankan di sesi terpisah setelah model dibuat).</summary>
        public static void RenderEverything()
        {
            OrganModelBuilder.RenderPreviews();
            foreach (var path in new[] { MicroDir + "/Alveolus.prefab", MicroDir + "/Vili.prefab", MicroDir + "/Nefron.prefab", MicroDir + "/Neuron.prefab", MicroDir + "/SelDarah.prefab",
                                         MicroDir + "/Sperma.prefab", MicroDir + "/SelTelur.prefab",
                                         KidDir + "/AnakLaki.prefab", KidDir + "/AnakPerempuan.prefab" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) StomachModelBuilder.RenderPreview(prefab, System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant());
            }
            RenderPartImages();
        }

        // ================================================================ gambar bagian organ

        [MenuItem("Organo/Model 3D/Render Gambar Bagian Organ", priority = 43)]
        public static void RenderPartImages()
        {
            StomachModelBuilder.EnsureFolder(ImgDir);
            var db = AssetDatabase.LoadAssetAtPath<OrganDatabase>("Assets/Organo/Data/OrganDatabase.asset");
            if (db == null) return;
            var micro = new Dictionary<string, GameObject>
            {
                ["alveolus"] = AssetDatabase.LoadAssetAtPath<GameObject>(MicroDir + "/Alveolus.prefab"),
                ["vili"] = AssetDatabase.LoadAssetAtPath<GameObject>(MicroDir + "/Vili.prefab"),
                ["nefron"] = AssetDatabase.LoadAssetAtPath<GameObject>(MicroDir + "/Nefron.prefab"),
                ["neuron"] = AssetDatabase.LoadAssetAtPath<GameObject>(MicroDir + "/Neuron.prefab"),
                ["seldarah"] = AssetDatabase.LoadAssetAtPath<GameObject>(MicroDir + "/SelDarah.prefab"),
                ["sperma"] = AssetDatabase.LoadAssetAtPath<GameObject>(MicroDir + "/Sperma.prefab"),
                ["seltelur"] = AssetDatabase.LoadAssetAtPath<GameObject>(MicroDir + "/SelTelur.prefab"),
            };
            var written = new List<(OrganInfo organ, string partId, string path, GameObject microPrefab)>();
            var heroes = new List<(OrganInfo, string)>();
            foreach (var organ in db.organs)
            {
                if (organ == null || organ.modelPrefab == null) continue;
                var content = OrganContent.Get(organ.id);
                string hero = ImgDir + "/" + organ.id + "_utuh.png";
                Render(organ.modelPrefab, null, organ.modelEuler, organ.tint, db.ghostMaterial, hero, 512);
                heroes.Add((organ, hero));
                if (content == null) continue;
                foreach (var part in content.parts)
                {
                    string path = ImgDir + "/" + organ.id + "_" + part.id + ".png";
                    GameObject mp = null;
                    if (!string.IsNullOrEmpty(part.micro) && micro.TryGetValue(part.micro, out mp) && mp != null)
                        Render(mp, null, new Vector3(6f, -14f, 0f), organ.tint, db.ghostMaterial, path, 320);
                    else if ((part.materials == null || part.materials.Length == 0) && part.inside != null && part.inside.Length > 0 && organ.insidePrefab != null)
                        Render(organ.insidePrefab, part.inside, organ.insideEuler, organ.tint, db.ghostMaterial, path, 320);
                    else
                        Render(organ.modelPrefab, part.materials, organ.modelEuler, organ.tint, db.ghostMaterial, path, 320);
                    written.Add((organ, part.id, path, mp));
                }
            }
            AssetDatabase.Refresh();
            foreach (var (organ, path) in heroes)
            {
                Configure(path);
                organ.heroImage = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                organ.partVisuals = new List<OrganPartVisual>();
                EditorUtility.SetDirty(organ);
            }
            foreach (var (organ, partId, path, mp) in written)
            {
                Configure(path);
                organ.partVisuals.Add(new OrganPartVisual { id = partId, image = AssetDatabase.LoadAssetAtPath<Texture2D>(path), microPrefab = mp });
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Organo] Gambar bagian organ: " + written.Count + " gambar + " + heroes.Count + " gambar organ utuh di " + ImgDir);
        }

        static void Configure(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            imp.textureType = TextureImporterType.Default;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.maxTextureSize = 512;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
        }

        /// <summary>Render prefab; kalau <paramref name="highlight"/> diisi, submesh lain diganti material pudar.</summary>
        static void Render(GameObject prefab, int[] highlight, Vector3 euler, Color bg, Material ghost, string path, int size)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                inst.transform.rotation = Quaternion.Euler(euler);
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                    if (r.gameObject.name == "Inner") r.enabled = highlight != null;
                if (highlight != null && ghost != null)
                    foreach (var r in inst.GetComponentsInChildren<Renderer>())
                    {
                        var mats = r.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++)
                            if (System.Array.IndexOf(highlight, i) < 0 && mats[i] != null)
                            {
                                // pudar dengan warna aslinya (sama seperti di aplikasi)
                                var f = new Material(ghost);
                                Color c = mats[i].HasProperty("_BaseColor") ? mats[i].GetColor("_BaseColor") : mats[i].color;
                                c = Color.Lerp(c, Color.white, 0.35f); c.a = 0.22f;
                                if (f.HasProperty("_BaseColor")) f.SetColor("_BaseColor", c); else f.color = c;
                                mats[i] = f;
                            }
                        r.sharedMaterials = mats;
                    }
                var b = new Bounds(); bool init = false;
                foreach (var r in inst.GetComponentsInChildren<Renderer>())
                { if (!init) { b = r.bounds; init = true; } else b.Encapsulate(r.bounds); }
                // dekati bagian yang disorot, organ lain tetap terlihat sebagai konteks
                if (highlight != null)
                {
                    Bounds pb = default; bool pinit = false;
                    foreach (var r in inst.GetComponentsInChildren<MeshRenderer>())
                    {
                        var mesh = r.GetComponent<MeshFilter>()?.sharedMesh;
                        if (mesh == null) continue;
                        foreach (int m in highlight)
                        {
                            if (m < 0 || m >= mesh.subMeshCount) continue;
                            var sb = Organo.ThreeD.OrganViewer3D.SubMeshBounds(mesh, m);
                            if (sb.size == Vector3.zero) continue;
                            for (int c = 0; c < 8; c++)
                            {
                                var wp = r.transform.TransformPoint(sb.center + Vector3.Scale(sb.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1)));
                                if (!pinit) { pb = new Bounds(wp, Vector3.zero); pinit = true; } else pb.Encapsulate(wp);
                            }
                        }
                    }
                    if (pinit)
                    {
                        float full = b.extents.magnitude, part = pb.extents.magnitude;
                        // bagian kecil (testis, prostat, ovarium, katup) didekati lebih jauh supaya tetap jelas di kartu kecil
                        float rad = Mathf.Clamp(part * 1.6f, full * 0.3f, full);
                        float t = Mathf.InverseLerp(full, full * 0.3f, rad);
                        b = new Bounds(Vector3.Lerp(b.center, pb.center, 0.25f + 0.65f * t), Vector3.one * (rad / Mathf.Sqrt(3f)) * 2f);
                    }
                }
                var lightGo = new GameObject("Light", typeof(Light));
                EditorSceneManager.MoveGameObjectToScene(lightGo, scene);
                var light = lightGo.GetComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.25f;
                lightGo.transform.rotation = Quaternion.Euler(30f, -25f, 0f);
                var fill = new GameObject("Fill", typeof(Light));
                EditorSceneManager.MoveGameObjectToScene(fill, scene);
                fill.GetComponent<Light>().type = LightType.Directional;
                fill.GetComponent<Light>().intensity = 0.45f;
                fill.transform.rotation = Quaternion.Euler(-20f, 150f, 0f);
                var camGo = new GameObject("Cam", typeof(Camera));
                EditorSceneManager.MoveGameObjectToScene(camGo, scene);
                var cam = camGo.GetComponent<Camera>();
                cam.scene = scene;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(bg.r, bg.g, bg.b, 0f); // latar transparan (warna tetap dipakai sebagai tepi halus)
                cam.fieldOfView = 30f;
                float radius = b.extents.magnitude;
                camGo.transform.position = b.center + new Vector3(0f, 0f, -radius / Mathf.Sin(15f * Mathf.Deg2Rad) * 0.95f);
                camGo.transform.LookAt(b.center);
                cam.nearClipPlane = 0.01f; cam.farClipPlane = radius * 20f;
                var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cam.targetTexture = rt;
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                cam.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
