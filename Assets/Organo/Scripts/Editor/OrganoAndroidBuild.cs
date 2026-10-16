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

        /// <summary>Folder 3 versi APK yang masing-masing punya keperluan sendiri (lihat <see cref="BuildTigaVersi"/>).</summary>
        public const string TigaVersiDir = "Builds/Android/Organo_3-Versi";
        /// <summary>APK full offline versi sebelum perbaikan kamera (dibuat 15 Okt 2026), disalin sebagai versi A.</summary>
        public const string ArsipOffline = "Builds/Android/arsip/Organo_2026-10-15_kuis-terkunci.apk";

        /// <summary>
        /// Tiga versi APK:
        /// A. Biasa, full offline — salinan APK versi sebelum perbaikan kamera (cadangan, tidak dibuild ulang).
        /// B. Perbaikan kamera untuk HP tanpa AR + kelas online/offline.
        /// C. Biasa (pengaturan kamera AR seperti versi A, simbol ORGANO_KAMERA_LAMA) + kelas online/offline.
        /// </summary>
        [MenuItem("Organo/Build/3 Versi APK (A offline, B kamera biasa + online, C online)", priority = 92)]
        public static void BuildTigaVersi()
        {
            Directory.CreateDirectory(TigaVersiDir);
            var log = new StringBuilder();
            string a = TigaVersiDir + "/A_Organo_Biasa_FullOffline.apk";
            if (File.Exists(ArsipOffline)) { File.Copy(ArsipOffline, a, true); log.AppendLine("A: disalin dari " + ArsipOffline); }
            else log.AppendLine("A: TIDAK ADA arsip " + ArsipOffline);
            log.AppendLine("B: " + BuildKe("B_Organo_KameraBiasa_Online-Offline.apk", new[] { "ORGANO_ONLINE" }));
            log.AppendLine("C: " + BuildKe("C_Organo_Biasa_Online-Offline.apk", new[] { "ORGANO_KAMERA_LAMA", "ORGANO_ONLINE" }));
            File.WriteAllText("Builds/Android/_kerja_3versi/build_log.txt", log.ToString());
            File.WriteAllText(TigaVersiDir + "/BACA_DULU.txt", TigaVersiReadme);
            Debug.Log("[Organo] 3 versi APK:\n" + log);
        }

        /// <summary>Build ulang versi B saja (versi yang dipakai sebagai hasil akhir) ke folder 3 versi.</summary>
        [MenuItem("Organo/Build/APK Versi B (kamera biasa + kelas online)", priority = 93)]
        public static void BuildVersiB()
        {
            Directory.CreateDirectory(TigaVersiDir);
            var log = "B: " + BuildKe("B_Organo_KameraBiasa_Online-Offline.apk", new[] { "ORGANO_ONLINE" });
            File.WriteAllText("Builds/Android/_kerja_3versi/build_log_B.txt", log);
            Debug.Log("[Organo] APK versi B: " + log);
        }

        const string TigaVersiReadme =
@"ORGANO - 3 VERSI APK
====================

Ketiganya aplikasi yang sama (paket com.organo.belajarorgan), jadi di satu HP hanya bisa terpasang
satu versi. Memasang versi lain di atas versi yang sudah ada biasanya langsung bisa dan progres tetap.
Kalau HP menolak, hapus dulu aplikasi Organo yang lama lalu pasang lagi (progres di HP itu ikut terhapus).

A_Organo_Biasa_FullOffline.apk
  Cadangan aplikasi biasa (versi 15 Oktober 2026, kuis terkunci), sepenuhnya offline.
  Tidak ada fitur kelas, tidak pernah memakai internet.
  Kamera: pengaturan lama (di HP tanpa ARCore gambar kamera bisa terbalik, kartu belum bisa di-scan).

B_Organo_KameraBiasa_Online-Offline.apk
  Versi dengan perbaikan kamera untuk HP yang tidak mendukung AR (seperti HP teman):
  gambar kamera tidak terbalik, tidak terus disuruh memasang ARCore, kartu tetap bisa di-scan
  dengan kamera biasa. Ditambah fitur Kelas (online/offline).

