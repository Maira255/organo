using UnityEngine;

namespace Organo
{
    /// <summary>Suara UI & narasi organ. Klip bisa diisi di komponen OrganoApp (slot audio).</summary>
    public static class OrganoAudio
    {
        static AudioSource s_Ui, s_Voice;
        static AudioClip s_Click, s_Success, s_Wrong;
        static bool s_Enabled = true;
        static float s_VoicePitch = 1f;

        public static void Init(GameObject host, AudioClip click, AudioClip success, AudioClip wrong)
        {
            s_Ui = host.AddComponent<AudioSource>();
            s_Ui.playOnAwake = false;
            s_Voice = host.AddComponent<AudioSource>();
            s_Voice.playOnAwake = false;
            s_Click = click; s_Success = success; s_Wrong = wrong;
        }

        public static bool Enabled { get => s_Enabled; set { s_Enabled = value; if (!value) StopVoice(); } }

        public static void SetVoiceSpeed(int speed) => s_VoicePitch = speed == 0 ? 0.85f : speed == 2 ? 1.15f : 1f;

        public static void Click() { if (s_Enabled && s_Ui != null && s_Click != null) s_Ui.PlayOneShot(s_Click, 0.5f); }
        public static void Success() { if (s_Enabled && s_Ui != null && s_Success != null) s_Ui.PlayOneShot(s_Success); }
        public static void Wrong() { if (s_Enabled && s_Ui != null && s_Wrong != null) s_Ui.PlayOneShot(s_Wrong); }

        public static bool PlayVoice(AudioClip clip)
        {
            if (s_Voice == null || clip == null || !s_Enabled) return false;
            s_Voice.Stop();
            s_Voice.clip = clip;
            s_Voice.pitch = s_VoicePitch;
            s_Voice.Play();
            return true;
        }

        public static void StopVoice() { if (s_Voice != null) s_Voice.Stop(); }
        public static bool VoicePlaying => s_Voice != null && s_Voice.isPlaying;

        /// <summary>0..1 posisi narasi yang sedang diputar, -1 kalau tidak ada.</summary>
        public static float VoiceProgress =>
            s_Voice != null && s_Voice.clip != null && s_Voice.isPlaying ? s_Voice.time / s_Voice.clip.length : -1f;
    }
}
