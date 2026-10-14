using System.Collections.Generic;
using UnityEngine;

namespace Organo.Data
{
    /// <summary>Konten bawaan dari desain Organo. Dipakai untuk membuat asset OrganInfo pertama kali.</summary>
    public static class OrganDefaults
    {
        public static readonly string[] Order = { "mulut", "kerongkongan", "lambung", "usushalus", "ususbesar", "paru", "jantung", "ginjal", "hati", "pankreas", "otak", "tenggorokan", "hidung", "mata", "rangka", "reprolaki", "reproperempuan" };

        /// <summary>
        /// Letak & ukuran organ di model tubuh anak 3D (satuan tubuh: tinggi ±1.3; x+ = kiri tubuh / kanan layar, depan = -z).
        /// Dipakai tampilan "Lihat di tubuh".
        /// </summary>
        public static (Vector3 pos, float size) BodyPlacement(string id)
        {
            switch (id)
            {
                case "otak": return (new Vector3(0f, 1.15f, 0.01f), 0.21f);
                case "mulut": return (new Vector3(0f, 1.04f, -0.12f), 0.09f);
                case "kerongkongan": return (new Vector3(0f, 0.87f, 0.02f), 0.2f);
                case "paru": return (new Vector3(0f, 0.76f, 0.0f), 0.3f);
                case "jantung": return (new Vector3(0.035f, 0.7f, -0.09f), 0.12f);
                // perut: di bawah rusuk dan DI ATAS pinggang celana/rok (±0.49); dulu usus berada di dalam celana
                case "hati": return (new Vector3(-0.06f, 0.615f, -0.04f), 0.2f);
                case "lambung": return (new Vector3(0.07f, 0.6f, -0.05f), 0.12f);
                case "pankreas": return (new Vector3(0.02f, 0.565f, 0.02f), 0.14f);
                case "usushalus": return (new Vector3(0f, 0.505f, -0.05f), 0.15f);
                case "ususbesar": return (new Vector3(0f, 0.51f, -0.02f), 0.22f);
                case "ginjal": return (new Vector3(0f, 0.56f, 0.07f), 0.22f);
                case "mata": return (new Vector3(0.057f, 1.135f, -0.115f), 0.06f);
                case "hidung": return (new Vector3(0f, 1.095f, -0.135f), 0.085f);
                case "tenggorokan": return (new Vector3(0f, 0.955f, -0.03f), 0.18f);
                case "rangka": return (new Vector3(0f, 0.645f, 0f), 1.3f);
                case "reprolaki": return (new Vector3(0f, 0.36f, -0.05f), 0.16f);
                case "reproperempuan": return (new Vector3(0f, 0.38f, -0.02f), 0.16f);
                default: return (new Vector3(0f, 0.6f, 0f), 0.12f);
            }
        }

        public static string SystemName(OrganSystem s)
        {
            switch (s)
            {
                case OrganSystem.Pencernaan: return "Pencernaan";
                case OrganSystem.Pernapasan: return "Pernapasan";
                case OrganSystem.PeredaranDarah: return "Peredaran Darah";
                case OrganSystem.Ekskresi: return "Ekskresi";
                case OrganSystem.Indera: return "Indera";
                case OrganSystem.Reproduksi: return "Reproduksi";
                case OrganSystem.Gerak: return "Gerak";
                default: return "Saraf";
            }
        }

        public static string RegionName(BodyRegion r)
        {
            switch (r)
            {
                case BodyRegion.Kepala: return "Kepala";
                case BodyRegion.Leher: return "Leher";
                case BodyRegion.Dada: return "Dada";
                case BodyRegion.Panggul: return "Panggul";
                case BodyRegion.SeluruhTubuh: return "Seluruh tubuh";
                default: return "Perut";
            }
        }

