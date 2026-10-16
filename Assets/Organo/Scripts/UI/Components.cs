using System;
using Organo.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Organo.UI
{
    /// <summary>Navbar bawah: Beranda, Library, [Scan bulat menonjol], Quiz, Profil (ikon garis / ikon isi saat aktif).</summary>
    public class NavBar : VisualElement
    {
        readonly OrganoApp m_App;
        readonly VisualElement[] m_Items = new VisualElement[5];
        readonly SvgImage[] m_Icons = new SvgImage[5];
        readonly VisualElement m_FabBtn;
        readonly SvgImage m_FabIcon;
        readonly Label m_FabLabel;

        static readonly (NavTab tab, string off, string on, string label)[] Items =
        {
            (NavTab.Beranda, "nav_home", "nav_home_on", "Beranda"),
            (NavTab.Library, "nav_library", "logo", "Pustaka"),
            (NavTab.Scan, null, null, null),
            (NavTab.Quiz, "nav_quiz", "nav_quiz_on", "Kuis"),
            (NavTab.Profil, "nav_profile", "nav_profile_on", "Profil"),
        };

        public NavBar(OrganoApp app)
        {
            m_App = app;
            AddToClassList("nav");
            var topShade = UI.Div().Abs(0, -6, 0).H(6).Gradient("#2E2A4A14", "#2E2A4A00", 0);
            topShade.pickingMode = PickingMode.Ignore;
            Add(topShade);
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
                var (tab, off, _, label) = Items[i];
                if (off == null) { var spacer = UI.Div("nav__item"); spacer.pickingMode = PickingMode.Ignore; Add(spacer); continue; }
                var item = UI.Div("nav__item");
                var ic = UI.Div("nav__icon");
                m_Icons[i] = UI.Icon(off, 20);
                ic.Add(m_Icons[i]);
                item.Add(ic);
                item.Add(UI.Text(label, "nav__label"));
                item.tooltip = label;
                var t = tab;
                item.Tap(() => m_App.GoTab(t));
                m_Items[i] = item;
                Add(item);
            }

            var fab = UI.Div("nav__fab");
            m_FabBtn = UI.Div("nav__fab-btn").Gradient(UI.Teal, UI.Purple, 135);
            m_FabBtn.Elev(Elevation.Fab);
            m_FabIcon = UI.Icon("nav_scan", 28);
            m_FabBtn.Add(m_FabIcon);
            fab.Add(m_FabBtn);
            m_FabLabel = UI.Text("Scan", "nav__label");
            m_FabLabel.style.marginTop = 4;
            fab.Add(m_FabLabel);
            fab.tooltip = "Scan kartu";
            fab.Tap(() => m_App.GoTab(NavTab.Scan));
            Add(fab);
        }

        public void SetActive(NavTab tab)
        {
            style.display = tab == NavTab.None ? DisplayStyle.None : DisplayStyle.Flex;
            for (int i = 0; i < Items.Length; i++)
            {
                if (m_Items[i] == null) continue;
                bool on = Items[i].tab == tab;
                m_Items[i].EnableInClassList("nav__item--on", on);
                m_Icons[i].Icon = on ? Items[i].on : Items[i].off;
            }
            bool scan = tab == NavTab.Scan;
            m_FabLabel.style.color = UI.C(scan ? UI.TealDark : UI.Faint);
            m_FabLabel.style.unityFontStyleAndWeight = scan ? FontStyle.Bold : FontStyle.Normal;
            m_FabIcon.style.rotate = new Rotate(scan ? 20 : 0);
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

        /// <summary>Ilustrasi kecil organ (versi tile/kartu dari desain). Organ reproduksi yang masih terkunci memakai ikon gembok.</summary>
        public static string MiniIcon(OrganInfo o) =>
            ReproAccess.IsLocked(o, OrganoApp.Instance != null ? OrganoApp.Instance.Progress : null) ? "lock_big"
            : OrganoIcons.Svg.ContainsKey("mini_" + o.id) ? "mini_" + o.id : o.IconKey;

        /// <summary>
        /// Organ ini masih terkunci di Library: kartunya belum pernah di-scan, atau organ reproduksi gender lain yang belum
        /// terbuka (<see cref="ReproAccess"/>).
        /// </summary>
        public static bool Locked(OrganInfo o)
        {
            var p = OrganoApp.Instance != null ? OrganoApp.Instance.Progress : null;
            return ReproAccess.IsLocked(o, p) || NeedsCard(o, p);
        }

        /// <summary>Organ dibuka di Library setelah kartunya di-scan.</summary>
        public static bool NeedsCard(OrganInfo o, PlayerProgress p) => o != null && p != null && !p.HasCard(o.id);

        /// <summary>Lembar "scan kartunya dulu" untuk organ yang kartunya belum pernah di-scan.</summary>
        public static void ShowCardLock(OrganoApp app, OrganInfo o)
        {
            var c = UI.Col(12);
            var head = UI.Col(8).Align(Align.Center);
            var badge = OrganBadge(o, 72, 0.62f, true);
            var lk = UI.Div().Size(26, 26).Radius(13).Bg(UI.Muted).Center().Abs(null, null, -2, -2).Kids(UI.Icon("lock_tiny", 12));
            badge.Add(lk);
            head.Add(badge);
            head.Add(UI.Text(o.displayName + " masih terkunci", "t-h2").TextCenter());
            var why = UI.Text("Scan kartu " + o.displayName + " untuk membuka organ ini.", "t-small", "t-muted").TextCenter();
            why.style.whiteSpace = WhiteSpace.Normal;
            head.Add(why);
            c.Add(head);
            OrganoApp.Overlay sh = null;
            c.Add(UI.Button("Scan kartu " + o.displayName, () => { sh.Close(); app.Go<Organo.Screens.ScanScreen>(o.id); }, "btn-primary"));
            c.Add(UI.Button("Nanti saja", () => sh.Close(), "btn-ghost"));
            sh = app.ShowSheet(c);
        }

        /// <summary>
        /// Lembar "masih terkunci" untuk organ reproduksi gender lain: jelaskan aturannya dan arahkan ke organ & kuis milik sendiri.
        /// </summary>
        public static void ShowReproLock(OrganoApp app, OrganInfo o)
        {
            var p = app.Progress;
            var c = UI.Col(12);
            var head = UI.Col(8).Align(Align.Center);
            head.Add(UI.Div().Size(64, 64).Radius(32).Bg(UI.PurpleSoft).Center().Kids(UI.Icon("lock_big", 30)));
            head.Add(UI.Text(o.displayName + " masih terkunci", "t-h2").TextCenter());
            var why = UI.Text(ReproAccess.LockReason(o.id, p), "t-small", "t-muted").TextCenter();
            why.style.whiteSpace = WhiteSpace.Normal;
            head.Add(why);
            c.Add(head);
            OrganoApp.Overlay sh = null;
            string own = ReproAccess.OwnId(p.gender);
            if (own == null)
                c.Add(UI.Button("Pilih tubuhku", () => { sh.Close(); app.Go<Organo.Screens.GenderScreen>("profile"); }, "btn-primary"));
            else
            {
                var ownOrgan = app.Database.Get(own);
                c.Add(UI.Button("Mulai " + ReproAccess.QuizName(own), () => { sh.Close(); Organo.Screens.QuizLocks.PlayOrgan(app, own); }, "btn-primary"));
                if (ownOrgan != null)
                    c.Add(UI.Button("Pelajari " + ownOrgan.displayName + " dulu", () => { sh.Close(); app.Go<Organo.Screens.OrganDetailScreen>(own); }, "btn-outline"));
            }
            c.Add(UI.Button("Nanti saja", () => sh.Close(), "btn-ghost"));
            sh = app.ShowSheet(c);
        }

        /// <summary>Buka organ; organ reproduksi yang terkunci menampilkan lembar penjelasan.</summary>
        public static void OpenOrgan(OrganoApp app, OrganInfo o, string suffix = "")
        {
            var p = app.Progress;
            if (ReproAccess.IsLocked(o, p)) { ShowReproLock(app, o); return; }
            if (NeedsCard(o, p)) { ShowCardLock(app, o); return; }
            app.Go<Organo.Screens.OrganDetailScreen>(o.id + suffix);
        }

        /// <summary>Lingkaran ilustrasi organ.</summary>
        public static VisualElement OrganBadge(OrganInfo o, float size, float iconScale = 0.62f, bool locked = false)
        {
            var c = UI.Div().Size(size, size).Radius(size / 2).Center();
            c.style.backgroundColor = locked ? UI.C(UI.Line) : o.tint;
            var icon = UI.Icon(MiniIcon(o), size * iconScale);
            if (locked) { icon.Tint = UI.C("#8C89A3"); icon.style.opacity = 0.35f; }
            c.Add(icon);
            return c;
        }

        /// <summary>Badge gembok: lingkaran #6B6785 dengan border putih 2px dan ikon gembok putih.</summary>
        public static VisualElement LockBadge(float size = 26f)
        {
            var b = UI.Div().Size(size, size).Radius(size / 2).Bg(UI.Muted).Border(2, "#FFFFFF").Center();
            b.Add(UI.Icon("lock_tiny", size * 0.46f));
            return b;
        }

        /// <summary>
        /// Kartu organ (Style Guide 8.6): pita sistem overline, lingkaran 104px, nama 14/600, keterangan 11/500.
        /// Dimiliki: border 2px warna sistem + elevation-2. Belum: latar #F6F5F9, border putus-putus #CFCCDD,
        /// ilustrasi abu-abu .35 dan badge gembok. Baru: pill "BARU" pink di kiri atas.
        /// </summary>
        public static VisualElement OrganCard(OrganInfo o, bool owned, float width = 168f, bool isNew = false, string sub = null, float art = 104f)
        {
            var (fg, soft) = OrganDefaults.SystemColors(o.system);
            var card = UI.Col(6, "organ-card").W(width).MinH(212).Radius(18);
            card.style.paddingBottom = 12;
            if (owned) { card.Bg("#FFFFFF").Border(2, fg); card.Elev(Elevation.E2); }
            else
            {
                card.Bg("#F6F5F9");
                card.Add(new DashedOutline(UI.C("#CFCCDD"), 2f, 18f));
            }
            var band = UI.Div("organ-card__band").Bg(owned ? fg : UI.C("#E4E2EC"));
            var bandText = UI.Text(OrganDefaults.SystemName(o.system).ToUpperInvariant(), "t-overline").TextCenter();
            bandText.style.color = owned ? Color.white : UI.C(UI.Muted);
            band.Add(bandText);
            card.Add(band);
            var circle = UI.Div().Size(art, art).Radius(art / 2).Center().Bg(owned ? soft : UI.C(UI.Line));
            circle.style.marginTop = 6;
            var icon = UI.Icon(MiniIcon(o), art * 0.58f);
            if (!owned) { icon.Tint = UI.C("#8C89A3"); icon.style.opacity = 0.35f; }
            circle.Add(icon);
            if (!owned) circle.Add(LockBadge().Abs(null, null, 0, 0));
            card.Add(circle);
            card.Add(UI.Text(o.displayName).FontSize(14).Bold().Fg(owned ? UI.C(UI.Ink) : UI.C(UI.Muted)).TextCenter());
            card.Add(UI.Text(sub ?? ("Kartu #" + o.cardNumber + " · " + OrganDefaults.SystemName(o.system))).FontSize(11).Fg(owned ? fg : UI.C(UI.Faint)).TextCenter());
            if (isNew)
            {
                var pill = UI.Pill("BARU", UI.Pink, "#FFFFFF").Abs(8, 34);
                pill.style.paddingTop = 2; pill.style.paddingBottom = 2; pill.style.paddingLeft = 8; pill.style.paddingRight = 8;
                card.Add(pill);
            }
            return card;
        }

        /// <summary>Chip bintang di header (ikon bintang pink sesuai desain).</summary>
        public static VisualElement Stars(int count, bool chip = true)
        {
            var row = UI.Row(6);
            if (chip) { row.Pad(6, 12).Bg("#FFFFFF").Border(1, "#EDEBF3").Radius(999); }
            row.Add(UI.Icon("star", 18));
            row.Add(UI.Text(count.ToString()).FontSize(14).Bold());
            row.tooltip = count + " bintang";
            return row;
        }

        /// <summary>Persentase dalam lingkaran 46px (#D5F3EF, teks #0F6B61 13/600).</summary>
        public static VisualElement PercentCircle(int percent, float size = 46f)
        {
            var c = UI.Div().Size(size, size).Radius(size / 2).Bg(UI.TealSoft).Center();
            c.Add(UI.Text(percent + "%").FontSize(13).Bold().Fg(UI.TealInk));
            return c;
        }

        /// <summary>Switch 44x26, knob bergeser 3px ke 21px.</summary>
        public static VisualElement Switch(bool on, Action<bool> changed, string label)
        {
            var sw = UI.Div("switch");
            sw.EnableInClassList("switch--on", on);
            sw.Add(UI.Div("switch__knob"));
            sw.tooltip = label;
            sw.Tap(() =>
            {
                bool v = !sw.ClassListContains("switch--on");
                sw.EnableInClassList("switch--on", v);
                changed?.Invoke(v);
            });
            return sw;
        }

        /// <summary>Kartu statistik (min 84px, angka 18/600, label 11px).</summary>
        public static VisualElement StatCard(string icon, string value, string label, Action tap = null)
        {
            var s = UI.Col(4, "card").Grow().MinH(84).Align(Align.Center).Justify(Justify.Center).Pad(8, 4).Radius(14);
            s.Elev(Elevation.E1);
            s.Add(UI.Icon(icon, 20));
            s.Add(UI.Text(value).FontSize(18).Bold());
            s.Add(UI.Text(label, "t-tiny").TextCenter());
            if (tap != null) s.Tap(tap);
            return s;
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
            col.Add(UI.Text(label, "field-label"));
            var ring = UI.Div("field-ring");
            var box = UI.Div("field");
            ring.Add(box);
            if (icon != null) box.Add(UI.Icon(icon, 20));
            var tf = new TextField { isPasswordField = password };
            tf.textEdition.placeholder = placeholder;
            tf.textEdition.hidePlaceholderOnFocus = true;
            tf.RegisterCallback<FocusInEvent>(_ => { box.AddToClassList("field--focus"); ring.AddToClassList("field-ring--focus"); });
            tf.RegisterCallback<FocusOutEvent>(_ => { box.RemoveFromClassList("field--focus"); ring.RemoveFromClassList("field-ring--focus"); });
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
            col.Add(ring);
            if (help != null) { var h = UI.Text(help, "t-caption"); h.style.marginTop = 6; col.Add(h); }
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

        /// <summary>Isi dialog "Yakin ingin keluar?" saat kuis berjalan.</summary>
        public static void ConfirmExitQuiz(OrganoApp app, Action onExit)
        {
            var c = UI.Col(8).Align(Align.Center);
            c.Add(UI.Icon("mascot_worry", 84, 72));
            c.Add(UI.Text("Yakin ingin keluar?", "t-h3").TextCenter());
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

    /// <summary>Border putus-putus (USS tidak punya border-style: dashed).</summary>
    public class DashedOutline : VisualElement
    {
        readonly Color m_Color;
        readonly float m_Width, m_Radius;

        public DashedOutline(Color color, float width, float radius)
        {
            m_Color = color; m_Width = width; m_Radius = radius;
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0; style.top = 0; style.right = 0; style.bottom = 0;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext mgc)
        {
            var r = contentRect;
            float h = m_Width / 2;
            r = new Rect(r.x + h, r.y + h, r.width - m_Width, r.height - m_Width);
            if (r.width <= 0 || r.height <= 0) return;
            float rad = Mathf.Min(m_Radius - h, Mathf.Min(r.width, r.height) / 2);
            var pts = new System.Collections.Generic.List<Vector2>();
            void Arc(Vector2 c, float a0)
            {
                for (int i = 0; i <= 6; i++)
                {
                    float a = (a0 + 90f * i / 6f) * Mathf.Deg2Rad;
                    pts.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad);
                }
            }
            Arc(new Vector2(r.xMax - rad, r.yMin + rad), -90);
            Arc(new Vector2(r.xMax - rad, r.yMax - rad), 0);
            Arc(new Vector2(r.xMin + rad, r.yMax - rad), 90);
            Arc(new Vector2(r.xMin + rad, r.yMin + rad), 180);
            pts.Add(pts[0]);
            var p = mgc.painter2D;
            p.strokeColor = m_Color;
            p.lineWidth = m_Width;
            p.lineCap = LineCap.Butt;
            p.BeginPath();
            float dash = 6f, gap = 4f, left = dash;
            bool on = true;
            p.MoveTo(pts[0]);
            for (int i = 1; i < pts.Count; i++)
            {
                var a = pts[i - 1]; var b = pts[i];
                float len = Vector2.Distance(a, b), pos = 0;
                while (len - pos > left)
                {
                    pos += left;
                    var q = Vector2.Lerp(a, b, pos / len);
                    if (on) p.LineTo(q); else p.MoveTo(q);
                    on = !on;
                    left = on ? dash : gap;
                }
                left -= len - pos;
                if (on) p.LineTo(b);
            }
            p.Stroke();
        }
    }
}
