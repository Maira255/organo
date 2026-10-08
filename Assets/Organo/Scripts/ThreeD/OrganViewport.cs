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
        bool m_Moved;
        Vector2 m_DownPos;
        int m_SelectedHotspot;

        public event Action Tapped;
        public bool ShowHotspots { get; set; } = true;
        public bool ShowCallout { get; set; } = true;

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
            RegisterCallback<WheelEvent>(e => { m_Viewer.Zoom(e.delta.y > 0 ? 0.92f : 1.08f); e.StopPropagation(); });
        }

        void OnAttach()
        {
            if (m_Viewer == null) return;
            m_Viewer.Attach(this, m_Organ);
            m_SelectedHotspot = 0;
            m_Tick = schedule.Execute(Tick).Every(0);
            if (m_Viewer.IsPlaceholder)
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
            var r = worldBound;
            if (r.width < 2 || r.height < 2) return;
            float panelW = panel.visualTree.worldBound.width;
            float ratio = panelW > 0 ? UnityEngine.Screen.width / panelW : 2f;
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
                dot.RegisterCallback<PointerDownEvent>(e => { m_SelectedHotspot = index; e.StopPropagation(); });
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
                dot.pickingMode = PickingMode.Position;
                var ring = dot[0];
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
            if (m_Pointers.Count == 2) m_PinchDist = PinchDistance();
            e.StopPropagation();
        }

        void OnMove(PointerMoveEvent e)
        {
            if (!m_Pointers.TryGetValue(e.pointerId, out var last)) return;
            Vector2 pos = e.position;
            m_Pointers[e.pointerId] = pos;
            if (m_Pointers.Count >= 2)
            {
                float d = PinchDistance();
                if (m_PinchDist > 1f && d > 1f) m_Viewer.Zoom(d / m_PinchDist);
                m_PinchDist = d;
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
            if (m_Pointers.Count == 0 && !m_Moved) Tapped?.Invoke();
            e.StopPropagation();
        }

        float PinchDistance()
        {
            Vector2 a = default, b = default; int i = 0;
            foreach (var p in m_Pointers.Values) { if (i == 0) a = p; else if (i == 1) b = p; i++; }
            return Vector2.Distance(a, b);
        }
    }
}
