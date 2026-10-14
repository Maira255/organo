using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.UIElements;

namespace Organo.UI
{
    public enum SvgSegKind { Move, Line, Cubic, Close }

    public struct SvgSeg
    {
        public SvgSegKind kind;
        public Vector2 p1, p2, p3; // Move/Line use p1; Cubic uses p1,p2 (controls) and p3 (end)
    }

    public class SvgShape
    {
        public readonly List<SvgSeg> segs = new List<SvgSeg>();
        public Color? fill;
        public Color? stroke;
        public float strokeWidth = 1f;
        public LineCap cap = LineCap.Butt;
        public LineJoin join = LineJoin.Miter;
        public bool evenOdd;
        public float[] dash;
    }

    /// <summary>
    /// Minimal SVG parser for the Organo design icons: svg/g/path/circle/ellipse/rect/line/polyline/polygon,
    /// transforms, inherited fill/stroke/opacity and inline style. Text, gradients and clip paths are ignored.
    /// Geometry is flattened into absolute viewBox coordinates so it can be drawn with Painter2D.
    /// </summary>
    public class SvgDocument
    {
        public Rect viewBox;
        public readonly List<SvgShape> shapes = new List<SvgShape>();

        static readonly Dictionary<string, SvgDocument> s_Cache = new Dictionary<string, SvgDocument>();

        public static SvgDocument Parse(string markup)
        {
            if (string.IsNullOrEmpty(markup)) return null;
            if (s_Cache.TryGetValue(markup, out var cached)) return cached;
            var doc = new SvgDocument();
            try
            {
                var xml = new XmlDocument();
                xml.LoadXml(markup);
                var root = xml.DocumentElement;
                if (root == null) return null;
                doc.viewBox = ReadViewBox(root);
                var ctx = Ctx.Default;
                ctx = ctx.Inherit(root, out _);
                foreach (XmlNode child in root.ChildNodes)
                    if (child is XmlElement e) doc.Walk(e, ctx);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Organo] SVG parse failed: " + ex.Message);
                return null;
            }
            s_Cache[markup] = doc;
            return doc;
        }

        static Rect ReadViewBox(XmlElement root)
        {
            var vb = root.GetAttribute("viewBox");
            if (!string.IsNullOrEmpty(vb))
            {
                var n = new NumberReader(vb);
                float x = n.Next(), y = n.Next(), w = n.Next(), h = n.Next();
                if (w > 0 && h > 0) return new Rect(x, y, w, h);
            }
            float.TryParse(root.GetAttribute("width"), NumberStyles.Float, CultureInfo.InvariantCulture, out var ww);
            float.TryParse(root.GetAttribute("height"), NumberStyles.Float, CultureInfo.InvariantCulture, out var hh);
            return new Rect(0, 0, ww > 0 ? ww : 24, hh > 0 ? hh : 24);
        }

        // ------------------------------------------------------------------ context

        struct Matrix
        {
            public float a, b, c, d, e, f;
            public static Matrix Identity => new Matrix { a = 1, d = 1 };
            public Vector2 Apply(Vector2 p) => new Vector2(a * p.x + c * p.y + e, b * p.x + d * p.y + f);
            public float Scale => Mathf.Sqrt(Mathf.Abs(a * d - b * c));
            public static Matrix operator *(Matrix m, Matrix n) => new Matrix
            {
                a = m.a * n.a + m.c * n.b,
                b = m.b * n.a + m.d * n.b,
                c = m.a * n.c + m.c * n.d,
                d = m.b * n.c + m.d * n.d,
                e = m.a * n.e + m.c * n.f + m.e,
                f = m.b * n.e + m.d * n.f + m.f
            };
        }

        struct Ctx
        {
            public Matrix m;
            public string fill, stroke;
            public float sw, opacity, fillOpacity, strokeOpacity;
            public LineCap cap;
            public LineJoin join;
            public bool evenOdd;
            public float[] dash;

