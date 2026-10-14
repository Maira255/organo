using System;
using System.Collections.Generic;
using Organo.AR;
using Organo.Data;
using Organo.Screens;
using Organo.ThreeD;
using Organo.UI;
using UnityEngine;
using UnityEngine.UIElements;
using U = Organo.UI.UI;

namespace Organo
{
    /// <summary>
    /// Titik masuk aplikasi Organo. Taruh pada GameObject dengan UIDocument (dibuat otomatis oleh menu Organo &gt; Setup).
    /// Mengatur layar, navbar, popup, studio 3D close-up, dan AR.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class OrganoApp : MonoBehaviour
    {
        public static OrganoApp Instance { get; private set; }

        /// <summary>
        /// Alat pengembang (akun uji, scan demo tanpa kamera) hanya aktif di Editor dan *Development Build*,
        /// tidak pernah di build rilis yang dibagikan ke sekolah.
        /// </summary>
        public static bool DevTools =>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            true;
#else
            false;
#endif

        [Header("Konten")]
        public OrganDatabase database;
        public StyleSheet styleSheet;

        [Header("Font (Poppins)")]
        [Tooltip("Poppins Regular (400) untuk teks biasa.")]
        public Font fontRegular;
        [Tooltip("Poppins SemiBold (600) dipakai untuk teks tebal (-unity-font-style: bold).")]
        public Font fontSemiBold;
        [Tooltip("Poppins Medium (500), cadangan untuk bobot 500.")]
        public Font fontMedium;

        [Header("AR & 3D")]
        public ARScanController ar;

        [Header("Kartu")]
        [Tooltip("Alamat situs unduhan kartu untuk guru/orang tua (folder Web/kartu-guru, dionlinekan di Vercel). Kosong = tautan tidak ditampilkan.")]
        public string cardsWebsiteUrl = "https://organo-kartu.vercel.app";

        [Header("Suara (opsional)")]
        public AudioClip clickSound;
        public AudioClip successSound;
        public AudioClip wrongSound;

        [Header("Debug")]
        [Tooltip("Lewati splash/onboarding/kenalan saat Play di Editor.")]
        public bool skipIntroInEditor;

        public PlayerProgress Progress { get; private set; }
        public OrganDatabase Database => database;
        public OrganViewer3D Viewer { get; private set; }
        /// <summary>Pilihan tubuh di Library (None = ikut profil).</summary>
        public Gender BodyGender { get; set; } = Gender.None;
        public AppScreen Current => m_Current;

        UIDocument m_Doc;
        VisualElement m_Root, m_Host, m_Overlay;
        NavBar m_Nav;
        readonly Dictionary<Type, AppScreen> m_Screens = new Dictionary<Type, AppScreen>();
        readonly List<(Type type, object arg)> m_History = new List<(Type, object)>();
        AppScreen m_Current;
        object m_CurrentArg;
        Rect m_LastSafeArea;
        Label m_Toast;
        IVisualElementScheduledItem m_ToastHide;

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            UnityEngine.Screen.sleepTimeout = SleepTimeout.NeverSleep;

            if (database == null) database = OrganDatabase.CreateDefault();
            database.EnsureComplete();
            Progress = PlayerProgress.Load();
            OrganoAudio.Init(gameObject, clickSound, successSound, wrongSound);
            OrganoAudio.Enabled = Progress.sound;
            OrganoAudio.NarrationEnabled = Progress.narration;
            OrganoAudio.SetVoiceSpeed(Progress.voiceSpeed);

            Viewer = OrganViewer3D.Create();
            Viewer.DatabaseProvider = () => database;
            Viewer.BodyPrefabProvider = () =>
            {
                var g = BodyGender != Gender.None ? BodyGender : Progress.gender;
                return g == Gender.Girl && database.bodyGirlPrefab != null ? database.bodyGirlPrefab : database.bodyBoyPrefab;
            };
            if (Camera.main != null) Camera.main.cullingMask &= ~(1 << OrganViewer3D.Layer);
            if (ar == null) ar = FindAnyObjectByType<ARScanController>();
            if (ar != null) ar.Init(database);
        }

        bool m_Started;

        void Start()
        {
            m_Started = true;
            Boot();
        }

        void OnEnable()
        {
            // UIDocument membuat ulang root saat diaktifkan kembali.
            if (m_Started) Boot();
        }

