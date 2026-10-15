using System;
using System.Collections.Generic;
using System.Linq;
using Organo.Screens;
using Organo.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Organo.EditorTools
{
    /// <summary>
    /// Uji tata letak di beberapa ukuran layar. Desain Organo dibuat untuk iPhone (390x844 dengan area aman notch & home bar);
    /// di Android area aman sering 0 dan tinggi layar berbeda. Setiap layar penting dibuka di tiap perangkat, difoto
    /// (Library/OrganoPreview/ui_L_*.png), lalu dicek otomatis:
    ///  - tombol di luar area gulir utuh di layar dan tidak menempel ke tepi atas/bawah,
    ///  - teks tidak terpotong (tinggi/lebar yang dibutuhkan muat) dan tidak keluar dari kotak berlatarnya.
    /// Jalankan: Unity -executeMethod Organo.EditorTools.OrganoSnapshots.RunLayoutAndQuit (tanpa -batchmode).
    /// </summary>
    public static partial class OrganoSnapshots
    {
        const string LayoutKey = "organo.snap.layout";

        public static void RunLayoutAndQuit() { SessionState.SetBool(LayoutKey, true); Start(true); }

        struct Device
        {
            public string name;
            public int w, h;
            public Vector2 insets; // area aman atas/bawah (unit UI)
        }

        static readonly Device[] Devices =
        {
            new Device { name = "iphone", w = 390, h = 844, insets = new Vector2(47, 34) },   // iPhone 13/14/15
            new Device { name = "android", w = 390, h = 866, insets = Vector2.zero },          // 1080x2400 layar penuh tanpa notch
            new Device { name = "kecil", w = 390, h = 693, insets = Vector2.zero },            // 720x1280 (16:9)
        };

        static List<(string, Action, float)> LayoutSteps()
        {
            var a = App;
            var list = new List<(string, Action, float)>();
            foreach (var d in Devices)
            {
                var dev = d;
                string p = "L_" + dev.name + "_";
                list.Add((p + "00_perangkat", () => SetDevice(dev), 0.8f));
                void Screen(string n, Action go, float wait = 1.3f)
                {
                    list.Add((p + n, go, wait));
                    list.Add((p + n + "_cek", () => CheckLayout(dev.name + " · " + n), 0.05f));
                }
                Screen("01_splash", () => a.Go<SplashScreen>(null, false), 1.0f);
                Screen("02_onboarding", () => a.Go<OnboardingScreen>(0, false));
                Screen("07_kenalan", () => a.Go<GenderScreen>(Organo.Data.Gender.Boy, false));
                Screen("08_beranda", () =>
                {
                    a.Progress.onboarded = true; a.Progress.gender = Organo.Data.Gender.Boy;
                    a.GoTab(NavTab.Beranda);
                });
                Screen("09_library", () => a.GoTab(NavTab.Library));
                Screen("09b_library_panggul", () => a.Go<LibraryBodyScreen>(Organo.Data.BodyRegion.Panggul, false), 1.4f);
                Screen("09c_tubuh_usus", () => a.Go<OrganDetailScreen>("usushalus|tubuh", false), 2.4f);
                Screen("09d_tubuh_ususbesar_perempuan", () =>
                {
                    a.Progress.gender = Organo.Data.Gender.Girl;
                    a.BodyGender = Organo.Data.Gender.Girl;
                    a.Go<OrganDetailScreen>("ususbesar|tubuh", false);
                }, 2.4f);
                list.Add((p + "09e_kembali_laki", () => { a.Progress.gender = Organo.Data.Gender.Boy; a.BodyGender = Organo.Data.Gender.Boy; }, 0.1f));
                Screen("10_detail_mulut", () => a.Go<OrganDetailScreen>("mulut", false), 2.0f);
                Screen("11_detail_dengarkan", () =>
                {
                    (a.Current as OrganDetailScreen)?.ToggleVoice(); // halaman menggulir sendiri ke paragraf yang dibacakan
                }, 1.8f);
                Screen("12_detail_bawah", () => Scroll(5000), 0.9f);
                Screen("13_info_mulut", () => a.Go<OrganInfoScreen>("mulut", false), 1.6f);
                Screen("13b_rangka", () => a.Go<OrganDetailScreen>("rangka|organ", false), 2.0f);
                Screen("14_kuis_hub", () => a.GoTab(NavTab.Quiz));
                Screen("14b_kuis_proses", () => a.Go<QuizHubScreen>(1, false));
                Screen("15_kuis_label", () => { QuizSession.StartProcess("cerna"); a.Go<QuizPlayScreen>(QuizSession.Current, false); }, 1.6f);
                Screen("16_kuis_pilihan", () =>
                {
                    QuizSession.StartOrgan(Organo.Data.ReproAccess.Boy);
                    var s = QuizSession.Current;
                    s.index = s.questions.Count - 1; // soal terakhir: soal keamanan tubuh yang panjang
                    a.Go<QuizPlayScreen>(s, false);
                }, 1.6f);
                Screen("17_kuis_dijawab", () => AnswerCurrentChoice(), 1.4f);
                Screen("18_scan", () => { a.GoTab(NavTab.Scan); a.ar?.End(); }, 1.4f);
                Screen("19_misi_scan", () => { QuizSession.StartOrgan("mulut"); a.Go<QuizScanScreen>(null, false); a.ar?.End(); }, 1.4f);
                Screen("20_scan_hasil_baru", () =>
                {
                    a.Progress.cards.Remove("jantung");
                    a.Go<ScanResultScreen>("jantung", false);
                    a.ar?.End();
                }, 1.0f);
                Screen("21_koleksi", () => a.Go<CardCollectionScreen>(null, false));
                Screen("22_kartu", () => a.Go<CardViewerScreen>("mulut", false));
                Screen("23_profil", () => a.GoTab(NavTab.Profil));
                Screen("24_aksesibilitas", () => a.Go<AccessibilityScreen>(null, false));
            }
            list.Add(("L_99_selesai", () => SetDevice(Devices[0], false), 0.4f));
            return list;
        }

        /// <summary>Ganti ukuran layar render & area aman tiruan (perangkat).</summary>
        static void SetDevice(Device d, bool insets = true)
        {
            var doc = App.GetComponent<UIDocument>();
            var rt = new RenderTexture(d.w * 2, d.h * 2, 24, RenderTextureFormat.ARGB32);
            doc.panelSettings.targetTexture = rt;
            if (s_RT != null) { s_RT.Release(); UnityEngine.Object.DestroyImmediate(s_RT); }
            s_RT = rt;
            OrganoApp.DebugInsets = insets ? d.insets : (Vector2?)null;
            Debug.Log("[Organo] Uji tata letak: " + d.name + " " + d.w + "x" + d.h + " area aman " + d.insets);
        }

        /// <summary>Kuis pilihan ganda: ketuk jawaban benar lalu "Periksa Jawaban" (seperti anak).</summary>
        static void AnswerCurrentChoice()
        {
            var s = QuizSession.Current;
            var root = App.Current?.Root;
            if (s == null || root == null) return;
            var q = s.Q;
            Label opt = null;
            root.Query<Label>().ForEach(l => { if (opt == null && l.text == q.options[q.correct]) opt = l; });
            if (opt == null) return;
            Tap(opt.parent.worldBound.center);
            Later(() =>
            {
                Label check = null;
                App.Current.Root.Query<Label>().ForEach(l => { if (l.text == "Periksa Jawaban") check = l; });
                if (check != null) Tap(check.parent.worldBound.center);
            }, 250);
        }

        static void CheckLayout(string label)
        {
            var cur = App.Current?.Root;
            if (cur == null || cur.panel == null) { Report(label + ": layar terbuka", false); return; }
            var tree = cur.panel.visualTree;
            float W = tree.layout.width, H = tree.layout.height;
            var issues = new List<string>();

            cur.Query(className: "btn").ForEach(b =>
            {
                if (!Shown(b) || Under<ScrollView>(b)) return;
                var r = b.worldBound;
                if (r.yMax > H - 6f || r.yMin < 6f || r.xMin < -0.5f || r.xMax > W + 0.5f)
                    issues.Add("tombol \"" + TextOf(b) + "\" menempel/keluar tepi (" + r.yMin.ToString("0") + "–" + r.yMax.ToString("0") + " dari " + H.ToString("0") + ")");
            });

            cur.Query<Label>().ForEach(l =>
            {
                if (!Shown(l) || string.IsNullOrWhiteSpace(l.text)) return;
                var r = l.worldBound;
                if (r.width < 2f || r.height < 2f) return;
                if (l.resolvedStyle.textOverflow == TextOverflow.Ellipsis) return;
                bool wrap = l.resolvedStyle.whiteSpace == WhiteSpace.Normal;
                if (wrap)
                {
                    // ukuran lokal (bukan worldBound): elemen di dalam panggung yang di-scale tetap diukur benar
                    var lr = l.layout;
                    var need = l.MeasureTextSize(l.text, lr.width, VisualElement.MeasureMode.Exactly, 0f, VisualElement.MeasureMode.Undefined);
                    if (need.y > lr.height + 1.5f) issues.Add("teks terpotong: \"" + Short(l.text) + "\" (" + need.y.ToString("0") + ">" + lr.height.ToString("0") + ")");
                }
                else if (!Under<ScrollView>(l))
                {
                    var need = l.MeasureTextSize(l.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
                    if (need.x > l.layout.width + 6f) issues.Add("teks terlalu panjang: \"" + Short(l.text) + "\"");
                }
                var box = BoxAncestor(l, cur);
                if (box != null)
                {
                    var br = box.worldBound;
                    if (r.yMax > br.yMax + 1.5f || r.xMax > br.xMax + 1.5f) issues.Add("teks keluar kotak: \"" + Short(l.text) + "\"");
                }
            });

            // layar scan: kartu yang mengisi bingkai harus >= 25% gambar kamera (syarat ARCore) di perangkat ini
            var frame = cur.Q<CardFrame>();
            if (frame != null && Shown(frame.Box))
            {
                float cov = CardFrame.CameraCoverage(new Vector2(W, H), frame.Box.worldBound.size, 4f / 3f);
                if (cov < CardFrame.MinCoverage) issues.Add("bingkai scan terlalu kecil: kartu " + (cov * 100).ToString("0") + "% gambar kamera (< 25%)");
                else Debug.Log("[Organo] " + label + ": bingkai " + frame.Box.worldBound.size + " = " + (cov * 100).ToString("0") + "% gambar kamera");
            }

            // sheet hasil scan: latarnya sampai tepi bawah layar (tanpa celah kamera), isinya di atas area aman bawah
            var sheet = cur.Q<DraggableSheet>();
            if (sheet != null && sheet.parent != null && Shown(sheet))
            {
                float bottom = sheet.parent.worldBound.yMin + sheet.layout.yMax; // tanpa geseran saat ringkas
                if (bottom < H - 0.5f) issues.Add("celah di bawah sheet: " + (H - bottom).ToString("0"));
                float inset = App.BottomInset;
                if (sheet.Peek.worldBound.yMax > H - inset + 0.5f) issues.Add("isi sheet masuk area aman bawah (" + inset.ToString("0") + ")");
            }

            Report(label + ": tombol & teks rapi di layar " + W.ToString("0") + "x" + H.ToString("0"), issues.Count == 0,
                issues.Count == 0 ? "" : string.Join(" | ", issues.Distinct().Take(6)));
        }

        static bool Shown(VisualElement e)
        {
            for (var v = e; v != null; v = v.parent)
            {
                var st = v.resolvedStyle;
                if (st.display == DisplayStyle.None || st.visibility == Visibility.Hidden || st.opacity < 0.05f) return false;
            }
            var r = e.worldBound;
            return !float.IsNaN(r.x) && r.width > 0f && r.height > 0f;
        }

        static bool Under<T>(VisualElement e) where T : VisualElement
        {
            for (var v = e.parent; v != null; v = v.parent) if (v is T) return true;
            return false;
        }

        /// <summary>Kotak berlatar terdekat (kartu, pita, kotak koreksi), bukan isi area gulir.</summary>
        static VisualElement BoxAncestor(VisualElement l, VisualElement stop)
        {
            for (var v = l.parent; v != null && v != stop; v = v.parent)
            {
                if (v is ScrollView || v.ClassListContains("unity-scroll-view__content-container")) return null;
                if (v.resolvedStyle.backgroundColor.a > 0.05f && v.resolvedStyle.position != Position.Absolute) return v;
            }
            return null;
        }

        static string TextOf(VisualElement e) => e.Q<Label>()?.text ?? e.name;
        static string Short(string t) => t.Length > 40 ? t.Substring(0, 40) + "…" : t;
    }
}
