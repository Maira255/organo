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
            new Badge { id = "first", name = "Langkah Pertama", desc = "Menyelesaikan kuis pertama", icon = "badge_trophy", color = U.Teal, progress = Mathf.Min(1, p.history.Count + p.QuizzesCompleted), goal = 1, cta = "Mulai Kuis" },
            new Badge { id = "champ", name = "Juara Kuis", desc = "Semua soal benar dalam satu kuis", icon = "star", color = U.Pink, progress = p.history.Any(h => h.correct == h.total) || p.QuizzesCompleted >= 3 ? 1 : 0, goal = 1, cta = "Mulai Kuis" },
            new Badge { id = "explorer", name = "Penjelajah Organ", desc = "Membuka 5 organ di Library", icon = "logo", color = U.Purple, progress = Mathf.Min(5, p.openedOrgans.Count), goal = 5, cta = "Buka Library" },
            new Badge { id = "stars", name = "Bintang Bersinar", desc = "Mengumpulkan 200 bintang", icon = "star", color = U.Yellow, progress = Mathf.Min(200, p.stars), goal = 200, cta = "Kumpulkan Bintang" },
            new Badge { id = "digest", name = "Ahli Pencernaan", desc = "Menyelesaikan semua kuis Season 1", icon = "cat_cerna", color = U.TealDark, progress = p.season1Done, goal = 5, cta = "Lanjut Kuis" },
        };

        public VisualElement Medal(float size)
        {
            var m = U.Div().Size(size, size).Radius(size / 2).Center().Bg(Earned ? color : "#ECEAF2");
            var ic = U.Icon(icon, size * 0.5f);
            ic.Tint = Earned ? Color.white : U.C("#BDBACE");
            m.Add(ic);
            if (!Earned)
            {
                var l = U.Div().Size(size * 0.36f, size * 0.36f).Radius(size * 0.18f).Bg("#FFFFFF").Center().Abs(null, null, -2, -2);
                l.Add(U.Icon("lock", size * 0.22f).Tinted(U.C(U.Faint)));
                m.Add(l);
            }
            return m;
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
                head.Add(U.Pill("Mode Tamu", U.YellowSoft, "#7A5A00").AlignSelf(Align.Center));
                var cta = U.Col(8).AlignSelf(Align.Stretch).Bg(U.TealSoft).Radius(14).Pad(12, 14).Align(Align.Center);
                cta.Add(U.Text("Daftar biar progress gak hilang!", "t-body").Bold().Fg(U.TealInk));
                cta.Add(U.Button("Daftar Sekarang", () => App.Go<RegisterScreen>(), "btn-primary", "btn-small").AlignSelf(Align.Stretch));
                head.Add(cta);
            }
            else
            {
                head.Add(U.Pill("Akun Siswa", U.TealSoft, U.TealDark).AlignSelf(Align.Center));
                head.Add(U.Text("Terhubung ke email orang tua · " + ForgotPasswordScreen.Mask(P.parentEmail), "t-tiny").TextCenter());
            }
            c.Add(head);

            var stats = U.Row(10);
            stats.Add(Stat("star", U.Yellow, P.stars.ToString(), "Bintang", null));
            stats.Add(Stat("cards_ok", null, P.cards.Count + "/" + Db.Count, "Kartu", () => App.Go<CardCollectionScreen>()));
            stats.Add(Stat("badge_trophy", U.Pink, P.QuizzesCompleted.ToString(), "Quiz Selesai", () => App.Go<QuizHistoryScreen>()));
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
            list.Add(Setting("rotate", "Riwayat Quiz", P.guest ? "Sesi ini" : P.history.Count + " kuis", () => App.Go<QuizHistoryScreen>()));
            list.Add(Setting(P.gender == Gender.Girl ? "kid_girl" : "kid_boy", "Materi Tubuh", P.gender == Gender.Girl ? "Perempuan" : "Laki-laki", () => App.Go<BodyMaterialScreen>()));
            list.Add(Setting("info", "Bahasa", "Indonesia", () => App.Toast("Bahasa lain segera hadir")));
            list.Add(SoundRow());
            list.Add(Setting("eye", "Mode Aksesibilitas", null, () => App.Go<AccessibilityScreen>()));
            list.Add(Setting("help", "Bantuan & Tentang", null, () => App.Toast("Organo v1.0 · Belajar organ tubuh lewat kartu, AR, dan kuis"), true));
            c.Add(list);

            var logout = U.Text(P.guest ? "Keluar dari Mode Tamu" : "Keluar").FontSize(14).Bold().Fg(U.Pink).AlignSelf(Align.Center).Pad(12, 16);
            logout.Tap(() =>
            {
                P.LogOut();
                App.ClearHistory();
                App.Go<LoginScreen>(push: false);
            });
            c.Add(logout);
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
            r.Add(U.Icon(icon, 20).Tinted(U.C(U.Faint)));
            r.Add(U.Text(label, "t-small").Bold().Grow());
            if (value != null) r.Add(U.Text(value, "t-caption"));
            r.Add(U.Icon("arrow_right", 14).Tinted(U.C("#BDBACE")));
            r.Tap(tap);
            return r;
        }

        VisualElement SoundRow()
        {
            var r = U.Row(14).MinH(48);
            r.style.borderBottomWidth = 1; r.style.borderBottomColor = U.C(U.Line);
            r.Add(U.Icon("nav_quiz", 20).Tinted(U.C(U.Faint)));
            r.Add(U.Text("Suara & Musik", "t-small").Bold().Grow());
            bool on = P.sound;
            var sw = U.Div().Size(44, 26).Radius(13).Bg(on ? U.Teal : "#C9C6D8");
            var knob = U.Div().Size(20, 20).Radius(10).Bg("#FFFFFF").Abs(on ? 21 : 3, 3);
            sw.Add(knob);
            r.Add(sw);
            r.Tap(() => { P.sound = !P.sound; OrganoAudio.Enabled = P.sound; P.Save(); Refresh(); });
            return r;
        }
    }

    // ==================================================================== Koleksi kartu

    public class CardCollectionScreen : AppScreen
    {
        int m_Filter; // 0 semua, 1 dimiliki, 2 belum

        protected override void Build(object arg)
        {
            if (arg is int f) m_Filter = f;
            string focus = arg as string;
            Root.Add(Parts.TopBar("Koleksi Kartu", App.Back));
            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 8;
            Root.Add(scroll);
            var c = U.Col(16);
            scroll.Add(c);

            int got = Db.Ordered.Count(o => P.HasCard(o.id)), total = Db.Count;
            var sum = U.Row(14, "card", "card-lg");
            sum.Add(new ProgressRing(got / (float)Mathf.Max(1, total), 68, U.C(U.Teal), U.C("#E6F6F4"), got.ToString(), "dari " + total));
            var st = U.Col(4).Grow();
            st.Add(U.Text("Kartu organ terkumpul", "t-body").Bold());
            st.Add(U.Text("Dapatkan kartu dengan menyelesaikan kuis atau scan kartu Organo.", "t-caption"));
            sum.Add(st);
            c.Add(sum);

            var chips = U.Row();
            string[] labels = { "Semua (" + total + ")", "Dimiliki (" + got + ")", "Belum (" + (total - got) + ")" };
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                var chip = U.Div("chip");
                chip.EnableInClassList("chip--on", m_Filter == i);
                chip.Add(U.Text(labels[i]));
                chip.Tap(() => Refresh(k));
                chips.Add(chip);
            }
            c.Add(chips);

            var grid = U.Row().Align(Align.FlexStart).Justify(Justify.SpaceBetween);
            grid.style.flexWrap = Wrap.Wrap;
            foreach (var o in Db.Ordered)
            {
                bool own = P.HasCard(o.id);
                if (m_Filter == 1 && !own || m_Filter == 2 && own) continue;
                var sub = own ? "Didapat · " + P.CardDate(o.id) : (o.season > 1 ? "Season " + o.season : "Kuis Pencernaan " + o.cardNumber);
                var card = Parts.OrganCard(o, own, 168, own && P.newestCard == o.id, sub);
                card.style.marginBottom = 14;
                var oo = o;
                card.Tap(() => ShowCard(oo));
                grid.Add(card);
            }
            c.Add(grid);

            if (focus != null && Db.Get(focus) != null) Root.schedule.Execute(() => ShowCard(Db.Get(focus))).ExecuteLater(250);
        }

        void ShowCard(OrganInfo o)
        {
            bool own = P.HasCard(o.id);
            var c = U.Col(10).Align(Align.Center);
            c.Add(U.Pill(own ? "Sudah dimiliki" : "Belum dimiliki", own ? U.TealSoft : "#F6F5F9", own ? U.TealDark : U.Muted).AlignSelf(Align.Center));
            c.Add(Parts.OrganBadge(o, 96, 0.62f, !own));
            c.Add(U.Text(own ? o.displayName : o.displayName + " (terkunci)", "t-h1").TextCenter());
            c.Add(U.Text(o.SystemLabel, "t-caption"));
            string how = own
                ? (o.id == P.newestCard ? "Kamu dapat dari scan kartu " + o.displayName + " barusan. Selamat!" : "Kamu dapat kartu ini pada " + P.CardDate(o.id) + ".")
                : (o.season > 1 ? "Terbuka di Season " + o.season + ". Bisa juga didapat dengan scan kartu " + o.displayName + "."
                                 : "Selesaikan Kuis Pencernaan " + o.cardNumber + ", atau scan kartu " + o.displayName + ".");
            c.Add(U.Text(how, "t-small", "t-muted").TextCenter());
            OrganoApp.Overlay sh = null;
            var col = U.Col(8).AlignSelf(Align.Stretch);
            if (own) col.Add(U.Button("Pelajari " + o.displayName, () => { sh.Close(); App.Go<OrganDetailScreen>(o.id); }, "btn-primary"));
            else
            {
                var row = U.Row(10);
                row.Add(U.Button("Ke Kuis", () => { sh.Close(); App.GoTab(NavTab.Quiz); }, "btn-outline").Grow());
                row.Add(U.Button("Scan Kartu", () => { sh.Close(); App.Go<ScanScreen>(o.id); }, "btn-primary").Grow());
                col.Add(row);
            }
            col.Add(U.Button("Tutup", () => sh.Close(), "btn-ghost"));
            c.Add(col);
            sh = App.ShowSheet(c);
        }
    }

    /// <summary>Cincin progres (ringkasan koleksi).</summary>
    public class ProgressRing : VisualElement
    {
        readonly float m_T;
        readonly Color m_Fill, m_Track;

        public ProgressRing(float t, float size, Color fill, Color track, string big, string small)
        {
            m_T = Mathf.Clamp01(t); m_Fill = fill; m_Track = track;
            style.width = size; style.height = size; style.flexShrink = 0;
            style.alignItems = Align.Center; style.justifyContent = Justify.Center;
            generateVisualContent += Draw;
            Add(U.Text(big, "t-h2"));
            Add(U.Text(small, "t-tiny"));
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
            show.Add(sel.Medal(96));
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

        protected override void Build(object arg)
        {
            if (m_Size < 0) m_Size = P.textSize;
            if (m_Speed < 0) m_Speed = P.voiceSpeed;
            Root.Add(Parts.TopBar("Aksesibilitas", () => { m_Size = m_Speed = -1; App.Back(); }));
            var scroll = U.Scroll();
            Root.Add(scroll);
            var c = U.Col(18);
            scroll.Add(c);
            float[] sizes = { 12, 14, 17 };

            var prev = U.Col(8, "card", "card-lg");
            prev.Add(U.Text("CONTOH TAMPILAN", "t-tiny").Bold());
            var pr = U.Row(12);
            pr.Add(Parts.OrganBadge(Db.Get("lambung"), 56));
            var pt = U.Col(2).Grow();
            pt.Add(U.Text("Lambung").FontSize(sizes[m_Size] + 2).Bold());
            pt.Add(U.Text("Lambung adalah kantong lentur yang mengaduk makanan.").FontSize(sizes[m_Size]));
            pr.Add(pt);
            prev.Add(pr);
            var voice = U.Row(6).Bg(U.TealSoft).Radius(10).Pad(6, 10);
            voice.Add(U.Text("Suara: \"Lambung adalah kantong lentur…\" · " + new[] { "pelan", "normal", "cepat" }[m_Speed], "t-caption").Fg(U.TealInk));
            prev.Add(voice);
            c.Add(prev);

            c.Add(Segment("Ukuran teks", new[] { "Kecil", "Sedang", "Besar" }, m_Size, i => { m_Size = i; Refresh(); }));
            c.Add(Segment("Kecepatan suara penjelasan", new[] { "Pelan", "Normal", "Cepat" }, m_Speed, i => { m_Speed = i; Refresh(); }));

            var bottom = U.Div().Pad(12, 20, 20, 20);
            bottom.Add(U.Button("Simpan", () =>
            {
                P.textSize = m_Size; P.voiceSpeed = m_Speed;
                P.Save();
                OrganoAudio.SetVoiceSpeed(m_Speed);
                App.ApplyFontScale();
                m_Size = m_Speed = -1;
                App.Toast("Pengaturan disimpan");
                App.Back();
            }, "btn-primary"));
            Root.Add(bottom);
        }

        static VisualElement Segment(string title, string[] options, int sel, System.Action<int> pick)
        {
            var col = U.Col(8);
            col.Add(U.Text(title, "t-section"));
            var row = U.Row(4).Pad(3).Radius(14).Bg("#EDEBF3");
            for (int i = 0; i < options.Length; i++)
            {
                int k = i;
                bool on = i == sel;
                var b = U.Div().Grow().MinH(44).Radius(11).Center().Bg(on ? "#FFFFFF" : "#00000000");
                if (on) b.Border(2, U.Teal);
                b.Add(U.Text(options[i], "t-small").Bold().Fg(on ? U.Ink : U.Muted));
                b.Tap(() => pick(k));
                row.Add(b);
            }
            col.Add(row);
            return col;
        }
    }
}
