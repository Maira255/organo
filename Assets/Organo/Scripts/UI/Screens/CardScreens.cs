using System;
using System.Collections.Generic;
using System.Linq;
using Organo.Data;
using Organo.UI;
using UnityEngine;
using UnityEngine.UIElements;
using U = Organo.UI.UI;

namespace Organo.Screens
{
    /// <summary>Tampilan kartu koleksi: gambar kartu asli (sama dengan kartu cetak), sisi belakang, dan kartu terkunci.</summary>
    public static class CardArt
    {
        /// <summary>Tinggi / lebar kartu (6,3 x 8,8 cm).</summary>
        public const float Aspect = 1090f / 780f;

        public static string SystemIcon(OrganSystem s)
        {
            switch (s)
            {
                case OrganSystem.Pencernaan: return "cat_cerna";
                case OrganSystem.Pernapasan: return "cat_napas";
                case OrganSystem.PeredaranDarah: return "cat_darah";
                case OrganSystem.Ekskresi: return "cat_ekskresi";
                case OrganSystem.Indera: return "cat_indera";
                default: return "cat_saraf";
            }
        }

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        public static string Number(OrganInfo o) => "No. " + o.cardNumber.ToString("00");

        /// <summary>Sisi depan kartu yang sudah dimiliki (gambar kartu cetak).</summary>
        public static VisualElement Front(OrganInfo o, float w, float radius)
        {
            var card = U.Div().Size(w, w * Aspect).Radius(radius).Bg("#FFFFFF");
            card.style.overflow = Overflow.Hidden;
            if (o.cardImage != null)
            {
                card.style.backgroundImage = new StyleBackground(o.cardImage);
                return card;
            }
            // cadangan bila gambar kartu belum dipasang
            var (fg, soft) = OrganDefaults.SystemColors(o.system);
            card.Bg(soft).Border(3, fg).Center();
            card.Add(Parts.OrganBadge(o, w * 0.6f));
            card.Add(U.Text(o.displayName).FontSize(Mathf.Max(11f, w * 0.08f)).Bold().Fg(fg).TextCenter());
            return card;
        }

        /// <summary>Kartu yang belum dimiliki: abu-abu, garis putus-putus, siluet organ & gembok.</summary>
        public static VisualElement Locked(OrganInfo o, float w, float radius)
        {
            bool big = w > 160f;
            var card = U.Col(big ? 10 : 4).Size(w, w * Aspect).Radius(radius).Bg("#F1F0F6").Center();
            card.Add(new DashedOutline(U.C("#CFCCDD"), 2f, radius));
            if (w >= 80f) card.Add(U.Text(Number(o)).FontSize(big ? 13 : 10).Bold().Fg(U.Faint).Abs(big ? 14 : 8, big ? 12 : 6));
            var badge = Parts.OrganBadge(o, w * 0.56f, 0.62f, true);
            badge.Add(Parts.LockBadge(big ? 36f : Mathf.Clamp(w * 0.2f, 12f, 22f)).Abs(null, null, 0, 0));
            card.Add(badge);
            if (big)
            {
                card.Add(U.Text(o.displayName).FontSize(18).Bold().Fg(U.Muted).TextCenter());
                card.Add(U.Text("Belum dimiliki", "t-caption").TextCenter());
            }
            return card;
        }

