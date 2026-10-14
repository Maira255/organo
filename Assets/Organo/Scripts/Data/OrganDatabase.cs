using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Organo.Data
{
    /// <summary>Daftar semua organ. Asset dibuat oleh menu Organo &gt; Setup; kalau kosong, isi bawaan dipakai.</summary>
    [CreateAssetMenu(menuName = "Organo/Organ Database", fileName = "OrganDatabase")]
    public class OrganDatabase : ScriptableObject
    {
        public List<OrganInfo> organs = new List<OrganInfo>();

        [Header("Tampilan \"Lihat di tubuh\" (dibuat oleh Organo > Model 3D)")]
        [Tooltip("Tubuh anak laki-laki transparan untuk close-up mode tubuh.")]
        public GameObject bodyBoyPrefab;
        [Tooltip("Tubuh anak perempuan transparan untuk close-up mode tubuh.")]
        public GameObject bodyGirlPrefab;
        [Tooltip("Material organ lain yang dipudarkan (transparan).")]
        public Material ghostMaterial;
        [Tooltip("Material transparan anak laki-laki (urutan sama dengan submesh prefab) untuk melihat organ di dalam.")]
        public Material[] bodyBoyFaded;
        [Tooltip("Material transparan anak perempuan.")]
        public Material[] bodyGirlFaded;

        Dictionary<string, OrganInfo> m_ById;

        public static OrganDatabase CreateDefault()
        {
            var db = CreateInstance<OrganDatabase>();
            foreach (var id in OrganDefaults.Order) db.organs.Add(OrganDefaults.Create(id));
            return db;
        }

        public void EnsureComplete()
        {
            organs.RemoveAll(o => o == null);
            foreach (var id in OrganDefaults.Order)
                if (organs.All(o => o.id != id)) organs.Add(OrganDefaults.Create(id));
            m_ById = null;
        }

        public OrganInfo Get(string id)
        {
            if (m_ById == null)
            {
                m_ById = new Dictionary<string, OrganInfo>();
                foreach (var o in organs) if (o != null) m_ById[o.id] = o;
            }
            return id != null && m_ById.TryGetValue(id, out var r) ? r : null;
        }

        /// <summary>Pemisah nama gambar kartu gaya lain di library AR, mis. "organo-jantung@kartun".</summary>
        public const char StyleSeparator = '@';

        public OrganInfo FindByReferenceImage(string imageName)
        {
            if (string.IsNullOrEmpty(imageName)) return null;
            int at = imageName.IndexOf(StyleSeparator);
            if (at > 0) imageName = imageName.Substring(0, at);
            return organs.FirstOrDefault(o => o != null && (o.ReferenceImageName == imageName || o.id == imageName || o.displayName == imageName));
        }

        public IEnumerable<OrganInfo> BySystem(OrganSystem s) => organs.Where(o => o != null && o.system == s).OrderBy(o => o.cardNumber);
        public IEnumerable<OrganInfo> ByRegion(BodyRegion r) => organs.Where(o => o != null && o.region == r).OrderBy(o => o.cardNumber);
        public IEnumerable<OrganInfo> Ordered => organs.Where(o => o != null).OrderBy(o => o.cardNumber);
        public int Count => organs.Count(o => o != null);
    }
}
