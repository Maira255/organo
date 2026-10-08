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
    // ==================================================================== Library: tubuh interaktif

    public class LibraryBodyScreen : AppScreen
    {
        public override NavTab Tab => NavTab.Library;

        BodyRegion? m_Region;
        Gender m_Body;
        SvgImage m_BodySvg;
        VisualElement m_Stage, m_Labels, m_Targets, m_Sheet, m_RegionList;
        Label m_Hint;

        static readonly Dictionary<BodyRegion, (float s, float ty, float oy, Rect hit)> Zoom = new Dictionary<BodyRegion, (float, float, float, Rect)>
        {
            { BodyRegion.Kepala, (2.3f, 150f, 54f, new Rect(159, 34, 72, 52)) },
            { BodyRegion.Leher, (2.6f, 105f, 96f, new Rect(165, 86, 60, 44)) },
            { BodyRegion.Dada, (1.55f, 34f, 160f, new Rect(140, 130, 110, 76)) },
            { BodyRegion.Perut, (1.5f, -56f, 252f, new Rect(140, 208, 110, 88)) },
        };

        protected override void Build(object arg)
        {
            if (arg is BodyRegion r) m_Region = r; else if (arg == null) m_Region = null;
            if (m_Body == Gender.None) m_Body = P.gender == Gender.Girl ? Gender.Girl : Gender.Boy;

            // Header: [kembali] Library [toggle tubuh]
            var toggle = U.Row(2).Pad(3).Radius(999).Bg("#EDEBF3");
            toggle.Add(BodyPill(Gender.Boy, "kid_boy"));
            toggle.Add(BodyPill(Gender.Girl, "kid_girl"));
            var bar = Parts.TopBar("Library", () => App.GoTab(NavTab.Beranda), toggle);
            bar.Q(className: "topbar__title").style.fontSize = 18;
            Root.Add(bar);

            var hintRow = U.Row(8).Margin(12, 20, 0, 20);
            var pill = U.Div().Grow().H(34).Radius(999).Bg(U.PurpleDark).Center();
            m_Hint = U.Text("", "t-small").Fg("#FFFFFF");
            pill.Add(m_Hint);
            hintRow.Add(pill);
            var list = U.Div().Size(40, 40).Radius(20).Bg("#FFFFFF").Border(1, "#CFCCDD").Center();
            list.Add(U.Icon("grid", 18));
            list.tooltip = "Lihat sebagai daftar";
            list.Tap(() => App.Go<LibraryListScreen>());
            hintRow.Add(list);
            Root.Add(hintRow);

            // Body stage (390x420)
            var clip = U.Div().H(420).W(390).NoShrink();
            clip.style.overflow = Overflow.Hidden;
            clip.style.marginTop = 18;
            m_Stage = U.Div().Size(390, 420);
            m_Stage.style.transitionProperty = new List<StylePropertyName> { "translate", "scale", "transform-origin" };
            m_Stage.style.transitionDuration = new List<TimeValue> { 0.55f, 0.55f, 0.55f };
            m_Stage.style.transitionTimingFunction = new List<EasingFunction> { EasingMode.EaseOutCubic, EasingMode.EaseOutCubic, EasingMode.EaseOutCubic };
            m_BodySvg = new SvgImage();
            m_BodySvg.AddToClassList("fill");
            m_Stage.Add(m_BodySvg);
            m_Labels = U.Div("fill");
            m_Labels.pickingMode = PickingMode.Ignore;
            foreach (var (text, y) in new[] { ("Kepala", 60f), ("Leher", 110f), ("Dada", 164f), ("Perut", 254f) })
            {
                var l = U.Text(text).FontSize(12).Bold().W(80).TextCenter().Abs(288, y - 14);
                m_Labels.Add(l);
            }
            m_Stage.Add(m_Labels);
            clip.Add(m_Stage);

            m_Targets = U.Div("fill");
            m_Targets.pickingMode = PickingMode.Ignore;
            foreach (var kv in Zoom)
            {
                var region = kv.Key;
                var hit = kv.Value.hit;
                var t = U.Div().Abs(hit.x, hit.y).Size(hit.width, hit.height).Center();
                var dot = U.Div().Size(18, 18).Radius(9).Bg("#FFFFFF").Border(2, U.TealDark);
                var ring = U.Div().Abs(-6, -6, -6, -6).Radius(999).Border(2, U.Teal);
                ring.name = "ping";
                dot.Add(ring);
                t.Add(dot);
                t.tooltip = OrganDefaults.RegionName(region);
                t.Tap(() => Pick(region));
                m_Targets.Add(t);
            }
            clip.Add(m_Targets);
            Root.Add(clip);

            // Default: daftar area tubuh
            m_RegionList = U.Col(10).Pad(0, 20, 0, 20).Abs(0, 540, 0);
            m_RegionList.Add(U.Text("Atau pilih bagian tubuh", "t-section"));
            var grid = U.Col(10);
            var row1 = U.Row(10); var row2 = U.Row(10);
            row1.Add(RegionButton(BodyRegion.Kepala, "region_head", "#FFF1F4", "Mulut"));
            row1.Add(RegionButton(BodyRegion.Leher, "region_neck", "#E8F8F5", "Kerongkongan"));
            row2.Add(RegionButton(BodyRegion.Dada, "region_chest", "#F3EFFD", "Jantung & paru-paru"));
            row2.Add(RegionButton(BodyRegion.Perut, "region_belly", "#FFF1F4", "Lambung & usus"));
            grid.Add(row1); grid.Add(row2);
            m_RegionList.Add(grid);
            Root.Add(m_RegionList);

            // Sheet organ per area
            m_Sheet = U.Col(0).Abs(0, 500, 0, 0).Bg("#FFFFFF").Pad(10, 20, 0, 20);
            m_Sheet.style.borderTopLeftRadius = 24; m_Sheet.style.borderTopRightRadius = 24;
            m_Sheet.style.borderTopWidth = 1; m_Sheet.style.borderTopColor = U.C(U.Line);
            Root.Add(m_Sheet);

            Apply();
        }

        VisualElement BodyPill(Gender g, string icon)
        {
            bool on = m_Body == g;
            var b = U.Div().Size(38, 38).Radius(19).Center().Bg(on ? "#FFFFFF" : "#00000000").Opacity(on ? 1 : 0.7f);
            if (on) b.Border(2, U.Teal);
            b.Add(U.Icon(icon, 28));
            b.tooltip = g == Gender.Girl ? "Tubuh perempuan" : "Tubuh laki-laki";
            b.Tap(() => { m_Body = g; Refresh(m_Region.HasValue ? (object)m_Region.Value : null); });
            return b;
        }

        VisualElement RegionButton(BodyRegion r, string icon, string bg, string organs)
        {
            var b = U.Row(10).Grow().H(72).Pad(0, 12).Radius(16).Bg("#FFFFFF").Border(1, U.Line);
            b.Add(U.Div().Size(40, 40).Radius(20).Bg(bg).Center().Kids(U.Icon(icon, 26)));
            var t = U.Col();
            t.Add(U.Text(OrganDefaults.RegionName(r), "t-body").Bold());
            t.Add(U.Text(organs, "t-tiny"));
            b.Add(t);
            b.Tap(() => Pick(r));
            return b;
        }

        void Pick(BodyRegion? r)
        {
            m_Region = r;
            Apply();
        }

        void Apply()
        {
            bool girl = m_Body == Gender.Girl;
            var reg = m_Region;
            bool fade = reg == BodyRegion.Dada || reg == BodyRegion.Perut || reg == BodyRegion.Leher;
            m_BodySvg.SetMarkup(OrganoIcons.Fill("body_template", new Dictionary<string, string>
            {
                { "shirtFill", girl ? "#F7B6C8" : "#8EDDD3" }, { "shirtStroke", girl ? "#E07A98" : "#5FBFB2" },
                { "shirtOp", fade ? "0.12" : "1" }, { "smileOp", reg == BodyRegion.Kepala ? "0" : "1" },
                { "headOp", reg == BodyRegion.Kepala ? "1" : "0" }, { "neckOp", reg == BodyRegion.Leher ? "1" : "0" },
                { "chestOp", reg == BodyRegion.Dada ? "1" : "0" }, { "bellyOp", reg == BodyRegion.Perut ? "1" : "0" },
                { "labelOp", "1" }, { "boyDisplay", girl ? "none" : "inline" }, { "girlDisplay", girl ? "inline" : "none" },
                { "bodyAlt", "Tubuh" }
            }));

            if (reg.HasValue)
            {
                var z = Zoom[reg.Value];
                m_Stage.style.transformOrigin = new TransformOrigin(195, z.oy);
                m_Stage.style.translate = new Translate(0, z.ty);
                m_Stage.style.scale = new Scale(new Vector3(z.s, z.s, 1));
            }
            else
            {
                m_Stage.style.transformOrigin = new TransformOrigin(195, 210);
                m_Stage.style.translate = new Translate(0, 0);
                m_Stage.style.scale = new Scale(Vector3.one);
            }
            m_Labels.style.opacity = reg.HasValue ? 0 : 1;
            m_Targets.Visible(!reg.HasValue);
            m_RegionList.Visible(!reg.HasValue);
            m_Sheet.Visible(reg.HasValue);
            m_Hint.text = reg.HasValue
                ? "Pilih organ di bagian " + OrganDefaults.RegionName(reg.Value).ToLowerInvariant()
                : "Ketuk bagian tubuh untuk melihat organnya!";

            m_Sheet.Clear();
            if (!reg.HasValue) return;
            m_Sheet.Add(U.Div("sheet__grip"));
            var head = U.Row().Justify(Justify.SpaceBetween).H(52);
            var ht = U.Col();
            ht.Add(U.Text(OrganDefaults.RegionName(reg.Value), "t-h2"));
            ht.Add(U.Text("Ketuk organ untuk melihatnya dari dekat", "t-caption"));
            head.Add(ht);
            var close = U.IconButton("close", () => Pick(null));
            close.style.marginRight = -10;
            close.tooltip = "Kembali ke tubuh";
            head.Add(close);
            m_Sheet.Add(head);
            var items = U.Row(12);
            items.style.marginTop = 8;
            foreach (var o in Db.ByRegion(reg.Value))
                items.Add(OrganTile(o, 104, 132));
            m_Sheet.Add(items);
        }

        VisualElement OrganTile(OrganInfo o, float w, float h)
        {
            bool got = P.HasCard(o.id);
            var t = U.Col(8).Size(w, h).Radius(16).Center().Bg(o.tint).Border(1, o.ring);
            t.Add(U.Icon(o.IconKey, 56));
            t.Add(U.Text(o.displayName, "t-small").Bold().TextCenter());
            var badge = U.Div().Size(22, 22).Radius(11).Abs(null, 8, 8).Center();
            if (got) { badge.Bg(U.TealDark); badge.Add(U.Icon("check", 12)); }
            else { badge.Bg("#FFFFFF").Border(1, U.Ink); badge.Add(U.Text("?").FontSize(12).Bold()); }
            t.Add(badge);
            t.tooltip = o.displayName + (got ? ", kartu sudah terkumpul" : ", kartu belum terkumpul");
            t.Tap(() => App.Go<OrganDetailScreen>(o.id));
            return t;
        }

        public override void Tick(float dt)
        {
            if (m_Targets == null || m_Region.HasValue) return;
            float p = Mathf.Repeat(Time.unscaledTime * 0.7f, 1f);
            m_Targets.Query<VisualElement>("ping").ForEach(ring =>
            {
                ring.style.scale = new Scale(new Vector3(1 + p, 1 + p, 1));
                ring.style.opacity = 1 - p;
            });
        }

        public override bool OnBack()
        {
            if (!m_Region.HasValue) return false;
            Pick(null);
            return true;
        }
    }

    // ==================================================================== Library: daftar

    public class LibraryListScreen : AppScreen
    {
        public override NavTab Tab => NavTab.Library;
        OrganSystem? m_Filter;
        bool m_Saraf;

        protected override void Build(object arg)
        {
            if (arg is OrganSystem s) { m_Filter = s; m_Saraf = s == OrganSystem.Saraf; }
            var body = U.IconButton("kid_boy", () => App.Go<LibraryBodyScreen>(push: false), 28, "Lihat tubuh");
            var bar = Parts.TopBar("Library", App.Back, body);
            bar.Q(className: "topbar__title").style.fontSize = 18;
            Root.Add(bar);

            var chips = U.Scroll(true);
            chips.style.flexGrow = 0;
            chips.style.flexShrink = 0;
            chips.contentContainer.style.paddingTop = 8;
            chips.contentContainer.style.flexDirection = FlexDirection.Row;
            Chip(chips, "Semua", !m_Filter.HasValue && !m_Saraf, () => { m_Filter = null; m_Saraf = false; Refresh(""); });
            Chip(chips, "Pencernaan", m_Filter == OrganSystem.Pencernaan, () => Refresh(OrganSystem.Pencernaan));
            Chip(chips, "Pernapasan", m_Filter == OrganSystem.Pernapasan, () => Refresh(OrganSystem.Pernapasan));
            Chip(chips, "Peredaran Darah", m_Filter == OrganSystem.PeredaranDarah, () => Refresh(OrganSystem.PeredaranDarah));
            Chip(chips, "Saraf", m_Saraf, () => Refresh(OrganSystem.Saraf));
            Root.Add(chips);

            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 12;
            Root.Add(scroll);
            var c = U.Col(22);
            scroll.Add(c);

            foreach (var sys in new[] { OrganSystem.Pencernaan, OrganSystem.Pernapasan, OrganSystem.PeredaranDarah })
            {
                if (m_Filter.HasValue && m_Filter != sys) continue;
                var sec = U.Col(10);
                var (fg, _) = OrganDefaults.SystemColors(sys);
                var h = U.Row(8);
                h.Add(U.Div().Size(8, 8).Radius(4).Bg(fg));
                h.Add(U.Text("Sistem " + OrganDefaults.SystemName(sys), "t-section"));
                sec.Add(h);
                var grid = U.Row().Align(Align.FlexStart);
                grid.style.flexWrap = Wrap.Wrap;
                int n = 0;
                foreach (var o in Db.BySystem(sys))
                {
                    var tile = Tile(o);
                    if (n % 3 == 0) tile.style.marginLeft = 0;
                    tile.style.marginBottom = 10;
                    grid.Add(tile);
                    n++;
                }
                sec.Add(grid);
                c.Add(sec);
            }

            if (!m_Filter.HasValue || m_Saraf)
            {
                var soon = U.Row(12, "card", "card-lg").Bg("#FFFBEB").Border(1, "#F1D77A");
                soon.Add(U.Div().Size(48, 48).Radius(24).Bg(U.YellowSoft).Center().Kids(U.Icon("cat_saraf", 28)));
                var t = U.Col(2).Grow();
                t.Add(U.Text("Sistem Saraf segera hadir", "t-body").Bold());
                t.Add(U.Text("Otak dan saraf sedang disiapkan. Nantikan, ya!", "t-caption"));
                soon.Add(t);
                c.Add(soon);
            }
        }

        static void Chip(VisualElement parent, string label, bool on, System.Action tap)
        {
            var chip = U.Div("chip");
            chip.EnableInClassList("chip--on", on);
            chip.Add(U.Text(label));
            chip.Tap(tap);
            parent.Add(chip);
        }

        VisualElement Tile(OrganInfo o)
        {
            bool got = P.HasCard(o.id);
            var t = U.Col(10).Size(110, 128).Radius(16).Center().Bg("#FFFFFF").Border(1, U.Line);
            t.style.marginLeft = 10;
            t.Add(U.Div().Size(60, 60).Radius(30).Bg(o.tint).Center().Kids(U.Icon(o.IconKey, 40)));
            t.Add(U.Text(o.displayName, "t-small").Bold().TextCenter());
            var badge = U.Div().Size(22, 22).Radius(11).Abs(null, 8, 8).Center();
            if (got) { badge.Bg(U.Teal); badge.Add(U.Icon("check", 12)); }
            else { badge.Bg("#FFFFFF").Border(1, U.Ink); badge.Add(U.Text("?").FontSize(12).Bold()); }
            t.Add(badge);
            t.Tap(() => App.Go<OrganDetailScreen>(o.id));
            return t;
        }
    }

    // ==================================================================== Close-up organ (3D)

    public class OrganDetailScreen : AppScreen
    {
        public override NavTab Tab => NavTab.None;

        OrganInfo m_Organ;
        Label m_About;
        VisualElement m_Fact;
        string[] m_Sentences;
        bool m_Playing;
        float m_PlayT, m_PlayDur;
        bool m_UsingClip;
        static bool s_ToldAboutVoice;

        protected override void Build(object arg)
        {
            m_Organ = Db.Get(arg as string) ?? Db.Get("lambung");
            var o = m_Organ;
            StopVoice();
            P.MarkOpened(o.id);

            bool fav = P.favorites.Contains(o.id);
            var favBtn = U.IconButton("bookmark", null);
            ((SvgImage)favBtn[0]).SetMarkup(OrganoIcons.Get("bookmark").Replace("fill=\"none\" stroke=\"#2E2A4A\"",
                fav ? "fill=\"#EF476F\" stroke=\"#EF476F\"" : "fill=\"none\" stroke=\"#2E2A4A\""));
            favBtn.Tap(() => { P.ToggleFavorite(o.id); Refresh(o.id); });
            favBtn.style.marginRight = -12;
            favBtn.tooltip = "Tandai favorit";
            Root.Add(Parts.TopBar(o.displayName, App.Back, favBtn));

            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 8;
            Root.Add(scroll);
            var c = U.Col(16);
            scroll.Add(c);

            // ---------- Close-up 3D card ----------
            var card = U.Div().H(250).Radius(16).Bg(o.tint).Border(1, o.ring);
            card.style.overflow = Overflow.Hidden;
            var ringOuter = U.Div().Size(236, 236).Radius(118).Border(1.5f, o.ring).Abs(175 - 118, 125 - 118);
            var ringInner = U.Div().Size(176, 176).Radius(88).Bg(U.C("#FFFFFF", 0.55f)).Border(1.5f, o.ring).Abs(175 - 88, 125 - 88);
            ringOuter.pickingMode = ringInner.pickingMode = PickingMode.Ignore;
            card.Add(ringOuter);
            card.Add(ringInner);

            var viewport = new OrganViewport(App.Viewer, o, true, "Geser untuk memutar · ketuk untuk dengar");
            viewport.AddToClassList("fill");
            viewport.Tapped += ToggleVoice;
            card.Add(viewport);

            var chipL = U.Row(6).Abs(12, 12).Bg("#FFFFFF").Border(1, U.Line).Radius(999).Pad(6, 10);
            chipL.pickingMode = PickingMode.Ignore;
            chipL.Add(U.Icon("zoom", 14));
            chipL.Add(U.Text("Close-up 3D").FontSize(11));
            card.Add(chipL);
            var chipR = U.Row(6).Abs(null, 12, 12).Bg(U.TealDark).Radius(999).Pad(6, 10).H(28);
            chipR.Add(U.Icon("ar", 14));
            chipR.Add(U.Text("Lihat di AR").FontSize(11).Fg("#FFFFFF"));
            chipR.Tap(() => App.Go<ScanScreen>(o.id));
            card.Add(chipR);

            var loc = U.Col(2).W(72).Abs(12, null, null, 12).Pad(6, 4).Radius(12).Bg("#FFFFFF").Border(1, U.Line).Align(Align.Center);
            loc.pickingMode = PickingMode.Ignore;
            loc.Add(U.Icon(RegionIcon(o.region), 26));
            loc.Add(U.Text("Letaknya di").FontSize(9).Fg(U.Muted));
            loc.Add(U.Text(o.WhereLabel).FontSize(11).Bold());
            card.Add(loc);
            c.Add(card);

            // ---------- Title ----------
            var titleCol = U.Col(6).Align(Align.FlexStart);
            titleCol.Add(U.Text(o.displayName, "t-h1"));
            titleCol.Add(U.Pill(o.SystemLabel, U.PurpleDark, "#FFFFFF"));
            c.Add(titleCol);

            // ---------- Tentang ----------
            var about = U.Col(8);
            var ah = U.Row(8);
            ah.Add(U.Text("Tentang " + o.displayName, "t-section").Grow());
            var listen = U.Row(4).Pad(4, 10).Radius(999).Bg(U.TealSoft);
            listen.Add(U.Icon("nav_quiz", 14).Tinted(U.C(U.TealInk)));
            listen.Add(U.Text("Dengarkan").FontSize(11).Bold().Fg(U.TealInk));
            listen.Tap(ToggleVoice);
            ah.Add(listen);
            about.Add(ah);
            m_Sentences = SplitSentences(o.about);
            m_About = U.Text("", "t-body");
            m_About.style.unityParagraphSpacing = 0;
            m_About.style.whiteSpace = WhiteSpace.Normal;
            about.Add(m_About);
            c.Add(about);
            RenderAbout(-1, false);

            // ---------- Fakta ----------
            m_Fact = U.Row(12).Align(Align.FlexStart).Bg(U.TealDark).Radius(16).Pad(14);
            m_Fact.Add(U.Icon("bulb", 24));
            var ft = U.Col(2).Grow();
            ft.Add(U.Text("Tau Gak Sih?", "t-body").Bold().Fg("#FFFFFF"));
            ft.Add(U.Text(o.funFact).FontSize(12).Fg("#FFFFFF"));
            m_Fact.Add(ft);
            c.Add(m_Fact);

            // ---------- Status kartu ----------
            if (P.HasCard(o.id))
            {
                var s = U.Row(10).Bg(U.TealSoft).Border(1, "#9CE6DC").Radius(12).Pad(10, 12);
                s.Add(U.Icon("cards_ok", 22));
                s.Add(U.Text("Kartu sudah terkumpul!", "t-caption").Fg(U.TealInk).Grow());
                var link = U.Row(2);
                link.Add(U.Text("Lihat koleksi").FontSize(12).Bold().Fg(U.TealDark));
                link.Add(U.Icon("arrow_right", 14).Tinted(U.C(U.TealDark)));
                link.Tap(() => App.Go<CardCollectionScreen>(o.id));
                s.Add(link);
                c.Add(s);
            }
            else
            {
                var s = U.Row(10).Align(Align.FlexStart).Bg("#F1F6C4").Border(1, "#DDE69A").Radius(12).Pad(10, 12);
                s.Add(U.Icon("cards_star", 22));
                var col = U.Col(8).Grow();
                col.Add(U.Text("Kartu belum terkumpul. Scan kartu Organo " + o.displayName + " atau selesaikan kuisnya, kartu langsung terbuka!", "t-caption").Fg(U.Ink));
                var btns = U.Row(8);
                var scan = U.Button("Scan Kartu", () => App.Go<ScanScreen>(o.id), "btn-dark", "btn-small");
                scan.Insert(0, U.Icon("scan_small", 15)); scan[1].style.marginLeft = 6;
                btns.Add(scan);
                btns.Add(U.Button("Ke Kuis", () => App.GoTab(NavTab.Quiz), "btn-outline", "btn-small"));
                col.Add(btns);
                s.Add(col);
                c.Add(s);
            }
            c.Add(U.Space(8));

            // ---------- Aksi bawah ----------
            var bottom = U.Div().Pad(12, 20, 20, 20).Bg(U.Surface);
            var quiz = U.Button("Mulai Quiz " + OrganDefaults.SystemName(o.system), () => App.Go<QuizPlayScreen>(o.system), "btn-primary");
            quiz.Bg(U.PinkDark);
            quiz.Add(U.Icon("arrow_right", 16));
            quiz[1].style.marginLeft = 8;
            bottom.Add(quiz);
            Root.Add(bottom);
        }

        static string RegionIcon(BodyRegion r)
        {
            switch (r)
            {
                case BodyRegion.Kepala: return "region_head";
                case BodyRegion.Leher: return "region_neck";
                case BodyRegion.Dada: return "region_chest";
                default: return "region_belly";
            }
        }

        static string[] SplitSentences(string text)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(text)) return list.ToArray();
            int start = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if ((ch == '.' || ch == '!' || ch == '?') && (i + 1 == text.Length || text[i + 1] == ' '))
                {
                    list.Add(text.Substring(start, i + 1 - start).Trim());
                    start = i + 1;
                }
            }
            if (start < text.Length && text.Substring(start).Trim().Length > 0) list.Add(text.Substring(start).Trim());
            return list.ToArray();
        }

        void RenderAbout(int active, bool factActive)
        {
            var parts = new List<string>();
            for (int i = 0; i < m_Sentences.Length; i++)
                parts.Add(i == active ? "<mark=#D5F3EFFF><color=#0F6B61>" + m_Sentences[i] + "</color></mark>" : m_Sentences[i]);
            m_About.text = string.Join(" ", parts);
            if (m_Fact != null) m_Fact.Border(factActive ? 3 : 0, "#9CE6DC");
        }

        void ToggleVoice()
        {
            if (m_Playing) { StopVoice(); return; }
            m_Playing = true;
            m_PlayT = 0;
            var speech = m_Organ.about + " Tau gak sih? " + m_Organ.funFact;
            m_UsingClip = OrganoAudio.PlayVoice(m_Organ.narration);
            float speed = P.voiceSpeed == 0 ? 0.8f : P.voiceSpeed == 2 ? 1.25f : 1f;
            m_PlayDur = m_UsingClip ? m_Organ.narration.length : Mathf.Max(6f, speech.Length / 13.5f) / speed;
            if (!m_UsingClip && !s_ToldAboutVoice)
            {
                s_ToldAboutVoice = true;
                App.Toast("Rekaman suara belum dipasang. Isi slot \"Narration\" di asset " + m_Organ.displayName + ".", 3.5f);
            }
        }

        void StopVoice()
        {
            m_Playing = false;
            OrganoAudio.StopVoice();
            if (m_About != null) RenderAbout(-1, false);
        }

        public override void Tick(float dt)
        {
            if (!m_Playing) return;
            m_PlayT += dt;
            float p = m_UsingClip ? OrganoAudio.VoiceProgress : m_PlayT / m_PlayDur;
            if (p < 0 || p >= 1f || (m_UsingClip && !OrganoAudio.VoicePlaying)) { StopVoice(); return; }
            int total = m_Organ.about.Length + 14 + m_Organ.funFact.Length;
            float pos = p * total;
            int acc = 0, active = -1;
            for (int i = 0; i < m_Sentences.Length; i++)
            {
                int len = m_Sentences[i].Length + 1;
                if (pos >= acc && pos < acc + len) { active = i; break; }
                acc += len;
            }
            RenderAbout(active, pos >= m_Organ.about.Length);
        }

        public override void Close() => StopVoice();
    }
}