        /// <summary>Sisi belakang kartu: data organ seperti kartu koleksi sungguhan.</summary>
        public static VisualElement Back(OrganInfo o, float w, float radius, string fact, string date)
        {
            var (fg, soft) = OrganDefaults.SystemColors(o.system);
            var card = U.Col(7).Size(w, w * Aspect).Radius(radius).Bg("#FFFFFF").Border(3, fg).Pad(14, 16);
            card.style.overflow = Overflow.Hidden;

            var top = U.Row(6).Justify(Justify.SpaceBetween);
            var sys = U.Row(4).Pad(3, 10, 3, 6).Radius(999).Bg(soft);
            sys.Add(U.Icon(SystemIcon(o.system), 16));
            sys.Add(U.Text(OrganDefaults.SystemName(o.system)).FontSize(11).Bold().Fg(fg));
            top.Add(sys);
            top.Add(U.Text(Number(o)).FontSize(11).Bold().Fg(U.Muted));
            card.Add(top);

            var head = U.Row(10);
            head.Add(Parts.OrganBadge(o, 56).NoShrink());
            var ht = U.Col(0).Grow();
            ht.Add(U.Text(o.displayName, "t-h3"));
            ht.Add(U.Text("Letak: " + o.WhereLabel, "t-caption"));
            head.Add(ht);
            card.Add(head);

            card.Add(U.Text("FUNGSI", "t-overline").Fg(fg));
            card.Add(Wrap(U.Text(o.function, "t-small").Fg(U.Ink)));

            if (!string.IsNullOrEmpty(fact))
            {
                var box = U.Col(2).Bg(soft).Radius(10).Pad(8, 10);
                box.Add(U.Text("TAHUKAH KAMU?", "t-overline").Fg(fg));
                box.Add(Wrap(U.Text(fact, "t-caption").Fg(U.Ink)));
                card.Add(box);
            }
            card.Add(U.Flex());
            card.Add(U.Text("Didapat: " + date, "t-tiny").Fg(U.Muted));
            return card;
        }

        static Label Wrap(Label l) { l.style.whiteSpace = WhiteSpace.Normal; return l; }
    }

    // ==================================================================== Koleksi kartu

    /// <summary>
    /// Album kartu organ: dikelompokkan per sistem (dengan progres tiap set), gambar kartu sama dengan kartu cetak.
    /// Ketuk kartu untuk membuka <see cref="CardViewerScreen"/>.
    /// Argumen: int = filter (0 semua, 1 dimiliki, 2 belum); string = id kartu yang disorot (mis. baru di-scan).
    /// </summary>
    public class CardCollectionScreen : AppScreen
    {
        int m_Filter;
        static float s_Scroll;
        static bool s_Restore;

        const float TileW = 104f, TileGap = 19f;

