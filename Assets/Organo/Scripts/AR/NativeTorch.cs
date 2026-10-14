using System;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Organo.AR
{
    /// <summary>
    /// Senter (lampu flash) untuk mode kamera biasa (tanpa AR). Mode AR memakai senter dari AR Foundation.
    /// - iOS: lewat plugin kecil Plugins/iOS/OrganoTorch.mm (AVCaptureDevice).
    /// - Android: lewat CameraManager.setTorchMode; gagal kalau kamera sedang dipakai, jadi dilaporkan tidak didukung.
    /// - Editor / laptop: tidak ada lampu flash.
    /// </summary>
    public static class NativeTorch
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern bool OrganoTorch_IsAvailable();
        [DllImport("__Internal")] static extern bool OrganoTorch_Set(bool on);
#endif
        static bool s_On;
        static bool? s_AndroidOk;

        public static bool Supported
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return OrganoTorch_IsAvailable();
#elif UNITY_ANDROID && !UNITY_EDITOR
                return s_AndroidOk != false && AndroidHasFlash();
#else
                return false;
#endif
            }
        }

        public static bool IsOn => s_On;

        public static bool Set(bool on)
        {
            bool ok = false;
#if UNITY_IOS && !UNITY_EDITOR
            ok = OrganoTorch_Set(on);
#elif UNITY_ANDROID && !UNITY_EDITOR
            ok = AndroidSet(on);
#endif
            s_On = ok && on;
            return ok;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject CameraManager()
        {
            using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                return activity.Call<AndroidJavaObject>("getSystemService", "camera");
        }

        static string BackCameraWithFlash(AndroidJavaObject mgr)
        {
            var ids = mgr.Call<string[]>("getCameraIdList");
            using (var keys = new AndroidJavaClass("android.hardware.camera2.CameraCharacteristics"))
            {
                var flashKey = keys.GetStatic<AndroidJavaObject>("FLASH_INFO_AVAILABLE");
                foreach (var id in ids)
                {
                    using (var ch = mgr.Call<AndroidJavaObject>("getCameraCharacteristics", id))
                    using (var has = ch.Call<AndroidJavaObject>("get", flashKey))
                        if (has != null && has.Call<bool>("booleanValue")) return id;
                }
            }
            return null;
        }

        static bool AndroidHasFlash()
        {
            try { using (var mgr = CameraManager()) return BackCameraWithFlash(mgr) != null; }
            catch { return false; }
        }

        static bool AndroidSet(bool on)
        {
            try
            {
                using (var mgr = CameraManager())
                {
                    var id = BackCameraWithFlash(mgr);
                    if (id == null) return false;
                    mgr.Call("setTorchMode", id, on);
                    s_AndroidOk = true;
                    return true;
                }
            }
            catch (Exception e)
            {
                // CAMERA_IN_USE: kamera sedang dipakai aplikasi, Android tidak mengizinkan senter terpisah
                Debug.LogWarning("[Organo] senter Android: " + e.Message);
                s_AndroidOk = false;
                return false;
            }
        }
#endif
    }
}
