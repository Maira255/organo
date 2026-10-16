using System.Collections.Generic;
using System.IO;
using System.Linq;
using Organo.AR;
using UnityEngine;

namespace Organo.EditorTools
{
    public static partial class MatcherSynthEval
    {
        /// <summary>
        /// Gambar kartu seperti terlihat kamera di bingkai scan: diperkecil (rata-rata area) ke ±tinggi <paramref name="h"/>
        /// piksel, diputar <paramref name="deg"/> derajat (latar abu-abu), lalu dikaburkan 3x3.
        /// </summary>
        public static Color32[] CameraLike(MatcherSynth.Card card, int h, float deg, out int ow, out int oh)
        {
            oh = h; ow = Mathf.RoundToInt(h * CardMatcher.Aspect);
            var small = new Color32[ow * oh];
            float sx = card.w / (float)ow, sy = card.h / (float)oh;
            for (int y = 0; y < oh; y++)
                for (int x = 0; x < ow; x++)
                {
                    int x0 = (int)(x * sx), x1 = Mathf.Max(x0 + 1, (int)((x + 1) * sx)), y0 = (int)(y * sy), y1 = Mathf.Max(y0 + 1, (int)((y + 1) * sy));
                    int r = 0, g = 0, b = 0, n = 0;
                    for (int yy = y0; yy < y1 && yy < card.h; yy += 2)
                        for (int xx = x0; xx < x1 && xx < card.w; xx += 2) { var c = card.px[yy * card.w + xx]; r += c.r; g += c.g; b += c.b; n++; }
                    n = Mathf.Max(1, n);
                    small[y * ow + x] = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
                }
            if (Mathf.Abs(deg) > 0.01f)
            {
                var rot = new Color32[ow * oh];
                float rad = deg * Mathf.Deg2Rad, cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
                for (int y = 0; y < oh; y++)
                    for (int x = 0; x < ow; x++)
                    {
                        float dx = x - ow / 2f, dy = y - oh / 2f;
                        int u = Mathf.RoundToInt(cs * dx + sn * dy + ow / 2f), v = Mathf.RoundToInt(-sn * dx + cs * dy + oh / 2f);
                        rot[y * ow + x] = u >= 0 && u < ow && v >= 0 && v < oh ? small[v * ow + u] : new Color32(128, 128, 128, 255);
                    }
                small = rot;
            }
            var blur = new Color32[ow * oh];
            for (int y = 0; y < oh; y++)
                for (int x = 0; x < ow; x++)
                {
                    int r = 0, g = 0, b = 0, n = 0;
                    for (int j = -1; j <= 1; j++)
                        for (int i = -1; i <= 1; i++)
                        {
                            int xx = Mathf.Clamp(x + i, 0, ow - 1), yy = Mathf.Clamp(y + j, 0, oh - 1);
                            var c = small[yy * ow + xx]; r += c.r; g += c.g; b += c.b; n++;
                        }
                    blur[y * ow + x] = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
                }
            return blur;
        }

        /// <summary>Ciri kartu dari gambar "seperti kamera" (opsi: beberapa kemiringan).</summary>
        public static CardMatcher BuildSigs(List<MatcherSynth.Card> cards, int h, float[] angles, int samples)
        {
            var m = new CardMatcher();
            var d = new float[CardMatcher.Size];
            foreach (var c in cards)
                foreach (float a in angles)
                {
                    var px = CameraLike(c, h, a, out int w, out int hh);
                    CardMatcher.Describe(px, w, hh, new Rect(0, 0, w, hh), d, samples);
                    m.Add(c.id, (float[])d.Clone());
                }
            return m;
        }

        /// <summary>Ciri kartu baru dibuat, lalu uji kenal (Evaluate2) dan uji pelacak.</summary>
        public static void RebuildAndEvaluate()
        {
            CardSignatureBuilder.Build();
            Evaluate2();
            EvaluateTrack();
        }

