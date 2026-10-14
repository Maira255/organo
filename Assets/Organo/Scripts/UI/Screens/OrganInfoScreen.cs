using Organo.Data;
using Organo.UI;
using UnityEngine;
using UnityEngine.UIElements;
using U = Organo.UI.UI;

namespace Organo.Screens
{
    /// <summary>Gambar organ & bagian-bagiannya (hasil render model 3D) dengan cadangan ikon.</summary>
    public static class OrganVisuals
    {
        public static VisualElement Picture(Texture2D tex, OrganInfo o, float w, float h, float iconSize)
        {
            var box = U.Div().Size(w, h).Radius(12).Bg(o.tint).Center();
            box.style.overflow = Overflow.Hidden;
            if (tex != null)
            {
                var img = U.Div("fill");
                img.pickingMode = PickingMode.Ignore;
                img.style.backgroundImage = new StyleBackground(tex);
                img.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                box.Add(img);
            }
            else box.Add(U.Icon(Parts.MiniIcon(o), iconSize));
            return box;
        }

        public static VisualElement PartPicture(OrganInfo o, PartDef p, float w, float h)
        {
            var vis = o.PartVisual(p.id);
            var box = Picture(vis != null ? vis.image : null, o, w, h, Mathf.Min(w, h) * 0.55f);
            bool insideOnly = string.IsNullOrEmpty(p.micro) && (p.materials == null || p.materials.Length == 0) && p.inside != null && p.inside.Length > 0;
            if (insideOnly)
            {
                var tag = U.Row(3).Abs(6, 6).Pad(2, 6).Radius(999).Bg(U.C(U.Ink, 0.75f));
                tag.pickingMode = PickingMode.Ignore;
                tag.Add(U.Icon("eye", 10).Tinted(Color.white));
                tag.Add(U.Text("Bagian dalam").FontSize(9).Fg("#FFFFFF"));
                box.Add(tag);
            }
            if (!string.IsNullOrEmpty(p.micro))
            {
                var tag = U.Row(3).Abs(6, 6).Pad(2, 6).Radius(999).Bg(U.C(U.Ink, 0.75f));
                tag.pickingMode = PickingMode.Ignore;
                tag.Add(U.Icon("zoom", 10).Tinted(Color.white));
                tag.Add(U.Text("Diperbesar").FontSize(9).Fg("#FFFFFF"));
                box.Add(tag);
            }
            return box;
        }

        /// <summary>Judul bagian dengan titik warna organ.</summary>
        public static VisualElement Section(string title, Color accent)
        {
            var h = U.Row(8);
            h.Add(U.Div().Size(8, 8).Radius(4).Bg(accent));
            h.Add(U.Text(title, "t-section"));
            return h;
        }
    }

    // ==================================================================== Info Lengkap organ

    public class OrganInfoScreen : AppScreen
    {
        public override NavTab Tab => NavTab.None;

