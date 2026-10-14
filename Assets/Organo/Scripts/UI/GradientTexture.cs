using System.Collections.Generic;
using UnityEngine;

namespace Organo.UI
{
    /// <summary>Linear gradients for UI Toolkit backgrounds (USS has no linear-gradient()).</summary>
    public static class GradientTexture
    {
        static readonly Dictionary<string, Texture2D> s_Cache = new Dictionary<string, Texture2D>();

        /// <summary>Cahaya bulat lembut (pengganti filter: blur) — warna di tengah, memudar ke transparan.</summary>
        public static Texture2D Radial(Color center)
        {
            var key = "radial" + ColorUtility.ToHtmlStringRGBA(center);
            if (s_Cache.TryGetValue(key, out var tex) && tex != null) return tex;
            const int n = 64;
            tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2((x + 0.5f) / n, (y + 0.5f) / n), new Vector2(0.5f, 0.5f)) * 2f;
                float a = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(d));
                px[y * n + x] = new Color(center.r, center.g, center.b, center.a * a * a);
            }
            tex.SetPixels(px);
            tex.Apply(false, true);
            s_Cache[key] = tex;
            return tex;
        }

        public static Texture2D Get(Color from, Color to, float cssAngleDeg)
        {
            var key = ColorUtility.ToHtmlStringRGBA(from) + ColorUtility.ToHtmlStringRGBA(to) + cssAngleDeg;
            if (s_Cache.TryGetValue(key, out var tex) && tex != null) return tex;

            const int n = 64;
            tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            // CSS angle: 0deg points up, 90deg points right. Texture y goes up.
            float rad = cssAngleDeg * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
            float half = (Mathf.Abs(dir.x) + Mathf.Abs(dir.y)) * 0.5f;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var p = new Vector2((x + 0.5f) / n - 0.5f, (y + 0.5f) / n - 0.5f);
                float t = Mathf.Clamp01(Vector2.Dot(p, dir) / (2 * half) + 0.5f);
                px[y * n + x] = Color.Lerp(from, to, t);
            }
            tex.SetPixels(px);
            tex.Apply(false, true);
            s_Cache[key] = tex;
            return tex;
        }
    }
}
