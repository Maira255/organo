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

        public static QuizSession Start(QuizType? only = null)
        {
            Current = new QuizSession { questions = QuizBank.Pencernaan(only) };
            return Current;
        }

        public void Answer(bool ok, string yours, string right)
        {
            var q = Q;
            answers.Add(new QuizAnswer { type = QuizBank.TypeName(q.type), question = q.prompt, yours = yours, right = right, explain = q.explain, isCorrect = ok });
            if (ok) OrganoAudio.Success(); else OrganoAudio.Wrong();
        }

        /// <summary>Lanjut ke soal berikutnya atau ke hasil.</summary>
        public void Next(OrganoApp app)
        {
            index++;
            if (index >= questions.Count) { Finish(app); return; }
            OpenCurrent(app, false);
        }

        public static void OpenCurrent(OrganoApp app, bool push = true)
        {
            var s = Current;
            if (s.Q.type == QuizType.MisiScan) app.Go<QuizScanScreen>(null, push);
            else app.Go<QuizPlayScreen>(s, push);
        }

        void Finish(OrganoApp app)
        {
            var p = app.Progress;
            int stars = Correct * 5;
            result = new QuizRecord
            {
                system = OrganDefaults.SystemName(system),
                quizNumber = Mathf.Min(5, p.season1Done + 1),
                correct = Correct, total = questions.Count, stars = stars,
                when = "Hari ini, " + System.DateTime.Now.ToString("HH.mm"),
                answers = new List<QuizAnswer>(answers)
            };
            p.history.Insert(0, result);
            p.stars += stars;
            if (questions.Count >= 5 && p.season1Done < 5) p.season1Done++;
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
            var hist = U.IconButton("rotate", () => App.Go<QuizHistoryScreen>(), 20, "Riwayat");
            hist.style.marginRight = -12;
            Root.Add(Parts.TopBar("Quiz", () => App.GoTab(NavTab.Beranda), hist));
            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 8;
            Root.Add(scroll);
            var c = U.Col(20);
            scroll.Add(c);

            int done = P.season1Done;
            var cont = U.Col(6).Radius(18).Pad(16).Gradient(U.Pink, U.Purple, 135);
            cont.style.overflow = Overflow.Hidden;
            cont.Add(U.Icon("mascot_quiz", 118).Abs(null, null, -4, -6));
            cont.Add(U.Text("Season 1 · Lanjutkan").FontSize(12).Bold().Fg(U.C("#FFFFFF", 0.9f)));
            cont.Add(U.Text("Sistem Pencernaan").FontSize(20).Bold().Fg("#FFFFFF"));
            cont.Add(U.Text("Kuis " + Mathf.Min(5, done + 1) + " dari 5 · ±5 menit").FontSize(12).Fg("#FFFFFF"));
            var bar = U.Progress(done / 5f, "#FFFFFF", "#FFFFFF55").W(170);
            cont.Add(bar);
            var go = U.Row(6).Bg("#FFFFFF").Radius(999).Pad(9, 16).AlignSelf(Align.FlexStart);
            go.style.marginTop = 6;
            go.Add(U.Text("Lanjut Kuis").FontSize(13).Bold().Fg(U.Pink));
            go.Add(U.Icon("arrow_right", 14).Tinted(U.C(U.Pink)));
            go.Tap(() => { QuizSession.Start(); QuizSession.OpenCurrent(App); });
            cont.Add(go);
            c.Add(cont);

            var last = P.history.FirstOrDefault();
            if (last != null)
            {
                var r = U.Row(12, "card", "card-lg");
                var ring = U.Div().Size(46, 46).Radius(23).Border(4, U.Teal).Center();
                ring.Add(U.Text(last.Percent + "%").FontSize(12).Bold().Fg(U.TealDark));
                r.Add(ring);
                var t = U.Col(2).Grow();
                t.Add(U.Text("HASIL TERAKHIR", "t-tiny").Bold());
                t.Add(U.Text(last.system + " · Kuis " + last.quizNumber + " · " + last.correct + "/" + last.total + " benar", "t-small").Bold());
                t.Add(U.Text(last.when + " · +" + last.stars + " bintang", "t-tiny"));
                r.Add(t);
                r.Add(U.Text("Riwayat").FontSize(12).Bold().Fg(U.Pink));
                r.Tap(() => App.Go<QuizHistoryScreen>());
                c.Add(r);
            }

            var types = U.Col(10);
            types.Add(U.Text("Latihan per jenis kuis", "t-section"));
            var row = U.Row(10);
            row.Add(TypeCard("Tempel Label", "Seret ke tubuh", "grid", U.TealSoft, QuizType.TempelLabel));
            row.Add(TypeCard("Pilihan Ganda 3D", "Putar & tebak", "rotate", U.PurpleSoft, QuizType.PilihanGanda3D));
            row.Add(TypeCard("Misi Scan", "Cari kartunya", "scan_small", U.PinkSoft, QuizType.MisiScan));
            types.Add(row);
            c.Add(types);

            var all = U.Col(10);
            var hdr = U.Row().Justify(Justify.SpaceBetween);
            hdr.Add(U.Text("Kumpulan kuis", "t-section"));
            hdr.Add(U.Text((done >= 5 ? 2 : 1) + " dari 4 season selesai", "t-caption"));
            all.Add(hdr);
            foreach (var s in QuizBank.Seasons)
            {
                int d = s.id == "tubuh" ? P.warmupDone : s.id == "cerna" ? done : 0;
                bool locked = s.id == "napas" ? done < 5 : s.id == "darah";
                bool complete = d >= s.total;
                var (fg, soft) = OrganDefaults.SystemColors(s.system);
                string color = locked ? "#BDBACE" : s.id == "tubuh" ? U.Teal : U.Pink;
                var card = U.Row(12).MinH(76).Radius(16).Pad(12, 14).Bg(locked ? "#FBFAFD" : "#FFFFFF").Border(1, !locked && !complete ? "#F5B7C6" : U.Line);
                var ic = U.Div().Size(48, 48).Radius(14).Center().Bg(locked ? U.C("#F6F5F9") : soft);
                ic.Add(U.Icon(s.id == "tubuh" ? "logo" : s.id == "cerna" ? "cat_cerna" : s.id == "napas" ? "cat_napas" : "cat_darah", 26));
                if (locked) ((SvgImage)ic[0]).Tint = U.C("#BDBACE");
                card.Add(ic);
                var mid = U.Col(4).Grow();
                mid.Add(U.Text(s.season, "t-tiny").Bold());
                mid.Add(U.Text(s.title, "t-small").Bold().Fg(locked ? U.Faint : U.Ink));
                var pr = U.Row(8);
                pr.Add(U.Progress((float)d / s.total, color, "#F0EEF5").Grow());
                pr.Add(U.Text(d + "/" + s.total + " kuis", "t-tiny"));
                mid.Add(pr);
                card.Add(mid);
                if (locked) card.Add(U.Icon("lock", 20).Tinted(U.C(U.Faint)));
                else if (complete) card.Add(U.Pill("Selesai", U.TealSoft, U.TealDark));
                else card.Add(U.Pill("Lanjut", U.PinkSoft, U.PinkDark));
                card.Tap(() =>
                {
                    if (locked) App.Toast("Selesaikan season sebelumnya untuk membuka!");
                    else { QuizSession.Start(); QuizSession.OpenCurrent(App); }
                });
                all.Add(card);
            }
            c.Add(all);
        }

        VisualElement TypeCard(string title, string sub, string icon, string bg, QuizType type)
        {
            var t = U.Col(8).Grow().MinH(120).Radius(16).Bg("#FFFFFF").Border(1, U.Line).Pad(12, 8).Align(Align.Center);
            t.Add(U.Div().Size(44, 44).Radius(22).Bg(bg).Center().Kids(U.Icon(icon, 22)));
            t.Add(U.Text(title, "t-small").Bold().TextCenter());
            t.Add(U.Text(sub, "t-tiny").TextCenter());
            t.Tap(() => { QuizSession.Start(type); QuizSession.OpenCurrent(App); });
            return t;
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
            { "lambung", new Vector2(210, 210) }, { "usushalus", new Vector2(195, 262) },
        };

        protected override void Build(object arg)
        {
            if (arg is OrganSystem || arg == null) { QuizSession.Start(); }
            S = QuizSession.Current;
            if (arg is QuizSession || arg is OrganSystem || arg == null) { m_Picked = -1; m_Checked = false; m_Placed.Clear(); m_Holding = null; }
            var q = S.Q;

            // Top bar + progres
            var top = U.Row(12).Pad(16, 20, 0, 20);
            var close = U.IconButton("close", () => Parts.ConfirmExitQuiz(App, () => App.GoTab(NavTab.Quiz)));
            close.style.marginLeft = -12;
            top.Add(close);
            top.Add(U.Text(q.type == QuizType.TempelLabel ? "Label Sistem Pencernaan" : "Quiz Sistem Pencernaan", "t-title").Grow().TextCenter());
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

        void BuildChoice(QuizQuestion q, VisualElement c, VisualElement bottom)
        {
            var head = U.Col(6);
            head.Add(U.Pill("Pilihan ganda", U.PurpleSoft, U.PurpleDark));
            head.Add(U.Text(q.prompt, "t-h2"));
            c.Add(head);

            var organ = Db.Get(q.organId);
            var box = U.Div().H(220).Radius(18).Bg("#F3EFFD").Border(1, "#CDBFF5");
            box.style.overflow = Overflow.Hidden;
            var vp = new OrganViewport(App.Viewer, organ, true, "Geser untuk memutar organ") { ShowHotspots = false };
            vp.AddToClassList("fill");
            box.Add(vp);
            var tag = U.Row(4).Abs(12, 12).Pad(4, 10).Radius(999).Bg(U.TealDark);
            tag.pickingMode = PickingMode.Ignore;
            tag.Add(U.Text("3D").FontSize(11).Bold().Fg("#FFFFFF"));
            box.Add(tag);
            c.Add(box);

            var opts = U.Col(10);
            for (int i = 0; i < q.options.Length; i++)
            {
                int k = i;
                bool sel = m_Picked == i, right = i == q.correct;
                string bg = "#FFFFFF", bd = "#E4E2EC", badgeBg = "#F6F5F9", badgeFg = U.Ink;
                float op = 1;
                if (!m_Checked && sel) { bg = "#F3EFFD"; bd = U.Purple; badgeBg = U.Purple; badgeFg = "#FFFFFF"; }
                if (m_Checked && right) { bg = U.TealSoft; bd = U.Teal; badgeBg = U.Teal; badgeFg = "#FFFFFF"; }
                else if (m_Checked && sel) { bg = U.PinkSoft; bd = U.Pink; badgeBg = U.Pink; badgeFg = "#FFFFFF"; }
                else if (m_Checked) op = 0.55f;
                var row = U.Row(12).MinH(52).Pad(10, 14).Radius(14).Bg(bg).Border(1.5f, bd).Opacity(op);
                row.Add(U.Div().Size(28, 28).Radius(14).Bg(badgeBg).Center().Kids(U.Text("ABCD"[i].ToString()).FontSize(13).Bold().Fg(badgeFg)));
                row.Add(U.Text(q.options[i], "t-small").Bold().Grow());
                if (m_Checked && right) row.Add(U.Div().Size(22, 22).Radius(11).Bg(U.Teal).Center().Kids(U.Icon("check", 12)));
                if (m_Checked && sel && !right) row.Add(U.Div().Size(22, 22).Radius(11).Bg(U.Pink).Center().Kids(U.Icon("x_small", 11)));
                row.Tap(() => { if (!m_Checked) { m_Picked = k; Refresh("keep"); } });
                opts.Add(row);
            }
            c.Add(opts);

            if (!m_Checked)
            {
                bool can = m_Picked >= 0;
                bottom.Add(U.Button("Periksa Jawaban", () =>
                {
                    if (!can) return;
                    m_Checked = true;
                    S.Answer(m_Picked == q.correct, q.options[m_Picked], q.options[q.correct]);
                    Refresh("keep");
                }, can ? "btn-primary" : "btn-disabled"));
            }
            else
            {
                bool ok = m_Picked == q.correct;
                var fb = U.Row(10).Align(Align.FlexStart).Radius(14).Pad(12).Bg(ok ? U.TealSoft : U.PinkSoft);
                fb.Add(U.Div().Size(26, 26).Radius(13).Bg(ok ? U.Teal : U.Pink).Center().Kids(U.Icon(ok ? "check" : "x_small", 13)));
                var ft = U.Col(2).Grow();
                ft.Add(U.Text(ok ? "Benar! Hebat!" : "Belum tepat, tidak apa-apa!", "t-body").Bold().Fg(ok ? U.TealInk : "#B0234A"));
                ft.Add(U.Text(q.explain, "t-caption").Fg(U.Ink));
                fb.Add(ft);
                bottom.Add(fb);
                bottom.Add(NextButton());
            }
        }

        VisualElement NextButton()
        {
            bool last = S.index >= S.questions.Count - 1;
            bool nextIsScan = !last && S.questions[S.index + 1].type == QuizType.MisiScan;
            var b = U.Button(last ? "Lihat Hasil Kuis" : nextIsScan ? "Lanjut ke Misi Scan" : "Soal Berikutnya", () =>
            {
                m_Picked = -1; m_Checked = false; m_Placed.Clear(); m_Holding = null;
                S.Next(App);
            }, "btn-primary");
            b.Add(U.Icon("arrow_right", 16));
            b[1].style.marginLeft = 8;
            return b;
        }

        // ---------------------------------------------------------- Tempel label

        void BuildLabel(QuizQuestion q, VisualElement c, VisualElement bottom)
        {
            var pill = U.Div().AlignSelf(Align.Center).Pad(7, 16).Radius(999).Bg(U.PurpleDark);
            pill.Add(U.Text("Tempel gambar organ ke posisi yang tepat!").FontSize(13).Fg("#FFFFFF"));
            c.Add(pill);

            var stage = U.Div().Size(350, 330).AlignSelf(Align.Center);
            stage.style.overflow = Overflow.Hidden;
            var body = new SvgImage();
            body.SetMarkup(OrganoIcons.Fill("body_template", new Dictionary<string, string>
            {
                { "shirtFill", "#8EDDD3" }, { "shirtStroke", "#5FBFB2" }, { "shirtOp", "0.35" }, { "smileOp", "1" },
                { "headOp", "0" }, { "neckOp", "0" }, { "chestOp", "0" }, { "bellyOp", "0" }, { "labelOp", "0" },
                { "boyDisplay", P.gender == Gender.Girl ? "none" : "inline" }, { "girlDisplay", P.gender == Gender.Girl ? "inline" : "none" }, { "bodyAlt", "" }
            }));
            body.Abs(-20, -10).Size(390, 420);
            body.style.scale = new Scale(new Vector3(0.82f, 0.82f, 1));
            body.style.transformOrigin = new TransformOrigin(195, 0);
            stage.Add(body);

            foreach (var id in q.labelOrgans)
            {
                var spot = LabelSpots[id];
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
                    target.Add(U.Text("?").Bold().Fg(U.Muted));
                    target.Tap(() =>
                    {
                        if (m_Holding == null || m_Checked) { App.Toast("Pilih organ di bawah dulu, lalu ketuk titiknya"); return; }
                        m_Placed[m_Holding] = id;
                        m_Holding = null;
                        Refresh("keep");
                    });
                }
                stage.Add(target);
            }
            c.Add(stage);

            var tray = U.Col(10);
            var remaining = q.labelOrgans.Where(id => !m_Placed.ContainsKey(id)).ToList();
            tray.Add(U.Text(remaining.Count > 0 ? "Organ yang tersisa · ketuk lalu pilih titiknya" : "Semua organ sudah ditempel!", "t-caption"));
            var tiles = U.Row(10);
            foreach (var id in remaining)
            {
                var o = Db.Get(id);
                bool hold = m_Holding == id;
                var t = U.Col(4).Size(76, 84).Radius(14).Center().Bg(hold ? "#F3EFFD" : "#FFFFFF").Border(hold ? 2 : 1, hold ? U.Purple : U.Line);
                t.Add(U.Icon(o.IconKey, 36));
                t.Add(U.Text(o.displayName).FontSize(10).Bold().TextCenter());
                t.Tap(() => { m_Holding = hold ? null : id; Refresh("keep"); });
                tiles.Add(t);
            }
            tray.Add(tiles);
            c.Add(tray);

            int placed = m_Placed.Count, total = q.labelOrgans.Length;
            if (!m_Checked)
            {
                var info = U.Row().Justify(Justify.SpaceBetween);
                info.Add(U.Text(m_Holding != null ? "Sekarang ketuk titik di tubuh" : "Tempel semua organ", "t-caption"));
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
                        "Mulut → Kerongkongan → Lambung → Usus Halus");
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
        VisualElement m_StandIn, m_Bottom, m_Stage;
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
            var top = U.Row(12).Pad(16 + App.TopInset, 20, 0, 20);
            top.Add(ScanParts.GlassButton("close", () => Parts.ConfirmExitQuiz(App, () => App.GoTab(NavTab.Quiz)), "Keluar"));
            top.Add(U.Flex());
            var step = U.Div().Pad(8, 14).Radius(999).Bg(U.C("#141220", 0.55f));
            step.Add(U.Text((S.index + 1) + "/" + S.questions.Count).FontSize(13).Bold().Fg("#FFFFFF"));
            top.Add(step);
            Root.Add(top);

            var mission = U.Col(6).Margin(14, 20, 0, 20).Bg("#FFFFFF").Radius(16).Pad(14, 16);
            mission.Add(U.Pill("Misi Scan", U.PinkSoft, U.PinkDark));
            mission.Add(U.Text(q.prompt, "t-title"));
            var hint = U.Row(6);
            hint.Add(U.Icon("bulb", 16).Tinted(U.C("#B07D00")));
            hint.Add(U.Text(q.hint, "t-caption").Grow());
            mission.Add(hint);
            Root.Add(mission);

            m_Stage = U.Div().Grow().Center();
            Root.Add(m_Stage);
            m_Bottom = U.Col(8).Pad(0, 20, 20, 20);
            Root.Add(m_Bottom);
            Render();
        }

        void Render()
        {
            var q = S.Q;
            m_Stage.Clear();
            m_Bottom.Clear();
            if (m_State == State.Right)
            {
                var organ = Db.Get(q.organId);
                var vp = new OrganViewport(App.Viewer, organ, false) { ShowHotspots = false };
                vp.Size(300, 260);
                m_Stage.Add(vp);
                var sheet = U.Col(8).Bg("#FFFFFF").Radius(20).Pad(16);
                var h = U.Row(8);
                h.Add(U.Div().Size(26, 26).Radius(13).Bg(U.Teal).Center().Kids(U.Icon("check", 13)));
                h.Add(U.Text("Benar, itu " + organ.displayName + "!", "t-h2").Grow());
                h.Add(U.Pill("+10 bintang", U.YellowSoft, "#7A5A00"));
                sheet.Add(h);
                sheet.Add(U.Text(q.explain, "t-small"));
                sheet.Add(U.Button(S.index >= S.questions.Count - 1 ? "Lihat Hasil Kuis" : "Soal Berikutnya", () => S.Next(App), "btn-primary"));
                m_Bottom.Add(sheet);
                return;
            }

            var frame = U.Div().Size(230, 230);
            var svg = U.Icon("scan_frame", 230);
            svg.AddToClassList("fill");
            svg.Tint = U.C(m_State == State.Wrong ? U.Pink : U.Teal);
            frame.Add(svg);
            if (m_State == State.Wrong)
            {
                var wrong = Db.Get(m_WrongId);
                var card = U.Col(6).Abs(60, 45).Size(110, 130).Radius(14).Bg("#FFFFFF").Border(2, U.Pink).Center();
                card.Add(U.Icon(wrong.IconKey, 50));
                card.Add(U.Text(wrong.displayName).FontSize(11).Bold());
                var x = U.Div().Size(26, 26).Radius(13).Bg(U.Pink).Border(2, "#FFFFFF").Center().Abs(null, -8, -8);
                x.Add(U.Icon("x_small", 11));
                card.Add(x);
                frame.Add(card);
            }
            frame.Tap(() =>
            {
                // Demo tanpa kamera: ketuk pertama = kartu salah, ketuk berikutnya = kartu benar
                if (App.ar != null && App.ar.IsCameraLive && App.ar.HasCardLibrary) return;
                OnCard(Db.Get(m_State == State.Idle ? "paru" : q.organId));
            });
            m_Stage.Add(frame);

            if (m_State == State.Wrong)
            {
                var wrong = Db.Get(m_WrongId);
                var toast = U.Col(4).Bg("#FFFFFF").Radius(16).Pad(12, 14);
                toast.Add(U.Text("Itu kartu " + wrong.displayName, "t-title").Fg("#B0234A"));
                toast.Add(U.Text(wrong.function + " Coba cari kartu lain, ya!", "t-caption").Fg(U.Ink));
                m_Bottom.Add(toast);
                m_Bottom.Add(U.Text("Ketuk bingkai untuk scan lagi").FontSize(12).Fg("#FFFFFF").AlignSelf(Align.Center));
            }
            else
            {
                var help = U.Col(6).Align(Align.Center);
                var pill = U.Div().Pad(8, 20).Radius(999).Bg(U.TealDark);
                pill.Add(U.Text("Arahkan kamera ke kartu organ").FontSize(13).Bold().Fg("#FFFFFF"));
                help.Add(pill);
                var skip = U.Row(0).Pad(8, 14).Radius(999).Bg(U.C("#141220", 0.6f));
                skip.Add(U.Text("Belum punya kartunya? ").FontSize(12).Fg("#FFFFFF"));
                skip.Add(U.Text("Lewati soal").FontSize(12).Bold().Fg("#9CE6DC").Tap(() =>
                {
                    S.Answer(false, "Dilewati", Db.Get(q.organId).displayName);
                    S.Next(App);
                }));
                help.Add(skip);
                m_Bottom.Add(help);
            }
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

        public override void Tick(float dt) => m_StandIn.Visible(!(App.ar != null && App.ar.IsCameraLive));

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
            var meta = U.Row(8);
            meta.Add(U.Pill("Sistem " + r.system, U.TealSoft, U.TealDark));
            meta.Add(U.Text("Tersimpan di Riwayat", "t-caption"));
            c.Add(meta);

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
            sr.Add(U.Icon("star", 26).Tinted(U.C(U.Yellow)));
            sr.Add(U.Text("+" + r.stars).FontSize(24).Bold().Fg("#7A5A00"));
            stars.Add(sr);
            stars.Add(U.Text("Total bintangmu: " + P.stars, "t-caption").Fg("#7A5A00"));
            c.Add(stars);

            var newest = P.newestCard != null ? Db.Get(P.newestCard) : Db.Get("lambung");
            if (newest != null && r.correct >= r.total - 1)
            {
                var unlock = U.Col(14).Align(Align.Center);
                unlock.Add(U.Text("Kamu dapat kartu baru!", "t-section"));
                var card = Parts.OrganCard(newest, true, 140);
                card.Tap(() => App.Go<CardCollectionScreen>(newest.id));
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
            var ring = U.Div().Size(54, 54).Radius(27).Border(5, U.Teal).Center();
            ring.Add(U.Text(m_R.Percent + "%").FontSize(13).Bold().Fg(U.TealDark));
            sum.Add(ring);
            var st = U.Col(2).Grow();
            st.Add(U.Text("Sistem " + m_R.system + " · Kuis " + m_R.quizNumber, "t-body").Bold());
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

            var answers = m_R.answers.Count > 0 ? m_R.answers : Sample();
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
            bottom.Add(U.Button("Ulangi Kuis", () => { QuizSession.Start(); QuizSession.OpenCurrent(App); }, "btn-outline").Grow());
            bottom.Add(U.Button("Selesai", () => App.GoTab(NavTab.Quiz), "btn-primary").Grow());
            Root.Add(bottom);
        }

        static List<QuizAnswer> Sample()
        {
            var list = new List<QuizAnswer>();
            foreach (var q in QuizBank.Pencernaan())
            {
                bool ok = q.type != QuizType.PilihanGanda3D || q.correct != 0 || list.Count != 2;
                string right = q.options != null ? q.options[q.correct] : q.organId;
                list.Add(new QuizAnswer { type = QuizBank.TypeName(q.type), question = q.prompt, yours = ok ? right : "Memompa darah ke seluruh tubuh", right = right, explain = q.explain, isCorrect = ok });
            }
            return list;
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

            if (P.guest)
            {
                var g = U.Col(4).Bg(U.YellowSoft).Radius(14).Pad(12, 14).Border(1, "#F1D77A");
                g.Add(U.Text("Kamu sedang di Mode Tamu", "t-body").Bold());
                g.Add(U.Text("Riwayat hanya ada selama sesi ini dan akan hilang saat kamu keluar.", "t-caption").Fg("#7A5A00"));
                g.Add(U.Text("Daftar biar riwayat tersimpan →").FontSize(12).Bold().Fg(U.TealDark).Tap(() => App.Go<RegisterScreen>()));
                c.Add(g);
            }

            var stats = U.Row(10);
            stats.Add(Stat(P.history.Count.ToString(), "Kuis selesai"));
            stats.Add(Stat(Mathf.RoundToInt(P.AveragePercent) + "%", "Rata-rata benar"));
            stats.Add(Stat(P.history.Sum(h => h.stars).ToString(), "Bintang didapat"));
            c.Add(stats);

            if (P.history.Count == 0)
                c.Add(U.Text("Belum ada kuis untuk sistem ini. Yuk mulai dari Beranda!", "t-small", "t-muted").TextCenter());

            foreach (var h in P.history)
            {
                var card = U.Col(10, "card", "card-lg");
                var row = U.Row(12);
                var ring = U.Div().Size(46, 46).Radius(23).Border(4, h.Percent >= 80 ? U.Teal : U.Yellow).Center();
                ring.Add(U.Text(h.Percent + "%").FontSize(12).Bold());
                row.Add(ring);
                var t = U.Col(2).Grow();
                t.Add(U.Text("Sistem " + h.system + " · Kuis " + h.quizNumber, "t-small").Bold());
                t.Add(U.Text(h.when + " · " + h.correct + "/" + h.total + " benar", "t-tiny"));
                row.Add(t);
                var s = U.Row(3).Pad(4, 8).Radius(999).Bg(U.YellowSoft);
                s.Add(U.Icon("star", 12).Tinted(U.C(U.Yellow)));
                s.Add(U.Text("+" + h.stars).FontSize(11).Bold().Fg("#7A5A00"));
                row.Add(s);
                card.Add(row);
                var btns = U.Row(8);
                var rec = h;
                btns.Add(U.Button("Lihat pembahasan", () => App.Go<QuizReviewScreen>(rec), "btn-soft", "btn-small").Grow());
                btns.Add(U.Button("Ulangi kuis ini", () => { QuizSession.Start(); QuizSession.OpenCurrent(App); }, "btn-outline", "btn-small").Grow());
                card.Add(btns);
                c.Add(card);
            }

            if (P.guest)
            {
                var old = U.Col(4, "card").Bg("#F6F5F9");
                old.Add(U.Text("Riwayat sebelumnya tidak tersimpan", "t-small").Bold());
                old.Add(U.Text("Masuk dengan akun supaya semua hasil kuis tersimpan dan bisa dilihat lagi.", "t-caption"));
                c.Add(old);
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
