using System.Linq;
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
        // penanda slide: titik aktif memanjang & terisi mengikuti waktu sampai slide berikutnya
        readonly VisualElement[] m_DotTrack = new VisualElement[SlideCount];
        readonly VisualElement[] m_DotFill = new VisualElement[SlideCount];
        const int SlideCount = 3;
        const float SlideSeconds = 4.5f;
        int m_Slide;
        float m_AutoTimer;
        float m_DragStartX, m_DragDx;
        bool m_Dragging;
        const float SlideW = 350f, SlideGap = 10f;

        protected override void Build(object arg)
        {
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
            who.Add(U.Text("Halo, " + P.nickname + "!", "t-h2"));
            header.Add(who);
            header.Add(Parts.Stars(P.stars).Tap(() => App.GoTab(NavTab.Profil)));
            c.Add(header);

            // Hero carousel
            var viewport = U.Div().H(222);
            viewport.style.marginTop = -8; viewport.style.marginBottom = -12;
            viewport.style.paddingTop = 8;
            viewport.style.overflow = Overflow.Hidden;
            m_Track = U.Row().Align(Align.FlexStart);
            m_Track.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("translate") };
            m_Track.style.transitionDuration = new List<TimeValue> { new TimeValue(0.45f, TimeUnit.Second) };
            // kuis berikutnya yang sudah terbuka dan belum dimainkan (bukan angka contoh)
            var (nextOrgan, nextProcess) = QuizAccess.Next(P);
            bool hasNext = nextOrgan != null || nextProcess != null;
            int quizTotal = QuizAccess.Order(P).Count + QuizBank.Processes.Count;
            int quizDone = QuizAccess.OrgansDone(P) + QuizAccess.ProcessesDone(P);
            m_Track.Add(Slide(U.Teal, U.Purple, 135, null, U.TealDark, "Ayo, Scan Kartumu!", null, "Mulai Scan", "hero_scan", () => App.GoTab(NavTab.Scan), -1));
            m_Track.Add(Slide(U.Purple, U.Pink, 135, null, U.Pink,
                nextProcess != null ? nextProcess.title : nextOrgan != null ? "Kuis " + Db.Get(nextOrgan)?.displayName : "Buka kuis baru!",
                hasNext ? "5 soal · " + quizDone + " dari " + quizTotal + " kuis selesai" : "Scan kartu lain untuk membuka kuisnya",
                hasNext ? "Mulai Kuis" : "Lihat Kuis", "hero_quiz",
                () =>
                {
                    if (nextProcess != null) QuizSession.Play(App, QuizSession.StartProcess(nextProcess.id));
                    else if (nextOrgan != null) QuizSession.Play(App, QuizSession.StartOrgan(nextOrgan));
                    else App.GoTab(NavTab.Quiz);
                },
                quizDone / (float)quizTotal));
            m_Track.Add(Slide(U.Teal, "#A18BEE", 225, null, "#7455D8", "Perpustakaan Organ", null, "Buka Pustaka", "hero_library", () => App.Go<LibraryListScreen>(), -1));
            viewport.Add(m_Track);
            var indicator = U.Row(0).Abs(0, null, 0, 14).Justify(Justify.Center);
            for (int i = 0; i < SlideCount; i++)
            {
                int k = i;
                var hit = U.Div().Pad(6, 3).Tap(() => { GoSlide(k); m_AutoTimer = 0; });
                var track = U.Div().H(6).W(6).Radius(3).Bg(U.C("#FFFFFF", 0.55f));
                track.style.overflow = Overflow.Hidden;
                track.style.transitionProperty = new List<StylePropertyName> { "width", "background-color" };
                track.style.transitionDuration = new List<TimeValue> { 0.3f, 0.3f };
                var fill = U.Div().H(6).Radius(3).Bg("#FFFFFF");
                fill.style.width = Length.Percent(0);
                track.Add(fill);
                hit.Add(track);
                hit.tooltip = "Slide " + (i + 1);
                indicator.Add(hit);
                m_DotTrack[i] = track;
                m_DotFill[i] = fill;
            }
            viewport.Add(indicator);
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
            // 7 kategori: rel geser horizontal (ujung kanan terpotong sebagai tanda bisa digeser)
            var cats = U.Scroll(true);
            cats.style.flexGrow = 0; cats.style.flexShrink = 0;
            cats.style.marginLeft = -20; cats.style.marginRight = -20;
            cats.contentContainer.style.flexDirection = FlexDirection.Row;
            cats.contentContainer.style.alignItems = Align.FlexStart;
            cats.contentContainer.style.paddingLeft = 16;
            cats.contentContainer.style.paddingRight = 12;
            cats.Add(Category("cat_cerna", U.TealSoft, "Pencernaan", OrganSystem.Pencernaan));
            cats.Add(Category("cat_napas", U.PurpleSoft, "Pernapasan", OrganSystem.Pernapasan));
            cats.Add(Category("cat_darah", U.PinkSoft, "Peredaran Darah", OrganSystem.PeredaranDarah));
            cats.Add(Category("cat_ekskresi", "#FDE7DA", "Ekskresi", OrganSystem.Ekskresi));
            cats.Add(Category("cat_saraf", U.YellowSoft, "Saraf", OrganSystem.Saraf));
            cats.Add(Category("cat_indera", "#DCEBFB", "Indera", OrganSystem.Indera));
            cats.Add(Category("cat_gerak", "#F4ECDD", "Gerak", OrganSystem.Gerak));
            cats.Add(Category("cat_repro", "#F6E5F5", "Reproduksi", OrganSystem.Reproduksi));
            cat.Add(cats);
            c.Add(cat);

            // Favoritku: organ yang ditandai bookmark, bisa langsung dibuka lagi
            // (bagian ini hanya muncul kalau sudah ada organ yang di-bookmark)
            var favs = P.favorites.Select(id => Db.Get(id)).Where(f => f != null).ToList();
            var favSec = U.Col(10);
            favSec.Add(Parts.SectionHeader("Favoritku", "Lihat semua", () => App.Go<LibraryListScreen>("favorit")));
            if (favs.Count > 0)
            {
                var rail = U.Scroll(true);
                rail.style.flexGrow = 0; rail.style.flexShrink = 0;
                rail.style.marginLeft = -20; rail.style.marginRight = -20;
                rail.contentContainer.style.flexDirection = FlexDirection.Row;
                rail.contentContainer.style.paddingLeft = 20;
                foreach (var f in favs)
                {
                    var t = U.Col(6).Size(92, 104).Radius(16).Center().Bg("#FFFFFF").Border(1, U.Line);
                    t.style.marginRight = 10;
                    t.Add(Parts.OrganBadge(f, 56));
                    t.Add(U.Text(f.displayName).FontSize(11).Bold().TextCenter());
                    var mark = U.Div().Size(20, 20).Radius(10).Abs(null, 6, 6).Bg(U.Pink).Center();
                    mark.pickingMode = PickingMode.Ignore;
                    mark.Add(U.Icon("bookmark", 11).Tinted(Color.white));
                    t.Add(mark);
                    var id = f.id;
                    t.Tap(() => App.Go<OrganDetailScreen>(id));
                    rail.Add(t);
                }
                favSec.Add(rail);
                c.Add(favSec);
            }

            c.Add(CollectionSection());

            // Materi tubuh
            bool girl = P.gender == Gender.Girl;
            var materi = U.Row(12, "card", "card-lg").Bg(girl ? "#FFF1F4" : "#EEF9F7").Border(1, girl ? "#F5B7C6" : "#BDEBE5");
            materi.Add(U.Icon(girl ? "kid_girl" : "kid_boy", 48));
            var mt = U.Col(2).Grow();
            mt.Add(U.Text("Tubuhku Tumbuh Besar", "t-body").Bold());
            mt.Add(U.Text(girl ? "Haid, perubahan bentuk tubuh, dan tubuhku milikku" : "Suara memberat, tumbuh rambut baru, dan tubuhku milikku", "t-caption"));
            materi.Add(mt);
            materi.Add(U.Icon("chevron_right", 18));
            materi.Elev(Elevation.E1);
            materi.Tap(() => App.Go<BodyMaterialScreen>());
            c.Add(materi);
            c.Add(U.Space(8));
        }

        VisualElement Slide(string from, string to, float angle, string tag, string tagColor, string title, string desc, string cta, string art, System.Action go, float progress)
        {
            var s = U.Div().Size(SlideW, 202).Radius(16).Gradient(from, to, angle).Pad(16, 16, 8, 16);
            s.style.marginRight = SlideGap;
            s.style.overflow = Overflow.Hidden;
            var glow = U.Div().Size(190, 190).Abs(null, -12, -24);
            glow.style.backgroundImage = new StyleBackground(GradientTexture.Radial(new Color(1, 1, 1, 0.45f)));
            glow.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
            glow.pickingMode = PickingMode.Ignore;
            s.Add(glow);
            var artIcon = U.Icon(art, 112, 120).Abs(null, 26, 18);
            s.Add(artIcon);
            var col = U.Col(6).W(190).Align(Align.FlexStart);
            col.style.flexGrow = 1;
            col.style.justifyContent = Justify.Center;
            if (tag != null) col.Add(U.Pill(tag, "#FFFFFF", tagColor));
            var tt = U.Text(title).FontSize(22).Bold().Fg("#FFFFFF");
            tt.style.whiteSpace = WhiteSpace.Normal;
            col.Add(tt);
            if (desc != null) col.Add(U.Text(desc).FontSize(13).Fg(U.C("#FFFFFF", 0.9f)));
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
            return s;
        }

        void EndDrag()
        {
            if (!m_Dragging) return;
            m_Dragging = false;
            m_Track.style.transitionDuration = new List<TimeValue> { new TimeValue(0.45f, TimeUnit.Second) };
            int n = m_Slide;
            if (m_DragDx < -60) n++; else if (m_DragDx > 60) n--;
            GoSlide(Mathf.Clamp(n, 0, SlideCount - 1));
            m_AutoTimer = 0;
            Root.schedule.Execute(() => m_DragDx = 0).ExecuteLater(50);
        }

        void GoSlide(int i)
        {
            m_Slide = i;
            m_Track.style.translate = new Translate(-i * (SlideW + SlideGap), 0);
            for (int k = 0; k < SlideCount; k++)
            {
                if (m_DotTrack[k] == null) continue;
                bool on = k == i;
                m_DotTrack[k].style.width = on ? 24 : 6;
                m_DotTrack[k].style.backgroundColor = U.C("#FFFFFF", on ? 0.35f : 0.55f);
                m_DotFill[k].style.width = Length.Percent(0);
            }
        }

        public override void Tick(float dt)
        {
            if (m_Dragging) return;
            m_AutoTimer += dt;
            if (m_AutoTimer > SlideSeconds) { m_AutoTimer = 0; GoSlide((m_Slide + 1) % SlideCount); }
            else if (m_DotFill[m_Slide] != null) m_DotFill[m_Slide].style.width = Length.Percent(Mathf.Clamp01(m_AutoTimer / SlideSeconds) * 100f);
        }

        /// <summary>Ringkasan koleksi kartu: tumpukan kartu terbaru + progres, ketuk untuk membuka album.</summary>
        VisualElement CollectionSection()
        {
            var owned = Db.Ordered.Where(o => P.HasCard(o.id)).ToList();
            int total = Db.Count;
            var sec = U.Col(10);
            sec.Add(Parts.SectionHeader("Koleksi Kartu", "Lihat semua", () => App.Go<CardCollectionScreen>()));

            var box = U.Row(14, "card").Pad(12, 14).Radius(16).Border(1, U.Line);
            box.Elev(Elevation.E1);
            // tumpukan 3 kartu: terbaru di depan
            var stack = U.Div().Size(92, 76).NoShrink();
            var newest = owned.OrderBy(o => o.id == P.newestCard ? 1 : 0).ToList();
            var pick = newest.Skip(Mathf.Max(0, newest.Count - 3)).ToList();
            float[] rot = { -12f, 8f, 0f };
            float[] x = { 4f, 40f, 22f };
            for (int i = 0; i < 3; i++)
            {
                VisualElement card;
                int k = i - (3 - pick.Count);
                if (pick.Count > 0 && k >= 0) card = CardArt.Front(pick[k], 46, 6);
                else if (pick.Count == 0) card = CardArt.Locked(Db.Ordered.ElementAt(i), 46, 6);
                else continue;
                var holder = U.Div().Size(46, 46 * CardArt.Aspect).Abs(x[i], 6);
                holder.style.rotate = new Rotate(rot[i]);
                holder.Add(card);
                if (pick.Count > 0) card.Elev(Elevation.E1);
                stack.Add(holder);
            }
            box.Add(stack);

            var t = U.Col(6).Grow();
            t.Add(U.Text(owned.Count + " dari " + total + " kartu", "t-body").Bold());
            t.Add(U.Progress(owned.Count / (float)Mathf.Max(1, total)));
            var newestOrgan = P.newestCard != null ? Db.Get(P.newestCard) : null;
            if (newestOrgan != null && owned.Count > 0) t.Add(U.Text("Terbaru: " + newestOrgan.displayName, "t-caption"));
            box.Add(t);
            box.Add(U.Icon("chevron_right", 18));
            box.tooltip = "Buka koleksi kartu";
            box.Tap(() => App.Go<CardCollectionScreen>());
            sec.Add(box);
            return sec;
        }

        VisualElement Category(string icon, string bg, string label, OrganSystem sys)
        {
            var c = U.Col(6).W(64).Align(Align.Center);
            c.style.marginRight = 4;
            var circle = U.Div().Size(50, 50).Radius(25).Bg(bg).Center();
            circle.Add(U.Icon(icon, 28));
            c.Add(circle);
            c.Add(U.Text(label).FontSize(10.5f).TextCenter());
            c.Tap(() =>
            {
                if (!Db.BySystem(sys).Any()) App.Toast("Sistem ini segera hadir. Nantikan, ya!");
                else App.Go<LibraryListScreen>(sys);
            });
            return c;
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
            else m_Tab = P.gender == Gender.Girl ? Gender.Girl : Gender.Boy;
            bool girl = m_Tab == Gender.Girl;

            Root.Add(Parts.TopBar("Materi Tubuh", App.Back));
            var tabs = U.Row(4).Pad(3).Radius(999).Bg("#EDEBF3").Margin(8, 20, 0, 20);
            foreach (var (gg, label) in new[] { (Gender.Boy, "Laki-laki"), (Gender.Girl, "Perempuan") })
            {
                bool on = m_Tab == gg;
                var t = U.Row(6).Grow().MinH(40).Radius(999).Center().Bg(on ? "#FFFFFF" : "#00000000");
                t.Add(U.Text(label, "t-small").Bold().Fg(on ? U.Ink : U.Muted));
                if (P.gender == gg)
                {
                    var mine = U.Pill("Pilihanmu", U.TealSoft, U.TealDark).AlignSelf(Align.Center);
                    mine.Pad(2, 8);
                    ((Label)mine[0]).style.fontSize = 10;
                    t.Add(mine);
                }
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
            var heroPic = U.Div().Size(84, 92).NoShrink();
            heroPic.style.overflow = Overflow.Hidden;
            heroPic.Add(U.Icon(girl ? "gender_girl" : "gender_boy", 84, 107));
            hero.Add(heroPic);
            c.Add(hero);

            var topics = girl
                ? new[]
                {
                    ("Tubuh makin tinggi", "Tinggi badan bertambah cepat dan bentuk tubuh mulai berubah, misalnya pinggul melebar dan payudara mulai tumbuh.", "Pakai pakaian yang nyaman untukmu."),
                    ("Haid (menstruasi)", "Suatu hari kamu akan mulai haid: keluar darah dari vagina beberapa hari setiap bulan. Ini tanda tubuhmu sehat dan tumbuh.", "Siapkan pembalut dan ceritakan kepada orang tua."),
                    ("Rambut & kulit", "Tumbuh rambut halus di ketiak dan kemaluan. Kulit bisa lebih berminyak dan muncul jerawat.", "Mandi teratur dan cuci muka dua kali sehari."),
                    ("Perasaan berubah", "Kadang perasaan mudah berubah. Itu wajar karena hormon sedang bekerja.", "Ceritakan perasaanmu kepada orang yang kamu percaya.")
                }
                : new[]
                {
                    ("Tubuh makin tinggi", "Tinggi badan bertambah cepat, bahu melebar, dan otot makin kuat.", "Makan bergizi dan cukup tidur membantu tubuhmu tumbuh."),
                    ("Suara memberat", "Suaramu bisa pecah-pecah lalu menjadi lebih berat. Jakun di leher juga mulai terlihat.", "Tidak apa-apa kalau suaramu kadang aneh, itu normal!"),
                    ("Tumbuh rambut baru", "Tumbuh rambut di ketiak, kemaluan, dan kadang kumis tipis. Kulit bisa berminyak dan berjerawat.", "Mandi teratur dan pakai deodoran bila perlu."),
                    ("Perasaan berubah", "Kadang perasaan mudah berubah. Itu wajar karena hormon sedang bekerja.", "Ceritakan perasaanmu kepada orang yang kamu percaya.")
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
                var chev = U.Icon("chevron_down", 18);
                chev.style.rotate = new Rotate(open ? 180 : 0);
                head.Add(chev);
                head.Tap(() => { m_Open = open ? -1 : k; Refresh(m_Tab); });
                card.Add(head);
                if (open)
                {
                    var bodyCol = U.Col(8).Pad(0, 14, 14, 62);
                    bodyCol.Add(U.Text(body, "t-small"));
                    var tipRow = U.Row(6).Bg(U.YellowSoft).Radius(10).Pad(8, 10);
                    tipRow.Add(U.Icon("bulb_teal", 14));
                    tipRow.Add(U.Text(tip, "t-caption").Fg("#7A5A00").Grow());
                    bodyCol.Add(tipRow);
                    card.Add(bodyCol);
                }
                c.Add(card);
            }

            var safety = U.Col(8, "card", "card-lg").Bg("#F3EFFD").Border(1, "#CDBFF5");
            var sh = U.Row(8);
            sh.Add(U.Div().Size(34, 34).Radius(17).Bg(U.PurpleDark).Center().Kids(U.Icon("shield_white", 20)));
            sh.Add(U.Text("Tubuhku milikku", "t-section").Fg(U.PurpleDark));
            safety.Add(sh);
            safety.Add(U.Text("Bagian tubuh yang tertutup pakaian dalam adalah bagian pribadi. Kalau ada yang menyentuh atau membuatmu tidak nyaman, katakan <b>TIDAK</b>, pergi, lalu bercerita kepada orang dewasa yang kamu percaya.", "t-small"));
            c.Add(safety);
            c.Add(U.Text("Punya pertanyaan? Tanyakan kepada orang tua atau guru, ya.", "t-caption").TextCenter());
            c.Add(U.Text("Ubah pilihan gender").FontSize(13).Bold().Fg(U.TealDark).AlignSelf(Align.Center).Tap(() => App.Go<GenderScreen>("profile")));
        }
    }
}
