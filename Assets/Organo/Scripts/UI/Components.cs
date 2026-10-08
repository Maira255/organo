using System;
using Organo.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Organo.UI
{
    /// <summary>Navbar bawah: Beranda, Library, [Scan bulat menonjol], Quiz, Profil.</summary>
    public class NavBar : VisualElement
    {
        readonly OrganoApp m_App;
        readonly VisualElement[] m_Items = new VisualElement[5];
        readonly VisualElement m_FabBtn;
        readonly Label m_FabLabel;

        static readonly (NavTab tab, string icon, string label)[] Items =
        {
            (NavTab.Beranda, "nav_home", "Beranda"),
            (NavTab.Library, "logo", "Library"),
            (NavTab.Scan, null, null),
            (NavTab.Quiz, "nav_quiz", "Quiz"),
            (NavTab.Profil, "nav_profile", "Profil"),
        };

        public NavBar(OrganoApp app)
        {
            m_App = app;
            AddToClassList("nav");
            var bg = new SvgImage("navbar_bg");
            bg.AddToClassList("nav__bg");
            bg.style.height = 84;
            bg.style.bottom = StyleKeyword.Auto;
            var bgFill = UI.Div("nav__bg").Bg("#FFFFFF");
            bgFill.style.top = 80;
            Add(bgFill);
            Add(bg);

            for (int i = 0; i < Items.Length; i++)
            {
                var (tab, icon, label) = Items[i];
                if (icon == null) { var spacer = UI.Div("nav__item"); spacer.pickingMode = PickingMode.Ignore; Add(spacer); continue; }
                var item = UI.Div("nav__item");
                var ic = UI.Div("nav__icon");
                var svg = UI.Icon(icon, 20);
                ic.Add(svg);
                item.Add(ic);
                item.Add(UI.Text(label, "nav__label"));
                var t = tab;
                item.Tap(() => m_App.GoTab(t));
                m_Items[i] = item;
                Add(item);
            }

            var fab = UI.Div("nav__fab");
            m_FabBtn = UI.Div("nav__fab-btn").Gradient(UI.Teal, UI.Purple, 135);
            m_FabBtn.Add(UI.Icon("nav_scan", 28));
            fab.Add(m_FabBtn);
            m_FabLabel = UI.Text("Scan", "nav__label");
            m_FabLabel.style.marginTop = 4;
            fab.Add(m_FabLabel);
            fab.Tap(() => m_App.GoTab(NavTab.Scan));
            Add(fab);
        }

        public void SetActive(NavTab tab)
        {
            style.display = tab == NavTab.None ? DisplayStyle.None : DisplayStyle.Flex;
            for (int i = 0; i < Items.Length; i++)
                m_Items[i]?.EnableInClassList("nav__item--on", Items[i].tab == tab);
            bool scan = tab == NavTab.Scan;
            m_FabLabel.style.color = UI.C(scan ? UI.TealDark : UI.Faint);
            m_FabLabel.style.unityFontStyleAndWeight = scan ? FontStyle.Bold : FontStyle.Normal;
            m_FabBtn.style.rotate = new Rotate(scan ? 20 : 0);
        }
    }

    public static class Parts
    {
        /// <summary>Top bar 3 kolom: [kembali] [judul] [aksi].</summary>
        public static VisualElement TopBar(string title, Action onBack, VisualElement trailing = null, bool dark = false)
        {
            var bar = UI.Div("topbar");
            var left = UI.Div("topbar__side");
            if (onBack != null)
            {
                var b = UI.IconButton("back", onBack);
                b.style.marginLeft = -12;
                if (dark) ((SvgImage)b[0]).Tint = Color.white;
                b.tooltip = "Kembali";
                left.Add(b);
            }
            bar.Add(left);
            var t = UI.Text(title, "topbar__title");
            if (dark) t.style.color = Color.white;
            bar.Add(t);
            var right = UI.Div("topbar__side", "topbar__side--end");
            if (trailing != null) right.Add(trailing);
            bar.Add(right);
            return bar;
        }

        public static VisualElement SectionHeader(string title, string action = null, Action onAction = null)
        {
            var row = UI.Row(12).Justify(Justify.SpaceBetween);
            row.Add(UI.Text(title, "t-section"));
            if (action != null) row.Add(UI.Text(action).FontSize(12).Fg(UI.Pink).Bold().Tap(onAction));
            return row;
        }

        /// <summary>Kotak ilustrasi organ (bulat berwarna + SVG organ).</summary>
        public static VisualElement OrganBadge(OrganInfo o, float size, float iconScale = 0.62f, bool locked = false)
        {
            var c = UI.Div().Size(size, size).Radius(size / 2).Center();
            c.style.backgroundColor = locked ? UI.C("#F6F5F9") : o.tint;
            var icon = UI.Icon(o.IconKey, size * iconScale);
            if (locked) { icon.Tint = UI.C("#C9C6D8"); }
            c.Add(icon);
            return c;
        }

        /// <summary>Kartu koleksi Organo (pita sistem, ilustrasi, nama, nomor).</summary>
        public static VisualElement OrganCard(OrganInfo o, bool owned, float width = 172f, bool isNew = false, string sub = null)
        {
            var (fg, soft) = OrganDefaults.SystemColors(o.system);
            var card = UI.Col(8, "organ-card").W(width);
            card.Border(owned ? 3 : 2, owned ? fg : UI.C("#D9D7E3"));
            var band = UI.Div("organ-card__band").Bg(owned ? fg : UI.C("#BDBACE"));
            band.Add(UI.Text(OrganDefaults.SystemName(o.system).ToUpperInvariant()));
            card.Add(band);
            float art = width * 0.7f;
            var circle = UI.Div().Size(art, art).Radius(art / 2).Center().Bg(owned ? soft : UI.C("#F6F5F9"));
            circle.style.marginTop = 6;
            var icon = UI.Icon(o.IconKey, art * 0.62f);
            if (!owned) icon.Tint = UI.C("#C9C6D8");
            circle.Add(icon);
            if (!owned)
            {
                var lockIc = UI.Icon("lock", 22).Tinted(UI.C(UI.Faint));
                lockIc.Abs(art / 2 - 11, art / 2 - 11);
                circle.Add(lockIc);
            }
            card.Add(circle);
            card.Add(UI.Text(owned ? o.displayName : "???", "t-title").TextCenter());
            card.Add(UI.Text(sub ?? ("Kartu #" + o.cardNumber + " · Season " + o.season), "t-tiny").Fg(owned ? fg : UI.C(UI.Faint)).TextCenter());
            if (isNew)
            {
                var ribbon = UI.Div().Bg(UI.Pink).W(110).Abs(null, 30, -30);
                ribbon.style.rotate = new Rotate(38);
                ribbon.Add(UI.Text("BARU").FontSize(10).Bold().Fg("#FFFFFF").TextCenter());
                ribbon.style.paddingTop = 3; ribbon.style.paddingBottom = 3;
                card.Add(ribbon);
            }
            return card;
        }

        /// <summary>Lingkaran bintang + angka.</summary>
        public static VisualElement Stars(int count, bool chip = true)
        {
            var row = UI.Row(6);
            if (chip) row.Pad(6, 12).Bg("#FFFFFF").Border(1, "#EDEBF3").Radius(999);
            row.Add(UI.Icon("star", 18).Tinted(UI.C(UI.Yellow)));
            row.Add(UI.Text(count.ToString()).Bold());
            return row;
        }

        public static VisualElement Avatar(Gender g, float size)
        {
            var a = UI.Div().Size(size, size).Radius(size / 2).Bg(UI.TealSoft).Center();
            a.style.overflow = Overflow.Hidden;
            a.Add(UI.Icon(g == Gender.Girl ? "kid_girl" : "kid_boy", size * 0.9f));
            return a;
        }

        public static VisualElement InfoNote(string text, string icon = "info")
        {
            var row = UI.Row(8).Align(Align.FlexStart);
            row.style.paddingLeft = 6; row.style.paddingRight = 6;
            var ic = UI.Icon(icon, 16); ic.style.marginTop = 1;
            row.Add(ic);
            row.Add(UI.Text(text, "t-caption").Grow());
            return row;
        }

        /// <summary>Kolom isian dengan ikon (email / password).</summary>
        public static VisualElement Field(string label, string icon, string placeholder, bool password, out TextField input, string help = null)
        {
            var col = UI.Col(0);
            col.Add(UI.Text(label, "field-label").Bold());
            var box = UI.Div("field");
            if (icon != null) box.Add(UI.Icon(icon, 20));
            var tf = new TextField { isPasswordField = password };
            tf.textEdition.placeholder = placeholder;
            tf.textEdition.hidePlaceholderOnFocus = true;
            tf.RegisterCallback<FocusInEvent>(_ => box.AddToClassList("field--focus"));
            tf.RegisterCallback<FocusOutEvent>(_ => box.RemoveFromClassList("field--focus"));
            box.Add(tf);
            if (password)
            {
                var eye = UI.IconButton("eye", null, 20);
                var eyeIcon = (SvgImage)eye[0];
                eye.Tap(() =>
                {
                    tf.isPasswordField = !tf.isPasswordField;
                    eyeIcon.Icon = tf.isPasswordField ? "eye" : "eye_off";
                });
                eye.tooltip = "Tampilkan password";
                box.Add(eye);
            }
            col.Add(box);
            if (help != null) { var h = UI.Text(help, "t-tiny"); h.style.marginTop = 6; col.Add(h); }
            input = tf;
            return col;
        }

        public static VisualElement Checkbox(bool on)
        {
            var b = UI.Div("checkbox");
            b.EnableInClassList("checkbox--on", on);
            if (on) b.Add(UI.Icon("check", 12));
            return b;
        }

        /// <summary>Isi dialog "Yakin mau keluar?" saat kuis berjalan.</summary>
        public static void ConfirmExitQuiz(OrganoApp app, Action onExit)
        {
            var c = UI.Col(8).Align(Align.Center);
            var ic = UI.Div().Size(64, 64).Radius(32).Bg(UI.PinkSoft).Center();
            ic.Add(UI.Icon("mascot_mini", 52));
            c.Add(ic);
            c.Add(UI.Text("Yakin mau keluar?", "t-h2").TextCenter());
            c.Add(UI.Text("Jawabanmu di kuis ini belum tersimpan. Kalau keluar, kamu harus mulai lagi dari awal.", "t-caption").TextCenter());
            var col = UI.Col(8).AlignSelf(Align.Stretch);
            col.style.marginTop = 12;
            OrganoApp.Overlay dlg = null;
            col.Add(UI.Button("Lanjut Kuis", () => dlg.Close(), "btn-primary"));
            col.Add(UI.Button("Ya, keluar", () => { dlg.Close(); onExit(); }, "btn-ghost"));
            c.Add(col);
            dlg = app.ShowDialog(c, true);
        }
    }
}
