using System.Collections.Generic;
using System.Linq;
using Organo.Data;
using Organo.ThreeD;
using Organo.UI;
using UnityEngine;
using UnityEngine.UIElements;
using U = Organo.UI.UI;

namespace Organo.Screens
{
    /// <summary>Kuis yang sedang dimainkan.</summary>
    public class QuizSession
    {
        public static QuizSession Current;
        public OrganSystem system = OrganSystem.Pencernaan;
        public List<QuizQuestion> questions;
        public int index;
        public readonly List<QuizAnswer> answers = new List<QuizAnswer>();
        public int Correct => answers.Count(a => a.isCorrect);
        public QuizQuestion Q => index < questions.Count ? questions[index] : null;
        public QuizRecord result;
        /// <summary>Kuis topik khusus (mis. "repro:reprolaki"); null = kuis biasa.</summary>
        public string topic;
        /// <summary>Season yang dimainkan (mis. "napas"); null = latihan per jenis soal / topik.</summary>
        public string seasonId;
        /// <summary>Id untuk riwayat & "Ulangi kuis" (lihat <see cref="QuizRecord.quizId"/>).</summary>
        public string quizId;
        public string title = "Sistem Pencernaan";
        /// <summary>Kartu yang didapat dari kuis ini (null = tidak ada kartu baru).</summary>
        public string newCard;
        /// <summary>Organ reproduksi gender lain yang baru saja terbuka karena kuis ini.</summary>
        public string unlocked;
        int m_StartCards;
        bool m_Finished;

        /// <summary>Tanpa jenis: Season 1 (Pencernaan). Dengan jenis: latihan soal jenis itu dari semua season.</summary>
        public static QuizSession Start(QuizType? only = null)
        {
            if (!only.HasValue) return StartSeason("cerna");
            Current = new QuizSession
            {
                questions = QuizBank.Practice(only.Value), quizId = "latihan:" + only.Value,
                title = "Latihan " + QuizBank.TypeName(only.Value),
            };
            Current.m_StartCards = OrganoApp.Instance != null ? OrganoApp.Instance.Progress.cards.Count : 0;
            return Current;
        }

        public static QuizSession StartSeason(string id)
        {
            var info = QuizBank.GetSeason(id) ?? QuizBank.Seasons[1];
            Current = new QuizSession
            {
                seasonId = info.id, quizId = QuizBank.SeasonQuizId(info.id), title = info.title,
                system = info.system, questions = QuizBank.Season(info.id),
            };
            Current.m_StartCards = OrganoApp.Instance != null ? OrganoApp.Instance.Progress.cards.Count : 0;
            return Current;
        }

        /// <summary>Mulai lagi kuis yang sama dengan catatan riwayat ini.</summary>
        public static QuizSession Restart(QuizRecord r)
        {
            string id = r?.quizId ?? "";
            if (id.StartsWith("repro:")) return StartTopic(id);
            if (id.StartsWith("latihan:") && System.Enum.TryParse(id.Substring(8), out QuizType t)) return Start(t);
            return StartSeason(QuizBank.RecordSeason(r) ?? "cerna");
        }

        /// <summary>Mulai kuis lalu buka soal pertamanya.</summary>
        public static void Play(OrganoApp app, QuizSession s) { Current = s; OpenCurrent(app); }

        /// <summary>Mulai kuis topik (organ reproduksi). Topik gender lain yang masih terkunci tidak bisa dimulai.</summary>
        public static QuizSession StartTopic(string topic)
        {
            Current = new QuizSession
            {
                topic = topic, quizId = topic, questions = QuizBank.Topic(topic), title = QuizBank.TopicTitle(topic),
                system = OrganSystem.Reproduksi,
            };
            Current.m_StartCards = OrganoApp.Instance != null ? OrganoApp.Instance.Progress.cards.Count : 0;
            return Current;
        }

        /// <summary>Kartu baru yang didapat selama kuis ini (dari misi scan atau hadiah kuis topik).</summary>
        public bool GainedCard(PlayerProgress p) => newCard != null || p.cards.Count > m_StartCards;

        public void Answer(bool ok, string yours, string right)
        {
            var q = Q;
            answers.Add(new QuizAnswer { type = QuizBank.TypeName(q.type), question = q.prompt, yours = yours, right = right, explain = q.explain, isCorrect = ok });
            if (ok) OrganoAudio.Success(); else OrganoAudio.Wrong();
        }

        /// <summary>
        /// Lanjut ke soal berikutnya atau ke hasil. <paramref name="from"/> = nomor soal saat tombol ditekan:
        /// ketukan ganda pada tombol lama diabaikan (soal tidak terlewati, hasil tidak tercatat dua kali).
        /// </summary>
        public void Next(OrganoApp app, int from = -1)
        {
            if (m_Finished || (from >= 0 && from != index)) return;
            index++;
            if (index >= questions.Count) { Finish(app); return; }
            OpenCurrent(app, false);
        }

        public static void OpenCurrent(OrganoApp app, bool push = true)
        {
            var s = Current;
            if (s?.Q == null) return;
            if (s.Q.type == QuizType.MisiScan) app.Go<QuizScanScreen>(null, push);
            else app.Go<QuizPlayScreen>(s, push);
        }

        void Finish(OrganoApp app)
        {
            if (m_Finished) return;
            m_Finished = true;
            var p = app.Progress;
            int stars = Correct * 5;
            if (topic != null)
            {
                bool ownBefore = ReproAccess.OwnQuizDone(p);
                if (!p.doneTopics.Contains(topic)) p.doneTopics.Add(topic);
                string organ = ReproAccess.OrganOfTopic(topic);
                if (organ != null && p.AddCard(organ, 0)) newCard = organ;
                if (!ownBefore && ReproAccess.OwnQuizDone(p)) unlocked = ReproAccess.OtherId(p.gender);
            }
            result = new QuizRecord
            {
                system = OrganDefaults.SystemName(system),
                quizId = quizId ?? "", title = title,
                quizNumber = p.history.Count(h => h.quizId == quizId) + 1,
                correct = Correct, total = questions.Count, stars = stars,
                when = "Hari ini, " + System.DateTime.Now.ToString("HH.mm"),
                answers = new List<QuizAnswer>(answers)
            };
            p.history.Insert(0, result);
            p.stars += stars;
            // penghitung lama (dipakai versi sebelumnya) tetap diisi
            if (seasonId == "cerna" && p.season1Done < 5) p.season1Done++;
            if (seasonId == "tubuh" && p.warmupDone < 5) p.warmupDone++;
            p.Save();
            app.Go<QuizResultScreen>(this, false);
        }
    }

