using System;
using System.Collections.Generic;
using Organo.Data;
using Organo.UI;
using UnityEngine;
using UnityEngine.UIElements;
using U = Organo.UI.UI;

namespace Organo.ThreeD
{
    /// <summary>
    /// Tempat 3D di UI: menampilkan RenderTexture dari <see cref="OrganViewer3D"/>, meneruskan geser (putar),
    /// cubit / scroll (zoom), ketuk (event <see cref="Tapped"/>) dan menggambar titik info di atas model.
    /// </summary>
    public class OrganViewport : VisualElement
    {
        readonly OrganViewer3D m_Viewer;
        readonly OrganInfo m_Organ;
        readonly VisualElement m_Image;
        readonly VisualElement m_HotLayer;
        readonly VisualElement m_Callout;
        readonly Label m_CalloutTitle, m_CalloutDesc;
        readonly List<VisualElement> m_Dots = new List<VisualElement>();
        readonly Dictionary<int, Vector2> m_Pointers = new Dictionary<int, Vector2>();
        IVisualElementScheduledItem m_Tick;
        float m_PinchDist;
        Vector2 m_PinchMid;
        bool m_Moved;
        Vector2 m_DownPos;
        int m_SelectedHotspot;

        public event Action Tapped;
        /// <summary>Titik diketuk (mode Library): kunci area tubuh / id organ.</summary>
        public event Action<string> HotspotTapped;
        /// <summary>Anak 3D di halaman awal Library.</summary>
        public bool LibraryMode { get; }
        readonly Color m_Backdrop;
        public OrganViewer3D Viewer => m_Viewer;
        /// <summary>Titik yang sedang dipilih (kunci), untuk menampilkan label di mode Library.</summary>
        public string SelectedKey { get; set; }
        public bool ShowHotspots { get; set; } = true;
        public bool ShowCallout { get; set; } = true;
        /// <summary>Organ utuh, potongan ("Lihat dalam"), atau di dalam tubuh anak.</summary>
        public OrganViewer3D.ViewMode ViewMode { get; private set; } = OrganViewer3D.ViewMode.Organ;
        public bool BodyMode => ViewMode == OrganViewer3D.ViewMode.Body;

        public void SetBodyMode(bool on) => SetViewMode(on ? OrganViewer3D.ViewMode.Body : OrganViewer3D.ViewMode.Organ);

        public void SetViewMode(OrganViewer3D.ViewMode mode)
        {
            if (ViewMode == mode || LibraryMode) return;
            ViewMode = mode;
            m_SelectedHotspot = mode == OrganViewer3D.ViewMode.Body ? 0 : (CalloutOnTapOnly ? -1 : 0);
            if (panel != null && m_Viewer != null)
            {
                m_Viewer.Attach(this, m_Organ, mode);
                UpdateBadge();
            }
        }
        /// <summary>Tampilkan callout titik ke-i (-1 = sembunyikan).</summary>
        public void SelectHotspot(int index) => m_SelectedHotspot = index;
        /// <summary>Callout hanya tampil setelah titik info diketuk (close-up Library).</summary>
        public bool CalloutOnTapOnly { get; set; }
        /// <summary>Model bisa diputar 360° ke segala arah (hasil scan), lihat <see cref="OrganViewer3D.FreeRotate"/>.</summary>
        public bool FreeRotate { get; set; }

        /// <summary>
        /// Ukuran gambar 3D mengikuti ukuran tata letak (bukan ukuran setelah <c>style.scale</c>): viewport yang diperbesar /
        /// diperkecil terus-menerus (model mengikuti kartu di kamera biasa) tidak membuat ulang tekstur render setiap frame.
        /// </summary>
        public bool IgnoreScale { get; set; }

        /// <summary>Pengali resolusi gambar 3D (mis. 0,75 di layar kamera supaya HP tidak cepat panas).</summary>
        public float RenderScale { get; set; } = 1f;

