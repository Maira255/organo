using System.Collections.Generic;
using System.IO;
using System.Linq;
using Organo.AR;
using UnityEngine;

namespace Organo.EditorTools
{
    /// <summary>
    /// Uji pengenal & pelacak kartu kamera biasa yang lebih mirip dunia nyata:
    /// - kartu: 45–105% bingkai, geser, miring sampai ±8°, gelap/terang, buram, noise, kartu lain di sampingnya;
    /// - bukan kartu: potongan tangkapan layar aplikasi (banyak gambar organ!) & tekstur acak (meja, buku, baju).
    /// Unity -batchmode -executeMethod Organo.EditorTools.MatcherSynth.Evaluate2  → Library/OrganoPreview/eval2_pengenal_kartu.txt
    /// </summary>
    public static partial class MatcherSynthEval
    {
        const int CW = MatcherSynth.CW, CH = MatcherSynth.CH;

        public static void PasteRotated(Color32[] dst, MatcherSynth.Card card, Vector2 center, float w, float h, float deg, float gain)
        {
            float rad = deg * Mathf.Deg2Rad, cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
            float ext = Mathf.Sqrt(w * w + h * h) / 2f + 1f;
            for (int y = Mathf.Max(0, (int)(center.y - ext)); y < Mathf.Min(CH, (int)(center.y + ext)); y++)
                for (int x = Mathf.Max(0, (int)(center.x - ext)); x < Mathf.Min(CW, (int)(center.x + ext)); x++)
                {
                    float dx = x - center.x, dy = y - center.y;
                    float u = (cs * dx + sn * dy) / w + 0.5f, v = (-sn * dx + cs * dy) / h + 0.5f;
                    if (u < 0 || u >= 1 || v < 0 || v >= 1) continue;
                    var c = card.px[(int)(v * card.h) * card.w + (int)(u * card.w)];
                    dst[y * CW + x] = new Color32((byte)Mathf.Clamp(c.r * gain, 0, 255), (byte)Mathf.Clamp(c.g * gain, 0, 255), (byte)Mathf.Clamp(c.b * gain, 0, 255), 255);
                }
        }

        static List<(Color32[] px, int w, int h)> s_Shots;

        static List<(Color32[] px, int w, int h)> Shots()
        {
            if (s_Shots != null) return s_Shots;
            s_Shots = new List<(Color32[], int, int)>();
            var files = Directory.GetFiles("Library/OrganoPreview", "ui_*.png")
                .Where(f => !Path.GetFileName(f).Contains("kartu") && !Path.GetFileName(f).Contains("koleksi")).OrderBy(f => f).ToList();
            for (int i = 0; i < files.Count; i += Mathf.Max(1, files.Count / 60))
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(File.ReadAllBytes(files[i]));
                int w = tex.width, h = tex.height;
                var raw = tex.GetPixels32();
                Object.DestroyImmediate(tex);
                var top = new Color32[raw.Length];
                for (int y = 0; y < h; y++) System.Array.Copy(raw, (h - 1 - y) * w, top, y * w, w);
                s_Shots.Add((top, w, h));
            }
            return s_Shots;
        }

        /// <summary>Latar "dunia nyata": potongan tangkapan layar aplikasi atau tekstur acak.</summary>
        static Color32[] Background(System.Random rnd, bool screenshot)
        {
            var dst = new Color32[CW * CH];
            if (screenshot && Shots().Count > 0)
            {
                var (px, w, h) = Shots()[rnd.Next(Shots().Count)];
                float s = 0.35f + (float)rnd.NextDouble() * 0.6f;              // bagian layar yang terlihat
                float sw = w * s, sh = sw * CH / CW;
                if (sh > h) { sh = h; sw = sh * CW / CH; }
                float ox = (float)rnd.NextDouble() * (w - sw), oy = (float)rnd.NextDouble() * (h - sh);
                for (int y = 0; y < CH; y++)
                    for (int x = 0; x < CW; x++)
                        dst[y * CW + x] = px[Mathf.Min(h - 1, (int)(oy + y * sh / CH)) * w + Mathf.Min(w - 1, (int)(ox + x * sw / CW))];
                return dst;
            }
            var a = new Color32((byte)rnd.Next(20, 240), (byte)rnd.Next(20, 240), (byte)rnd.Next(20, 240), 255);
            var b = new Color32((byte)rnd.Next(20, 240), (byte)rnd.Next(20, 240), (byte)rnd.Next(20, 240), 255);
            for (int y = 0; y < CH; y++)
                for (int x = 0; x < CW; x++)
                    dst[y * CW + x] = Color32.Lerp(a, b, (x + y) / (float)(CW + CH));
            int n = rnd.Next(4, 40);
            for (int i = 0; i < n; i++)
            {
                var c = new Color32((byte)rnd.Next(0, 256), (byte)rnd.Next(0, 256), (byte)rnd.Next(0, 256), 255);
                int x0 = rnd.Next(-40, CW), y0 = rnd.Next(-40, CH), rw = rnd.Next(6, 120), rh = rnd.Next(6, 160);
                bool round = rnd.Next(3) == 0, stripes = rnd.Next(4) == 0;
                for (int y = Mathf.Max(0, y0); y < Mathf.Min(CH, y0 + rh); y++)
                    for (int x = Mathf.Max(0, x0); x < Mathf.Min(CW, x0 + rw); x++)
                    {
                        if (round) { float u = (x - x0) / (float)rw - 0.5f, v = (y - y0) / (float)rh - 0.5f; if (u * u + v * v > 0.25f) continue; }
                        if (stripes && ((y - y0) / 4) % 2 == 0) continue;
                        dst[y * CW + x] = c;
                    }
            }
            return dst;
        }