        protected override void Build(object arg)
        {
            var o = Db.Get(arg as string ?? "lambung") ?? Db.Get("lambung");
            // organ reproduksi gender lain yang belum terbuka: tampilkan halaman terkunci
            if (Parts.Locked(o)) { Root.schedule.Execute(() => App.Go<OrganDetailScreen>(o.id, false)); return; }
            var content = OrganContent.Get(o.id);
            Root.Add(Parts.TopBar("Info Lengkap", App.Back));

            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 8;
            Root.Add(scroll);
            var c = U.Col(22);
            scroll.Add(c);

            // ---------- Hero ----------
            var hero = U.Col(0).Radius(20).Bg(o.tint).Border(1, o.ring);
            hero.style.overflow = Overflow.Hidden;
            var pic = OrganVisuals.Picture(o.heroImage, o, 350, 200, 120);
            pic.style.width = new StyleLength(StyleKeyword.Auto);
            pic.Radius(0);
            pic.style.alignSelf = Align.Stretch;
            hero.Add(pic);
            var heroText = U.Col(6).Pad(14, 16).Bg("#FFFFFF");
            heroText.Add(U.Text(o.displayName, "t-h1"));
            var meta = U.Row(8);
            var sysPill = U.Pill(o.SystemLabel, "#FFFFFF", "#FFFFFF");
            sysPill.style.backgroundColor = OrganDefaults.SystemColors(o.system).fg;
            meta.Add(sysPill);
            meta.Add(U.Pill("Letaknya di " + o.WhereLabel.ToLowerInvariant(), U.PurpleSoft, U.PurpleDark));
            heroText.Add(meta);
            hero.Add(heroText);
            hero.Elev(Elevation.E1);
            c.Add(hero);

            // ---------- Apa itu ----------
            var what = U.Col(8);
            what.Add(OrganVisuals.Section("Apa itu " + o.displayName + "?", o.accent));
            what.Add(Paragraph(o.about));
            if (!string.IsNullOrEmpty(o.function))
            {
                var job = U.Row(12).Align(Align.FlexStart).Bg(U.TealSoft).Radius(14).Pad(12, 14);
                job.Add(U.Icon("check_teal", 20));
                var jt = U.Col(2).Grow();
                jt.Add(U.Text("Tugas utamanya", "t-small").Bold().Fg(U.TealInk));
                jt.Add(Paragraph(o.function).FontSize(13).Fg(U.TealInk));
                job.Add(jt);
                what.Add(job);
            }
            c.Add(what);

            if (content == null) { c.Add(U.Space(16)); return; }

            // ---------- Bagian-bagian ----------
            var parts = U.Col(10);
            parts.Add(OrganVisuals.Section("Bagian-bagian " + o.displayName, o.accent));
            for (int i = 0; i < content.parts.Length; i++)
            {
                var p = content.parts[i];
                var card = U.Row(12).Align(Align.FlexStart).Bg("#FFFFFF").Border(1, U.Line).Radius(16).Pad(10);
                card.Add(OrganVisuals.PartPicture(o, p, 96, 96));
                var t = U.Col(4).Grow();
                var nameRow = U.Row(6);
                var num = U.Div().Size(20, 20).Radius(10).Bg(o.accent).Center();
                num.Add(U.Text((i + 1).ToString()).FontSize(10).Bold().Fg("#FFFFFF"));
                nameRow.Add(num);
                nameRow.Add(U.Text(p.name, "t-body").Bold());
                t.Add(nameRow);
                t.Add(Paragraph(p.function).FontSize(12).Bold().Fg(U.TealDark));
                t.Add(Paragraph(p.detail).FontSize(12));
                var see = U.Row(4).Margin(2, 0, 0, 0);
                see.Add(U.Text("Lihat 3D").FontSize(12).Bold().Fg(U.PinkDark));
                see.Add(U.Icon("arrow_right_pink", 14));
                string target = o.id + "#" + p.id;
                see.Tap(() => { if (!App.BackTo<OrganDetailScreen>(target)) App.Go<OrganDetailScreen>(target); });
                see.tooltip = "Sorot " + p.name + " di model 3D";
                t.Add(see);
                card.Add(t);
                parts.Add(card);
            }
            c.Add(parts);

            // ---------- Cara kerja ----------
            if (content.steps != null && content.steps.Length > 0)
            {
                var how = U.Col(0);
                how.Add(OrganVisuals.Section("Cara kerjanya", o.accent));
                how.Add(U.Space(10));
                for (int i = 0; i < content.steps.Length; i++)
                {
                    var row = U.Row(12).Align(Align.Stretch);
                    var rail = U.Col(0).W(28).Align(Align.Center).NoShrink();
                    var num = U.Div().Size(28, 28).Radius(14).Bg(U.Purple).Center();
                    num.Add(U.Text((i + 1).ToString()).FontSize(13).Bold().Fg("#FFFFFF"));
                    rail.Add(num);
                    if (i < content.steps.Length - 1)
                    {
                        var line = U.Div().W(2).Grow().Bg(U.PurpleSoft);
                        line.style.minHeight = 12;
                        rail.Add(line);
                    }
                    row.Add(rail);
                    var txt = Paragraph(content.steps[i]).Grow();
                    txt.style.paddingTop = 4;
                    txt.style.paddingBottom = 14;
                    row.Add(txt);
                    how.Add(row);
                }
                c.Add(how);
            }

            // ---------- Fakta menarik ----------
            var facts = U.Col(10);
            facts.Add(OrganVisuals.Section("Fakta menarik", o.accent));
            string[] colors = { "#FFF1BF", "#D5F3EF", "#E8E2FB", "#FDE0E7" };
            var all = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(o.funFact)) all.Add(o.funFact);
            if (content.facts != null)
                foreach (var f in content.facts) if (!all.Contains(f)) all.Add(f);
            // satu fakta acak; tombol "Fakta lain" mengacak lagi
            var factCard = U.Row(12).Align(Align.FlexStart).Bg(colors[0]).Radius(14).Pad(12, 14);
            factCard.Add(U.Icon("bulb_gold", 22));
            var fcol = U.Col(8).Grow();
            var ftext = Paragraph("").FontSize(13);
            fcol.Add(ftext);
            int current = -1;
            void Shuffle()
            {
                if (all.Count == 0) return;
                int next = Random.Range(0, all.Count);
                if (all.Count > 1 && next == current) next = (next + 1 + Random.Range(0, all.Count - 1)) % all.Count;
                current = next;
                ftext.text = all[current];
                factCard.Bg(colors[current % colors.Length]);
            }
            if (all.Count > 1)
            {
                var more = U.Row(4).AlignSelf(Align.FlexStart).Pad(4, 10).Radius(999).Bg(U.C("#FFFFFF", 0.75f));
                more.Add(U.Text("Fakta lain").FontSize(11).Bold().Fg(U.Ink));
                more.Add(U.Icon("arrow_right", 12).Tinted(U.C(U.Ink)));
                more.Tap(Shuffle);
                more.tooltip = "Acak fakta menarik lain";
                fcol.Add(more);
            }
            factCard.Add(fcol);
            facts.Add(factCard);
            Shuffle();
            c.Add(facts);

