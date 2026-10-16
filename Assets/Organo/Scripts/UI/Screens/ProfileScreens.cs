using System.Collections.Generic;
using System.Linq;
using Organo.Data;
using Organo.UI;
using UnityEngine;
using UnityEngine.UIElements;
using U = Organo.UI.UI;

namespace Organo.Screens
{
    public class Badge
    {
        public string id, name, desc, icon, color;
        public int progress, goal;
        public string cta;
        public bool Earned => progress >= goal;

        public static List<Badge> All(PlayerProgress p) => new List<Badge>
        {
            new Badge { id = "first", name = "Langkah Pertama", desc = "Menyelesaikan kuis pertama", icon = "badge_star", color = U.Teal, progress = Mathf.Min(1, p.history.Count + p.QuizzesCompleted), goal = 1, cta = "Mulai Kuis" },
            new Badge { id = "champ", name = "Juara Kuis", desc = "Semua soal benar dalam satu kuis", icon = "badge_trophy", color = U.Pink, progress = p.history.Any(h => h.total > 0 && h.correct == h.total) ? 1 : 0, goal = 1, cta = "Mulai Kuis" },
            new Badge { id = "explorer", name = "Penjelajah Organ", desc = "Membuka 5 organ di Pustaka", icon = "badge_medal", color = U.Purple, progress = Mathf.Min(5, p.openedOrgans.Count), goal = 5, cta = "Buka Pustaka" },
            new Badge { id = "stars", name = "Bintang Bersinar", desc = "Mengumpulkan 200 bintang", icon = "badge_star", color = U.Yellow, progress = Mathf.Min(200, p.stars), goal = 200, cta = "Kumpulkan Bintang" },
            new Badge { id = "digest", name = "Ahli Tubuh", desc = "Menyelesaikan semua kuis proses", icon = "badge_trophy", color = U.TealDark, progress = QuizAccess.ProcessesDone(p), goal = QuizBank.Processes.Count, cta = "Lanjut Kuis" },
        };

        /// <summary>Lencana: lingkaran 52-56px (daftar) atau 116px dengan border putih 5px (tampilan besar).</summary>
        public VisualElement Medal(float size)
        {
            var wrap = U.Div().Size(size, size);
            if (size >= 100 && Earned)
            {
                var glow = U.Div().Abs(-24, -24, -24, -24);
                glow.style.backgroundImage = new StyleBackground(GradientTexture.Radial(U.C(color, 0.45f)));
                glow.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
                glow.pickingMode = PickingMode.Ignore;
                wrap.Add(glow);
            }
            var m = U.Div().Size(size, size).Radius(size / 2).Center().Bg(Earned ? color : "#E4E2EC");
            if (size >= 100) m.Border(5, "#FFFFFF");
            var ic = U.Icon(icon, size * 0.5f);
            ic.Tint = Earned ? Color.white : U.C("#BDBACE");
            m.Add(ic);
            wrap.Add(m);
            if (!Earned) wrap.Add(Parts.LockBadge(Mathf.Clamp(size * 0.38f, 20f, 34f)).Abs(null, null, -2, -2));
            return wrap;
        }
    }

    // ==================================================================== Profil

    public class ProfileScreen : AppScreen
    {
        public override NavTab Tab => NavTab.Profil;