        /// <summary>Skor pelacak: kartu yang bergeser sedikit (harus tetap tinggi) vs. kartu diangkat (harus turun).</summary>
        public static void EvaluateTrack()
        {
            var m = CardMatcher.Load();
            var cards = MatcherSynth.LoadCards();
            var rnd = new System.Random(3);
            var pos = new List<float>();
            var neg = new List<float>();
            var iou = new List<float>();
            foreach (var c in cards)
                for (int t = 0; t < 6; t++)
                {
                    var smp = Positive(cards, c, rnd, 0.4f, 1.0f);
                    var start = smp.truth;
                    start.center += new Vector2((float)(rnd.NextDouble() - 0.5) * 0.16f * start.width, (float)(rnd.NextDouble() - 0.5) * 0.16f * start.height);
                    float sc = m.Track(smp.px, CW, CH, start, c.id, out var where);
                    pos.Add(sc);
                    iou.Add(IoU(where, smp.truth));
                    // kartu diangkat: gambar tanpa kartu, letak lama
                    neg.Add(m.Track(Negative(rnd).px, CW, CH, smp.truth, c.id, out _));
                }
            var lines = new List<string>
            {
                "lacak kartu bergeser: skor persentil 5/25/50 = " + Pct(pos, 0.05f) + " / " + Pct(pos, 0.25f) + " / " + Pct(pos, 0.5f) + ", IoU median " + Pct(iou, 0.5f),
                "lacak tanpa kartu: skor persentil 50/95/maks = " + Pct(neg, 0.5f) + " / " + Pct(neg, 0.95f) + " / " + Pct(neg, 1f),
                $"ambang lacak {CardMatcher.TrackKeep:0.00}: kartu tetap terlacak {pos.Count(x => x >= CardMatcher.TrackKeep) * 100 / pos.Count}%, tanpa kartu tetap 'terlacak' {neg.Count(x => x >= CardMatcher.TrackKeep) * 100 / neg.Count}%",
            };
            Debug.Log("[Organo][EvalTrack] " + string.Join(" | ", lines));
            File.AppendAllLines("Library/OrganoPreview/eval2_pengenal_kartu.txt", lines);
        }

        static string Pct(List<float> xs, float p)
        {
            var l = xs.OrderBy(x => x).ToList();
            return l.Count == 0 ? "-" : l[Mathf.Clamp((int)(p * (l.Count - 1)), 0, l.Count - 1)].ToString("0.00");
        }

        /// <summary>Kartu wajar: satu kartu di meja, 82–100% bingkai, miring ±3°, cahaya biasa.</summary>
        public static Sample Normal(MatcherSynth.Card card, System.Random rnd)
        {
            var f = MatcherSynth.Frame;
            var canvas = new Color32[CW * CH];
            var bg = new Color32((byte)rnd.Next(90, 200), (byte)rnd.Next(80, 180), (byte)rnd.Next(60, 160), 255);
            for (int i = 0; i < canvas.Length; i++) canvas[i] = bg;
            float sc = 0.82f + (float)rnd.NextDouble() * 0.18f;
            var at = f.center + new Vector2((float)(rnd.NextDouble() - 0.5) * 0.1f * f.width, (float)(rnd.NextDouble() - 0.5) * 0.08f * f.height);
            PasteRotated(canvas, card, at, f.width * sc, f.height * sc, (float)(rnd.NextDouble() * 2 - 1) * 3f, 0.8f + (float)rnd.NextDouble() * 0.3f);
            return new Sample { px = MatcherSynth.BlurNoise(canvas, rnd, 8f), id = card.id, truth = new Rect(at.x - f.width * sc / 2, at.y - f.height * sc / 2, f.width * sc, f.height * sc) };
        }

