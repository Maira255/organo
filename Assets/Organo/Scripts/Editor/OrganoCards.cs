using System.IO;
using System.Linq;
using Organo.Data;
using Organo.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using U = Organo.UI.UI;

namespace Organo.EditorTools
{
    /// <summary>
    /// Kartu Organo untuk dicetak & dipindai AR.
    /// 1) <see cref="OrganoSnapshots.RunCardsAndQuit"/> merender desain kartu (UI Toolkit) tiap organ ke
    ///    Assets/Organo/AR/Cards/{id}.png (780x1090 px, rasio kartu remi 63x88 mm).
    /// 2) <see cref="ImportAndBuild"/> memasang gambar ke slot Card Image, membangun XR Reference Image Library,
    ///    dan membuat halaman cetak A4 300 dpi di folder KartuCetak/ (cetak dengan skala 100% / "ukuran sebenarnya").
    /// Desain dibuat kaya detail & tidak berulang (pola konfeti + kode batang unik per kartu) supaya mudah dikenali kamera.
    /// </summary>
    public static class OrganoCards
    {
        public const string CardDir = "Assets/Organo/AR/Cards";
        /// <summary>Versi alternatif bergaya kartun (garis tebal, ilustrasi berwajah).</summary>
        public const string CartoonDir = "Assets/Organo/AR/CardsKartun";
        public const float W = 390f, H = 545f;   // satuan UI; dirender 2x = 780x1090 px
        const string PrintDir = "KartuCetak";