            public static Ctx Default => new Ctx
            {
                m = Matrix.Identity, fill = "#000", stroke = "none", sw = 1, opacity = 1, fillOpacity = 1, strokeOpacity = 1,
                cap = LineCap.Butt, join = LineJoin.Miter
            };

            public Ctx Inherit(XmlElement el, out bool hidden)
            {
                var c = this;
                var style = ParseStyle(el.GetAttribute("style"));
                hidden = Get(el, style, "display") == "none" || Get(el, style, "visibility") == "hidden";
                var v = Get(el, style, "fill"); if (v != null) c.fill = v;
                v = Get(el, style, "stroke"); if (v != null) c.stroke = v;
                v = Get(el, style, "stroke-width"); if (v != null) c.sw = ParseFloat(v, c.sw);
                v = Get(el, style, "opacity"); if (v != null) c.opacity *= ParseFloat(v, 1);
                v = Get(el, style, "fill-opacity"); if (v != null) c.fillOpacity = ParseFloat(v, 1);
                v = Get(el, style, "stroke-opacity"); if (v != null) c.strokeOpacity = ParseFloat(v, 1);
                v = Get(el, style, "fill-rule"); if (v != null) c.evenOdd = v == "evenodd";
                v = Get(el, style, "stroke-linecap");
                if (v != null) c.cap = v == "round" ? LineCap.Round : LineCap.Butt;
                v = Get(el, style, "stroke-linejoin");
                if (v != null) c.join = v == "round" ? LineJoin.Round : v == "bevel" ? LineJoin.Bevel : LineJoin.Miter;
                v = Get(el, style, "stroke-dasharray");
                if (v != null)
                {
                    var nr = new NumberReader(v);
                    var list = new List<float>();
                    while (nr.HasMore) list.Add(nr.Next());
                    c.dash = list.Count > 0 && list.Exists(x => x > 0) ? (list.Count % 2 == 1 ? list.Concat(list).ToArray() : list.ToArray()) : null;
                }
                v = el.GetAttribute("transform");
                if (!string.IsNullOrEmpty(v)) c.m = c.m * ParseTransform(v);
                return c;
            }
        }

        static string Get(XmlElement el, Dictionary<string, string> style, string name)
        {
            if (style != null && style.TryGetValue(name, out var s)) return s;
            return el.HasAttribute(name) ? el.GetAttribute(name).Trim() : null;
        }

        static Dictionary<string, string> ParseStyle(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var d = new Dictionary<string, string>();
            foreach (var part in s.Split(';'))
            {
                var i = part.IndexOf(':');
                if (i > 0) d[part.Substring(0, i).Trim()] = part.Substring(i + 1).Trim();
            }
            return d;
        }

        static float ParseFloat(string s, float fallback)
        {
            s = s.Replace("px", "").Trim();
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : fallback;
        }

        static Matrix ParseTransform(string s)
        {
            var m = Matrix.Identity;
            int i = 0;
            while (i < s.Length)
            {
                int open = s.IndexOf('(', i);
                if (open < 0) break;
                int close = s.IndexOf(')', open);
                if (close < 0) break;
                var name = s.Substring(i, open - i).Trim().TrimStart(',').Trim();
                var n = new NumberReader(s.Substring(open + 1, close - open - 1));
                var args = new List<float>();
                while (n.HasMore) args.Add(n.Next());
                var t = Matrix.Identity;
                switch (name)
                {
                    case "translate":
                        t.e = args.Count > 0 ? args[0] : 0; t.f = args.Count > 1 ? args[1] : 0; break;
                    case "scale":
                        t.a = args.Count > 0 ? args[0] : 1; t.d = args.Count > 1 ? args[1] : t.a; break;
                    case "rotate":
                    {
                        float ang = (args.Count > 0 ? args[0] : 0) * Mathf.Deg2Rad;
                        var r = new Matrix { a = Mathf.Cos(ang), b = Mathf.Sin(ang), c = -Mathf.Sin(ang), d = Mathf.Cos(ang) };
                        if (args.Count >= 3)
                        {
                            var to = Matrix.Identity; to.e = args[1]; to.f = args[2];
                            var back = Matrix.Identity; back.e = -args[1]; back.f = -args[2];
                            r = to * r * back;
                        }
                        t = r; break;
                    }
                    case "matrix":
                        if (args.Count >= 6) t = new Matrix { a = args[0], b = args[1], c = args[2], d = args[3], e = args[4], f = args[5] };
                        break;
                }
                m = m * t;
                i = close + 1;
            }
            return m;
        }

