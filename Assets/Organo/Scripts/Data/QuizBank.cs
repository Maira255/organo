using System.Collections.Generic;

namespace Organo.Data
{
    public enum QuizType { TempelLabel, PilihanGanda3D, MisiScan }

    public class QuizQuestion
    {
        public QuizType type;
        public string prompt;
        public string organId;          // organ 3D yang ditampilkan / dicari
        public string[] options;        // pilihan ganda
        public int correct;
        public string explain;
        public string hint;             // misi scan
        public string[] labelOrgans;    // tempel label: organ yang harus ditempel
    }

    public class SeasonInfo
    {
        public string id, season, title;
        public int total = 5;
        public OrganSystem system;
        public bool comingSoon;
    }

    /// <summary>Isi kuis dari desain (Season 1: Sistem Pencernaan). Tambah season baru di sini.</summary>
    public static class QuizBank
    {
        public static readonly List<SeasonInfo> Seasons = new List<SeasonInfo>
        {
            new SeasonInfo { id = "tubuh", season = "PEMANASAN", title = "Kenalan dengan Tubuh", system = OrganSystem.Pencernaan },
            new SeasonInfo { id = "cerna", season = "SEASON 1", title = "Sistem Pencernaan", system = OrganSystem.Pencernaan },
            new SeasonInfo { id = "napas", season = "SEASON 2", title = "Sistem Pernapasan", system = OrganSystem.Pernapasan },
            new SeasonInfo { id = "darah", season = "SEASON 3", title = "Peredaran Darah", system = OrganSystem.PeredaranDarah },
        };

        public static string TypeName(QuizType t)
        {
            switch (t)
            {
                case QuizType.TempelLabel: return "Tempel Label";
                case QuizType.PilihanGanda3D: return "Pilihan Ganda 3D";
                default: return "Misi Scan";
            }
        }

        public static List<QuizQuestion> Pencernaan(QuizType? only = null)
        {
            var all = new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    type = QuizType.TempelLabel,
                    prompt = "Tempel organ ke posisi yang tepat!",
                    labelOrgans = new[] { "mulut", "kerongkongan", "lambung", "usushalus" },
                    explain = "Makanan masuk lewat mulut, turun melalui kerongkongan, diaduk di lambung, lalu diserap di usus halus."
                },
                new QuizQuestion
                {
                    type = QuizType.PilihanGanda3D,
                    prompt = "Organ apa ini?",
                    organId = "lambung",
                    options = new[] { "Hati", "Lambung", "Usus Besar", "Pankreas" }, correct = 1,
                    explain = "Ini lambung, kantong lentur yang mengaduk makanan bersama asam lambung sampai jadi bubur halus."
                },
                new QuizQuestion
                {
                    type = QuizType.PilihanGanda3D,
                    prompt = "Organ ini bekerja untuk apa?",
                    organId = "usushalus",
                    options = new[] { "Menyerap sari-sari makanan", "Memompa darah ke seluruh tubuh", "Mengambil oksigen dari udara", "Mengunyah makanan" }, correct = 0,
                    explain = "Usus halus menyerap sari-sari makanan, lalu darah membawanya ke seluruh tubuh."
                },
                new QuizQuestion
                {
                    type = QuizType.PilihanGanda3D,
                    prompt = "Organ apa yang menyerap air dari sisa makanan?",
                    organId = "ususbesar",
                    options = new[] { "Usus Besar", "Kerongkongan", "Mulut", "Paru-paru" }, correct = 0,
                    explain = "Usus besar menyerap air dari sisa makanan sebelum dibuang dari tubuh."
                },
                new QuizQuestion
                {
                    type = QuizType.MisiScan,
                    prompt = "Cari lalu scan kartu organ yang memompa darah ke seluruh tubuh!",
                    hint = "Petunjuk: organ ini ada di dalam dada.",
                    organId = "jantung",
                    explain = "Jantung memompa darah ke seluruh tubuh tanpa henti, siang dan malam."
                },
            };
            if (only.HasValue) all.RemoveAll(q => q.type != only.Value);
            return all;
        }
    }
}
