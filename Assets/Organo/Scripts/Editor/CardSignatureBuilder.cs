using System.IO;
using System.Linq;
using Organo.AR;
using UnityEditor;
using UnityEngine;

namespace Organo.EditorTools
{
    /// <summary>
    /// Ciri kartu untuk pengenal kartu tanpa ARCore (<see cref="CardMatcher"/>): dibuat dari gambar kartu kedua gaya
    /// (AR/Cards & AR/CardsKartun) dan disimpan di Resources/OrganoCardSignatures.bytes (±330 KB).
    /// Dibuat ulang otomatis setiap build APK dan setiap gambar kartu dipasang ulang.
    /// </summary>
    public static class CardSignatureBuilder
    {
        public const string OutPath = "Assets/Organo/Resources/" + CardMatcher.ResourceName + ".bytes";

        [MenuItem("Organo/Kartu AR/Buat Ciri Kartu (HP tanpa AR)", priority = 25)]
        public static int Build()
        {
            var db = OrganoSetup.EnsureDatabase();
            var m = new CardMatcher();
            var desc = new float[CardMatcher.Size];
            foreach (var o in db.organs.Where(x => x != null).OrderBy(x => x.cardNumber))
                foreach (var dir in new[] { OrganoCards.CardDir, OrganoCards.CartoonDir })
                {
                    var path = dir + "/" + o.id + ".png";
                    if (!File.Exists(path)) continue;
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(File.ReadAllBytes(path));
                    var raw = tex.GetPixels32();
                    int w = tex.width, h = tex.height;
                    // GetPixels32: baris bawah dulu → baris atas dulu (sama seperti gambar kamera yang sudah ditegakkan)
                    var top = new Color32[raw.Length];
                    for (int y = 0; y < h; y++) System.Array.Copy(raw, (h - 1 - y) * w, top, y * w, w);
                    Object.DestroyImmediate(tex);
                    CardMatcher.Describe(top, w, h, new Rect(0, 0, w, h), desc, 6);
                    m.Add(o.id, (float[])desc.Clone());
                }
            Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
            File.WriteAllBytes(OutPath, m.ToBytes());
            AssetDatabase.ImportAsset(OutPath, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[Organo] Ciri kartu (HP tanpa AR): " + m.Count + " gambar kartu → " + OutPath);
            return m.Count;
        }
    }
}
