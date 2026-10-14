using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Organo.Data;
using UnityEditor;
using UnityEngine;

namespace Organo.EditorTools
{
    /// <summary>
    /// Suara pembaca yang lebih manusiawi: teks narasi diekspor ke Tools/suara/narasi.json, dibacakan oleh layanan suara AI
    /// (Tools/suara/buat_suara.py: ElevenLabs atau Microsoft Azure Neural), hasil MP3 di Assets/Organo/Audio/Narasi
    /// lalu dipasang ke asset organ. Organ tanpa rekaman tetap dibacakan suara HP (OrganoSpeech).
    /// </summary>
    public static class OrganoNarrationTools
    {
        public const string JsonPath = "Tools/suara/narasi.json";
        public const string AudioDir = "Assets/Organo/Audio/Narasi";

        [System.Serializable] class Item { public string file; public string text; }
        [System.Serializable] class ItemList { public List<Item> items = new List<Item>(); public int karakter; }

        [MenuItem("Organo/Suara/Ekspor Teks Narasi", priority = 60)]
        public static void ExportMenu() => Debug.Log("[Organo] " + Export());

        public static string Export()
        {
            var db = OrganoSetup.EnsureDatabase();
            var list = new ItemList();
            foreach (var o in db.organs.Where(x => x != null).OrderBy(x => x.cardNumber))
            {
                if (!string.IsNullOrEmpty(o.about)) list.items.Add(new Item { file = OrganNarration.AboutFile(o), text = o.about });
                var facts = OrganNarration.Facts(o);
                for (int i = 0; i < facts.Count; i++)
                    list.items.Add(new Item { file = OrganNarration.FactFile(o, i), text = OrganNarration.FactText(facts[i]) });
                list.items.Add(new Item { file = OrganNarration.CardFile(o), text = OrganNarration.CardText(o) });
            }
            list.karakter = list.items.Sum(i => i.text.Length);
            Directory.CreateDirectory(Path.GetDirectoryName(JsonPath));
            File.WriteAllText(JsonPath, JsonUtility.ToJson(list, true), new UTF8Encoding(false));
            return list.items.Count + " teks narasi (" + list.karakter + " karakter) ditulis ke " + JsonPath;
        }

        [MenuItem("Organo/Suara/Pasang Rekaman Narasi", priority = 61)]
        public static void ImportMenu() => Debug.Log("[Organo] " + Import());

        /// <summary>Pasang MP3/OGG/WAV di AudioDir ke asset organ (narration, factNarration, cardNarration).</summary>
        public static string Import()
        {
            AssetDatabase.Refresh();
            var db = OrganoSetup.EnsureDatabase();
            int clips = 0, organs = 0;
            foreach (var o in db.organs.Where(x => x != null))
            {
                bool any = false;
                var about = Load(OrganNarration.AboutFile(o));
                var card = Load(OrganNarration.CardFile(o));
                var facts = OrganNarration.Facts(o);
                var factClips = new AudioClip[facts.Count];
                for (int i = 0; i < facts.Count; i++) { factClips[i] = Load(OrganNarration.FactFile(o, i)); if (factClips[i] != null) clips++; }
                if (about != null) clips++;
                if (card != null) clips++;
                any = about != null || card != null || factClips.Any(c => c != null);
                o.narration = about;
                o.cardNarration = card;
                o.factNarration = factClips.Any(c => c != null) ? factClips : new AudioClip[0];
                EditorUtility.SetDirty(o);
                if (any) organs++;
            }
            AssetDatabase.SaveAssets();
            return clips + " rekaman dipasang ke " + organs + " organ (" + AudioDir + ")";
        }

        static AudioClip Load(string name)
        {
            foreach (var ext in new[] { ".mp3", ".ogg", ".wav" })
            {
                var path = AudioDir + "/" + name + ext;
                if (!File.Exists(path)) continue;
                Configure(path);
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            return null;
        }

        /// <summary>Mono, Vorbis kualitas sedang, dimuat saat diputar: rekaman suara kecil di APK (±1–3 MB total).</summary>
        static void Configure(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as AudioImporter;
            if (imp == null) return;
            var s = imp.defaultSampleSettings;
            bool dirty = !imp.forceToMono || s.compressionFormat != AudioCompressionFormat.Vorbis || s.loadType != AudioClipLoadType.CompressedInMemory || Mathf.Abs(s.quality - 0.45f) > 0.01f;
            if (!dirty) return;
            imp.forceToMono = true;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.45f;
            s.loadType = AudioClipLoadType.CompressedInMemory;
            s.preloadAudioData = false;
            imp.defaultSampleSettings = s;
            imp.SaveAndReimport();
        }
    }
}