        protected override void Build(object arg)
        {
            if (arg is int f) m_Filter = f;
            string focus = arg as string;
            bool restore = s_Restore && focus == null;
            s_Restore = false;

            var help = U.IconButton("help", () => CardSource.Show(App), 22f, "Kartunya dari mana?");
            if (help.Q<SvgImage>() is SvgImage hi) hi.Tint = U.C(U.Ink);
            Root.Add(Parts.TopBar("Koleksi Kartu", App.Back, help));
            var scroll = U.Scroll();
            scroll.contentContainer.style.paddingTop = 8;
            Root.Add(scroll);
            var c = U.Col(18);
            scroll.Add(c);

            int got = Db.Ordered.Count(o => P.HasCard(o.id)), total = Db.Count;
            var sum = U.Row(14).Pad(16).Radius(18).Gradient(U.Teal, U.Purple, 135);
            sum.Elev(Elevation.E3);
            sum.Add(new ProgressRing(got / (float)Mathf.Max(1, total), 72, Color.white, U.C("#FFFFFF", 0.3f), got.ToString(), "dari " + total, Color.white));
            var st = U.Col(6).Grow();
            st.style.justifyContent = Justify.Center;
            st.Add(U.Text(got == total ? "Koleksimu lengkap!" : "Kartu terkumpul").FontSize(16).Bold().Fg("#FFFFFF"));
            if (got < total)
            {
                // tombol langsung ke scan (cara mendapat kartu ada di tombol "?" di kanan atas)
                var scanBtn = U.Row(4).AlignSelf(Align.FlexStart).Pad(5, 12).Radius(999).Bg(U.C("#FFFFFF", 0.2f)).Border(1, U.C("#FFFFFF", 0.6f));
                scanBtn.Add(U.Text("Scan kartu").FontSize(11).Bold().Fg("#FFFFFF"));
                scanBtn.Tap(() => App.GoTab(NavTab.Scan));
                st.Add(scanBtn);
            }
            sum.Add(st);
            c.Add(sum);

            var chips = U.Row();
            string[] labels = { "Semua (" + total + ")", "Dimiliki (" + got + ")", "Belum (" + (total - got) + ")" };
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

            VisualElement focusTile = null;
            int shown = 0;
            foreach (OrganSystem sys in Enum.GetValues(typeof(OrganSystem)))
            {
                var all = Db.BySystem(sys).ToList();
                if (all.Count == 0) continue;
                var list = all.Where(o => m_Filter == 0 || (m_Filter == 1) == P.HasCard(o.id)).ToList();
                if (list.Count == 0) continue;
                shown += list.Count;
                int have = all.Count(o => P.HasCard(o.id));
                var (fg, soft) = OrganDefaults.SystemColors(sys);

                var sec = U.Col(10);
                var head = U.Row(10);
                head.Add(U.Div().Size(32, 32).Radius(16).Bg(soft).Center().Kids(U.Icon(CardArt.SystemIcon(sys), 20)));
                var ht = U.Col(4).Grow();
                var titleRow = U.Row(8).Justify(Justify.SpaceBetween);
                titleRow.Add(U.Text("Sistem " + OrganDefaults.SystemName(sys), "t-section"));
                if (have == all.Count)
                {
                    var done = U.Row(4).Pad(2, 8).Radius(999).Bg(fg);
                    done.Add(U.Icon("star", 11).Tinted(Color.white));
                    done.Add(U.Text("Lengkap").FontSize(11).Bold().Fg("#FFFFFF"));
                    titleRow.Add(done);
                }
                else titleRow.Add(U.Text(have + "/" + all.Count).FontSize(13).Bold().Fg(fg));
                ht.Add(titleRow);
                ht.Add(U.Progress(have / (float)all.Count, CardArt.Hex(fg), CardArt.Hex(soft), 5));
                head.Add(ht);
                sec.Add(head);

                for (int i = 0; i < list.Count; i += 3)
                {
                    var row = U.Row(TileGap).Align(Align.FlexStart);
                    for (int j = i; j < Mathf.Min(i + 3, list.Count); j++)
                    {
                        var tile = Tile(list[j], scroll);
                        if (list[j].id == focus) focusTile = tile;
                        row.Add(tile);
                    }
                    sec.Add(row);
                }
                c.Add(sec);
            }

            if (shown == 0) c.Add(EmptyState());
            c.Add(U.Space(12));

            if (focusTile != null)
            {
                var ring = U.Div().Abs(-5, -5, -5, -5).Radius(15).Border(3, U.Pink);
                ring.pickingMode = PickingMode.Ignore;
                focusTile[0].Add(ring);
                scroll.schedule.Execute(() => scroll.ScrollTo(focusTile)).ExecuteLater(120);
            }
            else if (restore)
            {
                float y = s_Scroll;
                scroll.schedule.Execute(() => scroll.scrollOffset = new Vector2(0, y)).ExecuteLater(60);
            }
        }

        VisualElement Tile(OrganInfo o, ScrollView scroll)
        {
            bool own = P.HasCard(o.id);
            var tile = U.Col(6).W(TileW).Align(Align.Center);
            var holder = U.Div().Size(TileW, TileW * CardArt.Aspect);
            var art = own ? CardArt.Front(o, TileW, 10) : CardArt.Locked(o, TileW, 10);
            holder.Add(art);
            if (own) art.Elev(Elevation.E2);
            if (own && P.newestCard == o.id)
            {
                var pill = U.Pill("BARU", U.Pink, "#FFFFFF").Abs(-4, -6);
                pill.style.paddingTop = 2; pill.style.paddingBottom = 2; pill.style.paddingLeft = 8; pill.style.paddingRight = 8;
                pill.pickingMode = PickingMode.Ignore;
                holder.Add(pill);
            }
            if (own && P.favorites.Contains(o.id))
            {
                var mark = U.Div().Size(22, 22).Radius(11).Abs(null, -6, -6).Bg(U.Pink).Border(2, "#FFFFFF").Center();
                mark.pickingMode = PickingMode.Ignore;
                mark.Add(U.Icon("bookmark", 11).Tinted(Color.white));
                holder.Add(mark);
            }
            tile.Add(holder);
            tile.Add(U.Text(o.displayName).FontSize(12).Bold().Fg(own ? U.C(U.Ink) : U.C(U.Muted)).TextCenter());
            if (own) tile.Add(U.Text(P.CardDate(o.id)).FontSize(10).Fg(U.C(U.Muted)).TextCenter());
            tile.tooltip = own ? "Lihat kartu " + o.displayName : "Kartu " + o.displayName + " belum dimiliki";
            string id = o.id;
            tile.Tap(() =>
            {
                s_Scroll = scroll.scrollOffset.y;
                s_Restore = true;
                App.Go<CardViewerScreen>(id + "|" + m_Filter);
            });
            return tile;
        }

