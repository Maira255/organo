using System.Collections.Generic;
using Organo.Data;
using Organo.UI;
using UnityEngine;
using UnityEngine.UIElements;
using U = Organo.UI.UI;

namespace Organo.Screens
{
    // ==================================================================== Splash

    public class SplashScreen : AppScreen
    {
        VisualElement m_Bar, m_Mascot, m_Orbit;
        Label m_Status;
        float m_T;
        bool m_Ready;
        static readonly string[] Tips = { "Menyiapkan organ…", "Membangunkan maskot…", "Hampir siap!" };

        protected override void Build(object arg)
        {
            m_T = 0; m_Ready = false;
            Root.Gradient(U.Teal, U.Purple, 160);
            Root.Pad(56, 24, 40, 24).Align(Align.Center);

            var deco1 = U.Div().Size(320, 320).Radius(160).Bg(U.C("#FFFFFF", 0.09f)).Abs(-120, -120);
            var deco2 = U.Div().Size(360, 360).Radius(180).Bg(U.C("#FFFFFF", 0.07f)).Abs(null, null, -140, -110);
            deco1.pickingMode = deco2.pickingMode = PickingMode.Ignore;
            Root.Add(deco1); Root.Add(deco2);

            Root.Add(U.Flex());
            var hero = U.Div().Size(280, 280);
            m_Orbit = U.Div("fill").Radius(140).Border(1.5f, U.C("#FFFFFF", 0.4f));
            hero.Add(m_Orbit);
            hero.Add(U.Div().Abs(44, 44, 44, 44).Radius(96).Bg(U.C("#FFFFFF", 0.16f)).Border(2, U.C("#FFFFFF", 0.22f)));
            m_Mascot = U.Icon("mascot_splash", 120, 112).Abs(80, 100);
            hero.Add(m_Mascot);
            var bubbles = new[] { ("region_chest", 15f, 15f), ("cat_napas", 213f, 15f), ("region_belly", 213f, 213f), ("organ_ususbesar", 15f, 213f) };
            foreach (var (icon, x, y) in bubbles)
            {
                var b = U.Div().Size(52, 52).Radius(26).Bg("#FFFFFF").Center().Abs(x, y);
                b.Add(U.Icon(icon, 28));
                hero.Add(b);
            }
            hero.Add(U.Icon("sparkles", 280).Abs(0, 0));
            Root.Add(hero);

            var word = U.Row(12);
            word.style.marginTop = 28;
            var logo = U.Div().Size(48, 48).Radius(14).Bg("#FFFFFF").Center();
            logo.Add(U.Icon("logo", 30));
            word.Add(logo);
            word.Add(U.Text("Organo").FontSize(40).Bold().Fg("#FFFFFF"));
            Root.Add(word);
            var tag = U.Text("Kenali organ tubuhmu, yuk!").FontSize(15).Fg("#FFFFFF");
            tag.style.marginTop = 8;
            Root.Add(tag);
            Root.Add(U.Flex());

            var loader = U.Col(12).W(220).Align(Align.Center);
            var track = U.Div().H(8).Radius(999).Bg(U.C("#FFFFFF", 0.3f)).AlignSelf(Align.Stretch);
            track.style.overflow = Overflow.Hidden;
            m_Bar = U.Div().H(8).Radius(999).Bg("#FFFFFF").W(0);
            track.Add(m_Bar);
            loader.Add(track);
            m_Status = U.Text(Tips[0]).FontSize(13).Bold().Fg("#FFFFFF").MinH(20);
            loader.Add(m_Status);
            Root.Add(loader);
            var ver = U.Text("versi 1.0").FontSize(11).Fg(U.C("#FFFFFF", 0.8f));
            ver.style.marginTop = 24;
            Root.Add(ver);

            Root.RegisterCallback<PointerUpEvent>(_ => { if (m_Ready) Continue(); });
        }

        void Continue()
        {
            if (!m_Ready) return;
            m_Ready = false;
            if (P.loggedIn || P.guest) App.GoTab(NavTab.Beranda);
            else if (P.onboarded) App.Go<LoginScreen>(push: false);
            else App.Go<OnboardingScreen>(push: false);
        }

