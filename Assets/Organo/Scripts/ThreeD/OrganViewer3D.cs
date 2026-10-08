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
    public class OrganViewer3D : MonoBehaviour
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
        float m_LastInput = -100f;
        readonly List<Hotspot> m_Hotspots = new List<Hotspot>();

        public struct Hotspot
        {
            public Transform anchor;
            public string title;
            public string description;
        }

        public RenderTexture Texture => m_RT;
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

        public void Attach(object owner, OrganInfo organ)
        {
            m_Owner = owner;
            m_Camera.enabled = true;
            if (organ != m_Organ || m_Model == null) Load(organ);
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

        public void Load(OrganInfo organ)
        {
            m_Organ = organ;
            if (m_Model != null) Destroy(m_Model);
            m_Hotspots.Clear();
            for (int i = m_Norm.childCount - 1; i >= 0; i--)
                if (m_Norm.GetChild(i) != m_Fit) Destroy(m_Norm.GetChild(i).gameObject);
            if (organ == null) return;

            SetBackdrop(organ.tint);
            IsPlaceholder = organ.modelPrefab == null;
            m_Model = IsPlaceholder
                ? OrganPlaceholder.Build(organ.id, organ.accent)
                : Instantiate(organ.modelPrefab);
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
            else if (organ.hotspots != null)
            {
                foreach (var info in organ.hotspots)
                {
                    var a = new GameObject("Hotspot " + info.title).transform;
                    a.SetParent(m_Norm, false);
                    a.localPosition = info.localPosition;
                    m_Hotspots.Add(new Hotspot { anchor = a, title = info.title, description = info.description });
                }
            }
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
            m_Yaw = 0f; m_Pitch = 0f; m_Zoom = 1f; m_YawVel = 0f;
            ApplyRotation();
        }

        /// <summary>Geser jari: delta dalam pixel UI.</summary>
        public void Drag(Vector2 delta)
        {
            m_LastInput = Time.unscaledTime;
            m_Yaw -= delta.x * 0.45f;
            m_Pitch = Mathf.Clamp(m_Pitch - delta.y * 0.35f, -60f, 60f);
            m_YawVel = -delta.x * 0.45f / Mathf.Max(Time.unscaledDeltaTime, 0.008f);
            ApplyRotation();
        }

        public void EndDrag() => m_LastInput = Time.unscaledTime;

        public void Zoom(float factor)
        {
            m_LastInput = Time.unscaledTime;
            m_Zoom = Mathf.Clamp(m_Zoom * factor, 0.6f, 2.4f);
        }

        void ApplyRotation()
        {
            var baseRot = m_Organ != null ? Quaternion.Euler(m_Organ.modelEuler) : Quaternion.identity;
            m_Pivot.localRotation = Quaternion.Euler(m_Pitch, m_Yaw, 0f) * baseRot;
        }

        void LateUpdate()
        {
            if (!m_Camera.enabled) return;
            float dt = Time.unscaledDeltaTime;
            bool idle = Time.unscaledTime - m_LastInput > idleDelay;

            // inersia setelah dilepas, lalu putar pelan saat idle
            m_YawVel = Mathf.Lerp(m_YawVel, idle ? -idleSpinSpeed : 0f, 1f - Mathf.Exp(-3f * dt));
            if (Time.unscaledTime - m_LastInput > 0.05f) m_Yaw += m_YawVel * dt;
            if (idle) m_Pitch = Mathf.Lerp(m_Pitch, 0f, 1f - Mathf.Exp(-1.5f * dt));
            ApplyRotation();

            var camT = m_Camera.transform;
            camT.localPosition = Vector3.Lerp(camT.localPosition, new Vector3(0, 0, -CameraDistance / m_Zoom), 1f - Mathf.Exp(-10f * dt));

            // sedikit "hidup": jantung berdetak, paru-paru bernapas, lainnya mengambang
            if (m_Organ != null)
            {
                float t = Time.time, pulse;
                if (m_Organ.id == "jantung") pulse = 1f + 0.05f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 7.5f)), 6f);
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
