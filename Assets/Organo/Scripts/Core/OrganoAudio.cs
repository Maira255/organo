using UnityEngine;

namespace Organo
{
    /// <summary>
    /// Suara UI & narasi organ. Klip bisa diisi di komponen OrganoApp (slot audio); kalau kosong, efek suara
    /// klik / benar / salah dibuat langsung (sintesis nada lembut), jadi aplikasi tidak pernah sunyi.
    /// Narasi: rekaman (slot Narration di asset organ) atau suara text-to-speech HP (<see cref="OrganoSpeech"/>).
    /// </summary>
    public static class OrganoAudio
    {
        static AudioSource s_Ui, s_Voice;
        static AudioClip s_Click, s_Success, s_Wrong;
        static bool s_Enabled = true;
        static float s_VoicePitch = 1f;
        static int s_VoiceSpeed = 1;
        static bool s_Speaking;

        public static void Init(GameObject host, AudioClip click, AudioClip success, AudioClip wrong)
        {
            s_Ui = host.AddComponent<AudioSource>();
            s_Ui.playOnAwake = false;
            s_Voice = host.AddComponent<AudioSource>();
            s_Voice.playOnAwake = false;
            s_Click = click != null ? click : Synth.Click();
            s_Success = success != null ? success : Synth.Success();
            s_Wrong = wrong != null ? wrong : Synth.Wrong();
            OrganoSpeech.Init();
        }

        public static bool Enabled { get => s_Enabled; set { s_Enabled = value; if (!value) StopVoice(); } }

        static bool s_Narration = true;
        /// <summary>Suara pembaca (rekaman narasi & text-to-speech). Mati = tidak membacakan apa pun.</summary>
        public static bool NarrationEnabled { get => s_Narration; set { s_Narration = value; if (!value) StopVoice(); } }

        public static void SetVoiceSpeed(int speed)
        {
            s_VoiceSpeed = speed;
            s_VoicePitch = speed == 0 ? 0.85f : speed == 2 ? 1.15f : 1f;
        }

        /// <summary>Kecepatan bicara text-to-speech (1 = normal).</summary>
        public static float SpeechRate => s_VoiceSpeed == 0 ? 0.8f : s_VoiceSpeed == 2 ? 1.2f : 1f;

        /// <summary>
        /// Bunyi "tik" saat tombol diketuk. Dimatikan (permintaan pengguna: tidak ada suara untuk setiap aksi);
        /// bunyi benar/salah di kuis dan bunyi kartu dikenali tetap ada karena itu umpan balik, bukan klik.
        /// </summary>
        public static bool ClickSound = false;

        public static void Click() { if (ClickSound && s_Enabled && s_Ui != null && s_Click != null) s_Ui.PlayOneShot(s_Click, 0.45f); }

        /// <summary>Efek suara UI sedang berbunyi (untuk uji).</summary>
        public static bool UiPlaying => s_Ui != null && s_Ui.isPlaying;
        public static void Success() { if (s_Enabled && s_Ui != null && s_Success != null) s_Ui.PlayOneShot(s_Success, 0.8f); }
        public static void Wrong() { if (s_Enabled && s_Ui != null && s_Wrong != null) s_Ui.PlayOneShot(s_Wrong, 0.75f); }

        public static bool PlayVoice(AudioClip clip) => PlayVoiceSequence(clip);

        // rekaman berurutan (mis. paragraf "Tentang" lalu "Tau gak sih? ..."), diputar satu per satu oleh Pump()
        static readonly System.Collections.Generic.List<AudioClip> s_Queue = new System.Collections.Generic.List<AudioClip>();
        static int s_QueueIndex;
        static float s_QueueTotal, s_QueueDone;

        /// <summary>Putar beberapa rekaman berurutan (yang null dilewati). False kalau tidak ada rekaman / suara mati.</summary>
        public static bool PlayVoiceSequence(params AudioClip[] clips)
        {
            if (s_Voice == null || !s_Enabled || !s_Narration || clips == null) return false;
            StopVoice();
            foreach (var c in clips) if (c != null) s_Queue.Add(c);
            if (s_Queue.Count == 0) return false;
            s_QueueTotal = 0f;
            foreach (var c in s_Queue) s_QueueTotal += c.length;
            s_QueueDone = 0f;
            s_QueueIndex = 0;
            PlayClip(s_Queue[0]);
            return true;
        }