        public override void Tick(float dt)
        {
            m_T += dt;
            float p = Mathf.Clamp01(m_T / 2.8f);
            m_Bar.style.width = Length.Percent(p * 100);
            m_Mascot.style.translate = new Translate(0, -10 * Mathf.Abs(Mathf.Sin(m_T * Mathf.PI / 1.6f)));
            m_Orbit.style.rotate = new Rotate(m_T * 9f);
            if (p >= 1f && !m_Ready && m_Status.text != "✓ Siap! Ayo mulai")
            {
                m_Ready = true;
                m_Status.text = "✓ Siap! Ayo mulai";
                m_Status.style.opacity = 1;
            }
            else if (!m_Ready && p < 1f)
            {
                m_Status.text = Tips[Mathf.Min(2, Mathf.FloorToInt(p * 100 / 34))];
                m_Status.style.opacity = 0.7f + 0.3f * Mathf.Abs(Mathf.Sin(m_T * 2.2f));
            }
        }
    }

    // ==================================================================== Onboarding

    public class OnboardingScreen : AppScreen
    {
        int m_Page;
        Vector2 m_SwipeStart;

        static readonly (string tag, string color, string soft, string title, string desc, string art)[] Pages =
        {
            ("Jelajahi tubuh", "#12857A", "#D5F3EF", "Kenalan sama organ tubuhmu!", "Ketuk bagian tubuh, lalu lihat organnya dari dekat lengkap dengan suara penjelasan.", "onb_explore"),
            ("Scan & AR", "#6F52D1", "#E8E2FB", "Scan kartu, organ jadi 3D!", "Arahkan kamera ke kartu Organo dan lihat organ muncul langsung di depanmu.", "onb_scan"),
            ("Kuis seru", "#D6385E", "#FDE0E7", "Main kuis, kumpulkan bintang!", "Jawab kuis, scan organ yang diminta, lalu kumpulkan kartu dan lencana."
                , "onb_quiz"),
        };

        protected override void Build(object arg)
        {
            if (arg is int page) m_Page = page;
            var pg = Pages[m_Page];

            var top = U.Row().Justify(Justify.SpaceBetween).Pad(16, 20, 0, 20);
            var left = U.Div().W(64);
            if (m_Page > 0) { var b = U.IconButton("back", () => Go(m_Page - 1)); b.style.marginLeft = -12; left.Add(b); }
            top.Add(left);
            var brand = U.Row(6);
            brand.Add(U.Icon("logo", 22));
            brand.Add(U.Text("Organo").FontSize(17).Bold().Fg(U.TealDark));
            top.Add(brand);
            var skip = U.Div().W(64).Align(Align.FlexEnd).MinH(44).Justify(Justify.Center);
            skip.Add(U.Text("Lewati").FontSize(13).Fg(U.Muted));
            skip.Tap(Finish);
            top.Add(skip);
            Root.Add(top);

            var art = U.Div().Grow().Center();
            art.Add(U.Icon(pg.art, 340));
            Root.Add(art);

            var text = U.Col(10).Align(Align.Center).Pad(0, 28, 0, 28);
            text.Add(U.Pill(pg.tag, pg.soft, pg.color).AlignSelf(Align.Center));
            text.Add(U.Text(pg.title, "t-h1").FontSize(24).TextCenter());
            text.Add(U.Text(pg.desc, "t-body", "t-muted").TextCenter().W(310));
            Root.Add(text);

            var bottom = U.Col(20).Align(Align.Center).Pad(24, 20, 28, 20);
            var dots = U.Row(8);
            for (int i = 0; i < Pages.Length; i++)
            {
                int k = i;
                var d = U.Div().H(8).Radius(4).W(i == m_Page ? 28 : 8).Bg(i == m_Page ? pg.color : "#D9D7E3");
                d.Tap(() => Go(k));
                dots.Add(d);
            }
            bottom.Add(dots);
            var btn = m_Page < Pages.Length - 1
                ? U.Button("Lanjut", () => Go(m_Page + 1), "btn-primary")
                : U.Button("Mulai Sekarang", Finish, "btn-primary");
            btn.Add(U.Icon("arrow_right", 18));
            btn[1].style.marginLeft = 8;
            btn.AlignSelf(Align.Stretch).MinH(54);
            bottom.Add(btn);
            Root.Add(bottom);

            Root.RegisterCallback<PointerDownEvent>(e => m_SwipeStart = e.position);
            Root.RegisterCallback<PointerUpEvent>(e =>
            {
                float dx = e.position.x - m_SwipeStart.x;
                if (Mathf.Abs(dx) > 50) Go(m_Page + (dx < 0 ? 1 : -1));
            });
        }

