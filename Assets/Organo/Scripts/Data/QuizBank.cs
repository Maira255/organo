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

    /// <summary>Kuis proses: cara kerja satu sistem tubuh, mis. perjalanan makanan dari mulut sampai usus.</summary>
    public class ProcessInfo
    {
        public string id, title, icon;
        public OrganSystem system;
        /// <summary>Kuis organ yang harus selesai dulu sebelum kuis proses ini terbuka.</summary>
        public string[] organs;
    }

    /// <summary>
    /// Isi kuis. Ada dua jenis:
    /// - <b>Kuis organ</b> (5 soal tentang satu organ): terbuka setelah kartu organnya di-scan, atau setelah kuis organ
    ///   sebelumnya di <see cref="OrganOrder"/> selesai (lihat <see cref="QuizAccess"/>).
    /// - <b>Kuis proses</b> (5 soal tentang cara kerja satu sistem): terbuka setelah semua kuis organnya selesai.
    /// </summary>
    public static class QuizBank
    {
        /// <summary>Urutan kuis organ (organ reproduksi menyusul sesuai tubuh anak, lihat <see cref="QuizAccess.Order"/>).</summary>
        public static readonly string[] OrganOrder =
        {
            "mulut", "kerongkongan", "lambung", "usushalus", "ususbesar", "hati", "pankreas",
            "hidung", "tenggorokan", "paru", "jantung", "ginjal", "otak", "mata", "rangka",
        };

        public static readonly List<ProcessInfo> Processes = new List<ProcessInfo>
        {
            new ProcessInfo { id = "cerna", title = "Proses Pencernaan", system = OrganSystem.Pencernaan, icon = "season_cerna",
                organs = new[] { "mulut", "kerongkongan", "lambung", "usushalus", "ususbesar", "hati", "pankreas" } },
            new ProcessInfo { id = "napas", title = "Proses Pernapasan", system = OrganSystem.Pernapasan, icon = "cat_napas",
                organs = new[] { "hidung", "tenggorokan", "paru" } },
            new ProcessInfo { id = "darah", title = "Peredaran Darah", system = OrganSystem.PeredaranDarah, icon = "cat_darah",
                organs = new[] { "jantung" } },
            new ProcessInfo { id = "ekskresi", title = "Proses Pengeluaran Zat Sisa", system = OrganSystem.Ekskresi, icon = "cat_ekskresi",
                organs = new[] { "ginjal", "paru" } },
            new ProcessInfo { id = "saraf", title = "Saraf dan Indera", system = OrganSystem.Saraf, icon = "cat_saraf",
                organs = new[] { "otak", "mata" } },
            new ProcessInfo { id = "tubuh", title = "Letak Organ di Tubuh", system = OrganSystem.Gerak, icon = "season_tubuh",
                organs = new[] { "mulut", "lambung", "usushalus", "paru", "jantung", "otak", "rangka" } },
        };

        public static ProcessInfo GetProcess(string id) => Processes.FirstOrDefault(s => s.id == id);

        /// <summary>Id riwayat kuis proses (awalan "season:" dipertahankan supaya riwayat versi lama tetap terbaca).</summary>
        public static string ProcessQuizId(string processId) => "season:" + processId;
        public static string OrganQuizId(string organId) => "organ:" + organId;

        /// <summary>Kuis proses dari satu catatan riwayat (null bila bukan kuis proses). Catatan lama tanpa id = Pencernaan.</summary>
        public static string ProcessOfRecord(QuizRecord r)
        {
            if (r == null) return null;
            if (!string.IsNullOrEmpty(r.quizId)) return r.quizId.StartsWith("season:") ? r.quizId.Substring(7) : null;
            return r.system == "Pencernaan" && r.total >= 5 ? "cerna" : null;
        }

        /// <summary>Organ dari satu catatan riwayat kuis organ (juga kuis reproduksi versi lama "repro:...").</summary>
        public static string OrganOfRecord(QuizRecord r)
        {
            var id = r?.quizId ?? "";
            if (id.StartsWith("organ:")) return id.Substring(6);
            if (id.StartsWith("repro:")) return id.Substring(6);
            return null;
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

        /// <summary>Judul kuis organ, mis. "Kuis Lambung" atau "Kuis Reproduksi Laki-laki".</summary>
        public static string OrganQuizTitle(OrganInfo o) => o == null ? "Kuis" : "Kuis " + o.displayName;

        /// <summary>Soal satu kuis proses (5 soal).</summary>
        public static List<QuizQuestion> Process(string id)
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

        // ================================================================== kuis organ

        static QuizQuestion Pick(string organ, string prompt, string[] options, int correct, string explain) =>
            new QuizQuestion { type = QuizType.PilihanGanda3D, organId = organ, prompt = prompt, options = options, correct = correct, explain = explain };

        static QuizQuestion Scan(string organ, string prompt, string hint, string explain) =>
            new QuizQuestion { type = QuizType.MisiScan, organId = organ, prompt = prompt, hint = hint, explain = explain };

        /// <summary>Soal kuis satu organ (5 soal).</summary>
        public static List<QuizQuestion> Organ(string id)
        {
            switch (id)
            {
                case "mulut": return new List<QuizQuestion>
                {
                    Pick(id, "Organ apa ini?", new[] { "Mulut", "Hidung", "Lambung", "Mata" }, 0,
                        "Ini mulut, pintu masuk makanan. Di dalamnya ada gigi, lidah, dan air liur."),
                    Pick(id, "Apa yang terjadi pada makanan di dalam mulut?", new[] { "Dikunyah gigi dan dicampur air liur", "Diserap menjadi sari makanan", "Diaduk bersama asam lambung", "Disaring menjadi urine" }, 0,
                        "Gigi mengunyah makanan menjadi potongan kecil, lalu air liur melunakkannya supaya mudah ditelan."),
                    Pick(id, "Gigi yang bentuknya pipih dan berguna untuk memotong makanan adalah ...", new[] { "Gigi seri", "Gigi taring", "Gigi geraham", "Gusi" }, 0,
                        "Gigi seri ada di depan dan bentuknya pipih seperti pahat. Gigi taring merobek makanan, sedangkan gigi geraham menggilingnya."),
                    Pick(id, "Kebiasaan mana yang baik untuk menjaga gigi?", new[] { "Menyikat gigi pagi dan sebelum tidur", "Makan permen sebelum tidur", "Menggigit benda keras", "Jarang berkumur" }, 0,
                        "Sikat gigi pagi dan sebelum tidur selama 2 menit, lalu kurangi permen dan minuman manis."),
                    Scan(id, "Cari lalu scan kartu organ tempat makanan dikunyah!", "Petunjuk: di dalamnya ada gigi dan lidah.",
                        "Mulut adalah awal perjalanan makanan di dalam tubuhmu."),
                };
                case "kerongkongan": return new List<QuizQuestion>
                {
                    Pick(id, "Organ apa ini?", new[] { "Kerongkongan", "Tenggorokan", "Usus Halus", "Ginjal" }, 0,
                        "Ini kerongkongan, saluran panjang dari mulut ke lambung."),
                    Pick(id, "Apa tugas kerongkongan?", new[] { "Mengantar makanan dari mulut ke lambung", "Mengalirkan udara ke paru-paru", "Menyerap air dari sisa makanan", "Memompa darah" }, 0,
                        "Kerongkongan mengantar makanan ke lambung. Jalan udara ke paru-paru adalah trakea (batang tenggorok)."),
                    Pick(id, "Mengapa makanan tetap sampai di lambung walaupun kamu sedang berbaring?", new[] { "Otot kerongkongan meremas makanan ke bawah", "Makanan jatuh karena berat", "Makanan ditiup udara dari paru-paru", "Makanan ditarik oleh lidah" }, 0,
                        "Otot kerongkongan meremas makanan seperti gelombang, jadi makanan terus terdorong ke lambung."),
                    Pick(id, "Kebiasaan mana yang baik saat makan?", new[] { "Mengunyah makanan sampai halus sebelum menelan", "Makan sambil berlari", "Tertawa saat mulut penuh makanan", "Menelan makanan tanpa dikunyah" }, 0,
                        "Kunyah makanan sampai halus, dan jangan makan sambil berlari atau tertawa supaya tidak tersedak."),
                    Scan(id, "Cari lalu scan kartu organ yang mengantar makanan dari mulut ke lambung!", "Petunjuk: bentuknya seperti selang panjang di leher dan dada.",
                        "Kerongkongan mendorong makanan turun ke lambung dalam beberapa detik saja."),
                };
                case "lambung": return new List<QuizQuestion>
                {
                    Pick(id, "Organ apa ini?", new[] { "Lambung", "Hati", "Usus Besar", "Pankreas" }, 0,
                        "Ini lambung, kantong lentur di perut bagian atas."),
                    Pick(id, "Apa yang terjadi pada makanan di dalam lambung?", new[] { "Diaduk bersama asam lambung sampai menjadi bubur", "Dikunyah oleh gigi", "Diserap menjadi sari makanan", "Disaring menjadi urine" }, 0,
                        "Dinding lambung yang berotot mengaduk makanan bersama asam lambung dan enzim sampai menjadi bubur halus."),
                    Pick(id, "Pintu keluar lambung menuju usus halus disebut ...", new[] { "Pilorus", "Fundus", "Epiglotis", "Rektum" }, 0,
                        "Pilorus membuka sedikit demi sedikit dan mengeluarkan bubur makanan ke usus dua belas jari, bagian awal usus halus."),
                    Pick(id, "Kebiasaan mana yang baik untuk lambung?", new[] { "Makan teratur dan tidak terlambat makan", "Sering melewatkan waktu makan", "Makan sampai terlalu kenyang", "Sering makan makanan yang sangat pedas" }, 0,
                        "Makan teratur, berhenti sebelum terlalu kenyang, dan kurangi makanan yang terlalu pedas."),
                    Scan(id, "Cari lalu scan kartu organ yang mengaduk makanan menjadi bubur!", "Petunjuk: bentuknya seperti kantong di perut bagian atas.",
                        "Lapisan lendir di dinding lambung melindunginya, jadi lambung tidak ikut tercerna oleh asamnya sendiri."),
                };
                case "usushalus": return new List<QuizQuestion>
                {
                    Pick(id, "Organ apa ini?", new[] { "Usus Halus", "Usus Besar", "Kerongkongan", "Paru-paru" }, 0,
                        "Ini usus halus, saluran panjang yang berkelok-kelok di tengah perut."),
                    Pick(id, "Apa tugas utama usus halus?", new[] { "Menyerap sari-sari makanan", "Menyerap air dari sisa makanan", "Membuat empedu", "Mengunyah makanan" }, 0,
                        "Usus halus menyerap sari-sari makanan, lalu darah membawanya ke seluruh tubuh."),
                    Pick(id, "Tonjolan kecil seperti jari di dinding usus halus yang membantu menyerap makanan disebut ...", new[] { "Vili", "Alveolus", "Nefron", "Konka" }, 0,
                        "Dinding usus halus penuh vili. Vili memperluas permukaan usus supaya sari makanan lebih banyak terserap."),
                    Pick(id, "Kebiasaan mana yang baik untuk pencernaanmu?", new[] { "Makan sayur dan buah setiap hari", "Jarang minum air putih", "Makan tanpa mencuci tangan", "Hanya makan makanan manis" }, 0,
                        "Sayur dan buah membantu pencernaan. Cuci tangan sebelum makan supaya kuman tidak ikut masuk."),
                    Scan(id, "Cari lalu scan kartu organ tempat sari-sari makanan diserap!", "Petunjuk: saluran panjang yang berkelok-kelok di tengah perut.",
                        "Kalau direntangkan, usus halus bisa lebih panjang daripada tinggi badanmu."),
                };
                case "ususbesar": return new List<QuizQuestion>
                {
                    Pick(id, "Organ apa ini?", new[] { "Usus Besar", "Usus Halus", "Lambung", "Ginjal" }, 0,
                        "Ini usus besar. Usus besar membingkai usus halus seperti pagar."),
                    Pick(id, "Apa tugas usus besar?", new[] { "Menyerap air dari sisa makanan", "Menyerap sari-sari makanan", "Memompa darah", "Membuat air liur" }, 0,
                        "Usus besar menyerap air dari sisa makanan. Sisa yang tidak terpakai lalu dibuang dari tubuh."),
                    Pick(id, "Tempat sisa makanan dikumpulkan sebelum dibuang dari tubuh adalah ...", new[] { "Rektum", "Lambung", "Kerongkongan", "Kantong empedu" }, 0,
                        "Sisa makanan bergerak di sepanjang usus besar sambil diserap airnya, lalu dikumpulkan di rektum sebelum dibuang."),
                    Pick(id, "Makanan apa yang membantu usus besar supaya kamu tidak sembelit?", new[] { "Sayur, buah, dan kacang-kacangan", "Gorengan", "Permen", "Minuman bersoda" }, 0,
                        "Makanan berserat dan air putih yang cukup membuat sisa makanan mudah dibuang."),
                    Scan(id, "Cari lalu scan kartu organ yang menyerap air dari sisa makanan!", "Petunjuk: letaknya membingkai usus halus.",
                        "Di dalam usus besar hidup banyak bakteri baik yang membantu tubuhmu."),
                };
                case "hati": return new List<QuizQuestion>
                {
                    Pick(id, "Organ apa ini?", new[] { "Hati", "Lambung", "Jantung", "Ginjal" }, 0,
                        "Ini hati, organ dalam terbesar di tubuhmu. Letaknya di perut kanan atas, di bawah tulang rusuk."),
                    Pick(id, "Cairan apa yang dibuat hati untuk membantu mencerna lemak?", new[] { "Empedu", "Air liur", "Asam lambung", "Insulin" }, 0,
                        "Hati membuat cairan empedu. Empedu membantu memecah lemak di usus halus."),
                    Pick(id, "Bagian yang menyimpan cairan empedu sebelum dipakai adalah ...", new[] { "Kantong empedu", "Kandung kemih", "Rektum", "Fundus" }, 0,
                        "Kantong empedu menyimpan empedu, lalu mengeluarkannya ke usus saat ada makanan berlemak."),
                    Pick(id, "Selain membuat empedu, hati juga bertugas ...", new[] { "Membersihkan zat berbahaya dari darah", "Memompa darah", "Mengambil oksigen dari udara", "Melihat benda" }, 0,
                        "Sel-sel hati menyaring dan membersihkan darah dari zat yang berbahaya bagi tubuh."),
                    Scan(id, "Cari lalu scan kartu organ dalam terbesar di tubuhmu!", "Petunjuk: letaknya di perut kanan atas, di bawah tulang rusuk.",
                        "Hati bisa tumbuh kembali ke ukuran semula, bahkan setelah sebagian besarnya diangkat dokter."),
                };
                case "pankreas": return new List<QuizQuestion>
                {
                    Pick(id, "Organ apa ini?", new[] { "Pankreas", "Hati", "Lambung", "Usus Besar" }, 0,
                        "Ini pankreas, kelenjar panjang yang letaknya di belakang lambung."),
                    Pick(id, "Pankreas membuat insulin. Apa gunanya insulin?", new[] { "Menjaga kadar gula di dalam darah", "Membuat tulang kuat", "Membuat suara", "Menyaring udara" }, 0,
                        "Insulin menjaga kadar gula di dalam darah tetap seimbang."),
                    Pick(id, "Ke mana cairan pencernaan dari pankreas dialirkan?", new[] { "Ke usus dua belas jari", "Ke mulut", "Ke paru-paru", "Ke ginjal" }, 0,
                        "Cairan pankreas masuk ke usus dua belas jari, bagian awal usus halus, untuk membantu memecah makanan."),
                    Pick(id, "Kebiasaan mana yang baik untuk pankreas?", new[] { "Mengurangi makanan dan minuman yang terlalu manis", "Minum minuman manis setiap saat", "Malas bergerak", "Makan permen sebelum tidur" }, 0,
                        "Kurangi makanan dan minuman yang terlalu manis, lalu rajin bergerak dan berolahraga."),
                    Scan(id, "Cari lalu scan kartu kelenjar yang membuat insulin!", "Petunjuk: bentuknya panjang dan letaknya di belakang lambung.",
                        "Pankreas bekerja dua kali: membantu mencerna makanan dan menjaga gula darah tetap seimbang."),
                };
                case "hidung": return new List<QuizQuestion>
                {
                    Pick(id, "Organ apa ini?", new[] { "Hidung", "Mulut", "Mata", "Tenggorokan" }, 0,
                        "Ini hidung, pintu masuk udara saat kamu bernapas."),
                    Pick(id, "Hidung punya dua tugas. Apa saja?", new[] { "Jalan masuk udara dan alat pencium", "Mengunyah dan menelan makanan", "Melihat dan mendengar", "Memompa darah dan membuat urine" }, 0,
                        "Hidung adalah jalan masuk udara sekaligus alat indera untuk mencium bau."),
                    Pick(id, "Apa gunanya bulu halus dan lendir di dalam hidung?", new[] { "Menyaring debu dan kotoran dari udara", "Mendengar suara", "Membuat darah", "Menyimpan makanan" }, 0,
                        "Bulu halus dan lendir di hidung menangkap debu dan kuman, jadi udara yang masuk ke paru-paru lebih bersih."),
                    Pick(id, "Kebiasaan mana yang baik untuk hidung?", new[] { "Menutup hidung dan mulut saat bersin", "Mengorek hidung dengan jari", "Selalu bernapas lewat mulut", "Memasukkan benda kecil ke hidung" }, 0,
                        "Tutup hidung dan mulut saat bersin, dan bernapaslah lewat hidung supaya udaranya tersaring."),
                    Scan(id, "Cari lalu scan kartu organ yang kamu pakai untuk mencium bau!", "Petunjuk: letaknya di tengah wajah.",
                        "Hidungmu bisa mengenali ribuan macam bau."),
                };
                case "tenggorokan": return new List<QuizQuestion>
                {
                    Pick(id, "Organ apa ini?", new[] { "Tenggorokan", "Kerongkongan", "Hidung", "Jantung" }, 0,
                        "Ini tenggorokan, jalan udara dan makanan di leher."),
                    Pick(id, "Di tenggorokan, jalan udara dan jalan makanan berpisah. Ke mana udara pergi?", new[] { "Ke trakea, lalu ke paru-paru", "Ke kerongkongan, lalu ke lambung", "Ke usus halus", "Ke ginjal" }, 0,
                        "Udara masuk ke laring dan trakea menuju paru-paru, sedangkan makanan masuk ke kerongkongan menuju lambung."),
                    Pick(id, "Bagian yang menutup jalan napas saat kamu menelan disebut ...", new[] { "Epiglotis", "Amandel", "Pita suara", "Anak tekak" }, 0,
                        "Epiglotis menutup laring saat kamu menelan, jadi makanan tidak salah jalan ke paru-paru."),
                    Pick(id, "Dari mana suaramu berasal?", new[] { "Dari pita suara di laring yang bergetar", "Dari lidah yang bergetar", "Dari paru-paru yang berbunyi", "Dari gigi yang beradu" }, 0,
                        "Pita suara di laring bergetar saat udara lewat. Getaran itulah yang menjadi suaramu."),
                    Scan(id, "Cari lalu scan kartu organ tempat jalan udara dan jalan makanan berpisah!", "Petunjuk: letaknya di leher dan di dalamnya ada kotak suara.",
                        "Jangan berbicara atau tertawa saat mulut penuh makanan supaya tidak tersedak."),
                };
                case "paru": return new List<QuizQuestion>
                {
                    Pick(id, "Organ apa ini?", new[] { "Paru-paru", "Jantung", "Hati", "Lambung" }, 0,
                        "Ini paru-paru. Kita punya dua paru-paru, di kanan dan kiri dada."),
                    Pick(id, "Apa yang diambil paru-paru dari udara saat kita menarik napas?", new[] { "Oksigen", "Karbon dioksida", "Sari makanan", "Air liur" }, 0,
                        "Paru-paru mengambil oksigen dari udara. Saat mengembuskan napas, paru-paru membuang karbon dioksida."),
                    Pick(id, "Kantong udara kecil tempat oksigen masuk ke darah disebut ...", new[] { "Alveolus", "Vili", "Nefron", "Pupil" }, 0,
                        "Di ujung cabang saluran napas ada jutaan alveolus. Di sanalah oksigen pindah ke darah."),
                    Pick(id, "Kebiasaan mana yang baik untuk paru-paru?", new[] { "Menjauhi asap rokok", "Bermain di dekat asap kendaraan", "Tidak pernah berolahraga", "Bersin tanpa menutup mulut" }, 0,
                        "Jauhi asap rokok dan asap kendaraan, lalu rajin berolahraga dan bermain di luar."),
                    Scan(id, "Cari lalu scan kartu organ yang kamu pakai untuk bernapas!", "Petunjuk: jumlahnya dua, di kanan dan kiri dada.",
                        "Paru-paru kiri sedikit lebih kecil supaya ada tempat untuk jantung."),
                };
                case "jantung": return new List<QuizQuestion>
                {
                    Pick(id, "Organ apa ini?", new[] { "Jantung", "Paru-paru", "Ginjal", "Lambung" }, 0,
                        "Ini jantung, otot kuat kira-kira sebesar kepalan tanganmu yang ada di dalam dada."),
                    Pick(id, "Apa tugas jantung?", new[] { "Memompa darah ke seluruh tubuh", "Mencerna makanan", "Membuat urine", "Melihat benda" }, 0,
                        "Jantung memompa darah ke seluruh tubuh tanpa henti, siang dan malam."),
                    Pick(id, "Ada berapa ruang di dalam jantung?", new[] { "2", "3", "4", "6" }, 2,
                        "Jantung punya 4 ruang: 2 serambi di bagian atas dan 2 bilik di bagian bawah."),
                    Pick(id, "Kebiasaan apa yang membuat jantung tetap sehat?", new[] { "Rajin bergerak dan berolahraga", "Makan gorengan setiap hari", "Tidur larut malam", "Duduk bermain HP seharian" }, 0,
                        "Berolahraga membuat otot jantung kuat. Makan sayur dan buah serta tidur cukup juga membantu jantung tetap sehat."),
                    Scan(id, "Cari lalu scan kartu organ yang memompa darah ke seluruh tubuh!", "Petunjuk: ukurannya kira-kira sebesar kepalan tanganmu.",
                        "Jantungmu berdetak sekitar seratus ribu kali setiap hari."),
                };
                case "ginjal": return new List<QuizQuestion>
                {
                    Pick(id, "Organ apa ini?", new[] { "Ginjal", "Jantung", "Hati", "Paru-paru" }, 0,
                        "Ini ginjal. Bentuknya seperti kacang merah, dan kita punya dua ginjal."),
                    Pick(id, "Apa tugas ginjal?", new[] { "Menyaring darah dan membuat urine", "Memompa darah", "Mencerna makanan", "Mengatur gerak tubuh" }, 0,
                        "Ginjal menyaring sisa zat dan air berlebih dari darah, lalu membuangnya sebagai urine."),
                    Pick(id, "Saluran yang mengalirkan urine dari ginjal ke kandung kemih disebut ...", new[] { "Ureter", "Uretra", "Trakea", "Kerongkongan" }, 0,
                        "Ureter mengalirkan urine dari ginjal ke kandung kemih. Uretra adalah saluran dari kandung kemih ke luar tubuh."),
                    Pick(id, "Kebiasaan mana yang baik untuk ginjal?", new[] { "Minum air putih yang cukup", "Sering menahan buang air kecil", "Makan makanan yang sangat asin", "Jarang minum" }, 0,
                        "Minum air putih yang cukup, jangan menahan buang air kecil, dan kurangi makanan yang terlalu asin."),
                    Scan(id, "Cari lalu scan kartu organ yang menyaring darah dan membuat urine!", "Petunjuk: bentuknya seperti kacang merah dan jumlahnya dua.",
                        "Ginjal menyaring darah siang dan malam."),
                };
                case "otak": return new List<QuizQuestion>
                {
                    Pick(id, "Organ apa ini?", new[] { "Otak", "Paru-paru", "Usus Halus", "Hati" }, 0,
                        "Ini otak, pusat kendali tubuhmu. Otak dilindungi tulang tengkorak yang keras."),
                    Pick(id, "Apa tugas otak?", new[] { "Mengatur pikiran dan gerakan tubuh", "Memompa darah", "Mencerna makanan", "Menyaring darah" }, 0,
                        "Otak membantumu berpikir, mengingat, merasakan, dan bergerak."),
                    Pick(id, "Bagian otak yang menjaga keseimbanganmu adalah ...", new[] { "Otak kecil", "Batang otak", "Lobus depan", "Lobus belakang" }, 0,
                        "Otak kecil menjaga keseimbangan. Batang otak mengatur napas dan detak jantung, sedangkan lobus belakang mengolah penglihatan."),
                    Pick(id, "Mana cara melindungi otak?", new[] { "Memakai helm saat bersepeda", "Bersepeda kencang tanpa helm", "Jarang tidur", "Bermain gim sampai larut malam" }, 0,
                        "Pakai helm saat bersepeda dan tidur cukup supaya otak bisa beristirahat."),
                    Scan(id, "Cari lalu scan kartu pusat kendali tubuhmu!", "Petunjuk: letaknya di dalam kepala.",
                        "Pesan saraf bisa melesat sekitar 400 km per jam, lebih cepat daripada mobil balap."),
                };
                case "mata": return new List<QuizQuestion>
                {
                    Pick(id, "Organ apa ini?", new[] { "Mata", "Hidung", "Mulut", "Otak" }, 0,
                        "Ini mata, alat indera untuk melihat."),
                    Pick(id, "Bagian mata yang membesar di tempat gelap dan mengecil di tempat terang adalah ...", new[] { "Pupil", "Kelopak mata", "Bulu mata", "Alis" }, 0,
                        "Pupil adalah lubang hitam di tengah mata. Pupil mengatur banyaknya cahaya yang masuk ke mata."),
                    Pick(id, "Bagian mata yang menangkap cahaya seperti layar adalah ...", new[] { "Retina", "Kornea", "Iris", "Bulu mata" }, 0,
                        "Retina ada di bagian belakang mata. Retina mengubah cahaya menjadi pesan, lalu saraf mata mengirimnya ke otak."),
                    Pick(id, "Mana cara menjaga kesehatan mata?", new[] { "Membaca di tempat yang terang", "Bermain HP di tempat gelap", "Mengucek mata dengan tangan kotor", "Menatap matahari secara langsung" }, 0,
                        "Bacalah di tempat yang cukup terang, istirahatkan mata setelah menatap layar, dan jangan pernah menatap matahari secara langsung."),
                    Scan(id, "Cari lalu scan kartu organ yang kamu pakai untuk melihat!", "Petunjuk: jumlahnya dua dan ada di wajahmu.",
                        "Mata menangkap cahaya, lalu otak membuat kita mengerti apa yang kita lihat."),
                };
                case "rangka": return new List<QuizQuestion>
                {
                    Pick(id, "Apa gunanya rangka (tulang-tulang) untuk tubuh?", new[] { "Menopang tubuh dan melindungi organ di dalamnya", "Menghaluskan makanan", "Mengambil oksigen dari udara", "Mendengar suara" }, 0,
                        "Rangka membuat tubuh bisa berdiri tegak dan melindungi organ di dalamnya."),
                    Pick(id, "Tulang yang melindungi jantung dan paru-paru adalah ...", new[] { "Tulang rusuk", "Tengkorak", "Tulang paha", "Tempurung lutut" }, 0,
                        "Tulang rusuk membentuk sangkar yang melindungi jantung dan paru-paru. Tengkorak melindungi otak."),
                    Pick(id, "Tulang terpanjang dan terkuat di tubuhmu adalah ...", new[] { "Tulang paha", "Tulang rusuk", "Tulang hasta", "Tulang selangka" }, 0,
                        "Tulang paha menopang berat tubuhmu saat berdiri, berjalan, dan berlari."),
                    Pick(id, "Makanan apa yang membuat tulang kuat?", new[] { "Susu, ikan, tahu, tempe, dan sayuran hijau", "Permen dan cokelat", "Minuman bersoda", "Keripik asin" }, 0,
                        "Makanan itu mengandung kalsium untuk tulang. Sinar matahari pagi juga membantu tubuh membuat vitamin D."),
                    Scan(id, "Cari lalu scan kartu kerangka penyangga tubuhmu!", "Petunjuk: pada orang dewasa jumlah tulangnya 206.",
                        "Lebih dari separuh tulangmu ada di tangan dan kaki."),
                };
                case ReproAccess.Boy:
                case ReproAccess.Girl:
                    return Repro(id);
                default: return Pencernaan();
            }
        }

        // ================================================================== kuis proses

        static List<QuizQuestion> Tubuh() => new List<QuizQuestion>
        {
            new QuizQuestion
            {
                type = QuizType.TempelLabel,
                prompt = "Tempel organ ke tempatnya di tubuh!",
                labelOrgans = new[] { "otak", "paru", "lambung", "usushalus" },
                answer = "Otak di kepala, paru-paru di dada, lambung dan usus halus di perut",
                explain = "Otak ada di dalam kepala, paru-paru di dada, lambung di perut bagian atas, dan usus halus di perut bagian tengah."
            },
            Pick("otak", "Organ ini ada di dalam kepala. Apa namanya?", new[] { "Otak", "Paru-paru", "Jantung", "Ginjal" }, 0,
                "Ini otak. Otak ada di dalam kepala dan mengatur semua yang kamu lakukan, dari berpikir sampai bergerak."),
            Pick("jantung", "Organ ini ada di dalam dada. Apa tugasnya?", new[] { "Memompa darah ke seluruh tubuh", "Mencerna makanan", "Membuat urine", "Melihat benda" }, 0,
                "Jantung memompa darah ke seluruh tubuh. Dalam sehari, jantung berdetak lebih dari 100 ribu kali."),
            Pick("rangka", "Tulang apa yang melindungi otak?", new[] { "Tengkorak", "Tulang rusuk", "Tulang panggul", "Tulang paha" }, 0,
                "Tengkorak melindungi otak, tulang rusuk melindungi jantung dan paru-paru, dan tulang panggul menopang tubuh bagian atas."),
            Scan("mulut", "Cari lalu scan kartu organ tempat makanan pertama kali masuk!", "Petunjuk: kamu memakainya untuk makan dan berbicara.",
                "Makanan masuk lewat mulut. Gigi mengunyahnya dan air liur membuatnya lunak supaya mudah ditelan."),
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
            Pick("paru", "Urutkan jalan udara saat kamu menarik napas. Mana yang benar?", new[] { "Hidung → tenggorokan → paru-paru", "Mulut → kerongkongan → lambung", "Paru-paru → hidung → tenggorokan", "Hidung → lambung → paru-paru" }, 0,
                "Udara masuk lewat hidung, melewati tenggorokan dan trakea, lalu sampai di paru-paru."),
            Pick("paru", "Di mana oksigen dari udara pindah ke darah?", new[] { "Di alveolus paru-paru", "Di lambung", "Di hidung", "Di usus besar" }, 0,
                "Di dalam paru-paru ada gelembung kecil bernama alveolus. Di sanalah oksigen dari udara masuk ke darah."),
            Pick("tenggorokan", "Saluran yang mengalirkan udara dari tenggorokan ke paru-paru adalah ...", new[] { "Trakea (batang tenggorok)", "Kerongkongan", "Usus halus", "Ureter" }, 0,
                "Trakea mengalirkan udara ke paru-paru. Kerongkongan berbeda: kerongkongan mengalirkan makanan ke lambung."),
            Scan("paru", "Cari lalu scan kartu organ tempat oksigen masuk ke darah!", "Petunjuk: jumlahnya dua, di kanan dan kiri dada.",
                "Saat menarik napas, paru-paru mengembang dan mengambil oksigen dari udara."),
        };

        static List<QuizQuestion> PeredaranDarah() => new List<QuizQuestion>
        {
            Pick("jantung", "Apa yang dibawa darah ke seluruh tubuh?", new[] { "Oksigen dan sari-sari makanan", "Sisa makanan", "Air liur", "Udara dari hidung saja" }, 0,
                "Darah membawa oksigen dari paru-paru dan sari-sari makanan dari usus ke seluruh bagian tubuh."),
            Pick("jantung", "Darah dari tubuh kembali ke jantung, lalu dipompa ke mana untuk mengambil oksigen?", new[] { "Ke paru-paru", "Ke lambung", "Ke ginjal", "Ke otak" }, 0,
                "Jantung memompa darah ke paru-paru untuk mengambil oksigen, lalu memompanya lagi ke seluruh tubuh."),
            Pick("jantung", "Pembuluh darah terbesar yang membawa darah keluar dari jantung adalah ...", new[] { "Aorta", "Trakea", "Ureter", "Kerongkongan" }, 0,
                "Aorta membawa darah kaya oksigen dari bilik kiri jantung ke seluruh tubuh."),
            Pick("jantung", "Apa gunanya katup jantung?", new[] { "Menjaga darah mengalir satu arah", "Membuat darah", "Menyaring udara", "Mencerna makanan" }, 0,
                "Katup jantung bekerja seperti pintu satu arah, jadi darah tidak mengalir mundur."),
            Scan("jantung", "Cari lalu scan kartu organ yang memompa darah ke seluruh tubuh!", "Petunjuk: organ ini ada di dalam dada.",
                "Jantung memompa darah ke seluruh tubuh tanpa henti, siang dan malam."),
        };

        static List<QuizQuestion> Ekskresi() => new List<QuizQuestion>
        {
            Pick("ginjal", "Urutkan perjalanan urine. Mana yang benar?", new[] { "Ginjal → ureter → kandung kemih", "Kandung kemih → ginjal → ureter", "Lambung → usus → ginjal", "Ginjal → jantung → paru-paru" }, 0,
                "Ginjal membuat urine, ureter mengalirkannya, lalu kandung kemih menampungnya sebelum dibuang."),
            Pick("ginjal", "Urine dari ginjal ditampung di mana sebelum dibuang?", new[] { "Kandung kemih", "Lambung", "Paru-paru", "Usus besar" }, 0,
                "Urine mengalir dari ginjal lewat ureter ke kandung kemih, lalu dibuang saat kita buang air kecil."),
            Pick("paru", "Paru-paru juga membuang zat sisa. Gas apa yang dibuang saat kita mengembuskan napas?", new[] { "Karbon dioksida", "Oksigen", "Sari makanan", "Urine" }, 0,
                "Saat mengembuskan napas, paru-paru membuang karbon dioksida. Karena itu paru-paru juga termasuk alat pengeluaran zat sisa."),
            Pick("ginjal", "Mengapa kita perlu minum air putih yang cukup?", new[] { "Supaya ginjal bisa menyaring darah dengan baik", "Supaya gigi tumbuh", "Supaya rambut cepat panjang", "Supaya tidak perlu bernapas" }, 0,
                "Air putih membantu ginjal membuang zat sisa dari darah lewat urine."),
            Scan("ginjal", "Cari lalu scan kartu organ yang menyaring darah dan membuat urine!", "Petunjuk: letaknya di perut bagian belakang, dekat pinggang.",
                "Ginjal menyaring darah siang dan malam. Minum air putih yang cukup membantu ginjal bekerja."),
        };

        static List<QuizQuestion> SarafIndera() => new List<QuizQuestion>
        {
            Pick("otak", "Bagaimana otak mengirim perintah ke seluruh tubuh?", new[] { "Lewat saraf", "Lewat tulang", "Lewat udara", "Lewat makanan" }, 0,
                "Otak mengirim dan menerima pesan lewat saraf. Saraf menjalar ke seluruh tubuh seperti kabel, dan pesannya bergerak sangat cepat."),
            Pick("mata", "Urutkan cara kita melihat. Mana yang benar?", new[] { "Cahaya masuk ke mata → retina → saraf mata → otak", "Otak → mata → cahaya", "Saraf mata → cahaya → lensa", "Cahaya → telinga → otak" }, 0,
                "Cahaya masuk ke mata dan ditangkap retina. Saraf mata mengirim pesannya ke otak, lalu otak membuat kita mengerti apa yang kita lihat."),
            Pick("otak", "Saat tanganmu menyentuh benda panas, siapa yang menerima pesan \"panas\" dan menyuruh tangan menjauh?", new[] { "Otak dan saraf", "Lambung", "Paru-paru", "Ginjal" }, 0,
                "Saraf di kulit mengirim pesan ke otak dan sumsum tulang belakang, lalu perintah untuk menarik tangan dikirim kembali dengan sangat cepat."),
            Pick("mata", "Mana cara menjaga mata saat belajar dengan layar?", new[] { "Istirahatkan mata setiap 20 menit", "Menatap layar dari dekat berjam-jam", "Bermain HP di tempat gelap", "Mengucek mata dengan tangan kotor" }, 0,
                "Istirahatkan mata setiap 20 menit dengan melihat benda yang jauh, dan belajarlah di tempat yang cukup terang."),
            Scan("mata", "Cari lalu scan kartu organ yang kamu pakai untuk melihat!", "Petunjuk: jumlahnya dua dan ada di wajahmu.",
                "Mata menangkap cahaya, lalu saraf mengirim gambarnya ke otak."),
        };

        public static List<QuizQuestion> Pencernaan() => new List<QuizQuestion>
        {
            new QuizQuestion
            {
                type = QuizType.TempelLabel,
                prompt = "Tempel organ ke posisi yang tepat!",
                labelOrgans = new[] { "mulut", "kerongkongan", "lambung", "usushalus" },
                answer = "Mulut → Kerongkongan → Lambung → Usus Halus",
                explain = "Makanan masuk lewat mulut, turun melalui kerongkongan, diaduk di lambung, lalu diserap di usus halus."
            },
            Pick("lambung", "Urutkan perjalanan makanan. Mana yang benar?", new[] { "Mulut → kerongkongan → lambung → usus halus → usus besar", "Mulut → lambung → kerongkongan → usus besar", "Kerongkongan → mulut → usus halus → lambung", "Mulut → paru-paru → lambung → usus halus" }, 0,
                "Makanan dikunyah di mulut, didorong kerongkongan, diaduk di lambung, diserap sari-sarinya di usus halus, lalu airnya diserap di usus besar."),
            Pick("usushalus", "Di mana sari-sari makanan diserap ke dalam darah?", new[] { "Usus halus", "Usus besar", "Kerongkongan", "Mulut" }, 0,
                "Usus halus menyerap sari-sari makanan, lalu darah membawanya ke seluruh tubuh."),
            Pick("hati", "Organ mana yang membantu pencernaan walaupun tidak dilewati makanan?", new[] { "Hati dan pankreas", "Mulut dan lambung", "Kerongkongan dan usus", "Paru-paru dan jantung" }, 0,
                "Hati membuat empedu dan pankreas membuat cairan pencernaan. Keduanya mengalirkan cairan itu ke usus halus."),
            Scan("hati", "Cari lalu scan kartu organ yang membuat cairan empedu!", "Petunjuk: organ terbesar di dalam perut, di sisi kanan.",
                "Hati membuat empedu yang membantu mencerna lemak. Hati juga menyaring racun dari darah."),
        };

        // ================================================================== organ reproduksi

        /// <summary>Kuis organ reproduksi (laki-laki / perempuan), selalu diakhiri soal keamanan tubuh.</summary>
        static List<QuizQuestion> Repro(string organ)
        {
            var safety = Pick(organ,
                "Ada orang yang ingin melihat atau menyentuh bagian pribadimu dan membuatmu tidak nyaman. Apa yang sebaiknya kamu lakukan?",
                new[] { "Berkata \"tidak\", pergi, lalu bercerita kepada orang dewasa yang kamu percaya", "Diam saja dan merahasiakannya", "Menurut saja karena orangnya lebih tua", "Menunggu sampai orang itu pergi sendiri" }, 0,
                "Organ reproduksi termasuk bagian tubuh pribadi. Tubuhmu milikmu: katakan TIDAK, pergi, lalu ceritakan kepada orang tua, guru, atau orang dewasa yang kamu percaya. Itu bukan salahmu.");
            if (organ == ReproAccess.Boy)
                return new List<QuizQuestion>
                {
                    Pick(organ, "Bagian mana yang membuat sel sperma?", new[] { "Testis", "Kandung kemih", "Kelenjar prostat", "Ginjal" }, 0,
                        "Testis (buah zakar) membuat sel sperma dan hormon testosteron. Testis mulai bekerja saat pubertas."),
                    Pick(organ, "Mengapa testis berada di dalam skrotum, di luar perut?", new[] { "Supaya suhunya sedikit lebih dingin daripada suhu tubuh", "Supaya lebih mudah bergerak", "Karena perut sudah penuh", "Supaya bisa mendengar suara" }, 0,
                        "Sel sperma membutuhkan suhu sedikit lebih dingin daripada suhu tubuh, jadi testis berada di skrotum di luar perut."),
                    Pick(organ, "Saluran di dalam penis yang dilewati urine disebut ...", new[] { "Ureter", "Uretra", "Saluran sperma", "Kerongkongan" }, 1,
                        "Uretra berawal dari kandung kemih dan memanjang di dalam penis. Ureter berbeda: ureter adalah saluran urine dari ginjal ke kandung kemih."),
                    Pick(organ, "Apa tugas saluran sperma (vas deferens)?", new[] { "Menyaring darah", "Menyimpan urine", "Mengantar sperma dari epididimis ke uretra", "Membuat hormon" }, 2,
                        "Saluran sperma naik dari epididimis, melingkari kandung kemih, lalu bergabung dengan uretra di dekat prostat."),
                    safety,
                };
            return new List<QuizQuestion>
            {
                Pick(organ, "Bagian mana yang menyimpan dan melepaskan sel telur?", new[] { "Rahim", "Ovarium (indung telur)", "Vagina", "Kandung kemih" }, 1,
                    "Ovarium menyimpan calon sel telur. Mulai pubertas, kira-kira sebulan sekali satu sel telur matang dan dilepaskan."),
                Pick(organ, "Di organ mana bayi tumbuh selama kehamilan?", new[] { "Tuba falopi", "Ovarium", "Rahim", "Lambung" }, 2,
                    "Rahim berdinding otot yang tebal dan lentur. Selama kehamilan, rahim melar mengikuti bayi yang tumbuh di dalamnya."),
                Pick(organ, "Saluran yang dilewati sel telur dari ovarium menuju rahim disebut ...", new[] { "Tuba falopi", "Ureter", "Usus halus", "Trakea" }, 0,
                    "Fimbria di ujung tuba falopi menangkap sel telur, lalu sel telur berjalan pelan di tuba falopi menuju rahim."),
                Pick(organ, "Apa yang terjadi saat haid (menstruasi)?", new[] { "Ovarium mengeluarkan air", "Lapisan dinding rahim yang menebal luruh dan keluar lewat vagina", "Lambung mengeluarkan asam", "Ada luka di kulit" }, 1,
                    "Setiap bulan dinding rahim menebal. Kalau tidak terjadi kehamilan, lapisan ini luruh dan keluar sebagai darah haid. Ini tanda tubuh yang sehat."),
                safety,
            };
        }
    }
}