        /// <summary>Bandingkan ukuran jendela pencarian kasar: kartu wajar, kartu sulit, dan gambar tanpa kartu.</summary>
        public static void EvaluateScales()
        {
            var m = CardMatcher.Load();
            var cards = MatcherSynth.LoadCards();
            var lines = new List<string>();
            var sets = new List<float[]> { new[] { 0.62f, 0.74f, 0.87f, 1f }, new[] { 0.74f, 0.86f, 0.98f, 1.1f } };
            foreach (bool centered in new[] { false, true })
            foreach (var set in sets)
            {

                m.Centered = centered;
                lines.Add(centered ? "-- tanpa templat (rata-rata kartu dikurangkan)" : "-- ciri biasa");
                var rnd = new System.Random(21);
                int nOk = 0, nWrong = 0, nAll = 0; var wrongs = new List<string>(); var weak = new List<string>();
                var normScores = new List<float>();
                foreach (var c in cards)
                {
                    int okc = 0;
                    for (int t = 0; t < 8; t++)
                    {
                        var r = m.Recognize(Normal(c, rnd).px, CW, CH, MatcherSynth.Frame);
                        nAll++;
                        bool sure = r.organId != null && r.score >= CardMatcher.RecognizeMin && r.margin >= CardMatcher.RecognizeMargin;
                        if (sure && r.organId == c.id) { nOk++; okc++; }
                        else if (sure) { nWrong++; wrongs.Add(c.id + ">" + r.organId); }
                        if (r.organId == c.id) normScores.Add(r.score);
                    }
                    if (okc < 5) weak.Add(c.id + " " + okc + "/8");
                }
                int hOk = 0, hWrong = 0, hAll = 0;
                foreach (var c in cards)
                    for (int t = 0; t < 4; t++)
                    {
                        var smp = Positive(cards, c, rnd);
                        var r = m.Recognize(smp.px, CW, CH, MatcherSynth.Frame);
                        hAll++;
                        bool sure = r.organId != null && r.score >= CardMatcher.RecognizeMin && r.margin >= CardMatcher.RecognizeMargin;
                        if (sure && r.organId == c.id) hOk++; else if (sure) hWrong++;
                    }
                var neg = new List<(float s, float g)>();
                for (int t = 0; t < 300; t++) { var r = m.Recognize(Negative(rnd).px, CW, CH, MatcherSynth.Frame); neg.Add((r.score, r.margin)); }
                int fp = neg.Count(n => n.s >= CardMatcher.RecognizeMin && n.g >= CardMatcher.RecognizeMargin);
                lines.Add($"skala {string.Join("/", set)}: wajar {nOk * 100 / nAll}% (salah {nWrong}: {string.Join(", ", wrongs.Take(6))}), skor wajar median {Median(normScores)}, lemah [{string.Join(", ", weak)}]");
                lines.Add($"    sulit {hOk * 100 / hAll}% (salah {hWrong}), bukan-kartu lolos {fp}/300, bukan-kartu maks {neg.Max(n => n.s):0.00}, maks dgn selisih>=0.05 {neg.Where(n => n.g >= 0.05f).Select(n => n.s).DefaultIfEmpty(0).Max():0.00}");
            }
            Debug.Log("[Organo][EvalScales] " + string.Join(" | ", lines));
            File.WriteAllLines("Library/OrganoPreview/eval_skala.txt", lines);
        }