        void Go(int page)
        {
            page = Mathf.Clamp(page, 0, Pages.Length - 1);
            if (page == m_Page) return;
            m_Page = page;
            Refresh(page);
        }

        void Finish()
        {
            P.onboarded = true;
            P.Save();
            App.Go<LoginScreen>(push: false);
        }

        public override bool OnBack()
        {
            if (m_Page == 0) return false;
            Go(m_Page - 1);
            return true;
        }
    }

    // ==================================================================== Login

    public class LoginScreen : AppScreen
    {
        protected override void Build(object arg)
        {
            var scroll = U.Scroll();
            Root.Add(Parts.TopBar("Masuk", () => App.Go<OnboardingScreen>(2, false)));
            Root.Add(scroll);
            var c = U.Col(12);
            scroll.Add(c);

            var hero = U.Col(6).Align(Align.Center);
            var mascot = U.Div().Size(132, 112).Align(Align.Center).Justify(Justify.FlexEnd);
            mascot.Add(U.Div().Size(112, 104).Radius(56).Bg(U.TealSoft).Abs(10, 4));
            mascot.Add(U.Icon("mascot_wave", 120, 108));
            hero.Add(mascot);
            hero.Add(U.Text("Halo, yuk masuk!", "t-h1").TextCenter());
            hero.Add(U.Text("Belajar organ tubuh lewat kartu, AR, dan kuis seru bareng Organo.", "t-small", "t-muted").TextCenter().W(280));
            c.Add(hero);
            c.Add(U.Space(8));

            c.Add(Parts.Field("Email", "mail", "nama@email.com", false, out var email));
            c.Add(Parts.Field("Password", "lock", "Masukkan password", true, out var pass));
            c.Add(U.Text("Lupa password?").FontSize(12).Bold().Fg(U.TealDark).AlignSelf(Align.FlexEnd).Tap(() => App.Go<ForgotPasswordScreen>()));

            c.Add(U.Button("Masuk", () =>
            {
                if (!string.IsNullOrWhiteSpace(email.value)) P.parentEmail = email.value.Trim();
                SignIn();
            }, "btn-primary"));

            var or = U.Row(12);
            or.Add(U.Div("divider")); or.Add(U.Text("atau", "t-caption")); or.Add(U.Div("divider"));
            c.Add(or);

            var google = U.Button("Masuk dengan Google", SignIn, "btn-outline");
            google.Insert(0, U.Icon("google", 20));
            google[1].style.marginLeft = 10;
            c.Add(google);

            var guest = U.Button("Masuk sebagai Tamu", ShowGuestSheet, "btn-soft");
            var gi = U.Icon("guest", 20); guest.Insert(0, gi); guest[1].style.marginLeft = 8;
            c.Add(guest);
            c.Add(Parts.InfoNote("Mode Tamu tidak menyimpan riwayat organ yang kamu buka, progres kuis, dan kartu yang terkumpul."));
            c.Add(U.Space(12));

            var footer = U.Row(0).Justify(Justify.Center);
            footer.Add(U.Text("Belum punya akun? ", "t-small", "t-muted"));
            footer.Add(U.Text("Daftar", "t-small").Bold().Fg(U.Pink).Tap(() => App.Go<RegisterScreen>()));
            c.Add(footer);
        }

        void SignIn()
        {
            P.loggedIn = true; P.guest = false; P.onboarded = true;
            P.Save();
            if (P.gender == Gender.None) App.Go<GenderScreen>(push: false);
            else App.GoTab(NavTab.Beranda);
        }

