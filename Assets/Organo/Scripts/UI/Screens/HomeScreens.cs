using System.Collections.Generic;
using Organo.Data;
using Organo.UI;
using UnityEngine;
using UnityEngine.UIElements;
using U = Organo.UI.UI;

namespace Organo.Screens
{
    // ==================================================================== Beranda

    public class HomeScreen : AppScreen
    {
        public override NavTab Tab => NavTab.Beranda;

        VisualElement m_Track;
        readonly List<VisualElement[]> m_Dots = new List<VisualElement[]>();
        int m_Slide;
        float m_AutoTimer;
        float m_DragStartX, m_DragDx;
        bool m_Dragging;
        const float SlideW = 350f, SlideGap = 10f;

        protected override void Build(object arg)
        {
            m_Dots.Clear();
            m_AutoTimer = 0;
            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 20;
            Root.Add(scroll);
            var c = U.Col(18);
            scroll.Add(c);

            // Header
            var header = U.Row(12).Justify(Justify.SpaceBetween);
            var who = U.Row(12).Tap(() => App.GoTab(NavTab.Profil));
            who.Add(Parts.Avatar(P.gender, 48));
            var hello = U.Col();
            hello.Add(U.Text("Halo, " + P.nickname + "!", "t-h2"));
            hello.Add(U.Text("Semangat belajar hari ini!", "t-small", "t-muted"));
            who.Add(hello);
            header.Add(who);
            header.Add(Parts.Stars(P.stars).Tap(() => App.GoTab(NavTab.Profil)));
            c.Add(header);

            if (P.guest)
            {
                var g = U.Row(10).Pad(10, 10, 10, 12).Radius(14).Bg(U.YellowSoft).Border(1, "#F1D77A");
                g.Add(U.Icon("info", 18));
                g.Add(U.Text("Kamu di Mode Tamu, progres hilang saat keluar.", "t-caption").Fg("#7A5A00").Grow());
                g.Add(U.Button("Daftar", () => App.Go<RegisterScreen>(), "btn-dark", "btn-small"));
                c.Add(g);
            }

            // Hero carousel
            var viewport = U.Div().H(202);
            viewport.style.overflow = Overflow.Hidden;
            m_Track = U.Row().Align(Align.FlexStart);
            m_Track.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("translate") };
            m_Track.style.transitionDuration = new List<TimeValue> { new TimeValue(0.45f, TimeUnit.Second) };
            int s1 = P.season1Done;
            m_Track.Add(Slide(U.Teal, U.Purple, 135, "Coba Sekarang", U.TealDark, "Yuk, Scan Kartumu!", "Lihat organ tubuhmu muncul langsung lewat AR", "Mulai Scan", "hero_scan", () => App.GoTab(NavTab.Scan), -1));
            m_Track.Add(Slide(U.Purple, U.Pink, 135, "Season 1", U.Pink, "Sistem Pencernaan", "Quiz " + s1 + " dari 5 selesai", "Mulai Quiz", "hero_quiz", () => App.Go<QuizPlayScreen>(), s1 / 5f));
            m_Track.Add(Slide(U.Teal, "#A18BEE", 225, "Jelajahi", "#7455D8", "Perpustakaan Organ", "Pelajari semua organ tubuh, kapan aja kamu mau", "Buka Library", "hero_library", () => App.Go<LibraryListScreen>(), -1));
            viewport.Add(m_Track);
            viewport.RegisterCallback<PointerDownEvent>(e => { m_Dragging = true; m_DragStartX = e.position.x; m_DragDx = 0; }, TrickleDown.TrickleDown);
            viewport.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!m_Dragging) return;
                m_DragDx = e.position.x - m_DragStartX;
                if (Mathf.Abs(m_DragDx) > 6)
                {
                    m_Track.style.transitionDuration = new List<TimeValue> { new TimeValue(0) };
                    float d = (m_Slide == 0 && m_DragDx > 0) || (m_Slide == 2 && m_DragDx < 0) ? m_DragDx * 0.35f : m_DragDx;
                    m_Track.style.translate = new Translate(-m_Slide * (SlideW + SlideGap) + d, 0);
                }
            }, TrickleDown.TrickleDown);
            viewport.RegisterCallback<PointerUpEvent>(e => EndDrag(), TrickleDown.TrickleDown);
            viewport.RegisterCallback<PointerLeaveEvent>(e => { if (m_Dragging) EndDrag(); });
            c.Add(viewport);
            GoSlide(m_Slide);

            // Kategori
            var cat = U.Col(10);
            cat.Add(U.Text("Kategori", "t-section"));
            var cats = U.Row().Justify(Justify.SpaceBetween).Align(Align.FlexStart);
            cats.Add(Category("cat_cerna", U.TealSoft, "Pencernaan", OrganSystem.Pencernaan));
            cats.Add(Category("cat_napas", U.PurpleSoft, "Pernapasan", OrganSystem.Pernapasan));
            cats.Add(Category("cat_darah", U.PinkSoft, "Peredaran Darah", OrganSystem.PeredaranDarah));
            cats.Add(Category("cat_saraf", U.YellowSoft, "Saraf", OrganSystem.Saraf));
            cat.Add(cats);
            c.Add(cat);

            // Progress quiz
            var prog = U.Col(10);
            prog.Add(Parts.SectionHeader("Progress Quiz", "Lihat semua", () => App.GoTab(NavTab.Quiz)));
            prog.Add(ProgressRow("1", U.Teal, "Sistem Pencernaan", s1 / 5f, s1 + "/5", "#E6F6F4"));
            prog.Add(ProgressRow("2", U.Purple, "Sistem Pernapasan", 0, s1 >= 5 ? "0/5" : "Terkunci", "#EEEAFB"));
            c.Add(prog);

            // Buddy
            var buddy = U.Row(12, "card").Border(1, "#BDEBE5").Pad(10, 14);
            buddy.Add(U.Icon("buddy", 44));
            var bt = U.Col().Grow();
            bt.Add(U.Text(P.habitsToday.Count >= 3 ? "Buddy-mu senang sekali!" : "Buddy-mu senang hari ini!", "t-body").Bold());
            bt.Add(U.Text(P.habitsToday.Count > 0 ? P.habitsToday.Count + " kebiasaan sehat tercatat" : "Yuk isi kebiasaan sehatmu", "t-caption"));
            buddy.Add(bt);
            buddy.Add(U.Icon("arrow_right", 16).Tinted(U.C(U.Faint)));
            buddy.Tap(ShowBuddy);
            c.Add(buddy);

            // Materi tubuh
            bool girl = P.gender == Gender.Girl;
            var materi = U.Row(12, "card", "card-lg").Bg(girl ? "#FFF1F4" : "#EEF9F7").Border(1, girl ? "#F5B7C6" : "#BDEBE5");
            materi.Add(U.Icon(girl ? "kid_girl" : "kid_boy", 48));
            var mt = U.Col(2).Grow();
            mt.Add(U.Text("MATERI TUBUH", "t-tiny").Bold().Fg(girl ? U.PinkDark : U.TealDark));
            mt.Add(U.Text("Tubuhku Tumbuh Besar", "t-body").Bold());
            mt.Add(U.Text(girl ? "Haid, perubahan bentuk tubuh, dan tubuhku milikku" : "Suara memberat, tumbuh rambut baru, dan tubuhku milikku", "t-caption"));
            materi.Add(mt);
            materi.Add(U.Icon("arrow_right", 16).Tinted(U.C(U.Faint)));
            materi.Tap(() => App.Go<BodyMaterialScreen>());
            c.Add(materi);
            c.Add(U.Space(8));
        }

        VisualElement Slide(string from, string to, float angle, string tag, string tagColor, string title, string desc, string cta, string art, System.Action go, float progress)
        {
            var s = U.Div().Size(SlideW, 202).Radius(16).Gradient(from, to, angle).Pad(16, 16, 8, 16);
            s.style.marginRight = SlideGap;
            s.style.overflow = Overflow.Hidden;
            var glow = U.Div().Size(150, 150).Radius(75).Bg(U.C("#FFFFFF", 0.18f)).Abs(null, 8, -4);
            glow.pickingMode = PickingMode.Ignore;
            s.Add(glow);
            var artIcon = U.Icon(art, 112, 120).Abs(null, 26, 18);
            s.Add(artIcon);
            var col = U.Col(6).W(190).Align(Align.FlexStart);
            var pill = U.Pill(tag, "#FFFFFF", tagColor);
            col.Add(pill);
            col.Add(U.Text(title).FontSize(20).Bold().Fg("#FFFFFF"));
            col.Add(U.Text(desc).FontSize(13).Fg(U.C("#FFFFFF", 0.9f)));
            if (progress >= 0)
            {
                var bar = U.Progress(progress, U.Pink, "#FFFFFF66").W(160);
                bar.style.backgroundColor = U.C("#FFFFFF", 0.4f);
                col.Add(bar);
            }
            var btn = U.Row(6).Bg("#FFFFFF").Radius(999).Pad(9, 16);
            btn.style.marginTop = 6;
            btn.Add(U.Text(cta).FontSize(13).Bold().Fg(tagColor));
            btn.Add(U.Icon("arrow_right", 14).Tinted(U.C(tagColor)));
            btn.Tap(() => { if (Mathf.Abs(m_DragDx) < 8) go(); });
            col.Add(btn);
            s.Add(col);

            var dots = U.Row().Abs(0, null, 0, 5).Justify(Justify.Center);
            var arr = new VisualElement[3];
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                var hit = U.Div().Pad(5, 3).Tap(() => { GoSlide(k); m_AutoTimer = 0; });
                arr[i] = U.Div().Radius(4);
                hit.Add(arr[i]);
                dots.Add(hit);
            }
            m_Dots.Add(arr);
            s.Add(dots);
            return s;
        }

        void EndDrag()
        {
            if (!m_Dragging) return;
            m_Dragging = false;
            m_Track.style.transitionDuration = new List<TimeValue> { new TimeValue(0.45f, TimeUnit.Second) };
            int n = m_Slide;
            if (m_DragDx < -60) n++; else if (m_DragDx > 60) n--;
            GoSlide(Mathf.Clamp(n, 0, 2));
            m_AutoTimer = 0;
            Root.schedule.Execute(() => m_DragDx = 0).ExecuteLater(50);
        }

        void GoSlide(int i)
        {
            m_Slide = i;
            m_Track.style.translate = new Translate(-i * (SlideW + SlideGap), 0);
            foreach (var arr in m_Dots)
                for (int k = 0; k < arr.Length; k++)
                {
                    bool on = k == i;
                    arr[k].Size(on ? 8 : 6, on ? 8 : 6).Bg(on ? U.C("#FFFFFF") : new Color(0, 0, 0, 0)).Border(1, U.C("#FFFFFF", on ? 1f : 0.6f));
                }
        }

        public override void Tick(float dt)
        {
            if (m_Dragging) return;
            m_AutoTimer += dt;
            if (m_AutoTimer > 4.5f) { m_AutoTimer = 0; GoSlide((m_Slide + 1) % 3); }
        }

        VisualElement Category(string icon, string bg, string label, OrganSystem sys)
        {
            var c = U.Col(6).W(76).Align(Align.Center);
            var circle = U.Div().Size(52, 52).Radius(26).Bg(bg).Center();
            circle.Add(U.Icon(icon, 28));
            c.Add(circle);
            c.Add(U.Text(label).FontSize(11).TextCenter());
            c.Tap(() =>
            {
                if (sys == OrganSystem.Saraf) App.Toast("Sistem Saraf segera hadir. Nantikan, ya!");
                else App.Go<LibraryListScreen>(sys);
            });
            return c;
        }

        VisualElement ProgressRow(string num, string color, string title, float t, string right, string track)
        {
            var r = U.Row(12, "card");
            r.Add(U.Div().Size(30, 30).Radius(15).Bg(color).Center().Kids(U.Text(num).FontSize(13).Bold().Fg("#FFFFFF")));
            var mid = U.Col(6).Grow();
            mid.Add(U.Text(title, "t-section"));
            mid.Add(U.Progress(t, color, track));
            r.Add(mid);
            var rl = U.Text(right).Bold().FontSize(right == "Terkunci" ? 12 : 14);
            if (right == "Terkunci") rl.Fg(U.Faint);
            r.Add(rl);
            r.Tap(() => App.GoTab(NavTab.Quiz));
            return r;
        }

        void ShowBuddy()
        {
            var habits = new (string id, string label)[]
            {
                ("air", "Minum 8 gelas air putih"),
                ("sayur", "Makan sayur dan buah"),
                ("gigi", "Sikat gigi pagi dan malam"),
                ("gerak", "Bermain atau olahraga 30 menit"),
                ("tidur", "Tidur cukup sebelum jam 9"),
            };
            var chosen = new HashSet<string>(P.habitsToday);
            var c = U.Col(10);
            var head = U.Col(4).Align(Align.Center);
            head.Add(U.Icon("mascot_happy", 96, 78));
            head.Add(U.Text("Yuk, isi kebiasaan sehatmu!", "t-h2").TextCenter());
            head.Add(U.Text("Buddy-mu akan makin semangat kalau kamu rajin!", "t-caption").TextCenter());
            c.Add(head);
            Label count = null;
            foreach (var (id, label) in habits)
            {
                var row = U.Row(12, "card");
                var box = Parts.Checkbox(chosen.Contains(id));
                row.Add(box);
                row.Add(U.Text(label, "t-small").Grow());
                row.Tap(() =>
                {
                    if (!chosen.Remove(id)) chosen.Add(id);
                    bool on = chosen.Contains(id);
                    box.EnableInClassList("checkbox--on", on);
                    box.Clear();
                    if (on) box.Add(U.Icon("check", 12));
                    row.Border(1, on ? U.Teal : U.Line);
                    count.text = chosen.Count + " dari " + habits.Length + " kebiasaan";
                });
                if (chosen.Contains(id)) row.Border(1, U.Teal);
                c.Add(row);
            }
            count = U.Text(chosen.Count + " dari " + habits.Length + " kebiasaan", "t-caption").TextCenter();
            c.Add(count);
            OrganoApp.Overlay sh = null;
            c.Add(U.Button("Simpan", () =>
            {
                P.habitsToday = new List<string>(chosen);
                P.Save();
                sh.Close();
                OrganoAudio.Success();
                Refresh();
            }, "btn-primary"));
            sh = App.ShowSheet(c);
        }
    }

    // ==================================================================== Materi Tubuh

    public class BodyMaterialScreen : AppScreen
    {
        Gender m_Tab;
        int m_Open = 0;

        protected override void Build(object arg)
        {
            if (arg is Gender g) m_Tab = g;
            else if (m_Tab == Gender.None) m_Tab = P.gender == Gender.Girl ? Gender.Girl : Gender.Boy;
            bool girl = m_Tab == Gender.Girl;

            Root.Add(Parts.TopBar("Materi Tubuh", App.Back));
            var tabs = U.Row(4).Pad(3).Radius(999).Bg("#EDEBF3").Margin(8, 20, 0, 20);
            foreach (var (gg, label) in new[] { (Gender.Boy, "Laki-laki"), (Gender.Girl, "Perempuan") })
            {
                bool on = m_Tab == gg;
                var t = U.Row(6).Grow().MinH(40).Radius(999).Center().Bg(on ? "#FFFFFF" : "#00000000");
                t.Add(U.Text(label, "t-small").Bold().Fg(on ? U.Ink : U.Muted));
                if (P.gender == gg) t.Add(U.Pill("Pilihanmu", U.TealSoft, U.TealDark));
                var cap = gg;
                t.Tap(() => { m_Open = 0; Refresh(cap); });
                tabs.Add(t);
            }
            Root.Add(tabs);

            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 16;
            Root.Add(scroll);
            var c = U.Col(14);
            scroll.Add(c);

            var hero = U.Row(12).Radius(16).Pad(16).Gradient(girl ? U.Pink : U.Teal, U.Purple, 135);
            var ht = U.Col(4).Grow();
            ht.Add(U.Text("TUBUHKU TUMBUH BESAR").FontSize(11).Bold().Fg(U.C("#FFFFFF", 0.9f)));
            ht.Add(U.Text(girl ? "Perubahan tubuh anak perempuan" : "Perubahan tubuh anak laki-laki").FontSize(18).Bold().Fg("#FFFFFF"));
            ht.Add(U.Text("Semua perubahan ini normal dan terjadi pada waktu yang berbeda untuk setiap anak.").FontSize(12).Fg(U.C("#FFFFFF", 0.92f)));
            hero.Add(ht);
            hero.Add(U.Icon(girl ? "gender_girl" : "gender_boy", 84));
            c.Add(hero);

            var topics = girl
                ? new[]
                {
                    ("Tubuh makin tinggi", "Tinggi badan bertambah cepat dan bentuk tubuh mulai berubah, misalnya pinggul melebar dan payudara mulai tumbuh.", "Pakai pakaian yang nyaman untukmu."),
                    ("Haid (menstruasi)", "Suatu hari kamu akan mulai haid: keluar darah dari vagina beberapa hari setiap bulan. Ini tanda tubuhmu sehat dan tumbuh.", "Siapkan pembalut dan ceritakan ke orang tua."),
                    ("Rambut & kulit", "Tumbuh rambut halus di ketiak dan kemaluan. Kulit bisa lebih berminyak dan muncul jerawat.", "Mandi teratur dan cuci muka dua kali sehari."),
                    ("Perasaan berubah", "Kadang perasaan mudah berubah. Itu wajar karena hormon sedang bekerja.", "Ceritakan perasaanmu ke orang yang kamu percaya.")
                }
                : new[]
                {
                    ("Tubuh makin tinggi", "Tinggi badan bertambah cepat, bahu melebar, dan otot makin kuat.", "Makan bergizi dan cukup tidur membantu tubuhmu tumbuh."),
                    ("Suara memberat", "Suaramu bisa pecah-pecah lalu menjadi lebih berat. Jakun di leher juga mulai terlihat.", "Tidak apa-apa kalau suaramu kadang aneh, itu normal!"),
                    ("Tumbuh rambut baru", "Tumbuh rambut di ketiak, kemaluan, dan kadang kumis tipis. Kulit bisa berminyak dan berjerawat.", "Mandi teratur dan pakai deodoran bila perlu."),
                    ("Perasaan berubah", "Kadang perasaan mudah berubah. Itu wajar karena hormon sedang bekerja.", "Ceritakan perasaanmu ke orang yang kamu percaya.")
                };
            for (int i = 0; i < topics.Length; i++)
            {
                var (title, body, tip) = topics[i];
                int k = i;
                bool open = m_Open == i;
                var card = U.Col(0, "card").Pad(0);
                var head = U.Row(12).Pad(12, 14);
                head.Add(U.Div().Size(36, 36).Radius(18).Bg(girl ? U.PinkSoft : U.TealSoft).Center().Kids(U.Text((i + 1).ToString()).Bold().Fg(girl ? U.PinkDark : U.TealDark)));
                head.Add(U.Text(title, "t-body").Bold().Grow());
                var chev = U.Icon("arrow_right", 16).Tinted(U.C(U.Faint));
                chev.style.rotate = new Rotate(open ? 90 : 0);
                head.Add(chev);
                head.Tap(() => { m_Open = open ? -1 : k; Refresh(m_Tab); });
                card.Add(head);
                if (open)
                {
                    var bodyCol = U.Col(8).Pad(0, 14, 14, 62);
                    bodyCol.Add(U.Text(body, "t-small"));
                    var tipRow = U.Row(6).Bg(U.YellowSoft).Radius(10).Pad(8, 10);
                    tipRow.Add(U.Icon("bulb", 16).Tinted(U.C("#B07D00")));
                    tipRow.Add(U.Text(tip, "t-caption").Fg("#7A5A00").Grow());
                    bodyCol.Add(tipRow);
                    card.Add(bodyCol);
                }
                c.Add(card);
            }

            var safety = U.Col(8, "card", "card-lg").Bg("#F3EFFD").Border(1, "#CDBFF5");
            var sh = U.Row(8);
            sh.Add(U.Icon("shield", 20).Tinted(U.C(U.PurpleDark)));
            sh.Add(U.Text("Tubuhku milikku", "t-section").Fg(U.PurpleDark));
            safety.Add(sh);
            safety.Add(U.Text("Bagian tubuh yang tertutup pakaian dalam adalah bagian pribadi. Kalau ada yang menyentuh atau membuatmu tidak nyaman, katakan <b>TIDAK</b>, pergi, lalu cerita ke orang dewasa yang kamu percaya.", "t-small"));
            c.Add(safety);
            c.Add(U.Text("Punya pertanyaan? Tanyakan ke orang tua atau guru, ya.", "t-caption").TextCenter());
            c.Add(U.Text("Ubah pilihan gender").FontSize(13).Bold().Fg(U.TealDark).AlignSelf(Align.Center).Tap(() => App.Go<GenderScreen>("profile")));
        }
    }
}