        void Boot()
        {
            m_Doc = GetComponent<UIDocument>();
            if (m_Doc.rootVisualElement == null) return;
            BuildShell();
            if (m_Current == null)
            {
#if UNITY_EDITOR
                if (skipIntroInEditor) { Progress.onboarded = true; if (Progress.gender == Gender.None) Progress.gender = Gender.Boy; }
#endif
                // sudah pernah dibuka sampai Beranda: langsung ke Beranda (splash, onboarding & kenalan hanya sekali)
                if (Progress.onboarded && Progress.gender != Gender.None) Go<HomeScreen>(push: false);
                else Go<SplashScreen>(push: false);
            }
            else ShowScreen(m_Current, m_CurrentArg);
        }

        void BuildShell()
        {
            m_Root = m_Doc.rootVisualElement;
            m_Root.Clear();
            if (styleSheet != null && !m_Root.styleSheets.Contains(styleSheet)) m_Root.styleSheets.Add(styleSheet);
            m_Root.AddToClassList("organo-root");
            ApplyFonts();
            m_Root.style.flexGrow = 1;

            m_Host = U.Div("screen-host");
            m_Root.Add(m_Host);
            m_Nav = new NavBar(this);
            m_Root.Add(m_Nav);
            m_Overlay = U.Div("fill");
            m_Overlay.pickingMode = PickingMode.Ignore;
            m_Root.Add(m_Overlay);

            // tidak bisa diketuk: toast yang sudah pudar (opacity 0) tetap menutupi tombol di bawahnya
            m_Toast = new Label { pickingMode = PickingMode.Ignore };
            var toastBox = U.Div("toast");
            toastBox.pickingMode = PickingMode.Ignore;
            toastBox.Add(m_Toast);
            m_Root.Add(toastBox);
            m_LastSafeArea = default;
            m_TopInset = m_BottomInset = -1f; // paksa area aman diterapkan ulang ke tampilan baru
            ApplyFontScale();
        }

        static UnityEngine.TextCore.Text.FontAsset s_FontAsset;

        /// <summary>Poppins: Regular untuk teks biasa, SemiBold untuk bold (sesuai style guide: 400/600).</summary>
        void ApplyFonts()
        {
            if (fontRegular == null) return;
            if (s_FontAsset == null)
            {
                s_FontAsset = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(fontRegular);
                if (s_FontAsset == null) return;
                var table = s_FontAsset.fontWeightTable;
                if (fontMedium != null)
                {
                    var medium = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(fontMedium);
                    if (medium != null && table.Length > 5) table[5].regularTypeface = medium;
                }
                if (fontSemiBold != null)
                {
                    var semi = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(fontSemiBold);
                    if (semi != null)
                    {
                        if (table.Length > 6) table[6].regularTypeface = semi;
                        if (table.Length > 7) table[7].regularTypeface = semi;
                    }
                }
            }
            m_Root.style.unityFontDefinition = FontDefinition.FromSDFFont(s_FontAsset);
        }

        // ------------------------------------------------------------ navigation

        public T GetScreen<T>() where T : AppScreen, new()
        {
            if (!m_Screens.TryGetValue(typeof(T), out var s))
            {
                s = new T();
                s.Init(this);
                m_Screens[typeof(T)] = s;
            }
            return (T)s;
        }

        /// <summary>Buka layar. push=false mengganti layar sekarang (tidak bisa kembali ke sana).</summary>
        public void Go<T>(object arg = null, bool push = true) where T : AppScreen, new()
        {
            var next = GetScreen<T>();
            if (m_Current != null && push) m_History.Add((m_Current.GetType(), m_CurrentArg));
            ShowScreen(next, arg);
        }

        /// <summary>Buka tab utama: riwayat dikosongkan supaya Kembali tidak berputar-putar.</summary>
        public void GoTab(NavTab tab)
        {
            m_History.Clear();
            switch (tab)
            {
                case NavTab.Beranda: Go<HomeScreen>(push: false); break;
                case NavTab.Library: Go<LibraryBodyScreen>(push: false); break;
                case NavTab.Scan: Go<ScanScreen>(push: false); break;
                case NavTab.Quiz: Go<QuizHubScreen>(push: false); break;
                case NavTab.Profil: Go<ProfileScreen>(push: false); break;
            }
        }

        public void Back()
        {
            if (CloseTopOverlay()) return;
            if (m_Current != null && m_Current.OnBack()) return;
            if (m_History.Count == 0)
            {
                if (!(m_Current is HomeScreen) && Progress.onboarded) GoTab(NavTab.Beranda);
                return;
            }
            var (type, arg) = m_History[m_History.Count - 1];
            m_History.RemoveAt(m_History.Count - 1);
            if (!m_Screens.TryGetValue(type, out var s)) return;
            ShowScreen(s, arg);
        }

