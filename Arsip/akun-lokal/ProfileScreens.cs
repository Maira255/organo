using System.Collections.Generic;
using System.Linq;
using Organo.Data;
using Organo.UI;
using UnityEngine;
using UnityEngine.UIElements;
using U = Organo.UI.UI;

namespace Organo.Screens
{
    public class Badge
    {
        public string id, name, desc, icon, color;
        public int progress, goal;
        public string cta;
        public bool Earned => progress >= goal;

        public static List<Badge> All(PlayerProgress p) => new List<Badge>
        {
            new Badge { id = "first", name = "Langkah Pertama", desc = "Menyelesaikan kuis pertama", icon = "badge_star", color = U.Teal, progress = Mathf.Min(1, p.history.Count + p.QuizzesCompleted), goal = 1, cta = "Mulai Kuis" },
            new Badge { id = "champ", name = "Juara Kuis", desc = "Semua soal benar dalam satu kuis", icon = "badge_trophy", color = U.Pink, progress = p.history.Any(h => h.total > 0 && h.correct == h.total) ? 1 : 0, goal = 1, cta = "Mulai Kuis" },
            new Badge { id = "explorer", name = "Penjelajah Organ", desc = "Membuka 5 organ di Library", icon = "badge_medal", color = U.Purple, progress = Mathf.Min(5, p.openedOrgans.Count), goal = 5, cta = "Buka Library" },
            new Badge { id = "stars", name = "Bintang Bersinar", desc = "Mengumpulkan 200 bintang", icon = "badge_star", color = U.Yellow, progress = Mathf.Min(200, p.stars), goal = 200, cta = "Kumpulkan Bintang" },
            new Badge { id = "digest", name = "Ahli Tubuh", desc = "Menyelesaikan semua season kuis", icon = "badge_trophy", color = U.TealDark, progress = p.SeasonsDone, goal = QuizBank.Seasons.Count, cta = "Lanjut Kuis" },
        };

        /// <summary>Lencana: lingkaran 52-56px (daftar) atau 116px dengan border putih 5px (tampilan besar).</summary>
        public VisualElement Medal(float size)
        {
            var wrap = U.Div().Size(size, size);
            if (size >= 100 && Earned)
            {
                var glow = U.Div().Abs(-24, -24, -24, -24);
                glow.style.backgroundImage = new StyleBackground(GradientTexture.Radial(U.C(color, 0.45f)));
                glow.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
                glow.pickingMode = PickingMode.Ignore;
                wrap.Add(glow);
            }
            var m = U.Div().Size(size, size).Radius(size / 2).Center().Bg(Earned ? color : "#E4E2EC");
            if (size >= 100) m.Border(5, "#FFFFFF");
            var ic = U.Icon(icon, size * 0.5f);
            ic.Tint = Earned ? Color.white : U.C("#BDBACE");
            m.Add(ic);
            wrap.Add(m);
            if (!Earned) wrap.Add(Parts.LockBadge(Mathf.Clamp(size * 0.38f, 20f, 34f)).Abs(null, null, -2, -2));
            return wrap;
        }
    }

    // ==================================================================== Profil

    public class ProfileScreen : AppScreen
    {
        public override NavTab Tab => NavTab.Profil;

