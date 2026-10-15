using System.Collections.Generic;
using System.Linq;

namespace Organo.Data
{
    /// <summary>
    /// Kuis terkunci dulu, lalu terbuka sedikit demi sedikit:
    /// - kuis organ terbuka setelah kartu organnya di-scan, atau setelah kuis organ sebelumnya (urutan
    ///   <see cref="QuizBank.OrganOrder"/>) selesai. Kuis organ pertama terbuka sejak awal sebagai titik mulai.
    ///   Organ reproduksi tetap mengikuti <see cref="ReproAccess"/> (gender lain terbuka setelah kuis milik sendiri);
    /// - kuis proses terbuka setelah semua kuis organnya selesai (atau sudah pernah diselesaikan sebelumnya).
    /// Kuis yang sudah selesai selalu terbuka untuk diulang.
    /// </summary>
    public static class QuizAccess
    {
        /// <summary>Urutan kuis organ untuk anak ini: organ umum, organ reproduksi miliknya sendiri, lalu milik gender lain.</summary>
        public static List<string> Order(PlayerProgress p)
        {
            var list = new List<string>(QuizBank.OrganOrder);
            var g = p != null ? p.gender : Gender.None;
            if (g == Gender.Girl) { list.Add(ReproAccess.Girl); list.Add(ReproAccess.Boy); }
            else { list.Add(ReproAccess.Boy); list.Add(ReproAccess.Girl); }
            return list;
        }

        public static bool OrganDone(PlayerProgress p, string organId) =>
            p != null && organId != null && (p.doneTopics.Contains(QuizBank.OrganQuizId(organId)) || ReproAccess.QuizDone(p, organId));

        /// <summary>Kuis organ sebelumnya dalam urutan (null untuk kuis pertama).</summary>
        public static string Previous(PlayerProgress p, string organId)
        {
            var order = Order(p);
            int i = order.IndexOf(organId);
            return i > 0 ? order[i - 1] : null;
        }

        public static bool OrganUnlocked(PlayerProgress p, string organId)
        {
            if (p == null || organId == null || ReproAccess.IsLocked(organId, p)) return false;
            if (OrganDone(p, organId) || p.HasCard(organId)) return true;
            var order = Order(p);
            int i = order.IndexOf(organId);
            return i == 0 || (i > 0 && OrganDone(p, order[i - 1]));
        }

        public static bool ProcessDone(PlayerProgress p, string processId) =>
            p != null && (p.history.Any(h => QuizBank.ProcessOfRecord(h) == processId)
                          || (processId == "cerna" && p.season1Done > 0) || (processId == "tubuh" && p.warmupDone > 0));

        /// <summary>Kuis organ yang belum selesai untuk membuka kuis proses ini.</summary>
        public static List<string> ProcessMissing(PlayerProgress p, ProcessInfo pr) =>
            pr == null ? new List<string>() : pr.organs.Where(o => !OrganDone(p, o)).ToList();

        public static bool ProcessUnlocked(PlayerProgress p, ProcessInfo pr) =>
            p != null && pr != null && (ProcessDone(p, pr.id) || ProcessMissing(p, pr).Count == 0);

        /// <summary>Jumlah benar terbaik di kuis ini (-1 = belum ada catatan).</summary>
        public static int Best(PlayerProgress p, string quizId)
        {
            int best = -1;
            if (p == null) return best;
            foreach (var h in p.history)
            {
                bool same = h.quizId == quizId || (quizId.StartsWith("organ:") && h.quizId == "repro:" + quizId.Substring(6));
                if (same && h.correct > best) best = h.correct;
            }
            return best;
        }

        public static int OrgansDone(PlayerProgress p) => Order(p).Count(o => OrganDone(p, o));
        public static int ProcessesDone(PlayerProgress p) => QuizBank.Processes.Count(s => ProcessDone(p, s.id));

        /// <summary>
        /// Kuis yang sebaiknya dimainkan berikutnya: kuis proses yang sudah terbuka tetapi belum selesai, kalau tidak ada
        /// kuis organ terbuka pertama yang belum selesai. (null, null) = semua kuis yang terbuka sudah selesai.
        /// </summary>
        public static (string organ, ProcessInfo process) Next(PlayerProgress p)
        {
            var pr = QuizBank.Processes.FirstOrDefault(s => ProcessUnlocked(p, s) && !ProcessDone(p, s.id));
            if (pr != null) return (null, pr);
            var organ = Order(p).FirstOrDefault(o => OrganUnlocked(p, o) && !OrganDone(p, o));
            return (organ, null);
        }

        /// <summary>Id semua kuis yang sedang terbuka ("organ:..." dan "season:..."), untuk melihat kuis yang baru terbuka.</summary>
        public static HashSet<string> UnlockedIds(PlayerProgress p)
        {
            var set = new HashSet<string>();
            foreach (var o in Order(p)) if (OrganUnlocked(p, o)) set.Add(QuizBank.OrganQuizId(o));
            foreach (var s in QuizBank.Processes) if (ProcessUnlocked(p, s)) set.Add(QuizBank.ProcessQuizId(s.id));
            return set;
        }
    }
}