        // ------------------------------------------------------------------ colours

        public static Color? ParseColor(string s, float alpha)
        {
            if (string.IsNullOrEmpty(s)) return null;
            s = s.Trim();
            if (s == "none" || s == "transparent") return null;
            Color c;
            if (s.StartsWith("rgba(") || s.StartsWith("rgb("))
            {
                var n = new NumberReader(s.Substring(s.IndexOf('(') + 1).TrimEnd(')'));
                float r = n.Next(), g = n.Next(), b = n.Next();
                float a = n.HasMore ? n.Next() : 1f;
                c = new Color(r / 255f, g / 255f, b / 255f, a);
            }
            else if (s == "white") c = Color.white;
            else if (s == "black" || s == "currentColor") c = Color.black;
            else if (!ColorUtility.TryParseHtmlString(s, out c)) return null;
            c.a *= alpha;
            return c;
        }

        // ------------------------------------------------------------------ elements

        void Walk(XmlElement el, Ctx parent)
        {
            var name = el.LocalName;
            if (name == "text" || name == "defs" || name == "clipPath" || name == "title" || name == "desc" ||
                name == "linearGradient" || name == "radialGradient" || name == "mask")
                return;

            var ctx = parent.Inherit(el, out bool hidden);
            if (hidden) return;

            if (name == "g" || name == "svg")
            {
                foreach (XmlNode child in el.ChildNodes)
                    if (child is XmlElement e) Walk(e, ctx);
                return;
            }

            var shape = new SvgShape();
            var b = new Builder(shape.segs, ctx.m);
            switch (name)
            {
                case "path": b.Path(el.GetAttribute("d")); break;
                case "circle":
                {
                    float cx = F(el, "cx"), cy = F(el, "cy"), r = F(el, "r");
                    b.Ellipse(cx, cy, r, r); break;
                }
                case "ellipse":
                    b.Ellipse(F(el, "cx"), F(el, "cy"), F(el, "rx"), F(el, "ry")); break;
                case "rect":
                {
                    float rx = el.HasAttribute("rx") ? F(el, "rx") : -1, ry = el.HasAttribute("ry") ? F(el, "ry") : -1;
                    if (rx < 0) rx = ry < 0 ? 0 : ry;
                    if (ry < 0) ry = rx;
                    b.Rect(F(el, "x"), F(el, "y"), F(el, "width"), F(el, "height"), rx, ry); break;
                }
                case "line":
                    b.MoveTo(new Vector2(F(el, "x1"), F(el, "y1")));
                    b.LineTo(new Vector2(F(el, "x2"), F(el, "y2"))); break;
                case "polyline":
                case "polygon":
                {
                    var n = new NumberReader(el.GetAttribute("points"));
                    bool first = true;
                    while (n.HasMore)
                    {
                        var p = new Vector2(n.Next(), n.HasMore ? n.Next() : 0);
                        if (first) { b.MoveTo(p); first = false; } else b.LineTo(p);
                    }
                    if (name == "polygon") b.Close();
                    break;
                }
                default: return;
            }
            if (shape.segs.Count == 0) return;

            shape.fill = ParseColor(ctx.fill, ctx.opacity * ctx.fillOpacity);
            if (name == "line" || name == "polyline") shape.fill = name == "polyline" ? shape.fill : null;
            shape.stroke = ParseColor(ctx.stroke, ctx.opacity * ctx.strokeOpacity);
            shape.strokeWidth = ctx.sw * ctx.m.Scale;
            shape.cap = ctx.cap;
            shape.join = ctx.join;
            shape.evenOdd = ctx.evenOdd;
            if (ctx.dash != null) shape.dash = ctx.dash.Select(x => x * ctx.m.Scale).ToArray();
            if (shape.fill == null && shape.stroke == null) return;
            shapes.Add(shape);
        }

