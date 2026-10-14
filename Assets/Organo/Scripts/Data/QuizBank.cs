using System.Collections.Generic;
using System.Linq;

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
        public string answer;           // tempel label: jawaban benar untuk pembahasan (kosong = nama organ berurutan)
    }

    public class SeasonInfo
    {
        public string id, season, title, icon;
        public OrganSystem system;
        /// <summary>"Pemanasan" / "Season 2" (bukan huruf besar semua).</summary>
        public string Label => season == "PEMANASAN" ? "Pemanasan" : "Season " + season.Substring(season.LastIndexOf(' ') + 1);
    }

    /// <summary>
    /// Isi kuis. Tiap season = satu kuis berisi 5 soal (tempel label / pilihan ganda 3D / misi scan) yang benar-benar
    /// bisa dimainkan; tidak ada season "segera hadir". Tambah season baru di <see cref="Seasons"/> dan <see cref="Season"/>.
    /// </summary>
    public static class QuizBank
    {
        public static readonly List<SeasonInfo> Seasons = new List<SeasonInfo>
        {
            new SeasonInfo { id = "tubuh", season = "PEMANASAN", title = "Kenalan dengan Tubuh", system = OrganSystem.Gerak, icon = "season_tubuh" },
            new SeasonInfo { id = "cerna", season = "SEASON 1", title = "Sistem Pencernaan", system = OrganSystem.Pencernaan, icon = "season_cerna" },
            new SeasonInfo { id = "napas", season = "SEASON 2", title = "Sistem Pernapasan", system = OrganSystem.Pernapasan, icon = "cat_napas" },
            new SeasonInfo { id = "darah", season = "SEASON 3", title = "Peredaran Darah", system = OrganSystem.PeredaranDarah, icon = "cat_darah" },
            new SeasonInfo { id = "ekskresi", season = "SEASON 4", title = "Sistem Ekskresi", system = OrganSystem.Ekskresi, icon = "cat_ekskresi" },
            new SeasonInfo { id = "saraf", season = "SEASON 5", title = "Saraf & Indera", system = OrganSystem.Saraf, icon = "cat_saraf" },
        };

        public static SeasonInfo GetSeason(string id) => Seasons.FirstOrDefault(s => s.id == id);

        /// <summary>Season kuis untuk organ dari sistem ini (dipakai tombol "Mulai Kuis" di halaman organ & hasil scan).</summary>
        public static string SeasonFor(OrganSystem system)
        {
            switch (system)
            {
                case OrganSystem.Pencernaan: return "cerna";
                case OrganSystem.Pernapasan: return "napas";
                case OrganSystem.PeredaranDarah: return "darah";
                case OrganSystem.Ekskresi: return "ekskresi";
                case OrganSystem.Saraf:
                case OrganSystem.Indera: return "saraf";
                default: return "tubuh";
            }
        }

        /// <summary>Id riwayat kuis: "season:cerna", "latihan:MisiScan", atau topik "repro:reprolaki".</summary>
        public static string SeasonQuizId(string seasonId) => "season:" + seasonId;

        /// <summary>Season dari satu catatan riwayat (null bila bukan kuis season). Catatan lama tanpa id = Pencernaan.</summary>
        public static string RecordSeason(QuizRecord r)
        {
            if (r == null) return null;
            if (!string.IsNullOrEmpty(r.quizId)) return r.quizId.StartsWith("season:") ? r.quizId.Substring(7) : null;
            return r.system == "Pencernaan" && r.total >= 5 ? "cerna" : null;
        }

        public static string TypeName(QuizType t)
        {
            switch (t)
            {
                case QuizType.TempelLabel: return "Tempel Label";
                case QuizType.PilihanGanda3D: return "Pilihan Ganda 3D";
                default: return "Misi Scan";
            }
        }

        /// <summary>Soal satu season (5 soal).</summary>
        public static List<QuizQuestion> Season(string id)
        {
            switch (id)
            {
                case "tubuh": return Tubuh();
                case "napas": return Pernapasan();
                case "darah": return PeredaranDarah();
                case "ekskresi": return Ekskresi();
                case "saraf": return SarafIndera();
                default: return Pencernaan();
            }
        }

        /// <summary>Latihan satu jenis soal: diambil dari semua season, maksimal 5 soal acak.</summary>
        public static List<QuizQuestion> Practice(QuizType type)
        {
            var all = Seasons.SelectMany(s => Season(s.id)).Where(q => q.type == type).ToList();
            var rnd = new System.Random();
            return all.OrderBy(_ => rnd.Next()).Take(5).ToList();
        }

        static List<QuizQuestion> Tubuh() => new List<QuizQuestion>
        {
            new QuizQuestion
            {
                type = QuizType.TempelLabel,
                prompt = "Tempel organ ke tempatnya di tubuh!",
                labelOrgans = new[] { "otak", "paru", "lambung", "usushalus" },
                answer = "Otak di kepala, paru-paru di dada, lambung & usus halus di perut",
                explain = "Otak ada di dalam kepala, paru-paru di dada, lambung di perut bagian atas, dan usus halus di perut bagian tengah."
            },
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "otak",
                prompt = "Organ apa ini?",
                options = new[] { "Otak", "Paru-paru", "Jantung", "Ginjal" }, correct = 0,
                explain = "Ini otak. Otak ada di dalam kepala dan mengatur semua yang kamu lakukan, dari berpikir sampai bergerak."
            },
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "jantung",
                prompt = "Organ ini ada di dalam dada. Apa tugasnya?",
                options = new[] { "Memompa darah ke seluruh tubuh", "Mencerna makanan", "Membuat urine", "Melihat benda" }, correct = 0,
                explain = "Jantung memompa darah ke seluruh tubuh. Dalam sehari, jantung berdetak lebih dari 100 ribu kali."
            },
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "rangka",
                prompt = "Apa gunanya rangka (tulang-tulang) untuk tubuh?",
                options = new[] { "Menopang tubuh dan melindungi organ di dalamnya", "Menghaluskan makanan", "Mengambil oksigen dari udara", "Mendengar suara" }, correct = 0,
                explain = "Rangka membuat tubuh bisa berdiri tegak dan melindungi organ. Tulang tengkorak melindungi otak, tulang rusuk melindungi jantung dan paru-paru."
            },
            new QuizQuestion
            {
                type = QuizType.MisiScan,
                prompt = "Cari lalu scan kartu organ tempat makanan pertama kali masuk!",
                hint = "Petunjuk: kamu memakainya untuk makan dan berbicara.",
                organId = "mulut",
                explain = "Makanan masuk lewat mulut. Gigi mengunyahnya dan air liur membuatnya lunak supaya mudah ditelan."
            },
        };

        static List<QuizQuestion> Pernapasan() => new List<QuizQuestion>
        {
            new QuizQuestion
            {
                type = QuizType.TempelLabel,
                prompt = "Tempel organ pernapasan ke posisi yang tepat!",
                labelOrgans = new[] { "hidung", "tenggorokan", "paru" },
                answer = "Hidung → Tenggorokan → Paru-paru",
                explain = "Udara masuk lewat hidung, turun melalui tenggorokan, lalu sampai di paru-paru."
            },
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "paru",
                prompt = "Organ apa ini?",
                options = new[] { "Paru-paru", "Jantung", "Hati", "Lambung" }, correct = 0,
                explain = "Ini paru-paru. Kita punya dua paru-paru, di kanan dan kiri dada, yang mengembang saat kita menarik napas."
            },
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "hidung",
                prompt = "Apa gunanya rambut halus dan lendir di dalam hidung?",
                options = new[] { "Menyaring debu dan kotoran dari udara", "Mendengar suara", "Membuat darah", "Menyimpan makanan" }, correct = 0,
                explain = "Rambut halus dan lendir di hidung menangkap debu dan kuman, jadi udara yang masuk ke paru-paru lebih bersih."
            },
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "tenggorokan",
                prompt = "Saluran ini mengalirkan udara dari hidung ke paru-paru. Apa namanya?",
                options = new[] { "Tenggorokan", "Kerongkongan", "Usus halus", "Ginjal" }, correct = 0,
                explain = "Tenggorokan (batang tenggorok) mengalirkan udara ke paru-paru. Kerongkongan berbeda: kerongkongan mengalirkan makanan ke lambung."
            },
            new QuizQuestion
            {
                type = QuizType.MisiScan,
                prompt = "Cari lalu scan kartu organ tempat oksigen masuk ke darah!",
                hint = "Petunjuk: jumlahnya dua, di kanan dan kiri dada.",
                organId = "paru",
                explain = "Di paru-paru ada gelembung kecil bernama alveolus. Di sanalah oksigen dari udara masuk ke darah."
            },
        };

        static List<QuizQuestion> PeredaranDarah() => new List<QuizQuestion>
        {
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "jantung",
                prompt = "Organ apa ini?",
                options = new[] { "Jantung", "Paru-paru", "Ginjal", "Lambung" }, correct = 0,
                explain = "Ini jantung, otot kuat kira-kira sebesar kepalan tanganmu yang ada di dalam dada."
            },
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "jantung",
                prompt = "Ada berapa ruang di dalam jantung?",
                options = new[] { "2", "3", "4", "6" }, correct = 2,
                explain = "Jantung punya 4 ruang: 2 serambi di bagian atas dan 2 bilik di bagian bawah."
            },
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "jantung",
                prompt = "Apa yang dibawa darah ke seluruh tubuh?",
                options = new[] { "Oksigen dan sari-sari makanan", "Sisa makanan", "Air liur", "Udara dari hidung saja" }, correct = 0,
                explain = "Darah membawa oksigen dari paru-paru dan sari-sari makanan dari usus ke seluruh bagian tubuh."
            },
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "jantung",
                prompt = "Kebiasaan apa yang membuat jantung tetap sehat?",
                options = new[] { "Rajin bergerak dan berolahraga", "Makan gorengan setiap hari", "Tidur larut malam", "Duduk main HP seharian" }, correct = 0,
                explain = "Berolahraga membuat otot jantung kuat. Makan sayur dan buah serta tidur cukup juga membantu jantung tetap sehat."
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

        static List<QuizQuestion> Ekskresi() => new List<QuizQuestion>
        {
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "ginjal",
                prompt = "Organ apa ini?",
                options = new[] { "Ginjal", "Jantung", "Hati", "Paru-paru" }, correct = 0,
                explain = "Ini ginjal. Bentuknya seperti kacang merah, dan kita punya dua ginjal."
            },
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "ginjal",
                prompt = "Apa tugas ginjal?",
                options = new[] { "Menyaring darah dan membuat urine", "Memompa darah", "Mencerna makanan", "Mengatur gerak tubuh" }, correct = 0,
                explain = "Ginjal menyaring sisa zat dan air berlebih dari darah, lalu membuangnya sebagai urine."
            },
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "ginjal",
                prompt = "Urine dari ginjal ditampung di mana sebelum dibuang?",
                options = new[] { "Kandung kemih", "Lambung", "Paru-paru", "Usus besar" }, correct = 0,
                explain = "Urine mengalir dari ginjal lewat ureter ke kandung kemih, lalu dibuang saat kita buang air kecil."
            },
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "paru",
                prompt = "Paru-paru juga membuang zat sisa. Gas apa yang dibuang saat kita mengembuskan napas?",
                options = new[] { "Karbon dioksida", "Oksigen", "Sari makanan", "Urine" }, correct = 0,
                explain = "Saat mengembuskan napas, paru-paru membuang karbon dioksida. Karena itu paru-paru juga termasuk alat ekskresi."
            },
            new QuizQuestion
            {
                type = QuizType.MisiScan,
                prompt = "Cari lalu scan kartu organ yang menyaring darah dan membuat urine!",
                hint = "Petunjuk: letaknya di perut bagian belakang, dekat pinggang.",
                organId = "ginjal",
                explain = "Ginjal menyaring darah siang dan malam. Minum air putih yang cukup membantu ginjal bekerja."
            },
        };

        static List<QuizQuestion> SarafIndera() => new List<QuizQuestion>
        {
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "otak",
                prompt = "Organ apa ini?",
                options = new[] { "Otak", "Paru-paru", "Usus halus", "Hati" }, correct = 0,
                explain = "Ini otak, pusat pengatur tubuh. Otak dilindungi tulang tengkorak yang keras."
            },
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "otak",
                prompt = "Bagaimana otak mengirim perintah ke seluruh tubuh?",
                options = new[] { "Lewat saraf", "Lewat tulang", "Lewat udara", "Lewat makanan" }, correct = 0,
                explain = "Otak mengirim dan menerima pesan lewat saraf. Saraf menjalar ke seluruh tubuh seperti kabel, dan pesannya bergerak sangat cepat."
            },
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "mata",
                prompt = "Bagian mata yang membesar di tempat gelap dan mengecil di tempat terang adalah...",
                options = new[] { "Pupil", "Kelopak mata", "Bulu mata", "Alis" }, correct = 0,
                explain = "Pupil adalah lubang hitam di tengah mata. Pupil mengatur banyaknya cahaya yang masuk ke mata."
            },
            new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = "mata",
                prompt = "Mana cara menjaga kesehatan mata?",
                options = new[] { "Membaca di tempat yang terang", "Main HP di tempat gelap", "Mengucek mata dengan tangan kotor", "Menatap matahari langsung" }, correct = 0,
                explain = "Bacalah di tempat yang cukup terang, istirahatkan mata setelah menatap layar, dan jangan pernah menatap matahari langsung."
            },
            new QuizQuestion
            {
                type = QuizType.MisiScan,
                prompt = "Cari lalu scan kartu organ yang kamu pakai untuk melihat!",
                hint = "Petunjuk: jumlahnya dua, ada di wajahmu.",
                organId = "mata",
                explain = "Mata menangkap cahaya, lalu saraf mengirim gambarnya ke otak. Otaklah yang membuat kita mengerti apa yang kita lihat."
            },
        };

        public static List<QuizQuestion> Pencernaan(QuizType? only = null)
        {
            var all = new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    type = QuizType.TempelLabel,
                    prompt = "Tempel organ ke posisi yang tepat!",
                    labelOrgans = new[] { "mulut", "kerongkongan", "lambung", "usushalus" },
                    answer = "Mulut → Kerongkongan → Lambung → Usus Halus",
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
                    prompt = "Cari lalu scan kartu organ yang membuat cairan empedu!",
                    hint = "Petunjuk: organ terbesar di dalam perut, di sisi kanan.",
                    organId = "hati",
                    explain = "Hati membuat empedu yang membantu mencerna lemak. Hati juga menyaring racun dari darah."
                },
            };
            if (only.HasValue) all.RemoveAll(q => q.type != only.Value);
            return all;
        }

        /// <summary>Judul kuis topik, mis. "Kuis Reproduksi Laki-laki".</summary>
        public static string TopicTitle(string topic) => ReproAccess.QuizName(ReproAccess.OrganOfTopic(topic));

        /// <summary>
        /// Kuis topik khusus. Saat ini: organ reproduksi (laki-laki / perempuan). Menyelesaikan kuis reproduksi milik
        /// sendiri membuka organ reproduksi gender lain (lihat <see cref="ReproAccess"/>).
        /// </summary>
        public static List<QuizQuestion> Topic(string topic)
        {
            string organ = ReproAccess.OrganOfTopic(topic);
            var safety = new QuizQuestion
            {
                type = QuizType.PilihanGanda3D, organId = organ,
                prompt = "Ada orang yang ingin melihat atau menyentuh bagian pribadimu dan membuatmu tidak nyaman. Apa yang sebaiknya kamu lakukan?",
                options = new[] { "Bilang \"tidak\", pergi, lalu cerita ke orang dewasa yang kamu percaya", "Diam saja dan merahasiakannya", "Menurut saja karena orangnya lebih tua", "Menunggu sampai orang itu pergi sendiri" }, correct = 0,
                explain = "Organ reproduksi termasuk bagian tubuh pribadi. Tubuhmu milikmu: katakan TIDAK, pergi, lalu ceritakan ke orang tua, guru, atau orang dewasa yang kamu percaya. Itu bukan salahmu."
            };
            if (organ == ReproAccess.Boy)
                return new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        type = QuizType.PilihanGanda3D, organId = organ,
                        prompt = "Bagian mana yang membuat sel sperma?",
                        options = new[] { "Testis", "Kandung kemih", "Kelenjar prostat", "Ginjal" }, correct = 0,
                        explain = "Testis (buah zakar) membuat sel sperma dan hormon testosteron. Testis mulai bekerja saat pubertas."
                    },
                    new QuizQuestion
                    {
                        type = QuizType.PilihanGanda3D, organId = organ,
                        prompt = "Mengapa testis berada di dalam skrotum, di luar perut?",
                        options = new[] { "Supaya suhunya sedikit lebih dingin dari suhu tubuh", "Supaya lebih mudah bergerak", "Karena perut sudah penuh", "Supaya bisa mendengar suara" }, correct = 0,
                        explain = "Sel sperma butuh suhu sedikit lebih dingin daripada suhu tubuh, jadi testis berada di skrotum di luar perut."
                    },
                    new QuizQuestion
                    {
                        type = QuizType.PilihanGanda3D, organId = organ,
                        prompt = "Saluran di dalam penis yang dilewati urine namanya...",
                        options = new[] { "Ureter", "Uretra", "Saluran sperma", "Kerongkongan" }, correct = 1,
                        explain = "Uretra berawal dari kandung kemih dan memanjang di dalam penis. Ureter berbeda: ureter adalah saluran urine dari ginjal ke kandung kemih."
                    },
                    new QuizQuestion
                    {
                        type = QuizType.PilihanGanda3D, organId = organ,
                        prompt = "Apa tugas saluran sperma (vas deferens)?",
                        options = new[] { "Menyaring darah", "Menyimpan urine", "Mengantar sperma dari epididimis ke uretra", "Membuat hormon" }, correct = 2,
                        explain = "Saluran sperma naik dari epididimis, melingkari kandung kemih, lalu bergabung dengan uretra di dekat prostat."
                    },
                    safety,
                };
            return new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    type = QuizType.PilihanGanda3D, organId = organ,
                    prompt = "Bagian mana yang menyimpan dan melepaskan sel telur?",
                    options = new[] { "Rahim", "Ovarium (indung telur)", "Vagina", "Kandung kemih" }, correct = 1,
                    explain = "Ovarium menyimpan calon sel telur. Mulai pubertas, kira-kira sebulan sekali satu sel telur matang dan dilepaskan."
                },
                new QuizQuestion
                {
                    type = QuizType.PilihanGanda3D, organId = organ,
                    prompt = "Di organ mana bayi tumbuh selama kehamilan?",
                    options = new[] { "Tuba falopi", "Ovarium", "Rahim", "Lambung" }, correct = 2,
                    explain = "Rahim berdinding otot yang tebal dan lentur. Selama kehamilan, rahim melar mengikuti bayi yang tumbuh di dalamnya."
                },
                new QuizQuestion
                {
                    type = QuizType.PilihanGanda3D, organId = organ,
                    prompt = "Saluran yang dilewati sel telur dari ovarium menuju rahim namanya...",
                    options = new[] { "Tuba falopi", "Ureter", "Usus halus", "Trakea" }, correct = 0,
                    explain = "Fimbria di ujung tuba falopi menangkap sel telur, lalu sel telur berjalan pelan di tuba falopi menuju rahim."
                },
                new QuizQuestion
                {
                    type = QuizType.PilihanGanda3D, organId = organ,
                    prompt = "Apa yang terjadi saat haid (menstruasi)?",
                    options = new[] { "Ovarium mengeluarkan air", "Lapisan dinding rahim yang menebal luruh dan keluar lewat vagina", "Lambung mengeluarkan asam", "Ada luka di kulit" }, correct = 1,
                    explain = "Setiap bulan dinding rahim menebal. Kalau tidak terjadi kehamilan, lapisan ini luruh dan keluar sebagai darah haid. Ini tanda tubuh yang sehat."
                },
                safety,
            };
        }
    }
}