        protected override void Build(object arg)
        {
            Root.Add(Parts.TopBar("Profil", () => App.GoTab(NavTab.Beranda)));
            var scroll = U.Scroll();
            Root.Add(scroll);
            var c = U.Col(12);
            scroll.Add(c);

            var head = U.Col(5).Align(Align.Center).Pad(8, 0);
            head.Add(Parts.Avatar(P.gender, 84));
            head.Add(U.Text(P.nickname, "t-h1"));
            if (P.guest)
            {
                head.Add(U.Pill("Mode Tamu", "#EDEBF3", "#4A4669").AlignSelf(Align.Center));
                var cta = U.Col(8).AlignSelf(Align.Stretch).Bg(U.TealSoft).Radius(14).Pad(12, 14).Align(Align.Center);
                cta.Add(U.Text("Daftar biar progress gak hilang!", "t-body").Bold().Fg(U.TealInk));
                cta.Add(U.Button("Daftar Sekarang", () => App.Go<RegisterScreen>(), "btn-primary", "btn-small").AlignSelf(Align.Stretch));
                head.Add(cta);
            }
            else
            {
                head.Add(U.Pill("Akun Siswa", U.TealSoft, U.TealDark).AlignSelf(Align.Center));
                var acc = AccountStore.Current;
                if (acc != null) head.Add(U.Text("@" + acc.username, "t-tiny").TextCenter());
            }
            // ganti profil: pilih profil lain yang tersimpan di HP ini / tambah profil baru
            var others = AccountStore.Accounts.Count(a => AccountStore.Current == null || a.id != AccountStore.Current.id);
            var sw = U.Row(8).AlignSelf(Align.Center).Pad(8, 16).Radius(999).Bg("#FFFFFF").Border(1.5f, U.Line);
            sw.style.marginTop = 4;
            sw.Add(U.Icon("retry", 16).Tinted(U.C(U.TealDark)));
            sw.Add(U.Text("Ganti Profil").FontSize(13).Bold().Fg(U.TealDark));
            if (others > 0) sw.Add(U.Div().Size(20, 20).Radius(10).Bg(U.TealSoft).Center().Kids(U.Text(others.ToString()).FontSize(11).Bold().Fg(U.TealDark)));
            sw.tooltip = "Pindah ke profil lain di HP ini";
            sw.Tap(ShowProfiles);
            head.Add(sw);
            c.Add(head);

            var stats = U.Row(10);
            stats.Add(Parts.StatCard("stat_star", P.stars.ToString(), "Bintang"));
            stats.Add(Parts.StatCard("stat_cards", P.cards.Count + "/" + Db.Count, "Kartu", () => App.Go<CardCollectionScreen>()));
            stats.Add(Parts.StatCard("stat_quiz", P.QuizzesCompleted.ToString(), "Quiz Selesai", () => App.Go<QuizHistoryScreen>()));
            c.Add(stats);

            var badges = Badge.All(P);
            var bsec = U.Col(8);
            bsec.Add(Parts.SectionHeader("Lencana", "Lihat semua", () => App.Go<BadgesScreen>()));
            var rail = U.Scroll(true);
            rail.style.flexShrink = 0;
            rail.contentContainer.style.flexDirection = FlexDirection.Row;
            rail.contentContainer.style.paddingLeft = 0; rail.contentContainer.style.paddingRight = 0; rail.contentContainer.style.paddingBottom = 0;
            foreach (var b in badges)
            {
                var item = U.Col(6).W(84).Align(Align.Center);
                item.style.marginRight = 12;
                item.Add(b.Medal(56));
                item.Add(U.Text(b.name).FontSize(10).TextCenter().Fg(b.Earned ? U.Ink : U.Faint));
                var bb = b;
                item.Tap(() => App.Go<BadgesScreen>(bb.id));
                rail.Add(item);
            }
            bsec.Add(rail);
            c.Add(bsec);

            var list = U.Col(0, "card", "card-lg").Pad(4, 14);
            list.Elev(Elevation.E1);
            if (!P.guest) list.Add(Setting("lock", "Kelola Akun", "Cadangan & keamanan", () => App.Go<AccountManageScreen>()));
            list.Add(Setting("set_history", "Riwayat Quiz", P.guest ? "Sesi ini" : P.history.Count + " kuis", () => App.Go<QuizHistoryScreen>()));
            list.Add(Setting("set_body", "Materi Tubuh", P.gender == Gender.Girl ? "Perempuan" : "Laki-laki", () => App.Go<BodyMaterialScreen>()));
            list.Add(SoundRow());
            list.Add(NarrationRow());
            list.Add(Setting("set_eye", "Mode Aksesibilitas", null, () => App.Go<AccessibilityScreen>()));
            list.Add(Setting("set_help", "Bantuan", null, ShowHelp, true));
            c.Add(list);

            var logout = U.Button(P.guest ? "Keluar dari Mode Tamu" : "Keluar dari Akun", ConfirmLogout, "btn-outline");
            logout.Border(1.5f, U.Pink);
            if (logout.Q<Label>() is Label ll) { ll.style.color = U.C(U.PinkDark); ll.style.unityFontStyleAndWeight = FontStyle.Bold; }
            logout.tooltip = "Keluar dari akun ini";
            c.Add(logout);
            c.Add(U.Space(8));
        }

