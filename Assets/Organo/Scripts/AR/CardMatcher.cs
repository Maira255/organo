using System;
using System.Collections.Generic;
using System.IO;
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
        /// <summary>Kecocokan minimum & selisih minimum dengan organ kedua terbaik.</summary>
        public const float MinScore = 0.40f, MinMargin = 0.05f;
        public const string ResourceName = "OrganoCardSignatures";

        readonly List<string> m_Ids = new List<string>();
        readonly List<float[]> m_Desc = new List<float[]>();
        readonly float[] m_Buf = new float[Size];

        public int Count => m_Ids.Count;

        public void Add(string organId, float[] descriptor)
        {
            m_Ids.Add(organId);
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
                    wr.Write(m_Ids[i]);
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
                            if (!best.TryGetValue(m_Ids[i], out float b) || dot > b) best[m_Ids[i]] = dot;
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
            return res;
        }

        // ---------------------------------------------------------------- arah gambar kamera

        /// <summary>
        /// Gambar kamera mentah (urutan <see cref="WebCamTexture.GetPixels32()"/>: baris bawah dulu) menjadi gambar tegak
        /// kecil seperti yang tampil di layar (baris atas dulu): tiap <paramref name="step"/> x step piksel dirata-rata,
        /// dicerminkan bila perlu, lalu diputar searah jarum jam sebesar <paramref name="rotation"/>
        /// (<see cref="WebCamTexture.videoRotationAngle"/>).
        /// </summary>
        public static Color32[] Upright(Color32[] tex, int width, int height, int step, int rotation, bool mirrorX, bool flipY, out int outW, out int outH)
        {
            step = Mathf.Max(1, step);
            int sw = width / step, sh = height / step;
            rotation = ((rotation % 360) + 360) % 360;
            bool side = rotation == 90 || rotation == 270;
            outW = side ? sh : sw;
            outH = side ? sw : sh;
            var dst = new Color32[sw * sh];
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
    }
}
