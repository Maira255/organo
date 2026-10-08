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
        public string system = "Pencernaan";
        public int quizNumber = 3;
        public int correct = 4;
        public int total = 5;
        public int stars = 20;
        public string when = "Hari ini, 09.41";
        public List<QuizAnswer> answers = new List<QuizAnswer>();
        public int Percent => total > 0 ? Mathf.RoundToInt(100f * correct / total) : 0;
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

    /// <summary>Progres anak. Akun disimpan di PlayerPrefs; Mode Tamu hanya selama sesi.</summary>
    [Serializable]
    public class PlayerProgress
    {
        const string Key = "organo.progress.v1";

        public bool loggedIn;
        public bool guest;
        public bool onboarded;
        public string nickname = "Dito";
        public string parentEmail = "orangtua@email.com";
        public string ageRange = "8-9";
        public Gender gender = Gender.None;
        public int stars = 120;
        public List<string> cards = new List<string> { "mulut", "kerongkongan", "lambung" };
        public List<string> cardDates = new List<string> { "28 Sep", "2 Okt", "Hari ini" };
        public List<string> openedOrgans = new List<string> { "mulut", "kerongkongan", "lambung", "jantung", "paru" };
        public List<string> favorites = new List<string>();
        public int season1Done = 3;
        public int warmupDone = 5;
        public List<QuizRecord> history = new List<QuizRecord>();
        public List<string> habitsToday = new List<string>();
        public bool sound = true;
        public bool music = true;
        public int textSize = 1;    // 0 kecil, 1 sedang, 2 besar
        public int voiceSpeed = 1;  // 0 pelan, 1 normal, 2 cepat
        public string newestCard;

        public event Action Changed;

        public static PlayerProgress Load()
        {
            var json = PlayerPrefs.GetString(Key, null);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var p = JsonUtility.FromJson<PlayerProgress>(json);
                    if (p != null) return p;
                }
                catch (Exception e) { Debug.LogWarning("[Organo] progress reset: " + e.Message); }
            }
            var fresh = new PlayerProgress();
            fresh.history.Add(new QuizRecord());
            return fresh;
        }

        public void Save()
        {
            if (!guest) PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            Changed?.Invoke();
        }

        public bool HasCard(string id) => cards.Contains(id);

        public string CardDate(string id)
        {
            int i = cards.IndexOf(id);
            return i >= 0 && i < cardDates.Count ? cardDates[i] : "Baru saja";
        }

        /// <returns>true kalau kartu ini baru.</returns>
        public bool AddCard(string id, int bonusStars = 10)
        {
            if (HasCard(id)) return false;
            cards.Add(id);
            cardDates.Add("Baru saja");
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

        public int QuizzesCompleted => warmupDone + season1Done;
        public float AveragePercent => history.Count == 0 ? 0 : (float)history.Average(h => h.Percent);

        public void StartGuest()
        {
            guest = true; loggedIn = false;
            stars = 0; cards.Clear(); cardDates.Clear(); history.Clear(); openedOrgans.Clear();
            season1Done = 0; warmupDone = 0; nickname = "Teman";
        }

        public void LogOut()
        {
            PlayerPrefs.DeleteKey(Key);
            var fresh = new PlayerProgress { onboarded = true };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(fresh), this);
            history.Add(new QuizRecord());
        }
    }
}
