using System.Collections.Generic;
using System.Linq;
using Organo.AR;
using Organo.Data;
using Organo.ThreeD;
using Organo.UI;
using UnityEngine;
using UnityEngine.UIElements;
using U = Organo.UI.UI;

namespace Organo.Screens
{
    /// <summary>Potongan UI yang dipakai halaman Scan dan Misi Scan.</summary>
    public static class ScanParts
    {
        /// <summary>Latar pengganti kamera (Editor / kamera belum aktif): ruangan buram seperti di desain.</summary>
        public static VisualElement CameraStandIn()
        {
            var bg = U.Div("fill").Gradient("#E3D3BA", "#A98A6A", 180);
            bg.pickingMode = PickingMode.Ignore;
            void Blob(float x, float y, float w, float h, float r, string c, float a = 0.7f)
            {
                var b = U.Div().Abs(x, y).Size(w, h).Radius(r).Bg(U.C(c, a));
                b.pickingMode = PickingMode.Ignore;
                bg.Add(b);
            }
            Blob(210, 40, 200, 260, 14, "#FFFAF0");
            Blob(20, 90, 120, 90, 10, "#9B7B5A");
            var floor = U.Div().Abs(0, 520, 0, 0).Gradient("#8D6B4D", "#5F442F", 180);
            floor.pickingMode = PickingMode.Ignore;
            bg.Add(floor);
            Blob(10, 400, 260, 190, 40, "#6E7F78");
            Blob(300, 380, 90, 200, 45, "#4F7A4A");
            var vignette = U.Div("fill").Bg(U.C("#140E08", 0.18f));
            vignette.pickingMode = PickingMode.Ignore;
            bg.Add(vignette);

#if ORGANO_KAMERA_LAMA
            // Build "kamera lama" (ORGANO_KAMERA_LAMA): tampilan kamera biasa persis seperti versi sebelum perbaikan HP tanpa AR.
            var feed = U.Div("fill");
            feed.pickingMode = PickingMode.Ignore;
            feed.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            feed.style.display = DisplayStyle.None;
            bg.Add(feed);
            bg.schedule.Execute(() =>
            {
                var ar = OrganoApp.Instance != null ? OrganoApp.Instance.ar : null;
                var tex = ar != null ? ar.WebcamImage : null;
                feed.style.display = tex != null ? DisplayStyle.Flex : DisplayStyle.None;
                if (tex == null) return;
                if (feed.resolvedStyle.backgroundImage.renderTexture != tex) feed.style.backgroundImage = Background.FromRenderTexture(tex);
                int rot = ((ar.WebcamRotation % 360) + 360) % 360;
                bool sideways = rot == 90 || rot == 270;
                float w = Mathf.Max(1f, bg.layout.width), h = Mathf.Max(1f, bg.layout.height);
                float s = sideways ? Mathf.Max(h / w, w / h) : 1f;
                feed.style.rotate = new Rotate(-rot);
                feed.style.scale = new Scale(new Vector3(s * (ar.WebcamMirrored ? -1f : 1f), s * (ar.WebcamFlipped ? -1f : 1f), 1f));
            }).Every(0);
            return bg;
#else
            // Gambar kamera asli (webcam / kamera biasa) menutupi latar dekorasi kalau tersedia. Gambar mentah kamera HP
            // biasanya miring (videoRotationAngle 90/270): kotaknya dibuat seukuran gambar mentah lalu diputar SEARAH
            // jarum jam sebesar sudut itu, dengan skala "cover" (memenuhi layar, tengahnya terlihat). Dulu diputar ke arah
            // sebaliknya (gambar terbalik 180°) dan diperbesar ±4x (buram).
            var feed = U.Div();
            feed.pickingMode = PickingMode.Ignore;
            feed.style.position = Position.Absolute;
            feed.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
            feed.style.display = DisplayStyle.None;
            bg.Add(feed);
            bg.schedule.Execute(() =>
            {
                var ar = OrganoApp.Instance != null ? OrganoApp.Instance.ar : null;
                var tex = ar != null ? ar.WebcamImage : null;
                feed.style.display = tex != null ? DisplayStyle.Flex : DisplayStyle.None;
                if (tex == null) return;
                if (feed.resolvedStyle.backgroundImage.renderTexture != tex) feed.style.backgroundImage = Background.FromRenderTexture(tex);
                int rot = ((ar.WebcamRotation % 360) + 360) % 360;
                bool sideways = rot == 90 || rot == 270;
                float W = Mathf.Max(1f, bg.layout.width), H = Mathf.Max(1f, bg.layout.height);
                float uw = sideways ? tex.height : tex.width, uh = sideways ? tex.width : tex.height;
                float k = Mathf.Max(W / uw, H / uh);
                float ew = tex.width * k, eh = tex.height * k;
                feed.style.width = ew;
                feed.style.height = eh;
                feed.style.left = (W - ew) / 2f;
                feed.style.top = (H - eh) / 2f;
                feed.style.rotate = new Rotate(rot);
                feed.style.scale = new Scale(new Vector3(ar.WebcamMirrored ? -1f : 1f, ar.WebcamFlipped ? -1f : 1f, 1f));
            }).Every(0);
            return bg;
#endif
        }

        /// <summary>
        /// Beri tahu pengenal kartu kamera biasa letak bingkai scan di tampilan kamera (dipakai di HP yang tidak mendukung AR).
        /// </summary>
        public static void TellScanFrame(ARScanController ar, VisualElement view, VisualElement frameBox, bool active)
        {
            if (ar == null) return;
            if (!active || view == null || frameBox == null || view.panel == null) { ar.ClearScanFrame(); return; }
            var v = view.worldBound;
            var f = frameBox.worldBound;
            if (float.IsNaN(v.width) || v.width < 2f || float.IsNaN(f.width) || f.width < 2f) { ar.ClearScanFrame(); return; }
            ar.SetScanFrame(new Rect(f.x - v.x, f.y - v.y, f.width, f.height), v.size);
        }

        /// <summary>Keterangan kecil tentang kamera yang dipakai (AR / kamera biasa / tidak ada kamera).</summary>
        public static string CameraNote(ARScanController ar)
        {
            if (ar == null) return null;
            if (ar.NoCamera) return "Kamera tidak ditemukan di perangkat ini";
            if (!ar.UsingWebcam) return null;
#if ORGANO_KAMERA_LAMA
            if (ar.ArUnsupported) return "HP ini belum mendukung AR · kamera biasa menyala";
#else
            if (ar.ArUnsupported) return "HP ini belum mendukung AR · kartu tetap bisa di-scan";
#endif
            return "Kamera laptop (Editor) · ketuk bingkai untuk mencoba";
        }

        public static VisualElement GlassButton(string icon, System.Action tap, string tip)
        {
            var b = U.Div("icon-btn", "icon-btn--glass");
            b.Add(U.Icon(icon, 22).Tinted(Color.white));
            b.tooltip = tip;
            b.Tap(tap);
            return b;
        }

        public static VisualElement Banner(string text, string bg, out Label label)
        {
            var b = U.Row(8).Pad(8, 18).Radius(999).Bg(bg).AlignSelf(Align.Center);
            label = U.Text(text).FontSize(13).Bold().Fg("#FFFFFF");
            b.Add(label);
            return b;
        }

        /// <summary>Petunjuk ARCore saat sesi AR belum siap melacak (kartu belum bisa dikenali).</summary>
        public static string NotReadyHint(ARScanController ar)
        {
            switch (UnityEngine.XR.ARFoundation.ARSession.notTrackingReason)
            {
                case UnityEngine.XR.ARSubsystems.NotTrackingReason.InsufficientLight: return "Terlalu gelap. Cari tempat yang lebih terang.";
                case UnityEngine.XR.ARSubsystems.NotTrackingReason.ExcessiveMotion: return "Pelan-pelan saja, jangan digerakkan terlalu cepat.";
                case UnityEngine.XR.ARSubsystems.NotTrackingReason.InsufficientFeatures: return "Arahkan ke kartu di atas meja, bukan ke dinding polos.";
                default: return "Kamera AR sedang bersiap. Gerakkan HP pelan ke kiri dan kanan.";
            }
        }
    }

