#if UNITY_ANDROID
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace Organo.EditorTools
{
    /// <summary>
    /// Android 11+ hanya mengizinkan aplikasi memakai mesin text-to-speech kalau manifest menyebut
    /// &lt;queries&gt; TTS_SERVICE. Ditambahkan otomatis ke manifest unityLibrary saat build.
    /// </summary>
    public class AndroidTtsQueries : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 10;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifest = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifest)) return;
            var xml = File.ReadAllText(manifest);
            if (xml.Contains("android.intent.action.TTS_SERVICE")) return;
            const string queries = "  <queries>\n    <intent>\n      <action android:name=\"android.intent.action.TTS_SERVICE\" />\n    </intent>\n  </queries>\n";
            int end = xml.LastIndexOf("</manifest>", System.StringComparison.Ordinal);
            if (end < 0) return;
            File.WriteAllText(manifest, xml.Substring(0, end) + queries + xml.Substring(end));
            Debug.Log("[Organo] <queries> TTS_SERVICE ditambahkan ke AndroidManifest.");
        }
    }
}
#endif
