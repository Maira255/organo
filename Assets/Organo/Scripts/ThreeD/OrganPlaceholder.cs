using UnityEngine;

namespace Organo.ThreeD
{
    /// <summary>
    /// Model sementara dari primitive Unity untuk tiap organ, dipakai selama slot model di OrganInfo masih kosong.
    /// Bentuknya kira-kira mengikuti ilustrasi di desain supaya rotasi & titik info sudah bisa dicoba.
    /// </summary>
    public static class OrganPlaceholder
    {
        static Shader s_Shader;

        public static GameObject Build(string organId, Color accent)
        {
            var root = new GameObject("Placeholder " + organId);
            Color light = Color.Lerp(accent, Color.white, 0.35f);
            Color dark = Color.Lerp(accent, Color.black, 0.2f);
            switch (organId)
            {
                case "jantung":
                    Part(root, PrimitiveType.Sphere, new Vector3(-0.17f, 0.1f, 0), new Vector3(0.5f, 0.5f, 0.42f), Vector3.zero, accent);
                    Part(root, PrimitiveType.Sphere, new Vector3(0.17f, 0.1f, 0), new Vector3(0.5f, 0.5f, 0.42f), Vector3.zero, accent);
                    Part(root, PrimitiveType.Sphere, new Vector3(0, -0.15f, 0), new Vector3(0.55f, 0.6f, 0.4f), new Vector3(0, 0, 45), accent);
                    Part(root, PrimitiveType.Cylinder, new Vector3(-0.08f, 0.42f, 0), new Vector3(0.09f, 0.15f, 0.09f), Vector3.zero, dark);
                    Part(root, PrimitiveType.Cylinder, new Vector3(0.1f, 0.4f, 0.02f), new Vector3(0.08f, 0.12f, 0.08f), new Vector3(0, 0, -20), dark);
                    break;
                case "paru":
                    Part(root, PrimitiveType.Capsule, new Vector3(-0.2f, -0.05f, 0), new Vector3(0.3f, 0.36f, 0.25f), new Vector3(0, 0, -8), accent);
                    Part(root, PrimitiveType.Capsule, new Vector3(0.21f, -0.05f, 0), new Vector3(0.32f, 0.38f, 0.25f), new Vector3(0, 0, 8), light);
                    Part(root, PrimitiveType.Cylinder, new Vector3(0, 0.35f, 0), new Vector3(0.07f, 0.17f, 0.07f), Vector3.zero, dark);
                    break;
                case "lambung":
                    Part(root, PrimitiveType.Sphere, new Vector3(0.05f, -0.08f, 0), new Vector3(0.8f, 0.6f, 0.45f), new Vector3(0, 0, -25), accent);
                    Part(root, PrimitiveType.Sphere, new Vector3(-0.22f, 0.14f, 0), new Vector3(0.34f, 0.38f, 0.32f), Vector3.zero, light);
                    Part(root, PrimitiveType.Cylinder, new Vector3(-0.24f, 0.4f, 0), new Vector3(0.12f, 0.12f, 0.12f), Vector3.zero, dark);
                    break;
                case "usushalus":
                    for (int i = 0; i < 18; i++)
                    {
                        float a = i / 18f * Mathf.PI * 4f;
                        float r = 0.18f + 0.08f * (i % 2);
                        Part(root, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * r, 0.3f - i * 0.035f, Mathf.Sin(a) * r * 0.6f),
                            Vector3.one * 0.17f, Vector3.zero, i % 2 == 0 ? accent : light);
                    }
                    break;
                case "ususbesar":
                    Part(root, PrimitiveType.Capsule, new Vector3(-0.32f, -0.05f, 0), new Vector3(0.16f, 0.36f, 0.16f), Vector3.zero, accent);
                    Part(root, PrimitiveType.Capsule, new Vector3(0, 0.32f, 0), new Vector3(0.16f, 0.36f, 0.16f), new Vector3(0, 0, 90), accent);
                    Part(root, PrimitiveType.Capsule, new Vector3(0.32f, -0.02f, 0), new Vector3(0.16f, 0.3f, 0.16f), Vector3.zero, accent);
                    Part(root, PrimitiveType.Capsule, new Vector3(0.12f, -0.3f, 0), new Vector3(0.14f, 0.22f, 0.14f), new Vector3(0, 0, 90), light);
                    break;
                case "kerongkongan":
                    Part(root, PrimitiveType.Cylinder, new Vector3(0, 0.05f, 0), new Vector3(0.16f, 0.42f, 0.16f), new Vector3(0, 0, 6), accent);
                    Part(root, PrimitiveType.Sphere, new Vector3(0, 0.46f, 0), new Vector3(0.22f, 0.12f, 0.22f), Vector3.zero, light);
                    Part(root, PrimitiveType.Sphere, new Vector3(0.04f, -0.38f, 0), new Vector3(0.2f, 0.12f, 0.2f), Vector3.zero, dark);
                    break;
                case "mulut":
                    Part(root, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.9f, 0.5f, 0.55f), Vector3.zero, accent);
                    Part(root, PrimitiveType.Cube, new Vector3(0, 0.03f, 0.24f), new Vector3(0.48f, 0.14f, 0.1f), Vector3.zero, Color.white);
                    Part(root, PrimitiveType.Sphere, new Vector3(0, -0.1f, 0.18f), new Vector3(0.36f, 0.12f, 0.2f), Vector3.zero, light);
                    break;
                default:
                    Part(root, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.8f, Vector3.zero, accent);
                    break;
            }
            return root;
        }

        static void Part(GameObject root, PrimitiveType type, Vector3 pos, Vector3 scale, Vector3 euler, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localEulerAngles = euler;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
        }

        public static Material Material(Color color)
        {
            if (s_Shader == null)
                s_Shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");
            var m = new Material(s_Shader) { color = color };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.55f);
            return m;
        }
    }
}