        /// <summary>Kembali ke layar sebelumnya bila jenisnya T, dengan argumen baru. False bila bukan.</summary>
        public bool BackTo<T>(object arg) where T : AppScreen
        {
            if (m_History.Count == 0 || m_History[m_History.Count - 1].Item1 != typeof(T)) return false;
            var type = m_History[m_History.Count - 1].Item1;
            m_History.RemoveAt(m_History.Count - 1);
            if (!m_Screens.TryGetValue(type, out var s)) return false;
            ShowScreen(s, arg);
            return true;
        }

        public void ClearHistory() => m_History.Clear();

        void ShowScreen(AppScreen next, object arg)
        {
            CloseAllOverlays();
            if (m_Current != null)
            {
                m_Current.Close();
                m_Current.Root.RemoveFromHierarchy();
            }
            m_Current = next;
            m_CurrentArg = arg;

            bool camera = next.UsesCamera;
            m_Root.EnableInClassList("ar-mode", camera);
            if (ar != null)
            {
                if (camera && !ar.IsRunning) ar.Begin();
                else if (!camera && ar.IsRunning) ar.End();
            }

            next.Open(arg);
            next.Root.AddToClassList("screen--enter");
            m_Host.Add(next.Root);
            next.Root.schedule.Execute(() => next.Root.RemoveFromClassList("screen--enter")).ExecuteLater(16);
            m_Nav.SetActive(next.Tab);
            ApplyInsets();
        }

        // ------------------------------------------------------------ overlays

        public class Overlay
        {
            internal VisualElement root, scrim, panel;
            internal bool dismissable;
            internal Action onClose;
            public bool IsOpen => root != null && root.parent != null;
            public void Close() => Instance.CloseOverlay(this);
        }

        readonly List<Overlay> m_Overlays = new List<Overlay>();

        /// <summary>Panel bawah (bottom sheet) seperti di desain.</summary>
        public Overlay ShowSheet(VisualElement content, bool dismissable = true, Action onClose = null, bool grip = true)
        {
            var o = new Overlay { dismissable = dismissable, onClose = onClose };
            o.root = U.Div("overlay");
            o.scrim = U.Div("scrim", "scrim--hidden");
            if (dismissable) o.scrim.Tap(() => CloseOverlay(o));
            o.panel = U.Col(0, "sheet", "sheet--hidden");
            if (grip) o.panel.Add(U.Div("sheet__grip"));
            o.panel.Add(content);
            // tarik ke bawah untuk menutup
            if (dismissable) o.panel.AddManipulator(new SwipeToDismiss(() => CloseOverlay(o)));
            o.root.Add(o.scrim);
            o.root.Add(o.panel);
            Open(o);
            return o;
        }

        /// <summary>Dialog di tengah layar (konfirmasi keluar, izin kamera).</summary>
        public Overlay ShowDialog(VisualElement content, bool dismissable = false, Action onClose = null)
        {
            var o = new Overlay { dismissable = dismissable, onClose = onClose };
            o.root = U.Div("overlay", "dialog-wrap");
            o.scrim = U.Div("scrim", "scrim--hidden");
            if (dismissable) o.scrim.Tap(() => CloseOverlay(o));
            o.panel = U.Col(0, "dialog");
            o.panel.style.opacity = 0;
            o.panel.Add(content);
            o.root.Add(o.scrim);
            o.root.Add(o.panel);
            Open(o);
            return o;
        }

        void Open(Overlay o)
        {
            m_Overlay.pickingMode = PickingMode.Position;
            m_Overlay.Add(o.root);
            m_Overlays.Add(o);
            o.root.schedule.Execute(() =>
            {
                o.scrim.RemoveFromClassList("scrim--hidden");
                o.panel.RemoveFromClassList("sheet--hidden");
                o.panel.style.opacity = 1;
            }).ExecuteLater(16);
        }

        public void CloseOverlay(Overlay o)
        {
            if (o == null || !m_Overlays.Remove(o)) return;
            o.scrim.AddToClassList("scrim--hidden");
            o.panel.AddToClassList("sheet--hidden");
            o.panel.style.opacity = o.panel.ClassListContains("dialog") ? 0 : 1;
            o.root.pickingMode = PickingMode.Ignore;
            o.root.schedule.Execute(() =>
            {
                o.root.RemoveFromHierarchy();
                if (m_Overlays.Count == 0) m_Overlay.pickingMode = PickingMode.Ignore;
            }).ExecuteLater(230);
            o.onClose?.Invoke();
        }