    /// <summary>
    /// Bingkai scan berbentuk kartu Organo (63 x 88 mm) yang BESAR. ARCore baru bisa mengenali gambar kalau gambar itu
    /// memenuhi minimal 25% gambar kamera (developers.google.com/ar/develop/augmented-images). Bingkai lama (persegi kecil)
    /// membuat kartu hanya ±8% gambar kamera sehingga hampir tidak pernah terdeteksi. Bingkai ini ±88% lebar layar, jadi
    /// kartu yang mengisinya menutupi ±30–40% gambar kamera. Bagian di luar bingkai digelapkan supaya anak mendekatkan HP.
    /// </summary>
    /// <summary>
    /// Kamera biasa (HP tanpa AR): model organ yang mengikuti kartu di gambar kamera. Viewport 3D berukuran tetap yang
    /// dipindah & diskalakan (tinggi model ±1,1x tinggi kartu di layar: kartu jauh = model kecil), dihaluskan supaya tidak
    /// bergetar; kartu hilang lebih dari ±0,6 detik = model memudar. Ukuran diatur lewat skala, bukan ukuran elemen,
    /// supaya tekstur 3D tidak dibuat ulang setiap frame. Dipakai halaman hasil scan & misi scan di kuis.
    /// </summary>
    public class CardFollowView
    {
        public const float Size = 340f;
        public readonly OrganViewport Viewport;
        Vector2 m_Center;
        float m_Scale = -1f, m_Alpha, m_LostAt = -1f;

        public CardFollowView(OrganViewer3D viewer, OrganInfo organ)
        {
            Viewport = new OrganViewport(viewer, organ, false) { FreeRotate = true, IgnoreScale = true, RenderScale = 0.8f };
            Viewport.style.position = Position.Absolute;
            Viewport.style.width = Size; Viewport.style.height = Size;
            Viewport.style.left = 0; Viewport.style.top = 0;
            Viewport.style.opacity = 0f;
            Viewport.pickingMode = PickingMode.Ignore;
        }

        /// <summary>Perbarui letak model. <paramref name="screen"/> = layar kamera (gambar kamera memenuhinya). True = model tampil.</summary>
        public bool Tick(ARScanController ar, VisualElement screen, OrganInfo organ, float dt)
        {
            var layer = Viewport.parent;
            if (ar == null || layer == null) return false;
            ar.SetCameraView(new Vector2(screen.layout.width, screen.layout.height));
            bool visible = ar.WebcamCardVisible && ar.CurrentOrgan == organ;
            if (visible)
            {
                m_LostAt = -1f;
                var card = ar.WebcamCardRect;
                var off = screen.worldBound.position - layer.worldBound.position;
                var center = new Vector2(card.center.x, card.center.y - card.height * 0.12f) + off;
                float scale = Mathf.Clamp(card.height * 1.1f / Size, 0.2f, 1.25f);
                float t = m_Scale < 0f ? 1f : 1f - Mathf.Exp(-12f * dt);
                m_Center = m_Scale < 0f ? center : Vector2.Lerp(m_Center, center, t);
                m_Scale = m_Scale < 0f ? scale : Mathf.Lerp(m_Scale, scale, t);
            }
            else if (m_LostAt < 0f) m_LostAt = Time.unscaledTime;
            bool show = m_Scale > 0f && (visible || Time.unscaledTime - m_LostAt < 0.6f);
            m_Alpha = Mathf.MoveTowards(m_Alpha, show ? 1f : 0f, dt * 4f);
            Viewport.style.opacity = m_Alpha;
            Viewport.pickingMode = m_Alpha > 0.5f ? PickingMode.Position : PickingMode.Ignore;
            if (m_Scale > 0f)
            {
                Viewport.style.left = m_Center.x - Size / 2f;
                Viewport.style.top = m_Center.y - Size / 2f;
                Viewport.style.scale = new Scale(new Vector3(m_Scale, m_Scale, 1f));
            }
            return show;
        }
    }

    public class CardFrame : VisualElement
    {
        public const float CardAspect = 88f / 63f;
        public const float WidthFraction = 0.88f;
        /// <summary>Batas bawah yang dijaga uji otomatis (ARCore: minimal 25% gambar kamera).</summary>
        public const float MinCoverage = 0.25f;

        /// <summary>Elemen di atas/bawah bingkai (bingkai mengisi ruang di antaranya).</summary>
        public VisualElement TopAnchor, BottomAnchor;
        public VisualElement Box { get; }
        /// <summary>Pil petunjuk kecil di dalam bingkai (atas); tidak memakan ruang layout. Kosong = tersembunyi.</summary>
        public string Hint
        {
            set
            {
                m_HintLabel.text = value ?? "";
                m_HintPill.Visible(!string.IsNullOrEmpty(value));
            }
        }
        readonly VisualElement m_HintPill;
        readonly Label m_HintLabel;
        readonly VisualElement[] m_Corners = new VisualElement[4];
        readonly VisualElement[] m_Shade = new VisualElement[4];
        readonly VisualElement m_Outline, m_Sweep;
        float m_SweepT;

