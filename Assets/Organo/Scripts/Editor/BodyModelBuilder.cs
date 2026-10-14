using System.IO;
using Organo.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Organo.EditorTools
{
    /// <summary>
    /// Tubuh anak 3D semi-transparan (laki-laki & perempuan) untuk tampilan close-up "Lihat di tubuh".
    /// Sengaja sederhana (±10–15 ribu segitiga) karena hanya jadi "wadah" organ.
    /// </summary>
    public static class BodyModelBuilder
    {
        const string Dir = "Assets/Organo/Models/Tubuh";

        [MenuItem("Organo/Model 3D/Buat Ulang Tubuh (Lihat di tubuh)", priority = 41)]
        public static void BuildBodies()
        {
            StomachModelBuilder.EnsureFolder(Dir);
            var skin = Transparent(Dir + "/Tubuh_Kulit.mat", new Color(0.95f, 0.77f, 0.63f, 0.2f), 0.4f);
            var hair = Transparent(Dir + "/Tubuh_Rambut.mat", new Color(0.35f, 0.27f, 0.21f, 0.45f), 0.3f);
            var bow = Transparent(Dir + "/Tubuh_Pita.mat", new Color(0.94f, 0.28f, 0.44f, 0.7f), 0.5f);
            var ghost = Transparent(Dir + "/Organ_Pudar.mat", new Color(0.86f, 0.84f, 0.92f, 0.28f), 0.3f);

            var boy = Build("TubuhLaki", false, new[] { skin, hair, bow });
            var girl = Build("TubuhPerempuan", true, new[] { skin, hair, bow });

            var db = AssetDatabase.LoadAssetAtPath<OrganDatabase>("Assets/Organo/Data/OrganDatabase.asset");
            if (db != null)
            {
                db.bodyBoyPrefab = boy;
                db.bodyGirlPrefab = girl;
                db.ghostMaterial = ghost;
                EditorUtility.SetDirty(db);
            }
            AssetDatabase.SaveAssets();
        }

        static GameObject Build(string name, bool girl, Material[] mats)
        {
            var sdf = new CompositeSdf(0.05f);
            // 0 kulit, 1 rambut, 2 pita
            sdf.Sphere(new Vector3(0f, 1.12f, 0f), 0.16f, 0, scale: new Vector3(1f, 1.08f, 0.98f));
            sdf.Tube(new[] { new Vector4(0f, 0.93f, 0f, 0.06f), new Vector4(0f, 1.0f, 0f, 0.06f) }, 0);
            sdf.Sphere(new Vector3(0f, 0.74f, 0f), 0.2f, 0, scale: new Vector3(1f, 1.22f, 0.6f));
            sdf.Sphere(new Vector3(0f, 0.5f, 0f), 0.18f, 0, scale: new Vector3(1.02f, 0.9f, 0.62f));
            foreach (var s in new[] { -1f, 1f })
            {
                sdf.Tube(new[] { new Vector4(s * 0.17f, 0.88f, 0f, 0.055f), new Vector4(s * 0.29f, 0.64f, 0f, 0.048f), new Vector4(s * 0.33f, 0.4f, 0f, 0.042f) }, 0);
                sdf.Tube(new[] { new Vector4(s * 0.085f, 0.38f, 0f, 0.07f), new Vector4(s * 0.09f, 0.18f, 0f, 0.06f), new Vector4(s * 0.09f, 0.0f, 0f, 0.055f) }, 0);
            }
            // rambut
            sdf.Sphere(new Vector3(0f, 1.17f, 0.02f), 0.17f, 1, scale: new Vector3(1.04f, 0.78f, 1.04f));
            if (girl)
            {
                foreach (var s in new[] { -1f, 1f })
                    sdf.Tube(new[] { new Vector4(s * 0.14f, 1.13f, 0.04f, 0.06f), new Vector4(s * 0.16f, 1.0f, 0.05f, 0.055f), new Vector4(s * 0.15f, 0.9f, 0.06f, 0.045f) }, 1);
                sdf.Sphere(new Vector3(0.11f, 1.28f, 0.0f), 0.05f, 2, scale: new Vector3(1.4f, 0.8f, 0.8f));
            }
            sdf.SetMaterialCount(3);

            var mesh = SurfaceNets.Build(sdf, new Bounds(new Vector3(0f, 0.66f, 0f), new Vector3(0.82f, 1.42f, 0.45f)), 0.018f);
            mesh.name = name;
            var meshPath = Dir + "/" + name + "_Mesh.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null) AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);

            var root = new GameObject(name);
            var body = new GameObject("Mesh", typeof(MeshFilter), typeof(MeshRenderer));
            body.transform.SetParent(root.transform, false);
            body.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = body.GetComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            var path = Dir + "/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("[Organo] Tubuh 3D dibuat: " + path + " (" + StomachModelBuilder.TriCount(mesh) + " segitiga).");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>Material URP Lit transparan (Surface Type: Transparent, blend Alpha).</summary>
        public static Material Transparent(string path, Color color, float smooth)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetShaderPassEnabled("DepthOnly", false);
            mat.SetShaderPassEnabled("ShadowCaster", false);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
