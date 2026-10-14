using System.Collections.Generic;
using Organo.Data;
using UnityEngine;

namespace Organo.ThreeD
{
    /// <summary>
    /// Studio 3D tersembunyi untuk close-up organ. Kamera khusus merender model organ ke RenderTexture
    /// yang ditampilkan oleh <see cref="OrganViewport"/> di UI. Model diambil dari <see cref="OrganInfo.modelPrefab"/>
    /// (slot model); kalau kosong dipakai placeholder sederhana supaya tempatnya tetap terlihat.
    /// </summary>
    public partial class OrganViewer3D : MonoBehaviour
    {
        public const int Layer = 30;
        static readonly Vector3 StagePosition = new Vector3(0f, -1000f, 0f);
        const float CameraDistance = 2.5f;

        [Tooltip("Kecepatan putar otomatis saat tidak disentuh (derajat/detik).")]
        public float idleSpinSpeed = 14f;
        [Tooltip("Detik tanpa sentuhan sebelum model berputar otomatis.")]
        public float idleDelay = 3f;

        Camera m_Camera;
        Transform m_Pivot, m_Norm, m_Fit;
        GameObject m_Model;
        RenderTexture m_RT;
        OrganInfo m_Organ;
        object m_Owner;
        float m_Yaw, m_Pitch, m_Zoom = 1f, m_YawVel;
        bool m_FreeRotate;
        Quaternion m_Free = Quaternion.identity;

        /// <summary>
        /// Putar bebas 360° ke segala arah (hasil scan): geser naik/turun juga memutar penuh (biasanya dibatasi ±60°)
        /// dan putaran selalu mengikuti arah jari di layar, berapa pun model sudah diputar.
        /// </summary>
        public bool FreeRotate
        {
            get => m_FreeRotate;
            set
            {
                if (m_FreeRotate == value) return;
                m_FreeRotate = value;
                m_Free = Quaternion.Euler(m_Pitch, m_Yaw, 0f);
            }
        }
        /// <summary>Geser kamera (unit model) saat diperbesar: zoom mengarah ke titik yang dicubit, bukan selalu ke tengah.</summary>
        Vector2 m_Pan;
        float m_LastInput = -100f;
        public const float MaxZoom = 3f;

        /// <summary>Model sedang diperbesar / digeser oleh jari (tombol di atas model sebaiknya menyingkir).</summary>
        public bool IsZoomed => m_Zoom > 1.08f || m_Pan.sqrMagnitude > 0.0004f;
        readonly List<Hotspot> m_Hotspots = new List<Hotspot>();

        public struct Hotspot
        {
            public Transform anchor;
            public string title;
            public string description;
            /// <summary>Kunci untuk UI (mis. nama area tubuh atau id organ).</summary>
            public string key;
        }

        public RenderTexture Texture => m_RT;
        /// <summary>Sedang menampilkan organ di dalam tubuh (mode "Lihat di tubuh").</summary>
        public bool BodyMode { get; private set; }
        /// <summary>Penyedia data tubuh: database organ & prefab tubuh sesuai gender (diisi OrganoApp).</summary>
        public System.Func<OrganDatabase> DatabaseProvider;
        public System.Func<GameObject> BodyPrefabProvider;
        public OrganInfo Organ => m_Organ;
        public bool IsPlaceholder { get; private set; }
        public IReadOnlyList<Hotspot> Hotspots => m_Hotspots;
        public Camera Camera => m_Camera;

        public static OrganViewer3D Create()
        {
            var go = new GameObject("Organo 3D Close-up Stage");
            go.transform.position = StagePosition;
            return go.AddComponent<OrganViewer3D>();
        }

