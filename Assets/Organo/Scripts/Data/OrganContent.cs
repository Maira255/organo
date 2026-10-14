using System.Collections.Generic;

namespace Organo.Data
{
    /// <summary>Satu bagian organ (mis. bronkus, alveolus) untuk halaman detail & Info Lengkap.</summary>
    public class PartDef
    {
        public string id;
        public string name;
        /// <summary>Fungsi singkat (1 baris) untuk kartu.</summary>
        public string function;
        /// <summary>Penjelasan mudah dimengerti (2–3 kalimat).</summary>
        public string detail;
        /// <summary>Indeks material (submesh) pada model 3D organ yang menunjukkan bagian ini.</summary>
        public int[] materials;
        /// <summary>Model mikro (diperbesar), mis. "alveolus". Kosong = bagian terlihat di model organ.</summary>
        public string micro;
        /// <summary>Indeks material di model potongan ("Lihat dalam"). Diisi = bagian ini terlihat di dalam organ.</summary>
        public int[] inside;
    }

    public class OrganContentData
    {
        public PartDef[] parts;
        /// <summary>Cara kerja organ, langkah demi langkah.</summary>
        public string[] steps;
        public string[] facts;
        public string[] tips;
    }

    /// <summary>
    /// Isi edukasi tiap organ: bagian-bagian & fungsinya, cara kerja, fakta menarik, tips sehat.
    /// Bahasa disesuaikan untuk anak SD.
    /// </summary>
    public static class OrganContent
    {
        static PartDef P(string id, string name, string function, string detail, int[] mats, string micro = null, int[] inside = null) =>
            new PartDef { id = id, name = name, function = function, detail = detail, materials = mats, micro = micro, inside = inside };

        /// <summary>Bagian yang hanya terlihat di model potongan ("Lihat dalam").</summary>
        static PartDef In(string id, string name, string function, string detail, params int[] inside) =>
            new PartDef { id = id, name = name, function = function, detail = detail, materials = new int[0], inside = inside };