        static void PlayClip(AudioClip clip)
        {
            s_Voice.clip = clip;
            s_Voice.pitch = s_VoicePitch;
            s_Voice.Play();
        }

        /// <summary>Lanjutkan ke rekaman berikutnya (dipanggil tiap frame oleh OrganoApp).</summary>
        public static void Pump()
        {
            if (s_Queue.Count == 0 || s_Voice == null || s_Voice.isPlaying) return;
            s_QueueDone += s_Queue[s_QueueIndex].length;
            s_QueueIndex++;
            if (s_QueueIndex >= s_Queue.Count) { s_Queue.Clear(); return; }
            PlayClip(s_Queue[s_QueueIndex]);
        }

        /// <summary>
        /// Bacakan teks dengan suara HP (kalau tidak ada rekaman). False kalau suara tidak tersedia.
        /// preview = contoh suara di Aksesibilitas (tetap berbunyi walau suara pembaca dimatikan).
        /// </summary>
        public static bool SpeakText(string text, bool preview = false)
        {
            if (!s_Enabled || (!s_Narration && !preview)) return false;
            StopVoice();
            s_Speaking = OrganoSpeech.Speak(text, SpeechRate);
            return s_Speaking;
        }

        public static void StopVoice()
        {
            s_Queue.Clear();
            if (s_Voice != null) s_Voice.Stop();
            if (s_Speaking) { OrganoSpeech.Stop(); s_Speaking = false; }
        }

        public static bool VoicePlaying => s_Voice != null && (s_Voice.isPlaying || s_Queue.Count > 0);
        public static bool SpeechPlaying => s_Speaking && OrganoSpeech.Speaking;

        /// <summary>0..1 posisi narasi (seluruh urutan rekaman) yang sedang diputar, -1 kalau tidak ada.</summary>
        public static float VoiceProgress =>
            s_Voice != null && s_Queue.Count > 0 && s_QueueTotal > 0f ? Mathf.Clamp01((s_QueueDone + s_Voice.time) / s_QueueTotal) : -1f;

        /// <summary>Efek suara sederhana yang dibuat dari gelombang (tanpa file audio).</summary>
        static class Synth
        {
            const int Rate = 44100;

            static AudioClip Make(string name, float seconds, System.Func<float, float> wave)
            {
                int n = Mathf.CeilToInt(seconds * Rate);
                var data = new float[n];
                for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
                var clip = AudioClip.Create(name, n, 1, Rate, false);
                clip.SetData(data, 0);
                return clip;
            }

            static float Tone(float t, float freq, float start, float length, float attack = 0.005f)
            {
                float lt = t - start;
                if (lt < 0f || lt > length) return 0f;
                float env = Mathf.Min(1f, lt / attack) * Mathf.Exp(-4.5f * lt / length);
                float ph = 2f * Mathf.PI * freq * lt;
                return env * (Mathf.Sin(ph) + 0.25f * Mathf.Sin(2f * ph) + 0.08f * Mathf.Sin(3f * ph));
            }

            /// <summary>"tik" pendek dan lembut.</summary>
            public static AudioClip Click() => Make("Organo Klik", 0.05f, t => 0.35f * Tone(t, 1250f, 0f, 0.045f, 0.002f));

            /// <summary>Tiga nada naik (do–mi–sol) yang ceria.</summary>
            public static AudioClip Success() => Make("Organo Benar", 0.55f, t =>
                0.32f * (Tone(t, 1046.5f, 0f, 0.22f) + Tone(t, 1318.5f, 0.09f, 0.24f) + Tone(t, 1568f, 0.18f, 0.36f)));

            /// <summary>Dua nada turun yang lembut (tidak mengagetkan).</summary>
            public static AudioClip Wrong() => Make("Organo Salah", 0.45f, t =>
                0.3f * (Tone(t, 392f, 0f, 0.2f, 0.01f) + Tone(t, 311f, 0.16f, 0.28f, 0.01f)));
        }
    }
}