        static Color32[] Gain(Color32[] src, float g)
        {
            var d = new Color32[src.Length];
            for (int i = 0; i < src.Length; i++)
                d[i] = new Color32((byte)Mathf.Clamp(src[i].r * g, 0, 255), (byte)Mathf.Clamp(src[i].g * g, 0, 255), (byte)Mathf.Clamp(src[i].b * g, 0, 255), 255);
            return d;
        }

        public struct Sample { public Color32[] px; public string id; public Rect truth; }

        public static Sample Positive(List<MatcherSynth.Card> cards, MatcherSynth.Card target, System.Random rnd, float minScale = 0.45f, float maxScale = 1.05f)
        {
            var canvas = Background(rnd, rnd.Next(2) == 0);
            var f = MatcherSynth.Frame;
            float s = minScale + (float)rnd.NextDouble() * (maxScale - minScale);
            float w = f.width * s, h = f.height * s;
            var c = new Vector2(f.center.x + (float)(rnd.NextDouble() - 0.5) * 0.25f * f.width, f.center.y + (float)(rnd.NextDouble() - 0.5) * 0.2f * f.height);
            if (rnd.Next(2) == 0)
            {
                PasteRotated(canvas, cards[rnd.Next(cards.Count)], c + new Vector2(-w * 1.08f, 0), w, h, 0f, 1f);
                PasteRotated(canvas, cards[rnd.Next(cards.Count)], c + new Vector2(w * 1.08f, 0), w, h, 0f, 1f);
            }
            PasteRotated(canvas, target, c, w, h, (float)(rnd.NextDouble() * 2 - 1) * 8f, 0.55f + (float)rnd.NextDouble() * 0.6f);
            return new Sample { px = MatcherSynth.BlurNoise(canvas, rnd, 12f), id = target.id, truth = new Rect(c.x - w / 2, c.y - h / 2, w, h) };
        }

        public static Sample Negative(System.Random rnd) =>
            new Sample { px = MatcherSynth.BlurNoise(Gain(Background(rnd, rnd.Next(3) != 0), 0.6f + (float)rnd.NextDouble() * 0.6f), rnd, 10f) };