        static float F(XmlElement el, string attr) => ParseFloat(el.GetAttribute(attr), 0f);

        // ------------------------------------------------------------------ geometry builder

        class Builder
        {
            readonly List<SvgSeg> m_Out;
            readonly Matrix m_M;
            Vector2 m_Cur, m_Start, m_LastCtrl;
            char m_LastCmd;

            public Builder(List<SvgSeg> output, Matrix m) { m_Out = output; m_M = m; }

            public void MoveTo(Vector2 p) { m_Cur = m_Start = p; m_Out.Add(new SvgSeg { kind = SvgSegKind.Move, p1 = m_M.Apply(p) }); }
            public void LineTo(Vector2 p) { m_Cur = p; m_Out.Add(new SvgSeg { kind = SvgSegKind.Line, p1 = m_M.Apply(p) }); }
            public void CubicTo(Vector2 c1, Vector2 c2, Vector2 p)
            {
                m_Cur = p; m_LastCtrl = c2;
                m_Out.Add(new SvgSeg { kind = SvgSegKind.Cubic, p1 = m_M.Apply(c1), p2 = m_M.Apply(c2), p3 = m_M.Apply(p) });
            }
            public void QuadTo(Vector2 c, Vector2 p)
            {
                var c1 = m_Cur + 2f / 3f * (c - m_Cur);
                var c2 = p + 2f / 3f * (c - p);
                CubicTo(c1, c2, p);
                m_LastCtrl = c;
            }
            public void Close() { m_Out.Add(new SvgSeg { kind = SvgSegKind.Close }); m_Cur = m_Start; }

            public void Ellipse(float cx, float cy, float rx, float ry)
            {
                if (rx <= 0 || ry <= 0) return;
                const float k = 0.5522848f;
                float ox = rx * k, oy = ry * k;
                MoveTo(new Vector2(cx + rx, cy));
                CubicTo(new Vector2(cx + rx, cy + oy), new Vector2(cx + ox, cy + ry), new Vector2(cx, cy + ry));
                CubicTo(new Vector2(cx - ox, cy + ry), new Vector2(cx - rx, cy + oy), new Vector2(cx - rx, cy));
                CubicTo(new Vector2(cx - rx, cy - oy), new Vector2(cx - ox, cy - ry), new Vector2(cx, cy - ry));
                CubicTo(new Vector2(cx + ox, cy - ry), new Vector2(cx + rx, cy - oy), new Vector2(cx + rx, cy));
                Close();
            }

            public void Rect(float x, float y, float w, float h, float rx, float ry)
            {
                if (w <= 0 || h <= 0) return;
                rx = Mathf.Min(rx, w / 2); ry = Mathf.Min(ry, h / 2);
                if (rx <= 0 || ry <= 0)
                {
                    MoveTo(new Vector2(x, y)); LineTo(new Vector2(x + w, y)); LineTo(new Vector2(x + w, y + h)); LineTo(new Vector2(x, y + h)); Close();
                    return;
                }
                const float k = 0.5522848f;
                float ox = rx * k, oy = ry * k;
                MoveTo(new Vector2(x + rx, y));
                LineTo(new Vector2(x + w - rx, y));
                CubicTo(new Vector2(x + w - rx + ox, y), new Vector2(x + w, y + ry - oy), new Vector2(x + w, y + ry));
                LineTo(new Vector2(x + w, y + h - ry));
                CubicTo(new Vector2(x + w, y + h - ry + oy), new Vector2(x + w - rx + ox, y + h), new Vector2(x + w - rx, y + h));
                LineTo(new Vector2(x + rx, y + h));
                CubicTo(new Vector2(x + rx - ox, y + h), new Vector2(x, y + h - ry + oy), new Vector2(x, y + h - ry));
                LineTo(new Vector2(x, y + ry));
                CubicTo(new Vector2(x, y + ry - oy), new Vector2(x + rx - ox, y), new Vector2(x + rx, y));
                Close();
            }

