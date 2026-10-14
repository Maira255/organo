using UnityEngine;
using UnityEngine.UIElements;

namespace Organo.UI
{
    /// <summary>Token elevasi dari Style Guide Organo (bagian 6).</summary>
    public enum Elevation
    {
        /// <summary>0 2px 8px rgba(46,42,74,.06) — kartu list, kartu biasa.</summary>
        E1,
        /// <summary>0 4px 12px rgba(46,42,74,.08) — kartu koleksi, kartu bintang.</summary>
        E2,
        /// <summary>0 6px 16px rgba(46,42,74,.12) — kartu gradien, kartu skor.</summary>
        E3,
        /// <summary>0 -6px 20px rgba(46,42,74,.12) — bottom sheet.</summary>
        Sheet,
        /// <summary>0 6px 16px rgba(20,18,32,.25) — kartu di atas kamera.</summary>
        Dark,
        /// <summary>0 6px 14px rgba(139,111,232,.35) — tombol Scan (FAB).</summary>
        Fab,
    }

    /// <summary>
    /// Bayangan lembut ala CSS box-shadow untuk UI Toolkit. Elemen ini diletakkan sebagai saudara (sibling) tepat di
    /// belakang target dan hanya menggambar area di luar target, jadi tidak menimpa latar kartu.
    /// </summary>
    public class ShadowElement : VisualElement
    {
        readonly VisualElement m_Target;
        readonly float m_Y, m_Blur;
        readonly Color m_Color;
        const int Layers = 7;

        ShadowElement(VisualElement target, float y, float blur, Color color)
        {
            m_Target = target;
            m_Y = y; m_Blur = blur; m_Color = color;
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            name = "shadow";
            generateVisualContent += Draw;
        }

        public static void Attach(VisualElement target, Elevation level)
        {
            float y, blur; Color c;
            switch (level)
            {
                case Elevation.E1: y = 2; blur = 8; c = new Color(46 / 255f, 42 / 255f, 74 / 255f, 0.06f); break;
                case Elevation.E2: y = 4; blur = 12; c = new Color(46 / 255f, 42 / 255f, 74 / 255f, 0.08f); break;
                case Elevation.E3: y = 6; blur = 16; c = new Color(46 / 255f, 42 / 255f, 74 / 255f, 0.12f); break;
                case Elevation.Sheet: y = -6; blur = 20; c = new Color(46 / 255f, 42 / 255f, 74 / 255f, 0.12f); break;
                case Elevation.Dark: y = 6; blur = 16; c = new Color(20 / 255f, 18 / 255f, 32 / 255f, 0.25f); break;
                default: y = 6; blur = 14; c = new Color(139 / 255f, 111 / 255f, 232 / 255f, 0.35f); break;
            }
            var sh = new ShadowElement(target, y, blur, c);
            target.RegisterCallback<AttachToPanelEvent>(_ => sh.Place());
            target.RegisterCallback<DetachFromPanelEvent>(_ => sh.RemoveFromHierarchy());
            target.RegisterCallback<GeometryChangedEvent>(_ => sh.Sync());
            if (target.panel != null) sh.Place();
        }

        void Place()
        {
            var parent = m_Target.hierarchy.parent;
            if (parent == null) return;
            if (hierarchy.parent != parent)
            {
                RemoveFromHierarchy();
                parent.hierarchy.Insert(parent.hierarchy.IndexOf(m_Target), this);
            }
            Sync();
        }

        void Sync()
        {
            if (hierarchy.parent == null) { Place(); return; }
            var r = m_Target.layout;
            bool visible = m_Target.resolvedStyle.display != DisplayStyle.None && r.width > 0 && r.height > 0;
            style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;
            float pad = m_Blur + Mathf.Abs(m_Y);
            style.left = r.x - pad;
            style.top = r.y - pad;
            style.width = r.width + pad * 2;
            style.height = r.height + pad * 2;
            style.opacity = m_Target.resolvedStyle.opacity;
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext mgc)
        {
            var r = m_Target.layout;
            float pad = m_Blur + Mathf.Abs(m_Y);
            var inner = new Rect(pad, pad, r.width, r.height);
            float radius = Mathf.Min(m_Target.resolvedStyle.borderTopLeftRadius, Mathf.Min(r.width, r.height) * 0.5f);
            var p = mgc.painter2D;
            for (int i = 0; i < Layers; i++)
            {
                float t = i / (float)(Layers - 1);
                float grow = Mathf.Lerp(-m_Blur * 0.45f, m_Blur, t);
                var outer = new Rect(inner.x - grow, inner.y - grow + m_Y, inner.width + grow * 2, inner.height + grow * 2);
                if (outer.width <= 0 || outer.height <= 0) continue;
                var col = m_Color;
                col.a = m_Color.a * 1.5f / Layers;
                p.fillColor = col;
                p.BeginPath();
                RoundRect(p, outer, Mathf.Max(0f, radius + grow));
                RoundRect(p, inner, radius);
                p.Fill(FillRule.OddEven);
            }
        }

        static void RoundRect(Painter2D p, Rect r, float rad)
        {
            rad = Mathf.Min(rad, Mathf.Min(r.width, r.height) * 0.5f);
            if (rad < 0.5f)
            {
                p.MoveTo(new Vector2(r.xMin, r.yMin)); p.LineTo(new Vector2(r.xMax, r.yMin));
                p.LineTo(new Vector2(r.xMax, r.yMax)); p.LineTo(new Vector2(r.xMin, r.yMax)); p.ClosePath();
                return;
            }
            p.MoveTo(new Vector2(r.xMin + rad, r.yMin));
            p.ArcTo(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), rad);
            p.ArcTo(new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax), rad);
            p.ArcTo(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMin), rad);
            p.ArcTo(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), rad);
            p.ClosePath();
        }
    }
}
