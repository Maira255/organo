using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Organo.EditorTools
{
    /// <summary>
    /// Build APK Android untuk dicoba di HP (scan kartu cetak sungguhan). Hasil: Builds/Android/Organo.apk
    /// dan ringkasan di Library/OrganoPreview/build_android.txt. Butuh modul Android Build Support.
    /// </summary>
    public static class OrganoAndroidBuild
    {
        public const string ApkPath = "Builds/Android/Organo.apk";
        public const string PackageId = "com.organo.belajarorgan";

        [MenuItem("Organo/Build/APK Android (untuk dicoba di HP)", priority = 90)]
        public static void BuildApk() => Build(false);

        [MenuItem("Organo/Build/APK Android Development (dengan akun uji)", priority = 91)]
        public static void BuildDevApk() => Build(true);

        static void Build(bool development)
        {
            string prevName = PlayerSettings.productName, prevCompany = PlayerSettings.companyName;
            var sb = new StringBuilder();
            try
            {
                PlayerSettings.productName = "Organo";
                PlayerSettings.companyName = "Organo";
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                EditorUserBuildSettings.buildAppBundle = false;
                sb.AppendLine(OrganoAppIcon.Apply());
                sb.AppendLine(OrganoAppIcon.MakeArCoreDepthOptional());
                // ARCore memperkirakan sendiri ukuran kartu (kertas 6,3 cm maupun kartu besar di layar laptop)
                OrganoSetup.SetCardLibrarySpecifySize(false);
                sb.AppendLine("Kartu AR: ukuran fisik diperkirakan ARCore (tidak dipatok 6,3 cm)");
                sb.AppendLine("Ciri kartu untuk HP tanpa AR: " + CardSignatureBuilder.Build() + " gambar kartu");
                var path = development ? ApkPath.Replace(".apk", "-dev.apk") : ApkPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Organo/Scenes/Organo.unity" },
                    locationPathName = path,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = development ? BuildOptions.Development : BuildOptions.None,
                });
                var s = report.summary;
                sb.Insert(0, "Build Android " + (development ? "(development)" : "(rilis)") + ": " + s.result + "\n");
                sb.AppendLine("File: " + Path.GetFullPath(path));
                if (File.Exists(path)) sb.AppendLine("Ukuran APK: " + (new FileInfo(path).Length / 1048576f).ToString("0.0") + " MB");
                sb.AppendLine("Lama build: " + s.totalTime);
                sb.AppendLine("Paket: " + PackageId + " · minSdk 25 · ARM64 · IL2CPP · ARCore opsional");
                foreach (var step in report.steps)
                    foreach (var m in step.messages.Where(m => m.type == LogType.Error || m.type == LogType.Exception))
                        sb.AppendLine("ERROR: " + m.content);
            }
            finally
            {
                PlayerSettings.productName = prevName;
                PlayerSettings.companyName = prevCompany;
                OrganoSetup.SetCardLibrarySpecifySize(true); // iOS (ARKit) & simulasi XR butuh ukuran kartu
                AssetDatabase.SaveAssets();
            }
            Directory.CreateDirectory("Library/OrganoPreview");
            File.WriteAllText("Library/OrganoPreview/build_android.txt", sb.ToString());
            Debug.Log("[Organo] " + sb);
        }
    }
}