    // ==================================================================== Pusat kuis

    public class QuizHubScreen : AppScreen
    {
        public override NavTab Tab => NavTab.Quiz;

        protected override void Build(object arg)
        {
            var hist = U.IconButton("history", () => App.Go<QuizHistoryScreen>(), 22, "Riwayat");
            hist.style.marginRight = -12;
            Root.Add(Parts.TopBar("Quiz", () => App.GoTab(NavTab.Beranda), hist));
            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 8;
            Root.Add(scroll);
            var c = U.Col(20);
            scroll.Add(c);

            // Kuis berikutnya: season pertama yang belum diselesaikan (semua selesai: main lagi season dengan skor terendah)
            var next = P.NextSeason;
            var replay = next ?? QuizBank.Seasons.OrderBy(x => P.SeasonBest(x.id)).First();
            var cont = U.Col(6).Radius(18).Pad(16).Gradient(U.Pink, U.Purple, 135);
            cont.style.overflow = Overflow.Hidden;
            cont.Add(U.Icon("mascot_quiz", 118).Abs(null, null, -4, -6));
            cont.Add(U.Text(next != null ? next.season : "SEMUA SEASON SELESAI").FontSize(12).Bold().Fg(U.C("#FFFFFF", 0.9f)));
            var heroTitle = U.Text(next != null ? next.title : "Hebat, " + P.nickname + "!").FontSize(20).Bold().Fg("#FFFFFF").W(200);
            heroTitle.style.whiteSpace = WhiteSpace.Normal;
            cont.Add(heroTitle);
            cont.Add(U.Text(next != null ? "5 soal" : "Main lagi " + replay.title).FontSize(12).Fg("#FFFFFF"));
            var go = U.Row(6).Bg("#FFFFFF").Radius(999).Pad(9, 16).AlignSelf(Align.FlexStart);
            go.style.marginTop = 6;
            go.Add(U.Text(next != null ? "Mulai" : "Main Lagi").FontSize(13).Bold().Fg(U.Pink));
            go.Add(U.Icon("arrow_right", 14).Tinted(U.C(U.Pink)));
            go.Tap(() => QuizSession.Play(App, QuizSession.StartSeason(replay.id)));
            cont.Add(go);
            c.Add(cont);

            var last = P.history.FirstOrDefault();
            if (last != null)
            {
                var r = U.Row(12, "card", "card-lg");
                r.Add(Parts.PercentCircle(last.Percent));
                var t = U.Col(2).Grow();
                t.Add(U.Text(last.Title + " · " + last.correct + "/" + last.total + " benar", "t-small").Bold());
                t.Add(U.Text(last.when + " · +" + last.stars + " bintang", "t-tiny"));
                r.Add(t);
                r.Add(U.Text("Riwayat").FontSize(12).Bold().Fg(U.Pink));
                r.Tap(() => App.Go<QuizHistoryScreen>());
                c.Add(r);
            }

            var all = U.Col(10);
            var hdr = U.Row().Justify(Justify.SpaceBetween);
            hdr.Add(U.Text("Season", "t-section"));
            hdr.Add(U.Text(P.SeasonsDone + " dari " + QuizBank.Seasons.Count + " selesai", "t-caption"));
            all.Add(hdr);
            foreach (var s in QuizBank.Seasons)
            {
                bool complete = P.SeasonDone(s.id);
                int best = P.SeasonBest(s.id);
                var (fg, soft) = OrganDefaults.SystemColors(s.system);
                var card = U.Row(12).MinH(68).Radius(16).Pad(10, 14).Bg("#FFFFFF").Border(1, next == s ? "#F5B7C6" : U.Line);
                var ic = U.Div().Size(48, 48).Radius(14).Center().Bg(soft);
                ic.Add(U.Icon(s.icon, 26));
                card.Add(ic);
                var mid = U.Col(2).Grow();
                mid.Add(U.Text(s.season, "t-tiny").Bold());
                mid.Add(U.Text(s.title, "t-small").Bold());
                card.Add(mid);
                if (complete && best >= 0) card.Add(Score(best, 5));
                else if (complete) card.Add(U.Pill("Selesai", U.TealSoft, U.TealDark));
                else card.Add(U.Pill("Main", U.PinkSoft, U.PinkDark));
                var id = s.id;
                card.Tap(() => QuizSession.Play(App, QuizSession.StartSeason(id)));
                card.tooltip = s.title;
                all.Add(card);
            }
            c.Add(all);

            var types = U.Col(10);
            types.Add(U.Text("Latihan", "t-section"));
            var row = U.Row(10).Align(Align.Stretch);
            row.Add(TypeCard("Tempel Label", "Seret ke tubuh", "qt_label", U.TealSoft, QuizType.TempelLabel));
            row.Add(TypeCard("Pilihan Ganda 3D", "Putar & tebak", "qt_3d", U.PurpleSoft, QuizType.PilihanGanda3D));
            row.Add(TypeCard("Misi Scan", "Cari kartunya", "qt_scan", U.PinkSoft, QuizType.MisiScan));
            types.Add(row);
            c.Add(types);
            c.Add(ReproSection());
        }