        public bool CloseTopOverlay()
        {
            for (int i = m_Overlays.Count - 1; i >= 0; i--)
                if (m_Overlays[i].dismissable) { CloseOverlay(m_Overlays[i]); return true; }
            return false;
        }

        void CloseAllOverlays()
        {
            foreach (var o in m_Overlays.ToArray()) { m_Overlays.Remove(o); o.root.RemoveFromHierarchy(); }
            m_Overlay.pickingMode = PickingMode.Ignore;
        }

        public void Toast(string message, float seconds = 2.2f)
        {
            m_Toast.text = message;
            var box = m_Toast.parent;
            box.AddToClassList("toast--show");
            m_ToastHide?.Pause();
            m_ToastHide = box.schedule.Execute(() => box.RemoveFromClassList("toast--show"));
            m_ToastHide.ExecuteLater((long)(seconds * 1000));
        }

        // ------------------------------------------------------------ settings

        public void ApplyFontScale()
        {
            if (m_Root == null) return;
            m_Root.EnableInClassList("text-small", Progress.textSize == 0);
            m_Root.EnableInClassList("text-large", Progress.textSize == 2);
            m_Root.EnableInClassList("reduce-motion", Progress.reduceMotion);
            m_Root.EnableInClassList("high-contrast", Progress.highContrast);
        }

        // ------------------------------------------------------------ loop

        void Update()
        {
            OrganoAudio.Pump();
            m_Current?.Tick(Time.unscaledDeltaTime);
            ApplySafeArea();
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Back();
#else
            if (Input.GetKeyDown(KeyCode.Escape)) Back();
#endif
        }

        /// <summary>
        /// Jarak minimum dari tepi layar (unit UI). Desain Organo dibuat untuk iPhone yang punya area aman (notch ±47, home bar ±34).
        /// Banyak HP Android melaporkan area aman 0 (layar penuh tanpa notch), sehingga isi menempel ke tepi atas/bawah.
        /// </summary>
        public const float MinTopInset = 28f, MinBottomInset = 14f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Uji tata letak: tiru area aman perangkat tertentu (mis. iPhone 47/34). Null = pakai Screen.safeArea.</summary>
        public static Vector2? DebugInsets;
#endif

        void ApplySafeArea()
        {
            var sa = UnityEngine.Screen.safeArea;
            if (m_Root == null || m_Root.panel == null) return;
            float panelH = m_Root.panel.visualTree.worldBound.height;
            if (panelH <= 0 || UnityEngine.Screen.height <= 0) return;
            float k = panelH / UnityEngine.Screen.height;
            float top = (UnityEngine.Screen.height - sa.yMax) * k, bottom = sa.yMin * k;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DebugInsets.HasValue) { top = DebugInsets.Value.x; bottom = DebugInsets.Value.y; }
#endif
            top = Mathf.Max(MinTopInset, top);
            bottom = Mathf.Max(MinBottomInset, bottom);
            if (Mathf.Approximately(top, m_TopInset) && Mathf.Approximately(bottom, m_BottomInset)) return;
            m_TopInset = top;
            m_BottomInset = bottom;
            ApplyInsets();
        }

        float m_TopInset, m_BottomInset;

        void ApplyInsets()
        {
            if (m_Host == null) return;
            bool nav = m_Nav.resolvedStyle.display != DisplayStyle.None && m_Nav.style.display != DisplayStyle.None;
            m_Host.style.marginTop = m_Current != null && m_Current.UsesCamera ? 0 : TopInset;
            m_Host.style.marginBottom = nav || (m_Current != null && m_Current.FullBleedBottom) ? 0 : BottomInset;
            m_Nav.style.height = 84 + BottomInset;
            m_Nav.style.paddingBottom = BottomInset;
        }

        /// <summary>Tinggi area aman atas (notch) dalam unit UI, untuk layar kamera yang memakai layar penuh.</summary>
        public float TopInset => Mathf.Max(0f, m_TopInset);
        /// <summary>Tinggi area aman bawah (home bar / minimum) dalam unit UI.</summary>
        public float BottomInset => Mathf.Max(0f, m_BottomInset);

        /// <summary>Tinggi layar tanpa area aman atas & bawah (unit UI); 0 bila tampilan belum ditata.</summary>
        public float ScreenHeight
        {
            get
            {
                if (m_Root == null || m_Root.panel == null) return 0f;
                float h = m_Root.panel.visualTree.worldBound.height;
                return h > 0 && !float.IsNaN(h) ? h - TopInset - BottomInset : 0f;
            }
        }
    }
}