        /// <summary>(warna tulisan, warna lembut) per sistem untuk kartu & chip.</summary>
        public static (Color fg, Color soft) SystemColors(OrganSystem s)
        {
            switch (s)
            {
                case OrganSystem.Pencernaan: return (Hex("#12857A"), Hex("#D5F3EF"));
                case OrganSystem.Pernapasan: return (Hex("#6F52D1"), Hex("#E8E2FB"));
                case OrganSystem.PeredaranDarah: return (Hex("#D6385E"), Hex("#FDE0E7"));
                case OrganSystem.Ekskresi: return (Hex("#B4532A"), Hex("#FDE7DA"));
                case OrganSystem.Indera: return (Hex("#2B7BD6"), Hex("#DCEBFB"));
                case OrganSystem.Reproduksi: return (Hex("#A2479E"), Hex("#F6E5F5"));
                case OrganSystem.Gerak: return (Hex("#8A6D3B"), Hex("#F4ECDD"));
                default: return (Hex("#B07D00"), Hex("#FFF1BF"));
            }
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        public static OrganInfo Create(string id)
        {
            var o = ScriptableObject.CreateInstance<OrganInfo>();
            Fill(o, id);
            return o;
        }

        public static void Fill(OrganInfo o, string id)
        {
            o.id = id;
            o.name = id;
            o.hotspots = new List<OrganHotspotInfo>();
            switch (id)
            {
                case "mulut":
                    Set(o, "Mulut", OrganSystem.Pencernaan, BodyRegion.Kepala, 1, 1, "#FFF1F4", "#F5B7C6", "#EF476F",
                        "Mulut adalah pintu masuk makanan. Gigi mengunyah makanan menjadi potongan kecil. Lidah membantumu merasakan manis, asin, asam, pahit, dan gurih. Air liur mulai melunakkan makanan sebelum ditelan.",
                        "Mulutmu membuat sekitar satu liter air liur setiap hari!",
                        "Tempat makanan dikunyah dan dicampur air liur",
                        "Mulut mengunyah makanan dan mencampurnya dengan air liur.");
                    Hot(o, "Gigi", "Memotong & mengunyah makanan", new Vector3(0f, 0.12f, 0.3f));
                    Hot(o, "Lidah", "Merasakan manis, asin, asam, pahit", new Vector3(0.1f, -0.08f, 0.25f));
                    Hot(o, "Air liur", "Melunakkan makanan", new Vector3(-0.25f, 0f, 0.15f));
                    break;
                case "kerongkongan":
                    Set(o, "Kerongkongan", OrganSystem.Pencernaan, BodyRegion.Leher, 2, 1, "#E8F8F5", "#A8E3DA", "#1AB8A6",
                        "Kerongkongan adalah saluran panjang dari mulut ke lambung. Ototnya meremas makanan turun pelan-pelan. Karena itu makanan tetap bisa masuk ke lambung walau kamu sedang berbaring.",
                        "Kamu tetap bisa menelan makanan walaupun sedang berdiri terbalik, lho!",
                        "Saluran yang mengantar makanan dari mulut ke lambung",
                        "Kerongkongan mendorong makanan dari mulut ke lambung.");
                    Hot(o, "Otot kerongkongan", "Meremas makanan turun", new Vector3(0.05f, 0.1f, 0.12f));
                    Hot(o, "Ke lambung", "Ujung bawah saluran", new Vector3(0f, -0.42f, 0.08f));
                    break;
                case "lambung":
                    Set(o, "Lambung", OrganSystem.Pencernaan, BodyRegion.Perut, 3, 1, "#FFF1F4", "#F5B7C6", "#F58CA3",
                        "Lambung adalah kantong lentur yang bisa melar. Di sini, makanan diaduk bersama asam lambung dan enzim. Sedikit demi sedikit, makanan berubah menjadi bubur halus.",
                        "Asam lambung sangat kuat, tetapi lapisan lendir di dinding lambung melindunginya supaya lambung tidak ikut tercerna!",
                        "Kantong elastis yang mencerna makanan dengan asam dan enzim",
                        "Lambung mengaduk makanan bersama asam lambung sampai jadi bubur halus.");
                    Hot(o, "Asam Lambung", "Membantu hancurkan makanan", new Vector3(0.1f, -0.05f, 0.3f));
                    Hot(o, "Otot lambung", "Mengaduk makanan", new Vector3(0.3f, 0.1f, 0.15f));
                    Hot(o, "Pintu masuk", "Dari kerongkongan", new Vector3(-0.2f, 0.4f, 0.05f));
                    break;
                case "usushalus":
                    Set(o, "Usus Halus", OrganSystem.Pencernaan, BodyRegion.Perut, 4, 1, "#FFF1F4", "#F5B7C6", "#EF476F",
                        "Usus halus berkelok-kelok di dalam perutmu. Di sinilah sari-sari makanan diserap oleh tubuh dan dibawa oleh darah. Tanpa usus halus, tubuhmu tidak mendapat tenaga dari makanan.",
                        "Kalau direntangkan, usus halus bisa lebih panjang dari tinggi badanmu!",
                        "Saluran berkelok tempat sari-sari makanan diserap",
                        "Usus halus menyerap sari-sari makanan, lalu darah membawanya ke seluruh tubuh.");
                    Hot(o, "Vili", "Menyerap sari makanan", new Vector3(0.15f, 0.05f, 0.3f));
                    Hot(o, "Lekukan usus", "Membuat usus muat di perut", new Vector3(-0.3f, -0.2f, 0.2f));
                    break;
                case "ususbesar":
                    Set(o, "Usus Besar", OrganSystem.Pencernaan, BodyRegion.Perut, 5, 1, "#FFF9E3", "#F1D77A", "#F2B705",
                        "Usus besar membingkai usus halus seperti pagar. Tugasnya menyerap air dari sisa makanan. Sisa yang tidak terpakai lalu dikumpulkan dan dibuang dari tubuh.",
                        "Di dalam usus besar hidup banyak bakteri baik yang membantu tubuhmu!",
                        "Saluran yang menyerap air dari sisa makanan",
                        "Usus besar menyerap air dari sisa makanan.");
                    Hot(o, "Penyerap air", "Mengambil air dari sisa makanan", new Vector3(0.35f, 0.2f, 0.15f));
                    Hot(o, "Bakteri baik", "Membantu tubuhmu", new Vector3(-0.3f, 0.25f, 0.15f));
                    break;
                case "paru":
                    Set(o, "Paru-paru", OrganSystem.Pernapasan, BodyRegion.Dada, 6, 2, "#F3EFFD", "#CDBFF5", "#8B6FE8",
                        "Ada dua paru-paru di dalam dadamu. Saat menarik napas, paru-paru mengambil oksigen dari udara. Saat menghembuskan napas, paru-paru membuang karbon dioksida yang tidak dibutuhkan tubuh.",
                        "Paru-paru kiri sedikit lebih kecil supaya ada tempat untuk jantung!",
                        "Sepasang organ untuk bernapas",
                        "Paru-paru untuk bernapas: mengambil oksigen dari udara.");
                    Hot(o, "Tenggorokan", "Jalan masuk udara", new Vector3(0f, 0.45f, 0.05f));
                    Hot(o, "Alveolus", "Tukar oksigen & karbon dioksida", new Vector3(0.3f, -0.1f, 0.2f));
                    break;
                case "jantung":
                    Set(o, "Jantung", OrganSystem.PeredaranDarah, BodyRegion.Dada, 7, 3, "#FFF1F4", "#F5B7C6", "#EF476F",
                        "Jantung adalah otot sebesar kepalan tanganmu. Ia memompa darah ke seluruh tubuh tanpa henti. Darah membawa oksigen dan makanan ke semua bagian tubuh. Kamu bisa merasakan detaknya di dada.",
                        "Jantungmu berdetak sekitar seratus ribu kali setiap hari!",
                        "Otot yang memompa darah ke seluruh tubuh",
                        "Jantung memompa darah ke seluruh tubuh tanpa henti, siang dan malam.");
                    Hot(o, "Bilik jantung", "Memompa darah", new Vector3(0.1f, -0.15f, 0.3f));
                    Hot(o, "Pembuluh darah", "Jalan darah keluar-masuk", new Vector3(-0.1f, 0.42f, 0.1f));
                    break;
                case "hati":
                    Set(o, "Hati", OrganSystem.Pencernaan, BodyRegion.Perut, 9, 1, "#FDECEC", "#F3B4B4", "#C8414B",
                        "Hati adalah organ dalam terbesar di tubuhmu. Letaknya di perut bagian kanan atas, di bawah tulang rusuk. Hati membuat cairan empedu untuk membantu mencerna lemak. Hati juga menyimpan cadangan tenaga dan membersihkan zat berbahaya dari darah.",
                        "Hati bisa tumbuh kembali ke ukuran semula, bahkan setelah sebagian besarnya diangkat dokter!",
                        "Organ terbesar di perut yang membuat empedu",
                        "Hati membuat empedu dan membersihkan darah.");
                    Hot(o, "Lobus kanan", "Bagian hati yang paling besar", new Vector3(-0.2f, 0f, -0.3f));
                    Hot(o, "Lobus kiri", "Bagian hati yang lebih kecil", new Vector3(0.3f, 0.05f, -0.15f));
                    Hot(o, "Kantong empedu", "Menyimpan cairan empedu", new Vector3(-0.05f, -0.2f, -0.2f));
                    break;
                case "pankreas":
                    Set(o, "Pankreas", OrganSystem.Pencernaan, BodyRegion.Perut, 10, 1, "#FFF4E6", "#F1D3A8", "#E9B07A",
                        "Pankreas adalah kelenjar panjang yang letaknya di belakang lambung. Pankreas membuat cairan pencernaan yang membantu memecah makanan di usus halus. Pankreas juga membuat insulin, yang menjaga kadar gula di dalam darah.",
                        "Pankreas bekerja dua kali: membantu mencerna makanan dan menjaga gula darahmu tetap seimbang!",
                        "Kelenjar yang membuat cairan pencernaan dan insulin",
                        "Pankreas membuat cairan pencernaan dan insulin.");
                    Hot(o, "Kepala pankreas", "Dikelilingi usus dua belas jari", new Vector3(-0.2f, -0.03f, -0.1f));
                    Hot(o, "Ekor pankreas", "Dekat limpa", new Vector3(0.35f, 0.1f, -0.06f));
                    break;
                case "otak":
                    Set(o, "Otak", OrganSystem.Saraf, BodyRegion.Kepala, 11, 5, "#FFF4F6", "#F5C2CF", "#F29BAE",
                        "Otak adalah pusat kendali tubuhmu. Letaknya di dalam kepala, dilindungi tulang tengkorak. Otak membantumu berpikir, mengingat, merasakan, dan bergerak. Setiap bagian otak punya tugas sendiri, misalnya untuk melihat, mendengar, dan menjaga keseimbangan.",
                        "Pesan saraf tercepat bisa melesat sekitar 400 km per jam, lebih cepat dari mobil balap!",
                        "Pusat kendali seluruh tubuh",
                        "Otak mengatur pikiran dan gerakan tubuh.");
                    Hot(o, "Otak besar", "Berpikir dan mengingat", new Vector3(0f, 0.2f, -0.3f));
                    Hot(o, "Otak kecil", "Menjaga keseimbangan", new Vector3(0f, -0.25f, 0.2f));
                    break;
                case "mata":
                    Set(o, "Mata", OrganSystem.Indera, BodyRegion.Kepala, 14, 5, "#EEF5FF", "#B8D4F5", "#4D9DE0",
                        "Mata adalah alat indera untuk melihat. Cahaya masuk lewat kornea yang bening, lalu melewati pupil, lubang hitam di tengah iris. Lensa memfokuskan cahaya ke retina di bagian belakang mata. Retina mengubah cahaya menjadi pesan, lalu saraf mata mengirimnya ke otak. Barulah kamu bisa melihat!",
                        "Pupilmu mengecil saat terang dan membesar saat gelap supaya cahaya yang masuk pas!",
                        "Alat indera untuk melihat",
                        "Mata menangkap cahaya dan mengirim gambarnya ke otak.");
                    Hot(o, "Kornea", "Jendela bening di depan mata", new Vector3(0f, 0f, -0.45f));
                    Hot(o, "Iris", "Bagian berwarna", new Vector3(0.12f, 0f, -0.42f));
                    break;
                case "hidung":
                    Set(o, "Hidung", OrganSystem.Pernapasan, BodyRegion.Kepala, 13, 2, "#FFF4EE", "#F5CDB8", "#E9946F",
                        "Hidung adalah pintu masuk udara saat kamu bernapas, sekaligus alat indera untuk mencium bau. Di dalam hidung ada bulu halus dan lendir yang menangkap debu dan kuman. Rak-rak berlekuk bernama konka menghangatkan dan melembapkan udara. Di atap rongga hidung ada sel-sel pencium yang mengirim pesan bau ke otak.",
                        "Hidungmu bisa mengenali ribuan macam bau, dari kue yang baru matang sampai hujan pertama!",
                        "Pintu masuk udara & alat pencium",
                        "Hidung menyaring udara yang masuk dan mencium bau.");
                    Hot(o, "Lubang hidung", "Pintu masuk udara", new Vector3(0.05f, -0.06f, -0.29f));
                    break;
                case "tenggorokan":
                    Set(o, "Tenggorokan", OrganSystem.Pernapasan, BodyRegion.Leher, 12, 2, "#F3EFFD", "#CDBFF5", "#8B6FE8",
                        "Tenggorokan ada di leher, di belakang hidung dan mulut. Di sini ada faring, jalan bersama untuk udara dan makanan. Lalu jalannya berpisah: udara masuk ke laring (kotak suara) dan trakea menuju paru-paru, sedangkan makanan masuk ke kerongkongan menuju lambung. Epiglotis menutup laring saat kamu menelan supaya makanan tidak salah jalan.",
                        "Suaramu berasal dari pita suara di laring yang bergetar saat udara lewat!",
                        "Jalan udara & makanan di leher",
                        "Tenggorokan mengatur udara ke paru-paru dan makanan ke kerongkongan.");
                    Hot(o, "Epiglotis", "Tutup saat menelan", new Vector3(0f, 0.22f, -0.02f));
                    break;
                case "rangka":
                    Set(o, "Rangka", OrganSystem.Gerak, BodyRegion.SeluruhTubuh, 15, 6, "#FBF6EA", "#E8D9B5", "#C79A4B",
                        "Rangka adalah susunan tulang yang menjadi kerangka tubuhmu. Rangka memberi bentuk tubuh, membuatmu bisa berdiri tegak, dan bersama otot membuatmu bisa bergerak. Tengkorak melindungi otak, tulang rusuk melindungi jantung dan paru-paru, dan tulang panggul menopang tubuh bagian atas. Di dalam tulang ada sumsum merah yang membuat sel-sel darah.",
                        "Orang dewasa punya 206 tulang, dan lebih dari separuhnya ada di tangan dan kaki!",
                        "Kerangka penyangga & pelindung tubuh",
                        "Rangka menegakkan tubuh, melindungi organ, dan membantu tubuh bergerak.");
                    Hot(o, "Tengkorak", "Melindungi otak", new Vector3(0f, 1.2f, -0.09f));
                    Hot(o, "Tulang rusuk", "Melindungi jantung & paru-paru", new Vector3(0.13f, 0.8f, -0.06f));
                    break;
                case "reprolaki":
                    Set(o, "Reproduksi Laki-laki", OrganSystem.Reproduksi, BodyRegion.Panggul, 16, 7, "#EEF1FF", "#BFCBF5", "#5B7BE0",
                        "Organ reproduksi laki-laki ada di panggul (perut bagian bawah) dan di antara kedua kaki. Testis membuat sel sperma dan hormon testosteron, yang mulai bekerja saat pubertas. Sperma disimpan di epididimis, lalu diantar saluran sperma ke dekat kelenjar prostat. Uretra di dalam penis adalah jalan keluar urine, dan setelah pubertas juga sperma, tetapi tidak pada saat yang sama. Organ ini termasuk bagian tubuh pribadi yang harus dijaga kebersihannya.",
                        "Testis berada di luar perut karena sel sperma butuh suhu sedikit lebih dingin daripada suhu tubuh!",
                        "Organ yang membuat sel sperma & hormon laki-laki",
                        "Organ reproduksi laki-laki membuat sel sperma dan hormon testosteron saat pubertas.");
                    Hot(o, "Testis", "Membuat sel sperma", new Vector3(0.08f, -0.42f, -0.08f));
                    Hot(o, "Prostat", "Membuat cairan untuk sperma", new Vector3(0f, 0.06f, -0.06f));
                    break;
                case "reproperempuan":
                    Set(o, "Reproduksi Perempuan", OrganSystem.Reproduksi, BodyRegion.Panggul, 17, 7, "#FFF0F5", "#F5C2D6", "#E0678F",
                        "Organ reproduksi perempuan ada di dalam panggul (perut bagian bawah). Dua ovarium (indung telur) menyimpan calon sel telur dan membuat hormon estrogen. Mulai pubertas, kira-kira sebulan sekali satu sel telur matang lalu berjalan lewat tuba falopi menuju rahim. Dinding rahim menebal untuk bersiap, lalu luruh dan keluar lewat vagina sebagai darah haid bila tidak terjadi kehamilan. Rahim juga tempat bayi tumbuh selama kehamilan.",
                        "Saat lahir, bayi perempuan sudah memiliki sekitar satu juta calon sel telur di indung telurnya!",
                        "Organ yang menyimpan sel telur & tempat bayi tumbuh",
                        "Organ reproduksi perempuan menyimpan sel telur dan menjadi tempat bayi tumbuh.");
                    Hot(o, "Rahim", "Tempat bayi tumbuh", new Vector3(0f, 0.14f, -0.12f));
                    Hot(o, "Ovarium", "Menyimpan sel telur", new Vector3(0.38f, 0.06f, -0.02f));
                    break;
                case "ginjal":
                    Set(o, "Ginjal", OrganSystem.Ekskresi, BodyRegion.Perut, 8, 4, "#FFEDEF", "#F5B7C6", "#E06A7E",
                        "Kamu punya dua ginjal yang bentuknya seperti kacang merah. Letaknya di perut bagian belakang, dekat punggung. Ginjal menyaring darah dan membuang sisa yang tidak dibutuhkan tubuh. Sisa itu keluar bersama air sebagai urine atau air seni.",
                        "Ginjal orang dewasa menyaring sekitar 180 liter cairan darah setiap hari, kira-kira sebanyak satu bak mandi!",
                        "Sepasang organ penyaring darah",
                        "Ginjal menyaring darah dan membuang sisanya lewat urine.");
                    Hot(o, "Penyaring darah", "Membersihkan darah", new Vector3(-0.15f, 0.05f, -0.3f));
                    Hot(o, "Pembuluh darah", "Darah masuk dan keluar", new Vector3(0.3f, 0.12f, -0.12f));
                    Hot(o, "Ureter", "Saluran urine ke kandung kemih", new Vector3(0.22f, -0.38f, -0.08f));
                    break;
            }
        }

        /// <summary>
        /// Samakan teks & identitas organ dengan isi di kode (nama, sistem, letak, nomor kartu, penjelasan, fakta, fungsi).
        /// Model 3D, gambar, dan kartu tidak diubah. Dipanggil oleh Organo > Setup supaya koreksi isi langsung terpakai.
        /// </summary>
        public static bool RefreshText(OrganInfo o)
        {
            if (o == null || System.Array.IndexOf(Order, o.id) < 0) return false;
            var d = ScriptableObject.CreateInstance<OrganInfo>();
            Fill(d, o.id);
            bool changed = o.displayName != d.displayName || o.system != d.system || o.region != d.region || o.cardNumber != d.cardNumber
                || o.season != d.season || o.about != d.about || o.funFact != d.funFact || o.shortDescription != d.shortDescription || o.function != d.function;
            o.displayName = d.displayName; o.system = d.system; o.region = d.region; o.cardNumber = d.cardNumber; o.season = d.season;
            o.about = d.about; o.funFact = d.funFact; o.shortDescription = d.shortDescription; o.function = d.function;
            Object.DestroyImmediate(d);
            return changed;
        }

        static void Set(OrganInfo o, string name, OrganSystem sys, BodyRegion reg, int card, int season,
            string tint, string ring, string accent, string about, string fact, string shortDesc, string function)
        {
            o.displayName = name; o.system = sys; o.region = reg; o.cardNumber = card; o.season = season;
            o.tint = Hex(tint); o.ring = Hex(ring); o.accent = Hex(accent);
            o.about = about; o.funFact = fact; o.shortDescription = shortDesc; o.function = function;
        }

        static void Hot(OrganInfo o, string title, string desc, Vector3 pos)
        {
            o.hotspots.Add(new OrganHotspotInfo { title = title, description = desc, localPosition = pos });
        }
    }
}
