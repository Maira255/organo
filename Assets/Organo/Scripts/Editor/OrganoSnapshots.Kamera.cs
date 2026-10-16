using System.Collections.Generic;
using Organo.AR;
using System.Linq;
using Organo.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace Organo.EditorTools
{
    /// <summary>
    /// Uji jalur kamera biasa (HP tanpa AR) dengan gambar kamera buatan: kartu dikenali → halaman hasil scan, model mengikuti
    /// kartu yang menjauh (mengecil & berpindah), kartu diangkat → model hilang, kartu lain → organ berganti, dan gambar
    /// tanpa kartu tidak memunculkan organ.
    /// </summary>
    public static partial class OrganoSnapshots
    {
        const int KW = MatcherSynth.CW, KH = MatcherSynth.CH;
        static List<MatcherSynth.Card> s_KCards;
        static System.Random s_KRnd;
        static float s_KCardH0;

        static MatcherSynth.Card KCard(string id) => s_KCards.Find(c => c.id == id);

        /// <summary>Gambar kamera: latar acak + kartu di letak (pusat & tinggi dalam piksel gambar).</summary>
        static Color32[] KFrame(string id, Vector2 center, float h)
        {
            var canvas = new Color32[KW * KH];
            var bg = new Color32(150, 128, 104, 255); // meja kayu
            for (int i = 0; i < canvas.Length; i++) canvas[i] = bg;
            if (id != null) MatcherSynthEval.PasteRotated(canvas, KCard(id), center, h * CardMatcher.Aspect, h, 2f, 0.95f);
            return MatcherSynth.BlurNoise(canvas, s_KRnd, 6f);
        }

        /// <summary>Letak bingkai scan layar Scan di gambar kamera buatan (seperti dihitung ARScanController).</summary>
        static Rect KScanFrame()
        {
            var scan = App.Current as ScanScreen;
            var view = scan.Root.worldBound;
            var box = scan.Frame.Box.worldBound;
            var local = new Rect(box.x - view.x, box.y - view.y, box.width, box.height);
            return CardMatcher.ScreenToImage(local, view.width, view.height, KW, KH);
        }

        static void KamMulai()
        {
            s_KCards = MatcherSynth.LoadCards();
            s_KRnd = new System.Random(4);
            App.ar.TestCamera = true;
            if (!App.Progress.HasCard("lambung")) App.Progress.AddCard("lambung", 0);
            App.GoTab(NavTab.Scan);
            // di Editor ARCore disimulasikan (meja berisi semua kartu): matikan supaya hanya jalur kamera biasa yang diuji
            App.ar.ResetDetection();
            if (App.ar.imageManager != null) App.ar.imageManager.enabled = false;
        }

        /// <summary>Beri tahu letak bingkai scan (di HP tanpa AR ini dilakukan layar Scan setiap frame).</summary>
        static void KTellFrame()
        {
            if (App.Current is ScanScreen scan) ScanParts.TellScanFrame(App.ar, scan.Root, scan.Frame.Box, true);
        }

        static void KamTanpaKartu()
        {
            KTellFrame();
            for (int i = 0; i < 12; i++) App.ar.AnalyzeForTest(MatcherSynthEval.Negative(s_KRnd).px, KW, KH);
            Report("kamera biasa: 12 gambar tanpa kartu tidak memunculkan organ", App.Current is ScanScreen && App.ar.CurrentOrgan == null,
                App.Current?.GetType().Name + " " + App.ar.CurrentOrgan?.id);
        }

        static void KamKenali()
        {
            var f = KScanFrame();
            s_KCardH0 = f.height * 0.9f;
            KTellFrame();
            int n = 0;
            for (; n < 8 && App.ar.CurrentOrgan == null; n++) App.ar.AnalyzeForTest(KFrame("lambung", f.center, s_KCardH0), KW, KH);
            Report("kamera biasa: kartu di bingkai dikenali setelah beberapa gambar (bukan satu gambar)", App.ar.CurrentOrgan?.id == "lambung" && n >= 3,
                (App.ar.CurrentOrgan?.id ?? "-") + " setelah " + n + " gambar");
        }

        static Rect s_KRectNear;

        static void KamHasil()
        {
            var res = App.Current as ScanResultScreen;
            Report("kamera biasa: halaman hasil scan terbuka untuk kartu itu", res != null && res.Organ?.id == "lambung", App.Current?.GetType().Name);
            var f = KScanFrame0;
            for (int i = 0; i < 3; i++) App.ar.AnalyzeForTest(KFrame("lambung", f.center, s_KCardH0), KW, KH);
            s_KRectNear = App.ar.WebcamCardRect;
            Report("kamera biasa: kartu terlacak di halaman hasil", App.ar.WebcamCardVisible && s_KRectNear.height > 10f, s_KRectNear.ToString());
        }

        static Rect KScanFrame0 => s_KFrame0;
        static Rect s_KFrame0;

        static void KamSimpanBingkai() => s_KFrame0 = KScanFrame();

        static void KamMenjauh()
        {
            // kartu dijauhkan pelan-pelan ke kiri atas: tingginya tinggal ±55%
            var from = s_KFrame0.center;
            var to = new Vector2(KW * 0.36f, KH * 0.38f);
            for (int i = 1; i <= 10; i++)
            {
                float t = i / 10f;
                App.ar.AnalyzeForTest(KFrame("lambung", Vector2.Lerp(from, to, t), s_KCardH0 * Mathf.Lerp(1f, 0.55f, t)), KW, KH);
            }
            var far = App.ar.WebcamCardRect;
            Report("kamera biasa: kartu menjauh → letak kartu mengecil (model ikut mengecil)",
                App.ar.WebcamCardVisible && far.height < s_KRectNear.height * 0.75f && far.height > s_KRectNear.height * 0.4f,
                s_KRectNear.height.ToString("0") + " → " + far.height.ToString("0"));
            Report("kamera biasa: kartu bergeser → letak kartu ikut bergeser",
                App.ar.WebcamCardVisible && far.center.x < s_KRectNear.center.x - 20f && far.center.y < s_KRectNear.center.y - 20f,
                s_KRectNear.center + " → " + far.center);
        }

        static void KamAngkat()
        {
            for (int i = 0; i < 4; i++) App.ar.AnalyzeForTest(KFrame(null, Vector2.zero, 0f), KW, KH);
            Report("kamera biasa: kartu diangkat → model disembunyikan", !App.ar.WebcamCardVisible);
        }

        static void KamGanti()
        {
            if (!App.Progress.HasCard("jantung")) App.Progress.AddCard("jantung", 0);
            int n = 0;
            for (; n < 10 && App.ar.CurrentOrgan?.id != "jantung"; n++)
                App.ar.AnalyzeForTest(KFrame("jantung", s_KFrame0.center, s_KCardH0), KW, KH);
            Report("kamera biasa: kartu lain diarahkan → organ berganti", App.ar.CurrentOrgan?.id == "jantung", (App.ar.CurrentOrgan?.id ?? "-") + " setelah " + n + " gambar");
        }

        static void KamGantiHalaman()
        {
            var res = App.Current as ScanResultScreen;
            Report("kamera biasa: halaman hasil ikut berganti ke organ kartu baru", res != null && res.Organ?.id == "jantung", res?.Organ?.id);
        }

        // ---- misi scan di kuis (kamera biasa): model di atas kartu, bukan diam di tengah
        static Rect KQuizFrame()
        {
            var root = App.Current.Root;
            var frame = root.Q<CardFrame>();
            var view = root.worldBound; var box = frame.Box.worldBound;
            return CardMatcher.ScreenToImage(new Rect(box.x - view.x, box.y - view.y, box.width, box.height), view.width, view.height, KW, KH);
        }

        static void KamKuisMulai()
        {
            App.ar.ResetDetection();
            var s = QuizSession.StartOrgan("hati");
            s.index = s.questions.FindIndex(x => x.type == Organo.Data.QuizType.MisiScan);
            App.Go<QuizScanScreen>(null, false);
            if (App.ar.imageManager != null) App.ar.imageManager.enabled = false;
        }

        static void KamKuisKenali()
        {
            var f = KQuizFrame();
            var frame = App.Current.Root.Q<CardFrame>();
            ScanParts.TellScanFrame(App.ar, App.Current.Root, frame.Box, true);
            for (int n = 0; n < 8 && App.ar.CurrentOrgan == null; n++) App.ar.AnalyzeForTest(KFrame("hati", f.center, f.height * 0.9f), KW, KH);
            s_KFrame0 = f;
            Report("kuis (kamera biasa): kartu yang dicari dikenali → jawaban benar", App.ar.CurrentOrgan?.id == "hati",
                App.ar.CurrentOrgan?.id);
            for (int i = 0; i < 3; i++) App.ar.AnalyzeForTest(KFrame("hati", f.center, f.height * 0.9f), KW, KH);
        }

        static void KamKuisModel()
        {
            var vps = App.Current.Root.Query<Organo.ThreeD.OrganViewport>().ToList();
            var vp = vps.Count > 0 ? vps[0] : null;
            Report("kuis (kamera biasa): model tampil di atas kartu (bukan viewer diam di tengah)",
                vp != null && vp.style.position == Position.Absolute && vp.resolvedStyle.opacity > 0.5f && App.ar.WebcamCardVisible,
                vp == null ? "tidak ada model" : "opacity " + vp.resolvedStyle.opacity.ToString("0.00"));
            for (int i = 0; i < 4; i++) App.ar.AnalyzeForTest(KFrame(null, Vector2.zero, 0f), KW, KH);
        }

        static void KamKuisAngkat()
        {
            var vps = App.Current.Root.Query<Organo.ThreeD.OrganViewport>().ToList();
            var vp = vps.Count > 0 ? vps[0] : null;
            Report("kuis (kamera biasa): kartu diangkat → model hilang", vp != null && vp.resolvedStyle.opacity < 0.2f && !App.ar.WebcamCardVisible,
                vp == null ? "tidak ada model" : "opacity " + vp.resolvedStyle.opacity.ToString("0.00"));
        }


        static bool s_KReadDone;

        /// <summary>Jalur gambar kamera biasa lewat GPU sama arahnya dengan GetPixels32 (cek urutan baris otomatis).</summary>
        static void KamBacaGpu()
        {
            s_KReadDone = false;
            App.StartCoroutine(App.ar.TestReadbackPath((ok, info) =>
            {
                Report("kamera biasa: gambar kamera lewat GPU tidak terbalik (cek urutan baris otomatis)", ok, info);
                s_KReadDone = true;
            }));
            s_WaitUntil = () => s_KReadDone;
        }

        static void KamSelesai()
        {
            App.ar.TestCamera = false;
            if (App.ar.imageManager != null) App.ar.imageManager.enabled = App.ar.IsRunning && App.ar.HasCardLibrary;
            App.ar.ResetDetection();
        }
    }
}
