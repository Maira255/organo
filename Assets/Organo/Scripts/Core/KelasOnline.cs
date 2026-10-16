using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Organo.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace Organo
{
    /// <summary>
    /// Kelas online (opsional). Murid bergabung ke kelas guru dengan kode kelas (Profil > Kelas); setelah itu ringkasan
    /// belajarnya (nilai kuis & soal yang salah, kuis organ/proses yang selesai, organ yang dipelajari, kartu, bintang)
    /// dikirim ke server situs kartu (<see cref="Server"/>/api/sinkron) supaya guru bisa melihatnya di /guru.
    /// Tanpa kelas, atau tanpa internet, aplikasi tetap bekerja offline; kiriman yang tertunda dikirim begitu ada internet.
    /// Sejak 16 Okt 2026 aplikasi kembali sepenuhnya offline: fitur ini MATI kecuali dibuild dengan simbol
    /// <c>ORGANO_ONLINE</c> (menu Kelas tidak tampil, tidak ada kiriman, tidak ada izin internet tambahan).
    /// Server & halaman guru diarsipkan di <c>Arsip/kelas-online</c>.
    /// </summary>
    public static class KelasOnline
    {
#if ORGANO_ONLINE
        public const bool Tersedia = true;
#else
        public const bool Tersedia = false;
#endif
        public const string Server = "https://organo-kartu.vercel.app";

        /// <summary>Jenis aplikasi yang dikirim ke guru (membedakan APK kamera biasa / kamera lama).</summary>
        public const string Varian =
#if ORGANO_KAMERA_LAMA
            "online";
#else
            "online-kamera-biasa";
#endif

        public enum Keadaan { BelumGabung, Tersinkron, Menunggu, Mengirim, KelasHilang, Ditolak }

        public static Keadaan Status { get; private set; } = Keadaan.BelumGabung;
        public static event Action Berubah;
        /// <summary>Pesan dari server saat kiriman ditolak (mis. kelas penuh) atau murid dikeluarkan guru.</summary>
        public static string Pesan { get; private set; } = "";

        static bool s_Kotor, s_Mengirim, s_SimpanSendiri, s_Paksa;
        static float s_Tunda = -1f, s_Ulang;
        // naik setiap gabung/keluar: jawaban kiriman lama yang datang terlambat diabaikan
        static int s_Sesi;

        public static bool Tergabung(PlayerProgress p) => Tersedia && p != null && !string.IsNullOrEmpty(p.kelasKode);

        static void Set(Keadaan k) { if (Status == k) return; Status = k; Berubah?.Invoke(); }

        /// <summary>Dipanggil saat aplikasi mulai / progres diganti: kirim ringkasan sekali (bisa saja ada yang tertunda).</summary>
        public static void Mulai(PlayerProgress p)
        {
            if (p == null) return;
            p.Changed -= OnChanged;
            p.Changed += OnChanged;
            Set(Tergabung(p) ? Keadaan.Menunggu : Keadaan.BelumGabung);
            if (Tergabung(p)) { s_Kotor = true; s_Tunda = 1f; }
        }

        static void OnChanged()
        {
            if (s_SimpanSendiri) return;
            var p = OrganoApp.Instance != null ? OrganoApp.Instance.Progress : null;
            if (!Tergabung(p)) return;
            // beberapa perubahan berdekatan (mis. kuis selesai + kartu baru) dikirim sekali
            s_Kotor = true;
            s_Tunda = 2f;
        }

        /// <summary>Dipanggil tiap frame oleh OrganoApp.</summary>
        public static void Tick(float dt)
        {
            if (!Tersedia || !s_Kotor || s_Mengirim) return;
            if (s_Tunda >= 0f) { s_Tunda -= dt; if (s_Tunda < 0f) Kirim(); return; }
            s_Ulang -= dt;
            if (s_Ulang <= 0f) Kirim();
        }

        /// <summary>
        /// Kirim sekarang. <paramref name="paksa"/> (tombol "Kirim sekarang" di Profil) = kirim walau ringkasannya sama
        /// dengan kiriman terakhir; selain itu ringkasan yang tidak berubah tidak dikirim ulang.
        /// </summary>
        public static void KirimSekarang(bool paksa = false)
        {
            var p = OrganoApp.Instance != null ? OrganoApp.Instance.Progress : null;
            if (!Tergabung(p)) return;
            s_Kotor = true;
            s_Paksa |= paksa;
            s_Tunda = -1f;
            Kirim();
        }

        static string Sidik(string json) => Hash128.Compute(json).ToString();
        // ringkasan yang terakhir ditolak server (mis. kelas penuh): tidak dikirim ulang sebelum berubah
        static string s_SidikDitolak = "";

        static void Kirim()
        {
            var app = OrganoApp.Instance;
            var p = app != null ? app.Progress : null;
            if (!Tergabung(p) || s_Mengirim) return;
            var sidik = Sidik(Json(p));
            if (!s_Paksa && Status == Keadaan.Ditolak && sidik == s_SidikDitolak) { s_Kotor = false; return; }
            if (!s_Paksa && !string.IsNullOrEmpty(p.kelasTerkirim) && p.kelasSidik == sidik)
            {
                // tidak ada yang berubah sejak kiriman terakhir yang berhasil
                s_Kotor = false;
                if (Status != Keadaan.KelasHilang && Status != Keadaan.Ditolak) Set(Keadaan.Tersinkron);
                return;
            }
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Set(Keadaan.Menunggu);
                s_Ulang = 15f;
                return;
            }
            app.StartCoroutine(KirimRutin(p));
        }

        // ------------------------------------------------------------------ data yang dikirim

        [Serializable]
        class Kiriman
        {
            public string kode, muridId, nama, gender, varian, versi;
            public int bintang;
            public List<string> kartu, dipelajari, kuisOrgan, kuisProses;
            public List<QuizRecord> riwayat;
        }

        /// <summary>Ringkasan belajar yang dikirim ke /api/sinkron (juga dipakai uji editor).</summary>
        public static string Json(PlayerProgress p) => JsonUtility.ToJson(new Kiriman
        {
            kode = p.kelasKode, muridId = p.muridId,
            nama = string.IsNullOrWhiteSpace(p.kelasNamaMurid) ? p.nickname : p.kelasNamaMurid,
            gender = p.gender.ToString(), varian = Varian, versi = Application.version,
            bintang = p.stars,
            kartu = new List<string>(p.cards),
            dipelajari = new List<string>(p.openedOrgans),
            kuisOrgan = QuizAccess.Order(p).Where(o => QuizAccess.OrganDone(p, o)).ToList(),
            kuisProses = QuizBank.Processes.Where(s => QuizAccess.ProcessDone(p, s.id)).Select(s => s.id).ToList(),
            riwayat = p.history.Take(150).ToList(),   // server menyimpan 150 kuis terakhir
        });

        static UnityWebRequest Post(string path, string json)
        {
            var req = new UnityWebRequest(Server + path, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 20,
            };
            req.SetRequestHeader("Content-Type", "application/json");
            return req;
        }

        static IEnumerator KirimRutin(PlayerProgress p)
        {
            s_Mengirim = true;
            s_Kotor = false;
            s_Paksa = false;
            int sesi = s_Sesi;
            Set(Keadaan.Mengirim);
            var json = Json(p);
            using (var req = Post("/api/sinkron", json))
            {
                yield return req.SendWebRequest();
                s_Mengirim = false;
                if (sesi != s_Sesi) yield break;   // murid sudah keluar / pindah kelas selama kiriman berjalan
                long kode = req.responseCode;
                if (req.result == UnityWebRequest.Result.Success)
                {
                    p.kelasTerkirim = DateTime.Now.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
                    p.kelasSidik = Sidik(json);
                    Pesan = "";
                    s_SimpanSendiri = true;
                    p.Save();
                    s_SimpanSendiri = false;
                    Set(s_Kotor ? Keadaan.Menunggu : Keadaan.Tersinkron);
                    if (s_Kotor) s_Tunda = 1f;
                }
                else if (kode == 404)
                {
                    // kelas sudah dihapus guru: berhenti mengirim, murid bisa keluar / gabung kelas lain
                    Set(Keadaan.KelasHilang);
                }
                else if (kode == 410)
                {
                    // guru mengeluarkan murid ini: aplikasi ikut keluar dari kelas (progres di HP tetap ada)
                    var pesan = PesanServer(req, "Kamu sudah dikeluarkan dari kelas oleh guru.");
                    KosongkanKelas(p);
                    Pesan = pesan;
                    Berubah?.Invoke();
                }
                else if (kode == 400 || kode == 403 || kode == 413)
                {
                    // ditolak server (mis. kelas penuh): tidak dicoba terus-menerus, baru dicoba lagi saat progres berubah
                    Pesan = PesanServer(req, "Kiriman ditolak server.");
                    s_SidikDitolak = Sidik(json);
                    Set(Keadaan.Ditolak);
                    Berubah?.Invoke();
                }
                else
                {
                    Debug.LogWarning("[Organo] Kirim ke kelas gagal: " + req.responseCode + " " + req.error);
                    s_Kotor = true;
                    s_Ulang = 30f;
                    Set(Keadaan.Menunggu);
                }
            }
        }

        // ------------------------------------------------------------------ gabung & keluar

        [Serializable]
        public class InfoKelas { public string kode, namaKelas, namaGuru, error; }

        public static string BersihkanKode(string kode) =>
            new string((kode ?? "").ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

        /// <summary>Cek kode kelas ke server. Hasil: info kelas, atau pesan kesalahan untuk anak.</summary>
        public static IEnumerator Cek(string kode, Action<InfoKelas, string> selesai)
        {
            kode = BersihkanKode(kode);
            if (kode.Length != 6) { selesai(null, "Kode kelas terdiri dari 6 huruf/angka."); yield break; }
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                selesai(null, "Belum ada internet. Sambungkan HP ke internet untuk bergabung ke kelas.");
                yield break;
            }
            using (var req = UnityWebRequest.Get(Server + "/api/kelas?kode=" + kode))
            {
                req.timeout = 20;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var info = JsonUtility.FromJson<InfoKelas>(req.downloadHandler.text);
                    selesai(info, null);
                }
                else if (req.responseCode == 404) selesai(null, "Kode kelas tidak ditemukan. Periksa lagi kodenya, ya.");
                else selesai(null, "Tidak bisa tersambung ke server. Coba lagi sebentar.");
            }
        }

        static string PesanServer(UnityWebRequest req, string cadangan)
        {
            try
            {
                var e = JsonUtility.FromJson<InfoKelas>(req.downloadHandler.text);
                if (e != null && !string.IsNullOrWhiteSpace(e.error)) return e.error;
            }
            catch (Exception) { }
            return cadangan;
        }

        public static void Gabung(PlayerProgress p, InfoKelas info, string namaMurid)
        {
            p.kelasKode = BersihkanKode(info.kode);
            p.kelasNama = info.namaKelas;
            p.kelasGuru = info.namaGuru;
            p.kelasNamaMurid = string.IsNullOrWhiteSpace(namaMurid) ? p.nickname : namaMurid.Trim();
            if (string.IsNullOrEmpty(p.muridId)) p.muridId = Guid.NewGuid().ToString("N");
            p.kelasTerkirim = p.kelasSidik = "";
            Pesan = "";
            s_Sesi++;
            s_SimpanSendiri = true;
            p.Save();
            s_SimpanSendiri = false;
            Set(Keadaan.Menunggu);
            KirimSekarang();
        }

        /// <summary>Keluar dari kelas: data di server dihapus (bila ada internet), aplikasi kembali offline.</summary>
        public static void Keluar(PlayerProgress p)
        {
            if (!Tergabung(p)) return;
            var json = JsonUtility.ToJson(new Kiriman { kode = p.kelasKode, muridId = p.muridId });
            json = json.Insert(1, "\"aksi\":\"keluar\",");
            if (OrganoApp.Instance != null && Application.internetReachability != NetworkReachability.NotReachable)
                OrganoApp.Instance.StartCoroutine(KirimKeluar(json));
            Pesan = "";
            KosongkanKelas(p);
        }

        static void KosongkanKelas(PlayerProgress p)
        {
            p.kelasKode = p.kelasNama = p.kelasGuru = p.kelasNamaMurid = p.kelasTerkirim = p.kelasSidik = "";
            p.muridId = "";
            s_Sesi++;
            s_Kotor = false;
            s_Paksa = false;
            s_SimpanSendiri = true;
            p.Save();
            s_SimpanSendiri = false;
            Set(Keadaan.BelumGabung);
        }

        static IEnumerator KirimKeluar(string json)
        {
            // tunggu kiriman ringkasan yang masih berjalan, supaya data murid tidak tertulis lagi setelah dihapus
            while (s_Mengirim) yield return null;
            using (var req = Post("/api/sinkron", json)) yield return req.SendWebRequest();
        }

        /// <summary>Kalimat status untuk Profil, mis. "Terkirim ke guru hari ini, 08.13".</summary>
        public static string StatusTeks(PlayerProgress p)
        {
            switch (Status)
            {
                case Keadaan.Mengirim: return "Mengirim ke guru…";
                case Keadaan.KelasHilang: return "Kelas ini sudah tidak ada. Keluar lalu gabung dengan kode kelas yang baru.";
                case Keadaan.Ditolak: return string.IsNullOrEmpty(Pesan) ? "Kiriman ditolak server." : Pesan;
                case Keadaan.Menunggu:
                    return Application.internetReachability == NetworkReachability.NotReachable
                        ? "Belum ada internet. Nilaimu dikirim otomatis saat HP tersambung."
                        : "Menunggu dikirim ke guru…";
                default:
                    if (DateTime.TryParse(p.kelasTerkirim, null, System.Globalization.DateTimeStyles.RoundtripKind, out var t))
                    {
                        // nama bulan ditulis sendiri: data budaya "id-ID" belum tentu ada di build IL2CPP
                        string[] bulan = { "Jan", "Feb", "Mar", "Apr", "Mei", "Jun", "Jul", "Agu", "Sep", "Okt", "Nov", "Des" };
                        var hari = t.Date == DateTime.Today ? "hari ini" : t.Date == DateTime.Today.AddDays(-1) ? "kemarin" : t.Day + " " + bulan[t.Month - 1];
                        return "Terkirim ke guru " + hari + ", " + t.ToString("HH.mm");
                    }
                    return "Siap dikirim ke guru.";
            }
        }
    }
}