        void ShowGuestSheet()
        {
            var c = U.Col(14);
            var head = U.Col(4);
            head.Add(U.Text("Masuk sebagai Tamu?", "t-h2"));
            head.Add(U.Text("Kamu tetap bisa belajar, scan, dan main kuis. Bedanya ada di data yang disimpan:", "t-small", "t-muted"));
            c.Add(head);

            var table = U.Col().Border(1, U.Line).Radius(14);
            table.style.overflow = Overflow.Hidden;
            var hdr = U.Row().Bg("#F6F5F9").Pad(8, 12);
            hdr.Add(U.Text("Yang disimpan").FontSize(11).Bold().Fg(U.Muted).Grow());
            hdr.Add(U.Text("Akun").FontSize(11).Bold().Fg(U.Muted).W(64).TextCenter());
            hdr.Add(U.Text("Tamu").FontSize(11).Bold().Fg(U.Muted).W(64).TextCenter());
            table.Add(hdr);
            foreach (var label in new[] { "Riwayat organ yang sudah dibuka", "Progres kuis & bintang", "Koleksi kartu organ", "Lanjut belajar di HP lain" })
            {
                var row = U.Row().Pad(10, 12);
                row.style.borderTopWidth = 1; row.style.borderTopColor = U.C(U.Line);
                row.Add(U.Text(label, "t-small").Grow());
                var yes = U.Div().W(64).Center();
                yes.Add(U.Div().Size(22, 22).Radius(11).Bg(U.Teal).Center().Kids(U.Icon("check", 12)));
                row.Add(yes);
                var no = U.Div().W(64).Center();
                no.Add(U.Div().Size(22, 22).Radius(11).Border(1.5f, "#C9C6D8").Bg("#FFFFFF").Center().Kids(U.Icon("x_small", 10).Tinted(U.C(U.Faint))));
                row.Add(no);
                table.Add(row);
            }
            c.Add(table);

            OrganoApp.Overlay sheet = null;
            var actions = U.Col(8);
            actions.Add(U.Button("Lanjut sebagai Tamu", () =>
            {
                sheet.Close();
                P.StartGuest();
                App.Go<GenderScreen>(push: false);
            }, "btn-primary"));
            actions.Add(U.Button("Pakai akun saja", () => sheet.Close(), "btn-ghost"));
            c.Add(actions);
            sheet = App.ShowSheet(c);
        }
    }

    // ==================================================================== Forgot password

    public class ForgotPasswordScreen : AppScreen
    {
        float m_Left;
        Label m_Resend;
        VisualElement m_ResendBtn;

        protected override void Build(object arg)
        {
            bool sent = arg is bool b && b;
            Root.Add(Parts.TopBar("Lupa Password", App.Back));
            var c = U.Col(16).Pad(8, 20, 24, 20).Grow();
            Root.Add(c);
            if (!sent)
            {
                var hero = U.Col(8).Align(Align.Center);
                hero.Add(U.Icon("lock_big", 110));
                hero.Add(U.Text("Lupa password? Tenang!", "t-h1").TextCenter());
                hero.Add(U.Text("Masukkan email akunmu. Kami kirim link untuk membuat password baru.", "t-small", "t-muted").TextCenter());
                c.Add(hero);
                c.Add(Parts.Field("Email", "mail", "nama@email.com", false, out _, "Akunmu dibuat orang tua? Minta bantuan mereka untuk membuka email."));
                c.Add(U.Button("Kirim Link", () => { m_Left = 45; Refresh(true); }, "btn-primary"));
                c.Add(U.Flex());
                var f = U.Row().Justify(Justify.Center);
                f.Add(U.Text("Ingat password? ", "t-small", "t-muted"));
                f.Add(U.Text("Masuk", "t-small").Bold().Fg(U.Pink).Tap(App.Back));
                c.Add(f);
            }
            else
            {
                var hero = U.Col(8).Align(Align.Center);
                hero.style.paddingTop = 12;
                hero.Add(U.Icon("mail_sent", 130, 110));
                hero.Add(U.Text("Cek email kamu!", "t-h1").TextCenter());
                hero.Add(U.Text("Link untuk membuat password baru sudah dikirim ke " + Mask(P.parentEmail) + ". Cek juga folder spam, ya.", "t-small", "t-muted").TextCenter());
                c.Add(hero);
                c.Add(U.Button("Buka Aplikasi Email", () => Application.OpenURL("mailto:"), "btn-primary"));
                m_ResendBtn = U.Button("", () => { if (m_Left <= 0) { m_Left = 45; App.Toast("Link dikirim ulang"); } }, "btn-outline");
                m_Resend = (Label)m_ResendBtn[0];
                c.Add(m_ResendBtn);
                c.Add(U.Flex());
                c.Add(U.Text("Kembali ke Masuk").FontSize(13).Bold().Fg(U.TealDark).AlignSelf(Align.Center).Tap(App.Back));
            }
        }

