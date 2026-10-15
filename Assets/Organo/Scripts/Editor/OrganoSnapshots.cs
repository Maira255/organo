using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Organo.Screens;
using Organo.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Organo.EditorTools
{
    /// <summary>
    /// Merender tiap layar Organo ke PNG (Library/OrganoPreview/ui_*.png) untuk dicek tanpa membuka HP.
    /// Menu: Organo &gt; Snapshot Semua Layar. Bisa juga dari command line:
    /// Unity -batchmode -projectPath . -executeMethod Organo.EditorTools.OrganoSnapshots.RunAndQuit
    /// </summary>
    [InitializeOnLoad]
    public static partial class OrganoSnapshots
    {
        const string PendingKey = "Organo.Snap.Pending";
        const string QuitKey = "Organo.Snap.Quit";
        const string OutDir = "Library/OrganoPreview";

        static int s_Step = -1;
        static double s_StepStart;
        static bool s_Ready;
        static RenderTexture s_RT;
        static List<(string name, Action action, float wait)> s_Steps;

        static OrganoSnapshots()
        {
            if (SessionState.GetBool(PendingKey, false)) EditorApplication.update += Drive;
        }

        [MenuItem("Organo/Snapshot Semua Layar", priority = 50)]
        public static void Run() => Start(false);

        public static void RunAndQuit() => Start(true);

        static bool s_TestsOnly;
        public static void RunTestsAndQuit() { s_TestsOnly = true; SessionState.SetBool("organo.snap.testsonly", true); Start(true); }

        const string SaveBackupPrefix = "organo.snap.save.";
        /// <summary>Data aplikasi di PlayerPrefs Editor yang disimpan dulu lalu dikembalikan setelah snapshot.</summary>
        static readonly string[] SaveKeys = { "organo.progress.local", "organo.onboarded", "organo.accounts.v1", "organo.progress.v1" };
        const string NoValue = "-";
        const string CardsKey = "organo.snap.cards";

        /// <summary>Render desain kartu Organo tiap organ ke Assets/Organo/AR/Cards lalu keluar.</summary>
        public static void RunCardsAndQuit() { SessionState.SetBool(CardsKey, true); Start(true); }

        /// <summary>Render kartu organ reproduksi saja (kartu ini hanya ada di aplikasi, tidak dicetak).</summary>
        public static void RunReproCardsAndQuit() { SessionState.SetString(CardsOnlyKey, Organo.Data.ReproAccess.Boy + "," + Organo.Data.ReproAccess.Girl); RunCardsAndQuit(); }
        const string CardsOnlyKey = "organo.snap.cardsonly";

        const string VariantsKey = "organo.snap.variants";
        public const int VariantCount = 8;
        public const string VariantDir = "Library/OrganoCardVariants";

        /// <summary>Render tiap kartu dalam beberapa varian pola ke Library/OrganoCardVariants untuk dinilai arcoreimg.</summary>
        public static void RunCardVariantsAndQuit() { SessionState.SetBool(VariantsKey, true); Start(true); }

        /// <summary>Varian pola hanya untuk kartu Rangka (kartu baru).</summary>
        public static void RunRangkaVariantsAndQuit() { SessionState.SetString(CardsOnlyKey, "rangka"); RunCardVariantsAndQuit(); }

        /// <summary>Render kartu Rangka saja (realistis & kartun).</summary>
        public static void RunRangkaCardsAndQuit() { SessionState.SetString(CardsOnlyKey, "rangka"); RunCardsAndQuit(); }

        static void Start(bool quit)
        {
            // progres milik pengguna di Editor disimpan dulu, snapshot mulai seperti aplikasi baru dipasang
            foreach (var k in SaveKeys)
            {
                string v = !PlayerPrefs.HasKey(k) ? NoValue : k == "organo.onboarded" ? PlayerPrefs.GetInt(k).ToString() : PlayerPrefs.GetString(k);
                SessionState.SetString(SaveBackupPrefix + k, v);
                PlayerPrefs.DeleteKey(k);
            }
            Organo.UI.Hints.ResetAll(); // petunjuk gerakan tampil seperti saat aplikasi pertama dibuka
            PlayerPrefs.SetInt("organo.camera.asked", 1);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(QuitKey, quit);
            EditorSceneManager.OpenScene("Assets/Organo/Scenes/Organo.unity");
            EditorApplication.update -= Drive;
            EditorApplication.update += Drive;
            EditorApplication.EnterPlaymode();
        }

        static OrganoApp App => OrganoApp.Instance;

        static void BuildSteps()
        {
            var a = App;
            // snapshot memakai latar kamera tiruan, bukan webcam laptop
            if (a.ar != null) a.ar.useWebcamInEditor = false;
            s_Steps = new List<(string, Action, float)>
            {
                ("01_splash", () => { }, 2.6f),
                ("02_onboarding", () => a.Go<OnboardingScreen>(0, false), 1f),
                ("03_onboarding3", () => a.Go<OnboardingScreen>(2, false), 1f),
                ("03b_uji_onboarding_selesai", TestOnboardingEnd, 0.8f),
                ("07_kenalan", () => a.Go<GenderScreen>(Organo.Data.Gender.Boy, false), 1f),
                ("07b_uji_tanpa_akun", TestOfflineSave, 0.3f),
                ("08_beranda", () => { a.Progress.onboarded = true; a.Progress.gender = Organo.Data.Gender.Boy; a.GoTab(NavTab.Beranda); }, 1.2f),
                ("09_library", () => a.GoTab(NavTab.Library), 1f),
                ("10_library_perut", () => a.Go<LibraryBodyScreen>(Organo.Data.BodyRegion.Perut, false), 1.4f),
                ("10c_library_dada", () => a.Go<LibraryBodyScreen>(Organo.Data.BodyRegion.Dada, false), 1.6f),
                ("10d_library_kepala", () => a.Go<LibraryBodyScreen>(Organo.Data.BodyRegion.Kepala, false), 1.6f),
                ("10e_library_leher", () => a.Go<LibraryBodyScreen>(Organo.Data.BodyRegion.Leher, false), 1.6f),
                ("10f_library_perempuan", () => { a.Go<LibraryBodyScreen>(null, false); (a.Current as LibraryBodyScreen)?.SetBody(Organo.Data.Gender.Girl); }, 1.6f),
                ("10g_library_laki", () => (a.Current as LibraryBodyScreen)?.SetBody(Organo.Data.Gender.Boy), 1.6f),
                ("11_closeup_lambung", () => a.Go<OrganDetailScreen>("lambung", false), 2f),
                ("12_closeup_jantung", () => a.Go<OrganDetailScreen>("jantung", false), 2f),
                ("13_closeup_paru", () => a.Go<OrganDetailScreen>("paru", false), 2f),
                ("14_closeup_ginjal", () => a.Go<OrganDetailScreen>("ginjal", false), 2f),
                ("14b_closeup_otak", () => a.Go<OrganDetailScreen>("otak", false), 2f),
                ("14c_closeup_mulut", () => a.Go<OrganDetailScreen>("mulut", false), 2f),
                ("14d_tubuh_lambung", () => a.Go<OrganDetailScreen>("lambung|tubuh", false), 2.5f),
                ("14e_tubuh_otak", () => a.Go<OrganDetailScreen>("otak|tubuh", false), 2.5f),
                ("14f_closeup_hati_organ", () => a.Go<OrganDetailScreen>("hati|organ", false), 2f),
                ("14g_paru_bronkus", () => a.Go<OrganDetailScreen>("paru#bronkus", false), 2.2f),
                ("14h_paru_bronkus_bawah", () => Scroll(380), 0.8f),
                ("14i_paru_alveolus", () => a.Go<OrganDetailScreen>("paru#alveolus", false), 2.2f),
                ("14j_jantung_serambi", () => a.Go<OrganDetailScreen>("jantung#serambi", false), 2.2f),
                ("14k_usushalus_vili", () => a.Go<OrganDetailScreen>("usushalus#vili", false), 2.2f),
                ("14l_ginjal_nefron", () => a.Go<OrganDetailScreen>("ginjal#nefron", false), 2.2f),
                ("14m_otak_neuron", () => a.Go<OrganDetailScreen>("otak#neuron", false), 2.2f),
                ("14n_ususbesar_sekum", () => a.Go<OrganDetailScreen>("ususbesar#apendiks", false), 2.2f),
                ("14o_lambung_fakta", () => { a.Go<OrganDetailScreen>("lambung", false); Scroll(900); }, 2f),
                ("14p_info_paru", () => a.Go<OrganInfoScreen>("paru", false), 1.5f),
                ("14q_info_paru_2", () => Scroll(700), 0.8f),
                ("14r_info_paru_3", () => Scroll(1500), 0.8f),
                ("14s_info_paru_4", () => Scroll(2400), 0.8f),
                ("14t_info_usus_besar", () => a.Go<OrganInfoScreen>("ususbesar", false), 1.5f),
                ("14u_paru_trakea", () => a.Go<OrganDetailScreen>("paru#trakea", false), 2.2f),
                ("14v_paru_hidung", () => a.Go<OrganDetailScreen>("paru#hidung", false), 2.2f),
                ("14w_paru_kerongkongan", () => a.Go<OrganDetailScreen>("paru#kerongkongan", false), 2.2f),
                ("14x_jantung_seldarah", () => a.Go<OrganDetailScreen>("jantung#seldarah", false), 2.2f),
                ("14y_usushalus", () => a.Go<OrganDetailScreen>("usushalus|organ", false), 2.2f),
                ("14z_info_jantung_fakta", () => { a.Go<OrganInfoScreen>("jantung", false); Scroll(2600); }, 1.5f),
                ("15_library_daftar", () => a.Go<LibraryListScreen>(null, false), 1f),
                ("16_scan", () => a.GoTab(NavTab.Scan), 1.2f),
                ("17_scan_hasil", () => a.Go<ScanResultScreen>("lambung", false), 2f),
                ("17b_scan_hasil_terbuka", () => a.Current?.Root.Q<DraggableSheet>()?.SetOpen(true), 1.2f),
                ("18_kartu_baru", () => a.Go<ScanResultScreen>("usushalus", false), 2f),
                ("19_quiz", () => a.GoTab(NavTab.Quiz), 1f),
                ("20_quiz_label", () => { QuizSession.StartProcess("cerna"); a.Go<QuizPlayScreen>(QuizSession.Current, false); }, 1f),
                ("21_quiz_pilihan", () => { var s = QuizSession.StartOrgan("lambung"); a.Go<QuizPlayScreen>(s, false); }, 2f),
                ("22_quiz_selesai", () => PlayThrough(QuizSession.StartOrgan("mulut"), 2), 1.5f),
                ("23_profil", () => a.GoTab(NavTab.Profil), 1f),
                ("24_koleksi", () => a.Go<CardCollectionScreen>(0, false), 1f),
                ("24b_koleksi_dimiliki", () => a.Go<CardCollectionScreen>(1, false), 1f),
                ("24c_kartu_depan", () => { if (!a.Progress.HasCard("lambung")) a.Progress.AddCard("lambung", 0); a.Go<CardViewerScreen>("lambung", false); }, 1.2f),
                ("24d_kartu_belakang", () => { var h = a.Current.Root.Q("card-holder"); if (h != null) Tap(h.worldBound.center); }, 1.2f),
                ("24e_kartu_terkunci", () => a.Go<CardViewerScreen>("otak", false), 1.2f),
                ("24f_beranda_koleksi", () => { a.GoTab(NavTab.Beranda); Scroll(470); }, 1.4f),
                ("25_lencana", () => a.Go<BadgesScreen>(null, false), 1f),
                ("26_aksesibilitas", () => a.Go<AccessibilityScreen>(null, false), 1f),
                ("27_materi", () => a.Go<BodyMaterialScreen>(null, false), 1f),
                ("27b_materi_perempuan", () => { a.Progress.gender = Organo.Data.Gender.Girl; a.Go<BodyMaterialScreen>(null, false); }, 1f),
                // ---- organ baru, tampilan "Dalam", favorit
                ("40_closeup_mata", () => { a.Progress.gender = Organo.Data.Gender.Boy; a.Go<OrganDetailScreen>("mata|organ", false); }, 2f),
                ("41_closeup_hidung", () => a.Go<OrganDetailScreen>("hidung|organ", false), 2f),
                ("42_closeup_tenggorokan", () => a.Go<OrganDetailScreen>("tenggorokan|organ", false), 2f),
                ("43_closeup_mulut", () => a.Go<OrganDetailScreen>("mulut|organ", false), 2f),
                ("44_dalam_jantung", () => a.Go<OrganDetailScreen>("jantung|dalam", false), 2f),
                ("45_dalam_paru", () => a.Go<OrganDetailScreen>("paru|dalam", false), 2f),
                ("46_dalam_lambung", () => a.Go<OrganDetailScreen>("lambung|dalam", false), 2f),
                ("47_dalam_ginjal", () => a.Go<OrganDetailScreen>("ginjal|dalam", false), 2f),
                ("48_dalam_otak", () => a.Go<OrganDetailScreen>("otak|dalam", false), 2f),
                ("49_dalam_mata", () => a.Go<OrganDetailScreen>("mata|dalam", false), 2f),
                ("50_dalam_hidung", () => a.Go<OrganDetailScreen>("hidung|dalam", false), 2f),
                ("51_dalam_tenggorokan", () => a.Go<OrganDetailScreen>("tenggorokan|dalam", false), 2f),
                ("52_dalam_mulut_gigi", () => a.Go<OrganDetailScreen>("mulut|dalam", false), 2f),
                ("53_jantung_katup", () => a.Go<OrganDetailScreen>("jantung#katup", false), 2.2f),
                ("54_mata_lensa", () => a.Go<OrganDetailScreen>("mata#lensa", false), 2.2f),
                ("55_dalam_usushalus", () => a.Go<OrganDetailScreen>("usushalus|dalam", false), 2f),
                ("56_dalam_hati", () => a.Go<OrganDetailScreen>("hati|dalam", false), 2f),
                ("57_dalam_pankreas", () => a.Go<OrganDetailScreen>("pankreas|dalam", false), 2f),
                ("58_dalam_ususbesar", () => a.Go<OrganDetailScreen>("ususbesar|dalam", false), 2f),
                ("59_dalam_kerongkongan", () => a.Go<OrganDetailScreen>("kerongkongan|dalam", false), 2f),
                ("60_info_mata", () => a.Go<OrganInfoScreen>("mata", false), 1.5f),
                ("61_beranda_favorit", () => { a.Progress.favorites.Clear(); a.Progress.favorites.AddRange(new[] { "jantung", "mata", "otak" }); a.GoTab(NavTab.Beranda); Scroll(330); }, 1.4f),
                ("62_library_favorit", () => a.Go<LibraryListScreen>("favorit", false), 1.2f),
                ("63_library_kepala", () => a.Go<LibraryBodyScreen>(Organo.Data.BodyRegion.Kepala, false), 1.4f),
                // ---- uji interaksi (jari disimulasikan), hasil di log "[Organo][Uji]"
                ("70_uji_sheet_awal", () => a.Go<ScanResultScreen>("lambung", false), 1.5f),
                ("71_uji_sheet_tarik_naik", TestSheetUp, 1.0f),
                ("72_uji_sheet_tarik_turun", TestSheetDown, 1.0f),
                ("72b_uji_putar_bebas", () => Report("viewer 3D hasil scan bisa diputar 360° ke segala arah",
                    App.Viewer != null && App.Viewer.FreeRotate && App.Current is ScanResultScreen), 0.2f),
                ("73_uji_detail_awal", () => a.Go<OrganDetailScreen>("paru|organ", false), 2f),
                ("74_uji_rail_geser", TestRail, 1.0f),
                ("75_uji_halaman_geser", TestPageScroll, 1.0f),
                ("76_uji_ketuk_kartu", TestTapCard, 1.6f),
                ("76b_uji_suara_pembaca", TestNarration, 2.0f),
                ("77_uji_kuis_awal", () => { QuizSession.StartProcess("cerna"); a.Go<QuizPlayScreen>(QuizSession.Current, false); }, 1.2f),
                ("78_uji_kuis_seret", TestQuizDrag, 1.0f),
                ("79_uji_library_sheet", () => a.Go<LibraryBodyScreen>(Organo.Data.BodyRegion.Perut, false), 1.4f),
                ("80_uji_library_sheet_turun", TestLibrarySwipe, 1.2f),
                ("81_uji_overlay", () => a.ShowSheet(new Label("Uji sheet: tarik ke bawah untuk menutup") { style = { height = 160, unityTextAlign = TextAnchor.MiddleCenter } }), 0.8f),
                ("82_uji_overlay_turun", TestOverlaySwipe, 1.0f),
                ("86_uji_suara", TestAudio, 0.5f),
                ("86b_uji_pengenal_kartu", TestCardMatcher, 1.0f),
                ("89_uji_kartu_awal", () => { foreach (var id in new[] { "mulut", "kerongkongan" }) if (!a.Progress.HasCard(id)) a.Progress.AddCard(id, 0); a.Go<CardViewerScreen>("mulut", false); }, 1.0f),
                ("89b_uji_kartu_geser", TestCardSwipe, 1.0f),
                ("89c_uji_kartu_balik", TestCardFlip, 1.0f),
                ("8b_profil", () => a.GoTab(NavTab.Profil), 1.2f),
                ("8c_profil_nama", () => (a.Current as ProfileScreen)?.ShowRename(), 1.0f),
                ("8d_profil_tutup", () => a.CloseTopOverlay(), 0.5f),
                ("8h_repro_peta", () =>
                {
                    var pr = a.Progress;
                    pr.onboarded = true; pr.gender = Organo.Data.Gender.Boy; pr.doneTopics.Clear();
                    pr.cards.Remove(Organo.Data.ReproAccess.Boy); pr.cards.Remove(Organo.Data.ReproAccess.Girl);
                    a.Go<LibraryBodyScreen>(Organo.Data.BodyRegion.Perut, false);
                }, 1.6f),
                ("8i_repro_daftar", () => a.Go<LibraryListScreen>(Organo.Data.OrganSystem.Reproduksi, false), 1.2f),
                ("8j_repro_laki", () => a.Go<OrganDetailScreen>("reprolaki|organ", false), 2.2f),
                ("8k_repro_laki_dalam", () => a.Go<OrganDetailScreen>("reprolaki|dalam", false), 2.2f),
                ("8l_uji_repro_kunci", TestReproLocked, 1.2f),
                ("8m_kuis_repro", () => { QuizSession.StartOrgan(Organo.Data.ReproAccess.Boy); a.Go<QuizPlayScreen>(QuizSession.Current, false); }, 1.8f),
                ("8n_uji_kuis_repro", TestReproQuiz, 1.8f),
                ("8u_uji_kuis_isi", TestQuizBank, 0.5f),
                ("8u2_uji_kuis_kunci", TestQuizLocks, 0.5f),
                ("8u3_kuis_organ", () => a.Go<QuizHubScreen>(0, false), 1.2f),
                ("8u4_kuis_proses", () => a.Go<QuizHubScreen>(1, false), 1.2f),
                ("8v_sorot_pilorus", () => a.Go<OrganDetailScreen>("lambung#pilorus", false), 2.2f),
                ("8w_uji_sorot_tidak_terbawa", TestHighlightNotCarried, 1.6f),
                ("8x_uji_kuis_ketuk", TestQuizTapInPlace, 1.6f),
                ("8y_kuis_napas_label", () => { QuizSession.StartProcess("napas"); a.Go<QuizPlayScreen>(QuizSession.Current, false); }, 1.2f),
                ("8y2_uji_label_ulang", TestLabelRetry, 1.6f),
                ("8z_kuis_hub", () => a.GoTab(NavTab.Quiz), 1.2f),
                ("8o_repro_perempuan", () => a.Go<OrganDetailScreen>("reproperempuan|organ", false), 2.2f),
                ("8p_repro_perempuan_dalam", () => a.Go<OrganDetailScreen>("reproperempuan|dalam", false), 2.2f),
                ("8q_repro_sperma", () => a.Go<OrganDetailScreen>("reprolaki#sperma", false), 2.2f),
                ("8r_repro_seltelur", () => a.Go<OrganDetailScreen>("reproperempuan#seltelur", false), 2.2f),
                ("8s_kuis_hub_repro", () => { a.GoTab(NavTab.Quiz); Scroll(1400); }, 1.4f),
                ("8t_beranda_kategori", () => { a.GoTab(NavTab.Beranda); Scroll(330); }, 1.4f),
                // layar Scan tanpa kamera menyala (supaya meja simulasi tidak langsung terdeteksi) untuk mengukur bingkai
                ("90_bingkai_scan", () => { a.GoTab(NavTab.Scan); a.ar?.End(); }, 1.6f),
                ("90b_uji_bingkai", TestScanFrame, 0.3f),
                ("90c_bantuan_scan", () => (a.Current as ScanScreen)?.ShowHelp(), 1.0f),
                ("90d_tutup_bantuan", () => { a.CloseTopOverlay(); a.GoTab(NavTab.Beranda); }, 0.6f),
                ("90e_misi_scan", () => { QuizSession.StartOrgan("mulut"); a.Go<QuizScanScreen>(null, false); a.ar?.End(); }, 1.6f),
                ("90e2_uji_misi_salah", TestScanWrongSteady, 1.2f),
                ("90f_uji_bingkai_misi", TestQuizScanFrame, 0.3f),
                ("90g_keluar_misi", () => a.GoTab(NavTab.Beranda), 0.6f),
                ("9a_ar_scan", () =>
                {
                    var pr = a.Progress;
                    pr.onboarded = true; if (pr.gender == Organo.Data.Gender.None) pr.gender = Organo.Data.Gender.Boy;
                    if (a.ar != null)
                    {
                        a.ar.useWebcamInEditor = false;
                        s_ArFirst = null; s_ArDetections = 0; s_ArSwitches.Clear();
                        a.ar.CardDetected += o => { s_ArDetections++; if (s_ArFirst == null) s_ArFirst = o.id; };
                        a.ar.CardSwitched += o => s_ArSwitches.Add(o.id);
                    }
                    a.GoTab(NavTab.Scan);
                }, 8f),
                ("9b_uji_ar_kartu", TestArScan, 1.5f),
                ("9b1_uji_ar_titik", TestArHotspotTap, 0.8f),
                ("9b2_uji_ar_putar", TestArRotate, 1.8f),
                ("9c_rangka", () => a.Go<OrganDetailScreen>("rangka|organ", false), 2.4f),
                ("9d_rangka_dalam", () => a.Go<OrganDetailScreen>("rangka|dalam", false), 2.4f),
                ("9e_rangka_tubuh", () => a.Go<OrganDetailScreen>("rangka|tubuh", false), 2.4f),
                ("9i_rangka_tengkorak", () => a.Go<OrganDetailScreen>("rangka#tengkorak", false), 2.6f),
                ("9j_uji_rangka_tengkorak", TestSkullFocus, 0.3f),
                ("9k_rangka_pipih", () => a.Go<OrganDetailScreen>("rangka#pipih", false), 2.4f),
                ("9l_uji_rangka_dalam", TestBoneInside, 0.3f),
                ("9m_rangka_utuh", () => a.Go<OrganDetailScreen>("rangka|organ", false), 1.6f),
                ("9n_uji_zoom_titik", TestZoomAtPoint, 2.4f),
                ("9f_jantung_tubuh_tulang", () =>
                {
                    a.Go<OrganDetailScreen>("jantung|tubuh", false);
                    a.Viewer?.ShowBones(true);
                }, 2.4f),
                ("9g_repro_laki_baru", () => a.Go<OrganDetailScreen>("reprolaki|organ", false), 2.4f),
                ("9h_repro_laki_baru_dalam", () => a.Go<OrganDetailScreen>("reprolaki|dalam", false), 2.4f),
                ("88_kartu_dari_mana", () => { a.GoTab(NavTab.Scan); a.cardsWebsiteUrl = "https://contoh.sekolah.id/kartu-organo"; CardSource.Show(a); }, 1.4f),
                ("28_ikon", ShowIconSheet, 1f),
            };
            if (SessionState.GetBool(VariantsKey, false))
            {
                s_Steps = new List<(string, Action, float)> { ("08_beranda", () => { a.Progress.onboarded = true; a.GoTab(NavTab.Beranda); }, 1.5f) };
                var onlyV = SessionState.GetString(CardsOnlyKey, "");
                foreach (var o in a.Database.Ordered)
                    for (int v = 0; v < VariantCount; v++)
                    {
                        if (onlyV.Length > 0 && System.Array.IndexOf(onlyV.Split(','), o.id) < 0) continue;
                        var organ = o; int vv = v;
                        s_Steps.Add(("var_r_" + organ.id + "_" + vv, () => { OrganoCards.ForceVariant = vv; ShowCard(organ, false); }, 0.5f));
                        s_Steps.Add(("var_k_" + organ.id + "_" + vv, () => { OrganoCards.ForceVariant = vv; ShowCard(organ, true); }, 0.5f));
                    }
                return;
            }
            if (SessionState.GetBool(CardsKey, false))
            {
                s_Steps = new List<(string, Action, float)> { ("08_beranda", () => { a.Progress.onboarded = true; a.GoTab(NavTab.Beranda); }, 1.5f) };
                var only = SessionState.GetString(CardsOnlyKey, "");
                foreach (var o in a.Database.Ordered)
                {
                    var organ = o;
                    if (only.Length > 0 && System.Array.IndexOf(only.Split(','), organ.id) < 0) continue;
                    s_Steps.Add(("card_" + organ.id, () => ShowCard(organ, false), 0.8f));
                    s_Steps.Add(("cardk_" + organ.id, () => ShowCard(organ, true), 0.8f));
                }
                return;
            }
            if (SessionState.GetBool(LayoutKey, false)) { s_Steps = LayoutSteps(); return; }
            if (s_TestsOnly || SessionState.GetBool("organo.snap.testsonly", false))
            {
                Organo.UI.TrackedGesture.Trace = true;
                s_Steps = s_Steps.Where(st => st.name.StartsWith("04_") || st.name.StartsWith("05_") || st.name.StartsWith("06_") || st.name.StartsWith("23_") || st.name.StartsWith("08_") || st.name.StartsWith("09_") || st.name.StartsWith("10") || st.name.StartsWith("16_") || st.name.StartsWith("17_") || st.name.StartsWith("20_") || st.name.StartsWith("21_") || st.name.StartsWith("24") || st.name.StartsWith("7") || st.name.StartsWith("8") || st.name.StartsWith("9")).ToList();
            }
        }

        static void Drive()
        {
            if (!SessionState.GetBool(PendingKey, false)) { EditorApplication.update -= Drive; return; }
            if (!EditorApplication.isPlaying || App == null) return;

            if (!s_Ready)
            {
                s_Ready = true;
                Directory.CreateDirectory(OutDir);
                s_RT = new RenderTexture(780, 1688, 24, RenderTextureFormat.ARGB32);
                var doc = App.GetComponent<UIDocument>();
                var ps = UnityEngine.Object.Instantiate(doc.panelSettings);
                ps.targetTexture = s_RT;
                ps.clearColor = true;
                ps.colorClearValue = new Color(0.36f, 0.31f, 0.27f, 1f); // pengganti feed kamera
                doc.panelSettings = ps;
                App.enabled = false;
                App.enabled = true;
                BuildSteps();
                s_Step = 0;
                s_StepStart = EditorApplication.timeSinceStartup;
                return;
            }

            if (s_Step >= s_Steps.Count) return;
            var (name, action, wait) = s_Steps[s_Step];
            double t = EditorApplication.timeSinceStartup - s_StepStart;
            if (t < 0.05) return;
            if (!s_ActionDone)
            {
                s_ActionDone = true;
                try { action(); } catch (Exception e) { Debug.LogError("[Organo] snapshot " + name + ": " + e); }
                s_StepStart = EditorApplication.timeSinceStartup;
                return;
            }
            if (t < wait) return;
            Capture(name);
            s_ActionDone = false;
            s_Step++;
            s_StepStart = EditorApplication.timeSinceStartup;
            if (s_Step >= s_Steps.Count) Finish();
        }

        static bool s_ActionDone;

        // ------------------------------------------------------------ uji interaksi

        static VisualElement UiRoot => App.GetComponent<UIDocument>().rootVisualElement;

        static void SendPointer(EventType type, Vector2 pos)
        {
            var ev = new Event { type = type, mousePosition = pos, button = 0, clickCount = 1 };
            EventBase pe = type == EventType.MouseDown ? PointerDownEvent.GetPooled(ev)
                : type == EventType.MouseUp ? PointerUpEvent.GetPooled(ev) : (EventBase)PointerMoveEvent.GetPooled(ev);
            UiRoot.panel.visualTree.SendEvent(pe);
            pe.Dispose();
        }

        static bool s_Spy;
        static void Drag(Vector2 from, Vector2 to, int steps = 10)
        {
            if (!s_Spy)
            {
                s_Spy = true;
                var vt = UiRoot.panel.visualTree;
                vt.RegisterCallback<PointerDownEvent>(e => Debug.Log("[Organo][Spy] down " + e.position + " target=" + (e.target as VisualElement)?.GetType().Name), TrickleDown.TrickleDown);
                vt.RegisterCallback<PointerMoveEvent>(e => Debug.Log("[Organo][Spy] move " + e.position + " target=" + (e.target as VisualElement)?.GetType().Name + " cap=" + (vt.panel.GetCapturingElement(e.pointerId) as VisualElement)?.GetType().Name), TrickleDown.TrickleDown);
                vt.RegisterCallback<PointerUpEvent>(e => Debug.Log("[Organo][Spy] up " + e.position), TrickleDown.TrickleDown);
            }
            SendPointer(EventType.MouseDown, from);
            for (int i = 1; i <= steps; i++) SendPointer(EventType.MouseDrag, Vector2.Lerp(from, to, i / (float)steps));
            SendPointer(EventType.MouseUp, to);
        }

        static void Tap(Vector2 at)
        {
            SendPointer(EventType.MouseDown, at);
            SendPointer(EventType.MouseUp, at);
        }

        static void Report(string what, bool ok, string detail = "") =>
            Debug.Log("[Organo][Uji] " + (ok ? "LULUS " : "GAGAL ") + what + (!string.IsNullOrEmpty(detail) ? " (" + detail + ")" : ""));

        static void Later(System.Action act, long ms) => UiRoot.schedule.Execute(act).ExecuteLater(ms);

        static void TestSheetUp()
        {
            var sheet = App.Current?.Root.Q<DraggableSheet>();
            if (sheet == null) { Report("sheet hasil scan ditemukan", false); return; }
            var from = sheet.Peek.worldBound.center;
            Drag(from, from + new Vector2(0, -320));
            Later(() => Report("sheet ditarik ke atas -> terbuka", sheet.IsOpen), 400);
        }

        static void TestSheetDown()
        {
            var sheet = App.Current?.Root.Q<DraggableSheet>();
            if (sheet == null) return;
            var from = sheet.Peek.worldBound.center;
            Drag(from, from + new Vector2(0, 320));
            Later(() => Report("sheet ditarik ke bawah -> ringkas", !sheet.IsOpen), 400);
        }

        static ScrollView FindScroll(bool horizontal)
        {
            ScrollView found = null;
            App.Current?.Root.Query<ScrollView>().ForEach(v =>
            {
                if (found == null && (v.mode == ScrollViewMode.Horizontal) == horizontal) found = v;
            });
            return found;
        }

        static void TestRail()
        {
            var rail = FindScroll(true);
            if (rail == null) { Report("rail bagian organ ditemukan", false); return; }
            float before = rail.scrollOffset.x;
            var c = rail.worldBound.center;
            Drag(c + new Vector2(120, 0), c + new Vector2(-150, 6));
            Later(() => Report("rail digeser kiri-kanan dengan menarik", rail.scrollOffset.x > before + 50f, before + " -> " + rail.scrollOffset.x), 300);
        }

        static void TestPageScroll()
        {
            var page = FindScroll(false);
            if (page == null) { Report("halaman bisa digulir", false); return; }
            float before = page.scrollOffset.y;
            var b = page.worldBound;
            var from = new Vector2(b.center.x, b.yMax - 80);
            Drag(from, from + new Vector2(0, -260));
            Later(() => Report("halaman digulir dengan menarik (mulai di atas kartu)", page.scrollOffset.y > before + 60f, before + " -> " + page.scrollOffset.y), 300);
        }

        /// <summary>
        /// Uji scan sungguhan lewat XR Simulation: AR Foundation melacak kartu di meja simulasi memakai library kartu yang sama
        /// dengan di HP. Dicek: kartu dikenali, nama gambar cocok dengan organ, dan model 3D muncul menempel di atas kartu.
        /// </summary>
        static string s_ArFirst;
        static int s_ArDetections;
        static readonly List<string> s_ArSwitches = new List<string>();

        /// <summary>
        /// Penyebab utama scan gagal di HP: kartu terlalu kecil di gambar kamera. ARCore baru mengenali gambar yang memenuhi
        /// minimal 25% gambar kamera. Uji ini mengukur bingkai scan sungguhan di layar dan menghitung bagian gambar kamera
        /// (4:3 dan 16:9, ditampilkan memenuhi layar) yang tertutup kartu bila kartu pas mengisi bingkai — juga untuk HP layar pendek.
        /// </summary>
        static void TestScanFrame()
        {
            var scan = App.Current as ScanScreen;
            if (scan == null || scan.Frame == null || scan.Frame.panel == null) { Report("layar Scan memakai bingkai kartu", false); return; }
            var f = scan.Frame;
            var box = f.Box.worldBound;
            var screen = f.panel.visualTree.layout.size;
            Report("bingkai scan berbentuk kartu 63x88 mm", Mathf.Abs(box.height / Mathf.Max(1f, box.width) - CardFrame.CardAspect) < 0.02f,
                box.width.ToString("0") + "x" + box.height.ToString("0") + " di layar " + screen.x.ToString("0") + "x" + screen.y.ToString("0"));
            float c43 = CardFrame.CameraCoverage(screen, box.size, 4f / 3f), c169 = CardFrame.CameraCoverage(screen, box.size, 16f / 9f);
            Report("kartu yang mengisi bingkai menutupi >= 25% gambar kamera (syarat ARCore)",
                c43 >= CardFrame.MinCoverage && c169 >= CardFrame.MinCoverage, (c43 * 100).ToString("0") + "% kamera 4:3, " + (c169 * 100).ToString("0") + "% kamera 16:9");
            // HP layar lebih pendek (16:9): ruang atas/bawah yang terpakai UI tetap sama, sisa tinggi untuk bingkai berkurang
            float top = f.TopAnchor != null ? f.TopAnchor.worldBound.yMax + 10f : 0f;
            float bottom = f.BottomAnchor != null ? f.BottomAnchor.worldBound.yMin - 10f : screen.y;
            float reserved = screen.y - (bottom - top);
            var shortScreen = new Vector2(screen.x, screen.x * 16f / 9f);
            var shortBox = CardFrame.FitSize(shortScreen.x, shortScreen.y - reserved);
            float cShort = CardFrame.CameraCoverage(shortScreen, shortBox, 4f / 3f);
            Report("di HP layar 16:9 kartu tetap menutupi >= 25% gambar kamera", cShort >= CardFrame.MinCoverage,
                shortBox.x.ToString("0") + "x" + shortBox.y.ToString("0") + " = " + (cShort * 100).ToString("0") + "%");
            Report("petunjuk scan menyuruh mendekatkan HP sampai kartu memenuhi bingkai",
                ScanScreen.ScanTips.Length > 0 && ScanScreen.ScanTips[0].Contains("memenuhi bingkai"));
        }

        static void TestQuizScanFrame()
        {
            var f = App.Current != null ? App.Current.Root.Q<CardFrame>() : null;
            if (!(App.Current is QuizScanScreen) || f == null || f.panel == null) { Report("Misi Scan memakai bingkai kartu", false); return; }
            var box = f.Box.worldBound;
            var screen = f.panel.visualTree.layout.size;
            float c43 = CardFrame.CameraCoverage(screen, box.size, 4f / 3f);
            Report("Misi Scan: kartu yang mengisi bingkai menutupi >= 25% gambar kamera", c43 >= CardFrame.MinCoverage,
                box.width.ToString("0") + "x" + box.height.ToString("0") + " = " + (c43 * 100).ToString("0") + "% kamera 4:3");
        }

        static void TestArScan()
        {
            var ar = App.ar;
            if (ar == null || ar.imageManager == null) { Report("komponen AR ada di scene", false); return; }
            Report("library kartu AR terpasang (" + (ar.imageManager.referenceLibrary?.count ?? 0) + " kartu)", ar.HasCardLibrary);
            Report("sesi AR berjalan (XR Simulation)", ar.IsCameraLive, UnityEngine.XR.ARFoundation.ARSession.state.ToString());
            var names = new System.Collections.Generic.List<string>();
            int mapped = 0;
            foreach (var img in ar.imageManager.trackables)
            {
                names.Add(img.referenceImage.name);
                if (App.Database.FindByReferenceImage(img.referenceImage.name) != null) mapped++;
            }
            Report("kartu di meja terdeteksi kamera AR", names.Count > 0, names.Count + " kartu: " + string.Join(", ", names));
            Report("setiap kartu terdeteksi cocok dengan organnya", names.Count > 0 && mapped == names.Count, mapped + "/" + names.Count);
            int cartoon = names.Count(n => n.EndsWith(Organo.Data.OrganDatabase.StyleSeparator + "kartun"));
            Report("kartu gaya Realistis dan Kartun sama-sama dikenali", cartoon > 0 && cartoon < names.Count,
                (names.Count - cartoon) + " Realistis, " + cartoon + " Kartun");
            var organsSeen = new System.Collections.Generic.HashSet<string>();
            foreach (var n in names) { var o = App.Database.FindByReferenceImage(n); if (o != null) organsSeen.Add(o.id); }
            int printable = App.Database.organs.Count(o => o != null && o.cardImage != null);
            Report("semua kartu cetak terdeteksi", organsSeen.Count == printable, organsSeen.Count + "/" + printable + " organ");
            bool onCard = false;
            if (ar.HasSpawnedModel && ar.CurrentOrgan != null)
                foreach (var img in ar.imageManager.trackables)
                    if (App.Database.FindByReferenceImage(img.referenceImage.name) == ar.CurrentOrgan)
                    {
                        // satu organ bisa punya dua kartu (Realistis & Kartun); model menempel di salah satunya
                        var spawned = img.transform.Find("AR " + ar.CurrentOrgan.displayName);
                        if (spawned != null && spawned.GetComponentInChildren<Renderer>() != null) onCard = true;
                    }
            Report("model 3D muncul menempel di atas kartu", onCard, ar.CurrentOrgan != null ? ar.CurrentOrgan.displayName : "-");
            // model tetap di atas kartu walau kartu di luar gambar kamera; petunjuk "arahkan ke kartu" hanya saat itu
            var mt = ar.ModelTransform;
            var card = mt != null && mt.parent != null ? mt.parent.parent : null;
            if (card != null && ar.arCamera != null)
            {
                var cv = ar.arCamera.WorldToViewportPoint(card.position);
                bool cardIn = cv.z > 0f && cv.x > -0.1f && cv.x < 1.1f && cv.y > -0.1f && cv.y < 1.1f;
                Report("petunjuk arahkan ke kartu hanya muncul saat kartu di luar gambar kamera", ar.ModelInView == cardIn,
                    "kartu " + cv.ToString("0.00") + " model " + ar.arCamera.WorldToViewportPoint(mt.position).ToString("0.00"));
            }
            else Report("model AR ada di atas kartu", false);
            Report("halaman hasil scan terbuka", App.Current is ScanResultScreen);
            Report("beberapa kartu terlihat: dipilih satu kartu (paling tengah)", s_ArDetections == 1, "terpilih " + s_ArFirst + ", deteksi " + s_ArDetections + "x");
            // kartu lain diarahkan ke kamera: halaman hasil scan ikut berganti ke organ kartu itu
            var scr = App.Current as ScanResultScreen;
            Report("halaman hasil scan menampilkan organ kartu yang dipakai AR", scr != null && ar.CurrentOrgan != null && scr.Organ == ar.CurrentOrgan,
                (scr?.Organ?.id ?? "-") + " / AR " + (ar.CurrentOrgan?.id ?? "-") + (s_ArSwitches.Count > 0 ? ", berganti: " + s_ArFirst + " -> " + string.Join(" -> ", s_ArSwitches) : ", tidak berganti"));
            if (s_ArSwitches.Count > 0)
                Report("berganti ke kartu yang ada di depan kamera", ar.ModelInView, ar.CurrentOrgan?.id);
        }

        /// <summary>Titik info model AR bisa diketuk: keterangannya sesuai titik itu; ketuk lagi menutup.</summary>
        static void TestArHotspotTap()
        {
            var scr = App.Current as ScanResultScreen;
            var ar = App.ar;
            if (scr == null || ar == null) { Report("halaman hasil scan AR terbuka", false); return; }
            var dots = new List<VisualElement>();
            scr.Root.Query(className: "hotspot").ForEach(dots.Add);
            int pick = -1;
            for (int i = 1; i < dots.Count && pick < 0; i++) if (Shown(dots[i])) pick = i;
            if (!ar.ModelInView || pick < 0 || pick >= ar.SpawnedHotspots.Count)
            {
                Debug.Log("[Organo] uji ketuk titik AR dilewati: model di luar kamera simulasi (" + dots.Count + " titik)");
                return;
            }
            string want = ar.SpawnedHotspots[pick].title;
            Label CalloutTitle()
            {
                var c = scr.Root.Q(className: "callout");
                return c != null && Shown(c) ? c.Q<Label>() : null;
            }
            Tap(dots[pick].worldBound.center);
            Later(() =>
            {
                var t = CalloutTitle();
                Report("ketuk titik AR menampilkan keterangan bagian itu", t != null && t.text == want, (t?.text ?? "tidak ada") + " / " + want);
                Tap(dots[pick].worldBound.center);
                Later(() => Report("ketuk titik yang sama lagi menutup keterangan", CalloutTitle() == null), 250);
            }, 250);
        }

        static void TestReproLocked()
        {
            var p = App.Progress;
            Report("organ reproduksi sendiri terbuka (tubuh laki-laki)", !Organo.Data.ReproAccess.IsLocked(Organo.Data.ReproAccess.Boy, p));
            Report("organ reproduksi gender lain terkunci sebelum kuis", Organo.Data.ReproAccess.IsLocked(Organo.Data.ReproAccess.Girl, p));
            App.Go<OrganDetailScreen>(Organo.Data.ReproAccess.Girl, false);
            bool lockedPage = false, viewer = false;
            App.Current.Root.Query<Label>().ForEach(l => { if (l.text == "Masih terkunci") lockedPage = true; });
            App.Current.Root.Query<Organo.ThreeD.OrganViewport>().ForEach(_ => viewer = true);
            Report("halaman organ terkunci tampil tanpa model 3D", lockedPage && !viewer);
            var none = new Organo.Data.PlayerProgress { gender = Organo.Data.Gender.None };
            Report("tubuh belum dipilih: kedua organ reproduksi terkunci", Organo.Data.ReproAccess.IsLocked(Organo.Data.ReproAccess.Boy, none) && Organo.Data.ReproAccess.IsLocked(Organo.Data.ReproAccess.Girl, none));
        }

        static void TestReproQuiz()
        {
            var s = QuizSession.Current;
            if (s == null || s.organId != Organo.Data.ReproAccess.Boy) { Report("kuis reproduksi dimulai", false); return; }
            int guard = 0;
            while (s.Q != null && guard++ < 20)
            {
                var q = s.Q;
                s.Answer(true, q.options[q.correct], q.options[q.correct]);
                s.Next(App);
            }
            var p = App.Progress;
            Report("kuis reproduksi laki-laki selesai tercatat", p.doneTopics.Contains(Organo.Data.ReproAccess.Topic(Organo.Data.ReproAccess.Boy))
                && Organo.Data.QuizAccess.OrganDone(p, Organo.Data.ReproAccess.Boy));
            Report("organ reproduksi perempuan terbuka setelah kuis milik sendiri", !Organo.Data.ReproAccess.IsLocked(Organo.Data.ReproAccess.Girl, p) && s.unlocked == Organo.Data.ReproAccess.Girl, s.unlocked);
            var girl = new Organo.Data.PlayerProgress { gender = Organo.Data.Gender.Girl };
            girl.doneTopics.Add(Organo.Data.ReproAccess.Topic(Organo.Data.ReproAccess.Boy));
            Report("anak perempuan tetap harus menyelesaikan kuis miliknya sendiri", Organo.Data.ReproAccess.IsLocked(Organo.Data.ReproAccess.Boy, girl));
        }

        /// <summary>Jawab semua soal kuis (soal ke-<paramref name="wrongAt"/> sengaja salah) sampai halaman hasil.</summary>
        static void PlayThrough(QuizSession s, int wrongAt = -1)
        {
            int guard = 0;
            while (s.Q != null && guard++ < 12)
            {
                var q = s.Q;
                bool ok = s.index != wrongAt;
                string right = q.options != null ? q.options[q.correct] : q.organId ?? "Semua tepat";
                string yours = ok || q.options == null ? right : q.options[(q.correct + 1) % q.options.Length];
                s.Answer(ok, yours, right);
                s.Next(App, s.index);
            }
        }

        /// <summary>Isi kuis: tiap kuis organ & kuis proses berisi 5 soal yang benar-benar bisa dimainkan.</summary>
        static void TestQuizBank()
        {
            var db = App.Database;
            var notes = new List<string>();
            void Check(string quiz, List<Organo.Data.QuizQuestion> qs)
            {
                if (qs.Count != 5) notes.Add(quiz + ": " + qs.Count + " soal");
                foreach (var q in qs)
                {
                    if (q.type == Organo.Data.QuizType.PilihanGanda3D)
                    {
                        var o = db.Get(q.organId);
                        if (o == null || o.modelPrefab == null) notes.Add(quiz + ": model 3D " + q.organId + " tidak ada");
                        if (q.options == null || q.correct < 0 || q.correct >= q.options.Length || q.options.Distinct().Count() != q.options.Length)
                            notes.Add(quiz + ": pilihan/indeks jawaban salah (" + q.prompt + ")");
                    }
                    else if (q.type == Organo.Data.QuizType.TempelLabel)
                    {
                        foreach (var id in q.labelOrgans)
                            if (db.Get(id) == null || !QuizPlayScreen.HasLabelSpot(id)) notes.Add(quiz + ": label " + id + " tanpa titik di tubuh");
                    }
                    else
                    {
                        var o = db.Get(q.organId);
                        if (o == null || o.cardImage == null) notes.Add(quiz + ": misi scan " + q.organId + " tidak punya kartu");
                    }
                }
            }
            foreach (var id in Organo.Data.QuizAccess.Order(App.Progress)) Check("organ " + id, Organo.Data.QuizBank.Organ(id));
            foreach (var pr in Organo.Data.QuizBank.Processes)
            {
                Check("proses " + pr.id, Organo.Data.QuizBank.Process(pr.id));
                foreach (var o in pr.organs) if (Organo.Data.QuizBank.OrganOrder.All(x => x != o)) notes.Add(pr.id + ": organ " + o + " tidak punya kuis organ");
            }
            Report("tiap kuis organ (17) dan kuis proses (6) berisi 5 soal yang bisa dimainkan", notes.Count == 0, string.Join("; ", notes.Take(6)));

            // jawaban benar tidak selalu di posisi A (pilihan diacak)
            int notFirst = 0, total = 0;
            for (int k = 0; k < 6; k++)
                foreach (var q in QuizSession.StartOrgan("jantung").questions)
                    if (q.type == Organo.Data.QuizType.PilihanGanda3D && q.options.Any(o => !int.TryParse(o, out _))) { total++; if (q.correct != 0) notFirst++; }
            Report("pilihan jawaban diacak (jawaban benar tidak selalu A)", total > 0 && notFirst > 0, notFirst + "/" + total + " bukan A");

            var darah = QuizSession.StartProcess("darah");
            Report("kuis proses yang diketuk memulai kuisnya sendiri", darah.processId == "darah" && darah.title == "Peredaran Darah");

            var s = QuizSession.StartProcess("ekskresi");
            App.Go<QuizPlayScreen>(s, false);
            s.Next(App, 0); s.Next(App, 0);
            Report("tombol lanjut terketuk dua kali: soal berikutnya tidak terlewati", s.index == 1, "sekarang soal ke-" + (s.index + 1));
        }

        /// <summary>
        /// Kuis terkunci: kuis organ pertama terbuka; kuis lain terbuka setelah kartunya di-scan atau kuis sebelumnya selesai;
        /// kuis proses terbuka setelah semua kuis organnya selesai.
        /// </summary>
        static void TestQuizLocks()
        {
            var keep = App.Progress;
            var p = new Organo.Data.PlayerProgress { gender = Organo.Data.Gender.Boy };
            p.cards.Clear(); p.history.Clear(); p.doneTopics.Clear(); p.season1Done = 0; p.warmupDone = 0;
            bool Open(string id) => Organo.Data.QuizAccess.OrganUnlocked(p, id);
            Report("awal: hanya kuis organ pertama (Mulut) yang terbuka", Open("mulut") && !Open("kerongkongan") && !Open("lambung") && !Open("rangka"));
            p.cards.Add("lambung");
            Report("scan kartu Lambung membuka Kuis Lambung (walau kuis sebelumnya belum selesai)", Open("lambung") && !Open("kerongkongan"));
            p.doneTopics.Add(Organo.Data.QuizBank.OrganQuizId("mulut"));
            Report("Kuis Mulut selesai membuka Kuis Kerongkongan", Open("kerongkongan") && !Open("usushalus"));
            var cerna = Organo.Data.QuizBank.GetProcess("cerna");
            Report("kuis proses Pencernaan masih terkunci sebelum semua kuis organnya selesai", !Organo.Data.QuizAccess.ProcessUnlocked(p, cerna)
                && Organo.Data.QuizAccess.ProcessMissing(p, cerna).Count == cerna.organs.Length - 1);
            foreach (var o in cerna.organs) if (!p.doneTopics.Contains("organ:" + o)) p.doneTopics.Add("organ:" + o);
            Report("semua kuis organ pencernaan selesai: Proses Pencernaan terbuka", Organo.Data.QuizAccess.ProcessUnlocked(p, cerna)
                && Organo.Data.QuizAccess.Next(p).process == cerna);
            Report("organ reproduksi gender lain tetap terkunci sampai kuis reproduksi sendiri selesai", !Open(Organo.Data.ReproAccess.Girl));

            // kuis yang baru terbuka ditampilkan di halaman hasil
            var s = QuizSession.StartOrgan("hidung");
            int before = keep.history.Count;
            PlayThrough(s);
            Report("kuis organ selesai: tercatat sekali, ditandai selesai, dan kuis berikutnya terbuka",
                keep.history.Count == before + 1 && Organo.Data.QuizAccess.OrganDone(keep, "hidung") && keep.history[0].quizId == "organ:hidung"
                && s.opened.Contains("organ:tenggorokan") && App.Current is QuizResultScreen,
                "terbuka: " + string.Join(", ", s.opened));
            var again = QuizSession.Restart(keep.history[0]);
            Report("Ulangi kuis dari riwayat memulai kuis yang sama", again != null && again.organId == "hidung");
        }

        /// <summary>Tempel label: yang salah kembali ke bawah dan harus dipindah sampai semua tepat; yang tepat terkunci.</summary>
        static void TestLabelRetry()
        {
            var s = QuizSession.Current;
            var root = App.Current?.Root;
            if (s?.Q == null || s.Q.type != Organo.Data.QuizType.TempelLabel || root == null) { Report("soal tempel label terbuka", false); return; }
            var q = s.Q;
            var screen = App.Current as QuizPlayScreen;
            // tempel organ pertama & kedua tertukar, sisanya tepat
            var placed = (Dictionary<string, string>)typeof(QuizPlayScreen).GetField("m_Placed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(screen);
            placed.Clear();
            for (int i = 0; i < q.labelOrgans.Length; i++) placed[q.labelOrgans[i]] = q.labelOrgans[i];
            placed[q.labelOrgans[0]] = q.labelOrgans[1];
            placed[q.labelOrgans[1]] = q.labelOrgans[0];
            screen.GetType().GetMethod("Refresh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(screen, new object[] { "keep" });
            int answered = s.answers.Count;
            Later(() =>
            {
                Label Find(string text) { Label f = null; App.Current.Root.Query<Label>().ForEach(l => { if (f == null && l.text == text) f = l; }); return f; }
                var check = Find("Periksa Jawaban");
                if (check != null) Tap(check.parent.worldBound.center);
                Later(() =>
                {
                    bool stillHere = s.index == 0 && Find("Soal Berikutnya") == null && placed.Count == q.labelOrgans.Length - 2;
                    Report("tempel label salah: tidak bisa lanjut, organ yang salah kembali ke bawah", stillHere && s.answers.Count == answered + 1 && !s.answers[s.answers.Count - 1].isCorrect,
                        placed.Count + " organ tetap tertempel");
                    placed[q.labelOrgans[0]] = q.labelOrgans[0];
                    placed[q.labelOrgans[1]] = q.labelOrgans[1];
                    screen.GetType().GetMethod("Refresh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(screen, new object[] { "keep" });
                    Later(() =>
                    {
                        var check2 = Find("Periksa Jawaban");
                        if (check2 != null) Tap(check2.parent.worldBound.center);
                        Later(() => Report("setelah semua tepat: boleh lanjut (jawaban pertama tetap tercatat salah)",
                            Find("Soal Berikutnya") != null && s.answers.Count == answered + 1), 250);
                    }, 200);
                }, 250);
            }, 200);
        }

        /// <summary>Misi scan: beberapa kartu salah bergantian di depan kamera, kabarnya tetap satu dan tidak dibuat ulang.</summary>
        static void TestScanWrongSteady()
        {
            var scr = App.Current as QuizScanScreen;
            if (scr == null) { Report("misi scan terbuka", false); return; }
            var on = typeof(QuizScanScreen).GetMethod("OnCard", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            on.Invoke(scr, new object[] { App.Database.Get("paru") });
            var first = scr.Root.Query<Label>().Where(l => l.text != null && l.text.StartsWith("Itu kartu")).First();
            var toast = first?.parent?.parent;
            on.Invoke(scr, new object[] { App.Database.Get("ginjal") });
            on.Invoke(scr, new object[] { App.Database.Get("paru") });
            on.Invoke(scr, new object[] { App.Database.Get("jantung") });
            int toasts = 0;
            scr.Root.Query<Label>().ForEach(l => { if (l.text != null && l.text.StartsWith("Itu kartu")) toasts++; });
            Report("misi scan: kartu salah bergantian tidak membuat kabar berkedip (satu kabar, tetap di tempat)",
                first != null && toasts == 1 && toast != null && toast.panel != null && first.text == "Itu kartu Paru-paru", (first?.text ?? "-") + ", " + toasts + " kabar");
            Report("misi scan: model AR tidak ditempel & kartu yang dicari didahulukan", App.ar == null || (!App.ar.SpawnModels && App.ar.PreferOrgan == "mulut"));
        }

        /// <summary>Bagian organ yang disorot di halaman organ tidak ikut terbawa ke soal kuis (organ sama, layar berbeda).</summary>
        static void TestHighlightNotCarried()
        {
            var v = App.Viewer;
            bool before = v != null && v.PartHighlighted;
            var s = QuizSession.StartOrgan("lambung"); // "Organ apa ini?" (lambung)
            App.Go<QuizPlayScreen>(s, false);
            Later(() => Report("sorotan bagian organ dari halaman organ tidak terbawa ke soal kuis",
                before && v != null && !v.PartHighlighted && v.Organ != null && v.Organ.id == "lambung",
                "sebelum " + before + ", sesudah " + (v != null && v.PartHighlighted)), 300);
        }

        /// <summary>Memilih & memeriksa jawaban hanya mengganti warna: model 3D dan gambarnya tidak dibuat ulang.</summary>
        static void TestQuizTapInPlace()
        {
            var root = App.Current?.Root;
            var vp = root?.Q<Organo.ThreeD.OrganViewport>();
            var s = QuizSession.Current;
            if (vp == null || s?.Q?.options == null) { Report("soal pilihan ganda 3D terbuka", false); return; }
            var q = s.Q;
            var tex = App.Viewer.Texture;
            Label Find(string text) { Label f = null; App.Current.Root.Query<Label>().ForEach(l => { if (f == null && l.text == text) f = l; }); return f; }
            bool Same() => App.Current.Root.Q<Organo.ThreeD.OrganViewport>() == vp && vp.panel != null && App.Viewer.Texture == tex;
            var wrong = Find(q.options[(q.correct + 1) % q.options.Length]);
            if (wrong == null) { Report("pilihan jawaban ditemukan", false); return; }
            Tap(wrong.parent.worldBound.center);
            Later(() =>
            {
                bool afterPick = Same();
                // periksa jawaban salah: belum boleh lanjut
                var check0 = Find("Periksa Jawaban");
                if (check0 != null) Tap(check0.parent.worldBound.center);
                Later(() =>
                {
                    Report("jawaban salah: muncul \"coba lagi\" dan belum ada tombol lanjut", Find("Belum tepat. Ayo, coba lagi!") != null && Find("Soal Berikutnya") == null);
                    var right = Find(q.options[q.correct]);
                    if (right != null) Tap(right.parent.worldBound.center);
                }, 250);
                Later(() =>
                {
                    var check = Find("Periksa Jawaban");
                    if (check != null) Tap(check.parent.worldBound.center);
                    Later(() =>
                    {
                        bool afterCheck = Same();
                        Report("memilih jawaban kuis tidak membuat ulang model 3D (tidak berkedip)", afterPick && afterCheck, "setelah pilih " + afterPick + ", setelah periksa " + afterCheck);
                        Report("periksa jawaban: koreksi & tombol lanjut muncul di tempat", Find("Benar! Hebat!") != null && Find("Soal Berikutnya") != null);
                        Report("jawaban salah dulu: tidak bisa lanjut sampai benar, percobaan pertama tercatat salah",
                            s.answers.Count > 0 && !s.answers[s.answers.Count - 1].isCorrect && s.Q == q);
                    }, 250);
                }, 550);
            }, 200);
        }

        /// <summary>Memilih "Tengkorak" mendekatkan kamera ke tengkorak (rangka setinggi badan, bukan kecil di atas).</summary>
        static void TestSkullFocus()
        {
            var v = App.Viewer;
            bool ok = v != null && v.PartHighlighted && v.Mode == Organo.ThreeD.OrganViewer3D.ViewMode.Organ && v.TryProject(0, out var uv, out _)
                      && uv.x > 0.25f && uv.x < 0.75f && uv.y > 0.1f && uv.y < 0.75f;
            Report("memilih Tengkorak: tengkorak didekati & berada di tengah kartu 3D", ok);
        }

        /// <summary>Tampilan "Dalam" rangka berisi tiga jenis tulang (pipa, pipih, pendek) yang bisa disorot.</summary>
        static void TestBoneInside()
        {
            var v = App.Viewer;
            var organ = App.Database.Get("rangka");
            var mr = organ != null && organ.insidePrefab != null ? organ.insidePrefab.GetComponentInChildren<MeshRenderer>() : null;
            int mats = mr != null ? mr.sharedMaterials.Length : 0;
            Report("model dalam rangka berisi tulang pipa, pipih & pendek (12 lapisan)", mats == 12, mats + " material");
            Report("bagian Tulang pipih disorot di tampilan Dalam", v != null && v.Mode == Organo.ThreeD.OrganViewer3D.ViewMode.Inside && v.PartHighlighted);
        }

        /// <summary>
        /// Zoom mengarah ke titik yang dicubit: tengkorak tetap di bawah jari (dulu zoom selalu ke tengah badan, tengkorak keluar
        /// layar). Saat diperbesar tombol di atas model menyingkir, "Lihat utuh" mengembalikan tampilan.
        /// </summary>
        static void TestZoomAtPoint()
        {
            var v = App.Viewer;
            if (v == null || !(App.Current is OrganDetailScreen)) { Report("halaman rangka terbuka", false); return; }
            v.ResetView();
            Label Find(string text) { Label f = null; App.Current.Root.Query<Label>().ForEach(l => { if (f == null && l.text == text) f = l; }); return f; }
            Later(() =>
            {
                if (!v.TryProject(0, out var before, out _)) { Report("titik tengkorak terlihat", false); return; }
                v.ZoomAt(2.8f, before);
                Later(() =>
                {
                    bool held = v.TryProject(0, out var after, out _) && (after - before).magnitude < 0.08f;
                    var organBtn = Find("Organ");
                    var reset = Find("Lihat utuh");
                    bool hidden = organBtn != null && organBtn.resolvedStyle.visibility == Visibility.Hidden;
                    bool shown = reset != null && reset.parent.resolvedStyle.display == DisplayStyle.Flex;
                    Report("zoom ke arah tengkorak: tengkorak tetap di bawah jari (bukan ke tengah badan)", held && v.IsZoomed,
                        "sebelum " + before.ToString("0.00") + " sesudah " + after.ToString("0.00"));
                    Report("saat diperbesar tombol di atas model menyingkir & ada tombol Lihat utuh", hidden && shown);
                    if (reset != null) Tap(reset.parent.worldBound.center);
                    Later(() => Report("Lihat utuh mengembalikan tampilan & tombolnya",
                        !v.IsZoomed && organBtn != null && organBtn.resolvedStyle.visibility == Visibility.Visible), 400);
                }, 900);
            }, 300);
        }

        static void TestCardSwipe()
        {
            var viewer = App.Current as CardViewerScreen;
            var h = App.Current.Root.Q("card-holder");
            if (viewer == null || h == null) { Report("kartu koleksi ditemukan", false); return; }
            string before = viewer.Current?.id;
            var c = h.worldBound.center;
            Drag(c + new Vector2(80, 0), c + new Vector2(-120, 4));
            Later(() => Report("kartu koleksi digeser ke kiri -> kartu berikutnya", viewer.Current != null && viewer.Current.id != before, before + " -> " + viewer.Current?.id), 400);
        }

        static void TestCardFlip()
        {
            var viewer = App.Current as CardViewerScreen;
            var h = App.Current.Root.Q("card-holder");
            if (viewer == null || h == null) { Report("kartu koleksi ditemukan", false); return; }
            Tap(h.worldBound.center);
            Later(() => Report("kartu koleksi diketuk -> dibalik ke sisi belakang", viewer.ShowingBack), 400);
        }

        static void TestTapCard()
        {
            var page = FindScroll(false);
            if (page != null) page.scrollOffset = Vector2.zero;
            var rail = FindScroll(true);
            if (rail == null) return;
            rail.scrollOffset = Vector2.zero;
            Later(() =>
            {
                var card = rail.contentContainer.childCount > 1 ? rail.contentContainer[1] : null;
                if (card == null) { Report("kartu bagian ditemukan", false); return; }
                Tap(card.worldBound.center);
                Later(() =>
                {
                    bool panel = false;
                    App.Current.Root.Query<Label>().ForEach(l => { if (l.text == "Bagian berikutnya") panel = true; });
                    Report("ketuk kartu bagian tetap berfungsi (tidak dianggap geser)", panel);
                }, 300);
            }, 150);
        }

        static void TestQuizDrag()
        {
            var root = App.Current?.Root;
            var sources = root?.Query(className: "drag-source").ToList();
            if (sources == null || sources.Count == 0) { Report("kartu kuis yang bisa diseret ditemukan", false); return; }
            int before = sources.Count;
            VisualElement target = null;
            root.Query<VisualElement>().ForEach(v =>
            {
                if (target == null && Mathf.Approximately(v.resolvedStyle.width, 44f) && Mathf.Approximately(v.resolvedStyle.height, 44f)
                    && v.childCount > 0 && v[0] is Label l && l.text == "?") target = v;
            });
            if (target == null) { Report("titik tujuan kuis ditemukan", false); return; }
            Drag(sources[0].worldBound.center, target.worldBound.center, 14);
            Later(() =>
            {
                int after = App.Current.Root.Query(className: "drag-source").ToList().Count;
                Report("organ diseret & dilepas ke tubuh (drag & drop)", after == before - 1, before + " -> " + after);
            }, 300);
        }

        static void TestLibrarySwipe()
        {
            var root = App.Current?.Root;
            var grip = root?.Q(className: "sheet__grip");
            if (grip == null) { Report("sheet area tubuh ditemukan", false); return; }
            var from = grip.worldBound.center + new Vector2(0, 30);
            Drag(from, from + new Vector2(0, 240));
            Later(() =>
            {
                bool back = App.Current is LibraryBodyScreen lib && lib.Region == null;
                Report("sheet area tubuh ditarik ke bawah -> kembali ke seluruh tubuh", back);
            }, 400);
        }

        static int OpenSheets()
        {
            int n = 0;
            UiRoot.Query(className: "sheet").ForEach(v => { if (!v.ClassListContains("sheet--hidden")) n++; });
            return n;
        }

        /// <summary>Selesai onboarding langsung ke kenalan (nama & gender), tidak ada halaman Masuk/Daftar.</summary>
        static void TestOnboardingEnd()
        {
            Label btn = null;
            App.Current?.Root.Query<Label>().ForEach(l => { if (btn == null && l.text == "Mulai Sekarang") btn = l; });
            if (btn == null) { Report("tombol Mulai Sekarang ada", false); return; }
            Tap(btn.parent.worldBound.center);
            Later(() => Report("onboarding selesai langsung ke kenalan (tanpa Masuk/Daftar)", App.Current is GenderScreen && App.Progress.onboarded), 300);
        }

        /// <summary>Progres tersimpan di HP tanpa akun, dan progres dari versi yang masih memakai akun ikut pindah.</summary>
        static void TestOfflineSave()
        {
            var p = App.Progress;
            int stars = p.stars;
            p.stars = stars + 7;
            p.Save();
            Report("progres tersimpan di HP tanpa akun (dimuat ulang)", Organo.Data.PlayerProgress.Load().stars == stars + 7);
            p.stars = stars;
            p.Save();

            string mine = PlayerPrefs.GetString("organo.progress.local");
            PlayerPrefs.DeleteKey("organo.progress.local");
            PlayerPrefs.SetString("organo.accounts.v1", "{\"accounts\":[{\"id\":\"lama1\"},{\"id\":\"lama2\"}],\"current\":\"lama2\"}");
            PlayerPrefs.SetString("organo.progress.lama2", "{\"onboarded\":true,\"loggedIn\":true,\"nickname\":\"Lama\",\"stars\":42,\"gender\":2}");
            var moved = Organo.Data.PlayerProgress.Load();
            Report("progres dari versi berakun pindah ke HP ini (profil terakhir dipakai)",
                moved.stars == 42 && moved.nickname == "Lama" && moved.gender == Organo.Data.Gender.Girl && moved.onboarded && PlayerPrefs.HasKey("organo.progress.local"),
                moved.nickname + ", " + moved.stars + " bintang");
            PlayerPrefs.DeleteKey("organo.accounts.v1");
            PlayerPrefs.DeleteKey("organo.progress.lama2");
            PlayerPrefs.SetString("organo.progress.local", mine);
        }

        static void TestAudio()
        {
            var src = App.GetComponents<AudioSource>();
            Report("efek suara benar/salah tersedia", src.Length >= 2);
            foreach (var s in src) s.Stop();
            OrganoAudio.Click();
            Report("tombol diketuk tanpa bunyi klik", !OrganoAudio.UiPlaying);
            OrganoAudio.Success(); OrganoAudio.Wrong();
            Report("status suara narasi di Editor", OrganoSpeech.Status == OrganoSpeech.State.NotSupported, OrganoSpeech.Status.ToString());
        }

        /// <summary>Ketuk model tidak membacakan; speaker mematikan / menyalakan suara pembaca (tersimpan).</summary>
        static void TestNarration()
        {
            var scr = App.Current as OrganDetailScreen;
            var vp = App.Current?.Root.Q<Organo.ThreeD.OrganViewport>();
            if (scr == null || vp == null) { Report("halaman organ terbuka", false); return; }
            Report("halaman organ tetap memakai putaran biasa (bukan putar bebas hasil scan)", !App.Viewer.FreeRotate);
            var page = FindScroll(false);
            if (page != null) page.scrollOffset = Vector2.zero;
            Later(() =>
            {
                Tap(vp.worldBound.center);
                Later(() =>
                {
                    Report("ketuk model 3D tidak membacakan", !scr.VoicePlaying);
                    VisualElement toggle = null;
                    App.Current.Root.Query<VisualElement>().ForEach(e => { if (toggle == null && e.tooltip == "Matikan suara pembaca") toggle = e; });
                    if (toggle == null) { Report("tombol suara pembaca ada", false); return; }
                    page?.ScrollTo(toggle);
                    Later(() =>
                    {
                        Tap(toggle.worldBound.center);
                        Later(() =>
                        {
                            scr.ToggleVoice();
                            bool muted = !App.Progress.narration && !OrganoAudio.NarrationEnabled && !scr.VoicePlaying && !OrganoAudio.SpeakText("uji");
                            Report("suara pembaca dimatikan: tidak ada yang dibacakan", muted);
                            Tap(toggle.worldBound.center);
                            Later(() => Report("suara pembaca dinyalakan lagi", App.Progress.narration && OrganoAudio.NarrationEnabled), 250);
                        }, 250);
                    }, 250);
                }, 300);
            }, 200);
        }

        /// <summary>Model AR (XR Simulation) bisa diputar ke atas/bawah sampai satu putaran penuh.</summary>
        static void TestArRotate()
        {
            var ar = App.ar;
            var m = ar != null ? ar.ModelTransform : null;
            if (m == null || !ar.ModelVisible) { Report("model AR ada untuk diputar", false); return; }
            var start = m.rotation;
            ar.RotateSpawned(new Vector2(0f, 360f)); // 0,5° per titik: 180°
            Later(() =>
            {
                float half = Quaternion.Angle(start, m.rotation);
                ar.RotateSpawned(new Vector2(0f, 360f)); // total 360°
                Later(() =>
                {
                    float full = Quaternion.Angle(start, m.rotation);
                    Report("model AR bisa diputar ke atas/bawah 360° (kembali ke arah awal)", half > 150f && full < 10f,
                        "180°: " + half.ToString("0") + "°, 360°: " + full.ToString("0") + "°");
                }, 700);
            }, 700);
        }

        static void TestOverlaySwipe()
        {
            VisualElement panel = null;
            UiRoot.Query(className: "sheet").ForEach(v => { if (!v.ClassListContains("sheet--hidden")) panel = v; });
            if (panel == null) { Report("overlay sheet ditemukan", false); return; }
            var from = panel.worldBound.center;
            Drag(from, from + new Vector2(0, 260));
            Later(() => Report("overlay sheet ditarik ke bawah -> tertutup", OpenSheets() == 0), 500);
        }

        static void Scroll(float y)
        {
            var sv = App.Current?.Root.Q<ScrollView>(className: "unity-scroll-view");
            if (sv == null) return;
            // ambil ScrollView vertikal (bukan rail horizontal)
            App.Current.Root.Query<ScrollView>().ForEach(v => { if (v.mode != ScrollViewMode.Horizontal && sv.mode == ScrollViewMode.Horizontal) sv = v; });
            sv.schedule.Execute(() => sv.scrollOffset = new Vector2(0, y)).ExecuteLater(250);
        }

        static VisualElement s_CardLayer;

        static void ShowCard(Organo.Data.OrganInfo o, bool cartoon)
        {
            s_CardLayer?.RemoveFromHierarchy();
            var root = App.GetComponent<UIDocument>().rootVisualElement;
            s_CardLayer = new VisualElement();
            s_CardLayer.style.position = Position.Absolute;
            s_CardLayer.style.left = 0; s_CardLayer.style.top = 0; s_CardLayer.style.right = 0; s_CardLayer.style.bottom = 0;
            s_CardLayer.style.backgroundColor = Color.white;
            s_CardLayer.Add(cartoon ? OrganoCards.CardArtCartoon(o) : OrganoCards.CardArt(o));
            root.Add(s_CardLayer);
        }

        static void SaveCard(string id, string dir)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = s_RT;
            float scale = s_RT.width / 390f;
            int w = Mathf.RoundToInt(OrganoCards.W * scale), h = Mathf.RoundToInt(OrganoCards.H * scale);
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, s_RT.height - h, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(dir + "/" + id + ".png", tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log("[Organo] Kartu dirender: " + id);
        }

        static void Capture(string name)
        {
            if (name.EndsWith("_cek")) return; // langkah uji tata letak tanpa gambar
            if (name.StartsWith("var_"))
            {
                // var_r_paru_3 -> Library/OrganoCardVariants/r/paru_3.png
                var style = name.Substring(4, 1);
                SaveCard(name.Substring(6), VariantDir + "/" + style);
                return;
            }
            if (name.StartsWith("card_")) { SaveCard(name.Substring(5), OrganoCards.CardDir); return; }
            if (name.StartsWith("cardk_")) { SaveCard(name.Substring(6), OrganoCards.CartoonDir); return; }
            var prev = RenderTexture.active;
            RenderTexture.active = s_RT;
            var tex = new Texture2D(s_RT.width, s_RT.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, s_RT.width, s_RT.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(OutDir, "ui_" + name + ".png"), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        static void Finish()
        {
            SessionState.SetBool(PendingKey, false);
            SessionState.SetBool("organo.snap.testsonly", false);
            SessionState.SetBool(CardsKey, false);
            SessionState.EraseString(CardsOnlyKey);
            SessionState.SetBool(VariantsKey, false);
            SessionState.SetBool(LayoutKey, false);
            OrganoApp.DebugInsets = null;
            OrganoCards.ForceVariant = -1;
            foreach (var k in SaveKeys)
            {
                var saved = SessionState.GetString(SaveBackupPrefix + k, NoValue);
                if (saved == NoValue) PlayerPrefs.DeleteKey(k);
                else if (k == "organo.onboarded") PlayerPrefs.SetInt(k, int.TryParse(saved, out var on) ? on : 0);
                else PlayerPrefs.SetString(k, saved);
            }
            PlayerPrefs.Save();
            EditorApplication.update -= Drive;
            Debug.Log("[Organo] Snapshot selesai: " + s_Steps.Count + " layar di " + OutDir);
            bool quit = SessionState.GetBool(QuitKey, false);
            EditorApplication.ExitPlaymode();
            if (quit) EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }

        /// <summary>Lembar semua ikon (untuk dibandingkan dengan SVG asli di desain).</summary>
        static void ShowIconSheet()
        {
            var root = App.GetComponent<UIDocument>().rootVisualElement;
            var sheet = new VisualElement();
            sheet.style.position = Position.Absolute;
            sheet.style.left = 0; sheet.style.top = 0; sheet.style.right = 0; sheet.style.bottom = 0;
            sheet.style.backgroundColor = Color.white;
            sheet.style.flexDirection = FlexDirection.Row;
            sheet.style.flexWrap = Wrap.Wrap;
            sheet.style.paddingLeft = 4; sheet.style.paddingTop = 4;
            foreach (var key in OrganoIcons.Svg.Keys.Where(k => k != "body_template" && k != "navbar_bg"))
            {
                var cell = new VisualElement();
                cell.style.width = 48; cell.style.height = 56; cell.style.alignItems = Align.Center;
                var bg = new VisualElement();
                bg.style.width = 40; bg.style.height = 40; bg.style.backgroundColor = key == "nav_scan" || key.StartsWith("lock_white") || key == "check" || key == "x_small" ? UI.UI.C(UI.UI.Teal) : new Color(0.96f, 0.96f, 0.97f);
                bg.style.alignItems = Align.Center; bg.style.justifyContent = Justify.Center;
                bg.Add(new SvgImage(key, 34));
                cell.Add(bg);
                var l = new Label(key);
                l.style.fontSize = 6; l.style.color = Color.black;
                cell.Add(l);
                sheet.Add(cell);
            }
            root.Add(sheet);
        }
    }
}
