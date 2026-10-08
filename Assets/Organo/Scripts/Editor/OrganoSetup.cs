using System.IO;
using System.Linq;
using Organo.AR;
using Organo.Data;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Organo.EditorTools
{
    /// <summary>
    /// Menghubungkan Organo ke project Unity: membuat scene AR, PanelSettings UI Toolkit, asset organ (slot model 3D),
    /// library kartu AR, dan build settings. Menu: Organo &gt; ...
    /// </summary>
    [InitializeOnLoad]
    public static class OrganoSetup
    {
        const string Root = "Assets/Organo";
        const string ScenePath = Root + "/Scenes/Organo.unity";
        const string PanelPath = Root + "/UI/OrganoPanelSettings.asset";
        const string ThemePath = Root + "/UI/OrganoTheme.tss";
        const string UssPath = Root + "/UI/Organo.uss";
        const string DataDir = Root + "/Data";
        const string DbPath = DataDir + "/OrganDatabase.asset";
        const string LibPath = Root + "/AR/OrganoCardLibrary.asset";
        const string AutoKey = "Organo.AutoSetupDone.v1";

        static OrganoSetup()
        {
            // Sekali saja: siapkan semuanya otomatis setelah skrip pertama kali dikompilasi.
            if (SessionState.GetBool(AutoKey, false) || EditorPrefs.GetBool(AutoKey + Application.dataPath, false)) return;
            SessionState.SetBool(AutoKey, true);
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(ScenePath)) return;
                try
                {
                    Run(false);
                    EditorPrefs.SetBool(AutoKey + Application.dataPath, true);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[Organo] Setup otomatis belum bisa jalan (" + e.Message + "). Jalankan menu Organo > Setup Scene & Asset.");
                }
            };
        }

        [MenuItem("Organo/Setup Scene && Asset", priority = 0)]
        public static void SetupMenu()
        {
            Run(true);
        }

        [MenuItem("Organo/Buka Scene Organo", priority = 1)]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath)) { Run(true); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Organo/Bangun Library Kartu AR", priority = 20)]
        public static void BuildCardLibraryMenu()
        {
            var db = AssetDatabase.LoadAssetAtPath<OrganDatabase>(DbPath);
            if (db == null) { Debug.LogWarning("[Organo] Jalankan Organo > Setup dulu."); return; }
            int n = BuildCardLibrary(db);
            EditorUtility.DisplayDialog("Organo", n == 0
                ? "Belum ada gambar kartu. Isi field 'Card Image' di asset organ (Assets/Organo/Data) lalu jalankan menu ini lagi."
                : n + " kartu dimasukkan ke " + LibPath + ".", "OK");
        }

        [MenuItem("Organo/Pilih Asset Organ (slot model 3D)", priority = 21)]
        public static void SelectOrgans()
        {
            var db = AssetDatabase.LoadAssetAtPath<OrganDatabase>(DbPath);
            if (db == null) { Run(true); db = AssetDatabase.LoadAssetAtPath<OrganDatabase>(DbPath); }
            Selection.objects = db.organs.Cast<Object>().ToArray();
            EditorGUIUtility.PingObject(db.organs.FirstOrDefault());
        }

        [MenuItem("Organo/Reset Progres Pemain", priority = 40)]
        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey("organo.progress.v1");
            PlayerPrefs.DeleteKey("organo.camera.asked");
            Debug.Log("[Organo] Progres pemain dihapus.");
        }

        // ------------------------------------------------------------------------------------

        static void Run(bool interactive)
        {
            EnsureFolder(Root + "/Scenes"); EnsureFolder(Root + "/UI"); EnsureFolder(DataDir); EnsureFolder(Root + "/AR");
            AssetDatabase.Refresh();

            var panel = EnsurePanelSettings();
            var db = EnsureDatabase();
            var lib = EnsureLibrary();
            BuildCardLibrary(db);
            ConfigurePlayer();

            if (interactive)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Populate(scene, panel, db, lib);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            else
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                Populate(scene, panel, db, lib);
                EditorSceneManager.SaveScene(scene, ScenePath);
                EditorSceneManager.CloseScene(scene, true);
            }
            AddToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[Organo] Siap! Scene: " + ScenePath + ". Buka lewat menu Organo > Buka Scene Organo lalu tekan Play. " +
                      "Slot model 3D close-up ada di asset organ " + DataDir + " (field 'Model Prefab').");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static PanelSettings EnsurePanelSettings()
        {
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (ps == null)
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(ps, PanelPath);
            }
            ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(390, 844);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0f; // ikuti lebar: 1 px desain = 1 unit UI
            ps.clearColor = false;
            EditorUtility.SetDirty(ps);
            return ps;
        }

        static OrganDatabase EnsureDatabase()
        {
            var db = AssetDatabase.LoadAssetAtPath<OrganDatabase>(DbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<OrganDatabase>();
                AssetDatabase.CreateAsset(db, DbPath);
            }
            db.organs.RemoveAll(o => o == null);
            foreach (var id in OrganDefaults.Order)
            {
                var path = DataDir + "/Organ_" + id + ".asset";
                var o = AssetDatabase.LoadAssetAtPath<OrganInfo>(path);
                if (o == null)
                {
                    o = ScriptableObject.CreateInstance<OrganInfo>();
                    OrganDefaults.Fill(o, id);
                    AssetDatabase.CreateAsset(o, path);
                }
                if (!db.organs.Contains(o)) db.organs.Add(o);
            }
            EditorUtility.SetDirty(db);
            return db;
        }

        static XRReferenceImageLibrary EnsureLibrary()
        {
            var lib = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibPath);
            if (lib != null) return lib;
            lib = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
            AssetDatabase.CreateAsset(lib, LibPath);
            return lib;
        }

        /// <returns>jumlah kartu yang dimasukkan.</returns>
        static int BuildCardLibrary(OrganDatabase db)
        {
            var lib = EnsureLibrary();
            while (lib.count > 0) lib.RemoveAt(lib.count - 1);
            int n = 0;
            foreach (var o in db.organs.Where(o => o != null && o.cardImage != null))
            {
                lib.Add();
                int i = lib.count - 1;
                lib.SetName(i, o.ReferenceImageName);
                lib.SetTexture(i, o.cardImage, false);
                lib.SetSpecifySize(i, true);
                float aspect = (float)o.cardImage.height / Mathf.Max(1, o.cardImage.width);
                lib.SetSize(i, new Vector2(o.cardWidthMeters, o.cardWidthMeters * aspect));
                n++;
            }
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            return n;
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            if (string.IsNullOrEmpty(PlayerSettings.iOS.cameraUsageDescription))
                PlayerSettings.iOS.cameraUsageDescription = "Organo memakai kamera untuk memindai kartu organ. Gambar kamera tidak disimpan.";
        }

        static void Populate(Scene scene, PanelSettings panel, OrganDatabase db, XRReferenceImageLibrary lib)
        {
            GameObject Make(string name, params System.Type[] comps)
            {
                var go = new GameObject(name, comps);
                SceneManager.MoveGameObjectToScene(go, scene);
                return go;
            }

            // Cahaya
            var lightGo = Make("Directional Light", typeof(Light));
            var light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            // AR Session
            var sessionGo = Make("AR Session", typeof(ARSession), typeof(ARInputManager));

            // XR Origin + kamera AR
            var originGo = Make("XR Origin (Organo AR)", typeof(XROrigin));
            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(originGo.transform, false);
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(ARCameraManager), typeof(ARCameraBackground));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(offset.transform, false);
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0xFA, 0xFA, 0xFA, 0xFF);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 50f;
            cam.cullingMask &= ~(1 << 30);
            var tpd = camGo.AddComponent<TrackedPoseDriver>();
            tpd.positionInput = new InputActionProperty(new InputAction("Position", binding: "<HandheldARInputDevice>/devicePosition", expectedControlType: "Vector3"));
            tpd.rotationInput = new InputActionProperty(new InputAction("Rotation", binding: "<HandheldARInputDevice>/deviceRotation", expectedControlType: "Quaternion"));
            var camMgr = camGo.GetComponent<ARCameraManager>();
            camMgr.requestedLightEstimation = LightEstimation.AmbientIntensity;
            var origin = originGo.GetComponent<XROrigin>();
            origin.Camera = cam;
            origin.CameraFloorOffsetObject = offset;

            var imgMgr = originGo.AddComponent<ARTrackedImageManager>();
            imgMgr.referenceLibrary = lib;
            imgMgr.requestedMaxNumberOfMovingImages = 1;
            imgMgr.enabled = false; // dinyalakan oleh ARScanController saat halaman Scan dibuka

            // EventSystem (input UI Toolkit lewat Input System)
            Make("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // Aplikasi
            var appGo = Make("Organo App", typeof(UIDocument), typeof(ARScanController), typeof(OrganoApp));
            var doc = appGo.GetComponent<UIDocument>();
            doc.panelSettings = panel;
            var scanner = appGo.GetComponent<ARScanController>();
            scanner.session = sessionGo.GetComponent<ARSession>();
            scanner.cameraManager = camMgr;
            scanner.cameraBackground = camGo.GetComponent<ARCameraBackground>();
            scanner.imageManager = imgMgr;
            scanner.arCamera = cam;
            var app = appGo.GetComponent<OrganoApp>();
            app.database = db;
            app.styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            app.ar = scanner;
        }

        static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == ScenePath);
            foreach (var s in scenes) s.enabled = false;
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