        public static string Mask(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@")) return "di***@email.com";
            int at = email.IndexOf('@');
            return email.Substring(0, Mathf.Min(2, at)) + "***" + email.Substring(at);
        }

        public override void Tick(float dt)
        {
            if (m_Resend == null || m_Resend.panel == null) return;
            if (m_Left > 0) m_Left -= dt;
            bool waiting = m_Left > 0;
            m_Resend.text = waiting ? "Kirim ulang dalam 0:" + Mathf.CeilToInt(m_Left).ToString("00") : "Kirim ulang link";
            m_ResendBtn.Border(1, waiting ? "#E4E2EC" : U.Ink);
            m_Resend.style.color = U.C(waiting ? U.Faint : U.Ink);
        }
    }

    // ==================================================================== Register (2 steps)

    public class RegisterScreen : AppScreen
    {
        int m_Step = 1;
        string m_Age = "8-9";
        bool m_Parent, m_Agree, m_Report = true;
        string m_Name = "", m_Email = "";

        protected override void Build(object arg)
        {
            if (arg is int s) m_Step = s;
            Root.Add(Parts.TopBar(m_Step == 1 ? "Daftar" : "Izin Orang Tua", () => { if (m_Step == 2) Refresh(1); else App.Back(); },
                U.Text(m_Step + "/2", "t-small", "t-muted").Bold()));
            var bars = U.Row(6).Pad(10, 20, 0, 20);
            bars.Add(U.Div().H(6).Radius(3).Bg(U.Teal).Grow());
            bars.Add(U.Div().H(6).Radius(3).Bg(m_Step == 2 ? U.Teal : "#E4E2EC").Grow());
            Root.Add(bars);

            var scroll = U.Scroll();
            Root.Add(scroll);
            var c = U.Col(16);
            c.style.paddingTop = 16;
            scroll.Add(c);
            var bottom = U.Col(10).Pad(12, 20, 20, 20);
            Root.Add(bottom);

            if (m_Step == 1)
            {
                c.Add(U.Text("Buat akun Organo", "t-h1"));
                c.Add(U.Text("Dengan akun, riwayat belajar, bintang, dan kartumu tersimpan.", "t-small", "t-muted"));
                c.Add(Parts.Field("Nama panggilan", "guest", "Contoh: Dito", false, out var name, "Cukup nama panggilan, tidak perlu nama lengkap."));
                name.value = m_Name;
                name.RegisterValueChangedCallback(e => m_Name = e.newValue);

                var ageCol = U.Col(6);
                ageCol.Add(U.Text("Umur", "field-label").Bold());
                var ages = U.Row(8);
                foreach (var (id, label) in new[] { ("6-7", "6–7 th"), ("8-9", "8–9 th"), ("10-12", "10–12 th"), ("13+", "13+ th") })
                {
                    bool on = m_Age == id;
                    var a = U.Div().MinH(44).Radius(12).Grow().Center().Border(2, on ? U.Teal : U.Line).Bg(on ? "#F2FBF9" : "#FFFFFF");
                    var l = U.Text(label, "t-small"); if (on) l.Bold();
                    a.Add(l);
                    a.Tap(() => { m_Age = id; Refresh(1); });
                    ages.Add(a);
                }
                ageCol.Add(ages);
                c.Add(ageCol);

                c.Add(Parts.Field("Email orang tua / wali", "mail", "orangtua@email.com", false, out var mail, "Dipakai untuk masuk dan untuk izin orang tua."));
                mail.value = m_Email;
                mail.RegisterValueChangedCallback(e => m_Email = e.newValue);
                c.Add(Parts.Field("Buat password", "lock", "Minimal 8 karakter", true, out _));

                bottom.Add(U.Button("Lanjut", () => Refresh(2), "btn-primary"));
                var f = U.Row().Justify(Justify.Center);
                f.Add(U.Text("Sudah punya akun? ", "t-small", "t-muted"));
                f.Add(U.Text("Masuk", "t-small").Bold().Fg(U.Pink).Tap(App.Back));
                bottom.Add(f);
            }
            else
            {
                var hero = U.Row(12).Bg(U.YellowSoft).Radius(16).Pad(14);
                hero.Add(U.Icon("mascot_happy", 64, 52));
                var ht = U.Col(2).Grow();
                ht.Add(U.Text("Panggil orang tuamu, ya!", "t-title"));
                ht.Add(U.Text("Bagian ini perlu diisi oleh orang tua atau wali.", "t-caption"));
                hero.Add(ht);
                c.Add(hero);

                var data = U.Col(8, "card");
                data.Add(U.Text("Data yang kami simpan", "t-section"));
                foreach (var t in new[] { "Nama panggilan, umur, dan pilihan gender", "Progres kuis, bintang, dan kartu yang terkumpul", "Email orang tua untuk masuk dan pemberitahuan" })
                {
                    var r = U.Row(8).Align(Align.FlexStart);
                    r.Add(U.Text("•", "t-small").Fg(U.Teal).Bold());
                    r.Add(U.Text(t, "t-small").Grow());
                    data.Add(r);
                }
                var safe = U.Row(8).Bg(U.TealSoft).Radius(10).Pad(8, 10);
                safe.Add(U.Icon("shield", 16));
                safe.Add(U.Text("Tanpa iklan. Data tidak dijual atau dibagikan.", "t-caption").Fg(U.TealInk).Grow());
                data.Add(safe);
                c.Add(data);

                c.Add(CheckRow("Saya orang tua / wali anak ini", "Wajib", m_Parent, () => { m_Parent = !m_Parent; Refresh(2); }));
                c.Add(CheckRow("Saya menyetujui Kebijakan Privasi & Syarat Penggunaan", "Wajib", m_Agree, () => { m_Agree = !m_Agree; Refresh(2); }));
                c.Add(CheckRow("Kirim laporan belajar mingguan ke email saya", "Opsional", m_Report, () => { m_Report = !m_Report; Refresh(2); }));
                c.Add(U.Text("Baca Kebijakan Privasi & Syarat Penggunaan").FontSize(13).Bold().Fg(U.TealDark).Tap(ShowPolicy));

                bool can = m_Parent && m_Agree;
                var create = U.Button(can ? "Buat Akun" : "Centang 2 persetujuan dulu", () =>
                {
                    if (!can) return;
                    P.loggedIn = true; P.guest = false; P.onboarded = true;
                    if (!string.IsNullOrWhiteSpace(m_Name)) P.nickname = m_Name.Trim();
                    if (!string.IsNullOrWhiteSpace(m_Email)) P.parentEmail = m_Email.Trim();
                    P.ageRange = m_Age;
                    P.Save();
                    App.Go<GenderScreen>(push: false);
                }, can ? "btn-primary" : "btn-disabled");
                bottom.Add(create);
            }
        }

