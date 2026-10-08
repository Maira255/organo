using System;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Organo.AR
{
    public enum PermissionState { Unknown, Granted, Denied }

    /// <summary>Izin kamera untuk scan kartu (Android/iOS). Di Editor selalu diizinkan.</summary>
    public static class CameraPermission
    {
        const string AskedKey = "organo.camera.asked";

        public static bool AskedBefore => PlayerPrefs.GetInt(AskedKey, 0) == 1;

        public static PermissionState Current
        {
            get
            {
#if UNITY_EDITOR
                return AskedBefore ? PermissionState.Granted : PermissionState.Unknown;
#elif UNITY_ANDROID
                if (Permission.HasUserAuthorizedPermission(Permission.Camera)) return PermissionState.Granted;
                return AskedBefore ? PermissionState.Denied : PermissionState.Unknown;
#elif UNITY_IOS
                if (Application.HasUserAuthorization(UserAuthorization.WebCam)) return PermissionState.Granted;
                return AskedBefore ? PermissionState.Denied : PermissionState.Unknown;
#else
                return PermissionState.Granted;
#endif
            }
        }

        public static void Request(Action<bool> done)
        {
            PlayerPrefs.SetInt(AskedKey, 1);
#if UNITY_EDITOR
            done?.Invoke(true);
#elif UNITY_ANDROID
            var cb = new PermissionCallbacks();
            cb.PermissionGranted += _ => done?.Invoke(true);
            cb.PermissionDenied += _ => done?.Invoke(false);
            Permission.RequestUserPermission(Permission.Camera, cb);
#elif UNITY_IOS
            OrganoApp.Instance.StartCoroutine(RequestIos(done));
#else
            done?.Invoke(true);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        static System.Collections.IEnumerator RequestIos(Action<bool> done)
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            done?.Invoke(Application.HasUserAuthorization(UserAuthorization.WebCam));
        }
#endif

        /// <summary>Buka halaman pengaturan aplikasi supaya anak/orang tua bisa menyalakan izin kamera.</summary>
        public static void OpenAppSettings()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                using (var uri = uriClass.CallStatic<AndroidJavaObject>("fromParts", "package", Application.identifier, null))
                using (var intent = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS", uri))
                {
                    intent.Call<AndroidJavaObject>("addFlags", 0x10000000);
                    activity.Call("startActivity", intent);
                }
            }
            catch (Exception e) { Debug.LogWarning("[Organo] open settings: " + e.Message); }
#elif UNITY_IOS && !UNITY_EDITOR
            Application.OpenURL("app-settings:");
#else
            Debug.Log("[Organo] (Editor) buka Pengaturan > Izin Kamera di HP.");
#endif
        }
    }
}
