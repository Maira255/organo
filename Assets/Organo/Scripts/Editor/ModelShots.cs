using System;
using System.IO;
using System.Linq;
using Organo.Data;
using UnityEditor;
using UnityEngine;

namespace Organo.EditorTools
{
    /// <summary>
    /// Foto model 3D organ dari beberapa arah (depan, kanan, belakang, kiri, atas) ke Library/OrganoPreview/model_&lt;id&gt;.png,
    /// untuk memeriksa bentuk model tanpa membuka aplikasi.
    /// Unity -batchmode -projectPath . -executeMethod Organo.EditorTools.ModelShots.RenderFromArgs -organoModels usushalus,ususbesar
    /// </summary>
    public static class ModelShots
    {
        const int Tile = 420;

        public static void RenderFromArgs()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-organoModels");
            var ids = i >= 0 && i + 1 < args.Length ? args[i + 1].Split(',') : new[] { "usushalus" };
            foreach (var id in ids) Render(id.Trim());
        }

        [MenuItem("Organo/Model 3D/Foto Model Usus Halus (5 arah)", priority = 90)]
        static void RenderSmallIntestine() => Render("usushalus");

        public static void Render(string id)
        {
            var db = OrganoSetup.EnsureDatabase();
            var organ = db.Get(id);
            if (organ == null || organ.modelPrefab == null) { Debug.LogWarning("[Organo] model tidak ada: " + id); return; }
            var root = new GameObject("ModelShots");
            try
            {
                var model = (GameObject)UnityEngine.Object.Instantiate(organ.modelPrefab, root.transform);
                var rs = model.GetComponentsInChildren<Renderer>();
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                var lightGo = new GameObject("Light");
                lightGo.transform.SetParent(root.transform);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                lightGo.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
                var camGo = new GameObject("Cam");
                camGo.transform.SetParent(root.transform);
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.97f, 0.95f, 0.96f);
                cam.fieldOfView = 30f;
                float dist = b.extents.magnitude / Mathf.Sin(15f * Mathf.Deg2Rad) * 1.05f;
                cam.nearClipPlane = dist * 0.05f; cam.farClipPlane = dist * 4f;
                var rt = new RenderTexture(Tile, Tile, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cam.targetTexture = rt;
                var views = new (string name, Vector3 dir)[]
                {
                    ("depan", Vector3.back), ("kanan", Vector3.right), ("belakang", Vector3.forward), ("kiri", Vector3.left), ("atas", Vector3.up + Vector3.back * 0.15f),
                };
                var sheet = new Texture2D(Tile * views.Length, Tile, TextureFormat.RGB24, false);
                var tile = new Texture2D(Tile, Tile, TextureFormat.RGB24, false);
                for (int v = 0; v < views.Length; v++)
                {
                    var dir = views[v].dir.normalized;
                    cam.transform.position = b.center + dir * dist;
                    cam.transform.LookAt(b.center, Mathf.Abs(dir.y) > 0.9f ? Vector3.forward : Vector3.up);
                    cam.Render();
                    RenderTexture.active = rt;
                    tile.ReadPixels(new Rect(0, 0, Tile, Tile), 0, 0);
                    tile.Apply();
                    sheet.SetPixels(v * Tile, 0, Tile, Tile, tile.GetPixels());
                }
                RenderTexture.active = null;
                sheet.Apply();
                Directory.CreateDirectory("Library/OrganoPreview");
                File.WriteAllBytes("Library/OrganoPreview/model_" + id + ".png", sheet.EncodeToPNG());
                int tris = model.GetComponentsInChildren<MeshFilter>().Where(m => m.sharedMesh != null).Sum(m => (int)m.sharedMesh.GetIndexCount(0) / 3);
                Debug.Log("[Organo] foto model " + id + " (" + string.Join(", ", views.Select(x => x.name)) + "), segitiga ±" + tris);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(tile);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