        /// <summary>Lembar "Ganti Profil": semua profil di HP ini, ketuk salah satu lalu masukkan password-nya.</summary>
        public void ShowProfiles()
        {
            var current = AccountStore.Current;
            var c = U.Col(12);
            c.Add(U.Text("Ganti Profil", "t-h2"));

            OrganoApp.Overlay sh = null;
            var list = U.Col(8);
            foreach (var acc in AccountStore.Accounts)
            {
                bool me = current != null && acc.id == current.id;
                var prog = me ? P : AccountStore.LoadProgress(acc);
                var r = U.Row(12).Pad(10, 12).Radius(14).Bg(me ? "#F2FBF9" : "#FFFFFF").Border(me ? 2 : 1, me ? U.Teal : U.Line);
                r.Add(Parts.Avatar((Gender)acc.gender, 44));
                var t = U.Col(2).Grow();
                t.Add(U.Text(acc.nickname, "t-body").Bold());
                t.Add(U.Text("@" + acc.username + " · " + prog.stars + " bintang · " + prog.cards.Count + " kartu", "t-caption"));
                r.Add(t);
                if (me) r.Add(U.Pill("Dipakai", U.Teal, "#FFFFFF"));
                else
                {
                    r.Add(U.Icon("chevron_right", 18));
                    var target = acc;
                    r.Tap(() => { sh.Close(); ShowSwitch(target); });
                    r.tooltip = "Masuk sebagai " + acc.nickname;
                }
                list.Add(r);
            }
            if (AccountStore.Accounts.Count == 0)
                list.Add(Parts.InfoNote("Belum ada profil tersimpan di HP ini. Daftar supaya progresmu tersimpan."));
            c.Add(list);

            var add = U.Row(12).Pad(10, 12).Radius(14).Bg("#FFFFFF");
            add.Add(new DashedOutline(U.C("#CFCCDD"), 1.5f, 14f));
            add.Add(U.Div().Size(44, 44).Radius(22).Bg(U.TealSoft).Center().Kids(U.Text("+").FontSize(24).Bold().Fg(U.TealDark)));
            var at = U.Col(2).Grow();
            at.Add(U.Text("Tambah profil baru", "t-body").Bold());
            if (P.guest) at.Add(U.Text("Progres Mode Tamu ikut tersimpan", "t-caption"));
            add.Add(at);
            add.Tap(() => { sh.Close(); App.Go<RegisterScreen>(1); });
            c.Add(add);

            c.Add(U.Button("Tutup", () => sh.Close(), "btn-ghost"));
            sh = App.ShowSheet(c);
        }

        /// <summary>Masuk ke profil lain dengan password-nya (tanpa keluar dulu).</summary>
        public void ShowSwitch(LocalAccount acc)
        {
            var c = U.Col(12);
            var head = U.Col(6).Align(Align.Center);
            head.Add(Parts.Avatar((Gender)acc.gender, 64));
            head.Add(U.Text("Masuk sebagai " + acc.nickname, "t-h2").TextCenter());
            head.Add(U.Text("@" + acc.username, "t-caption").TextCenter());
            c.Add(head);
            if (P.guest)
                c.Add(Parts.InfoNote("Progres Mode Tamu (" + P.stars + " bintang, " + P.cards.Count + " kartu) tidak disimpan dan akan hilang.", "alert"));
            c.Add(Parts.Field("Password " + acc.nickname, "lock", "Masukkan password", true, out var pass));
            var error = AccountUi.Error();
            c.Add(error);
            OrganoApp.Overlay sh = null;
            c.Add(U.Button("Masuk", () =>
            {
                var err = AccountUi.SwitchTo(App, acc.username, pass.value);
                if (err != null) { AccountUi.Show(error, err); OrganoAudio.Wrong(); return; }
                sh.Close();
                OrganoAudio.Success();
                App.Toast("Halo, " + acc.nickname + "!", 2f);
                if (App.Progress.gender == Gender.None) App.Go<GenderScreen>(push: false);
                else App.GoTab(NavTab.Beranda);
            }, "btn-primary"));
            var links = U.Row(0).Justify(Justify.SpaceBetween);
            links.Add(U.Text("Pilih profil lain").FontSize(13).Bold().Fg(U.TealDark).Pad(6, 4).Tap(() => { sh.Close(); ShowProfiles(); }));
            links.Add(U.Text("Lupa password?").FontSize(13).Bold().Fg(U.TealDark).Pad(6, 4).Tap(() => { sh.Close(); App.Go<ForgotPasswordScreen>(acc.username); }));
            c.Add(links);
            sh = App.ShowSheet(c);
        }