        void Awake()
        {
            transform.position = StagePosition;
            m_Pivot = new GameObject("Pivot").transform;
            m_Pivot.SetParent(transform, false);
            m_Norm = new GameObject("Normalized Space").transform;
            m_Norm.SetParent(m_Pivot, false);
            m_Fit = new GameObject("Model Slot").transform;
            m_Fit.SetParent(m_Norm, false);

            var camGo = new GameObject("Close-up Camera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -CameraDistance);
            m_Camera = camGo.AddComponent<Camera>();
            m_Camera.clearFlags = CameraClearFlags.SolidColor;
            m_Camera.backgroundColor = new Color(1f, 1f, 1f, 0f);
            m_Camera.cullingMask = 1 << Layer;
            m_Camera.fieldOfView = 30f;
            m_Camera.nearClipPlane = 0.05f;
            m_Camera.farClipPlane = 20f;
            m_Camera.allowHDR = false;
            m_Camera.allowMSAA = false;
            m_Camera.depth = -10;
            m_Camera.enabled = false;

            if (RenderSettings.sun == null && FindAnyObjectByType<Light>() == null)
            {
                var light = new GameObject("Close-up Light").AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.SetParent(transform, false);
                light.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
                light.intensity = 1.1f;
            }
        }

        void OnDestroy()
        {
            if (m_RT != null) { m_RT.Release(); Destroy(m_RT); }
        }

        // ------------------------------------------------------------ ownership (one viewport at a time)

        public void Attach(object owner, OrganInfo organ, bool bodyMode = false) => Attach(owner, organ, bodyMode ? ViewMode.Body : ViewMode.Organ);

        /// <summary>Pakai studio untuk satu viewport: organ utuh, potongan ("Lihat dalam"), atau di dalam tubuh.</summary>
        public void Attach(object owner, OrganInfo organ, ViewMode mode)
        {
            bool newOwner = m_Owner != owner;
            m_Owner = owner;
            m_Camera.enabled = true;
            if (mode == ViewMode.Inside && (organ == null || organ.insidePrefab == null)) mode = ViewMode.Organ;
            if (organ != m_Organ || m_Model == null || Mode != mode)
            {
                if (mode == ViewMode.Body) LoadBody(organ); else Load(organ, mode == ViewMode.Inside);
            }
            else if (newOwner) ResetForNewOwner();
        }

        /// <summary>
        /// Organ yang sama dipakai layar lain (mis. lambung dari Library lalu soal kuis): model tidak dimuat ulang, tapi
        /// sorot bagian, tulang, zoom & putaran dari layar sebelumnya dibuang supaya tidak ikut terbawa.
        /// </summary>
        void ResetForNewOwner()
        {
            if (Mode == ViewMode.Organ || Mode == ViewMode.Inside)
            {
                if (m_PartHighlighted) HighlightPart(null, null, null);
                m_Fit.localPosition = m_BaseFitPos;
                m_Fit.localScale = m_BaseFitScale;
                m_FitAnimating = false;
            }
            else if (Mode == ViewMode.Body)
                SetBonesState(m_Organ != null && m_Organ.id == SkeletonId ? 2 : 0);
            ResetView();
            m_Camera.transform.localPosition = new Vector3(0f, 0f, -CameraDistance);
            m_Pan = Vector2.zero;
            m_LastInput = Time.unscaledTime;
        }

        Transform m_FocusHolder;

        static Bounds LocalBounds(GameObject go, Transform space)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            var toLocal = space.worldToLocalMatrix;
            Bounds b = new Bounds(); bool init = false;
            foreach (var r in rs)
            {
                var c = r.bounds.center; var e = r.bounds.extents;
                for (int i = 0; i < 8; i++)
                {
                    var p = toLocal.MultiplyPoint3x4(c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z));
                    if (!init) { b = new Bounds(p, Vector3.zero); init = true; } else b.Encapsulate(p);
                }
            }
            return b;
        }

        void FitToBounds(Bounds b)
        {
            float size = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            float s = size > 1e-5f ? 1f / size : 1f;
            m_Fit.localScale = Vector3.one * s;
            m_Fit.localPosition = -b.center * s;
        }

        public void Detach(object owner)
        {
            if (m_Owner != owner) return;
            m_Owner = null;
            m_Camera.enabled = false;
        }