        /// <summary>Viewport anak 3D untuk halaman awal Library.</summary>
        public static OrganViewport Library(OrganViewer3D viewer, Color backdrop) =>
            new OrganViewport(viewer, backdrop);

        OrganViewport(OrganViewer3D viewer, Color backdrop) : this(viewer, null, true, "Geser untuk memutar badan")
        {
            LibraryMode = true;
            m_Backdrop = backdrop;
            ShowCallout = false;
            AddToClassList("viewport3d--library");
        }

        public OrganViewport(OrganViewer3D viewer, OrganInfo organ, bool showHint = false, string hintText = "Geser untuk memutar organ")
        {
            m_Viewer = viewer;
            m_Organ = organ;
            AddToClassList("viewport3d");
            pickingMode = PickingMode.Position;

            m_Image = new VisualElement { pickingMode = PickingMode.Ignore };
            m_Image.AddToClassList("fill");
            Add(m_Image);

            m_HotLayer = new VisualElement { pickingMode = PickingMode.Ignore };
            m_HotLayer.AddToClassList("fill");
            Add(m_HotLayer);

            m_Callout = U.Col(2, "callout");
            m_Callout.pickingMode = PickingMode.Ignore;
            m_CalloutTitle = U.Text("", "b").FontSize(12);
            m_CalloutDesc = U.Text("", "t-muted").FontSize(10.5f);
            m_Callout.Add(m_CalloutTitle);
            m_Callout.Add(m_CalloutDesc);
            m_Callout.style.display = DisplayStyle.None;
            m_HotLayer.Add(m_Callout);

            if (viewer == null)
            {
                Add(U.Text("Viewer 3D belum siap", "t-caption").TextCenter());
                return;
            }

            if (showHint)
            {
                var hint = U.Row(6, "viewport3d__hint");
                hint.pickingMode = PickingMode.Ignore;
                hint.Add(U.Icon("rotate", 12).Tinted(Color.white));
                hint.Add(U.Text(hintText));
                Add(hint);
            }

            RegisterCallback<AttachToPanelEvent>(_ => OnAttach());
            RegisterCallback<DetachFromPanelEvent>(_ => OnDetach());
            RegisterCallback<PointerDownEvent>(OnDown);
            RegisterCallback<PointerMoveEvent>(OnMove);
            RegisterCallback<PointerUpEvent>(OnUp);
            RegisterCallback<PointerCancelEvent>(e => { m_Pointers.Remove(e.pointerId); this.ReleasePointer(e.pointerId); });
            RegisterCallback<WheelEvent>(e =>
            {
                // zoom ke arah kursor (bukan selalu ke tengah model)
                if (Mathf.Abs(e.delta.y) > 0.01f) m_Viewer.ZoomAt(e.delta.y > 0 ? 0.94f : 1.06f, ToUv(e.mousePosition));
                e.StopImmediatePropagation();
            });
        }

        void OnAttach()
        {
            if (m_Viewer == null) return;
            m_Viewer.FreeRotate = FreeRotate && !LibraryMode;
            if (LibraryMode) m_Viewer.AttachLibrary(this, m_Backdrop);
            else m_Viewer.Attach(this, m_Organ, ViewMode);
            m_SelectedHotspot = CalloutOnTapOnly && !BodyMode ? -1 : 0;
            // gambar 3D langsung dipasang (tanpa menunggu Tick pertama) supaya tidak ada satu frame kosong / berkedip
            if (m_Viewer.Texture != null) m_Image.style.backgroundImage = Background.FromRenderTexture(m_Viewer.Texture);
            m_Tick = schedule.Execute(Tick).Every(0);
            UpdateBadge();
        }