        /// <summary>Konfirmasi keluar. Akun: progres tetap tersimpan. Tamu: progres hilang, ditawari daftar dulu.</summary>
        public void ConfirmLogout()
        {
            var c = U.Col(12);
            OrganoApp.Overlay sh = null;
            if (P.guest)
            {
                c.Add(U.Text("Keluar dari Mode Tamu?", "t-h2"));
                var d = U.Text("Progres Mode Tamu (" + P.stars + " bintang, " + P.cards.Count + " kartu) tidak disimpan dan akan hilang. Daftar dulu supaya semuanya tersimpan.", "t-small", "t-muted");
                d.style.whiteSpace = WhiteSpace.Normal;
                c.Add(d);
                c.Add(U.Button("Daftar biar tersimpan", () => { sh.Close(); App.Go<RegisterScreen>(1); }, "btn-primary"));
                var leave = U.Button("Tetap Keluar", () => { sh.Close(); AccountUi.LogOut(App); }, "btn-ghost");
                if (leave.Q<Label>() is Label ll) ll.style.color = U.C(U.PinkDark);
                c.Add(leave);
            }
            else
            {
                c.Add(U.Text("Keluar dari akun " + P.nickname + "?", "t-h2"));
                var d = U.Text("Bintang, kartu, dan progres belajarmu tetap tersimpan di HP ini. Masuk lagi kapan saja, atau pilih profil lain di halaman Masuk.", "t-small", "t-muted");
                d.style.whiteSpace = WhiteSpace.Normal;
                c.Add(d);
                c.Add(U.Button("Ya, Keluar", () => { sh.Close(); AccountUi.LogOut(App); }, "btn-primary"));
                c.Add(U.Button("Ganti ke profil lain saja", () => { sh.Close(); ShowProfiles(); }, "btn-outline"));
            }
            c.Add(U.Button("Batal", () => sh.Close(), "btn-ghost"));
            sh = App.ShowSheet(c);
        }

        /// <summary>Bantuan: cara scan, asal kartu, dan versi aplikasi (dulu hanya toast).</summary>
        void ShowHelp()
        {
            var c = U.Col(12);
            c.Add(U.Text("Bantuan", "t-h2"));
            var tips = U.Col(8);
            for (int i = 0; i < ScanScreen.ScanTips.Length; i++)
            {
                var r = U.Row(10).Align(Align.FlexStart);
                r.Add(U.Div().Size(22, 22).Radius(11).Bg(U.TealSoft).Center().NoShrink().Kids(U.Text((i + 1).ToString()).FontSize(11).Bold().Fg(U.TealDark)));
                var t = U.Text(ScanScreen.ScanTips[i], "t-small").Fg(U.Ink).Grow();
                t.style.whiteSpace = WhiteSpace.Normal;
                r.Add(t);
                tips.Add(r);
            }
            c.Add(U.Text("Supaya kartu cepat dikenali", "t-section"));
            c.Add(tips);
            OrganoApp.Overlay sh = null;
            c.Add(U.Button("Kartunya dari mana?", () => { sh.Close(); CardSource.Show(App); }, "btn-outline"));
            c.Add(U.Text("Organo " + Application.version, "t-caption").TextCenter());
            c.Add(U.Button("Tutup", () => sh.Close(), "btn-ghost"));
            sh = App.ShowSheet(c);
        }

        VisualElement Stat(string icon, string tint, string value, string label, System.Action tap)
        {
            var s = U.Col(4, "card").Grow().MinH(84).Align(Align.Center).Justify(Justify.Center).Pad(8, 4);
            var ic = U.Icon(icon, 22);
            if (tint != null) ic.Tint = U.C(tint);
            s.Add(ic);
            s.Add(U.Text(value, "t-h2"));
            s.Add(U.Text(label, "t-tiny").TextCenter());
            if (tap != null) s.Tap(tap);
            return s;
        }

        static VisualElement Setting(string icon, string label, string value, System.Action tap, bool last = false)
        {
            var r = U.Row(14).MinH(48);
            if (!last) { r.style.borderBottomWidth = 1; r.style.borderBottomColor = U.C(U.Line); }
            r.Add(U.Icon(icon, 22));
            r.Add(U.Text(label, "t-body").Grow());
            if (value != null) r.Add(U.Text(value, "t-small", "t-muted"));
            r.Add(U.Icon("chevron_right", 18));
            r.Tap(tap);
            return r;
        }

        VisualElement SoundRow()
        {
            var r = U.Row(14).MinH(48);
            r.style.borderBottomWidth = 1; r.style.borderBottomColor = U.C(U.Line);
            r.Add(U.Icon("set_sound", 22));
            r.Add(U.Text("Suara & Musik", "t-body").Grow());
            r.Add(Parts.Switch(P.sound, v => { P.sound = v; OrganoAudio.Enabled = v; P.Save(); }, "Suara & Musik"));
            return r;
        }

        VisualElement NarrationRow()
        {
            var r = U.Row(14).MinH(48);
            r.style.borderBottomWidth = 1; r.style.borderBottomColor = U.C(U.Line);
            r.Add(U.Icon("set_voice", 22));
            r.Add(U.Text("Suara Pembaca", "t-body").Grow());
            r.Add(Parts.Switch(P.narration, v => { P.narration = v; OrganoAudio.NarrationEnabled = v; P.Save(); }, "Suara Pembaca"));
            return r;
        }
    }

