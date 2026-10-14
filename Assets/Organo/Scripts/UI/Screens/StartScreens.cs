using System.Collections.Generic;
using Organo.Data;
using Organo.UI;
using UnityEngine;
using UnityEngine.UIElements;
using U = Organo.UI.UI;

namespace Organo.Screens
{
    // ==================================================================== Splash

    public class SplashScreen : AppScreen
    {
        VisualElement m_Bar, m_Mascot, m_Orbit;
        Label m_Status;
        SvgImage m_ReadyIcon;
        float m_T;
        bool m_Ready;
        static readonly string[] Tips = { "Menyiapkan organ…", "Membangunkan maskot…", "Hampir siap!" };
        /// <summary>Lama bilah loading berjalan sampai penuh ("Siap! Ayo mulai"), dalam detik.</summary>
        public const float LoadDuration = 3.5f;
        /// <summary>Jeda setelah "Siap!" sebelum otomatis pindah ke onboarding / beranda.</summary>
        public const float AutoContinueDelay = 0.7f;

        protected override void Build(object arg)
        {
            m_T = 0; m_Ready = false;
            Root.Gradient(U.Teal, U.Purple, 160);
            Root.Pad(56, 24, 40, 24).Align(Align.Center);

            var deco1 = U.Div().Size(320, 320).Radius(160).Bg(U.C("#FFFFFF", 0.09f)).Abs(-120, -120);
            var deco2 = U.Div().Size(360, 360).Radius(180).Bg(U.C("#FFFFFF", 0.07f)).Abs(null, null, -140, -110);
            deco1.pickingMode = deco2.pickingMode = PickingMode.Ignore;
            Root.Add(deco1); Root.Add(deco2);

            Root.Add(U.Flex());
            var hero = U.Div().Size(280, 280);
            m_Orbit = U.Div("fill").Radius(140).Border(1.5f, U.C("#FFFFFF", 0.4f));
            hero.Add(m_Orbit);
            hero.Add(U.Div().Abs(44, 44, 44, 44).Radius(96).Bg(U.C("#FFFFFF", 0.16f)).Border(2, U.C("#FFFFFF", 0.22f)));
            m_Mascot = U.Icon("mascot_splash", 120, 112).Abs(80, 100);
            hero.Add(m_Mascot);
            var bubbles = new[] { ("region_chest", 15f, 15f), ("cat_napas", 213f, 15f), ("region_belly", 213f, 213f), ("organ_ususbesar", 15f, 213f) };
            foreach (var (icon, x, y) in bubbles)
            {
                var b = U.Div().Size(52, 52).Radius(26).Bg("#FFFFFF").Center().Abs(x, y);
                b.Add(U.Icon(icon, 28));
                hero.Add(b);
            }
            hero.Add(U.Icon("sparkles", 280).Abs(0, 0));
            Root.Add(hero);

            var word = U.Row(12);
            word.style.marginTop = 28;
            var logo = U.Div().Size(48, 48).Radius(14).Bg("#FFFFFF").Center();
            logo.Add(U.Icon("logo", 30));
            word.Add(logo);
            word.Add(U.Text("Organo").FontSize(40).Bold().Fg("#FFFFFF"));
            Root.Add(word);
            var tag = U.Text("Kenali organ tubuhmu, yuk!").FontSize(15).Fg("#FFFFFF");
            tag.style.marginTop = 8;
            Root.Add(tag);
            Root.Add(U.Flex());

            var loader = U.Col(12).W(220).Align(Align.Center);
            var track = U.Div().H(8).Radius(999).Bg(U.C("#FFFFFF", 0.3f)).AlignSelf(Align.Stretch);
            track.style.overflow = Overflow.Hidden;
            m_Bar = U.Div().H(8).Radius(999).Bg("#FFFFFF").W(0);
            track.Add(m_Bar);
            loader.Add(track);
            var status = U.Row(6).MinH(20);
            m_ReadyIcon = U.Icon("check", 16);
            m_ReadyIcon.Visible(false);
            status.Add(m_ReadyIcon);
            m_Status = U.Text(Tips[0]).FontSize(13).Bold().Fg("#FFFFFF");
            status.Add(m_Status);
            loader.Add(status);
            Root.Add(loader);
            var ver = U.Text("versi 1.0").FontSize(11).Fg(U.C("#FFFFFF", 0.8f));
            ver.style.marginTop = 24;
            Root.Add(ver);

            Root.RegisterCallback<PointerUpEvent>(_ => { if (m_Ready) Continue(); });
        }

        void Continue()
        {
            if (!m_Ready) return;
            m_Ready = false;
            if (!P.onboarded) App.Go<OnboardingScreen>(push: false);
            else if (P.gender == Gender.None) App.Go<GenderScreen>(push: false);
            else App.GoTab(NavTab.Beranda);
        }