        /// <summary>Ambang untuk ciri tanpa templat: kartu wajar, kartu sulit, & gambar tanpa kartu pada beberapa ambang.</summary>
        public static void EvaluateThresholds()
        {
            var m = CardMatcher.Load();
            m.Centered = true;

            var cards = MatcherSynth.LoadCards();
            var rnd = new System.Random(31);
            var norm = new List<(string id, CardMatcher.Result r)>();
            var hard = new List<(string id, CardMatcher.Result r)>();
            foreach (var c in cards)
            {
                for (int t = 0; t < 8; t++) norm.Add((c.id, m.Recognize(Normal(c, rnd).px, CW, CH, MatcherSynth.Frame)));
                for (int t = 0; t < 4; t++) hard.Add((c.id, m.Recognize(Positive(cards, c, rnd).px, CW, CH, MatcherSynth.Frame)));
            }
            var neg = new List<CardMatcher.Result>();
            for (int t = 0; t < 400; t++) neg.Add(m.Recognize(Negative(rnd).px, CW, CH, MatcherSynth.Frame));
            var lines = new List<string> { $"bukan-kartu: skor maks {neg.Max(n => n.score):0.00}, dgn selisih>=0.04 {neg.Where(n => n.margin >= 0.04f).Select(n => n.score).DefaultIfEmpty(0).Max():0.00}" };
            foreach (float thr in new[] { 0.46f, 0.48f, 0.5f, 0.52f, 0.55f })
                foreach (float mg in new[] { 0.04f, 0.06f, 0.08f })
                {
                    bool Ok(CardMatcher.Result r) => r.organId != null && r.score >= thr && r.margin >= mg;
                    int nOk = norm.Count(x => Ok(x.r) && x.r.organId == x.id), nW = norm.Count(x => Ok(x.r) && x.r.organId != x.id);
                    int hOk = hard.Count(x => Ok(x.r) && x.r.organId == x.id), hW = hard.Count(x => Ok(x.r) && x.r.organId != x.id);
                    int fp = neg.Count(Ok);
                    var weak = norm.GroupBy(x => x.id + (norm.IndexOf(x) % 16 < 8 ? "" : "")).Where(g => g.Count(x => Ok(x.r) && x.r.organId == x.id) < g.Count() / 2).Select(g => g.Key);
                    lines.Add($"ambang {thr:0.00} selisih {mg:0.00}: wajar {nOk * 100 / norm.Count}% (salah {nW}), sulit {hOk * 100 / hard.Count}% (salah {hW}), bukan-kartu lolos {fp}/400, lemah: {string.Join(" ", weak)}");
                }
            Debug.Log("[Organo][EvalThr] " + string.Join(" | ", lines));
            File.WriteAllLines("Library/OrganoPreview/eval_ambang.txt", lines);
        }

        /// <summary>Setiap kartu 16 gambar wajar: berapa yang dikenali, dan rincian yang gagal.</summary>
        public static void EvaluateEachCard()
        {
            var m = CardMatcher.Load();
            var cards = MatcherSynth.LoadCards();
            var rnd = new System.Random(77);
            var lines = new List<string>();
            foreach (var c in cards)
            {
                int ok = 0; var fails = new List<string>();
                for (int t = 0; t < 16; t++)
                {
                    var smp = Normal(c, rnd);
                    var r = m.Recognize(smp.px, CW, CH, MatcherSynth.Frame);
                    if (r.organId == c.id && r.score >= CardMatcher.RecognizeMin && r.margin >= CardMatcher.RecognizeMargin) ok++;
                    else
                    {
                        float iou = IoU(r.crop, smp.truth);
                        fails.Add($"{r.organId} {r.score:0.00}/{r.margin:0.00} letak {iou:0.00}");
                    }
                }
                lines.Add($"{c.id}{(c.cartoon ? " (kartun)" : "")}: {ok}/16" + (fails.Count > 0 ? " | gagal: " + string.Join("; ", fails.Take(4)) : ""));
            }
            Debug.Log("[Organo][EvalEach] " + string.Join(" | ", lines));
            File.WriteAllLines("Library/OrganoPreview/eval_tiap_kartu.txt", lines);
        }

        public static void EvalNow() { EvaluateEachCard(); EvaluateThresholds(); EvaluateWeak(); EvaluateTrack(); }