        VisualElement EmptyState()
        {
            var box = U.Col(10).Align(Align.Center).Pad(20, 16).Radius(18).Bg("#FFFFFF").Border(1, U.Line);
            box.Add(U.Icon("mascot_scan", 84));
            box.Add(U.Text(m_Filter == 1 ? "Belum ada kartu" : "Semua kartu sudah kamu punya!", "t-title").TextCenter());
            if (m_Filter == 1)
            {
                var row = U.Row(10).AlignSelf(Align.Stretch);
                row.Add(U.Button("Ke Library", () => App.GoTab(NavTab.Library), "btn-outline").Grow());
                row.Add(U.Button("Scan Kartu", () => App.GoTab(NavTab.Scan), "btn-primary").Grow());
                box.Add(row);
            }
            return box;
        }
    }

    // ==================================================================== Lihat kartu

    /// <summary>
    /// Satu kartu besar: ketuk untuk membalik (depan = gambar kartu, belakang = data organ), geser kiri/kanan untuk
    /// kartu lain, lalu buka 3D, dengarkan, Info Lengkap, atau scan. Argumen: "id" atau "id|filter".
    /// </summary>
    public class CardViewerScreen : AppScreen
    {
        const float CardW = 248f;

        readonly List<OrganInfo> m_List = new List<OrganInfo>();
        int m_Index;
        bool m_Back;
        string m_Fact;
        VisualElement m_Holder, m_Info, m_Dots, m_Fav, m_HintRow;
        Label m_Hint;

        /// <summary>Kartu yang sedang dilihat (dipakai uji otomatis).</summary>
        public OrganInfo Current => m_Index >= 0 && m_Index < m_List.Count ? m_List[m_Index] : null;
        public bool ShowingBack => m_Back;

        protected override void Build(object arg)
        {
            string id = null;
            int filter = 0;
            if (arg is string s)
            {
                var parts = s.Split('|');
                id = parts[0];
                if (parts.Length > 1) int.TryParse(parts[1], out filter);
            }
            m_List.Clear();
            m_List.AddRange(Db.Ordered.Where(o => filter == 0 || (filter == 1) == P.HasCard(o.id)));
            if (m_List.Count == 0) m_List.AddRange(Db.Ordered);
            m_Index = Mathf.Max(0, m_List.FindIndex(o => o.id == id));
            m_Back = false;

            m_Fav = U.IconButton("bookmark", ToggleFavorite, 22f, "Simpan ke Favoritku");
            Root.Add(Parts.TopBar("Kartu Organo", App.Back, m_Fav));

            var scroll = U.Scroll();
            Root.Add(scroll);
            var c = U.Col(12).Align(Align.Center);
            scroll.Add(c);


            // panggung kartu: wadah yang digeser/dibalik (bayangan ikut bergerak)
            var stage = U.Div().Size(CardW + 40, CardW * CardArt.Aspect + 16).Center();
            m_Holder = U.Div().Size(CardW, CardW * CardArt.Aspect);
            m_Holder.name = "card-holder";
            m_Holder.Tap(Flip);
            m_Holder.AddManipulator(new CardSwipe(Step));
            stage.Add(m_Holder);
            c.Add(stage);

            var hintRow = U.Row(6).AlignSelf(Align.Center);
            hintRow.Add(U.Icon("rotate", 14).Tinted(U.C(U.Muted)));
            m_Hint = U.Text("", "t-caption");
            hintRow.Add(m_Hint);
            c.Add(hintRow);
            m_HintRow = hintRow;

            var nav = U.Row(12).AlignSelf(Align.Center);
            nav.Add(NavButton(true));
            m_Dots = U.Row(5);
            nav.Add(m_Dots);
            nav.Add(NavButton(false));
            c.Add(nav);

            m_Info = U.Col(12).AlignSelf(Align.Stretch);
            m_Info.style.marginTop = 4;
            c.Add(m_Info);
            c.Add(U.Space(16));

            Render(0);
        }

