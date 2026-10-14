using UnityEditor;
using UnityEngine;

namespace Organo.EditorTools
{
    /// <summary>Pengaturan build supaya Organo bisa dipasang di HP anak SD pada umumnya.</summary>
    public static class OrganoBuildSettings
    {
        [MenuItem("Organo/Terapkan Pengaturan Build HP", priority = 24)]
        public static void Apply()
        {
            // Android 7.1 (API 25) = batas minimum Unity 6.3 (ARCore butuh API 24+); sebelumnya API 34 (Android 14) sehingga kebanyakan HP tidak bisa memasang.
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.iOS.cameraUsageDescription = "Organo memakai kamera untuk memindai kartu organ dan menampilkan organ 3D. Gambar kamera tidak disimpan.";
            AssetDatabase.SaveAssets();
            Debug.Log("[Organo] Pengaturan build: Android min API " + (int)PlayerSettings.Android.minSdkVersion + ", target otomatis; teks izin kamera iOS diperbarui.");
        }
    }
}
