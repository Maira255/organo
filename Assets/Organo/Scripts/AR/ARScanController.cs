using System;
using System.Collections;
using System.Collections.Generic;
using Organo.Data;
using Organo.ThreeD;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Organo.AR
{
    /// <summary>
    /// Scan kartu Organo: menjalankan AR Session hanya saat halaman Scan/Misi Scan terbuka, mengenali kartu lewat
    /// ARTrackedImageManager, lalu memunculkan model organ 3D di atas kartu.
    /// </summary>
    public class ARScanController : MonoBehaviour
    {
        [Header("Komponen AR (diisi otomatis oleh Organo > Setup)")]
        public ARSession session;
        public ARCameraManager cameraManager;
        public ARCameraBackground cameraBackground;
        public ARTrackedImageManager imageManager;
        public Camera arCamera;

        [Header("Deteksi")]
        [Range(0f, 1f)] public float darkThreshold = 0.12f;

        [Header("Kamera cadangan")]
        [Tooltip("Di Editor pakai webcam laptop (kamera asli) alih-alih XR Simulation. Matikan untuk menguji AR dengan XR Simulation.")]
        public bool useWebcamInEditor = true;

        public event Action<OrganInfo> CardDetected;
        /// <summary>Organ berganti karena HP diarahkan ke kartu lain saat hasil scan terbuka (lihat <see cref="SwitchEnabled"/>).</summary>
        public event Action<OrganInfo> CardSwitched;
        /// <summary>Boleh berganti ke kartu lain yang diarahkan ke kamera (hanya saat halaman hasil scan terbuka).</summary>
        public bool SwitchEnabled { get; set; }

        OrganDatabase m_Db;
        GameObject m_Spawned;
        Transform m_SpawnNorm;
        readonly List<OrganViewer3D.Hotspot> m_SpawnHotspots = new List<OrganViewer3D.Hotspot>();
        string m_CurrentId;
        // kartu yang baru saja di-reset diabaikan sebentar (id organ → sampai kapan) supaya tidak langsung terdeteksi lagi.
        // Disimpan per kartu: dulu hanya satu kartu, sehingga dua kartu salah di depan kamera bergantian terus (misi scan).
        readonly Dictionary<string, float> m_Ignore = new Dictionary<string, float>();
        // sejak kapan tiap kartu terlacak penuh (kartu yang baru muncul didahulukan saat berganti kartu)
        readonly Dictionary<ARTrackedImage, float> m_TrackedSince = new Dictionary<ARTrackedImage, float>();

        /// <summary>Tempelkan model 3D di atas kartu (false untuk misi scan: kuis menampilkan organnya sendiri).</summary>
        public bool SpawnModels { get; set; } = true;
        /// <summary>Organ yang sedang dicari (misi scan): kalau beberapa kartu terlihat, kartu ini didahulukan.</summary>
        public string PreferOrgan { get; set; }
        /// <summary>Organ yang boleh ditampilkan modelnya (organ reproduksi yang masih terkunci tidak ditampilkan).</summary>
        public Func<OrganInfo, bool> ModelAllowed { get; set; }

        bool CanSpawn(OrganInfo organ) => SpawnModels && organ != null && (ModelAllowed == null || ModelAllowed(organ));
        bool Ignored(string organId) => organId != null && m_Ignore.TryGetValue(organId, out var until) && Time.unscaledTime < until;
        float TrackedSince(ARTrackedImage img) => img != null && m_TrackedSince.TryGetValue(img, out var t) ? t : 0f;
        float m_DarkSince = -1f;

        public bool IsRunning { get; private set; }
        public bool IsDark { get; private set; }
        public OrganInfo CurrentOrgan { get; private set; }
        public bool HasSpawnedModel => m_Spawned != null;
        public IReadOnlyList<OrganViewer3D.Hotspot> SpawnedHotspots => m_SpawnHotspots;

        public bool HasCardLibrary
        {
            get
            {
                if (imageManager == null) return false;
                var lib = imageManager.referenceLibrary;
                return lib != null && lib.count > 0;
            }
        }

        /// <summary>Kamera AR benar-benar jalan (di HP). Di Editor tanpa XR Simulation ini false.</summary>
        public bool IsCameraLive =>
            IsRunning && (ARSession.state == ARSessionState.SessionTracking || ARSession.state == ARSessionState.SessionInitializing);

        /// <summary>
        /// ARCore sudah melacak gerak HP. Kartu baru bisa dikenali setelah ini; sebelumnya ARCore masih "bersiap"
        /// dan butuh HP digerakkan pelan supaya melihat sekeliling.
        /// </summary>
        public bool TrackingReady => IsRunning && ARSession.state == ARSessionState.SessionTracking;

        // ---------------- diagnosis (lembar "Info teknis" di layar Scan & logcat)
        public int FramesReceived { get; private set; }
        public string LastSeen { get; private set; } = "-";
        public float LastSeenTime { get; private set; } = -1f;

        // ---------------- kamera cadangan (webcam / kamera biasa) bila AR tidak tersedia
        WebCamTexture m_Webcam;
        bool m_WebcamFront;
        RenderTexture m_WebcamRT, m_Probe;
        float m_ProbeTimer;
        bool m_TorchWanted;

        /// <summary>Kamera biasa sedang menyala (Editor dengan webcam, atau HP yang belum mendukung AR).</summary>
        public bool UsingWebcam => m_Webcam != null && m_Webcam.isPlaying;
        /// <summary>Gambar kamera biasa untuk latar UI (null kalau tidak dipakai).</summary>
        public RenderTexture WebcamImage => UsingWebcam && m_Webcam.width > 16 ? m_WebcamRT : null;
        public int WebcamRotation => m_Webcam != null ? m_Webcam.videoRotationAngle : 0;
        public bool WebcamMirrored => m_WebcamFront;
        public bool WebcamFlipped => m_Webcam != null && m_Webcam.videoVerticallyMirrored;
        /// <summary>HP ini tidak mendukung AR (atau ARCore gagal dipasang): kamera biasa dipakai.</summary>
        public bool ArUnsupported { get; private set; }
        /// <summary>Tidak ada kamera sama sekali.</summary>
        public bool NoCamera { get; private set; }

        public bool TorchSupported
        {
            get
            {
                if (UsingWebcam) return NativeTorch.Supported;
                try { return cameraManager != null && cameraManager.subsystem != null && cameraManager.DoesCurrentCameraSupportTorch(); }
                catch { return false; }
            }
        }

        /// <summary>Senter benar-benar menyala (dibaca dari kamera, bukan dari tombol).</summary>
        public bool TorchOn
        {
            get
            {
                if (UsingWebcam) return NativeTorch.IsOn;
                try { return cameraManager != null && cameraManager.subsystem != null && cameraManager.currentCameraTorchMode == XRCameraTorchMode.On; }
                catch { return false; }
            }
        }

        public void Init(OrganDatabase db)
        {
            m_Db = db;
            // estimasi cahaya dibutuhkan untuk peringatan "ruangan terlalu gelap"
            if (cameraManager != null) cameraManager.requestedLightEstimation = LightEstimation.AmbientIntensity;
            if (imageManager != null) imageManager.trackablesChanged.AddListener(OnTrackablesChanged);
            if (cameraManager != null) cameraManager.frameReceived += OnFrame;
            // model AR melayang di atas kartu tanpa lantai penerima bayangan: bayangan hanya menambah kerja GPU (HP panas)
            if (arCamera != null && arCamera.TryGetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(out var urp))
                urp.renderShadows = false;
            SetRunning(false);
        }

        void OnDestroy()
        {
            if (imageManager != null) imageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
            if (cameraManager != null) cameraManager.frameReceived -= OnFrame;
        }

        public void Begin()
        {
            ResetDetection();
            m_TorchWanted = false;
            NoCamera = false;
#if UNITY_EDITOR
            if (useWebcamInEditor && WebCamTexture.devices.Length > 0)
            {
                IsRunning = true;
                StartWebcam();
                return;
            }
#endif
            // HP yang sudah ketahuan tidak bisa memakai ARCore: langsung kamera biasa (tidak diminta memasang ARCore lagi)
#if ORGANO_KAMERA_LAMA
            if (ArUnsupported)      // build "kamera lama": ARCore selalu dicek / ditawarkan lagi seperti versi sebelumnya
#else
            if (ArUnsupported || PlayerPrefs.GetInt(ArUnavailableKey, 0) == 1)
#endif
            {
                ArUnsupported = true;
                IsRunning = true;
                StartWebcam();
                return;
            }
            SetRunning(true);
            // AR: 60 fps seperti sebelumnya. Dengan 30 fps kartu lebih lambat dikenali ARCore (laporan uji 16 Okt 2026).
            Application.targetFrameRate = 60;
            StartCoroutine(EnsureArAvailable());
        }

        const string ArUnavailableKey = "organo.ar.unavailable";
        const string ArInstallFailKey = "organo.ar.installfail";

        /// <summary>Coba lagi memakai AR (mis. setelah ARCore dipasang sendiri dari Play Store).</summary>
        public void RetryAr()
        {
            PlayerPrefs.DeleteKey(ArUnavailableKey);
            PlayerPrefs.DeleteKey(ArInstallFailKey);
            PlayerPrefs.Save();
            ArUnsupported = false;
            End();
            Begin();
        }

        public void End()
        {
            ResetDetection();
            SetTorch(false);
            StopAllCoroutines();
            StopWebcam();
            SetRunning(false);
        }

        /// <summary>Cek dukungan AR; pasang ARCore bila perlu; kalau tetap tidak bisa, pakai kamera biasa.</summary>
        IEnumerator EnsureArAvailable()
        {
            if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
                yield return ARSession.CheckAvailability();
            if (ARSession.state == ARSessionState.NeedsInstall)
                yield return ARSession.Install();
            if (ARSession.state == ARSessionState.Unsupported || ARSession.state == ARSessionState.NeedsInstall)
            {
                // Mis. HP yang tidak lagi didukung Google: toko aplikasi menolak memasang "Google Play Services for AR".
                // HP yang memang tidak didukung diingat, jadi aplikasi tidak menyuruh memasang ARCore setiap kali Scan dibuka.
                // Pemasangan yang dibatalkan / gagal baru diingat setelah 2 kali (dulu langsung diingat selamanya, jadi HP yang
                // sebenarnya mendukung AR tetap memakai kamera biasa hanya karena sekali menekan "batal").
                Debug.Log("[Organo] AR tidak didukung (" + ARSession.state + "), pakai kamera biasa.");
#if !ORGANO_KAMERA_LAMA
                int fails = PlayerPrefs.GetInt(ArInstallFailKey, 0) + 1;
                PlayerPrefs.SetInt(ArInstallFailKey, fails);
                if (ARSession.state == ARSessionState.Unsupported || fails >= 2) PlayerPrefs.SetInt(ArUnavailableKey, 1);
                PlayerPrefs.Save();
#endif
                ArUnsupported = true;
                SetRunning(false);
                IsRunning = true;
                StartWebcam();
            }
        }

        void StartWebcam()
        {
            // kamera biasa: 30 fps sudah sama dengan kecepatan kamera (HP lebih dingin)
            Application.targetFrameRate = 30;
            if (m_Webcam != null) return;
            var devices = WebCamTexture.devices;
            if (devices.Length == 0) { NoCamera = true; return; }
            int pick = 0;
            for (int i = 0; i < devices.Length; i++) if (!devices[i].isFrontFacing) { pick = i; break; }
            m_WebcamFront = devices[pick].isFrontFacing;
            m_Webcam = new WebCamTexture(devices[pick].name, 1280, 720, 30);
            m_Webcam.Play();
        }

        void StopWebcam()
        {
            if (m_Webcam != null) { m_Webcam.Stop(); Destroy(m_Webcam); m_Webcam = null; }
            if (m_WebcamRT != null) { m_WebcamRT.Release(); Destroy(m_WebcamRT); m_WebcamRT = null; }
            if (m_Probe != null) { m_Probe.Release(); Destroy(m_Probe); m_Probe = null; }
            if (m_Half != null) { m_Half.Release(); Destroy(m_Half); m_Half = null; }
            if (m_Small != null) { m_Small.Release(); Destroy(m_Small); m_Small = null; }
            m_ReadbackPending = false;
            NativeTorch.Set(false);
        }

        // ---------------- pengenal kartu dengan kamera biasa (HP tanpa AR)
        CardMatcher m_Matcher;
        bool m_MatcherLoaded;
        Rect m_ScanFrame;
        Vector2 m_ScanView;
        bool m_HasScanFrame;

        /// <summary>
        /// Letak bingkai scan di tampilan kamera (unit UI, kiri atas) dan ukuran tampilan kameranya. Diisi layar Scan /
        /// Misi Scan; dipakai untuk mengenali kartu dengan kamera biasa (HP yang tidak mendukung AR).
        /// </summary>
        public void SetScanFrame(Rect frame, Vector2 view)
        {
            m_ScanFrame = frame;
            m_ScanView = view;
            m_HasScanFrame = frame.width > 1f && frame.height > 1f && view.x > 1f && view.y > 1f;
        }

        public void ClearScanFrame() { m_HasScanFrame = false; ClearVotes(); }

        /// <summary>Kartu bisa dikenali dengan kamera biasa (ciri kartu ada & kamera biasa menyala).</summary>
#if ORGANO_KAMERA_LAMA
        public bool CanRecognizeWithCamera => false;   // build "kamera lama": kartu hanya dikenali ARCore
#else
        public bool CanRecognizeWithCamera => (UsingWebcam || TestCamera) && Matcher != null && Matcher.Count > 0;
#endif

        CardMatcher Matcher
        {
            get
            {
                if (!m_MatcherLoaded) { m_MatcherLoaded = true; m_Matcher = CardMatcher.Load(); }
                return m_Matcher;
            }
        }

        // ---------------- gambar kamera kecil untuk pengenal (diperkecil di GPU, dibaca tanpa menahan CPU)
        RenderTexture m_Half, m_Small;
        bool m_ReadbackPending;
        // Urutan baris hasil baca GPU bisa berbeda antar HP (Vulkan / GLES): dicek sekali dengan gambar uji 2x2 yang
        // melewati jalur yang sama (3 kali Blit). 1 = baris bawah dulu (seperti GetPixels32), -1 = baris atas dulu.
        int m_RowOrder;
        bool m_Calibrating;

        void CalibrateReadback()
        {
            m_Calibrating = true;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var red = new Color32(255, 0, 0, 255); var blue = new Color32(0, 0, 255, 255);
            tex.SetPixels32(new[] { red, red, blue, blue }); // baris 0 = bawah: bawah merah, atas biru
            tex.Apply();
            var a = RenderTexture.GetTemporary(2, 2, 0, RenderTextureFormat.ARGB32);
            var b = RenderTexture.GetTemporary(2, 2, 0, RenderTextureFormat.ARGB32);
            var c = RenderTexture.GetTemporary(2, 2, 0, RenderTextureFormat.ARGB32);
            a.filterMode = b.filterMode = c.filterMode = FilterMode.Point;
            Graphics.Blit(tex, a); Graphics.Blit(a, b); Graphics.Blit(b, c);
            AsyncGPUReadback.Request(c, 0, TextureFormat.RGBA32, req =>
            {
                m_Calibrating = false;
                if (!req.hasError)
                {
                    var d = req.GetData<Color32>();
                    m_RowOrder = d.Length >= 4 && d[0].b > d[0].r ? -1 : 1;
                }
                else m_RowOrder = 1;
                Debug.Log("[Organo][AR] urutan baris baca GPU: " + (m_RowOrder > 0 ? "bawah dulu" : "atas dulu (dibalik)"));
                RenderTexture.ReleaseTemporary(a); RenderTexture.ReleaseTemporary(b); RenderTexture.ReleaseTemporary(c);
                Destroy(tex);
            });
        }

        /// <summary>
        /// Uji Editor: gambar 64x36 (separuh ATAS merah) lewat jalur kamera biasa (3 kali Blit, baca GPU, dibalik sesuai
        /// cek urutan baris) harus sama dengan GetPixels32 (baris bawah dulu: baris terakhir merah).
        /// </summary>
        public IEnumerator TestReadbackPath(Action<bool, string> done)
        {
            m_RowOrder = 0;
            CalibrateReadback();
            while (m_Calibrating) yield return null;
            var tex = new Texture2D(64, 36, TextureFormat.RGBA32, false);
            var px = new Color32[64 * 36];
            for (int y = 0; y < 36; y++)
                for (int x = 0; x < 64; x++) px[y * 64 + x] = y >= 18 ? new Color32(255, 0, 0, 255) : new Color32(0, 0, 255, 255);
            tex.SetPixels32(px); tex.Apply();
            var a = new RenderTexture(64, 36, 0, RenderTextureFormat.ARGB32);
            var b = new RenderTexture(32, 18, 0, RenderTextureFormat.ARGB32);
            var c = new RenderTexture(16, 9, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(tex, a); Graphics.Blit(a, b); Graphics.Blit(b, c);
            bool finished = false; Color32[] got = null;
            AsyncGPUReadback.Request(c, 0, TextureFormat.RGBA32, req =>
            {
                if (!req.hasError) { got = req.GetData<Color32>().ToArray(); if (m_RowOrder < 0) FlipRows(got, 16, 9); }
                finished = true;
            });
            while (!finished) yield return null;
            bool ok = got != null && got[8 * 16 + 8].r > 200 && got[8 * 16 + 8].b < 60 && got[0].b > 200 && got[0].r < 60;
            done(ok, (m_RowOrder > 0 ? "bawah dulu" : "atas dulu") + (got != null ? ", baris pertama " + got[0] + ", terakhir " + got[8 * 16 + 8] : ", gagal dibaca"));
            a.Release(); b.Release(); c.Release();
            Destroy(a); Destroy(b); Destroy(c); Destroy(tex);
            m_RowOrder = 0;
        }

        static void FlipRows(Color32[] px, int w, int h)
        {
            var row = new Color32[w];
            for (int y = 0; y < h / 2; y++)
            {
                System.Array.Copy(px, y * w, row, 0, w);
                System.Array.Copy(px, (h - 1 - y) * w, px, y * w, w);
                System.Array.Copy(row, 0, px, (h - 1 - y) * w, w);
            }
        }
        float m_ProcessTimer;
        Color32[] m_Raw, m_Up;
        int m_SmallW, m_SmallH;

        // ---------------- pengambilan suara: kartu baru dianggap dikenali kalau 3 dari 4 pemeriksaan terakhir sepakat
        readonly string[] m_Votes = new string[4];
        int m_VoteAt;
        Rect m_VoteRect;

        // ---------------- pelacak kartu (kamera biasa): letak kartu di gambar tegak kecil
        bool m_TrkActive;
        Rect m_TrkRect;
        int m_TrkMiss, m_TrkTick;
        float m_TrkLost = -1f;
        int m_ImgW, m_ImgH;
        Vector2 m_ViewSize;

        /// <summary>Kartu sedang terlacak di gambar kamera biasa (model di halaman hasil scan mengikuti kartu).</summary>
        public bool WebcamCardVisible => (UsingWebcam || TestCamera) && m_TrkActive && CurrentOrgan != null;

        /// <summary>Uji Editor: jalur kamera biasa dijalankan dengan gambar buatan (<see cref="AnalyzeForTest"/>).</summary>
        public bool TestCamera { get; set; }

        /// <summary>Uji Editor: satu gambar kamera buatan (baris ATAS dulu, tegak) untuk pengenal / pelacak kamera biasa.</summary>
        public void AnalyzeForTest(Color32[] topFirst, int w, int h)
        {
            var raw = new Color32[w * h];
            for (int y = 0; y < h; y++) System.Array.Copy(topFirst, (h - 1 - y) * w, raw, y * w, w);
            Analyze(raw, w, h, 1);
        }

        /// <summary>Ukuran tampilan kamera (unit UI) di halaman yang memakai pelacak, untuk <see cref="WebcamCardRect"/>.</summary>
        public void SetCameraView(Vector2 view) { if (view.x > 1f && view.y > 1f) m_ViewSize = view; }

        /// <summary>Letak kartu di tampilan kamera (unit UI, kiri atas tampilan) — berlaku bila <see cref="WebcamCardVisible"/>.</summary>
        public Rect WebcamCardRect =>
            m_ImgW > 0 && m_ViewSize.x > 1f ? CardMatcher.ImageToScreen(m_TrkRect, m_ViewSize.x, m_ViewSize.y, m_ImgW, m_ImgH) : default;

        void ClearVotes()
        {
            for (int i = 0; i < m_Votes.Length; i++) m_Votes[i] = null;
        }

        /// <summary>Catat hasil satu pemeriksaan; kembalikan organ yang sudah disepakati (null kalau belum).</summary>
        string Vote(string organId)
        {
            m_Votes[m_VoteAt] = organId;
            m_VoteAt = (m_VoteAt + 1) % m_Votes.Length;
            if (organId == null) return null;
            int n = 0;
            foreach (var v in m_Votes) if (v == organId) n++;
            return n >= 3 ? organId : null;
        }

        static bool Sure(CardMatcher.Result r) =>
            r.organId != null && r.score >= CardMatcher.RecognizeMin && r.margin >= CardMatcher.RecognizeMargin;

        /// <summary>
        /// Kamera biasa (HP tanpa AR). Gambar kamera diperkecil di GPU (±320 piksel) lalu dibaca tanpa menunggu, ±4 kali per
        /// detik saat mencari kartu dan ±6 kali per detik saat melacak:
        /// - belum ada kartu: isi bingkai scan dikenali (<see cref="CardMatcher.Recognize"/>); kartu dianggap ada kalau 3 dari 4
        ///   pemeriksaan sepakat (±1 detik), supaya meja/buku/layar tidak tiba-tiba memunculkan organ;
        /// - halaman hasil scan: kartu dilacak (letak & ukuran, untuk model yang mengikuti kartu), kartu lain yang diarahkan ke
        ///   kamera membuat organ berganti, dan kartu yang hilang dicari lagi.
        /// </summary>
        void ProcessCamera()
        {
            if (!CanRecognizeWithCamera) return;
            bool tracking = m_CurrentId != null && (SwitchEnabled || TrackCard);
            bool detecting = m_CurrentId == null && m_PendingOrgan == null && m_HasScanFrame;
            if (!tracking && !detecting) { m_TrkActive = m_TrkActive && m_CurrentId != null; return; }
            m_ProcessTimer -= Time.unscaledDeltaTime;
            if (m_ProcessTimer > 0f || m_ReadbackPending) return;
            m_ProcessTimer = tracking ? 0.15f : 0.25f;

            if (SystemInfo.supportsAsyncGPUReadback && m_WebcamRT != null)
            {
                if (m_RowOrder == 0) { if (!m_Calibrating) CalibrateReadback(); return; }
                // 1280x720 → 640x360 → 320x180 (dua kali diperkecil = rata-rata yang cukup halus)
                int sw = Mathf.Max(32, m_WebcamRT.width / 4), sh = Mathf.Max(32, m_WebcamRT.height / 4);
                if (m_Small == null || m_Small.width != sw || m_Small.height != sh)
                {
                    if (m_Half != null) { m_Half.Release(); Destroy(m_Half); }
                    if (m_Small != null) { m_Small.Release(); Destroy(m_Small); }
                    m_Half = new RenderTexture(sw * 2, sh * 2, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear };
                    m_Small = new RenderTexture(sw, sh, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear };
                    m_Half.Create(); m_Small.Create();
                }
                Graphics.Blit(m_WebcamRT, m_Half);
                Graphics.Blit(m_Half, m_Small);
                m_ReadbackPending = true;
                AsyncGPUReadback.Request(m_Small, 0, TextureFormat.RGBA32, req =>
                {
                    m_ReadbackPending = false;
                    if (req.hasError || !UsingWebcam) return;
                    var data = req.GetData<Color32>();
                    if (m_Raw == null || m_Raw.Length != data.Length) m_Raw = new Color32[data.Length];
                    data.CopyTo(m_Raw);
                    m_SmallW = req.width; m_SmallH = req.height;
                    // samakan dengan GetPixels32 (baris bawah dulu) sesuai hasil cek gambar uji
                    if (m_RowOrder < 0) FlipRows(m_Raw, m_SmallW, m_SmallH);
                    Analyze(m_Raw, m_SmallW, m_SmallH, 1);
                });
                return;
            }
            // cadangan (HP tanpa baca GPU): ambil piksel webcam langsung
            int w = m_Webcam.width, h = m_Webcam.height;
            if (m_Raw == null || m_Raw.Length != w * h) m_Raw = new Color32[w * h];
            m_Webcam.GetPixels32(m_Raw);
            Analyze(m_Raw, w, h, Mathf.Max(1, Mathf.Max(w, h) / 320));
        }

        void Analyze(Color32[] raw, int w, int h, int step)
        {
            if (!CanRecognizeWithCamera) return;
            m_Up = CardMatcher.Upright(raw, w, h, step, WebcamRotation, WebcamMirrored, WebcamFlipped, out int uw, out int uh, m_Up);
            m_ImgW = uw; m_ImgH = uh;
            if (m_CurrentId == null) Detect(m_Up, uw, uh);
            else if (SwitchEnabled || TrackCard) Track(m_Up, uw, uh);
        }

        void Detect(Color32[] img, int uw, int uh)
        {
            if (!m_HasScanFrame || m_PendingOrgan != null) return;
            var frame = CardMatcher.ScreenToImage(m_ScanFrame, m_ScanView.x, m_ScanView.y, uw, uh);
            var res = Matcher.Recognize(img, uw, uh, frame);
            var organ = Sure(res) ? m_Db.Get(res.organId) : null;
            if (organ != null && Ignored(organ.id)) organ = null;
            var agreed = Vote(organ != null ? organ.id : null);
            if (organ != null) m_VoteRect = res.crop;
            if (agreed == null) return;
            ClearVotes();
            LastSeen = organ.id + " (kamera biasa, cocok " + res.score.ToString("0.00") + ", selisih " + res.margin.ToString("0.00") + ")";
            LastSeenTime = Time.unscaledTime;
            Debug.Log("[Organo][AR] kartu dikenali kamera biasa: " + LastSeen);
            m_TrkRect = m_VoteRect;
            m_TrkActive = true;
            m_TrkMiss = 0;
            m_TrkLost = -1f;
            Confirm(null, organ, false);
        }

        /// <summary>Bingkai tengah (seperti bingkai scan) di gambar tegak: tempat kartu dicari lagi saat hilang.</summary>
        static Rect CenterFrame(int uw, int uh)
        {
            float h = Mathf.Min(uh * 0.62f, uw * 0.86f / CardMatcher.Aspect);
            return CardMatcher.Window(uw / 2f, uh * 0.48f, h);
        }

        void Track(Color32[] img, int uw, int uh)
        {
            m_TrkTick++;
            if (m_TrkActive)
            {
                float sc = Matcher.Track(img, uw, uh, m_TrkRect, m_CurrentId, out var r);
                if (sc >= CardMatcher.TrackKeep)
                {
                    // dihaluskan sedikit supaya model tidak bergetar
                    m_TrkRect = new Rect(Vector2.Lerp(m_TrkRect.position, r.position, 0.7f), Vector2.Lerp(m_TrkRect.size, r.size, 0.7f));
                    m_TrkMiss = 0;
                }
                else if (++m_TrkMiss >= 3) { m_TrkActive = false; m_TrkLost = Time.unscaledTime; ClearVotes(); }
                // kartu lain di tempat yang sama (mis. kartu di layar laptop diganti): organ berganti
                if (SwitchEnabled && m_TrkActive && m_TrkTick % 2 == 0)
                {
                    var other = Matcher.MatchWindow(img, uw, uh, m_TrkRect, 2);
                    // Sure: unggul jelas atas organ kedua (bisa organ yang sedang dipakai) dengan ciri pembeda
                    bool another = Sure(other) && other.organId != m_CurrentId;
                    var agreed = Vote(another ? other.organId : null);
                    if (agreed != null) SwitchWebcam(agreed, other.crop);
                }
                return;
            }
            // kartu hilang: cari lagi di tengah (kartu apa pun), sesekali juga kartu yang sama di seluruh gambar
            var res = Matcher.Recognize(img, uw, uh, CenterFrame(uw, uh));
            if (!SwitchEnabled && Sure(res) && res.organId != m_CurrentId) res.organId = null; // misi scan: hanya kartu itu
            if (Sure(res))
            {
                var agreed = Vote(res.organId);
                if (agreed == m_CurrentId || (agreed == null && res.organId == m_CurrentId && res.score >= CardMatcher.RecognizeMin + 0.04f))
                {
                    m_TrkRect = res.crop; m_TrkActive = true; m_TrkMiss = 0; ClearVotes();
                    return;
                }
                if (agreed != null) { SwitchWebcam(agreed, res.crop); return; }
            }
            else Vote(null);
            if (m_TrkTick % 4 == 0)
            {
                var again = Matcher.Search(img, uw, uh, m_CurrentId);
                if (again.score >= CardMatcher.SearchMin) { m_TrkRect = again.crop; m_TrkActive = true; m_TrkMiss = 0; ClearVotes(); }
            }
        }

        void SwitchWebcam(string organId, Rect where)
        {
            var organ = m_Db.Get(organId);
            ClearVotes();
            if (organ == null || !CanSpawn(organ) || Ignored(organ.id)) return;
            bool changed = organ != CurrentOrgan;
            m_CurrentId = organ.id;
            CurrentOrgan = organ;
            m_TrkRect = where;
            m_TrkActive = true;
            m_TrkMiss = 0;
            LastSeen = organ.id + " (kamera biasa, ganti kartu)";
            LastSeenTime = Time.unscaledTime;
            Debug.Log("[Organo][AR] kamera biasa: ganti ke kartu " + organ.id);
            if (changed) CardSwitched?.Invoke(organ);
        }

        void UpdateWebcam()
        {
            if (!UsingWebcam || !m_Webcam.didUpdateThisFrame || m_Webcam.width <= 16) return;
            if (m_WebcamRT == null || m_WebcamRT.width != m_Webcam.width || m_WebcamRT.height != m_Webcam.height)
            {
                if (m_WebcamRT != null) { m_WebcamRT.Release(); Destroy(m_WebcamRT); }
                m_WebcamRT = new RenderTexture(m_Webcam.width, m_Webcam.height, 0, RenderTextureFormat.ARGB32) { name = "Organo Webcam" };
                m_WebcamRT.Create();
            }
            Graphics.Blit(m_Webcam, m_WebcamRT);
            ProcessCamera();

            // terang/gelap dari gambar kamera (pengganti estimasi cahaya AR)
            m_ProbeTimer -= Time.unscaledDeltaTime;
            if (m_ProbeTimer > 0f || !SystemInfo.supportsAsyncGPUReadback) return;
            m_ProbeTimer = 0.5f;
            if (m_Probe == null) { m_Probe = new RenderTexture(8, 8, 0, RenderTextureFormat.ARGB32); m_Probe.Create(); }
            Graphics.Blit(m_WebcamRT, m_Probe);
            AsyncGPUReadback.Request(m_Probe, 0, TextureFormat.RGBA32, req =>
            {
                if (req.hasError || !UsingWebcam) return;
                var data = req.GetData<Color32>();
                float sum = 0f;
                foreach (var c in data) sum += (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
                SetBrightness(sum / Mathf.Max(1, data.Length));
            });
            // (pengenal kartu memakai gambar kecil sendiri: lihat ProcessCamera)
        }

        void SetBrightness(float b)
        {
            if (b < darkThreshold)
            {
                if (m_DarkSince < 0) m_DarkSince = Time.unscaledTime;
                IsDark = Time.unscaledTime - m_DarkSince > 1.2f;
            }
            else { m_DarkSince = -1f; IsDark = false; }
        }

        void SetRunning(bool on)
        {
            IsRunning = on;
            if (session != null) session.enabled = on;
            if (cameraManager != null) cameraManager.enabled = on;
            if (cameraBackground != null) cameraBackground.enabled = on;
            if (imageManager != null) imageManager.enabled = on && HasCardLibrary;
        }

        public void ResetDetection()
        {
            if (m_CurrentId != null) m_Ignore[m_CurrentId] = Time.unscaledTime + 2.5f;
            m_CurrentId = null;
            CurrentOrgan = null;
            m_Pending = null;
            m_PendingOrgan = null;
            IsDark = false;
            m_DarkSince = -1f;
            if (m_Spawned != null) Destroy(m_Spawned);
            m_Spawned = null;
            m_SpawnAnchor = null;
            m_SpawnImage = null;
            m_Placed = false;
            m_WatchImage = null;
            m_SwitchTo = null;
            ClearVotes();
            m_TrkActive = false;
            m_TrkMiss = 0;
            TrackCard = false;
            m_SpawnHotspots.Clear();
        }

        /// <summary>
        /// Nyalakan/matikan senter. Permintaan disimpan dan diterapkan ulang tiap frame kamera sampai kamera siap
        /// (sebelumnya permintaan hilang kalau kamera AR belum siap tepat saat tombol ditekan).
        /// </summary>
        public void SetTorch(bool on)
        {
            m_TorchWanted = on;
            if (UsingWebcam) { NativeTorch.Set(on); return; }
            ApplyArTorch();
        }

        void ApplyArTorch()
        {
            if (cameraManager == null || cameraManager.subsystem == null) return;
            try
            {
                if (!cameraManager.DoesCurrentCameraSupportTorch()) return;
                var want = m_TorchWanted ? XRCameraTorchMode.On : XRCameraTorchMode.Off;
                if (cameraManager.requestedCameraTorchMode != want) cameraManager.requestedCameraTorchMode = want;
            }
            catch (Exception e) { Debug.LogWarning("[Organo] torch: " + e.Message); }
        }

        void OnFrame(ARCameraFrameEventArgs args)
        {
            FramesReceived++;
            ApplyArTorch();
            var le = args.lightEstimation;
            float? b = le.averageBrightness ?? le.averageMainLightBrightness;
            if (!b.HasValue) return;
            SetBrightness(b.Value);
        }

        void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            if (args.added.Count == 0 && args.updated.Count == 0) return;
            for (int i = 0; i < args.added.Count; i++)
            {
                var img = args.added[i];
                if (img == null) continue;
                LastSeen = img.referenceImage.name + " (" + img.trackingState + ")";
                LastSeenTime = Time.unscaledTime;
                Debug.Log("[Organo][AR] kartu dikenali ARCore: " + LastSeen);
                NoteTracking(img);
            }
            for (int i = 0; i < args.updated.Count; i++) NoteTracking(args.updated[i]);
            PickCard(args.added);
        }

        void NoteTracking(ARTrackedImage img)
        {
            if (img == null) return;
            if (img.trackingState != TrackingState.Tracking) m_TrackedSince.Remove(img);
            else if (!m_TrackedSince.ContainsKey(img)) m_TrackedSince[img] = Time.unscaledTime;
        }

        // Kartu yang sudah dikenali ARCore tapi posisinya belum pasti (belum Tracking): ditunggu sebentar
        ARTrackedImage m_Pending;
        OrganInfo m_PendingOrgan;
        float m_PendingSince;
        const float PendingWait = 0.8f;

        /// <summary>
        /// Pilih satu kartu. Kalau beberapa kartu terlihat sekaligus (mis. kartu-kartu di meja kelas), ambil yang paling dekat
        /// ke tengah layar. Kartu yang sudah terpilih dipertahankan sampai <see cref="ResetDetection"/> (tombol "Scan kartu lain"),
        /// jadi model 3D tidak melompat-lompat antar kartu.
        /// Kartu yang BARU ditambahkan ARCore sudah pasti dikenali walau statusnya belum Tracking (mis. kartu di layar laptop
        /// yang ukurannya beda dari kartu cetak), jadi ikut dihitung; model 3D baru ditempel kalau posisinya sudah Tracking.
        /// </summary>
        void PickCard(IReadOnlyList<ARTrackedImage> fresh)
        {
            if (m_Db == null || imageManager == null || m_CurrentId != null || m_PendingOrgan != null) return;
            ARTrackedImage best = null;
            OrganInfo bestOrgan = null;
            float bestScore = float.MaxValue;
            foreach (var img in imageManager.trackables)
            {
                if (img == null) continue;
                bool tracking = img.trackingState == TrackingState.Tracking;
                if (!tracking && !Contains(fresh, img)) continue;
                var organ = m_Db.FindByReferenceImage(img.referenceImage.name);
                if (organ == null || Ignored(organ.id)) continue;
                float score = tracking ? 0f : 5f; // yang posisinya sudah pasti didahulukan
                if (tracking && arCamera != null)
                {
                    var vp = arCamera.WorldToViewportPoint(img.transform.position);
                    score = vp.z > 0f ? new Vector2(vp.x - 0.5f, vp.y - 0.5f).sqrMagnitude : 4f;
                }
                if (organ.id == PreferOrgan) score -= 10f; // misi scan: kartu yang dicari menang
                if (score < bestScore) { bestScore = score; best = img; bestOrgan = organ; }
            }
            if (best == null) return;
            if (best.trackingState == TrackingState.Tracking) Confirm(best, bestOrgan, true);
            else
            {
                m_Pending = best;
                m_PendingOrgan = bestOrgan;
                m_PendingSince = Time.unscaledTime;
            }
        }

        static bool Contains(IReadOnlyList<ARTrackedImage> list, ARTrackedImage img)
        {
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++) if (list[i] == img) return true;
            return false;
        }

        void Confirm(ARTrackedImage img, OrganInfo organ, bool spawn)
        {
            m_Pending = null;
            m_PendingOrgan = null;
            m_CurrentId = organ.id;
            CurrentOrgan = organ;
            // kartu yang posisinya belum pasti tetap diamati: model AR ditempel begitu ARCore melacaknya (lihat UpdatePending)
            bool can = CanSpawn(organ);
            m_WatchImage = spawn || !can ? null : img;
            if (spawn && can && img != null) SpawnOn(img, organ);
            Debug.Log("[Organo][AR] kartu terpilih: " + organ.id + (spawn ? " (model di atas kartu)" : " (posisi belum pasti, tampil di viewer 3D dulu)"));
            CardDetected?.Invoke(organ);
        }

        ARTrackedImage m_WatchImage;

        void UpdatePending()
        {
            if (m_PendingOrgan != null)
            {
                if (m_Pending != null && m_Pending.trackingState == TrackingState.Tracking) Confirm(m_Pending, m_PendingOrgan, true);
                else if (m_Pending == null || Time.unscaledTime - m_PendingSince > PendingWait) Confirm(m_Pending, m_PendingOrgan, false);
                return;
            }
            // kartu sudah dikenali tapi model belum ditempel: tempel saat ARCore sudah yakin dengan posisi & ukurannya
            if (m_Spawned == null && m_WatchImage != null && CurrentOrgan != null && m_WatchImage.trackingState == TrackingState.Tracking && CanSpawn(CurrentOrgan))
            {
                var img = m_WatchImage;
                m_WatchImage = null;
                SpawnOn(img, CurrentOrgan);
                Debug.Log("[Organo][AR] model ditempel setelah kartu terlacak: " + CurrentOrgan.id);
            }
        }

        /// <summary>Ringkasan keadaan AR untuk lembar "Info teknis" (dibaca guru saat scan belum berhasil).</summary>
        public string DebugReport()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("Sesi AR: ").Append(ARSession.state);
            if (ARSession.notTrackingReason != NotTrackingReason.None) sb.Append(" (").Append(ARSession.notTrackingReason).Append(')');
            sb.Append('\n');
            sb.Append("Kamera: ");
            if (UsingWebcam) sb.Append("kamera biasa (tanpa AR)");
            else
            {
                XRCameraConfiguration? cfg = null;
                try { cfg = cameraManager != null && cameraManager.subsystem != null ? cameraManager.currentConfiguration : null; } catch { }
                sb.Append(cfg.HasValue ? cfg.Value.width + "x" + cfg.Value.height + (cfg.Value.framerate.HasValue ? " @" + cfg.Value.framerate.Value + "fps" : "") : "-");
                sb.Append(" · frame ").Append(FramesReceived);
            }
            sb.Append('\n');
            int lib = imageManager != null && imageManager.referenceLibrary != null ? imageManager.referenceLibrary.count : 0;
            sb.Append("Daftar kartu: ").Append(lib).Append(" gambar · pelacak ")
              .Append(imageManager != null && imageManager.enabled && imageManager.subsystem != null && imageManager.subsystem.running ? "aktif" : "mati").Append('\n');
            sb.Append("Kartu terlihat: ");
            int n = 0;
            if (imageManager != null)
                foreach (var img in imageManager.trackables)
                {
                    if (img == null) continue;
                    if (n < 6)
                    {
                        if (n > 0) sb.Append(", ");
                        sb.Append(img.referenceImage.name.Replace("organo-", "")).Append(" (").Append(img.trackingState).Append(')');
                    }
                    n++;
                }
            if (n == 0) sb.Append("belum ada");
            else if (n > 6) sb.Append(" +").Append(n - 6).Append(" lainnya");
            sb.Append('\n');
            sb.Append("Terakhir dikenali: ").Append(LastSeen);
            if (LastSeenTime >= 0f) sb.Append(", ").Append(Mathf.RoundToInt(Time.unscaledTime - LastSeenTime)).Append(" dtk lalu");
            return sb.ToString();
        }

        // Model AR berdiri di atas kartu seperti benda sungguhan: posisi & arahnya tetap di dunia (ikut kartu), jadi makin
        // jauh makin kecil dan HP bisa dibawa memutari kartu untuk melihat sisi lain. Saat pertama muncul model menghadap
        // kamera (tampak depan, sama seperti close-up); setelah itu tidak lagi ikut berputar mengikuti HP.
        Transform m_SpawnAnchor;
        ARTrackedImage m_SpawnImage;
        /// <summary>Arah model relatif kartu: ditetapkan saat pertama terlihat, lalu diputar jari.</summary>
        Quaternion m_ModelLocal = Quaternion.identity;
        bool m_Placed;
        float m_NormalSign = 1f;
        Quaternion m_ModelBase = Quaternion.identity;
        bool m_Snap = true;
        float m_LastTracked = -1f;

        /// <summary>Transform model AR yang diputar (untuk uji).</summary>
        public Transform ModelTransform => m_SpawnNorm;

        /// <summary>Model AR ditampilkan (kartu pernah terlacak dan ARCore masih tahu posisinya).</summary>
        public bool ModelVisible => m_Spawned != null && m_Spawned.activeSelf;

        /// <summary>Model AR ada di dalam gambar kamera (false = kartu di luar layar, beri petunjuk arahkan ke kartu).</summary>
        public bool ModelInView
        {
            get
            {
                if (!ModelVisible || arCamera == null) return false;
                var vp = arCamera.WorldToViewportPoint(m_Spawned.transform.position);
                return vp.z > 0f && vp.x > -0.1f && vp.x < 1.1f && vp.y > -0.1f && vp.y < 1.1f;
            }
        }

        /// <summary>Ukuran kartu sebenarnya relatif terhadap kartu cetak 6,3 cm (kartu di layar laptop bisa 2x lebih besar).</summary>
        float CardScale()
        {
            if (m_SpawnImage == null) return 1f;
            float w = m_SpawnImage.size.x;
            return float.IsNaN(w) || w <= 0.001f ? 1f : Mathf.Clamp(w / 0.063f, 0.5f, 4f);
        }

        void SpawnOn(ARTrackedImage image, OrganInfo organ)
        {
            if (m_Spawned != null) Destroy(m_Spawned);
            m_SpawnHotspots.Clear();
            var anchor = image.transform;

            m_Spawned = new GameObject("AR " + organ.displayName);
            m_Spawned.transform.SetParent(anchor, false);
            m_Spawned.transform.localPosition = new Vector3(0f, organ.arModelSize * 0.6f, 0f);
            m_SpawnAnchor = anchor;
            m_SpawnImage = image;
            m_Snap = true;
            m_Placed = false;
            m_ModelLocal = Quaternion.identity;
            m_ModelBase = Quaternion.Euler(organ.modelEuler);
            m_SpawnNorm = new GameObject("Normalized").transform;
            m_SpawnNorm.SetParent(m_Spawned.transform, false);
            m_SpawnNorm.localScale = Vector3.one * organ.arModelSize;
            UpdateModelPose();

            var fit = new GameObject("Fit").transform;
            fit.SetParent(m_SpawnNorm, false);
            var prefab = organ.ArPrefab;
            var model = prefab != null ? Instantiate(prefab) : OrganPlaceholder.Build(organ.id, organ.accent);
            model.transform.SetParent(fit, false);
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            FitToUnit(fit, model);

            var inPrefab = model.GetComponentsInChildren<OrganHotspot>(true);
            if (inPrefab.Length > 0)
                foreach (var h in inPrefab) m_SpawnHotspots.Add(new OrganViewer3D.Hotspot { anchor = h.transform, title = h.title, description = h.description });
            else if (organ.hotspots != null)
                foreach (var h in organ.hotspots)
                {
                    var a = new GameObject("Hotspot " + h.title).transform;
                    a.SetParent(m_SpawnNorm, false);
                    a.localPosition = h.localPosition;
                    m_SpawnHotspots.Add(new OrganViewer3D.Hotspot { anchor = a, title = h.title, description = h.description });
                }
        }

        static void FitToUnit(Transform fit, GameObject model)
        {
            var rs = model.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return;
            var toLocal = fit.worldToLocalMatrix;
            Bounds b = default; bool init = false;
            foreach (var r in rs)
            {
                var c = r.bounds.center; var e = r.bounds.extents;
                for (int i = 0; i < 8; i++)
                {
                    var p = toLocal.MultiplyPoint3x4(c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z));
                    if (!init) { b = new Bounds(p, Vector3.zero); init = true; } else b.Encapsulate(p);
                }
            }
            float size = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (size <= 1e-5f) return;
            fit.localScale = Vector3.one / size;
            fit.localPosition = -b.center / size;
        }

        /// <summary>
        /// Putar model di atas kartu dengan jari (delta pixel UI): ke kiri/kanan dan ke atas/bawah, 360° ke segala arah.
        /// Putaran mengikuti arah jari di layar dari sudut mana pun HP melihat kartu.
        /// </summary>
        public void RotateSpawned(Vector2 delta)
        {
            if (!m_Placed || arCamera == null) return;
            var cam = arCamera.transform;
            var q = Quaternion.AngleAxis(-delta.x * 0.5f, cam.up) * Quaternion.AngleAxis(-delta.y * 0.5f, cam.right);
            var a = m_SpawnAnchor != null ? m_SpawnAnchor.rotation : Quaternion.identity;
            m_ModelLocal = Quaternion.Inverse(a) * q * a * m_ModelLocal;
        }

        /// <summary>
        /// Model berdiri di atas kartu (di sisi kartu yang menghadap kamera) dengan ukuran sebanding kartu. Saat pertama
        /// terlihat model dihadapkan ke kamera seperti close-up (kamera close-up melihat model dari sumbu -Z dengan rotasi
        /// <see cref="OrganInfo.modelEuler"/>), jadi organ tampak dari depan baik kartu tergeletak di meja maupun berdiri di
        /// layar laptop. Setelah itu arahnya tetap terhadap kartu, seperti benda sungguhan.
        /// </summary>
        void UpdateModelPose()
        {
            if (m_Spawned == null || m_SpawnNorm == null || arCamera == null) return;
            // Kartu terlalu jauh / keluar layar: ARCore memakai posisi terakhir kartu yang tetap di dunia, jadi model tetap
            // di sana (mengecil kalau HP menjauh). Hanya disembunyikan kalau ARCore kehilangan kartu sama sekali.
            // Model hanya tampil selama kartunya terlihat kamera (Tracking); kartu keluar layar / ditutup = model hilang
            // (permintaan 16 Okt 2026: "kalau tidak scan kartunya, organnya hilang"). Jeda 0,35 detik supaya tidak berkedip.
            var state = m_SpawnImage != null ? m_SpawnImage.trackingState : TrackingState.Tracking;
            if (state == TrackingState.Tracking) m_LastTracked = Time.unscaledTime;
            bool show = state == TrackingState.Tracking || (m_Placed && Time.unscaledTime - m_LastTracked < 0.35f);
            if (m_Spawned.activeSelf != show) { m_Spawned.SetActive(show); m_Snap = true; }
            if (!show) return;

            var cam = arCamera.transform;
            var anchorPos = m_SpawnAnchor != null ? m_SpawnAnchor.position : m_Spawned.transform.position;
            var anchorRot = m_SpawnAnchor != null ? m_SpawnAnchor.rotation : Quaternion.identity;
            var normal = m_SpawnAnchor != null ? m_SpawnAnchor.up : Vector3.up; // kartu AR datar di bidang XZ-nya
            if (!m_Placed)
            {
                var toCam = cam.position - anchorPos;
                var dir = toCam.sqrMagnitude > 1e-8f ? toCam.normalized : -cam.forward;
                m_NormalSign = Vector3.Dot(normal, dir) >= 0f ? 1f : -1f;
                // tegak menurut gravitasi (bukan kemiringan HP); kalau kamera tepat di atas kartu pakai arah atas layar
                var up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) < 0.98f ? Vector3.up : cam.up;
                m_ModelLocal = Quaternion.Inverse(anchorRot) * Quaternion.LookRotation(-dir, up);
                m_Placed = true;
                m_Snap = true;
            }
            float k = CardScale();
            float size = (CurrentOrgan != null ? CurrentOrgan.arModelSize : 0.08f) * k;
            m_SpawnNorm.localScale = Vector3.one * size;
            var targetPos = anchorPos + normal * m_NormalSign * (size * 0.6f + Mathf.Sin(Time.time * 1.6f) * 0.004f * k);
            var targetRot = anchorRot * m_ModelLocal * m_ModelBase;
            // dihaluskan: getaran kecil posisi kartu dari ARCore tidak membuat model bergetar
            float t = m_Snap ? 1f : 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            m_Spawned.transform.position = Vector3.Lerp(m_Spawned.transform.position, targetPos, t);
            m_SpawnNorm.rotation = Quaternion.Slerp(m_SpawnNorm.rotation, targetRot, t);
            m_Snap = false;
        }

        void LateUpdate() => UpdateModelPose();

        /// <summary>Posisi layar (0..1, kiri-atas) dari titik info model AR.</summary>
        public bool TryProjectHotspot(int i, out Vector2 uv)
        {
            uv = default;
            if (arCamera == null || i < 0 || i >= m_SpawnHotspots.Count || m_SpawnHotspots[i].anchor == null) return false;
            var vp = arCamera.WorldToViewportPoint(m_SpawnHotspots[i].anchor.position);
            if (vp.z <= 0) return false;
            uv = new Vector2(vp.x, 1f - vp.y);
            return true;
        }

        void Update()
        {
            UpdateWebcam();
            UpdatePending();
            UpdateSwitch();
        }

        // kartu lain yang sedang ditunggu sebelum organ berganti (supaya tidak berganti-ganti karena sekilas terlihat)
        ARTrackedImage m_SwitchTo;
        float m_SwitchSince;
        const float SwitchHold = 0.4f;

        /// <summary>
        /// Kartu lain diarahkan ke kamera saat hasil scan terbuka: organ ikut berganti (dulu kartu pertama dipertahankan sampai
        /// layar ditutup, jadi model mulut tetap muncul walau kamera sudah melihat kartu rangka). Kartu baru harus terlacak
        /// penuh, dekat tengah layar, dan bertahan sebentar. Organ berganti kalau kartu yang sedang dipakai sudah tidak
        /// terlacak / di luar layar / jauh lebih ke pinggir, atau kalau kartu baru muncul di tempat yang hampir sama
        /// (kartu di layar laptop diganti: kartu lama bisa masih dianggap terlacak oleh ARCore beberapa saat).
        /// </summary>
        void UpdateSwitch()
        {
            // pelacak gambar mati (kamera biasa): daftar kartu ARCore lama tidak dipakai untuk berganti kartu
            if (!SwitchEnabled || m_Db == null || imageManager == null || !imageManager.enabled || arCamera == null || CurrentOrgan == null || m_PendingOrgan != null)
            {
                m_SwitchTo = null;
                return;
            }
            ARTrackedImage best = null;
            float bestDist = float.MaxValue, bestSince = 0f;
            foreach (var img in imageManager.trackables)
            {
                if (img == null || img == m_SpawnImage || img == m_WatchImage) continue;
                float d = CenterDistance(img);
                if (d > 0.3f || !CanSpawn(m_Db.FindByReferenceImage(img.referenceImage.name))) continue;
                // beberapa kartu dekat tengah: yang paling baru terlacak didahulukan, lalu yang paling ke tengah
                float since = TrackedSince(img);
                if (best == null || since > bestSince + 0.05f || (Mathf.Abs(since - bestSince) <= 0.05f && d < bestDist)) { best = img; bestDist = d; bestSince = since; }
            }
            float current = m_SpawnImage != null ? CenterDistance(m_SpawnImage) : float.MaxValue;
            bool currentGone = current == float.MaxValue || current > bestDist + 0.15f;
            bool newerHere = best != null && bestSince > TrackedSince(m_SpawnImage) + 0.2f && bestDist <= current + 0.1f;
            if (best == null || !(currentGone || newerHere)) { m_SwitchTo = null; return; }
            if (m_SwitchTo != best) { m_SwitchTo = best; m_SwitchSince = Time.unscaledTime; return; }
            if (Time.unscaledTime - m_SwitchSince < SwitchHold) return;

            m_SwitchTo = null;
            var organ = m_Db.FindByReferenceImage(best.referenceImage.name);
            bool changed = organ != CurrentOrgan;
            m_CurrentId = organ.id;
            CurrentOrgan = organ;
            m_WatchImage = null;
            SpawnOn(best, organ); // kartu Realistis ↔ Kartun organ yang sama: model pindah saja, halaman tetap
            Debug.Log("[Organo][AR] ganti ke kartu " + best.referenceImage.name + (changed ? " (organ berganti)" : " (organ sama)"));
            if (changed) CardSwitched?.Invoke(organ);
        }

        /// <summary>Jarak pusat kartu ke tengah gambar kamera (0 = tepat di tengah); tak hingga kalau tidak terlacak penuh / di luar layar.</summary>
        float CenterDistance(ARTrackedImage img)
        {
            if (img.trackingState != TrackingState.Tracking) return float.MaxValue;
            var vp = arCamera.WorldToViewportPoint(img.transform.position);
            if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) return float.MaxValue;
            return new Vector2(vp.x - 0.5f, vp.y - 0.5f).magnitude;
        }

        /// <summary>
        /// Lacak kartu yang sudah dikenali kamera biasa walau tidak sedang di halaman hasil scan (misi scan di kuis), tanpa
        /// berganti ke kartu lain.
        /// </summary>
        public bool TrackCard { get; set; }

        /// <summary>
        /// Misi scan dijawab benar: tampilkan model organ itu di atas kartunya (AR), atau lacak kartunya (kamera biasa),
        /// seperti hasil scan — bukan model diam di tengah layar.
        /// </summary>
        public void ShowModelOnCard()
        {
            SpawnModels = true;
            if (CurrentOrgan == null) return;
            if (UsingWebcam || TestCamera) { TrackCard = true; return; }
            if (m_Spawned != null || imageManager == null || m_Db == null) return;
            ARTrackedImage best = null;
            foreach (var img in imageManager.trackables)
            {
                if (img == null || m_Db.FindByReferenceImage(img.referenceImage.name) != CurrentOrgan) continue;
                if (best == null || (img.trackingState == TrackingState.Tracking && best.trackingState != TrackingState.Tracking)) best = img;
            }
            if (best == null) return;
            if (best.trackingState == TrackingState.Tracking) SpawnOn(best, CurrentOrgan);
            else m_WatchImage = best;
        }

        /// <summary>Simulasi hasil scan (Editor / sebelum gambar kartu dipasang).</summary>
        public void SimulateDetection(OrganInfo organ)
        {
            if (organ == null) return;
            m_CurrentId = organ.id;
            CurrentOrgan = organ;
            CardDetected?.Invoke(organ);
        }
    }
}
