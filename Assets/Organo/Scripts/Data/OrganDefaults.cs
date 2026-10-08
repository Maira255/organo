using System.Collections.Generic;
using UnityEngine;

namespace Organo.Data
{
    /// <summary>Konten bawaan dari desain Organo. Dipakai untuk membuat asset OrganInfo pertama kali.</summary>
    public static class OrganDefaults
    {
        public static readonly string[] Order = { "mulut", "kerongkongan", "lambung", "usushalus", "ususbesar", "paru", "jantung" };

        public static string SystemName(OrganSystem s)
        {
            switch (s)
            {
                case OrganSystem.Pencernaan: return "Pencernaan";
                case OrganSystem.Pernapasan: return "Pernapasan";
                case OrganSystem.PeredaranDarah: return "Peredaran Darah";
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
                        "Mulut adalah pintu masuk makanan. Gigi mengunyah makanan menjadi potongan kecil. Lidah membantumu merasakan manis, asin, dan asam. Air liur mulai melunakkan makanan sebelum ditelan.",
                        "Air liurmu bisa mencapai hampir satu liter dalam sehari!",
                        "Tempat makanan dikunyah dan dicampur air liur",
                        "Mulut mengunyah makanan dan mencampurnya dengan air liur.");
                    Hot(o, "Gigi", "Memotong & mengunyah makanan", new Vector3(0f, 0.12f, 0.3f));
                    Hot(o, "Lidah", "Merasakan manis, asin, asam", new Vector3(0.1f, -0.08f, 0.25f));
                    Hot(o, "Air liur", "Melunakkan makanan", new Vector3(-0.25f, 0f, 0.15f));
                    break;
                case "kerongkongan":
                    Set(o, "Kerongkongan", OrganSystem.Pencernaan, BodyRegion.Leher, 2, 1, "#E8F8F5", "#A8E3DA", "#1AB8A6",
                        "Kerongkongan adalah saluran panjang dari mulut ke lambung. Ototnya meremas makanan turun pelan-pelan. Karena itu makanan tetap bisa masuk ke lambung walau kamu sedang berbaring.",
                        "Kamu tetap bisa menelan makanan sambil berdiri di atas kepala, lho!",
                        "Saluran yang mengantar makanan dari mulut ke lambung",
                        "Kerongkongan mendorong makanan dari mulut ke lambung.");
                    Hot(o, "Otot kerongkongan", "Meremas makanan turun", new Vector3(0.05f, 0.1f, 0.12f));
                    Hot(o, "Ke lambung", "Ujung bawah saluran", new Vector3(0f, -0.42f, 0.08f));
                    break;
                case "lambung":
                    Set(o, "Lambung", OrganSystem.Pencernaan, BodyRegion.Perut, 3, 1, "#FFF1F4", "#F5B7C6", "#F58CA3",
                        "Lambung adalah kantong lentur yang bisa melar. Di sini, makanan diaduk bersama asam lambung dan enzim. Sedikit demi sedikit, makanan berubah menjadi bubur halus.",
                        "Asam lambung cukup kuat untuk melarutkan logam, tetapi lapisan lambung melindungi dirinya sendiri!",
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
            }
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