        public override void Tick(float dt)
        {
            m_T += dt;
            float p = Mathf.Clamp01(m_T / LoadDuration);
            m_Bar.style.width = Length.Percent(p * 100);
            m_Mascot.style.translate = new Translate(0, -10 * Mathf.Abs(Mathf.Sin(m_T * Mathf.PI / 1.6f)));
            m_Orbit.style.rotate = new Rotate(m_T * 9f);
            if (p >= 1f && !m_Ready && m_Status.text != "Siap! Ayo mulai")
            {
                m_Ready = true;
                m_Status.text = "Siap! Ayo mulai";
                m_ReadyIcon.Visible(true);
                m_Status.style.opacity = 1;
            }
            // lanjut sendiri setelah jeda singkat (tidak perlu diketuk; ketukan hanya mempercepat)
            if (m_Ready && m_T >= LoadDuration + AutoContinueDelay) Continue();
            else if (!m_Ready && p < 1f)
            {
                m_Status.text = Tips[Mathf.Min(2, Mathf.FloorToInt(p * 100 / 34))];
                m_Status.style.opacity = 0.7f + 0.3f * Mathf.Abs(Mathf.Sin(m_T * 2.2f));
            }
        }
    }

    // ==================================================================== Onboarding

    public class OnboardingScreen : AppScreen
    {
        int m_Page;
        Vector2 m_SwipeStart;

        static readonly (string tag, string color, string soft, string title, string desc, string art)[] Pages =
        {
            ("Jelajahi tubuh", "#12857A", "#D5F3EF", "Kenalan sama organ tubuhmu!", "Ketuk bagian tubuh, lalu lihat organnya dari dekat lengkap dengan suara penjelasan.", "onb_explore"),
            ("Scan & AR", "#6F52D1", "#E8E2FB", "Scan kartu, organ jadi 3D!", "Arahkan kamera ke kartu Organo dan lihat organ muncul langsung di depanmu.", "onb_scan"),
            ("Kuis seru", "#D6385E", "#FDE0E7", "Main kuis, kumpulkan bintang!", "Jawab kuis, scan organ yang diminta, lalu kumpulkan kartu dan lencana."
                , "onb_quiz"),
        };

        protected override void Build(object arg)
        {
            if (arg is int page) m_Page = page;
            var pg = Pages[m_Page];

            var top = U.Row().Justify(Justify.SpaceBetween).Pad(16, 20, 0, 20);
            var left = U.Div().W(64);
            if (m_Page > 0) { var b = U.IconButton("back", () => Go(m_Page - 1)); b.style.marginLeft = -12; left.Add(b); }
            top.Add(left);
            var brand = U.Row(6);
            brand.Add(U.Icon("logo", 22));
            brand.Add(U.Text("Organo").FontSize(17).Bold().Fg(U.TealDark));
            top.Add(brand);
            var skip = U.Div().W(64).Align(Align.FlexEnd).MinH(44).Justify(Justify.Center);
            skip.Add(U.Text("Lewati").FontSize(13).Fg(U.Muted));
            skip.Tap(Finish);
            top.Add(skip);
            Root.Add(top);

            var art = U.Div().Grow().Center();
            art.Add(U.Icon(pg.art, 340));
            Root.Add(art);

            var text = U.Col(10).Align(Align.Center).Pad(0, 28, 0, 28);
            text.Add(U.Text(pg.title, "t-h1").FontSize(24).TextCenter());
            text.Add(U.Text(pg.desc, "t-body", "t-muted").TextCenter().W(310));
            Root.Add(text);

            var bottom = U.Col(20).Align(Align.Center).Pad(24, 20, 28, 20);
            var dots = U.Row(8);
            for (int i = 0; i < Pages.Length; i++)
            {
                int k = i;
                var d = U.Div().H(8).Radius(4).W(i == m_Page ? 28 : 8).Bg(i == m_Page ? pg.color : "#D9D7E3");
                d.Tap(() => Go(k));
                dots.Add(d);
            }
            bottom.Add(dots);
            var btn = m_Page < Pages.Length - 1
                ? U.Button("Lanjut", () => Go(m_Page + 1), "btn-primary")
                : U.Button("Mulai Sekarang", Finish, "btn-primary");
            btn.Add(U.Icon("arrow_right", 18));
            btn[1].style.marginLeft = 8;
            btn.AlignSelf(Align.Stretch).MinH(54);
            bottom.Add(btn);
            Root.Add(bottom);

            Root.RegisterCallback<PointerDownEvent>(e => m_SwipeStart = e.position);
            Root.RegisterCallback<PointerUpEvent>(e =>
            {
                float dx = e.position.x - m_SwipeStart.x;
                if (Mathf.Abs(dx) > 50) Go(m_Page + (dx < 0 ? 1 : -1));
            });
        }

        void Go(int page)
        {
            page = Mathf.Clamp(page, 0, Pages.Length - 1);
            if (page == m_Page) return;
            m_Page = page;
            Refresh(page);
        }

        void Finish()
        {
            P.onboarded = true;
            P.Save();
            App.Go<GenderScreen>(push: false);
        }

        public override bool OnBack()
        {
            if (m_Page == 0) return false;
            Go(m_Page - 1);
            return true;
        }
    }