        public CardFrame()
        {
            AddToClassList("fill");
            pickingMode = PickingMode.Ignore;
            for (int i = 0; i < 4; i++)
            {
                m_Shade[i] = new VisualElement { pickingMode = PickingMode.Ignore };
                m_Shade[i].style.position = Position.Absolute;
                m_Shade[i].style.backgroundColor = U.C("#0A0814", 0.32f);
                Add(m_Shade[i]);
            }
            Box = new VisualElement { name = "card-frame" };
            Box.style.position = Position.Absolute;
            Add(Box);
            m_Outline = U.Div("fill").Radius(16);
            m_Outline.pickingMode = PickingMode.Ignore;
            SetBorder(m_Outline, 1.5f, U.C("#FFFFFF", 0.55f));
            Box.Add(m_Outline);
            m_Sweep = U.Div().Abs(18, 20, 18).H(3).Radius(2).Bg(U.Teal);
            m_Sweep.pickingMode = PickingMode.Ignore;
            Box.Add(m_Sweep);
            for (int i = 0; i < 4; i++)
            {
                var c = new VisualElement { pickingMode = PickingMode.Ignore };
                c.style.position = Position.Absolute;
                c.style.width = 48; c.style.height = 48;
                bool left = i == 0 || i == 3, top = i < 2;
                if (left) c.style.left = -3; else c.style.right = -3;
                if (top) c.style.top = -3; else c.style.bottom = -3;
                const float w = 5f, r = 20f;
                if (top) c.style.borderTopWidth = w; else c.style.borderBottomWidth = w;
                if (left) c.style.borderLeftWidth = w; else c.style.borderRightWidth = w;
                if (top && left) c.style.borderTopLeftRadius = r;
                if (top && !left) c.style.borderTopRightRadius = r;
                if (!top && !left) c.style.borderBottomRightRadius = r;
                if (!top && left) c.style.borderBottomLeftRadius = r;
                m_Corners[i] = c;
                Box.Add(c);
            }
            m_HintPill = U.Div().Abs(12, 14, 12);
            m_HintPill.style.alignItems = Align.Center;
            m_HintPill.pickingMode = PickingMode.Ignore;
            var pill = U.Div().Pad(5, 12).Radius(999).Bg(U.C("#141220", 0.62f));
            pill.pickingMode = PickingMode.Ignore;
            m_HintLabel = U.Text("").FontSize(12).Fg("#FFFFFF").TextCenter();
            m_HintLabel.style.whiteSpace = WhiteSpace.Normal;
            pill.Add(m_HintLabel);
            m_HintPill.Add(pill);
            m_HintPill.Visible(false);
            Box.Add(m_HintPill);
            Tint = U.C(U.Teal);
            RegisterCallback<GeometryChangedEvent>(_ => Relayout());
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                TopAnchor?.RegisterCallback<GeometryChangedEvent>(__ => Relayout());
                BottomAnchor?.RegisterCallback<GeometryChangedEvent>(__ => Relayout());
            });
        }

        static void SetBorder(VisualElement e, float w, Color c)
        {
            e.style.borderTopWidth = w; e.style.borderBottomWidth = w; e.style.borderLeftWidth = w; e.style.borderRightWidth = w;
            e.style.borderTopColor = c; e.style.borderBottomColor = c; e.style.borderLeftColor = c; e.style.borderRightColor = c;
        }

        public Color Tint
        {
            set
            {
                foreach (var c in m_Corners)
                {
                    c.style.borderTopColor = value; c.style.borderBottomColor = value;
                    c.style.borderLeftColor = value; c.style.borderRightColor = value;
                }
                m_Sweep.style.backgroundColor = value;
            }
        }

        public bool SweepVisible { set => m_Sweep.Visible(value); }

        public void Tick(float dt)
        {
            m_SweepT = Mathf.Repeat(m_SweepT + dt / 1.8f, 1f);
            float h = Box.resolvedStyle.height;
            if (float.IsNaN(h) || h <= 0) return;
            m_Sweep.style.top = 20 + Mathf.PingPong(m_SweepT * 2f, 1f) * Mathf.Max(0f, h - 40f);
        }

        /// <summary>Ukuran bingkai (lebar, tinggi) untuk ruang yang tersedia.</summary>
        public static Vector2 FitSize(float availW, float availH)
        {
            float w = Mathf.Min(availW * WidthFraction, availH / CardAspect);
            return new Vector2(w, w * CardAspect);
        }

        /// <summary>
        /// Bagian gambar kamera yang tertutup kartu bila kartu pas mengisi bingkai. Gambar CPU ARCore umumnya 4:3 lanskap;
        /// di HP potret ia ditampilkan memenuhi seluruh layar (terpotong kiri-kanan), jadi ukurannya dihitung dari layar penuh.
        /// </summary>
        public static float CameraCoverage(Vector2 screen, Vector2 card, float cameraAspect = 4f / 3f)
        {
            float camH = screen.y, camW = screen.y / cameraAspect;
            if (camW < screen.x) { camW = screen.x; camH = screen.x * cameraAspect; }
            return card.x * card.y / Mathf.Max(1f, camW * camH);
        }

        void Relayout()
        {
            float w = resolvedStyle.width, h = resolvedStyle.height;
            if (float.IsNaN(w) || float.IsNaN(h) || w <= 0 || h <= 0) return;
            var me = worldBound;
            float top = 0f, bottom = h;
            if (TopAnchor != null && TopAnchor.panel != null && TopAnchor.worldBound.height > 0) top = Mathf.Clamp(TopAnchor.worldBound.yMax - me.yMin + 10f, 0f, h);
            if (BottomAnchor != null && BottomAnchor.panel != null && BottomAnchor.worldBound.height > 0) bottom = Mathf.Clamp(BottomAnchor.worldBound.yMin - me.yMin - 10f, top, h);
            var size = FitSize(w, Mathf.Max(0f, bottom - top));
            float x = (w - size.x) * 0.5f, y = top + (bottom - top - size.y) * 0.5f;
            Box.style.left = x; Box.style.top = y; Box.style.width = size.x; Box.style.height = size.y;
            Place(m_Shade[0], 0, 0, w, y);                              // atas
            Place(m_Shade[1], 0, y + size.y, w, h - y - size.y);        // bawah
            Place(m_Shade[2], 0, y, x, size.y);                         // kiri
            Place(m_Shade[3], x + size.x, y, w - x - size.x, size.y);   // kanan
        }

        static void Place(VisualElement e, float x, float y, float w, float h)
        {
            e.style.left = x; e.style.top = y; e.style.width = Mathf.Max(0f, w); e.style.height = Mathf.Max(0f, h);
        }
    }

    /// <summary>
    /// "Kartunya dari mana?": kartu dicetak guru/orang tua dari situs kartu Organo (Web/kartu-guru),
    /// lalu dibagikan setelah murid belajar di Library.
    /// </summary>
    public static class CardSource
    {
        public static void Show(OrganoApp app)
        {
            var c = U.Col(14);
            var head = U.Col(4);
            head.Add(U.Text("Kartunya dari mana?", "t-h2"));
            head.Add(U.Text("Kartu Organo dicetak oleh guru atau orang tuamu, lalu dibagikan setelah kamu belajar.", "t-small", "t-muted"));
            c.Add(head);

            var steps = U.Col(10);
            string[] lines =
            {
                "Pelajari dulu organnya di Pustaka",
                "Mintalah kartunya kepada guru atau orang tua",
                "Scan kartunya untuk membuka organ & kuis",
            };
            for (int i = 0; i < lines.Length; i++)
            {
                var r = U.Row(10);
                r.Add(U.Div().Size(26, 26).Radius(13).Bg(U.TealSoft).Center().Kids(U.Text((i + 1).ToString()).FontSize(12).Bold().Fg(U.TealDark)));
                r.Add(U.Text(lines[i], "t-small").Fg(U.Ink).Grow());
                steps.Add(r);
            }
            c.Add(steps);

            var adult = U.Col(6).Bg("#F6F5F9").Radius(14).Pad(12, 14);
            adult.Add(U.Text("Untuk guru & orang tua", "t-caption").Bold().Fg(U.Ink));
            string url = app.cardsWebsiteUrl != null ? app.cardsWebsiteUrl.Trim() : "";
            if (url.Length > 0)
            {
                adult.Add(U.Text("Unduh kartu siap cetak (A4) dari:", "t-caption", "t-muted"));
                adult.Add(U.Text(url, "t-caption").Bold().Fg(U.TealDark));
                var links = U.Row(14);
                links.Add(U.Text("Salin tautan").FontSize(12).Bold().Fg(U.TealDark).Pad(4, 0).Tap(() =>
                {
                    GUIUtility.systemCopyBuffer = url;
                    app.Toast("Tautan disalin");
                }));
                links.Add(U.Text("Buka situs").FontSize(12).Bold().Fg(U.TealDark).Pad(4, 0).Tap(() => Application.OpenURL(url)));
                adult.Add(links);
                adult.Add(U.Text("Belum sempat mencetak? Buka situs ini di laptop, tablet, atau HP lain, pilih \"Scan dari layar\", lalu scan kartunya langsung dari layar.", "t-caption", "t-muted"));
            }
            else
            {
                adult.Add(U.Text("File kartu siap cetak (A4, 17 kartu) tersedia di situs kartu Organo. Cetak dengan skala 100%.", "t-caption", "t-muted"));
            }
            c.Add(adult);

            OrganoApp.Overlay sheet = null;
            var actions = U.Col(8);
            actions.Add(U.Button("Belajar di Pustaka", () => { sheet.Close(); app.GoTab(NavTab.Library); }, "btn-primary"));
            actions.Add(U.Button("Tutup", () => sheet.Close(), "btn-ghost"));
            c.Add(actions);
            sheet = app.ShowSheet(c);
        }
    }

    // ==================================================================== Scan kartu

    public class ScanScreen : AppScreen
    {
        enum Phase { Scanning, Dark, Unknown }

        public override NavTab Tab => NavTab.Scan;
        public override bool UsesCamera => true;

        Phase m_Phase;
        string m_DemoOrgan;
        bool m_Torch;
        float m_ScanTime;
        VisualElement m_StandIn, m_Dim, m_StatusIcon, m_Actions, m_Tips, m_Torch_, m_StatusWrap;
        Label m_Title, m_Desc, m_Hint;
        CardFrame m_Frame;
        OrganoApp.Overlay m_PermDialog;
        VisualElement m_CamNote;
        Label m_CamNoteText;

        /// <summary>Bingkai scan (untuk uji otomatis ukuran bingkai vs gambar kamera).</summary>
        public CardFrame Frame => m_Frame;

        ARScanController Ar => App.ar;

        protected override void Build(object arg)
        {
            m_DemoOrgan = arg as string ?? m_DemoOrgan ?? "lambung";
            m_Phase = Phase.Scanning;
            m_ScanTime = 0;
            m_Torch = false;
            Ar?.ResetDetection();
            if (Ar != null) { Ar.CardDetected -= OnCard; Ar.CardDetected += OnCard; }

            m_StandIn = ScanParts.CameraStandIn();
            Root.Add(m_StandIn);
            m_Dim = U.Div("fill").Bg(U.C("#0A0814", 0f));
            m_Dim.pickingMode = PickingMode.Ignore;
            Root.Add(m_Dim);

            // Bingkai besar berbentuk kartu (lihat CardFrame: ARCore butuh kartu >= 25% gambar kamera)
            m_Frame = new CardFrame();
            Root.Add(m_Frame);
            m_Frame.Box.tooltip = "Bingkai scan";
            m_Frame.Box.Tap(() =>
            {
                // Demo: ketuk bingkai = kartu terdeteksi (berguna di Editor & sebelum gambar kartu dipasang)
                if (OrganoApp.DevTools && m_Phase == Phase.Scanning && (Ar == null || !Ar.IsCameraLive || !Ar.HasCardLibrary))
                    OnCard(Db.Get(m_DemoOrgan));
            });

            var top = U.Row().Justify(Justify.SpaceBetween).Pad(16 + App.TopInset, 20, 0, 20);
            m_Torch_ = ScanParts.GlassButton("torch", () => SetTorch(!m_Torch), "Senter");
            top.Add(m_Torch_);
            top.Add(ScanParts.GlassButton("help", ShowHelp, "Bantuan"));
            Root.Add(top);

            var hintWrap = U.Div().AlignSelf(Align.Center).Pad(6, 14).Radius(12).Bg(U.C("#141220", 0.62f));
            hintWrap.style.marginTop = 10;
            hintWrap.style.maxWidth = 330;
            m_Hint = U.Text("Dekatkan HP sampai kartu memenuhi bingkai").FontSize(12).Fg("#FFFFFF").TextCenter();
            m_Hint.style.whiteSpace = WhiteSpace.Normal;
            hintWrap.Add(m_Hint);
            hintWrap.pickingMode = PickingMode.Ignore;
            Root.Add(hintWrap);
            m_CamNote = U.Div().AlignSelf(Align.Center).Pad(4, 12).Radius(999).Bg(U.C("#141220", 0.6f));
            m_CamNote.style.marginTop = 6;
            m_CamNoteText = U.Text("").FontSize(11).Fg("#FFFFFF");
            m_CamNote.Add(m_CamNoteText);
            m_CamNote.pickingMode = PickingMode.Ignore;
            Root.Add(m_CamNote);
            m_Frame.TopAnchor = hintWrap;

            var stage = U.Div().Grow();
            stage.pickingMode = PickingMode.Ignore;
            Root.Add(stage);

            // Status card (hanya saat terlalu gelap / belum dikenali), melayang di atas bagian bawah bingkai
            var statusWrap = U.Col(0).Pad(0, 20, 8, 20);
            statusWrap.style.position = Position.Absolute;
            statusWrap.style.left = 0; statusWrap.style.right = 0; statusWrap.style.bottom = 58;
            m_StatusWrap = statusWrap;
            var status = U.Col(10).Bg("#FFFFFF").Radius(18).Pad(14, 16);
            status.Elev(Elevation.Dark);
            var head = U.Row(12);
            m_StatusIcon = U.Div().Size(40, 40).Radius(20).Center();
            head.Add(m_StatusIcon);
            var ht = U.Col(2).Grow();
            m_Title = U.Text("", "t-section");
            m_Desc = U.Text("", "t-caption");
            ht.Add(m_Title); ht.Add(m_Desc);
            head.Add(ht);
            status.Add(head);
            m_Tips = U.Col(6).Bg("#F6F5F9").Radius(12).Pad(10, 12);
            foreach (var t in ScanTips)
            {
                var r = U.Row(8);
                r.Add(U.Icon("check_teal", 14).Tinted(U.C(U.Teal)));
                var tl = U.Text(t, "t-caption").Fg(U.Ink).Grow();
                tl.style.whiteSpace = WhiteSpace.Normal;
                r.Add(tl);
                m_Tips.Add(r);
            }
            status.Add(m_Tips);
            m_Actions = U.Row(8);
            status.Add(m_Actions);
            statusWrap.Add(status);

            var helper = U.Row(0).AlignSelf(Align.Center).Pad(9, 16).Radius(999).Bg(U.C("#141220", 0.6f));
            helper.style.marginBottom = 12;
            helper.Add(U.Text("Belum punya kartu?").FontSize(12).Fg("#FFFFFF").Pad(0, 4, 0, 0));
            helper.Add(U.Text("Cara mendapatkannya").FontSize(12).Bold().Fg("#9CE6DC").Tap(() => CardSource.Show(App)));

            Root.Add(helper);
            Root.Add(statusWrap);
            m_Frame.BottomAnchor = helper;

            SetPhase(Phase.Scanning);
            if (CameraPermission.Current != PermissionState.Granted) Root.schedule.Execute(ShowPermission).ExecuteLater(250);
        }

        void SetPhase(Phase p)
        {
            m_Phase = p;
            m_ScanTime = 0;
            string color, title, desc, iconBg, icon;
            switch (p)
            {
                case Phase.Dark:
                    color = U.Yellow; title = "Ruangannya terlalu gelap"; iconBg = U.YellowSoft; icon = "moon";
                    desc = "Nyalakan senter atau pindah ke tempat yang lebih terang."; break;
                case Phase.Unknown:
                    color = U.Pink; title = "Hmm, kartunya belum dikenali"; iconBg = U.PinkSoft; icon = "question";
                    desc = "Coba cek beberapa hal ini lalu scan lagi."; break;
                default:
                    color = U.Teal; title = "Tahan kartunya, ya!"; iconBg = U.TealSoft; icon = "scan_small";
                    desc = "Jaga HP tetap diam sampai organ muncul."; break;
            }
            m_Frame.Tint = U.C(color);
            m_Frame.SweepVisible = p == Phase.Scanning;
            m_Dim.style.backgroundColor = U.C("#0A0814", p == Phase.Dark ? 0.62f : 0f);
            m_StatusIcon.Bg(iconBg);
            m_StatusIcon.Clear();
            var ic = U.Icon(icon, 20);
            if (icon == "scan_small") ic.Tint = U.C(U.TealInk);
            m_StatusIcon.Add(ic);
            m_Title.text = title;
            m_Desc.text = desc;
            m_Tips.Visible(p == Phase.Unknown);
            m_StatusWrap.Visible(p != Phase.Scanning || (OrganoApp.DevTools && (Ar == null || !Ar.IsCameraLive || !Ar.HasCardLibrary)));

            m_Actions.Clear();
            if (p == Phase.Dark)
                m_Actions.Add(U.Button("Nyalakan Senter", () => { SetTorch(true); SetPhase(Phase.Scanning); }, "btn-small").Bg(U.Yellow).Grow());
            else if (p == Phase.Unknown)
            {
                m_Actions.Add(U.Button("Coba Lagi", () => SetPhase(Phase.Scanning), "btn-primary", "btn-small").Grow());
                m_Actions.Add(U.Button("Lihat Kartu", () => App.Go<CardCollectionScreen>(), "btn-outline", "btn-small").Grow());
            }
            else if (OrganoApp.DevTools && (Ar == null || !Ar.IsCameraLive || !Ar.HasCardLibrary))
            {
                var demo = U.Row(0).Grow();
                demo.style.flexWrap = Wrap.Wrap;
                demo.Add(U.Text("Demo: ketuk bingkai untuk hasil scan · ").FontSize(11).Fg(U.Muted));
                demo.Add(U.Text("gelap").FontSize(11).Bold().Fg(U.TealDark).Tap(() => SetPhase(Phase.Dark)));
                demo.Add(U.Text(" · ").FontSize(11).Fg(U.Muted));
                demo.Add(U.Text("tak dikenali").FontSize(11).Bold().Fg(U.TealDark).Tap(() => SetPhase(Phase.Unknown)));
                demo.Add(U.Text(" · ").FontSize(11).Fg(U.Muted));
                demo.Add(U.Text("kartu baru").FontSize(11).Bold().Fg(U.TealDark).Tap(() =>
                {
                    var missing = Db.Ordered.FirstOrDefault(o => !P.HasCard(o.id) && !ReproAccess.IsLocked(o, P));
                    OnCard(missing ?? Db.Get(m_DemoOrgan));
                }));
                m_Actions.Add(demo);
            }
        }

        void SetTorch(bool on)
        {
            if (on && (Ar == null || !Ar.TorchSupported))
            {
                bool editor = Application.isEditor;
                App.Toast(editor ? "Laptop tidak punya lampu senter. Coba di HP, ya!"
                    : Ar != null && Ar.UsingWebcam ? "Senter hanya bisa dipakai saat mode AR"
                    : Ar != null && !Ar.IsCameraLive ? "Tunggu kamera menyala dulu, lalu coba lagi"
                    : "Senter tidak tersedia di HP ini", 2.6f);
                on = false;
            }
            m_Torch = on;
            Ar?.SetTorch(on);
            PaintTorch(on);
            if (on) Root.schedule.Execute(() =>
            {
                // pastikan senter benar-benar menyala
                if (m_Torch && Ar != null && !Ar.TorchOn) { App.Toast("Senter belum bisa menyala. Coba lagi, ya!"); m_Torch = false; Ar.SetTorch(false); PaintTorch(false); }
            }).ExecuteLater(1500);
        }

        void PaintTorch(bool on)
        {
            m_Torch_.Bg(on ? U.C(U.Yellow) : U.C("#141220", 0.55f));
            m_Torch_.tooltip = on ? "Matikan senter" : "Nyalakan senter";
        }

        void OnCard(OrganInfo organ)
        {
            if (organ == null || Root.panel == null) return;
            // kartu organ reproduksi gender lain yang belum terbuka: jelaskan cara membukanya, organnya tidak ditampilkan
            if (ReproAccess.IsLocked(organ, P))
            {
                Ar?.ResetDetection();
                Parts.ShowReproLock(App, organ);
                return;
            }
            OrganoAudio.Success();
            App.Go<ScanResultScreen>(organ.id);
        }

        /// <summary>Tips scan (juga dipakai lembar Bantuan). Urutan = paling sering jadi penyebab gagal.</summary>
        public static readonly string[] ScanTips =
        {
            "Dekatkan HP sampai kartu memenuhi bingkai (kartu cetak ±15 cm, kartu di layar ±20 cm)",
            "Tahan HP diam 2 detik, tempat cukup terang",
            "Hindari pantulan lampu. Kartu di layar: kecerahan sedang (jangan maksimal) & miringkan sedikit",
            "Pakai kartu Organo dari guru atau situs kartu Organo",
        };

        float m_NotReadyTime;

        public override void Tick(float dt)
        {
            bool live = Ar != null && Ar.IsCameraLive;
            m_StandIn.Visible(!live);
            var note = ScanParts.CameraNote(Ar);
            m_CamNote.Visible(note != null);
            if (note != null && m_CamNoteText.text != note) m_CamNoteText.text = note;
            if (m_Torch && Ar != null && Ar.UsingWebcam == false && !Ar.IsCameraLive) { m_Torch = false; PaintTorch(false); }
            m_Frame.Tick(dt);
            ScanParts.TellScanFrame(Ar, m_StandIn, m_Frame.Box, m_Phase == Phase.Scanning && !live);

            // ARCore baru bisa mengenali kartu setelah sesi AR melacak gerak HP
            bool ready = live && Ar.TrackingReady;
            m_NotReadyTime = live && !ready ? m_NotReadyTime + dt : 0f;
            string hint = m_NotReadyTime > 1.5f ? ScanParts.NotReadyHint(Ar) : "Dekatkan HP sampai kartu memenuhi bingkai";
            if (m_Hint.text != hint) m_Hint.text = hint;

            if (m_Phase != Phase.Scanning) return;
            if (ready || !live) m_ScanTime += dt;
            if (live && Ar.IsDark && !m_Torch) SetPhase(Phase.Dark);
            else if (ready && Ar.HasCardLibrary && m_ScanTime > 15f) SetPhase(Phase.Unknown);
        }

        /// <summary>Tombol "?": tips scan + info teknis (izin kamera kalau belum diberikan).</summary>
        public void ShowHelp()
        {
            if (CameraPermission.Current != PermissionState.Granted) { ShowPermission(); return; }
            var c = U.Col(12);
            c.Add(U.Text("Tips supaya kartu cepat dikenali", "t-h2"));
            var tips = U.Col(8);
            for (int i = 0; i < ScanTips.Length; i++)
            {
                var r = U.Row(10).Align(Align.FlexStart);
                r.Add(U.Div().Size(24, 24).Radius(12).Bg(U.TealSoft).Center().Kids(U.Text((i + 1).ToString()).FontSize(12).Bold().Fg(U.TealDark)));
                var t = U.Text(ScanTips[i], "t-small").Fg(U.Ink).Grow();
                t.style.whiteSpace = WhiteSpace.Normal;
                r.Add(t);
                tips.Add(r);
            }
            c.Add(tips);

            // Info teknis: keadaan kamera AR & kartu yang dilihat ARCore (untuk guru/orang tua bila scan belum berhasil)
            var tech = U.Col(6).Bg("#F6F5F9").Radius(12).Pad(10, 12);
            var techHead = U.Row(6);
            techHead.Add(U.Text("Info teknis (untuk guru)", "t-caption").Bold().Fg(U.Ink).Grow());
            var report = U.Text(Ar != null ? Ar.DebugReport() : "Kamera AR tidak tersedia", "t-caption").Fg(U.Muted);
            report.style.whiteSpace = WhiteSpace.Normal;
            techHead.Add(U.Text("Salin").FontSize(12).Bold().Fg(U.TealDark).Pad(2, 4).Tap(() =>
            {
                GUIUtility.systemCopyBuffer = "Organo " + Application.version + " · " + SystemInfo.deviceModel + "\n" + report.text;
                App.Toast("Info teknis disalin");
            }));
            tech.Add(techHead);
            tech.Add(report);
            report.schedule.Execute(() => { if (Ar != null) report.text = Ar.DebugReport(); }).Every(500);
            c.Add(tech);

            OrganoApp.Overlay sheet = null;
            c.Add(U.Button("Mengerti", () => sheet.Close(), "btn-primary"));
            // HP yang tadinya ditandai tidak mendukung AR (mis. ARCore baru dipasang sendiri): coba AR lagi
#if !ORGANO_KAMERA_LAMA
            if (Ar != null && Ar.ArUnsupported)
                c.Add(U.Button("Coba pakai AR lagi", () => { sheet.Close(); Ar.RetryAr(); }, "btn-ghost"));
#endif
            sheet = App.ShowSheet(c);
        }

        void ShowPermission()
        {
            if (m_PermDialog != null && m_PermDialog.IsOpen) return;
            bool denied = CameraPermission.Current == PermissionState.Denied;
            var c = U.Col(10).Align(Align.Center);
            var icon = U.Div().Size(64, 64).Radius(32).Gradient(U.Teal, U.Purple, 135).Center();
            icon.Add(U.Icon(denied ? "lock_white" : "camera", 34));
            c.Add(icon);
            c.Add(U.Text(denied ? "Izin kamera ditolak" : "Izinkan kamera, ya!", "t-h2").TextCenter());
            c.Add(U.Text(denied
                ? "Organo butuh kamera untuk memindai kartu. Kamu bisa menyalakannya lagi lewat Pengaturan HP."
                : "Organo butuh kamera untuk memindai kartu dan menampilkan organ 3D di depanmu.", "t-small", "t-muted").TextCenter());
            if (denied)
            {
                var steps = U.Col(6).AlignSelf(Align.Stretch).Bg("#F6F5F9").Radius(12).Pad(10, 12);
                string[] s = { "Buka <b>Pengaturan</b> di HP-mu", "Pilih aplikasi <b>Organo</b>", "Nyalakan izin <b>Kamera</b>" };
                for (int i = 0; i < s.Length; i++)
                {
                    var r = U.Row(8);
                    r.Add(U.Div().Size(20, 20).Radius(10).Bg(U.Teal).Center().Kids(U.Text((i + 1).ToString()).FontSize(11).Bold().Fg("#FFFFFF")));
                    r.Add(U.Text(s[i], "t-caption").Fg(U.Ink));
                    steps.Add(r);
                }
                c.Add(steps);
            }
            var note = U.Row(8).AlignSelf(Align.Stretch).Bg(U.TealSoft).Radius(10).Pad(8, 10);
            note.Add(U.Icon("shield", 16));
            note.Add(U.Text("Gambar dari kamera tidak disimpan dan tidak dikirim ke mana pun.", "t-caption").Fg(U.TealInk).Grow());
            c.Add(note);
            var col = U.Col(8).AlignSelf(Align.Stretch);
            col.style.marginTop = 6;
            if (denied)
            {
                col.Add(U.Button("Buka Pengaturan", () => { m_PermDialog.Close(); CameraPermission.OpenAppSettings(); }, "btn-primary"));
                col.Add(U.Button("Lihat Pustaka dulu", () => { m_PermDialog.Close(); App.GoTab(NavTab.Library); }, "btn-ghost"));
            }
            else
            {
                col.Add(U.Button("Izinkan Kamera", () =>
                {
                    m_PermDialog.Close();
                    CameraPermission.Request(ok =>
                    {
                        if (!ok) ShowPermission();
                        else if (Ar != null) { Ar.End(); Ar.Begin(); }
                    });
                }, "btn-primary"));
                col.Add(U.Button("Jangan sekarang", () => { m_PermDialog.Close(); App.GoTab(NavTab.Library); }, "btn-ghost"));
            }
            c.Add(col);
            m_PermDialog = App.ShowDialog(c, true);
        }

        public override void Close()
        {
            if (Ar != null) { Ar.CardDetected -= OnCard; Ar.ClearScanFrame(); }
            if (m_Torch) Ar?.SetTorch(false);
        }
    }

    // ==================================================================== Hasil scan & kartu baru

    public class ScanResultScreen : AppScreen
    {
        public override bool UsesCamera => true;
        // sheet menempel ke tepi bawah layar; area aman bawah jadi jarak di dalam sheet
        public override bool FullBleedBottom => true;

        OrganInfo m_Organ;
        DraggableSheet m_Sheet;
        VisualElement m_ArLayer, m_StandIn;
        readonly List<VisualElement> m_ArDots = new List<VisualElement>();
        readonly List<Vector2> m_ArDotPos = new List<Vector2>();
        Label m_ArCalloutT, m_ArCalloutD;
        VisualElement m_ArCallout;
        Vector2 m_LastPos, m_DownPos;
        bool m_Dragged;
        /// <summary>Titik info AR yang diketuk (-1 = belum ada, keterangan disembunyikan).</summary>
        int m_ArSel = -1;

        ARScanController Ar => App.ar;
        /// <summary>Organ yang sedang ditampilkan (untuk uji).</summary>
        public OrganInfo Organ => m_Organ;

        protected override void Build(object arg)
        {
            // HP diarahkan ke kartu lain: halaman ini dibuka ulang dengan organ kartu itu
            if (Ar != null) { Ar.CardSwitched -= OnSwitched; Ar.CardSwitched += OnSwitched; Ar.SwitchEnabled = true; }
            var organ = Db.Get(arg as string) ?? Db.Get("lambung");
            bool added = P.AddCard(organ.id);
            // dibangun ulang saat model AR baru menempel: kabar "kartu baru" tidak diulang
            bool announce = added && !m_Rebuilding;
            m_Rebuilding = false;
            m_Organ = organ;
            var o = m_Organ;
            m_ArDots.Clear();
            m_ArDotPos.Clear();
            m_ArSel = -1;
            m_Follow = null;

            m_StandIn = ScanParts.CameraStandIn();
            Root.Add(m_StandIn);

            var top = U.Row(12).Justify(Justify.SpaceBetween).Align(Align.FlexStart).Pad(16 + App.TopInset, 20, 0, 20);
            top.Add(ScanParts.GlassButton("close", () => { Ar?.ResetDetection(); App.GoTab(NavTab.Scan); }, "Tutup"));
            Root.Add(top);

            // Kartu baru tidak lagi ditampilkan besar menutupi layar: organ yang di-scan langsung terlihat,
            // kabar "kartu baru" cukup muncul sebentar lalu hilang sendiri.
            BuildResult(o);
            if (announce) ShowNewCardNotice(o);
        }

        bool m_Rebuilding;

        void OnSwitched(OrganInfo organ)
        {
            if (organ == null || Root.panel == null || organ == m_Organ) return;
            OrganoAudio.Success();
            App.Go<ScanResultScreen>(organ.id, push: false);
        }

        public override void Close()
        {
            if (Ar == null) return;
            Ar.CardSwitched -= OnSwitched;
            Ar.SwitchEnabled = false;
        }

        void ShowNewCardNotice(OrganInfo o)
        {
            var notice = U.Row(10).AlignSelf(Align.Center).Pad(8, 14, 8, 10).Radius(999).Bg("#FFFFFF");
            notice.Elev(Elevation.E2);
            notice.style.position = Position.Absolute;
            notice.style.top = 74 + App.TopInset; // di bawah tombol tutup
            notice.style.left = 20; notice.style.right = 20;
            notice.style.justifyContent = Justify.Center;
            notice.pickingMode = PickingMode.Ignore;
            notice.Add(U.Icon("cards_star", 22));
            notice.Add(U.Text("Kartu " + o.displayName + " masuk koleksi!").FontSize(13).Bold().Fg(U.Ink));
            var star = U.Row(3).Pad(3, 8).Radius(999).Bg(U.YellowSoft);
            star.Add(U.Icon("stat_star", 12).Tinted(U.C(U.Yellow)));
            star.Add(U.Text("+10").FontSize(11).Bold().Fg("#7A5A00"));
            notice.Add(star);
            notice.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity"), new StylePropertyName("translate") };
            notice.style.transitionDuration = new List<TimeValue> { new TimeValue(0.35f), new TimeValue(0.35f) };
            Root.Add(notice);
            notice.schedule.Execute(() =>
            {
                notice.style.opacity = 0f;
                notice.style.translate = new Translate(0, -12);
            }).ExecuteLater(2600);
            notice.schedule.Execute(() => notice.RemoveFromHierarchy()).ExecuteLater(3100);
        }

        void BuildResult(OrganInfo o)
        {

            // AR stage: model nyata menempel di kartu (HP), atau viewer 3D (Editor / posisi kartu belum pasti).
            // Kalau model AR baru ditempel belakangan, layar ini otomatis berganti ke tampilan AR (lihat Tick).
            bool live = Ar != null && Ar.IsCameraLive && Ar.HasSpawnedModel;
            m_Live = live;
            m_ArLayer = U.Div().Grow();
            if (live)
            {
                m_ArLayer.RegisterCallback<PointerDownEvent>(e =>
                {
                    m_LastPos = m_DownPos = e.position;
                    m_Dragged = false;
                    m_ArLayer.CapturePointer(e.pointerId);
                });
                m_ArLayer.RegisterCallback<PointerMoveEvent>(e =>
                {
                    if (!m_ArLayer.HasPointerCapture(e.pointerId)) return;
                    if (!m_Dragged && ((Vector2)e.position - m_DownPos).magnitude > 8f) m_Dragged = true;
                    // geser ke segala arah: model berputar 360° mengikuti jari (ketukan tidak ikut memutar)
                    if (m_Dragged) Ar.RotateSpawned((Vector2)e.position - m_LastPos);
                    m_LastPos = e.position;
                });
                m_ArLayer.RegisterCallback<PointerUpEvent>(e =>
                {
                    if (!m_ArLayer.HasPointerCapture(e.pointerId)) return;
                    m_ArLayer.ReleasePointer(e.pointerId);
                    if (!m_Dragged) TapSpot(m_ArLayer.WorldToLocal(e.position));
                });
                m_ArCallout = U.Col(2, "callout");
                m_ArCallout.pickingMode = PickingMode.Ignore;
                m_ArCalloutT = U.Text("", "b").FontSize(12);
                m_ArCalloutD = U.Text("", "t-muted").FontSize(10.5f);
                m_ArCallout.Add(m_ArCalloutT); m_ArCallout.Add(m_ArCalloutD);
                m_ArLayer.Add(m_ArCallout);
            }
            else if (Ar != null && Ar.CanRecognizeWithCamera)
            {
                // HP tanpa AR (kamera biasa): model mengikuti kartu di gambar kamera (lihat CardFollowView)
                m_Follow = new CardFollowView(App.Viewer, o);
                m_ArLayer.Add(m_Follow.Viewport);
            }
            else if (Ar != null && Ar.IsCameraLive)
            {
                // AR, kartu sudah dikenali tapi posisinya belum pasti: TIDAK ada model diam di tengah layar. Model baru
                // muncul saat ARCore menempelkannya di atas kartu (layar ini dibangun ulang, lihat Tick).
            }
            else
            {
                var shadow = U.Div().Size(200, 30).Radius(100).Bg(U.C("#140E08", 0.35f)).AlignSelf(Align.Center).Abs(95, null, null, 46);
                m_ArLayer.Add(shadow);
                var vp = new OrganViewport(App.Viewer, o, false) { FreeRotate = true };
                vp.style.position = Position.Absolute;
                vp.style.left = 20; vp.style.right = 20; vp.style.top = 20; vp.style.bottom = 40;
                m_ArLayer.Add(vp);
            }
            Root.Add(m_ArLayer);

            var hint = U.Div().AlignSelf(Align.Center).Pad(6, 14).Radius(999).Bg(U.C("#141220", 0.6f));
            // "putar dengan jari" cukup beberapa kali pertama; petunjuk saat kartu belum / tidak terlihat selalu tampil
            m_ShowRotateHint = Hints.Take(Hints.ScanRotate, 3);
            m_HintText = U.Text(live || Ar == null || !Ar.IsCameraLive ? HintRotate : HintWait).FontSize(11).Fg("#FFFFFF");
            m_HintBox = hint;
            m_SheetOpen = false;
            hint.Visible(m_HintText.text != HintRotate || m_ShowRotateHint);
            hint.Add(m_HintText);
            hint.style.marginBottom = 12;
            hint.pickingMode = PickingMode.Ignore;
            Root.Add(hint);
            // ruang untuk sheet ringkas (sheet melayang di atas tampilan AR)
            var spacer = U.Div().NoShrink().H(230);
            spacer.pickingMode = PickingMode.Ignore;
            Root.Add(spacer);

            // ---------- Sheet yang bisa ditarik ke atas / ke bawah ----------
            var sheet = new DraggableSheet();
            m_Sheet = sheet;
            sheet.PeekHeightChanged += h => spacer.style.height = h;
            sheet.BottomInset = App.BottomInset;
            var title = U.Row(10);
            title.style.flexWrap = Wrap.Wrap;
            title.Add(U.Text(o.displayName, "t-h2"));
            title.Add(U.Pill(o.SystemLabel, U.Purple, "#FFFFFF"));
            sheet.Peek.Add(title);
            var desc = U.Text(o.shortDescription, "t-caption");
            desc.style.whiteSpace = WhiteSpace.Normal;
            sheet.Peek.Add(desc);
            var have = U.Row(8).MinH(40).Pad(8, 12).Radius(12).Bg(U.TealSoft).Border(1, "#9CE6DC");
            have.Add(U.Icon("cards_ok", 20));
            have.Add(U.Text("Kartu " + o.displayName + " di koleksimu", "t-caption").Fg(U.TealInk).Grow());
            have.Add(U.Text("Lihat").FontSize(12).Bold().Fg(U.TealInk));
            have.Tap(() => App.Go<CardViewerScreen>(o.id));
            sheet.Peek.Add(have);
            var btns = U.Row(10);
            btns.style.marginTop = 6;
            btns.Add(U.Button("Pelajari", () => App.Go<OrganDetailScreen>(o.id), "btn-outline").Grow().MinH(48));
            // kartunya baru di-scan, jadi kuis organ ini sudah terbuka
            btns.Add(U.Button("Mulai Kuis", () => QuizLocks.PlayOrgan(App, o.id), "btn-primary").Grow().MinH(48));
            sheet.Peek.Add(btns);
            // panah kecil = tanda sheet bisa ditarik (tanpa kalimat petunjuk)
            var up = U.Icon("chevron_down", 14).Tinted(U.C(U.Faint));
            up.pickingMode = PickingMode.Ignore;
            up.style.rotate = new Rotate(180);
            up.style.alignSelf = Align.Center;
            sheet.Peek.Add(up);
            sheet.OpenChanged += open =>
            {
                up.style.rotate = new Rotate(open ? 0 : 180);
                m_SheetOpen = open;
                hint.Visible(!open && (m_HintText.text != HintRotate || m_ShowRotateHint));
            };
            BuildMore(o, sheet.More.contentContainer);
            Root.Add(sheet);
        }

        /// <summary>Isi sheet saat ditarik ke atas: fungsi, bagian-bagian, fakta acak, info lengkap.</summary>
        void BuildMore(OrganInfo o, VisualElement c)
        {
            c.SetGap(18, false);
            var content = OrganContent.Get(o.id);

            if (!string.IsNullOrEmpty(o.function))
            {
                var job = U.Row(12).Align(Align.FlexStart).Bg(U.TealSoft).Radius(14).Pad(12, 14);
                job.Add(U.Icon("check_teal", 20));
                var jt = U.Col(2).Grow();
                jt.Add(U.Text("Tugas utamanya", "t-small").Bold().Fg(U.TealInk));
                var jl = U.Text(o.function, "t-body").FontSize(13).Fg(U.TealInk);
                jl.style.whiteSpace = WhiteSpace.Normal;
                jt.Add(jl);
                job.Add(jt);
                c.Add(job);
            }

            if (content != null && content.parts.Length > 0)
            {
                var sec = U.Col(10);
                sec.Add(OrganVisuals.Section("Bagian-bagian " + o.displayName, o.accent));
                var rail = U.Scroll(true);
                rail.style.flexGrow = 0; rail.style.flexShrink = 0;
                rail.style.marginLeft = -20; rail.style.marginRight = -20;
                rail.contentContainer.style.flexDirection = FlexDirection.Row;
                rail.contentContainer.style.paddingLeft = 20;
                rail.contentContainer.style.paddingRight = 8;
                foreach (var pd in content.parts)
                {
                    var pc = U.Col(6).W(124).MinH(160).NoShrink().Radius(16).Bg("#FFFFFF").Border(1, U.Line).Pad(8); // tinggi ikut isi: teks fungsi panjang tidak keluar kartu
                    pc.style.marginRight = 10;
                    pc.Add(OrganVisuals.PartPicture(o, pd, 106, 76));
                    var nm = U.Text(pd.name, "t-small").Bold();
                    nm.style.whiteSpace = WhiteSpace.Normal;
                    pc.Add(nm);
                    var fn = U.Text(pd.function).FontSize(10).Fg(U.Muted);
                    fn.style.whiteSpace = WhiteSpace.Normal;
                    pc.Add(fn);
                    string target = o.id + "#" + pd.id;
                    pc.Tap(() => App.Go<OrganDetailScreen>(target));
                    pc.tooltip = "Lihat " + pd.name + " di model 3D";
                    rail.Add(pc);
                }
                sec.Add(rail);
                c.Add(sec);
            }

            // satu fakta acak
            var facts = new List<string>();
            if (!string.IsNullOrEmpty(o.funFact)) facts.Add(o.funFact);
            if (content?.facts != null) foreach (var f in content.facts) if (!facts.Contains(f)) facts.Add(f);
            if (facts.Count > 0)
            {
                var fact = U.Row(12).Align(Align.FlexStart).Bg(U.TealDark).Radius(16).Pad(14);
                fact.Add(U.Icon("bulb", 24));
                var ft = U.Col(4).Grow();
                ft.Add(U.Text("Tahukah Kamu?", "t-body").Bold().Fg("#FFFFFF"));
                var fl = U.Text(facts[Random.Range(0, facts.Count)]).FontSize(12).Fg("#FFFFFF");
                fl.style.whiteSpace = WhiteSpace.Normal;
                ft.Add(fl);
                if (facts.Count > 1)
                {
                    var next = U.Row(4).AlignSelf(Align.FlexStart).Pad(4, 10).Radius(999).Bg(U.C("#FFFFFF", 0.18f));
                    next.Add(U.Text("Fakta lain").FontSize(11).Bold().Fg("#FFFFFF"));
                    next.Add(U.Icon("arrow_right", 12).Tinted(Color.white));
                    next.Tap(() =>
                    {
                        int i = facts.IndexOf(fl.text), j = Random.Range(0, facts.Count - 1);
                        fl.text = facts[j >= i ? j + 1 : j];
                    });
                    ft.Add(next);
                }
                fact.Add(ft);
                c.Add(fact);
            }

            var info = U.Row(12).Bg(U.PurpleSoft).Border(1, "#D3C8F7").Radius(16).Pad(12, 14);
            info.Add(U.Div().Size(44, 44).Radius(22).Bg("#FFFFFF").Center().Kids(U.Icon("g_book", 26)));
            var it = U.Col(2).Grow();
            it.Add(U.Text("Info Lengkap " + o.displayName, "t-body").Bold());
            info.Add(it);
            info.Add(U.Icon("chevron_right", 18));
            info.Tap(() => App.Go<OrganInfoScreen>(o.id));
            c.Add(info);
        }

        /// <summary>Ketuk titik info: tampilkan nama & tugas bagian itu (ketuk lagi / ketuk tempat lain = tutup).</summary>
        void TapSpot(Vector2 local)
        {
            int hit = -1;
            float best = 32f;
            for (int i = 0; i < m_ArDots.Count && i < m_ArDotPos.Count; i++)
            {
                if (m_ArDots[i].resolvedStyle.display == DisplayStyle.None) continue;
                float d = Vector2.Distance(local, m_ArDotPos[i]);
                if (d < best) { best = d; hit = i; }
            }
            m_ArSel = hit == m_ArSel ? -1 : hit;
        }

        const string HintRotate = "Ketuk titik untuk melihat bagiannya, geser untuk memutar";
        const string HintWait = "Gerakkan HP pelan di depan kartu supaya organ muncul di atasnya";
        const string HintLost = "Arahkan kamera ke kartu lagi untuk melihat organ";
        bool m_Live, m_ShowRotateHint, m_SheetOpen;
        // model yang mengikuti kartu (kamera biasa)
        CardFollowView m_Follow;

        void TickTracked(float dt)
        {
            bool show = m_Follow.Tick(Ar, Root, m_Organ, dt);
            string ht = show ? HintRotate : HintLost;
            if (m_HintText != null && m_HintText.text != ht)
            {
                m_HintText.text = ht;
                m_HintBox?.Visible(!m_SheetOpen && (ht != HintRotate || m_ShowRotateHint));
            }
        }
        Label m_HintText;
        VisualElement m_HintBox;

        public override void Tick(float dt)
        {
            m_StandIn.Visible(!(Ar != null && Ar.IsCameraLive));
            if (m_Sheet != null) m_Sheet.BottomInset = App.BottomInset; // area aman bisa baru diketahui setelah layar dibuka
            if (m_Follow != null && Ar != null) { TickTracked(dt); return; }
            // model AR baru ditempel (ARCore sudah yakin dengan posisi kartu): ganti dari viewer 3D ke tampilan AR
            if (!m_Live && Ar != null && Ar.IsCameraLive && Ar.HasSpawnedModel && Ar.CurrentOrgan == m_Organ && m_ArLayer != null)
            {
                m_Rebuilding = true;
                Refresh(m_Organ.id);
                return;
            }
            if (m_ArCallout == null || Ar == null) return;
            // model (di atas kartu) di luar layar / kartu hilang dari ARCore: beri petunjuk arahkan kamera ke kartu
            bool visible = Ar.ModelInView;
            string ht = visible ? HintRotate : HintLost;
            if (m_HintText != null && m_HintText.text != ht)
            {
                m_HintText.text = ht;
                m_HintBox?.Visible(!m_SheetOpen && (ht != HintRotate || m_ShowRotateHint));
            }
            if (!visible)
            {
                foreach (var d in m_ArDots) d.Visible(false);
                m_ArCallout.Visible(false);
                return;
            }
            var spots = Ar.SpawnedHotspots;
            var r = m_ArLayer.worldBound;
            var rootR = Root.worldBound;
            while (m_ArDots.Count < spots.Count)
            {
                // titik tidak menangkap sentuhan sendiri: lapisan AR yang membedakan ketuk (pilih titik) dan geser (putar)
                var d = U.Div("hotspot");
                d.pickingMode = PickingMode.Ignore;
                var ring = U.Div("hotspot__ring");
                ring.pickingMode = PickingMode.Ignore;
                d.Add(ring);
                m_ArLayer.Add(d);
                m_ArDots.Add(d);
                m_ArDotPos.Add(Vector2.zero);
                m_ArCallout.BringToFront(); // keterangan di atas titik lain
            }
            if (m_ArSel >= spots.Count) m_ArSel = -1;
            m_ArCallout.Visible(false);
            // cincin berdenyut seperti titik di close-up: tanda titik bisa diketuk
            float pulse = 1f + 0.6f * Mathf.Repeat(Time.unscaledTime * 0.6f, 1f);
            for (int i = 0; i < m_ArDots.Count; i++)
            {
                if (!Ar.TryProjectHotspot(i, out var uv)) { m_ArDots[i].Visible(false); continue; }
                float x = uv.x * rootR.width - (r.x - rootR.x), y = uv.y * rootR.height - (r.y - rootR.y);
                m_ArDotPos[i] = new Vector2(x, y);
                m_ArDots[i].Visible(true);
                m_ArDots[i].style.left = x;
                m_ArDots[i].style.top = y;
                m_ArDots[i].EnableInClassList("hotspot--selected", i == m_ArSel);
                var ring = m_ArDots[i][0];
                ring.style.scale = new Scale(new Vector3(pulse, pulse, 1f));
                ring.style.opacity = 1.6f - pulse;
                if (i == m_ArSel)
                {
                    m_ArCallout.Visible(true);
                    m_ArCalloutT.text = spots[i].title;
                    m_ArCalloutD.text = spots[i].description;
                    // di kanan titik; kalau tidak muat, di kirinya
                    m_ArCallout.style.left = x + 20 + 136 <= r.width ? x + 20 : Mathf.Max(0, x - 20 - 128);
                    m_ArCallout.style.top = Mathf.Clamp(y - 24, 0, Mathf.Max(0, r.height - 70));
                }
            }
        }
    }
}
