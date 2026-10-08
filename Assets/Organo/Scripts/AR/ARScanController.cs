using System;
using System.Collections.Generic;
using Organo.Data;
using Organo.ThreeD;
using UnityEngine;
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

        public event Action<OrganInfo> CardDetected;

        OrganDatabase m_Db;
        GameObject m_Spawned;
        Transform m_SpawnNorm;
        readonly List<OrganViewer3D.Hotspot> m_SpawnHotspots = new List<OrganViewer3D.Hotspot>();
        string m_CurrentId;
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

        public bool TorchSupported => cameraManager != null && cameraManager.subsystem != null && cameraManager.DoesCurrentCameraSupportTorch();

        public void Init(OrganDatabase db)
        {
            m_Db = db;
            if (imageManager != null) imageManager.trackablesChanged.AddListener(OnTrackablesChanged);
            if (cameraManager != null) cameraManager.frameReceived += OnFrame;
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
            SetRunning(true);
        }

        public void End()
        {
            ResetDetection();
            SetTorch(false);
            SetRunning(false);
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
            m_CurrentId = null;
            CurrentOrgan = null;
            IsDark = false;
            m_DarkSince = -1f;
            if (m_Spawned != null) Destroy(m_Spawned);
            m_Spawned = null;
            m_SpawnHotspots.Clear();
        }

        public void SetTorch(bool on)
        {
            if (cameraManager == null || cameraManager.subsystem == null) return;
            try
            {
                if (cameraManager.DoesCurrentCameraSupportTorch())
                    cameraManager.requestedCameraTorchMode = on ? XRCameraTorchMode.On : XRCameraTorchMode.Off;
            }
            catch (Exception e) { Debug.LogWarning("[Organo] torch: " + e.Message); }
        }

        void OnFrame(ARCameraFrameEventArgs args)
        {
            var le = args.lightEstimation;
            float? b = le.averageBrightness ?? le.averageMainLightBrightness;
            if (!b.HasValue) return;
            if (b.Value < darkThreshold)
            {
                if (m_DarkSince < 0) m_DarkSince = Time.unscaledTime;
                IsDark = Time.unscaledTime - m_DarkSince > 1.2f;
            }
            else { m_DarkSince = -1f; IsDark = false; }
        }

        void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            foreach (var img in args.added) Consider(img);
            foreach (var img in args.updated) Consider(img);
        }

        void Consider(ARTrackedImage img)
        {
            if (img == null || img.trackingState != TrackingState.Tracking || m_Db == null) return;
            var organ = m_Db.FindByReferenceImage(img.referenceImage.name);
            if (organ == null || organ.id == m_CurrentId) return;
            m_CurrentId = organ.id;
            CurrentOrgan = organ;
            SpawnOn(img.transform, organ);
            CardDetected?.Invoke(organ);
        }

        void SpawnOn(Transform anchor, OrganInfo organ)
        {
            if (m_Spawned != null) Destroy(m_Spawned);
            m_SpawnHotspots.Clear();

            m_Spawned = new GameObject("AR " + organ.displayName);
            m_Spawned.transform.SetParent(anchor, false);
            m_Spawned.transform.localPosition = new Vector3(0f, organ.arModelSize * 0.6f, 0f);
            m_SpawnNorm = new GameObject("Normalized").transform;
            m_SpawnNorm.SetParent(m_Spawned.transform, false);
            m_SpawnNorm.localScale = Vector3.one * organ.arModelSize;
            m_SpawnNorm.localRotation = Quaternion.Euler(0f, 180f, 0f);

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

        /// <summary>Putar model di atas kartu dengan jari (delta pixel horizontal).</summary>
        public void RotateSpawned(float dx)
        {
            if (m_SpawnNorm != null) m_SpawnNorm.Rotate(0f, -dx * 0.5f, 0f, Space.Self);
        }

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
            if (m_Spawned != null)
            {
                // sedikit melayang di atas kartu
                var p = m_Spawned.transform.localPosition;
                float baseY = CurrentOrgan != null ? CurrentOrgan.arModelSize * 0.6f : 0.05f;
                p.y = baseY + Mathf.Sin(Time.time * 1.6f) * 0.004f;
                m_Spawned.transform.localPosition = p;
            }
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
