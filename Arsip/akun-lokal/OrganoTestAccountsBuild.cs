using System.IO;
using Organo.Data;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Organo.EditorTools
{
    /// <summary>
    /// Penjaga akun uji saat build:
    /// - Build rilis: akun uji TIDAK ikut (salinan di Resources dihapus, kodenya juga tidak dikompilasi).
    /// - Development Build: data akun uji disalin sementara ke Resources supaya tombol "Muat akun uji" bisa dipakai
    ///   di HP penguji, lalu dihapus lagi setelah build selesai.
    /// Akun yang dibuat di Editor tersimpan di PlayerPrefs Editor (registry Windows), bukan di aplikasi.
    /// </summary>
    public class OrganoTestAccountsBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        const string ResDir = "Assets/Organo/Resources";
        const string ResPath = ResDir + "/" + AccountStore.TestAccountsResource + ".json";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            RemoveCopy();
            bool dev = (report.summary.options & BuildOptions.Development) != 0;
            if (dev && File.Exists(AccountStore.TestAccountsEditorPath))
            {
                Directory.CreateDirectory(ResDir);
                File.Copy(AccountStore.TestAccountsEditorPath, ResPath, true);
                AssetDatabase.ImportAsset(ResPath, ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("[Organo] Development Build: akun uji disertakan (tombol \"Muat akun uji\" di halaman Masuk).");
            }
            else Debug.Log("[Organo] Build rilis: akun uji & alat pengembang TIDAK disertakan.");
        }

        public void OnPostprocessBuild(BuildReport report) => RemoveCopy();

        /// <summary>Hapus salinan akun uji di Resources (juga sisa dari build yang gagal).</summary>
        static void RemoveCopy()
        {
            if (File.Exists(ResPath)) AssetDatabase.DeleteAsset(ResPath);
            if (AssetDatabase.IsValidFolder(ResDir) && Directory.GetFileSystemEntries(ResDir).Length == 0) AssetDatabase.DeleteAsset(ResDir);
        }

        /// <summary>
        /// Batch (uji): build Windows rilis & development ke folder temp lalu cari jejak akun uji di datanya.
        /// Hasil: Library/OrganoPreview/cek_akun_uji.txt
        /// </summary>
        public static void VerifyBuilds()
        {
            var scenes = new[] { "Assets/Organo/Scenes/Organo.unity" };
            var sb = new System.Text.StringBuilder();
            foreach (var dev in new[] { false, true })
            {
                var dir = Path.Combine(Path.GetTempPath(), dev ? "OrganoCekDev" : "OrganoCekRilis");
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = Path.Combine(dir, "Organo.exe"),
                    target = BuildTarget.StandaloneWindows64,
                    options = dev ? BuildOptions.Development : BuildOptions.None,
                });
                int hits = 0;
                foreach (var f in Directory.GetFiles(Path.Combine(dir, "Organo_Data"), "*", SearchOption.TopDirectoryOnly))
                {
                    var bytes = File.ReadAllBytes(f);
                    if (Contains(bytes, System.Text.Encoding.UTF8.GetBytes("uji.sedang"))) hits++;
                }
                bool codeHasSeed = false;
                var managed = Path.Combine(dir, "Organo_Data", "Managed", "Assembly-CSharp.dll");
                if (File.Exists(managed)) codeHasSeed = Contains(File.ReadAllBytes(managed), System.Text.Encoding.UTF8.GetBytes("SeedTestAccounts"));
                sb.AppendLine((dev ? "Development Build" : "Build rilis") + ": " + r.summary.result
                    + " | data akun uji ditemukan di " + hits + " file | kode akun uji ada: " + (codeHasSeed ? "ya" : "tidak"));
            }
            Directory.CreateDirectory("Library/OrganoPreview");
            File.WriteAllText("Library/OrganoPreview/cek_akun_uji.txt", sb.ToString());
            Debug.Log("[Organo] " + sb);
        }

        static bool Contains(byte[] hay, byte[] needle)
        {
            for (int i = 0; i <= hay.Length - needle.Length; i++)
            {
                int j = 0;
                while (j < needle.Length && hay[i + j] == needle[j]) j++;
                if (j == needle.Length) return true;
            }
            return false;
        }
    }
}