        VisualElement NavButton(bool prev)
        {
            var b = U.Div().Size(40, 40).Radius(20).Bg("#FFFFFF").Border(1, U.Line).Center();
            var ic = U.Icon("chevron_right", 18);
            if (prev) ic.style.rotate = new Rotate(180);
            b.Add(ic);
            b.tooltip = prev ? "Kartu sebelumnya" : "Kartu berikutnya";
            b.Tap(() => { if (!Step(prev ? -1 : 1)) Bounce(prev ? -1 : 1); });
            return b;
        }

        /// <summary>Pindah kartu. False bila sudah di ujung daftar.</summary>
        bool Step(int dir)
        {
            int next = m_Index + dir;
            if (next < 0 || next >= m_List.Count) return false;
            m_Index = next;
            m_Back = false;
            OrganoAudio.StopVoice();
            Render(dir);
            return true;
        }

        void Render(int fromDir)
        {
            var o = Current;
            if (o == null) return;
            bool own = P.HasCard(o.id);
            m_Fact = PickFact(o);
            // petunjuk membalik hanya untuk kartu yang dimiliki (kartu terkunci sudah jelas dari gambarnya)
            m_Hint.text = m_Back ? "Ketuk untuk melihat gambar kartu" : "Ketuk kartu untuk membalik";
            m_HintRow.Visible(own);
            m_Fav.style.display = own ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateFav();
            RenderFace();
            RenderDots();
            RenderInfo(o, own);

            // kartu baru masuk dari sisi geser
            m_Holder.style.transitionDuration = new List<TimeValue> { 0f };
            m_Holder.style.rotate = new Rotate(0);
            if (fromDir == 0 || P.reduceMotion) { m_Holder.style.translate = new Translate(0, 0); return; }
            m_Holder.style.translate = new Translate(fromDir > 0 ? 300 : -300, 0);
            m_Holder.schedule.Execute(() =>
            {
                m_Holder.style.transitionProperty = new List<StylePropertyName> { "translate" };
                m_Holder.style.transitionDuration = new List<TimeValue> { 0.22f };
                m_Holder.style.transitionTimingFunction = new List<EasingFunction> { EasingMode.EaseOutCubic };
                m_Holder.style.translate = new Translate(0, 0);
            }).ExecuteLater(16);
        }

        void RenderFace()
        {
            var o = Current;
            m_Holder.Clear();
            bool own = P.HasCard(o.id);
            VisualElement face = !own ? CardArt.Locked(o, CardW, 16)
                : m_Back ? CardArt.Back(o, CardW, 16, m_Fact, P.CardDate(o.id, true))
                : CardArt.Front(o, CardW, 16);
            m_Holder.Add(face);
            face.Elev(own ? Elevation.E3 : Elevation.E1);
        }

        void RenderDots()
        {
            m_Dots.Clear();
            for (int i = 0; i < m_List.Count; i++)
            {
                var o = m_List[i];
                bool on = i == m_Index, own = P.HasCard(o.id);
                var (fg, _) = OrganDefaults.SystemColors(o.system);
                var d = U.Div().Size(on ? 18 : 8, 8).Radius(4);
                if (own) d.Bg(on ? fg : U.C(CardArt.Hex(fg), 0.45f));
                else d.Bg(on ? U.C(U.Muted) : U.C("#DCDAE5"));
                m_Dots.Add(d);
            }
        }

