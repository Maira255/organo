using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Organo.UI
{
    /// <summary>Small builder helpers so screens read like the design's auto-layout frames.</summary>
    public static class UI
    {
        // ------------------------------------------------------------ colours

        static readonly Dictionary<string, Color> s_Colors = new Dictionary<string, Color>();

        public static Color C(string hex)
        {
            if (s_Colors.TryGetValue(hex, out var c)) return c;
            ColorUtility.TryParseHtmlString(hex, out c);
            s_Colors[hex] = c;
            return c;
        }

        public static Color C(string hex, float alpha) { var c = C(hex); c.a = alpha; return c; }

        public const string Teal = "#1AB8A6", TealDark = "#12857A", TealInk = "#0F6B61", TealSoft = "#D5F3EF";
        public const string Purple = "#8B6FE8", PurpleDark = "#6F52D1", PurpleSoft = "#E8E2FB";
        public const string Pink = "#EF476F", PinkDark = "#D6385E", PinkSoft = "#FDE0E7";
        public const string Yellow = "#F2B705", YellowSoft = "#FFF1BF";
        public const string Ink = "#2E2A4A", Muted = "#6B6785", Faint = "#8C89A3", Line = "#ECEAF2", Surface = "#FAFAFA";

        // ------------------------------------------------------------ elements

        static readonly HashSet<string> s_PillClasses = new HashSet<string>
        {
            "btn", "chip", "pill", "progress", "progress__fill", "nav__icon", "viewport3d__hint", "toast", "sheet__grip", "switch"
        };

        public static VisualElement Div(params string[] classes)
        {
            var v = new VisualElement();
            foreach (var c in classes)
            {
                v.AddToClassList(c);
                if (s_PillClasses.Contains(c)) AutoPill(v);
            }
            return v;
        }

        /// <summary>
        /// radius-pill (999px) versi CSS: sudut = setengah sisi terpendek. UI Toolkit menskalakan radius besar
        /// jadi elips, jadi radius dihitung ulang setiap ukuran elemen berubah.
        /// </summary>
        public static T AutoPill<T>(T el) where T : VisualElement
        {
            if (el.ClassListContains("auto-pill")) return el;
            el.AddToClassList("auto-pill");
            el.RegisterCallback<GeometryChangedEvent>(e =>
            {
                float r = Mathf.Max(0f, Mathf.Min(e.newRect.width, e.newRect.height) * 0.5f);
                if (Mathf.Abs(el.resolvedStyle.borderTopLeftRadius - r) < 0.01f) return;
                el.style.borderTopLeftRadius = r; el.style.borderTopRightRadius = r;
                el.style.borderBottomLeftRadius = r; el.style.borderBottomRightRadius = r;
            });
            return el;
        }

        public static VisualElement Row(float gap = 0f, params string[] classes)
        {
            var v = Div(classes);
            v.AddToClassList("row");
            if (gap > 0) v.SetGap(gap, true);
            return v;
        }

        public static VisualElement Col(float gap = 0f, params string[] classes)
        {
            var v = Div(classes);
            v.AddToClassList("col");
            if (gap > 0) v.SetGap(gap, false);
            return v;
        }

        public static Label Text(string text, params string[] classes)
        {
            var l = new Label(text);
            foreach (var c in classes) l.AddToClassList(c);
            return l;
        }

        public static SvgImage Icon(string icon, float size, float height = 0f) => new SvgImage(icon, size, height);

        public static VisualElement Button(string text, Action onClick, params string[] classes)
        {
            var b = Div("btn");
            foreach (var c in classes) b.AddToClassList(c);
            b.Add(Text(text));
            b.Tap(onClick);
            return b;
        }

        public static VisualElement IconButton(string icon, Action onClick, float iconSize = 22f, string tooltip = null)
        {
            var b = Div("icon-btn");
            b.Add(Icon(icon, iconSize));
            b.Tap(onClick);
            if (!string.IsNullOrEmpty(tooltip)) b.tooltip = tooltip;
            return b;
        }

        public static ScrollView Scroll(bool horizontal = false)
        {
            var s = new ScrollView(horizontal ? ScrollViewMode.Horizontal : ScrollViewMode.Vertical);
            s.AddToClassList("scroll");
            s.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            s.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            s.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
            s.mouseWheelScrollSize = 36f;
            s.AddManipulator(new DragScroll(s));
            return s;
        }

        public static VisualElement Space(float h) { var v = new VisualElement(); v.style.height = h; v.style.flexShrink = 0; return v; }
        public static VisualElement Flex() { var v = new VisualElement(); v.style.flexGrow = 1; return v; }

        public static VisualElement Progress(float t, string fill = Teal, string track = "#E6F6F4", float height = 6f)
        {
            var p = Div("progress");
            p.style.height = height;
            p.style.backgroundColor = C(track);
            var f = Div("progress__fill");
            f.style.width = Length.Percent(Mathf.Clamp01(t) * 100f);
            f.style.backgroundColor = C(fill);
            p.Add(f);
            return p;
        }

        public static VisualElement Pill(string text, string bg, string fg)
        {
            var p = Div("pill");
            p.style.backgroundColor = C(bg);
            var l = Text(text);
            l.style.color = C(fg);
            p.Add(l);
            return p;
        }

        // ------------------------------------------------------------ fluent style

        public static T Tap<T>(this T el, Action onClick) where T : VisualElement
        {
            if (onClick == null) return el;
            el.AddToClassList("pressable");
            el.focusable = true;
            // ketukan yang tidak mengunci pointer, jadi rail/halaman/sheet tetap bisa digeser mulai dari kartu ini
            el.AddManipulator(new TapGesture(() =>
            {
                OrganoAudio.Click();
                onClick();
            }));
            return el;
        }

        public static T Bg<T>(this T el, string hex) where T : VisualElement { el.style.backgroundColor = C(hex); return el; }
        public static T Bg<T>(this T el, Color c) where T : VisualElement { el.style.backgroundColor = c; return el; }
        public static T Fg<T>(this T el, string hex) where T : VisualElement { el.style.color = C(hex); return el; }
        public static T Fg<T>(this T el, Color c) where T : VisualElement { el.style.color = c; return el; }
        public static T Size<T>(this T el, float w, float h) where T : VisualElement { el.style.width = w; el.style.height = h; el.style.flexShrink = 0; return el; }
        public static T W<T>(this T el, float w) where T : VisualElement { el.style.width = w; return el; }
        public static T H<T>(this T el, float h) where T : VisualElement { el.style.height = h; return el; }
        public static T MinH<T>(this T el, float h) where T : VisualElement { el.style.minHeight = h; return el; }
        public static T FontSize<T>(this T el, float s) where T : VisualElement { el.style.fontSize = s; return el; }
        public static T Bold<T>(this T el) where T : VisualElement { el.style.unityFontStyleAndWeight = FontStyle.Bold; return el; }
        public static T Grow<T>(this T el, float g = 1f) where T : VisualElement { el.style.flexGrow = g; el.style.flexShrink = 1; el.style.flexBasis = 0; return el; }
        public static T NoShrink<T>(this T el) where T : VisualElement { el.style.flexShrink = 0; return el; }
        public static T AlignSelf<T>(this T el, Align a) where T : VisualElement { el.style.alignSelf = a; return el; }
        public static T Align<T>(this T el, Align a) where T : VisualElement { el.style.alignItems = a; return el; }
        public static T Justify<T>(this T el, Justify j) where T : VisualElement { el.style.justifyContent = j; return el; }
        public static T Center<T>(this T el) where T : VisualElement { el.style.alignItems = UnityEngine.UIElements.Align.Center; el.style.justifyContent = UnityEngine.UIElements.Justify.Center; return el; }
        public static T TextCenter<T>(this T el) where T : VisualElement { el.style.unityTextAlign = TextAnchor.MiddleCenter; return el; }
        public static T Opacity<T>(this T el, float o) where T : VisualElement { el.style.opacity = o; return el; }

        public static T Radius<T>(this T el, float r) where T : VisualElement
        {
            if (r >= 500f) return AutoPill(el);
            el.style.borderTopLeftRadius = r; el.style.borderTopRightRadius = r;
            el.style.borderBottomLeftRadius = r; el.style.borderBottomRightRadius = r;
            return el;
        }

        public static T Border<T>(this T el, float w, string hex) where T : VisualElement => el.Border(w, C(hex));

        public static T Border<T>(this T el, float w, Color c) where T : VisualElement
        {
            el.style.borderTopWidth = w; el.style.borderRightWidth = w; el.style.borderBottomWidth = w; el.style.borderLeftWidth = w;
            el.style.borderTopColor = c; el.style.borderRightColor = c; el.style.borderBottomColor = c; el.style.borderLeftColor = c;
            return el;
        }

        public static T Pad<T>(this T el, float all) where T : VisualElement => el.Pad(all, all, all, all);
        public static T Pad<T>(this T el, float v, float h) where T : VisualElement => el.Pad(v, h, v, h);

        public static T Pad<T>(this T el, float t, float r, float b, float l) where T : VisualElement
        {
            el.style.paddingTop = t; el.style.paddingRight = r; el.style.paddingBottom = b; el.style.paddingLeft = l;
            return el;
        }

        public static T Margin<T>(this T el, float t, float r, float b, float l) where T : VisualElement
        {
            el.style.marginTop = t; el.style.marginRight = r; el.style.marginBottom = b; el.style.marginLeft = l;
            return el;
        }

        public static T Abs<T>(this T el, float? left = null, float? top = null, float? right = null, float? bottom = null) where T : VisualElement
        {
            el.style.position = Position.Absolute;
            if (left.HasValue) el.style.left = left.Value;
            if (top.HasValue) el.style.top = top.Value;
            if (right.HasValue) el.style.right = right.Value;
            if (bottom.HasValue) el.style.bottom = bottom.Value;
            return el;
        }

        public static T Gradient<T>(this T el, string from, string to, float angleDeg = 135f) where T : VisualElement
        {
            el.style.backgroundImage = new StyleBackground(GradientTexture.Get(C(from), C(to), angleDeg));
            el.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
            return el;
        }

        public static T Visible<T>(this T el, bool visible) where T : VisualElement
        {
            el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            return el;
        }

        /// <summary>Elevasi dari style guide (bayangan lembut di bawah elemen).</summary>
        public static T Elev<T>(this T el, Elevation level) where T : VisualElement
        {
            ShadowElement.Attach(el, level);
            return el;
        }

        /// <summary>Adds children separated by a fixed gap (USS has no flex gap).</summary>
        public static VisualElement Kids(this VisualElement parent, params VisualElement[] children)
        {
            foreach (var c in children) if (c != null) parent.Add(c);
            return parent;
        }

        // ------------------------------------------------------------ gap emulation

        class GapInfo { public float gap; public bool row; }

        public static void SetGap(this VisualElement el, float gap, bool row)
        {
            el.userData = new GapInfo { gap = gap, row = row };
            el.RegisterCallback<GeometryChangedEvent>(_ => ApplyGap(el));
            el.schedule.Execute(() => ApplyGap(el));
        }

        static void ApplyGap(VisualElement el)
        {
            if (!(el.userData is GapInfo g)) return;
            bool first = true;
            foreach (var child in el.Children())
            {
                if (child.resolvedStyle.display == DisplayStyle.None || child.resolvedStyle.position == Position.Absolute) continue;
                float m = first ? 0 : g.gap;
                if (g.row) { if (child.style.marginLeft.value.value != m) child.style.marginLeft = m; }
                else if (child.style.marginTop.value.value != m) child.style.marginTop = m;
                first = false;
            }
        }
    }
}
