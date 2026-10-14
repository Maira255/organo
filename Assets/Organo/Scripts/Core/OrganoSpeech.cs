using System.Collections.Generic;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Organo
{
    /// <summary>
    /// Narasi organ dengan suara mesin text-to-speech HP (Bahasa Indonesia), dipakai kalau rekaman suara
    /// (slot Narration) belum diisi.
    /// - Android: android.speech.tts.TextToSpeech. Mesin <b>Google Speech Services</b> diutamakan (suaranya jauh lebih alami
    ///   daripada mesin bawaan sebagian HP), lalu dipilih suara Indonesia terpasang dengan kualitas tertinggi. Anak/guru bisa
    ///   memilih suara lain di Profil > Aksesibilitas (<see cref="Voices"/>). Butuh &lt;queries&gt; TTS_SERVICE (ditambahkan saat build).
    /// - iOS: AVSpeechSynthesizer lewat Plugins/iOS/OrganoSpeech.mm (suara id-ID kualitas tertinggi yang terpasang).
    /// - Editor / laptop: tidak ada suara (teks tetap disorot kalimat demi kalimat).
    /// </summary>
    public static class OrganoSpeech
    {
        public enum State { NotSupported, Starting, Ready, MissingLanguage, Failed }

        public static State Status { get; private set; } = State.NotSupported;
        /// <summary>Siap berbicara Bahasa Indonesia (kalau suara Indonesia belum terpasang, tidak berbicara supaya tidak terdengar aneh).</summary>
        public static bool Available => Status == State.Ready;

        /// <summary>Nada suara: sedikit lebih ceria dari normal, tetap terdengar wajar.</summary>
        public const float Pitch = 1.05f;
        /// <summary>Tempo dasar: sedikit lebih pelan dari bawaan supaya jelas untuk anak (dikali pilihan Lambat/Normal/Cepat).</summary>
        public const float BaseRate = 0.92f;
        const string VoicePref = "organo.tts.voice";

        /// <summary>Satu pilihan suara pembaca Bahasa Indonesia di HP ini.</summary>
        public class VoiceOption
        {
            public string id;
            public string label;
            public bool network;
            public int quality;
#if UNITY_ANDROID && !UNITY_EDITOR
            internal AndroidJavaObject voice;
#endif
        }

        /// <summary>Suara Indonesia yang terpasang (kosong di Editor / iOS / sebelum mesin siap).</summary>
        public static readonly List<VoiceOption> Voices = new List<VoiceOption>();
        /// <summary>Id suara yang sedang dipakai (null = bawaan mesin).</summary>
        public static string CurrentVoice { get; private set; }
        /// <summary>Nama mesin suara yang dipakai (mis. com.google.android.tts).</summary>
        public static string Engine { get; private set; } = "-";

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void OrganoSpeech_Speak(string text, float rate, float pitch);
        [DllImport("__Internal")] static extern void OrganoSpeech_Stop();
        [DllImport("__Internal")] static extern bool OrganoSpeech_IsSpeaking();
        [DllImport("__Internal")] static extern bool OrganoSpeech_HasIndonesian();
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        const string GoogleTts = "com.google.android.tts";
        static AndroidJavaObject s_Tts;
        static bool s_VoicesLoaded;

        class InitListener : AndroidJavaProxy
        {
            public InitListener() : base("android.speech.tts.TextToSpeech$OnInitListener") { }

            // Dipanggil Android (bukan thread utama Unity) setelah mesin TTS siap: jangan sentuh API Unity di sini.
            public void onInit(int status)
            {
                if (status != 0 || s_Tts == null) { Status = State.Failed; return; }
                try
                {
                    using (var locale = new AndroidJavaObject("java.util.Locale", "id", "ID"))
                    {
                        int r = s_Tts.Call<int>("setLanguage", locale);
                        // LANG_MISSING_DATA = -1, LANG_NOT_SUPPORTED = -2
                        Status = r < 0 ? State.MissingLanguage : State.Ready;
                    }
                }
                catch { Status = State.Failed; }
            }
        }
#endif

        /// <summary>Siapkan mesin suara (dipanggil sekali saat aplikasi mulai).</summary>
        public static void Init()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (s_Tts != null) return;
            Status = State.Starting;
            try
            {
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        // Mesin Google diutamakan; kalau tidak terpasang Android memakai mesin bawaan HP.
                        try { s_Tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, new InitListener(), GoogleTts); }
                        catch (System.Exception e) { Debug.LogWarning("[Organo] TTS: " + e.Message); Status = State.Failed; }
                    }));
                }
            }
            catch (System.Exception e) { Debug.LogWarning("[Organo] TTS: " + e.Message); Status = State.Failed; }
#elif UNITY_IOS && !UNITY_EDITOR
            Status = OrganoSpeech_HasIndonesian() ? State.Ready : State.MissingLanguage;
            Engine = "AVSpeechSynthesizer (suara id-ID terbaik)";