    // ==================================================================== Kelola akun lokal

    public class AccountManageScreen : AppScreen
    {
        protected override void Build(object arg)
        {
            var acc = AccountStore.Current;
            Root.Add(Parts.TopBar("Kelola Akun", App.Back));
            var scroll = U.Scroll();
            Root.Add(scroll);
            var c = U.Col(14);
            c.style.paddingTop = 8;
            scroll.Add(c);
            if (acc == null)
            {
                c.Add(Parts.InfoNote("Kamu sedang di Mode Tamu. Daftar dulu supaya punya akun."));
                c.Add(U.Button("Daftar Sekarang", () => App.Go<RegisterScreen>(1), "btn-primary"));
                return;
            }

            var head = U.Row(12, "card");
            head.Add(Parts.Avatar(P.gender, 56));
            var ht = U.Col(2).Grow();
            ht.Add(U.Text(acc.nickname, "t-title"));
            ht.Add(U.Text("@" + acc.username + " · dibuat " + acc.created, "t-caption"));
            head.Add(ht);
            c.Add(head);

            // kode cadangan
            var backup = U.Col(8, "card").Bg("#F2FBF9").Border(1, "#9CE6DC");
            backup.Add(U.Text("Kode Cadangan", "t-section"));
            var bt = U.Text("Akunmu hanya tersimpan di HP ini. Simpan kode cadangan (misalnya kirim ke chat orang tua) supaya progres bisa dipulihkan di HP lain lewat tombol \"Pulihkan dari Kode Cadangan\" di halaman Masuk.", "t-small");
            bt.style.whiteSpace = WhiteSpace.Normal;
            backup.Add(bt);
            backup.Add(U.Button("Salin Kode Cadangan", () =>
            {
                var code = AccountStore.ExportBackup(acc);
                GUIUtility.systemCopyBuffer = code;
                ShowCode(code);
            }, "btn-primary", "btn-small"));
            c.Add(backup);

            var list = U.Col(0, "card", "card-lg").Pad(4, 14);
            list.Add(Row("lock", "Ganti Password", ChangePassword));
            list.Add(Row("parent", "Hapus Akun (perlu PIN orang tua)", DeleteAccount, true));
            c.Add(list);
            c.Add(Parts.InfoNote("Lupa password? Di halaman Masuk ketuk \"Lupa password?\" lalu minta orang tuamu memasukkan PIN."));
        }

        static VisualElement Row(string icon, string label, System.Action tap, bool last = false)
        {
            var r = U.Row(14).MinH(52);
            if (!last) { r.style.borderBottomWidth = 1; r.style.borderBottomColor = U.C(U.Line); }
            r.Add(U.Icon(icon, 22));
            r.Add(U.Text(label, "t-body").Grow());
            r.Add(U.Icon("chevron_right", 18));
            r.Tap(tap);
            return r;
        }

        void ShowCode(string code)
        {
            var c = U.Col(10);
            c.Add(U.Text("Kode cadangan disalin!", "t-h2"));
            c.Add(U.Text("Tempel kode ini di chat atau catatan orang tua. Siapa pun yang punya kode ini bisa memulihkan akunmu, jadi jangan dibagikan ke orang lain, ya.", "t-small", "t-muted"));
            var tf = new TextField { multiline = true, isReadOnly = true, value = code };
            tf.AddToClassList("field");
            tf.style.maxHeight = 140;
            tf.style.whiteSpace = WhiteSpace.Normal;
            c.Add(tf);
            OrganoApp.Overlay sh = null;
            c.Add(U.Button("Selesai", () => sh.Close(), "btn-primary"));
            sh = App.ShowSheet(c);
        }

        void ChangePassword()
        {
            var acc = AccountStore.Current;
            var c = U.Col(10);
            c.Add(U.Text("Ganti Password", "t-h2"));
            c.Add(Parts.Field("Password lama", "lock", "Password sekarang", true, out var old));
            c.Add(Parts.Field("Password baru", "lock", "Minimal 6 karakter", true, out var pass));
            c.Add(Parts.Field("Ulangi password baru", "lock", "Tulis lagi", true, out var pass2));
            var error = AccountUi.Error();
            c.Add(error);
            OrganoApp.Overlay sh = null;
            c.Add(U.Button("Simpan", () =>
            {
                var err = AccountStore.ChangePassword(acc, old.value, pass.value, pass2.value);
                if (err != null) { AccountUi.Show(error, err); OrganoAudio.Wrong(); return; }
                sh.Close();
                App.Toast("Password baru disimpan", 2f);
            }, "btn-primary"));
            sh = App.ShowSheet(c);
        }

