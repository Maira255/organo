#if UNITY_ANDROID
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace Organo.EditorTools
{
    /// <summary>
    /// Di Windows, Gradle (Java bawaan Unity) memakai daftar sertifikat Java sendiri. Kalau antivirus/jaringan sekolah
    /// memeriksa HTTPS (mis. AVG Web Shield), unduhan library Android gagal dengan "PKIX path building failed".
    /// Callback ini membuat Gradle memakai daftar sertifikat Windows (yang sudah dipercaya sistem) — hanya saat build di Windows.
    /// </summary>
    public class AndroidGradleWindowsCerts : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 10;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor) return;
            var props = Path.Combine(Directory.GetParent(path).FullName, "gradle.properties");
            if (!File.Exists(props)) return;
            var text = File.ReadAllText(props);
            const string line = "systemProp.javax.net.ssl.trustStoreType=Windows-ROOT";
            if (text.Contains(line)) return;
            File.AppendAllText(props, (text.EndsWith("\n") ? "" : "\n") + line + "\n");
        }
    }
}
#endif