            public void Path(string d)
            {
                if (string.IsNullOrEmpty(d)) return;
                var r = new NumberReader(d);
                char cmd = ' ';
                while (true)
                {
                    r.SkipSeparators();
                    if (!r.HasChars) break;
                    if (r.PeekIsCommand(out var c)) { cmd = c; r.Advance(); }
                    else if (cmd == ' ') break;
                    bool rel = char.IsLower(cmd);
                    Vector2 o = rel ? m_Cur : Vector2.zero;
                    switch (char.ToUpperInvariant(cmd))
                    {
                        case 'M':
                            MoveTo(o + new Vector2(r.Next(), r.Next()));
                            cmd = rel ? 'l' : 'L'; // subsequent pairs are implicit lineto
                            break;
                        case 'L': LineTo(o + new Vector2(r.Next(), r.Next())); break;
                        case 'H': LineTo(new Vector2((rel ? m_Cur.x : 0) + r.Next(), m_Cur.y)); break;
                        case 'V': LineTo(new Vector2(m_Cur.x, (rel ? m_Cur.y : 0) + r.Next())); break;
                        case 'C':
                        {
                            var c1 = o + new Vector2(r.Next(), r.Next());
                            var c2 = o + new Vector2(r.Next(), r.Next());
                            var p = o + new Vector2(r.Next(), r.Next());
                            CubicTo(c1, c2, p); break;
                        }
                        case 'S':
                        {
                            var c1 = (m_LastCmd == 'C' || m_LastCmd == 'S') ? 2 * m_Cur - m_LastCtrl : m_Cur;
                            var c2 = o + new Vector2(r.Next(), r.Next());
                            var p = o + new Vector2(r.Next(), r.Next());
                            CubicTo(c1, c2, p); break;
                        }
                        case 'Q':
                        {
                            var qc = o + new Vector2(r.Next(), r.Next());
                            var p = o + new Vector2(r.Next(), r.Next());
                            QuadTo(qc, p); break;
                        }
                        case 'T':
                        {
                            var tc = (m_LastCmd == 'Q' || m_LastCmd == 'T') ? 2 * m_Cur - m_LastCtrl : m_Cur;
                            var p = o + new Vector2(r.Next(), r.Next());
                            QuadTo(tc, p); break;
                        }
                        case 'A':
                        {
                            float rx = r.Next(), ry = r.Next(), rot = r.Next();
                            bool large = r.NextFlag(), sweep = r.NextFlag();
                            var p = o + new Vector2(r.Next(), r.Next());
                            Arc(rx, ry, rot, large, sweep, p); break;
                        }
                        case 'Z': Close(); break;
                        default: return;
                    }
                    m_LastCmd = char.ToUpperInvariant(cmd);
                    if (m_LastCmd == 'Z') cmd = ' ';
                }
            }

