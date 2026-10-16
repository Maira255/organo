using System.IO;
using System.Linq;
using Organo.AR;
using UnityEditor;
using UnityEngine;

namespace Organo.EditorTools
{
    /// <summary>
    /// Ciri kartu untuk pengenal kartu tanpa ARCore (<see cref="CardMatcher"/>): dibuat dari gambar kartu kedua gaya
    /// (AR/Cards & AR/CardsKartun) yang dibuat "seperti terlihat kamera" (diperkecil ke tinggi 240 piksel & dikaburkan),
    /// masing-masing tegak dan miring ±5°, lalu disimpan di Resources/OrganoCardSignatures.bytes (±1 MB).
    /// Uji buatan (MatcherSynthEval.EvaluateSigs): dibanding ciri dari gambar kartu yang tajam, kartu yang memenuhi bingkai
    /// lebih sering dikenali (±50% → ±70% per gambar) tanpa gambar bukan-kartu ikut dikenali.
    /// Dibuat ulang otomatis setiap build APK dan setiap gambar kartu dipasang ulang.
    /// </summary>
    public static class CardSignatureBuilder
    {
        public const string OutPath = "Assets/Organo/Resources/" + CardMatcher.ResourceName + ".bytes";

        [MenuItem("Organo/Kartu AR/Buat Ciri Kartu (HP tanpa AR)", priority = 25)]
        public static int Build()
        {
            var m = new CardMatcher();
            var desc = new float[CardMatcher.Size];
            foreach (var card in MatcherSynth.LoadCards())
                foreach (float deg in new[] { 0f, -5f, 5f })
                {
                    var px = MatcherSynthEval.CameraLike(card, 240, deg, out int w, out int h);
                    CardMatcher.Describe(px, w, h, new Rect(0, 0, w, h), desc, 2);
                    // nama ciri: "lambung" (realistis) / "lambung@k" (kartun), "~5" = dimiringkan 5°
                    string name = card.id + (card.cartoon ? "@k" : "");
                    m.Add(deg == 0f ? name : name + "~" + deg.ToString("0"), (float[])desc.Clone());
                }
            Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
            File.WriteAllBytes(OutPath, m.ToBytes());
            AssetDatabase.ImportAsset(OutPath, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[Organo] Ciri kartu (HP tanpa AR): " + m.Count + " gambar kartu → " + OutPath);
            return m.Count;
        }
    }
}
