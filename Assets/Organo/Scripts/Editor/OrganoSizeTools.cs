using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Organo.EditorTools
{
    /// <summary>Alat ukuran aplikasi: rampingkan mesh model 3D & buat laporan ukuran dari build sungguhan.</summary>
    public static class OrganoSizeTools
    {
        const string ModelsDir = "Assets/Organo/Models";

        /// <summary>
        /// Semua mesh model Organo: buang tangent (tidak dipakai karena tanpa normal map) dan aktifkan kompresi mesh
        /// (hanya memperkecil ukuran file aplikasi; di HP mesh tetap utuh).
        /// </summary>
        [MenuItem("Organo/Ukuran Aplikasi/Rampingkan Mesh Model 3D", priority = 70)]
        public static void SlimMeshes()
        {
            long before = 0, after = 0;
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Mesh", new[] { ModelsDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh == null) continue;
                before += EstimateBytes(mesh);
                if (mesh.tangents != null && mesh.tangents.Length > 0) mesh.tangents = null;
                MeshUtility.SetMeshCompression(mesh, ModelImporterMeshCompression.Medium);
                MeshUtility.Optimize(mesh);
                after += EstimateBytes(mesh);
                EditorUtility.SetDirty(mesh);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Organo] " + n + " mesh dirampingkan: data verteks " + (before / 1048576f).ToString("0.0") + " MB -> " + (after / 1048576f).ToString("0.0") + " MB (sebelum kompresi build).");
        }

        static long EstimateBytes(Mesh m)
        {
            int stride = 0;
            for (int s = 0; s < m.vertexBufferCount; s++) stride += m.GetVertexBufferStride(s);
            long idx = 0;
            for (int i = 0; i < m.subMeshCount; i++) idx += m.GetIndexCount(i);
            return (long)m.vertexCount * stride + idx * (m.indexFormat == UnityEngine.Rendering.IndexFormat.UInt32 ? 4 : 2);
        }

        /// <summary>
        /// Build Windows (modul yang terpasang) untuk mengukur isi aplikasi, lalu tulis ringkasan ke
        /// Library/OrganoPreview/ukuran_aplikasi.txt. Isi aset hampir sama untuk Android/iOS.
        /// </summary>
        [MenuItem("Organo/Ukuran Aplikasi/Buat Laporan Ukuran", priority = 71)]
        public static void Report()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { "Assets/Organo/Scenes/Organo.unity" };
            var outDir = Path.Combine(Path.GetTempPath(), "OrganoSizeBuild"); // di luar proyek (Unity menolak build di dalam Library)
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outDir, "Organo.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var sb = new StringBuilder();
            sb.AppendLine("Laporan ukuran Organo (build Windows untuk mengukur isi aset)");
            sb.AppendLine("Hasil build: " + report.summary.result + ", total " + (report.summary.totalSize / 1048576f).ToString("0.0") + " MB");
            var groups = report.packedAssets.SelectMany(p => p.contents)
                .GroupBy(c => Category(c.sourceAssetPath, c.type))
                .Select(g => (name: g.Key, bytes: g.Sum(c => (long)c.packedSize)))
                .OrderByDescending(g => g.bytes).ToList();
            long assets = groups.Sum(g => g.bytes);
            sb.AppendLine("Isi aset (sebelum kompresi paket): " + (assets / 1048576f).ToString("0.0") + " MB");
            foreach (var (name, bytes) in groups) sb.AppendLine("  - " + name + ": " + (bytes / 1048576f).ToString("0.00") + " MB");
            sb.AppendLine();
            sb.AppendLine("Aset terbesar:");
            foreach (var c in report.packedAssets.SelectMany(p => p.contents).OrderByDescending(c => c.packedSize).Take(25))
                sb.AppendLine("  " + (c.packedSize / 1048576f).ToString("0.00") + " MB  " + c.sourceAssetPath + " (" + c.type?.Name + ")");
            Directory.CreateDirectory("Library/OrganoPreview");
            File.WriteAllText("Library/OrganoPreview/ukuran_aplikasi.txt", sb.ToString());
            Debug.Log("[Organo] " + sb);
        }

        static string Category(string path, System.Type type)
        {
            string t = type != null ? type.Name : "";
            if (path.StartsWith("Assets/Organo/Models")) return t == "Mesh" ? "Model 3D Organo (mesh)" : "Model 3D Organo (lainnya)";
            if (path.StartsWith("Assets/Organo/Images")) return "Gambar bagian organ";
            if (path.StartsWith("Assets/Organo/AR")) return "Kartu AR";
            if (path.StartsWith("Assets/Organo/Fonts")) return "Font";
            if (t == "Shader" || t == "ComputeShader") return "Shader";
            if (t == "Texture2D" || t == "Cubemap" || t == "Texture2DArray") return "Tekstur lain";
            if (t == "Mesh") return "Mesh lain";
            if (t == "AudioClip") return "Audio";
            if (path.StartsWith("Packages/")) return "Paket Unity (AR, URP, dll)";
            return "Lainnya (" + t + ")";
        }
    }
}
