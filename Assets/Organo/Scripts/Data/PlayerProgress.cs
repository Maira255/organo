using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Organo.Data
{
    public enum Gender { None, Boy, Girl }

    [Serializable]
    public class QuizRecord
    {
        public string system = "";
        public int quizNumber = 1;
        public int correct;
        public int total;
        public int stars;
        public string when = "";
        /// <summary>Kuis yang dimainkan: "season:cerna", "latihan:MisiScan", "repro:reprolaki" (kosong = catatan lama).</summary>
        public string quizId = "";
        /// <summary>Judul untuk riwayat, mis. "Sistem Pernapasan" (kosong = catatan lama, pakai "Sistem " + system).</summary>
        public string title = "";
        public List<QuizAnswer> answers = new List<QuizAnswer>();
        public int Percent => total > 0 ? Mathf.RoundToInt(100f * correct / total) : 0;
        public string Title => !string.IsNullOrEmpty(title) ? title : string.IsNullOrEmpty(system) ? "Kuis" : "Sistem " + system;
    }

    [Serializable]
    public class QuizAnswer
    {
        public string type;
        public string question;
        public string yours;
        public string right;
        public string explain;
        public bool isCorrect;
    }

    /// <summary>Progres anak, disimpan di HP ini saja (satu profil, tanpa akun dan tanpa internet).</summary>
    [Serializable]
    public class PlayerProgress
    {
        public bool onboarded;
        public string nickname = "Dito";
        public Gender gender = Gender.None;
        public int stars = 120;
        public List<string> cards = new List<string> { "mulut", "kerongkongan", "lambung" };
        public List<string> cardDates = new List<string> { "28 Sep", "2 Okt", "Hari ini" };
        public List<string> openedOrgans = new List<string> { "mulut", "kerongkongan", "lambung", "jantung", "paru" };
        public List<string> favorites = new List<string>();
        /// <summary>Kuis organ yang sudah selesai ("organ:lambung"; kuis reproduksi versi lama: "repro:reprolaki").</summary>
        public List<string> doneTopics = new List<string>();
        public int season1Done = 3;
        public int warmupDone = 5;
        public List<QuizRecord> history = new List<QuizRecord>();
        public bool sound = true;
        /// <summary>Suara pembaca (narasi organ / text-to-speech). Mati = tidak pernah membacakan.</summary>
        public bool narration = true;
        public bool music = true;
        public int textSize = 1;    // 0 kecil, 1 sedang, 2 besar
        public int voiceSpeed = 1;  // 0 pelan, 1 normal, 2 cepat
        public string newestCard;
        public bool captions = true;
        public bool highContrast;
        public bool reduceMotion;

        public event Action Changed;

        const string Key = "organo.progress.local";

        /// <summary>Progres di HP ini; pertama kali dibuka: progres kosong (lalu onboarding & pilih gender).</summary>
        public static PlayerProgress Load()
        {
            var json = PlayerPrefs.GetString(Key, null);
            bool moved = string.IsNullOrEmpty(json) && (json = OldSave()) != null;
            PlayerProgress p = null;
            if (!string.IsNullOrEmpty(json))
            {
                try { p = JsonUtility.FromJson<PlayerProgress>(json); }
                catch (Exception e) { Debug.LogWarning("[Organo] progres rusak, mulai baru: " + e.Message); }
            }
            if (p == null) { p = Fresh(); p.onboarded = PlayerPrefs.GetInt("organo.onboarded", 0) == 1; }
            if (moved) p.Save();
            return p;
        }

        [Serializable] class OldAccount { public string id; }
        [Serializable] class OldStore { public List<OldAccount> accounts = new List<OldAccount>(); public string current; }

        /// <summary>
        /// Progres dari versi yang masih memakai akun: profil yang terakhir dipakai (atau profil pertama), lalu progres
        /// versi sebelum ada akun. Data lama tidak dihapus.
        /// </summary>
        static string OldSave()
        {
            try
            {
                var store = JsonUtility.FromJson<OldStore>(PlayerPrefs.GetString("organo.accounts.v1", "{}"));
                var acc = store?.accounts?.Find(a => a.id == store.current) ?? (store?.accounts?.Count > 0 ? store.accounts[0] : null);
                var json = acc != null ? PlayerPrefs.GetString("organo.progress." + acc.id, null) : null;
                if (!string.IsNullOrEmpty(json)) return json;
            }
            catch { }
            var legacy = PlayerPrefs.GetString("organo.progress.v1", null);
            return string.IsNullOrEmpty(legacy) ? null : legacy;
        }

        /// <summary>Progres kosong (tanpa bintang, kartu, atau riwayat contoh).</summary>
        public static PlayerProgress Fresh()
        {
            var p = new PlayerProgress();
            p.stars = 0;
            p.cards.Clear(); p.cardDates.Clear(); p.openedOrgans.Clear(); p.history.Clear(); p.favorites.Clear(); p.doneTopics.Clear();
            p.season1Done = 0; p.warmupDone = 0;
            p.nickname = "Teman";
            p.newestCard = null;
            return p;
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Hapus progres di HP ini (menu Editor "Reset Progres Pemain").</summary>
        public static void Wipe()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.DeleteKey("organo.onboarded");
            // data versi berakun juga, supaya tidak dipindahkan lagi saat aplikasi dibuka
            PlayerPrefs.DeleteKey("organo.accounts.v1");
            PlayerPrefs.DeleteKey("organo.progress.v1");
            PlayerPrefs.Save();
        }

        public bool HasCard(string id) => cards.Contains(id);

        static readonly string[] Months = { "Jan", "Feb", "Mar", "Apr", "Mei", "Jun", "Jul", "Agu", "Sep", "Okt", "Nov", "Des" };
        static readonly string[] MonthsLong = { "Januari", "Februari", "Maret", "April", "Mei", "Juni", "Juli", "Agustus", "September", "Oktober", "November", "Desember" };

        /// <summary>Tanggal kartu didapat untuk ditampilkan: "Hari ini", "Kemarin", "2 Okt" (data lama berupa teks tampil apa adanya).</summary>
        public string CardDate(string id, bool longForm = false)
        {
            int i = cards.IndexOf(id);
            string raw = i >= 0 && i < cardDates.Count ? cardDates[i] : null;
            if (string.IsNullOrEmpty(raw)) return "Baru saja";
            if (!DateTime.TryParseExact(raw, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
                return raw;
            int days = (DateTime.Today - d.Date).Days;
            if (days == 0) return "Hari ini";
            if (days == 1) return "Kemarin";
            if (longForm) return d.Day + " " + MonthsLong[d.Month - 1] + " " + d.Year;
            return d.Day + " " + Months[d.Month - 1] + (d.Year != DateTime.Today.Year ? " " + d.Year : "");
        }

        /// <returns>true kalau kartu ini baru.</returns>
        public bool AddCard(string id, int bonusStars = 10)
        {
            if (HasCard(id)) return false;
            cards.Add(id);
            cardDates.Add(DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            newestCard = id;
            stars += bonusStars;
            Save();
            return true;
        }

        public void MarkOpened(string id)
        {
            if (openedOrgans.Contains(id)) return;
            openedOrgans.Add(id);
            Save();
        }

        public void ToggleFavorite(string id)
        {
            if (!favorites.Remove(id)) favorites.Add(id);
            Save();
        }

        public int QuizzesCompleted => Mathf.Max(history.Count, warmupDone + season1Done);

        public float AveragePercent => history.Count == 0 ? 0 : (float)history.Average(h => h.Percent);
    }
}
