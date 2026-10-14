using UnityEngine;
using UnityEngine.UIElements;

namespace Organo.UI
{
    /// <summary>
    /// Geser isi ScrollView dengan menekan lalu menarik (mouse / pena / jari), juga di atas kartu yang bisa diketuk.
    /// Setelah jari bergerak beberapa piksel ke arah gulir, ketukan pada kartu dibatalkan dan isi ikut tergeser,
    /// lalu meluncur pelan (inersia) saat dilepas. Kalau arah geser berbeda (mis. rail kiri-kanan di dalam halaman
    /// yang digulir ke bawah), gestur diserahkan ke ScrollView yang cocok.
    /// Elemen dengan kelas <c>drag-source</c> (kartu kuis yang diseret) atau model 3D tidak ikut menggeser.
    /// </summary>
    public class DragScroll : TrackedGesture
    {
        const float Threshold = 8f;
        readonly ScrollView m_View;
        float m_Velocity;
        IVisualElementScheduledItem m_Glide;

        public DragScroll(ScrollView view) { m_View = view; }

        bool Horizontal => m_View.mode == ScrollViewMode.Horizontal;

        protected override bool Accept(PointerDownEvent e) => !HasClassUpTo(e.target, m_View, "drag-source", "viewport3d");

        protected override void OnBegin() => m_Glide?.Pause();

        protected override int Decide(Vector2 d)
        {
            float along = Horizontal ? Mathf.Abs(d.x) : Mathf.Abs(d.y);
            float across = Horizontal ? Mathf.Abs(d.y) : Mathf.Abs(d.x);
            if (along < Threshold && across < Threshold) return 0;
            if (across > along * 1.2f) return -1;
            return along >= Threshold ? 1 : 0;
        }

        protected override void OnDrag(Vector2 total, Vector2 delta) => ScrollBy(-(Horizontal ? delta.x : delta.y));

        protected override void OnDragEnd(Vector2 total, Vector2 velocity)
        {
            m_Velocity = -(Horizontal ? velocity.x : velocity.y);
            if (Mathf.Abs(m_Velocity) < 30f) return;
            m_Glide?.Pause();
            m_Glide = m_View.schedule.Execute(() =>
            {
                float dt = Mathf.Max(Time.unscaledDeltaTime, 0.008f);
                ScrollBy(m_Velocity * dt);
                m_Velocity *= Mathf.Exp(-5f * dt);
                if (Mathf.Abs(m_Velocity) < 20f) m_Glide.Pause();
            }).Every(0);
        }

        void ScrollBy(float amount)
        {
            var o = m_View.scrollOffset;
            if (Horizontal)
            {
                float max = Mathf.Max(0f, m_View.contentContainer.layout.width - m_View.contentViewport.layout.width);
                o.x = Mathf.Clamp(o.x + amount, 0f, max);
            }
            else
            {
                float max = Mathf.Max(0f, m_View.contentContainer.layout.height - m_View.contentViewport.layout.height);
                o.y = Mathf.Clamp(o.y + amount, 0f, max);
            }
            m_View.scrollOffset = o;
        }
    }
}