            void Arc(float rx, float ry, float phiDeg, bool large, bool sweep, Vector2 p)
            {
                var p0 = m_Cur;
                if (rx == 0 || ry == 0 || p0 == p) { LineTo(p); return; }
                rx = Mathf.Abs(rx); ry = Mathf.Abs(ry);
                float phi = phiDeg * Mathf.Deg2Rad, cos = Mathf.Cos(phi), sin = Mathf.Sin(phi);
                float dx2 = (p0.x - p.x) / 2, dy2 = (p0.y - p.y) / 2;
                float x1p = cos * dx2 + sin * dy2, y1p = -sin * dx2 + cos * dy2;
                float lambda = x1p * x1p / (rx * rx) + y1p * y1p / (ry * ry);
                if (lambda > 1) { float s = Mathf.Sqrt(lambda); rx *= s; ry *= s; }
                float num = rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p;
                float den = rx * rx * y1p * y1p + ry * ry * x1p * x1p;
                float coef = (large != sweep ? 1 : -1) * Mathf.Sqrt(Mathf.Max(0, num / Mathf.Max(den, 1e-9f)));
                float cxp = coef * rx * y1p / ry, cyp = -coef * ry * x1p / rx;
                float cx = cos * cxp - sin * cyp + (p0.x + p.x) / 2, cy = sin * cxp + cos * cyp + (p0.y + p.y) / 2;
                var u = new Vector2((x1p - cxp) / rx, (y1p - cyp) / ry);
                var v = new Vector2((-x1p - cxp) / rx, (-y1p - cyp) / ry);
                float theta1 = Angle(new Vector2(1, 0), u);
                float dTheta = Angle(u, v);
                if (!sweep && dTheta > 0) dTheta -= 2 * Mathf.PI;
                else if (sweep && dTheta < 0) dTheta += 2 * Mathf.PI;
                int n = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(dTheta) / (Mathf.PI / 2) - 1e-4f));
                float delta = dTheta / n, t = 4f / 3f * Mathf.Tan(delta / 4);
                for (int i = 0; i < n; i++)
                {
                    float a1 = theta1 + i * delta, a2 = a1 + delta;
                    var e1 = Pt(a1); var e2 = Pt(a2);
                    var c1 = e1 + t * Der(a1);
                    var c2 = e2 - t * Der(a2);
                    CubicTo(c1, c2, i == n - 1 ? p : e2);
                }

                Vector2 Pt(float a) => new Vector2(cx + rx * Mathf.Cos(a) * cos - ry * Mathf.Sin(a) * sin,
                                                  cy + rx * Mathf.Cos(a) * sin + ry * Mathf.Sin(a) * cos);
                Vector2 Der(float a) => new Vector2(-rx * Mathf.Sin(a) * cos - ry * Mathf.Cos(a) * sin,
                                                   -rx * Mathf.Sin(a) * sin + ry * Mathf.Cos(a) * cos);
            }

            static float Angle(Vector2 a, Vector2 b) => Mathf.Atan2(a.x * b.y - a.y * b.x, a.x * b.x + a.y * b.y);
        }

        // ------------------------------------------------------------------ number tokenizer

        class NumberReader
        {
            readonly string m_S;
            int m_I;
            public NumberReader(string s) { m_S = s ?? ""; }

            public bool HasChars => m_I < m_S.Length;
            public bool HasMore { get { SkipSeparators(); return m_I < m_S.Length && IsNumStart(m_S[m_I]); } }
            public void Advance() => m_I++;

            public void SkipSeparators()
            {
                while (m_I < m_S.Length && (char.IsWhiteSpace(m_S[m_I]) || m_S[m_I] == ',')) m_I++;
            }

            public bool PeekIsCommand(out char c)
            {
                c = m_I < m_S.Length ? m_S[m_I] : ' ';
                return char.IsLetter(c) && c != 'e' && c != 'E';
            }

            static bool IsNumStart(char c) => char.IsDigit(c) || c == '-' || c == '+' || c == '.';

            public bool NextFlag()
            {
                SkipSeparators();
                if (m_I < m_S.Length && (m_S[m_I] == '0' || m_S[m_I] == '1')) return m_S[m_I++] == '1';
                return Next() != 0;
            }

            public float Next()
            {
                SkipSeparators();
                int start = m_I;
                if (m_I < m_S.Length && (m_S[m_I] == '-' || m_S[m_I] == '+')) m_I++;
                bool dot = false;
                while (m_I < m_S.Length)
                {
                    char c = m_S[m_I];
                    if (char.IsDigit(c)) { m_I++; continue; }
                    if (c == '.' && !dot) { dot = true; m_I++; continue; }
                    if ((c == 'e' || c == 'E') && m_I + 1 < m_S.Length && (char.IsDigit(m_S[m_I + 1]) || m_S[m_I + 1] == '-' || m_S[m_I + 1] == '+'))
                    {
                        m_I += 2;
                        while (m_I < m_S.Length && char.IsDigit(m_S[m_I])) m_I++;
                    }
                    break;
                }
                if (m_I == start) { m_I++; return 0; }
                float.TryParse(m_S.Substring(start, m_I - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var f);
                return f;
            }
        }
    }
}