        public void Resize(int width, int height)
        {
            width = Mathf.Clamp(width, 64, 1400);
            height = Mathf.Clamp(height, 64, 1400);
            if (m_RT != null && m_RT.width == width && m_RT.height == height) return;
            if (m_RT != null) { m_Camera.targetTexture = null; m_RT.Release(); Destroy(m_RT); }
            m_RT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "Organo Close-up RT",
                antiAliasing = QualitySettings.antiAliasing > 1 ? QualitySettings.antiAliasing : 4,
                useMipMap = false
            };
            m_RT.Create();
            m_Camera.targetTexture = m_RT;
            m_Camera.aspect = (float)width / height;
        }

        public void SetBackdrop(Color tint)
        {
            // Alpha 0 = transparent di atas kartu UI. RGB = warna kartu, jadi kalau pipeline
            // membuang alpha, latarnya tetap menyatu dengan desain.
            m_Camera.backgroundColor = new Color(tint.r, tint.g, tint.b, 0f);
        }

        // ------------------------------------------------------------ model slot

        public void Load(OrganInfo organ) => Load(organ, false);

        public void Load(OrganInfo organ, bool inside)
        {
            ClearModel();
            BodyMode = false;
            if (inside && (organ == null || organ.insidePrefab == null)) inside = false;
            Mode = inside ? ViewMode.Inside : ViewMode.Organ;
            m_FitAnimating = false;
            m_Organ = organ;
            if (organ == null) return;

            SetBackdrop(organ.tint);
            var prefab = inside ? organ.insidePrefab : organ.modelPrefab;
            IsPlaceholder = prefab == null;
            m_Model = IsPlaceholder
                ? OrganPlaceholder.Build(organ.id, organ.accent)
                : Instantiate(prefab);
            m_Model.name = "Model " + organ.displayName;
            m_Model.transform.SetParent(m_Fit, false);
            m_Model.transform.localPosition = Vector3.zero;
            m_Model.transform.localRotation = Quaternion.identity;
            PrepareModel(m_Model);
            FitToUnit();
            m_Norm.localScale = Vector3.one * Mathf.Max(0.05f, organ.modelScale);

            // Titik info: komponen OrganHotspot di prefab punya prioritas, kalau tidak pakai data OrganInfo.
            var inPrefab = m_Model.GetComponentsInChildren<OrganHotspot>(true);
            if (inPrefab.Length > 0)
            {
                foreach (var h in inPrefab)
                    m_Hotspots.Add(new Hotspot { anchor = h.transform, title = h.title, description = h.description });
            }
            else if (organ.hotspots != null && !inside)
            {
                foreach (var info in organ.hotspots)
                {
                    var a = new GameObject("Hotspot " + info.title).transform;
                    a.SetParent(m_Norm, false);
                    a.localPosition = info.localPosition;
                    m_Hotspots.Add(new Hotspot { anchor = a, title = info.title, description = info.description });
                }
            }
            CaptureModelMaterials();
            ResetView();
        }

        static void PrepareModel(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
            foreach (var a in go.GetComponentsInChildren<Animator>(true)) a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        /// <summary>Skala & pusatkan model apa pun ke kotak 1 unit, jadi model dari mana saja langsung pas.</summary>
        void FitToUnit()
        {
            m_Fit.localScale = Vector3.one;
            m_Fit.localPosition = Vector3.zero;
            var renderers = m_Model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            var toLocal = m_Fit.worldToLocalMatrix;
            var b = new Bounds();
            bool init = false;
            foreach (var r in renderers)
            {
                var wb = r.bounds;
                var c = wb.center; var e = wb.extents;
                for (int i = 0; i < 8; i++)
                {
                    var corner = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                    var p = toLocal.MultiplyPoint3x4(corner);
                    if (!init) { b = new Bounds(p, Vector3.zero); init = true; } else b.Encapsulate(p);
                }
            }
            float size = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (size <= 1e-5f) return;
            float s = 1f / size;
            m_Fit.localScale = Vector3.one * s;
            m_Fit.localPosition = -b.center * s;
        }

        // ------------------------------------------------------------ interaction

        public void ResetView()
        {
            m_Yaw = 0f; m_Pitch = 0f; m_Zoom = 1f; m_YawVel = 0f; m_Pan = Vector2.zero;
            m_Free = Quaternion.identity;
            ApplyRotation();
        }

        /// <summary>Geser jari: delta dalam pixel UI.</summary>
        public void Drag(Vector2 delta)
        {
            m_LastInput = Time.unscaledTime;
            if (m_FreeRotate)
                m_Free = Quaternion.AngleAxis(-delta.x * 0.45f, Vector3.up) * Quaternion.AngleAxis(-delta.y * 0.35f, Vector3.right) * m_Free;
            else
            {
                m_Yaw -= delta.x * 0.45f;
                m_Pitch = Mathf.Clamp(m_Pitch - delta.y * 0.35f, -60f, 60f);
            }
            m_YawVel = -delta.x * 0.45f / Mathf.Max(Time.unscaledDeltaTime, 0.008f);
            ApplyRotation();
        }

        public void EndDrag() => m_LastInput = Time.unscaledTime;

        public void Zoom(float factor) => ZoomAt(factor, new Vector2(0.5f, 0.5f));

        /// <summary>
        /// Perbesar / perkecil ke arah titik di layar (uv 0..1, kiri-atas): titik di bawah jari tetap di bawah jari,
        /// jadi tengkorak (atau bagian mana pun) bisa didekati langsung, tidak selalu ke tengah badan.
        /// </summary>
        public void ZoomAt(float factor, Vector2 uv)
        {
            m_LastInput = Time.unscaledTime;
            var before = ScreenOffset(uv, m_Zoom);
            m_Zoom = Mathf.Clamp(m_Zoom * factor, 0.75f, MaxZoom);
            m_Pan += before - ScreenOffset(uv, m_Zoom);
            ClampPan();
        }

        /// <summary>Geser tampilan dengan dua jari (delta dalam uv layar 0..1).</summary>
        public void PanBy(Vector2 deltaUv)
        {
            m_LastInput = Time.unscaledTime;
            var h = HalfView(m_Zoom);
            m_Pan -= new Vector2(deltaUv.x * 2f * h.x, -deltaUv.y * 2f * h.y);
            ClampPan();
        }

        /// <summary>Kembali ke tampilan utuh (zoom & geser dihapus, putaran tetap).</summary>
        public void ResetZoom()
        {
            m_Zoom = 1f;
            m_Pan = Vector2.zero;
            m_LastInput = Time.unscaledTime;
        }

        /// <summary>Setengah lebar & tinggi bidang pandang di tengah model, untuk zoom z.</summary>
        Vector2 HalfView(float zoom)
        {
            float h = CameraDistance / zoom * Mathf.Tan(m_Camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            return new Vector2(h * m_Camera.aspect, h);
        }

        Vector2 ScreenOffset(Vector2 uv, float zoom)
        {
            var h = HalfView(zoom);
            return new Vector2((uv.x - 0.5f) * 2f * h.x, (0.5f - uv.y) * 2f * h.y);
        }

        void ClampPan()
        {
            // makin besar zoom, makin jauh boleh digeser; tanpa zoom model selalu di tengah
            float max = 0.7f * Mathf.Clamp01((m_Zoom - 1f) / 1.4f);
            m_Pan = Vector2.ClampMagnitude(m_Pan, max);
        }

        void ApplyRotation()
        {
            var baseRot = m_Organ != null && Mode == ViewMode.Organ ? Quaternion.Euler(m_Organ.modelEuler)
                : m_Organ != null && Mode == ViewMode.Inside ? Quaternion.Euler(m_Organ.insideEuler)
                : Mode == ViewMode.Micro ? Quaternion.Euler(6f, -14f, 0f) : Quaternion.Euler(4f, 0f, 0f);
            m_Pivot.localRotation = (m_FreeRotate ? m_Free : Quaternion.Euler(m_Pitch, m_Yaw, 0f)) * baseRot;
        }

        /// <summary>Putaran bebas tanpa miring: hanya arah hadap mendatarnya (untuk kembali tegak saat dibiarkan).</summary>
        static Quaternion Upright(Quaternion q)
        {
            var f = q * Vector3.forward;
            f.y = 0f;
            return f.sqrMagnitude < 1e-4f ? q : Quaternion.LookRotation(f.normalized, Vector3.up);
        }

        void LateUpdate()
        {
            if (!m_Camera.enabled) return;
            float dt = Time.unscaledDeltaTime;
            bool calm = OrganoApp.Instance != null && OrganoApp.Instance.Progress.reduceMotion;
            UpdateFitAnimation(dt);
            // Library & "Lihat dalam" tidak berputar sendiri: model potongan harus tetap menghadap kamera
            bool library = Mode == ViewMode.Library || Mode == ViewMode.Inside;
            // saat diperbesar model tidak berputar sendiri (bagian yang sedang dilihat tidak lari)
            bool idle = !calm && !library && !IsZoomed && Time.unscaledTime - m_LastInput > idleDelay;
            if (library && Time.unscaledTime - m_LastInput > idleDelay)
            {
                // anak kembali menghadap depan pelan-pelan
                m_Yaw = Mathf.LerpAngle(m_Yaw, 0f, 1f - Mathf.Exp(-2f * dt));
                m_Pitch = Mathf.Lerp(m_Pitch, 0f, 1f - Mathf.Exp(-2f * dt));
                m_YawVel = 0f;
            }

            // inersia setelah dilepas, lalu putar pelan saat idle
            m_YawVel = Mathf.Lerp(m_YawVel, idle ? -idleSpinSpeed : 0f, 1f - Mathf.Exp(-3f * dt));
            if (Time.unscaledTime - m_LastInput > 0.05f)
            {
                if (m_FreeRotate) m_Free = Quaternion.AngleAxis(m_YawVel * dt, Vector3.up) * m_Free;
                else m_Yaw += m_YawVel * dt;
            }
            if (idle)
            {
                if (m_FreeRotate) m_Free = Quaternion.Slerp(m_Free, Upright(m_Free), 1f - Mathf.Exp(-1.5f * dt));
                else m_Pitch = Mathf.Lerp(m_Pitch, 0f, 1f - Mathf.Exp(-1.5f * dt));
            }
            ApplyRotation();

            var camT = m_Camera.transform;
            if (m_Zoom <= 1.02f && m_Pan != Vector2.zero) m_Pan = Vector2.Lerp(m_Pan, Vector2.zero, 1f - Mathf.Exp(-8f * dt));
            camT.localPosition = Vector3.Lerp(camT.localPosition, new Vector3(m_Pan.x, m_Pan.y, -CameraDistance / m_Zoom), 1f - Mathf.Exp(-10f * dt));

            // sedikit "hidup": jantung berdetak, paru-paru bernapas, lainnya mengambang
            if (m_Organ != null)
            {
                float t = Time.time, pulse;
                if (BodyMode)
                {
                    // organ yang dipilih "bernapas" pelan supaya mudah ditemukan
                    if (m_FocusHolder != null) m_FocusHolder.localScale = Vector3.one * (calm ? 1f : 1f + 0.08f * Mathf.Sin(t * 3f));
                    pulse = 1f;
                }
                else if (calm || Mode != ViewMode.Organ) pulse = 1f;
                else if (m_Organ.id == "jantung") pulse = 1f + 0.05f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 7.5f)), 6f);
                else if (m_Organ.id == "paru") pulse = 1f + 0.035f * Mathf.Sin(t * 1.6f);
                else pulse = 1f;
                m_Pivot.localScale = Vector3.one * pulse;
                m_Pivot.localPosition = new Vector3(0f, 0.02f * Mathf.Sin(t * 1.3f), 0f);
            }
        }

        /// <summary>Posisi titik info di viewport (0..1, origin kiri-atas) dan apakah menghadap kamera.</summary>
        public bool TryProject(int index, out Vector2 uv, out float facing)
        {
            uv = default; facing = 0f;
            if (index < 0 || index >= m_Hotspots.Count || m_Hotspots[index].anchor == null) return false;
            var world = m_Hotspots[index].anchor.position;
            var vp = m_Camera.WorldToViewportPoint(world);
            if (vp.z <= 0) return false;
            uv = new Vector2(vp.x, 1f - vp.y);
            var toCam = (m_Camera.transform.position - m_Pivot.position).normalized;
            var outward = world - m_Pivot.position;
            facing = outward.sqrMagnitude < 1e-6f ? 1f : Vector3.Dot(outward.normalized, toCam);
            return true;
        }
    }
}