        void RenderInfo(OrganInfo o, bool own)
        {
            m_Info.Clear();
            var (fg, soft) = OrganDefaults.SystemColors(o.system);
            var title = U.Row(10).Justify(Justify.SpaceBetween);
            var tl = U.Col(2).Grow();
            tl.Add(U.Text(o.displayName, "t-h2").Fg(own ? U.C(U.Ink) : U.C(U.Muted)));
            string when = own ? P.CardDate(o.id, true) : null;
            if (when == "Hari ini" || when == "Kemarin" || when == "Baru saja") when = when.ToLowerInvariant();
            tl.Add(U.Text(own ? "Didapat " + when : o.SystemLabel, "t-caption"));
            title.Add(tl);
            if (own) title.Add(U.Pill(OrganDefaults.SystemName(o.system), CardArt.Hex(soft), CardArt.Hex(fg)));
            m_Info.Add(title);

            if (own)
            {
                var see = U.Button("Lihat 3D", () => App.Go<OrganDetailScreen>(o.id), "btn-primary");
                var ic = U.Icon("qt_3d", 20).Tinted(Color.white);
                ic.style.marginRight = 8;
                see.Insert(0, ic);
                m_Info.Add(see);
                var row = U.Row(8);
                // ketuk lagi saat sedang dibacakan = berhenti; tidak ada tombol kalau suara pembaca dimatikan
                if (P.narration)
                    row.Add(U.Button("Dengarkan", () => { if (OrganoAudio.VoicePlaying || OrganoAudio.SpeechPlaying) OrganoAudio.StopVoice(); else Speak(o); }, "btn-outline", "btn-small").Grow());
                row.Add(U.Button("Info Lengkap", () => App.Go<OrganInfoScreen>(o.id), "btn-outline", "btn-small").Grow());
                if (o.Printable) row.Add(U.Button("Scan AR", () => App.Go<ScanScreen>(o.id), "btn-outline", "btn-small").Grow());
                m_Info.Add(row);
            }
            else if (!o.Printable)
            {
                // kartu organ reproduksi: tidak dicetak, didapat dari kuisnya (gender lain terbuka setelah kuis milik sendiri)
                bool locked = Parts.Locked(o);
                var box = U.Col(8).Pad(12, 14).Radius(14).Bg("#F6E5F5");
                var t = U.Text(locked ? ReproAccess.LockReason(o.id, P) : "Didapat dari " + ReproAccess.QuizName(o.id) + ".", "t-caption").Fg(U.Ink);
                t.style.whiteSpace = WhiteSpace.Normal;
                box.Add(t);
                m_Info.Add(box);
                var row = U.Row(10);
                row.Add(U.Button("Pelajari dulu", () => Parts.OpenOrgan(App, o), "btn-outline").Grow());
                row.Add(U.Button("Mulai Kuis", () =>
                {
                    if (Parts.Locked(o)) { Parts.ShowReproLock(App, o); return; }
                    QuizSession.StartTopic(ReproAccess.Topic(o.id));
                    QuizSession.OpenCurrent(App);
                }, "btn-primary").Grow());
                m_Info.Add(row);
            }
            else
            {
                var row = U.Row(10);
                row.Add(U.Button("Pelajari dulu", () => App.Go<OrganDetailScreen>(o.id), "btn-outline").Grow());
                row.Add(U.Button("Scan Kartu", () => App.Go<ScanScreen>(o.id), "btn-primary").Grow());
                m_Info.Add(row);
                m_Info.Add(U.Text("Kartunya dari mana?").FontSize(13).Bold().Fg(U.TealDark).AlignSelf(Align.Center).Pad(6, 12).Tap(() => CardSource.Show(App)));
            }
        }

        string PickFact(OrganInfo o)
        {
            var facts = OrganNarration.Facts(o);
            return facts.Count == 0 ? null : facts[UnityEngine.Random.Range(0, facts.Count)];
        }

        void Flip()
        {
            var o = Current;
            if (o == null) return;
            if (!P.HasCard(o.id)) { Bounce(0); App.Toast(o.Printable ? "Kartu " + o.displayName + " belum kamu punya. Scan kartunya dulu, ya!" : "Kartu " + o.displayName + " didapat dari " + ReproAccess.QuizName(o.id) + "."); return; }
            m_Back = !m_Back;
            m_Hint.text = (m_Back ? "Ketuk untuk melihat gambar kartu" : "Ketuk kartu untuk membalik") + " · geser untuk kartu lain";
            if (P.reduceMotion) { RenderFace(); return; }
            m_Holder.style.transitionProperty = new List<StylePropertyName> { "scale" };
            m_Holder.style.transitionDuration = new List<TimeValue> { 0.12f };
            m_Holder.style.transitionTimingFunction = new List<EasingFunction> { EasingMode.EaseIn };
            m_Holder.style.scale = new Scale(new Vector3(0.02f, 1f, 1f));
            m_Holder.schedule.Execute(() =>
            {
                RenderFace();
                m_Holder.style.transitionTimingFunction = new List<EasingFunction> { EasingMode.EaseOut };
                m_Holder.style.scale = new Scale(Vector3.one);
            }).ExecuteLater(120);
        }

