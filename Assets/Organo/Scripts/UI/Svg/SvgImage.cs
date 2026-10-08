using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Organo.UI
{
    /// <summary>
    /// Draws an SVG (from <see cref="OrganoIcons"/> or raw markup) with Painter2D, scaled to fit the element.
    /// Monochrome icons can be recoloured with <see cref="Tint"/> or the USS custom property --icon-color.
    /// </summary>
    public class SvgImage : VisualElement
    {
        static readonly CustomStyleProperty<Color> s_IconColor = new CustomStyleProperty<Color>("--icon-color");

        SvgDocument m_Doc;
        Color? m_Tint;
        Color? m_StyleTint;
        string m_Icon;

        public SvgImage()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("svg");
            generateVisualContent += OnGenerate;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyle);
        }

        public SvgImage(string icon, float size = 0f, float height = 0f) : this()
        {
            Icon = icon;
            if (size > 0f)
            {
                style.width = size;
                style.height = height > 0f ? height : size;
                style.flexShrink = 0;
            }
        }

        public string Icon
        {
            get => m_Icon;
            set
            {
                m_Icon = value;
                m_Doc = string.IsNullOrEmpty(value) ? null : SvgDocument.Parse(OrganoIcons.Get(value));
                MarkDirtyRepaint();
            }
        }

        public void SetMarkup(string svg)
        {
            m_Icon = null;
            m_Doc = SvgDocument.Parse(svg);
            MarkDirtyRepaint();
        }

        public Color? Tint
        {
            get => m_Tint;
            set { m_Tint = value; MarkDirtyRepaint(); }
        }

        public SvgImage Tinted(Color c) { Tint = c; return this; }

        /// <summary>Aspect ratio (w/h) of the current viewBox, or 1.</summary>
        public float Aspect => m_Doc != null && m_Doc.viewBox.height > 0 ? m_Doc.viewBox.width / m_Doc.viewBox.height : 1f;

        void OnCustomStyle(CustomStyleResolvedEvent e)
        {
            m_StyleTint = e.customStyle.TryGetValue(s_IconColor, out var c) ? c : (Color?)null;
            MarkDirtyRepaint();
        }

        void OnGenerate(MeshGenerationContext mgc)
        {
            var doc = m_Doc;
            if (doc == null) return;
            var r = contentRect;
            var vb = doc.viewBox;
            if (r.width <= 0 || r.height <= 0 || vb.width <= 0 || vb.height <= 0) return;

            float s = Mathf.Min(r.width / vb.width, r.height / vb.height);
            var off = new Vector2(r.x + (r.width - vb.width * s) * 0.5f - vb.x * s,
                                  r.y + (r.height - vb.height * s) * 0.5f - vb.y * s);
            var tint = m_Tint ?? m_StyleTint;
            var p = mgc.painter2D;

            foreach (var shape in doc.shapes)
            {
                if (shape.fill.HasValue)
                {
                    p.fillColor = Recolor(shape.fill.Value, tint);
                    Build(p, shape.segs, s, off);
                    p.Fill(shape.evenOdd ? FillRule.OddEven : FillRule.NonZero);
                }
                if (shape.stroke.HasValue && shape.strokeWidth > 0f)
                {
                    p.strokeColor = Recolor(shape.stroke.Value, tint);
                    p.lineWidth = Mathf.Max(0.5f, shape.strokeWidth * s);
                    p.lineCap = shape.cap;
                    p.lineJoin = shape.join;
                    Build(p, shape.segs, s, off);
                    p.Stroke();
                }
            }
        }

        static Color Recolor(Color c, Color? tint)
        {
            if (!tint.HasValue) return c;
            var t = tint.Value;
            return new Color(t.r, t.g, t.b, t.a * c.a);
        }

        static void Build(Painter2D p, List<SvgSeg> segs, float s, Vector2 off)
        {
            p.BeginPath();
            bool open = false;
            foreach (var seg in segs)
            {
                switch (seg.kind)
                {
                    case SvgSegKind.Move:
                        p.MoveTo(seg.p1 * s + off); open = true; break;
                    case SvgSegKind.Line:
                        if (!open) { p.MoveTo(seg.p1 * s + off); open = true; } else p.LineTo(seg.p1 * s + off);
                        break;
                    case SvgSegKind.Cubic:
                        p.BezierCurveTo(seg.p1 * s + off, seg.p2 * s + off, seg.p3 * s + off); break;
                    case SvgSegKind.Close:
                        if (open) p.ClosePath();
                        break;
                }
            }
        }
    }
}
