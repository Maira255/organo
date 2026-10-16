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
            Report("ciri kartu untuk HP tanpa AR tersedia (17 kartu x 2 gaya x 3 kemiringan)", m != null && m.Count == 102, m != null ? m.Count + " gambar" : "tidak ada");
            if (m == null || m.Count == 0) return;

            // gambar kamera buatan: kartu di bingkai, kartu lain berdempetan, gelap/terang, buram, noise (lihat MatcherSynth).
            // Dikenali = cara yang dipakai aplikasi (Recognize + ambang RecognizeMin/RecognizeMargin).
            bool Sure(CardMatcher.Result r) => r.organId != null && r.score >= CardMatcher.RecognizeMin && r.margin >= CardMatcher.RecognizeMargin;
            var cards = MatcherSynth.LoadCards();
            var rnd = new System.Random(3);
            int ok = 0, wrong = 0, miss = 0, total = 0;
            var wrongs = new List<string>();
            var weak = new List<string>();
            foreach (var c in cards)
            {
                int okCard = 0;
                for (int t = 0; t < 4; t++)
                {
                    total++;
                    var res = m.Recognize(MatcherSynth.Scene(cards, c, rnd), MatcherSynth.CW, MatcherSynth.CH, MatcherSynth.Frame);
                    if (!Sure(res)) miss++;
                    else if (res.organId == c.id) { ok++; okCard++; }
                    else { wrong++; wrongs.Add(c.id + "→" + res.organId); }
                }
            }
            // kuis "Misi Scan": setiap kartu (kedua gaya) dalam kondisi wajar (satu kartu di meja, memenuhi bingkai, sedikit
            // miring, cahaya biasa) harus dikenali — aplikasi butuh 3 dari 4 pemeriksaan sepakat (±1 detik)
            var f = MatcherSynth.Frame;
            foreach (var c in cards)
            {
                int okCard = 0;
                for (int t = 0; t < 8; t++)
                {
                    var canvas = new Color32[MatcherSynth.CW * MatcherSynth.CH];
                    var bg = new Color32((byte)rnd.Next(90, 200), (byte)rnd.Next(80, 180), (byte)rnd.Next(60, 160), 255);
                    for (int i = 0; i < canvas.Length; i++) canvas[i] = bg;
                    float sc = 0.82f + (float)rnd.NextDouble() * 0.18f;
                    var at = f.center + new Vector2((float)(rnd.NextDouble() - 0.5) * 0.1f * f.width, (float)(rnd.NextDouble() - 0.5) * 0.08f * f.height);
                    MatcherSynthEval.PasteRotated(canvas, c, at, f.width * sc, f.height * sc, (float)(rnd.NextDouble() * 2 - 1) * 3f, 0.8f + (float)rnd.NextDouble() * 0.3f);
                    var res = m.Recognize(MatcherSynth.BlurNoise(canvas, rnd, 8f), MatcherSynth.CW, MatcherSynth.CH, f);
                    if (Sure(res) && res.organId == c.id) okCard++;
                    else if (Sure(res)) { wrong++; wrongs.Add(c.id + "→" + res.organId + " (wajar)"); }
                }
                if (okCard < 5) weak.Add(c.id + " (" + okCard + "/8)");
            }
            // kondisi berat (kartu lain berdempetan, gelap, buram): boleh sesekali terbaca kartu tetangga, tapi kartunya sendiri
            // tetap sering dikenali; kondisi wajar: tidak pernah tertukar
            Report("kamera biasa: kartu di bingkai dikenali (kondisi berat) & tidak tertukar (kondisi wajar)",
                ok >= total * 0.5f && wrong <= total * 0.04f && !wrongs.Exists(w => w.EndsWith("(wajar)")),
                "benar " + ok + "/" + total + ", salah " + wrong + ", tidak " + miss + (wrongs.Count > 0 ? " (" + string.Join(", ", wrongs) + ")" : ""));
            Report("kamera biasa (kuis Misi Scan): semua 34 kartu (17 organ x 2 gaya) dikenali dalam kondisi wajar", weak.Count == 0 && cards.Count == 34,
                cards.Count + " kartu" + (weak.Count > 0 ? ", lemah: " + string.Join(", ", weak) : ""));

            // bingkai tanpa kartu (meja polos / kartu hanya di pinggir / latar ramai): tidak boleh dianggap kartu
            int fp = 0;
            for (int t = 0; t < 40; t++)
                if (Sure(m.Recognize(MatcherSynth.Scene(cards, null, rnd), MatcherSynth.CW, MatcherSynth.CH, MatcherSynth.Frame))) fp++;
            for (int t = 0; t < 60; t++)
                if (Sure(m.Recognize(MatcherSynthEval.Negative(rnd).px, MatcherSynth.CW, MatcherSynth.CH, MatcherSynth.Frame))) fp++;
            Report("kamera biasa: bingkai tanpa kartu tidak dianggap kartu (polos & latar ramai)", fp == 0, fp + "/100");

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