        /// <summary>Goyang kecil: sudah di ujung daftar / kartu terkunci.</summary>
        void Bounce(int dir)
        {
            if (P.reduceMotion) return;
            m_Holder.style.transitionProperty = new List<StylePropertyName> { "translate" };
            m_Holder.style.transitionDuration = new List<TimeValue> { 0.1f };
            m_Holder.style.translate = new Translate(dir == 0 ? 10 : -dir * 18, 0);
            m_Holder.schedule.Execute(() => m_Holder.style.translate = new Translate(0, 0)).ExecuteLater(110);
        }

        void ToggleFavorite()
        {
            var o = Current;
            if (o == null || !P.HasCard(o.id)) return;
            P.ToggleFavorite(o.id);
            bool now = P.favorites.Contains(o.id);
            UpdateFav();
            App.Toast(now ? o.displayName + " disimpan di Favoritku" : o.displayName + " dihapus dari Favoritku");
        }

        void UpdateFav()
        {
            var o = Current;
            bool fav = o != null && P.favorites.Contains(o.id);
            var ic = m_Fav.Q<SvgImage>();
            if (ic != null) ic.Tint = fav ? U.C(U.Pink) : U.C(U.Muted);
            m_Fav.Bg(fav ? U.PinkSoft : "#00000000");
            m_Fav.Radius(20);
        }

        void Speak(OrganInfo o)
        {
            string text = OrganNarration.CardText(o) + (m_Fact != null ? " " + OrganNarration.FactText(m_Fact) : "");
            // rekaman kartu (nama + tugas) lalu fakta yang sedang tampil; kalau belum ada rekaman, suara HP
            bool ok = (o.cardNarration != null && OrganoAudio.PlayVoiceSequence(o.cardNarration, OrganNarration.FactClip(o, m_Fact)))
                      || OrganoAudio.SpeakText(text);
            if (!ok)
                App.Toast(OrganoAudio.Enabled ? "Suara narasi berbunyi di HP Android/iOS yang punya suara Bahasa Indonesia." : "Suara sedang dimatikan di Profil.", 3f);
        }

        public override void Close() => OrganoAudio.StopVoice();

        /// <summary>Geser kartu ke kiri/kanan (kartu ikut jari, lalu terbang keluar).</summary>
        class CardSwipe : TrackedGesture
        {
            readonly Func<int, bool> m_Go;
            public CardSwipe(Func<int, bool> go) { m_Go = go; }

            protected override int Decide(Vector2 d)
            {
                if (Mathf.Abs(d.x) < 10f && Mathf.Abs(d.y) < 10f) return 0;
                return Mathf.Abs(d.x) > Mathf.Abs(d.y) ? 1 : -1;
            }

            protected override void OnDragStart()
            {
                target.style.transitionDuration = new List<TimeValue> { 0f };
            }

            protected override void OnDrag(Vector2 total, Vector2 delta)
            {
                target.style.translate = new Translate(total.x, 0);
                target.style.rotate = new Rotate(total.x * 0.04f);
            }

            protected override void OnDragEnd(Vector2 total, Vector2 velocity)
            {
                int dir = total.x < -70f || velocity.x < -650f ? 1 : total.x > 70f || velocity.x > 650f ? -1 : 0;
                var t = target;
                t.style.transitionProperty = new List<StylePropertyName> { "translate", "rotate" };
                t.style.transitionDuration = new List<TimeValue> { 0.16f, 0.16f };
                if (dir != 0 && m_Go(dir)) return; // kartu berikutnya masuk dari sisi lain
                t.style.translate = new Translate(0, 0);
                t.style.rotate = new Rotate(0);
            }
        }
    }
}