C_Organo_Biasa_Online-Offline.apk
  Aplikasi biasa seperti A (pengaturan kamera AR tidak diubah), ditambah fitur Kelas (online/offline).

FITUR KELAS (B dan C)
  - Tanpa bergabung ke kelas, aplikasi tetap bekerja offline seperti biasa.
  - Guru membuat kelas di https://organo-kartu.vercel.app/guru lalu mendapat KODE KELAS (6 huruf/angka)
    dan KUNCI GURU. Kunci guru jangan dibagikan ke murid.
  - Murid membuka Profil > Kelas, memasukkan kode kelas dan namanya, lalu Gabung.
  - Nilai kuis (termasuk soal yang salah), progres belajar, kartu, dan bintang dikirim otomatis
    saat ada internet. Kalau sedang offline, kiriman menunggu sampai ada internet.
  - Guru membuka /guru dengan kode kelas + kunci guru untuk melihat evaluasi murid dan mengunduh rekap (CSV).
";

        // APK dibuild di folder kerja (Unity menaruh folder simbol debug di sampingnya), lalu hanya APK-nya disalin
        static string BuildKe(string nama, string[] defines)
        {
            const string kerja = "Builds/Android/_kerja_3versi";
            Directory.CreateDirectory(kerja);
            var hasil = Build(false, kerja + "/" + nama, defines);
            if (File.Exists(kerja + "/" + nama) && hasil.StartsWith("Build Android (rilis): Succeeded"))
                File.Copy(kerja + "/" + nama, TigaVersiDir + "/" + nama, true);
            return hasil;
        }

        static void Build(bool development) => Build(development, null, null);

        /// <summary>
        /// Build APK. <paramref name="defines"/>: simbol tambahan hanya untuk build ini, mis. ORGANO_ONLINE (dengan kelas
        /// online) atau ORGANO_KAMERA_LAMA (kamera biasa seperti versi sebelum perbaikan HP tanpa AR). Mengembalikan ringkasan.
        /// </summary>
        static string Build(bool development, string outPath, string[] defines)
        {
            defines = defines ?? new string[0];
            bool online = defines.Contains("ORGANO_ONLINE");
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
                // kelas online mengirim nilai ke server guru: izin internet selalu dicantumkan
                PlayerSettings.Android.forceInternetPermission = online;
                EditorUserBuildSettings.buildAppBundle = false;
                sb.AppendLine(OrganoAppIcon.Apply());
                sb.AppendLine(OrganoAppIcon.MakeArCoreDepthOptional());
                // ARCore memperkirakan sendiri ukuran kartu (kertas 6,3 cm maupun kartu besar di layar laptop)
                OrganoSetup.SetCardLibrarySpecifySize(false);
                sb.AppendLine("Kartu AR: ukuran fisik diperkirakan ARCore (tidak dipatok 6,3 cm)");
                sb.AppendLine("Ciri kartu untuk HP tanpa AR: " + CardSignatureBuilder.Build() + " gambar kartu");
                var path = outPath ?? (development ? ApkPath.Replace(".apk", "-dev.apk") : ApkPath);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Organo/Scenes/Organo.unity" },
                    locationPathName = path,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = development ? BuildOptions.Development : BuildOptions.None,
                    extraScriptingDefines = defines,
                });
                var s = report.summary;
                sb.Insert(0, "Build Android " + (development ? "(development)" : "(rilis)") + ": " + s.result + "\n");
                sb.AppendLine("File: " + Path.GetFullPath(path));
                if (File.Exists(path)) sb.AppendLine("Ukuran APK: " + (new FileInfo(path).Length / 1048576f).ToString("0.0") + " MB");
                sb.AppendLine("Lama build: " + s.totalTime);
                sb.AppendLine("Paket: " + PackageId + " · minSdk 25 · ARM64 · IL2CPP · ARCore opsional");
                sb.AppendLine("Kelas online: " + (online ? "ya (" + KelasOnline.Server + ")" : "tidak (full offline)")
                    + " · kamera biasa: " + (defines.Contains("ORGANO_KAMERA_LAMA") ? "seperti versi lama" : "perbaikan HP tanpa AR"));
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
            return sb.ToString();
        }
    }
}
