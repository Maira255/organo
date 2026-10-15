using System.Collections.Generic;
using UnityEngine;

namespace Organo.Data
{
    /// <summary>
    /// Teks yang dibacakan (narasi) & rekamannya. Satu sumber untuk halaman organ, penampil kartu, ekspor teks ke
    /// pembuat suara (Tools/suara/buat_suara.py) dan pemasangan rekaman, supaya urutan fakta selalu cocok dengan rekamannya.
    /// </summary>
    public static class OrganNarration
    {
        public const string FactIntro = "Tahukah kamu? ";

        /// <summary>Daftar fakta menarik organ (fakta utama dulu, lalu fakta tambahan tanpa duplikat).</summary>
        public static List<string> Facts(OrganInfo o)
        {
            var list = new List<string>();
            if (o == null) return list;
            if (!string.IsNullOrEmpty(o.funFact)) list.Add(o.funFact);
            var c = OrganContent.Get(o.id);
            if (c?.facts != null)
                foreach (var f in c.facts)
                    if (!string.IsNullOrEmpty(f) && !list.Contains(f)) list.Add(f);
            return list;
        }

        public static string FactText(string fact) => FactIntro + fact;

        /// <summary>Kalimat kartu (penampil kartu): nama organ lalu tugasnya.</summary>
        public static string CardText(OrganInfo o) => o.displayName + ". " + o.function;

        /// <summary>Rekaman untuk fakta tertentu (null kalau belum ada).</summary>
        public static AudioClip FactClip(OrganInfo o, string fact)
        {
            if (o == null || o.factNarration == null || string.IsNullOrEmpty(fact)) return null;
            int i = Facts(o).IndexOf(fact);
            return i >= 0 && i < o.factNarration.Length ? o.factNarration[i] : null;
        }

        /// <summary>Nama berkas rekaman (tanpa ekstensi) untuk tiap teks.</summary>
        public static string AboutFile(OrganInfo o) => o.id + "_tentang";
        public static string FactFile(OrganInfo o, int i) => o.id + "_fakta" + (i + 1);
        public static string CardFile(OrganInfo o) => o.id + "_kartu";
    }
}
