using System;
using UnityEngine;
using UnityEngine.UIElements;
using U = Organo.UI.UI;

namespace Organo.UI
{
    /// <summary>
    /// Bottom sheet yang bisa ditarik ke atas (terbuka) dan ke bawah (ringkas), lalu "menempel" ke posisi terdekat.
    /// Bagian <see cref="Peek"/> selalu terlihat; <see cref="More"/> (bisa digulir) muncul saat sheet ditarik ke atas.
    /// Ketuk pegangan untuk membuka/menutup.
    /// </summary>
    public class DraggableSheet : VisualElement
    {
        const float Threshold = 6f;

        public VisualElement Peek { get; }
        public ScrollView More { get; }
        public bool IsOpen { get; private set; }
        /// <summary>Tinggi bagian yang terlihat saat ringkas (untuk memberi ruang di layar).</summary>
        public event Action<float> PeekHeightChanged;
        public event Action<bool> OpenChanged;

        readonly VisualElement m_Grip;
        float m_Collapsed;      // jarak geser ke bawah saat ringkas
        float m_Offset;
        float m_BottomInset;

        /// <summary>Area aman bawah: latar sheet tetap menempel ke tepi layar, isinya naik sebesar ini.</summary>
        public float BottomInset
        {
            get => m_BottomInset;
            set
            {
                if (Mathf.Approximately(value, m_BottomInset)) return;
                m_BottomInset = value;
                style.paddingBottom = value;
                m_Collapsed = -1f; // paksa ukur ulang
                Measure();
            }
        }

        public DraggableSheet()
        {
            AddToClassList("drag-sheet");
            style.position = Position.Absolute;
            style.left = 0; style.right = 0; style.bottom = 0;
            style.height = Length.Percent(80);
            style.backgroundColor = U.C("#FFFFFF");
            style.borderTopLeftRadius = 22; style.borderTopRightRadius = 22;
            style.paddingLeft = 20; style.paddingRight = 20;
            style.flexDirection = FlexDirection.Column;
            this.Elev(Elevation.Sheet);

            var gripArea = U.Div().H(22).Center().NoShrink();
            m_Grip = U.Div("sheet__grip");
            m_Grip.style.marginTop = 0; m_Grip.style.marginBottom = 0;
            gripArea.Add(m_Grip);
            gripArea.tooltip = "Tarik ke atas untuk info lengkap";
            Add(gripArea);

            Peek = U.Col(8).NoShrink();
            Add(Peek);

            More = U.Scroll();
            More.style.flexGrow = 1;
            More.style.marginLeft = -20; More.style.marginRight = -20;
            More.contentContainer.style.paddingTop = 26;
            More.contentContainer.style.paddingBottom = 24;
            Add(More);

            RegisterCallback<GeometryChangedEvent>(_ => Measure());
            Peek.RegisterCallback<GeometryChangedEvent>(_ => Measure());
            this.AddManipulator(new SheetDrag(this));
            // ketuk pegangan = buka / tutup
            gripArea.RegisterCallback<ClickEvent>(_ => { if (!m_JustDragged) SetOpen(!IsOpen); });
        }

        bool m_JustDragged;
        float m_StartOffset;

        void Measure()
        {
            float h = layout.height;
            if (float.IsNaN(h) || h <= 0 || float.IsNaN(Peek.layout.yMax)) return;
            float peekH = Peek.layout.yMax + 10f + m_BottomInset;
            float collapsed = Mathf.Max(0f, h - peekH);
            if (Mathf.Abs(collapsed - m_Collapsed) < 0.5f) return;
            m_Collapsed = collapsed;
            Snap(IsOpen, false);
            PeekHeightChanged?.Invoke(peekH);
        }

        public void SetOpen(bool open) => Snap(open, true);

        void Snap(bool open, bool animate)
        {
            bool changed = open != IsOpen;
            IsOpen = open;
            SetOffset(open ? 0f : m_Collapsed, animate);
            m_Grip.style.backgroundColor = U.C(open ? "#BDBACE" : "#D9D7E3");
            if (!open) More.scrollOffset = Vector2.zero;
            if (changed) OpenChanged?.Invoke(open);
        }

        void SetOffset(float y, bool animate)
        {
            m_Offset = y;
            style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { "translate" };
            style.transitionDuration = new System.Collections.Generic.List<TimeValue> { animate ? 0.28f : 0f };
            style.transitionTimingFunction = new System.Collections.Generic.List<EasingFunction> { EasingMode.EaseOutCubic };
            style.translate = new Translate(0, y);
            More.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { "opacity" };
            More.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { animate ? 0.28f : 0f };
            FadeMore(y);
        }

        /// <summary>Isi lengkap muncul pelan saat sheet ditarik naik; saat ringkas tidak mengintip di area aman bawah.</summary>
        void FadeMore(float offset) => More.style.opacity = m_Collapsed <= 0f ? 1f : Mathf.Clamp01((m_Collapsed - offset) / 40f);

        /// <summary>Gestur tarik naik/turun sheet (dilacak dari akar panel supaya tidak pernah tersangkut).</summary>
        class SheetDrag : TrackedGesture
        {
            readonly DraggableSheet m_Sheet;
            public SheetDrag(DraggableSheet sheet) { m_Sheet = sheet; }

            protected override int Decide(Vector2 d)
            {
                if (Mathf.Abs(d.y) < Threshold && Mathf.Abs(d.x) < Threshold) return 0;
                if (Mathf.Abs(d.x) > Mathf.Abs(d.y) * 1.5f) return -1; // geser ke samping: biarkan rail
                if (Mathf.Abs(d.y) < Threshold) return 0;
                // saat terbuka, isi yang bisa digulir tetap digulir (kecuali sudah di paling atas dan ditarik ke bawah)
                if (m_Sheet.IsOpen && Within(StartTarget, m_Sheet.More, m_Sheet) && (d.y < 0 || m_Sheet.More.scrollOffset.y > 1f)) return -1;
                return 1;
            }

            protected override void OnDragStart()
            {
                m_Sheet.m_StartOffset = m_Sheet.m_Offset;
                m_Sheet.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { 0f };
                m_Sheet.More.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { 0f };
            }

            protected override void OnDrag(Vector2 total, Vector2 delta)
            {
                // sedikit "karet" kalau ditarik melewati batas
                float off = m_Sheet.m_StartOffset + total.y;
                if (off < 0) off *= 0.25f;
                if (off > m_Sheet.m_Collapsed) off = m_Sheet.m_Collapsed + (off - m_Sheet.m_Collapsed) * 0.25f;
                m_Sheet.m_Offset = off;
                m_Sheet.style.translate = new Translate(0, off);
                m_Sheet.FadeMore(off);
            }

            protected override void OnDragEnd(Vector2 total, Vector2 velocity)
            {
                bool open = Mathf.Abs(velocity.y) > 500f ? velocity.y < 0 : m_Sheet.m_Offset < m_Sheet.m_Collapsed * 0.5f;
                m_Sheet.Snap(open, true);
                // klik setelah menyeret tidak dihitung sebagai ketukan pegangan
                m_Sheet.m_JustDragged = true;
                m_Sheet.schedule.Execute(() => m_Sheet.m_JustDragged = false).ExecuteLater(60);
            }
        }
    }
}