        /// <summary>Kuis organ reproduksi: milik sendiri dulu, gender lain terbuka setelahnya.</summary>
        VisualElement ReproSection()
        {
            var sec = U.Col(10);
            var hdr = U.Row().Justify(Justify.SpaceBetween);
            hdr.Add(U.Text("Topik: Organ Reproduksi", "t-section"));
            hdr.Add(U.Text(P.doneTopics.Count(t => t.StartsWith("repro:")) + " dari 2 selesai", "t-caption"));
            sec.Add(hdr);
            string own = ReproAccess.OwnId(P.gender);
            var order = own == ReproAccess.Girl ? new[] { ReproAccess.Girl, ReproAccess.Boy } : new[] { ReproAccess.Boy, ReproAccess.Girl };
            var (fg, soft) = OrganDefaults.SystemColors(OrganSystem.Reproduksi);
            foreach (var id in order)
            {
                var organ = Db.Get(id);
                if (organ == null) continue;
                bool locked = ReproAccess.IsLocked(id, P), done = ReproAccess.QuizDone(P, id);
                var card = U.Row(12).MinH(76).Radius(16).Pad(12, 14).Bg(locked ? "#FBFAFD" : "#FFFFFF").Border(1, locked || done ? U.Line : "#E6C3E4");
                var ic = U.Div().Size(48, 48).Radius(14).Center().Bg(locked ? U.C("#F6F5F9") : soft);
                ic.Add(U.Icon(Parts.MiniIcon(organ), 30));
                card.Add(ic);
                var mid = U.Col(3).Grow();
                mid.Add(U.Text(id == own ? "ORGAN REPRODUKSIMU" : "ORGAN REPRODUKSI LAINNYA", "t-tiny").Bold());
                mid.Add(U.Text(ReproAccess.QuizName(id), "t-small").Bold().Fg(locked ? U.Faint : U.Ink));
                var sub = U.Text(locked ? (P.gender == Gender.None ? "Pilih tubuhmu dulu" : "Terbuka setelah " + ReproAccess.QuizName(own)) : done ? "Selesai · kartu didapat" : "5 soal · dapat kartu " + organ.displayName, "t-tiny");
                sub.style.whiteSpace = WhiteSpace.Normal;
                mid.Add(sub);
                card.Add(mid);
                if (locked) card.Add(U.Icon("lock_gray", 18));
                else if (done) card.Add(U.Pill("Ulangi", U.TealSoft, U.TealDark));
                else card.Add(U.Pill("Mulai", CardArt.Hex(soft), CardArt.Hex(fg)));
                var oo = organ;
                card.Tap(() =>
                {
                    if (locked) { Parts.ShowReproLock(App, oo); return; }
                    QuizSession.StartTopic(ReproAccess.Topic(oo.id));
                    QuizSession.OpenCurrent(App);
                });
                card.tooltip = ReproAccess.QuizName(id);
                sec.Add(card);
            }
            return sec;
        }

        VisualElement TypeCard(string title, string sub, string icon, string bg, QuizType type)
        {
            var t = U.Col(8).Grow().MinH(120).Radius(16).Bg("#FFFFFF").Border(1, U.Line).Pad(12, 8).Align(Align.Center);
            t.Elev(Elevation.E1);
            t.Add(U.Div().Size(48, 48).Radius(24).Bg(bg).Center().Kids(U.Icon(icon, 26)));
            t.Add(U.Text(title, "t-small").Bold().TextCenter());
            t.Add(U.Text(sub, "t-tiny").TextCenter());
            t.Tap(() => QuizSession.Play(App, QuizSession.Start(type)));
            return t;
        }

        /// <summary>Skor terbaik season: bintang kecil + "4/5".</summary>
        static VisualElement Score(int best, int total)
        {
            var s = U.Row(4).Pad(4, 10).Radius(999).Bg(best >= total ? U.TealSoft : U.YellowSoft);
            s.Add(U.Icon("stat_star", 12).Tinted(U.C(best >= total ? U.TealDark : U.Yellow)));
            s.Add(U.Text(best + "/" + total).FontSize(12).Bold().Fg(best >= total ? U.TealDark : "#7A5A00"));
            return s;
        }
    }

    // ==================================================================== Soal: Tempel Label & Pilihan Ganda 3D

    public class QuizPlayScreen : AppScreen
    {
        QuizSession S;
        int m_Picked = -1;
        bool m_Checked;
        string m_Holding;
        readonly Dictionary<string, string> m_Placed = new Dictionary<string, string>();

        static readonly Dictionary<string, Vector2> LabelSpots = new Dictionary<string, Vector2>
        {
            { "mulut", new Vector2(195, 62) }, { "kerongkongan", new Vector2(195, 108) },
            { "lambung", new Vector2(214, 195) }, { "usushalus", new Vector2(190, 245) },
            { "otak", new Vector2(195, 40) }, { "paru", new Vector2(182, 150) },
            { "hidung", new Vector2(195, 50) }, { "tenggorokan", new Vector2(195, 104) },
        };

        /// <summary>Organ ini punya titik tempel di gambar tubuh (soal tempel label).</summary>
        public static bool HasLabelSpot(string organId) => LabelSpots.ContainsKey(organId);

        protected override void Build(object arg)
        {
            if (arg is OrganSystem sys) QuizSession.StartSeason(QuizBank.SeasonFor(sys));
            else if (arg == null || QuizSession.Current == null) QuizSession.Start();
            S = QuizSession.Current;
            if (arg is QuizSession || arg is OrganSystem || arg == null) { m_Picked = -1; m_Checked = false; m_Placed.Clear(); m_Holding = null; }
            var q = S.Q;

            // Top bar + progres
            var top = U.Row(12).Pad(16, 20, 0, 20);
            var close = U.IconButton("close", () => Parts.ConfirmExitQuiz(App, () => App.GoTab(NavTab.Quiz)));
            close.style.marginLeft = -12;
            top.Add(close);
            top.Add(U.Text(S.title, "t-title").Grow().TextCenter());
            top.Add(U.Text((S.index + 1) + "/" + S.questions.Count, "t-small", "t-muted").Bold());
            Root.Add(top);
            var segs = U.Row(6).Pad(12, 20, 0, 20);
            for (int i = 0; i < S.questions.Count; i++)
                segs.Add(U.Div().H(6).Radius(3).Grow().Bg(i < S.index ? U.Teal : i == S.index ? U.Purple : "#E4E2EC"));
            Root.Add(segs);

            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 16;
            Root.Add(scroll);
            var c = U.Col(14);
            scroll.Add(c);
            var bottom = U.Col(8).Pad(12, 20, 20, 20);
            Root.Add(bottom);

            if (q.type == QuizType.TempelLabel) BuildLabel(q, c, bottom);
            else BuildChoice(q, c, bottom);
        }

        // ---------------------------------------------------------- Pilihan ganda 3D