        void DeleteAccount()
        {
            var acc = AccountStore.Current;
            var c = U.Col(10);
            c.Add(U.Text("Hapus akun " + acc.nickname + "?", "t-h2"));
            c.Add(U.Text("Bintang, kartu, dan riwayat belajar akun ini akan dihapus dari HP ini dan tidak bisa dikembalikan (kecuali kamu punya kode cadangan).", "t-small", "t-muted"));
            c.Add(Parts.Field("PIN orang tua", "parent", "••••", true, out var pin));
            pin.maxLength = 4;
            var error = AccountUi.Error();
            c.Add(error);
            OrganoApp.Overlay sh = null;
            var del = U.Button("Hapus Akun", () =>
            {
                var err = AccountStore.Delete(acc, pin.value);
                if (err != null) { AccountUi.Show(error, err); OrganoAudio.Wrong(); return; }
                sh.Close();
                var fresh = PlayerProgress.Fresh();
                fresh.onboarded = true;
                App.UseProgress(fresh);
                App.ClearHistory();
                App.Toast("Akun dihapus dari HP ini", 2.2f);
                App.Go<LoginScreen>(push: false);
            }, "btn-primary");
            del.Bg(U.PinkDark);
            c.Add(del);
            c.Add(U.Button("Batal", () => sh.Close(), "btn-ghost"));
            sh = App.ShowSheet(c);
        }
    }

    /// <summary>Cincin progres (ringkasan koleksi).</summary>
    public class ProgressRing : VisualElement
    {
        readonly float m_T;
        readonly Color m_Fill, m_Track;

        public ProgressRing(float t, float size, Color fill, Color track, string big, string small, Color? text = null)
        {
            m_T = Mathf.Clamp01(t); m_Fill = fill; m_Track = track;
            style.width = size; style.height = size; style.flexShrink = 0;
            style.alignItems = Align.Center; style.justifyContent = Justify.Center;
            generateVisualContent += Draw;
            var b = U.Text(big).FontSize(18).Bold();
            var sm = U.Text(small).FontSize(10);
            if (text.HasValue) { b.style.color = text.Value; sm.style.color = text.Value; }
            Add(b);
            Add(sm);
        }

        void Draw(MeshGenerationContext mgc)
        {
            var r = contentRect;
            var p = mgc.painter2D;
            var c = r.center;
            float rad = Mathf.Min(r.width, r.height) / 2 - 5;
            p.lineWidth = 8;
            p.lineCap = LineCap.Round;
            p.strokeColor = m_Track;
            p.BeginPath(); p.Arc(c, rad, 0, 360); p.Stroke();
            if (m_T <= 0) return;
            p.strokeColor = m_Fill;
            p.BeginPath(); p.Arc(c, rad, -90, -90 + 360 * m_T); p.Stroke();
        }
    }

    // ==================================================================== Lencana

    public class BadgesScreen : AppScreen
    {
        string m_Sel;

        protected override void Build(object arg)
        {
            var all = Badge.All(P);
            if (arg is string id) m_Sel = id;
            var sel = all.FirstOrDefault(b => b.id == m_Sel) ?? all[0];
            Root.Add(Parts.TopBar("Lencana", App.Back, U.Text(all.Count(b => b.Earned) + "/" + all.Count, "t-small", "t-muted").Bold()));
            var scroll = U.Scroll();
            Root.Add(scroll);
            var c = U.Col(16);
            scroll.Add(c);

            var show = U.Col(10, "card", "card-lg").Align(Align.Center).Pad(20, 16);
            show.Add(U.Pill(sel.Earned ? "Didapat" : "Terkunci", sel.Earned ? U.TealSoft : "#F6F5F9", sel.Earned ? U.TealDark : U.Muted).AlignSelf(Align.Center));
            show.Add(sel.Medal(116));
            show.Add(U.Text(sel.name, "t-h1").TextCenter());
            show.Add(U.Text(sel.desc, "t-small", "t-muted").TextCenter());
            if (!sel.Earned)
            {
                var pr = U.Col(6).AlignSelf(Align.Stretch);
                var row = U.Row().Justify(Justify.SpaceBetween);
                row.Add(U.Text("Progres", "t-caption").Bold());
                row.Add(U.Text(sel.progress + "/" + sel.goal, "t-caption").Bold());
                pr.Add(row);
                pr.Add(U.Progress((float)sel.progress / sel.goal, sel.color, "#F0EEF5", 8));
                show.Add(pr);
                show.Add(U.Button(sel.cta, () =>
                {
                    if (sel.id == "explorer") App.GoTab(NavTab.Library);
                    else App.GoTab(NavTab.Quiz);
                }, "btn-primary").AlignSelf(Align.Stretch));
            }
            c.Add(show);

            c.Add(U.Text("Semua lencana", "t-section"));
            var grid = U.Row().Justify(Justify.SpaceBetween);
            grid.style.flexWrap = Wrap.Wrap;
            foreach (var b in all)
            {
                bool on = b.id == sel.id;
                var item = U.Col(6).W(104).Pad(10, 4).Radius(14).Align(Align.Center).Bg(on ? "#F3EFFD" : "#FFFFFF").Border(on ? 2 : 1, on ? U.Purple : U.Line);
                item.style.marginBottom = 12;
                item.Add(b.Medal(52));
                item.Add(U.Text(b.name).FontSize(11).TextCenter());
                var bid = b.id;
                item.Tap(() => Refresh(bid));
                grid.Add(item);
            }
            c.Add(grid);
        }
    }

