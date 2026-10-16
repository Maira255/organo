using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Organo.Data;
using Organo.Screens;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace Organo.EditorTools
{
    /// <summary>
    /// Uji kelas online tanpa internet: baris "Kelas" di Profil, lembar Gabung Kelas, dan isi ringkasan yang dikirim ke guru.
    /// Ringkasan contoh disimpan di Library/OrganoPreview/kelas_kiriman.json (bisa dikirim manual ke server untuk dicoba).
    /// </summary>
    public static partial class OrganoSnapshots
    {
        static bool AdaTeks(VisualElement root, string teks) => root.Query<Label>().Where(l => l.text == teks).ToList().Count > 0;

        static void TestKelasProfil()
        {
            var screen = App.Current as ProfileScreen;
            if (!KelasOnline.Tersedia)
            {
                // aplikasi offline (bawaan): tidak ada menu Kelas, tidak ada kiriman ke server
                Report("aplikasi offline: Profil tanpa menu Kelas", screen != null && !AdaTeks(screen.Root, "Kelas") && !AdaTeks(screen.Root, "Belum bergabung"));
                KelasOnline.KirimSekarang(true);
                Report("aplikasi offline: tidak mengirim apa pun", KelasOnline.Status == KelasOnline.Keadaan.BelumGabung && !KelasOnline.Tergabung(App.Progress));
                return;
            }
            Report("profil punya baris Kelas (belum bergabung)", screen != null && AdaTeks(screen.Root, "Kelas") && AdaTeks(screen.Root, "Belum bergabung"));
            screen?.ShowKelas();
        }

        static void TestKelasLembar()
        {
            if (!KelasOnline.Tersedia) return;
            Report("lembar Gabung Kelas: kolom kode + tombol Cek Kode",
                AdaTeks(UiRoot, "Gabung Kelas") && AdaTeks(UiRoot, "Cek Kode") && AdaTeks(UiRoot, "Kode kelas"));
            Report("kode kelas dirapikan (huruf besar, tanpa spasi/strip)", KelasOnline.BersihkanKode(" k7m-2qx ") == "K7M2QX",
                KelasOnline.BersihkanKode(" k7m-2qx "));

            // ringkasan dari salinan progres (progres asli tidak diubah supaya tidak ada kiriman sungguhan)
            var p = JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(App.Progress));
            p.kelasKode = "UJI234"; p.kelasNama = "Kelas Uji"; p.kelasGuru = "Bu Guru"; p.kelasNamaMurid = "Murid Uji";
            p.muridId = "0123456789abcdef0123456789abcdef";
            p.history.Insert(0, new QuizRecord
            {
                quizId = "organ:lambung", title = "Lambung", system = "Pencernaan", correct = 4, total = 5, stars = 2,
                waktu = "2026-10-16T09:30:00.0000000+07:00",
                answers =
                {
                    new QuizAnswer { type = "pilihan", question = "Apa fungsi lambung?", yours = "Menyaring darah", right = "Mencerna makanan dengan asam lambung", isCorrect = false },
                    new QuizAnswer { type = "pilihan", question = "Lambung ada di sistem apa?", yours = "Pencernaan", right = "Pencernaan", isCorrect = true },
                },
            });
            var json = KelasOnline.Json(p);
            Report("ringkasan untuk guru berisi kode, murid, kuis, kartu, bintang",
                json.Contains("\"kode\":\"UJI234\"") && json.Contains("\"nama\":\"Murid Uji\"") && json.Contains("\"kuisOrgan\":[")
                && json.Contains("\"kuisProses\":[") && json.Contains("\"kartu\":[") && json.Contains("\"bintang\":")
                && json.Contains("\"waktu\":\"2026-10-16T09:30") && json.Contains("Menyaring darah") && json.Contains("\"varian\":\"" + KelasOnline.Varian + "\""),
                json.Length + " karakter");
            Directory.CreateDirectory(OutDir);
            File.WriteAllText(Path.Combine(OutDir, "kelas_kiriman.json"), json);
            Report("progres asli tidak ikut bergabung kelas", !KelasOnline.Tergabung(App.Progress) && KelasOnline.Status == KelasOnline.Keadaan.BelumGabung);
        }

        // Kode berisi I, O, 0, 1 (tidak pernah dipakai kode kelas): server pasti menjawab 404, tidak ada data yang tersimpan.
        const string KodeMustahil = "UJI0O1";

        /// <summary>Bergabung ke kelas yang tidak ada: kiriman sungguhan ke server dijawab 404 → status "kelas sudah tidak ada".</summary>
        static void MulaiKelasMustahil()
        {
            if (!KelasOnline.Tersedia) return;
            var p = App.Progress;
            p.kelasKode = KodeMustahil; p.kelasNama = "Kelas Uji"; p.kelasGuru = "Bu Guru"; p.kelasNamaMurid = "Murid Uji";
            p.muridId = "0123456789abcdef0123456789abcdef";
            KelasOnline.KirimSekarang();
            (App.Current as ProfileScreen)?.ShowKelas();
            s_WaitUntil = () => KelasOnline.Status != KelasOnline.Keadaan.Mengirim;
        }

        static void TestKelasBergabung()
        {
            if (!KelasOnline.Tersedia) return;
            Report("lembar kelas saat bergabung: nama kelas, nama murid, Keluar dari kelas",
                AdaTeks(UiRoot, "Kelas Uji") && AdaTeks(UiRoot, "Namamu di kelas: Murid Uji") && AdaTeks(UiRoot, "Keluar dari kelas"));
            if (Application.internetReachability == NetworkReachability.NotReachable)
                Debug.Log("[Organo] uji kiriman kelas dilewati: tidak ada internet");
            else
                Report("kiriman ke server: kelas yang tidak ada dijawab 'kelas sudah tidak ada'",
                    KelasOnline.Status == KelasOnline.Keadaan.KelasHilang && AdaTeks(UiRoot, KelasOnline.StatusTeks(App.Progress))
                    && !AdaTeks(UiRoot, "Kirim sekarang"),
                    KelasOnline.Status.ToString());
            KelasOnline.Keluar(App.Progress);
            Report("keluar dari kelas: aplikasi kembali offline", !KelasOnline.Tergabung(App.Progress) && KelasOnline.Status == KelasOnline.Keadaan.BelumGabung
                && App.Progress.muridId == "" && AdaTeks(UiRoot, "Gabung Kelas"));
        }

        // ------------------------------------------------------------ uji ujung-ke-ujung dengan server asli

        [Serializable] class KelasBaru { public string kode, kunci, namaKelas, namaGuru, error; }
        [Serializable] class MuridGuru { public string muridId, nama; public int bintang; public List<string> kartu = new List<string>(); public List<QuizRecord> riwayat = new List<QuizRecord>(); }
        [Serializable] class EvaluasiGuru { public List<MuridGuru> murid = new List<MuridGuru>(); }

        static bool s_UjiServerSelesai;

        /// <summary>
        /// Aplikasi ↔ server sungguhan: buat kelas uji, cek kode & gabung lewat jalur aplikasi, ringkasan terkirim dan
        /// terlihat guru, ringkasan sama tidak dikirim ulang, perubahan progres terkirim sendiri, murid yang dikeluarkan guru
        /// ikut keluar di aplikasi, lalu kelas uji dihapus. Dilewati bila tidak ada internet.
        /// </summary>
        static void MulaiUjiKelasServer()
        {
            if (!KelasOnline.Tersedia) return;
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.Log("[Organo] uji kelas dengan server dilewati: tidak ada internet");
                return;
            }
            s_UjiServerSelesai = false;
            App.StartCoroutine(UjiKelasServer());
            s_WaitUntil = () => s_UjiServerSelesai;
            s_WaitMax = 120;
        }

        static UnityWebRequest PostJson(string path, string json)
        {
            var r = new UnityWebRequest(KelasOnline.Server + path, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 30,
            };
            r.SetRequestHeader("Content-Type", "application/json");
            return r;
        }

        static IEnumerator Tunggu(Func<bool> syarat, float maks)
        {
            float mulai = Time.realtimeSinceStartup;
            while (!syarat() && Time.realtimeSinceStartup - mulai < maks) yield return null;
        }

        static EvaluasiGuru s_Ev;

        static IEnumerator LihatGuru(string kunciJson)
        {
            s_Ev = null;
            using (var r = PostJson("/api/evaluasi", "{" + kunciJson + "}"))
            {
                yield return r.SendWebRequest();
                if (r.result == UnityWebRequest.Result.Success) s_Ev = JsonUtility.FromJson<EvaluasiGuru>(r.downloadHandler.text);
            }
        }

        static IEnumerator UjiKelasServer()
        {
            var p = App.Progress;
            KelasBaru k = null;
            using (var r = PostJson("/api/kelas", "{\"aksi\":\"buat\",\"namaKelas\":\"Uji Otomatis Unity\",\"namaGuru\":\"Editor\"}"))
            {
                yield return r.SendWebRequest();
                if (r.result == UnityWebRequest.Result.Success) k = JsonUtility.FromJson<KelasBaru>(r.downloadHandler.text);
                Report("server: buat kelas uji", k != null && k.kode != null && k.kode.Length == 6, r.responseCode + " " + r.error);
            }
            if (k == null) { s_UjiServerSelesai = true; yield break; }
            string kunciJson = "\"kode\":\"" + k.kode + "\",\"kunci\":\"" + k.kunci + "\"";

            // 1. cek kode lewat jalur aplikasi (huruf kecil + strip)
            KelasOnline.InfoKelas info = null; string salah = "?";
            yield return KelasOnline.Cek(k.kode.Substring(0, 3).ToLowerInvariant() + "-" + k.kode.Substring(3).ToLowerInvariant(), (i, e) => { info = i; salah = e; });
            Report("aplikasi: cek kode kelas dikenali", info != null && salah == null && info.namaKelas == "Uji Otomatis Unity", salah);

            if (info != null)
            {
                // 2. gabung → ringkasan langsung terkirim
                KelasOnline.Gabung(p, info, "Murid Unity");
                yield return Tunggu(() => KelasOnline.Status != KelasOnline.Keadaan.Menunggu && KelasOnline.Status != KelasOnline.Keadaan.Mengirim, 30);
                Report("aplikasi: gabung lalu ringkasan terkirim", KelasOnline.Status == KelasOnline.Keadaan.Tersinkron && p.kelasTerkirim != "",
                    KelasOnline.Status + " " + KelasOnline.Pesan);
                Report("aplikasi: status terkirim tampil untuk murid", KelasOnline.StatusTeks(p).StartsWith("Terkirim ke guru hari ini"), KelasOnline.StatusTeks(p));

                // 3. tidak ada perubahan → tidak dikirim ulang (hemat kuota)
                var terkirim = p.kelasTerkirim;
                KelasOnline.KirimSekarang();
                Report("aplikasi: ringkasan yang sama tidak dikirim ulang", KelasOnline.Status == KelasOnline.Keadaan.Tersinkron && p.kelasTerkirim == terkirim);

                // 4. guru melihat murid
                yield return LihatGuru(kunciJson);
                var m = s_Ev != null ? s_Ev.murid.FirstOrDefault() : null;
                Report("guru: murid terlihat dengan nama, bintang, kartu, riwayat",
                    m != null && s_Ev.murid.Count == 1 && m.nama == "Murid Unity" && m.bintang == p.stars && m.kartu.Count == p.cards.Count
                    && m.riwayat.Count == Mathf.Min(150, p.history.Count),
                    s_Ev == null ? "evaluasi gagal" : s_Ev.murid.Count + " murid, bintang " + (m != null ? m.bintang : -1) + " vs " + p.stars);

                // 5. progres berubah → terkirim sendiri (setelah jeda 2 detik)
                p.stars += 5;
                p.Save();
                yield return Tunggu(() => p.kelasTerkirim != terkirim && KelasOnline.Status == KelasOnline.Keadaan.Tersinkron, 30);
                yield return LihatGuru(kunciJson);
                m = s_Ev != null ? s_Ev.murid.FirstOrDefault() : null;
                Report("aplikasi: perubahan progres terkirim otomatis", m != null && m.bintang == p.stars, m != null ? m.bintang + " vs " + p.stars : "evaluasi gagal");

                // 6. guru mengeluarkan murid → kiriman berikutnya ditolak dan aplikasi ikut keluar
                using (var r = PostJson("/api/evaluasi", "{" + kunciJson + ",\"aksi\":\"hapus-murid\",\"muridId\":\"" + p.muridId + "\"}"))
                {
                    yield return r.SendWebRequest();
                    Report("guru: keluarkan murid", r.result == UnityWebRequest.Result.Success, r.responseCode + " " + r.error);
                }
                p.stars += 1;
                p.Save();
                yield return Tunggu(() => !KelasOnline.Tergabung(p), 30);
                Report("aplikasi: dikeluarkan guru → keluar dari kelas + pesan untuk murid",
                    !KelasOnline.Tergabung(p) && KelasOnline.Pesan.Contains("dikeluarkan"), KelasOnline.Status + " " + KelasOnline.Pesan);
                yield return LihatGuru(kunciJson);
                Report("guru: murid yang dikeluarkan tidak muncul lagi", s_Ev != null && s_Ev.murid.Count == 0, s_Ev == null ? "evaluasi gagal" : s_Ev.murid.Count + " murid");
            }

            // 7. bersihkan: hapus kelas uji
            using (var r = PostJson("/api/kelas", "{" + kunciJson + ",\"aksi\":\"hapus\"}"))
            {
                yield return r.SendWebRequest();
                Report("server: hapus kelas uji", r.result == UnityWebRequest.Result.Success, r.responseCode + " " + r.error);
            }
            s_UjiServerSelesai = true;
        }
    }
}