        void UpdateBadge()
        {
            this.Q("slot-badge")?.RemoveFromHierarchy();
            if (m_Viewer.IsPlaceholder && !BodyMode && !LibraryMode)
            {
                var badge = U.Div("slot-badge");
                badge.pickingMode = PickingMode.Ignore;
                badge.Add(U.Text("Model 3D sementara · isi slot \"Model Prefab\" di asset " + (m_Organ != null ? m_Organ.displayName : "organ")));
                badge.name = "slot-badge";
                if (this.Q("slot-badge") == null) Add(badge);
            }
        }

        void OnDetach()
        {
            m_Tick?.Pause();
            m_Viewer?.Detach(this);
        }

        void Tick()
        {
            if (panel == null || m_Viewer == null) return;
            var r = IgnoreScale ? new Rect(worldBound.position, layout.size) : worldBound;
            if (r.width < 2 || r.height < 2 || float.IsNaN(r.width)) return;
            float panelW = panel.visualTree.worldBound.width;
            float ratio = (panelW > 0 ? UnityEngine.Screen.width / panelW : 2f) * RenderScale;
            m_Viewer.Resize(Mathf.RoundToInt(r.width * ratio), Mathf.RoundToInt(r.height * ratio));
            var tex = m_Viewer.Texture;
            if (tex != null && m_Image.resolvedStyle.backgroundImage.renderTexture != tex)
                m_Image.style.backgroundImage = Background.FromRenderTexture(tex);
            UpdateHotspots(r.width, r.height);
        }