        protected override void Build(object arg)
        {
            Root.Add(Parts.TopBar("Profil", () => App.GoTab(NavTab.Beranda)));
            var scroll = U.Scroll();
            Root.Add(scroll);
            var c = U.Col(12);
            scroll.Add(c);

            var head = U.Col(5).Align(Align.Center).Pad(8, 0);
            head.Add(Parts.Avatar(P.gender, 84));
            // nama panggilan: ketuk untuk mengubah (tidak ada akun, cukup nama untuk sapaan)
            var name = U.Row(6).AlignSelf(Align.Center).Pad(4, 8);
            name.Add(U.Text(P.nickname, "t-h1"));
            name.Add(U.Icon("edit", 16).Tinted(U.C(U.Muted)));
            name.tooltip = "Ubah nama panggilan";
            name.Tap(ShowRename);
            head.Add(name);
            c.Add(head);

            var stats = U.Row(10);
            stats.Add(Parts.StatCard("stat_star", P.stars.ToString(), "Bintang"));
            stats.Add(Parts.StatCard("stat_cards", P.cards.Count + "/" + Db.Count, "Kartu", () => App.Go<CardCollectionScreen>()));
            stats.Add(Parts.StatCard("stat_quiz", P.QuizzesCompleted.ToString(), "Kuis Selesai", () => App.Go<QuizHistoryScreen>()));
            c.Add(stats);

            var badges = Badge.All(P);
            var bsec = U.Col(8);
            bsec.Add(Parts.SectionHeader("Lencana", "Lihat semua", () => App.Go<BadgesScreen>()));
            var rail = U.Scroll(true);
            rail.style.flexShrink = 0;
            rail.contentContainer.style.flexDirection = FlexDirection.Row;
            rail.contentContainer.style.paddingLeft = 0; rail.contentContainer.style.paddingRight = 0; rail.contentContainer.style.paddingBottom = 0;
            foreach (var b in badges)
            {
                var item = U.Col(6).W(84).Align(Align.Center);
                item.style.marginRight = 12;
                item.Add(b.Medal(56));
                item.Add(U.Text(b.name).FontSize(10).TextCenter().Fg(b.Earned ? U.Ink : U.Faint));
                var bb = b;
                item.Tap(() => App.Go<BadgesScreen>(bb.id));
                rail.Add(item);
            }
            bsec.Add(rail);
            c.Add(bsec);

            var list = U.Col(0, "card", "card-lg").Pad(4, 14);
            list.Elev(Elevation.E1);
            // kelas online (opsional): tidak ada di build full offline
            if (KelasOnline.Tersedia)
                list.Add(Setting("kelas", "Kelas", KelasOnline.Tergabung(P) ? P.kelasNama : "Belum bergabung", ShowKelas));
            list.Add(Setting("set_history", "Riwayat Kuis", P.history.Count + " kuis", () => App.Go<QuizHistoryScreen>()));
            list.Add(Setting("set_body", "Materi Tubuh", P.gender == Gender.Girl ? "Perempuan" : "Laki-laki", () => App.Go<BodyMaterialScreen>()));
            list.Add(SoundRow());
            list.Add(NarrationRow());
            list.Add(Setting("set_eye", "Mode Aksesibilitas", null, () => App.Go<AccessibilityScreen>()));
            list.Add(Setting("set_help", "Bantuan", null, ShowHelp, true));
            c.Add(list);

            c.Add(U.Space(8));
        }

        /// <summary>
        /// Kelas online: gabung dengan kode kelas dari guru (hasil belajar dikirim ke guru), lihat status kiriman, atau keluar.
        /// Tanpa kelas aplikasi tetap offline.
        /// </summary>
        public void ShowKelas()
        {
            var c = U.Col(12);
            OrganoApp.Overlay sh = null;
            sh = App.ShowSheet(c, true, () => KelasOnline.Berubah -= Segarkan);
            void Segarkan() { if (c.panel != null) IsiKelas(c, () => sh.Close()); }
            KelasOnline.Berubah += Segarkan;
            IsiKelas(c, () => sh.Close());
        }