        /// <summary>
        /// Soal pilihan ganda dibangun sekali. Memilih / memeriksa jawaban hanya mengganti warna baris dan tombol di tempat:
        /// model 3D (dan RenderTexture-nya) tidak dibuat ulang, jadi tidak berkedip setiap kali jawaban diketuk.
        /// </summary>
        void BuildChoice(QuizQuestion q, VisualElement c, VisualElement bottom)
        {
            // soal panjang (mis. soal keamanan tubuh) memakai huruf sedikit lebih kecil supaya pilihan jawaban tetap terlihat
            var prompt = U.Text(q.prompt, q.prompt.Length > 90 ? "t-title" : "t-h2");
            prompt.style.whiteSpace = WhiteSpace.Normal;
            var fixedTop = U.Col(12).Pad(16, 20, 4, 20).NoShrink();
            fixedTop.Add(prompt);
            var sv = c.GetFirstAncestorOfType<ScrollView>();
            Root.Insert(sv != null ? Root.IndexOf(sv) : Root.childCount, fixedTop);
            if (sv != null) sv.contentContainer.style.paddingTop = 8;

            var organ = Db.Get(q.organId);
            // HP layar pendek / soal panjang: kotak 3D lebih pendek supaya pilihan jawaban tetap terlihat.
            // Tingginya dihitung sekali dari tinggi layar (bukan mengikuti tata letak), jadi model tidak berubah ukuran.
            float screenH = App.ScreenHeight;
            float boxH = screenH > 0 ? Mathf.Round(Mathf.Clamp(screenH * 0.23f, 120f, 200f)) : 200f;
            var box = U.Div().H(boxH).Radius(18).Bg("#F3EFFD").Border(1, "#CDBFF5").NoShrink();
            box.style.overflow = Overflow.Hidden;
            var vp = new OrganViewport(App.Viewer, organ, Hints.Take(Hints.QuizRotate), "Geser untuk memutar") { ShowHotspots = false };
            vp.AddToClassList("fill");
            box.Add(vp);
            fixedTop.Add(box);

            // pilihan ganda: cukup ketuk salah satu jawaban (drag & drop hanya untuk soal tempel label)
            var rows = new List<(VisualElement row, VisualElement badge, Label letter, VisualElement mark)>();
            var check = U.Button("Periksa Jawaban", null, "btn-disabled");
            void Paint()
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    var (row, badge, letter, mark) = rows[i];
                    bool sel = m_Picked == i, right = i == q.correct;
                    string bg = "#FFFFFF", bd = "#E4E2EC", badgeBg = "#F6F5F9", badgeFg = U.Ink, markBg = null, icon = null;
                    float op = 1;
                    if (!m_Checked && sel) { bg = "#F3EFFD"; bd = U.Purple; badgeBg = U.Purple; badgeFg = "#FFFFFF"; markBg = U.Purple; icon = "check"; }
                    if (m_Checked && right) { bg = U.TealSoft; bd = U.Teal; badgeBg = U.Teal; badgeFg = "#FFFFFF"; markBg = U.Teal; icon = "check"; }
                    else if (m_Checked && sel) { bg = U.PinkSoft; bd = U.Pink; badgeBg = U.Pink; badgeFg = "#FFFFFF"; markBg = U.Pink; icon = "x_small"; }
                    else if (m_Checked) op = 0.55f;
                    row.Bg(bg).Border(1.5f, bd).Opacity(op);
                    badge.Bg(badgeBg);
                    letter.Fg(badgeFg);
                    mark.Clear();
                    mark.Visible(icon != null);
                    if (icon != null) { mark.Bg(markBg); mark.Add(U.Icon(icon, icon == "check" ? 12 : 11)); }
                }
                bool can = m_Picked >= 0 && !m_Checked;
                check.EnableInClassList("btn-primary", can);
                check.EnableInClassList("btn-disabled", !can);
            }

            var opts = U.Col(10);
            for (int i = 0; i < q.options.Length; i++)
            {
                int k = i;
                var row = U.Row(12).MinH(52).Pad(10, 14).Radius(14);
                var badge = U.Div().Size(28, 28).Radius(14).Center();
                var letter = U.Text("ABCD"[i].ToString()).FontSize(13).Bold();
                badge.Add(letter);
                row.Add(badge);
                row.Add(U.Text(q.options[i], "t-small").Bold().Grow());
                var mark = U.Div().Size(22, 22).Radius(11).Center();
                row.Add(mark);
                row.Tap(() => { if (m_Checked || m_Picked == k) return; m_Picked = k; Paint(); });
                rows.Add((row, badge, letter, mark));
                opts.Add(row);
            }
            c.Add(opts);

