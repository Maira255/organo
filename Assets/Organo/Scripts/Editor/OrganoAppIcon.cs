using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Organo.EditorTools
{
    /// <summary>
    /// Ikon aplikasi Organo (bukan ikon Unity). Gambar dibuat oleh Tools/ikon/buat_ikon.py ke Assets/Organo/Icon:
    /// adaptive icon Android (lapisan belakang gradasi teal-ungu + lapisan depan maskot dalam sudut bingkai scan),
    /// ikon bulat, dan ikon biasa. Dipasang otomatis setiap build APK (pengaturan ikon ada di ProjectSettings yang tidak di-commit).
    /// </summary>
    public static class OrganoAppIcon
    {
        const string Dir = "Assets/Organo/Icon/";
        const string Full = Dir + "Organo_Icon.png";
        const string Round = Dir + "Organo_Icon_Round.png";
        const string Background = Dir + "Organo_Icon_Background.png";
        const string Foreground = Dir + "Organo_Icon_Foreground.png";

        [MenuItem("Organo/Build/Pasang Ikon Aplikasi", priority = 92)]
        public static void ApplyMenu() => Debug.Log("[Organo] " + Apply());

        /// <returns>ringkasan ikon yang dipasang.</returns>
        public static string Apply()
        {
            foreach (var p in new[] { Full, Round, Background, Foreground }) Configure(p);
            var full = AssetDatabase.LoadAssetAtPath<Texture2D>(Full);
            var round = AssetDatabase.LoadAssetAtPath<Texture2D>(Round);
            var bg = AssetDatabase.LoadAssetAtPath<Texture2D>(Background);
            var fg = AssetDatabase.LoadAssetAtPath<Texture2D>(Foreground);
            if (full == null || round == null || bg == null || fg == null)
                return "Ikon belum ada di " + Dir + " (jalankan python Tools/ikon/buat_ikon.py).";

            // ikon bawaan (semua platform)
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { full }, IconKind.Application);

            // Android: adaptive (2 lapisan), round, legacy — semua ukuran kerapatan layar
            int set = 0;
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
            {
                var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                string name = kind.ToString();
                foreach (var icon in icons)
                {
                    if (icon.maxLayerCount >= 2) icon.SetTextures(bg, fg);
                    else icon.SetTexture(name.Contains("Round") ? round : full);
                    set++;
                }
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
            }
            AssetDatabase.SaveAssets();
            var kinds = string.Join(", ", PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android).Select(k => k.ToString()));
            return "Ikon Organo dipasang (" + set + " ukuran Android: " + kinds + ").";
        }

        static void Configure(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            bool dirty = imp.textureType != TextureImporterType.Default || imp.mipmapEnabled || imp.textureCompression != TextureImporterCompression.Uncompressed
                         || !imp.alphaIsTransparency || imp.npotScale != TextureImporterNPOTScale.None;
            if (!dirty) return;
            imp.textureType = TextureImporterType.Default;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.maxTextureSize = 1024;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        /// <summary>
        /// ARCore Depth tidak dipakai Organo. Kalau disetel "Required", manifest APK mewajibkan fitur depth
        /// (uses-feature com.google.ar.core.depth) dan Play Store menyembunyikan aplikasi dari HP tanpa sensor/depth API.
        /// </summary>
        public static string MakeArCoreDepthOptional()
        {
            const string path = "Assets/XR/Settings/AR Core Settings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (settings == null) return "ARCore settings tidak ditemukan";
            var so = new SerializedObject(settings);
            var depth = so.FindProperty("m_Depth");
            if (depth == null) return "ARCore settings: field depth tidak ada";
            if (depth.enumValueIndex != 1)
            {
                depth.enumValueIndex = 1; // 0 = Required, 1 = Optional
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
            return "ARCore depth: Optional";
        }
    }
}
