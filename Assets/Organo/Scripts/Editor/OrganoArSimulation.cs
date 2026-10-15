using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Simulation;

namespace Organo.EditorTools
{
    /// <summary>
    /// Uji scan kartu tanpa HP: lingkungan XR Simulation berisi meja kelas dengan semua kartu cetak kedua gaya (ukuran asli 6,3 x 8,8 cm).
    /// Saat Play di Editor (XR Simulation aktif untuk Standalone), AR Foundation benar-benar menjalankan pelacakan gambar
    /// dengan library kartu yang sama seperti di HP, jadi alur scan → kartu dikenali → model 3D muncul di atas kartu ikut teruji.
    /// </summary>
    public static class OrganoArSimulation
    {
        const string Dir = "Assets/Organo/AR/Simulasi";
        public const string EnvPath = Dir + "/MejaKelasOrgano.prefab";
        const string PrefsPath = "Assets/XR/UserSimulationSettings/Resources/XRSimulationPreferences.asset";
        public const float TableY = 0.75f;
        public static readonly Vector3 CameraPos = new Vector3(0f, 1.1f, -0.45f);

        [MenuItem("Organo/Kartu AR/Simulasi XR: Meja dengan Semua Kartu", priority = 26)]
        public static void BuildAndUse()
        {
            var prefab = BuildEnvironment();
            UseEnvironment(prefab);
            Debug.Log("[Organo] Lingkungan simulasi XR dipakai: " + EnvPath + ". Matikan 'Use Webcam In Editor' di ARScanController lalu Play > Scan.");
        }

        public static GameObject BuildEnvironment()
        {
            var db = OrganoSetup.EnsureDatabase();
            StomachModelBuilder.EnsureFolder(Dir);
            var root = new GameObject("Meja Kelas Organo");
            var envType = typeof(SimulatedTrackedImage).Assembly.GetType("UnityEngine.XR.Simulation.SimulationEnvironment");
            var env = root.AddComponent(envType);
            var so = new SerializedObject(env);
            var look = Quaternion.LookRotation(new Vector3(0f, TableY, 0f) - CameraPos, Vector3.up);
            foreach (var name in new[] { "m_CameraStartingPose", "m_DefaultViewPose" })
            {
                var pose = so.FindProperty(name);
                if (pose == null) continue;
                pose.FindPropertyRelative("position").vector3Value = CameraPos;
                pose.FindPropertyRelative("rotation").quaternionValue = look;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            Box(root, "Lantai", new Vector3(0f, -0.01f, 0f), new Vector3(4f, 0.02f, 4f), Mat("Lantai", "#E9E4DC"));
            Box(root, "Meja", new Vector3(0f, TableY - 0.02f, 0.02f), new Vector3(1.1f, 0.04f, 0.72f), Mat("Meja", "#C9A27A"));
            foreach (var x in new[] { -0.5f, 0.5f })
                foreach (var z in new[] { -0.3f, 0.34f })
                    Box(root, "Kaki meja", new Vector3(x, (TableY - 0.04f) * 0.5f, z), new Vector3(0.05f, TableY - 0.04f, 0.05f), Mat("Meja", "#C9A27A"));
            var light = new GameObject("Cahaya", typeof(Light));
            light.transform.SetParent(root.transform, false);
            light.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            var l = light.GetComponent<Light>();
            l.type = LightType.Directional; l.intensity = 1.1f;

            // kartu cetak di atas meja (5 per baris), seperti dibagikan guru: 15 kartu Realistis lalu 15 kartu Kartun
            var organs = db.organs.Where(o => o != null && o.cardImage != null).OrderBy(o => o.cardNumber).ToList();
            var cards = new System.Collections.Generic.List<(string name, Texture2D tex, float width)>();
            foreach (var (dir, label) in new[] { (OrganoCards.CardDir, "Realistis"), (OrganoCards.CartoonDir, "Kartun") })
                foreach (var o in organs)
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/" + o.id + ".png");
                    if (tex != null) cards.Add(("Kartu " + o.displayName + " (" + label + ")", tex, o.cardWidthMeters));
                }
            var setGuid = typeof(SimulatedTrackedImage).GetMethod("SetSerializedGuid", BindingFlags.NonPublic | BindingFlags.Instance);
            int rows = Mathf.CeilToInt(cards.Count / 5f);
            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                int col = i % 5, row = i / 5;
                var go = new GameObject(c.name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3((col - 2) * 0.09f, TableY + 0.0005f, ((rows - 1) * 0.5f - row) * 0.105f);
                var img = go.AddComponent<SimulatedTrackedImage>();
                var s2 = new SerializedObject(img);
                s2.FindProperty("m_Image").objectReferenceValue = c.tex;
                float aspect = (float)c.tex.height / Mathf.Max(1, c.tex.width);
                s2.FindProperty("m_ImagePhysicalSizeMeters").vector2Value = new Vector2(c.width, c.width * aspect);
                s2.ApplyModifiedPropertiesWithoutUndo();
                setGuid?.Invoke(img, null);
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, EnvPath);
            Object.DestroyImmediate(root);
            Debug.Log("[Organo] Lingkungan simulasi dibuat dengan " + cards.Count + " kartu: " + EnvPath);
            return prefab;
        }

        public static void UseEnvironment(GameObject prefab)
        {
            var prefs = AssetDatabase.LoadAssetAtPath<ScriptableObject>(PrefsPath);
            if (prefs == null) { Debug.LogWarning("[Organo] XRSimulationPreferences tidak ditemukan."); return; }
            var so = new SerializedObject(prefs);
            so.FindProperty("m_EnvironmentPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefs);
            AssetDatabase.SaveAssets();
        }

        static Material Mat(string name, string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return StomachModelBuilder.MakeMaterial(Dir + "/" + name + ".mat", c, 0.25f);
        }

        static void Box(GameObject root, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}