        /// <summary>Kartu lemah: skor tiap organ (3 teratas) untuk beberapa gambar wajar.</summary>
        public static void EvaluateWeak()
        {
            var m = CardMatcher.Load();
            m.Centered = true;

            var cards = MatcherSynth.LoadCards();
            var rnd = new System.Random(41);
            var lines = new List<string>();
            for (int ci = 0; ci < cards.Count; ci++)
            {
                var c = cards[ci];
                if (c.id != "hidung" && c.id != "reproperempuan" && c.id != "tenggorokan") continue;
                string style = ci < cards.Count / 2 ? "realistis" : "kartun";
                for (int t = 0; t < 4; t++)
                {
                    var smp = Normal(c, rnd);
                    var all = new List<string>();
                    foreach (var o in cards.Select(x => x.id).Distinct())
                    {
                        float sc = m.Track(smp.px, CW, CH, smp.truth, o, out var w);
                        all.Add(o + " " + m.ScoreOrgan(smp.px, CW, CH, w, o, 2).ToString("0.00"));
                    }
                    var r = m.Recognize(smp.px, CW, CH, MatcherSynth.Frame);
                    lines.Add($"{c.id} ({style}) → {r.organId} {r.score:0.00}/{r.margin:0.00} | di letak benar: " + string.Join(", ", all.OrderByDescending(x => float.Parse(x.Split(' ')[1], System.Globalization.CultureInfo.InvariantCulture)).Take(3)));
                }
            }
            Debug.Log("[Organo][EvalWeak] " + string.Join(" | ", lines));
            File.WriteAllLines("Library/OrganoPreview/eval_lemah.txt", lines);
        }

        /// <summary>Bandingkan beberapa cara membuat ciri kartu (kenal per gambar pada 0 gambar-bukan-kartu dikenali).</summary>
        public static void EvaluateSigs()
        {
            var cards = MatcherSynth.LoadCards();
            var variants = new List<(string name, CardMatcher m)>
            {
                ("file sekarang (tajam)", CardMatcher.Load()),
                ("kecil+kabur h240", BuildSigs(cards, 240, new[] { 0f }, 2)),
                ("kecil+kabur h160", BuildSigs(cards, 160, new[] { 0f }, 2)),
                ("kecil+kabur h240 miring ±5", BuildSigs(cards, 240, new[] { -5f, 0f, 5f }, 2)),
            };
            var lines = new List<string>();
            foreach (var (name, m) in variants)
            {
                var rnd = new System.Random(11);
                var pos = new List<(bool right, float score, float margin, float scale)>();
                foreach (var c in cards)
                    for (int t = 0; t < 8; t++)
                    {
                        var smp = Positive(cards, c, rnd);
                        var r = m.Recognize(smp.px, CW, CH, MatcherSynth.Frame);
                        pos.Add((r.organId == c.id, r.score, r.margin, smp.truth.height / MatcherSynth.Frame.height));
                    }
                var neg = new List<(float score, float margin)>();
                for (int t = 0; t < 300; t++) { var r = m.Recognize(Negative(rnd).px, CW, CH, MatcherSynth.Frame); neg.Add((r.score, r.margin)); }
                lines.Add($"== {name}: benar teratas {pos.Count(p => p.right) * 100 / pos.Count}%, skor median benar {Median(pos.Where(p => p.right).Select(p => p.score))}, negatif skor maks {neg.Max(n => n.score):0.00}");
                foreach (float mg in new[] { 0.03f, 0.05f, 0.07f, 0.09f })
                {
                    // ambang skor terendah yang tidak meloloskan satu pun gambar bukan-kartu dengan selisih ini
                    float thr = neg.Where(n => n.margin >= mg).Select(n => n.score).DefaultIfEmpty(0f).Max() + 0.01f;
                    int ok = pos.Count(p => p.right && p.score >= thr && p.margin >= mg);
                    int okBig = pos.Count(p => p.right && p.scale >= 0.7f && p.score >= thr && p.margin >= mg);
                    int big = pos.Count(p => p.scale >= 0.7f);
                    int wrong = pos.Count(p => !p.right && p.score >= thr && p.margin >= mg);
                    lines.Add($"   selisih {mg:0.00} → ambang {thr:0.00}: kenal {ok * 100 / pos.Count}% (kartu >=70% bingkai {okBig * 100 / Mathf.Max(1, big)}%), salah kartu {wrong}");
                }
            }
            Debug.Log("[Organo][EvalSigs]\n" + string.Join("\n", lines));
            File.WriteAllLines("Library/OrganoPreview/eval_ciri_kartu.txt", lines);
        }
    }
}
