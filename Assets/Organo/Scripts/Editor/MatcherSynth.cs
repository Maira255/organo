using System.Collections.Generic;
using System.IO;
using System.Linq;
using Organo.AR;
using UnityEngine;

namespace Organo.EditorTools
{
    /// <summary>
    /// Gambar kamera buatan untuk menguji pengenal kartu kamera biasa (<see cref="CardMatcher"/>): kartu di bingkai scan
    /// (80–102% bingkai, geser, gelap/terang), kartu lain berdempetan di kiri-kanan, buram & noise.
    /// </summary>
    public static class MatcherSynth
    {
        public const int CW = 216, CH = 384;

        public static Rect Frame
        {
            get
            {
                float fw = CW * 0.8f, fh = fw * 88f / 63f;
                return new Rect((CW - fw) / 2f, (CH - fh) / 2f, fw, fh);
            }
        }

        public struct Card { public string id; public Color32[] px; public int w, h; }

        public static List<Card> LoadCards()
        {
            var list = new List<Card>();
            var db = OrganoSetup.EnsureDatabase();
            foreach (var dir in new[] { OrganoCards.CardDir, OrganoCards.CartoonDir })
                foreach (var o in db.Ordered)
                {
                    var path = dir + "/" + o.id + ".png";
                    if (!File.Exists(path)) continue;
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(File.ReadAllBytes(path));
                    int w = tex.width, h = tex.height;
                    var raw = tex.GetPixels32();
                    Object.DestroyImmediate(tex);
                    var top = new Color32[raw.Length];
                    for (int y = 0; y < h; y++) System.Array.Copy(raw, (h - 1 - y) * w, top, y * w, w);
                    list.Add(new Card { id = o.id, px = top, w = w, h = h });
                }
            return list;
        }

        public static void Paste(Color32[] dst, Card card, Rect r, float gain)
        {
            for (int y = Mathf.Max(0, (int)r.yMin); y < Mathf.Min(CH, (int)r.yMax); y++)
                for (int x = Mathf.Max(0, (int)r.xMin); x < Mathf.Min(CW, (int)r.xMax); x++)
                {
                    int sx = Mathf.Clamp((int)((x - r.xMin) / r.width * card.w), 0, card.w - 1);
                    int sy = Mathf.Clamp((int)((y - r.yMin) / r.height * card.h), 0, card.h - 1);
                    var c = card.px[sy * card.w + sx];
                    dst[y * CW + x] = new Color32((byte)Mathf.Clamp(c.r * gain, 0, 255), (byte)Mathf.Clamp(c.g * gain, 0, 255), (byte)Mathf.Clamp(c.b * gain, 0, 255), 255);
                }
        }

        public static Color32[] BlurNoise(Color32[] src, System.Random rnd, float noise)
        {
            var dst = new Color32[src.Length];
            for (int y = 0; y < CH; y++)
                for (int x = 0; x < CW; x++)
                {
                    int r = 0, g = 0, b = 0, n = 0;
                    for (int j = -1; j <= 1; j++)
                        for (int i = -1; i <= 1; i++)
                        {
                            int xx = Mathf.Clamp(x + i, 0, CW - 1), yy = Mathf.Clamp(y + j, 0, CH - 1);
                            var c = src[yy * CW + xx]; r += c.r; g += c.g; b += c.b; n++;
                        }
                    float e = (float)(rnd.NextDouble() * 2 - 1) * noise;
                    dst[y * CW + x] = new Color32((byte)Mathf.Clamp(r / n + e, 0, 255), (byte)Mathf.Clamp(g / n + e, 0, 255), (byte)Mathf.Clamp(b / n + e, 0, 255), 255);
                }
            return dst;
        }

        static Color32[] Blank(System.Random rnd)
        {
            var canvas = new Color32[CW * CH];
            var bgc = new Color32((byte)rnd.Next(40, 235), (byte)rnd.Next(40, 235), (byte)rnd.Next(40, 235), 255);
            for (int i = 0; i < canvas.Length; i++) canvas[i] = bgc;
            return canvas;
        }

        /// <summary>Gambar kamera dengan kartu target di bingkai (target null = bingkai tanpa kartu).</summary>
        public static Color32[] Scene(List<Card> cards, Card? target, System.Random rnd)
        {
            var canvas = Blank(rnd);
            var f = Frame;
            float s = 0.8f + (float)rnd.NextDouble() * 0.22f;
            float w = f.width * s, h = f.height * s;
            float cx = f.center.x + (float)(rnd.NextDouble() - 0.5) * 0.12f * f.width, cy = f.center.y + (float)(rnd.NextDouble() - 0.5) * 0.1f * f.height;
            if (target.HasValue)
            {
                Paste(canvas, cards[rnd.Next(cards.Count)], new Rect(cx - w * 1.56f, cy - h / 2, w, h), 1f);
                Paste(canvas, cards[rnd.Next(cards.Count)], new Rect(cx + w * 0.56f, cy - h / 2, w, h), 1f);
                Paste(canvas, target.Value, new Rect(cx - w / 2, cy - h / 2, w, h), 0.6f + (float)rnd.NextDouble() * 0.5f);
                return BlurNoise(canvas, rnd, 10f);
            }
            if (rnd.Next(2) == 1) Paste(canvas, cards[rnd.Next(cards.Count)], new Rect(f.xMax - f.width * 0.25f, f.y, f.width, f.height), 1f);
            return BlurNoise(canvas, rnd, 14f);
        }

        /// <summary>
        /// Batch: bandingkan pengaturan pencocokan (titik sampel, ambang kecocokan & selisih) pada banyak gambar buatan.
        /// Unity -batchmode -executeMethod Organo.EditorTools.MatcherSynth.Evaluate
        /// </summary>
        public static void Evaluate()
        {
            var m = CardMatcher.Load();
            var cards = LoadCards();
            var lines = new List<string>();
            foreach (int samples in new[] { 2, 3 })
            {
                m.Samples = samples;
                var rnd = new System.Random(7);
                var pos = new List<(bool right, float score, float margin)>();
                foreach (var c in cards)
                    for (int t = 0; t < 6; t++)
                    {
                        var r = m.Match(Scene(cards, c, rnd), CW, CH, Frame);
                        pos.Add((r.organId == c.id, r.score, r.margin));
                    }
                var neg = new List<(float score, float margin)>();
                for (int t = 0; t < 120; t++)
                {
                    var r = m.Match(Scene(cards, null, rnd), CW, CH, Frame);
                    neg.Add((r.score, r.margin));
                }
                foreach (float thr in new[] { 0.35f, 0.4f, 0.45f, 0.5f })
                    foreach (float mg in new[] { 0.03f, 0.05f, 0.07f, 0.1f })
                    {
                        int ok = pos.Count(p => p.right && p.score >= thr && p.margin >= mg);
                        int wrong = pos.Count(p => !p.right && p.score >= thr && p.margin >= mg);
                        int fp = neg.Count(n => n.score >= thr && n.margin >= mg);
                        lines.Add($"sampel {samples} ambang {thr:0.00} selisih {mg:0.00}: benar {ok * 100 / pos.Count}% salah {wrong} dari {pos.Count}, kosong dikenali {fp} dari {neg.Count}");
                    }
            }
            Debug.Log("[Organo][Eval]\n" + string.Join("\n", lines));
            File.WriteAllLines("Library/OrganoPreview/eval_pengenal_kartu.txt", lines);
        }
    }
}