        static float IoU(Rect a, Rect b)
        {
            float ix = Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin));
            float iy = Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
            float i = ix * iy;
            return i / (a.width * a.height + b.width * b.height - i);
        }

        /// <summary>Skor kenal awal (bingkai) + verifikasi (lacak halus di sekitar hasilnya) untuk satu gambar.</summary>
        public static (CardMatcher.Result first, float verified, Rect where) Detect(CardMatcher m, Color32[] px)
        {
            var r = m.Recognize(px, CW, CH, MatcherSynth.Frame);
            return (r, r.score, r.crop);
        }

        public static void Evaluate2()
        {
            var m = CardMatcher.Load();
            m.Samples = 2;
            var cards = MatcherSynth.LoadCards();
            var rnd = new System.Random(11);
            var lines = new List<string>();
            var pos = new List<(string id, bool right, float score, float margin, float verified, float iou, float scale)>();
            foreach (var c in cards)
                for (int t = 0; t < 10; t++)
                {
                    var smp = Positive(cards, c, rnd);
                    var (r, v, where) = Detect(m, smp.px);
                    pos.Add((c.id, r.organId == c.id, r.score, r.margin, v, r.organId == c.id ? IoU(where, smp.truth) : 0f, smp.truth.height / MatcherSynth.Frame.height));
                }
            var neg = new List<(float score, float margin, float verified)>();
            for (int t = 0; t < 400; t++)
            {
                var (r, v, _) = Detect(m, Negative(rnd).px);
                neg.Add((r.score, r.margin, v));
            }
            foreach (float thr in new[] { 0.5f })
                foreach (float mg in new[] { 0.04f, 0.06f, 0.08f, 0.1f, 0.12f })
                    foreach (float ver in new[] { 0.5f, 0.55f, 0.6f, 0.65f })
                    {
                        bool Ok(float s, float g, float v) => s >= thr && g >= mg && v >= ver;
                        int ok = pos.Count(p => p.right && Ok(p.score, p.margin, p.verified));
                        int okBig = pos.Count(p => p.right && p.scale >= 0.7f && Ok(p.score, p.margin, p.verified));
                        int big = pos.Count(p => p.scale >= 0.7f);
                        int wrong = pos.Count(p => !p.right && Ok(p.score, p.margin, p.verified));
                        int fp = neg.Count(n => Ok(n.score, n.margin, n.verified));
                        lines.Add($"ambang {thr:0.00} selisih {mg:0.00} verifikasi {ver:0.00}: kenal {ok * 100 / pos.Count}% (kartu >=70% bingkai {okBig * 100 / Mathf.Max(1, big)}%), salah kartu {wrong}, bukan-kartu dikenali {fp} dari {neg.Count}");
                    }
            lines.Add("");
            lines.Add("negatif teratas menurut skor (skor/selisih): " + string.Join("; ", neg.OrderByDescending(n => n.score).Take(8).Select(n => $"{n.score:0.00}/{n.margin:0.00}")));
            lines.Add("negatif teratas menurut selisih (skor/selisih): " + string.Join("; ", neg.OrderByDescending(n => n.margin).Take(8).Select(n => $"{n.score:0.00}/{n.margin:0.00}")));
            lines.Add("positif benar, selisih median " + Median(pos.Where(p => p.right).Select(p => p.margin)) + ", skor median " + Median(pos.Where(p => p.right).Select(p => p.score)));
            foreach (var band in new[] { (0.45f, 0.6f), (0.6f, 0.75f), (0.75f, 0.9f), (0.9f, 1.1f) })
            {
                var g = pos.Where(p => p.scale >= band.Item1 && p.scale < band.Item2).ToList();
                lines.Add($"  kartu {band.Item1 * 100:0}-{band.Item2 * 100:0}% bingkai: benar {g.Count(p => p.right)}/{g.Count}, kenal (0.60 & 0.08) {g.Count(p => p.right && p.score >= 0.6f && p.margin >= 0.08f)}");
            }
            lines.Add("positif benar: verifikasi median " + Median(pos.Where(p => p.right).Select(p => p.verified)) + ", IoU median " + Median(pos.Where(p => p.right).Select(p => p.iou)));
            foreach (var g in pos.GroupBy(p => p.id))
                lines.Add($"  {g.Key}: benar {g.Count(p => p.right)}/{g.Count()}, verifikasi median {Median(g.Where(p => p.right).Select(p => p.verified))}");

            // pelacak: kartu jauh/dekat di mana saja, dicari ulang di seluruh gambar (organ diketahui)
            int found = 0, n2 = 0; float iouSum = 0;
            var rnd2 = new System.Random(5);
            foreach (var c in cards)
                for (int t = 0; t < 3; t++)
                {
                    var smp = Positive(cards, c, rnd2, 0.35f, 1.0f);
                    var r = m.Search(smp.px, CW, CH, c.id);
                    n2++;
                    float iou = IoU(r.crop, smp.truth);
                    if (iou > 0.5f) found++;
                    iouSum += iou;
                }
            lines.Add($"pelacak (cari ulang organ yang diketahui): letak benar {found}/{n2}, IoU rata-rata {iouSum / n2:0.00}");
            var negSearch = new List<float>();
            for (int t = 0; t < 60; t++) negSearch.Add(m.Search(Negative(rnd2).px, CW, CH, cards[t % cards.Count].id).score);
            lines.Add("cari ulang di gambar tanpa kartu, skor teratas: " + string.Join(", ", negSearch.OrderByDescending(x => x).Take(6).Select(x => x.ToString("0.00"))));
            var posSearch = new List<float>();
            foreach (var c in cards) posSearch.Add(m.Search(Positive(cards, c, rnd2, 0.35f, 1.0f).px, CW, CH, c.id).score);
            lines.Add("cari ulang dengan kartu, skor terendah: " + string.Join(", ", posSearch.OrderBy(x => x).Take(6).Select(x => x.ToString("0.00"))));

            // waktu
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var one = Positive(cards, cards[0], rnd2);
            for (int i = 0; i < 20; i++) m.Track(one.px, CW, CH, one.truth, cards[0].id, out _);
            lines.Add($"waktu (Editor): lacak {sw.ElapsedMilliseconds / 20f:0.0} ms");
            sw.Restart();
            for (int i = 0; i < 5; i++) m.Search(one.px, CW, CH, cards[0].id);
            lines.Add($"waktu (Editor): cari ulang 1 organ {sw.ElapsedMilliseconds / 5f:0.0} ms");
            sw.Restart();
            for (int i = 0; i < 3; i++) m.Search(one.px, CW, CH, null);
            lines.Add($"waktu (Editor): cari semua kartu {sw.ElapsedMilliseconds / 3f:0.0} ms");
            sw.Restart();
            for (int i = 0; i < 5; i++) m.Match(one.px, CW, CH, MatcherSynth.Frame);
            lines.Add($"waktu (Editor): kenal di bingkai {sw.ElapsedMilliseconds / 5f:0.0} ms");

            Debug.Log("[Organo][Eval2]\n" + string.Join("\n", lines));
            File.WriteAllLines("Library/OrganoPreview/eval2_pengenal_kartu.txt", lines);
        }

        static string Median(IEnumerable<float> xs)
        {
            var l = xs.OrderBy(x => x).ToList();
            return l.Count == 0 ? "-" : l[l.Count / 2].ToString("0.00");
        }
    }
}