    // ==================================================================== Aksesibilitas

    public class AccessibilityScreen : AppScreen
    {
        int m_Size = -1, m_Speed = -1;
        bool m_Captions, m_Contrast, m_Calm, m_Loaded;

        protected override void Build(object arg)
        {
            if (!m_Loaded)
            {
                m_Loaded = true;
                m_Size = P.textSize; m_Speed = P.voiceSpeed;
                m_Captions = P.captions; m_Contrast = P.highContrast; m_Calm = P.reduceMotion;
            }
            Root.Add(Parts.TopBar("Aksesibilitas", () => { m_Loaded = false; App.Back(); }));
            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 8;
            Root.Add(scroll);
            var c = U.Col(18);
            scroll.Add(c);
            float[] sizes = { 13, 15, 18 };
            float fs = sizes[m_Size];

            // Contoh tampilan langsung
            var prev = U.Col(6).Pad(16).Radius(16);
            if (m_Contrast) prev.Bg("#1B1830").Border(2, "#FFE27A"); else prev.Bg("#FFFBF2").Border(1, "#BDEBE5");
            string ink = m_Contrast ? "#FFFFFF" : U.Ink;
            var pr = U.Row(12);
            pr.Add(Parts.OrganBadge(Db.Get("lambung"), 56));
            var pt = U.Col(2).Grow();
            pt.Add(U.Text("Lambung").FontSize(fs + 5).Bold().Fg(ink));
            // contoh kalimat yang sedang dibacakan disorot (sama seperti di halaman organ)
            var sample = U.Text(m_Captions && !m_Contrast
                ? "<mark=#1AB8A62E><color=#0F6B61>Lambung adalah kantong lentur</color></mark> yang mengaduk makanan."
                : "Lambung adalah kantong lentur yang mengaduk makanan.").FontSize(fs).Fg(ink);
            sample.style.whiteSpace = WhiteSpace.Normal;
            pt.Add(sample);
            pr.Add(pt);
            prev.Add(pr);
            c.Add(prev);

            // Ukuran teks: 3 kartu "Aa"
            var sizeCol = U.Col(8);
            sizeCol.Add(U.Text("Ukuran teks", "t-section"));
            var sizeRow = U.Row(10);
            string[] sizeNames = { "Kecil", "Sedang", "Besar" };
            float[] aa = { 14, 18, 24 };
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                bool on = m_Size == i;
                var b = U.Col(2).Grow().MinH(72).Radius(14).Center().Border(2, on ? U.Teal : U.Line).Bg(on ? "#F2FBF9" : "#FFFFFF");
                b.Add(U.Text("Aa").FontSize(aa[i]).Bold());
                b.Add(U.Text(sizeNames[i], "t-caption"));
                b.Tap(() => { m_Size = k; Refresh(); });
                sizeRow.Add(b);
            }
            sizeCol.Add(sizeRow);
            c.Add(sizeCol);

            // Kecepatan suara: segmen pill
            var spCol = U.Col(8);
            spCol.Add(U.Text("Kecepatan suara penjelasan", "t-section"));
            var seg = U.Row(4).Pad(4).Radius(999).Bg("#EDEBF3");
            string[] speeds = { "Lambat", "Normal", "Cepat" };
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                bool on = m_Speed == i;
                var b = U.Div().Grow().MinH(40).Radius(999).Center().Bg(on ? "#FFFFFF" : "#00000000");
                if (on) b.Elev(Elevation.E1);
                b.Add(U.Text(speeds[i], "t-small").Bold().Fg(on ? U.Ink : U.Muted));
                b.Tap(() => { m_Speed = k; Refresh(); });
                seg.Add(b);
            }
            spCol.Add(seg);
            c.Add(spCol);