        void IsiKelas(VisualElement c, System.Action tutup)
        {
            c.Clear();
            if (KelasOnline.Tergabung(P))
            {
                c.Add(U.Text("Kelas", "t-h2"));
                var box = U.Row(12).Pad(12, 14).Radius(14).Bg(U.TealSoft);
                box.Add(U.Div().Size(44, 44).Radius(22).Bg("#FFFFFF").Center().Kids(U.Icon("kelas", 24)));
                var t = U.Col(2).Grow();
                t.Add(U.Text(P.kelasNama, "t-body").Bold());
                t.Add(U.Text(P.kelasGuru + " · kode " + P.kelasKode, "t-caption"));
                box.Add(t);
                c.Add(box);
                c.Add(U.Text("Namamu di kelas: " + (string.IsNullOrWhiteSpace(P.kelasNamaMurid) ? P.nickname : P.kelasNamaMurid), "t-small"));
                var st = U.Text(KelasOnline.StatusTeks(P), "t-caption");
                st.style.whiteSpace = WhiteSpace.Normal;
                c.Add(st);
                if (KelasOnline.Status != KelasOnline.Keadaan.KelasHilang)
                    c.Add(U.Button("Kirim sekarang", () => KelasOnline.KirimSekarang(true), "btn-primary"));
                var keluar = U.Button("Keluar dari kelas", () => KonfirmasiKeluar(c, tutup), "btn-ghost");
                if (keluar.Q<Label>() is Label kl) kl.style.color = U.C(U.PinkDark);
                c.Add(keluar);
                return;
            }

            c.Add(U.Text("Gabung Kelas", "t-h2"));
            if (!string.IsNullOrEmpty(KelasOnline.Pesan))
            {
                // mis. "Kamu sudah dikeluarkan dari Kelas 5A oleh guru."
                var catatan = U.Text(KelasOnline.Pesan, "t-small").Fg(U.PinkDark);
                catatan.style.whiteSpace = WhiteSpace.Normal;
                c.Add(catatan);
            }
            var info = U.Text("Masukkan kode kelas dari gurumu. Nilai kuis, progres belajar, kartu, dan bintangmu akan terlihat oleh guru.", "t-small", "t-muted");
            info.style.whiteSpace = WhiteSpace.Normal;
            c.Add(info);
            c.Add(Parts.Field("Kode kelas", "kelas", "6 huruf/angka", false, out var kode));
            kode.maxLength = 9;
            var err = U.Text("", "t-small").Fg(U.PinkDark);
            err.style.whiteSpace = WhiteSpace.Normal;
            err.Visible(false);
            c.Add(err);
            VisualElement cek = null;
            cek = U.Button("Cek Kode", () =>
            {
                cek.SetEnabled(false);
                err.Visible(false);
                App.StartCoroutine(KelasOnline.Cek(kode.value, (kelas, salah) =>
                {
                    cek.SetEnabled(true);
                    if (salah != null) { err.text = salah; err.Visible(true); OrganoAudio.Wrong(); return; }
                    KonfirmasiGabung(c, kelas, tutup);
                }));
            }, "btn-primary");
            c.Add(cek);
            c.Add(U.Button("Nanti saja", tutup, "btn-ghost"));
        }

        void KonfirmasiGabung(VisualElement c, KelasOnline.InfoKelas kelas, System.Action tutup)
        {
            c.Clear();
            c.Add(U.Text("Gabung ke kelas ini?", "t-h2"));
            var box = U.Row(12).Pad(12, 14).Radius(14).Bg(U.TealSoft);
            box.Add(U.Div().Size(44, 44).Radius(22).Bg("#FFFFFF").Center().Kids(U.Icon("kelas", 24)));
            var t = U.Col(2).Grow();
            t.Add(U.Text(kelas.namaKelas, "t-body").Bold());
            t.Add(U.Text(kelas.namaGuru, "t-caption"));
            box.Add(t);
            c.Add(box);
            c.Add(Parts.Field("Namamu (dilihat guru)", "guest", "Nama lengkap atau nama panggilan", false, out var nama));
            nama.maxLength = 30;
            nama.value = P.nickname == "Teman" ? "" : P.nickname;
            c.Add(U.Button("Gabung", () =>
            {
                KelasOnline.Gabung(P, kelas, nama.value);
                OrganoAudio.Success();
                App.Toast("Kamu bergabung ke " + kelas.namaKelas);
                tutup();
                Refresh();
            }, "btn-primary"));
            c.Add(U.Button("Batal", () => IsiKelas(c, tutup), "btn-ghost"));
        }

        void KonfirmasiKeluar(VisualElement c, System.Action tutup)
        {
            c.Clear();
            c.Add(U.Text("Keluar dari " + P.kelasNama + "?", "t-h2"));
            var d = U.Text("Nilai dan progresmu di kelas ini akan dihapus dari halaman guru. Progres di HP ini tetap tersimpan.", "t-small", "t-muted");
            d.style.whiteSpace = WhiteSpace.Normal;
            c.Add(d);
            var ya = U.Button("Ya, keluar", () => { KelasOnline.Keluar(P); tutup(); Refresh(); }, "btn-primary");
            ya.Bg(U.PinkDark);
            c.Add(ya);
            c.Add(U.Button("Batal", () => IsiKelas(c, tutup), "btn-ghost"));
        }

