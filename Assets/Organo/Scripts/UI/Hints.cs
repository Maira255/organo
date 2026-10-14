using UnityEngine;

namespace Organo.UI
{
    /// <summary>
    /// Petunjuk gerakan (mis. "Geser untuk memutar") hanya tampil beberapa kali pertama. Setelah anak tahu caranya,
    /// petunjuk tidak lagi menutupi gambar / model.
    /// </summary>
    public static class Hints
    {
        public const string QuizRotate = "quiz-rotate";
        public const string LibraryRotate = "library-rotate";
        public const string ScanRotate = "scan-rotate";
        public const string DetailParts = "detail-parts";

        static readonly string[] All = { QuizRotate, LibraryRotate, ScanRotate, DetailParts };
        const string Prefix = "organo.hint.";

        /// <summary>True bila petunjuk ini masih boleh tampil; sekaligus dicatat sudah tampil sekali.</summary>
        public static bool Take(string key, int times = 2)
        {
            int n = PlayerPrefs.GetInt(Prefix + key, 0);
            if (n >= times) return false;
            PlayerPrefs.SetInt(Prefix + key, n + 1);
            return true;
        }

        /// <summary>Tampilkan lagi semua petunjuk (snapshot / uji tampilan).</summary>
        public static void ResetAll()
        {
            foreach (var k in All) PlayerPrefs.DeleteKey(Prefix + k);
        }
    }
}
