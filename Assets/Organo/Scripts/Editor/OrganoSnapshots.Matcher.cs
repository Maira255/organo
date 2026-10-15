using System.Collections.Generic;
using Organo.AR;
using UnityEngine;
using UnityEngine.UIElements;

namespace Organo.EditorTools
{
    /// <summary>
    /// Uji pengenal kartu dengan kamera biasa (HP tanpa AR): gambar kamera buatan (kartu di bingkai scan, sedikit
    /// geser/kecil, gelap/terang, buram, noise, kartu lain di sekelilingnya) dicocokkan dengan ciri kartu.
    /// </summary>
    public static partial class OrganoSnapshots
    {
        static void TestCardMatcher()
        {
            var m = CardMatcher.Load();
            Report("ciri kartu untuk HP tanpa AR tersedia (17 kartu x 2 gaya)", m != null && m.Count == 34, m != null ? m.Count + " gambar" : "tidak ada");
            if (m == null || m.Count == 0) return;

            // gambar kamera buatan: kartu di bingkai, kartu lain berdempetan, gelap/terang, buram, noise (lihat MatcherSynth)
            var cards = MatcherSynth.LoadCards();
            var rnd = new System.Random(3);
            int ok = 0, wrong = 0, miss = 0, total = 0;
            var wrongs = new List<string>();
            foreach (var c in cards)
                for (int t = 0; t < 2; t++)
                {
                    total++;
                    var res = m.Match(MatcherSynth.Scene(cards, c, rnd), MatcherSynth.CW, MatcherSynth.CH, MatcherSynth.Frame);
                    if (!res.Confident) miss++;
                    else if (res.organId == c.id) ok++;
                    else { wrong++; wrongs.Add(c.id + "→" + res.organId); }
                }
            Report("kamera biasa: kartu di bingkai dikenali & tidak pernah tertukar", wrong == 0 && ok >= total * 0.65f,
                "benar " + ok + "/" + total + ", salah " + wrong + ", tidak " + miss + (wrongs.Count > 0 ? " (" + string.Join(", ", wrongs) + ")" : ""));

            // bingkai tanpa kartu (meja polos / kartu hanya di pinggir): tidak boleh dianggap kartu
            int fp = 0;
            for (int t = 0; t < 40; t++)
                if (m.Match(MatcherSynth.Scene(cards, null, rnd), MatcherSynth.CW, MatcherSynth.CH, MatcherSynth.Frame).Confident) fp++;
            Report("kamera biasa: bingkai tanpa kartu tidak dianggap kartu", fp == 0, fp + "/40");

            // bingkai di layar → piksel gambar kamera tegak (tampilan "cover": tengah layar = tengah gambar)
            var mapped = CardMatcher.ScreenToImage(new Rect(195 - 10, 422 - 10, 20, 20), 390, 844, 144, 256);
            Report("bingkai di layar dipetakan ke gambar kamera (tengah ke tengah)", Mathf.Abs(mapped.center.x - 72) < 0.5f && Mathf.Abs(mapped.center.y - 128) < 0.5f,
                mapped.ToString());

            // arah gambar: gambar mentah 4x2 (baris bawah dulu) diputar 90° searah jarum jam
            var raw = new Color32[8];
            for (int i = 0; i < 8; i++) raw[i] = new Color32((byte)i, 0, 0, 255);
            var up = CardMatcher.Upright(raw, 4, 2, 1, 90, false, false, out int uw, out int uh);
            // gambar tampil (atas dulu): baris atas = mentah baris 1 (4..7), baris bawah = 0..3
            // diputar searah jarum jam: kolom kiri gambar tampil (4 di atas, 0 di bawah) menjadi baris atas dari kanan ke kiri
            bool rotOk = uw == 2 && uh == 4 && up[0].r == 0 && up[1].r == 4 && up[2].r == 1 && up[7].r == 7;
            Report("gambar kamera ditegakkan searah jarum jam (sama dengan tampilan layar)", rotOk,
                uw + "x" + uh + ": " + string.Join(",", System.Array.ConvertAll(up, c => c.r.ToString())));

            // tampilan UI: rotate positif = searah jarum jam (dasar perbaikan kamera terbalik)
            var host = App.Current?.Root;
            if (host != null)
            {
                var box = new VisualElement();
                box.style.position = Position.Absolute; box.style.left = 100; box.style.top = 100; box.style.width = 100; box.style.height = 100;
                var dot = new VisualElement();
                dot.style.position = Position.Absolute; dot.style.left = 0; dot.style.top = 0; dot.style.width = 10; dot.style.height = 10;
                box.Add(dot);
                box.style.rotate = new Rotate(90);
                host.Add(box);
                Later(() =>
                {
                    var b = box.worldBound; var d = dot.worldBound;
                    bool cw = d.center.x > b.center.x && d.center.y < b.center.y; // kiri-atas → kanan-atas bila searah jarum jam
                    Report("rotate UI positif memutar searah jarum jam", cw, "titik " + d.center.ToString("0") + ", kotak " + b.center.ToString("0"));
                    box.RemoveFromHierarchy();
                }, 200);
            }
        }
    }
}