        /// <summary>Ubah nama panggilan (dipakai untuk sapaan di Beranda & Kuis).</summary>
        public void ShowRename()
        {
            var c = U.Col(12);
            c.Add(U.Text("Nama panggilan", "t-h2"));
            c.Add(Parts.Field("Nama panggilanmu", "guest", "Contoh: Dito", false, out var field));
            field.maxLength = 16;
            field.value = P.nickname == "Teman" ? "" : P.nickname;
            OrganoApp.Overlay sh = null;
            c.Add(U.Button("Simpan", () =>
            {
                P.nickname = string.IsNullOrWhiteSpace(field.value) ? "Teman" : field.value.Trim();
                P.Save();
                sh.Close();
                Refresh();
            }, "btn-primary"));
            c.Add(U.Button("Batal", () => sh.Close(), "btn-ghost"));
            sh = App.ShowSheet(c);
        }

        /// <summary>Bantuan: cara scan, asal kartu, dan versi aplikasi (dulu hanya toast).</summary>
        void ShowHelp()
        {
            var c = U.Col(12);
            c.Add(U.Text("Bantuan", "t-h2"));
            var tips = U.Col(8);
            for (int i = 0; i < ScanScreen.ScanTips.Length; i++)
            {
                var r = U.Row(10).Align(Align.FlexStart);
                r.Add(U.Div().Size(22, 22).Radius(11).Bg(U.TealSoft).Center().NoShrink().Kids(U.Text((i + 1).ToString()).FontSize(11).Bold().Fg(U.TealDark)));
                var t = U.Text(ScanScreen.ScanTips[i], "t-small").Fg(U.Ink).Grow();
                t.style.whiteSpace = WhiteSpace.Normal;
                r.Add(t);
                tips.Add(r);
            }
            c.Add(U.Text("Supaya kartu cepat dikenali", "t-section"));
            c.Add(tips);
            OrganoApp.Overlay sh = null;
            c.Add(U.Button("Kartunya dari mana?", () => { sh.Close(); CardSource.Show(App); }, "btn-outline"));
            c.Add(U.Text("Organo " + Application.version, "t-caption").TextCenter());
            c.Add(U.Button("Tutup", () => sh.Close(), "btn-ghost"));
            sh = App.ShowSheet(c);
        }

        VisualElement Stat(string icon, string tint, string value, string label, System.Action tap)
        {
            var s = U.Col(4, "card").Grow().MinH(84).Align(Align.Center).Justify(Justify.Center).Pad(8, 4);
            var ic = U.Icon(icon, 22);
            if (tint != null) ic.Tint = U.C(tint);
            s.Add(ic);
            s.Add(U.Text(value, "t-h2"));
            s.Add(U.Text(label, "t-tiny").TextCenter());
            if (tap != null) s.Tap(tap);
            return s;
        }

        static VisualElement Setting(string icon, string label, string value, System.Action tap, bool last = false)
        {
            var r = U.Row(14).MinH(48);
            if (!last) { r.style.borderBottomWidth = 1; r.style.borderBottomColor = U.C(U.Line); }
            r.Add(U.Icon(icon, 22));
            r.Add(U.Text(label, "t-body").Grow());
            if (value != null) r.Add(U.Text(value, "t-small", "t-muted"));
            r.Add(U.Icon("chevron_right", 18));
            r.Tap(tap);
            return r;
        }

        VisualElement SoundRow()
        {
            var r = U.Row(14).MinH(48);
            r.style.borderBottomWidth = 1; r.style.borderBottomColor = U.C(U.Line);
            r.Add(U.Icon("set_sound", 22));
            r.Add(U.Text("Suara & Musik", "t-body").Grow());
            r.Add(Parts.Switch(P.sound, v => { P.sound = v; OrganoAudio.Enabled = v; P.Save(); }, "Suara & Musik"));
            return r;
        }

        VisualElement NarrationRow()
        {
            var r = U.Row(14).MinH(48);
            r.style.borderBottomWidth = 1; r.style.borderBottomColor = U.C(U.Line);
            r.Add(U.Icon("set_voice", 22));
            r.Add(U.Text("Suara Pembaca", "t-body").Grow());
            r.Add(Parts.Switch(P.narration, v => { P.narration = v; OrganoAudio.NarrationEnabled = v; P.Save(); }, "Suara Pembaca"));
            return r;
        }
    }

