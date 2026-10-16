using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Organo.AR
{
    /// <summary>
    /// Pengenal kartu tanpa ARCore (HP yang tidak mendukung AR, kamera biasa). Anak menempatkan kartu di bingkai scan,
    /// jadi letak & ukuran kartu di gambar kamera kira-kira sudah diketahui: isi bingkai (dengan sedikit geser & skala)
    /// diperkecil menjadi 24x33 piksel warna (Y, Cr, Cb), lalu dicocokkan ke semua kartu (kedua gaya).
    /// Uji buatan (kartu miring/kecil, buram, gelap, kartu lain berdempetan, kartu di layar laptop): tidak pernah salah kartu,
    /// tidak pernah "mengenali" bingkai kosong, dan ±80% gambar kamera langsung dikenali (Editor: MatcherSynth.Evaluate).
    /// Ciri kartu dibuat di Editor (<c>OrganoCardSignatures.bytes</c> di Resources) dengan fungsi <see cref="Describe"/> yang sama.
    /// </summary>
    public class CardMatcher
    {
        public const int W = 24, H = 33;
        public const int Size = W * H * 3;
        /// <summary>Kecocokan minimum & selisih minimum dengan organ kedua terbaik (<see cref="Match"/>, cara lama).</summary>
        public const float MinScore = 0.40f, MinMargin = 0.05f;
        /// <summary>
        /// Ambang <see cref="Recognize"/>: skor halus & selisih dengan organ kedua. Uji buatan dengan latar "dunia nyata"
        /// (potongan layar aplikasi, tekstur acak): gambar tanpa kartu tertinggi 0,59 (selisih 0,05); kartu yang benar median 0,71.
        /// </summary>
        public const float RecognizeMin = 0.50f, RecognizeMargin = 0.06f;
        /// <summary>Cari ulang kartu yang sudah dikenali di seluruh gambar (<see cref="Search"/>, 1 organ): gambar tanpa kartu ≤ 0,52.</summary>
        public const float SearchMin = 0.56f;
        /// <summary>Pelacak: kartu dianggap masih terlihat selama kecocokan di letak terbarunya setidaknya ini.</summary>
        public const float TrackKeep = 0.5f;
        public const string ResourceName = "OrganoCardSignatures";

        readonly List<string> m_Ids = new List<string>();
        readonly List<float[]> m_Desc = new List<float[]>();
        // ciri kartu tegak (bukan versi miring "id~5"): dipakai pencarian kasar supaya ringan
        readonly List<bool> m_Upright = new List<bool>();
        readonly List<string> m_Names = new List<string>();
        readonly float[] m_Buf = new float[Size];

        public int Count => m_Ids.Count;

        // gaya kartu tiap ciri (0 realistis, 1 kartun): tiap gaya punya templat sendiri untuk mencari letak kartu
        readonly List<int> m_Style = new List<int>();

        /// <summary>
        /// Tambah ciri kartu. "lambung" = kartu Realistis, "lambung@k" = kartu Kartun, akhiran "~5" = dimiringkan 5° (ciri
        /// tambahan untuk kartu miring).
        /// </summary>
        public void Add(string organId, float[] descriptor)
        {
            int tilde = organId.IndexOf('~'), at = organId.IndexOf('@');
            int end = organId.Length;
            if (tilde >= 0) end = Mathf.Min(end, tilde);
            if (at >= 0) end = Mathf.Min(end, at);
            m_Names.Add(organId);
            m_Ids.Add(organId.Substring(0, end));
            m_Upright.Add(tilde < 0);
            m_Style.Add(at >= 0 ? 1 : 0);
            m_Desc.Add(descriptor);
        }

        /// <summary>Ciri kartu dari Resources (null kalau belum dibuat).</summary>
        public static CardMatcher Load()
        {
            var asset = Resources.Load<TextAsset>(ResourceName);
            return asset != null ? FromBytes(asset.bytes) : null;
        }

        public static CardMatcher FromBytes(byte[] bytes)
        {
            var m = new CardMatcher();
            using (var r = new BinaryReader(new MemoryStream(bytes)))
            {
                int n = r.ReadInt32(), w = r.ReadInt32(), h = r.ReadInt32();
                if (w != W || h != H) return m;
                for (int i = 0; i < n; i++)
                {
                    string id = r.ReadString();
                    var d = new float[Size];
                    for (int k = 0; k < Size; k++) d[k] = r.ReadSingle();
                    m.Add(id, d);
                }
            }
            return m;
        }

        public byte[] ToBytes()
        {
            using (var ms = new MemoryStream())
            using (var wr = new BinaryWriter(ms))
            {
                wr.Write(m_Ids.Count); wr.Write(W); wr.Write(H);
                for (int i = 0; i < m_Ids.Count; i++)
                {
                    wr.Write(m_Names[i]);
                    foreach (var v in m_Desc[i]) wr.Write(v);
                }
                wr.Flush();
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Ciri satu potongan gambar. <paramref name="px"/> berurutan baris dari ATAS (bukan urutan tekstur Unity),
        /// <paramref name="crop"/> dalam piksel dengan titik (0,0) di kiri atas. Hasil: Y, Cr, Cb kecil yang tiap kanalnya
        /// dikurangi rata-ratanya & dinormalkan (tahan terang/gelap & kontras), Cr/Cb berbobot 0,6.
        /// </summary>
        public static void Describe(Color32[] px, int width, int height, Rect crop, float[] outDesc, int samples = 3)
        {
            float cw = crop.width / W, ch = crop.height / H;
            int o = 0;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float sr = 0, sg = 0, sb = 0;
                    for (int sy = 0; sy < samples; sy++)
                        for (int sx = 0; sx < samples; sx++)
                        {
                            float fx = crop.x + (x + (sx + 0.5f) / samples) * cw - 0.5f;
                            float fy = crop.y + (y + (sy + 0.5f) / samples) * ch - 0.5f;
                            Sample(px, width, height, fx, fy, out float r, out float g, out float b);
                            sr += r; sg += g; sb += b;
                        }
                    float inv = 1f / (samples * samples);
                    float R = sr * inv, G = sg * inv, B = sb * inv;
                    float Y = 0.299f * R + 0.587f * G + 0.114f * B;
                    outDesc[o] = Y;
                    outDesc[W * H + o] = (R - Y) * 0.713f;
                    outDesc[2 * W * H + o] = (B - Y) * 0.564f;
                    o++;
                }
            float total = 0f;
            for (int c = 0; c < 3; c++)
            {
                int a = c * W * H, b = a + W * H;
                float mean = 0f;
                for (int i = a; i < b; i++) mean += outDesc[i];
                mean /= W * H;
                float norm = 0f;
                for (int i = a; i < b; i++) { outDesc[i] -= mean; norm += outDesc[i] * outDesc[i]; }
                norm = Mathf.Sqrt(norm);
                float scale = (norm > 1e-3f ? 1f / norm : 0f) * (c == 0 ? 1f : 0.6f);
                for (int i = a; i < b; i++) { outDesc[i] *= scale; total += outDesc[i] * outDesc[i]; }
            }
            total = Mathf.Sqrt(total);
            if (total > 1e-6f) for (int i = 0; i < Size; i++) outDesc[i] /= total;
        }

        static void Sample(Color32[] px, int width, int height, float fx, float fy, out float r, out float g, out float b)
        {
            fx = Mathf.Clamp(fx, 0f, width - 1.001f);
            fy = Mathf.Clamp(fy, 0f, height - 1.001f);
            int x0 = (int)fx, y0 = (int)fy;
            float tx = fx - x0, ty = fy - y0;
            var c00 = px[y0 * width + x0]; var c10 = px[y0 * width + x0 + 1];
            var c01 = px[(y0 + 1) * width + x0]; var c11 = px[(y0 + 1) * width + x0 + 1];
            float w00 = (1 - tx) * (1 - ty), w10 = tx * (1 - ty), w01 = (1 - tx) * ty, w11 = tx * ty;
            r = c00.r * w00 + c10.r * w10 + c01.r * w01 + c11.r * w11;
            g = c00.g * w00 + c10.g * w10 + c01.g * w01 + c11.g * w11;
            b = c00.b * w00 + c10.b * w10 + c01.b * w01 + c11.b * w11;
        }

        public struct Result
        {
            public string organId;
            public float score, margin;
            /// <summary>Potongan gambar (piksel, kiri atas) tempat organ pemenang paling cocok.</summary>
            public Rect crop;
            public bool Confident => organId != null && score >= MinScore && margin >= MinMargin;
        }

        /// <summary>
        /// Cocokkan isi bingkai (<paramref name="frame"/>, piksel, kiri atas) ke semua kartu. Kartu jarang pas sekali di
        /// bingkai, jadi dicoba 3 skala x 3 geser mendatar x 3 geser tegak.
        /// </summary>
        /// <summary>Titik sampel per sel ciri (3 = 3x3; lebih banyak = lebih halus, lebih lambat).</summary>
        public int Samples = 3;

        public Result Match(Color32[] px, int width, int height, Rect frame)
        {
            var best = new Dictionary<string, float>();
            var where = new Dictionary<string, Rect>();
            foreach (float s in new[] { 0.82f, 0.92f, 1f })
                foreach (float dx in new[] { -0.06f, 0f, 0.06f })
                    foreach (float dy in new[] { -0.05f, 0f, 0.05f })
                    {
                        float w = frame.width * s, h = frame.height * s;
                        float cx = frame.center.x + dx * frame.width, cy = frame.center.y + dy * frame.height;
                        var crop = Rect.MinMaxRect(Mathf.Max(0, cx - w / 2), Mathf.Max(0, cy - h / 2), Mathf.Min(width, cx + w / 2), Mathf.Min(height, cy + h / 2));
                        if (crop.width < W || crop.height < H) continue;
                        Describe(px, width, height, crop, m_Buf, Samples);
                        for (int i = 0; i < m_Desc.Count; i++)
                        {
                            float dot = 0f;
                            var d = m_Desc[i];
                            for (int k = 0; k < Size; k++) dot += d[k] * m_Buf[k];
                            if (!best.TryGetValue(m_Ids[i], out float b) || dot > b) { best[m_Ids[i]] = dot; where[m_Ids[i]] = crop; }
                        }
                    }
            var res = new Result { score = -1f };
            float second = -1f;
            foreach (var kv in best)
            {
                if (kv.Value > res.score) { second = res.score; res.score = kv.Value; res.organId = kv.Key; }
                else if (kv.Value > second) second = kv.Value;
            }
            res.margin = res.score - second;
            if (res.organId != null) res.crop = where[res.organId];
            return res;
        }

        // ---------------------------------------------------------------- pelacak kartu (kamera biasa)

        /// <summary>Kartu selalu tegak 63:88 (lebar:tinggi).</summary>
        public const float Aspect = 63f / 88f;
        readonly float[] m_Track = new float[Size];
        readonly Dictionary<string, int[]> m_Index = new Dictionary<string, int[]>();

        int[] Indices(string organId)
        {
            if (organId == null) return new int[0];
            if (m_Index.TryGetValue(organId, out var idx)) return idx;
            var list = new List<int>();
            for (int i = 0; i < m_Ids.Count; i++) if (m_Ids[i] == organId) list.Add(i);
            return m_Index[organId] = list.ToArray();
        }

        /// <summary>
        /// Menentukan KARTU MANA setelah "templat kartu" dibuang: semua kartu punya bingkai, pita judul, & warna dasar yang
        /// sama, jadi rata-rata semua ciri kartu dikurangkan dari ciri kartu & dari gambar kamera; yang dibandingkan tinggal
        /// bagian pembeda (gambar organ, judul). Uji buatan: kartu wajar tidak lagi tertukar (mis. hati ↔ usus halus) dan
        /// gambar tanpa kartu jauh di bawah ambang. Templatnya sendiri justru dipakai untuk mencari LETAK kartu
        /// (<see cref="Locate"/>), dan pelacak memakai ciri biasa (halus terhadap geser).
        /// </summary>
        public bool Centered = true;
        float[] m_Mean;
        readonly List<float[]> m_Templates = new List<float[]>();
        readonly List<float[]> m_DescC = new List<float[]>();

        void Prepare()
        {
            if (m_Mean != null && m_DescC.Count == m_Desc.Count) return;
            m_Mean = new float[Size];
            foreach (var d in m_Desc) for (int k = 0; k < Size; k++) m_Mean[k] += d[k] / m_Desc.Count;
            m_Templates.Clear();
            for (int style = 0; style <= 1; style++)
            {
                var t = new float[Size];
                int n = 0;
                for (int i = 0; i < m_Desc.Count; i++)
                    if (m_Style[i] == style) { n++; for (int k = 0; k < Size; k++) t[k] += m_Desc[i][k]; }
                if (n == 0) continue;
                Normalize(t);
                m_Templates.Add(t);
            }
            m_DescC.Clear();
            foreach (var d in m_Desc)
            {
                var c = new float[Size];
                for (int k = 0; k < Size; k++) c[k] = d[k] - m_Mean[k];
                Normalize(c);
                m_DescC.Add(c);
            }
        }

        static void Normalize(float[] v)
        {
            float n = 0f;
            for (int k = 0; k < v.Length; k++) n += v[k] * v[k];
            n = Mathf.Sqrt(n);
            if (n > 1e-6f) for (int k = 0; k < v.Length; k++) v[k] /= n;
        }

        /// <summary>Ciri gambar kamera dibuat sebanding dengan ciri kartu yang sudah dibuang templatnya.</summary>
        void CenterQuery(float[] q)
        {
            Prepare();
            for (int k = 0; k < Size; k++) q[k] -= m_Mean[k];
            Normalize(q);
        }

        float Dot(int i, float[] d, bool centered = false)
        {
            float dot = 0f;
            var s = centered ? m_DescC[i] : m_Desc[i];
            for (int k = 0; k < Size; k++) dot += s[k] * d[k];
            return dot;
        }

        /// <summary>Kemiripan dengan templat kartu (gaya Realistis atau Kartun, mana yang lebih mirip).</summary>
        float DotTemplate(float[] d)
        {
            Prepare();
            float best = -1f;
            foreach (var t in m_Templates)
            {
                float dot = 0f;
                for (int k = 0; k < Size; k++) dot += t[k] * d[k];
                best = Mathf.Max(best, dot);
            }
            return best;
        }

        /// <summary>
        /// Letak kartu (kartu apa pun) di sekitar bingkai: jendela yang paling mirip "templat kartu" (rata-rata semua kartu:
        /// bingkai, pita judul, warna dasar). Cari kasar 5 ukuran x 5x5 geser, lalu diperhalus.
        /// </summary>
        public Rect Locate(Color32[] px, int width, int height, Rect frame, out float score)
        {
            score = -1f;
            var best = frame;
            foreach (float s in new[] { 0.7f, 0.8f, 0.9f, 1f, 1.1f })
                for (int yi = -2; yi <= 2; yi++)
                    for (int xi = -2; xi <= 2; xi++)
                    {
                        var r = Window(frame.center.x + xi * 0.05f * frame.width, frame.center.y + yi * 0.05f * frame.height, frame.height * s);
                        if (!Fits(r, width, height)) continue;
                        Describe(px, width, height, r, m_Track, 1);
                        float d = DotTemplate(m_Track);
                        if (d > score) { score = d; best = r; }
                    }
            for (int pass = 0; pass < 2; pass++)
            {
                float move = pass == 0 ? 0.03f : 0.015f, zoom = pass == 0 ? 0.04f : 0.02f;
                var c = best.center; float h = best.height;
                for (int si = -1; si <= 1; si++)
                    for (int yi = -1; yi <= 1; yi++)
                        for (int xi = -1; xi <= 1; xi++)
                        {
                            float hh = h * (1f + si * zoom);
                            var r = Window(c.x + xi * move * hh * Aspect, c.y + yi * move * hh, hh);
                            if (!Fits(r, width, height)) continue;
                            Describe(px, width, height, r, m_Track, 1);
                            float d = DotTemplate(m_Track);
                            if (d > score) { score = d; best = r; }
                        }
            }
            return best;
        }

        public static Rect Window(float cx, float cy, float h) => new Rect(cx - h * Aspect / 2f, cy - h / 2f, h * Aspect, h);

        static bool Fits(Rect r, int width, int height) =>
            r.width >= W && r.height >= H && r.xMin >= -0.08f * r.width && r.yMin >= -0.08f * r.height
            && r.xMax <= width + 0.08f * r.width && r.yMax <= height + 0.08f * r.height;

        /// <summary>Kecocokan potongan <paramref name="crop"/> dengan kartu organ ini (gaya mana pun yang paling cocok).</summary>
        public float ScoreOrgan(Color32[] px, int width, int height, Rect crop, string organId, int samples = 1, bool centered = false)
        {
            var idx = Indices(organId);
            if (idx.Length == 0 || !Fits(crop, width, height)) return -1f;
            Describe(px, width, height, crop, m_Track, samples);
            if (centered) CenterQuery(m_Track);
            float best = -1f;
            foreach (int i in idx) best = Mathf.Max(best, Dot(i, m_Track, centered));
            return best;
        }

        /// <summary>Organ yang paling cocok dengan satu potongan (semua kartu), beserta selisih dengan organ kedua.</summary>
        public Result MatchWindow(Color32[] px, int width, int height, Rect crop, int samples = 2)
        {
            var res = new Result { score = -1f, crop = crop };
            if (!Fits(crop, width, height)) return res;
            Describe(px, width, height, crop, m_Track, samples);
            if (Centered) CenterQuery(m_Track);
            float second = -1f;
            var best = new Dictionary<string, float>();
            for (int i = 0; i < m_Desc.Count; i++)
            {
                float d = Dot(i, m_Track, Centered);
                if (!best.TryGetValue(m_Ids[i], out float b) || d > b) best[m_Ids[i]] = d;
            }
            foreach (var kv in best)
            {
                if (kv.Value > res.score) { second = res.score; res.score = kv.Value; res.organId = kv.Key; }
                else if (kv.Value > second) second = kv.Value;
            }
            res.margin = res.score - second;
            return res;
        }

        /// <summary>
        /// Lacak kartu organ yang sudah dikenali: cari di sekitar letak sebelumnya (geser & skala), lalu perhalus.
        /// Hasil: kecocokan terbaik dan letak kartu itu (letak lama bila tidak ada yang lebih baik).
        /// </summary>
        public float Track(Color32[] px, int width, int height, Rect prev, string organId, out Rect best)
        {
            best = prev;
            float bestScore = ScoreOrgan(px, width, height, prev, organId);
            // kasar: geser 12% & skala 12%; lalu halus: geser 4% & skala 5%
            for (int pass = 0; pass < 2; pass++)
            {
                float move = pass == 0 ? 0.12f : 0.04f, zoom = pass == 0 ? 0.12f : 0.05f;
                var c = best.center; float h = best.height;
                for (int si = -1; si <= 1; si++)
                    for (int yi = -1; yi <= 1; yi++)
                        for (int xi = -1; xi <= 1; xi++)
                        {
                            if (si == 0 && yi == 0 && xi == 0) continue;
                            float hh = h * (1f + si * zoom);
                            var r = Window(c.x + xi * move * hh * Aspect, c.y + yi * move * hh, hh);
                            float sc = ScoreOrgan(px, width, height, r, organId);
                            if (sc > bestScore) { bestScore = sc; best = r; }
                        }
            }
            return bestScore;
        }

        /// <summary>
        /// Kenali kartu di sekitar bingkai scan: (1) cari kasar 4 ukuran (62–100% bingkai) x 3x3 geser, catat letak terbaik
        /// tiap organ; (2) tiga organ teratas diperhalus letaknya (<see cref="Track"/>); pemenang = skor halus tertinggi,
        /// selisih = dengan skor halus organ kedua. Kartu yang agak jauh / tidak pas di tengah tetap dikenali, dan gambar
        /// tanpa kartu (meja, buku, layar) cocok "sedikit" dengan banyak organ sekaligus sehingga selisihnya kecil.
        /// </summary>
        /// <summary>
        /// Kenali kartu di sekitar bingkai scan: (1) letak kartu dicari dengan templat kartu (<see cref="Locate"/>);
        /// (2) di letak itu semua kartu dinilai dengan ciri pembeda (templat dibuang), tiga organ teratas diperhalus letaknya
        /// sedikit; pemenang = skor tertinggi, selisih = dengan organ kedua. Gambar tanpa kartu (meja, buku, layar) hanya
        /// cocok "sedikit" dengan kartu mana pun.
        /// </summary>
        public Result Recognize(Color32[] px, int width, int height, Rect frame)
        {
            var at = Locate(px, width, height, frame, out _);
            var res = new Result { score = -1f, crop = at };
            if (!Fits(at, width, height)) return res;
            Describe(px, width, height, at, m_Buf, 2);
            if (Centered) CenterQuery(m_Buf);
            var first = new Dictionary<string, float>();
            for (int i = 0; i < m_Desc.Count; i++)
            {
                float d = Dot(i, m_Buf, Centered);
                if (!first.TryGetValue(m_Ids[i], out float b) || d > b) first[m_Ids[i]] = d;
            }
            float second = -1f;
            foreach (var kv in first.OrderByDescending(x => x.Value).Take(3))
            {
                // perhalus untuk organ ini: geser 6% & skala 7%, lalu geser 3% & skala 3,5%
                float v = kv.Value;
                var where = at;
                for (int pass = 0; pass < 2; pass++)
                {
                    float move = pass == 0 ? 0.06f : 0.03f, zoom = pass == 0 ? 0.07f : 0.035f;
                    var c = where.center; float h = where.height;
                    for (int si = -1; si <= 1; si++)
                        for (int yi = -1; yi <= 1; yi++)
                            for (int xi = -1; xi <= 1; xi++)
                            {
                                if (si == 0 && yi == 0 && xi == 0) continue;
                                float hh = h * (1f + si * zoom);
                                var r = Window(c.x + xi * move * hh * Aspect, c.y + yi * move * hh, hh);
                                float sc = ScoreOrgan(px, width, height, r, kv.Key, 1, Centered);
                                if (sc > v) { v = sc; where = r; }
                            }
                }
                v = Mathf.Max(v, ScoreOrgan(px, width, height, where, kv.Key, 2, Centered));
                if (v > res.score) { second = res.score; res.score = v; res.organId = kv.Key; res.crop = where; }
                else if (v > second) second = v;
            }
            res.margin = res.score - second;
            return res;
        }

        /// <summary>
        /// Cari kartu di seluruh gambar (kartu hilang dari pelacak / kartu lain diarahkan ke kamera). organId null = semua
        /// kartu (lebih berat, jaringnya lebih kasar). Hasil terbaik diperhalus dengan <see cref="Track"/>.
        /// </summary>
        public Result Search(Color32[] px, int width, int height, string organId)
        {
            var res = new Result { score = -1f };
            float[] heights = organId != null ? new[] { 0.32f, 0.45f, 0.6f, 0.78f } : new[] { 0.4f, 0.58f, 0.78f };
            float step = organId != null ? 0.22f : 0.3f;
            foreach (float fh in heights)
            {
                float h = height * fh, w = h * Aspect;
                if (w > width * 1.05f) { w = width * 1.05f; h = w / Aspect; }
                for (float cy = h / 2f; cy <= height - h / 2f + 0.5f; cy += Mathf.Max(4f, h * step))
                    for (float cx = w / 2f; cx <= width - w / 2f + 0.5f; cx += Mathf.Max(4f, w * step))
                    {
                        var r = Window(cx, cy, h);
                        if (organId != null)
                        {
                            float sc = ScoreOrgan(px, width, height, r, organId);
                            if (sc > res.score) { res.score = sc; res.organId = organId; res.crop = r; }
                        }
                        else
                        {
                            var m = MatchWindow(px, width, height, r, 1);
                            if (m.score > res.score) res = m;
                        }
                    }
            }
            if (res.organId != null)
            {
                float refined = Track(px, width, height, res.crop, res.organId, out var rr);
                if (refined > res.score) { res.score = refined; res.crop = rr; }
            }
            return res;
        }

        // ---------------------------------------------------------------- arah gambar kamera

        /// <summary>
        /// Gambar kamera mentah (urutan <see cref="WebCamTexture.GetPixels32()"/>: baris bawah dulu) menjadi gambar tegak
        /// kecil seperti yang tampil di layar (baris atas dulu): tiap <paramref name="step"/> x step piksel dirata-rata,
        /// dicerminkan bila perlu, lalu diputar searah jarum jam sebesar <paramref name="rotation"/>
        /// (<see cref="WebCamTexture.videoRotationAngle"/>).
        /// </summary>
        public static Color32[] Upright(Color32[] tex, int width, int height, int step, int rotation, bool mirrorX, bool flipY, out int outW, out int outH, Color32[] reuse = null)
        {
            step = Mathf.Max(1, step);
            int sw = width / step, sh = height / step;
            rotation = ((rotation % 360) + 360) % 360;
            bool side = rotation == 90 || rotation == 270;
            outW = side ? sh : sw;
            outH = side ? sw : sh;
            var dst = reuse != null && reuse.Length == sw * sh ? reuse : new Color32[sw * sh];
            float inv = 1f / (step * step);
            for (int y = 0; y < sh; y++)          // y: baris dari atas pada gambar kecil yang belum diputar
                for (int x = 0; x < sw; x++)
                {
                    int r = 0, g = 0, b = 0;
                    for (int j = 0; j < step; j++)
                    {
                        int ty = y * step + j;
                        int row = flipY ? ty : height - 1 - ty;        // tekstur dimulai dari baris bawah
                        for (int i = 0; i < step; i++)
                        {
                            int tx = x * step + i;
                            var c = tex[row * width + (mirrorX ? width - 1 - tx : tx)];
                            r += c.r; g += c.g; b += c.b;
                        }
                    }
                    var avg = new Color32((byte)(r * inv), (byte)(g * inv), (byte)(b * inv), 255);
                    int ux, uy;
                    switch (rotation)
                    {
                        case 90: ux = sh - 1 - y; uy = x; break;
                        case 180: ux = sw - 1 - x; uy = sh - 1 - y; break;
                        case 270: ux = y; uy = sw - 1 - x; break;
                        default: ux = x; uy = y; break;
                    }
                    dst[uy * outW + ux] = avg;
                }
            return dst;
        }

        /// <summary>
        /// Bagian gambar tegak (uw x uh) yang tampil di layar (sw x sh) dengan cara "cover" (memenuhi layar, tengahnya
        /// terlihat): skala & geser untuk memetakan titik layar ke piksel gambar.
        /// </summary>
        public static Rect ScreenToImage(Rect screenRect, float sw, float sh, int uw, int uh)
        {
            float k = Mathf.Max(sw / uw, sh / uh);
            float ox = (sw - uw * k) / 2f, oy = (sh - uh * k) / 2f;
            return Rect.MinMaxRect((screenRect.xMin - ox) / k, (screenRect.yMin - oy) / k, (screenRect.xMax - ox) / k, (screenRect.yMax - oy) / k);
        }

        /// <summary>Kebalikan <see cref="ScreenToImage"/>: potongan gambar tegak (piksel) ke letaknya di layar.</summary>
        public static Rect ImageToScreen(Rect imageRect, float sw, float sh, int uw, int uh)
        {
            float k = Mathf.Max(sw / uw, sh / uh);
            float ox = (sw - uw * k) / 2f, oy = (sh - uh * k) / 2f;
            return Rect.MinMaxRect(imageRect.xMin * k + ox, imageRect.yMin * k + oy, imageRect.xMax * k + ox, imageRect.yMax * k + oy);
        }
    }
}