            // Suara pembaca: pilih suara yang paling enak didengar dari suara Bahasa Indonesia di HP ini
            var voCol = U.Col(8);
            voCol.Add(U.Text("Suara pembaca", "t-section"));
            OrganoSpeech.EnsureVoices();
            if (OrganoSpeech.Voices.Count == 0)
            {
                string note = Application.isEditor ? "Pilihan suara muncul saat aplikasi dibuka di HP."
                    : OrganoSpeech.Status == OrganoSpeech.State.MissingLanguage ? "Pasang suara Bahasa Indonesia di Pengaturan HP > Text-to-speech (pilih Google)."
                    : "Suara pembaca dipilih otomatis: suara Bahasa Indonesia terbaik di HP ini.";
                var nt = U.Text(note, "t-caption");
                nt.style.whiteSpace = WhiteSpace.Normal;
                voCol.Add(nt);
            }
            else
            {
                var list = U.Col(0, "card").Pad(2, 14);
                list.Elev(Elevation.E1);
                for (int i = 0; i < OrganoSpeech.Voices.Count; i++)
                {
                    var v = OrganoSpeech.Voices[i];
                    bool on = OrganoSpeech.CurrentVoice == v.id;
                    var r = U.Row(12).MinH(52).Pad(6, 0);
                    if (i < OrganoSpeech.Voices.Count - 1) { r.style.borderBottomWidth = 1; r.style.borderBottomColor = U.C(U.Line); }
                    var dot = U.Div().Size(22, 22).Radius(11).Border(2, on ? U.Teal : "#CFCCDD").Center();
                    if (on) dot.Add(U.Div().Size(10, 10).Radius(5).Bg(U.Teal));
                    r.Add(dot);
                    var lbl = U.Text(v.label, "t-body").Grow();
                    if (on) lbl.Bold();
                    r.Add(lbl);
                    var tryIt = U.Row(4).Pad(6, 12).Radius(999).Bg(U.TealSoft);
                    tryIt.Add(U.Icon("set_sound", 14).Tinted(U.C(U.TealInk)));
                    tryIt.Add(U.Text("Coba").FontSize(12).Bold().Fg(U.TealInk));
                    r.Add(tryIt);
                    string id = v.id;
                    r.Tap(() =>
                    {
                        OrganoSpeech.SetVoice(id);
                        OrganoAudio.SpeakText("Halo! Aku akan membacakan penjelasan organ tubuh untukmu.", true);
                        Refresh();
                    });
                    list.Add(r);
                }
                voCol.Add(list);
            }
            c.Add(voCol);

            // Switch
            var sw = U.Col(0, "card", "card-lg").Pad(4, 14);
            sw.Elev(Elevation.E1);
            sw.Add(SwitchRow("Sorot kalimat yang dibacakan", null, m_Captions, v => { m_Captions = v; Refresh(); }, false));
            sw.Add(SwitchRow("Kontras tinggi", null, m_Contrast, v => { m_Contrast = v; Refresh(); }, false));
            sw.Add(SwitchRow("Kurangi animasi", null, m_Calm, v => { m_Calm = v; Refresh(); }, true));
            c.Add(sw);

            var bottom = U.Div().Pad(12, 20, 20, 20);
            bottom.Add(U.Button("Simpan", () =>
            {
                P.textSize = m_Size; P.voiceSpeed = m_Speed;
                P.captions = m_Captions; P.highContrast = m_Contrast; P.reduceMotion = m_Calm;
                P.Save();
                OrganoAudio.SetVoiceSpeed(m_Speed);
                App.ApplyFontScale();
                m_Loaded = false;
                App.Toast("Pengaturan disimpan");
                App.Back();
            }, "btn-primary"));
            Root.Add(bottom);
        }

        static VisualElement SwitchRow(string title, string hint, bool on, System.Action<bool> changed, bool last)
        {
            var r = U.Row(12).MinH(56).Pad(10, 0);
            if (!last) { r.style.borderBottomWidth = 1; r.style.borderBottomColor = U.C(U.Line); }
            var t = U.Col(2).Grow();
            t.Add(U.Text(title, "t-body").Bold());
            if (!string.IsNullOrEmpty(hint)) t.Add(U.Text(hint, "t-caption"));
            r.Add(t);
            r.Add(Parts.Switch(on, changed, title));
            return r;
        }
    }
}