        void UpdateHotspots(float w, float h)
        {
            var spots = m_Viewer.Hotspots;
            while (m_Dots.Count < spots.Count)
            {
                int index = m_Dots.Count;
                var dot = U.Div("hotspot");
                var ring = U.Div("hotspot__ring");
                ring.pickingMode = PickingMode.Ignore;
                dot.Add(ring);
                if (LibraryMode)
                {
                    var label = U.Text("", "hotspot__label");
                    label.pickingMode = PickingMode.Ignore;
                    dot.Add(label);
                    // dot tidak menangkap ketukan; ketukan dicari ke titik terdekat (target besar untuk jari anak)
                    dot.pickingMode = PickingMode.Ignore;
                }
                else
                    dot.RegisterCallback<PointerDownEvent>(e => { m_SelectedHotspot = m_SelectedHotspot == index && CalloutOnTapOnly ? -1 : index; e.StopPropagation(); });
                m_HotLayer.Insert(0, dot);
                m_Dots.Add(dot);
            }
            float pulse = 1f + 0.6f * Mathf.Repeat(Time.unscaledTime * 0.6f, 1f);
            bool calloutShown = false;
            for (int i = 0; i < m_Dots.Count; i++)
            {
                var dot = m_Dots[i];
                Vector2 p = default; float f = 0f;
                bool ok = ShowHotspots && i < spots.Count && m_Viewer.TryProject(i, out p, out f) && f > -0.15f;
                if (!ok) { dot.style.display = DisplayStyle.None; continue; }
                dot.style.display = DisplayStyle.Flex;
                dot.style.left = p.x * w;
                dot.style.top = p.y * h;
                dot.style.opacity = Mathf.Clamp01((f + 0.15f) * 3f);
                var ring = dot[0];
                if (LibraryMode)
                {
                    var key = spots[i].key;
                    bool region = m_Viewer.FocusedRegion == null;
                    bool sel = !region && key == SelectedKey;
                    var label = (Label)dot[1];
                    label.text = spots[i].title;
                    label.style.display = region || sel ? DisplayStyle.Flex : DisplayStyle.None;
                    dot.EnableInClassList("hotspot--selected", sel);
                    // label area tubuh di kanan, label organ di atas titik
                    dot.EnableInClassList("hotspot--region", region);
                }
                else dot.pickingMode = PickingMode.Position;
                ring.style.scale = new Scale(new Vector3(pulse, pulse, 1));
                ring.style.opacity = 1.6f - pulse;

                if (ShowCallout && i == m_SelectedHotspot && f > 0.2f)
                {
                    calloutShown = true;
                    m_CalloutTitle.text = spots[i].title;
                    m_CalloutDesc.text = spots[i].description;
                    float cx = p.x * w;
                    bool right = cx < w * 0.55f;
                    m_Callout.style.left = right ? Mathf.Min(cx + 18, w - 136) : Mathf.Max(8, cx - 146);
                    m_Callout.style.top = Mathf.Clamp(p.y * h - 22, 8, h - 60);
                }
            }
            m_Callout.style.display = calloutShown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ------------------------------------------------------------ input

        void OnDown(PointerDownEvent e)
        {
            m_Pointers[e.pointerId] = e.position;
            this.CapturePointer(e.pointerId);
            if (m_Pointers.Count == 1) { m_Moved = false; m_DownPos = e.position; }
            if (m_Pointers.Count == 2) { m_PinchDist = PinchDistance(); m_PinchMid = PinchMid(); }
            e.StopPropagation();
        }

        void OnMove(PointerMoveEvent e)
        {
            if (!m_Pointers.TryGetValue(e.pointerId, out var last)) return;
            Vector2 pos = e.position;
            m_Pointers[e.pointerId] = pos;
            if (m_Pointers.Count >= 2)
            {
                // cubit = zoom ke titik tengah dua jari; dua jari digeser bersama = menggeser tampilan
                float d = PinchDistance();
                var mid = PinchMid();
                if (m_PinchDist > 1f && d > 1f) m_Viewer.ZoomAt(d / m_PinchDist, ToUv(mid));
                var size = layout.size;
                if (size.x > 1f && size.y > 1f) m_Viewer.PanBy(new Vector2((mid.x - m_PinchMid.x) / size.x, (mid.y - m_PinchMid.y) / size.y));
                m_PinchDist = d;
                m_PinchMid = mid;
                m_Moved = true;
            }
            else
            {
                if (!m_Moved && (pos - m_DownPos).magnitude > 6f) m_Moved = true;
                if (m_Moved) m_Viewer.Drag(pos - last);
            }
            e.StopPropagation();
        }

        void OnUp(PointerUpEvent e)
        {
            if (!m_Pointers.Remove(e.pointerId)) return;
            this.ReleasePointer(e.pointerId);
            m_Viewer.EndDrag();
            if (m_Pointers.Count == 0 && !m_Moved)
            {
                string key = LibraryMode ? NearestHotspot(this.WorldToLocal(e.position)) : null;
                if (key != null) HotspotTapped?.Invoke(key);
                else Tapped?.Invoke();
            }
            e.StopPropagation();
        }

        string NearestHotspot(Vector2 local)
        {
            var spots = m_Viewer.Hotspots;
            float best = 56f; string key = null;
            float w = layout.width, h = layout.height;
            for (int i = 0; i < spots.Count; i++)
            {
                if (!m_Viewer.TryProject(i, out var p, out var f) || f < -0.15f) continue;
                float d = Vector2.Distance(local, new Vector2(p.x * w, p.y * h));
                if (d < best) { best = d; key = spots[i].key; }
            }
            return key;
        }

        /// <summary>Posisi panel → uv viewport (0..1, kiri-atas).</summary>
        Vector2 ToUv(Vector2 panelPos)
        {
            var local = this.WorldToLocal(panelPos);
            var size = layout.size;
            return new Vector2(size.x > 1f ? local.x / size.x : 0.5f, size.y > 1f ? local.y / size.y : 0.5f);
        }

        Vector2 PinchMid()
        {
            Vector2 sum = default; int n = 0;
            foreach (var p in m_Pointers.Values) { if (n < 2) sum += p; n++; }
            return n > 0 ? sum / Mathf.Min(n, 2) : default;
        }

        float PinchDistance()
        {
            Vector2 a = default, b = default; int i = 0;
            foreach (var p in m_Pointers.Values) { if (i == 0) a = p; else if (i == 1) b = p; i++; }
            return Vector2.Distance(a, b);
        }
    }
}