            check.Tap(() =>
            {
                if (m_Picked < 0 || m_Checked) return;
                m_Checked = true;
                S.Answer(m_Picked == q.correct, q.options[m_Picked], q.options[q.correct]);
                Paint();
                ShowFeedback(c, m_Picked == q.correct, q.explain);
                bottom.Clear();
                bottom.Add(NextButton());
            });
            bottom.Add(check);
            Paint();
            if (m_Checked) { ShowFeedback(c, m_Picked == q.correct, q.explain); bottom.Clear(); bottom.Add(NextButton()); }
        }

        /// <summary>Kotak koreksi ikut area gulir (bukan bar bawah) supaya penjelasan panjang tidak terjepit / keluar kotak.</summary>
        static void ShowFeedback(VisualElement c, bool ok, string explain)
        {
            var fb = U.Row(10).Align(Align.FlexStart).Radius(14).Pad(12).Bg(ok ? U.TealSoft : U.PinkSoft).Border(1, ok ? "#9CE6DC" : "#F5B8C6");
            fb.Add(U.Div().Size(26, 26).Radius(13).Bg(ok ? U.Teal : U.Pink).Center().NoShrink().Kids(U.Icon(ok ? "check" : "x_small", 13)));
            var ft = U.Col(2).Grow();
            ft.Add(U.Text(ok ? "Benar! Hebat!" : "Belum tepat, tidak apa-apa!", "t-body").Bold().Fg(ok ? U.TealInk : "#B0234A"));
            var ex = U.Text(explain, "t-caption").Fg(U.Ink);
            ex.style.whiteSpace = WhiteSpace.Normal;
            ft.Add(ex);
            fb.Add(ft);
            c.Add(fb);
            var sv = c.GetFirstAncestorOfType<ScrollView>();
            if (sv != null) fb.RegisterCallback<GeometryChangedEvent>(_ => sv.ScrollTo(fb));
        }

        VisualElement NextButton()
        {
            bool last = S.index >= S.questions.Count - 1;
            bool nextIsScan = !last && S.questions[S.index + 1].type == QuizType.MisiScan;
            int at = S.index;
            var b = U.Button(last ? "Lihat Hasil Kuis" : nextIsScan ? "Lanjut ke Misi Scan" : "Soal Berikutnya", () =>
            {
                // ketukan ganda: soal berikutnya tidak boleh ikut terlewati
                if (S.index != at) return;
                m_Picked = -1; m_Checked = false; m_Placed.Clear(); m_Holding = null;
                S.Next(App, at);
            }, "btn-primary");
            b.Add(U.Icon("arrow_right", 16));
            b[1].style.marginLeft = 8;
            return b;
        }

        // ---------------------------------------------------------- Tempel label

        void BuildLabel(QuizQuestion q, VisualElement c, VisualElement bottom)
        {
            var pill = U.Div().AlignSelf(Align.Center).Pad(7, 16).Radius(999).Bg(U.PurpleDark);
            pill.Add(U.Text(q.prompt).FontSize(13).Fg("#FFFFFF"));
            c.Add(pill);

            var stage = U.Div().Size(350, 330).AlignSelf(Align.Center);
            stage.style.overflow = Overflow.Hidden;
            var body = new SvgImage();
            body.SetMarkup(OrganoIcons.Fill("body_template", new Dictionary<string, string>
            {
                { "shirtFill", "#8EDDD3" }, { "shirtStroke", "#5FBFB2" }, { "shirtOp", "0.35" }, { "smileOp", "1" },
                { "headOp", "0" }, { "neckOp", "0" }, { "chestOp", "0" }, { "bellyOp", "0" }, { "labelOp", "0" },
                { "boyDisplay", P.gender == Gender.Girl ? "none" : "inline" }, { "girlDisplay", P.gender == Gender.Girl ? "inline" : "none" }, { "bodyAlt", "" }, { "hairOp", "1" }
            }));
            body.Abs(-20, -10).Size(390, 420);
            body.style.scale = new Scale(new Vector3(0.82f, 0.82f, 1));
            body.style.transformOrigin = new TransformOrigin(195, 0);
            stage.Add(body);

            var zones = new List<VisualElement>();
            foreach (var id in q.labelOrgans)
            {
                if (!LabelSpots.TryGetValue(id, out var spot)) spot = new Vector2(195, 210);
                var pos = new Vector2(-20 + 195 + (spot.x - 195) * 0.82f, -10 + spot.y * 0.82f);
                var placedOrgan = m_Placed.FirstOrDefault(kv => kv.Value == id).Key;
                var target = U.Div().Abs(pos.x - 22, pos.y - 22).Size(44, 44).Radius(22).Center();
                if (placedOrgan != null)
                {
                    var o = Db.Get(placedOrgan);
                    bool ok = !m_Checked || placedOrgan == id;
                    target.Bg("#FFFFFF").Border(2, m_Checked ? (ok ? U.Teal : U.Pink) : U.Purple);
                    target.Add(U.Icon(o.IconKey, 28));
                    var cap = U.Text(o.displayName).FontSize(10).Bold().Abs(-20, 44).W(84).TextCenter();
                    target.Add(cap);
                    if (!m_Checked) target.Tap(() => { m_Placed.Remove(placedOrgan); Refresh("keep"); });
                }
                else
                {
                    target.Bg(U.C("#FFFFFF", 0.8f)).Border(2, m_Holding != null ? U.Purple : "#BDBACE");
                    target.style.transitionProperty = new List<StylePropertyName> { "scale" };
                    target.style.transitionDuration = new List<TimeValue> { 0.12f };
                    target.Add(U.Text("?").Bold().Fg(U.Muted));
                    target.Tap(() =>
                    {
                        if (m_Holding == null || m_Checked) { App.Toast("Seret organ dari bawah ke titik ini"); return; }
                        m_Placed[m_Holding] = id;
                        m_Holding = null;
                        Refresh("keep");
                    });
                }
                stage.Add(target);
                zones.Add(target);
            }
            c.Add(stage);

            var tray = U.Col(10);
            var remaining = q.labelOrgans.Where(id => !m_Placed.ContainsKey(id)).ToList();
            var tiles = U.Row(10).Justify(Justify.Center);
            foreach (var id in remaining)
            {
                var o = Db.Get(id);
                bool hold = m_Holding == id;
                VisualElement Tile(bool on)
                {
                    var tile = U.Col(4).Size(80, 84).Radius(14).Center().Bg(on ? "#F3EFFD" : "#FFFFFF").Border(on ? 2 : 1, on ? U.Purple : U.Line);
                    tile.Add(U.Icon(o.IconKey, 36));
                    // nama panjang (Kerongkongan, Tenggorokan) tetap satu baris, tidak terpotong di tengah kata
                    var nm = U.Text(o.displayName).FontSize(9.5f).Bold().TextCenter();
                    nm.style.whiteSpace = WhiteSpace.NoWrap;
                    nm.style.marginLeft = nm.style.marginRight = 0;
                    nm.style.paddingLeft = nm.style.paddingRight = 0;
                    tile.Add(nm);
                    return tile;
                }
                var t = Tile(hold);
                if (!m_Checked)
                {
                    var drag = new DragSource(Root, () => Tile(true), () => zones, z =>
                    {
                        if (z < 0) return;
                        var spotId = q.labelOrgans[z];
                        foreach (var kv in m_Placed.Where(kv => kv.Value == spotId).ToList()) m_Placed.Remove(kv.Key);
                        m_Placed[id] = spotId;
                        m_Holding = null;
                        Refresh("keep");
                    });
                    drag.Tapped = () => { m_Holding = hold ? null : id; Refresh("keep"); };
                    t.AddManipulator(drag);
                    t.tooltip = "Seret " + o.displayName + " ke tubuh";
                }
                tiles.Add(t);
            }
            tray.Add(tiles);
            c.Add(tray);

            int placed = m_Placed.Count, total = q.labelOrgans.Length;
            if (!m_Checked)
            {
                var info = U.Row().Justify(Justify.SpaceBetween);
                info.Add(U.Text(m_Holding != null ? "Sekarang ketuk titik di tubuh" : "", "t-caption"));
                info.Add(U.Text(placed + "/" + total, "t-caption").Bold());
                bottom.Add(info);
                bool can = placed == total;
                bottom.Add(U.Button("Periksa Jawaban", () =>
                {
                    if (!can) return;
                    m_Checked = true;
                    int right = m_Placed.Count(kv => kv.Key == kv.Value);
                    var wrongs = m_Placed.Where(kv => kv.Key != kv.Value).Select(kv => Db.Get(kv.Key).displayName).ToList();
                    S.Answer(right == total, wrongs.Count == 0 ? "Semua tepat" : "Tertukar: " + string.Join(", ", wrongs),
                        !string.IsNullOrEmpty(q.answer) ? q.answer : string.Join(", ", q.labelOrgans.Select(x => Db.Get(x)?.displayName ?? x)));
                    Refresh("keep");
                }, can ? "btn-primary" : "btn-disabled"));
            }
            else
            {
                int right = m_Placed.Count(kv => kv.Key == kv.Value);
                var fb = U.Row(10).Radius(14).Pad(12).Bg(right == total ? U.TealSoft : U.PinkSoft);
                fb.Add(U.Text(right == total ? "Benar semua! " + q.explain : right + " dari " + total + " tepat. " + q.explain, "t-caption").Fg(U.Ink).Grow());
                bottom.Add(fb);
                bottom.Add(NextButton());
            }
        }

        public override bool OnBack()
        {
            Parts.ConfirmExitQuiz(App, () => App.GoTab(NavTab.Quiz));
            return true;
        }
    }

    // ==================================================================== Soal: Misi Scan

    public class QuizScanScreen : AppScreen
    {
        public override bool UsesCamera => true;
        enum State { Idle, Wrong, Right }
        State m_State;
        string m_WrongId;
        VisualElement m_StandIn, m_Bottom, m_Stage, m_WrongToast;
        CardFrame m_Frame;
        QuizSession S;

        protected override void Build(object arg)
        {
            S = QuizSession.Current ?? QuizSession.Start(QuizType.MisiScan);
            m_State = State.Idle;
            App.ar?.ResetDetection();
            if (App.ar != null) { App.ar.CardDetected -= OnCard; App.ar.CardDetected += OnCard; }
            var q = S.Q;

            m_StandIn = ScanParts.CameraStandIn();
            Root.Add(m_StandIn);
            // bingkai besar berbentuk kartu (ARCore butuh kartu >= 25% gambar kamera), di antara misi & tombol bawah
            m_Frame = new CardFrame();
            m_Frame.Box.Tap(() =>
            {
                // Demo tanpa kamera: ketuk pertama = kartu salah, ketuk berikutnya = kartu benar
                if (!OrganoApp.DevTools || (App.ar != null && App.ar.IsCameraLive && App.ar.HasCardLibrary)) return;
                OnCard(Db.Get(m_State == State.Idle ? "paru" : S.Q.organId));
            });
            Root.Add(m_Frame);
            var top = U.Row(12).Pad(16 + App.TopInset, 20, 0, 20);
            top.Add(ScanParts.GlassButton("close", () => Parts.ConfirmExitQuiz(App, () => App.GoTab(NavTab.Quiz)), "Keluar"));
            top.Add(U.Flex());
            var step = U.Div().Pad(8, 14).Radius(999).Bg(U.C("#141220", 0.55f));
            step.Add(U.Text((S.index + 1) + "/" + S.questions.Count).FontSize(13).Bold().Fg("#FFFFFF"));
            top.Add(step);
            Root.Add(top);

            var mission = U.Col(6).Margin(14, 20, 0, 20).Bg("#FFFFFF").Radius(16).Pad(14, 16);
            var prompt = U.Text(q.prompt, "t-title");
            prompt.style.whiteSpace = WhiteSpace.Normal;
            mission.Add(prompt);
            var hint = U.Row(6);
            hint.Add(U.Icon("bulb", 16).Tinted(U.C("#B07D00")));
            hint.Add(U.Text(q.hint, "t-caption").Grow());
            mission.Add(hint);
            Root.Add(mission);

            m_Stage = U.Div().Grow().Center();
            m_Stage.pickingMode = PickingMode.Ignore;
            Root.Add(m_Stage);
            m_Bottom = U.Col(8).Pad(0, 20, 20, 20);
            Root.Add(m_Bottom);
            m_Frame.TopAnchor = mission;
            m_Frame.BottomAnchor = m_Bottom;
            Render();
        }

        void Render()
        {
            var q = S.Q;
            m_Stage.Clear();
            m_Bottom.Clear();
            m_WrongToast?.RemoveFromHierarchy();
            m_WrongToast = null;
            m_Frame.Visible(m_State != State.Right);
            m_Frame.Tint = U.C(m_State == State.Wrong ? U.Pink : U.Teal);
            m_Frame.SweepVisible = m_State == State.Idle;
            if (m_State == State.Right)
            {
                var organ = Db.Get(q.organId);
                var vp = new OrganViewport(App.Viewer, organ, false) { ShowHotspots = false, FreeRotate = true };
                vp.Size(300, 260);
                m_Stage.Add(vp);
                var sheet = U.Col(8).Bg("#FFFFFF").Radius(20).Pad(16);
                var h = U.Row(8);
                h.Add(U.Div().Size(26, 26).Radius(13).Bg(U.Teal).Center().Kids(U.Icon("check", 13)));
                h.Add(U.Text("Benar, itu " + organ.displayName + "!", "t-h2").Grow());
                h.Add(U.Pill("+10 bintang", U.YellowSoft, "#7A5A00"));
                sheet.Add(h);
                sheet.Add(U.Text(q.explain, "t-small"));
                int at = S.index;
                sheet.Add(U.Button(S.index >= S.questions.Count - 1 ? "Lihat Hasil Kuis" : "Soal Berikutnya", () => S.Next(App, at), "btn-primary"));
                m_Bottom.Add(sheet);
                return;
            }

            // petunjuk di dalam bingkai (tidak memakan ruang) supaya bingkai tetap besar di HP layar pendek
            m_Frame.Hint = m_State == State.Idle ? "Dekatkan HP sampai kartu memenuhi bingkai" : null;
            if (m_State == State.Wrong)
            {
                var wrong = Db.Get(m_WrongId);
                var toast = U.Row(12).Bg("#FFFFFF").Radius(16).Pad(12, 14).Align(Align.FlexStart);
                // melayang di atas bagian bawah bingkai: ukuran bingkai (dan jarak scan) tidak berubah
                toast.style.position = Position.Absolute;
                toast.style.left = 20; toast.style.right = 20; toast.style.bottom = 70;
                var mini = U.Div().Size(46, 46).Radius(12).Bg(U.PinkSoft).Border(2, U.Pink).Center();
                mini.Add(U.Icon(wrong.IconKey, 30));
                toast.Add(mini);
                var tc = U.Col(4).Grow();
                tc.Add(U.Text("Itu kartu " + wrong.displayName, "t-title").Fg("#B0234A"));
                var tx = U.Text(wrong.function + " Coba cari kartu lain, ya!", "t-caption").Fg(U.Ink);
                tx.style.whiteSpace = WhiteSpace.Normal;
                tc.Add(tx);
                toast.Add(tc);
                Root.Add(toast);
                m_WrongToast = toast;
            }
            // kartunya tidak ada (juga setelah salah kartu): soal boleh dilewati
            var help = U.Col(6).Align(Align.Center);
            var skip = U.Row(0).Pad(8, 14).Radius(999).Bg(U.C("#141220", 0.6f));
            skip.Add(U.Text("Belum punya kartunya?").FontSize(12).Fg("#FFFFFF").Pad(0, 4, 0, 0));
            int from = S.index;
            skip.Add(U.Text("Lewati soal").FontSize(12).Bold().Fg("#9CE6DC").Tap(() =>
            {
                if (S.index != from || m_State == State.Right) return;
                S.Answer(false, "Dilewati", Db.Get(q.organId).displayName);
                S.Next(App, from);
            }));
            help.Add(skip);
            m_Bottom.Add(help);
        }

        void OnCard(OrganInfo o)
        {
            if (o == null || m_State == State.Right) return;
            // kartu salah yang sama masih di depan kamera: jangan berbunyi & berkedip berulang-ulang
            if (m_State == State.Wrong && o.id == m_WrongId) { App.ar?.ResetDetection(); return; }
            var q = S.Q;
            if (o.id == q.organId)
            {
                m_State = State.Right;
                S.Answer(true, o.displayName, o.displayName);
                App.Progress.stars += 10;
                App.Progress.AddCard(o.id, 0);
            }
            else
            {
                m_State = State.Wrong;
                m_WrongId = o.id;
                OrganoAudio.Wrong();
                App.ar?.ResetDetection();
            }
            Render();
        }

        public override void Tick(float dt)
        {
            m_StandIn.Visible(!(App.ar != null && App.ar.IsCameraLive));
            m_Frame.Tick(dt);
        }

        public override void Close()
        {
            if (App.ar != null) App.ar.CardDetected -= OnCard;
        }

        public override bool OnBack()
        {
            Parts.ConfirmExitQuiz(App, () => App.GoTab(NavTab.Quiz));
            return true;
        }
    }

    // ==================================================================== Hasil kuis

    public class QuizResultScreen : AppScreen
    {
        float m_T;
        VisualElement m_Confetti;

        protected override void Build(object arg)
        {
            var s = arg as QuizSession ?? QuizSession.Current;
            var r = s?.result ?? P.history.FirstOrDefault() ?? new QuizRecord();
            m_T = 0;
            m_Confetti = U.Div("fill");
            m_Confetti.pickingMode = PickingMode.Ignore;
            var colors = new[] { U.Teal, U.Purple, U.Pink, U.Yellow };
            var rnd = new System.Random(7);
            for (int i = 0; i < 28; i++)
            {
                var p = U.Div().Size(8, 12).Radius(2).Bg(colors[i % 4]).Abs(rnd.Next(0, 380), -20 - rnd.Next(0, 300));
                p.userData = new Vector3(rnd.Next(60, 140), rnd.Next(-30, 30), rnd.Next(0, 360));
                m_Confetti.Add(p);
            }
            Root.Add(m_Confetti);

            Root.Add(Parts.TopBar("Hasil Kuis", () => App.GoTab(NavTab.Quiz)));
            var scroll = U.Scroll();
            Root.Add(scroll);
            var c = U.Col(16).Align(Align.Center);
            scroll.Add(c);
            c.Add(U.Icon("mascot_cheer", 170, 150));
            c.Add(U.Text("Kuis Selesai!", "t-display").TextCenter());
            c.Add(U.Pill(r.Title, U.TealDark, "#FFFFFF").AlignSelf(Align.Center));

            var score = U.Col(12, "card", "card-lg").AlignSelf(Align.Stretch).Pad(16, 24).Align(Align.Center);
            score.Add(U.Text(r.correct + " dari " + r.total + " Benar", "t-h1"));
            var dots = U.Row(6);
            for (int i = 0; i < r.total; i++)
            {
                bool ok = i < r.answers.Count ? r.answers[i].isCorrect : i < r.correct;
                dots.Add(U.Div().Size(28, 28).Radius(14).Bg(ok ? U.Teal : U.Pink).Center().Kids(U.Icon(ok ? "check" : "x_small", 13)));
            }
            score.Add(dots);
            c.Add(score);

            var stars = U.Col(2).Bg(U.YellowSoft).Radius(16).Pad(12, 28).Align(Align.Center);
            var sr = U.Row(6);
            sr.Add(U.Icon("star_big", 30));
            sr.Add(U.Text("+" + r.stars).FontSize(24).Bold().Fg("#7A5A00"));
            stars.Add(sr);
            stars.Add(U.Text("Total bintangmu: " + P.stars, "t-caption").Fg("#7A5A00"));
            c.Add(stars);

            if (s != null && s.unlocked != null && Db.Get(s.unlocked) is OrganInfo opened)
            {
                var un = U.Row(12).AlignSelf(Align.Stretch).Radius(16).Pad(12, 14).Bg("#F6E5F5").Border(1, "#E6C3E4");
                un.Add(U.Div().Size(44, 44).Radius(22).Bg("#FFFFFF").Center().Kids(U.Icon(Parts.MiniIcon(opened), 30)));
                var ut = U.Col(4).Grow();
                ut.Add(U.Text(opened.displayName + " sekarang terbuka!", "t-body").Bold());
                ut.Add(U.Text("Pelajari sekarang").FontSize(13).Bold().Fg("#A2479E").Tap(() => App.Go<OrganDetailScreen>(opened.id)));
                un.Add(ut);
                c.Add(un);
            }

            var newest = s != null && s.newCard != null ? Db.Get(s.newCard) : P.newestCard != null ? Db.Get(P.newestCard) : null;
            if (newest != null && s != null && s.GainedCard(P))
            {
                var unlock = U.Col(14).Align(Align.Center);
                unlock.Add(U.Text("Kamu dapat kartu baru!", "t-section"));
                var card = Parts.OrganCard(newest, true, 140);
                card.Tap(() => App.Go<CardViewerScreen>(newest.id));
                unlock.Add(card);
                unlock.Add(U.Text("Lihat di Koleksi Kartu (" + P.cards.Count + "/" + Db.Count + ")").FontSize(13).Bold().Fg(U.TealDark).Tap(() => App.Go<CardCollectionScreen>(newest.id)));
                c.Add(unlock);
            }

            var bottom = U.Col(6).Pad(0, 20, 20, 20);
            bottom.Add(U.Button("Lanjut Belajar", () => App.GoTab(NavTab.Beranda), "btn-primary"));
            var two = U.Row(10);
            two.Add(U.Button("Lihat Riwayat", () => App.Go<QuizHistoryScreen>(), "btn-ghost").Grow());
            two.Add(U.Button("Lihat Pembahasan", () => App.Go<QuizReviewScreen>(r), "btn-ghost").Grow());
            bottom.Add(two);
            Root.Add(bottom);
        }

        public override void Tick(float dt)
        {
            m_T += dt;
            foreach (var p in m_Confetti.Children())
            {
                var v = (Vector3)p.userData;
                p.style.top = p.style.top.value.value + v.x * dt;
                p.style.left = p.style.left.value.value + Mathf.Sin(m_T * 2 + v.z) * v.y * dt;
                p.style.rotate = new Rotate(v.z + m_T * 180);
                if (m_T > 4) p.style.opacity = Mathf.Clamp01(1 - (m_T - 4));
            }
        }
    }

    // ==================================================================== Pembahasan

    public class QuizReviewScreen : AppScreen
    {
        QuizRecord m_R;
        int m_Filter;

        protected override void Build(object arg)
        {
            if (arg is QuizRecord r) m_R = r;
            if (m_R == null) m_R = P.history.FirstOrDefault() ?? new QuizRecord();
            if (arg is int f) m_Filter = f;
            Root.Add(Parts.TopBar("Pembahasan", App.Back));
            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 8;
            Root.Add(scroll);
            var c = U.Col(14);
            scroll.Add(c);

            var sum = U.Row(12, "card", "card-lg");
            sum.Add(Parts.PercentCircle(m_R.Percent));
            var st = U.Col(2).Grow();
            st.Add(U.Text(m_R.Title, "t-body").Bold());
            st.Add(U.Text(m_R.correct + " dari " + m_R.total + " benar · " + m_R.when, "t-caption"));
            sum.Add(st);
            c.Add(sum);

            var chips = U.Row();
            string[] labels = { "Semua", "Benar", "Salah" };
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

            var answers = m_R.answers;
            if (answers.Count == 0) c.Add(U.Text("Pembahasan tidak tersedia untuk kuis ini.", "t-small", "t-muted").TextCenter());
            for (int i = 0; i < answers.Count; i++)
            {
                var a = answers[i];
                if (m_Filter == 1 && !a.isCorrect || m_Filter == 2 && a.isCorrect) continue;
                var card = U.Col(8, "card", "card-lg");
                var h = U.Row(8);
                h.Add(U.Div().Size(26, 26).Radius(13).Bg(a.isCorrect ? U.Teal : U.Pink).Center().Kids(U.Text((i + 1).ToString()).FontSize(12).Bold().Fg("#FFFFFF")));
                h.Add(U.Text(a.type, "t-tiny").Bold().Grow());
                h.Add(U.Pill(a.isCorrect ? "Benar" : "Salah", a.isCorrect ? U.TealSoft : U.PinkSoft, a.isCorrect ? U.TealInk : "#B0234A"));
                card.Add(h);
                card.Add(U.Text(a.question, "t-body").Bold());
                card.Add(U.Text("<b>Jawabanmu:</b> " + a.yours, "t-small").Fg(a.isCorrect ? U.TealInk : "#B0234A"));
                if (!a.isCorrect)
                {
                    var right = U.Div().Radius(10).Bg(U.TealSoft).Pad(8, 10);
                    right.Add(U.Text("<b>Jawaban benar:</b> " + a.right, "t-small").Fg(U.TealInk));
                    card.Add(right);
                }
                card.Add(U.Text(a.explain, "t-caption"));
                c.Add(card);
            }

            var bottom = U.Row(10).Pad(12, 20, 20, 20);
            var rec = m_R;
            bottom.Add(U.Button("Ulangi Kuis", () => QuizSession.Play(App, QuizSession.Restart(rec)), "btn-outline").Grow());
            bottom.Add(U.Button("Selesai", () => App.GoTab(NavTab.Quiz), "btn-primary").Grow());
            Root.Add(bottom);
        }

    }

    // ==================================================================== Riwayat

    public class QuizHistoryScreen : AppScreen
    {
        protected override void Build(object arg)
        {
            Root.Add(Parts.TopBar("Riwayat Quiz", App.Back));
            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 8;
            Root.Add(scroll);
            var c = U.Col(14);
            scroll.Add(c);

            var stats = U.Row(10);
            stats.Add(Stat(P.history.Count.ToString(), "Kuis selesai"));
            stats.Add(Stat(Mathf.RoundToInt(P.AveragePercent) + "%", "Rata-rata benar"));
            stats.Add(Stat(P.history.Sum(h => h.stars).ToString(), "Bintang didapat"));
            c.Add(stats);

            if (P.history.Count == 0)
            {
                c.Add(U.Text("Belum ada kuis yang selesai.", "t-small", "t-muted").TextCenter());
                c.Add(U.Button("Mulai Kuis", () => App.GoTab(NavTab.Quiz), "btn-primary"));
            }

            foreach (var h in P.history)
            {
                var card = U.Col(10, "card", "card-lg");
                var row = U.Row(12);
                row.Add(Parts.PercentCircle(h.Percent));
                var t = U.Col(2).Grow();
                t.Add(U.Text(h.Title, "t-small").Bold());
                t.Add(U.Text(h.when + " · " + h.correct + "/" + h.total + " benar", "t-tiny"));
                row.Add(t);
                var s = U.Row(3).Pad(4, 8).Radius(999).Bg(U.YellowSoft);
                s.Add(U.Icon("stat_star", 12).Tinted(U.C(U.Yellow)));
                s.Add(U.Text("+" + h.stars).FontSize(11).Bold().Fg("#7A5A00"));
                row.Add(s);
                card.Add(row);
                var btns = U.Row(8);
                var rec = h;
                if (rec.answers.Count > 0) btns.Add(U.Button("Lihat pembahasan", () => App.Go<QuizReviewScreen>(rec), "btn-soft", "btn-small").Grow());
                btns.Add(U.Button("Ulangi kuis ini", () => QuizSession.Play(App, QuizSession.Restart(rec)), "btn-outline", "btn-small").Grow());
                card.Add(btns);
                c.Add(card);
            }

        }

        static VisualElement Stat(string value, string label)
        {
            var s = U.Col(2, "card").Grow().Align(Align.Center).Pad(10, 6);
            s.Add(U.Text(value, "t-h2"));
            s.Add(U.Text(label, "t-tiny").TextCenter());
            return s;
        }
    }
}
