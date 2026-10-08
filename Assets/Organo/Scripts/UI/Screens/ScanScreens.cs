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
            return bg;
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
        float m_ScanTime, m_Sweep;
        VisualElement m_StandIn, m_Dim, m_Frame, m_SweepLine, m_Card, m_StatusIcon, m_Actions, m_Tips, m_Torch_, m_Banner, m_MascotRow;
        Label m_BannerLabel, m_Title, m_Desc;
        SvgImage m_FrameSvg;
        OrganoApp.Overlay m_PermDialog;

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

            var top = U.Row().Justify(Justify.SpaceBetween).Pad(16 + App.TopInset, 20, 0, 20);
            m_Torch_ = ScanParts.GlassButton("torch", () => SetTorch(!m_Torch), "Senter");
            top.Add(m_Torch_);
            top.Add(ScanParts.GlassButton("help", ShowPermission, "Bantuan"));
            Root.Add(top);

            m_Banner = ScanParts.Banner("Arahkan kamera ke kartu organ", U.TealDark, out m_BannerLabel);
            m_Banner.style.marginTop = 16;
            Root.Add(m_Banner);

            // Reticle
            var stage = U.Div().Grow().Center();
            m_Frame = U.Div().Size(256, 256);
            m_FrameSvg = U.Icon("scan_frame", 256);
            m_FrameSvg.AddToClassList("fill");
            m_Card = U.Col(10).Abs(68, 58).Size(120, 140).Radius(14).Bg("#FFFFFF").Center().Opacity(0.92f);
            m_Card.style.rotate = new Rotate(-4);
            var demo = Db.Get(m_DemoOrgan) ?? Db.Get("lambung");
            m_Card.Add(U.Icon(demo.IconKey, 56, 50));
            m_Card.Add(U.Div().Size(60, 5).Radius(3).Bg("#D9D7E3"));
            m_Frame.Add(m_Card);
            m_SweepLine = U.Div().Abs(22, 30, 22).H(3).Radius(2).Bg(U.Teal);
            m_SweepLine.pickingMode = PickingMode.Ignore;
            m_Frame.Add(m_SweepLine);
            m_Frame.Add(m_FrameSvg);
            m_Frame.tooltip = "Bingkai scan";
            m_Frame.Tap(() =>
            {
                // Demo: ketuk bingkai = kartu terdeteksi (berguna di Editor & sebelum gambar kartu dipasang)
                if (m_Phase == Phase.Scanning && (Ar == null || !Ar.IsCameraLive || !Ar.HasCardLibrary))
                    OnCard(Db.Get(m_DemoOrgan));
            });
            stage.Add(m_Frame);
            Root.Add(stage);

            // Mascot hint (idle)
            m_MascotRow = U.Row(0).Align(Align.FlexStart).Pad(0, 20, 8, 20);
            m_MascotRow.Add(U.Icon("mascot_scan", 72));
            var bubble = U.Div().Bg("#FFFFFF").Radius(16).Pad(10, 14);
            bubble.style.marginLeft = -4;
            bubble.style.marginTop = 8;
            bubble.Add(U.Text("Yuk, coba scan kartumu!", "t-small").Bold());
            m_MascotRow.Add(bubble);

            // Status card
            var statusWrap = U.Col(0).Pad(0, 20, 12, 20);
            var status = U.Col(10).Bg("#FFFFFF").Radius(18).Pad(14, 16);
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
            foreach (var t in new[] { "Pakai kartu Organo, bukan gambar lain", "Seluruh kartu masuk ke dalam bingkai", "Hindari pantulan cahaya di kartu" })
            {
                var r = U.Row(8);
                r.Add(U.Text("✓").Bold().Fg(U.Teal).FontSize(12));
                r.Add(U.Text(t, "t-caption").Fg(U.Ink).Grow());
                m_Tips.Add(r);
            }
            status.Add(m_Tips);
            m_Actions = U.Row(8);
            status.Add(m_Actions);
            statusWrap.Add(status);

            var helper = U.Row(0).AlignSelf(Align.Center).Pad(9, 16).Radius(999).Bg(U.C("#141220", 0.6f));
            helper.style.marginBottom = 12;
            helper.Add(U.Text("Belum punya kartu? ").FontSize(12).Fg("#FFFFFF"));
            helper.Add(U.Text("Lihat Library").FontSize(12).Bold().Fg("#9CE6DC").Tap(() => App.GoTab(NavTab.Library)));

            Root.Add(m_MascotRow);
            Root.Add(statusWrap);
            Root.Add(helper);

            SetPhase(Phase.Scanning);
            if (CameraPermission.Current != PermissionState.Granted) Root.schedule.Execute(ShowPermission).ExecuteLater(250);
        }

        void SetPhase(Phase p)
        {
            m_Phase = p;
            m_ScanTime = 0;
            string color, banner, title, desc, iconBg, icon;
            switch (p)
            {
                case Phase.Dark:
                    color = U.Yellow; banner = "#B07D00"; title = "Ruangannya terlalu gelap"; iconBg = U.YellowSoft; icon = "moon";
                    desc = "Nyalakan senter atau pindah ke tempat yang lebih terang."; break;
                case Phase.Unknown:
                    color = U.Pink; banner = "#C93758"; title = "Hmm, kartunya belum dikenali"; iconBg = U.PinkSoft; icon = "question";
                    desc = "Coba cek beberapa hal ini lalu scan lagi."; break;
                default:
                    color = U.Teal; banner = U.TealDark; title = "Tahan kartunya, ya!"; iconBg = U.TealSoft; icon = "scan_small";
                    desc = "Jaga HP tetap diam sampai organ muncul."; break;
            }
            m_BannerLabel.text = p == Phase.Scanning ? "Sedang memindai…" : p == Phase.Dark ? "Terlalu gelap" : "Kartu tidak dikenali";
            m_Banner.Bg(banner);
            m_FrameSvg.Tint = U.C(color);
            m_SweepLine.Visible(p == Phase.Scanning);
            m_Card.style.rotate = new Rotate(p == Phase.Unknown ? -14 : -4);
            m_Card.style.opacity = p == Phase.Dark ? 0.35f : 0.92f;
            m_Dim.style.backgroundColor = U.C("#0A0814", p == Phase.Dark ? 0.62f : 0f);
            m_StatusIcon.Bg(iconBg);
            m_StatusIcon.Clear();
            var ic = U.Icon(icon, 20);
            if (icon == "scan_small") ic.Tint = U.C(U.TealInk);
            m_StatusIcon.Add(ic);
            m_Title.text = title;
            m_Desc.text = desc;
            m_Tips.Visible(p == Phase.Unknown);
            m_MascotRow.Visible(p == Phase.Scanning);

            m_Actions.Clear();
            if (p == Phase.Dark)
                m_Actions.Add(U.Button("Nyalakan Senter", () => { SetTorch(true); SetPhase(Phase.Scanning); }, "btn-small").Bg(U.Yellow).Grow());
            else if (p == Phase.Unknown)
            {
                m_Actions.Add(U.Button("Coba Lagi", () => SetPhase(Phase.Scanning), "btn-primary", "btn-small").Grow());
                m_Actions.Add(U.Button("Lihat Kartu", () => App.Go<CardCollectionScreen>(), "btn-outline", "btn-small").Grow());
            }
            else if (Ar == null || !Ar.IsCameraLive || !Ar.HasCardLibrary)
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
                    var missing = Db.Ordered.FirstOrDefault(o => !P.HasCard(o.id));
                    OnCard(missing ?? Db.Get(m_DemoOrgan));
                }));
                m_Actions.Add(demo);
            }
        }

        void SetTorch(bool on)
        {
            m_Torch = on;
            Ar?.SetTorch(on);
            m_Torch_.Bg(on ? U.C(U.Yellow) : U.C("#141220", 0.55f));
            if (on && Ar != null && Ar.IsCameraLive && !Ar.TorchSupported) App.Toast("Senter tidak tersedia di HP ini");
        }

        void OnCard(OrganInfo organ)
        {
            if (organ == null || Root.panel == null) return;
            OrganoAudio.Success();
            App.Go<ScanResultScreen>(organ.id);
        }

        public override void Tick(float dt)
        {
            bool live = Ar != null && Ar.IsCameraLive;
            m_StandIn.Visible(!live);
            m_Sweep = Mathf.Repeat(m_Sweep + dt / 1.8f, 1f);
            float y = 30 + Mathf.PingPong(m_Sweep * 2f, 1f) * 196;
            m_SweepLine.style.top = y;
            if (m_Phase != Phase.Scanning) return;
            m_ScanTime += dt;
            if (live && Ar.IsDark && !m_Torch) SetPhase(Phase.Dark);
            else if (live && Ar.HasCardLibrary && m_ScanTime > 15f) SetPhase(Phase.Unknown);
        }

        void ShowPermission()
        {
            if (m_PermDialog != null && m_PermDialog.IsOpen) return;
            bool denied = CameraPermission.Current == PermissionState.Denied;
            var c = U.Col(10).Align(Align.Center);
            var icon = U.Div().Size(64, 64).Radius(32).Gradient(U.Teal, U.Purple, 135).Center();
            icon.Add(U.Icon(denied ? "lock_white" : "nav_scan", 30));
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
                col.Add(U.Button("Lihat Library dulu", () => { m_PermDialog.Close(); App.GoTab(NavTab.Library); }, "btn-ghost"));
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
            if (Ar != null) Ar.CardDetected -= OnCard;
            if (m_Torch) Ar?.SetTorch(false);
        }
    }

    // ==================================================================== Hasil scan & kartu baru

    public class ScanResultScreen : AppScreen
    {
        public override bool UsesCamera => true;

        OrganInfo m_Organ;
        bool m_IsNew;
        VisualElement m_ArLayer, m_StandIn, m_Reveal;
        readonly List<VisualElement> m_ArDots = new List<VisualElement>();
        Label m_ArCalloutT, m_ArCalloutD;
        VisualElement m_ArCallout;
        float m_RevealT;
        float m_LastX;

        ARScanController Ar => App.ar;

        protected override void Build(object arg)
        {
            m_Organ = Db.Get(arg as string) ?? Db.Get("lambung");
            var o = m_Organ;
            m_IsNew = P.AddCard(o.id);
            m_RevealT = 0;
            m_ArDots.Clear();

            m_StandIn = ScanParts.CameraStandIn();
            Root.Add(m_StandIn);
            if (m_IsNew)
            {
                var dim = U.Div("fill").Bg(U.C("#141220", 0.78f));
                dim.pickingMode = PickingMode.Ignore;
                Root.Add(dim);
            }

            var top = U.Row(12).Justify(Justify.SpaceBetween).Align(Align.FlexStart).Pad(16 + App.TopInset, 20, 0, 20);
            top.Add(ScanParts.GlassButton("close", () => { Ar?.ResetDetection(); App.GoTab(NavTab.Scan); }, "Tutup"));
            if (m_IsNew)
            {
                var ok = U.Row(6).Pad(8, 14, 8, 10).Radius(999).Bg(U.TealDark);
                ok.Add(U.Icon("check", 14));
                ok.Add(U.Text("Kartu dikenali").FontSize(13).Bold().Fg("#FFFFFF"));
                top.Add(ok);
            }
            else
            {
                var name = U.Div().Pad(8, 16).Radius(999).Bg(U.C("#141220", 0.55f));
                name.Add(U.Text(o.displayName).FontSize(13).Fg("#FFFFFF"));
                top.Add(name);
            }
            Root.Add(top);

            if (m_IsNew) BuildNewCard(o); else BuildResult(o);
        }

        void BuildResult(OrganInfo o)
        {
            var banner = U.Row(8).AlignSelf(Align.Center).Pad(8, 16, 8, 12).Radius(999).Bg(U.TealDark);
            banner.style.marginTop = 16;
            banner.Add(U.Div().Size(20, 20).Radius(10).Bg("#FFFFFF").Center().Kids(U.Icon("check", 12).Tinted(U.C(U.TealDark))));
            banner.Add(U.Text(o.displayName + " terdeteksi!").FontSize(13).Bold().Fg("#FFFFFF"));
            banner.Add(U.Icon("mascot_mini", 30));
            Root.Add(banner);

            // AR stage: model nyata menempel di kartu (HP), atau viewer 3D (demo di Editor)
            bool live = Ar != null && Ar.IsCameraLive && Ar.HasSpawnedModel;
            m_ArLayer = U.Div().Grow();
            if (live)
            {
                m_ArLayer.RegisterCallback<PointerDownEvent>(e => { m_LastX = e.position.x; m_ArLayer.CapturePointer(e.pointerId); });
                m_ArLayer.RegisterCallback<PointerMoveEvent>(e =>
                {
                    if (!m_ArLayer.HasPointerCapture(e.pointerId)) return;
                    Ar.RotateSpawned(e.position.x - m_LastX);
                    m_LastX = e.position.x;
                });
                m_ArLayer.RegisterCallback<PointerUpEvent>(e => m_ArLayer.ReleasePointer(e.pointerId));
                m_ArCallout = U.Col(2, "callout");
                m_ArCallout.pickingMode = PickingMode.Ignore;
                m_ArCalloutT = U.Text("", "b").FontSize(12);
                m_ArCalloutD = U.Text("", "t-muted").FontSize(10.5f);
                m_ArCallout.Add(m_ArCalloutT); m_ArCallout.Add(m_ArCalloutD);
                m_ArLayer.Add(m_ArCallout);
            }
            else
            {
                var shadow = U.Div().Size(200, 30).Radius(100).Bg(U.C("#140E08", 0.35f)).AlignSelf(Align.Center).Abs(95, null, null, 46);
                m_ArLayer.Add(shadow);
                var vp = new OrganViewport(App.Viewer, o, false);
                vp.style.position = Position.Absolute;
                vp.style.left = 20; vp.style.right = 20; vp.style.top = 20; vp.style.bottom = 40;
                m_ArLayer.Add(vp);
            }
            Root.Add(m_ArLayer);

            var hint = U.Div().AlignSelf(Align.Center).Pad(6, 14).Radius(999).Bg(U.C("#141220", 0.6f));
            hint.Add(U.Text("Putar dengan jari untuk melihat sisi lain").FontSize(11).Fg("#FFFFFF"));
            hint.style.marginBottom = 12;
            Root.Add(hint);

            var sheet = U.Col(8).Bg("#FFFFFF").Pad(10, 20, 24, 20);
            sheet.style.borderTopLeftRadius = 20; sheet.style.borderTopRightRadius = 20;
            sheet.Add(U.Div("sheet__grip"));
            var title = U.Row(10);
            title.style.flexWrap = Wrap.Wrap;
            title.Add(U.Text(o.displayName, "t-h2"));
            title.Add(U.Pill(o.SystemLabel, U.Purple, "#FFFFFF"));
            sheet.Add(title);
            sheet.Add(U.Text(o.shortDescription, "t-caption"));
            var have = U.Row(8).MinH(40).Pad(8, 12).Radius(12).Bg(U.TealSoft).Border(1, "#9CE6DC");
            have.Add(U.Icon("cards_ok", 20));
            have.Add(U.Text("Kartu " + o.displayName + " sudah ada di koleksimu", "t-caption").Fg(U.TealInk).Grow());
            have.Add(U.Text("Lihat").FontSize(12).Bold().Fg(U.TealInk));
            have.Tap(() => App.Go<CardCollectionScreen>(o.id));
            sheet.Add(have);
            var btns = U.Row(10);
            btns.style.marginTop = 6;
            btns.Add(U.Button("Pelajari Lebih Lanjut", () => App.Go<OrganDetailScreen>(o.id), "btn-outline").Grow().MinH(48));
            btns.Add(U.Button("Mulai Quiz", () => App.Go<QuizPlayScreen>(o.system), "btn-primary").Grow().MinH(48));
            sheet.Add(btns);
            Root.Add(sheet);
        }

        void BuildNewCard(OrganInfo o)
        {
            var reveal = U.Col(14).Grow().Center().Pad(0, 20);
            var head = U.Col(2).Align(Align.Center);
            head.Add(U.Text("KARTU BARU TERBUKA!").FontSize(13).Bold().Fg("#9CE6DC"));
            head.Add(U.Text(o.displayName).FontSize(26).Bold().Fg("#FFFFFF"));
            reveal.Add(head);
            var stage = U.Div().Size(280, 300).Center();
            var glow = U.Div().Size(200, 200).Radius(100).Bg(U.C(U.Teal, 0.35f)).Abs(40, 50);
            stage.Add(glow);
            stage.Add(U.Icon("sparkles", 280).Abs(0, 10));
            m_Reveal = Parts.OrganCard(o, true, 172, true);
            stage.Add(m_Reveal);
            reveal.Add(stage);
            Root.Add(reveal);

            var sheet = U.Col(12).Bg("#FFFFFF").Pad(18, 20, 24, 20);
            sheet.style.borderTopLeftRadius = 24; sheet.style.borderTopRightRadius = 24;
            sheet.Add(U.Text("Kartu otomatis masuk ke koleksimu. Penjelasan lengkap " + o.displayName + " sekarang bisa dibuka di Library.", "t-small").Fg("#4A4669").TextCenter());
            var prog = U.Row(12).Pad(12, 14).Radius(14).Bg("#F6F5F9");
            var pc = U.Col(6).Grow();
            var pr = U.Row().Justify(Justify.SpaceBetween);
            int got = P.cards.Count, total = Db.Count;
            pr.Add(U.Text("Koleksi kartu").FontSize(12).Bold());
            pr.Add(U.Text(got + " / " + total).FontSize(12).Bold().Fg(U.TealDark));
            pc.Add(pr);
            var track = U.Div().H(8).Radius(999).Bg("#E4E2EC");
            track.style.overflow = Overflow.Hidden;
            var fill = U.Div().H(8).Radius(999).Gradient(U.Teal, U.Purple, 90);
            fill.style.width = Length.Percent(100f * got / Mathf.Max(1, total));
            track.Add(fill);
            pc.Add(track);
            prog.Add(pc);
            var star = U.Row(4).Pad(6, 10).Radius(999).Bg(U.YellowSoft);
            star.Add(U.Icon("star", 14).Tinted(U.C(U.Yellow)));
            star.Add(U.Text("+10").FontSize(12).Bold().Fg("#7A5A00"));
            prog.Add(star);
            sheet.Add(prog);
            var btns = U.Row(10);
            btns.Add(U.Button("Lihat Koleksi", () => App.Go<CardCollectionScreen>(o.id), "btn-outline").Grow().MinH(50));
            var learn = U.Button("Pelajari " + o.displayName, () => App.Go<OrganDetailScreen>(o.id), "btn-primary").MinH(50);
            learn.Grow(1.3f);
            btns.Add(learn);
            sheet.Add(btns);
            sheet.Add(U.Text("Scan kartu lain").FontSize(13).Bold().Fg(U.TealDark).AlignSelf(Align.Center).Tap(() => { Ar?.ResetDetection(); App.GoTab(NavTab.Scan); }));
            Root.Add(sheet);
        }

        public override void Tick(float dt)
        {
            m_StandIn.Visible(!(Ar != null && Ar.IsCameraLive));
            if (m_Reveal != null && m_IsNew)
            {
                m_RevealT += dt;
                float t = Mathf.Clamp01(m_RevealT / 0.7f);
                float flip = Mathf.Abs(Mathf.Cos(t * Mathf.PI)); // kartu "berbalik"
                float sx = t < 1 ? Mathf.Max(0.02f, t < 0.5f ? flip : flip) : 1f;
                m_Reveal.style.scale = new Scale(new Vector3(sx, 1, 1));
                m_Reveal.style.rotate = new Rotate(Mathf.Sin(m_RevealT * 1.4f) * 2f);
            }
            if (m_ArCallout == null || Ar == null) return;
            var spots = Ar.SpawnedHotspots;
            var r = m_ArLayer.worldBound;
            var rootR = Root.worldBound;
            while (m_ArDots.Count < spots.Count)
            {
                var d = U.Div("hotspot");
                d.Add(U.Div("hotspot__ring"));
                m_ArLayer.Add(d);
                m_ArDots.Add(d);
            }
            m_ArCallout.Visible(false);
            for (int i = 0; i < m_ArDots.Count; i++)
            {
                if (!Ar.TryProjectHotspot(i, out var uv)) { m_ArDots[i].Visible(false); continue; }
                float x = uv.x * rootR.width - (r.x - rootR.x), y = uv.y * rootR.height - (r.y - rootR.y);
                m_ArDots[i].Visible(true);
                m_ArDots[i].style.left = x;
                m_ArDots[i].style.top = y;
                if (i == 0)
                {
                    m_ArCallout.Visible(true);
                    m_ArCalloutT.text = spots[0].title;
                    m_ArCalloutD.text = spots[0].description;
                    m_ArCallout.style.left = Mathf.Min(x + 20, r.width - 136);
                    m_ArCallout.style.top = Mathf.Max(0, y - 24);
                }
            }
        }
    }
}
