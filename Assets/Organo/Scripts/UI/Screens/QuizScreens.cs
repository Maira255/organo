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
    /// <summary>Kuis yang sedang dimainkan (kuis organ atau kuis proses).</summary>
    public class QuizSession
    {
        public static QuizSession Current;
        public OrganSystem system = OrganSystem.Pencernaan;
        public List<QuizQuestion> questions;
        public int index;
        public readonly List<QuizAnswer> answers = new List<QuizAnswer>();
        /// <summary>Soal yang benar pada percobaan pertama. Soal yang salah harus diulang sampai benar, tetapi tidak dihitung benar.</summary>
        public int Correct => answers.Count(a => a.isCorrect);
        public QuizQuestion Q => index < questions.Count ? questions[index] : null;
        public QuizRecord result;
        /// <summary>Kuis organ (mis. "lambung"); null = kuis proses.</summary>
        public string organId;
        /// <summary>Kuis proses (mis. "napas"); null = kuis organ.</summary>
        public string processId;
        /// <summary>Id untuk riwayat & "Ulangi kuis" (lihat <see cref="QuizRecord.quizId"/>).</summary>
        public string quizId;
        public string title = "Proses Pencernaan";
        /// <summary>Organ reproduksi gender lain yang baru saja terbuka karena kuis ini.</summary>
        public string unlocked;
        /// <summary>Kuis yang baru terbuka karena kuis ini selesai ("organ:..." / "season:...").</summary>
        public readonly List<string> opened = new List<string>();
        int m_StartCards;
        bool m_Finished;

        public static QuizSession StartOrgan(string organId)
        {
            var o = OrganoApp.Instance != null ? OrganoApp.Instance.Database.Get(organId) : null;
            Current = Make(QuizBank.Organ(organId), QuizBank.OrganQuizId(organId), QuizBank.OrganQuizTitle(o), o != null ? o.system : OrganSystem.Pencernaan);
            Current.organId = organId;
            return Current;
        }

        public static QuizSession StartProcess(string id)
        {
            var info = QuizBank.GetProcess(id) ?? QuizBank.Processes[0];
            Current = Make(QuizBank.Process(info.id), QuizBank.ProcessQuizId(info.id), info.title, info.system);
            Current.processId = info.id;
            return Current;
        }

        static QuizSession Make(List<QuizQuestion> qs, string quizId, string title, OrganSystem sys)
        {
            var s = new QuizSession { questions = qs, quizId = quizId, title = title, system = sys };
            foreach (var q in qs) Shuffle(q);
            s.m_StartCards = OrganoApp.Instance != null ? OrganoApp.Instance.Progress.cards.Count : 0;
            return s;
        }

        /// <summary>Acak urutan pilihan jawaban supaya jawaban benar tidak selalu A (pilihan berupa angka tetap berurutan).</summary>
        static void Shuffle(QuizQuestion q)
        {
            if (q.type != QuizType.PilihanGanda3D || q.options == null || q.options.Length < 2) return;
            if (q.options.All(o => int.TryParse(o, out _))) return;
            var right = q.options[q.correct];
            var rnd = new System.Random();
            q.options = q.options.OrderBy(_ => rnd.Next()).ToArray();
            q.correct = System.Array.IndexOf(q.options, right);
        }

        /// <summary>Mulai lagi kuis yang sama dengan catatan riwayat ini (null = kuis lama yang sudah tidak ada).</summary>
        public static QuizSession Restart(QuizRecord r)
        {
            var organ = QuizBank.OrganOfRecord(r);
            if (organ != null) return StartOrgan(organ);
            var process = QuizBank.ProcessOfRecord(r);
            return process != null && QuizBank.GetProcess(process) != null ? StartProcess(process) : null;
        }

        /// <summary>Mulai kuis lalu buka soal pertamanya (null = kembali ke halaman Kuis).</summary>
        public static void Play(OrganoApp app, QuizSession s)
        {
            if (s == null) { app.GoTab(NavTab.Quiz); return; }
            Current = s;
            OpenCurrent(app);
        }

        /// <summary>Kartu baru yang didapat selama kuis ini (dari misi scan).</summary>
        public bool GainedCard(PlayerProgress p) => p.cards.Count > m_StartCards;

        /// <summary>Catat jawaban percobaan pertama (benar/salah) dan bunyikan umpan baliknya.</summary>
        public void Answer(bool ok, string yours, string right)
        {
            var q = Q;
            answers.Add(new QuizAnswer { type = QuizBank.TypeName(q.type), question = q.prompt, yours = yours, right = right, explain = q.explain, isCorrect = ok });
            Sound(ok);
        }

        /// <summary>Bunyi benar/salah untuk percobaan berikutnya (tidak dicatat lagi).</summary>
        public static void Sound(bool ok) { if (ok) OrganoAudio.Success(); else OrganoAudio.Wrong(); }

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
            var before = QuizAccess.UnlockedIds(p);
            int stars = Correct * 5;
            if (organId != null)
            {
                bool ownBefore = ReproAccess.OwnQuizDone(p);
                var key = QuizBank.OrganQuizId(organId);
                if (!p.doneTopics.Contains(key)) p.doneTopics.Add(key);
                if (ReproAccess.IsRepro(organId))
                {
                    var topic = ReproAccess.Topic(organId);
                    if (!p.doneTopics.Contains(topic)) p.doneTopics.Add(topic);
                    if (!ownBefore && ReproAccess.OwnQuizDone(p)) unlocked = ReproAccess.OtherId(p.gender);
                }
            }
            result = new QuizRecord
            {
                system = OrganDefaults.SystemName(system),
                quizId = quizId ?? "", title = title,
                quizNumber = p.history.Count(h => h.quizId == quizId) + 1,
                correct = Correct, total = questions.Count, stars = stars,
                when = "Hari ini, " + System.DateTime.Now.ToString("HH.mm"),
                waktu = System.DateTime.Now.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                answers = new List<QuizAnswer>(answers)
            };
            p.history.Insert(0, result);
            p.stars += stars;
            foreach (var id in QuizAccess.UnlockedIds(p)) if (!before.Contains(id)) opened.Add(id);
            p.Save();
            app.Go<QuizResultScreen>(this, false);
        }
    }

    /// <summary>Lembar "kuis masih terkunci": cara membukanya (scan kartu / selesaikan kuis sebelumnya).</summary>
    public static class QuizLocks
    {
        public static string OrganName(OrganoApp app, string id) => app.Database.Get(id)?.displayName ?? id;

        /// <summary>Kalimat singkat cara membuka kuis organ, mis. "Scan kartu Lambung atau selesaikan Kuis Kerongkongan".</summary>
        public static string OrganReason(OrganoApp app, string organId)
        {
            var p = app.Progress;
            if (ReproAccess.IsLocked(organId, p)) return ReproAccess.LockReason(organId, p);
            var prev = QuizAccess.Previous(p, organId);
            string scan = "Scan kartu " + OrganName(app, organId);
            return prev != null ? scan + " atau selesaikan Kuis " + OrganName(app, prev) + "." : scan + ".";
        }

        public static void ShowOrgan(OrganoApp app, string organId)
        {
            var p = app.Progress;
            var c = U.Col(14);
            var head = U.Col(8).Align(Align.Center);
            head.Add(U.Div().Size(64, 64).Radius(32).Bg(U.PurpleSoft).Center().Kids(U.Icon("lock_big", 30)));
            head.Add(U.Text("Kuis " + OrganName(app, organId) + " masih terkunci", "t-h2").TextCenter());
            var why = U.Text(OrganReason(app, organId), "t-small", "t-muted").TextCenter();
            why.style.whiteSpace = WhiteSpace.Normal;
            head.Add(why);
            c.Add(head);
            OrganoApp.Overlay sh = null;
            if (ReproAccess.IsLocked(organId, p))
            {
                string own = ReproAccess.OwnId(p.gender);
                if (own == null) c.Add(U.Button("Pilih tubuhku", () => { sh.Close(); app.Go<GenderScreen>("profile"); }, "btn-primary"));
                else if (QuizAccess.OrganUnlocked(p, own))
                    c.Add(U.Button("Mulai Kuis " + OrganName(app, own), () => { sh.Close(); QuizSession.Play(app, QuizSession.StartOrgan(own)); }, "btn-primary"));
            }
            else
            {
                var scan = U.Button("Scan Kartu " + OrganName(app, organId), () => { sh.Close(); app.Go<ScanScreen>(organId); }, "btn-primary");
                scan.Insert(0, U.Icon("scan_small", 16).Tinted(Color.white)); scan[1].style.marginLeft = 8;
                c.Add(scan);
                var prev = QuizAccess.Previous(p, organId);
                if (prev != null && QuizAccess.OrganUnlocked(p, prev))
                    c.Add(U.Button("Mulai Kuis " + OrganName(app, prev), () => { sh.Close(); QuizSession.Play(app, QuizSession.StartOrgan(prev)); }, "btn-outline"));
            }
            c.Add(U.Button("Nanti saja", () => sh.Close(), "btn-ghost"));
            sh = app.ShowSheet(c);
        }

        public static void ShowProcess(OrganoApp app, ProcessInfo pr)
        {
            var p = app.Progress;
            var missing = QuizAccess.ProcessMissing(p, pr);
            var c = U.Col(12);
            var head = U.Col(8).Align(Align.Center);
            head.Add(U.Div().Size(64, 64).Radius(32).Bg(U.PurpleSoft).Center().Kids(U.Icon("lock_big", 30)));
            head.Add(U.Text(pr.title + " masih terkunci", "t-h2").TextCenter());
            head.Add(U.Text("Selesaikan dulu kuis organ berikut:", "t-small", "t-muted").TextCenter());
            c.Add(head);
            var list = U.Col(6);
            foreach (var id in pr.organs)
            {
                bool done = QuizAccess.OrganDone(p, id);
                var r = U.Row(10).Pad(8, 12).Radius(12).Bg(done ? U.TealSoft : "#F6F5F9");
                r.Add(U.Icon(done ? "check_teal" : "lock_gray", 16));
                r.Add(U.Text("Kuis " + OrganName(app, id), "t-small").Bold().Grow());
                r.Add(U.Text(done ? "Selesai" : QuizAccess.OrganUnlocked(p, id) ? "Terbuka" : "Terkunci", "t-tiny"));
                list.Add(r);
            }
            c.Add(list);
            OrganoApp.Overlay sh = null;
            var first = missing.FirstOrDefault(o => QuizAccess.OrganUnlocked(p, o));
            if (first != null)
                c.Add(U.Button("Mulai Kuis " + OrganName(app, first), () => { sh.Close(); QuizSession.Play(app, QuizSession.StartOrgan(first)); }, "btn-primary"));
            c.Add(U.Button("Nanti saja", () => sh.Close(), "btn-ghost"));
            sh = app.ShowSheet(c);
        }

        /// <summary>Mulai kuis organ, atau tampilkan cara membukanya kalau masih terkunci.</summary>
        public static void PlayOrgan(OrganoApp app, string organId)
        {
            if (QuizAccess.OrganUnlocked(app.Progress, organId)) QuizSession.Play(app, QuizSession.StartOrgan(organId));
            else ShowOrgan(app, organId);
        }
    }

    // ==================================================================== Pusat kuis

    public class QuizHubScreen : AppScreen
    {
        public override NavTab Tab => NavTab.Quiz;
        /// <summary>0 = kuis organ, 1 = kuis proses.</summary>
        int m_Tab;

        protected override void Build(object arg)
        {
            if (arg is int tab) m_Tab = tab;
            var hist = U.IconButton("history", () => App.Go<QuizHistoryScreen>(), 22, "Riwayat");
            hist.style.marginRight = -12;
            Root.Add(Parts.TopBar("Kuis", () => App.GoTab(NavTab.Beranda), hist));
            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 8;
            Root.Add(scroll);
            var c = U.Col(18);
            scroll.Add(c);

            // Kuis berikutnya: kuis proses yang baru terbuka, atau kuis organ terbuka yang belum selesai
            var (nextOrgan, nextProcess) = QuizAccess.Next(P);
            var cont = U.Col(6).Radius(18).Pad(16).Gradient(U.Pink, U.Purple, 135);
            cont.style.overflow = Overflow.Hidden;
            cont.Add(U.Icon("mascot_quiz", 118).Abs(null, null, -4, -6));
            bool any = nextOrgan != null || nextProcess != null;
            string heroTitle = nextProcess != null ? nextProcess.title : nextOrgan != null ? "Kuis " + QuizLocks.OrganName(App, nextOrgan) : "Hebat, " + P.nickname + "!";
            cont.Add(U.Text(nextProcess != null ? "KUIS PROSES TERBUKA" : any ? "KUIS BERIKUTNYA" : "SEMUA KUIS TERBUKA SUDAH SELESAI").FontSize(12).Bold().Fg(U.C("#FFFFFF", 0.9f)));
            var ht = U.Text(heroTitle).FontSize(20).Bold().Fg("#FFFFFF").W(200);
            ht.style.whiteSpace = WhiteSpace.Normal;
            cont.Add(ht);
            var sub = U.Text(any ? "5 soal" : "Scan kartu lain untuk membuka kuis baru").FontSize(12).Fg("#FFFFFF").W(200);
            sub.style.whiteSpace = WhiteSpace.Normal;
            cont.Add(sub);
            var go = U.Row(6).Bg("#FFFFFF").Radius(999).Pad(9, 16).AlignSelf(Align.FlexStart);
            go.style.marginTop = 6;
            go.Add(U.Text(any ? "Mulai" : "Scan Kartu").FontSize(13).Bold().Fg(U.Pink));
            go.Add(U.Icon("arrow_right", 14).Tinted(U.C(U.Pink)));
            go.Tap(() =>
            {
                if (nextProcess != null) QuizSession.Play(App, QuizSession.StartProcess(nextProcess.id));
                else if (nextOrgan != null) QuizSession.Play(App, QuizSession.StartOrgan(nextOrgan));
                else App.GoTab(NavTab.Scan);
            });
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

            // dua jenis kuis: tentang satu organ, atau cara kerja satu sistem (proses)
            var order = QuizAccess.Order(P);
            var seg = U.Row(4).Pad(4).Radius(999).Bg("#EDEBF3");
            string[] names = { "Kuis Organ", "Kuis Proses" };
            string[] counts = { QuizAccess.OrgansDone(P) + "/" + order.Count, QuizAccess.ProcessesDone(P) + "/" + QuizBank.Processes.Count };
            for (int i = 0; i < 2; i++)
            {
                int k = i;
                bool on = m_Tab == i;
                var b = U.Row(6).Grow().MinH(42).Radius(999).Center().Bg(on ? "#FFFFFF" : "#00000000");
                if (on) b.Elev(Elevation.E1);
                b.Add(U.Text(names[i], "t-small").Bold().Fg(on ? U.Ink : U.Muted));
                b.Add(U.Text(counts[i]).FontSize(11).Fg(on ? U.Pink : U.Faint));
                b.Tap(() => { if (m_Tab != k) Refresh(k); });
                seg.Add(b);
            }
            c.Add(seg);
            c.Add(m_Tab == 0 ? OrganGrid(order) : ProcessList());
        }

        /// <summary>Kuis tiap organ: terbuka setelah kartunya di-scan atau kuis sebelumnya selesai.</summary>
        VisualElement OrganGrid(List<string> order)
        {
            var grid = U.Col(10);
            VisualElement row = null;
            for (int i = 0; i < order.Count; i++)
            {
                if (i % 3 == 0) { row = U.Row(10).Align(Align.Stretch); grid.Add(row); }
                row.Add(OrganTile(order[i]));
            }
            for (int i = order.Count % 3; i > 0 && i < 3; i++) row.Add(U.Div().Grow().W(0)); // baris terakhir: ukuran kartu tetap sama
            return grid;
        }

        VisualElement OrganTile(string id)
        {
            var o = Db.Get(id);
            bool open = QuizAccess.OrganUnlocked(P, id), done = QuizAccess.OrganDone(P, id);
            int best = QuizAccess.Best(P, QuizBank.OrganQuizId(id));
            var (fg, soft) = OrganDefaults.SystemColors(o != null ? o.system : OrganSystem.Pencernaan);
            var t = U.Col(6).Grow().W(0).MinH(122).Radius(16).Pad(10, 6).Align(Align.Center).Bg(open ? "#FFFFFF" : "#FBFAFD").Border(1, U.Line);
            if (open) t.Elev(Elevation.E1);
            var ic = U.Div().Size(52, 52).Radius(26).Center().Bg(open ? soft : U.C("#F1F0F5"));
            var icon = U.Icon(o != null ? Parts.MiniIcon(o) : "lock_gray", 34);
            if (!open) icon.style.opacity = 0.35f;
            ic.Add(icon);
            if (!open) ic.Add(Parts.LockBadge(20).Abs(null, null, -4, -4));
            t.Add(ic);
            var nm = U.Text(o != null ? o.displayName : id, "t-tiny").Bold().TextCenter().Fg(open ? U.Ink : U.Faint);
            nm.style.whiteSpace = WhiteSpace.Normal;
            t.Add(nm);
            if (done && best >= 0) t.Add(Score(best, 5));
            else if (open) t.Add(U.Pill("Mulai", CardArt.Hex(soft), CardArt.Hex(fg)));
            else t.Add(U.Text("Terkunci", "t-tiny").Fg(U.Faint));
            t.Tap(() => QuizLocks.PlayOrgan(App, id));
            t.tooltip = "Kuis " + (o != null ? o.displayName : id);
            return t;
        }

        /// <summary>Kuis cara kerja satu sistem: terbuka setelah semua kuis organnya selesai.</summary>
        VisualElement ProcessList()
        {
            var all = U.Col(10);
            foreach (var s in QuizBank.Processes)
            {
                bool open = QuizAccess.ProcessUnlocked(P, s), done = QuizAccess.ProcessDone(P, s.id);
                int best = QuizAccess.Best(P, QuizBank.ProcessQuizId(s.id));
                var missing = QuizAccess.ProcessMissing(P, s);
                var (fg, soft) = OrganDefaults.SystemColors(s.system);
                var card = U.Row(12).MinH(72).Radius(16).Pad(10, 14).Bg(open ? "#FFFFFF" : "#FBFAFD").Border(1, U.Line);
                var ic = U.Div().Size(48, 48).Radius(14).Center().Bg(open ? soft : U.C("#F1F0F5"));
                var icon = U.Icon(s.icon, 26);
                if (!open) icon.style.opacity = 0.4f;
                ic.Add(icon);
                card.Add(ic);
                var mid = U.Col(2).Grow();
                mid.Add(U.Text(s.title, "t-small").Bold().Fg(open ? U.Ink : U.Faint));
                var info = U.Text(open ? s.organs.Length + " organ · 5 soal"
                    : "Selesaikan " + missing.Count + " kuis organ lagi: " + string.Join(", ", missing.Select(m => QuizLocks.OrganName(App, m))), "t-tiny");
                info.style.whiteSpace = WhiteSpace.Normal;
                mid.Add(info);
                card.Add(mid);
                if (!open) card.Add(U.Icon("lock_gray", 18));
                else if (done && best >= 0) card.Add(Score(best, 5));
                else card.Add(U.Pill("Mulai", U.PinkSoft, U.PinkDark));
                var pr = s;
                card.Tap(() =>
                {
                    if (QuizAccess.ProcessUnlocked(P, pr)) QuizSession.Play(App, QuizSession.StartProcess(pr.id));
                    else QuizLocks.ShowProcess(App, pr);
                });
                card.tooltip = s.title;
                all.Add(card);
            }
            return all;
        }

        /// <summary>Skor terbaik: bintang kecil + "4/5".</summary>
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
        /// <summary>Soal sudah dijawab benar (boleh lanjut). Jawaban salah harus diulang sampai benar.</summary>
        bool m_Checked;
        /// <summary>Percobaan pertama sudah dicatat (hanya percobaan pertama yang dihitung benar/salah).</summary>
        bool m_Answered;
        string m_Holding;
        readonly Dictionary<string, string> m_Placed = new Dictionary<string, string>();
        /// <summary>Pilihan ganda: pilihan yang sudah dicoba dan salah.</summary>
        readonly HashSet<int> m_Wrong = new HashSet<int>();
        /// <summary>Tempel label: organ yang sudah di tempat yang tepat (terkunci).</summary>
        readonly HashSet<string> m_Locked = new HashSet<string>();
        string m_LabelMsg;

        void ResetAnswer() { m_Picked = -1; m_Checked = false; m_Answered = false; m_Placed.Clear(); m_Holding = null; m_Wrong.Clear(); m_Locked.Clear(); m_LabelMsg = null; }

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
            if (arg == null || QuizSession.Current == null) QuizSession.StartProcess("cerna");
            S = QuizSession.Current;
            if (arg is QuizSession || arg == null) ResetAnswer();
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
                    bool sel = m_Picked == i, right = i == q.correct, wrong = m_Wrong.Contains(i);
                    string bg = "#FFFFFF", bd = "#E4E2EC", badgeBg = "#F6F5F9", badgeFg = U.Ink, markBg = null, icon = null;
                    float op = 1;
                    if (!m_Checked && sel) { bg = "#F3EFFD"; bd = U.Purple; badgeBg = U.Purple; badgeFg = "#FFFFFF"; markBg = U.Purple; icon = "check"; }
                    if (m_Checked && right) { bg = U.TealSoft; bd = U.Teal; badgeBg = U.Teal; badgeFg = "#FFFFFF"; markBg = U.Teal; icon = "check"; }
                    else if (wrong) { bg = U.PinkSoft; bd = "#F5B8C6"; badgeBg = U.Pink; badgeFg = "#FFFFFF"; markBg = U.Pink; icon = "x_small"; op = 0.6f; }
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
                row.Tap(() => { if (m_Checked || m_Picked == k || m_Wrong.Contains(k)) return; m_Picked = k; Paint(); });
                rows.Add((row, badge, letter, mark));
                opts.Add(row);
            }
            c.Add(opts);

            // jawaban salah: pilihan itu dicoret dan anak memilih lagi sampai benar (baru boleh lanjut)
            VisualElement retry = null;
            check.Tap(() =>
            {
                if (m_Picked < 0 || m_Checked) return;
                bool ok = m_Picked == q.correct;
                if (!m_Answered) { m_Answered = true; S.Answer(ok, q.options[m_Picked], q.options[q.correct]); }
                else QuizSession.Sound(ok);
                retry?.RemoveFromHierarchy();
                retry = null;
                if (ok)
                {
                    m_Checked = true;
                    Paint();
                    ShowFeedback(c, true, q.explain);
                    bottom.Clear();
                    bottom.Add(NextButton());
                }
                else
                {
                    m_Wrong.Add(m_Picked);
                    m_Picked = -1;
                    Paint();
                    retry = ShowFeedback(c, false, "Coba pilih jawaban yang lain.");
                }
            });
            bottom.Add(check);
            Paint();
            if (m_Checked) { ShowFeedback(c, true, q.explain); bottom.Clear(); bottom.Add(NextButton()); }
        }

        /// <summary>Kotak koreksi ikut area gulir (bukan bar bawah) supaya penjelasan panjang tidak terjepit / keluar kotak.</summary>
        static VisualElement ShowFeedback(VisualElement c, bool ok, string explain)
        {
            var fb = U.Row(10).Align(Align.FlexStart).Radius(14).Pad(12).Bg(ok ? U.TealSoft : U.PinkSoft).Border(1, ok ? "#9CE6DC" : "#F5B8C6");
            fb.Add(U.Div().Size(26, 26).Radius(13).Bg(ok ? U.Teal : U.Pink).Center().NoShrink().Kids(U.Icon(ok ? "check" : "x_small", 13)));
            var ft = U.Col(2).Grow();
            ft.Add(U.Text(ok ? "Benar! Hebat!" : "Belum tepat. Ayo, coba lagi!", "t-body").Bold().Fg(ok ? U.TealInk : "#B0234A"));
            var ex = U.Text(explain, "t-caption").Fg(U.Ink);
            ex.style.whiteSpace = WhiteSpace.Normal;
            ft.Add(ex);
            fb.Add(ft);
            c.Add(fb);
            var sv = c.GetFirstAncestorOfType<ScrollView>();
            if (sv != null) fb.RegisterCallback<GeometryChangedEvent>(_ => sv.ScrollTo(fb));
            return fb;
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
                ResetAnswer();
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
                    bool fixedOk = m_Checked || m_Locked.Contains(placedOrgan);
                    target.Bg("#FFFFFF").Border(2, fixedOk ? U.Teal : U.Purple);
                    target.Add(U.Icon(o.IconKey, 28));
                    var cap = U.Text(o.displayName).FontSize(10).Bold().Abs(-20, 44).W(84).TextCenter();
                    target.Add(cap);
                    if (!fixedOk) target.Tap(() => { m_Placed.Remove(placedOrgan); Refresh("keep"); });
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
                        if (m_Placed.Any(kv => kv.Value == spotId && m_Locked.Contains(kv.Key))) return; // titik ini sudah terisi dengan tepat
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
                if (m_LabelMsg != null && m_Holding == null)
                {
                    var msg = U.Text(m_LabelMsg, "t-caption").Fg("#B0234A");
                    msg.style.whiteSpace = WhiteSpace.Normal;
                    bottom.Add(msg);
                }
                var info = U.Row().Justify(Justify.SpaceBetween);
                info.Add(U.Text(m_Holding != null ? "Sekarang ketuk titik di tubuh" : "", "t-caption"));
                info.Add(U.Text(placed + "/" + total, "t-caption").Bold());
                bottom.Add(info);
                bool can = placed == total;
                bottom.Add(U.Button("Periksa Jawaban", () =>
                {
                    if (!can) return;
                    int right = m_Placed.Count(kv => kv.Key == kv.Value);
                    if (!m_Answered)
                    {
                        m_Answered = true;
                        var wrongs = m_Placed.Where(kv => kv.Key != kv.Value).Select(kv => Db.Get(kv.Key).displayName).ToList();
                        S.Answer(right == total, wrongs.Count == 0 ? "Semua tepat" : "Tertukar: " + string.Join(", ", wrongs),
                            !string.IsNullOrEmpty(q.answer) ? q.answer : string.Join(", ", q.labelOrgans.Select(x => Db.Get(x)?.displayName ?? x)));
                    }
                    else QuizSession.Sound(right == total);
                    if (right == total) { m_Checked = true; m_LabelMsg = null; }
                    else
                    {
                        // yang sudah tepat dikunci, yang belum tepat kembali ke bawah untuk dicoba lagi
                        foreach (var kv in m_Placed.ToList())
                        {
                            if (kv.Key == kv.Value) m_Locked.Add(kv.Key);
                            else m_Placed.Remove(kv.Key);
                        }
                        m_LabelMsg = right + " dari " + total + " sudah tepat. Pindahkan organ yang belum tepat, lalu periksa lagi.";
                    }
                    Refresh("keep");
                }, can ? "btn-primary" : "btn-disabled"));
            }
            else
            {
                var fb = U.Row(10).Radius(14).Pad(12).Bg(U.TealSoft);
                var ft = U.Text("Benar semua! " + q.explain, "t-caption").Fg(U.Ink).Grow();
                ft.style.whiteSpace = WhiteSpace.Normal;
                fb.Add(ft);
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
        float m_WrongSince;
        VisualElement m_StandIn, m_Bottom, m_Stage, m_WrongToast;
        VisualElement m_WrongMini;
        Label m_WrongTitle, m_WrongText;
        CardFrame m_Frame;
        QuizSession S;
        // jawaban benar di HP tanpa AR: model mengikuti kartu di gambar kamera
        CardFollowView m_Follow;

        protected override void Build(object arg)
        {
            S = QuizSession.Current ?? QuizSession.StartOrgan("mulut");
            if (S.Q == null || S.Q.type != QuizType.MisiScan)
            {
                // tanpa soal misi scan (mis. dibuka langsung untuk uji): pakai soal scan dari kuis Mulut
                S = QuizSession.StartOrgan("mulut");
                S.index = S.questions.FindIndex(x => x.type == QuizType.MisiScan);
            }
            m_State = State.Idle;
            m_WrongToast = null;
            m_Follow = null;
            var q = S.Q;
            if (App.ar != null)
            {
                App.ar.ResetDetection();
                // misi scan: tidak menempelkan model AR di kartu, dan kartu yang dicari didahulukan bila beberapa kartu terlihat
                App.ar.SpawnModels = false;
                App.ar.PreferOrgan = q.organId;
                App.ar.CardDetected -= OnCard; App.ar.CardDetected += OnCard;
            }

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
            if (m_State != State.Wrong) { m_WrongToast?.RemoveFromHierarchy(); m_WrongToast = null; }
            m_Frame.Visible(m_State != State.Right);
            m_Frame.Tint = U.C(m_State == State.Wrong ? U.Pink : U.Teal);
            m_Frame.SweepVisible = m_State == State.Idle;
            if (m_State == State.Right)
            {
                var organ = Db.Get(q.organId);
                var ar = App.ar;
                if (ar != null && ar.CanRecognizeWithCamera)
                {
                    // HP tanpa AR: model muncul di atas kartu yang dipegang dan ikut kartunya (hilang kalau kartu diangkat)
                    ar.ShowModelOnCard();
                    m_Follow = new CardFollowView(App.Viewer, organ);
                    m_Follow.Viewport.ShowHotspots = false;
                    Root.Insert(1, m_Follow.Viewport);
                }
                else if (ar != null && ar.IsCameraLive)
                {
                    // AR: model ditempel di atas kartunya (bukan model diam di tengah layar); hilang kalau kartu tidak terlihat
                    ar.ShowModelOnCard();
                }
                else
                {
                    // tanpa kamera (Editor): viewer 3D biasa
                    var vp = new OrganViewport(App.Viewer, organ, false) { ShowHotspots = false, FreeRotate = true };
                    vp.Size(300, 260);
                    m_Stage.Add(vp);
                }
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
            if (m_State == State.Wrong) ShowWrong();
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

        /// <summary>
        /// Kabar "itu kartu X" dibuat sekali lalu hanya isinya yang diganti, jadi tidak berkedip. Dulu kabar ini dibuat ulang
        /// setiap kali kartu terdeteksi; dengan beberapa kartu salah di depan kamera (mis. lembar kartu di meja) kabarnya
        /// berganti-ganti terus sehingga terlihat bergetar.
        /// </summary>
        void ShowWrong()
        {
            var wrong = Db.Get(m_WrongId);
            if (wrong == null) return;
            if (m_WrongToast == null)
            {
                var toast = U.Row(12).Bg("#FFFFFF").Radius(16).Pad(12, 14).Align(Align.FlexStart);
                // melayang di atas bagian bawah bingkai: ukuran bingkai (dan jarak scan) tidak berubah
                toast.style.position = Position.Absolute;
                toast.style.left = 20; toast.style.right = 20; toast.style.bottom = 70;
                toast.pickingMode = PickingMode.Ignore;
                m_WrongMini = U.Div().Size(46, 46).Radius(12).Bg(U.PinkSoft).Border(2, U.Pink).Center();
                toast.Add(m_WrongMini);
                var tc = U.Col(4).Grow();
                m_WrongTitle = U.Text("", "t-title").Fg("#B0234A");
                tc.Add(m_WrongTitle);
                m_WrongText = U.Text("", "t-caption").Fg(U.Ink);
                m_WrongText.style.whiteSpace = WhiteSpace.Normal;
                tc.Add(m_WrongText);
                toast.Add(tc);
                Root.Add(toast);
                m_WrongToast = toast;
            }
            m_WrongMini.Clear();
            m_WrongMini.Add(U.Icon(wrong.IconKey, 30));
            m_WrongTitle.text = "Itu kartu " + wrong.displayName;
            m_WrongText.text = wrong.function + " Coba cari kartu yang lain, ya!";
        }

        void OnCard(OrganInfo o)
        {
            if (o == null || m_State == State.Right) return;
            var q = S.Q;
            if (o.id == q.organId)
            {
                m_State = State.Right;
                S.Answer(true, o.displayName, o.displayName);
                App.Progress.stars += 10;
                App.Progress.AddCard(o.id, 0);
                Render();
                return;
            }
            App.ar?.ResetDetection();
            // kartu salah yang sama masih di depan kamera, atau kartu salah lain tepat setelahnya: kabar tetap tenang
            if (m_State == State.Wrong && (o.id == m_WrongId || Time.unscaledTime - m_WrongSince < 2f)) return;
            bool first = m_State != State.Wrong;
            m_State = State.Wrong;
            m_WrongId = o.id;
            m_WrongSince = Time.unscaledTime;
            OrganoAudio.Wrong();
            if (first) Render();
            else ShowWrong();
        }

        public override void Tick(float dt)
        {
            bool live = App.ar != null && App.ar.IsCameraLive;
            m_StandIn.Visible(!live);
            m_Frame.Tick(dt);
            ScanParts.TellScanFrame(App.ar, m_StandIn, m_Frame.Box, m_State != State.Right && !live);
            if (m_Follow != null && App.ar != null) m_Follow.Tick(App.ar, Root, Db.Get(S.Q.organId), dt);
        }

        public override void Close()
        {
            if (App.ar == null) return;
            App.ar.CardDetected -= OnCard;
            App.ar.SpawnModels = true;
            App.ar.TrackCard = false;
            App.ar.PreferOrgan = null;
            App.ar.ClearScanFrame();
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

            if (s != null && s.opened.Count > 0)
            {
                var box = U.Col(8).AlignSelf(Align.Stretch).Radius(16).Pad(12, 14).Bg(U.PurpleSoft).Border(1, "#D3C8F7");
                box.Add(U.Text(s.opened.Count == 1 ? "Kuis baru terbuka!" : s.opened.Count + " kuis baru terbuka!", "t-body").Bold());
                foreach (var id in s.opened)
                {
                    bool organQuiz = id.StartsWith("organ:");
                    string key = id.Substring(id.IndexOf(':') + 1);
                    var pr = organQuiz ? null : QuizBank.GetProcess(key);
                    var row = U.Row(10).Pad(8, 10).Radius(12).Bg("#FFFFFF");
                    var organ = organQuiz ? Db.Get(key) : null;
                    row.Add(U.Icon(organ != null ? Parts.MiniIcon(organ) : pr != null ? pr.icon : "qt_3d", 26));
                    row.Add(U.Text(organQuiz ? "Kuis " + (organ?.displayName ?? key) : pr?.title ?? key, "t-small").Bold().Grow());
                    row.Add(U.Text("Mulai").FontSize(12).Bold().Fg(U.Pink));
                    row.Tap(() => QuizSession.Play(App, organQuiz ? QuizSession.StartOrgan(key) : QuizSession.StartProcess(key)));
                    box.Add(row);
                }
                c.Add(box);
            }

            var newest = P.newestCard != null ? Db.Get(P.newestCard) : null;
            if (newest != null && s != null && s.GainedCard(P))
            {
                var unlock = U.Col(14).Align(Align.Center);
                unlock.Add(U.Text("Kamu mendapat kartu baru!", "t-section"));
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
            Root.Add(Parts.TopBar("Riwayat Kuis", App.Back));
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
                if (QuizBank.OrganOfRecord(rec) != null || QuizBank.ProcessOfRecord(rec) != null)
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