        static VisualElement CheckRow(string label, string hint, bool on, System.Action toggle)
        {
            var r = U.Row(12, "card").Border(1, on ? U.Teal : U.Line);
            r.Add(Parts.Checkbox(on));
            var t = U.Col(2).Grow();
            t.Add(U.Text(label, "t-small").Bold());
            t.Add(U.Text(hint, "t-tiny"));
            r.Add(t);
            r.Tap(toggle);
            return r;
        }

        void ShowPolicy()
        {
            var c = U.Col(10);
            c.Add(U.Text("Ringkasan Kebijakan Privasi", "t-h2"));
            void Item(string bold, string rest)
            {
                var r = U.Row(6).Align(Align.FlexStart);
                r.Add(U.Text("•").Fg(U.Teal).Bold());
                r.Add(U.Text("<b>" + bold + "</b>" + rest, "t-small").Grow());
                c.Add(r);
            }
            Item("Untuk apa datanya?", " Hanya untuk menyimpan progres belajar anak dan mengirim info ke orang tua.");
            Item("Kamera:", " gambar kamera hanya dipakai saat scan dan tidak disimpan.");
            Item("Hak orang tua:", " bisa melihat, mengubah, atau menghapus data anak kapan saja lewat email.");
            Item("Tanpa iklan", " dan tanpa menjual data.");
            c.Add(U.Text("[Teks kebijakan lengkap disiapkan bersama tim hukum.]", "t-caption"));
            OrganoApp.Overlay sh = null;
            c.Add(U.Button("Mengerti", () => sh.Close(), "btn-primary"));
            sh = App.ShowSheet(c);
        }
    }