    /// <summary>Cincin progres (ringkasan koleksi).</summary>
    public class ProgressRing : VisualElement
    {
        readonly float m_T;
        readonly Color m_Fill, m_Track;

        public ProgressRing(float t, float size, Color fill, Color track, string big, string small, Color? text = null)
        {
            m_T = Mathf.Clamp01(t); m_Fill = fill; m_Track = track;
            style.width = size; style.height = size; style.flexShrink = 0;
            style.alignItems = Align.Center; style.justifyContent = Justify.Center;
            generateVisualContent += Draw;
            var b = U.Text(big).FontSize(18).Bold();
            var sm = U.Text(small).FontSize(10);
            if (text.HasValue) { b.style.color = text.Value; sm.style.color = text.Value; }
            Add(b);
            Add(sm);
        }

        void Draw(MeshGenerationContext mgc)
        {
            var r = contentRect;
            var p = mgc.painter2D;
            var c = r.center;
            float rad = Mathf.Min(r.width, r.height) / 2 - 5;
            p.lineWidth = 8;
            p.lineCap = LineCap.Round;
            p.strokeColor = m_Track;
            p.BeginPath(); p.Arc(c, rad, 0, 360); p.Stroke();
            if (m_T <= 0) return;
            p.strokeColor = m_Fill;
            p.BeginPath(); p.Arc(c, rad, -90, -90 + 360 * m_T); p.Stroke();
        }
    }

    // ==================================================================== Lencana

    public class BadgesScreen : AppScreen
    {
        string m_Sel;

        protected override void Build(object arg)
        {
            var all = Badge.All(P);
            if (arg is string id) m_Sel = id;
            var sel = all.FirstOrDefault(b => b.id == m_Sel) ?? all[0];
            Root.Add(Parts.TopBar("Lencana", App.Back, U.Text(all.Count(b => b.Earned) + "/" + all.Count, "t-small", "t-muted").Bold()));
            var scroll = U.Scroll();
            Root.Add(scroll);
            var c = U.Col(16);
            scroll.Add(c);

            var show = U.Col(10, "card", "card-lg").Align(Align.Center).Pad(20, 16);
            show.Add(U.Pill(sel.Earned ? "Didapat" : "Terkunci", sel.Earned ? U.TealSoft : "#F6F5F9", sel.Earned ? U.TealDark : U.Muted).AlignSelf(Align.Center));
            show.Add(sel.Medal(116));
            show.Add(U.Text(sel.name, "t-h1").TextCenter());
            show.Add(U.Text(sel.desc, "t-small", "t-muted").TextCenter());
            if (!sel.Earned)
            {
                var pr = U.Col(6).AlignSelf(Align.Stretch);
                var row = U.Row().Justify(Justify.SpaceBetween);
                row.Add(U.Text("Progres", "t-caption").Bold());
                row.Add(U.Text(sel.progress + "/" + sel.goal, "t-caption").Bold());
                pr.Add(row);
                pr.Add(U.Progress((float)sel.progress / sel.goal, sel.color, "#F0EEF5", 8));
                show.Add(pr);
                show.Add(U.Button(sel.cta, () =>
                {
                    if (sel.id == "explorer") App.GoTab(NavTab.Library);
                    else App.GoTab(NavTab.Quiz);
                }, "btn-primary").AlignSelf(Align.Stretch));
            }
            c.Add(show);

            c.Add(U.Text("Semua lencana", "t-section"));
            var grid = U.Row().Justify(Justify.SpaceBetween);
            grid.style.flexWrap = Wrap.Wrap;
            foreach (var b in all)
            {
                bool on = b.id == sel.id;
                var item = U.Col(6).W(104).Pad(10, 4).Radius(14).Align(Align.Center).Bg(on ? "#F3EFFD" : "#FFFFFF").Border(on ? 2 : 1, on ? U.Purple : U.Line);
                item.style.marginBottom = 12;
                item.Add(b.Medal(52));
                item.Add(U.Text(b.name).FontSize(11).TextCenter());
                var bid = b.id;
                item.Tap(() => Refresh(bid));
                grid.Add(item);
            }
            c.Add(grid);
        }
    }

