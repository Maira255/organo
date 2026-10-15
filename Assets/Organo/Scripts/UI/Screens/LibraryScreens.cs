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
        VisualElement m_Stage, m_Labels, m_Targets, m_Sheet;

        /// <summary>Area tubuh yang sedang didekati (null = seluruh tubuh).</summary>
        public BodyRegion? Region => m_Region;

        const float LabelX = 288f;
        /// <summary>
        /// Titik tiap area (di sisi kanan bagian tubuh) dan tinggi labelnya, dalam koordinat gambar 390x420.
        /// Perut = bagian bawah baju di atas pinggang. Panggul = pinggul / bagian atas celana (organ reproduksi), bukan lutut.
        /// </summary>
        static readonly (BodyRegion region, float dotX, float y)[] Callouts =
        {
            (BodyRegion.Kepala, 233f, 34f), (BodyRegion.Leher, 208f, 94f), (BodyRegion.Dada, 248f, 160f), (BodyRegion.Perut, 238f, 222f),
            (BodyRegion.Panggul, 236f, 288f),
        };

        static string RegionIcon(BodyRegion r) =>
            r == BodyRegion.Kepala ? "region_head" : r == BodyRegion.Leher ? "region_neck" : r == BodyRegion.Dada ? "region_chest"
            : r == BodyRegion.Panggul ? "cat_repro" : "region_belly";

        static readonly Dictionary<BodyRegion, (float s, float ty, float oy, Rect hit)> Zoom = new Dictionary<BodyRegion, (float, float, float, Rect)>
        {
            { BodyRegion.Kepala, (2.3f, 150f, 54f, new Rect(159, 34, 72, 52)) },
            { BodyRegion.Leher, (2.6f, 105f, 96f, new Rect(165, 86, 60, 44)) },
            { BodyRegion.Dada, (1.55f, 34f, 160f, new Rect(140, 120, 110, 76)) },
            { BodyRegion.Perut, (1.5f, -40f, 252f, new Rect(140, 196, 110, 60)) },
            { BodyRegion.Panggul, (1.5f, -104f, 300f, new Rect(146, 256, 98, 58)) },
        };

        protected override void Build(object arg)
        {
            if (arg is BodyRegion r) m_Region = r; else if (arg == null) m_Region = null;
            if (m_Body == Gender.None) m_Body = P.gender == Gender.Girl ? Gender.Girl : Gender.Boy;
            // tampilan "Di tubuh" di close-up ikut pilihan tubuh di sini
            App.BodyGender = m_Body;

            // Header: [kembali] Library [toggle tubuh]
            // pilihan tubuh: dua lingkaran berwarna (teal / pink) di wadah putih, yang terpilih diberi cincin & centang
            var toggle = U.Row(6).Pad(4).Radius(999).Bg("#FFFFFF").Border(1, U.Line);
            toggle.Elev(Elevation.E1);
            toggle.Add(BodyPill(Gender.Boy, "kid_boy"));
            toggle.Add(BodyPill(Gender.Girl, "kid_girl"));
            toggle.NoShrink();
            var bar = Parts.TopBar("Pustaka", () => App.GoTab(NavTab.Beranda), toggle);
            bar.Q(className: "topbar__title").style.fontSize = 18;
            // sisi kiri & kanan dilebarkan sama supaya wadah (dan bayangannya) pas membungkus kedua ikon,
            // sementara judul tetap di tengah
            bar.Query(className: "topbar__side").ForEach(side => side.style.width = 100);
            Root.Add(bar);

            // tombol daftar & rangka melayang di sisi kiri atas panggung (label area tubuh sudah jadi petunjuknya)
            var tools = U.Col(8).Abs(20, 8);
            var list = U.Div().Size(40, 40).Radius(20).Bg("#FFFFFF").Border(1, "#CFCCDD").Center();
            list.Add(U.Icon("grid", 18));
            list.tooltip = "Lihat sebagai daftar";
            list.Tap(() => App.Go<LibraryListScreen>());
            var bones = U.Div().Size(40, 40).Radius(20).Bg("#FFFFFF").Border(1, "#CFCCDD").Center();
            bones.Add(U.Icon("bone", 20));
            bones.tooltip = "Lihat rangka (tulang)";
            bones.Tap(() => { if (Db.Get(OrganViewer3D.SkeletonId) is OrganInfo sk) Parts.OpenOrgan(App, sk); });
            tools.Add(list);
            tools.Add(bones);

            // Body stage (390x420). Di layar pendek seluruh panggung diperkecil (inner di-scale) supaya daftar area tetap terlihat.
            var clip = U.Div().H(420).W(390).NoShrink();
            clip.style.overflow = Overflow.Hidden;
            clip.style.marginTop = 8;
            var inner = U.Div().Size(390, 420);
            inner.style.transformOrigin = new TransformOrigin(Length.Percent(50), 0, 0);
            clip.Add(inner);
            m_Stage = U.Div().Size(390, 420);
            m_Stage.style.transitionProperty = new List<StylePropertyName> { "translate", "scale", "transform-origin" };
            m_Stage.style.transitionDuration = new List<TimeValue> { 0.55f, 0.55f, 0.55f };
            m_Stage.style.transitionTimingFunction = new List<EasingFunction> { EasingMode.EaseOutCubic, EasingMode.EaseOutCubic, EasingMode.EaseOutCubic };
            m_BodySvg = new SvgImage();
            m_BodySvg.AddToClassList("fill");
            m_Stage.Add(m_BodySvg);
            // titik di tepi kanan tiap area + garis penghubung ke label (wajah & baju ngk tertutup titik)
            m_Labels = U.Div("fill");
            m_Labels.pickingMode = PickingMode.Ignore;
            foreach (var (region, dotX, y) in Callouts)
            {
                var line = U.Div().Abs(dotX + 8, y - 0.75f).Size(LabelX - dotX - 8, 1.5f).Bg(U.C(U.TealDark, 0.45f));
                line.pickingMode = PickingMode.Ignore;
                m_Labels.Add(line);
                var tag = U.Row(6).Abs(LabelX, y - 14).H(28).Pad(0, 12, 0, 8).Radius(14).Bg("#FFFFFF").Border(1.5f, "#CFCCDD");
                tag.Add(U.Icon(RegionIcon(region), 16));
                tag.Add(U.Text(OrganDefaults.RegionName(region)).FontSize(12).Bold());
                var rg = region;
                tag.Tap(() => Pick(rg));
                tag.tooltip = "Lihat organ di bagian " + OrganDefaults.RegionName(region).ToLowerInvariant();
                m_Labels.Add(tag);
            }
            m_Stage.Add(m_Labels);
            inner.Add(m_Stage);

            m_Targets = U.Div("fill");
            m_Targets.pickingMode = PickingMode.Ignore;
            foreach (var kv in Zoom)
            {
                var region = kv.Key;
                var hit = kv.Value.hit;
                // area ketuk tetap menutupi bagian tubuhnya (tanpa titik di tengah wajah/baju)
                var t = U.Div().Abs(hit.x, hit.y).Size(hit.width, hit.height);
                t.tooltip = OrganDefaults.RegionName(region);
                t.Tap(() => Pick(region));
                m_Targets.Add(t);
            }
            foreach (var (region, dotX, y) in Callouts)
            {
                var dot = U.Div().Abs(dotX - 8, y - 8).Size(16, 16).Radius(8).Bg("#FFFFFF").Border(3, U.TealDark);
                var ring = U.Div().Abs(-6, -6, -6, -6).Radius(999).Border(2, U.Teal);
                ring.name = "ping";
                ring.pickingMode = PickingMode.Ignore;
                dot.Add(ring);
                var rg = region;
                dot.Tap(() => Pick(rg));
                m_Targets.Add(dot);
            }
            inner.Add(m_Targets);
            clip.Add(tools);
            // area sisa layar: panggung diletakkan sedikit di atas tengahnya (tidak menempel ke atas dengan ruang kosong di bawah)
            var area = U.Div().Grow();
            area.Add(clip);
            Root.Add(area);

            // Sheet organ per area
            m_Sheet = U.Col(0).Abs(0, 500, 0, 0).Bg("#FFFFFF").Pad(10, 20, 0, 20);
            m_Sheet.style.borderTopLeftRadius = 24; m_Sheet.style.borderTopRightRadius = 24;
            m_Sheet.style.borderTopWidth = 1; m_Sheet.style.borderTopColor = U.C(U.Line);
            // tarik sheet ke bawah untuk kembali ke seluruh tubuh
            m_Sheet.AddManipulator(new SwipeToDismiss(() => Pick(null)));
            Root.Add(m_Sheet);

            // Panggung mengikuti tinggi layar: di HP layar pendek tubuh diperkecil supaya sheet organ (±215) tetap muat
            area.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                float h = Root.resolvedStyle.height, y = area.layout.y + 8f, ah = area.layout.height;
                if (float.IsNaN(h) || h <= 0 || float.IsNaN(y) || float.IsNaN(ah)) return;
                float s = Mathf.Clamp(Mathf.Min((h - y - 16f) / 420f, (h - y - 215f) / 374f), 0.62f, 1f);
                if (!Mathf.Approximately(clip.resolvedStyle.height, 420f * s)) clip.style.height = 420f * s;
                inner.style.scale = new Scale(new Vector3(s, s, 1f));
                float off = Mathf.Round(Mathf.Min(60f, Mathf.Max(0f, ah - 8f - 420f * s) * 0.4f)); // maks. 60: bagian yang didekati tetap di atas sheet organ
                clip.style.marginTop = 8f + off;
                // sheet organ tetap di posisi semula (tidak ikut turun) supaya isinya muat
                m_Sheet.style.top = y + 374f * s;
            });

            Apply();
        }

        /// <summary>Ganti tubuh anak laki-laki / perempuan.</summary>
        public void SetBody(Gender g) { m_Body = g; Refresh(m_Region.HasValue ? (object)m_Region.Value : null); }

        VisualElement BodyPill(Gender g, string icon)
        {
            bool on = m_Body == g;
            bool girl = g == Gender.Girl;
            string soft = girl ? U.PinkSoft : U.TealSoft, strong = girl ? U.Pink : U.Teal;
            var b = U.Div().Size(38, 38).Radius(19).Center().Bg(on ? soft : "#F4F3F8");
            b.Border(2, on ? strong : "#00000000");
            var face = U.Icon(icon, 30);
            if (!on) face.style.opacity = 0.55f;
            b.Add(face);
            if (on)
            {
                var tick = U.Div().Size(14, 14).Radius(7).Abs(null, null, -3, -3).Bg(strong).Border(1.5f, "#FFFFFF").Center();
                tick.pickingMode = PickingMode.Ignore;
                tick.Add(U.Icon("check", 8));
                b.Add(tick);
            }
            b.tooltip = g == Gender.Girl ? "Tubuh perempuan" : "Tubuh laki-laki";
            b.Tap(() => SetBody(g));
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
                { "labelOp", reg.HasValue ? "0" : "1" }, { "boyDisplay", girl ? "none" : "inline" }, { "girlDisplay", girl ? "inline" : "none" },
                { "bodyAlt", "Tubuh" }, { "hairOp", reg == BodyRegion.Kepala ? "0.5" : "1" }
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
            m_Labels.Visible(!reg.HasValue);
            m_Targets.Visible(!reg.HasValue);
            m_Sheet.Visible(reg.HasValue);

            m_Sheet.Clear();
            if (!reg.HasValue) return;
            m_Sheet.Add(U.Div("sheet__grip"));
            var head = U.Row().Justify(Justify.SpaceBetween).H(52);
            head.Add(U.Text(OrganDefaults.RegionName(reg.Value), "t-h2"));
            var close = U.IconButton("close", () => Pick(null));
            close.style.marginRight = -10;
            close.tooltip = "Kembali ke tubuh";
            head.Add(close);
            m_Sheet.Add(head);
            var rail = U.Scroll(true);
            rail.style.flexGrow = 0; rail.style.flexShrink = 0;
            rail.style.marginLeft = -20; rail.style.marginRight = -20;
            rail.contentContainer.style.flexDirection = FlexDirection.Row;
            rail.contentContainer.style.paddingTop = 8;
            rail.contentContainer.style.paddingBottom = 8;
            rail.contentContainer.style.paddingLeft = 20;
            // Panggul: organ reproduksi sesuai tubuh yang sedang tampil (tidak lagi digabung ke perut)
            string reproId = m_Body == Gender.Girl ? ReproAccess.Girl : ReproAccess.Boy;
            var organs = Db.ByRegion(reg.Value).Where(o => !ReproAccess.IsRepro(o) || o.id == reproId).ToList();
            foreach (var o in organs)
            {
                var tile = OrganTile(o, 104, 132);
                tile.style.marginRight = 12;
                rail.Add(tile);
            }
            m_Sheet.Add(rail);
        }

        VisualElement OrganTile(OrganInfo o, float w, float h)
        {
            bool got = P.HasCard(o.id), locked = Parts.Locked(o);
            var t = U.Col(8).Size(w, h).Radius(16).Center().Bg(locked ? U.C("#F6F5F9") : o.tint).Border(1, locked ? U.C(U.Line) : o.ring);
            t.Add(U.Icon(Parts.MiniIcon(o), locked ? 40 : 54));
            var name = U.Text(o.displayName, "t-small").Bold().TextCenter().Fg(locked ? U.C(U.Muted) : U.C(U.Ink));
            name.style.whiteSpace = WhiteSpace.Normal;
            t.Add(name);
            if (!locked)
            {
                var badge = U.Div().Size(22, 22).Radius(11).Abs(null, 8, 8).Center();
                if (got) { badge.Bg(U.TealDark); badge.Add(U.Icon("check", 12)); }
                else { badge.Bg("#FFFFFF").Border(1, U.Ink); badge.Add(U.Text("?").FontSize(12).Bold()); }
                t.Add(badge);
            }
            t.tooltip = o.displayName + (locked ? ", masih terkunci" : got ? ", kartu sudah terkumpul" : ", kartu belum terkumpul");
            t.Tap(() => Parts.OpenOrgan(App, o));
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
        bool m_Saraf, m_Fav;

        protected override void Build(object arg)
        {
            if (arg is OrganSystem s) { m_Filter = s; m_Saraf = s == OrganSystem.Saraf; m_Fav = false; }
            else if (arg as string == "favorit") { m_Filter = null; m_Saraf = false; m_Fav = true; }
            else if (arg as string == "") { m_Filter = null; m_Saraf = false; m_Fav = false; }
            var body = U.IconButton("kid_boy", () => App.Go<LibraryBodyScreen>(push: false), 28, "Lihat tubuh");
            var bar = Parts.TopBar("Pustaka", App.Back, body);
            bar.Q(className: "topbar__title").style.fontSize = 18;
            Root.Add(bar);

            var chips = U.Scroll(true);
            chips.style.flexGrow = 0;
            chips.style.flexShrink = 0;
            chips.contentContainer.style.paddingTop = 8;
            chips.contentContainer.style.flexDirection = FlexDirection.Row;
            Chip(chips, "Semua", !m_Filter.HasValue && !m_Saraf && !m_Fav, () => Refresh(""));
            Chip(chips, "♥ Favorit", m_Fav, () => Refresh("favorit"));
            Chip(chips, "Pencernaan", m_Filter == OrganSystem.Pencernaan, () => Refresh(OrganSystem.Pencernaan));
            Chip(chips, "Pernapasan", m_Filter == OrganSystem.Pernapasan, () => Refresh(OrganSystem.Pernapasan));
            Chip(chips, "Peredaran Darah", m_Filter == OrganSystem.PeredaranDarah, () => Refresh(OrganSystem.PeredaranDarah));
            Chip(chips, "Ekskresi", m_Filter == OrganSystem.Ekskresi, () => Refresh(OrganSystem.Ekskresi));
            Chip(chips, "Saraf", m_Saraf, () => Refresh(OrganSystem.Saraf));
            Chip(chips, "Indera", m_Filter == OrganSystem.Indera, () => Refresh(OrganSystem.Indera));
            Chip(chips, "Gerak", m_Filter == OrganSystem.Gerak, () => Refresh(OrganSystem.Gerak));
            Chip(chips, "Reproduksi", m_Filter == OrganSystem.Reproduksi, () => Refresh(OrganSystem.Reproduksi));
            Root.Add(chips);

            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 12;
            Root.Add(scroll);
            var c = U.Col(22);
            scroll.Add(c);

            if (m_Fav)
            {
                var favs = P.favorites.Select(id => Db.Get(id)).Where(f => f != null).ToList();
                var sec = U.Col(10);
                var h = U.Row(8);
                h.Add(U.Div().Size(8, 8).Radius(4).Bg(U.Pink));
                h.Add(U.Text("Favoritku", "t-section"));
                sec.Add(h);
                if (favs.Count == 0)
                {
                    var empty = U.Col(8, "card", "card-lg").Align(Align.Center).Bg("#FFF1F4").Border(1, "#F9C9D5");
                    empty.Add(U.Icon("bookmark", 34));
                    empty.Add(U.Text("Belum ada organ favorit", "t-body").Bold());
                    var et = U.Text("Ketuk ikon bookmark di halaman organ.", "t-caption").TextCenter();
                    et.style.whiteSpace = WhiteSpace.Normal;
                    empty.Add(et);
                    sec.Add(empty);
                }
                else
                {
                    var grid = U.Row().Align(Align.FlexStart);
                    grid.style.flexWrap = Wrap.Wrap;
                    int n = 0;
                    foreach (var o in favs)
                    {
                        var tile = Tile(o);
                        if (n % 3 == 0) tile.style.marginLeft = 0;
                        tile.style.marginBottom = 10;
                        grid.Add(tile);
                        n++;
                    }
                    sec.Add(grid);
                }
                c.Add(sec);
                return;
            }

            foreach (var sys in new[] { OrganSystem.Pencernaan, OrganSystem.Pernapasan, OrganSystem.PeredaranDarah, OrganSystem.Ekskresi, OrganSystem.Saraf, OrganSystem.Indera, OrganSystem.Gerak, OrganSystem.Reproduksi })
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
            bool got = P.HasCard(o.id), locked = Parts.Locked(o);
            var t = U.Col(10).Size(110, 128).Radius(16).Center().Bg(locked ? "#FBFAFD" : "#FFFFFF").Border(1, U.Line);
            t.Elev(Elevation.E1);
            t.style.marginLeft = 10;
            t.Add(U.Div().Size(64, 64).Radius(32).Bg(locked ? U.C("#F1F0F6") : o.tint).Center().Kids(U.Icon(Parts.MiniIcon(o), locked ? 30 : 44)));
            var name = U.Text(o.displayName, "t-small").Bold().TextCenter().Fg(locked ? U.C(U.Muted) : U.C(U.Ink));
            name.style.whiteSpace = WhiteSpace.Normal;
            t.Add(name);
            var badge = U.Div().Size(22, 22).Radius(11).Abs(null, 8, 8).Center();
            if (locked) { badge.Bg(U.Muted); badge.Add(U.Icon("lock_tiny", 10)); }
            else if (got) { badge.Bg(U.Teal); badge.Add(U.Icon("check", 12)); }
            else { badge.Bg("#FFFFFF").Border(1, U.Ink); badge.Add(U.Text("?").FontSize(12).Bold()); }
            t.Add(badge);
            if (P.favorites.Contains(o.id))
            {
                var fav = U.Div().Size(22, 22).Radius(11).Abs(8, 8).Bg(U.Pink).Center();
                fav.pickingMode = PickingMode.Ignore;
                fav.Add(U.Icon("bookmark", 12).Tinted(Color.white));
                t.Add(fav);
            }
            t.Tap(() => Parts.OpenOrgan(App, o));
            return t;
        }
    }

    // ==================================================================== Close-up organ (3D)

    public class OrganDetailScreen : AppScreen
    {
        public override NavTab Tab => NavTab.None;

        OrganInfo m_Organ;
        OrganViewer3D.ViewMode m_View;
        bool m_ShowBones;
        Label m_About;
        ScrollView m_PageScroll;
        VisualElement m_AboutSection;
        VisualElement m_Fact;
        string[] m_Sentences;
        bool m_Playing;
        float m_PlayT, m_PlayDur;
        bool m_UsingClip, m_UsingTts;
        static bool s_ToldAboutVoice;
        VisualElement m_VoiceToggle, m_Listen;

        // bagian organ
        OrganViewport m_Viewport;
        OrganContentData m_Content;
        int m_Part = -1;
        readonly List<VisualElement> m_PartCards = new List<VisualElement>();
        VisualElement m_PartPanel, m_PartChip;
        Label m_PartChipText;
        System.Action<OrganViewer3D.ViewMode, bool> m_SetMode;
        // tombol di atas model 3D yang menyingkir saat model diperbesar (supaya tengkorak dsb. bisa dilihat)
        readonly List<VisualElement> m_ZoomHide = new List<VisualElement>();
        VisualElement m_ZoomReset;
        bool m_ZoomedUI;
        // fakta menarik
        readonly List<string> m_Facts = new List<string>();
        int m_FactIndex;
        Label m_FactText, m_FactCount;

        protected override void Build(object arg)
        {
            var key = arg as string ?? "lambung";
            string partArg = null;
            int hash = key.IndexOf('#');
            if (hash >= 0) { partArg = key.Substring(hash + 1); key = key.Substring(0, hash); m_View = OrganViewer3D.ViewMode.Organ; }
            if (key.EndsWith("|tubuh")) { m_View = OrganViewer3D.ViewMode.Body; key = key.Substring(0, key.Length - 6); }
            else if (key.EndsWith("|dalam")) { m_View = OrganViewer3D.ViewMode.Inside; key = key.Substring(0, key.Length - 6); }
            else if (key.EndsWith("|organ")) { m_View = OrganViewer3D.ViewMode.Organ; key = key.Substring(0, key.Length - 6); }
            m_Organ = Db.Get(key) ?? Db.Get("lambung");
            var o = m_Organ;
            StopVoice();
            if (Parts.Locked(o)) { BuildLocked(o); return; }
            m_ShowBones = false;
            bool repro = ReproAccess.IsRepro(o);
            if (repro && m_View == OrganViewer3D.ViewMode.Body) m_View = OrganViewer3D.ViewMode.Organ;
            m_Content = OrganContent.Get(o.id);
            m_Part = -1;
            m_PartCards.Clear();
            m_PartPanel = null;
            if (partArg != null && m_Content != null)
                m_Part = System.Array.FindIndex(m_Content.parts, x => x.id == partArg);
            P.MarkOpened(o.id);

            bool fav = P.favorites.Contains(o.id);
            var favBtn = U.IconButton("bookmark", null);
            ((SvgImage)favBtn[0]).SetMarkup(OrganoIcons.Get("bookmark").Replace("fill=\"none\" stroke=\"#2E2A4A\"",
                fav ? "fill=\"#EF476F\" stroke=\"#EF476F\"" : "fill=\"none\" stroke=\"#2E2A4A\""));
            favBtn.Tap(() =>
            {
                P.ToggleFavorite(o.id);
                bool now = P.favorites.Contains(o.id);
                ((SvgImage)favBtn[0]).SetMarkup(OrganoIcons.Get("bookmark").Replace("fill=\"none\" stroke=\"#2E2A4A\"",
                    now ? "fill=\"#EF476F\" stroke=\"#EF476F\"" : "fill=\"none\" stroke=\"#2E2A4A\""));
                App.Toast(now ? "Disimpan ke Favoritku" : "Dihapus dari Favoritku", 1.6f);
            });
            favBtn.style.marginRight = -12;
            favBtn.tooltip = fav ? "Hapus dari Favoritku" : "Simpan ke Favoritku";
            Root.Add(Parts.TopBar(o.displayName, App.Back, favBtn));

            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 8;
            Root.Add(scroll);
            m_PageScroll = scroll;
            var c = U.Col(16);
            scroll.Add(c);

            // ---------- Close-up 3D card ----------
            bool tall = o.id == OrganViewer3D.SkeletonId; // rangka setinggi badan: kartu 3D lebih tinggi
            m_ZoomHide.Clear();
            m_ZoomedUI = false;
            var card = U.Div().H(tall ? 300 : 250).Radius(16).Bg(o.tint).Border(1, o.ring);
            card.Elev(Elevation.E2);
            card.style.overflow = Overflow.Hidden;
            var ringOuter = U.Div().Size(236, 236).Radius(118).Border(1.5f, o.ring).Abs(175 - 118, 125 - 118);
            var ringInner = U.Div().Size(176, 176).Radius(88).Bg(U.C("#FFFFFF", 0.55f)).Border(1.5f, o.ring).Abs(175 - 88, 125 - 88);
            ringOuter.pickingMode = ringInner.pickingMode = PickingMode.Ignore;
            // lingkaran hiasan selalu di tengah kartu (tinggi kartu menyesuaikan layar)
            foreach (var ring in new[] { ringOuter, ringInner })
            {
                ring.style.top = Length.Percent(50);
                ring.style.translate = new Translate(0, Length.Percent(-50));
            }
            card.Add(ringOuter);
            card.Add(ringInner);

            var viewport = new OrganViewport(App.Viewer, o, false) { CalloutOnTapOnly = true };
            viewport.AddToClassList("fill");
            // ketuk model tidak lagi membacakan: sering tersentuh tanpa sengaja saat memutar model
            // bagian yang dipilih dari halaman lain langsung disorot setelah model terpasang
            viewport.RegisterCallback<AttachToPanelEvent>(_ => { if (m_Part >= 0) ApplyPart3D(); });
            card.Add(viewport);
            m_Viewport = viewport;

            // Pilih tampilan: organ utuh, "Dalam" (organ dipotong, terlihat isinya) atau "Tubuh" (letaknya di tubuh)
            bool hasInside = o.insidePrefab != null;
            if (m_View == OrganViewer3D.ViewMode.Inside && !hasInside) m_View = OrganViewer3D.ViewMode.Organ;
            var seg = U.Row(2).Abs(12, 12).Bg("#FFFFFF").Border(1, U.Line).Radius(999).Pad(3);
            var segs = new List<(VisualElement el, OrganViewer3D.ViewMode mode)>();
            VisualElement modeHint = null;
            Label modeHintText = null;
            VisualElement locBox = null;
            bool skeleton = o.id == OrganViewer3D.SkeletonId;
            var bonesChip = U.Row(5).Abs(null, 12, 12).Pad(5, 10).Radius(999).Bg("#FFFFFF").Border(1, U.Line);
            var bonesIcon = U.Icon("bone", 14);
            var bonesText = U.Text("Tulang").FontSize(11).Bold();
            bonesChip.Add(bonesIcon); bonesChip.Add(bonesText);
            bonesChip.tooltip = "Tampilkan tulang tembus pandang";
            void PaintBones()
            {
                bonesChip.Bg(m_ShowBones ? U.C(U.Ink) : U.C("#FFFFFF"));
                bonesText.style.color = U.C(m_ShowBones ? "#FFFFFF" : U.Ink);
                bonesIcon.Tint = U.C(m_ShowBones ? "#FFFFFF" : U.Ink);
            }
            bonesChip.Tap(() =>
            {
                m_ShowBones = !m_ShowBones;
                App.Viewer?.ShowBones(m_ShowBones);
                PaintBones();
            });
            void SetMode(OrganViewer3D.ViewMode view, bool keepPart)
            {
                m_View = view;
                if (m_Part >= 0 && !keepPart && !PartFits(m_Content.parts[m_Part], view)) { m_Part = -1; UpdatePartUI(); }
                viewport.SetViewMode(view);
                foreach (var (el, mode) in segs)
                {
                    bool on = mode == view;
                    el.Bg(on ? U.Teal : "#00000000");
                    ((Label)el[1]).style.color = U.C(on ? "#FFFFFF" : U.Ink);
                    ((SvgImage)el[0]).Tint = U.C(on ? "#FFFFFF" : U.Ink);
                }
                bool body = view == OrganViewer3D.ViewMode.Body, inside = view == OrganViewer3D.ViewMode.Inside;
                modeHint.Visible(body || inside);
                modeHintText.text = body ? (skeleton ? "Rangka di dalam tubuh" : o.displayName + " menyala")
                    : skeleton ? "Isi tulang pipa, pipih & pendek" : "Tampak dalam " + o.displayName.ToLowerInvariant();
                bonesChip.Visible(body && !skeleton);
                if (body && !skeleton) { App.Viewer?.ShowBones(m_ShowBones); PaintBones(); }
                locBox?.Visible(!body);
                if (!keepPart && m_Part >= 0) ApplyPart3D();
            }
            VisualElement Segment(string icon, string label, OrganViewer3D.ViewMode mode, string tip)
            {
                var el = U.Row(4).Pad(5, 9).Radius(999);
                el.Add(U.Icon(icon, 13));
                el.Add(U.Text(label).FontSize(11).Bold());
                el.Tap(() => SetMode(mode, false));
                el.tooltip = tip;
                segs.Add((el, mode));
                seg.Add(el);
                return el;
            }
            Segment("zoom", "Organ", OrganViewer3D.ViewMode.Organ, "Lihat organ utuh");
            if (hasInside) Segment("eye", "Dalam", OrganViewer3D.ViewMode.Inside, "Lihat bagian dalam organ (organ dipotong)");
            // organ reproduksi tidak ditempatkan di model tubuh anak; letaknya cukup ditunjukkan di peta kecil
            if (!repro) Segment("set_body", "Tubuh", OrganViewer3D.ViewMode.Body, "Lihat letak organ di dalam tubuh");
            card.Add(seg);
            // keterangan mode di bawah tengah kartu (tidak menimpa tombol mode di atas)
            modeHint = U.Row(6).Abs(null, null, null, 52).Pad(5, 10).Radius(999).Bg(U.C(U.Ink, 0.75f));
            modeHint.style.left = Length.Percent(50);
            modeHint.style.translate = new Translate(Length.Percent(-50), 0);
            modeHint.pickingMode = PickingMode.Ignore;
            modeHint.Add(U.Div().Size(8, 8).Radius(4).Bg(o.accent));
            modeHintText = U.Text("").FontSize(11).Fg("#FFFFFF");
            modeHint.Add(modeHintText);
            card.Add(modeHint);
            var chipR = U.Row(6).Abs(null, null, 12, 12).Bg(U.TealDark).Radius(999).Pad(6, 10).H(28);
            chipR.Add(U.Icon("ar", 14));
            chipR.Add(U.Text("Lihat di AR").FontSize(11).Fg("#FFFFFF"));
            chipR.Tap(() => App.Go<ScanScreen>(o.id));
            card.Add(chipR);
            card.Add(bonesChip);

            // bagian yang sedang disorot (ketuk x untuk kembali ke organ utuh)
            m_PartChip = U.Row(6).Abs(12, 50).Pad(5, 6, 5, 10).Radius(999).Bg(U.C(U.Ink, 0.85f));
            m_PartChip.Add(U.Icon("zoom", 12).Tinted(Color.white));
            m_PartChipText = U.Text("").FontSize(11).Bold().Fg("#FFFFFF");
            m_PartChip.Add(m_PartChipText);
            var chipX = U.Div().Size(18, 18).Radius(9).Bg(U.C("#FFFFFF", 0.25f)).Center();
            chipX.Add(U.Icon("x_small", 9).Tinted(Color.white));
            m_PartChip.Add(chipX);
            m_PartChip.Tap(() => SelectPart(-1));
            m_PartChip.tooltip = "Lihat organ utuh";
            card.Add(m_PartChip);

            var loc = U.Col(2).W(72).Abs(12, null, null, 12).Pad(6, 4).Radius(12).Bg("#FFFFFF").Border(1, U.Line).Align(Align.Center);
            loc.pickingMode = PickingMode.Ignore;
            var map = new SvgImage();
            var dot = LocatorDot(o.id);
            map.SetMarkup(OrganoIcons.Fill("locator_template", new Dictionary<string, string> { { "d.dx", dot.x.ToString(System.Globalization.CultureInfo.InvariantCulture) }, { "d.dy", dot.y.ToString(System.Globalization.CultureInfo.InvariantCulture) } }));
            map.Size(40, 66);
            loc.Add(map);
            loc.Add(U.Text("Letaknya di").FontSize(9).Fg(U.Muted));
            loc.Add(U.Text(o.WhereLabel).FontSize(11).Bold());
            // rangka ada di seluruh tubuh: peta letak tidak berguna dan hanya menutupi kaki rangka
            if (!skeleton) { card.Add(loc); locBox = loc; }

            // saat diperbesar: tombol mode, AR, peta & keterangan menyingkir; tombol "Lihat utuh" mengembalikan tampilan
            m_ZoomHide.AddRange(new[] { seg, chipR, loc, modeHint, bonesChip });
            m_ZoomReset = U.Row(5).Abs(null, 12, 12).Pad(5, 10).Radius(999).Bg(U.C(U.Ink, 0.85f));
            m_ZoomReset.Add(U.Icon("zoom", 12).Tinted(Color.white));
            m_ZoomReset.Add(U.Text("Lihat utuh").FontSize(11).Bold().Fg("#FFFFFF"));
            m_ZoomReset.Tap(() => App.Viewer?.ResetZoom());
            m_ZoomReset.tooltip = "Kembali ke tampilan utuh";
            m_ZoomReset.Visible(false);
            card.Add(m_ZoomReset);
            // Kartu 3D di luar ScrollView: menggeser / scroll di atas model hanya memutar & zoom model,
            // tidak ikut menggulir halaman.
            var stage = U.Div().Pad(8, 20, 4, 20).NoShrink();
            stage.Add(card);
            Root.Insert(Root.IndexOf(scroll), stage);
            // HP layar pendek: kartu 3D sedikit lebih pendek supaya ruang baca di bawahnya tidak terlalu sempit
            stage.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                float h = Root.resolvedStyle.height;
                if (float.IsNaN(h) || h <= 0) return;
                float want = Mathf.Round(tall ? Mathf.Clamp(h * 0.42f, 240f, 340f) : Mathf.Clamp(h * 0.3f, 190f, 250f));
                if (!Mathf.Approximately(card.resolvedStyle.height, want)) card.style.height = want;
            });

            m_SetMode = SetMode;
            SetMode(m_View, true);
            UpdatePartUI();

            // ---------- Title ----------
            var titleCol = U.Col(6).Align(Align.FlexStart);
            titleCol.Add(U.Text(o.displayName, "t-h1"));
            var sysCol = OrganDefaults.SystemColors(o.system).fg;
            var sysPill = U.Pill(o.SystemLabel, "#FFFFFF", "#FFFFFF");
            sysPill.style.backgroundColor = sysCol;
            titleCol.Add(sysPill);
            c.Add(titleCol);

            // ---------- Bagian-bagian ----------
            if (m_Content != null && m_Content.parts.Length > 0)
            {
                var sec = U.Col(10);
                var sh = U.Row(8);
                sh.Add(OrganVisuals.Section("Bagian-bagian " + o.displayName, o.accent).Grow());
                sh.Add(U.Text(m_Content.parts.Length + " bagian", "t-caption"));
                sec.Add(sh);
                var rail = U.Scroll(true);
                rail.style.flexGrow = 0; rail.style.flexShrink = 0;
                rail.style.marginLeft = -20; rail.style.marginRight = -20;
                rail.contentContainer.style.flexDirection = FlexDirection.Row;
                rail.contentContainer.style.paddingLeft = 20;
                rail.contentContainer.style.paddingRight = 8;
                rail.contentContainer.style.paddingTop = 2;
                rail.contentContainer.style.paddingBottom = 6;
                for (int i = 0; i < m_Content.parts.Length; i++)
                {
                    int index = i;
                    var pd = m_Content.parts[i];
                    var pc = U.Col(6).W(124).MinH(166).NoShrink().Radius(16).Bg("#FFFFFF").Border(1, U.Line).Pad(8); // tinggi ikut isi
                    pc.style.marginRight = 10;
                    pc.Add(OrganVisuals.PartPicture(o, pd, 106, 76));
                    var nm = U.Text(pd.name, "t-small").Bold();
                    nm.style.whiteSpace = WhiteSpace.Normal;
                    pc.Add(nm);
                    var fn = U.Text(pd.function).FontSize(10).Fg(U.Muted);
                    fn.style.whiteSpace = WhiteSpace.Normal;
                    pc.Add(fn);
                    pc.Tap(() => SelectPart(index == m_Part ? -1 : index));
                    pc.tooltip = pd.name + ": " + pd.function;
                    m_PartCards.Add(pc);
                    rail.Add(pc);
                }
                sec.Add(rail);
                m_PartPanel = U.Col(8).Bg(o.tint).Border(1, o.ring).Radius(16).Pad(14);
                sec.Add(m_PartPanel);
                c.Add(sec);
                UpdatePartUI();
            }

            // ---------- Tentang ----------
            var about = U.Col(8);
            var ah = U.Row(8);
            ah.Add(U.Text("Tentang " + o.displayName, "t-section").Grow());
            // suara pembaca nyala/mati (tersimpan) + tombol Dengarkan / Berhenti
            m_VoiceToggle = U.Div().Size(30, 30).Radius(15).Center();
            m_VoiceToggle.Tap(() => SetNarration(!P.narration));
            ah.Add(m_VoiceToggle);
            m_Listen = U.Row(4).Pad(4, 10).Radius(999).MinH(30);
            m_Listen.Tap(ToggleVoice);
            ah.Add(m_Listen);
            about.Add(ah);
            m_Sentences = SplitSentences(o.about);
            m_About = U.Text("", "t-body");
            m_About.style.unityParagraphSpacing = 0;
            m_About.style.whiteSpace = WhiteSpace.Normal;
            about.Add(m_About);
            c.Add(about);
            m_AboutSection = about;
            RenderAbout(-1, false);
            UpdateVoiceUI();

            // ---------- Fakta menarik (bisa diganti-ganti) ----------
            m_Facts.Clear();
            m_Facts.AddRange(OrganNarration.Facts(o)); // urutan sama dengan rekaman fakta (factNarration)
            m_FactIndex = 0;
            m_Fact = U.Row(12).Align(Align.FlexStart).Bg(U.TealDark).Radius(16).Pad(14);
            m_Fact.Add(U.Icon("bulb", 24));
            var ft = U.Col(4).Grow();
            var fh = U.Row(8);
            fh.Add(U.Text("Tahukah Kamu?", "t-body").Bold().Fg("#FFFFFF").Grow());
            m_FactCount = U.Text("").FontSize(11).Fg(U.C("#FFFFFF", 0.8f));
            fh.Add(m_FactCount);
            ft.Add(fh);
            m_FactText = U.Text("").FontSize(12).Fg("#FFFFFF");
            m_FactText.style.whiteSpace = WhiteSpace.Normal;
            ft.Add(m_FactText);
            if (m_Facts.Count > 1)
            {
                var next = U.Row(4).AlignSelf(Align.FlexStart).Pad(4, 10).Radius(999).Bg(U.C("#FFFFFF", 0.18f));
                next.style.marginTop = 4;
                next.Add(U.Text("Fakta lain").FontSize(11).Bold().Fg("#FFFFFF"));
                next.Add(U.Icon("arrow_right", 12).Tinted(Color.white));
                next.Tap(() => ShowFact(-1));
                next.tooltip = "Tampilkan fakta menarik berikutnya";
                ft.Add(next);
            }
            m_Fact.Add(ft);
            c.Add(m_Fact);
            m_FactIndex = -1;
            ShowFact(-1);

            // ---------- Info lengkap ----------
            var info = U.Row(12).Bg(U.PurpleSoft).Border(1, "#D3C8F7").Radius(16).Pad(12, 14);
            info.Add(U.Div().Size(44, 44).Radius(22).Bg("#FFFFFF").Center().Kids(U.Icon("g_book", 26)));
            var it = U.Col(2).Grow();
            it.Add(U.Text("Info Lengkap " + o.displayName, "t-body").Bold());
            info.Add(it);
            info.Add(U.Icon("chevron_right", 18));
            info.Tap(() => App.Go<OrganInfoScreen>(o.id));
            info.tooltip = "Buka info lengkap " + o.displayName;
            c.Add(info);

            // ---------- Organ reproduksi: tubuh pribadi & materi pubertas ----------
            if (repro)
            {
                var safe = U.Col(8).Bg("#F3EFFD").Border(1, "#CDBFF5").Radius(16).Pad(14);
                var sh2 = U.Row(8);
                sh2.Add(U.Div().Size(32, 32).Radius(16).Bg(U.PurpleDark).Center().Kids(U.Icon("shield_white", 18)));
                sh2.Add(U.Text("Tubuhku milikku", "t-section").Fg(U.PurpleDark));
                safe.Add(sh2);
                var st = U.Text("Organ reproduksi adalah bagian tubuh pribadi. Tidak ada yang boleh menyentuh atau melihatnya, kecuali orang tua atau dokter untuk menjaga kesehatanmu. Kalau ada yang membuatmu tidak nyaman, katakan TIDAK, pergi, lalu bercerita kepada orang dewasa yang kamu percaya.", "t-small");
                st.style.whiteSpace = WhiteSpace.Normal;
                safe.Add(st);
                safe.Add(U.Text("Baca juga: Tubuhku Tumbuh Besar (pubertas)").FontSize(13).Bold().Fg(U.TealDark).Tap(() => App.Go<BodyMaterialScreen>(ReproAccess.GenderOf(o.id))));
                c.Add(safe);
            }

            // ---------- Status kartu ----------
            if (P.HasCard(o.id))
            {
                var s = U.Row(10).Bg(U.TealSoft).Border(1, "#9CE6DC").Radius(12).Pad(10, 12);
                s.Add(U.Icon("cards_ok", 22));
                s.Add(U.Text("Kartu sudah terkumpul!", "t-caption").Fg(U.TealInk).Grow());
                var link = U.Row(2);
                link.Add(U.Text("Lihat kartu").FontSize(12).Bold().Fg(U.TealDark));
                link.Add(U.Icon("chevron_teal", 14));
                link.Tap(() => App.Go<CardViewerScreen>(o.id));
                s.Add(link);
                c.Add(s);
            }
            else
            {
                // kartu cetak hanya didapat dengan men-scan kartunya (kuis biasa tidak memberi kartu)
                var s = U.Row(10).Bg("#F1F6C4").Border(1, "#DDE69A").Radius(12).Pad(10, 12);
                s.Add(U.Icon("cards_star", 22));
                s.Add(U.Text("Kartu belum terkumpul", "t-caption").Fg(U.Ink).Grow());
                var scan = U.Button("Scan Kartu", () => App.Go<ScanScreen>(o.id), "btn-dark", "btn-small");
                scan.Insert(0, U.Icon("scan_small", 15)); scan[1].style.marginLeft = 6;
                s.Add(scan);
                c.Add(s);
            }
            c.Add(U.Space(8));

            // ---------- Aksi bawah ----------
            var bottom = U.Div().Pad(12, 20, 20, 20).Bg(U.Surface);
            // kuis organ ini: terbuka setelah kartunya di-scan atau kuis organ sebelumnya selesai
            bool quizOpen = QuizAccess.OrganUnlocked(P, o.id);
            var quiz = U.Button(quizOpen ? "Mulai Kuis " + o.displayName : "Kuis " + o.displayName + " terkunci", () => QuizLocks.PlayOrgan(App, o.id), quizOpen ? "btn-primary" : "btn-outline");
            if (quizOpen)
            {
                quiz.Bg(U.PinkDark);
                quiz.Add(U.Icon("arrow_right", 16));
                quiz[1].style.marginLeft = 8;
            }
            else
            {
                quiz.Insert(0, U.Icon("lock_gray", 16));
                quiz[1].style.marginLeft = 8;
            }
            bottom.Add(quiz);
            Root.Add(bottom);
        }

        /// <summary>Organ reproduksi gender lain yang belum terbuka: tidak ada model/gambar, hanya penjelasan & jalan ke kuis.</summary>
        void BuildLocked(OrganInfo o)
        {
            Root.Add(Parts.TopBar(o.displayName, App.Back));
            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 8;
            Root.Add(scroll);
            var c = U.Col(16).Align(Align.Center);
            scroll.Add(c);
            var box = U.Col(10).AlignSelf(Align.Stretch).H(230).Radius(16).Bg("#F6F5F9").Center();
            box.Add(new DashedOutline(U.C("#CFCCDD"), 2f, 16f));
            box.Add(U.Div().Size(88, 88).Radius(44).Bg("#FFFFFF").Center().Kids(U.Icon("lock_big", 40)));
            box.Add(U.Text("Masih terkunci", "t-title").Fg(U.Muted));
            c.Add(box);
            c.Add(U.Text(o.displayName, "t-h1").TextCenter());
            var why = U.Text(ReproAccess.LockReason(o.id, P), "t-body").TextCenter();
            why.style.whiteSpace = WhiteSpace.Normal;
            c.Add(why);
            var steps = U.Col(10).AlignSelf(Align.Stretch).Bg("#FFFFFF").Border(1, U.Line).Radius(16).Pad(14);
            string own = ReproAccess.OwnId(P.gender);
            var lines = own == null
                ? new[] { "Pilih tubuhmu di Profil > Materi Tubuh", "Pelajari organ reproduksimu sendiri", "Selesaikan kuisnya, lalu organ ini terbuka" }
                : new[] { "Pelajari " + Db.Get(own)?.displayName, "Selesaikan " + ReproAccess.QuizName(own), "Organ ini langsung terbuka" };
            for (int i = 0; i < lines.Length; i++)
            {
                var r = U.Row(10);
                bool done = own != null && (i == 0 ? P.openedOrgans.Contains(own) : i == 1 && ReproAccess.QuizDone(P, own));
                r.Add(U.Div().Size(26, 26).Radius(13).Bg(done ? U.Teal : U.TealSoft).Center().Kids(done ? (VisualElement)U.Icon("check", 12) : U.Text((i + 1).ToString()).FontSize(12).Bold().Fg(U.TealDark)));
                var t = U.Text(lines[i], "t-small").Fg(U.Ink).Grow();
                t.style.whiteSpace = WhiteSpace.Normal;
                r.Add(t);
                steps.Add(r);
            }
            c.Add(steps);
            var bottom = U.Col(8).Pad(12, 20, 20, 20).Bg(U.Surface);
            if (own == null) bottom.Add(U.Button("Pilih tubuhku", () => App.Go<GenderScreen>("profile"), "btn-primary"));
            else
            {
                bottom.Add(U.Button("Mulai " + ReproAccess.QuizName(own), () => QuizLocks.PlayOrgan(App, own), "btn-primary"));
                bottom.Add(U.Button("Pelajari " + Db.Get(own)?.displayName, () => App.Go<OrganDetailScreen>(own, false), "btn-outline"));
            }
            Root.Add(bottom);
        }

        // ------------------------------------------------------------ bagian organ

        static bool HasOuter(PartDef p) => (p.materials != null && p.materials.Length > 0) || !string.IsNullOrEmpty(p.micro);
        static bool HasInside(PartDef p) => p.inside != null && p.inside.Length > 0;

        /// <summary>Apakah bagian ini bisa ditunjukkan di tampilan tersebut (tubuh: tidak ada sorot bagian).</summary>
        bool PartFits(PartDef p, OrganViewer3D.ViewMode view) =>
            view == OrganViewer3D.ViewMode.Inside ? HasInside(p) && m_Organ.insidePrefab != null
            : view == OrganViewer3D.ViewMode.Organ && HasOuter(p);

        void SelectPart(int index)
        {
            if (m_Content == null) return;
            m_Part = index;
            ApplyPart3D();
            UpdatePartUI();
        }

        /// <summary>
        /// Tunjukkan bagian yang dipilih di model 3D: model mikro (diperbesar), organ dipotong ("Dalam") untuk bagian
        /// yang ada di dalam, atau organ utuh dengan bagian itu disorot dan kamera mendekat.
        /// </summary>
        void ApplyPart3D()
        {
            var viewer = App.Viewer;
            if (viewer == null || m_Viewport == null || m_Viewport.panel == null) return;
            var part = m_Part >= 0 ? m_Content.parts[m_Part] : null;
            if (part == null)
            {
                if (viewer.Mode == OrganViewer3D.ViewMode.Micro) viewer.Load(m_Organ, m_View == OrganViewer3D.ViewMode.Inside);
                else viewer.HighlightPart(null, null, null);
                m_Viewport.SelectHotspot(-1);
                UpdatePartUI();
                return;
            }
            var vis = m_Organ.PartVisual(part.id);
            if (!string.IsNullOrEmpty(part.micro) && vis != null && vis.microPrefab != null)
            {
                if (m_View == OrganViewer3D.ViewMode.Body) m_SetMode?.Invoke(OrganViewer3D.ViewMode.Organ, true);
                viewer.LoadMicro(vis.microPrefab, m_Organ);
                m_Viewport.SelectHotspot(-1);
                UpdatePartUI();
                return;
            }
            bool outer = part.materials != null && part.materials.Length > 0;
            bool inside = HasInside(part) && m_Organ.insidePrefab != null;
            var want = inside && (m_View == OrganViewer3D.ViewMode.Inside || !outer) ? OrganViewer3D.ViewMode.Inside : OrganViewer3D.ViewMode.Organ;
            if (m_View != want) m_SetMode?.Invoke(want, true);
            if (viewer.Mode != want) viewer.Load(m_Organ, want == OrganViewer3D.ViewMode.Inside);
            viewer.HighlightPart(want == OrganViewer3D.ViewMode.Inside ? part.inside : part.materials, part.name, part.function);
            m_Viewport.SelectHotspot(0);
            UpdatePartUI();
        }

        void UpdatePartUI()
        {
            if (m_Content == null) return;
            var o = m_Organ;
            for (int i = 0; i < m_PartCards.Count; i++)
            {
                bool on = i == m_Part;
                m_PartCards[i].Bg(on ? o.tint : U.C("#FFFFFF")).Border(on ? 2 : 1, on ? o.accent : U.C(U.Line));
            }
            var part = m_Part >= 0 ? m_Content.parts[m_Part] : null;
            bool micro = part != null && !string.IsNullOrEmpty(part.micro);
            if (m_PartChip != null)
            {
                m_PartChip.Visible(part != null);
                if (part != null) m_PartChipText.text = part.name + (micro ? " · diperbesar" : m_View == OrganViewer3D.ViewMode.Inside ? " · tampak dalam" : "");
            }
            if (m_PartPanel == null) return;
            m_PartPanel.Clear();
            m_PartPanel.Visible(part != null);
            if (part == null) return;
            var head = U.Row(10);
            var num = U.Div().Size(26, 26).Radius(13).Bg(o.accent).Center();
            num.Add(U.Text((m_Part + 1).ToString()).FontSize(12).Bold().Fg("#FFFFFF"));
            head.Add(num);
            head.Add(U.Text(part.name, "t-body").Bold().Grow());
            m_PartPanel.Add(head);
            var fn = U.Text(part.function, "t-small").Bold().Fg(U.TealDark);
            fn.style.whiteSpace = WhiteSpace.Normal;
            m_PartPanel.Add(fn);
            var det = U.Text(part.detail, "t-body").FontSize(13);
            det.style.whiteSpace = WhiteSpace.Normal;
            m_PartPanel.Add(det);
            if (micro)
                m_PartPanel.Add(U.Text("Bagian ini sangat kecil, jadi model 3D-nya diperbesar ribuan kali.", "t-caption"));
            var btns = U.Row(8);
            btns.style.marginTop = 2;
            var whole = U.Button("Organ utuh", () => SelectPart(-1), "btn-outline", "btn-small");
            btns.Add(whole);
            var nextBtn = U.Button("Bagian berikutnya", () => SelectPart((m_Part + 1) % m_Content.parts.Length), "btn-dark", "btn-small");
            btns.Add(nextBtn);
            m_PartPanel.Add(btns);
        }

        string CurrentFact => m_FactIndex >= 0 && m_FactIndex < m_Facts.Count ? m_Facts[m_FactIndex] : m_Organ.funFact ?? "";

        void ShowFact(int index)
        {
            if (m_FactText == null || m_Facts.Count == 0) return;
            // -1 = acak (selalu beda dari fakta yang sedang tampil)
            if (index < 0)
            {
                index = Random.Range(0, m_Facts.Count);
                if (m_Facts.Count > 1 && index == m_FactIndex) index = (index + 1 + Random.Range(0, m_Facts.Count - 1)) % m_Facts.Count;
            }
            m_FactIndex = index % m_Facts.Count;
            m_FactText.text = m_Facts[m_FactIndex];
            m_FactCount.text = "";
        }

        /// <summary>Posisi organ di mini-peta tubuh (koordinat desain 390x420).</summary>
        static Vector2 LocatorDot(string id)
        {
            switch (id)
            {
                case "mulut": return new Vector2(195, 60);
                case "kerongkongan": return new Vector2(195, 100);
                case "jantung": return new Vector2(208, 156);
                case "paru": return new Vector2(182, 150);
                case "lambung": return new Vector2(212, 196);
                case "usushalus": return new Vector2(195, 240);
                case "ususbesar": return new Vector2(160, 238);
                case "ginjal": return new Vector2(222, 230);
                case "hati": return new Vector2(178, 194);
                case "pankreas": return new Vector2(204, 214);
                case "otak": return new Vector2(195, 38);
                case "mata": return new Vector2(206, 46);
                case "hidung": return new Vector2(195, 53);
                case "tenggorokan": return new Vector2(190, 96);
                case "reprolaki":
                case "reproperempuan": return new Vector2(195, 286);
                case "rangka": return new Vector2(195, 180);
                default: return new Vector2(195, 210);
            }
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
            // <mark> digambar DI ATAS teks, jadi warnanya harus transparan; kalau tidak, kalimat yang dibacakan tertutup kotak
            for (int i = 0; i < m_Sentences.Length; i++)
                parts.Add(i == active && P.captions ? "<mark=#1AB8A62E><color=#0F6B61>" + m_Sentences[i] + "</color></mark>" : m_Sentences[i]);
            m_About.text = string.Join(" ", parts);
            if (m_Fact != null) m_Fact.Border(factActive && P.captions ? 3 : 0, "#9CE6DC");
        }

        /// <summary>Sedang membacakan "Tentang" (untuk uji).</summary>
        public bool VoicePlaying => m_Playing;

        public void ToggleVoice()
        {
            if (m_Playing) { StopVoice(); return; }
            if (!P.narration) return;
            m_Playing = true;
            m_PlayT = 0;
            UpdateVoiceUI();
            // tampilkan paragraf yang dibacakan (bisa tersembunyi di bawah kartu 3D kalau halaman sedang digulir)
            if (m_PageScroll != null && m_AboutSection != null) m_PageScroll.ScrollTo(m_AboutSection);
            var speech = m_Organ.about + " " + OrganNarration.FactText(CurrentFact);
            // rekaman suara (paragraf "Tentang" lalu faktanya) → suara text-to-speech HP → kalau tidak ada, teks tetap disorot
            m_UsingClip = m_Organ.narration != null && OrganoAudio.PlayVoiceSequence(m_Organ.narration, OrganNarration.FactClip(m_Organ, CurrentFact));
            m_UsingTts = !m_UsingClip && OrganoAudio.SpeakText(speech);
            m_PlayDur = m_UsingClip ? m_Organ.narration.length : Mathf.Max(6f, speech.Length / (14f * OrganoAudio.SpeechRate));
            if (!m_UsingClip && !m_UsingTts && !s_ToldAboutVoice)
            {
                s_ToldAboutVoice = true;
                switch (OrganoSpeech.Status)
                {
                    case OrganoSpeech.State.NotSupported: App.Toast("Suara narasi berbunyi di HP Android/iOS. Di sini teksnya disorot saja, ya.", 3.2f); break;
                    case OrganoSpeech.State.Starting: App.Toast("Suara sedang disiapkan. Coba ketuk Dengarkan lagi sebentar lagi.", 3f); break;
                    case OrganoSpeech.State.MissingLanguage: App.Toast("Pasang suara Bahasa Indonesia di Pengaturan HP > Text-to-speech supaya narasi bisa berbunyi.", 4f); break;
                    default: App.Toast(OrganoAudio.Enabled ? "Suara narasi belum tersedia di HP ini." : "Suara sedang dimatikan di Profil.", 3f); break;
                }
            }
        }

        void StopVoice()
        {
            m_Playing = false;
            OrganoAudio.StopVoice();
            if (m_About != null) RenderAbout(-1, false);
            UpdateVoiceUI();
        }

        void SetNarration(bool on)
        {
            if (!on) StopVoice();
            P.narration = on;
            P.Save();
            OrganoAudio.NarrationEnabled = on;
            UpdateVoiceUI();
            App.Toast(on ? "Suara pembaca dinyalakan" : "Suara pembaca dimatikan");
        }

        /// <summary>Speaker nyala/coret dan tombol Dengarkan ⇄ Berhenti (disembunyikan saat suara pembaca mati).</summary>
        void UpdateVoiceUI()
        {
            if (m_VoiceToggle == null || m_Listen == null) return;
            bool on = P.narration;
            m_VoiceToggle.Clear();
            m_VoiceToggle.Bg(on ? U.TealSoft : "#EDEBF3");
            m_VoiceToggle.Add(U.Icon(on ? "set_sound" : "sound_off", 16).Tinted(U.C(on ? U.TealInk : U.Muted)));
            m_VoiceToggle.tooltip = on ? "Matikan suara pembaca" : "Nyalakan suara pembaca";
            m_Listen.Visible(on);
            m_Listen.Clear();
            m_Listen.Bg(m_Playing ? U.PinkSoft : U.TealSoft);
            string fg = m_Playing ? U.PinkDark : U.TealInk;
            m_Listen.Add(U.Icon(m_Playing ? "stop" : "play", 12).Tinted(U.C(fg)));
            m_Listen.Add(U.Text(m_Playing ? "Berhenti" : "Dengarkan").FontSize(11).Bold().Fg(fg));
        }

        public override void Tick(float dt)
        {
            UpdateZoomUI();
            if (!m_Playing) return;
            m_PlayT += dt;
            float p = m_UsingClip ? OrganoAudio.VoiceProgress : m_PlayT / m_PlayDur;
            // suara HP: selesai saat mesin berhenti berbicara (sorotan berhenti di kalimat terakhir sampai selesai)
            if (m_UsingTts) { if (m_PlayT > 1f && !OrganoAudio.SpeechPlaying) { StopVoice(); return; } p = Mathf.Min(p, 0.995f); }
            if (p < 0 || p >= 1f || (m_UsingClip && !OrganoAudio.VoicePlaying)) { StopVoice(); return; }
            int total = m_Organ.about.Length + 14 + CurrentFact.Length;
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

        void UpdateZoomUI()
        {
            var v = App.Viewer;
            bool zoomed = v != null && m_Viewport != null && m_Viewport.panel != null && v.IsZoomed;
            if (zoomed == m_ZoomedUI) return;
            m_ZoomedUI = zoomed;
            foreach (var e in m_ZoomHide) if (e != null) e.style.visibility = zoomed ? Visibility.Hidden : Visibility.Visible;
            m_ZoomReset?.Visible(zoomed);
        }

        public override void Close() => StopVoice();
    }
}