#else
            Status = State.NotSupported;
#endif
        }

        /// <summary>Muat daftar suara Indonesia & pakai pilihan tersimpan / suara terbaik (di thread utama).</summary>
        public static void EnsureVoices()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (s_VoicesLoaded || !Available || s_Tts == null) return;
            s_VoicesLoaded = true;
            try
            {
                // Android tidak memberi tahu mesin yang sedang dipakai; mesin Google dipakai kalau terpasang.
                bool google = false;
                using (var list = s_Tts.Call<AndroidJavaObject>("getEngines"))
                {
                    var engines = list != null ? list.Call<AndroidJavaObject[]>("toArray") : null;
                    if (engines != null) foreach (var e in engines) if (e != null && e.Get<string>("name") == GoogleTts) google = true;
                }
                Engine = google ? "Google Speech Services" : s_Tts.Call<string>("getDefaultEngine");
            }
            catch { }
            try
            {
                Voices.Clear();
                var set = s_Tts.Call<AndroidJavaObject>("getVoices");
                var arr = set != null ? set.Call<AndroidJavaObject[]>("toArray") : null;
                if (arr != null)
                    foreach (var v in arr)
                    {
                        if (v == null) continue;
                        string lang;
                        using (var loc = v.Call<AndroidJavaObject>("getLocale")) lang = loc.Call<string>("getLanguage");
                        if (lang != "in" && lang != "id" && lang != "ind") continue;
                        bool notInstalled = false;
                        using (var feats = v.Call<AndroidJavaObject>("getFeatures"))
                            if (feats != null) notInstalled = feats.Call<bool>("contains", "notInstalled");
                        if (notInstalled) continue;
                        Voices.Add(new VoiceOption
                        {
                            id = v.Call<string>("getName"),
                            network = v.Call<bool>("isNetworkConnectionRequired"),
                            quality = v.Call<int>("getQuality"),
                            voice = v,
                        });
                    }
                // offline dulu (tetap jalan tanpa internet), lalu kualitas tertinggi
                Voices.Sort((a, b) => a.network != b.network ? (a.network ? 1 : -1) : b.quality != a.quality ? b.quality - a.quality : string.CompareOrdinal(a.id, b.id));
                for (int i = 0; i < Voices.Count; i++)
                    Voices[i].label = "Suara " + (i + 1) + (Voices[i].network ? " (butuh internet)" : "");
                string saved = PlayerPrefs.GetString(VoicePref, "");
                var pick = Voices.Find(x => x.id == saved) ?? (Voices.Count > 0 ? Voices[0] : null);
                if (pick != null) ApplyVoice(pick);
            }
            catch (System.Exception e) { Debug.LogWarning("[Organo] TTS voices: " + e.Message); }
#endif
        }

        /// <summary>Pakai suara tertentu (disimpan untuk berikutnya).</summary>
        public static void SetVoice(string id)
        {
            var v = Voices.Find(x => x.id == id);
            if (v == null) return;
            PlayerPrefs.SetString(VoicePref, id);
            PlayerPrefs.Save();
#if UNITY_ANDROID && !UNITY_EDITOR
            ApplyVoice(v);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static void ApplyVoice(VoiceOption v)
        {
            try { if (s_Tts.Call<int>("setVoice", v.voice) == 0) CurrentVoice = v.id; }
            catch (System.Exception e) { Debug.LogWarning("[Organo] TTS setVoice: " + e.Message); }
        }
#endif

        /// <summary>Bacakan teks. <paramref name="rate"/> 1 = normal.</summary>
        public static bool Speak(string text, float rate)
        {
            if (!Available || string.IsNullOrEmpty(text)) return false;
            EnsureVoices();
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                s_Tts.Call<int>("setSpeechRate", rate * BaseRate);
                s_Tts.Call<int>("setPitch", Pitch);
                using (var bundle = new AndroidJavaObject("android.os.Bundle"))
                    return s_Tts.Call<int>("speak", text, 0, bundle, "organo") == 0; // QUEUE_FLUSH
            }
            catch (System.Exception e) { Debug.LogWarning("[Organo] TTS speak: " + e.Message); return false; }
#elif UNITY_IOS && !UNITY_EDITOR
            OrganoSpeech_Speak(text, rate * BaseRate, Pitch);
            return true;
#else
            return false;
#endif
        }

        public static void Stop()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { s_Tts?.Call<int>("stop"); } catch { }
#elif UNITY_IOS && !UNITY_EDITOR
            OrganoSpeech_Stop();
#endif
        }

        public static bool Speaking
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try { return s_Tts != null && s_Tts.Call<bool>("isSpeaking"); } catch { return false; }
#elif UNITY_IOS && !UNITY_EDITOR
                return OrganoSpeech_IsSpeaking();
#else
                return false;
#endif
            }
        }
    }
}