    // ==================================================================== Aksesibilitas

    public class AccessibilityScreen : AppScreen
    {
        int m_Size = -1, m_Speed = -1;
        bool m_Captions, m_Contrast, m_Calm, m_Loaded;

        protected override void Build(object arg)
        {
            if (!m_Loaded)
            {
                m_Loaded = true;
                m_Size = P.textSize; m_Speed = P.voiceSpeed;
                m_Captions = P.captions; m_Contrast = P.highContrast; m_Calm = P.reduceMotion;
            }
            Root.Add(Parts.TopBar("Aksesibilitas", () => { m_Loaded = false; App.Back(); }));
            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 8;
            Root.Add(scroll);
            var c = U.Col(18);
            scroll.Add(c);
            float[] sizes = { 13, 15, 18 };
            float fs = sizes[m_Size];

            // Contoh tampilan langsung
            var prev = U.Col(6).Pad(16).Radius(16);
            if (m_Contrast) prev.Bg("#1B1830").Border(2, "#FFE27A"); else prev.Bg("#FFFBF2").Border(1, "#BDEBE5");
            string ink = m_Contrast ? "#FFFFFF" : U.Ink;
            var pr = U.Row(12);
            pr.Add(Parts.OrganBadge(Db.Get("lambung"), 56));
            var pt = U.Col(2).Grow();
            pt.Add(U.Text("Lambung").FontSize(fs + 5).Bold().Fg(ink));
            // contoh kalimat yang sedang dibacakan disorot (sama seperti di halaman organ)
            var sample = U.Text(m_Captions && !m_Contrast
                ? "<mark=#1AB8A62E><color=#0F6B61>Lambung adalah kantong lentur</color></mark> yang mengaduk makanan."
                : "Lambung adalah kantong lentur yang mengaduk makanan.").FontSize(fs).Fg(ink);
            sample.style.whiteSpace = WhiteSpace.Normal;
            pt.Add(sample);
            pr.Add(pt);
            prev.Add(pr);
            c.Add(prev);

            // Ukuran teks: 3 kartu "Aa"
            var sizeCol = U.Col(8);
            sizeCol.Add(U.Text("Ukuran teks", "t-section"));
            var sizeRow = U.Row(10);
            string[] sizeNames = { "Kecil", "Sedang", "Besar" };
            float[] aa = { 14, 18, 24 };
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                bool on = m_Size == i;
                var b = U.Col(2).Grow().MinH(72).Radius(14).Center().Border(2, on ? U.Teal : U.Line).Bg(on ? "#F2FBF9" : "#FFFFFF");
                b.Add(U.Text("Aa").FontSize(aa[i]).Bold());
                b.Add(U.Text(sizeNames[i], "t-caption"));
                b.Tap(() => { m_Size = k; Refresh(); });
                sizeRow.Add(b);
            }
            sizeCol.Add(sizeRow);
            c.Add(sizeCol);

            // Kecepatan suara: segmen pill
            var spCol = U.Col(8);
            spCol.Add(U.Text("Kecepatan suara penjelasan", "t-section"));
            var seg = U.Row(4).Pad(4).Radius(999).Bg("#EDEBF3");
            string[] speeds = { "Lambat", "Normal", "Cepat" };
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                bool on = m_Speed == i;
                var b = U.Div().Grow().MinH(40).Radius(999).Center().Bg(on ? "#FFFFFF" : "#00000000");
                if (on) b.Elev(Elevation.E1);
                b.Add(U.Text(speeds[i], "t-small").Bold().Fg(on ? U.Ink : U.Muted));
                b.Tap(() => { m_Speed = k; Refresh(); });
                seg.Add(b);
            }
            spCol.Add(seg);
            c.Add(spCol);

