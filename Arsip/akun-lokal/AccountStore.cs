using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Organo.Data
{
    /// <summary>Satu akun anak yang tersimpan di HP ini (tanpa server).</summary>
    [Serializable]
    public class LocalAccount
    {
        public string id;
        public string username;
        public string nickname;
        public string ageRange;
        public int gender;
        public string salt;
        public string hash;
        public string pinSalt;
        public string pinHash;
        public string created;
        public string consentDate;
    }

    /// <summary>
    /// Akun lokal yang jujur: login benar-benar dicek (password & PIN orang tua disimpan sebagai hash PBKDF2,
    /// bukan teks asli), beberapa profil anak dalam satu HP, progres tiap akun terpisah, dan kode cadangan
    /// untuk memindahkan progres ke HP lain. Tidak ada data yang dikirim ke internet.
    /// </summary>
    public static class AccountStore
    {
        const string Key = "organo.accounts.v1";
        const string LegacyProgressKey = "organo.progress.v1";
        const string OnboardedKey = "organo.onboarded";
        const string BackupPrefix = "ORGANO1:";
        const int Iterations = 20000;
        const int MaxFails = 5;
        const float LockSeconds = 30f;

        [Serializable]
        class Store
        {
            public List<LocalAccount> accounts = new List<LocalAccount>();
            public string current;
        }

        [Serializable]
        class Backup
        {
            public LocalAccount account;
            public string progress;
        }

        static Store s_Store;
        static readonly Dictionary<string, (int fails, float until)> s_Fails = new Dictionary<string, (int, float)>();

        static Store Data
        {
            get
            {
                if (s_Store != null) return s_Store;
                var json = PlayerPrefs.GetString(Key, null);
                try { s_Store = string.IsNullOrEmpty(json) ? new Store() : JsonUtility.FromJson<Store>(json) ?? new Store(); }
                catch { s_Store = new Store(); }
                return s_Store;
            }
        }

        static void Persist()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public static IReadOnlyList<LocalAccount> Accounts => Data.accounts;
        public static LocalAccount Current => Data.accounts.Find(a => a.id == Data.current);
        public static LocalAccount Find(string username)
        {
            var u = Normalize(username);
            return Data.accounts.Find(a => a.username == u);
        }

        public static bool Onboarded
        {
            get => PlayerPrefs.GetInt(OnboardedKey, 0) == 1;
            set { PlayerPrefs.SetInt(OnboardedKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Progres dari versi lama Organo (sebelum ada akun) yang bisa dipindahkan ke akun baru.</summary>
        public static bool HasLegacyProgress => PlayerPrefs.HasKey(LegacyProgressKey);

        static string ProgressKey(string id) => "organo.progress." + id;

        public static string Normalize(string username) => (username ?? "").Trim().ToLowerInvariant();

        // ------------------------------------------------------------ validasi

        public static string ValidateUsername(string username)
        {
            var u = Normalize(username);
            if (u.Length < 3) return "Nama pengguna minimal 3 huruf.";
            if (u.Length > 20) return "Nama pengguna maksimal 20 huruf.";
            foreach (var ch in u)
                if (!(ch >= 'a' && ch <= 'z') && !(ch >= '0' && ch <= '9') && ch != '.' && ch != '_')
                    return "Pakai huruf, angka, titik, atau garis bawah saja (tanpa spasi).";
            if (Find(u) != null) return "Nama pengguna ini sudah dipakai di HP ini.";
            return null;
        }

        public static string ValidatePassword(string password, string confirm)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 6) return "Password minimal 6 karakter.";
            if (password != confirm) return "Password dan ulangi password belum sama.";
            return null;
        }

        public static string ValidatePin(string pin, string confirm)
        {
            if (string.IsNullOrEmpty(pin) || pin.Length != 4) return "PIN harus 4 angka.";
            foreach (var ch in pin) if (ch < '0' || ch > '9') return "PIN hanya berisi angka.";
            if (pin != confirm) return "PIN dan ulangi PIN belum sama.";
            return null;
        }

        // ------------------------------------------------------------ hash

        static string NewSalt()
        {
            var b = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(b);
            return Convert.ToBase64String(b);
        }

        static string Hash(string secret, string salt)
        {
            using (var kdf = new Rfc2898DeriveBytes(secret ?? "", Convert.FromBase64String(salt), Iterations, HashAlgorithmName.SHA256))
                return Convert.ToBase64String(kdf.GetBytes(32));
        }

        static bool SameHash(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        // ------------------------------------------------------------ akun

        /// <summary>Buat akun baru. Progres awal: progres tamu yang sedang berjalan, progres versi lama, atau kosong.</summary>
        public static LocalAccount Create(string username, string nickname, string ageRange, string password, string pin, PlayerProgress start)
        {
            var acc = new LocalAccount
            {
                id = Guid.NewGuid().ToString("N").Substring(0, 12),
                username = Normalize(username),
                nickname = string.IsNullOrWhiteSpace(nickname) ? username.Trim() : nickname.Trim(),
                ageRange = ageRange,
                salt = NewSalt(),
                pinSalt = NewSalt(),
                created = DateTime.Now.ToString("yyyy-MM-dd"),
                consentDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            };
            acc.hash = Hash(password, acc.salt);
            acc.pinHash = Hash(pin, acc.pinSalt);

            PlayerProgress progress = null;
            if (start != null && (start.guest || start.seed)) progress = start;
            else if (HasLegacyProgress)
            {
                try { progress = JsonUtility.FromJson<PlayerProgress>(PlayerPrefs.GetString(LegacyProgressKey)); } catch { }
                if (progress != null) PlayerPrefs.DeleteKey(LegacyProgressKey);
            }
            if (progress == null) progress = PlayerProgress.Fresh();
            progress.guest = false;
            progress.seed = false;
            progress.loggedIn = true;
            progress.onboarded = true;
            progress.nickname = acc.nickname;
            progress.ageRange = ageRange;
            acc.gender = (int)progress.gender;

            Data.accounts.Add(acc);
            Data.current = acc.id;
            Persist();
            PlayerPrefs.SetString(ProgressKey(acc.id), JsonUtility.ToJson(progress));
            PlayerPrefs.Save();
            return acc;
        }

        /// <summary>Cek nama pengguna & password. Setelah 5 kali salah, dikunci 30 detik.</summary>
        public static (LocalAccount account, string error) SignIn(string username, string password)
        {
            var u = Normalize(username);
            if (u.Length == 0) return (null, "Tulis nama penggunamu dulu.");
            if (string.IsNullOrEmpty(password)) return (null, "Tulis password-mu dulu.");
            if (s_Fails.TryGetValue(u, out var f) && f.until > Time.realtimeSinceStartup)
                return (null, "Terlalu banyak salah. Tunggu " + Mathf.CeilToInt(f.until - Time.realtimeSinceStartup) + " detik, ya.");
            var acc = Find(u);
            if (acc == null) return (null, "Nama pengguna belum terdaftar di HP ini. Daftar dulu, atau pulihkan dari kode cadangan.");
            if (!SameHash(Hash(password, acc.salt), acc.hash))
            {
                int fails = (s_Fails.TryGetValue(u, out f) ? f.fails : 0) + 1;
                s_Fails[u] = fails >= MaxFails ? (0, Time.realtimeSinceStartup + LockSeconds) : (fails, 0f);
                return (null, fails >= MaxFails ? "Terlalu banyak salah. Tunggu 30 detik, ya." : "Password-nya belum tepat. Coba lagi, ya.");
            }
            s_Fails.Remove(u);
            Data.current = acc.id;
            Persist();
            return (acc, null);
        }

        // ------------------------------------------------------------ akun uji
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Seluruh bagian ini tidak ikut dikompilasi di build rilis. Data akun uji ada di folder Editor (tidak pernah
        // masuk aplikasi); hanya Development Build yang mendapat salinannya di Resources (lihat OrganoTestAccountsBuild).

        /// <summary>Sumber data akun uji (di dalam folder Editor, jadi tidak ikut build).</summary>
        public const string TestAccountsEditorPath = "Assets/Organo/Editor/TestData/OrganoTestAccounts.json";
        /// <summary>Nama salinan di Resources yang hanya ada selama Development Build dibuat.</summary>
        public const string TestAccountsResource = "OrganoTestAccounts";

        [Serializable]
        class SeedAccount
        {
            public string username, nickname, password, pin, gender, ageRange;
            public int stars, season1Done, warmupDone;
            public List<string> cards = new List<string>(), openedOrgans = new List<string>(), favorites = new List<string>();
        }

        [Serializable]
        class SeedFile { public List<SeedAccount> accounts = new List<SeedAccount>(); }

        /// <summary>
        /// Buat (atau timpa) akun uji dari OrganoTestAccounts.json. Akun yang sedang masuk tidak berubah.
        /// Mengembalikan jumlah akun uji yang dibuat.
        /// </summary>
        public static int SeedTestAccounts()
        {
            string json = null;
#if UNITY_EDITOR
            if (System.IO.File.Exists(TestAccountsEditorPath)) json = System.IO.File.ReadAllText(TestAccountsEditorPath);
#endif
            if (json == null)
            {
                var file = Resources.Load<TextAsset>(TestAccountsResource);
                if (file != null) json = file.text;
            }
            if (string.IsNullOrEmpty(json)) return 0;
            var data = JsonUtility.FromJson<SeedFile>(json);
            if (data?.accounts == null) return 0;
            string keepCurrent = Data.current;
            int n = 0;
            foreach (var a in data.accounts)
            {
                var old = Find(a.username);
                if (old != null)
                {
                    PlayerPrefs.DeleteKey(ProgressKey(old.id));
                    Data.accounts.Remove(old);
                    if (keepCurrent == old.id) keepCurrent = null;
                }
                var p = PlayerProgress.Fresh();
                p.seed = true;
                p.gender = a.gender == "Girl" ? Gender.Girl : a.gender == "Boy" ? Gender.Boy : Gender.None;
                p.stars = a.stars;
                p.season1Done = a.season1Done;
                p.warmupDone = a.warmupDone;
                // tanggal contoh: kartu didapat satu per hari sampai hari ini
                for (int k = 0; k < a.cards.Count; k++)
                {
                    p.cards.Add(a.cards[k]);
                    p.cardDates.Add(System.DateTime.Today.AddDays(k - a.cards.Count + 1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                }
                p.openedOrgans.AddRange(a.openedOrgans);
                p.favorites.AddRange(a.favorites);
                for (int i = 0; i < a.season1Done; i++)
                    p.history.Add(new QuizRecord { system = "Pencernaan", quizNumber = i + 1, correct = 4, total = 5, stars = 20, when = "Akun uji" });
                Create(a.username, a.nickname, a.ageRange, a.password, a.pin, p);
                n++;
            }
            Data.current = keepCurrent;
            Persist();
            return n;
        }
#endif

        /// <summary>Isi progres akun (JSON) untuk dilihat di jendela Database Akun.</summary>
        public static string ProgressJson(LocalAccount acc) => acc == null ? "" : PlayerPrefs.GetString(ProgressKey(acc.id), "");

        /// <summary>Hapus akun tanpa PIN (hanya untuk alat pengembang di Editor).</summary>
        public static void DeleteForDeveloper(LocalAccount acc)
        {
            if (acc == null) return;
            PlayerPrefs.DeleteKey(ProgressKey(acc.id));
            Data.accounts.Remove(acc);
            if (Data.current == acc.id) Data.current = null;
            Persist();
        }

        /// <summary>Hapus semua akun & progres di perangkat ini (menu Editor "Reset Progres Pemain").</summary>
        public static void WipeAll()
        {
            foreach (var a in Data.accounts) PlayerPrefs.DeleteKey(ProgressKey(a.id));
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.DeleteKey(OnboardedKey);
            PlayerPrefs.DeleteKey(LegacyProgressKey);
            PlayerPrefs.Save();
            s_Store = null;
        }

        /// <summary>Baca ulang dari PlayerPrefs (dipakai uji otomatis setelah memulihkan data).</summary>
        public static void ReloadCache() { s_Store = null; s_Fails.Clear(); }

        public static void SignOut()
        {
            Data.current = null;
            Persist();
        }

        public static bool CheckPin(LocalAccount acc, string pin) => acc != null && SameHash(Hash(pin, acc.pinSalt), acc.pinHash);

        /// <summary>Atur password baru (dengan PIN orang tua). Mengembalikan pesan kesalahan atau null.</summary>
        public static string ResetPassword(LocalAccount acc, string pin, string password, string confirm)
        {
            if (acc == null) return "Akun tidak ditemukan.";
            var key = "pin:" + acc.id;
            if (s_Fails.TryGetValue(key, out var f) && f.until > Time.realtimeSinceStartup)
                return "Terlalu banyak salah. Tunggu " + Mathf.CeilToInt(f.until - Time.realtimeSinceStartup) + " detik, ya.";
            if (!CheckPin(acc, pin))
            {
                int fails = (s_Fails.TryGetValue(key, out f) ? f.fails : 0) + 1;
                s_Fails[key] = fails >= MaxFails ? (0, Time.realtimeSinceStartup + LockSeconds) : (fails, 0f);
                return "PIN orang tua belum tepat.";
            }
            s_Fails.Remove(key);
            var err = ValidatePassword(password, confirm);
            if (err != null) return err;
            acc.salt = NewSalt();
            acc.hash = Hash(password, acc.salt);
            Persist();
            return null;
        }

        public static string ChangePassword(LocalAccount acc, string oldPassword, string password, string confirm)
        {
            if (acc == null) return "Akun tidak ditemukan.";
            if (!SameHash(Hash(oldPassword, acc.salt), acc.hash)) return "Password lama belum tepat.";
            var err = ValidatePassword(password, confirm);
            if (err != null) return err;
            acc.salt = NewSalt();
            acc.hash = Hash(password, acc.salt);
            Persist();
            return null;
        }

        /// <summary>Hapus akun beserta progresnya dari HP ini (perlu PIN orang tua).</summary>
        public static string Delete(LocalAccount acc, string pin)
        {
            if (acc == null) return "Akun tidak ditemukan.";
            if (!CheckPin(acc, pin)) return "PIN orang tua belum tepat.";
            PlayerPrefs.DeleteKey(ProgressKey(acc.id));
            Data.accounts.Remove(acc);
            if (Data.current == acc.id) Data.current = null;
            Persist();
            return null;
        }

        // ------------------------------------------------------------ progres

        public static PlayerProgress LoadProgress(LocalAccount acc)
        {
            PlayerProgress p = null;
            var json = acc != null ? PlayerPrefs.GetString(ProgressKey(acc.id), null) : null;
            if (!string.IsNullOrEmpty(json))
            {
                try { p = JsonUtility.FromJson<PlayerProgress>(json); }
                catch (Exception e) { Debug.LogWarning("[Organo] progres rusak, mulai baru: " + e.Message); }
            }
            if (p == null) p = PlayerProgress.Fresh();
            p.guest = false;
            p.loggedIn = acc != null;
            p.onboarded = true;
            if (acc != null) { p.nickname = acc.nickname; p.ageRange = acc.ageRange; }
            return p;
        }

        public static void SaveProgress(PlayerProgress p)
        {
            var acc = Current;
            if (acc == null || p == null || p.guest || !p.loggedIn) return;
            PlayerPrefs.SetString(ProgressKey(acc.id), JsonUtility.ToJson(p));
            bool changed = false;
            if ((int)p.gender != acc.gender) { acc.gender = (int)p.gender; changed = true; }
            if (!string.IsNullOrEmpty(p.nickname) && p.nickname != acc.nickname) { acc.nickname = p.nickname; changed = true; }
            if (changed) Persist();
            else PlayerPrefs.Save();
        }

        // ------------------------------------------------------------ kode cadangan

        /// <summary>Kode cadangan (teks) berisi akun & progres, untuk dipindah ke HP lain lewat salin-tempel.</summary>
        public static string ExportBackup(LocalAccount acc)
        {
            if (acc == null) return null;
            var b = new Backup { account = acc, progress = PlayerPrefs.GetString(ProgressKey(acc.id), "") };
            var raw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(b));
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(ms, System.IO.Compression.CompressionLevel.Optimal, true)) gz.Write(raw, 0, raw.Length);
                return BackupPrefix + Convert.ToBase64String(ms.ToArray());
            }
        }

        /// <summary>Pulihkan akun dari kode cadangan. Kalau akunnya sudah ada di HP ini, progresnya diperbarui.</summary>
        public static (LocalAccount account, string error) ImportBackup(string code)
        {
            code = (code ?? "").Trim().Replace("\n", "").Replace("\r", "").Replace(" ", "");
            if (!code.StartsWith(BackupPrefix)) return (null, "Kode cadangan tidak dikenali. Pastikan disalin lengkap, dimulai dengan ORGANO1:");
            Backup b;
            try
            {
                var data = Convert.FromBase64String(code.Substring(BackupPrefix.Length));
                using (var ms = new MemoryStream(data))
                using (var gz = new GZipStream(ms, CompressionMode.Decompress))
                using (var outMs = new MemoryStream())
                {
                    gz.CopyTo(outMs);
                    b = JsonUtility.FromJson<Backup>(Encoding.UTF8.GetString(outMs.ToArray()));
                }
            }
            catch { return (null, "Kode cadangan rusak atau tidak lengkap."); }
            if (b?.account == null || string.IsNullOrEmpty(b.account.id)) return (null, "Kode cadangan tidak berisi akun.");
            var same = Data.accounts.Find(a => a.id == b.account.id);
            var clash = Find(b.account.username);
            if (same == null && clash != null) return (null, "Nama pengguna \"" + clash.username + "\" sudah dipakai akun lain di HP ini.");
            if (same != null) Data.accounts.Remove(same);
            Data.accounts.Add(b.account);
            Persist();
            if (!string.IsNullOrEmpty(b.progress)) PlayerPrefs.SetString(ProgressKey(b.account.id), b.progress);
            PlayerPrefs.Save();
            return (b.account, null);
        }
    }
}
