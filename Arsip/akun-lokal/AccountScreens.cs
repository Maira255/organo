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
        SvgImage m_ReadyIcon;
        float m_T;
        bool m_Ready;
        static readonly string[] Tips = { "Menyiapkan organ…", "Membangunkan maskot…", "Hampir siap!" };
        /// <summary>Lama bilah loading berjalan sampai penuh ("Siap! Ayo mulai"), dalam detik.</summary>
        public const float LoadDuration = 3.5f;
        /// <summary>Jeda setelah "Siap!" sebelum otomatis pindah ke onboarding / beranda.</summary>
        public const float AutoContinueDelay = 0.7f;

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
            var status = U.Row(6).MinH(20);
            m_ReadyIcon = U.Icon("check", 16);
            m_ReadyIcon.Visible(false);
            status.Add(m_ReadyIcon);
            m_Status = U.Text(Tips[0]).FontSize(13).Bold().Fg("#FFFFFF");
            status.Add(m_Status);
            loader.Add(status);
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
            float p = Mathf.Clamp01(m_T / LoadDuration);
            m_Bar.style.width = Length.Percent(p * 100);
            m_Mascot.style.translate = new Translate(0, -10 * Mathf.Abs(Mathf.Sin(m_T * Mathf.PI / 1.6f)));
            m_Orbit.style.rotate = new Rotate(m_T * 9f);
            if (p >= 1f && !m_Ready && m_Status.text != "Siap! Ayo mulai")
            {
                m_Ready = true;
                m_Status.text = "Siap! Ayo mulai";
                m_ReadyIcon.Visible(true);
                m_Status.style.opacity = 1;
            }
            // lanjut sendiri setelah jeda singkat (tidak perlu diketuk; ketukan hanya mempercepat)
            if (m_Ready && m_T >= LoadDuration + AutoContinueDelay) Continue();
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
            AccountStore.Onboarded = true;
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

    // ==================================================================== Login (akun lokal)

    public class LoginScreen : AppScreen
    {
        string m_User = "";

        protected override void Build(object arg)
        {
            if (arg is string u) m_User = u;
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
            c.Add(hero);

            // profil yang sudah ada di HP ini: ketuk untuk mengisi nama pengguna
            TextField userField = null, passField = null;
            if (AccountStore.Accounts.Count > 0)
            {
                var pick = U.Col(8);
                pick.Add(U.Text("Pilih profilmu", "t-section"));
                var rail = U.Scroll(true);
                rail.style.flexGrow = 0; rail.style.flexShrink = 0;
                rail.style.marginLeft = -20; rail.style.marginRight = -20;
                rail.contentContainer.style.flexDirection = FlexDirection.Row;
                rail.contentContainer.style.paddingLeft = 20;
                foreach (var acc in AccountStore.Accounts)
                {
                    bool on = AccountStore.Normalize(m_User) == acc.username;
                    var chip = U.Col(4).W(84).Pad(8, 4).Radius(16).Align(Align.Center).Bg(on ? "#F2FBF9" : "#FFFFFF").Border(on ? 2 : 1, on ? U.Teal : U.Line);
                    chip.style.marginRight = 10;
                    chip.Add(Parts.Avatar((Gender)acc.gender, 48));
                    chip.Add(U.Text(acc.nickname).FontSize(12).Bold().TextCenter());
                    chip.Add(U.Text("@" + acc.username).FontSize(10).Fg(U.Muted).TextCenter());
                    var name = acc.username;
                    chip.Tap(() => { m_User = name; Refresh(name); });
                    rail.Add(chip);
                }
                var add = U.Col(4).W(84).Pad(8, 4).Radius(16).Align(Align.Center).Bg("#FFFFFF");
                add.Add(new DashedOutline(U.C("#CFCCDD"), 1.5f, 16f));
                add.Add(U.Div().Size(48, 48).Radius(24).Bg(U.TealSoft).Center().Kids(U.Text("+").FontSize(26).Bold().Fg(U.TealDark)));
                add.Add(U.Text("Profil baru").FontSize(12).Bold().TextCenter());
                add.Add(U.Text("Daftar").FontSize(10).Fg(U.Muted).TextCenter());
                add.Tap(() => App.Go<RegisterScreen>(1));
                rail.Add(add);
                pick.Add(rail);
                c.Add(pick);
            }

            c.Add(Parts.Field("Nama pengguna", "guest", "Contoh: dito.keren", false, out userField));
            userField.value = m_User;
            userField.RegisterValueChangedCallback(e => m_User = e.newValue);
            c.Add(Parts.Field("Password", "lock", "Masukkan password", true, out passField));
            var error = AccountUi.Error();
            c.Add(error);
            c.Add(U.Text("Lupa password?").FontSize(12).Bold().Fg(U.TealDark).AlignSelf(Align.FlexEnd).Tap(() => App.Go<ForgotPasswordScreen>(m_User)));

            c.Add(U.Button("Masuk", () =>
            {
                var (acc, err) = AccountStore.SignIn(userField.value, passField.value);
                if (err != null) { AccountUi.Show(error, err); OrganoAudio.Wrong(); return; }
                App.UseProgress(AccountStore.LoadProgress(acc));
                App.ClearHistory();
                if (P.gender == Gender.None) App.Go<GenderScreen>(push: false);
                else App.GoTab(NavTab.Beranda);
            }, "btn-primary"));

            var or = U.Row(12);
            or.Add(U.Div("divider")); or.Add(U.Text("atau", "t-caption")); or.Add(U.Div("divider"));
            c.Add(or);

            var guest = U.Button("Masuk sebagai Tamu", ShowGuestSheet, "btn-soft");
            var gi = U.Icon("guest", 20); guest.Insert(0, gi); guest[1].style.marginLeft = 8;
            c.Add(guest);
            var restore = U.Button("Pulihkan dari Kode Cadangan", () => AccountUi.ShowRestore(App, acc => { m_User = acc.username; Refresh(acc.username); }), "btn-outline");
            c.Add(restore);
            c.Add(U.Space(12));

            var footer = U.Row(0).Justify(Justify.Center);
            footer.Add(U.Text("Belum punya akun?", "t-small", "t-muted").Pad(0, 4, 0, 0));
            footer.Add(U.Text("Daftar", "t-small").Bold().Fg(U.Pink).Tap(() => App.Go<RegisterScreen>(1)));
            c.Add(footer);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            {
                // khusus pengembang (tidak ada di build rilis): buat 3 akun uji dari OrganoTestAccounts.json
                var dev = U.Text("Muat akun uji (mode pengembang)").FontSize(12).Bold().Fg(U.Muted).AlignSelf(Align.Center).Pad(10, 12);
                dev.Tap(() =>
                {
                    int n = AccountStore.SeedTestAccounts();
                    App.Toast(n + " akun uji siap. Lihat README untuk nama pengguna & password-nya.", 3f);
                    Refresh(m_User);
                });
                c.Add(dev);
            }
#endif
        }

        void ShowGuestSheet()
        {
            var c = U.Col(14);
            var head = U.Col(4);
            head.Add(U.Text("Masuk sebagai Tamu?", "t-h2"));
            var note = U.Text("Bintang, kartu, dan progres kuis tidak disimpan.", "t-small", "t-muted");
            note.style.whiteSpace = WhiteSpace.Normal;
            head.Add(note);
            c.Add(head);

            OrganoApp.Overlay sheet = null;
            var actions = U.Col(8);
            actions.Add(U.Button("Lanjut sebagai Tamu", () =>
            {
                sheet.Close();
                AccountStore.SignOut();
                var g = PlayerProgress.Fresh();
                g.StartGuest();
                g.onboarded = true;
                App.UseProgress(g);
                App.Go<GenderScreen>(push: false);
            }, "btn-primary"));
            actions.Add(U.Button("Pakai akun saja", () => sheet.Close(), "btn-ghost"));
            c.Add(actions);
            sheet = App.ShowSheet(c);
        }
    }

    /// <summary>Potongan UI bersama untuk layar akun.</summary>
    public static class AccountUi
    {
        public static Label Error()
        {
            var l = U.Text("", "t-small").Fg(U.PinkDark);
            l.style.whiteSpace = WhiteSpace.Normal;
            l.Visible(false);
            return l;
        }

        public static void Show(Label error, string message)
        {
            error.text = message ?? "";
            error.Visible(!string.IsNullOrEmpty(message));
        }

        /// <summary>
        /// Pindah ke profil lain di HP ini (perlu password profil tujuan). Progres profil sekarang disimpan dulu,
        /// jadi tidak ada yang hilang. Mengembalikan pesan kesalahan, atau null bila berhasil.
        /// </summary>
        public static string SwitchTo(OrganoApp app, string username, string password)
        {
            var p = app.Progress;
            if (p != null && !p.guest && p.loggedIn && AccountStore.Current != null) p.Save();
            var (acc, err) = AccountStore.SignIn(username, password);
            if (err != null) return err;
            app.UseProgress(AccountStore.LoadProgress(acc));
            app.ClearHistory();
            return null;
        }

        /// <summary>Keluar dari akun / Mode Tamu lalu buka halaman Masuk (progres akun tetap tersimpan di HP ini).</summary>
        public static void LogOut(OrganoApp app)
        {
            string user = AccountStore.Current?.username;
            bool guest = app.Progress.guest;
            app.Progress.LogOut();
            app.ClearHistory();
            app.Toast(guest ? "Kamu keluar dari Mode Tamu." : "Progresmu tetap tersimpan di HP ini. Sampai jumpa!", 2.4f);
            app.Go<LoginScreen>(user ?? "", false);
        }

        /// <summary>Lembar "pulihkan dari kode cadangan": tempel kode, akun & progres dipindahkan ke HP ini.</summary>
        public static void ShowRestore(OrganoApp app, System.Action<LocalAccount> done)
        {
            var c = U.Col(10);
            c.Add(U.Text("Pulihkan dari Kode Cadangan", "t-h2"));
            c.Add(U.Text("Buka Profil > Kelola Akun > Salin Kode Cadangan di HP lama, kirim kodenya ke HP ini (misalnya lewat chat orang tua), lalu tempel di sini.", "t-small", "t-muted"));
            var tf = new TextField { multiline = true };
            tf.AddToClassList("field");
            tf.style.minHeight = 96;
            tf.style.whiteSpace = WhiteSpace.Normal;
            tf.textEdition.placeholder = "ORGANO1:...";
            c.Add(tf);
            var paste = U.Text("Tempel dari papan klip").FontSize(12).Bold().Fg(U.TealDark).Tap(() => tf.value = GUIUtility.systemCopyBuffer);
            c.Add(paste);
            var error = Error();
            c.Add(error);
            OrganoApp.Overlay sh = null;
            c.Add(U.Button("Pulihkan", () =>
            {
                var (acc, err) = AccountStore.ImportBackup(tf.value);
                if (err != null) { Show(error, err); return; }
                sh.Close();
                app.Toast("Akun @" + acc.username + " dipulihkan. Masuk dengan password lamamu.", 3f);
                done?.Invoke(acc);
            }, "btn-primary"));
            c.Add(U.Button("Batal", () => sh.Close(), "btn-ghost"));
            sh = app.ShowSheet(c);
        }
    }

    // ==================================================================== Lupa password (PIN orang tua)

    public class ForgotPasswordScreen : AppScreen
    {
        protected override void Build(object arg)
        {
            string user = arg as string ?? "";
            Root.Add(Parts.TopBar("Lupa Password", App.Back));
            var scroll = U.Scroll();
            Root.Add(scroll);
            var c = U.Col(14);
            c.style.paddingTop = 8;
            scroll.Add(c);

            var hero = U.Col(8).Align(Align.Center);
            hero.Add(U.Icon("lock_big", 100));
            hero.Add(U.Text("Lupa password? Tenang!", "t-h1").TextCenter());
            hero.Add(U.Text("Minta orang tuamu memasukkan PIN orang tua yang dibuat saat mendaftar, lalu buat password baru.", "t-small", "t-muted").TextCenter());
            c.Add(hero);

            c.Add(Parts.Field("Nama pengguna", "guest", "Contoh: dito.keren", false, out var userField));
            userField.value = user;
            c.Add(Parts.Field("PIN orang tua (4 angka)", "parent", "••••", true, out var pinField));
            pinField.maxLength = 4;
            c.Add(Parts.Field("Password baru", "lock", "Minimal 6 karakter", true, out var passField));
            c.Add(Parts.Field("Ulangi password baru", "lock", "Tulis lagi password barunya", true, out var confirmField));
            var error = AccountUi.Error();
            c.Add(error);
            c.Add(U.Button("Simpan Password Baru", () =>
            {
                var acc = AccountStore.Find(userField.value);
                if (acc == null) { AccountUi.Show(error, "Nama pengguna belum terdaftar di HP ini."); return; }
                var err = AccountStore.ResetPassword(acc, pinField.value, passField.value, confirmField.value);
                if (err != null) { AccountUi.Show(error, err); OrganoAudio.Wrong(); return; }
                OrganoAudio.Success();
                App.Toast("Password baru disimpan. Silakan masuk, ya!", 2.6f);
                App.Go<LoginScreen>(acc.username, false);
            }, "btn-primary"));
            c.Add(Parts.InfoNote("Lupa PIN orang tua juga? Akun bisa dipulihkan dari kode cadangan, atau dihapus lalu dibuat ulang."));
        }
    }

    // ==================================================================== Daftar (2 langkah)

    public class RegisterScreen : AppScreen
    {
        int m_Step = 1;
        string m_Age = "8-9";
        bool m_Parent, m_Agree;
        string m_Name = "", m_User = "", m_Pass = "", m_Pass2 = "", m_Pin = "", m_Pin2 = "";
        string m_Error;

        protected override void Build(object arg)
        {
            if (arg is int s) { m_Step = s; if (s == 1 && App.Current != this) m_Error = null; }
            Root.Add(Parts.TopBar(m_Step == 1 ? "Daftar" : "Izin Orang Tua", () => { if (m_Step == 2) { m_Error = null; Refresh(1); } else App.Back(); },
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
            var error = AccountUi.Error();

            if (m_Step == 1)
            {
                c.Add(U.Text("Buat akun Organo", "t-h1"));
                if (P.guest) c.Add(Parts.InfoNote("Progres Mode Tamu yang sedang berjalan akan dipindahkan ke akun baru ini."));
                else if (AccountStore.HasLegacyProgress) c.Add(Parts.InfoNote("Progres dari versi Organo sebelumnya ditemukan dan akan dipindahkan ke akun baru ini."));
                c.Add(Parts.Field("Nama panggilan", "guest", "Contoh: Dito", false, out var name));
                name.value = m_Name;
                name.RegisterValueChangedCallback(e => m_Name = e.newValue);
                c.Add(Parts.Field("Nama pengguna (untuk masuk)", "guest", "Contoh: dito.keren", false, out var user));
                user.value = m_User;
                user.RegisterValueChangedCallback(e => m_User = e.newValue);

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

                c.Add(Parts.Field("Buat password", "lock", "Minimal 6 karakter", true, out var pass));
                pass.value = m_Pass;
                pass.RegisterValueChangedCallback(e => m_Pass = e.newValue);
                c.Add(Parts.Field("Ulangi password", "lock", "Tulis lagi password-nya", true, out var pass2));
                pass2.value = m_Pass2;
                pass2.RegisterValueChangedCallback(e => m_Pass2 = e.newValue);
                c.Add(error);
                AccountUi.Show(error, m_Error);

                bottom.Add(U.Button("Lanjut", () =>
                {
                    m_Error = string.IsNullOrWhiteSpace(m_Name) ? "Tulis nama panggilanmu dulu."
                        : AccountStore.ValidateUsername(m_User) ?? AccountStore.ValidatePassword(m_Pass, m_Pass2);
                    if (m_Error != null) { AccountUi.Show(error, m_Error); OrganoAudio.Wrong(); return; }
                    Refresh(2);
                }, "btn-primary"));
                var f = U.Row().Justify(Justify.Center);
                f.Add(U.Text("Sudah punya akun?", "t-small", "t-muted").Pad(0, 4, 0, 0));
                f.Add(U.Text("Masuk", "t-small").Bold().Fg(U.Pink).Tap(() => App.Go<LoginScreen>(push: false)));
                bottom.Add(f);
            }
            else
            {
                var hero = U.Row(12).Bg(U.YellowSoft).Radius(16).Pad(14);
                hero.Add(U.Icon("parent", 56));
                var ht = U.Col(2).Grow();
                ht.Add(U.Text("Panggil orang tuamu, ya!", "t-title"));
                ht.Add(U.Text("Bagian ini perlu diisi oleh orang tua atau wali.", "t-caption"));
                hero.Add(ht);
                c.Add(hero);

                var data = U.Col(8, "card");
                data.Add(U.Text("Data yang disimpan", "t-section"));
                foreach (var t in new[] { "Nama panggilan, nama pengguna, umur, dan pilihan gender", "Progres kuis, bintang, dan kartu yang terkumpul", "Password & PIN orang tua (diacak, bukan teks asli)" })
                {
                    var r = U.Row(8).Align(Align.FlexStart);
                    r.Add(U.Text("•", "t-small").Fg(U.Teal).Bold());
                    r.Add(U.Text(t, "t-small").Grow());
                    data.Add(r);
                }
                var safe = U.Row(8).Bg(U.TealSoft).Radius(10).Pad(8, 10);
                safe.Add(U.Icon("check_teal", 14));
                safe.Add(U.Text("Semua data hanya disimpan di HP ini. Tanpa iklan, tidak dikirim ke internet.", "t-caption").Fg(U.TealInk).Grow());
                data.Add(safe);
                c.Add(data);

                c.Add(Parts.Field("PIN orang tua (4 angka)", "parent", "••••", true, out var pin, "Dipakai untuk mengatur ulang password anak dan menghapus akun."));
                pin.maxLength = 4;
                pin.value = m_Pin;
                pin.RegisterValueChangedCallback(e => m_Pin = e.newValue);
                c.Add(Parts.Field("Ulangi PIN", "parent", "••••", true, out var pin2));
                pin2.maxLength = 4;
                pin2.value = m_Pin2;
                pin2.RegisterValueChangedCallback(e => m_Pin2 = e.newValue);

                c.Add(CheckRow("Saya orang tua / wali anak ini", "Wajib", m_Parent, () => { m_Parent = !m_Parent; Refresh(2); }));
                c.Add(CheckRow("Saya menyetujui Kebijakan Privasi & Syarat Penggunaan", "Wajib", m_Agree, () => { m_Agree = !m_Agree; Refresh(2); }));
                c.Add(U.Text("Baca Kebijakan Privasi & Syarat Penggunaan").FontSize(13).Bold().Fg(U.TealDark).Tap(ShowPolicy));
                c.Add(error);
                AccountUi.Show(error, m_Error);

                bool can = m_Parent && m_Agree;
                var create = U.Button(can ? "Buat Akun" : "Centang 2 persetujuan dulu", () =>
                {
                    if (!can) return;
                    m_Error = AccountStore.ValidatePin(m_Pin, m_Pin2) ?? AccountStore.ValidateUsername(m_User);
                    if (m_Error != null) { AccountUi.Show(error, m_Error); OrganoAudio.Wrong(); return; }
                    var acc = AccountStore.Create(m_User, m_Name, m_Age, m_Pass, m_Pin, P);
                    App.UseProgress(AccountStore.LoadProgress(acc));
                    m_Pass = m_Pass2 = m_Pin = m_Pin2 = "";
                    m_Error = null;
                    App.ClearHistory();
                    OrganoAudio.Success();
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
            Item("Untuk apa datanya?", " Hanya untuk menyimpan progres belajar anak di HP ini.");
            Item("Tidak dikirim ke mana pun:", " Organo tidak punya server; data tidak meninggalkan HP kecuali orang tua menyalin kode cadangan.");
            Item("Kamera:", " gambar kamera hanya dipakai saat scan dan tidak disimpan.");
            Item("Hak orang tua:", " bisa melihat progres, mengatur ulang password, dan menghapus akun beserta datanya (Profil > Kelola Akun, dengan PIN).");
            Item("Tanpa iklan", " dan tanpa menjual data.");
            OrganoApp.Overlay sh = null;
            c.Add(U.Button("Mengerti", () => sh.Close(), "btn-primary"));
            sh = App.ShowSheet(c);
        }
    }

    // ==================================================================== Gender

    public class GenderScreen : AppScreen
    {
        Gender m_Pick;
        bool m_FromProfile;

        protected override void Build(object arg)
        {
            if (arg is Gender g) m_Pick = g;
            else if (arg == null) m_Pick = P.gender;
            // dibuka dari Profil / Materi Tubuh: setelah memilih, kembali ke halaman itu (bukan ke Beranda)
            if (arg is string) m_FromProfile = true;
            else if (arg == null) m_FromProfile = false;
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
            what.Elev(Elevation.E1);
            what.Add(U.Text("Yang disesuaikan untukmu", "t-section"));
            foreach (var (t, ic) in new[] { ("Tokoh tubuh di Library", "g_person"), ("Materi perubahan tubuh saat tumbuh besar", "g_book"), ("Contoh cerita di kuis", "g_chat") })
            {
                var r = U.Row(8);
                r.Add(U.Icon(ic, 18));
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
            bool back = m_FromProfile;
            bottom.Add(U.Button(!picked ? "Pilih salah satu dulu" : back ? "Simpan" : "Lanjut ke Beranda", () =>
            {
                if (!picked) return;
                P.gender = m_Pick;
                P.Save();
                if (back) App.Back(); else App.GoTab(NavTab.Beranda);
            }, picked ? "btn-primary" : "btn-disabled"));
            bottom.Add(U.Text("Bisa diubah kapan saja di Profil.", "t-caption").TextCenter());
            Root.Add(bottom);
        }

        VisualElement Choice(Gender g, string art, string label)
        {
            bool on = m_Pick == g;
            var card = U.Col(10).Grow().MinH(200).Radius(20).Center().Pad(16, 12).Border(2, on ? U.Teal : U.Line).Bg(on ? "#F2FBF9" : "#FFFFFF");
            // gambar di kotaknya sendiri (dipotong rapi), tulisan di bawahnya dengan jarak — tidak menumpuk
            var pic = U.Div().Size(120, 128).NoShrink();
            pic.style.overflow = Overflow.Hidden;
            pic.Add(U.Icon(art, 120, 153));
            card.Add(pic);
            var name = U.Text(label, "t-title").TextCenter();
            name.style.marginTop = 4;
            card.Add(name);
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