        static readonly Dictionary<string, OrganContentData> s_Data = new Dictionary<string, OrganContentData>
        {
            ["mulut"] = new OrganContentData
            {
                parts = new[]
                {
                    P("bibir", "Bibir", "Menutup mulut saat mengunyah", "Bibir menjaga makanan tetap di dalam mulut. Bibir juga membantumu berbicara dan tersenyum.", new[] { 0 }),
                    P("seri", "Gigi seri", "Memotong makanan", "Delapan gigi di depan (4 atas, 4 bawah) berbentuk pipih seperti pahat. Gunanya memotong makanan, misalnya saat menggigit roti.", new[] { 1 }),
                    P("taring", "Gigi taring", "Merobek makanan", "Gigi taring runcing dan ada di samping gigi seri. Gunanya merobek makanan yang alot, seperti daging.", new[] { 2 }),
                    P("geraham", "Gigi geraham", "Menggiling makanan", "Gigi geraham ada di belakang. Permukaannya lebar dan bertonjolan untuk menggiling makanan sampai halus.", new[] { 3 }),
                    P("gusi", "Gusi", "Menjaga gigi tetap kuat", "Gusi adalah daging lembut berwarna merah muda yang memeluk leher gigi supaya gigi tidak goyang.", new[] { 4 }, null, new[] { 3 }),
                    P("lidah", "Lidah", "Merasakan & mendorong makanan", "Lidah adalah otot yang penuh bintil kecil berisi pengecap. Lidah merasakan manis, asin, asam, pahit, dan gurih, lalu mendorong makanan ke tenggorokan.", new[] { 5 }),
                    P("langit", "Langit-langit", "Atap rongga mulut", "Langit-langit memisahkan mulut dan hidung. Bagian depannya keras, bagian belakangnya lunak.", new[] { 6 }),
                    P("tekak", "Anak tekak", "Menutup jalan ke hidung saat menelan", "Anak tekak (uvula) menggantung di belakang langit-langit. Saat menelan, langit-langit lunak dan anak tekak naik supaya makanan tidak masuk ke hidung.", new[] { 7 }),
                    In("email", "Email gigi", "Lapisan terkeras di tubuh", "Email adalah lapisan paling luar gigi yang putih mengilap. Email adalah bagian paling keras di seluruh tubuhmu dan melindungi gigi dari kerusakan.", 0),
                    In("dentin", "Dentin", "Lapisan kuat di bawah email", "Dentin berwarna kekuningan dan membentuk sebagian besar gigi. Dentin lebih lunak dari email.", 1),
                    In("pulpa", "Pulpa", "Bagian hidup berisi saraf", "Pulpa ada di tengah gigi. Isinya saraf dan pembuluh darah. Kalau gigi berlubang sampai pulpa, gigi terasa ngilu.", 2, 5),
                    In("akar", "Akar gigi", "Menancap di tulang rahang", "Akar gigi tertanam di dalam gusi dan tulang rahang, dilapisi sementum supaya gigi kuat menempel.", 6, 4),
                },
                steps = new[] { "Gigi seri memotong, gigi taring merobek, gigi geraham menggiling makanan.", "Air liur membasahi dan melunakkan makanan.", "Lidah membentuk makanan jadi gumpalan kecil.", "Gumpalan makanan didorong ke tenggorokan untuk ditelan." },
                facts = new[] { "Mulutmu membuat sekitar satu liter air liur setiap hari!", "Anak-anak punya 20 gigi susu, orang dewasa punya 32 gigi.", "Email gigi adalah bagian paling keras di tubuhmu.", "Lidah tidak bisa merasakan rasa kalau sedang benar-benar kering.", "Bentuk dan pola permukaan lidah setiap orang berbeda-beda!" },
                tips = new[] { "Sikat gigi pagi dan sebelum tidur selama 2 menit.", "Kunyah makanan pelan-pelan sampai halus.", "Kurangi permen dan minuman manis.", "Periksa gigi ke dokter gigi 6 bulan sekali." },
            },
            ["kerongkongan"] = new OrganContentData
            {
                parts = new[]
                {
                    P("faring", "Tenggorokan (faring)", "Jalan masuk dari mulut", "Faring adalah ruang di belakang mulut. Makanan lewat sini sebelum masuk ke kerongkongan.", new[] { 0 }),
                    P("otot", "Otot kerongkongan", "Meremas makanan turun", "Otot berbentuk cincin meremas bergantian seperti ombak, mendorong makanan turun. Gerakan ini disebut gerak peristaltik.", new[] { 1, 2 }),
                    P("sfingter", "Pintu lambung", "Menutup agar makanan tidak naik", "Di ujung bawah ada otot berbentuk cincin yang membuka saat makanan lewat, lalu menutup lagi supaya isi lambung tidak naik.", new[] { 3 }),
                    In("lipatan", "Lipatan dalam", "Membuat kerongkongan bisa melebar", "Saat kosong, dinding dalam kerongkongan berlipat-lipat. Lipatan ini meregang saat gumpalan makanan lewat.", 5),
                    In("gumpalan", "Gumpalan makanan", "Didorong turun oleh otot", "Makanan yang sudah dikunyah turun sebagai gumpalan kecil (bolus). Otot di dindingnya meremas dari atas ke bawah.", 7, 6),
                },
                steps = new[] { "Makanan yang ditelan masuk ke tenggorokan.", "Otot kerongkongan meremas makanan turun sedikit demi sedikit.", "Pintu lambung membuka dan makanan masuk ke lambung." },
                facts = new[] { "Kamu tetap bisa menelan makanan walaupun sedang berdiri terbalik, lho!", "Makanan butuh sekitar 7 detik untuk sampai ke lambung.", "Panjang kerongkongan orang dewasa kira-kira sepanjang penggaris 25 cm.", "Saat menelan, ada 'tutup' kecil yang menjaga makanan tidak masuk ke saluran napas." },
                tips = new[] { "Jangan makan sambil berlari atau tertawa.", "Minum air putih yang cukup.", "Telan makanan setelah dikunyah halus." },
            },
            ["lambung"] = new OrganContentData
            {
                parts = new[]
                {
                    P("kardia", "Pintu masuk", "Tempat makanan masuk dari kerongkongan", "Bagian atas lambung tersambung ke kerongkongan. Dari sini makanan masuk ke dalam lambung.", new[] { 2 }),
                    P("fundus", "Fundus", "Kubah penampung di bagian atas", "Fundus adalah bagian lambung yang menggembung ke atas. Di sini makanan dan udara bisa tertampung sebentar.", new[] { 3 }),
                    P("badan", "Badan lambung", "Mengaduk makanan dengan asam", "Dinding badan lambung berotot. Otot ini mengaduk makanan bersama asam lambung dan enzim sampai menjadi bubur.", new[] { 0 }),
                    P("pilorus", "Pilorus", "Pintu keluar ke usus", "Pilorus adalah bagian lambung yang menyempit. Ia hanya membuka sedikit-sedikit supaya bubur makanan masuk ke usus pelan-pelan.", new[] { 4 }),
                    P("duodenum", "Usus dua belas jari", "Bagian awal usus halus", "Setelah keluar dari lambung, makanan masuk ke usus dua belas jari untuk dicerna lebih lanjut.", new[] { 5 }),
                    In("rugae", "Lipatan lambung", "Membuat lambung bisa melar", "Dinding dalam lambung penuh lipatan bernama rugae. Saat lambung terisi, lipatan ini meregang sehingga lambung bisa menampung banyak makanan.", 6),
                    In("dinding", "Dinding berotot", "Meremas & mengaduk makanan", "Dinding lambung tebal karena tersusun dari tiga lapis otot. Otot-otot ini meremas makanan dari berbagai arah.", 7),
                    In("bubur", "Bubur makanan", "Makanan yang sedang dicerna", "Di dalam lambung, makanan bercampur dengan asam dan enzim lalu berubah menjadi bubur kental.", 8),
                },
                steps = new[] { "Makanan masuk dari kerongkongan.", "Otot lambung mengaduk makanan bersama asam dan enzim.", "Makanan berubah menjadi bubur halus.", "Bubur makanan keluar pelan-pelan ke usus halus." },
                facts = new[] { "Asam lambung sangat kuat, tetapi lapisan lendir di dinding lambung melindunginya supaya lambung tidak ikut tercerna!", "Lambung orang dewasa bisa melar sampai menampung sekitar 1–1,5 liter makanan.", "Bunyi 'kruk-kruk' saat lapar berasal dari lambung dan usus yang bergerak.", "Lapisan dalam lambung berganti baru setiap beberapa hari." },
                tips = new[] { "Makan teratur, jangan sering terlambat makan.", "Berhenti makan sebelum terlalu kenyang.", "Kurangi makanan yang terlalu pedas." },
            },
            ["usushalus"] = new OrganContentData
            {
                parts = new[]
                {
                    P("duodenum", "Usus dua belas jari", "Mencampur makanan dengan cairan pencernaan", "Bagian awal usus halus. Di sini cairan dari hati dan pankreas bercampur dengan makanan.", new[] { 0 }),
                    P("jejunum", "Usus kosong (jejunum)", "Menyerap sebagian besar sari makanan", "Bagian tengah usus halus. Sari-sari makanan seperti gula dan protein diserap di sini.", new[] { 1 }),
                    P("ileum", "Usus penyerapan (ileum)", "Menyerap sisa sari makanan", "Bagian akhir usus halus yang menyerap vitamin dan sisa sari makanan, lalu tersambung ke usus besar.", new[] { 2 }),
                    P("pembuluh", "Pembuluh darah", "Membawa sari makanan ke tubuh", "Sari makanan yang diserap masuk ke pembuluh darah, lalu dibawa ke seluruh tubuh sebagai tenaga.", new[] { 3, 4 }),
                    In("rongga", "Rongga & dinding usus", "Jalan bubur makanan", "Usus halus berbentuk pipa. Di dalamnya bubur makanan bergerak pelan, sementara dinding dalamnya yang berjonjot menyerap sari-sari makanan.", 5, 6),
                    P("vili", "Vili", "Jari-jari kecil penyerap makanan", "Dinding dalam usus halus penuh tonjolan kecil seperti jari, namanya vili. Vili membuat permukaan usus sangat luas supaya sari makanan cepat terserap.", new int[0], "vili"),
                },
                steps = new[] { "Bubur makanan masuk dari lambung.", "Cairan dari hati dan pankreas memecah makanan jadi sangat kecil.", "Vili menyerap sari-sari makanan.", "Darah membawa sari makanan ke seluruh tubuh." },
                facts = new[] { "Kalau direntangkan, usus halus orang dewasa panjangnya sekitar 3–5 meter, jauh lebih panjang dari tinggi badanmu!", "Usus halus disebut 'halus' karena diameternya lebih kecil, bukan karena pendek.", "Ada jutaan vili di dalam usus halusmu.", "Makanan bisa berada di usus halus selama 3–5 jam." },
                tips = new[] { "Makan sayur dan buah setiap hari.", "Minum air putih yang cukup.", "Cuci tangan sebelum makan supaya kuman tidak ikut masuk." },
            },
            ["ususbesar"] = new OrganContentData
            {
                parts = new[]
                {
                    P("sekum", "Sekum", "Kantong awal usus besar", "Sekum adalah kantong di awal usus besar, tempat sisa makanan masuk dari usus halus.", new[] { 0 }),
                    P("apendiks", "Usus buntu", "Kantong kecil berisi bakteri baik", "Usus buntu adalah saluran kecil yang menempel di sekum. Para ilmuwan menduga usus buntu menjadi tempat menyimpan bakteri baik untuk usus.", new[] { 1 }),
                    P("naik", "Kolon naik", "Menyerap air sambil naik ke atas", "Bagian usus besar di sisi kanan perut. Sisa makanan bergerak naik sambil airnya diserap.", new[] { 2 }),
                    P("melintang", "Kolon melintang", "Menyeberangi perut dari kanan ke kiri", "Bagian usus besar yang melintang di bawah lambung. Air terus diserap di sini.", new[] { 3 }),
                    P("turun", "Kolon turun", "Membawa sisa makanan ke bawah", "Bagian usus besar di sisi kiri perut. Sisa makanan makin padat saat turun.", new[] { 4 }),
                    P("rektum", "Rektum", "Tempat sisa makanan sebelum dibuang", "Rektum menyimpan kotoran sampai kamu pergi ke toilet.", new[] { 5 }),
                    In("dalam", "Rongga usus besar", "Tempat air diserap", "Dinding dalam usus besar menyerap air dari sisa makanan sehingga sisanya makin padat.", 7, 8),
                },
                steps = new[] { "Sisa makanan masuk dari usus halus ke sekum.", "Usus besar menyerap air dari sisa makanan.", "Bakteri baik membantu mengurai sisa makanan.", "Sisa yang padat dikumpulkan di rektum lalu dibuang." },
                facts = new[] { "Di dalam usus besar hidup triliunan bakteri baik!", "Kantong-kantong di usus besar namanya haustra, membuatnya terlihat seperti manik-manik.", "Usus besar lebih pendek dari usus halus, tetapi lebih lebar.", "Sisa makanan bisa berada di usus besar selama 1–2 hari." },
                tips = new[] { "Makan makanan berserat seperti sayur, buah, dan kacang.", "Minum air putih supaya tidak sembelit.", "Jangan menahan ke toilet terlalu lama." },
            },
            ["hati"] = new OrganContentData
            {
                parts = new[]
                {
                    P("kanan", "Lobus kanan", "Bagian hati yang paling besar", "Lobus kanan membuat empedu dan menyaring darah. Bagian ini jauh lebih besar dari lobus kiri.", new[] { 0 }),
                    P("kiri", "Lobus kiri", "Bagian hati yang lebih kecil", "Lobus kiri bekerja sama dengan lobus kanan membersihkan darah dan menyimpan cadangan tenaga.", new[] { 1 }),
                    P("ligamen", "Ligamen", "Tali yang menggantung hati", "Ligamen seperti tali kuat yang memisahkan dua lobus dan menahan hati tetap di tempatnya.", new[] { 2 }),
                    P("empedu", "Kantong empedu", "Menyimpan cairan empedu", "Kantong kecil berwarna hijau di bawah hati. Ia menyimpan empedu dan mengeluarkannya ke usus untuk mencerna lemak.", new[] { 3 }),
                    P("pembuluh", "Pembuluh darah", "Darah masuk untuk dibersihkan", "Darah dari usus masuk ke hati lewat pembuluh darah, lalu dibersihkan sebelum beredar ke seluruh tubuh.", new[] { 4, 5 }, null, new[] { 4, 5 }),
                    In("sel", "Sel-sel hati", "Pekerja pembersih darah", "Bagian dalam hati tersusun dari jutaan sel hati. Sel-sel ini membersihkan darah, menyimpan tenaga, dan membuat empedu.", 6),
                    In("saluranempedu", "Saluran empedu", "Mengalirkan empedu", "Empedu yang dibuat sel hati mengalir lewat saluran kecil berwarna hijau menuju kantong empedu.", 3),
                },
                steps = new[] { "Darah membawa sari makanan dari usus ke hati.", "Hati menyaring zat berbahaya dari darah.", "Hati menyimpan cadangan tenaga dan membuat empedu.", "Empedu dialirkan ke usus untuk membantu mencerna lemak." },
                facts = new[] { "Hati bisa tumbuh kembali ke ukuran semula, bahkan setelah sebagian besarnya diangkat dokter!", "Hati adalah organ dalam terbesar di tubuhmu.", "Hati melakukan lebih dari 500 tugas berbeda.", "Kira-kira satu setengah liter darah melewati hati setiap menit." },
                tips = new[] { "Makan makanan bergizi seimbang.", "Kurangi makanan berlemak dan gorengan.", "Jangan sembarangan minum obat tanpa izin orang tua." },
            },
            ["pankreas"] = new OrganContentData
            {
                parts = new[]
                {
                    P("kepala", "Kepala pankreas", "Dipeluk usus dua belas jari", "Bagian pankreas yang paling lebar. Dari sini cairan pencernaan dialirkan ke usus dua belas jari.", new[] { 0 }),
                    P("badan", "Badan pankreas", "Membuat cairan pencernaan", "Bagian tengah pankreas membuat cairan yang memecah karbohidrat, protein, dan lemak.", new[] { 6 }),
                    P("ekor", "Ekor pankreas", "Banyak membuat insulin", "Ujung pankreas dekat limpa. Di sini banyak 'pulau' kecil yang membuat insulin untuk mengatur gula darah.", new[] { 7 }),
                    P("duodenum", "Usus dua belas jari", "Tempat cairan pankreas masuk", "Usus berbentuk huruf C yang memeluk kepala pankreas. Cairan pankreas bertemu makanan di sini.", new[] { 1 }),
                    P("limpa", "Limpa", "Tetangga yang menyaring darah", "Limpa bukan bagian pankreas, tetapi letaknya bersebelahan. Limpa menyaring darah dan melawan kuman.", new[] { 2 }),
                    P("empedu", "Kantong empedu", "Menyimpan cairan empedu", "Kantong empedu mengirim empedu lewat saluran kecil ke usus dua belas jari, bersama cairan pankreas.", new[] { 5 }),
                    In("saluran", "Saluran pankreas", "Jalan cairan pencernaan", "Saluran di tengah pankreas mengalirkan cairan pencernaan ke usus dua belas jari.", 8),
                    In("pulau", "Pulau Langerhans", "Pembuat insulin", "Di dalam pankreas ada kelompok sel kecil seperti pulau-pulau. Sel-sel inilah yang membuat insulin untuk mengatur gula darah.", 10),
                },
                steps = new[] { "Saat makanan masuk usus, pankreas mengeluarkan cairan pencernaan.", "Cairan itu memecah makanan menjadi bagian sangat kecil.", "Pankreas juga membuat insulin.", "Insulin membantu gula dari makanan masuk ke sel tubuh sebagai tenaga." },
                facts = new[] { "Pankreas bekerja dua kali: membantu mencerna makanan dan menjaga gula darahmu tetap seimbang!", "Pankreas membuat sekitar 1 liter cairan pencernaan setiap hari.", "Pankreas orang dewasa panjangnya sekitar 15 cm, kira-kira sepanjang pensil.", "Pankreas tersembunyi di belakang lambung." },
                tips = new[] { "Kurangi makanan dan minuman terlalu manis.", "Rajin bergerak dan berolahraga.", "Makan sayur, buah, dan biji-bijian." },
            },
            ["paru"] = new OrganContentData
            {
                parts = new[]
                {
                    P("hidung", "Hidung", "Pintu masuk udara", "Udara masuk lewat lubang hidung. Rambut halus dan lendir di dalam hidung menyaring debu, lalu udara dihangatkan dan dilembapkan sebelum masuk lebih dalam.", new[] { 5 }),
                    P("faring", "Faring (tekak)", "Persimpangan jalan udara & makanan", "Faring ada di belakang hidung dan mulut. Udara dan makanan sama-sama lewat sini, lalu berpisah: udara ke laring, makanan ke kerongkongan.", new[] { 6 }),
                    P("laring", "Laring (kotak suara)", "Tempat suara dibuat", "Laring ada di leher bagian atas. Di dalamnya ada pita suara yang bergetar saat kamu berbicara atau bernyanyi. Di atasnya ada katup kecil (epiglotis) yang menutup saat kamu menelan.", new[] { 3 }),
                    P("kerongkongan", "Kerongkongan", "Jalan makanan di belakang trakea", "Kerongkongan bukan jalan udara, tetapi letaknya tepat di belakang batang tenggorok. Saat kamu menelan, epiglotis menutup laring supaya makanan masuk ke kerongkongan, bukan ke paru-paru.", new[] { 7 }),
                    P("trakea", "Trakea (batang tenggorok)", "Pipa udara dari leher ke dada", "Trakea adalah pipa berlapis cincin tulang rawan supaya tetap terbuka. Udara lewat sini menuju paru-paru.", new[] { 1, 2 }, null, new[] { 1, 2, 9 }),
                    P("bronkus", "Bronkus", "Cabang udara ke paru kanan & kiri", "Di dada, trakea bercabang dua menjadi bronkus. Satu ke paru-paru kanan, satu ke paru-paru kiri. Bronkus lalu bercabang terus semakin kecil seperti ranting pohon.", new[] { 4 }, null, new[] { 4, 9 }),
                    P("lobus", "Lobus paru-paru", "Ruang-ruang besar paru-paru", "Paru-paru kanan punya 3 lobus, paru-paru kiri punya 2 lobus supaya ada tempat untuk jantung.", new[] { 0 }, null, new[] { 8 }),
                    P("alveolus", "Alveolus", "Kantong udara tempat tukar oksigen", "Di ujung cabang terkecil (bronkiolus) ada kantong udara mungil seperti buah anggur, namanya alveolus. Di sini oksigen masuk ke darah dan karbon dioksida keluar dari darah.", new int[0], "alveolus"),
                },
                steps = new[] { "Kamu menarik napas lewat hidung. Hidung menyaring dan menghangatkan udara.", "Udara lewat faring lalu laring.", "Udara turun melewati trakea lalu bronkus.", "Udara sampai ke alveolus, oksigen masuk ke darah.", "Karbon dioksida keluar saat kamu menghembuskan napas." },
                facts = new[] { "Paru-paru kiri sedikit lebih kecil supaya ada tempat untuk jantung!", "Ada ratusan juta alveolus di kedua paru-parumu.", "Kalau semua alveolus dibentangkan, luasnya kira-kira setengah lapangan tenis!", "Kamu bernapas lebih dari 20.000 kali setiap hari.", "Hidungmu bisa mengenali ribuan macam bau!", "Percikan bersin bisa terbang sampai beberapa meter, makanya tutup hidung dan mulut saat bersin!" },
                tips = new[] { "Hindari asap rokok dan asap kendaraan.", "Rajin berolahraga dan bermain di luar.", "Tutup mulut saat batuk atau bersin." },
            },
            ["jantung"] = new OrganContentData
            {
                parts = new[]
                {
                    P("bilik", "Bilik (ventrikel)", "Ruang pemompa darah", "Jantung punya dua bilik di bagian bawah. Bilik kanan (biru) memompa darah ke paru-paru, bilik kiri (merah) memompa darah ke seluruh tubuh. Dinding bilik kiri paling tebal karena harus memompa paling kuat.", new[] { 0 }, null, new[] { 6, 8 }),
                    P("serambi", "Serambi (atrium)", "Ruang penerima darah", "Dua serambi di bagian atas menerima darah. Serambi kanan menerima darah dari tubuh, serambi kiri menerima darah dari paru-paru.", new[] { 1 }, null, new[] { 5, 7 }),
                    In("katup", "Katup jantung", "Pintu satu arah", "Di antara serambi dan bilik ada katup. Katup hanya membuka ke satu arah sehingga darah tidak berbalik. Bunyi deg-deg jantung berasal dari katup yang menutup.", 10),
                    P("aorta", "Aorta", "Pembuluh darah terbesar", "Aorta membawa darah kaya oksigen dari jantung ke seluruh tubuh. Cabang-cabangnya menuju kepala dan lengan.", new[] { 2 }),
                    P("vena", "Vena cava", "Membawa darah kembali ke jantung", "Vena cava adalah pembuluh besar yang membawa darah dari seluruh tubuh kembali ke serambi kanan.", new[] { 3 }),
                    P("pulmonalis", "Arteri paru-paru", "Membawa darah ke paru-paru", "Pembuluh ini membawa darah ke paru-paru supaya darah mendapat oksigen baru.", new[] { 4 }),
                    P("seldarah", "Sel darah merah", "Membawa oksigen ke seluruh tubuh", "Sel darah merah berbentuk seperti donat pipih. Warnanya merah karena berisi hemoglobin yang mengikat oksigen di paru-paru, lalu mengantarkannya ke seluruh tubuh. Di dalam darah juga ada sel darah putih (pelawan kuman) dan keping darah (menutup luka).", new int[0], "seldarah"),
                },
                steps = new[] { "Sel darah merah membawa oksigen ke seluruh tubuh, lalu kembali membawa karbon dioksida.", "Darah dari tubuh masuk ke serambi kanan.", "Bilik kanan memompa darah ke paru-paru untuk mengambil oksigen.", "Darah kaya oksigen kembali ke serambi kiri.", "Bilik kiri memompa darah lewat aorta ke seluruh tubuh." },
                facts = new[] { "Jantungmu berdetak sekitar seratus ribu kali setiap hari!", "Jantung sebesar kepalan tanganmu sendiri.", "Bunyi 'deg-deg' jantung berasal dari katup jantung yang menutup.", "Jantung terus bekerja bahkan saat kamu tidur.", "Setetes kecil darah berisi ratusan juta sel darah merah!", "Darah bisa berkeliling seluruh tubuhmu dalam waktu sekitar 1 menit.", "Tubuhmu membuat jutaan sel darah merah baru setiap detik di dalam sumsum tulang." },
                tips = new[] { "Rajin bergerak, berlari, dan bermain.", "Makan buah dan sayur, kurangi gorengan.", "Tidur cukup setiap malam." },
            },
            ["ginjal"] = new OrganContentData
            {
                parts = new[]
                {
                    P("ginjal", "Ginjal", "Penyaring darah", "Ginjal berbentuk seperti kacang merah. Bagian luarnya penuh penyaring kecil yang membersihkan darah.", new[] { 0 }),
                    P("arteri", "Arteri (pembuluh nadi)", "Membawa darah yang perlu disaring", "Arteri dari aorta membawa darah yang masih mengandung sisa zat masuk ke ginjal.", new[] { 1 }),
                    P("vena", "Vena (pembuluh balik)", "Membawa darah bersih keluar", "Setelah disaring, darah bersih keluar lewat vena dan kembali ke jantung.", new[] { 2 }),
                    P("ureter", "Ureter", "Saluran urine ke kandung kemih", "Ureter adalah saluran kecil yang mengalirkan urine dari ginjal ke kandung kemih.", new[] { 3 }),
                    P("adrenal", "Kelenjar adrenal", "Topi kecil penghasil hormon", "Kelenjar kecil di atas tiap ginjal. Ia membuat hormon yang membantu tubuh saat kaget atau takut.", new[] { 4 }),
                    In("korteks", "Korteks (kulit ginjal)", "Lapisan luar penyaring", "Lapisan luar ginjal disebut korteks. Di sinilah banyak nefron menyaring darah.", 5),
                    In("medula", "Medula (piramida)", "Pengumpul urine", "Di bagian tengah ada piramida-piramida kecil. Urine dari nefron dikumpulkan dan dialirkan lewat piramida ini.", 6),
                    In("pelvis", "Pelvis ginjal", "Corong menuju ureter", "Pelvis adalah rongga berbentuk corong yang menampung urine sebelum mengalir ke ureter.", 7),
                    P("nefron", "Nefron", "Saringan super kecil", "Di dalam setiap ginjal ada sekitar satu juta nefron. Nefron menyaring darah, menyimpan yang berguna, dan membuang sisanya menjadi urine.", new int[0], "nefron"),
                },
                steps = new[] { "Darah masuk ke ginjal lewat arteri.", "Nefron menyaring darah dan memisahkan sisa.", "Darah bersih keluar lewat vena.", "Sisa dan air menjadi urine, lalu mengalir lewat ureter." },
                facts = new[] { "Ginjal orang dewasa menyaring sekitar 180 liter cairan darah setiap hari, kira-kira sebanyak satu bak mandi!", "Ginjal kanan sedikit lebih rendah karena ada hati di atasnya.", "Setiap ginjal punya sekitar satu juta nefron.", "Kamu bisa hidup sehat dengan satu ginjal saja." },
                tips = new[] { "Minum air putih yang cukup setiap hari.", "Jangan menahan buang air kecil.", "Kurangi makanan terlalu asin." },
            },
            ["otak"] = new OrganContentData
            {
                parts = new[]
                {
                    P("depan", "Lobus depan", "Berpikir & merencanakan", "Bagian depan otak membantumu berpikir, memecahkan masalah, mengatur perasaan, dan menggerakkan tubuh.", new[] { 0 }),
                    P("atas", "Lobus atas (ubun-ubun)", "Merasakan sentuhan", "Bagian atas otak menerima rasa sentuhan, panas, dingin, dan sakit dari kulitmu.", new[] { 1 }),
                    P("samping", "Lobus samping", "Mendengar & mengingat", "Bagian samping otak membantumu mendengar suara, memahami kata, dan mengingat.", new[] { 2 }),
                    P("belakang", "Lobus belakang", "Melihat", "Bagian belakang otak mengolah apa yang dilihat matamu menjadi gambar yang kamu pahami.", new[] { 3 }),
                    P("kecil", "Otak kecil", "Menjaga keseimbangan", "Otak kecil membantumu berjalan, bersepeda, dan menjaga keseimbangan tanpa jatuh.", new[] { 4 }),
                    P("batang", "Batang otak", "Mengatur napas & detak jantung", "Batang otak menyambungkan otak dengan sumsum tulang belakang. Ia mengatur napas dan detak jantung secara otomatis.", new[] { 5 }),
                    In("abu", "Materi abu-abu", "Lapisan luar tempat berpikir", "Lapisan luar otak berwarna lebih gelap disebut materi abu-abu. Di sinilah sel-sel saraf bekerja berpikir dan mengingat.", 6),
                    In("putih", "Materi putih", "Kabel penghubung", "Bagian dalam yang lebih pucat berisi 'kabel' saraf yang menghubungkan bagian-bagian otak.", 7),
                    In("kalosum", "Korpus kalosum", "Jembatan otak kiri & kanan", "Korpus kalosum adalah jembatan lebar yang membuat otak kiri dan kanan bisa saling mengobrol.", 8),
                    P("neuron", "Sel saraf (neuron)", "Pengantar pesan listrik", "Otak tersusun dari miliaran sel saraf. Sel-sel ini saling mengirim pesan listrik yang sangat cepat.", new int[0], "neuron"),
                },
                steps = new[] { "Mata, telinga, dan kulit mengirim pesan ke otak.", "Sel saraf meneruskan pesan dengan sangat cepat.", "Otak memahami pesan dan memutuskan apa yang harus dilakukan.", "Otak mengirim perintah ke otot, lalu tubuhmu bergerak." },
                facts = new[] { "Pesan saraf tercepat bisa melesat sekitar 400 km per jam, lebih cepat dari mobil balap!", "Otak memakai sekitar seperlima tenaga tubuhmu.", "Otak tersusun dari sekitar 86 miliar sel saraf.", "Saat tidur, otak tetap bekerja dan menyimpan ingatan." },
                tips = new[] { "Tidur cukup supaya otak bisa beristirahat.", "Pakai helm saat bersepeda.", "Rajin membaca dan bermain teka-teki." },
            },
            ["mata"] = new OrganContentData
            {
                parts = new[]
                {
                    P("kornea", "Kornea", "Jendela bening di depan mata", "Kornea adalah lapisan bening dan melengkung di bagian paling depan mata. Cahaya pertama kali masuk lewat sini, dan kornea ikut memfokuskannya.", new[] { 1 }, null, new[] { 1, 9 }),
                    P("iris", "Iris", "Bagian berwarna pengatur cahaya", "Iris adalah bagian mata yang berwarna (misalnya cokelat, hitam, hijau, atau biru). Otot di iris membuat pupil membesar atau mengecil.", new[] { 2 }, null, new[] { 2 }),
                    P("pupil", "Pupil", "Lubang masuk cahaya", "Pupil adalah lubang hitam di tengah iris. Saat terang pupil mengecil, saat gelap pupil membesar.", new[] { 3 }),
                    P("sklera", "Bagian putih mata", "Pelindung bola mata", "Bagian putih yang keras ini (sklera) membungkus dan melindungi bola mata.", new[] { 0 }, null, new[] { 0 }),
                    In("lensa", "Lensa", "Memfokuskan cahaya", "Lensa ada di belakang iris. Lensa bening dan bisa menebal atau menipis untuk memfokuskan cahaya ke retina, seperti lensa kamera.", 6),
                    In("badankaca", "Badan kaca", "Jeli pengisi bola mata", "Bagian dalam bola mata berisi jeli bening yang menjaga bentuk mata tetap bulat.", 8),
                    In("retina", "Retina", "Layar penangkap cahaya", "Retina ada di bagian paling belakang mata. Retina punya jutaan sel peka cahaya yang mengubah cahaya menjadi pesan untuk otak.", 7),
                    P("saraf", "Saraf mata", "Mengirim gambar ke otak", "Saraf mata seperti kabel yang membawa pesan dari retina ke otak. Otaklah yang akhirnya 'melihat' gambar itu.", new[] { 5 }, null, new[] { 5 }),
                    P("otot", "Otot mata", "Menggerakkan bola mata", "Ada otot-otot kecil yang menempel di bola mata. Mereka membuat matamu bisa melirik ke atas, bawah, kiri, dan kanan.", new[] { 4 }),
                },
                steps = new[] { "Cahaya masuk lewat kornea yang bening.", "Cahaya melewati pupil, lubang di tengah iris.", "Lensa memfokuskan cahaya ke retina.", "Retina mengubah cahaya jadi pesan, lalu saraf mata mengirimnya ke otak." },
                facts = new[] { "Pupilmu mengecil saat terang dan membesar saat gelap supaya cahaya yang masuk pas!", "Gambar di retina sebenarnya terbalik, lalu otakmu membaliknya kembali.", "Kamu berkedip sekitar 15–20 kali setiap menit.", "Mata manusia bisa membedakan jutaan warna." },
                tips = new[] { "Istirahatkan mata setiap 20 menit saat menatap layar.", "Membaca di tempat yang cukup terang.", "Jangan menatap matahari secara langsung.", "Makan wortel dan sayuran hijau." },
            },
            ["hidung"] = new OrganContentData
            {
                parts = new[]
                {
                    P("luar", "Hidung luar", "Pintu masuk udara", "Bagian hidung yang kamu lihat di wajah. Di bawahnya ada dua lubang hidung tempat udara masuk.", new[] { 0 }),
                    P("bulu", "Bulu hidung", "Penyaring debu", "Bulu-bulu halus di dalam lubang hidung menangkap debu dan kotoran supaya tidak masuk ke paru-paru.", new[] { 5 }),
                    P("rongga", "Rongga hidung", "Ruang penghangat udara", "Di balik hidung ada rongga yang dilapisi lendir. Lendir menangkap kuman, dan udara dihangatkan serta dilembapkan di sini.", new[] { 1 }, null, new[] { 7 }),
                    P("konka", "Konka", "Rak berlekuk di dinding rongga", "Konka adalah tiga 'rak' berlekuk di dinding rongga hidung. Bentuknya membuat udara berputar sehingga lebih hangat, lembap, dan bersih.", new[] { 2 }, null, new[] { 2 }),
                    P("sekat", "Sekat hidung", "Pembagi rongga kiri & kanan", "Dinding tipis di tengah hidung membagi rongga hidung menjadi kiri dan kanan.", new[] { 3 }),
                    P("penciuman", "Daerah penciuman", "Pencium bau", "Di atap rongga hidung ada jutaan sel pencium. Saat menangkap bau, mereka mengirim pesan lewat saraf penciuman ke otak.", new[] { 4 }, null, new[] { 4 }),
                    P("nasofaring", "Jalan ke tenggorokan", "Udara turun ke tenggorokan", "Di belakang rongga hidung, udara berbelok turun ke tenggorokan, lalu menuju paru-paru.", new[] { 6 }, null, new[] { 6 }),
                },
                steps = new[] { "Udara masuk lewat lubang hidung.", "Bulu hidung dan lendir menangkap debu dan kuman.", "Konka menghangatkan dan melembapkan udara.", "Sel pencium di atap rongga hidung mengenali bau.", "Udara turun ke tenggorokan menuju paru-paru." },
                facts = new[] { "Hidungmu bisa mengenali ribuan macam bau!", "Ada sekitar 10 juta sel pencium di dalam hidungmu.", "Bau dan rasa saling membantu: saat pilek, makanan terasa hambar.", "Percikan bersin bisa terbang sampai beberapa meter, makanya tutup hidung dan mulut saat bersin!" },
                tips = new[] { "Bernapaslah lewat hidung, bukan mulut.", "Jangan mengorek hidung dengan jari.", "Tutup hidung dan mulut saat bersin.", "Pakai masker di tempat berdebu." },
            },
            ["tenggorokan"] = new OrganContentData
            {
                parts = new[]
                {
                    P("faring", "Faring", "Persimpangan udara & makanan", "Faring adalah ruang di belakang hidung dan mulut. Udara dan makanan sama-sama lewat sini sebelum berpisah jalan.", new[] { 0 }),
                    P("amandel", "Amandel", "Penjaga dari kuman", "Amandel ada di kiri dan kanan tenggorokan. Tugasnya membantu melawan kuman yang masuk lewat mulut.", new[] { 6 }),
                    P("epiglotis", "Epiglotis", "Tutup saat menelan", "Epiglotis adalah tutup kecil seperti daun. Saat kamu menelan, epiglotis menutup jalan napas supaya makanan masuk ke kerongkongan.", new[] { 1 }, null, new[] { 1 }),
                    P("laring", "Laring (kotak suara)", "Tempat suara dibuat", "Laring adalah pintu masuk ke batang tenggorok. Bagian depannya bisa terasa di leher sebagai jakun.", new[] { 2 }, null, new[] { 2 }),
                    In("pitasuara", "Pita suara", "Bergetar saat bicara", "Di dalam laring ada dua lipatan bernama pita suara. Saat bernapas pita suara terbuka, saat bicara pita suara merapat dan bergetar sehingga muncul suara.", 7),
                    P("trakea", "Trakea", "Jalan udara ke paru-paru", "Trakea atau batang tenggorok adalah pipa bercincin yang membawa udara ke paru-paru.", new[] { 3, 4 }, null, new[] { 4, 8 }),
                    P("kerongkongan", "Kerongkongan", "Jalan makanan ke lambung", "Kerongkongan ada tepat di belakang trakea dan membawa makanan ke lambung.", new[] { 5 }, null, new[] { 5, 9 }),
                },
                steps = new[] { "Udara dan makanan masuk ke faring.", "Saat bernapas, udara turun lewat laring ke trakea.", "Saat menelan, epiglotis menutup laring.", "Makanan masuk ke kerongkongan menuju lambung." },
                facts = new[] { "Suaramu berasal dari pita suara yang bergetar ratusan kali setiap detik!", "Kamu tidak bisa bernapas dan menelan pada saat yang sama.", "Pita suara anak laki-laki memanjang saat besar, makanya suaranya jadi lebih berat.", "Tersedak terjadi kalau makanan salah masuk ke jalan napas." },
                tips = new[] { "Jangan berbicara atau tertawa saat mulut penuh makanan.", "Minum air putih supaya tenggorokan tidak kering.", "Jangan berteriak terlalu lama.", "Kunyah makanan sampai halus sebelum ditelan." },
            },

            ["rangka"] = new OrganContentData
            {
                // material model rangka: 0 tengkorak, 1 gigi, 2 tulang belakang, 3 tulang rawan, 4 tulang rusuk, 5 tulang dada, 6 bahu, 7 lengan atas,
                // 8 lengan bawah, 9 tangan, 10 panggul, 11 tulang paha, 12 tempurung lutut, 13 tulang kering & betis, 14 kaki
                // model "Dalam" (tiga jenis tulang dibelah, materi IPA kelas 5 SD): tulang pipa (paha) 0 selaput tulang, 1 tulang rawan sendi,
                // 2 tulang keras, 3 tulang spons, 4 sumsum merah (dipakai ketiganya), 5 sumsum kuning; tulang pipih 6 luar, 7 tulang keras,
                // 8 tulang spons; tulang pendek 9 luar, 10 tulang keras, 11 tulang spons
                parts = new[]
                {
                    P("tengkorak", "Tengkorak", "Pelindung otak", "Tengkorak tersusun dari banyak tulang yang menyatu seperti helm. Bagian atasnya melindungi otak, bagian depannya membentuk wajah: rongga mata, hidung, dan rahang tempat gigi tertanam. Tulang tengkorak termasuk tulang pipih.", new[] { 0, 1 }, null, new[] { 6, 7, 8 }),
                    P("belakang", "Tulang belakang", "Penegak tubuh & pelindung saraf", "Tulang belakang tersusun dari ruas-ruas yang bertumpuk, dari leher, punggung, pinggang, sampai tulang kelangkang dan tulang ekor (33 ruas; sebagian menyatu saat dewasa). Di dalamnya ada sumsum tulang belakang, yaitu saraf yang terlindung. Bantalan tulang rawan di antara ruas membuat punggung bisa membungkuk dan memutar. Ruas tulang belakang termasuk tulang pendek.", new[] { 2 }, null, new[] { 9, 10, 11 }),
                    P("rusuk", "Tulang rusuk", "Pelindung jantung & paru-paru", "Ada 12 pasang tulang rusuk yang membentuk sangkar dada. Tujuh pasang atas tersambung ke tulang dada, tiga pasang berikutnya tersambung ke tulang rawan di atasnya, dan dua pasang terbawah 'melayang'. Sangkar ini ikut naik-turun saat kamu bernapas. Tulang rusuk termasuk tulang pipih.", new[] { 4 }, null, new[] { 6, 7, 8 }),
                    P("dada", "Tulang dada", "Tempat tulang rusuk bertemu", "Tulang dada berbentuk pipih seperti dasi di tengah dada. Tulang rusuk tersambung ke tulang dada lewat tulang rawan yang lentur.", new[] { 5 }, null, new[] { 6, 7, 8 }),
                    P("bahu", "Tulang selangka & belikat", "Membentuk bahu", "Tulang selangka melintang di depan bahu, tulang belikat berbentuk segitiga di punggung. Keduanya menjadi tempat lengan tersambung ke tubuh. Tulang belikat termasuk tulang pipih.", new[] { 6 }, null, new[] { 6, 7, 8 }),
                    P("lenganatas", "Tulang lengan atas", "Dari bahu sampai siku", "Tulang lengan atas tersambung ke bahu lewat sendi peluru, jadi lenganmu bisa berputar ke banyak arah. Di siku ia bertemu tulang lengan bawah lewat sendi engsel. Tulang lengan atas termasuk tulang pipa.", new[] { 7 }, null, new[] { 0, 2, 3, 4, 5 }),
                    P("lenganbawah", "Tulang hasta & pengumpil", "Dua tulang lengan bawah", "Lengan bawah punya dua tulang: hasta (di sisi kelingking) dan pengumpil (di sisi ibu jari). Karena pengumpil bisa memutar di sekitar hasta, kamu bisa membalik telapak tangan. Keduanya termasuk tulang pipa.", new[] { 8 }, null, new[] { 0, 2, 3, 4, 5 }),
                    P("tangan", "Tulang tangan", "Banyak tulang kecil", "Setiap tangan punya 27 tulang: 8 tulang pergelangan, 5 tulang telapak, dan 14 ruas jari. Banyaknya tulang kecil membuat tanganmu bisa menulis, menggambar, dan menggenggam dengan rapi. Tulang pergelangan tangan termasuk tulang pendek.", new[] { 9 }, null, new[] { 9, 10, 11 }),
                    P("panggul", "Tulang panggul", "Penopang tubuh bagian atas", "Tulang panggul berbentuk seperti mangkuk. Ia menopang tubuh bagian atas, menjadi tempat tulang paha tersambung, dan melindungi kandung kemih serta organ di perut bagian bawah.", new[] { 10 }),
                    P("paha", "Tulang paha", "Tulang terpanjang & terkuat", "Tulang paha adalah tulang terpanjang dan terkuat di tubuhmu. Kepalanya yang bulat masuk ke mangkuk tulang panggul membentuk sendi peluru. Tulang paha termasuk tulang pipa; lihat tampilan \"Dalam\" untuk melihat isinya.", new[] { 11 }, null, new[] { 0, 1, 2, 3, 4, 5 }),
                    P("lutut", "Tempurung lutut", "Pelindung sendi lutut", "Tempurung lutut adalah tulang kecil berbentuk bundar di depan lutut. Ia melindungi sendi lutut dan membantu otot paha meluruskan kaki.", new[] { 12 }),
                    P("kering", "Tulang kering & betis", "Penopang kaki bagian bawah", "Kaki bagian bawah punya dua tulang: tulang kering yang besar di depan (bisa kamu raba di bawah lutut) dan tulang betis yang ramping di samping luar. Keduanya termasuk tulang pipa.", new[] { 13 }, null, new[] { 0, 2, 3, 4, 5 }),
                    P("kaki", "Tulang kaki", "Menopang & menyeimbangkan", "Setiap kaki punya 26 tulang yang membentuk lengkungan. Lengkungan ini membuatmu bisa berdiri, berjalan, dan melompat dengan seimbang. Tulang pergelangan kaki termasuk tulang pendek.", new[] { 14 }, null, new[] { 9, 10, 11 }),
                    P("sendi", "Sendi & tulang rawan", "Tempat tulang bertemu", "Sendi adalah tempat dua tulang bertemu, misalnya di lutut, siku, dan bahu. Ujung tulang dilapisi tulang rawan yang licin, dan di antara ruas tulang belakang ada bantalan tulang rawan supaya tulang tidak saling bergesekan.", new[] { 3 }, null, new[] { 1 }),
                    In("keras", "Tulang keras (kompak)", "Lapisan luar yang padat", "Bagian luar tulang padat dan keras karena mengandung banyak kalsium. Lapisan inilah yang membuat tulang kuat menahan berat tubuhmu.", 2, 7, 10),
                    In("spons", "Tulang spons", "Berongga, ringan tapi kuat", "Di ujung-ujung tulang ada tulang spons yang penuh rongga kecil seperti spons. Bentuknya membuat tulang ringan tetapi tetap kuat.", 3, 8, 11),
                    In("pipa", "Tulang pipa", "Panjang & berongga", "Tulang pipa berbentuk panjang seperti pipa, misalnya tulang paha, tulang lengan, dan tulang kering. Batangnya berongga dan berisi sumsum kuning, ujung-ujungnya berisi tulang spons dan sumsum merah.", 0, 2, 3, 4, 5),
                    In("pipih", "Tulang pipih", "Lebar & tipis", "Tulang pipih berbentuk lebar dan tipis, misalnya tengkorak, tulang rusuk, tulang dada, dan tulang belikat. Isinya seperti roti lapis: dua lapis tulang keras mengapit tulang spons dan sumsum merah. Tulang pipih melindungi organ di baliknya.", 6, 7, 8),
                    In("pendek", "Tulang pendek", "Kecil, hampir seperti kotak", "Tulang pendek berukuran kecil dan bentuknya hampir seperti kotak, misalnya tulang pergelangan tangan, pergelangan kaki, dan ruas tulang belakang. Lapisan tulang kerasnya tipis dan isinya hampir semuanya tulang spons, jadi ringan tetapi kuat menahan beban.", 9, 10, 11),
                    In("sumsummerah", "Sumsum merah", "Pabrik sel darah", "Sumsum merah mengisi rongga tulang spons. Di sinilah sel darah merah, sel darah putih, dan keping darah dibuat setiap hari.", 4),
                    In("sumsumkuning", "Sumsum kuning", "Simpanan lemak", "Rongga di tengah batang tulang berisi sumsum kuning yang sebagian besar berupa lemak sebagai cadangan tenaga.", 5),
                },
                steps = new[] { "Rangka memberi bentuk tubuh dan membuatmu bisa berdiri tegak.", "Tengkorak, tulang rusuk, dan tulang panggul melindungi organ di dalamnya.", "Otot menempel pada tulang. Saat otot menarik, tulang bergerak di sendinya. Itu sebabnya tulang disebut alat gerak pasif.", "Sumsum merah di dalam tulang membuat sel-sel darah.", "Tulang menyimpan kalsium supaya tetap keras dan kuat." },
                facts = new[] { "Bayi lahir dengan sekitar 270–300 tulang, tetapi orang dewasa hanya punya 206 karena sebagian tulang menyatu.", "Tulang paha adalah tulang terpanjang dan terkuat di tubuh manusia.", "Tulang terkecil ada di dalam telinga, namanya tulang sanggurdi, panjangnya hanya sekitar 3 milimeter!", "Lebih dari separuh tulang tubuhmu ada di tangan dan kaki.", "Tulang terus diperbarui; kira-kira setiap 10 tahun seluruh rangka orang dewasa sudah berganti dengan tulang baru." },
                tips = new[] { "Minum susu, makan ikan, tahu, tempe, dan sayuran hijau supaya tulang mendapat kalsium.", "Bermain di bawah sinar matahari pagi membantu tubuh membuat vitamin D untuk tulang.", "Duduklah tegak dan bawa tas ransel dengan dua tali di kedua bahu.", "Rajin berolahraga, lompat tali, dan berlari supaya tulang makin kuat.", "Pakai helm dan pelindung lutut saat bersepeda atau bermain sepatu roda." },
            },
            ["reprolaki"] = new OrganContentData
            {
                // material model: 0 testis, 1 epididimis, 2 skrotum, 3 saluran sperma, 4 vesikula seminalis, 5 prostat, 6 uretra, 7 batang penis,
                // 8 kandung kemih, 9 ureter, 10 kepala penis, 11-12 pembuluh darah tali sperma;
                // potongan: 13 tubulus, 14 rongga kandung kemih, 15 jaringan berongga, 16-17 skrotum, 18 selaput & sekat testis, 19 jaringan sekitar uretra
                parts = new[]
                {
                    P("testis", "Testis (buah zakar)", "Membuat sel sperma & hormon", "Ada dua testis berbentuk lonjong, dibungkus selaput putih yang kuat. Mulai pubertas, testis membuat jutaan sel sperma setiap hari dan hormon testosteron. Hormon inilah yang membuat suara memberat, otot makin kuat, dan tumbuh rambut baru.", new[] { 0 }, null, new[] { 0, 13, 18 }),
                    P("skrotum", "Skrotum (kantong zakar)", "Membungkus & menjaga suhu testis", "Skrotum adalah kantong kulit yang membungkus testis di luar tubuh. Saat dingin skrotum mengerut mendekat ke tubuh, saat panas mengendur, supaya testis tetap sedikit lebih dingin dari suhu tubuh.", new[] { 2 }, null, new[] { 16, 17 }),
                    P("epididimis", "Epididimis", "Tempat sperma menjadi matang", "Epididimis adalah saluran sangat panjang yang tergulung rapat. Kepalanya ada di atas testis, badannya di belakang, dan ekornya di bawah. Sperma tinggal di sini sekitar dua minggu sampai matang dan bisa berenang.", new[] { 1 }, null, new[] { 1 }),
                    P("vasdeferens", "Saluran sperma (vas deferens)", "Mengantar sperma ke uretra", "Saluran sperma naik dari epididimis, masuk ke panggul, melingkari kandung kemih, lalu melebar (ampula) di belakangnya sebelum bergabung dengan saluran dari vesikula seminalis di dalam prostat.", new[] { 3 }, null, new[] { 3 }),
                    P("tali", "Tali sperma", "Saluran sperma & pembuluh darah testis", "Saluran sperma naik dari skrotum bersama pembuluh darah dan saraf dalam satu ikatan yang disebut tali sperma. Pembuluh nadi (merah) membawa oksigen dan makanan untuk testis, pembuluh balik (biru) membawa darah kembali.", new[] { 11, 12 }, null, new[] { 11, 12 }),
                    P("vesikula", "Vesikula seminalis", "Membuat cairan bertenaga untuk sperma", "Sepasang kelenjar berlekuk-lekuk di belakang kandung kemih. Cairannya mengandung gula yang memberi tenaga bagi sperma untuk berenang.", new[] { 4 }, null, new[] { 4 }),
                    P("prostat", "Kelenjar prostat", "Membuat cairan pelindung sperma", "Pada orang dewasa, prostat kira-kira sebesar buah kenari dan mengelilingi uretra tepat di bawah kandung kemih. Cairannya bercampur dengan sperma membentuk cairan mani (semen).", new[] { 5 }, null, new[] { 5 }),
                    P("uretra", "Uretra", "Saluran keluar urine & sperma", "Uretra berawal dari kandung kemih, melewati prostat, lalu memanjang di dalam penis sampai lubang di ujungnya. Uretra dilewati urine, dan setelah pubertas juga sperma, tetapi tidak pada saat yang sama karena ada otot yang mengatur jalannya.", new[] { 6 }, null, new[] { 6, 19 }),
                    P("penis", "Penis", "Jalan keluar urine", "Batang penis berisi uretra dan jaringan lunak berongga. Ujungnya disebut kepala penis; di sanalah lubang uretra tempat urine keluar. Penis termasuk bagian tubuh pribadi, jadi jaga kebersihannya saat mandi.", new[] { 7, 10 }, null, new[] { 7, 15, 19 }),
                    P("kandungkemih", "Kandung kemih", "Penampung urine (bukan organ reproduksi)", "Kandung kemih menampung urine dari ginjal yang turun lewat ureter. Ia bukan organ reproduksi, tetapi letaknya bersebelahan dan berbagi saluran uretra.", new[] { 8, 9 }, null, new[] { 8, 14, 9 }),
                    In("tubulus", "Tubulus seminiferus", "Pabrik sel sperma", "Bagian dalam testis terbagi menjadi ratusan ruang kecil. Setiap ruang berisi saluran yang bergulung-gulung, dan di dinding saluran inilah sel sperma dibuat.", 13),
                    P("sperma", "Sel sperma", "Sel kecil yang bisa berenang", "Sel sperma punya kepala berisi bahan pembawa sifat (misalnya warna mata), bagian tengah penghasil tenaga, dan ekor panjang untuk berenang. Ukurannya sangat kecil, hanya bisa dilihat dengan mikroskop.", new int[0], "sperma"),
                },
                steps = new[] { "Mulai pubertas, testis membuat sel sperma dan hormon testosteron.", "Sperma pindah ke epididimis dan menjadi matang.", "Saluran sperma mengantar sperma ke dekat kelenjar prostat.", "Vesikula seminalis dan prostat menambahkan cairan sehingga terbentuk cairan mani.", "Uretra di dalam penis menjadi jalan keluar urine, dan setelah pubertas juga sperma, tetapi tidak bersamaan." },
                facts = new[] { "Testis berada di luar perut karena sel sperma butuh suhu sedikit lebih dingin daripada suhu tubuh!", "Sel sperma termasuk sel terkecil di tubuh, panjangnya hanya sekitar 0,05 milimeter.", "Epididimis yang tergulung rapat panjangnya sekitar 6 meter kalau diluruskan!", "Hormon testosteron dari testis membuat suara anak laki-laki memberat saat pubertas.", "Pubertas anak laki-laki biasanya mulai antara umur 9 dan 14 tahun, dan waktunya berbeda-beda untuk setiap anak." },
                tips = new[] { "Mandi dua kali sehari dan bersihkan bagian pribadimu dengan air bersih.", "Ganti celana dalam setiap hari dan pilih yang nyaman, tidak terlalu ketat.", "Organ reproduksi adalah bagian tubuh pribadi. Tidak ada yang boleh menyentuh atau melihatnya, kecuali orang tua atau dokter untuk menjaga kesehatanmu. Kalau ada yang membuatmu tidak nyaman, katakan TIDAK, pergi, lalu cerita ke orang dewasa yang kamu percaya.", "Kalau bagian pribadimu terasa sakit atau terbentur, segera beri tahu orang tua." },
            },
            ["reproperempuan"] = new OrganContentData
            {
                // material model: 0 rahim, 1 leher rahim, 2 tuba falopi, 3 fimbria, 4 ovarium, 5 vagina, 6 ligamen;
                // potongan: 7 endometrium, 8 otot rahim, 9 folikel, 10 sel telur
                parts = new[]
                {
                    P("ovarium", "Ovarium (indung telur)", "Menyimpan sel telur & membuat hormon", "Ada dua ovarium di kiri dan kanan rahim, pada orang dewasa kira-kira sebesar buah kenari. Di dalamnya tersimpan calon sel telur. Ovarium juga membuat hormon estrogen yang mengatur perubahan tubuh saat pubertas.", new[] { 4 }, null, new[] { 4, 9, 10 }),
                    P("tuba", "Tuba falopi (saluran telur)", "Jalan sel telur menuju rahim", "Dua saluran tipis yang menghubungkan daerah ovarium dengan rahim. Sel telur berjalan pelan di dalamnya, didorong rambut-rambut halus di dinding saluran.", new[] { 2 }, null, new[] { 2 }),
                    P("fimbria", "Fimbria", "Jari-jari penangkap sel telur", "Ujung tuba falopi melebar seperti corong dengan jari-jari lembut. Saat sel telur keluar dari ovarium, fimbria menangkap dan mengarahkannya masuk ke tuba.", new[] { 3 }, null, new[] { 3 }),
                    P("rahim", "Rahim (uterus)", "Tempat bayi tumbuh", "Rahim berbentuk seperti buah pir terbalik dengan dinding otot yang tebal dan lentur. Selama kehamilan, rahim melar mengikuti bayi yang tumbuh di dalamnya.", new[] { 0 }, null, new[] { 7, 8 }),
                    P("serviks", "Leher rahim (serviks)", "Pintu bawah rahim", "Bagian rahim yang paling bawah dan sempit. Saluran kecil di tengahnya menghubungkan rahim dengan vagina.", new[] { 1 }, null, new[] { 1 }),
                    P("vagina", "Vagina", "Saluran dari rahim ke luar tubuh", "Vagina adalah saluran lentur dari leher rahim ke luar tubuh. Darah haid keluar lewat sini, dan saat melahirkan bayi juga keluar lewat jalan ini.", new[] { 5 }, null, new[] { 5 }),
                    P("ligamen", "Ligamen ovarium", "Tali penahan ovarium", "Tali jaringan yang kuat menahan ovarium tetap di dekat rahim.", new[] { 6 }, null, new[] { 6 }),
                    In("endometrium", "Endometrium (lapisan dalam rahim)", "Lapisan yang menebal tiap bulan", "Lapisan dalam rahim menebal setiap bulan, bersiap seperti kasur empuk. Kalau tidak terjadi kehamilan, lapisan ini luruh dan keluar sebagai darah haid selama beberapa hari.", 7),
                    In("miometrium", "Dinding otot rahim", "Otot kuat yang bisa melar", "Dinding rahim tersusun dari otot tebal. Otot ini bisa melar sangat besar saat hamil dan meremas kuat saat melahirkan.", 8),
                    In("folikel", "Folikel", "Kantong kecil berisi calon sel telur", "Di dalam ovarium ada banyak kantong kecil bernama folikel. Biasanya setiap bulan satu folikel tumbuh paling besar dan melepaskan sel telurnya.", 9, 10),
                    P("seltelur", "Sel telur", "Sel terbesar di tubuh manusia", "Sel telur berbentuk bulat dan dibungkus lapisan pelindung. Ukurannya sekitar 0,1 milimeter, kira-kira seukuran titik kecil. Inti selnya berisi bahan pembawa sifat dari ibu.", new int[0], "seltelur"),
                },
                steps = new[] { "Mulai pubertas, ovarium mematangkan satu sel telur kira-kira sebulan sekali.", "Fimbria menangkap sel telur, lalu sel telur berjalan pelan di tuba falopi.", "Sementara itu, dinding rahim menebal untuk bersiap.", "Kalau tidak terjadi kehamilan, lapisan dinding rahim luruh dan keluar lewat vagina sebagai darah haid.", "Siklus ini berulang kira-kira setiap 21–35 hari. Di awal pubertas, haid sering belum teratur, dan itu normal." },
                facts = new[] { "Saat lahir, bayi perempuan sudah memiliki sekitar satu juta calon sel telur di indung telurnya!", "Sel telur adalah sel terbesar di tubuh manusia, tetapi tetap sangat kecil, seukuran titik.", "Rahim kira-kira sebesar buah pir, tetapi saat hamil bisa melar sampai sebesar semangka!", "Haid pertama biasanya datang antara umur 9 dan 15 tahun, dan waktunya berbeda untuk setiap anak.", "Rata-rata satu siklus haid berlangsung sekitar 28 hari." },
                tips = new[] { "Mandi dua kali sehari dan bersihkan bagian pribadimu dari depan ke belakang supaya kuman tidak terbawa.", "Ganti celana dalam setiap hari, pilih yang berbahan katun supaya menyerap keringat.", "Saat haid, ganti pembalut setiap 4–6 jam atau saat sudah terasa penuh.", "Organ reproduksi adalah bagian tubuh pribadi. Tidak ada yang boleh menyentuh atau melihatnya, kecuali orang tua atau dokter untuk menjaga kesehatanmu. Kalau ada yang membuatmu tidak nyaman, katakan TIDAK, pergi, lalu cerita ke orang dewasa yang kamu percaya." },
            },
        };

        public static OrganContentData Get(string organId) =>
            organId != null && s_Data.TryGetValue(organId, out var d) ? d : null;
    }
}