            // Suara pembaca: pilih suara yang paling enak didengar dari suara Bahasa Indonesia di HP ini
            var voCol = U.Col(8);
            voCol.Add(U.Text("Suara pembaca", "t-section"));
            OrganoSpeech.EnsureVoices();
            if (OrganoSpeech.Voices.Count == 0)
            {
                string note = Application.isEditor ? "Pilihan suara muncul saat aplikasi dibuka di HP."
                    : OrganoSpeech.Status == OrganoSpeech.State.MissingLanguage ? "Pasang suara Bahasa Indonesia di Pengaturan HP > Text-to-speech (pilih Google)."
                    : "Suara pembaca dipilih otomatis: suara Bahasa Indonesia terbaik di HP ini.";
                var nt = U.Text(note, "t-caption");
                nt.style.whiteSpace = WhiteSpace.Normal;
                voCol.Add(nt);
            }
            else
            {
                var list = U.Col(0, "card").Pad(2, 14);
                list.Elev(Elevation.E1);
                for (int i = 0; i < OrganoSpeech.Voices.Count; i++)
                {
                    var v = OrganoSpeech.Voices[i];
                    bool on = OrganoSpeech.CurrentVoice == v.id;
                    var r = U.Row(12).MinH(52).Pad(6, 0);
                    if (i < OrganoSpeech.Voices.Count - 1) { r.style.borderBottomWidth = 1; r.style.borderBottomColor = U.C(U.Line); }
                    var dot = U.Div().Size(22, 22).Radius(11).Border(2, on ? U.Teal : "#CFCCDD").Center();
                    if (on) dot.Add(U.Div().Size(10, 10).Radius(5).Bg(U.Teal));
                    r.Add(dot);
                    var lbl = U.Text(v.label, "t-body").Grow();
                    if (on) lbl.Bold();
                    r.Add(lbl);
                    var tryIt = U.Row(4).Pad(6, 12).Radius(999).Bg(U.TealSoft);
                    tryIt.Add(U.Icon("set_sound", 14).Tinted(U.C(U.TealInk)));
                    tryIt.Add(U.Text("Coba").FontSize(12).Bold().Fg(U.TealInk));
                    r.Add(tryIt);
                    string id = v.id;
                    r.Tap(() =>
                    {
                        OrganoSpeech.SetVoice(id);
                        OrganoAudio.SpeakText("Halo! Aku akan membacakan penjelasan organ tubuh untukmu.", true);
                        Refresh();
                    });
                    list.Add(r);
                }
                voCol.Add(list);
            }
            c.Add(voCol);

            // Switch
            var sw = U.Col(0, "card", "card-lg").Pad(4, 14);
            sw.Elev(Elevation.E1);
            sw.Add(SwitchRow("Sorot kalimat yang dibacakan", null, m_Captions, v => { m_Captions = v; Refresh(); }, false));
            sw.Add(SwitchRow("Kontras tinggi", null, m_Contrast, v => { m_Contrast = v; Refresh(); }, false));
            sw.Add(SwitchRow("Kurangi animasi", null, m_Calm, v => { m_Calm = v; Refresh(); }, true));
            c.Add(sw);

            var bottom = U.Div().Pad(12, 20, 20, 20);
            bottom.Add(U.Button("Simpan", () =>
            {
                P.textSize = m_Size; P.voiceSpeed = m_Speed;
                P.captions = m_Captions; P.highContrast = m_Contrast; P.reduceMotion = m_Calm;
                P.Save();
                OrganoAudio.SetVoiceSpeed(m_Speed);
                App.ApplyFontScale();
                m_Loaded = false;
                App.Toast("Pengaturan disimpan");
                App.Back();
            }, "btn-primary"));
            Root.Add(bottom);
        }

        /// <summary>Keluar dari Aksesibilitas (kembali, Simpan, tombol kembali HP): contoh suara pembaca langsung berhenti.</summary>
        public override void Close()
        {
            OrganoAudio.StopVoice();
            OrganoSpeech.Stop();
        }

        static VisualElement SwitchRow(string title, string hint, bool on, System.Action<bool> changed, bool last)
        {
            var r = U.Row(12).MinH(56).Pad(10, 0);
            if (!last) { r.style.borderBottomWidth = 1; r.style.borderBottomColor = U.C(U.Line); }
            var t = U.Col(2).Grow();
            t.Add(U.Text(title, "t-body").Bold());
            if (!string.IsNullOrEmpty(hint)) t.Add(U.Text(hint, "t-caption"));
            r.Add(t);
            r.Add(Parts.Switch(on, changed, title));
            return r;
        }
    }
}
