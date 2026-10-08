using System;
using System.Collections.Generic;
using UnityEngine;

namespace Organo.Data
{
    public enum OrganSystem { Pencernaan, Pernapasan, PeredaranDarah, Saraf }
    public enum BodyRegion { Kepala, Leher, Dada, Perut }

    [Serializable]
    public class OrganHotspotInfo
    {
        public string title;
        public string description;
        [Tooltip("Posisi titik info dalam ruang model yang sudah dinormalisasi (kira-kira -0.5..0.5). " +
                 "Bisa diganti dengan komponen OrganHotspot di dalam prefab model.")]
        public Vector3 localPosition;
    }

    /// <summary>
    /// Satu organ di Organo. Asset ini adalah "slot" untuk konten: teks, warna, model 3D close-up,
    /// suara penjelasan, dan gambar kartu untuk AR.
    /// </summary>
    [CreateAssetMenu(menuName = "Organo/Organ Info", fileName = "Organ")]
    public class OrganInfo : ScriptableObject
    {
        [Header("Identitas")]
        public string id = "lambung";
        public string displayName = "Lambung";
        public OrganSystem system = OrganSystem.Pencernaan;
        public BodyRegion region = BodyRegion.Perut;
        [Tooltip("Nomor kartu (1-7) dan season.")]
        public int cardNumber = 1;
        public int season = 1;

        [Header("Teks")]
        [TextArea(3, 8)] public string about;
        [TextArea(2, 4)] public string funFact;
        [TextArea(1, 3)] public string shortDescription;
        [Tooltip("Penjelasan singkat saat menjawab misi scan.")]
        [TextArea(1, 3)] public string function;

        [Header("Warna kartu (dari desain)")]
        public Color tint = new Color32(0xFF, 0xF1, 0xF4, 0xFF);
        public Color ring = new Color32(0xF5, 0xB7, 0xC6, 0xFF);
        public Color accent = new Color32(0xEF, 0x47, 0x6F, 0xFF);

        [Header("3D close-up  —  SLOT MODEL")]
        [Tooltip("Tarik prefab model 3D organ ke sini (FBX/GLB/prefab). Kalau kosong, Organo menampilkan model placeholder.")]
        public GameObject modelPrefab;
        [Tooltip("Rotasi awal model di viewer close-up.")]
        public Vector3 modelEuler = new Vector3(8f, -20f, 0f);
        [Tooltip("Pengali ukuran setelah model dinormalisasi agar pas di viewer.")]
        public float modelScale = 1f;
        public List<OrganHotspotInfo> hotspots = new List<OrganHotspotInfo>();

        [Header("Suara penjelasan")]
        [Tooltip("Rekaman narasi (about + fakta). Dipakai saat anak mengetuk organ di close-up.")]
        public AudioClip narration;

        [Header("AR  —  kartu fisik")]
        [Tooltip("Gambar sisi depan kartu Organo. Dipakai untuk membuat XR Reference Image Library (menu Organo > Bangun Library Kartu AR).")]
        public Texture2D cardImage;
        [Tooltip("Lebar kartu fisik dalam meter (kartu remi standar 0.063).")]
        public float cardWidthMeters = 0.063f;
        [Tooltip("Model untuk AR. Kosong = pakai modelPrefab.")]
        public GameObject arModelPrefab;
        [Tooltip("Tinggi model di atas kartu (meter).")]
        public float arModelSize = 0.08f;

        public string ReferenceImageName => "organo-" + id;
        public string IconKey => "organ_" + id;
        public string SystemLabel => "Sistem " + OrganDefaults.SystemName(system);
        public string WhereLabel => OrganDefaults.RegionName(region);
        public GameObject ArPrefab => arModelPrefab != null ? arModelPrefab : modelPrefab;
    }
}
