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
                    if (shape.dash != null) BuildDashed(p, shape.segs, s, off, shape.dash);
                    else Build(p, shape.segs, s, off);
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

        /// <summary>Garis putus-putus: path diratakan jadi polyline lalu dipotong sesuai dasharray.</summary>
        static void BuildDashed(Painter2D p, List<SvgSeg> segs, float s, Vector2 off, float[] dash)
        {
            p.BeginPath();
            var poly = new List<Vector2>();
            Vector2 start = default, cur = default;
            void Flush()
            {
                if (poly.Count < 2) { poly.Clear(); return; }
                int di = 0;
                float left = dash[0] * s;
                bool on = true;
                if (on) p.MoveTo(poly[0]);
                for (int i = 1; i < poly.Count; i++)
                {
                    var a = poly[i - 1]; var b = poly[i];
                    float len = Vector2.Distance(a, b);
                    float pos = 0f;
                    while (len - pos > left)
                    {
                        pos += left;
                        var q = Vector2.Lerp(a, b, pos / Mathf.Max(len, 1e-5f));
                        if (on) p.LineTo(q); else p.MoveTo(q);
                        on = !on;
                        di = (di + 1) % dash.Length;
                        left = Mathf.Max(0.01f, dash[di] * s);
                    }
                    left -= len - pos;
                    if (on) p.LineTo(b);
                }
                poly.Clear();
            }
            foreach (var seg in segs)
            {
                switch (seg.kind)
                {
                    case SvgSegKind.Move:
                        Flush();
                        cur = start = seg.p1 * s + off;
                        poly.Add(cur);
                        break;
                    case SvgSegKind.Line:
                        cur = seg.p1 * s + off;
                        poly.Add(cur);
                        break;
                    case SvgSegKind.Cubic:
                    {
                        var c1 = seg.p1 * s + off; var c2 = seg.p2 * s + off; var e = seg.p3 * s + off;
                        var from = cur;
                        for (int k = 1; k <= 12; k++)
                        {
                            float t = k / 12f, u = 1 - t;
                            poly.Add(u * u * u * from + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * e);
                        }
                        cur = e;
                        break;
                    }
                    case SvgSegKind.Close:
                        poly.Add(start);
                        cur = start;
                        break;
                }
            }
            Flush();
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