            // ---------- Tips sehat ----------
            if (content.tips != null && content.tips.Length > 0)
            {
                var tips = U.Col(8).Bg("#FFFFFF").Border(1, U.Line).Radius(16).Pad(14);
                tips.Add(U.Text("Tips menjaga " + o.displayName.ToLowerInvariant(), "t-body").Bold());
                foreach (var tip in content.tips)
                {
                    var r = U.Row(10).Align(Align.FlexStart);
                    var tick = U.Div().Size(20, 20).Radius(10).Bg(U.Teal).Center().NoShrink();
                    tick.Add(U.Icon("check", 11));
                    r.Add(tick);
                    r.Add(Paragraph(tip).FontSize(13).Grow());
                    tips.Add(r);
                }
                c.Add(tips);
            }
            c.Add(U.Space(8));

            // ---------- Aksi bawah ----------
            var bottom = U.Div().Pad(12, 20, 20, 20).Bg(U.Surface);
            var back3d = U.Button("Lihat Model 3D", () => { if (!App.BackTo<OrganDetailScreen>(o.id)) App.Go<OrganDetailScreen>(o.id); }, "btn-primary");
            back3d.Insert(0, U.Icon("qt_3d", 18));
            back3d[1].style.marginLeft = 8;
            bottom.Add(back3d);
            Root.Add(bottom);
        }

        static Label Paragraph(string text)
        {
            var l = U.Text(text ?? "", "t-body");
            l.style.whiteSpace = WhiteSpace.Normal;
            return l;
        }
    }
}