    // ==================================================================== Gender

    public class GenderScreen : AppScreen
    {
        Gender m_Pick;

        protected override void Build(object arg)
        {
            if (arg is Gender g) m_Pick = g;
            else if (arg == null) m_Pick = P.gender;
            bool fromProfile = App.Current is ProfileScreen || arg is string;
            Root.Add(Parts.TopBar("Pilih Gender", App.Back));
            var scroll = U.Scroll();
            Root.Add(scroll);
            var c = U.Col(16);
            c.style.paddingTop = 8;
            scroll.Add(c);
            c.Add(U.Text("Kamu laki-laki atau perempuan?", "t-h1"));
            c.Add(U.Text("Pilih sesuai dirimu, ya! Materi tentang tubuh akan disesuaikan dengan pilihanmu.", "t-small", "t-muted"));

            var cards = U.Row(12);
            cards.Add(Choice(Gender.Boy, "gender_boy", "Laki-laki"));
            cards.Add(Choice(Gender.Girl, "gender_girl", "Perempuan"));
            c.Add(cards);

            var what = U.Col(10, "card");
            what.Add(U.Text("Yang disesuaikan untukmu", "t-section"));
            foreach (var t in new[] { "Tokoh tubuh di Library", "Materi perubahan tubuh saat tumbuh besar", "Contoh cerita di kuis" })
            {
                var r = U.Row(8);
                r.Add(U.Div().Size(18, 18).Radius(9).Bg(U.Teal).Center().Kids(U.Icon("check", 10)));
                r.Add(U.Text(t, "t-small").Grow());
                what.Add(r);
            }
            c.Add(what);
            string other = m_Pick == Gender.Girl ? "laki-laki" : m_Pick == Gender.Boy ? "perempuan" : null;
            c.Add(Parts.InfoNote(other != null
                ? "Materi tubuh " + other + " tetap bisa kamu pelajari kapan saja lewat tombol tubuh di Library."
                : "Apa pun pilihanmu, materi tubuh laki-laki dan perempuan tetap bisa dipelajari dari Library."));

            var bottom = U.Col(8).Pad(12, 20, 20, 20);
            bool picked = m_Pick != Gender.None;
            bottom.Add(U.Button(picked ? "Lanjut ke Beranda" : "Pilih salah satu dulu", () =>
            {
                if (!picked) return;
                P.gender = m_Pick;
                P.Save();
                App.GoTab(NavTab.Beranda);
            }, picked ? "btn-primary" : "btn-disabled"));
            bottom.Add(U.Text("Bisa diubah kapan saja di Profil.", "t-caption").TextCenter());
            Root.Add(bottom);
        }

        VisualElement Choice(Gender g, string art, string label)
        {
            bool on = m_Pick == g;
            var card = U.Col(12).Grow().MinH(200).Radius(20).Center().Pad(16, 12).Border(2, on ? U.Teal : U.Line).Bg(on ? "#F2FBF9" : "#FFFFFF");
            card.Add(U.Icon(art, 120));
            card.Add(U.Text(label, "t-title"));
            if (on)
            {
                var tick = U.Div().Size(26, 26).Radius(13).Bg(U.Teal).Center().Abs(null, 10, 10);
                tick.Add(U.Icon("check", 14));
                card.Add(tick);
            }
            card.Tap(() => { m_Pick = g; Refresh(g); });
            return card;
        }
    }
}