    // ==================================================================== Gender

    public class GenderScreen : AppScreen
    {
        Gender m_Pick;
        bool m_FromProfile;
        string m_Name;

        protected override void Build(object arg)
        {
            if (arg is Gender g) m_Pick = g;
            else if (arg == null) m_Pick = P.gender;
            // dibuka dari Profil / Materi Tubuh: setelah memilih, kembali ke halaman itu (bukan ke Beranda)
            if (arg is string) m_FromProfile = true;
            else if (arg == null) { m_FromProfile = false; m_Name = null; }
            // pertama kali dibuka (setelah onboarding): sekalian kenalan, nama panggilan untuk sapaan di Beranda
            bool first = !m_FromProfile;
            Root.Add(Parts.TopBar(first ? "Kenalan Dulu" : "Pilih Gender", first ? (System.Action)(() => App.Go<OnboardingScreen>(2, false)) : App.Back));
            var scroll = U.Scroll();
            Root.Add(scroll);
            var c = U.Col(16);
            c.style.paddingTop = 8;
            scroll.Add(c);
            if (first)
            {
                c.Add(Parts.Field("Nama panggilanmu", "guest", "Contoh: Dito", false, out var name));
                name.maxLength = 16;
                name.value = m_Name ?? (P.nickname == "Teman" ? "" : P.nickname);
                name.RegisterValueChangedCallback(e => m_Name = e.newValue);
            }
            c.Add(U.Text("Kamu laki-laki atau perempuan?", first ? "t-section" : "t-h1"));
            c.Add(U.Text("Pilih sesuai dirimu, ya! Materi tentang tubuh akan disesuaikan dengan pilihanmu.", "t-small", "t-muted"));

            var cards = U.Row(12);
            cards.Add(Choice(Gender.Boy, "gender_boy", "Laki-laki"));
            cards.Add(Choice(Gender.Girl, "gender_girl", "Perempuan"));
            c.Add(cards);

            var what = U.Col(10, "card");
            what.Elev(Elevation.E1);
            what.Add(U.Text("Yang disesuaikan untukmu", "t-section"));
            foreach (var (t, ic) in new[] { ("Tokoh tubuh di Library", "g_person"), ("Materi perubahan tubuh saat tumbuh besar", "g_book"), ("Contoh cerita di kuis", "g_chat") })
            {
                var r = U.Row(8);
                r.Add(U.Icon(ic, 18));
                r.Add(U.Text(t, "t-small").Grow());
                what.Add(r);
            }
            c.Add(what);
            string other = m_Pick == Gender.Girl ? "laki-laki" : m_Pick == Gender.Boy ? "perempuan" : null;
            c.Add(Parts.InfoNote(other != null
                ? "Materi tubuh " + other + " tetap bisa kamu pelajari kapan saja lewat tombol tubuh di Library."
                : "Apa pun pilihanmu, materi tubuh laki-laki dan perempuan tetap bisa dipelajari dari Library."));

            var bottom = U.Col(8).Pad(12, 20, 20, 20);
            bool picked = m_Pick != Gender.None;
            bool back = m_FromProfile;
            bottom.Add(U.Button(!picked ? "Pilih salah satu dulu" : back ? "Simpan" : "Lanjut ke Beranda", () =>
            {
                if (!picked) return;
                P.gender = m_Pick;
                if (!back && m_Name != null) P.nickname = string.IsNullOrWhiteSpace(m_Name) ? "Teman" : m_Name.Trim();
                P.Save();
                if (back) App.Back(); else App.GoTab(NavTab.Beranda);
            }, picked ? "btn-primary" : "btn-disabled"));
            bottom.Add(U.Text("Bisa diubah kapan saja di Profil.", "t-caption").TextCenter());
            Root.Add(bottom);
        }

        public override bool OnBack()
        {
            if (m_FromProfile) return false;
            App.Go<OnboardingScreen>(2, false);
            return true;
        }

        VisualElement Choice(Gender g, string art, string label)
        {
            bool on = m_Pick == g;
            var card = U.Col(10).Grow().MinH(200).Radius(20).Center().Pad(16, 12).Border(2, on ? U.Teal : U.Line).Bg(on ? "#F2FBF9" : "#FFFFFF");
            // gambar di kotaknya sendiri (dipotong rapi), tulisan di bawahnya dengan jarak — tidak menumpuk
            var pic = U.Div().Size(120, 128).NoShrink();
            pic.style.overflow = Overflow.Hidden;
            pic.Add(U.Icon(art, 120, 153));
            card.Add(pic);
            var name = U.Text(label, "t-title").TextCenter();
            name.style.marginTop = 4;
            card.Add(name);
            if (on)
            {
                var tick = U.Div().Size(26, 26).Radius(13).Bg(U.Teal).Center().Abs(null, 10, 10);
                tick.Add(U.Icon("check", 14));
                card.Add(tick);
            }
            card.Tap(() => { m_Pick = g; Refresh(g); });
            return card;
        }
    }
}