        static float Hash01(string s, int i)
        {
            unchecked
            {
                int h = 17;
                foreach (var ch in s) h = h * 31 + ch;
                h = h * 31 + i * 7919;
                h ^= h << 13; h ^= h >> 17; h ^= h << 5;
                return (h & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        static readonly string[] DoodleShapes =
        {
            "<circle cx='12' cy='12' r='8' fill='{f}' stroke='#2E2A4A' stroke-width='2.2'/><circle cx='9' cy='9' r='2.2' fill='#FFFFFF'/>",
            "<path d='M12 3L21 20H3Z' fill='{f}' stroke='#2E2A4A' stroke-width='2.2' stroke-linejoin='round'/>",
            "<path d='M12 2l2.9 6.2 6.6.8-4.9 4.6 1.3 6.6L12 17l-5.9 3.2 1.3-6.6L2.5 9l6.6-.8z' fill='{f}' stroke='#2E2A4A' stroke-width='2' stroke-linejoin='round'/>",
            "<path d='M2 14Q5 6 8 14T14 14T20 14' fill='none' stroke='{f}' stroke-width='4' stroke-linecap='round'/><path d='M2 14Q5 6 8 14T14 14T20 14' fill='none' stroke='#2E2A4A' stroke-width='1.4' stroke-linecap='round'/>",
            "<rect x='4' y='4' width='16' height='16' rx='4' fill='{f}' stroke='#2E2A4A' stroke-width='2.2'/><path d='M8 12h8M12 8v8' stroke='#2E2A4A' stroke-width='2' stroke-linecap='round'/>",
            "<path d='M12 21C5 15 3 12 3 8.5A4.5 4.5 0 0 1 12 6a4.5 4.5 0 0 1 9 2.5C21 12 19 15 12 21z' fill='{f}' stroke='#2E2A4A' stroke-width='2'/>",
            "<path d='M3 18L8 6L13 18L18 6L22 16' fill='none' stroke='#2E2A4A' stroke-width='2.4' stroke-linecap='round' stroke-linejoin='round'/>",
            "<circle cx='7' cy='7' r='3' fill='#2E2A4A'/><circle cx='17' cy='9' r='2.4' fill='{f}' stroke='#2E2A4A' stroke-width='1.6'/><circle cx='11' cy='17' r='3.4' fill='{f}' stroke='#2E2A4A' stroke-width='1.6'/>",
            "<ellipse cx='12' cy='12' rx='9' ry='5' fill='{f}' stroke='#2E2A4A' stroke-width='2.2' transform='rotate(-25 12 12)'/>",
            "<path d='M12 3v18M3 12h18M6 6l12 12M18 6L6 18' stroke='#2E2A4A' stroke-width='2' stroke-linecap='round'/><circle cx='12' cy='12' r='3' fill='{f}' stroke='#2E2A4A' stroke-width='1.5'/>",
        };

        static string DoodleIcon(int shape, Color fill)
        {
            string hex = "#" + ColorUtility.ToHtmlStringRGB(fill);
            string key = "doodle_" + shape + "_" + hex;
            if (!OrganoIcons.Svg.ContainsKey(key))
                OrganoIcons.Svg[key] = "<svg viewBox='0 0 24 24'>" + DoodleShapes[shape].Replace("{f}", hex) + "</svg>";
            return key;
        }

        /// <summary>Varian pola terbaik per kartu (hasil penilaian arcoreimg), disimpan di AR/card_seeds.json.</summary>
        public const string SeedsPath = "Assets/Organo/AR/card_seeds.json";

        [System.Serializable] class SeedEntry { public string key; public int seed; }
        [System.Serializable] class SeedList { public System.Collections.Generic.List<SeedEntry> items = new System.Collections.Generic.List<SeedEntry>(); }

        static System.Collections.Generic.Dictionary<string, int> s_Seeds;

        /// <summary>Varian yang sedang dirender (-1 = pakai varian terbaik yang tersimpan).</summary>
        public static int ForceVariant = -1;

        static int Variant(string key)
        {
            if (ForceVariant >= 0) return ForceVariant;
            if (s_Seeds == null)
            {
                s_Seeds = new System.Collections.Generic.Dictionary<string, int>();
                if (File.Exists(SeedsPath))
                    foreach (var e in JsonUtility.FromJson<SeedList>(File.ReadAllText(SeedsPath)).items) s_Seeds[e.key] = e.seed;
            }
            return s_Seeds.TryGetValue(key, out var v) ? v : 0;
        }

        static string V(string seed, string key) { int v = Variant(key); return v == 0 ? seed : seed + "#" + v; }

        static int Seed(string s)
        {
            unchecked { int h = 23; foreach (var ch in s) h = h * 31 + ch; return h; }
        }

        /// <summary>
        /// Coretan tersebar merata (grid berjitter, acak unik per kartu) supaya seluruh kartu punya banyak "titik pola"
        /// untuk dikenali kamera. Zona <paramref name="keepClear"/> dibiarkan kosong (teks, lingkaran gambar).
        /// </summary>
        static void Doodles(VisualElement area, string seed, float cell, float w, float h, Color[] palette, System.Func<float, float, bool> keepClear,
            float minSize = 13f, float maxSize = 26f, float opacity = 1f)
        {
            var rng = new System.Random(Seed(seed));
            for (float gy = 0; gy < h; gy += cell)
                for (float gx = 0; gx < w; gx += cell)
                {
                    float size = Mathf.Lerp(minSize, Mathf.Min(maxSize, cell * 1.1f), (float)rng.NextDouble());
                    float x = gx + (float)rng.NextDouble() * Mathf.Max(1f, cell - size * 0.6f) - size * 0.2f;
                    float y = gy + (float)rng.NextDouble() * Mathf.Max(1f, cell - size * 0.6f) - size * 0.2f;
                    int shape = rng.Next(DoodleShapes.Length);
                    var col = palette[rng.Next(palette.Length)];
                    float rot = (float)rng.NextDouble() * 360f;
                    if (keepClear != null && keepClear(x + size * 0.5f, y + size * 0.5f)) continue;
                    var d = U.Icon(DoodleIcon(shape, col), size).Abs(x, y);
                    d.style.rotate = new Rotate(rot);
                    if (opacity < 1f) d.style.opacity = opacity;
                    d.pickingMode = PickingMode.Ignore;
                    area.Add(d);
                }
        }

        /// <summary>Desain sisi depan kartu (dipakai saat render di play mode).</summary>
        public static VisualElement CardArt(OrganInfo o)
        {
            var (fg, soft) = OrganDefaults.SystemColors(o.system);
            var card = U.Div().Size(W, H).Bg(fg);
            card.style.position = Position.Absolute;
            card.style.left = 0; card.style.top = 0;
            card.style.overflow = Overflow.Hidden;

            // pita atas: nomor kartu, sistem, logo
            var top = U.Row(8).Abs(18, 14, 18).H(40);
            var num = U.Div().Size(40, 40).Radius(20).Bg("#FFFFFF").Center();
            num.Add(U.Text(o.cardNumber.ToString("00")).FontSize(17).Bold().Fg(fg));
            top.Add(num);
            var sys = U.Col(0).Grow();
            sys.Add(U.Text("SISTEM " + OrganDefaults.SystemName(o.system).ToUpperInvariant()).FontSize(12).Bold().Fg("#FFFFFF"));
            sys.Add(U.Text("Kartu Organo · Season " + o.season).FontSize(10).Fg(U.C("#FFFFFF", 0.85f)));
            top.Add(sys);
            var logo = U.Row(4).Pad(4, 8).Radius(999).Bg("#FFFFFF");
            logo.Add(U.Icon("logo", 16));
            logo.Add(U.Text("Organo").FontSize(12).Bold().Fg(U.TealDark));
            top.Add(logo);
            card.Add(top);

            // panel putih berisi pola unik
            var panel = U.Div().Abs(14, 64, 14, 14).Radius(22).Bg("#FFFFFF");
            panel.style.overflow = Overflow.Hidden;
            card.Add(panel);
            Color[] palette = { fg, o.accent, o.ring, U.C(U.Yellow), U.C(U.Purple), U.C(U.Teal), U.C(U.Pink), U.C("#FFFFFF") };
            // area teks (nama & fungsi) dibiarkan bersih
            Doodles(panel, V(o.id, "r:" + o.id), 27f, 362f, 467f, palette,
                (x, y) => (y > 266f && y < 368f) || y > 398f || new Vector2(x - 181f, y - 138f).magnitude < 116f);

            // gambar organ 3D di lingkaran
            var ring = U.Div().Size(244, 244).Radius(122).Abs(59, 16).Bg(soft).Border(5, fg).Center();
            ring.style.overflow = Overflow.Hidden;
            // pola tipis di belakang organ (gambar organ berlatar transparan)
            var ringPattern = U.Div("fill");
            ringPattern.pickingMode = PickingMode.Ignore;
            Doodles(ringPattern, V(o.id + "-r", "r:" + o.id), 24f, 234f, 234f, palette, null, 11f, 20f, 0.4f);
            ring.Add(ringPattern);
            if (o.heroImage != null)
            {
                var img = U.Div("fill");
                img.style.backgroundImage = new StyleBackground(o.heroImage);
                img.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                ring.Add(img);
            }
            else ring.Add(U.Icon(Parts.MiniIcon(o), 170));
            panel.Add(ring);
            var badge = U.Div().Size(72, 72).Radius(36).Abs(262, 196).Bg("#FFFFFF").Border(3, fg).Center();
            badge.Add(U.Icon(o.IconKey, 52));
            panel.Add(badge);

            // nama & fungsi
            var text = U.Col(4).Abs(18, 276, 18).Align(Align.Center);
            var name = U.Text(o.displayName.ToUpperInvariant()).FontSize(30).Bold().Fg(U.Ink).TextCenter();
            text.Add(name);
            var fn = U.Text(o.shortDescription).FontSize(13).Fg(U.Muted).TextCenter();
            fn.style.whiteSpace = WhiteSpace.Normal;
            text.Add(fn);
            panel.Add(text);

            // kode batang unik + ajakan scan
            var bottom = U.Row(10).Abs(18, null, 18, 16).H(54).Pad(0, 12).Radius(14).Bg(soft);
            var bars = U.Row(0).H(34);
            for (int i = 0; i < 26; i++)
            {
                float w = 1.5f + Mathf.Floor(Hash01(o.id, 500 + i) * 3f) * 1.5f;
                var bar = U.Div().W(w).H(34).Bg(i % 2 == 0 ? U.C(U.Ink) : U.C("#00000000"));
                bars.Add(bar);
            }
            bottom.Add(bars);
            var cta = U.Col(0).Grow();
            cta.Add(U.Text("Scan aku!").FontSize(15).Bold().Fg(fg));
            cta.Add(U.Text("Buka Organo > Scan").FontSize(10).Fg(U.Ink));
            bottom.Add(cta);
            bottom.Add(U.Icon("scan_small", 26));
            panel.Add(bottom);
            return card;
        }

        /// <summary>Versi kartun: garis tepi tebal, latar sinar matahari, ilustrasi organ berwajah & balon kata.</summary>
        public static VisualElement CardArtCartoon(OrganInfo o)
        {
            var (fg, soft) = OrganDefaults.SystemColors(o.system);
            const string Ink = "#2E2A4A";
            var card = U.Div().Size(W, H).Bg("#FFF8E7");
            card.style.position = Position.Absolute;
            card.style.left = 0; card.style.top = 0;
            card.style.overflow = Overflow.Hidden;

            // bingkai stiker tebal
            var frame = U.Div().Abs(10, 10, 10, 10).Radius(26).Bg(soft).Border(5, Ink);
            frame.style.overflow = Overflow.Hidden;
            card.Add(frame);

            // latar sinar matahari di tengah
            var sun = U.Div().Size(560, 560).Abs(-95, -100);
            sun.pickingMode = PickingMode.Ignore;
            for (int i = 0; i < 18; i++)
            {
                var ray = U.Div().Abs(265, 0).Size(30, 560).Bg(i % 2 == 0 ? U.C("#FFFFFF", 0.55f) : U.C("#FFFFFF", 0f));
                ray.style.transformOrigin = new TransformOrigin(15, 280);
                ray.style.rotate = new Rotate(i * 10f);
                ray.pickingMode = PickingMode.Ignore;
                sun.Add(ray);
            }
            frame.Add(sun);

            // coretan kartun bergaris tebal di latar (pola unik tiap kartu, banyak "titik pola" untuk kamera AR)
            Color[] palette = { fg, o.accent, U.C("#FFD34E"), U.C("#FFFFFF"), U.C(U.Pink), U.C(U.Teal), U.C(U.Purple) };
            Doodles(frame, V(o.id + "-k", "k:" + o.id), 28f, 360f, 515f, palette,
                (x, y) => y < 64f || (y > 306f && y < 404f) || y > 448f || new Vector2(x - 172f, y - 190f).magnitude < 124f, 14f, 28f);

            // judul: pita nama + nomor dalam lencana bintang
            var title = U.Row(8).Abs(18, 16, 18).H(54).Pad(0, 14).Radius(18).Bg(fg).Border(4, Ink);
            var num = U.Div().Size(38, 38).Radius(19).Bg("#FFD34E").Border(3, Ink).Center();
            num.Add(U.Text(o.cardNumber.ToString()).FontSize(16).Bold().Fg(Ink));
            title.Add(num);
            title.Add(U.Text(o.displayName.ToUpperInvariant()).FontSize(o.displayName.Length > 11 ? 20 : 24).Bold().Fg("#FFFFFF").Grow());
            frame.Add(title);

            // ilustrasi organ berwajah (gaya kartun dari aplikasi)
            var blob = U.Div().Size(236, 236).Radius(118).Abs(64, 82).Bg("#FFFFFF").Border(5, Ink).Center();
            blob.style.overflow = Overflow.Hidden;
            var blobPattern = U.Div("fill");
            blobPattern.pickingMode = PickingMode.Ignore;
            Doodles(blobPattern, V(o.id + "-b", "k:" + o.id), 24f, 226f, 226f, palette, null, 11f, 20f, 0.35f);
            blob.Add(blobPattern);
            var art = U.Icon(o.IconKey, 196);
            art.style.position = Position.Absolute;
            art.style.left = 15; art.style.top = 15;
            blob.Add(art);
            frame.Add(blob);

            // balon kata dari si organ
            var bubble = U.Col(0).Abs(22, 324, 22).Pad(10, 14).Radius(18).Bg("#FFFFFF").Border(4, Ink);
            var say = U.Text("\u201C" + o.function + "\u201D").FontSize(13).Bold().Fg(Ink).TextCenter();
            say.style.whiteSpace = WhiteSpace.Normal;
            bubble.Add(say);
            frame.Add(bubble);
            var tail = U.Div().Size(22, 22).Abs(176, 314).Bg("#FFFFFF").Border(4, Ink);
            tail.style.rotate = new Rotate(45);
            tail.style.borderBottomWidth = 0; tail.style.borderRightWidth = 0;
            frame.Add(tail);

            // bawah: maskot, ajakan scan, kode batang unik
            var bottom = U.Row(8).Abs(18, null, 18, 14).H(58).Pad(0, 10).Radius(16).Bg("#FFD34E").Border(4, Ink);
            bottom.Add(U.Icon("mascot_mini", 40));
            var cta = U.Col(0).Grow();
            cta.Add(U.Text("SCAN AKU!").FontSize(16).Bold().Fg(Ink));
            cta.Add(U.Text("Sistem " + OrganDefaults.SystemName(o.system)).FontSize(10).Fg(Ink));
            bottom.Add(cta);
            var bars = U.Row(0).H(34);
            for (int i = 0; i < 18; i++)
            {
                float w = 1.5f + Mathf.Floor(Hash01(o.id, 500 + i) * 3f) * 1.5f;
                bars.Add(U.Div().W(w).H(34).Bg(i % 2 == 0 ? U.C(Ink) : U.C("#00000000")));
            }
            bottom.Add(bars);
            frame.Add(bottom);
            return card;
        }

        /// <summary>Batch: baca Library/OrganoCardVariants/best.txt lalu simpan sebagai varian terbaik.</summary>
        public static void SaveSeedsFromFile() => SaveSeeds(File.ReadAllText("Library/OrganoCardVariants/best.txt"));

        /// <summary>Simpan varian terbaik (dipanggil setelah penilaian). Format baris: "r:paru 3".</summary>
        public static void SaveSeeds(string lines)
        {
            var list = new SeedList();
            foreach (var line in lines.Split('\n'))
            {
                var parts = line.Trim().Split(' ');
                if (parts.Length == 2 && int.TryParse(parts[1], out var v)) list.items.Add(new SeedEntry { key = parts[0], seed = v });
            }
            File.WriteAllText(SeedsPath, JsonUtility.ToJson(list, true));
            s_Seeds = null;
        }

        // ------------------------------------------------------------------ impor & library

        [MenuItem("Organo/Kartu AR/Pakai Desain Realistis (pasang & bangun library)", priority = 23)]
        public static void ImportAndBuild() => ImportStyle(CardDir);

        [MenuItem("Organo/Kartu AR/Pakai Desain Kartun (pasang & bangun library)", priority = 24)]
        public static void ImportCartoon() => ImportStyle(CartoonDir);

        static void ImportStyle(string dir)
        {
            var db = OrganoSetup.EnsureDatabase();
            AssetDatabase.Refresh();
            int n = 0;
            foreach (var other in new[] { CardDir, CartoonDir }) ConfigureTextures(db, other);
            foreach (var o in db.organs.Where(x => x != null))
            {
                var path = dir + "/" + o.id + ".png";
                if (!File.Exists(path)) continue;
                o.cardImage = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                o.cardWidthMeters = 0.063f;
                EditorUtility.SetDirty(o);
                n++;
            }
            AssetDatabase.SaveAssets();
            int lib = OrganoSetup.RebuildCardLibrary(db);
            Debug.Log("[Organo] Kartu AR (" + dir + "): " + n + " gambar dipasang, " + lib + " kartu di library.");
            BuildPrintSheets(db, CardDir, "Realistis");
            BuildPrintSheets(db, CartoonDir, "Kartun");
        }

        /// <summary>Batch: terapkan ulang pengaturan impor gambar kartu (kedua gaya).</summary>
        public static void ApplyTextureSettings()
        {
            var db = OrganoSetup.EnsureDatabase();
            foreach (var dir in new[] { CardDir, CartoonDir }) ConfigureTextures(db, dir);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Gambar kartu dikompres (ukuran aplikasi kecil); ARCore/ARKit membaca file aslinya saat build.
        /// Mipmap aktif supaya kartu kecil di album Koleksi tetap halus (tidak bergerigi).
        /// </summary>
        static void ConfigureTextures(OrganDatabase db, string dir)
        {
            foreach (var o in db.organs.Where(x => x != null))
            {
                var path = dir + "/" + o.id + ".png";
                if (!File.Exists(path)) continue;
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                imp.textureType = TextureImporterType.Default;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.mipmapEnabled = true;
                imp.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
                imp.isReadable = false;
                imp.alphaSource = TextureImporterAlphaSource.None;
                imp.maxTextureSize = 1024;
                imp.textureCompression = TextureImporterCompression.CompressedHQ;
                imp.SaveAndReimport();
            }
        }

        /// <summary>Halaman A4 300 dpi berisi 9 kartu (3x3) ukuran asli 63x88 mm + garis potong.</summary>
        public static void BuildPrintSheets(OrganDatabase db, string dir, string style)
        {
            const int PageW = 2480, PageH = 3508;              // A4 @300 dpi
            const int CardW = 744, CardH = 1039;               // 63x88 mm @300 dpi
            const int Gap = 40;
            int ox = (PageW - (CardW * 3 + Gap * 2)) / 2, oy = (PageH - (CardH * 3 + Gap * 2)) / 2;
            var organs = db.organs.Where(o => o != null && o.Printable && File.Exists(dir + "/" + o.id + ".png")).OrderBy(o => o.cardNumber).ToList();
            var outDir = Path.Combine(PrintDir, style);
            Directory.CreateDirectory(outDir);
            int page = 0;
            for (int start = 0; start < organs.Count; start += 9)
            {
                page++;
                var sheet = new Texture2D(PageW, PageH, TextureFormat.RGB24, false);
                var white = new Color32[PageW * PageH];
                for (int i = 0; i < white.Length; i++) white[i] = new Color32(255, 255, 255, 255);
                sheet.SetPixels32(white);
                for (int k = 0; k < 9 && start + k < organs.Count; k++)
                {
                    var src = new Texture2D(2, 2);
                    src.LoadImage(File.ReadAllBytes(dir + "/" + organs[start + k].id + ".png"));
                    int cx = ox + (k % 3) * (CardW + Gap);
                    int cyTop = oy + (k / 3) * (CardH + Gap);
                    int cy = PageH - cyTop - CardH; // tekstur: baris 0 di bawah
                    for (int y = 0; y < CardH; y++)
                        for (int x = 0; x < CardW; x++)
                            sheet.SetPixel(cx + x, cy + y, src.GetPixelBilinear((x + 0.5f) / CardW, (y + 0.5f) / CardH));
                    // garis potong tipis abu-abu
                    var line = new Color(0.75f, 0.75f, 0.78f);
                    for (int x = -20; x < CardW + 20; x++) { Set(sheet, cx + x, cy - 2, line); Set(sheet, cx + x, cy + CardH + 1, line); }
                    for (int y = -20; y < CardH + 20; y++) { Set(sheet, cx - 2, cy + y, line); Set(sheet, cx + CardW + 1, cy + y, line); }
                    Object.DestroyImmediate(src);
                }
                sheet.Apply();
                var file = Path.Combine(outDir, "Organo_Kartu_" + style + "_Halaman" + page + ".png");
                File.WriteAllBytes(file, sheet.EncodeToPNG());
                Object.DestroyImmediate(sheet);
                Debug.Log("[Organo] Halaman cetak: " + Path.GetFullPath(file));
            }
        }

        static void Set(Texture2D t, int x, int y, Color c)
        {
            if (x >= 0 && y >= 0 && x < t.width && y < t.height) t.SetPixel(x, y, c);
        }
    }
}
