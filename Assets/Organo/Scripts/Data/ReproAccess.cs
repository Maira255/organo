namespace Organo.Data
{
    /// <summary>
    /// Aturan organ reproduksi: anak selalu bisa mempelajari organ reproduksi sesuai tubuhnya sendiri.
    /// Organ reproduksi gender lain baru terbuka setelah ia menyelesaikan kuis reproduksi miliknya sendiri.
    /// Kalau tubuh (laki-laki/perempuan) belum dipilih, keduanya terkunci sampai dipilih.
    /// </summary>
    public static class ReproAccess
    {
        public const string Boy = "reprolaki";
        public const string Girl = "reproperempuan";

        public static bool IsRepro(OrganInfo o) => o != null && o.system == OrganSystem.Reproduksi;
        public static bool IsRepro(string id) => id == Boy || id == Girl;

        public static Gender GenderOf(string organId) => organId == Boy ? Gender.Boy : organId == Girl ? Gender.Girl : Gender.None;

        /// <summary>Organ reproduksi milik tubuh ini (null bila tubuh belum dipilih).</summary>
        public static string OwnId(Gender g) => g == Gender.Boy ? Boy : g == Gender.Girl ? Girl : null;
        public static string OtherId(Gender g) => g == Gender.Boy ? Girl : g == Gender.Girl ? Boy : null;

        /// <summary>Id topik kuis untuk satu organ reproduksi (disimpan di <see cref="PlayerProgress.doneTopics"/>).</summary>
        public static string Topic(string organId) => "repro:" + organId;
        public static string OrganOfTopic(string topic) => topic != null && topic.StartsWith("repro:") ? topic.Substring(6) : null;

        public static bool QuizDone(PlayerProgress p, string organId) => p != null && organId != null && p.doneTopics.Contains(Topic(organId));

        public static bool OwnQuizDone(PlayerProgress p) => p != null && QuizDone(p, OwnId(p.gender));

        public static bool IsLocked(OrganInfo o, PlayerProgress p) => o != null && IsLocked(o.id, p);

        public static bool IsLocked(string organId, PlayerProgress p)
        {
            if (!IsRepro(organId) || p == null) return false;
            if (p.gender == Gender.None) return true;
            if (organId == OwnId(p.gender)) return false;
            return !OwnQuizDone(p);
        }

        /// <summary>Nama kuis reproduksi untuk organ ini, mis. "Kuis Reproduksi Laki-laki".</summary>
        public static string QuizName(string organId) => organId == Boy ? "Kuis Reproduksi Laki-laki" : "Kuis Reproduksi Perempuan";

        /// <summary>Penjelasan singkat kenapa organ ini terkunci (untuk anak).</summary>
        public static string LockReason(string organId, PlayerProgress p)
        {
            if (p == null || p.gender == Gender.None)
                return "Pilih dulu tubuhmu (laki-laki atau perempuan) di Profil > Materi Tubuh. Setelah itu kamu bisa mulai dari organ reproduksimu sendiri.";
            string own = OwnId(p.gender);
            return "Pelajari organ reproduksimu sendiri, lalu selesaikan " + QuizName(own) + ". Setelah itu organ ini terbuka.";
        }
    }
}
